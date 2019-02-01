using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Point
{
    public float x, y, z;
    public int id;
 

    public Point(float v1, float v2)
    {
        x = v1;
        y = v2;
    }
}

[System.Serializable]
public class Triangle
{
    public Point p0, p1, p2;
}

public class VoronoiShattering : MonoBehaviour
{
    public int random_seed = 0;
    public int seed_count = 20;
    public List<Vector3> seed_points;
    public List<Point> points;
    public List<Triangle> triangles;
    private Mesh mesh;


    void Start()
    {
        Refresh();
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.R))
        {
            Refresh();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        for (int i = 0; i < seed_points.Count; i++)
        {
            float radius = 0.05f;
            if (seed_points.Count > 0)
            {
                Gizmos.DrawSphere(seed_points[i], radius);
            }
        }
        if(triangles.Count > 0)
        {
            Vector2 v0, v1, v2;
            v0.x = triangles[0].p0.x;
            v0.y = triangles[0].p0.y;
            v1.x = triangles[0].p1.x;
            v1.y = triangles[0].p1.y;
            v2.x = triangles[0].p2.x;
            v2.y = triangles[0].p2.y;
            Gizmos.DrawLine(v0, v1);
            Gizmos.DrawLine(v1, v2);
            Gizmos.DrawLine(v2, v0);
        }
    }

    private void Refresh()
    {
        seed_points.Clear();
        points.Clear();
        triangles.Clear();
        Random.InitState(random_seed);

        mesh = GetComponent<MeshFilter>().sharedMesh;
        Bounds bounds = mesh.bounds;

        Vector3 max = bounds.max;
        Vector3 min = bounds.min;

        for (int i = 0; i < seed_count; i++)
        {
            float randomX = Random.Range(min.x, max.x);
            float randomY = Random.Range(min.y, max.y);
            float randomZ = Random.Range(min.z, max.z);

            seed_points.Add(new Vector3(transform.position.x + randomX,  transform.position.y + randomY, transform.position.z + randomZ));
        }

        CreateDelaunayTriangulation();
    }

    public bool CreateDelaunayTriangulation()
    {
        if (seed_points.Count < 3)
        {
            return false;
        }

        for (int i = 0; i < seed_points.Count; i++)
        {
            Point point = new Point(0, 0);
            point.x = seed_points[i].x;
            point.y = seed_points[i].y;
            point.z = seed_points[i].z;
            point.id = i;
            points.Add(point);
        }

        float xmin = points[0].x;
        float ymin = points[0].y;
        float xmax = xmin;
        float ymax = ymin;

        for(int i =0; i < points.Count; i++)
        {
            Point p = points[i];
            if(p.x < xmax)
            {
                xmin = p.z;
            }
            else if (p.x > xmax)
            {
                xmax = p.x;
            }

            if(p.y < ymin)
            {
                ymin = p.y;
            }
            else if(p.y > ymax)
            {
                ymax = p.y;
            }
        }

        float xrange = xmax - xmin;
        float yrange = ymax - ymin;

        float dmax = (xrange > yrange) ? xrange : yrange;
        float xmid = (xmax + xmin) / 2;
        float ymid = (ymax + ymin) / 2;

        Point p0 = new Point((xmid - 2 * dmax), (ymid - dmax));
        Point p1 = new Point(xmid, (ymid + 2 * dmax));
        Point p2 = new Point((xmid + 2 * dmax), (ymid - dmax));

        p0.id = points.Count + 1;
        p1.id = points.Count + 2;
        p2.id = points.Count + 3;

        points.Add(p0);
        points.Add(p1);
        points.Add(p2);
        triangles.Clear();

        Triangle core_triangle = new Triangle();
        core_triangle.p0 = p0;
        core_triangle.p1 = p1;
        core_triangle.p2 = p2;

        triangles.Add(core_triangle);




        return true;
    }
}
