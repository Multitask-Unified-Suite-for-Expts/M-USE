using System;
using System.Collections.Generic;
using UnityEngine;
using USE_Data;

public class JoystickTracker : InputTracker
{
    public float movementSpeed = 100f;
    public float rotationSpeed = 180f;
    public Vector2 JoystickInput { get; private set; }
    //public Transform playerCamTransform;
    private Vector3 moveDirection;
    public bool isActive;
    public GameObject Player;

    public void assignCamera(Camera camera)
    {
        //Debug.LogError(GameObject.Find("JoystickWWW_Camera")); this is null
        //playerCamTransform = GameObject.Find("JoystickWWW_Camera").transform;
        //playerCamTransform = camera.transform;
        //if (playerCamTransform == null)
        //{
        //    playerCamTransform = GameObject.FindGameObjectWithTag("JoystickWWW_Camera").transform;
        //}
        //if (playerCamTransform == null)
        //{ 
        //    Debug.LogError("JoystickWWW_Camera transform not found. Ensure that your camera is named 'JoystickWWW_Camera' or tagged as 'JoystickWWW_Camera'.");
        //}
    }
    
    public override void CustomUpdate()
    {
        // // Read the horizontal and vertical axis values from the InputBroker
        // float horizontalInput = InputBroker.GetAxis("Horizontal");
        // float verticalInput = InputBroker.GetAxis("Vertical");
        //
        // // Store the joystick input as a Vector2
        // JoystickInput = new Vector2(horizontalInput, verticalInput);

        //if (playerCamTransform != null)
        {
            if (isActive)
            {
                float horizontalInput = InputBroker.GetAxis("Horizontal");
                float verticalInput = InputBroker.GetAxis("Vertical");
        
                Vector3 cameraForward = Player.transform.forward;
                Vector3 cameraRight = Player.transform.right;
                cameraForward.y = 0f;
                cameraRight.y = 0f;
                cameraForward.Normalize();
                cameraRight.Normalize();
                Debug.Log("vertical input: " + verticalInput);
                Debug.Log("horiz input: " + horizontalInput);
                Debug.Log("cameraforward: " + cameraForward);

                if (Math.Abs(verticalInput) > 0f)
                {
                    moveDirection = cameraForward * verticalInput;
                }
                else
                {
                    moveDirection = Vector3.zero;
                }

                Quaternion cameraRotation = Quaternion.Euler(0f, horizontalInput * rotationSpeed * Time.deltaTime, 0f);
                Player.transform.rotation *= cameraRotation;
            }
        }
    }

    private void FixedUpdate()
    {
        if (isActive)
        { 
            Debug.Log(Time.frameCount + "/ position: " + transform.position + "/ move direction: " + moveDirection);
            Player.GetComponent<Rigidbody>().MovePosition(transform.position + moveDirection * movementSpeed * Time.fixedDeltaTime);
        }
    }

    private void LateUpdate()
    {
        if (isActive)
        {
            // (playerCamTransform != null)
            //{
                //Debug.Log("position being coupled: " + Player.transform.position.ToString());
                //playerCamTransform.position = Player.transform.position;
            //}
        }
    }

    public override void AddFieldsToFrameData(DataController frameData)
    {
        frameData.AddDatum("JoystickInput", () => JoystickInput.ToString());
    }

    public override void FindCurrentTarget()
    {
    }
}