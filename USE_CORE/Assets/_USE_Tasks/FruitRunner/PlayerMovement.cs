using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerMovement : MonoBehaviour
{
    private Rigidbody Rb;
    private Vector3 TargetPos;
    private bool AllowInput;
    private bool IsShifting = false;
    private float SideShiftSpeed = 19f;

    public readonly Vector3 LeftPos = new Vector3(-1.9f, 0f, 0f);
    public readonly Vector3 MiddlePos = Vector3.zero;
    public readonly Vector3 RightPos = new Vector3(1.9f, 0f, 0f);

    private AudioManager audioManager;
    public MovementCirclesController CirclesController;

    public Animator Animator;

    public TokenFBController TokenFbController;


    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Quaddle"))
        {
            StartAnimation("Happy");
            TokenFbController.AddTokens(other.gameObject, 1, .4f);
        }
        else if(other.CompareTag("Blockade"))
        {
            TokenFbController.RemoveTokens(gameObject, 1, .4f);
        }
    }

    void Start()
    {
        Rb = GetComponent<Rigidbody>();
        transform.position = Vector3.zero;
        audioManager = GameObject.Find("AudioManager").GetComponent<AudioManager>();

        Animator = GetComponent<Animator>();
        StartAnimation("idle");

        TargetPos = MiddlePos;
    }

    private void Update()
    {
        if (!IsShifting)
        {
            transform.position = TargetPos; //keep it in place if not shifting

            if(AllowInput)
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

            if (distance <= 0.05f)
            {
                transform.position = TargetPos;
                IsShifting = false;
            }
        }
        else
        {
            transform.position = TargetPos;
        }
    }

    public void MoveToPosition(Vector3 newPos)
    {
        if (newPos == TargetPos)
            return;

        TargetPos = newPos;
        IsShifting = true;
        audioManager.PlaySlideClip();
    }

    public void AllowUserInput()
    {
        AllowInput = true;
    }

    public void DisableUserInput()
    {
        AllowInput = false;
    }

    private void HandleInput()
    {
        if (InputBroker.GetKeyDown(KeyCode.LeftArrow))
        {
            if (transform.position == MiddlePos)
            {
                MoveToPosition(LeftPos);
                CirclesController.HighlightActiveCircle(CirclesController.LeftCircleGO);
            }
            else if (transform.position == RightPos)
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


    public void StartAnimation(string newAnimName)
    {
        if (Animator == null)
            Animator = GetComponent<Animator>();

        switch (newAnimName.ToLower())
        {
            case "idle":
                Animator.Play("Idle");
                break;
            case "run":
                Animator.Play("Run");
                break;
            case "injured":
                Animator.Play("Injured");
                break;
            case "happy":
                Animator.Play("Happy");
                break;
            case "sad":
                Animator.Play("Sad");
                break;
            default:
                Debug.LogWarning("Invalid Animation State Provided. Options are: Idle, Run, Injured, Happy, Sad");
                break;
        }
    }



}








