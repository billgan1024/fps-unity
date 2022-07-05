using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// the bullet script that the real bullet uses
public class Bullet : MonoBehaviour
{
    public Vector3 velocity;
    public GameObject displayBullet;
    public float projectileSize;
    public GameObject bulletImpactPrefab;

    private LayerMask collideableMask;

    // Start is called before the first frame update
    void Start()
    {
        collideableMask = LayerMask.GetMask(new string[]{"Ground", "Player"});
    }

    void OnTriggerEnter(Collider other) {
        // if(other.tag == "Ground") {
        //     Debug.Log("destroyed bullet");
        //     Destroy(gameObject);
        // }
    }

    void FixedUpdate() {
        // collide with the nearest object if there is one
        RaycastHit[] hitData = Physics.RaycastAll(transform.position, velocity, velocity.magnitude*Time.fixedDeltaTime, collideableMask);
        if(hitData.Length > 0) {
            transform.position = hitData.OrderBy(hit => hit.distance).First().point;
            Destroy(gameObject);

            if(displayBullet != null) Destroy(displayBullet);

            // Instantiate(bulletImpactPrefab, transform.position, Quaternion.Euler(-transform.rotation.eulerAngles));
        }
        transform.position += velocity*Time.fixedDeltaTime;
    }
}
