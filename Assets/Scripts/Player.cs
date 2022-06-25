
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

// state and input manager for the player
// has variables and a current state; this script passes a copy of itself 
// so that the current state can update its variables and switch to another state
public class Player : MonoBehaviour 
{
    [Header("Stats")]
    public float groundAcc;
    public float airAcc;
    public float kbAcc;
    public float wallAcc; 
    public float jumpSpeed;
    public float groundHorizontalDrag;
    public float groundVerticalDrag;
    public float airHorizontalDrag;
    public float airVerticalDrag;
    public float wallDrag;
    public float kbDrag;
    public float airGravity;
    public float wallGravity;
    public float slopeLimit;
    [Header("Input")]
    public Vector2 inputDir;
    [Header("State")]
    public float horizontalDrag;
    public float verticalDrag;
    public float gravity;
    // slopenormal: value for general movement along a slope
    public Vector3 slopeNormal;
    public Vector3 wallNormal;
    public List<GameObject> grounds, walls; 
    public float xRotation;
    public float yRotation;
    public bool doubleJump;
    [Header("Weapon")]
    public GameObject bulletPrefab;
    public float bulletSpeed;
    public bool isFiring;
    public float firingTimer;
    public float firingDelay;
    private bool jumpedThisFrame; 
    private bool releasedJumpThisFrame;
    private Rigidbody rb;
    private CameraManager cam;
    public enum PlayerState {
        Ground,
        Air,
        Wall,
        Knockback
    }
    
    public PlayerState state;

    // Start is called before the first frame update
    void Start() {
        Cursor.lockState = CursorLockMode.Locked;
        rb = GetComponent<Rigidbody>();

        // find a spawnpoint and teleport to a particular location, then start the follow
        GameObject spawnpoints = GameObject.Find("Spawn Points");
        transform.position = spawnpoints.transform.GetChild(Random.Range(0, spawnpoints.transform.childCount)).position;

        cam = GameObject.Find("Main Camera").GetComponent<CameraManager>();
        cam.StartFollow(this);

        // initialize variables
        slopeNormal = Vector3.up;
        TransitionAir();
    }   

    void FixedUpdate() {
        firingTimer = Mathf.Max(0, firingTimer - Time.fixedDeltaTime);

        if(isFiring && firingTimer <= 0) {
            GameObject bullet = Instantiate(bulletPrefab, cam.transform.position, Quaternion.identity);
            bullet.GetComponent<Rigidbody>().velocity = cam.transform.forward*bulletSpeed;
            firingTimer = firingDelay;
        }
        // Debug.Log("local position from player object " + transform.localPosition);
        // transform.localPosition += Vector3.right*Time.fixedDeltaTime;
        // Debug.Log(rb.velocity.y);
        // get the y-rotation (horizontal rotation) such that 
        // we can rotate the movement vector in that direction 
        
        Vector3 moveDir = Vector3.ProjectOnPlane(transform.right*inputDir.x + transform.forward*inputDir.y, slopeNormal).normalized;
        Debug.DrawRay(transform.position, moveDir*10);

        switch(state) {
            case PlayerState.Ground:
            rb.AddForce(moveDir*groundAcc, ForceMode.Acceleration);
            if(jumpedThisFrame && rb.velocity.y <= jumpSpeed) {
                //update drag+gravity immediately (or don't)

                Debug.Log("jumped");
                slopeNormal = Vector3.up;
                TransitionAir();
                rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
                rb.AddForce(Vector3.up*(jumpSpeed-rb.velocity.y), ForceMode.VelocityChange);
            }
            break;

            case PlayerState.Air:
            // drag moment for air
            // rb.drag = 0;
            rb.AddForce(moveDir*airAcc, ForceMode.Acceleration);

            if(wallNormal != Vector3.zero) {
                if(jumpedThisFrame) {
                    // start a wall ride
                    Debug.Log("started wallride");
                    TransitionWall();
                }
            } else {
                if(doubleJump && jumpedThisFrame && rb.velocity.y <= jumpSpeed) {
                    // double jump normally
                    doubleJump = false;
                    // rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
                    // rb.velocity = new Vector3(rb.velocity.x, jumpSpeed, rb.velocity.z);
                    Debug.Log("doublejumped");
                rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
                    rb.AddForce(Vector3.up*(jumpSpeed-rb.velocity.y), ForceMode.VelocityChange);
                }
            }
            break;

            // basically implement lucio wallride
            case PlayerState.Wall:
            rb.AddForce(moveDir*wallAcc, ForceMode.Acceleration);
            if(releasedJumpThisFrame) {
                // perform a walljump

            }
            break;
        }

        Vector3 dh = -horizontalDrag*Vector3.ProjectOnPlane(rb.velocity, slopeNormal);
        Vector3 dv = -verticalDrag*Vector3.Dot(rb.velocity, slopeNormal)*slopeNormal;
    
        Vector3 drag = dh+dv;
        Debug.DrawRay(transform.position, drag, Color.blue);
        rb.AddForce(drag, ForceMode.Acceleration);

        // rb.velocity -= Vector3.up*gravity*Time.fixedDeltaTime;
        rb.AddForce(Vector3.down*gravity, ForceMode.Acceleration);

        jumpedThisFrame = false;
        releasedJumpThisFrame = false;  
    }

    void OnMove(InputValue value) {
        inputDir = value.Get<Vector2>();
    }

    void OnFire(InputValue value) {
        isFiring = value.Get<float>() == 1;
    }

    void OnLook(InputValue value) {
        // Debug.Log("moved pointer");
        if(Cursor.lockState == CursorLockMode.Locked) {
            Vector2 delta = value.Get<Vector2>();
            yRotation += delta.x*GameManager.mouseSens;
            xRotation = Mathf.Clamp(xRotation - delta.y*GameManager.mouseSens, -90, 90);
            transform.rotation = Quaternion.Euler(0, yRotation, 0);
        }
    }

    void OnJump(InputValue value) {
        float val = value.Get<float>();
        if(val == 1) jumpedThisFrame = true;
        else releasedJumpThisFrame = true;
    }
    // void OnPause(InputValue value) {
    //     // pressed esc
    //     Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? 
    //         CursorLockMode.None : CursorLockMode.Locked;
    // }

    // go to the ground state with this ground transform
    void TransitionGround(Transform groundTransform) {
        horizontalDrag = groundHorizontalDrag;
        verticalDrag = groundVerticalDrag;
        gravity = 0;
        doubleJump = true;
        // FollowPlayer fp = cam.GetComponent<FollowPlayer>();
        // // immediately convert the lerp endpoints to relative endpoints
        // // first, convert to global coordinates using the previous parent
        // fp.ConvertToGlobal();
        // transform.SetParent(groundTransform);
        // cam.transform.SetParent(groundTransform);
        // fp.ConvertToLocal(groundTransform);

        state = PlayerState.Ground;
    }
    
    void TransitionWall() {
        horizontalDrag = verticalDrag = wallDrag;
        gravity = wallGravity;

        state = PlayerState.Wall;
    }
    // apparently this is called the moment you begin touching something
    // edit: we should keep track of the slope normal all the time

    // also keep track of wall normals
    // the invariant is that grounds always represent the list of objects for which 
    // we consider to ground the player, and the list of walls always represent the list of 
    // objects for which you can attach to if you are currently midair, and slopeNormal/wallNormal
    // represents the current normal that u use to handle ground movement / wall movement
    // when the grounds/walls list change, you can use that to invoke a state change
    void OnCollisionEnter(Collision collision) {
        switch(collision.gameObject.tag) {
            case "Ground":
            DrawContactNormals(collision.contacts);
            foreach (ContactPoint contact in collision.contacts) {
                float angle = Vector3.Angle(contact.normal, Vector3.up);
                if(angle < 90) {
                    if(!grounds.Contains(collision.gameObject)) grounds.Add(collision.gameObject);
                    if(grounds.Count > 0) {
                        slopeNormal = contact.normal;
                    }
                } else if(angle == 90) {
                    if(!walls.Contains(collision.gameObject)) walls.Add(collision.gameObject);
                    wallNormal = contact.normal;
                }
            }
            break;
        }
    }

    // called for every frame and for every collider the rigidbody is still touching
    // grounds is always a list of ground objects we are touching at an acceptable angle
    void OnCollisionStay(Collision collision) {
        switch(collision.gameObject.tag) {
            // only for objects tagged as ground
            case "Ground": 
            DrawContactNormals(collision.contacts);
                bool isGround = false, isWall = false;
                foreach (ContactPoint contact in collision.contacts) {
                    float angle = Vector3.Angle(contact.normal, Vector3.up);
                    if(angle < 90)  {
                        isGround = true;
                        slopeNormal = contact.normal; // just update randomly
                    } else if(angle == 90) {
                        isWall = true; 
                        wallNormal = contact.normal; // just update randomly (doesn't matter most of the time)
                    }
                }
                // check if this collision was at a good angle for a ground check, because the normal could have
                // turned into a normal that doesn't have a valid angle
                if(isGround) {
                    if(!grounds.Contains(collision.gameObject)) {
                        grounds.Add(collision.gameObject); 
                    }
                } else {
                    grounds.Remove(collision.gameObject);
                    if(grounds.Count == 0) {
                        slopeNormal = Vector3.up;
                    }
                }
                // same with a wall check
                if(isWall) {
                    if(!walls.Contains(collision.gameObject)) {
                        walls.Add(collision.gameObject); 
                    }
                } else {
                    walls.Remove(collision.gameObject);
                    if(walls.Count == 0) wallNormal = Vector3.zero;
                }
            break;
        }
    }

    // stopped touching a particular gameobject (just remvoe it from the list)
    void OnCollisionExit(Collision collision) {
        switch(collision.gameObject.tag) {
            case "Ground":
                grounds.Remove(collision.gameObject);
                walls.Remove(collision.gameObject);
                if(grounds.Count == 0) {
                    slopeNormal = Vector3.up;
                }
                if(walls.Count == 0) wallNormal = Vector3.zero;
            break;
        }
    }
    void DrawContactNormals(ContactPoint[] contacts) {
        foreach (ContactPoint contact in contacts) 
            Debug.DrawRay(contact.point, contact.normal*5, Color.green);
    }
    void TransitionAir() {
        horizontalDrag = airHorizontalDrag;
        verticalDrag = airVerticalDrag;
        gravity = airGravity;
        // FollowPlayer fp = cam.GetComponent<FollowPlayer>();
        // // immediately convert lerp endpoints to global coordinates
        // fp.ConvertToGlobal();
        // transform.SetParent(null);
        // cam.transform.SetParent(null);
        // immediately convert the lerp endpoints back to normal coordinates
        state = PlayerState.Air;
    }
}