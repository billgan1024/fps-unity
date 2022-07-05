
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using System.Linq;

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
    public float airAcc, kbAcc, wallAcc;
    public float jumpSpeed, wallBoostSpeed;
    // edit: this bug of updating drag force variables has been fixed with a new project apparently
    // now we just have varying horizontal drag and a constant vertical drag
    // horizontal drag force applies to the player with respect to the current ground normal
    public float groundHorizontalDrag, airHorizontalDrag, wallHorizontalDrag, kbHorizontalDrag;
    public float airGravity, wallGravity, kbGravity;
    public float verticalDrag;
    public float slopeLimit;
    [Header("Input")]
    public Vector2 inputDir;
    [Header("State")]
    public float horizontalDrag;
    public float wallRotation;
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
    public float xRotation, yRotation;
    public bool doubleJump;
    [Header("Weapon")]
    public GameObject realBulletPrefab, displayBulletPrefab;
    public float bulletSpeed;
    public bool isFiring;
    public float firingTimer, firingDelay;
    private bool jumpedThisFrame, releasedJumpThisFrame;
    private Rigidbody rb;
    private CameraManager cam;
    [Header("Misc")]
    public float maxHeight;
    public float cameraWallRotation;
    private AudioSource audioSource;
    private Transform bulletOrigin;
    private LayerMask aimableMask;
    public float displayBulletSpawnThreshold;
    [Header("Audio")]
    public AudioClip rifleFireSound;
    public AudioClip jumpSound;
    [Header("Debug")]
    public bool tapToFire = false;
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
        audioSource = GetComponent<AudioSource>();
        aimableMask = LayerMask.GetMask(new string[]{"Ground", "Player"});

        GameObject spawnpoints = GameObject.Find("Spawn Points");
        transform.position = spawnpoints.transform.GetChild(Random.Range(0, spawnpoints.transform.childCount)).position;

        cam = GameObject.Find("Main Camera").GetComponent<CameraManager>();
        cam.StartFollow(this);

        // grab a reference to the bullet origin object so we can spawn bullets there
        bulletOrigin = cam.transform.Find("Rifle").Find("Bullet Origin");

        TransitionAir();
    }   

    void Update() {
    }
    void FixedUpdate() {
        // fix random stuff happening when we touch a wall 
        // edit: we just freeze the rotation to avoid accumulating angular velocity, then move the player 
        // rb.angularVelocity = Vector3.zero;
        maxHeight = Mathf.Max(maxHeight, transform.position.y);
        firingTimer = Mathf.Max(0, firingTimer - Time.fixedDeltaTime);

        // get ground and wall normal each step 
        groundNormal = groundNormals.Count > 0 ? groundNormals[0].normal : Vector3.up;
        wallNormal = wallNormals.Count > 0 ? wallNormals[0].normal : Vector3.zero;
         
        if(isFiring && firingTimer <= 0) {
            // bullets originate from cam.transform.position
            audioSource.PlayOneShot(rifleFireSound, 0.4f);

            GameObject realBullet = Instantiate(realBulletPrefab, cam.transform.position, Quaternion.identity);

            // aimable mask: only ground and players at the moment
            RaycastHit[] hitData = Physics.RaycastAll(cam.transform.position, cam.transform.forward, Mathf.Infinity, aimableMask);
            
            // vector3 that represents where the player is looking (basically we implement a raycast that stops at ground and players)

            realBullet.GetComponent<Bullet>().velocity = cam.transform.forward*bulletSpeed;

            // find the closest object (point) the player is looking at and aim the display bullet there, otherwise use the forward vector
            // find the speed to make the display bullet and the normal bullet reach the look point at the same time
            // we set the rotation of the displaybullet for non-spherical bullets
            if(hitData.Length > 0) {

                RaycastHit firstHit = hitData.OrderBy(hit => hit.distance).First();
                Vector3 lookPoint = firstHit.point;

                Vector3 displayBulletDir = (lookPoint-bulletOrigin.position).normalized;
                
                // only fire if the look point is in front of us
                if(firstHit.distance > displayBulletSpawnThreshold) {
                    GameObject displayBullet = Instantiate(displayBulletPrefab, bulletOrigin.position, Quaternion.identity);

                    displayBullet.GetComponent<Rigidbody>().velocity = (lookPoint-bulletOrigin.position).normalized*bulletSpeed*
                        ((lookPoint-bulletOrigin.position).magnitude)/firstHit.distance;
                    realBullet.transform.rotation = displayBullet.transform.rotation = cam.transform.rotation;
                    realBullet.GetComponent<Bullet>().displayBullet = displayBullet;
                }
                    
            } else {
                GameObject displayBullet = Instantiate(displayBulletPrefab, bulletOrigin.position, Quaternion.identity);
                displayBullet.GetComponent<Rigidbody>().velocity = cam.transform.forward*bulletSpeed;
                
                realBullet.transform.rotation = displayBullet.transform.rotation = cam.transform.rotation;

                realBullet.GetComponent<Bullet>().displayBullet = displayBullet;
            }

            firingTimer = firingDelay;
        }
        
        // apply the rotation to the movement directions
        Quaternion moveRotation = Quaternion.Euler(0, yRotation, 0);
        Vector3 moveDir = Vector3.ProjectOnPlane(moveRotation*transform.right*inputDir.x + moveRotation*transform.forward*inputDir.y, groundNormal).normalized;
        Debug.DrawRay(transform.position, moveDir*10);

        switch(state) {
            case PlayerState.Ground:
                cam.zRotationTarget = 0;
                rb.AddForce(moveDir*groundAcc, ForceMode.Acceleration);
                if(jumpedThisFrame && rb.velocity.y <= jumpSpeed) {
                    Debug.Log("jumped");
                    rb.AddForce(Vector3.up*(jumpSpeed-rb.velocity.y), ForceMode.VelocityChange);
                    // audioSource.PlayOneShot(jumpSound);
                    TransitionAir();
                }
            break;

            case PlayerState.Air:
                cam.zRotationTarget = 0;
                rb.AddForce(moveDir*airAcc, ForceMode.Acceleration);
                if(wallNormal != Vector3.zero) {
                    // todo: update z rotation here

                    if(jumpedThisFrame) {
                        Debug.Log("started wallride");
                        TransitionWall();
                    }
                } else {
                    if(doubleJump && jumpedThisFrame && rb.velocity.y <= jumpSpeed) {
                        doubleJump = false;
                        Debug.Log("doublejumped");
                        audioSource.PlayOneShot(jumpSound);
                        rb.AddForce(Vector3.up*(jumpSpeed-rb.velocity.y), ForceMode.VelocityChange);
                    }
                }
            break;

            // basically implement lucio wallride
            // while you hold jump, you're essentially stuck to the wall (just project this vector to the wall)
            case PlayerState.Wall:
                // todo: update z rotation here (it's dependent on the signed angle between the wall normal and the look direction projected onto 
                // the x-z plane)
                // use signed angle since we care about the orientation of the two vectors with respect to the x-z plane
                cam.zRotationTarget = cameraWallRotation*Mathf.Sin(Mathf.Deg2Rad*Vector3.SignedAngle(wallNormal, cam.transform.forward, Vector3.up));
                rb.AddForce(Vector3.ProjectOnPlane(moveDir, wallNormal)*wallAcc, ForceMode.Acceleration);
                if(releasedJumpThisFrame) {
                    // perform a walljump
                    Debug.Log("jumping off wall because jump was released");
                    rb.AddForce(wallNormal*20, ForceMode.VelocityChange);
                    if(rb.velocity.y <= jumpSpeed) rb.AddForce(Vector3.up*(jumpSpeed-rb.velocity.y), ForceMode.VelocityChange);
                    TransitionKnockback(); 
                }
            break;
            
            case PlayerState.Knockback:
                cam.zRotationTarget = 0;
                rb.AddForce(moveDir*kbAcc, ForceMode.Acceleration);
                if(wallNormal != Vector3.zero) {
                    if(jumpedThisFrame) {
                        // start a wall ride
                        Debug.Log("started wallride");
                        // add some velocity
                        TransitionWall();
                    }
                } else {
                    if(doubleJump && jumpedThisFrame && rb.velocity.y <= jumpSpeed) {
                        // double jump normally
                        doubleJump = false;
                        Debug.Log("doublejumped");
                        audioSource.PlayOneShot(jumpSound);
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
            // transform.rotation = Quaternion.Euler(0, yRotation, 0);
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
    
    // immediately update normals and velocity 
    void TransitionWall() {
        horizontalDrag = wallHorizontalDrag;
        gravity = wallGravity;


        // rb.velocity = Vector3.ProjectOnPlane(rb.velocity, wallNormal);
        if(rb.velocity.y <= wallBoostSpeed) rb.AddForce(Vector3.up*(wallBoostSpeed-rb.velocity.y), ForceMode.VelocityChange);

        state = PlayerState.Wall;
    }

    void OnCollisionEnter(Collision collision) {
        // Debug.Log("collision entered");
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
        // Debug.Log("collision exited");
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
    // can try updating the normals here to keep them up to date sooner
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
        if(angle <= slopeLimit) {
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
        } else if(prevWallLength > 0 && curWallLength == 0) {
            // stopped touching a wall
            switch(state) {
                case PlayerState.Wall:
                    // jump off the wall automatically if you leave the wall
                    // use the wallNormal before we reset it
                    Debug.Log("jumping off wall because you left it");
                    rb.AddForce(wallNormal*20, ForceMode.VelocityChange);
                    if(rb.velocity.y <= jumpSpeed) rb.AddForce(Vector3.up*(jumpSpeed-rb.velocity.y), ForceMode.VelocityChange);
                    TransitionKnockback(); 
                break;
            }
        }
    }
}