using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RendererCpu : MonoBehaviour
{
    public bool DisableUnityRender = true;
    // Start is called before the first frame update
    void Awake()
    {
        Camera.main.GetComponent<TestCpuRender>().Meshes.Add(gameObject.GetComponent<MeshFilter>());
        gameObject.GetComponent<Renderer>().enabled = false;
    }


}
