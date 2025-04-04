using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BGRenderer : MonoBehaviour
{
    public bool AnimatedMesh;
    public BGRenderer ParentAnimator;
    [Tooltip("Updates all transform data from Unity transform every frame.")]
    public bool DynamicUnityTransform;
    [Tooltip("This bakes the mesh vertices to it's transform data.  Memory trade off since it creates a unique clone of mesh per object, but it's fast!")]
    public bool StaticBakedMesh;
    [Tooltip("Creates a unique material for this object.")]
    public bool UniqueMaterial;
    [Tooltip("You must manually set mesh bounds since unity does not easily provide accurate mesh bounds")]
    public float MeshBounds = 2;
    [Tooltip("Uses all properties from a given meshes material, grouping it into that material instance.")]
    public BGRenderer CopyMaterial;
    [Tooltip("Will disable rendering.  Will not update in real time unless manually updated")]
    public bool DisableBgRender;
    [Tooltip("Disables Unity render to reduce overhead.")]
    public bool DisableUnityRender = true;
    [Tooltip("Object tint.  Only shows up for non textured objects since it was deemed too expensive.")]
    public Color Tint = Color.white;
    [Tooltip("Generates a random unique tint for this object")]
    public bool RandomTint;
    [Tooltip("Texture assigned to this objects material.  Select 'uniqie material' if there are other matching mesehes in scene with other textures")]
    public Texture2D texture;
    [Tooltip("Uses unique compressed specular and metal in alpha. See the 'PhysicalBasedTextureCompress script'")]
    public bool UseAlphaSpecMetal;
    [Tooltip("Specular value of material if alpha specular is disabled.")]
    public float SpecularMult;
    [Tooltip("Metallic value of material if alpha specular is disabled.")]
    public float MetalicMult;
    [Tooltip("Lods to use")]
    public Lod[] Lods;
    [Tooltip("Mesh groups allow multiple meshes to be rendered as one mesh, savinging on performance and transform count.")]
    public BGRenderer[] MeshGroup;
    [Tooltip("This randomizes the start frame of the animation on start.")]
    public bool RandomizeFrame;
    [HideInInspector]
    public float CurrentAnimFrame;
    public List<BGRenderer> AnimFrames;
    [HideInInspector]
    public Vector2Int CurrentClip;
    [HideInInspector]
    public Vector2Int LastClip;
    [HideInInspector]
    public List<Vector2Int> AnimClipIndex;
    [HideInInspector]
    public float PlaySpeed = 1;
    [HideInInspector]
    public int PlayMode;
    [HideInInspector]
    public Mesh mymesh;
    [HideInInspector]
    public Material mymat;
    [HideInInspector]
    public int MeshIndex;
    [HideInInspector]
    public int ObjectIndex;
    [HideInInspector]
    public int TexId;
    [HideInInspector]
    public int currenlod = -10;
    [HideInInspector]
    public bool called;
    [HideInInspector]
    public float cameraDist;
    [HideInInspector]
    public List<BGRenderer> childAnims;


    [System.Serializable]
    public class Lod
    {
        public BGRenderer mesh;
        public BGRenderer meshgroup;
        public float EndDistance = 20;

    }

    //void Awake()
    //{
    //    MeshFilter mf = gameObject.GetComponent<MeshFilter>();
    //    if (UniqueMaterial)
    //    {
    //        mf.sharedMesh = mf.mesh;

    //    }
    //}

    // Start is called before the first frame update
    public void Start()
    {
        if (!called)
        {
            if (RandomTint)
            {
                Tint = new Color(Random.value, Random.value, Random.value, 1);
            }

            if (AnimatedMesh)
            {
              
                if (Lods.Length > 0)
                {
                    AnimFrames = Lods[0].mesh.AnimFrames;
                    AnimClipIndex = Lods[0].mesh.AnimClipIndex;
                    CurrentClip = AnimClipIndex[0];
                  
                }

            }

            if (RandomizeFrame && ParentAnimator == null)
            {
                CurrentAnimFrame = Random.Range(CurrentClip.x, CurrentClip.y - 1);
            }
            else
            {
                if (ParentAnimator != null)
                {
                    if (ParentAnimator.childAnims == null) ParentAnimator.childAnims = new List<BGRenderer>();
                    ParentAnimator.childAnims.Add(this);
                }
            }

            bool debugestuf = false ;


            if (AnimFrames == null) AnimFrames = new List<BGRenderer>();
            if (Lods == null) Lods = new Lod[0];

            called = true;
            currenlod = -10;
            BgCamera cm = Camera.main.GetComponent<BgCamera>();

         
                SkinnedMeshRenderer sm = gameObject.GetComponent<SkinnedMeshRenderer>();

                MeshFilter mf = gameObject.GetComponent<MeshFilter>();

                bool nomesh = false;

                if (sm != null)
                {
                mymat = sm.sharedMaterial;
                if (AnimFrames.Count > 0)
                    {
                       // mf = AnimFrames[0].GetComponent<MeshFilter>();
                        mf = gameObject.AddComponent<MeshFilter>();
                        mf.sharedMesh = AnimFrames[0].GetComponent<MeshFilter>().sharedMesh;
                       // mf.sharedMesh = mf.mesh;
                        Debug.Log("IS ANIMATED");
                        debugestuf = true;
                    }
                    else
                    {
                        Debug.Log("could not find animated frames "+transform.name);

                        nomesh = true;
                    }
                }

                if (mf != null)
                {
                if (!UniqueMaterial && !CopyMaterial) mymesh = mf.sharedMesh;
                mymat = mf.GetComponent<Renderer>().sharedMaterial;
                if (!mf.sharedMesh.isReadable)
                    {
                        Debug.Log("mesh is not readable");
                        nomesh = true;
                    }
                }



            LastClip = CurrentClip;

            if (!nomesh)
                {

                    if (UniqueMaterial)
                    {
                        if (mymesh == null)
                        {
                            mymesh = mf.mesh;
                       
                            // mf.sharedMesh = mymesh;
                        }

                    }

                    if (CopyMaterial)
                    {
                        if (CopyMaterial.mymesh == null)
                        {
                            MeshFilter mf2 = CopyMaterial.GetComponent<MeshFilter>();
                            CopyMaterial.mymesh = mf2.mesh;
                        }

                        texture = CopyMaterial.texture;
                        mf.sharedMesh = CopyMaterial.mymesh;
                    }



                // int rnd = Random.Range(0, cm.Meshes.Count - 1);


                if (cm != null && cm.enabled) cm.Meshes.Add(mf);

                 

                    if (DisableUnityRender)
                    {
                        if (sm != null) sm.enabled = false;
                        else gameObject.GetComponent<Renderer>().enabled = false;
                    }
                }

            }
        
    }


    int index;
    Vector3 temppos;
    Vector4 temprot;
    public void UpdateChildren()
    {
         temppos = BgCamera.allTransformsarray[ObjectIndex].position;
         temprot = BgCamera.allTransformsarray[ObjectIndex].rotation;
   

        for (int i = 0; i < childAnims.Count; i++)
        {
             index = childAnims[i].ObjectIndex;
          //  ref BgCamera.BgTrans temptrans = ref BgCamera.allTransformsarray[index];

            BgCamera.allTransformsarray[index].position = temppos;
            BgCamera.allTransformsarray[index].rotation = temprot;
        }
    }



    [ContextMenu("Update Material Properties")]
    public void UpdateMaterialProperties()
    {
        float met = (float)(((int)(MetalicMult * 4) ) * 0.25f);
     //   Debug.Log("metallic value " + met);
        BgCamera.UpdateMaterialProps(MeshIndex, TexId, SpecularMult, met);
    }



    Quaternion trot;
    [ContextMenu("Update Unity Transform")]
    public void UpdateUnityTransform()
    {
        index = ObjectIndex;
        BgCamera.allTransformsarray[index].position = transform.position;
        trot = transform.rotation;
        BgCamera.allTransformsarray[index].rotation = new Vector4(trot.x, trot.y, trot.z, trot.w);
        BgCamera.allTransformsarray[index].scale = transform.lossyScale;
    }


}
