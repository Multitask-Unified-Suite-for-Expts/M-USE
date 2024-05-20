using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class FR_PlayerManager : MonoBehaviour
{
    private Rigidbody Rb;
    private Vector3 TargetPos;
    private bool AllowInput;
    private bool IsShifting = false;
    private readonly float SideShiftSpeed = 19f;

    public readonly Vector3 LeftPos = new Vector3(-1.9f, 0f, 0f);
    public readonly Vector3 MiddlePos = Vector3.zero;
    public readonly Vector3 RightPos = new Vector3(1.9f, 0f, 0f);

    public FR_FloorManager floorManager;
    private FR_AudioManager audioManager;
    public MovementCirclesController MovementCirclesController;

    public Transform CanvasTransform;

    public Animator Animator;

    public TokenFBController TokenFbController;

    public bool AllowItemPickupAnimations;

    public bool UsingBananas;

    public GameObject CelebrationConfetti;
    public GameObject FinalPlane;

    public enum AnimationStates { Idle, Run, Injured, Happy, Sad, Cheer};
    public AnimationStates CurrentAnimationState;



    void Start()
    {
        Rb = GetComponent<Rigidbody>();
        transform.position = Vector3.zero;
        TargetPos = MiddlePos;

        //Setup Movement Circles:
        MovementCirclesController = gameObject.AddComponent<MovementCirclesController>();
        MovementCirclesController.SetupMovementCircles(CanvasTransform, this);

        audioManager = gameObject.AddComponent<FR_AudioManager>();

        try
        {
            floorManager = GameObject.Find("FloorManager").GetComponent<FR_FloorManager>();
        }
        catch(Exception e)
        {
            Debug.LogError("FR_PlayerManager Start() method failed! Couldnt find the FloorManager GameObject | Error: " + e.Message);
        }

        Animator = GetComponent<Animator>();
        StartAnimation("idle");
    }


    private void Update()
    {
        if (!IsShifting)
        {
            transform.position = TargetPos; //keep it in place if not shifting

            if (AllowInput)
                HandleKeyboardInput();
        }

        //Temporary hotkey to allow toggling of animations:
        if (InputBroker.GetKeyDown(KeyCode.I))
            AllowItemPickupAnimations = !AllowItemPickupAnimations;
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
    }

    public void MoveToPosition(Vector3 newPos)
    {
        if (newPos == TargetPos)
            return;

        TriggerPlayerShiftEvent(newPos);

        TargetPos = newPos;
        IsShifting = true;
        audioManager.PlaySlideClip();
    }

    private void TriggerPlayerShiftEvent(Vector3 newPos)
    {
        string from = "";
        if (TargetPos == LeftPos)
            from = "Left";
        else if (TargetPos == MiddlePos)
            from = "Middle";
        else
            from = "Right";

        string to = "";
        if (newPos == LeftPos)
            to = "Left";
        else if (newPos == MiddlePos)
            to = "Middle";
        else
            to = "Right";

        FR_EventManager.TriggerPlayerShift(from, to);
       
    }

    public void AllowUserInput()
    {
        AllowInput = true;
    }

    public void DisableUserInput()
    {
        AllowInput = false;
    }

    private void HandleKeyboardInput()
    {
        //if(Animator.GetCurrentAnimatorStateInfo(0).IsName("Injured")) //Disable Input when doing injury animation
        //    return;

        if (InputBroker.GetKeyDown(KeyCode.LeftArrow))
        {
            if (transform.position == MiddlePos)
            {
                MoveToPosition(LeftPos);
                MovementCirclesController.HighlightActiveCircle(MovementCirclesController.LeftCircleGO);
            }
            else if (transform.position == RightPos)
            {
                MoveToPosition(MiddlePos);
                MovementCirclesController.HighlightActiveCircle(MovementCirclesController.MiddleCircleGO);
            }
        }

        else if (InputBroker.GetKeyDown(KeyCode.RightArrow))
        {
            if (transform.position == MiddlePos)
            {
                MoveToPosition(RightPos);
                MovementCirclesController.HighlightActiveCircle(MovementCirclesController.RightCircleGO);

            }
            else if (transform.position == LeftPos)
            {
                MoveToPosition(MiddlePos);
                MovementCirclesController.HighlightActiveCircle(MovementCirclesController.MiddleCircleGO);

            }
        }


    }

    public void FinalCelebration()
    {
        TargetPos = MiddlePos;
        transform.position = MiddlePos;

        DisableUserInput();
        StartAnimation("Cheer");
        GameObject landingGO = Instantiate(Resources.Load<GameObject>("Prefabs/Podium"));
        landingGO.transform.parent = transform;

        CelebrationConfetti = Instantiate(Resources.Load<GameObject>("Prefabs/Confetti"));
        CelebrationConfetti.SetActive(true);
        MovementCirclesController.Instantiated.SetActive(false);

        if (UsingBananas)
            return;

        FinalPlane = Instantiate(Resources.Load<GameObject>("Prefabs/FinalPlane"));
        FinalPlane.SetActive(true);
    }

    public void DestroyFinalPlane()
    {
        if (FinalPlane != null)
            Destroy(FinalPlane);
    }

    //Helper method used by trial level at end to put player back in middle for celebration. 
    public void SetToMiddlePos()
    {
        TargetPos = MiddlePos;
        transform.position = MiddlePos;
    }


    public void StartAnimation(string newAnimName)
    {
        if (Animator == null)
            Animator = GetComponent<Animator>();


        switch (newAnimName.ToLower())
        {
            case "idle":
                CurrentAnimationState = AnimationStates.Idle;
                Animator.Play("Idle");
                break;
            case "run":
                CurrentAnimationState = AnimationStates.Run;
                Animator.Play("Run");
                //Animator.Play(floorManager.FloorMovementSpeed >= 10f ? "Run" : "Jog"); //I also removed jog from animator
                break;
            case "injured":
                CurrentAnimationState = AnimationStates.Injured;
                Animator.Play("Injured");
                break;
            case "happy":
                if (AllowItemPickupAnimations)
                {
                    CurrentAnimationState = AnimationStates.Happy;
                    Animator.Play("Happy");
                }
                break;
            case "sad":
                if (AllowItemPickupAnimations)
                {
                    CurrentAnimationState = AnimationStates.Sad;
                    Animator.Play("Sad");
                }
                break;
            case "cheer":
                CurrentAnimationState = AnimationStates.Cheer;
                audioManager.PlayCrowdCheering();
                Animator.Play("Cheer");
                break;
            default:
                Debug.LogWarning("Invalid Animation State Provided. Options are: Idle, Run, Injured, Happy, Sad, Cheer");
                break;
        }
    }

    private void OnDestroy()
    {
        if(audioManager != null)
            audioManager.StopAllAudio();

        if (CelebrationConfetti != null)
            Destroy(CelebrationConfetti);
    }

}









