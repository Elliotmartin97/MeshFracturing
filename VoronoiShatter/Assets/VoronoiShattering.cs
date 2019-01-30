using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoronoiShattering : MonoBehaviour
{
    public int seed = 0;
    public int point_amount = 20;
    public List<Vector3> points;
    private Mesh mesh;

    void Start()
    {
        Random.InitState(seed);

        mesh = GetComponent<MeshFilter>().sharedMesh;
        Bounds bounds = mesh.bounds;

        Vector3 max = bounds.max;
        Vector3 min = bounds.min;

        for (int i = 0; i < point_amount; i++)
        {
            float randomX = Random.Range(min.x, max.x);
            float randomY = Random.Range(min.y, max.y);
            float randomZ = Random.Range(min.z, max.z);

            points.Add(new Vector3(randomX, randomY, randomZ));
        }


       
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.R))
        {
            SeedRefresh();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        for (int i = 0; i < points.Count; i++)
        {
            float radius = 0.05f;
            if (points.Count > 0)
            {
                Gizmos.DrawSphere(points[i], radius);
            }
        }
    }

    private void SeedRefresh()
    {
        points.Clear();
        Random.InitState(seed);

        mesh = GetComponent<MeshFilter>().sharedMesh;
        Bounds bounds = mesh.bounds;

        Vector3 max = bounds.max;
        Vector3 min = bounds.min;

        for (int i = 0; i < point_amount; i++)
        {
            float randomX = Random.Range(min.x, max.x);
            float randomY = Random.Range(min.y, max.y);
            float randomZ = Random.Range(min.z, max.z);

            points.Add(new Vector3(randomX, randomY, randomZ));
        }
    }
}
