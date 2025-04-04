using UnityEngine;
using System.Collections.Generic;

public class TerrainToMesh : MonoBehaviour
{
    public float TerrainLodDistance = 65;
    public int TriangleCount;
    public int TriangleCountLow;
    [Header("Terrain Settings")]
    public Terrain terrain;

    [Header("Mesh Subdivision Settings (High LOD)")]
    // These dimensions cover the entire terrain span.
    public int polyWidth = 10;
    public int polyLength = 10;
    public Texture2D tiledtexture;
    public Color Tint = Color.white;
    public bool UseAlphaForSpecular;
    public float TextureTile = 20f;
    public int numMeshes = 32;
    public bool generateMeshOnStart = true;

    [Header("Mesh LOD2 Settings")]
    // Factor relative to high LOD resolution (e.g. 0.5 gives half the triangles).
    [Range(0.1f, 1f)]
    public float lod2Factor = 0.5f;
    // How much to push border vertices downward and outward.
    public float lowLODSkirtVertical = 1.0f;
    public float lowLODSkirtHorizontal = 0.2f;

    List<GameObject> obs;
    List<GameObject> obs2;

    private void Start()
    {
        
    }

    private void Awake()
    {
        if (enabled)
        {
            obs = new List<GameObject>();
            obs2 = new List<GameObject>();
            if (terrain == null)
            {
                terrain = FindObjectOfType<Terrain>();
                if (terrain == null)
                {
                    Debug.LogError("No Terrain found in the scene!");
                    return;
                }
            }

            if (generateMeshOnStart)
                GenerateMesh();

            terrain.enabled = false;
        }
    }

    public void GenerateMesh()
    {
        TerrainData tData = terrain.terrainData;
        Vector3 terrainSize = tData.size;
        Vector3 terrainOrigin = terrain.transform.position;

        int gridCountX = Mathf.CeilToInt(Mathf.Sqrt(numMeshes));
        int gridCountZ = Mathf.CeilToInt((float)numMeshes / gridCountX);

        // For each submesh the high LOD dimensions.
        int subPolyWidth = polyWidth / gridCountX;
        int subPolyLength = polyLength / gridCountZ;

        // Clean up any previous children.
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        for (int gz = 0; gz < gridCountZ; gz++)
        {
            for (int gx = 0; gx < gridCountX; gx++)
            {
                // Define region boundaries in normalized terrain space.
                float regionStartX = (float)gx / gridCountX;
                float regionEndX = (float)(gx + 1) / gridCountX;
                float regionStartZ = (float)gz / gridCountZ;
                float regionEndZ = (float)(gz + 1) / gridCountZ;

                // Calculate region center (for positioning).
                float centerNormalizedX = (regionStartX + regionEndX) * 0.5f;
                float centerNormalizedZ = (regionStartZ + regionEndZ) * 0.5f;
                float centerX = centerNormalizedX * terrainSize.x;
                float centerZ = centerNormalizedZ * terrainSize.z;
                float centerY = tData.GetInterpolatedHeight(centerNormalizedX, centerNormalizedZ);
                Vector3 regionCenter = terrainOrigin + new Vector3(centerX, centerY, centerZ);

                // --- High LOD Mesh Generation ---
                int verticesX = subPolyWidth + 1;
                int verticesZ = subPolyLength + 1;
                Vector3[] vertices = new Vector3[verticesX * verticesZ];
                Vector2[] uvs = new Vector2[verticesX * verticesZ];
                Vector3[] normals = new Vector3[verticesX * verticesZ];

                for (int z = 0; z < verticesZ; z++)
                {
                    for (int x = 0; x < verticesX; x++)
                    {
                        float tLocalX = (float)x / subPolyWidth;
                        float tLocalZ = (float)z / subPolyLength;
                        float globalX = Mathf.Lerp(regionStartX, regionEndX, tLocalX);
                        float globalZ = Mathf.Lerp(regionStartZ, regionEndZ, tLocalZ);

                        float height = tData.GetInterpolatedHeight(globalX, globalZ);
                        Vector3 worldPos = terrainOrigin + new Vector3(globalX * terrainSize.x, height, globalZ * terrainSize.z);
                        Vector3 localPos = worldPos - regionCenter;
                        int index = z * verticesX + x;
                        vertices[index] = localPos;
                        uvs[index] = new Vector2(globalX, globalZ) * TextureTile;
                        normals[index] = tData.GetInterpolatedNormal(globalX, globalZ);
                    }
                }

                int[] triangles = new int[subPolyWidth * subPolyLength * 6];
                int triIndex = 0;
                for (int z = 0; z < subPolyLength; z++)
                {
                    for (int x = 0; x < subPolyWidth; x++)
                    {
                        int baseIndex = z * verticesX + x;
                        triangles[triIndex++] = baseIndex;
                        triangles[triIndex++] = baseIndex + verticesX;
                        triangles[triIndex++] = baseIndex + verticesX + 1;
                        triangles[triIndex++] = baseIndex;
                        triangles[triIndex++] = baseIndex + verticesX + 1;
                        triangles[triIndex++] = baseIndex + 1;
                    }
                }

                Mesh mesh = new Mesh();
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.name = $"TerrainMesh_{gx}_{gz}";
                mesh.vertices = vertices;
                mesh.triangles = triangles;
                mesh.uv = uvs;
                mesh.normals = normals;
                mesh.RecalculateBounds();

                GameObject meshObj = new GameObject(mesh.name);
                meshObj.transform.parent = this.transform;
                meshObj.transform.position = regionCenter;
                meshObj.transform.rotation = Quaternion.identity;
                meshObj.transform.localScale = Vector3.one;

                MeshFilter mf = meshObj.AddComponent<MeshFilter>();
                MeshRenderer mr = meshObj.AddComponent<MeshRenderer>();
                mf.sharedMesh = mesh;
                TriangleCount += (mesh.triangles.Length / 3);

                MeshRenderer parentRenderer = GetComponent<MeshRenderer>();
                if (parentRenderer != null)
                    mr.sharedMaterial = parentRenderer.sharedMaterial;

                obs.Add(meshObj);

                // --- LOD2 Mesh Generation (using factor) ---
                // Compute reduced resolution based on the factor.
                int lod2SubPolyWidth = Mathf.Max(1, Mathf.RoundToInt(subPolyWidth * lod2Factor));
                int lod2SubPolyLength = Mathf.Max(1, Mathf.RoundToInt(subPolyLength * lod2Factor));

                // Add an extra ring (2 extra vertices per axis) for the skirt.
                int lod2_verticesX = lod2SubPolyWidth + 3;
                int lod2_verticesZ = lod2SubPolyLength + 3;
                Vector3[] lod2_vertices = new Vector3[lod2_verticesX * lod2_verticesZ];
                Vector2[] lod2_uvs = new Vector2[lod2_verticesX * lod2_verticesZ];
                Vector3[] lod2_normals = new Vector3[lod2_verticesX * lod2_verticesZ];

                for (int z = 0; z < lod2_verticesZ; z++)
                {
                    for (int x = 0; x < lod2_verticesX; x++)
                    {
                        int index = z * lod2_verticesX + x;
                        float tLocalX, tLocalZ;

                        // Interior vertices use indices 1 .. lod2_verticesX-2.
                        if (x == 0)
                            tLocalX = 0f;
                        else if (x == lod2_verticesX - 1)
                            tLocalX = 1f;
                        else
                            tLocalX = (x - 1) / (float)lod2SubPolyWidth;

                        if (z == 0)
                            tLocalZ = 0f;
                        else if (z == lod2_verticesZ - 1)
                            tLocalZ = 1f;
                        else
                            tLocalZ = (z - 1) / (float)lod2SubPolyLength;

                        float globalX = Mathf.Lerp(regionStartX, regionEndX, tLocalX);
                        float globalZ = Mathf.Lerp(regionStartZ, regionEndZ, tLocalZ);
                        float height = tData.GetInterpolatedHeight(globalX, globalZ);
                        Vector3 worldPos = terrainOrigin + new Vector3(globalX * terrainSize.x, height, globalZ * terrainSize.z);
                        Vector3 localPos = worldPos - regionCenter;

                        // Check if this vertex is on the border (extra skirt).
                        bool isBorder = (x == 0 || x == lod2_verticesX - 1 || z == 0 || z == lod2_verticesZ - 1);
                        if (isBorder)
                        {
                            // Compute horizontal direction from the center.
                            Vector3 dir = localPos;
                            dir.y = 0;
                            if (dir != Vector3.zero)
                                dir = dir.normalized;
                            // Offset outward and push downward.
                            localPos += dir * lowLODSkirtHorizontal;
                            localPos.y -= lowLODSkirtVertical;
                        }

                        lod2_vertices[index] = localPos;
                        lod2_uvs[index] = new Vector2(globalX, globalZ) * TextureTile;

                        // Blend the terrain normal with down if border, otherwise use terrain normal.
                        Vector3 terrainNormal = tData.GetInterpolatedNormal(globalX, globalZ);
                        //if (isBorder)
                        //    lod2_normals[index] = Vector3.Lerp(terrainNormal, Vector3.down, 0.5f).normalized;
                        //else
                        //    lod2_normals[index] = terrainNormal;

                        lod2_normals[index] = terrainNormal;
                    }
                }

                // Create triangles for the LOD2 mesh.
                int lod2_quadCount = (lod2_verticesX - 1) * (lod2_verticesZ - 1);
                int[] lod2_triangles = new int[lod2_quadCount * 6];
                int lod2_triIndex = 0;
                for (int z = 0; z < lod2_verticesZ - 1; z++)
                {
                    for (int x = 0; x < lod2_verticesX - 1; x++)
                    {
                        int baseIndex = z * lod2_verticesX + x;
                        lod2_triangles[lod2_triIndex++] = baseIndex;
                        lod2_triangles[lod2_triIndex++] = baseIndex + lod2_verticesX;
                        lod2_triangles[lod2_triIndex++] = baseIndex + lod2_verticesX + 1;
                        lod2_triangles[lod2_triIndex++] = baseIndex;
                        lod2_triangles[lod2_triIndex++] = baseIndex + lod2_verticesX + 1;
                        lod2_triangles[lod2_triIndex++] = baseIndex + 1;
                    }
                }

                Mesh lod2Mesh = new Mesh();
                lod2Mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                lod2Mesh.name = mesh.name + "_LOD2";
                lod2Mesh.vertices = lod2_vertices;
                lod2Mesh.triangles = lod2_triangles;
                lod2Mesh.uv = lod2_uvs;
                lod2Mesh.normals = lod2_normals;
                lod2Mesh.RecalculateBounds();

                // Create a child GameObject for the LOD2 mesh.
                GameObject lod2Obj = new GameObject(lod2Mesh.name);
                lod2Obj.transform.parent = meshObj.transform;
                lod2Obj.transform.localPosition = Vector3.zero;
                lod2Obj.transform.localRotation = Quaternion.identity;
                lod2Obj.transform.localScale = Vector3.one;

                MeshFilter lod2MF = lod2Obj.AddComponent<MeshFilter>();
                MeshRenderer lod2MR = lod2Obj.AddComponent<MeshRenderer>();

                TriangleCountLow += (lod2Mesh.triangles.Length / 3);

                lod2MF.sharedMesh = lod2Mesh;
                // Use the same material as the high LOD.
                lod2MR.sharedMaterial = mr.sharedMaterial;

                MeshCollider lod2Col = lod2Obj.AddComponent<MeshCollider>();
                lod2Col.sharedMesh = lod2Mesh;


                obs2.Add(lod2Obj);

                // Disable the LOD2 object for now.
               // lod2Obj.SetActive(false);
            }
        }

        MeshFilter parentMF = GetComponent<MeshFilter>();
        if (parentMF != null)
            parentMF.sharedMesh = null;

        Debug.Log("Generated " + (gridCountX * gridCountZ) + " high LOD sub-meshes with LOD2 children.");


        float cullsize = Mathf.Sqrt((float)numMeshes);

        cullsize = terrain.terrainData.size.x / cullsize;

        Debug.Log("tmesh cull size is " + cullsize);

        for (int i = 0; i < obs2.Count; i++)
        {
            BGRenderer br = obs2[i].AddComponent<BGRenderer>();
            br.texture = tiledtexture;
            br.Tint = Tint;
            br.MeshBounds = cullsize;
            br.StaticBakedMesh = true;
            br.DisableBgRender = true;
            br.UseAlphaSpecMetal = UseAlphaForSpecular;
          //  br.SpecularMult = 0.9f;
            MeshCollider col = obs2[i].AddComponent<MeshCollider>();
            col.sharedMesh = obs2[i].GetComponent<MeshFilter>().sharedMesh;
        }

        for (int i = 0; i < obs.Count; i++)
        {
            BGRenderer br = obs[i].AddComponent<BGRenderer>();
            br.texture = tiledtexture;
            br.Tint = Tint;
            br.MeshBounds = cullsize;
            br.StaticBakedMesh = true;
            br.UseAlphaSpecMetal = UseAlphaForSpecular;
            //  br.SpecularMult = 0.9f;


            MeshCollider col = obs[i].AddComponent<MeshCollider>();
            col.sharedMesh = obs[i].GetComponent<MeshFilter>().sharedMesh;

            br.Lods = new BGRenderer.Lod[3];
            br.Lods[0] = new BGRenderer.Lod();
            br.Lods[0].EndDistance = TerrainLodDistance;
            br.Lods[0].mesh = br;

            br.Lods[1] = new BGRenderer.Lod();
            br.Lods[1].EndDistance = 9999;
            br.Lods[1].mesh = obs2[i].GetComponent<BGRenderer>();


        }
    }
}
