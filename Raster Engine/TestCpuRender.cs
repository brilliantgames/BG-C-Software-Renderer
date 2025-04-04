using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Runtime.InteropServices;


public class TestCpuRender : MonoBehaviour
{
    public bool TestBuffSet;
    public bool Unsafe;
    public bool Render;
    public bool Raster = true;
    public bool DoVertex = true;
    public bool CPlus;
   // public bool CPlusClear;
    public int Threads = 1;
    public bool DisableTextureWrite;
    public bool DisableBuffClear;

    public int Width = 1280;
    public int Height = 720;
    public float fov = 60f;
    public int shapecount = 10;
    public Light DirectionalLight;
    public Color Ambient = new Color32(40, 80, 250, 255);
    Color sn;
    Thread[] threads;
    Vector3 sundir;
    public Texture2D render;
    public RenderTexture rendergpu;

    Color32[] cols2;

    Color32[] black;
    
    int[] shapes;
    int[] screenverts;
    Color32[] shapecols;
    Color32[] rendercols;

    public Color32 col1;
    public Color32 col2;

    public int PixelWrites;
    byte[][] TestRender;

    public List<MeshFilter> Meshes;


    public List<int> ObjectMeshInstance;

    List<int[]> Tris;
    List<Vector3[]> Verts;
    List<Mesh> meshes;
    List<float> bounding;
    List<Vector3[]> Norms;

    public int TrisOnScreen;

    int[] depth;

    Vector3 cameraForward;
    Vector3 cameraPosition;
    Vector3 cameraUp;
    Vector3 cameraRight;


    float aspectRatio;
    float tanFovHalf;
    public Transform CamPlane;

   public Material preview;

    public trans[] Positions;



    BufferToTexture texconvert;


    [DllImport("Functions")]
    private static extern void CStart(int screenheight, int triscount);

    //[DllImport("Functions")]
    //private static extern void CopyScreenTextures(Color32[] colors, int[] dep, int row);

    //[DllImport("Functions")]
    //private static extern void CopyTrisStuff(int row, int[] shape, Color32[] rcols, int[] screenvert, int[] edge);


    [DllImport("Functions")]
    private static extern void CopyBuffers(Color32[] colors, int[] dep, int[] shape, Color32[] rcols, int[] screenvert, int[] edge);


    [DllImport("Functions")]
    private static extern void CRasterTris(int TrisOnScreen, int Height, int Width);

    [DllImport("Functions")]
    private static extern void ClearBuffers(int Height, int Width);




    [System.Serializable]
    public struct trans
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }

    void UpdateCamStats()
    {

      
        tanFovHalf = Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
        aspectRatio = Width / (float)Height;

        cameraForward = transform.forward;
        cameraPosition = transform.position;
        cameraUp = transform.up;
        cameraRight = transform.right;
    }

    Vector3 WorldToScreenPoint(Vector3 worldPoint)
    {

        Vector3 toPoint;
        Vector3 localPoint;
        Vector2 projectedPoint;
        Vector3 screenpoint;

        toPoint = worldPoint - cameraPosition;

        localPoint.x = Vector3.Dot(toPoint, cameraRight);
        localPoint.y = Vector3.Dot(toPoint, cameraUp);
        localPoint.z = Vector3.Dot(toPoint, cameraForward);


        // Apply perspective projection

        projectedPoint.x = localPoint.x / (localPoint.z * tanFovHalf * aspectRatio);
        projectedPoint.y = localPoint.y / (localPoint.z * tanFovHalf);
   
        // Debug.Log(localPoint.z);

        // Map to screen space
       // screenpoint = new Vector3();
        screenpoint.x = (projectedPoint.x + 1) * 0.5f * (float)Width;
        screenpoint.y = ((projectedPoint.y + 1) * 0.5f) * (float)Height;
        screenpoint.z = localPoint.z;



        return screenpoint;
    }


   
   // int[] nv;


    //temp vert screen array to be added to all screen vert array
   


    public int triscount;



    Vector3[] processedscreenverts;


    void GetScreenVerts()
    {
        int cur = 0;
        int cur2 = 0;
        for (int v = 0; v < ObjectMeshInstance.Count; v++)
        {

            int ob = ObjectMeshInstance[v];
            for (int i = 0; i < Tris[ob].Length / 3; i++)
            {
                cur = i * 3;
                processedscreenverts[cur2] = WorldToScreenPoint(Verts[ob][Tris[ob][cur]]);
                processedscreenverts[cur2+1] = WorldToScreenPoint(Verts[ob][Tris[ob][cur + 1]]);
                processedscreenverts[cur2+2] = WorldToScreenPoint(Verts[ob][Tris[ob][cur + 2]]);

                cur2 += 3;
            }
        }
    }

    // Modified GetBoxFromTriangle that also computes edge function coefficients.

    void VertexFunction(Vector3 v1, Vector3 v2, Vector3 v3, ref int[] edgeCoeffs, ref int[] nv, ref Color32 vertcol, Vector3 norm, int[] vertsscreen, ref bool onscreen)
    {
        Color tmp;
        //int[] vertsscreen;
        Vector3 scrn;

        Vector3 sv1;
        Vector3 sv2;
        Vector3 sv3;

        float greatestx;
        float greatesty;
        Vector2 corn;
        Vector3 dir = (v1 - cameraPosition).normalized;

        if (Vector3.Dot(dir, -norm) > -0.01f)
        {
            // Compute screen-space vertices.
        sv1 = WorldToScreenPoint(v1);
        sv2 = WorldToScreenPoint(v2);
        sv3 = WorldToScreenPoint(v3);

        //  Vector3 dir = v1 - cameraPosition;



            if (((sv1.x <= 0 || sv1.x > Width || sv1.y < 0 || sv1.y > Height)
            && (sv2.x <= 0 || sv2.x > Width || sv2.y < 0 || sv2.y > Height)
            && (sv3.x <= 0 || sv3.x > Width || sv3.y < 0 || sv3.y > Height)))
            {
                edgeCoeffs = null;
            }
            else
            {

                onscreen = true;

                //sv1 = Vector3.one;
                //sv2 = Vector3.one;
                //sv3 = Vector3.one;


                sv1.x = Mathf.RoundToInt(sv1.x);
                sv1.y = Mathf.RoundToInt(sv1.y);
                sv2.x = Mathf.RoundToInt(sv2.x);
                sv2.y = Mathf.RoundToInt(sv2.y);
                sv3.x = Mathf.RoundToInt(sv3.x);
                sv3.y = Mathf.RoundToInt(sv3.y);


                // Store screen verts in order { x0, y0, x1, y1, x2, y2 }
                
                vertsscreen[0] = (int)sv1.x;
                vertsscreen[1] = (int)sv1.y;
                vertsscreen[2] = (int)sv2.x;
                vertsscreen[3] = (int)sv2.y;
                vertsscreen[4] = (int)sv3.x;
                vertsscreen[5] = (int)sv3.y;

                vertsscreen[6] = (int)(sv1.z * 10000);
                vertsscreen[7] = (int)(sv2.z * 10000);
                vertsscreen[8] = (int)(sv3.z * 10000);

                // vertsscreen[6] = (int)(Vector3.Distance(v1, cameraPosition) * 10000);

                // Compute the bounding box.
                corn = sv1;
                corn.x = Mathf.Min(corn.x, sv2.x);
                corn.x = Mathf.Min(corn.x, sv3.x);
                corn.y = Mathf.Min(corn.y, sv2.y);
                corn.y = Mathf.Min(corn.y, sv3.y);



                greatestx = (int)(Mathf.Max(Mathf.Abs(sv1.x - sv2.x), Mathf.Abs(sv1.x - sv3.x)));
                greatestx = (int)(Mathf.Max(greatestx, Mathf.Abs(sv3.x - sv2.x)));
                greatesty = (int)(Mathf.Max(Mathf.Abs(sv1.y - sv2.y), Mathf.Abs(sv1.y - sv3.y)));
                greatesty = (int)(Mathf.Max(greatesty, Mathf.Abs(sv3.y - sv2.y)));

                if (corn.x < 0) greatestx -= Mathf.Abs(corn.x);
                if (corn.y < 0) greatesty -= Mathf.Abs(corn.y);

               // if (nv == null) nv = new int[4];
                nv[0] = (int)Mathf.Max(0, corn.x);
                nv[1] = (int)Mathf.Max(0, corn.y);
                nv[2] = (int)greatestx;
                nv[3] = (int)greatesty;

                // Precompute edge function coefficients for each edge.
                // Edge 0: from sv1 to sv2



                // Compute edge coefficients as before:
                int A0 = (int)(sv2.y - sv1.y);
                int B0 = -(int)(sv2.x - sv1.x);
                int C0 = -(A0 * (int)sv1.x + B0 * (int)sv1.y);
                int A1 = (int)(sv3.y - sv2.y);
                int B1 = -(int)(sv3.x - sv2.x);
                int C1 = -(A1 * (int)sv2.x + B1 * (int)sv2.y);
                int A2 = (int)(sv1.y - sv3.y);
                int B2 = -(int)(sv1.x - sv3.x);
                int C2 = -(A2 * (int)sv3.x + B2 * (int)sv3.y);

                // Compute the triangle area (absolute value of the cross product).
                int triArea = Mathf.Abs((int)((sv2.x - sv1.x) * (sv3.y - sv1.y) - (sv2.y - sv1.y) * (sv3.x - sv1.x)));

                // Allocate the array with 10 elements (instead of 9) and store the area in the last element.
                edgeCoeffs = new int[10] { A0, B0, C0, A1, B1, C1, A2, B2, C2, triArea };


                tmp = vertcol;
                //tmp.r = vertcol.r * 0.00390625f;
                //tmp.g = vertcol.g * 0.00390625f;
                //tmp.b = vertcol.b * 0.00390625f;



                tmp = ((tmp * sn) * Mathf.Min(1, Mathf.Max(0, Vector3.Dot(sundir, norm)))) + (tmp * Ambient);

                //tmp.r = norm.x;
                //tmp.g = norm.y;
                //tmp.b = norm.z;
               // tmp = Color.black;
                vertcol = tmp;
                //vertcol.r = (byte)(norm.x * 255);
                //vertcol.g = (byte)(norm.y * 255);
                //vertcol.b = (byte)(norm.z * 255);


                Interlocked.Increment(ref TrisOnScreen);
                //TrisOnScreen++;
            }

        }
        else edgeCoeffs = null;

        //  return nv;
    }


    int[] edgeCoeffsArray;

    bool donecols;
    int tcnt = 0;
    void GetAllTrisScreen(object thread)
    {
       // TrisOnScreen = 0;
        
        // Ensure edgeCoeffsArray is sized appropriately
      if(edgeCoeffsArray == null)  edgeCoeffsArray = new int[shapes.Length * 10];

        Vector3 obpos;
        // Color32 cl;
        //Debug.Log("trans form checks " + Tris.Count);


        int[] vertsscreen;

        int cur = 0;
        Color32 cl;
        int[] coeffs;
        int tcount = ObjectMeshInstance.Count;
        int tcount2 = ObjectMeshInstance.Count;

        int strt = 0;
        int am = tcount;

        int[] nv;

        if (Threads > 1)
        {
            am = Mathf.CeilToInt((float)tcount / (float)Threads);

            strt = am * (int)thread;

            am = Mathf.Min(tcount, (am * ((int)thread + 1)));
        }
        //  cur = strt;

        Vector3 dir;

        for (int i = 0; i < strt; i++)
        {
            cur += (ObjectMeshInstance.Count / 3);
        }
        //if ((int)thread == 1) Debug.Log("first thread tris are " + cur);
        //  Debug.Log("thread " + (int)thread + " strt " + strt + " end " + am);
        //   }

        nv = new int[4];
        vertsscreen = new int[12];
        coeffs = new int[10];


       // GetScreenVerts();

        for (int o = strt; o < am; o++)
            {
            int i = ObjectMeshInstance[o];
            obpos = Positions[o].position;
            dir = (obpos) - cameraPosition;
            if (dir.magnitude < bounding[i] || Vector3.Dot((dir).normalized, cameraForward) > 0.2)
            {
                tcount2 = Tris[i].Length / 3;

                for (int t = 0; t < tcount2; t++)
                {
                    bool onscreen = false;

                    int ct = t * 3;
     
                    cl = shapecols[cur];
                  
                
                    VertexFunction(Verts[i][Tris[i][ct]] + obpos,
                                                       Verts[i][Tris[i][ct + 1]] + obpos,
                                                       Verts[i][Tris[i][ct + 2]] + obpos,
                                                       ref coeffs, ref nv, ref cl, Norms[i][Tris[i][ct]], vertsscreen, ref onscreen);
                    if (onscreen)
                    {

                        int tnt = Interlocked.Increment(ref tcnt)-1;

                        rendercols[tnt] = cl;
                        // edgeCoeffsArray[tnt] = coeffs;
                       // int tnt = tcnt;
                        int tnt3 = tnt * 12;
                        int tnt4 = tnt * 10;

                        int tnt2 = tnt * 4;

                        for (int e = 0; e < 10; e++)
                        {
                            edgeCoeffsArray[tnt4 + e] = coeffs[e];
                        }

                        // Add the screen verts calculated in GetBoxFromTriangle.
                        //  screenverts[tnt] = vertsscreen;

                        
                        for (int v = 0; v < 9; v++)
                        {
                            screenverts[tnt3+v] = vertsscreen[v];
                        }

                        shapes[tnt2] = nv[0];
                        shapes[tnt2+1] = nv[1];
                        shapes[tnt2+2] = nv[2];
                        shapes[tnt2+3] = nv[3];

                        // tcnt++;
                    }

                    cur += 1;
                }
            }
            else cur += (Tris[i].Length / 3);


        }
        donecols = true;
    }
 

    // Start is called before the first frame update
    void Start()
    {
        texconvert = gameObject.GetComponent<BufferToTexture>();
       
        //bufff.SetData(cols2);
       // nv = new int[4];
        UpdateCamStats();

        if (CamPlane != null) CamPlane.gameObject.SetActive(true);

        if (Meshes.Count > 0)
        {
            triscount = 0;

            Tris = new List<int[]>();
            Verts = new List<Vector3[]>();
            Norms = new List<Vector3[]>();

            meshes = new List<Mesh>();
            bounding = new List<float>(); 
            ObjectMeshInstance = new List<int>();

            Positions = new trans[Meshes.Count];

            Vector3 bnds = new Vector3();

            for (int i = 0; i < Meshes.Count; i++)
            {
                Positions[i] = new trans();
                Positions[i].position = Meshes[i].transform.position;
                Positions[i].rotation = Meshes[i].transform.rotation;
                Positions[i].scale = Meshes[i].transform.localScale;

                if (!meshes.Contains(Meshes[i].sharedMesh))
                {

                    Tris.Add(Meshes[i].sharedMesh.triangles);
                    Norms.Add(Meshes[i].sharedMesh.normals);
                    Verts.Add(Meshes[i].sharedMesh.vertices);

                    meshes.Add(Meshes[i].sharedMesh);
                    bnds = Meshes[i].GetComponent<Renderer>().bounds.size;
                    bounding.Add(Mathf.Max(bnds.x, bnds.y, bnds.z));
                }

                ObjectMeshInstance.Add(meshes.IndexOf(Meshes[i].sharedMesh));
                triscount += (Tris[ObjectMeshInstance[i]].Length / 3);
            }

            shapes = new int[triscount * 4];
            screenverts = new int[triscount * 12];

            processedscreenverts = new Vector3[triscount * 3];

            for (int s = 0; s < triscount; s++)
            {
               // shapes[s] = new int[4];
               // screenverts[s] = new int[12];
            }

            shapecols = new Color32[triscount];
            rendercols = new Color32[triscount];

            if (!donecols)
            {
                int cntd = 0;
                for (int i = 0; i < ObjectMeshInstance.Count; i++)
                {
                    for (int t = 0; t < Tris[ObjectMeshInstance[i]].Length / 3; t++)
                    {
                        shapecols[cntd] = new Color32((byte)Random.Range(0, 256), (byte)Random.Range(0, 256), (byte)Random.Range(0, 256), 1);
                        cntd += 1;
                    }
                }
            }


            GetAllTrisScreen(0);
            shapecount = triscount;
        }
        //else
        //{
        //    shapes = new Vector4[shapecount];
        //    shapecols = new Color32[shapecount];
        //    for (int i = 0; i < shapecount; i++)
        //    {

        //        if (Random.value > 0.9f) shapes[i] = new Vector4(Random.Range(0, Res), Random.Range(0, Res), Mathf.Max(1, (float)Random.Range(10, Res / 4) * Random.value), Mathf.Max(1, (float)Random.Range(10, Res / 4) * Random.value));
        //        else shapes[i] = new Vector4(Random.Range(0, Res), Random.Range(0, Res), Mathf.Max(1, (float)Random.Range(2, Res / 10) * Random.value), Mathf.Max(1, (float)Random.Range(2, Res / 10) * Random.value));

        //        shapecols[i] = new Color32((byte)Random.Range(0, 256), (byte)Random.Range(0, 256), (byte)Random.Range(0, 256), 1);

        //    }
        //}

        cols2 = new Color32[Height * Width];
        black = new Color32[Height * Width];

        TestRender = new byte[Height][];

        for (int i = 0; i < Height; i++)
        {

           // cols2[i] = new Color32[Width];
           // black[i] = new Color32[Width];
            TestRender[i] = new byte[Width];
        }


        Color32 cur = col1;
        for (int x = 0; x < Height; x++)
        {
            cur = Color32.Lerp(col2, col1, (float)x / (float)Height);
            for (int y = 0; y < Width; y++)
            {
                black[(x * Width) + y] = cur;
            }

        }

            render = new Texture2D(Width, Height);
        rendergpu = new RenderTexture(Width, Height, 1);
        rendergpu.enableRandomWrite = true;
        rendergpu.Create();


        render.Apply();

        depth = new int[Height * Width];

        //for (int i = 0; i < Height; i++)
        //{
        //    depth[i] = new int[Width];
        //}


        //CPLUS STUFF

        if (CPlus)
        {
           // CStart(Height, triscount);

            CopyBuffers(cols2, depth, shapes, rendercols, screenverts, edgeCoeffsArray);

            //COPY TEXTURE ARRAYS
            for (int i = 0; i < Height; i++)
            {
               // CopyScreenTextures(cols2[i], depth[i], i);
            }

            //COPY VERT STUFF
            for (int i = 0; i < triscount; i++)
            {
              //  CopyTrisStuff(i, shapes[i], rendercols, screenverts[i], edgeCoeffsArray[i]);
            }

        }



    }


   
    // Update is called once per frame
    void Update()
    {
      

        if (Render)
        {

            if(DirectionalLight != null)
            {
                sn = DirectionalLight.color * DirectionalLight.intensity;
                sundir = -DirectionalLight.transform.forward;
            }

            UpdateCamStats();


            if(!DisableBuffClear && !CPlus)    ClearThirty();

            if (CPlus) ClearBuffers(Height, Width);

            if (DoVertex)
            {
                tcnt = 0;
                TrisOnScreen = 0;

                if (Threads > 1)
                {
                    if(threads == null)
                    {
                        threads = new Thread[Threads];
                    }

                    for (int i = 0; i < Threads; i++)
                    {
                        threads[i] = new Thread(GetAllTrisScreen);
                        threads[i].Priority = System.Threading.ThreadPriority.Highest;
                        threads[i].Start(i);
                       // GetAllTrisScreen(i);
                    }

                    int tdone = 0;

                    for (int i = 0; i < threads.Length; i++)
                    {
                        threads[i].Join(); // Main thread waits here
                    }

                    //for (int i = 0; i < threads.Length; i++)
                    //{
                    //    while (threads[i].IsAlive)
                    //    {

                    //    }

                    //}



                }
               else GetAllTrisScreen(0);

            }

            if (CPlus)
            {
              

                if (Raster)  CRasterTris(TrisOnScreen, Height, Width);
            }
            else
            {
                if (Raster && !Unsafe) RasterTris();
                if (Raster && Unsafe) RasterTris_Fixed_Optimized();
            }
           // RenderShapesUnsafe2D();

         

            if (!DisableTextureWrite)
            {
                if (TestBuffSet)
                {
                    texconvert.SetRenderTexture32(rendergpu, cols2);
                   // bufff.SetData(cols2);
                }
                else
                {
                    render.SetPixels32(cols2);
                    render.Apply();
                }
            }


            if (preview != null)
            {
                if(TestBuffSet) preview.mainTexture = rendergpu;
                else  preview.mainTexture = render;

            }
        }
        
    }


    Color32 row;
    //void RenderShapes()
    //{
    //    PixelWrites = 0;
    //    int strtx = 0;
    //    int strty = 0;
    //    Color32 curcol;

    //    //DRAW 2D BOUNDING RECTANGLE FOR TRIS, LATER MUST CHECK IF INSIDE TRIANGLE BEFORE DRAWING USING screen verts array

    //    for (int i = 0; i < shapes.Length; i++)
    //    {
    //        strty = (int)shapes[i][1];
    //        curcol = shapecols[i];

    //        for (int x = 0; x < shapes[i][2]; x++)
    //        {

    //            strtx = (int)shapes[i][0] + x;

    //            if (strtx >= Width) break;

    //            for (int y = 0; y < shapes[i][3]; y++)
    //            {
    //                if (strty + y >= Height) break;
    //                cols2[strty + y][strtx] = curcol;
    //                PixelWrites += 1;
    //            }
    //        }
    //    }
    //}

    int[] edgeCoeffs;
    void RasterTris()
    {
        PixelWrites = 0;
        Color32 curcol;
        int[] edgeCoeffs;


        float u;
        float v;
        float w;

        // Also precompute the per-pixel increments (derivatives) for the barycentrics.
        float du; // because w1 increments by A1 per x
        float dv; // because w2 increments by A2 per x
        float dw; // because w0 increments by A0 per x

        byte dpwrite = 0;

        float dp1;
        float dp2;
        float dp3;

        for (int i = 0; i < TrisOnScreen; i++)
        {
            // Get bounding box parameters
            int curi = i * 4;

            int minX = shapes[curi];
            int minY = shapes[curi+1];
            int boxWidth = shapes[curi+2];
            int boxHeight = shapes[curi+3];
            curcol = rendercols[i];

            // Retrieve the screen-space vertices.
            int vcnt = i * 12;

            int v0x = screenverts[vcnt + 0];
            int v0y = screenverts[vcnt + 1];
            int v1x = screenverts[vcnt + 2];
            int v1y = screenverts[vcnt + 3];
            int v2x = screenverts[vcnt + 4];
            int v2y = screenverts[vcnt + 5];

            // "mydepth" is stored in screenverts[i][6] (for depth testing)
            float mydepth = screenverts[vcnt + 6];
            dp1 = mydepth;
            dp2 = screenverts[vcnt + 7];
            dp3 = screenverts[vcnt + 8];


            // Retrieve the precomputed edge coefficients.
            // Our edgeCoeffs array now has 10 elements:
            // [0]=A0, [1]=B0, [2]=C0, [3]=A1, [4]=B1, [5]=C1, [6]=A2, [7]=B2, [8]=C2, [9]=triArea


            //edgeCoeffs = edgeCoeffsArray[i];
            //int A0 = edgeCoeffs[0], B0 = edgeCoeffs[1], C0 = edgeCoeffs[2];
            //int A1 = edgeCoeffs[3], B1 = edgeCoeffs[4], C1 = edgeCoeffs[5];
            //int A2 = edgeCoeffs[6], B2 = edgeCoeffs[7], C2 = edgeCoeffs[8];

            int tnt4 = i * 10;
            int A0 = edgeCoeffsArray[tnt4], B0 = edgeCoeffsArray[tnt4+1], C0 = edgeCoeffsArray[tnt4+2];
            int A1 = edgeCoeffsArray[tnt4+3], B1 = edgeCoeffsArray[tnt4+4], C1 = edgeCoeffsArray[tnt4+5];
            int A2 = edgeCoeffsArray[tnt4+6], B2 = edgeCoeffsArray[tnt4+7], C2 = edgeCoeffsArray[tnt4+8];

            int triArea = edgeCoeffsArray[tnt4+9]; // total triangle area (should be > 0)
            float invArea = triArea != 0 ? 1.0f / triArea : 0.0f;



            // For each scanline within the bounding box:
            for (int y = minY; y < minY + boxHeight; y++)
            {
                if (y < 0 || y >= Height)
                {
                    // Still need to update the edge functions if off-screen.
                    continue;
                }

                // Compute the starting edge function values at x = minX.
                int w0 = A0 * minX + B0 * y + C0;
                int w1 = A1 * minX + B1 * y + C1;
                int w2 = A2 * minX + B2 * y + C2;


                // Compute the initial barycentric weights for this scanline.
                // Using our convention: u = w1/area, v = w2/area, w = w0/area.
                u = w1 * invArea;
                v = w2 * invArea;
                w = w0 * invArea;

                // Also precompute the per-pixel increments (derivatives) for the barycentrics.
                du = A1 * invArea; // because w1 increments by A1 per x
                dv = A2 * invArea; // because w2 increments by A2 per x
                dw = A0 * invArea; // because w0 increments by A0 per x

                // Loop over the scanline (horizontal direction)
                for (int x = minX; x < minX + boxWidth; x++)
                {
                    if (x < 0 || x >= Width)
                    {
                        w0 += A0; w1 += A1; w2 += A2;
                        u += du; v += dv; w += dw;
                        continue;
                    }

                    mydepth = (dp1 * u) + (dp2 * v) + (dp3 * w);
                    int cur = (y * Width) + x;
                    // If the pixel is inside the triangle (edge tests) and passes depth test...
                    if (w0 >= 0 && w1 >= 0 && w2 >= 0 && mydepth < depth[cur])
                    {
                        // At this point, u, v, w are the barycentrics for the pixel (x,y)
                        // You can use them to interpolate vertex attributes (like UVs, normals, etc.)
                        // For example:
                        // interpolatedValue = u * v0Value + v * v1Value + w * v2Value;

                        // For our demo, we just update depth and set the pixel color.
                        depth[cur] = (int)mydepth;
                        cols2[cur] = curcol;
                        //dpwrite = (byte)((mydepth * 0.00001f) * 255);
                        //cols2[y][x].r = dpwrite;
                        //cols2[y][x].g = dpwrite;
                        //cols2[y][x].b = dpwrite;

                        PixelWrites++;
                    }

                    // Increment the edge functions for the next pixel.
                    w0 += A0; w1 += A1; w2 += A2;
                    // And update the barycentrics incrementally.
                    u += du; v += dv; w += dw;
                }
            }
        }
    }


    unsafe void RasterTris_Fixed_Optimized()
    {
        PixelWrites = 0;
        for (int i = 0; i < TrisOnScreen; i++)
        {
            // Get triangle bounding box parameters.
            int curi = i * 4;

            int minX = shapes[curi];
            int minY = shapes[curi + 1];
            int boxWidth = shapes[curi + 2];
            int boxHeight = shapes[curi + 3];

            Color32 curcol = rendercols[i];

            // Retrieve per-vertex depth values (stored in screenverts[i][6..8]).
            int vcnt = i * 12;
            float dp1 = screenverts[vcnt + 6];
            float dp2 = screenverts[vcnt + 7];
            float dp3 = screenverts[vcnt + 8];

            // Retrieve the precomputed edge coefficients.
            //int[] edgeCoeffs = edgeCoeffsArray[i];
            //int A0 = edgeCoeffs[0], B0 = edgeCoeffs[1], C0 = edgeCoeffs[2];
            //int A1 = edgeCoeffs[3], B1 = edgeCoeffs[4], C1 = edgeCoeffs[5];
            //int A2 = edgeCoeffs[6], B2 = edgeCoeffs[7], C2 = edgeCoeffs[8];
            //int triArea = edgeCoeffs[9];  // Precomputed triangle area.

            int tnt4 = i * 10;
            int A0 = edgeCoeffsArray[tnt4], B0 = edgeCoeffsArray[tnt4 + 1], C0 = edgeCoeffsArray[tnt4 + 2];
            int A1 = edgeCoeffsArray[tnt4 + 3], B1 = edgeCoeffsArray[tnt4 + 4], C1 = edgeCoeffsArray[tnt4 + 5];
            int A2 = edgeCoeffsArray[tnt4 + 6], B2 = edgeCoeffsArray[tnt4 + 7], C2 = edgeCoeffsArray[tnt4 + 8];

            int triArea = edgeCoeffsArray[tnt4 + 9]; // total triangle area (should be > 0)


            if (triArea == 0) continue;
            float invArea = 1.0f / triArea;

            // Clamp the bounding box to the screen to avoid per-pixel bounds checks.
            int startX = Mathf.Max(minX, 0);
            int endX = Mathf.Min(minX + boxWidth, Width);
            int startY = Mathf.Max(minY, 0);
            int endY = Mathf.Min(minY + boxHeight, Height);

            // Loop over each scanline in the clamped vertical range.
            for (int y = startY; y < endY; y++)
            {
                // Compute starting edge function values at x = minX.
                int w0 = A0 * minX + B0 * y + C0;
                int w1 = A1 * minX + B1 * y + C1;
                int w2 = A2 * minX + B2 * y + C2;

                // Compute initial barycentrics at x = minX.
                float u = w1 * invArea;
                float v = w2 * invArea;
                float w = w0 * invArea;

                // Precompute per-pixel increments for barycentrics.
                float du = A1 * invArea;
                float dv = A2 * invArea;
                float dw = A0 * invArea;

                // If the bounding box doesn't start at the left edge, advance the values.
                int dx = startX - minX;
                if (dx > 0)
                {
                    w0 += A0 * dx;
                    w1 += A1 * dx;
                    w2 += A2 * dx;
                    u += du * dx;
                    v += dv * dx;
                    w += dw * dx;
                }

                // Fix the current row for both the color buffer and the depth buffer.
                //fixed (Color32* pColRow = cols2[y])
                //fixed (int* pDepthRow = depth[y])
                //{
                    // Loop over the horizontal span in the clamped range.
                    for (int x = startX; x < endX; x++)
                    {
                        // Compute interpolated depth via barycentrics.
                        float pixelDepth = (dp1 * u) + (dp2 * v) + (dp3 * w);
                    int cur = (y * Width) + x;
                    // If the pixel is inside the triangle (all edge tests pass) and depth test passes...
                    if (w0 >= 0 && w1 >= 0 && w2 >= 0 && pixelDepth < depth[cur])
                        {
                            depth[cur] = (int)pixelDepth;
                        cols2[cur] = curcol;
                            PixelWrites++;
                        }
                        // Increment the edge functions and barycentrics for the next pixel.
                        w0 += A0;
                        w1 += A1;
                        w2 += A2;
                        u += du;
                        v += dv;
                        w += dw;
                    }
                }
            }
       // }
    }




    int[] depthhigh;

    void ClearThirty()
    {
        if(depthhigh == null)
        {
            depthhigh = new int[Height * Width];

            for (int i = 0; i < depthhigh.Length; i++)
            {
              
                    depthhigh[i] = 100000000;
                
            }

        }

        int pcount = 0;
       
        System.Array.Copy(depthhigh, depth, Width * Height);
        System.Array.Copy(black, cols2, Width * Height);

        // Debug.Log("pixel clear count " + pcount);
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(0, 0, 100, 32), "FPS: "+ Mathf.RoundToInt(1/Time.smoothDeltaTime));
        GUI.Label(new Rect(0, 32, 100, 32), "Tris: " + triscount);
    }


}
