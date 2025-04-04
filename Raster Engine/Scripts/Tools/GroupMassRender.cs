using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class GroupMassRender : MonoBehaviour
{
    public BGRenderer ObjectToInstance;
    public bool UseLod;
    public bool ThreadLodProcessing;
    public BGRenderer ObjectToInstanceLod2;
    public float Lod2Distance = 10;
    public float MaxRenderdist = 1000;
    public int Count;
    public float ObjectSpacing = 1.5f;
    public bool RaycastToGround;
    public float RandomOffset;
    public float HeightRndOffset;
    public bool RandomRotations;
    public bool RandomTints = true;
    BgCamera.BgTrans[] transforms;
    BgCamera.BgTrans[] transformsfinal;
    bool HaveCreated;
    int count;
    public bool RenderWithUnity;
    Matrix4x4[][] unityrendertest;


    public static void Shuffle<T>(T[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1); // UnityEngine.Random
            (array[i], array[j]) = (array[j], array[i]);
        }
    }

    // Start is called before the first frame update
    void CreateObjects()
    {
        Vector3 start = transform.position;


        int width = (int)Mathf.Sqrt((float)Count);

        start.x -= (width * ObjectSpacing) / 2;
        Vector3 current = start;

        transforms = new BgCamera.BgTrans[Count];

        Vector4 scl = Vector3.one;
        scl.w = 2;
        Quaternion rot = transform.rotation;
        Vector4 vrot = new Vector4(rot.x, rot.y, rot.z, rot.w);

        int meshindex = ObjectToInstance.MeshIndex;

        int texindex = ObjectToInstance.TexId;

        Vector3 curoff;

        RaycastHit hit = new RaycastHit();

        for (int x = 0; x < width; x++)
        {

            
            for (int z = 0; z < width; z++)
            {
                int cur = (x * width) + z;
                transforms[cur] = new BgCamera.BgTrans();

                curoff = current;

                curoff.x += (Random.value - 0.5f) * RandomOffset;
                curoff.z += (Random.value - 0.5f) * RandomOffset;

                curoff.y += (Random.value - 0.5f) * HeightRndOffset;

                if (RandomRotations)
                {
                    rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                    vrot = new Vector4(rot.x, rot.y, rot.z, rot.w);
                }

                if (RaycastToGround)
                {
                    curoff.y += 200;

                    if(Physics.Raycast(curoff, Vector3.down * 1000, out hit))
                    {
                        curoff.y = hit.point.y;
                    }
                }

                //ASSIGN TRANSFORM STUFF
                transforms[cur].position = curoff;
               transforms[cur].rotation = vrot;
                transforms[cur].scale = scl;
              

                //SIMPLY ASSIGN MESH INDEX FROM THE BG RENDERER YOU WANT TO INSTANCE, TEXTURES WILL ALSO BE SHARED
                //ANY OBJECT CAN RENDER A DIFFERENT MESH AND TEXTURE.. NO RULES!
                transforms[cur].meshindex = meshindex;
              

                //RANDOMIZE COLOR.. BECAUSE WE CAN

                if (RandomTints) transforms[cur].tint = new Color32((byte)Random.Range(0, 255), (byte)Random.Range(0, 255), (byte)Random.Range(0, 255), 255);
                else transforms[cur].tint = ObjectToInstance.Tint;


                current.z += ObjectSpacing;
            }
            current.x += ObjectSpacing;
            current.z = start.z;
        }

        Shuffle(transforms);

        if (!UseLod)
        {
            transformsfinal = transforms;
            count = transforms.Length;

        }
        else
        {
            transformsfinal = new BgCamera.BgTrans[Count];

            for (int i = 0; i < transforms.Length; i++)
            {
                transformsfinal[i] = transforms[i];
            }
        }


        if (RenderWithUnity)
        {
            int am = Mathf.CeilToInt((float)Count / 1023);
            unityrendertest = new Matrix4x4[am][];

      
            int currow = 0;
            int curint = 0;
            int tot = 0;
            for (int i = 0; i < transforms.Length; i++)
            {
                if (unityrendertest[currow] == null)
                {
                    int mx = Mathf.Min(transforms.Length - tot, 1023);
                    Debug.Log("length is " + mx);
                    unityrendertest[currow] = new Matrix4x4[mx];
                }

              

                rot = new Quaternion(transforms[i].rotation.x, transforms[i].rotation.y, transforms[i].rotation.z, transforms[i].rotation.w);
                unityrendertest[currow][curint].SetTRS(transforms[i].position, Quaternion.identity, transforms[i].scale);

                 curint++;
                if (curint > 1022)
                {
                    curint = 0;
                    currow += 1;
                }
                tot += 1;
            }


        


        }

    }

    private void OnDestroy()
    {
        if (thread != null)
        {
            running = false;
            thread.Abort();
        }
    }

    Thread thread;
    bool running;
    void ThreadLod()
    {
        while (running)
        {
           DoLod();
        }

    }

    Vector3 campos;
        void DoLod()
    {
        float dist;

        int counterr =0 ;

        int lod1ind = ObjectToInstance.MeshIndex;
        int lod2ind = ObjectToInstanceLod2.MeshIndex;


        for (int i = 0; i < transforms.Length; i++)
        {
            dist = Vector3.Distance(campos, transforms[i].position);
            if (dist > MaxRenderdist)
            {
                transforms[i].meshindex = -1;
            }
            else
            {
               
                if (dist > Lod2Distance)
                {
                    transforms[i].meshindex = lod2ind;
                }
                else
                {
                    transforms[i].meshindex = lod1ind;
                }

              //  transformsfinal[counterr] = transforms[i];

                counterr += 1;
            }

          

        }

        
        count = transforms.Length;
    }


    // Update is called once per frame
    void Update()
    {
        //WE CREATE IN THE FIRST UPDATE FRAME JUST IN CASE THE MESH INDEX HAS NOT YET BEEN DETERMINED BY ENGINE
        if (!HaveCreated)
        {
            HaveCreated = true;
            CreateObjects();
        }


        if (RenderWithUnity)
        {
            Mesh msh = ObjectToInstance.GetComponent<MeshFilter>().sharedMesh;

            Material mat = ObjectToInstance.GetComponent<Renderer>().sharedMaterial;

            mat.enableInstancing = true;
            for (int i = 0; i < unityrendertest.Length; i++)
            {
                if (unityrendertest[i] != null) Graphics.DrawMeshInstanced(msh, 0, mat, unityrendertest[i]);
            }

        }
        else
        {
            if (UseLod)
            {

                campos = Camera.main.transform.position;

                if (ThreadLodProcessing)
                {

                    if (!running)
                    {
                        running = true;
                        thread = new Thread(ThreadLod);
                        thread.Start();
                    }


                }
                else DoLod();

            }



            // Debug.Log("rendering " + count);

            //CALL GROUP RENDER ONCE PER FRAME.  WATCH OUT, THIS HAS A COST, SO SUM ALL YOUR MESHES INTO ONE CALL IF YOU CAN!
            BgCamera.RenderObjectGroup(transforms, count);
        }
        
    }
}
