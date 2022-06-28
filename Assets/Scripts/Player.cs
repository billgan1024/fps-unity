
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
    [System.Serializable]
    public struct Contact {
        public GameObject obj;
        public Vector3 normal;
        public Contact(GameObject obj, Vector3 normal) {
            this.obj = obj;
            this.normal = normal;
        }
    }
    [Header("Stats")]
    // basically ground acc is high and ground drag is high, kb acc is low and drag is low so that we can apply velocity changes
    // to knock the player in some direction
    public float groundAcc;
    public float airAcc;
    public float kbAcc;
    public float wallAcc; 
    public float jumpSpeed;
    public float wallBoostSpeed;
    public float normalVerticalDrag;
    // edit: this bug of updating drag force variables has been fixed with a new project apparently
    // now we just have varying horizontal drag and a constant vertical drag
    // horizontal drag force applies to the player with respect to the current ground normal
    public float groundHorizontalDrag, airHorizontalDrag;
    // wall drag and vertical drag are all with a ground normal of Vector3.up
    public float wallHorizontalDrag, kbHorizontalDrag;
    public float airGravity;
    public float wallGravity;
    public float kbGravity;

    public float slopeLimit;
    [Header("Input")]
    public Vector2 inputDir;
    [Header("State")]
    public float horizontalDrag;
    public float verticalDrag;
    public float gravity;
    // groundNormal: value for general movement along a slope
    public Vector3 groundNormal;
    public Vector3 wallNormal;

    // maintain an accurate list of ground normals and wall normals
    // oncollisionenter: add all normals associated with this ground object to the map of ground objects to lists of normals
    // oncollisionstay: update the two maps
    // oncollisionexit: remove all normals
    // remember that reference types are really just stored as pointers
    // then, we just grab a particular ground and wall normal to use in fixedupdate, and fire events when the player started touching ground
    // (list became nonempty) or stopped touching ground (list became empty)
    // now state changes will only update player movement stuff, and not the ground/wall normal

    // c# tuples moment (pog)
    // make this show up in the unity editor (basically allow unity to convert it into a readable format)
    public List<Contact> groundNormals, wallNormals;
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
    public float maxHeight;
    public enum PlayerState {
        Ground,
        Air,
        // wall: is the player wallriding?
        Wall,
        // knockback: is the player being knocked back (while mid-air)?
        Knockback
    }
    
    public PlayerState state;

    // Start is called before the first frame update
    void Start() {
        Cursor.lockState = CursorLockMode.Locked;
        rb = GetComponent<Rigidbody>();
        // edit: don't do this! remember that initialization returns a reference, so these two lists would be pointing to the same list
        // groundNormals = wallNormals = new List<Contact>();

        // find a spawnpoint and teleport to a particular location, then start the follow
        GameObject spawnpoints = GameObject.Find("Spawn Points");
        transform.position = spawnpoints.transform.GetChild(Random.Range(0, spawnpoints.transform.childCount)).position;

        cam = GameObject.Find("Main Camera").GetComponent<CameraManager>();
        cam.StartFollow(this);

        // initialize variables
        groundNormal = Vector3.up;
        verticalDrag = normalVerticalDrag;
        TransitionAir();
    }   

    void FixedUpdate() {
        maxHeight = Mathf.Max(maxHeight, transform.position.y);
        firingTimer = Mathf.Max(0, firingTimer - Time.fixedDeltaTime);

        // get ground and wall normal 
        // since we push and remove things from the list, the selection really doesn't matter here
        groundNormal = groundNormals.Count > 0 ? groundNormals[0].normal : Vector3.up;
        wallNormal = wallNormals.Count > 0 ? wallNormals[0].normal : Vector3.zero;
         
        if(isFiring && firingTimer <= 0) {
            GameObject bullet = Instantiate(bulletPrefab, cam.transform.position, Quaternion.identity);
            bullet.GetComponent<Rigidbody>().velocity = cam.transform.forward*bulletSpeed;
            firingTimer = firingDelay;
        }
        
        Vector3 moveDir = Vector3.ProjectOnPlane(transform.right*inputDir.x + transform.forward*inputDir.y, groundNormal).normalized;
        Debug.DrawRay(transform.position, moveDir*10);

        switch(state) {
            case PlayerState.Ground:
            rb.AddForce(moveDir*groundAcc, ForceMode.Acceleration);
            if(jumpedThisFrame && rb.velocity.y <= jumpSpeed) {
                //update drag+gravity immediately (or don't)
                Debug.Log("jumped");
                TransitionAir();
                rb.AddForce(Vector3.up*(jumpSpeed-rb.velocity.y), ForceMode.VelocityChange);
            }
            break;

            case PlayerState.Air:
            rb.AddForce(moveDir*airAcc, ForceMode.Acceleration);

            if(wallNormal != Vector3.zero) {
                if(jumpedThisFrame) {
                    // start a wall ride
                    Debug.Log("started wallride");
                    TransitionWall();
                    // add some velocity
                    if(rb.velocity.y <= wallBoostSpeed) rb.AddForce(Vector3.up*(wallBoostSpeed-rb.velocity.y), ForceMode.VelocityChange);
                }
            } else {
                if(doubleJump && jumpedThisFrame && rb.velocity.y <= jumpSpeed) {
                    // double jump normally
                    doubleJump = false;
                    Debug.Log("doublejumped");
                    rb.AddForce(Vector3.up*(jumpSpeed-rb.velocity.y), ForceMode.VelocityChange);
                }
            }
            break;

            // basically implement lucio wallride
            // while you hold jump, you're essentially stuck to the wall (just project this vector to the wall)
            case PlayerState.Wall:
            rb.AddForce(Vector3.ProjectOnPlane(moveDir, wallNormal)*wallAcc, ForceMode.Acceleration);
            if(releasedJumpThisFrame) {
                // perform a walljump
                Debug.Log("jumping off wall");
                TransitionKnockback(); 
                rb.AddForce(wallNormal*20, ForceMode.VelocityChange);
                if(rb.velocity.y <= jumpSpeed) rb.AddForce(Vector3.up*(jumpSpeed-rb.velocity.y), ForceMode.VelocityChange);
            }
            break;
            
            case PlayerState.Knockback:
            rb.AddForce(moveDir*kbAcc, ForceMode.Acceleration);
            if(wallNormal != Vector3.zero) {
                if(jumpedThisFrame) {
                    // start a wall ride
                    Debug.Log("started wallride");
                    TransitionWall();
                    // add some velocity
                    if(rb.velocity.y <= wallBoostSpeed) rb.AddForce(Vector3.up*(wallBoostSpeed-rb.velocity.y), ForceMode.VelocityChange);
                }
            } else {
                if(doubleJump && jumpedThisFrame && rb.velocity.y <= jumpSpeed) {
                    // double jump normally
                    doubleJump = false;
                    Debug.Log("doublejumped");
                    rb.AddForce(Vector3.up*(jumpSpeed-rb.velocity.y), ForceMode.VelocityChange);
                }
            }
            break;
        }

        Vector3 dh = -horizontalDrag*Vector3.ProjectOnPlane(rb.velocity, groundNormal);
        Vector3 dv = -verticalDrag*Vector3.Dot(rb.velocity, groundNormal)*groundNormal;
    
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
    void TransitionGround() {
        horizontalDrag = groundHorizontalDrag;
        gravity = 0;
        doubleJump = true;
        // FollowPlayer fp = cam.GetComponent<FollowPlayer>();
        // immediately convert the lerp endpoints to relative endpoints
        // first, convert to global coordinates using the previous parent
        // fp.ConvertToGlobal();
        // transform.SetParent(groundTransform);
        // cam.transform.SetParent(groundTransform);
        // fp.ConvertToLocal(groundTransform);

        state = PlayerState.Ground;
    }
    
    void TransitionWall() {
        horizontalDrag = wallHorizontalDrag;
        gravity = wallGravity;

        state = PlayerState.Wall;
    }

    void OnCollisionEnter(Collision collision) {
        switch(collision.gameObject.tag) {
            case "Ground":
            DrawContactNormals(collision.contacts);
            int numGrounds = groundNormals.Count, numWalls = wallNormals.Count;
            foreach (ContactPoint contact in collision.contacts) AddContact(contact);
            ChangeStateFromCollisions(numGrounds, numWalls, groundNormals.Count, wallNormals.Count);
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
            int numGrounds = groundNormals.Count, numWalls = wallNormals.Count;
            // just clear the contacts and re-add them
            ClearContacts(collision.gameObject);
            foreach (ContactPoint contact in collision.contacts) AddContact(contact);
            ChangeStateFromCollisions(numGrounds, numWalls, groundNormals.Count, wallNormals.Count);
            break;
        }
    }

    // stopped touching a particular gameobject (just remvoe it from the list)
    void OnCollisionExit(Collision collision) {
        switch(collision.gameObject.tag) {
            case "Ground":
            int numGrounds = groundNormals.Count, numWalls = wallNormals.Count;
            ClearContacts(collision.gameObject);
            ChangeStateFromCollisions(numGrounds, numWalls, groundNormals.Count, wallNormals.Count);
            break;
        }
    }
    void DrawContactNormals(ContactPoint[] contacts) {
        foreach (ContactPoint contact in contacts) 
            Debug.DrawRay(contact.point, contact.normal*5, Color.green);
    }
    void TransitionAir() {
        horizontalDrag = airHorizontalDrag;
        gravity = airGravity;
        // FollowPlayer fp = cam.GetComponent<FollowPlayer>();
        // // immediately convert lerp endpoints to global coordinates
        // fp.ConvertToGlobal();
        // transform.SetParent(null);
        // cam.transform.SetParent(null);
        // immediately convert the lerp endpoints back to normal coordinates
        state = PlayerState.Air;
    }

    void TransitionKnockback() {
        horizontalDrag = kbHorizontalDrag;
        gravity = airGravity;
        state = PlayerState.Knockback;
    }

    // clears all contacts from a particular gameobject in the list of grounds and walls
    void ClearContacts(GameObject g) {
        for(int i = 0; i < groundNormals.Count; i++) {
            if(groundNormals[i].obj == g) {
                groundNormals.RemoveAt(i); i--;
            }
        }
        for(int i = 0; i < wallNormals.Count; i++) {
            if(wallNormals[i].obj == g) {
                wallNormals.RemoveAt(i); i--;
            }
        }
    }

    void AddContact(ContactPoint contact) {
        float angle = Vector3.Angle(contact.normal, Vector3.up);
        if(angle < 90) {
            // Debug.Log("ground detected");
            groundNormals.Add(new Contact(contact.otherCollider.gameObject, contact.normal));
        } else if(angle == 90) {
            // Debug.Log("slope detected");
            wallNormals.Add(new Contact(contact.otherCollider.gameObject, contact.normal));
        }
    }
    void ChangeStateFromCollisions(int prevGroundLength, int prevWallLength, int curGroundLength, int curWallLength) {
        if(prevGroundLength == 0 && curGroundLength > 0) {
            // started touching ground
            switch(state) {
                case PlayerState.Air:
                    Debug.Log("air->ground");
                    TransitionGround();
                break;
                case PlayerState.Wall:
                    Debug.Log("wall->ground");
                    TransitionGround();
                break;
                case PlayerState.Knockback:
                    Debug.Log("knockback->ground");
                    TransitionGround();
                break;
            }
        } else if(prevGroundLength > 0 && curGroundLength == 0) {
            // stopped touching ground
            switch(state) {
                case PlayerState.Ground:
                    Debug.Log("ground->air");
                    TransitionAir();
                break;
            }
        }

        if(prevWallLength == 0 && curWallLength > 0) {
            // started touching a wall
        } else if(prevWallLength > 0 && curWallLength == 0) {
            // stopped touching a wall
            switch(state) {
                case PlayerState.Wall:
                    // jump off the wall automatically if you leave the wall
                    Debug.Log("jumping off wall");
                    TransitionKnockback(); 
                    rb.AddForce(wallNormal*20, ForceMode.VelocityChange);
                    if(rb.velocity.y <= jumpSpeed) rb.AddForce(Vector3.up*(jumpSpeed-rb.velocity.y), ForceMode.VelocityChange);
                    // TransitionAir();
                break;
            }
        }
    }
}