using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MeshSlicing : MonoBehaviour
{
    public float radius = 0.025f;
    private Mesh mesh;
    private GameObject cam_pos;
    public Vector3 start_point;
    public List<Vector3> points = new List<Vector3>();
    private Vector3 last_point;
    private Vector3 min_dist;
    private Vector3 new_point;
    private Vector3 vwp;
    public Vector3 center;
    private MeshCollider mesh_col;
    private Color point_color;
    private GameObject copy;
    // Start is called before the first frame update
    void Start()
    {
        cam_pos = GameObject.Find("Main Camera");
        mesh = GetComponent<MeshFilter>().mesh;
        mesh_col = GetComponent<MeshCollider>();
       // Debug.Log(mesh.vertices.Length);
        point_color = Color.green;
        copy = gameObject;
    }

    // Update is called once per frame
    void Update()
    {
        center = transform.TransformPoint(mesh.bounds.center);
        Camera cam = cam_pos.GetComponent<Camera>();
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 1000))
        {
            if(hit.collider.gameObject == this.gameObject)
            {
                if(Input.GetMouseButtonDown(0))
                {
                    Slice(hit.point);
                }
            }
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
            for(int i = 0; i< points.Count; i++)
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

        min_dist = Vector3.positiveInfinity;
        for (int i = 0; i < mesh.vertexCount; i++)
        {
            vwp = transform.TransformPoint(mesh.vertices[i]);
            if(Vector3.Distance(vwp, start_point) < Vector3.Distance(min_dist, start_point) && vwp.y > start.y)
            {
                min_dist = vwp;
            }
        }
        new_point = min_dist;
        last_point = new_point;
        new_point.x = start_point.x;
        points.Add(new_point);
        int count = 0;
        while(CheckUp())
        {
            new_point = min_dist;
            last_point = new_point;
            new_point.x = start_point.x;
            points.Add(new_point);
            if(count >= 100)
            {
                break;
            }
        }
        while (CheckForward())
        {
            new_point = min_dist;
            last_point = new_point;
            new_point.x = start_point.x;
            points.Add(new_point);
            if (count >= 100)
            {
                break;
            }
        }
        while (CheckDown())
        {
            new_point = min_dist;
            last_point = new_point;
            new_point.x = start_point.x;
            points.Add(new_point);
            if (count >= 100)
            {
                break;
            }
        }
        while (CheckBackward())
        {
            new_point = min_dist;
            last_point = new_point;
            new_point.x = start_point.x;
            points.Add(new_point);
            if (count >= 100)
            {
                break;
            }
        }

        List<Vector3> left_verts = new List<Vector3>();
        List<Vector2> left_uvs = new List<Vector2>();
        List<Vector3> left_normals = new List<Vector3>();

        List<Vector3> right_verts = new List<Vector3>();
        List<Vector2> right_uvs = new List<Vector2>();
        List<Vector3> right_normals = new List<Vector3>();

        for (int i = 0; i < mesh.vertexCount; i++)
        {
            Vector3 v = transform.TransformPoint(mesh.vertices[i]);
            if (v.x < start_point.x)
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


        RebuildMesh(mesh, old_mesh, left_verts, left_uvs, left_normals);
        mesh_col.sharedMesh = mesh;

        new_obj.name = gameObject.name + "_right";
        Mesh right_mesh = new_obj.GetComponent<MeshFilter>().mesh;
        RebuildMesh(right_mesh, old_mesh, right_verts, right_uvs, right_normals);
        new_obj.GetComponent<MeshFilter>().mesh = right_mesh;
        new_obj.GetComponent<MeshCollider>().sharedMesh = right_mesh;
        gameObject.name = gameObject.name + "_left";


    }

    bool CheckUp()
    {
        bool found = false;
        min_dist = Vector3.positiveInfinity;
        for (int i = 0; i < mesh.vertexCount; i++)
        {
            vwp = transform.TransformPoint(mesh.vertices[i]);
            if (Vector3.Distance(vwp, last_point) < Vector3.Distance(min_dist, last_point) && vwp.y > last_point.y)
            {
                min_dist = vwp;
                found = true;
            }
        }
        if(found)
        {
            return true;
        }
        return false;
    }

    bool CheckForward()
    {
        bool found = false;
        min_dist = Vector3.positiveInfinity;
        for (int i = 0; i < mesh.vertexCount; i++)
        {
            vwp = transform.TransformPoint(mesh.vertices[i]);
            if (Vector3.Distance(vwp, last_point) < Vector3.Distance(min_dist, last_point) && vwp.z > last_point.z)
            {
                min_dist = vwp;
                found = true;
            }
        }
        if (found)
        {
            return true;
        }
        return false;
    }

    bool CheckDown()
    {
        bool found = false;
        min_dist = Vector3.positiveInfinity;
        for (int i = 0; i < mesh.vertexCount; i++)
        {
            vwp = transform.TransformPoint(mesh.vertices[i]);
            if (Vector3.Distance(vwp, last_point) < Vector3.Distance(min_dist, last_point) && vwp.y < last_point.y)
            {
                min_dist = vwp;
                found = true;
            }
        }
        if (found)
        {
            return true;
        }
        return false;
    }

    bool CheckBackward()
    {
        bool found = false;
        min_dist = Vector3.positiveInfinity;
        for (int i = 0; i < mesh.vertexCount; i++)
        {
            vwp = transform.TransformPoint(mesh.vertices[i]);
            if (Vector3.Distance(vwp, last_point) < Vector3.Distance(min_dist, last_point) && vwp.z < last_point.z)
            {
                min_dist = vwp;
                found = true;
            }
        }
        if (found)
        {
            return true;
        }
        return false;
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

            for (int j = 0; j < new_verts.Count; j++)
            {
                for (int z = 0; z < 3; z++)
                {
                    if (mesh_verts[z] == new_verts[j] && mesh_uvs[z] == new_uvs[j] && mesh_normals[z] == new_normals[j])
                    {
                        //first vert is valid
                        tri_indexes[z] = j;
                        Debug.Log(j);
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
    }
}
