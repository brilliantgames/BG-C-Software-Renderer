using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Collections.LowLevel.Unsafe;


//NOTE!  YOU MUST ALLOW UNSAFE CODE FOR THIS TO WORK IN A UNITY PROJECT!

public class BgCamera : MonoBehaviour
{
    [Tooltip("Uses a CPU thread to calculate LODs and animation timing when large numbers of objects are present.")]
    public bool ThreadLodAndAnimation = true;
    [Tooltip("Rendering will use all cores of the CPU for rendering tasks")]
    public bool UseCpuThreadCount = true;
    [Tooltip("Enalbes SIMD optimization for rendering. Highly recommended especially for higher resolution.")]
    public bool UseSimdOptimize = true;
    [Tooltip("Displays FPS and other important engine stats.")]
    public bool DebugMode;
    bool showhud;
    [Tooltip("CPU threads used for rasterizing(automatically set with UseCPuThreadCount)")]
    public int ThreadCount = 1;
    [Tooltip("CPU threads used for differed pixel lighting(automatically set with UseCPuThreadCount)")]
    public int PixelThreadCount = 1;
    public bool render = true;
    [Tooltip("shows triangle debug view")]
    public bool DebugTriangles;
    [Tooltip("Must be assigned!  The scenes main directional light")]
    public Light Sun;
    [Tooltip("Ambient light for sky")]
    public Color Ambient = new Color(0.4f, 0.5f, 0.6f);
    [Tooltip("ground light color for rake bounce light")]
    public Color GroundBounceAmbient = new Color(0.4f, 0.4f, 0.4f);
    [Tooltip("Background sky color.")]
    public Color Background = new Color(0.3f,0.3f,1,1);
    [Tooltip("Background horizon color.")]
    public Color Horizon = new Color(0.75f,0.75f,1,1);
    public bool DisablePointLights;
    public float FogDist = 800;
    [Tooltip("Per pixel differed lighting")]
    public bool DifferedRender = true;
    [Tooltip("Per vertex lighting")]
    public bool VertexLighting;
    [Tooltip("Will use current screen resolution for resolution.(Dangerous for high res displays)")]
    public bool UseScreenRes;
    public int Width = 1280;
    public int Height = 720;
    public float fov = 60f;

    //CAMERA STATS
    Vector3 cameraForward;
    Vector3 cameraPosition;
    Vector3 cameraUp;
    Vector3 cameraRight;
    float aspectRatio;
    float tanFovHalf;

    public List<MeshFilter> Meshes;

    public List<Mesh> MeshTrack;

    Thread[] threads;

    public static List<BgTrans> allTransforms;

    List<BGRenderer> dynamicunity;

    public static BgTrans[] allTransformsarray;

    public List<BGRenderer> Lods;
    public static List<Texture2D> alltextures;
    List<Color32[]> alltexarrays;
    public static List<BgTrans[]> MassGroupRenders;
    public static List<int> counts;
    Thread lodthread;

    public class bgmesh
    {
        public int[] Tris;
        public Vector3[] Verts;
        public Vector3[] Norms;
        public Vector2[] uvs;
        public Vector3[] facenorms;
        public BgTrans[] Positions;
        public Color32[] texture;
        public Color32[] cols;

        public Texture2D textwod;
        public int texindex;
        public int TexWidth;
        public int TexHeight;
        public float spec;
        public float met;
        public bool UseAlpha;
        public List<int> group;
        public List<Transform> transforms;
        public Color tint = Color.white;
        public bool StaticBaked;
    };

    public struct BgTrans
    {
        public Vector3 position;
        public Vector4 scale;
        public Vector4 rotation;
        public int meshindex;
        public Color32 tint;

    };




    [StructLayout(LayoutKind.Sequential)]
    public struct color32
    {
        public byte r, g, b, a;

        public color32(byte r, byte g, byte b, byte a = 255)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        // Implicit: Unity Color32 → color32
        public static implicit operator color32(Color32 c) => new color32(c.r, c.g, c.b, c.a);

        // Implicit: Unity Color (float) → color32 (byte)
        public static implicit operator color32(Color c) => new color32(
            (byte)(Mathf.Clamp01(c.r) * 255f),
            (byte)(Mathf.Clamp01(c.g) * 255f),
            (byte)(Mathf.Clamp01(c.b) * 255f),
            (byte)(Mathf.Clamp01(c.a) * 255f)
        );

        // Explicit: color32 → Unity Color (float)
        public static explicit operator Color(color32 c) => new Color(
            c.r / 255f,
            c.g / 255f,
            c.b / 255f,
            c.a / 255f
        );

        // Optional: For debugging
        public override string ToString() => $"color32({r}, {g}, {b}, {a})";
    }


    public List<bgmesh> allmeshes;
    List<int> trisCounts;
    List<int> vertCounts;
    int[] MeshIndexes;
    public RenderTexture Final;
    public RenderStats Stats;
    public Renderer quad;
    Material mat;

    [System.Serializable]
    public class RenderStats
    {
        public int TotalTris;
        public int TotalMeshes;
        public int PixelsFilled;
        public int TrisOnScreen;
        public int DrawCalls;
    }


    struct Vector4Int
    {
      public int x, y, z, w;
    }

    public struct PointLight
    {
      public  Color color;
      public Vector3 position;
      public float Range;
    }

    PointLight[] pointlightsrender;
    public List<BGPointLight> pointLights;

    //TEXTURES
    Color32[] RenderTexture;
    Color32[] normsScreen;
    int[] Depth;

    [DllImport("RasterEngine")]
    //private static extern void InitializeBuffers(int MeshCount, int[] meshIndexes, Color32[] rendertex, float[] depth, int[] trisCounts, int[] vertCounts, Vector3[] positions, int width, int height);
    private static extern void InitializeBuffers(Color32[] rendertex, Color32[] normsScreen, int[] depth, int width, int height);
    // void InitializeBuffers(Color32* rendertex, float* depth, int width, int height)
    [DllImport("RasterEngine")]
    private static extern void SetMeshData(int row, Vector3[] verts, Vector3[] norms, int[] tris);

    [DllImport("RasterEngine")]
    private static extern void AddMeshes(bgmesh[] allmeshes);

    [DllImport("RasterEngine")]
    private static extern void SetCameraSettings(int width, int height, Vector3 cameraforward, Vector3 cameraposition,
        Vector3 cameraup, Vector3 cameraright, float aspectratio, float tanFovhalf);

   // [DllImport("RasterEngine")]
    [DllImport("RasterEngine", CallingConvention = CallingConvention.Cdecl)]
    private static extern int RenderAll(int ObjectCount);

    [DllImport("RasterEngine")]
    private static extern void Clear(int threads, Color32 background, Color32 clearColor);

    [DllImport("RasterEngine")]
    private static extern void DestroyBuffers(int count);

    [DllImport("RasterEngine")]
    private static extern Vector4Int RenderObject(Vector3 Objectpos, Vector3[] VertLoc, int[] TrisLoc, int trisCount);

    [DllImport("RasterEngine")]
    private static extern void Differed(int numThreads);

    [DllImport("RasterEngine")]
    private static extern void SetLighting(Vector3 Ssundir, Color Ssuncol, float Ssunintense, Color Sambient,
    Color Sgroundambient, Color Sskycolor, Color Shorzcolor, PointLight[] Spointlights, int Spointlightcount, Color Sfogcolor, float Sfogdist);



    [DllImport("RasterEngine")]
    private static extern void AddTexture(Color32[] texture, int count);

    [DllImport("RasterEngine")]
    private static extern void AddMesh(Vector3[] verts, Vector3[] norms, Vector3[] faces, Vector2[] uvs, Color32[] triscols, int[] tris, int vcount, int tcount,
        int texindex, int texwidth, int texheight, int groupcount, int[] group, float spec, float met, bool isstatic, bool usealpha);

    [DllImport("RasterEngine")]
    private static extern void UpdateMeshProps(int meshindex, int texindex, int texwidth, int texheight, float spec, float met);

    public static void UpdateMaterialProps(int meshindex, int texindex, float spec, float met)
    {
       // Debug.Log("tex index is " + texindex + " tex count is " + alltextures.Count);
        int w = 1;
        int h = 1;

        if (texindex >= 0)
        {
            w = alltextures[texindex].width;
            h = alltextures[texindex].height;
        }
        UpdateMeshProps(meshindex, texindex, w, h, spec, met);
    }


    //__declspec(dllexport) Vector4Int BgRenderObjects(
    //    Vector3* Objects, int numThreads, int Count,
    //    Vector3* VertLoc, Vector3* NormsLoc,
    //    Color32* vertcols, int* TrisLoc, int trisCount)
    //{

    // [DllImport("RasterEngine")]
    [DllImport("RasterEngine", CallingConvention = CallingConvention.Cdecl)]
    private static extern Vector4Int BgRenderObjects(
        BgTrans[] Objects, int numThreads, int Count,
        Vector3[] vertLoc, Vector3[] normsLoc, Vector3[] facenorms,
        Color32[] vertcols, int[] trisLoc, int trisCount, int vertCount);

    [DllImport("RasterEngine", CallingConvention = CallingConvention.Cdecl)]
    private static extern Vector4Int RenderObjects(
        BgTrans[] Objects, int numThreads, int Count,
        Vector3[] vertLoc, Vector3[] normsLoc, Vector2[] uvs, Vector3[] facenorms,
        Color32[] vertcols, int[] trisLoc, int trisCount, int vertCount, Color32 Tint, 
        int texindex, float texw, float texh, bool NoTextures);

    [DllImport("RasterEngine", CallingConvention = CallingConvention.Cdecl)]
    private static extern Vector4Int RenderObjectsPooled(
        System.IntPtr Objects, int numThreads, int Count, bool NoTextures, bool UseSimd, bool VertexLighting);

    // [DllImport("RasterEngine", CallingConvention = CallingConvention.Cdecl)]
    // private static extern Vector4Int RenderObjectsPooled(
    //BgTrans[] Objects, int numThreads, int Count, bool NoTextures, bool UseSimd, bool VertexLighting);


    BufferToTexture bt;
    int MeshCount;

     Vector3 generateNormal(Vector3 p1, Vector3 p2, Vector3 p3) {
	// Compute edge vectors directly
	float ux = p2.x - p1.x, uy = p2.y - p1.y, uz = p2.z - p1.z;
    float vx = p3.x - p1.x, vy = p3.y - p1.y, vz = p3.z - p1.z;

    // Compute cross product
    float nx = uy * vz - uz * vy;
    float ny = uz * vx - ux * vz;
    float nz = ux * vy - uy * vx;

    // Compute squared length (avoiding sqrt if already zero)
    float lengthSq = nx * nx + ny * ny + nz * nz;
        if (lengthSq == 0.0f) return Vector3.zero;

	// Normalize using reciprocal square root for performance
	float invLength = 1.0f / Mathf.Sqrt(lengthSq);
	return new Vector3( nx* invLength, ny * invLength, nz* invLength);
}


    Vector3[] GenerateFaceNorms(Vector3[] verts, int[] Tris)
    {
        Vector3[] fnorms = new Vector3[Tris.Length/3];
        for (int i = 0; i < Tris.Length/3; i++)
        {
            int cur = i * 3;

          fnorms[i] = generateNormal(verts[Tris[cur]], verts[Tris[cur+1]], verts[Tris[cur+2]]);

        }
        return fnorms;
    }



    public static float GetMaxBoundingBoxRadius(MeshRenderer renderer)
    {
        if (renderer == null)
            return 100;

        // Get the bounding box of the renderer
        Bounds bounds = renderer.bounds;

        // Compute the max radius (distance from center to the farthest corner)
        return bounds.extents.magnitude;
    }

    static void Shuffle<T>(IList<T> list)
    {
       System.Random rng = new System.Random();
        int n = list.Count;
        for (int i = n - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1); // Random index from 0 to i
            (list[i], list[j]) = (list[j], list[i]); // Swap elements
        }
    }


    public static void RenderObjectGroup(BgTrans[] objects, int count)
    {
        MassGroupRenders.Add(objects);
        if (counts == null) counts = new List<int>();
        counts.Add(count);
    }


    void Start()
    {
        showhud = true;
        try
        {
            //INITIALIZE STUFF!
            dynamicunity = new List<BGRenderer>();

            if (MassGroupRenders == null) MassGroupRenders = new List<BgTrans[]>();
            if (counts != null) counts = new List<int>();

            if (UseScreenRes)
            {
                Width = Screen.width;
                Height = Screen.height;
            }

            alltexarrays = new List<Color32[]>();
            alltextures = new List<Texture2D>();
            pointlightsrender = new PointLight[0];
            if (UseCpuThreadCount)
            {
                ThreadCount = SystemInfo.processorCount;
                PixelThreadCount = ThreadCount;
            }


            Lods = new List<BGRenderer>();
            bt = gameObject.GetComponent<BufferToTexture>();

            Stats = new RenderStats();

            MeshIndexes = new int[Meshes.Count];


            MeshTrack = new List<Mesh>();
            trisCounts = new List<int>();
            vertCounts = new List<int>();

            int mcounter = 0;

            allmeshes = new List<bgmesh>();
            BGRenderer rnd;

            //SHUFFLE MESHES FOR EVEN THREAD DISTRIBUTION
            Shuffle(Meshes);


            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();


            //GET MESH DATA
            for (int i = 0; i < Meshes.Count; i++)
            {
                rnd = Meshes[i].GetComponent<BGRenderer>();

                int ind = MeshTrack.IndexOf(Meshes[i].sharedMesh);

                //ADD NEW MESH
                if (ind < 0)
                {


                    MeshTrack.Add(Meshes[i].sharedMesh);
                    ind = MeshTrack.Count - 1;

                    bgmesh nm = new bgmesh();

                    nm.Verts = Meshes[i].sharedMesh.vertices;
                    nm.Norms = Meshes[i].sharedMesh.normals;
                    nm.uvs = Meshes[i].sharedMesh.uv;
                    nm.tint = rnd.Tint;

                    nm.Tris = Meshes[i].sharedMesh.triangles;

                    nm.TexWidth = 1;
                    nm.TexHeight = 1;
                    nm.group = new List<int>();
                    nm.texindex = -1;
                    nm.StaticBaked = rnd.StaticBakedMesh;

                    nm.spec = rnd.SpecularMult;
                    nm.met = (float)(((int)(rnd.MetalicMult * 4)) * 0.25f);

                    nm.UseAlpha = rnd.UseAlphaSpecMetal;

                    if (rnd.texture != null)
                    {
                     
                            if (!alltextures.Contains(rnd.texture))
                        {
                            Color32[] ntt = rnd.texture.GetPixels32();
                            nm.texture = ntt;
                            nm.TexWidth = rnd.texture.width;
                            nm.TexHeight = rnd.texture.height;
                           
                            alltexarrays.Add(ntt);
                            alltextures.Add(rnd.texture);
                        }

                        nm.textwod = rnd.texture;
                        nm.texture = alltexarrays[alltextures.IndexOf(rnd.texture)];

                        nm.texindex = Mathf.Max(0, alltextures.IndexOf(rnd.texture));

                        nm.TexWidth = alltextures[nm.texindex].width;
                        nm.TexHeight = alltextures[nm.texindex].height;

                      
                    }

                    //GENERATE FACE NORMALS FOR CULLING
                    nm.facenorms = GenerateFaceNorms(nm.Verts, nm.Tris);

                    nm.transforms = new List<Transform>();
                    nm.cols = new Color32[nm.Tris.Length / 3];

                    for (int c = 0; c < nm.cols.Length; c++)
                    {
                        nm.cols[c] = new Color32((byte)Random.RandomRange(0, 256), (byte)Random.RandomRange(0, 256), (byte)Random.RandomRange(0, 256), 255);
                    }

                    allmeshes.Add(nm);

                    mcounter += 1;
                }
             

                    allmeshes[ind].transforms.Add(Meshes[i].transform);


                if (rnd.DynamicUnityTransform)
                {
                    dynamicunity.Add(rnd);
                }

                Stats.TotalTris += allmeshes[ind].Tris.Length / 3;
            }

            Debug.Log("meshsetup 1 process time: " + stopwatch.ElapsedMilliseconds);
            stopwatch.Reset();
            stopwatch.Start();

            allTransforms = new List<BgTrans>();
            //CREATE POSITION ARRAYS
            int tcount = 1;
            for (int i = 0; i < allmeshes.Count; i++)
            {
                allmeshes[i].Positions = new BgTrans[allmeshes[i].transforms.Count];
                // float sz = GetMaxBoundingBoxRadius(Meshes[i].GetComponent<MeshRenderer>()) * 2f;

                float sz = 2;
                for (int t = 0; t < allmeshes[i].transforms.Count; t++)
                {


                    rnd = allmeshes[i].transforms[t].GetComponent<BGRenderer>();

                 

                    sz = rnd.MeshBounds;

                    allmeshes[i].Positions[t].position = allmeshes[i].transforms[t].position;
                    allmeshes[i].Positions[t].scale.x = allmeshes[i].transforms[t].lossyScale.x;
                    allmeshes[i].Positions[t].scale.y = allmeshes[i].transforms[t].lossyScale.y;
                    allmeshes[i].Positions[t].scale.z = allmeshes[i].transforms[t].lossyScale.z;

                    allmeshes[i].Positions[t].scale.w = sz;
                    allmeshes[i].Positions[t].rotation.x = allmeshes[i].transforms[t].rotation.x;
                    allmeshes[i].Positions[t].rotation.y = allmeshes[i].transforms[t].rotation.y;
                    allmeshes[i].Positions[t].rotation.z = allmeshes[i].transforms[t].rotation.z;
                    allmeshes[i].Positions[t].rotation.w = allmeshes[i].transforms[t].rotation.w;

                    allmeshes[i].Positions[t].tint = rnd.Tint;

                    if (!rnd.DisableBgRender) allmeshes[i].Positions[t].meshindex = i;
                    else allmeshes[i].Positions[t].meshindex = -1;


                    int rndrange = allTransforms.Count;


                    if (allTransforms.Count < 3000000)
                    {
                        rnd.ObjectIndex = allTransforms.Count;
                        allTransforms.Add(allmeshes[i].Positions[t]);
                    }
                    else
                    {
                        rndrange = Random.Range(0, allTransforms.Count - 1);
                        allTransforms.Insert(rndrange, allmeshes[i].Positions[t]);
                        rnd.ObjectIndex = rndrange;
                    }


                    rnd.TexId = allmeshes[i].texindex;

                    //ADD TO LOD MANAGE IF USING LODS
                    if (rnd.Lods.Length > 0) Lods.Add(rnd);

                    rnd.MeshIndex = i;
                    //rnd.ObjectIndex =

                }

                if (allmeshes[i].textwod != null) tcount += 1;
            }

            //CHECK FOR MESH GROUPS
            for (int i = 0; i < allmeshes.Count; i++)
            {

                for (int t = 0; t < allmeshes[i].transforms.Count; t++)
                {
                    rnd = allmeshes[i].transforms[t].GetComponent<BGRenderer>();

                    if (rnd.MeshGroup != null)
                    {
                        if (rnd.MeshGroup.Length > 0)
                        {
                            for (int g = 0; g < rnd.MeshGroup.Length; g++)
                            {
                                if (!allmeshes[i].group.Contains(rnd.MeshGroup[g].MeshIndex))
                                {
                                    allmeshes[i].group.Add(rnd.MeshGroup[g].MeshIndex);
                                }
                            }
                        }
                    }

                }

            }


            Debug.Log("meshsetup 2 process time: " + stopwatch.ElapsedMilliseconds);
            stopwatch.Reset();
            stopwatch.Start();

            //NOW ADD POSITIONS


            //GET MESH DATA


            //CREATE TEXTURES
            RenderTexture = new Color32[Width * Height];
            normsScreen = new Color32[Width * Height];
            Depth = new int[Width * Height];
            Final = new RenderTexture(Width, Height, 1);
            Final.enableRandomWrite = true;
            Final.filterMode = FilterMode.Point;
            Final.Create();



            //INTITALIZE BUFFERS
            InitializeBuffers(RenderTexture, normsScreen, Depth, Width, Height);

            //ADD MESHES
            int[] grp = new int[1];
            for (int i = 0; i < allmeshes.Count; i++)
            {
                    AddMesh(allmeshes[i].Verts, allmeshes[i].Norms, allmeshes[i].facenorms, allmeshes[i].uvs, allmeshes[i].cols,
                    allmeshes[i].Tris, allmeshes[i].Verts.Length, allmeshes[i].Tris.Length, allmeshes[i].texindex+1, 
                    allmeshes[i].TexWidth, allmeshes[i].TexHeight, allmeshes[i].group.Count+1, allmeshes[i].group.ToArray(), 
                    allmeshes[i].spec, allmeshes[i].met, allmeshes[i].StaticBaked, allmeshes[i].UseAlpha);
            }


            //ADD TEXTURES
            Color32[] blank = new Color32[1];
            blank[0] = Color.white;
            AddTexture(blank, 1);
            int texcount = 0;
            texcount += 1;

            for (int i = 0; i < alltexarrays.Count; i++)
            {
                    AddTexture(alltexarrays[i], alltexarrays[i].Length);

                    texcount += 1;
            }



            quad.gameObject.SetActive(true);
            mat = quad.sharedMaterial;
            mat.mainTexture = Final;


            MeshCount = Meshes.Count;

            UpdateTransformArray();
        }
        catch (System.Exception ex)
        {
            Debug.LogError(ex);
            enabled = false;
        }
     
    }

    void UpdateTransformArray()
    {
        allTransformsarray = allTransforms.ToArray();
    }

    void UpdateCamStats()
    {
        tanFovHalf = Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
        aspectRatio = Width / (float)Height;

        cameraForward = transform.forward;
        cameraPosition = transform.position;
        cameraUp = transform.up;
        cameraRight = transform.right;

        //UPDATE CPLUS
        SetCameraSettings(Width, Height, cameraForward, cameraPosition, cameraUp, cameraRight, aspectRatio, tanFovHalf);
    }

   

   public static float delta;





      public static void LodManage(List<BGRenderer> lods, BGRenderer[] lodarray, Vector3 cameraPosition)
    {
        BGRenderer temprend;
        BGRenderer temprend2;

        BgTrans templod;

        try
        {
            int cnt = 0;
            if (lods != null) cnt = lods.Count;
            else cnt = lodarray.Length;

            for (int i = 0; i < cnt; i++)
            {

                if (lods != null) temprend = lods[i];
                else temprend = lodarray[i];

                if (temprend.AnimatedMesh)
                {
                    if (temprend.ParentAnimator == null)
                    {
                       
                        temprend.CurrentAnimFrame = Mathf.Min(temprend.CurrentClip.y-0.01f, temprend.CurrentAnimFrame + delta * 30 * temprend.PlaySpeed);

                        if (temprend.CurrentAnimFrame >= temprend.CurrentClip.y - 0.1f || temprend.CurrentClip != temprend.LastClip)
                        {
                            if ((temprend.PlayMode == 1) && temprend.CurrentClip == temprend.LastClip)
                            {
                                temprend.PlaySpeed = 0;
                                temprend.CurrentAnimFrame = temprend.CurrentClip.y - 1;
                            }
                            else
                            {
                                temprend.CurrentAnimFrame = temprend.CurrentClip.x;
                            }

                            temprend.LastClip = temprend.CurrentClip;

                        }

                    }
                }

            }




            for (int i = 0; i < cnt; i++)
        {
                if (lods != null) temprend = lods[i];
                else temprend = lodarray[i];

                int objectindex = temprend.ObjectIndex;
            ref BgTrans temptrans = ref allTransformsarray[objectindex];
            float distance = 0;

            //IMMITATE PARENT ANIMATOR
            if (temprend.AnimatedMesh)
            {
                if (temprend.ParentAnimator != null)
                {
                    distance = temprend.ParentAnimator.cameraDist;
                }
                else
                {
                    distance = Vector3.Distance(allTransformsarray[objectindex].position, cameraPosition);
                    temprend.cameraDist = distance;
                }
            }
            else distance = Vector3.Distance(allTransformsarray[objectindex].position, cameraPosition);


                int chosenlod = -1;

                chosenlod = -1;

                for (int l = 0; l < temprend.Lods.Length; l++)
                {
                    if(distance < temprend.Lods[l].EndDistance)
                    {
                        chosenlod = l;
                        break;
                    }

                }
                //Debug.Log("choses lod for " + temprend.name+ " is " + chosenlod+" distance is "+distance);


            if (chosenlod != temprend.currenlod || temprend.AnimatedMesh)
            {


                //ASSIGN NEW MESH PROPS ON LOD CHANGE
                if (chosenlod >= 0)
                {
                   

                    if (chosenlod != temprend.currenlod)
                    {
                        temprend.currenlod = chosenlod;
                        if (temprend.AnimatedMesh)
                        {
                                temptrans.meshindex = temprend.Lods[chosenlod].mesh.AnimFrames[(int)temprend.CurrentAnimFrame].MeshIndex;
                        }
                        else
                        {
                             temptrans.meshindex = temprend.Lods[chosenlod].mesh.MeshIndex;
                        }
                    }

                        if (temprend.AnimatedMesh && temprend.ParentAnimator == null)
                        {
                            temptrans.meshindex = temprend.Lods[chosenlod].mesh.AnimFrames[(int)temprend.CurrentAnimFrame].MeshIndex;

                            if (temprend.childAnims.Count > 0)
                            {
                                for (int c = 0; c < temprend.childAnims.Count; c++)
                                {
                                    temprend2 = temprend.childAnims[c];
                                    ref BgTrans temptrans2 = ref allTransformsarray[temprend.childAnims[c].ObjectIndex];

                                    temprend2.CurrentAnimFrame = temprend2.ParentAnimator.CurrentAnimFrame;

                                    if (temprend2.currenlod >= 0) temptrans2.meshindex = temprend2.Lods[temprend2.currenlod].mesh.AnimFrames[(int)temprend2.CurrentAnimFrame].MeshIndex;
                                }
                            }
                        }
                    }
                else
                {
                    temptrans.meshindex = -1;
                    temprend.currenlod = -1;
                }

                }
        }
        }
        catch (System.Exception)
        {

            throw;
        }

    }


    void UpdatePointLights()
    {
        if (pointlightsrender.Length != pointLights.Count) pointlightsrender = new PointLight[pointLights.Count];

        for (int i = 0; i < pointLights.Count; i++)
        {
            if (pointLights[i].Dynamic)
            {
                pointLights[i].position = pointLights[i].transform.position;
            }

            pointlightsrender[i].position = pointLights[i].position;
            pointlightsrender[i].color = pointLights[i].color * pointLights[i].Intensity;
            pointlightsrender[i].Range = pointLights[i].Range;
        }

    }

    bool lodrunning;
    bool dolod;

    void Lodthread()
    {
       System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

        while (lodrunning)
        {
          
                stopwatch.Restart(); // Start measuring time

                LodManage(Lods, null, cameraPosition);

                stopwatch.Stop(); // Stop measuring time
                delta = (float)stopwatch.Elapsed.TotalSeconds; // Convert to seconds
      
            
        }
    }

    // Update is called once per frame
    void Update()
    {

        if (render)
        {
            if (DebugMode)
            {
                if (Input.GetKeyUp(KeyCode.H))
                {
                    showhud = !showhud;
                }

                if (Input.GetKeyUp(KeyCode.R))
                {
                    if (DifferedRender)
                    {
                        DifferedRender = false;
                        VertexLighting = true;
                    }
                    else
                    {
                        DifferedRender = true;
                        VertexLighting = false;
                    }
                }


                if (Input.GetKeyUp(KeyCode.T))
                {
                    DebugTriangles = !DebugTriangles;

                }

                if (Input.GetKeyUp(KeyCode.L))
                {
                    if (DifferedRender == false && VertexLighting == false)
                    {
                        VertexLighting = true;
                    }
                    else
                    {
                        if (DifferedRender == false && VertexLighting == true)
                        {
                            DifferedRender = true;
                            VertexLighting = false;
                        }
                        else
                        {
                            if (DifferedRender == true && VertexLighting == false)
                                DifferedRender = false;
                            VertexLighting = false;
                        }
                    }

         
                   

             

        }


            }


            //DYNAMIC UNITY TRANSFORMS
            int ind = 0;
            Quaternion rot;
            Vector4 rotwrite;
            for (int i = 0; i < dynamicunity.Count; i++)
            {
                ind = dynamicunity[i].ObjectIndex;
                allTransformsarray[ind].position = dynamicunity[i].transform.position;
                allTransformsarray[ind].scale = dynamicunity[i].transform.lossyScale;

                rot = dynamicunity[i].transform.rotation;
                rotwrite = new Vector4(rot.x, rot.y, rot.z, rot.w);

                allTransformsarray[ind].rotation = rotwrite;

            }


            if (VertexLighting && DifferedRender) DifferedRender = false;

            UpdateCamStats();

            //LOD MANAGE

            if (!ThreadLodAndAnimation || !dolod || Lods.Count < 500)
            {
                
                dolod = true;
                delta = Time.deltaTime;
                lodrunning = false;
                LodManage(Lods, null, cameraPosition);
            }
            else
            {
            
           
                    if (!lodrunning)
                    {
                        
                       lodthread = new Thread(Lodthread);
                        lodthread.Start();
                    }
                
                lodrunning = true;
            }



            UpdatePointLights();

            //CLEAR RENDER TEXTUER FIRST
            Clear(1, Background, Horizon);

            //RASTER RENDER
            DoRender();

            int pcount = pointlightsrender.Length;
            if (DisablePointLights) pcount = 0;

            //SET ALL THE LIGHTING SETTINGS TO ENGINE
            SetLighting(-Sun.transform.forward, (Sun.color), Sun.intensity, Ambient, GroundBounceAmbient,
                   Background, Horizon, pointlightsrender, pcount, Color.Lerp(Background, Horizon, 0.65f), FogDist);

            //differed
            if (DifferedRender && !DebugTriangles)
            {
                Differed(PixelThreadCount);
            }

            //SET THE RENDER TEXTURE TO GPU FOR VIEWING
             bt.SetRenderTexture32(Final, RenderTexture);

        }
        
    }


    //FASTEST I COULD COME UP WITH FOR SENDING AN ARRAY TO C++
    unsafe System.IntPtr AllocAndWrite(BgTrans[] array, out int count)
    {
        count = array.Length;
        int size = UnsafeUtility.SizeOf<BgTrans>();
        int totalSize = count * size;

        void* ptr = UnsafeUtility.Malloc(totalSize, 16, Unity.Collections.Allocator.Temp);

        fixed (BgTrans* srcPtr = array)
        {
            UnsafeUtility.MemCpy(ptr, srcPtr, totalSize);
        }

        return (System.IntPtr)ptr;
    }




    System.IntPtr ptr;
    System.IntPtr gptr;
    bool written;
    unsafe void DoRender()
    {
        int pixelcount = 0;
        int triscnt = 0;
        Vector4Int st = new Vector4Int();

        // === RENDER MAIN TRANSFORMS ===
        int tcount = Mathf.Min(allTransforms.Count / 2, ThreadCount);
       ptr = AllocAndWrite(allTransformsarray, out int count);

        if (DebugTriangles) st = RenderObjectsPooled(ptr, tcount, allTransforms.Count, DebugTriangles, false, VertexLighting);
        else   st = RenderObjectsPooled(ptr, tcount, allTransforms.Count, DebugTriangles, UseSimdOptimize, VertexLighting);
        pixelcount = st.x;
        triscnt = st.y;
        UnsafeUtility.Free((void*)ptr, Unity.Collections.Allocator.Temp);

        // === RENDER MASS GROUPS ===
        if (MassGroupRenders != null)
        {
            for (int i = 0; i < MassGroupRenders.Count; i++)
            {
                int groupCount = counts[i];
                tcount = Mathf.Min(groupCount / 2, ThreadCount);

                //CONVERT TRANSFORM ARRAY TO FAST C++ ARRAY
                gptr = AllocAndWrite(MassGroupRenders[i], out int _);
                Vector4Int st2 = new Vector4Int();
                if(DebugTriangles) st2 = RenderObjectsPooled(gptr, tcount, groupCount, DebugTriangles, false, VertexLighting);
                else st2 = RenderObjectsPooled(gptr, tcount, groupCount, DebugTriangles, UseSimdOptimize, VertexLighting);
                UnsafeUtility.Free((void*)gptr, Unity.Collections.Allocator.Temp);

                pixelcount += st2.x;
                triscnt += st2.y;
            }
        }

        // === STATS ===
        Stats.DrawCalls = allmeshes.Count;
        Stats.PixelsFilled = pixelcount;
        Stats.TrisOnScreen = triscnt;

        // === CLEAR ===
        MassGroupRenders?.Clear();
        counts?.Clear();

       // written = true;
    }





    private void OnDestroy()
    {
        MassGroupRenders = null;
       DestroyBuffers(0);
        lodrunning = false;
      if(lodthread != null)  lodthread.Abort();
    }

    private void OnGUI()
    {
        if (DebugMode && showhud)
        {
            GUI.skin.label.fontSize = 16;

            GUI.Label(new Rect(0, 0, 200, 36), "FPS: " + Mathf.RoundToInt(1 / Time.smoothDeltaTime));

            GUI.Label(new Rect(0, 25, 200, 36), "Tris: " + Stats.TotalTris);

            GUI.Label(new Rect(0, 50, 400, 36), "Tris On Screen: " + Stats.TrisOnScreen + ". Pixels Filled: " + Stats.PixelsFilled);

            GUI.Label(new Rect(0, 75, 200, 36), "Draw Calls: " + Stats.DrawCalls);

            GUI.Label(new Rect(0, 100, 200, 36), "Textures: " + alltextures.Count);

            GUI.Label(new Rect(0, 125, 200, 36), "Transforms: " + allTransforms.Count);

            GUI.Label(new Rect(0, 150, 200, 36), "Threads Used: " + ThreadCount);


            GUI.Label(new Rect((Screen.width / 2) - 200, Screen.height - 50, 300, 36), "Press L to toggle light modes");

            GUI.Label(new Rect((Screen.width / 2) +100, Screen.height - 50, 300, 36), "Press T to toggle triangle debug");
        }
    }

    //BRING IT TO THE BIG SCREEN
    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (Final != null)
        {
            // Blit the given render texture to the screen
            Graphics.Blit(Final, dest);
        }
        else
        {
            // Fallback: just blit the default source
            Graphics.Blit(src, dest);
        }
    }


}
