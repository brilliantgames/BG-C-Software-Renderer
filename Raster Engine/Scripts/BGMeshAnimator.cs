using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BGMeshAnimator : MonoBehaviour
{

    public List<SkinnedMeshRenderer> Meshes;

    public AnimClip[] AnimationClips;

    Animator anim;

    [System.Serializable]
    public class AnimClip
    {
        public AnimationClip clip;
        public int Frames = 20;
        public List<MeshList> MeshFrames;

    }

    [System.Serializable]
    public class MeshList
    {
        public Mesh[] MeshFrames;
    }

    // Start is called before the first frame update
    void Awake()
    {
        anim = gameObject.GetComponent<Animator>();

        BakeAnimations();
    }


    public static Mesh BakeMeshLocal(Mesh mesh, Transform transform)
    {
        // Create a new mesh instance and set its name.
        Mesh bakedMesh = new Mesh();
        bakedMesh.name = mesh.name + " (Baked Local)";

        // Create a transformation matrix from the transform's local properties.
        Matrix4x4 localMatrix = Matrix4x4.TRS(transform.localPosition, transform.localRotation, transform.localScale);
        // Use the inverse transpose for correct normal transformation (handles non-uniform scaling).
        Matrix4x4 normalMatrix = localMatrix.inverse.transpose;

        // Bake vertices using the local matrix.
        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = localMatrix.MultiplyPoint3x4(vertices[i]);
        }
        bakedMesh.vertices = vertices;

        // Bake normals if available.
        if (mesh.normals != null && mesh.normals.Length == mesh.vertexCount)
        {
            Vector3[] normals = mesh.normals;
            for (int i = 0; i < normals.Length; i++)
            {
                normals[i] = normalMatrix.MultiplyVector(normals[i]).normalized;
            }
            bakedMesh.normals = normals;
        }
        else
        {
            bakedMesh.RecalculateNormals();
        }

        // Bake tangents if available.
        if (mesh.tangents != null && mesh.tangents.Length == mesh.vertexCount)
        {
            Vector4[] tangents = mesh.tangents;
            for (int i = 0; i < tangents.Length; i++)
            {
                // Extract the tangent as a Vector3, transform it, then reapply the w component.
                Vector3 tangent = new Vector3(tangents[i].x, tangents[i].y, tangents[i].z);
                tangent = localMatrix.MultiplyVector(tangent).normalized;
                tangents[i] = new Vector4(tangent.x, tangent.y, tangent.z, tangents[i].w);
            }
            bakedMesh.tangents = tangents;
        }

        // Copy UVs, colors, and other mesh data if they exist.
        bakedMesh.uv = mesh.uv;
        bakedMesh.uv2 = mesh.uv2;
        bakedMesh.colors = mesh.colors;
        bakedMesh.colors32 = mesh.colors32;

        // Copy triangles and submesh data.
        bakedMesh.subMeshCount = mesh.subMeshCount;
        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            bakedMesh.SetTriangles(mesh.GetTriangles(i), i);
        }

        // Optionally copy bindposes and bone weights.
        bakedMesh.bindposes = mesh.bindposes;
        bakedMesh.boneWeights = mesh.boneWeights;

        // Recalculate bounds for the new vertex positions.
        bakedMesh.RecalculateBounds();

        return bakedMesh;
    }


    void BakeAnimations()
    {
        int startframe = 0;
        for (int i = 0; i < AnimationClips.Length; i++)
        {
            string cnam = AnimationClips[i].clip.name;
            anim.speed = 0.00001f;

          

            Vector2Int clipindex = new Vector2Int(startframe, startframe + AnimationClips[i].Frames);
            startframe += AnimationClips[i].Frames;

            //ADD CLIP INDEXS
            for (int m = 0; m < Meshes.Count; m++)
            {
                BGRenderer mm = Meshes[m].gameObject.GetComponent<BGRenderer>();
                if (mm.AnimClipIndex == null) mm.AnimClipIndex = new List<Vector2Int>();
                mm.AnimClipIndex.Add(clipindex);
            }

            for (int f = 0; f < AnimationClips[i].Frames; f++)
            {
                float normtime = (float)f / (float)AnimationClips[i].Frames;
                anim.Play(AnimationClips[i].clip.name, 0, normtime);

                anim.Update(0);
                Mesh[] nm = new Mesh[Meshes.Count];

               

               
             
                for (int m = 0; m < Meshes.Count; m++)
                {
                   

                    nm[m] = new Mesh();

                    if (Meshes[m].rootBone == null)
                    {
                        Transform last = Meshes[m].transform.parent;
                        Meshes[m].transform.parent = transform;
                        nm[m] = BakeMeshLocal(Meshes[m].sharedMesh, Meshes[m].transform);
                        Meshes[m].transform.parent = last;
                    }
                    else
                    {
                        Meshes[m].BakeMesh(nm[m]);
                    }

                    GameObject newob = new GameObject(cnam + f + " - " + Meshes[m].name);

                    MeshFilter mf = newob.AddComponent<MeshFilter>();

                    MeshRenderer nr = newob.AddComponent<MeshRenderer>();
                  //  nr.enabled = false;
                    nr.sharedMaterial = Meshes[m].sharedMaterial;

                    mf.sharedMesh = nm[m];

                    newob.transform.position = Meshes[m].transform.position;
                    newob.transform.rotation = Meshes[m].transform.rotation;
                    newob.transform.parent = Meshes[m].transform;

                   BGRenderer nbr = newob.AddComponent<BGRenderer>();
                 
                    BGRenderer mm = Meshes[m].gameObject.GetComponent<BGRenderer>();
                    if (mm.AnimFrames == null) mm.AnimFrames = new List<BGRenderer>();

                   
                    nbr.texture = mm.texture;
                    nbr.DisableBgRender = true;
                    mm.AnimFrames.Add(nbr);
                    nbr.SpecularMult = mm.SpecularMult;
                    nbr.MetalicMult = mm.MetalicMult;
                    nbr.UseAlphaSpecMetal = mm.UseAlphaSpecMetal;
                  

                  //  nbr.IsDynamic = true;
                   // nbr.NoAdd = true;
                   // nbr.CopyMaterial = mm;
                    //nbr.enabled = false;
                   // nbr.Start();
                  //  mm.AnimFrames.Add(nbr);
                }

                MeshList ml = new MeshList();

                ml.MeshFrames = nm;

                AnimationClips[i].MeshFrames.Add(ml);

               

            }

        }


        for (int i = 0; i < Meshes.Count; i++)
        {
            BGRenderer mm = Meshes[i].gameObject.GetComponent<BGRenderer>();

            int[] mindex = new int[mm.MeshGroup.Length];

            for (int id = 0; id < mindex.Length; id++)
            {
                mindex[id] = Meshes.IndexOf(mm.MeshGroup[id].GetComponent<SkinnedMeshRenderer>());
            }

            int m = 0;
            for (int a = 0; a < AnimationClips.Length; a++)
            {

                for (int cf = 0; cf < AnimationClips[a].Frames; cf++)
                {
                    BGRenderer am = mm.AnimFrames[m];
                    am.MeshGroup = new BGRenderer[mm.MeshGroup.Length];

                    //am.SpecularMult = Meshes[m].gameObject.GetComponent<BGRenderer>().SpecularMult;
                    //am.MetalicMult = Meshes[m].gameObject.GetComponent<BGRenderer>().MetalicMult;

                    for (int g = 0; g < mm.MeshGroup.Length; g++)
                    {
                        am.MeshGroup[g] = Meshes[mindex[g]].gameObject.GetComponent<BGRenderer>().AnimFrames[m];
                     
                        //  am.MeshGroup[g] = AnimationClips[a]
                    }


                    m += 1;

                }
            }
            
        }


        anim.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
