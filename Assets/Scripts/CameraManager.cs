using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// smoothly track a player in local coordinates and also manages rotation
// also makes sure the player mesh is attached to the camera as the camera moves
public class CameraManager : MonoBehaviour
{
    Vector3 previousPosition, currentPosition;
    public Vector3 offset;
    private Player player;
    private GameObject playerMesh;
    // Start is called before the first frame update
    void Awake() {
        playerMesh = GameObject.Find("Player Mesh");
    }

    // Update is called once per frame
    void Update() {
        // note: we lerp in local coordinates in the same amount of time, so it is accurate across objects
        // at various scales
        transform.localPosition = Vector3.Lerp(previousPosition, currentPosition, (Time.time-Time.fixedTime)*60);
        // then the camera is just offset by some vector (in global space)
        // and we attach the player mesh to the camera at the original position
        transform.position += offset;
        playerMesh.transform.position = transform.position - offset;
    }
    // sync rotation
    void LateUpdate() {
        transform.rotation = Quaternion.Euler(player.xRotation, player.yRotation, 0);
        playerMesh.transform.rotation = Quaternion.Euler(0, player.yRotation, 0);
    }

    public void StartFollow(Player player) {
        transform.localPosition = previousPosition = currentPosition = player.transform.localPosition;
        this.player = player;
        transform.position += offset;
        playerMesh.transform.position = transform.position - offset;
    }

    void FixedUpdate() {
        previousPosition = currentPosition;
        currentPosition = player.transform.position;
    }
}