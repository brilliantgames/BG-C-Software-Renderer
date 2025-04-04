using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BGPointLight : MonoBehaviour
{
    public Color color = Color.white;
    public float Intensity = 1;
    public float Range = 15;
    public bool Dynamic = true;
    [HideInInspector]
    public Vector3 position;

    // Start is called before the first frame update
    void Start()
    {
        position = transform.position;

        BgCamera bc = Camera.main.GetComponent<BgCamera>();

        bc.pointLights.Add(this);
        
    }

    private void OnDestroy()
    {
        BgCamera bc = Camera.main.GetComponent<BgCamera>();

        bc.pointLights.Remove(this);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = color * 1.5f;
        Gizmos.DrawSphere(transform.position, 0.5f);
    }

}
