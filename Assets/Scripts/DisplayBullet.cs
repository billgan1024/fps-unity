using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// todo: make the bullet (and its trail) approach finalSize
public class DisplayBullet : MonoBehaviour
{
    public float finalSize, sizeChangeSpeed, finalLightSize;
    private TrailRenderer trail;
    private Light glowLight;
    // Start is called before the first frame update
    void Start()
    {
        trail = GetComponent<TrailRenderer>();
        glowLight = GetComponentInChildren<Light>();
    }

    // Update is called once per frame
    void Update()
    {
        transform.localScale = Vector3.one*Mathf.MoveTowards(transform.localScale.x, finalSize, sizeChangeSpeed*Time.deltaTime);
        trail.startWidth = transform.localScale.x;
        glowLight.intensity = transform.localScale.x*finalLightSize/finalSize; 
    }
}
