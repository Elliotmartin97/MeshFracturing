using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


// TO DO
//Mesh Index is fucked when generating points, cylinders especially
//Rebuilding triangles currently gets nearest point from invalid vert, ends up in middle, maybe rebuild triangles when generating points as the old points are simply moved to slice pos 

public class MeshSlicing : MonoBehaviour
{
    public float radius = 0.025f;
    private Mesh mesh;
    private GameObject cam_pos;
    public Vector3 start_point;
    public List<Vector3> points = new List<Vector3>();
    private Vector3 min_dist;
    private Vector3 new_point;
    private Vector3 vwp;
    public Vector3 center;
    private MeshCollider mesh_col;
    private Color point_color;
    private GameObject copy;
    //public GameObject slice_viewer;
    public float slice_angle = 0.0f;
    private Ray ray;
    private Transform slice_transform;

    // Start is called before the first frame update
    void Start()
    {
        cam_pos = GameObject.Find("Main Camera");
        mesh = GetComponent<MeshFilter>().mesh;
        mesh_col = GetComponent<MeshCollider>();
        point_color = Color.green;
        copy = gameObject;
    }

    // Update is called once per frame
    void Update()
    {
        center = transform.TransformPoint(mesh.bounds.center);
        Camera cam = cam_pos.GetComponent<Camera>();
        ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 1000))
        {
            if(hit.collider.gameObject == this.gameObject)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Slice(hit.point);
                }
                // Slice(hit.point);
            }
        }

        if(Input.GetAxis("Mouse ScrollWheel") > 0.0f)
        {
            slice_angle++;
        }
        else if (Input.GetAxis("Mouse ScrollWheel") < 0.0f) 
        {
            slice_angle--;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if (start_point != Vector3.zero)
        {
            Gizmos.DrawSphere(start_point, radius);
        }
        if (points.Count > 0)
        {
            for(int i = 0; i < points.Count; i++)
            {
                Gizmos.color = point_color;
                Gizmos.DrawSphere(points[i], radius);
            }
           
        }

    }

    void Slice(Vector3 start)
    {
        points.Clear();
        start_point = start;

        GameObject slice_viewer = new GameObject();
        slice_transform = slice_viewer.transform;
        slice_transform.position = start;
        slice_transform.rotation = Quaternion.LookRotation(ray.direction);
        slice_transform.Rotate(transform.forward, -slice_angle);


        List<Vector3> left_verts = new List<Vector3>();
        List<Vector2> left_uvs = new List<Vector2>();
        List<Vector3> left_normals = new List<Vector3>();

        List<Vector3> right_verts = new List<Vector3>();
        List<Vector2> right_uvs = new List<Vector2>();
        List<Vector3> right_normals = new List<Vector3>();

        GenerateSlicePoints(slice_transform);

        for (int i = 0; i < mesh.vertexCount; i++)
        {
            Vector3 v = transform.TransformPoint(mesh.vertices[i]);
            Vector3 relative_point = slice_transform.InverseTransformPoint(v);
            if (relative_point.x < 0.0f)
            {
                Debug.Log("relative");
                left_verts.Add(mesh.vertices[i]);
                left_uvs.Add(mesh.uv[i]);
                left_normals.Add(mesh.normals[i]);
            }
            else
            {
                Debug.Log("nope");
                right_verts.Add(mesh.vertices[i]);
                right_uvs.Add(mesh.uv[i]);
                right_normals.Add(mesh.normals[i]);
            }
        }
        GameObject new_obj = Instantiate(copy);

        Mesh old_mesh = new Mesh();
        old_mesh.vertices = mesh.vertices;
        old_mesh.uv = mesh.uv;
        old_mesh.normals = mesh.normals;
        old_mesh.triangles = mesh.triangles;


        RebuildMesh(mesh, old_mesh, left_verts, left_uvs, left_normals);
        mesh_col.sharedMesh = mesh;

        new_obj.name = gameObject.name + "_right";
        Mesh right_mesh = new_obj.GetComponent<MeshFilter>().mesh;
        RebuildMesh(right_mesh, old_mesh, right_verts, right_uvs, right_normals);
        new_obj.GetComponent<MeshFilter>().mesh = right_mesh;
        new_obj.GetComponent<MeshCollider>().sharedMesh = right_mesh;
        gameObject.name = gameObject.name + "_left";

        Destroy(slice_viewer);
    }

    void GenerateSlicePoints(Transform slice_dir)
    {
        int[] tris = mesh.triangles;

        for (int i = 0; i < tris.Length; i += 3)
        {
            
            if (!IsRelative(tris[i]))
            {
                Vector3 n = FindNearestRelative(tris[i], 0);
                Vector3 v = transform.TransformPoint(mesh.vertices[tris[i]]);
                MoveTowardRelative(v, n);
            }
            if(!IsRelative(tris[i+1]))
            {
                Vector3 n = FindNearestRelative(tris[i], 1);
                Vector3 v = transform.TransformPoint(mesh.vertices[tris[i+1]]);
                MoveTowardRelative(v, n);
            }
            if(!IsRelative(tris[i+2]))
            {
                Vector3 n = FindNearestRelative(tris[i], 2);
                Vector3 v = transform.TransformPoint(mesh.vertices[tris[i+2]]);
                MoveTowardRelative(v, n);
            }
        }
    }

    bool IsRelative(int vert_index)
    {
        Vector3 vert = mesh.vertices[vert_index];
        Vector3 v = transform.TransformPoint(vert);
        Vector3 relative_point = slice_transform.InverseTransformPoint(v);
        if (relative_point.x < 0.0f)
        {
            return true;
        }
        return false;
    }

    Vector3 FindNearestRelative(int tri_index, int vert_index)
    {
        Vector3 nearest = Vector3.positiveInfinity;
        Vector3 current = transform.TransformPoint(mesh.vertices[tri_index + vert_index]);
        for (int i = 0; i < 3; i++)
        {
            Vector3 v = transform.TransformPoint(mesh.vertices[tri_index + i]);
            if(i != vert_index && IsRelative(tri_index + i))
            {
                if(Vector3.Distance(current, v) < Vector3.Distance(current, nearest))
                {
                    nearest = v;
                }
            }
        }
        return nearest;
    }

    void MoveTowardRelative(Vector3 invalid, Vector3 valid)
    {
        Vector3 new_point = invalid;
        Vector3 relative_point = slice_transform.InverseTransformPoint(new_point);
        int count = 0;
        while(relative_point.x > 0.0f)
        {
            new_point = Vector3.MoveTowards(new_point, valid, 0.01f);
            count++;
            relative_point = slice_transform.InverseTransformPoint(new_point);
            if(count > 500)
            {
                Debug.Log("BROKE THE WHILE LOOP");
                break;
            }
        }
        points.Add(new_point);
    }

    void RebuildMesh(Mesh current_mesh, Mesh old_mesh, List<Vector3> new_verts, List<Vector2> new_uvs, List<Vector3> new_normals)
    {
        Mesh new_mesh = new Mesh();
        new_mesh.vertices = old_mesh.vertices;
        new_mesh.uv = old_mesh.uv;
        new_mesh.normals = old_mesh.normals;
        new_mesh.triangles = old_mesh.triangles;

        Vector3[] verts = new_mesh.vertices;
        Vector2[] uvs = new_mesh.uv;
        Vector3[] normals = new_mesh.normals;
        int[] tris = new_mesh.triangles;

        List<int> rebuilt_tris = new List<int>();
        int v_count = new_verts.Count;
        //add triangles from existing verts
        for (int i = 0; i < tris.Length; i += 3)
        {
            List<Vector3> mesh_verts = new List<Vector3>();
            List<Vector2> mesh_uvs = new List<Vector2>();
            List<Vector3> mesh_normals = new List<Vector3>();

            mesh_verts.Add(verts[tris[i + 0]]);
            mesh_verts.Add(verts[tris[i + 1]]);
            mesh_verts.Add(verts[tris[i + 2]]);
            mesh_uvs.Add(uvs[tris[i + 0]]);
            mesh_uvs.Add(uvs[tris[i + 1]]);
            mesh_uvs.Add(uvs[tris[i + 2]]);
            mesh_normals.Add(normals[tris[i + 0]]);
            mesh_normals.Add(normals[tris[i + 1]]);
            mesh_normals.Add(normals[tris[i + 2]]);

            int[] tri_indexes = { -1, -1, -1 };
            int invalid_count = 3;

            for (int j = 0; j < v_count; j++)
            {
                for (int z = 0; z < 3; z++)
                {
                    if (mesh_verts[z] == new_verts[j] && mesh_uvs[z] == new_uvs[j] && mesh_normals[z] == new_normals[j])
                    {
                        //first vert is valid
                        tri_indexes[z] = j;
                        invalid_count--;
                    }
                }
            }
            if (tri_indexes[0] != -1 && tri_indexes[1] != -1 && tri_indexes[2] != -1)
            {
                //if all 3 verts are valid, add triangle
                for (int z = 0; z < 3; z++)
                {
                    rebuilt_tris.Add(tri_indexes[z]);
                }
            }
            else if (invalid_count > 0)
            {
                Vector3 closest_point = Vector3.positiveInfinity;
                for (int z = 0; z < 3; z++)
                {
                    
                    if (tri_indexes[z] == -1)
                    {

                        //mesh_verts[z] compare and set then add to new verts
                        for (int p = 0; p < points.Count; p++)
                        {
                            Vector3 vlp = transform.InverseTransformPoint(points[p]);
                            if (Vector3.Distance(mesh_verts[z], vlp) < Vector3.Distance(mesh_verts[z], closest_point))
                            {
                                closest_point = vlp;
                            }
                        }

                        new_verts.Add(closest_point);
                        new_uvs.Add(mesh_uvs[z]);
                        new_normals.Add(mesh_normals[z]);
                        tri_indexes[z] = new_verts.Count - 1;
                        rebuilt_tris.Add(tri_indexes[z]);
                    }
                    else
                    {
                        rebuilt_tris.Add(tri_indexes[z]);
                    }
                }
            }
        }
        current_mesh.Clear();
        current_mesh.vertices = new_verts.ToArray();
        current_mesh.uv = new_uvs.ToArray();
        current_mesh.normals = new_normals.ToArray();
        current_mesh.triangles = rebuilt_tris.ToArray();
        current_mesh.RecalculateNormals();
        current_mesh.RecalculateBounds();
        current_mesh.RecalculateTangents();
    }
}
