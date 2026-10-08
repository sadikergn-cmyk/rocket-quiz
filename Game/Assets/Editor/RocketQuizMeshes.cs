using System.Collections.Generic;
using UnityEngine;

// Procedural meshes for the RocketQuiz scene (planets, rocket parts, plumes).
public static class RocketQuizMeshes
{
    // Point on the unit sphere for equirect texture coordinates (u, v).
    public static Vector3 SpherePoint(float u, float v)
    {
        float theta = u * Mathf.PI * 2f;
        float phi = (v - 0.5f) * Mathf.PI;
        float c = Mathf.Cos(phi);
        return new Vector3(-c * Mathf.Cos(theta), Mathf.Sin(phi), -c * Mathf.Sin(theta));
    }

    public static Mesh Sphere(string name, int columns, int rows)
    {
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();

        for (int i = 0; i <= rows; i++)
        {
            float v = (float)i / rows;
            for (int j = 0; j <= columns; j++)
            {
                float u = (float)j / columns;
                Vector3 p = SpherePoint(u, v);
                vertices.Add(p);
                normals.Add(p);
                uvs.Add(new Vector2(u, v));
            }
        }

        int stride = columns + 1;
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                int a = i * stride + j;
                AddQuad(vertices, normals, triangles, a, a + stride, a + stride + 1, a + 1);
            }
        }

        return Finish(name, vertices, normals, uvs, triangles);
    }

    // Unit plane in XZ (-1..1) facing up.
    public static Mesh Plane(string name)
    {
        var vertices = new List<Vector3>
        {
            new Vector3(-1f, 0f, -1f), new Vector3(-1f, 0f, 1f), new Vector3(1f, 0f, 1f), new Vector3(1f, 0f, -1f),
        };
        var normals = new List<Vector3> { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
        var uvs = new List<Vector2> { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
        var triangles = new List<int>();
        AddQuad(vertices, normals, triangles, 0, 1, 2, 3);
        return Finish(name, vertices, normals, uvs, triangles);
    }

    // Revolves a (radius, height) profile around Y. List the profile so the outside stays on the
    // right-hand side while walking it (bottom cap -> side going up -> top cap).
    public static Mesh Lathe(string name, Vector2[] profile, bool smooth, int segments = 40, float startDegrees = 0f, float sweepDegrees = 360f)
    {
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();

        int count = profile.Length;
        var segmentNormals = new Vector2[count - 1];
        for (int k = 0; k < count - 1; k++)
        {
            Vector2 d = profile[k + 1] - profile[k];
            segmentNormals[k] = new Vector2(d.y, -d.x).normalized;
        }

        int stride = segments + 1;
        if (smooth)
        {
            for (int k = 0; k < count; k++)
            {
                Vector2 n = Vector2.zero;
                if (k > 0) n += segmentNormals[k - 1];
                if (k < count - 1) n += segmentNormals[k];
                AddRing(vertices, normals, uvs, profile[k], n.normalized, (float)k / (count - 1), segments, startDegrees, sweepDegrees);
            }
            for (int k = 0; k < count - 1; k++)
            {
                for (int j = 0; j < segments; j++)
                {
                    int a = k * stride + j;
                    AddQuad(vertices, normals, triangles, a, a + stride, a + stride + 1, a + 1);
                }
            }
        }
        else
        {
            for (int k = 0; k < count - 1; k++)
            {
                int ringStart = vertices.Count;
                AddRing(vertices, normals, uvs, profile[k], segmentNormals[k], (float)k / (count - 1), segments, startDegrees, sweepDegrees);
                AddRing(vertices, normals, uvs, profile[k + 1], segmentNormals[k], (float)(k + 1) / (count - 1), segments, startDegrees, sweepDegrees);
                for (int j = 0; j < segments; j++)
                {
                    int a = ringStart + j;
                    AddQuad(vertices, normals, triangles, a, a + stride, a + stride + 1, a + 1);
                }
            }
        }

        return Finish(name, vertices, normals, uvs, triangles);
    }

    private static void AddRing(List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, Vector2 point, Vector2 normal, float v, int segments, float startDegrees, float sweepDegrees)
    {
        for (int j = 0; j <= segments; j++)
        {
            float u = (float)j / segments;
            float angle = (startDegrees + sweepDegrees * u) * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            vertices.Add(new Vector3(point.x * cos, point.y, point.x * sin));
            normals.Add(new Vector3(normal.x * cos, normal.y, normal.x * sin));
            uvs.Add(new Vector2(u, v));
        }
    }

    // Adds two triangles, choosing the winding that agrees with the vertex normals.
    private static void AddQuad(List<Vector3> vertices, List<Vector3> normals, List<int> triangles, int a, int b, int c, int d)
    {
        AddTriangle(vertices, normals, triangles, a, b, c);
        AddTriangle(vertices, normals, triangles, a, c, d);
    }

    private static void AddTriangle(List<Vector3> vertices, List<Vector3> normals, List<int> triangles, int a, int b, int c)
    {
        Vector3 face = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
        if (face.sqrMagnitude < 1e-12f)
        {
            return;
        }
        Vector3 wanted = normals[a] + normals[b] + normals[c];
        if (Vector3.Dot(face, wanted) >= 0f)
        {
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
        }
        else
        {
            triangles.Add(a);
            triangles.Add(c);
            triangles.Add(b);
        }
    }

    private static Mesh Finish(string name, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> triangles)
    {
        var mesh = new Mesh { name = name };
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }
}
