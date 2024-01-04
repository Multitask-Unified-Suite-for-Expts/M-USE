using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerMovement : MonoBehaviour
{
    private Rigidbody Rb;
    private Vector3 TargetPos;
    private bool IsShifting = false;
    private float SideShiftSpeed = 19f;

    [HideInInspector] public Vector3 LeftPos = new Vector3(-2.25f, 0f, 0f);
    [HideInInspector] public Vector3 MiddlePos = Vector3.zero;
    [HideInInspector] public Vector3 RightPos = new Vector3(2.25f, 0f, 0f);

    private AudioManager audioManager;
    public MovementCirclesController CirclesController;

    void Start()
    {
        Rb = GetComponent<Rigidbody>();
        transform.position = Vector3.zero;
        audioManager = GameObject.Find("AudioManager").GetComponent<AudioManager>();

        TargetPos = MiddlePos;
    }

    private void Update()
    {
        if(!IsShifting)
        {
            transform.position = TargetPos; //keep it in place if not shifting
            HandleInput();
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

            if (distance < 0.025f)
            {
                transform.position = TargetPos;
                IsShifting = false;
            }
        }
    }

    public void MoveToPosition(Vector3 newPos)
    {
        if(newPos == transform.position)
            return;
        
        TargetPos = newPos;
        IsShifting = true;
        audioManager.PlaySlideClip();
    }

    private void HandleInput()
    {
        if (InputBroker.GetKeyDown(KeyCode.LeftArrow))
        {
            if(transform.position == MiddlePos)
            {
                MoveToPosition(LeftPos);
                CirclesController.HighlightActiveCircle(CirclesController.LeftCircleGO);
            }
            else if(transform.position == RightPos)
            {
                MoveToPosition(MiddlePos);
                CirclesController.HighlightActiveCircle(CirclesController.MiddleCircleGO);
            }
        }

        else if (InputBroker.GetKeyDown(KeyCode.RightArrow))
        {
            if (transform.position == MiddlePos)
            {
                MoveToPosition(RightPos);
                CirclesController.HighlightActiveCircle(CirclesController.RightCircleGO);

            }
            else if (transform.position == LeftPos)
            {
                MoveToPosition(MiddlePos);
                CirclesController.HighlightActiveCircle(CirclesController.MiddleCircleGO);

            }
        }


    }

}










