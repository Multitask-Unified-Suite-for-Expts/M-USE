using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float movementSpeed = 10f;
    public float rotationSpeed = 180f;

    private Transform playerCamTransform;
    private Vector3 moveDirection;

    private void Start()
    {
        playerCamTransform = GameObject.Find("PlayerCam")?.transform;
        if (playerCamTransform == null)
        {
            playerCamTransform = GameObject.FindGameObjectWithTag("PlayerCam")?.transform;
        }
        if (playerCamTransform == null)
        {
            Debug.LogError("PlayerCam transform not found. Ensure that your camera is named 'PlayerCam' or tagged as 'PlayerCam'.");
        }
    }

    private void Update()
    {
        float horizontalInput = InputBroker.GetAxis("Horizontal");
        float verticalInput = InputBroker.GetAxis("Vertical");

        Vector3 cameraForward = playerCamTransform.forward;
        Vector3 cameraRight = playerCamTransform.right;
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        if (verticalInput > 0f)
        {
            moveDirection = cameraForward * verticalInput;
        }
        else if (verticalInput < 0f)
        {
            moveDirection = cameraForward * verticalInput;
        }
        else
        {
            moveDirection = Vector3.zero;
        }

        Quaternion cameraRotation = Quaternion.Euler(0f, horizontalInput * rotationSpeed * Time.deltaTime, 0f);
        playerCamTransform.rotation *= cameraRotation;
    }

    private void FixedUpdate()
    {
        GetComponent<Rigidbody>().MovePosition(transform.position + moveDirection * movementSpeed * Time.fixedDeltaTime);
    }
}