using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class TessellateMesh : MonoBehaviour
{
    [Tooltip("Subdivision multiplier. 1 = no change, 2 = 4 triangles per original, 3 = 9 triangles, etc.")]
    public int multiplier = 2;

    void Start()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            Debug.LogError("No MeshFilter found on " + gameObject.name);
            return;
        }

        Mesh originalMesh = meshFilter.mesh;
        if (originalMesh == null)
        {
            Debug.LogError("MeshFilter on " + gameObject.name + " does not contain a mesh.");
            return;
        }

        // Clamp multiplier to be at least 1.
        multiplier = Mathf.Max(1, multiplier);

        Mesh tessellatedMesh = Tessellate(originalMesh, multiplier);
        meshFilter.mesh = tessellatedMesh;
    }

    Mesh Tessellate(Mesh mesh, int m)
    {
        List<Vector3> newVerts = new List<Vector3>();
        List<int> newTriangles = new List<int>();
        List<Vector2> newUVs = new List<Vector2>();

        Vector3[] oldVerts = mesh.vertices;
        int[] oldTriangles = mesh.triangles;
        Vector2[] oldUVs = mesh.uv;
        bool hasUV = (oldUVs != null && oldUVs.Length == oldVerts.Length);

        // Process each original triangle independently.
        for (int t = 0; t < oldTriangles.Length; t += 3)
        {
            int i0 = oldTriangles[t];
            int i1 = oldTriangles[t + 1];
            int i2 = oldTriangles[t + 2];

            Vector3 v0 = oldVerts[i0];
            Vector3 v1 = oldVerts[i1];
            Vector3 v2 = oldVerts[i2];

            Vector2 uv0 = hasUV ? oldUVs[i0] : Vector2.zero;
            Vector2 uv1 = hasUV ? oldUVs[i1] : Vector2.zero;
            Vector2 uv2 = hasUV ? oldUVs[i2] : Vector2.zero;

            // Create a temporary grid to store indices of the new vertices.
            // The grid has (m+1) rows and each row i has (m - i + 1) vertices.
            int[,] grid = new int[m + 1, m + 1]; // only indices where i+j <= m are valid

            // Generate vertices using barycentric coordinates.
            for (int i = 0; i <= m; i++)
            {
                for (int j = 0; j <= m - i; j++)
                {
                    // Compute weights: t along edge v0->v1 and s along edge v0->v2.
                    float tParam = (float)i / m;
                    float sParam = (float)j / m;
                    Vector3 newV = v0 + (v1 - v0) * tParam + (v2 - v0) * sParam;
                    newVerts.Add(newV);

                    if (hasUV)
                    {
                        Vector2 newUV = uv0 + (uv1 - uv0) * tParam + (uv2 - uv0) * sParam;
                        newUVs.Add(newUV);
                    }
                    // Record the index in the grid.
                    grid[i, j] = newVerts.Count - 1;
                }
            }

            // Now create triangles from the grid.
            // Each "cell" in the grid (when i+j < m) produces up to two small triangles.
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < m - i; j++)
                {
                    // First triangle: vertices at (i, j), (i+1, j), (i, j+1)
                    int idx0 = grid[i, j];
                    int idx1 = grid[i + 1, j];
                    int idx2 = grid[i, j + 1];
                    newTriangles.Add(idx0);
                    newTriangles.Add(idx1);
                    newTriangles.Add(idx2);

                    // Second triangle: exists except along the hypotenuse.
                    if (j < m - i - 1)
                    {
                        int idx3 = grid[i + 1, j + 1];
                        newTriangles.Add(idx1);
                        newTriangles.Add(idx3);
                        newTriangles.Add(idx2);
                    }
                }
            }
        }

        Mesh newMesh = new Mesh();
        newMesh.vertices = newVerts.ToArray();
        newMesh.triangles = newTriangles.ToArray();
        if (hasUV)
            newMesh.uv = newUVs.ToArray();
        newMesh.RecalculateNormals();
        newMesh.RecalculateBounds();
        return newMesh;
    }
}
