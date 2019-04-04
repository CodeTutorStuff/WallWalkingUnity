using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Control : MonoBehaviour {

    public SpecialPlayerController spc;

    public Transform camera;

    public float rotationSpeed;
    float rotationY; //used to keep track of player yaw

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(KeyCode.Escape)) //should be handled elsewhere
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        if (Input.GetMouseButton(0)) //should be handled elsewhere
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        if (Input.GetKeyDown(KeyCode.C)) //should be handled elsewhere
        {
            spc.ChangeWallWalk();
        }

        spc.Move(transform.right * Input.GetAxis("Horizontal") + transform.forward * Input.GetAxis("Vertical"));

        spc.RotateYaw(rotationSpeed * Input.GetAxisRaw("Mouse X") * Time.deltaTime);

        rotationY += Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;
        rotationY = Mathf.Clamp(rotationY, -30f, 30f);
        camera.transform.localRotation = Quaternion.Euler(-rotationY, 0, 0);
    }
}
