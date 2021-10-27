using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    public float moveSpeed;
    public float rotSpeed;
    public Transform camBase;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        //Translation
        if (Input.GetKey(KeyCode.Mouse2))
        {
            camBase.transform.Translate(Input.GetAxis("Mouse X") * -moveSpeed * Vector3.right);
            camBase.transform.Translate(Input.GetAxis("Mouse Y") * -moveSpeed * Vector3.up);
        }
        //Rotation
        if (Input.GetKey(KeyCode.Mouse1))
        {
            camBase.transform.Rotate(Vector3.up, Input.GetAxis("Mouse X") * rotSpeed);
            transform.Rotate(Vector3.right, -Input.GetAxis("Mouse Y") * rotSpeed);
        }
        //Scroll
        camBase.transform.Translate(Vector3.forward * moveSpeed * Input.GetAxis("Mouse ScrollWheel"));
    }
}
