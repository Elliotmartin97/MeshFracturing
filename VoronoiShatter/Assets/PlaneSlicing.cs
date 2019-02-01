using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PlaneSlicing : MonoBehaviour
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
    private List<Vector3> new_verts = new List<Vector3>();
    private List<Vector2> new_uvs = new List<Vector2>();
    private List<Vector3> new_normals = new List<Vector3>();
    private Color point_color;
    // Start is called before the first frame update
    void Start()
    {
        cam_pos = GameObject.Find("Main Camera");
        mesh = GetComponent<MeshFilter>().mesh;
        mesh_col = GetComponent<MeshCollider>();
        //Debug.Log(mesh.vertices.Length);
        Debug.Log(mesh.triangles.Length);
        point_color = Color.green;
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
        new_verts.Clear();
        new_uvs.Clear();
        new_normals.Clear();
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

        List<Vector3> mesh_list_0 = new List<Vector3>();
        List<Vector3> mesh_list_1 = new List<Vector3>();

        for (int i = 0; i < mesh.vertexCount; i++)
        {
            Vector3 v = transform.TransformPoint(mesh.vertices[i]);
            if (v.x < start_point.x)
            {
                mesh_list_0.Add(mesh.vertices[i]);
                new_verts.Add(mesh.vertices[i]);
                new_uvs.Add(mesh.uv[i]);
                new_normals.Add(mesh.normals[i]);
            }
        }
        //Debug.Log(new_verts.Count);
        RebuildMesh();
       


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

    void RebuildMesh()
    {
        int[] tris = mesh.triangles;
        Vector3[] verts = mesh.vertices;
        Vector2[] uvs = mesh.uv;
        Vector3[] normals = mesh.normals;

        List<Vector3> rebuilt_verts = new List<Vector3>();
        List<Vector2> rebuilt_uvs = new List<Vector2>();
        List<Vector3> rebuilt_normals = new List<Vector3>();
        List<int> rebuilt_tris = new List<int>();

        Debug.Log(tris.Length);
        //add triangles from existing verts
        for (int i = 0; i < tris.Length; i += 3)
        {
            Vector3 vert0 = verts[tris[i + 0]];
            Vector3 vert1 = verts[tris[i + 1]];
            Vector3 vert2 = verts[tris[i + 2]];
            Vector2 uv0 = uvs[tris[i + 0]];
            Vector2 uv1 = uvs[tris[i + 1]];
            Vector2 uv2 = uvs[tris[i + 2]];
            Vector3 normal0 = normals[tris[i + 0]];
            Vector3 normal1 = normals[tris[i + 1]];
            Vector3 normal2 = normals[tris[i + 2]];
            int[] tri_indexes = { -1, -1, -1 };

            for (int j = 0; j < new_verts.Count; j++)
            {
                if (vert0 == new_verts[j] && uv0 == new_uvs[j] && normal0 == new_normals[j])
                {
                    //first vert is valid
                    tri_indexes[0] = j;
                }
                if(vert1 == new_verts[j] && uv1 == new_uvs[j] && normal1 == new_normals[j])
                {
                    //second vert is valid
                    tri_indexes[1] = j;
                }
                if(vert2 == new_verts[j] && uv2 == new_uvs[j] && normal2 == new_normals[j])
                {
                    //third vert is valid
                    tri_indexes[2] = j;
                }
            }
            if(tri_indexes[0] != -1 && tri_indexes[1] != -1 && tri_indexes[2] != -1)
            {
                //if all 3 verts are valid, add triangle
                for(int z = 0; z < 3; z++)
                {
                    rebuilt_tris.Add(tri_indexes[z]);
                }
            }
        }

        

      //  Debug.Log(rebuilt_tris.Count);
        mesh.Clear();
        mesh.vertices = new_verts.ToArray();
        mesh.uv = new_uvs.ToArray();
        mesh.normals = new_normals.ToArray();
        mesh.triangles = rebuilt_tris.ToArray();
        mesh.RecalculateNormals();
        //Debug.Log(mesh.vertices.Length);
        //Debug.Log(mesh.triangles.Length);
        mesh_col.sharedMesh = mesh;
    }
}
