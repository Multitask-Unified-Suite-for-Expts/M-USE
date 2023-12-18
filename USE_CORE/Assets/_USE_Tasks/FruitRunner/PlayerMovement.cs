using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerMovement : MonoBehaviour
{
    private Rigidbody Rb;
    private Vector3 TargetPos;
    private bool IsShifting = false;
    private float SideShiftSpeed = 20f;

    private Vector3 MovementAmount = new Vector3(1.25f, 0, 0);

    private AudioManager audioManager;


    void Start()
    {
        Rb = GetComponent<Rigidbody>();
        transform.position = Vector3.zero;
        audioManager = GameObject.Find("AudioManager").GetComponent<AudioManager>();

    }

    void Update()
    {
        if (!IsShifting)
        {
            HandleMovement();
        }
    }

    private void FixedUpdate()
    {
        if (IsShifting)
        {
            Vector3 direction = (TargetPos - transform.position).normalized;
            float distance = Vector3.Distance(transform.position, TargetPos);
            float movementAmount = Mathf.Min(SideShiftSpeed * Time.fixedDeltaTime, distance);
            Rb.MovePosition(transform.position + direction * movementAmount);

            if (distance < 0.01f)
                IsShifting = false;
        }
    }

    private void HandleMovement()
    {
        if (InputBroker.GetKeyDown(KeyCode.LeftArrow))
        {
            if(transform.position.x > -1)
                MoveToPosition(transform.position - MovementAmount);
        }

        else if (InputBroker.GetKeyDown(KeyCode.RightArrow))
        {
            if(transform.position.x < 1)
                MoveToPosition(transform.position + MovementAmount);
        }

        //else if (Input.GetMouseButtonDown(0))
        //{
        //    float touchX = Input.mousePosition.x / Screen.width;
        //    Debug.LogWarning("TOUCH X = " + touchX);

        //    if (touchX < 0.5f && transform.position.x > leftXPos)
        //    {
        //        MoveToPosition(transform.position - MovementAmount);
        //    }
        //    else if (touchX >= 0.5f && transform.position.x < rightXPos)
        //    {
        //        MoveToPosition(transform.position + MovementAmount);
        //    }
        //}
    }

    private void MoveToPosition(Vector3 newPos)
    {
        TargetPos = new Vector3(newPos.x, 0f, 0f);
        IsShifting = true;
        audioManager.PlaySlideClip();
    }

}
