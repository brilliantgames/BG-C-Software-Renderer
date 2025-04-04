using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.InteropServices;

public class Testcplus : MonoBehaviour
{
    public bool DoCSharp;
    public bool TestDistance;
    public bool TestPixelShad;
    public int Size = 1000;
    public bool ShowFloats;
    Vector3[] positions;
    float[] positionsf;
    float[] arry;
    public float[] debug;
    public Light sun;
    UnityEngine.Color[] diffuse;
    UnityEngine.Color[] norm;
    UnityEngine.Color[] output;
    UnityEngine.Color32[] outputthirty;
    public Texture2D render;

    UnityEngine.Color[] outputtst;

    RenderTextures rt;
    public bool Applytex = true;
    // Import the C++ function
    [DllImport("Functions")]
    private static extern void SayHello();

    [DllImport("Functions")]
    private static extern int AddNumbers(int a, int b);

    [DllImport("Functions")]
    private static extern void ModifyArray(float[] arr, int count);

    [DllImport("Functions")]
    private static extern float Vector3Distance(Vector3 a, Vector3 b);

    [DllImport("Functions")]
    private static extern void GetDistances(float[] distanceout, int size, float[] positions, float[] compar);

    [DllImport("Functions")]
    private static extern void AssignTemps(Vector3[] inpu);

    [DllImport("Functions")]
    private static extern void DoTemps(int size, float[] outs, Vector3 pos);

    [DllImport("Functions")]
    private static extern void SetArrays(UnityEngine.Color[] sdiffuse, UnityEngine.Color[] snorm, UnityEngine.Color[] soutput, UnityEngine.Color32[] outthiry);

     [DllImport("Functions")]
    private static extern void Frag(int size, Vector3 sdir, UnityEngine.Color sncl);

    [DllImport("Functions")]
    private static extern void ConvertTo32(int size);

    void Start()
    {
      

        rt = gameObject.GetComponent<RenderTextures>();

        outputtst = new Color[rt.textureWidth * rt.textureHeight];

        render = new Texture2D(rt.textureWidth, rt.textureHeight, TextureFormat.ARGB32, false);
        output = new Color[rt.textureWidth * rt.textureHeight];

        outputthirty = new Color32[rt.textureWidth * rt.textureHeight];

        diffuse = rt.diffuseTexture.GetPixels(0);
        norm = rt.normalTexture.GetPixels(0);


        // Call the C++ function
        SayHello();
        int result = AddNumbers(5, 10);
        Debug.Log("Result from C++: " + result);

        arry = new float[Size];

        positions = new Vector3[Size];
        positionsf = new float[Size * 3];
       // UnityEngine.Random rn = new System.Random();

        for (int i = 0; i < Size; i++)
        {
            // arry[i] = UnityEngine.Random.value * 10000;
            arry[i] = 1;

             positions[i] = new Vector3(UnityEngine.Random.value * 10000, UnityEngine.Random.value * 10000, UnityEngine.Random.value * 10000);
           
            positionsf[i * 3 + 0] = positions[i].x;
            positionsf[i * 3 + 1] = positions[i].y;
            positionsf[i * 3 + 2] = positions[i].z;

        }

        SetArrays(diffuse, norm, output, outputthirty);

        AssignTemps(positions);
    }

    float dx;
    float dy;
    float dz;

    void GetDistancess(float[] distanceout, int size, float[] positions, float[] compar)
    {
        float dst = 0;
        float[] tmp;

        for (int i = 0; i < size; i++)
        {
            //tmp = positions[i];
            dx = positions[i * 3 + 0] - compar[0];
            dy = positions[i * 3 + 1] - compar[1];
            dz = compar[2] - positions[i * 3 + 2];

            distanceout[i] = Mathf.Sqrt((dx * dx + dy * dy + dz * dz));

            //distanceout[i] += 25;
        }
    }


    void Frag()
    {
        int lng = diffuse.Length;
        Vector3 nrm = new Vector3();

        Color sncl = sun.color * sun.intensity;

        Vector3 sdir = -sun.transform.forward;

        if (DoCSharp)
        {
            Color tcol;
            for (int i = 0; i < lng; i++)
            {
                nrm.x = norm[i].r;
                nrm.y = norm[i].g;
                nrm.z = norm[i].b;
            
                output[i] = (Color)diffuse[i] * Mathf.Max(0, Mathf.Min(1, Vector3.Dot(nrm, sdir))) * sncl;
            }
        }
        else
        {
            Frag(output.Length, sdir, sncl);
            ConvertTo32(output.Length);
        }

        if (Applytex)
        {
            render.SetPixels32(outputthirty);
            render.Apply();
        }

    }


    private void Update()
    {

        if (ShowFloats)
        {
            debug = arry;
        }

        if (TestPixelShad)
        {
            Frag();
        }
        else
        {
            if (TestDistance)
            {

                Vector3 cam = transform.position;
                float[] camf = new float[3];
                camf[0] = cam.x;
                camf[1] = cam.y;
                camf[2] = cam.z;

                float dist = 0;

                //AssignTemps(positions);
                DoTemps(Size, arry, cam);


                //if (DoCSharp)
                //{
                //    //for (int i = 0; i < Size; i++)
                //    //{
                //    //    arry[i] = Vector3.Distance(cam, positions[i]);
                //    //}
                //    GetDistancess(arry, Size, positionsf, camf);
                //}
                //else
                //{

                //    GetDistances(arry, Size, positionsf, camf);
                //}

            }
            else
            {
                if (DoCSharp)
                {
                    for (int i = 0; i < Size; i++)
                    {
                        arry[i] *= 2;  // Example modification: Multiply each element by 2

                        arry[i] -= 3;
                        arry[i] += 2;

                        arry[i] /= 2;

                    }
                }
                else
                {
                    ModifyArray(arry, arry.Length);
                }

                Debug.Log(arry[0]);
                Debug.Log(arry[arry.Length - 1]);
            }
        }
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(0, 0, 100, 32), "FPS: "+(Mathf.RoundToInt(1/Time.smoothDeltaTime)));
    }

}