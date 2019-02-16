using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


// TO DO
//Rebuilding triangles currently gets nearest point from invalid vert, ends up in middle, maybe rebuild triangles when generating points as the old points are simply moved to slice pos 

public class MeshSlicing : MonoBehaviour
{
    public float radius = 0.025f;
    private Mesh mesh;
    private GameObject cam_pos;
    public Vector3 start_point;
    public List<Vector3> right_points = new List<Vector3>();
    public List<Vector3> left_points = new List<Vector3>();
    private Vector3 min_dist;
    private Vector3 vwp;
    public Vector3 center;
    private MeshCollider mesh_col;
    private Color point_color;
    private GameObject copy;
    //public GameObject slice_viewer;
    public float slice_angle = 0.0f;
    private Ray ray;
    private Transform slice_transform;

    private List<int> left_triangles = new List<int>();
    private List<Vector3> left_verts = new List<Vector3>();
    private List<Vector2> left_uvs = new List<Vector2>();
    private List<Vector3> left_normals = new List<Vector3>();
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
                //Slice(hit.point);
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
        if (right_points.Count > 0)
        {
            for(int i = 0; i < right_points.Count; i++)
            {
                Gizmos.color = point_color;
                Gizmos.DrawSphere(right_points[i], radius);
            }
           
        }
        if (left_points.Count > 0)
        {
            for (int i = 0; i < left_points.Count; i++)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(left_points[i], radius);
            }

        }

    }

    void Slice(Vector3 start)
    {
        right_points.Clear();
        left_points.Clear();
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

        //GenerateSlicePoints(slice_transform);

        for (int i = 0; i < mesh.vertexCount; i++)
        {
            Vector3 v = transform.TransformPoint(mesh.vertices[i]);
            Vector3 relative_point = slice_transform.InverseTransformPoint(v);
            if (relative_point.x < 0.0f)
            {
                left_verts.Add(mesh.vertices[i]);
                left_uvs.Add(mesh.uv[i]);
                left_normals.Add(mesh.normals[i]);
            }
            else
            {
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

        Mesh left_mesh = mesh;

        new_obj.name = gameObject.name + "_right";
        Mesh right_mesh = new_obj.GetComponent<MeshFilter>().mesh;
        RebuildMesh(right_mesh, old_mesh, right_verts, right_uvs, right_normals, false);
        new_obj.GetComponent<MeshFilter>().mesh = right_mesh;
        new_obj.GetComponent<MeshCollider>().sharedMesh = right_mesh;

        RebuildMesh(left_mesh, old_mesh, left_verts, left_uvs, left_normals, true);
        gameObject.name = gameObject.name + "_left";
        mesh = left_mesh;
        mesh_col.sharedMesh = mesh;
        Destroy(slice_viewer);
    }

    void GenerateSlicePoints(Transform slice_dir)
    {
        int[] tris = mesh.triangles;
        Vector3[] verts = mesh.vertices;
        Vector2[] uvs = mesh.uv;
        Vector3[] normals = mesh.normals;
        for (int i = 0; i < tris.Length; i += 3)
        {
            int invalid = 0;
            //only generate points on faces are sliced
            for (int idx = 0; idx < 3; idx++)
            {
                if(!IsRelative(i + idx))
                {
                    invalid++;
                }
            }
            if (invalid > 0 && invalid < 3)
            {
                //slicing through this triangle
                SetSlicePoints(i, true, invalid);
                SetSlicePoints(i, false, invalid);
            }
        }
    }

    bool IsRelative(int vert_index)
    {
        int[] tris = mesh.triangles;
        Vector3 local_vert = mesh.vertices[tris[vert_index]];
        Vector3 world_vert = transform.TransformPoint(local_vert);
        Vector3 relative_point = slice_transform.InverseTransformPoint(world_vert);
        if (relative_point.x < 0.0f)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    void SetSlicePoints(int tri_index, bool side, int invalid_count)
    {
        int[] tris = mesh.triangles;
        if (side)
        {
            Vector3 nearest_valid = Vector3.positiveInfinity;
            Vector3 current_invalid = Vector3.positiveInfinity;
            for (int i = 0; i < 3; i++)
            {
                if (!IsRelative(tri_index + i))
                {
                    current_invalid = mesh.vertices[tris[tri_index + i]];
                }
            }
            for (int z = 0; z < 3; z++)
            {
                Vector3 v = transform.TransformPoint(mesh.vertices[tris[tri_index + z]]);
                if (IsRelative(tri_index + z))
                {
                    if (Vector3.Distance(current_invalid, v) < Vector3.Distance(current_invalid, nearest_valid))
                    {
                        nearest_valid = v;
                    }
                }
            }
            MoveTowardRelative(tri_index, nearest_valid, true);
        }
        else
        {
            Vector3 nearest_invalid = Vector3.positiveInfinity;
            Vector3 current_valid = Vector3.positiveInfinity;
            for (int i = 0; i < 3; i++)
            {
                if (IsRelative(tri_index + i))
                {
                    current_valid = mesh.vertices[tris[tri_index + i]];
                }
            }
            for (int z = 0; z < 3; z++)
            {
                Vector3 v = transform.TransformPoint(mesh.vertices[tris[tri_index + z]]);
                if (!IsRelative(tri_index + z))
                {
                    if (Vector3.Distance(current_valid, v) < Vector3.Distance(current_valid, nearest_invalid))
                    {
                        nearest_invalid = v;
                    }
                }
            }  
            MoveTowardRelative(tri_index, nearest_invalid, false);
        }
    }

    void RebuildMesh(Mesh current_mesh, Mesh old_mesh, List<Vector3> new_verts, List<Vector2> new_uvs, List<Vector3> new_normals, bool side)
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
            else if (invalid_count > 0 && invalid_count < 3)
            {
                int[] temp_tri_index = tri_indexes;
                for (int z = 0; z < 3; z++)
                {
                    if (temp_tri_index[z] == -1)
                    {
                        //mesh_verts[z] compare and set then add to new verts
                        Vector3 invalid_vert = mesh_verts[z];
                        Vector3 nearest_valid = Vector3.positiveInfinity;
                        int valid_index = int.MinValue;
                        for (int k = 0; k < 3; k++)
                        {
                            if(side)
                            {
                                if (IsRelative(i + k))
                                {
                                    Vector3 current_valid = mesh_verts[k];

                                    if (Vector3.Distance(invalid_vert, current_valid) < Vector3.Distance(invalid_vert, nearest_valid))
                                    {
                                        nearest_valid = current_valid;
                                        valid_index = k;
                                        left_points.Add(transform.TransformPoint(current_valid));
                                    }
                                }
                            }
                            else
                            {
                                if (!IsRelative(i + k))
                                {
                                    Vector3 current_valid = mesh_verts[k];

                                    if (Vector3.Distance(invalid_vert, current_valid) < Vector3.Distance(invalid_vert, nearest_valid))
                                    {
                                        nearest_valid = current_valid;
                                        valid_index = k;
                                        left_points.Add(transform.TransformPoint(current_valid));
                                    }
                                }
                            }
                        }
                        Vector3 new_point;
                        new_point = transform.InverseTransformPoint(MoveTrianglePoint(i, z, valid_index, side));
                        new_verts.Add(new_point);
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

    void MoveTowardRelative(int tri_index, Vector3 point, bool side)
    {
        int[] tris = mesh.triangles;
        if (side)
        {
            for (int i = 0; i < 3; i++)
            {
                if (!IsRelative(tri_index + i))
                {
                    Vector3 local_vert = mesh.vertices[tris[tri_index + i]];
                    Vector3 world_vert = transform.TransformPoint(local_vert);
                    Vector3 relative_point = slice_transform.InverseTransformPoint(world_vert);
                    int count = 0;

                    while (relative_point.x > 0.0f)
                    {
                        world_vert = Vector3.MoveTowards(world_vert, point, 0.005f);
                        count++;
                        relative_point = slice_transform.InverseTransformPoint(world_vert);
                        if (count > 1500)
                        {
                            Debug.Log("BROKE OUT OF THE WHILE LOOP");
                            Debug.Log(point);
                            break;
                        }
                    }
                    right_points.Add(world_vert);
                }
            }
        }
        else
        {
            for (int i = 0; i < 3; i++)
            {
                if (IsRelative(tri_index + i))
                {
                    Vector3 local_vert = mesh.vertices[tris[tri_index + i]];
                    Vector3 world_vert = transform.TransformPoint(local_vert);
                    Vector3 relative_point = slice_transform.InverseTransformPoint(world_vert);
                    int count = 0;

                    while (relative_point.x < 0.0f)
                    {
                        world_vert = Vector3.MoveTowards(world_vert, point, 0.005f);
                        count++;
                        relative_point = slice_transform.InverseTransformPoint(world_vert);
                        if (count > 1500)
                        {
                            Debug.Log("BROKE OUT OF THE WHILE LOOP");
                            Debug.Log(point);
                            break;
                        }
                    }
                    left_points.Add(world_vert);
                }
            }
        }
    }

    Vector3 MoveTrianglePoint(int tri_index, int invalid_index, int valid_index, bool side)
    {
        int[] tris = mesh.triangles;

        Vector3 valid = transform.TransformPoint(mesh.vertices[tris[tri_index + valid_index]]);
        Vector3 invalid = transform.TransformPoint(mesh.vertices[tris[tri_index + invalid_index]]);
        Vector3 relative_point = slice_transform.InverseTransformPoint(invalid);
        int count = 0;
        if (side)
        {
            while (relative_point.x > 0.0f)
            {
                invalid = Vector3.MoveTowards(invalid, valid, 0.01f);
                count++;
                relative_point = slice_transform.InverseTransformPoint(invalid);
                if (count > 1500)
                {
                    Debug.Log("BROKE OUT OF THE WHILE LOOP");
                    break;
                }
            }
        }
        else
        {
            while (relative_point.x < 0.0f)
            {
                invalid = Vector3.MoveTowards(invalid, valid, 0.01f);
                count++;
                relative_point = slice_transform.InverseTransformPoint(invalid);
                if (count > 1500)
                {
                    Debug.Log("BROKE OUT OF THE WHILE LOOP");
                    break;
                }
            }
        }
        return invalid;
    }
}
