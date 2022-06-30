using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    public float amplitude, timeMultiplier;
    private float yStart;
    // Start is called before the first frame update
    void Start()
    {
        yStart = transform.position.y; 
    }

    // Update is called once per frame
    void Update()
    {
        // transform.position = new Vector3(transform.position.x, yStart+amplitude*Mathf.Sin(Time.time*timeMultiplier), transform.position.z);
    }
}
