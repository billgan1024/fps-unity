using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        foreach(Transform child in transform) {
            child.tag = "Ground";
        }   
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
