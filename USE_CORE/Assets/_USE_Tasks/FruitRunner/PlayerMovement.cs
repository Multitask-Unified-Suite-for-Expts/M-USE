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

    public FloorManager FloorManager;
    private AudioManager audioManager;
    public MovementCirclesController CirclesController;

    public Animator Animator;

    public TokenFBController TokenFbController;

    public bool AllowHappyAndSadAnimations = false;

    public GameObject CelebrationConfetti;

    public GameObject PlayerConfetti;

    public enum AnimationStates { Idle, Run, Injured, Happy, Sad, Cheer};
    public AnimationStates CurrentAnimationState;


    void Start()
    {
        Rb = GetComponent<Rigidbody>();
        transform.position = Vector3.zero;
        TargetPos = MiddlePos;

        audioManager = GameObject.Find("AudioManager").GetComponent<AudioManager>();

        Animator = GetComponent<Animator>();
        StartAnimation("idle");

        Debug.LogWarning("INSTANTIATING!");
        PlayerConfetti = Instantiate(Resources.Load<GameObject>("Prefabs/PlayerConfetti"));
        PlayerConfetti.transform.parent = transform;
        PlayerConfetti.transform.position = Vector3.zero;
        PlayerConfetti.SetActive(false);
    }

    private void Update()
    {
        if (!IsShifting)
        {
            transform.position = TargetPos; //keep it in place if not shifting

            if(AllowInput)
                HandleKeyboardInput();
        }

        if(PlayerConfetti != null)
        {
            PlayerConfetti.SetActive(CurrentAnimationState == AnimationStates.Run);
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

    private void HandleKeyboardInput()
    {
        //Disable Input when doing injury animation
        if(Animator.GetCurrentAnimatorStateInfo(0).IsName("Injured"))
            return;

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

    public void FinalCelebration()
    {
        TargetPos = MiddlePos;
        transform.position = MiddlePos;

        PlayerConfetti.SetActive(false);
        DisableUserInput();
        StartAnimation("Cheer");
        GameObject landingGO = Instantiate(Resources.Load<GameObject>("Prefabs/Podium"));
        landingGO.transform.parent = transform;

        CelebrationConfetti = Instantiate(Resources.Load<GameObject>("Prefabs/Confetti"));
        CelebrationConfetti.SetActive(true);
        CirclesController.Instantiated.SetActive(false);
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
                break;
            case "injured":
                CurrentAnimationState = AnimationStates.Injured;
                Animator.Play("Injured");
                break;
            case "happy":
                if (AllowHappyAndSadAnimations)
                {
                    CurrentAnimationState = AnimationStates.Happy;
                    Animator.Play("Happy");
                }
                break;
            case "sad":
                if (AllowHappyAndSadAnimations)
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








