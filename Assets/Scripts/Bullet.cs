using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public Vector3 speed;
    private Rigidbody rb;
    private bool collided;
    // apparently querying layers is buggy so just use this
    private int groundLayer;
    // Start is called before the first frame update
    void Start()
    {
        // rb = GetComponent<Rigidbody>();
        groundLayer = LayerMask.NameToLayer("Ground");
    }
    // Update is called once per frame
    void Update()
    {     
        // this handles fast collision 
        // the layermask is an actual bitmask so u just use a bit shift to represent the ground layer
        // also get size according to transform automatically
        if(Physics.OverlapSphere(transform.position, transform.lossyScale.x, 1 << groundLayer).Length > 0) Destroy(gameObject);
    }

    void FixedUpdate() {
        // get all hits of the bullet using a spherecast
        // if there is at least one hit, find the closest one, move the bullet to that position, and then 
        // destroy the bullet 
        // RaycastHit[] hitInfo = Physics.SphereCastAll(transform.position, transform.)

    }

    void OnTriggerEnter(Collider other) {
        // if(other.tag == "Ground") {
        //     Debug.Log("destroyed bullet");
        //     Destroy(gameObject);
        // }
    }
}
