using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotate : MonoBehaviour
{
    public float RotateSpeed = 10;
    // Start is called before the first frame update
  

    // Update is called once per frame
    void Update()
    {

        transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y + RotateSpeed * Time.deltaTime, transform.eulerAngles.z);

    }
}
