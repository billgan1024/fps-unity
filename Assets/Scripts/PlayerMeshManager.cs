using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMeshManager : MonoBehaviour
{
    private GameObject cam;
    private Player player;
    private Vector3 offset;
    // Start is called before the first frame update
    void Start()
    {
        cam = GameObject.Find("Main Camera");
        offset = cam.GetComponent<CameraManager>().offset;
        player = GameObject.Find("Player").GetComponent<Player>();
    }

    // track camera position and player rotation
    // this is called before draw so the mesh will be moved there fast enough
    void LateUpdate() {
        transform.position = cam.transform.position - offset;
        transform.rotation = Quaternion.Euler(0, player.yRotation, 0);
    }
}
