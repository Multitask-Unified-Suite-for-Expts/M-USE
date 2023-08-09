using UnityEngine;
using System.Collections.Generic;
using USE_States;
using EffortControl_Namespace;
using System.Linq;
using USE_ExperimentTemplate_Trial;
using ConfigDynamicUI;
using UnityEngine.UI;
using TMPro;


public class EffortControl_TrialLevel : ControlLevel_Trial_Template
{
    public EffortControl_TrialDef currentTrial => GetCurrentTrialDef<EffortControl_TrialDef>();
    public EffortControl_TaskLevel currentTask => GetTaskLevel<EffortControl_TaskLevel>();
    public EffortControl_TaskDef currentTaskDef => GetTaskDef<EffortControl_TaskDef>();

    //Prefabs to Instantiate:
    public GameObject StimNoMaterialPrefab;
    public GameObject StimLeftPrefab;
    public GameObject StimRightPrefab;
    public GameObject RewardPrefab;
    public GameObject OutlinePrefab;

    [HideInInspector] public Vector3 OriginalStartButtonPosition;

    public GameObject EC_CanvasGO;

    private GameObject StartButton, StimLeft, StimRight, TrialStim, BalloonContainerLeft, BalloonContainerRight,
               BalloonOutline, RewardContainerLeft, RewardContainerRight, Reward, MiddleBarrier;

    private Color Red;
    private Color32 OffWhiteOutlineColor = new Color32(250, 249, 246, 0);

    private Vector3 LeftScaleUpAmount;
    private Vector3 RightScaleUpAmount;
    private Vector3 MaxScale;
    private Vector3 TrialStimInitLocalScale;
    private Vector3 LeftContainerOriginalPosition;
    private Vector3 RightContainerOriginalPosition;
    private Vector3 LeftRewardContainerOriginalPosition;
    private Vector3 RightRewardContainerOriginalPosition;
    private Vector3 LeftStimOriginalPosition;
    private Vector3 RightStimOriginalPosition;


    [System.NonSerialized] public int Response = -1;
    private int InflationsNeeded; //becomes left/right num clicks once they make selection. 
    private int NumInflations;
    private bool AddTokenInflateAudioPlayed;
    private bool ObjectsCreated;
    [HideInInspector] private List<GameObject> RemoveParentList;
    [HideInInspector] private GameObject Wrapper;

    private string SideChoice; //left or right
    private string RewardChoice; //higher or lower
    private string EffortChoice; //higher or lower

    //To center the balloon they selected:
    protected float CenteringSpeed;
    private Vector3 CenteredPos;
    [HideInInspector] public bool Flashing;

    //Variables to Inflate balloon at interval rate
    private float InflateClipDuration;
    private bool Inflate;
    private readonly float MaxInflation_Y = 35f; //IS THIS WORKING?!?!
    private float ScalePerInflation_Y;
    [HideInInspector] public float ScaleTimer;
    private Vector3 IncrementAmounts;
    private Vector3 NextScale;

    //Trial specific Data variables:
    [HideInInspector] public float AvgClickTime;
    [HideInInspector] public float ChooseDuration;
    //Block specific Data variables:
    [HideInInspector] public int RewardPulses_Block;
    [HideInInspector] public int TotalTouches_Block;
    [HideInInspector] public int Completions_Block;
    [HideInInspector] public int NumChosenLeft_Block;
    [HideInInspector] public int NumChosenRight_Block;
    [HideInInspector] public int NumHigherEffortChosen_Block;
    [HideInInspector] public int NumLowerEffortChosen_Block;
    [HideInInspector] public int NumSameEffortChosen_Block;
    [HideInInspector] public int NumHigherRewardChosen_Block;
    [HideInInspector] public int NumLowerRewardChosen_Block;
    [HideInInspector] public int NumSameRewardChosen_Block;
    [HideInInspector] public int NumAborted_Block;

    [HideInInspector] public ConfigNumber minObjectTouchDuration, maxObjectTouchDuration, scalingInterval, inflateDuration, itiDuration, popToFeedbackDelay, choiceToTouchDelay, sbToBalloonDelay; //ScalingInterval is used for balloonInflation!

    [HideInInspector] public GameObject MaxOutline_Left;
    [HideInInspector] public GameObject MaxOutline_Right;

    [HideInInspector] public bool InflateAudioPlayed;

    [HideInInspector] public List<float> clickTimings;
    [HideInInspector] public float timeTracker;

    [HideInInspector] public float BalloonSelectedTime;
    [HideInInspector] public float StartButtonSelectedTime;

    [HideInInspector] public int TrialTouches;

    [HideInInspector] public List<GameObject> ObjectList;

    [HideInInspector] public GameObject debugTextGO;
    [HideInInspector] public TextMeshProUGUI debugText;

    public override void DefineControlLevel()
    {
        State InitTrial = new State("InitTrial");
        State ChooseBalloon = new State("ChooseBalloon");
        State CenterSelection = new State("CenterSelection");
        State InflateBalloon = new State("InflateBalloon");
        State Feedback = new State("Feedback");
        State ITI = new State("ITI");
        AddActiveStates(new List<State> { InitTrial, ChooseBalloon, CenterSelection, InflateBalloon, Feedback, ITI });

        Add_ControlLevel_InitializationMethod(() =>
        {
            Camera.main.transform.position = new Vector3(0, .6f, Screen.fullScreen && Screen.width != 1920 ? -2.5f : -2.18f);

            if (AudioFBController != null)
                InflateClipDuration = AudioFBController.GetClip("EC_Inflate").length;

            if (StartButton == null)
            {
                if (SessionValues.SessionDef.IsHuman)
                {
                    StartButton = SessionValues.HumanStartPanel.StartButtonGO;
                    SessionValues.HumanStartPanel.SetVisibilityOnOffStates(InitTrial, InitTrial);
                }
                else
                {
                    StartButton = SessionValues.USE_StartButton.CreateStartButton(EC_CanvasGO.GetComponent<Canvas>(), currentTaskDef.StartButtonPosition, currentTaskDef.StartButtonScale);
                    SessionValues.USE_StartButton.SetVisibilityOnOffStates(InitTrial, InitTrial);
                }
            }

            if (!ObjectsCreated)
                CreateObjects();

            CenteringSpeed = 1.5f;
        });

        //SETUP TRIAL state -----------------------------------------------------------------------------------------------------
        SetupTrial.AddInitializationMethod(() =>
        {
            LoadConfigUIVariables();
            if (TrialCount_InTask != 0)
                currentTask.SetTaskSummaryString();

            if (TokenFBController != null)
                SetTokenVariables();
        });
        SetupTrial.SpecifyTermination(() => true, InitTrial);

        //INIT Trial state -------------------------------------------------------------------------------------------------------
        var Handler = SessionValues.SelectionTracker.SetupSelectionHandler("trial", "MouseButton0Click", SessionValues.MouseTracker, InitTrial, InflateBalloon);
        if (!SessionValues.SessionDef.IsHuman)
            TouchFBController.EnableTouchFeedback(Handler, currentTaskDef.TouchFeedbackDuration, currentTaskDef.StartButtonScale * 10, EC_CanvasGO);

        InitTrial.AddInitializationMethod(() =>
        {
            TokenFBController.enabled = false;
            ResetRelativeStartTime(); 
            DisableAllGameobjects();

            ResetToOriginalPositions();

            if(TrialStim != null)
            {
                TrialStim.transform.localScale = TrialStimInitLocalScale;
                TrialStim = null;
            }

            currentTask.CalculateBlockSummaryString();

            if(Handler.AllSelections.Count > 0)
                Handler.ClearSelections();
            Handler.MinDuration = minObjectTouchDuration.value;
            Handler.MaxDuration = maxObjectTouchDuration.value;           
        });
        InitTrial.SpecifyTermination(() => Handler.LastSuccessfulSelectionMatches(SessionValues.SessionDef.IsHuman ? SessionValues.HumanStartPanel.StartButtonChildren : SessionValues.USE_StartButton.StartButtonChildren), Delay, () =>
        {
            DelayDuration = sbToBalloonDelay.value;
            StateAfterDelay = ChooseBalloon;
            SessionValues.EventCodeManager.SendCodeImmediate("StartButtonSelected");
        });

        //Choose Balloon state -------------------------------------------------------------------------------------------------------
        ChooseBalloon.AddInitializationMethod(() =>
        {
            Input.ResetInputAxes(); //reset input in case they holding down

            MiddleBarrier.SetActive(true);

            BalloonContainerLeft.transform.localPosition = new Vector3(-1.14f, -1.5f, .5f);
            BalloonContainerRight.transform.localPosition = new Vector3(1.14f, -1.5f, .5f);

            RewardContainerLeft.transform.localPosition = new Vector3(-.99f, 1.5825f, 0);
            RewardContainerRight.transform.localPosition = new Vector3(.99f, 1.5825f, 0);

            MaxScale = new Vector3(65.5f, 0, 65.5f);
            LeftScaleUpAmount = MaxScale / currentTrial.NumClicksLeft;
            RightScaleUpAmount = MaxScale / currentTrial.NumClicksRight;

            CreateBalloonOutlines(currentTrial.NumClicksLeft, LeftScaleUpAmount, StimLeft.transform.position, BalloonContainerLeft);
            CreateBalloonOutlines(currentTrial.NumClicksRight, RightScaleUpAmount, StimRight.transform.position, BalloonContainerRight);
            CreateRewards(currentTrial.NumCoinsLeft, RewardContainerLeft.transform.position, RewardContainerLeft);
            CreateRewards(currentTrial.NumCoinsRight, RewardContainerRight.transform.position, RewardContainerRight);
            CreateTransparentBalloons();
            ActivateObjects();

            SideChoice = null;

            if(Handler.AllSelections.Count > 0)
                Handler.ClearSelections();
        });

        ChooseBalloon.AddUpdateMethod(() =>
        {
            if (Handler.SuccessfulSelections.Count > 0)
            {
                BalloonSelectedTime = Time.time;
                if (Handler.LastSelection.SelectedGameObject.name.Contains("Left"))
                {
                    SideChoice = "Left";
                    TrialStim = StimLeft;
                }
                else if (Handler.LastSelection.SelectedGameObject.name.Contains("Right"))
                {
                    SideChoice = "Right";
                    TrialStim = StimRight;
                }
            }

            //Neg FB if touch outside balloon. Adding "sideChoice == null" so that they cant click outside balloon at the end and mess up pop audio.
            if (InputBroker.GetMouseButtonDown(0) && SideChoice == null)
            {
                GameObject hitGO = InputBroker.RaycastBoth(InputBroker.mousePosition);
                if (hitGO == null)
                    AudioFBController.Play("Negative");
            }
        });
        ChooseBalloon.SpecifyTermination(() => SideChoice != null, CenterSelection, () =>
        {
            SessionValues.EventCodeManager.SendCodeImmediate("Button0PressedOnTargetObject");//SELECTION STUFF (code may not be exact and/or could be moved to Selection handler)
            SessionValues.EventCodeManager.SendCodeImmediate(TaskEventCodes["BalloonChosen"]);
            
            DestroyChildren(SideChoice == "Left" ? RewardContainerRight : RewardContainerLeft);
            InflationsNeeded = SideChoice == "Left" ? currentTrial.NumClicksLeft : currentTrial.NumClicksRight;
            AudioFBController.Play("EC_BalloonChosen");
            RecordChoices();
        });

        //Center Selection state -------------------------------------------------------------------------------------------------------
        CenterSelection.AddInitializationMethod(() =>
        {
            ChooseDuration = ChooseBalloon.TimingInfo.Duration;

            Wrapper = new GameObject("Wrapper");

            float xValue = .98f;
            if (Screen.fullScreen && Screen.width > 1920)
                xValue = Screen.width > 3000 ? .96f : .88f; //test the .95 its for mac

            CenteredPos = new Vector3(SideChoice == "Left" ? xValue : -xValue, 0, 0);

            MiddleBarrier.SetActive(false);

            if (SideChoice == "Left")
            {
                SetParents(Wrapper, new List<GameObject>() {BalloonContainerLeft, StimLeft, RewardContainerLeft});
                DestroyChildren(BalloonContainerRight);
                StimRight.SetActive(false);
            }
            else
            {
                SetParents(Wrapper, new List<GameObject>() {BalloonContainerRight, StimRight, RewardContainerRight});
                DestroyChildren(BalloonContainerLeft);
                StimLeft.SetActive(false);
            }
        });
        CenterSelection.AddUpdateMethod(() =>
        {
            if(Wrapper.transform.position != CenteredPos)
                Wrapper.transform.position = Vector3.MoveTowards(Wrapper.transform.position, CenteredPos, CenteringSpeed * Time.deltaTime);
        });
        CenterSelection.SpecifyTermination(() => Wrapper.transform.position == CenteredPos, Delay);

        CenterSelection.AddDefaultTerminationMethod(() =>
        {
            StateAfterDelay = InflateBalloon;
            DelayDuration = choiceToTouchDelay.value;

            RemoveParents(Wrapper, RemoveParentList);

            if (SideChoice == "Left")
            {
                RewardContainerLeft.SetActive(false); //set Reward tokens to inactive since they are replaced by tokenbar tokens.
                MaxOutline_Left.transform.parent = null; //remove parent of transparent balloon outline. 
            }
            else
            {
                RewardContainerRight.SetActive(false);
                MaxOutline_Right.transform.parent = null;
            }
            TokenFBController.SetTotalTokensNum(SideChoice == "Left" ? currentTrial.NumCoinsLeft : currentTrial.NumCoinsRight);
            TokenFBController.enabled = true;
        });

        //Inflate Balloon state -------------------------------------------------------------------------------------------------------
        int outlineClicksRemaining = 1;
        int successfulSelections = 0;
        float startTime = 0;
        float holdTime = 0;
        List<GameObject> correctObjects = new List<GameObject>();
        InflateBalloon.AddInitializationMethod(() =>
        {
            //Debug.Log("STIM LOCAL Y = " + TrialStim.transform.localScale.y);
            ScalePerInflation_Y = (MaxInflation_Y - TrialStim.transform.localScale.y) / (SideChoice == "Left" ? currentTrial.NumClicksLeft : currentTrial.NumClicksRight);
            //Debug.Log("SCALE PER INFLATION Y = " + ScalePerInflation_Y);
            IncrementAmounts = new Vector3();
            Flashing = false;
            InflateAudioPlayed = false;
            ScaleTimer = 0;
            SessionValues.MouseTracker.ResetClicks();
            clickTimings = new List<float>();
            timeTracker = 0;

            TrialTouches = 0;
            NumInflations = 0;

            if (Handler.AllSelections.Count > 0)
                Handler.ClearSelections();

            SetTrialSummaryString();

            outlineClicksRemaining = currentTrial.ClicksPerOutline;

            successfulSelections = 0;
            startTime = 0;
            holdTime = 0;
            successfulSelections = 0;

            correctObjects = new List<GameObject>() {TrialStim, MaxOutline_Left, MaxOutline_Right };
        });
        InflateBalloon.AddUpdateMethod(() =>
        {
            if (Inflate)
            {
                if (!InflateAudioPlayed)
                {
                    AudioFBController.Play("EC_Inflate");
                    InflateAudioPlayed = true;
                }

                ScaleTimer += Time.deltaTime;
                if (ScaleTimer >= (InflateClipDuration / scalingInterval.value)) //When timer hits for next inflation
                {
                    if (TrialStim.transform.localScale != NextScale)
                        ScaleToNextInterval();
                    else
                    {
                        Inflate = false;
                        Handler.HandlerActive = true;
                        if (NumInflations >= InflationsNeeded)
                        {
                            Response = 1;
                            AvgClickTime = clickTimings.Average();
                        }
                    }
                    ScaleTimer = 0; //Reset Timer for next inflation increment;
                }
            }

            if (InputBroker.GetMouseButtonDown(0))
                startTime = Time.time;

            if(InputBroker.GetMouseButtonUp(0))
            {
                TrialTouches++;
                SetTrialSummaryString();

                holdTime = Time.time - startTime;

                if(holdTime >= Handler.MinDuration && holdTime < Handler.MaxDuration)
                {
                    GameObject clickedGO = InputBroker.RaycastBoth(InputBroker.mousePosition);
                    if(clickedGO != null && correctObjects.Contains(clickedGO)) //If they correctly clicked inside the balloon
                    {
                        successfulSelections++;

                        if (outlineClicksRemaining > 1 && !Inflate)
                        {
                            if (outlineClicksRemaining > 1) //Dont play on the last one because full inflate will play 
                                AudioFBController.Play("SHORT_INFLATION");
                            outlineClicksRemaining--;
                        }

                        else if (successfulSelections > NumInflations && !Inflate && outlineClicksRemaining == 1)
                        {
                            if (NumInflations < InflationsNeeded)
                            {
                                Input.ResetInputAxes();
                                clickTimings.Add(Time.time - timeTracker);
                                timeTracker = Time.time;

                                Handler.HandlerActive = false;
                                NumInflations++;

                                SessionValues.EventCodeManager.SendCodeNextFrame("Button0PressedOnTargetObject");//SELECTION STUFF (code may not be exact and/or could be moved to Selection handler)
                                SessionValues.EventCodeManager.SendCodeNextFrame("CorrectResponse");

                                CalculateInflation(); //Sets Inflate to TRUE at end of func
                                InflateAudioPlayed = false;

                                outlineClicksRemaining = currentTrial.ClicksPerOutline;
                            }
                        }
                    }
                }

            }

            //Neg FB if touch outside balloon. Adding response != 1 so that they cant click outside balloon at the end and mess up pop audio.
            if (InputBroker.GetMouseButtonDown(0) && Response != 1)
            {
                GameObject hitGO = InputBroker.RaycastBoth(InputBroker.mousePosition);
                if (hitGO == null)
                    AudioFBController.Play("Negative");
            }

        });
        InflateBalloon.AddTimer(() => inflateDuration.value, Delay);
        InflateBalloon.SpecifyTermination(() => Response == 1, Delay);
        InflateBalloon.AddDefaultTerminationMethod(() =>
        {
            StateAfterDelay = Feedback;
            DelayDuration = popToFeedbackDelay.value;
            TotalTouches_Block += TrialTouches;
            currentTask.Touches_Task += TrialTouches;

            if (SideChoice == "Left")
                MaxOutline_Left.transform.parent = BalloonContainerLeft.transform;
            else
                MaxOutline_Right.transform.parent = BalloonContainerRight.transform;

            DestroyChildren(SideChoice == "Left" ? BalloonContainerLeft : BalloonContainerRight);
            InflateAudioPlayed = false;
            
            
            if (Response == 1)
            {
                if(SessionValues.SessionDef.IsHuman)
                    AudioFBController.Play("EC_HarshPop"); //better for humans
                else
                    AudioFBController.Play("EC_NicePop"); //better for monkeys
            }
            else
            {
                NumAborted_Block++;
                AudioFBController.Play("TimeRanOut");
                TokenFBController.enabled = false;
                SessionValues.EventCodeManager.SendCodeImmediate("NoChoice");
            }
            TrialStim.SetActive(false);
        });

        //Feedback state -------------------------------------------------------------------------------------------------------
        Feedback.AddInitializationMethod(() =>
        {
            if (Response == 1)
            {
                GameObject CenteredGO = new GameObject();
                CenteredGO.transform.position = new Vector3(0, .5f, 0);
                TokenFBController.AddTokens(CenteredGO, SideChoice == "Left" ? currentTrial.NumCoinsLeft : currentTrial.NumCoinsRight);
                Destroy(CenteredGO);

                if (SessionValues.SyncBoxController != null)
                {
                    GiveReward();
                    SessionValues.EventCodeManager.SendCodeNextFrame("SyncBoxController_RewardPulseSent");
                }

                Completions_Block++;
                AddTokenInflateAudioPlayed = true;
            }
          //  else
               // EventCodeManager.SendCodeNextFrame(SessionEventCodes["Unrewarded"]);
        });
        Feedback.SpecifyTermination(() => AddTokenInflateAudioPlayed && !AudioFBController.IsPlaying() && !TokenFBController.IsAnimating(), ITI);
        Feedback.SpecifyTermination(() => true && Response != 1, ITI);
        Feedback.AddDefaultTerminationMethod(() =>
        {
            TokenFBController.enabled = false;
            AddTokenInflateAudioPlayed = false;
        });

        //ITI state -------------------------------------------------------------------------------------------------------
        ITI.AddTimer(itiDuration.value, FinishTrial);
        
        DefineTrialData();
        DefineFrameData();
    }

    //HELPER FUNCTIONS -------------------------------------------------------------------------------------------------------
    public override void ResetTrialVariables()
    {
        NumInflations = 0;
        Response = -1;
        ChooseDuration = 0;
        InflationsNeeded = 0;
        AvgClickTime = 0;
        SideChoice = "";
        EffortChoice = "";
        RewardChoice = "";
        TrialTouches = 0;
    }

    public override void FinishTrialCleanup() //called automatically at start of FinishTrial state
    {
        if (TrialStim != null && TrialStim.activeInHierarchy)
            TrialStim.SetActive(false);

        if (MiddleBarrier.activeInHierarchy)
            MiddleBarrier.SetActive(false);

        if (BalloonContainerLeft != null)
            DestroyChildren(BalloonContainerLeft);
        if (BalloonContainerRight != null)
            DestroyChildren(BalloonContainerRight);
        if (RewardContainerLeft != null)
            DestroyChildren(RewardContainerLeft);
        if (RewardContainerRight != null)
            DestroyChildren(RewardContainerRight);

        Destroy(MaxOutline_Right);
        Destroy(MaxOutline_Left);

        if(AbortCode == 0) //Normal
            currentTask.CalculateBlockSummaryString();

        if (AbortCode == AbortCodeDict["RestartBlock"] || AbortCode == AbortCodeDict["PreviousBlock"] || AbortCode == AbortCodeDict["EndBlock"]) //If used RestartBlock, PreviousBlock, or EndBlock hotkeys
        {
            NumAborted_Block++;
            currentTask.ClearStrings();
            currentTask.BlockSummaryString.AppendLine("");
        }

        ClearTrialSummaryString();
    }

    public void ActivateObjects()
    {
        foreach (GameObject go in ObjectList)
        {
            if (go != null)
                go.SetActive(true);
        }
    }

    public void ResetBlockVariables()
    {
        Completions_Block = 0;
        NumChosenLeft_Block = 0;
        NumChosenRight_Block = 0;
        NumHigherRewardChosen_Block = 0;
        NumLowerRewardChosen_Block = 0;
        NumHigherEffortChosen_Block = 0;
        NumLowerEffortChosen_Block = 0;
        NumSameEffortChosen_Block = 0;
        NumSameRewardChosen_Block = 0;
        TotalTouches_Block = 0;
        RewardPulses_Block = 0;
        NumAborted_Block = 0;
    }

    void ScaleToNextInterval()
    {
        //If close and next increment would go over target scale, recalculate the exact amount:
        if (TrialStim.transform.localScale.x + IncrementAmounts.x > NextScale.x) 
            IncrementAmounts = new Vector3((NextScale.x - TrialStim.transform.localScale.x), (NextScale.y - TrialStim.transform.localScale.y), (NextScale.z - TrialStim.transform.localScale.z));

        //Scale:
        TrialStim.transform.localScale += new Vector3(IncrementAmounts.x, IncrementAmounts.y, IncrementAmounts.z);
    }

    void CalculateInflation()
    {      
        GameObject container = (SideChoice == "Left") ? BalloonContainerLeft : BalloonContainerRight;
        NextScale = container.transform.GetChild(NumInflations-1).transform.localScale;
        NextScale.y = ScalePerInflation_Y + TrialStim.transform.localScale.y;
        Vector3 difference = NextScale - TrialStim.transform.localScale;
        IncrementAmounts = new Vector3((difference.x / scalingInterval.value), (difference.y / scalingInterval.value), (difference.z / scalingInterval.value));

        Inflate = true;
    }

    void RecordChoices()
    {
        if(SideChoice == "Left")
        {
            NumChosenLeft_Block++;
            EffortChoice = CompareValues(currentTrial.NumClicksLeft, currentTrial.NumClicksRight);
            RewardChoice = CompareValues(currentTrial.NumCoinsLeft, currentTrial.NumCoinsRight);
        }
        else
        {
            NumChosenRight_Block++;
            EffortChoice = CompareValues(currentTrial.NumClicksRight, currentTrial.NumClicksLeft);
            RewardChoice = CompareValues(currentTrial.NumCoinsRight, currentTrial.NumCoinsLeft);
        }

        if (EffortChoice == "Higher")
            NumHigherEffortChosen_Block++;
        else if (EffortChoice == "Lower")
            NumLowerEffortChosen_Block++;
        else
            NumSameEffortChosen_Block++;

        if (RewardChoice == "Higher")
            NumHigherRewardChosen_Block++;
        else if (RewardChoice == "Lower")
            NumLowerRewardChosen_Block++;
        else
            NumSameRewardChosen_Block++;
    }

    public string CompareValues(int chosenValue, int otherValue)
    {
        if (chosenValue == otherValue)
            return "Same";
        else if (chosenValue > otherValue)
            return "Higher";
        else
            return "Lower";
    }

    void SetTokenVariables() //NEED TO TEST ALL THESE Y OFFSETS
    {
        float tokenSize = SessionValues.SessionDef.MacMainDisplayBuild && !Application.isEditor ? 212 : 106;
        float yOffset = SessionValues.SessionDef.MacMainDisplayBuild && !Application.isEditor ? 45 : 5; //was 22

        if(SessionValues.WebBuild && !Application.isEditor)
        {
            tokenSize = 116;
            yOffset = 25;

            if (Screen.fullScreen && Screen.width > 1920)
            {
                tokenSize = Screen.width > 3000 ? 103 : 121;
                yOffset = Screen.width > 3000 ? 98 : 120;
            }
        }

        TokenFBController.tokenSize = tokenSize;
        TokenFBController.tokenBoxYOffset = yOffset;

        TokenFBController.SetFlashingTime(1.5f);
        TokenFBController.tokenSpacing = -(Screen.width * .009375f);
    }

    void SetParents(GameObject wrapper, List<GameObject> objects) // 1) Setting the parent of each GO, and 2) Adding to RemovalList (so can remove easily later)
    {
        RemoveParentList = new List<GameObject>();
        foreach(GameObject go in objects)
        {
            go.transform.parent = wrapper.transform; //set parent
            RemoveParentList.Add(go); //add to remove parent list for later
        }
    }

    void RemoveParents(GameObject wrapper, List<GameObject> objects)
    {
        if (objects.Count < 1 || objects == null)
            Debug.Log("There are no objects in the List!");
        else
        {
            foreach (GameObject go in objects)
                go.transform.parent = null;
        }
        Destroy(wrapper);
    }

    void ResetToOriginalPositions()
    {
        BalloonContainerLeft.transform.position = LeftContainerOriginalPosition;
        RewardContainerLeft.transform.position = LeftRewardContainerOriginalPosition;
        StimLeft.transform.position = LeftStimOriginalPosition;

        BalloonContainerRight.transform.position = RightContainerOriginalPosition;
        RewardContainerRight.transform.position = RightRewardContainerOriginalPosition;
        StimRight.transform.position = RightStimOriginalPosition;
    }

    void GiveReward()
    {
        if (SideChoice == "Left")
        {
            SessionValues.SyncBoxController.SendRewardPulses(currentTrial.NumPulsesLeft, currentTrial.PulseSizeLeft);
           // SessionInfoPanel.UpdateSessionSummaryValues(("totalRewardPulses",currentTrial.NumPulsesLeft));
            RewardPulses_Block += currentTrial.NumPulsesLeft;
            currentTask.RewardPulses_Task += currentTrial.NumPulsesLeft;

        }
        else
        {
            SessionValues.SyncBoxController.SendRewardPulses(currentTrial.NumPulsesRight, currentTrial.PulseSizeRight);
           // SessionInfoPanel.UpdateSessionSummaryValues(("totalRewardPulses",currentTrial.NumPulsesRight));
            RewardPulses_Block += currentTrial.NumPulsesRight;
            currentTask.RewardPulses_Task += currentTrial.NumPulsesRight;
        }
    }

    void DisableAllGameobjects()
    {
        StimLeft.SetActive(false);
        StimRight.SetActive(false);
        BalloonOutline.SetActive(false);
    }

    void LoadConfigUIVariables()
    {
        minObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("minObjectTouchDuration");
        maxObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("maxObjectTouchDuration");
        scalingInterval = ConfigUiVariables.get<ConfigNumber>("scalingInterval");
        inflateDuration = ConfigUiVariables.get<ConfigNumber>("inflateDuration");
        itiDuration = ConfigUiVariables.get<ConfigNumber>("itiDuration");
        popToFeedbackDelay = ConfigUiVariables.get<ConfigNumber>("popToFeedbackDelay");
        choiceToTouchDelay = ConfigUiVariables.get<ConfigNumber>("choiceToTouchDelay");
        sbToBalloonDelay = ConfigUiVariables.get<ConfigNumber>("sbToBalloonDelay");
    }


    void CreateObjects()
    {
        StimLeft = Instantiate(StimLeftPrefab, StimLeftPrefab.transform.position, StimLeftPrefab.transform.rotation);
        StimLeft.name = "StimLeft";
        StimLeft.transform.localScale *= 1.2f;
        StimLeft.transform.localPosition = new Vector3(StimLeft.transform.localPosition.x, -.0785f, StimLeft.transform.localPosition.z);
        Red = StimLeft.GetComponent<Renderer>().material.color;
        StimLeft.GetComponent<Renderer>().material.color = Red;
        TrialStimInitLocalScale = StimLeft.transform.localScale;
        AddRigidBody(StimLeft);
        ObjectList.Add(StimLeft);

        StimRight = Instantiate(StimRightPrefab, StimRightPrefab.transform.position, StimRightPrefab.transform.rotation);
        StimRight.name = "StimRight";
        StimRight.transform.localScale *= 1.2f;
        StimRight.transform.localPosition = new Vector3(StimRight.transform.localPosition.x, -.0785f, StimRight.transform.localPosition.z);
        StimRight.GetComponent<Renderer>().material.color = Red;
        AddRigidBody(StimRight);
        ObjectList.Add(StimRight);

        Reward = Instantiate(RewardPrefab, RewardPrefab.transform.position, RewardPrefab.transform.rotation);
        Reward.name = "Reward";
        Reward.GetComponent<Renderer>().material.color = Color.gray; //turn token color to grey so they dont look collected yet.

        BalloonOutline = Instantiate(OutlinePrefab, OutlinePrefab.transform.position, OutlinePrefab.transform.rotation);
        BalloonOutline.name = "Outline";
        BalloonOutline.transform.localScale = new Vector3(10, 0.01f, 10);
        BalloonOutline.GetComponent<Renderer>().material.color = OffWhiteOutlineColor;

        BalloonContainerLeft = new GameObject("BalloonContainerLeft");
        ObjectList.Add(BalloonContainerLeft);
        BalloonContainerRight = new GameObject("BalloonContainerRight");
        ObjectList.Add(BalloonContainerRight);

        RewardContainerLeft = new GameObject("RewardContainerLeft");
        ObjectList.Add(RewardContainerLeft);
        RewardContainerRight = new GameObject("RewardContainerRight");
        ObjectList.Add(RewardContainerRight);

        CreateMiddleBarrier();

        float wrapperXpos = .14f; //for normal 1920 x 1080
        if(Screen.fullScreen && Screen.width != 1920)
            wrapperXpos = Screen.width > 3000 ? .125f : .05f; //.05 for ipad fullscreen, .08 for mac fullscreen

        //Wrap Left side objects in container and Center to left side:
        GameObject leftWrapper = new GameObject();
        leftWrapper.name = "LeftWrapper";
        List<GameObject> leftList = new List<GameObject>() { BalloonContainerLeft, RewardContainerLeft, StimLeft };
        SetParents(leftWrapper, leftList);
        leftWrapper.transform.position = new Vector3(-wrapperXpos, -.05f, 0); //Centering on left half of screen. 

        //Wrap Right side objects in container and Center to right side:
        GameObject rightWrapper = new GameObject();
        rightWrapper.name = "RightWrapper";
        List<GameObject> rightList = new List<GameObject>() { BalloonContainerRight, RewardContainerRight, StimRight };
        SetParents(rightWrapper, rightList);
        rightWrapper.transform.position = new Vector3(wrapperXpos, -.05f, 0); //Centering on right half of screen. 


        LeftContainerOriginalPosition = BalloonContainerLeft.transform.position;
        RightContainerOriginalPosition = BalloonContainerRight.transform.position;
        LeftRewardContainerOriginalPosition = RewardContainerLeft.transform.position;
        RightRewardContainerOriginalPosition = RewardContainerRight.transform.position;
        LeftStimOriginalPosition = StimLeft.transform.position;
        RightStimOriginalPosition = StimRight.transform.position;

        //now that positions are set, remove parents so the balloon is clickable. 
        RemoveParents(leftWrapper, leftList);
        RemoveParents(rightWrapper, rightList);

        StartButton.SetActive(false);
        BalloonOutline.SetActive(false);
        Reward.SetActive(false);

        ObjectsCreated = true;
    }

    void CreateMiddleBarrier()
    {
        MiddleBarrier = new GameObject("MiddleBarrier");
        MiddleBarrier.transform.SetParent(EC_CanvasGO.transform, false);
        Image image = MiddleBarrier.AddComponent<Image>();
        image.rectTransform.anchoredPosition = Vector2.zero;

        if (SessionValues.SessionDef.MacMainDisplayBuild)
            image.transform.localScale = new Vector3(.06f, 15f, .001f);
        else
            image.transform.localScale = new Vector3(.06f, 11f, .001f);


        #if (UNITY_WEBGL && !UNITY_EDITOR)
            image.transform.localScale = new Vector3(.06f, 15f, .001f);
        #endif

        MiddleBarrier.SetActive(false);
    }

    void CreateTransparentBalloons()
    {
        //transparent balloon with size of biggest outline, allows easy detection of clicks within the entire balloon
        MaxOutline_Left = Instantiate(StimNoMaterialPrefab, StimLeft.transform.position, StimLeftPrefab.transform.rotation);
        MaxOutline_Left.name = "MaxOutline_Left";
        MaxOutline_Left.transform.localScale = new Vector3(70f, .1f, 70f);
        MaxOutline_Left.transform.SetParent(BalloonContainerLeft.transform);

        MaxOutline_Right = Instantiate(StimNoMaterialPrefab, StimRight.transform.position, StimRightPrefab.transform.rotation);
        MaxOutline_Right.name = "MaxOutline_Right";
        MaxOutline_Right.transform.localScale = new Vector3(70f, .1f, 70f);
        MaxOutline_Right.transform.SetParent(BalloonContainerRight.transform);

        ObjectList.Add(MaxOutline_Left);
        ObjectList.Add(MaxOutline_Right);
    }

    void CreateBalloonOutlines(int numBalloons, Vector3 ScaleUpAmount, Vector3 pos, GameObject container)
    {

        for (int i = 1; i <= numBalloons; i ++)
        {
            GameObject outline = Instantiate(BalloonOutline, pos, BalloonOutline.transform.rotation);
            outline.transform.parent = container.transform;
            outline.name = "Outline_" + (container.name.ToLower().Contains("left") ? "Left_" : "Right_") + i;
            outline.transform.localScale += i * ScaleUpAmount;
            AddRigidBody(outline);
            ObjectList.Add(outline);
        }
    }

    void CreateRewards(int NumRewards, Vector3 pos, GameObject container)
    {
        int numCoins = Mathf.Max(currentTrial.NumCoinsLeft, currentTrial.NumCoinsRight);
        float scaler = 1f; //100% for 9 or less
        if(numCoins > 9)
        {
            int num = numCoins;
            while (num > 9)
            {
                scaler -= .0555f;
                num--;
            }
        }

        float width = (Reward.GetComponent<Renderer>().bounds.size.x - .035f) * scaler; //Get Reward width! (-.35f cuz need to be closer together)
        pos -= new Vector3((NumRewards - 1) * (width / 2), 0, 0);
        for (int i = 0; i < NumRewards; i++)
        {
            GameObject RewardClone = Instantiate(Reward, pos, Reward.transform.rotation, container.transform);
            RewardClone.transform.localScale *= scaler;
            RewardClone.transform.Translate(new Vector3(i * width, 0.028f, 0), Space.World);
            RewardClone.name = "Reward" + SideChoice + (i + 1);
            AddRigidBody(RewardClone);
            ObjectList.Add(RewardClone);
        }
    }

    void SetTrialSummaryString()
    {
        TrialSummaryString = "Touches: " + TrialTouches +
                            "\nSide Chosen: " + SideChoice +
                            "\nReward Chosen: " + RewardChoice +
                            "\nEffort Chosen: " + EffortChoice;
    }

    void ClearTrialSummaryString()
    {
        TrialSummaryString = "";
    }

    void DefineTrialData()
    {
        TrialData.AddDatum("ClicksNeeded", () => InflationsNeeded);
        TrialData.AddDatum("ClicksNeededLeft", () => currentTrial.NumClicksLeft);
        TrialData.AddDatum("ClicksNeededRight", () => currentTrial.NumClicksRight);
        TrialData.AddDatum("NumCoinsLeft", () => currentTrial.NumCoinsLeft);
        TrialData.AddDatum("NumCoinsRight", () => currentTrial.NumCoinsRight);
        TrialData.AddDatum("ChosenSide", () => SideChoice);
        TrialData.AddDatum("ChosenEffort", () => EffortChoice);
        TrialData.AddDatum("ChosenReward", () => RewardChoice);
        TrialData.AddDatum("TimeTakenToChoose", () => ChooseDuration);
        TrialData.AddDatum("AverageClickTimes", () => AvgClickTime);
        TrialData.AddDatum("ClicksPerOutline", () => currentTrial.ClicksPerOutline);
        TrialData.AddDatum("TrialTouches", () => TrialTouches);
    }

    void DefineFrameData()
    {
        FrameData.AddDatum("TouchPosition", () => InputBroker.mousePosition);
        FrameData.AddDatum("StartButton", () => StartButton.activeInHierarchy);
        FrameData.AddDatum("StimLeft", () => StimLeft.activeInHierarchy);
        FrameData.AddDatum("StimRight", () => StimRight.activeInHierarchy);
    }

}
