using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class LowPolyGrassGenerator : MonoBehaviour
{
    [Header("Blade Settings")]
    [Tooltip("Base bend amount applied along the blade's height.")]
    public float bendAmount = 0.2f;

    [Tooltip("Minimum multiplier for the bend amount per blade.")]
    public float bendRandomMin = 0.8f;

    [Tooltip("Maximum multiplier for the bend amount per blade.")]
    public float bendRandomMax = 1.2f;

    [Tooltip("Animation curve to control the bend along the blade's height. (0,0) at base, (1,1) at tip for linear).")]
    public AnimationCurve bendCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Tooltip("Number of segments along the blade (more segments = smoother curve).")]
    [Range(2, 20)]
    public int polygonCount = 4;

    [Tooltip("Height of each grass blade.")]
    public float bladeHeight = 1.0f;

    [Tooltip("Width of the grass blade at the base.")]
    public float bladeWidth = 0.1f;

    [Header("Group Settings")]
    [Tooltip("Number of grass blades in the clump.")]
    public int groupBladesCount = 10;

    [Tooltip("Radius for random blade placement within the clump.")]
    public float groupRadius = 0.2f;

    [Tooltip("Maximum random rotation (in degrees) for each blade.")]
    public float randomRotation = 20f;

    void Start()
    {
        GenerateGrass();
    }

    /// <summary>
    /// Generates a clump of two‐sided grass blades with two LOD meshes.
    /// The high‐LOD version uses multiple segments to define the curved, tapered blade,
    /// while the low‐LOD version (child object) uses only one subdivision (2 triangles per blade)
    /// and exactly matches the curves and positions of the high‐LOD version.
    /// </summary>
    public void GenerateGrass()
    {
        // High LOD mesh data (detailed version)
        Mesh highPolyMesh = new Mesh();
        highPolyMesh.name = "LowPolyGrass_High";
        List<Vector3> highVertices = new List<Vector3>();
        List<int> highTriangles = new List<int>();
        List<Vector2> highUVs = new List<Vector2>();

        // Low LOD mesh data (simplified version)
        Mesh lowPolyMesh = new Mesh();
        lowPolyMesh.name = "LowPolyGrass_Low";
        List<Vector3> lowVertices = new List<Vector3>();
        List<int> lowTriangles = new List<int>();
        List<Vector2> lowUVs = new List<Vector2>();

        // Loop through each blade in the clump.
        for (int bladeIndex = 0; bladeIndex < groupBladesCount; bladeIndex++)
        {
            // Randomize base position within a circle.
            Vector2 circle = Random.insideUnitCircle * groupRadius;
            Vector3 bladePosition = new Vector3(circle.x, 0, circle.y);

            // Randomize overall Y-axis rotation.
            float rotationAngle = Random.Range(-randomRotation, randomRotation);
            Quaternion rotation = Quaternion.Euler(0, rotationAngle, 0);

            // Randomize the effective bend amount.
            float effectiveBend = bendAmount * Random.Range(bendRandomMin, bendRandomMax);

            // Get one random, consistent bend direction for this blade (in the XZ plane).
            Vector2 bendDir = Random.insideUnitCircle;
            if (bendDir == Vector2.zero)
                bendDir = Vector2.right;
            bendDir.Normalize();

            // --- High LOD: Detailed Blade Generation ---
            int highStartIndex = highVertices.Count;
            List<Vector3> bladeHighVertices = new List<Vector3>();
            List<Vector2> bladeHighUVs = new List<Vector2>();

            // Create rows for the blade (except the tip) with two vertices each.
            for (int i = 0; i < polygonCount; i++)
            {
                float t = i / (float)polygonCount; // normalized height (0 at base, nearly 1 at top)
                float y = t * bladeHeight;
                float curveFactor = bendCurve.Evaluate(t);
                float offsetAmount = curveFactor * y * effectiveBend;
                Vector3 offset = new Vector3(bendDir.x, 0, bendDir.y) * offsetAmount;
                // Taper the blade width with height.
                float widthScale = 1f - t;
                Vector3 left = new Vector3(-bladeWidth * 0.5f * widthScale, y, 0) + offset;
                Vector3 right = new Vector3(bladeWidth * 0.5f * widthScale, y, 0) + offset;
                // Apply overall rotation and position.
                left = rotation * left + bladePosition;
                right = rotation * right + bladePosition;
                bladeHighVertices.Add(left);
                bladeHighVertices.Add(right);
                bladeHighUVs.Add(new Vector2(0, t));
                bladeHighUVs.Add(new Vector2(1, t));
            }
            // Tip vertex (a single point at the top).
            {
                float t = 1f;
                float y = bladeHeight;
                float curveFactor = bendCurve.Evaluate(t);
                float offsetAmount = curveFactor * y * effectiveBend;
                Vector3 offset = new Vector3(bendDir.x, 0, bendDir.y) * offsetAmount;
                Vector3 tip = new Vector3(0, y, 0) + offset;
                tip = rotation * tip + bladePosition;
                bladeHighVertices.Add(tip);
                bladeHighUVs.Add(new Vector2(0.5f, t));
            }
            // Add the high-LOD blade vertices and UVs.
            highVertices.AddRange(bladeHighVertices);
            highUVs.AddRange(bladeHighUVs);
            int highVertexCount = bladeHighVertices.Count; // Should be (2 * polygonCount + 1)

            // Create triangles for the high-LOD front face.
            for (int i = 0; i < polygonCount - 1; i++)
            {
                int rowStart = highStartIndex + i * 2;
                highTriangles.Add(rowStart);         // left vertex current row
                highTriangles.Add(rowStart + 2);       // left vertex next row
                highTriangles.Add(rowStart + 3);       // right vertex next row

                highTriangles.Add(rowStart);           // left vertex current row
                highTriangles.Add(rowStart + 3);       // right vertex next row
                highTriangles.Add(rowStart + 1);       // right vertex current row
            }
            // Top segment for the high-LOD front face.
            {
                int rowStart = highStartIndex + (polygonCount - 1) * 2;
                int tipIndex = highStartIndex + highVertexCount - 1;
                highTriangles.Add(rowStart);
                highTriangles.Add(tipIndex);
                highTriangles.Add(rowStart + 1);
            }
            // Duplicate high-LOD vertices for the back face (reversed winding).
            int highBackStartIndex = highVertices.Count;
            for (int i = highStartIndex; i < highStartIndex + highVertexCount; i++)
            {
                highVertices.Add(highVertices[i]);
                highUVs.Add(highUVs[i]);
            }
            for (int i = 0; i < polygonCount - 1; i++)
            {
                int rowStart = highBackStartIndex + i * 2;
                highTriangles.Add(rowStart);
                highTriangles.Add(rowStart + 3);
                highTriangles.Add(rowStart + 2);

                highTriangles.Add(rowStart);
                highTriangles.Add(rowStart + 1);
                highTriangles.Add(rowStart + 3);
            }
            {
                int rowStart = highBackStartIndex + (polygonCount - 1) * 2;
                int tipIndex = highBackStartIndex + highVertexCount - 1;
                highTriangles.Add(rowStart);
                highTriangles.Add(rowStart + 1);
                highTriangles.Add(tipIndex);
            }

            // --- Low LOD: Simplified Blade Generation (1 subdivision) ---
            int lowStartIndex = lowVertices.Count;
            // Base row at t = 0 (offset is zero because y is 0).
            {
                float t = 0f;
                float y = 0f;
                float widthScale = 1f; // full width at the base
                Vector3 left = new Vector3(-bladeWidth * 0.5f * widthScale, y, 0);
                Vector3 right = new Vector3(bladeWidth * 0.5f * widthScale, y, 0);
                left = rotation * left + bladePosition;
                right = rotation * right + bladePosition;
                lowVertices.Add(left);
                lowVertices.Add(right);
                lowUVs.Add(new Vector2(0, t));
                lowUVs.Add(new Vector2(1, t));
            }
            // Tip row at t = 1.
            {
                float t = 1f;
                float y = bladeHeight;
                float curveFactor = bendCurve.Evaluate(t);
                float offsetAmount = curveFactor * y * effectiveBend;
                Vector3 offset = new Vector3(bendDir.x, 0, bendDir.y) * offsetAmount;
                Vector3 tip = new Vector3(0, y, 0) + offset;
                tip = rotation * tip + bladePosition;
                lowVertices.Add(tip);
                lowUVs.Add(new Vector2(0.5f, t));
            }
            // Front face triangle for low LOD (using the two base vertices and the tip).
            lowTriangles.Add(lowStartIndex);     // left base
            lowTriangles.Add(lowStartIndex + 2);   // tip
            lowTriangles.Add(lowStartIndex + 1);   // right base

            // Duplicate for low LOD back face with reversed winding.
            int lowBackStartIndex = lowVertices.Count;
            lowVertices.Add(lowVertices[lowStartIndex]);
            lowVertices.Add(lowVertices[lowStartIndex + 1]);
            lowVertices.Add(lowVertices[lowStartIndex + 2]);
            lowUVs.Add(lowUVs[lowStartIndex]);
            lowUVs.Add(lowUVs[lowStartIndex + 1]);
            lowUVs.Add(lowUVs[lowStartIndex + 2]);
            lowTriangles.Add(lowBackStartIndex);       // left base
            lowTriangles.Add(lowBackStartIndex + 1);     // right base
            lowTriangles.Add(lowBackStartIndex + 2);     // tip
        }

        // Build the high-LOD mesh.
        highPolyMesh.SetVertices(highVertices);
        highPolyMesh.SetTriangles(highTriangles, 0);
        highPolyMesh.SetUVs(0, highUVs);
        highPolyMesh.RecalculateNormals();
        highPolyMesh.RecalculateBounds();

        // Assign the high-LOD mesh to the current GameObject.
        MeshFilter mf = GetComponent<MeshFilter>();
        mf.mesh = highPolyMesh;

        // Build the low-LOD mesh.
        lowPolyMesh.SetVertices(lowVertices);
        lowPolyMesh.SetTriangles(lowTriangles, 0);
        lowPolyMesh.SetUVs(0, lowUVs);
        lowPolyMesh.RecalculateNormals();
        lowPolyMesh.RecalculateBounds();

        // Create a child GameObject for the low-LOD mesh.
        GameObject lowLodObj = new GameObject("LowPolyGrass_LOD");
        lowLodObj.transform.parent = this.transform;
        lowLodObj.transform.localPosition = Vector3.zero;
        lowLodObj.transform.localRotation = Quaternion.identity;
        lowLodObj.transform.localScale = Vector3.one;
        MeshFilter lowMF = lowLodObj.AddComponent<MeshFilter>();
        lowMF.mesh = lowPolyMesh;
        MeshRenderer lowMR = lowLodObj.AddComponent<MeshRenderer>();
        // Optionally assign the same material as the parent.
        MeshRenderer parentMR = GetComponent<MeshRenderer>();
        if (parentMR != null)
        {
            lowMR.material = parentMR.material;
        }
    }
}


