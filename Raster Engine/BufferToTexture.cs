using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BufferToTexture : MonoBehaviour
{
    public ComputeShader Compute;
    static ComputeShader compute;
     static ComputeBuffer buff;
    static ComputeBuffer buffe;
    Color[] tst;
    private void Start()
    {
        buff = null;
        buffe = null;
       // ComputeBuffer colorBuffer = new ComputeBuffer(count, 4, ComputeBufferType.Structured);

        compute = Compute;

        //tst = new Color[1920 * 1080];

        //for (int i = 0; i < tst.Length; i++)
        //{
        //    tst[i] = Color.red;
        //}


    }


    private void OnDestroy()
    {
        if (buff != null)
        {
            buff.Dispose();
            buffe.Dispose();
        }
    }


    public void SetRenderTexture32(RenderTexture texture, Color32[] colors)
    {
        int width = texture.width;
        int height = texture.height;


        if (buff == null)
        {
           // buff = new ComputeBuffer(width * height, 16);
            buff = new ComputeBuffer(width * height, 4);

            buffe = new ComputeBuffer(1, 16);
        }
        compute.SetBool("UseFloat", false);
        buff.SetData(colors);

        compute.SetInt("width", width);
        compute.SetInt("height", height);

        compute.SetTexture(0, "Result", texture);
        compute.SetBuffer(0, "buff", buff);
        compute.SetBuffer(0, "bufff", buffe);

        //Debug.Log("Dispatching. width: " + (width / 8) + " height: " + (height / 8));
        compute.Dispatch(0, width / 8, height / 8, 1);

    }


    public void SetRenderTexture(RenderTexture texture, Color[] colors)
    {
        int width = texture.width;
        int height = texture.height;


        if (buff == null)
        {
            // buff = new ComputeBuffer(width * height, 16);
            buff = new ComputeBuffer(width * height, 16);

            buffe = new ComputeBuffer(1, 4);
        }

        compute.SetBool("UseFloat", true);
        buff.SetData(colors);

        compute.SetInt("width", width);
        compute.SetInt("height", height);


        compute.SetTexture(0, "Result", texture);
        compute.SetBuffer(0, "bufff", buff);
        compute.SetBuffer(0, "buff", buffe);


        //Debug.Log("Dispatching. width: " + (width / 8) + " height: " + (height / 8));
        compute.Dispatch(0, width / 8, height / 8, 1);

    }

}
