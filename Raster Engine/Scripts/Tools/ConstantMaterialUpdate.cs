using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstantMaterialUpdate : MonoBehaviour
{
    BGRenderer rend;
    // Start is called before the first frame update
    void Start()
    {
        rend = gameObject.GetComponent<BGRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        rend.UpdateMaterialProperties();
    }
}
