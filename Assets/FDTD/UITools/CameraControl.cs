using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    public float moveSpeed;
    public float rotSpeed;
    public float scrollSpeed;
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
            camBase.transform.Translate(Input.GetAxis("Mouse X") * -moveSpeed * camBase.transform.right*Time.deltaTime, Space.World);
            camBase.transform.Translate(Input.GetAxis("Mouse Y") * -moveSpeed * transform.up * Time.deltaTime, Space.World);
        }
        //Rotation
        if (Input.GetKey(KeyCode.Mouse1))
        {
            camBase.transform.Rotate(Vector3.up, Input.GetAxis("Mouse X") * rotSpeed * Time.deltaTime);
            transform.Rotate(Vector3.right, -Input.GetAxis("Mouse Y") * rotSpeed * Time.deltaTime);
        }
        //Scroll
        camBase.transform.Translate(transform.forward * scrollSpeed * Input.GetAxis("Mouse ScrollWheel")*Time.deltaTime, Space.World);
    }
}
