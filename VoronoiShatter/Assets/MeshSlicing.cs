using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


public class MeshSlicing : MonoBehaviour
{
    public int iterations;
    public float slice_angle = 0.0f;
    public bool add_rigidbody = false;
    private Mesh mesh;
    private GameObject cam_pos;
    private Vector3 min_dist;
    public Vector3 center;
    public bool trim = false;
    private MeshCollider mesh_col;
    private GameObject copy;
    private Transform slice_transform;
    private bool original = true;
    private Camera cam;

    // Start is called before the first frame update
    void Start()
    {
        Init();
    }

    void Init()
    {
        cam_pos = GameObject.Find("Main Camera");
        cam = cam_pos.GetComponent<Camera>();
        mesh = GetComponent<MeshFilter>().mesh;
        mesh_col = GetComponent<MeshCollider>();
    }

    // Update is called once per frame
    void Update()
    {
        center = transform.TransformPoint(mesh.bounds.center);
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 1000))
        {
            if(hit.collider.gameObject == this.gameObject)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    GameObject slice_viewer = new GameObject();
                    Transform slice_tran = slice_viewer.transform;
                    slice_tran.position = hit.point;
                    slice_tran.rotation = Quaternion.LookRotation(ray.direction);
                    slice_tran.Rotate(slice_tran.rotation.x, slice_tran.rotation.y, slice_angle);

                    if(!trim)
                    {
                        if (add_rigidbody)
                        {
                            gameObject.AddComponent<Rigidbody>();
                        }
                    }
                    Slice(slice_tran);

                    Destroy(slice_viewer);
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


    void Slice(Transform slice_T)
    {
        Init();
        slice_transform = slice_T;

        List<Vector3> left_verts = new List<Vector3>();
        List<Vector2> left_uvs = new List<Vector2>();
        List<Vector3> left_normals = new List<Vector3>();

        List<Vector3> right_verts = new List<Vector3>();
        List<Vector2> right_uvs = new List<Vector2>();
        List<Vector3> right_normals = new List<Vector3>();


        for (int i = 0; i < mesh.vertexCount; i++)
        {
            Vector3 v = transform.TransformPoint(mesh.vertices[i]);
            Vector3 relative_point = slice_transform.InverseTransformPoint(v);
            //compare relative vertex x to the slice x
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

        iterations--;
        copy = gameObject;
        GameObject right_obj = Instantiate(copy);
        GameObject left_obj = Instantiate(copy);

        Mesh old_mesh = new Mesh();
        old_mesh.vertices = mesh.vertices;
        old_mesh.uv = mesh.uv;
        old_mesh.normals = mesh.normals;
        old_mesh.triangles = mesh.triangles;


        left_obj.name = gameObject.name + "_left";
        Mesh left_mesh = left_obj.GetComponent<MeshFilter>().mesh;
        RebuildMesh(left_mesh, old_mesh, left_verts, left_uvs, left_normals, true, false);
        left_obj.GetComponent<MeshFilter>().mesh = left_mesh;
        left_obj.GetComponent<MeshCollider>().sharedMesh = left_mesh;


        right_obj.name = gameObject.name + "_right";
        Mesh right_mesh = right_obj.GetComponent<MeshFilter>().mesh;
        RebuildMesh(right_mesh, old_mesh, right_verts, right_uvs, right_normals, false, false);
        right_obj.GetComponent<MeshFilter>().mesh = right_mesh;
        right_obj.GetComponent<MeshCollider>().sharedMesh = right_mesh;
        




        if (iterations > 0)
        {
            GameObject temp_left = new GameObject();
            GameObject temp_right = new GameObject();

            if(trim && original)
            {
                if(TrimFromCenter(slice_transform))
                {
                    if (!left_obj.GetComponent<Rigidbody>())
                    {
                        left_obj.AddComponent<Rigidbody>();
                    }
                    Transform left_slice = temp_left.transform;
                    left_slice.position = transform.TransformPoint(left_mesh.bounds.center);
                    left_slice.rotation = Quaternion.LookRotation(-slice_transform.right);
                    left_slice.Rotate(left_slice.rotation.x, left_slice.rotation.y, slice_angle);
                    left_obj.GetComponent<MeshSlicing>().original = false;
                    left_obj.GetComponent<MeshSlicing>().Slice(left_slice);
                    right_obj.GetComponent<MeshSlicing>().iterations++;
                }
                else
                {
                    if (add_rigidbody)
                    {
                        if(!right_obj.GetComponent<Rigidbody>())
                        {
                            right_obj.AddComponent<Rigidbody>();
                        }
                    }
                    Transform right_slice = temp_right.transform;
                    right_slice.position = transform.TransformPoint(right_mesh.bounds.center);
                    right_slice.rotation = Quaternion.LookRotation(slice_transform.right);
                    right_slice.Rotate(right_slice.rotation.x, right_slice.rotation.y, slice_angle);
                    right_obj.GetComponent<MeshSlicing>().original = false;
                    right_obj.GetComponent<MeshSlicing>().Slice(right_slice);
                    left_obj.GetComponent<MeshSlicing>().iterations++;
                }
            }
            else
            {
                Transform left_slice = temp_left.transform;
                left_slice.position = transform.TransformPoint(left_mesh.bounds.center);
                left_slice.rotation = Quaternion.LookRotation(-slice_transform.right);
                left_slice.Rotate(left_slice.rotation.x, left_slice.rotation.y, slice_angle);
                left_obj.GetComponent<MeshSlicing>().Slice(left_slice);

                Transform right_slice = temp_right.transform;
                right_slice.position = transform.TransformPoint(right_mesh.bounds.center);
                right_slice.rotation = Quaternion.LookRotation(slice_transform.right);
                right_slice.Rotate(right_slice.rotation.x, right_slice.rotation.y, slice_angle);
                right_obj.GetComponent<MeshSlicing>().Slice(right_slice);
            }

            Destroy(temp_left);
            Destroy(temp_right);
            
        }
        Destroy(gameObject);
    }

    bool TrimFromCenter(Transform slice_T)
    {
        Vector3 world_vert = transform.TransformPoint(mesh.bounds.center);
        Vector3 relative = slice_T.InverseTransformPoint(world_vert);
        if(relative.x > 0)
        {
            return true;
        }
        else
        {
            return false;
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


    void RebuildMesh(Mesh current_mesh, Mesh old_mesh, List<Vector3> new_verts, List<Vector2> new_uvs, List<Vector3> new_normals, bool side, bool trim)
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
        List<Vector3> slice_vertices = new List<Vector3>();
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
                        if (tri_indexes[z] == -1)
                        {
                            //first vert is valid
                            tri_indexes[z] = j;
                            invalid_count--;
                        }
                    }
                }
            }
          //  Debug.Log(invalid_count);
            if (tri_indexes[0] != -1 && tri_indexes[1] != -1 && tri_indexes[2] != -1)
            {
                //if all 3 verts are valid, add triangle
                for (int z = 0; z < 3; z++)
                {
                    rebuilt_tris.Add(tri_indexes[z]);
                }
            }
            else if (invalid_count == 2)
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
                            if (side)
                            {
                                if (IsRelative(i + k))
                                {
                                    Vector3 current_valid = mesh_verts[k];

                                    if (Vector3.Distance(invalid_vert, current_valid) < Vector3.Distance(invalid_vert, nearest_valid))
                                    {
                                        nearest_valid = current_valid;
                                        valid_index = k;
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
                                    }
                                }
                            }
                        }
                        Vector3 new_point = transform.InverseTransformPoint(MoveTrianglePoint(i, z, valid_index, side));
                        new_verts.Add(new_point);
                        new_uvs.Add(mesh_uvs[z]);
                        new_normals.Add(mesh_normals[z]);
                        tri_indexes[z] = new_verts.Count - 1;
                        rebuilt_tris.Add(tri_indexes[z]);
                        slice_vertices.Add(new_point);
                    }
                    else
                    {
                        rebuilt_tris.Add(tri_indexes[z]);
                    }
                }
            }
            else if (invalid_count == 1)
            {
                Vector3 invalid_vert;
                Vector3 valid_vert_1 = Vector3.zero;
                Vector3 valid_vert_2 = Vector3.zero;
                int invalid_idx = -1, valid_idx_1 = -1, valid_idx_2 = -1;
                for (int z = 0; z < 3; z++)
                {
                    if (tri_indexes[z] == -1)
                    {
                        invalid_vert = mesh_verts[z];
                        invalid_idx = z;
                    }
                    else if (valid_vert_1 == Vector3.zero)
                    {
                        valid_vert_1 = mesh_verts[z];
                        valid_idx_1 = z;
                    }
                    else
                    {
                        valid_vert_2 = mesh_verts[z];
                        valid_idx_2 = z;
                    }
                }
                if (invalid_idx == -1 || valid_idx_1 == -1 || valid_idx_2 == -1)
                {
                    Debug.Log("oh shit " + invalid_idx + " " + valid_idx_1 + " " + valid_idx_2);
                    Debug.Log(tri_indexes[0] + " " + tri_indexes[1] + " " + tri_indexes[2]);
                }

                //build first triangle by moving old vert to new point
                Vector3 new_point = transform.InverseTransformPoint(MoveTrianglePoint(i, invalid_idx, valid_idx_1, side));
                new_verts.Add(new_point);
                new_uvs.Add(mesh_uvs[invalid_idx]);
                new_normals.Add(mesh_normals[invalid_idx]);
                tri_indexes[invalid_idx] = new_verts.Count - 1;
                rebuilt_tris.Add(tri_indexes[0]);
                rebuilt_tris.Add(tri_indexes[1]);
                rebuilt_tris.Add(tri_indexes[2]);
                slice_vertices.Add(new_point);

                //build second triangle by using point from the first and secondary new point
                Vector3 secondary_point = transform.InverseTransformPoint(MoveTrianglePoint(i, invalid_idx, valid_idx_2, side));
                new_verts.Add(secondary_point);
                new_uvs.Add(mesh_uvs[invalid_idx]);
                new_normals.Add(mesh_normals[invalid_idx]);
                tri_indexes[invalid_idx] = new_verts.Count - 1;
                tri_indexes[valid_idx_1] = new_verts.Count - 2;
                rebuilt_tris.Add(tri_indexes[0]);
                rebuilt_tris.Add(tri_indexes[1]);
                rebuilt_tris.Add(tri_indexes[2]);
                slice_vertices.Add(secondary_point);

            }
        }

        Vector3 center = Vector3.zero;
        for (int t = 0; t < slice_vertices.Count; t++)
        {
            center += slice_vertices[t];
        }
        center = center / slice_vertices.Count;

        //find duplicate points on slice
        for (int v = 0; v < slice_vertices.Count; v++)
        {
            for(int v2 = 0; v2 < slice_vertices.Count; v2++)
            {
                if(v != v2 && Vector3.Distance(slice_vertices[v],slice_vertices[v2]) < 0.1f)
                {
                   // found duplicate
                    if ((slice_vertices.Count - 1) > v)
                    {
                        slice_vertices.RemoveAt(v);
                    }
                }
            }
        }


        if(side)
        {
            slice_vertices.Sort(SortClockWise);
        }
        else
        {
            slice_vertices.Sort(SortAntiClockWise);
        }

        //normal of the slice direction depending on side
        Vector3 normal;
        if(side)
        {
            normal = (-slice_transform.right).normalized;
        }
        else
        {
            normal = (slice_transform.right).normalized;
        }
        List<int> slice_indexes = new List<int>();

        //make new verts based on slice point, add indexes for triangle order
        for (int p = 0; p < slice_vertices.Count; p++)
        {
            //create new uvs for the new slice points
            Vector2 new_uv = transform.worldToLocalMatrix.MultiplyPoint(slice_vertices[p]);
            new_uv.x += 0.5f;
            new_uv.y += 0.5f;

            new_verts.Add(slice_vertices[p]);
            new_uvs.Add(new_uv);
            new_normals.Add(normal);
            slice_indexes.Add(new_verts.Count - 1);
        }
        Vector2 center_uv = transform.worldToLocalMatrix.MultiplyPoint(center);
        center_uv.x += 0.5f;
        center_uv.y += 0.5f;

        new_verts.Add(center);
        new_uvs.Add(center_uv);
        new_normals.Add(normal);

        //create triangles in clockwise order and join the last with the first
        for (int i = 0; i < slice_vertices.Count; i++)
        {
            if(i < slice_vertices.Count - 1)
            {
                rebuilt_tris.Add(slice_indexes[i]);
                rebuilt_tris.Add(slice_indexes[i+1]);
                rebuilt_tris.Add(new_verts.Count - 1);
            }
            else
            {
                rebuilt_tris.Add(slice_indexes[i]);
                rebuilt_tris.Add(slice_indexes[0]);
                rebuilt_tris.Add(new_verts.Count - 1);
            }
        }


        //view slice points in scene view

        current_mesh.Clear();
        current_mesh.vertices = new_verts.ToArray();
        current_mesh.uv = new_uvs.ToArray();
        current_mesh.normals = new_normals.ToArray();
        current_mesh.triangles = rebuilt_tris.ToArray();
        current_mesh.RecalculateNormals();
        current_mesh.RecalculateBounds();
        current_mesh.RecalculateTangents();
    }

    public int SortClockWise(Vector3 v1, Vector3 v2)
    {

        Vector3 world_vert1 = transform.TransformPoint(v1);
        Vector3 world_vert2 = transform.TransformPoint(v2);
        Vector3 rv1 = slice_transform.InverseTransformPoint(world_vert1);
        Vector3 rv2 = slice_transform.InverseTransformPoint(world_vert2);

        return Mathf.Atan2(rv1.y, -rv1.z).CompareTo(Mathf.Atan2(rv2.y, -rv2.z));
    }
    public int SortAntiClockWise(Vector3 v1, Vector3 v2)
    {
        Vector3 world_vert1 = transform.TransformPoint(v1);
        Vector3 world_vert2 = transform.TransformPoint(v2);
        Vector3 rv1 = slice_transform.InverseTransformPoint(world_vert1);
        Vector3 rv2 = slice_transform.InverseTransformPoint(world_vert2);
        return Mathf.Atan2(rv1.y, rv1.z).CompareTo(Mathf.Atan2(rv2.y, rv2.z));
    }


    Vector3 MoveTrianglePoint(int tri_index, int invalid_index, int valid_index, bool side)
    {
        int[] tris = mesh.triangles;
        Vector3 local_valid = mesh.vertices[tris[tri_index + valid_index]];
        Vector3 local_invalid = mesh.vertices[tris[tri_index + invalid_index]];
        Vector3 valid = transform.TransformPoint(mesh.vertices[tris[tri_index + valid_index]]);
        Vector3 invalid = transform.TransformPoint(mesh.vertices[tris[tri_index + invalid_index]]);
        Vector3 relative_point = slice_transform.InverseTransformPoint(invalid);



        //TRIGANOMETRY Method

        //relative_point.x = 0;
        //relative_point.z = 0;
        //float a = Vector3.Distance(local_invalid, relative_point);
        //float b = Vector3.Distance(relative_point, local_valid);
        //float c = Vector3.Distance(local_invalid, local_valid);

        //float arccosA = Mathf.Acos(((b * b) + (c * c) - (a * a)) / ((b * c) * 2));
        //Vector3 direction = local_invalid - relative_point;
        //arccosA = Vector3.Angle(local_valid, direction);
        //float arccosB = 180 - 90 - arccosA;
        //float arccosC = 90;


        //float hypL = a * Mathf.Sin(arccosB) / Mathf.Sin(arccosA);
        //float opL = (a * Mathf.Sin(arccosA)) / Mathf.Sin(arccosB);
        //Debug.Log(opL);
        //Vector3 new_point = slice_transform.TransformPoint(relative_point);

        //new_point.y = transform.TransformPoint(local_invalid).y - opL;
        //new_point.z = invalid.z;

        //LINE INTERSECTION

        //Vector3 LineA1 = local_invalid;
        //Vector3 LineA2 = local_valid;
        //Vector3 LineB1 = relative_point;
        //Vector3 LineB2 = relative_point;
        //LineB2.y = local_valid.y;

        //Vector3 new_point = (transform.TransformPoint(GetLineIntersectionPoint(LineA1, LineA2, LineB1, LineB2)));
        //return new_point;

        int count = 0;
      
        if (side)
        {
            while (relative_point.x > 0.0f)
            {
                invalid = Vector3.MoveTowards(invalid, valid, 0.01f);
                count++;
                relative_point = slice_transform.InverseTransformPoint(invalid);
                if (count > 3000)
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
                if (count > 3000)
                {
                    Debug.Log("BROKE OUT OF THE WHILE LOOP");
                    break;
                }
            }
        }
        return invalid;
    }

    public Vector3 GetLineIntersectionPoint(Vector3 A1, Vector3 A2, Vector3 B1, Vector3 B2)
    {

        float tmp = (B2.x - B1.x) * (A2.y - A1.y) - (B2.y - B1.y) * (A2.x - A1.x);

        if (tmp == 0)
        {
            // No solution!
            return Vector3.zero;
        }

        float mu = ((A1.x - B1.x) * (A2.y - A1.y) - (A1.y - B1.y) * (A2.x - A1.x)) / tmp;


        return new Vector3(
            B1.x + (B2.x - B1.x) * mu,
            B1.y + (B2.y - B1.y) * mu,
            B1.z + (B2.z - B1.z) * mu
        );
    }
}
