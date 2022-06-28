using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        int groundLayer = LayerMask.NameToLayer("Ground");
        foreach(Transform child in transform) {
            child.tag = "Ground";
            child.gameObject.layer = groundLayer;
        }   
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
