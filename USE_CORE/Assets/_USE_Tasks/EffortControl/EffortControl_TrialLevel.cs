using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using USE_States;
using EffortControl_Namespace;
using System.Linq;
using USE_ExperimentTemplate_Trial;
using ConfigDynamicUI;
using USE_UI;
using UnityEngine.UI;


public class EffortControl_TrialLevel : ControlLevel_Trial_Template
{
    public EffortControl_TrialDef currentTrial => GetCurrentTrialDef<EffortControl_TrialDef>();
    public EffortControl_TaskLevel currentTask => GetTaskLevel<EffortControl_TaskLevel>();

    //Prefabs to Instantiate:
    public GameObject StimNoMaterialPrefab;
    public GameObject StimLeftPrefab;
    public GameObject StimRightPrefab;
    public GameObject RewardPrefab;
    public GameObject OutlinePrefab;

    public Vector3 ButtonPosition;
    public float ButtonScale;
    public Vector3 OriginalStartButtonPosition;

    public GameObject EC_CanvasGO;
    public USE_StartButton StartButtonClassInstance;

    private GameObject StartButton, StimLeft, StimRight, TrialStim, BalloonContainerLeft, BalloonContainerRight,
               BalloonOutline, RewardContainerLeft, RewardContainerRight, Reward, MiddleBarrier, Borders;

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

    //Set in task level:
    [HideInInspector] public bool IsHuman;
    [HideInInspector] public string MaterialFilePath;

    [System.NonSerialized] public int Response = -1;
    private int ClicksNeeded; //becomes left/right num clicks once they make selection. 
    private int ClickCount;
    private bool AddTokenInflateAudioPlayed;
    private bool ObjectsCreated;
    [HideInInspector] private List<GameObject> RemoveParentList;
    [HideInInspector] private GameObject Wrapper;

    private string SideChoice; //left or right
    private string RewardChoice; //higher or lower
    private string EffortChoice; //higher or lower

    //To center the balloon they selected:
    public float CenteringSpeed;
    private Vector3 CenteredPos;
    [HideInInspector] public bool Flashing;

    //Variables to Inflate balloon at interval rate
    private float InflateClipDuration;
    private bool Inflate;
    private readonly float MaxInflation_Y = 35f;
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

    [HideInInspector] public ConfigNumber scalingInterval, inflateDuration, itiDuration, popToFeedbackDelay, choiceToTouchDelay, sbToBalloonDelay; //ScalingInterval is used for balloonInflation!

    [HideInInspector] public GameObject MaxOutline_Left;
    [HideInInspector] public GameObject MaxOutline_Right;

    [HideInInspector] public bool InflateAudioPlayed;

    [HideInInspector] public List<float> clickTimings;
    [HideInInspector] public float timeTracker;
    [HideInInspector] public int mouseClicks;

    [HideInInspector] public float BalloonSelectedTime;
    [HideInInspector] public float StartButtonSelectedTime;

    [HideInInspector] public int TrialTouches;

    [HideInInspector] public List<GameObject> ObjectList;

    public override void DefineControlLevel()
    {
        State InitTrial = new State("InitTrial");
        State ChooseBalloon = new State("ChooseBalloon");
        State CenterSelection = new State("CenterSelection");
        State InflateBalloon = new State("InflateBalloon");
        State Feedback = new State("Feedback");
        State ITI = new State("ITI");
        AddActiveStates(new List<State> { InitTrial, ChooseBalloon, CenterSelection, InflateBalloon, Feedback, ITI });

        SelectionHandler<EffortControl_StimDef> mouseHandler = new SelectionHandler<EffortControl_StimDef>();

        Add_ControlLevel_InitializationMethod(() =>
        {
            //potentially refactor this?
            LoadTextures(MaterialFilePath);
  
            //can we move SetTokenVariables into TokenFbController class?
            if(TokenFBController != null)
                SetTokenVariables();

            if(AudioFBController != null)
                InflateClipDuration = AudioFBController.GetClip("EC_Inflate").length;
            
            if (StartButton == null)
            {
                StartButtonClassInstance = new USE_StartButton(EC_CanvasGO.GetComponent<Canvas>(), ButtonPosition, ButtonScale);
                StartButton = StartButtonClassInstance.StartButtonGO;
            }

            if (!ObjectsCreated)
                CreateObjects();

            CenteringSpeed = 1.5f;
        });

        //SETUP TRIAL state -----------------------------------------------------------------------------------------------------
        SetupTrial.AddInitializationMethod(() =>
        {
            LoadConfigUIVariables();
            EventCodeManager.SendCodeImmediate(TaskEventCodes["TrlStart"]);
        });
        SetupTrial.SpecifyTermination(() => true, InitTrial);

        MouseTracker.AddSelectionHandler(mouseHandler, InitTrial);
        //INIT Trial state -------------------------------------------------------------------------------------------------------
        InitTrial.AddInitializationMethod(() =>
        {
            if (!Borders.activeInHierarchy)
                Borders.SetActive(true);

            TokenFBController.enabled = false;
            ResetRelativeStartTime(); 
            DisableAllGameobjects();
            StartButton.SetActive(true);

            ResetToOriginalPositions();

            if(TrialStim != null)
            {
                TrialStim.transform.localScale = TrialStimInitLocalScale;
                TrialStim = null;
            }
        });
        InitTrial.SpecifyTermination(() => mouseHandler.SelectionMatches(StartButton), Delay, () =>
        {
            DelayDuration = sbToBalloonDelay.value;
            StateAfterDelay = ChooseBalloon;
            StartButton.SetActive(false);
            EventCodeManager.SendCodeImmediate(TaskEventCodes["StartButtonSelected"]);
        });

        //Choose Balloon state -------------------------------------------------------------------------------------------------------
        //should automatically have selection handlers for mouse, gaze, touch in session/task/trial levels
        MouseTracker.AddSelectionHandler(mouseHandler, ChooseBalloon);
        ChooseBalloon.AddInitializationMethod(() =>
        {
            Input.ResetInputAxes(); //reset input in case they holding down
            //maybe this should be a method in selectionhandlers?

            MaxScale = new Vector3(60, 0, 60);
            LeftScaleUpAmount = MaxScale / currentTrial.NumClicksLeft;
            RightScaleUpAmount = MaxScale / currentTrial.NumClicksRight;

            CreateBalloonOutlines(currentTrial.NumClicksLeft, LeftScaleUpAmount, currentTrial.ClicksPerOutline, StimLeft.transform.position, BalloonContainerLeft);
            CreateBalloonOutlines(currentTrial.NumClicksRight, RightScaleUpAmount, currentTrial.ClicksPerOutline, StimRight.transform.position, BalloonContainerRight);
            CreateRewards(currentTrial.NumCoinsLeft, RewardContainerLeft.transform.position, RewardContainerLeft);
            CreateRewards(currentTrial.NumCoinsRight, RewardContainerRight.transform.position, RewardContainerRight);
            CreateTransparentBalloons();
            ActivateObjects();

            SideChoice = null;
        });

        ChooseBalloon.AddUpdateMethod(() =>
        {
            GameObject hit = mouseHandler.SelectedGameObject;
            if (hit != null)
            {
                BalloonSelectedTime = Time.time;
                if (hit.transform.name.Contains("Left"))
                {
                    SideChoice = "Left";
                    TrialStim = StimLeft;
                }
                else if (hit.transform.name.Contains("Right"))
                {
                    SideChoice = "Right";
                    TrialStim = StimRight;
                }
            }

            //this will be move to selectionhandler / inputtrackers
            //Neg FB if touch outside balloon. Adding response != 1 so that they cant click outside balloon at the end and mess up pop audio.
            if (InputBroker.GetMouseButtonDown(0) && SideChoice == null)
            {
                Ray ray = Camera.main.ScreenPointToRay(InputBroker.mousePosition);
                RaycastHit hitt;
                if (!Physics.Raycast(ray, out hitt))
                {
                    AudioFBController.audioSource.Stop();
                    AudioFBController.Play("Negative");
                    //implement stopping previous sounds or waiting until they end in AudioFeedbackController
                }
            }

        });
        ChooseBalloon.SpecifyTermination(() => SideChoice != null, CenterSelection, () =>
        {
            EventCodeManager.SendCodeImmediate(TaskEventCodes["BalloonChosen"]);

            DestroyChildren(SideChoice == "Left" ? RewardContainerRight : RewardContainerLeft);
            ClicksNeeded = (SideChoice == "Left" ? currentTrial.NumClicksLeft : currentTrial.NumClicksRight);
            AudioFBController.Play("EC_BalloonChosen");
            RecordChoices();
            SetTrialSummaryString();
        });

        //Center Selection state -------------------------------------------------------------------------------------------------------
        CenterSelection.AddInitializationMethod(() =>
        {
            ChooseDuration = ChooseBalloon.TimingInfo.Duration;

            Wrapper = new GameObject();
            Wrapper.name = "Wrapper";
            CenteredPos = new Vector3((SideChoice == "Left" ? 1f : -1f), 0, 0);

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
        MouseTracker.AddSelectionHandler(mouseHandler, InflateBalloon);

        InflateBalloon.AddInitializationMethod(() =>
        {
            ScalePerInflation_Y = (MaxInflation_Y - TrialStim.transform.localScale.y) / (SideChoice == "Left" ? currentTrial.NumClicksLeft : currentTrial.NumClicksRight);
            timeTracker = Time.time;
            IncrementAmounts = new Vector3();
            Flashing = false;
            InflateAudioPlayed = false;
            ScaleTimer = 0;
            MouseTracker.ResetClickCount();
            clickTimings = new List<float>();
            timeTracker = 0;
            mouseClicks = 0;
        });
        InflateBalloon.AddUpdateMethod(() =>
        {
            if (Inflate)
            {
                if (!InflateAudioPlayed)
                {
                    if (AudioFBController.IsPlaying())
                        AudioFBController.audioSource.Stop();
                    AudioFBController.Play("EC_Inflate");
                    EventCodeManager.SendCodeImmediate(TaskEventCodes["SelectionAuditoryFbOn"]);
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
                        mouseHandler.Start();
                        if (ClickCount >= ClicksNeeded)
                        {
                            Response = 1;
                            AvgClickTime = clickTimings.Average();
                        }
                    }

                    ScaleTimer = 0; //Reset Timer for next inflation increment;
                }
            }

            if (mouseHandler.SelectionMatches(TrialStim) || mouseHandler.SelectionMatches(MaxOutline_Left) ||
                mouseHandler.SelectionMatches(MaxOutline_Right))
            {
                Input.ResetInputAxes();
                ClickCount++;
                clickTimings.Add(Time.time - timeTracker);
                timeTracker = Time.time;
                mouseHandler.Stop();

                CalculateInflation(); //Sets Inflate to TRUE at end of func
                InflateAudioPlayed = false;
            }

            if (mouseClicks != MouseTracker.GetClickCount())
            {
                TrialTouches += MouseTracker.GetClickCount() - mouseClicks;
                SetTrialSummaryString();
                mouseClicks = MouseTracker.GetClickCount();
            }

            //Neg FB if touch outside balloon. Adding response != 1 so that they cant click outside balloon at the end and mess up pop audio.
            if (InputBroker.GetMouseButtonDown(0) && Response != 1)
            {
                //should also be handled by inputbroker
                Ray ray = Camera.main.ScreenPointToRay(InputBroker.mousePosition);
                RaycastHit hit;
                if (!Physics.Raycast(ray, out hit))
                {
                    if (AudioFBController.IsPlaying())
                        AudioFBController.audioSource.Stop();
                    AudioFBController.Play("Negative");
                    EventCodeManager.SendCodeImmediate(TaskEventCodes["SelectionAuditoryFbOn"]);
                }
            }
        });
        InflateBalloon.AddTimer(() => inflateDuration.value, Delay);
        InflateBalloon.SpecifyTermination(() => Response == 1, Delay);
        InflateBalloon.AddDefaultTerminationMethod(() =>
        {
            StateAfterDelay = Feedback;
            DelayDuration = popToFeedbackDelay.value;
            //add trial touches to total touches:
            TotalTouches_Block += TrialTouches;

            if (SideChoice == "Left")
                MaxOutline_Left.transform.parent = BalloonContainerLeft.transform;
            else
                MaxOutline_Right.transform.parent = BalloonContainerRight.transform;

            DestroyChildren(SideChoice == "Left" ? BalloonContainerLeft : BalloonContainerRight);
            InflateAudioPlayed = false;
            
            
            if (Response == 1)
            {
                AudioFBController.audioSource.Stop();
                if(IsHuman)
                    AudioFBController.Play("EC_HarshPop"); //better for humans
                else
                    AudioFBController.Play("EC_NicePop"); //better for monkeys
            }
            else
            {
                NumAborted_Block++;
                AudioFBController.Play("TimeRanOut");
                TokenFBController.enabled = false;
                EventCodeManager.SendCodeImmediate(TaskEventCodes["NoChoice"]);
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

                if (SyncBoxController != null)
                {
                    GiveReward();
                    EventCodeManager.SendCodeNextFrame(TaskEventCodes["Rewarded"]);
                }

                Completions_Block++;
                AddTokenInflateAudioPlayed = true;
            }
            else
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["Unrewarded"]);
        });
        Feedback.SpecifyTermination(() => AddTokenInflateAudioPlayed && !AudioFBController.IsPlaying() && !TokenFBController.IsAnimating(), ITI);
        Feedback.SpecifyTermination(() => true && Response != 1, ITI);
        Feedback.AddDefaultTerminationMethod(() =>
        {
            TokenFBController.enabled = false;
            AddTokenInflateAudioPlayed = false;
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["TrlEnd"]);
        });

        //ITI state -------------------------------------------------------------------------------------------------------
        ITI.AddTimer(itiDuration.value, FinishTrial);
        
        DefineTrialData();
        DefineFrameData();
    }

    //HELPER FUNCTIONS -------------------------------------------------------------------------------------------------------
    public override void ResetTrialVariables()
    {
        ClickCount = 0;
        Response = -1;
        ChooseDuration = 0;
        ClicksNeeded = 0;
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

        if (AbortCode == AbortCodeDict["RestartBlock"] || AbortCode == AbortCodeDict["PreviousBlock"]) //If used RestartBlock or PreviousBlock hotkeys
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
        TrialStim.transform.localScale = new Vector3(TrialStim.transform.localScale.x + IncrementAmounts.x, TrialStim.transform.localScale.y + IncrementAmounts.y, TrialStim.transform.localScale.z + IncrementAmounts.z);
    }

    void CalculateInflation()
    {      
        GameObject container = (SideChoice == "Left") ? BalloonContainerLeft : BalloonContainerRight;
        NextScale = container.transform.GetChild(ClickCount-1).transform.localScale;
        NextScale.y = ScalePerInflation_Y + TrialStim.transform.localScale.y;
        Vector3 difference = NextScale - TrialStim.transform.localScale;
        IncrementAmounts = new Vector3((difference.x / scalingInterval.value), (difference.y / scalingInterval.value), (difference.z / scalingInterval.value));

        Inflate = true;
    }

    IEnumerator FlashOutline()
    {
        Flashing = true;
        GameObject container = (SideChoice == "Left") ? BalloonContainerLeft : BalloonContainerRight;

        GameObject child = container.transform.GetChild(ClickCount).transform.gameObject;
        if (child != null && Response != 1 && !Inflate)
        {
            child.transform.GetComponent<Renderer>().material.color = Color.red;
            yield return new WaitForSeconds(.5f);
            if(child != null)
            {
                child.transform.GetComponent<Renderer>().material.color = OffWhiteOutlineColor;
                yield return new WaitForSeconds(.5f);
            }
        }
        Flashing = false;
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

    void SetTokenVariables()
    {
        TokenFBController.SetFlashingTime(1.5f);
        TokenFBController.tokenBoxYOffset = 20;
        TokenFBController.tokenSize = 105;
        TokenFBController.tokenSpacing = -18;
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
            SyncBoxController.SendRewardPulses(currentTrial.NumPulsesLeft, currentTrial.PulseSizeLeft);
            SessionInfoPanel.UpdateSessionSummaryValues(("totalRewardPulses",currentTrial.NumPulsesLeft));
            RewardPulses_Block += currentTrial.NumPulsesLeft;
        }
        else
        {
            SyncBoxController.SendRewardPulses(currentTrial.NumPulsesRight, currentTrial.PulseSizeRight);
            SessionInfoPanel.UpdateSessionSummaryValues(("totalRewardPulses",currentTrial.NumPulsesRight));
            RewardPulses_Block += currentTrial.NumPulsesRight;
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
        Red = StimLeft.GetComponent<Renderer>().material.color;
        StimLeft.GetComponent<Renderer>().material.color = Red;
        TrialStimInitLocalScale = StimLeft.transform.localScale;
        AddRigidBody(StimLeft);
        ObjectList.Add(StimLeft);

        StimRight = Instantiate(StimRightPrefab, StimRightPrefab.transform.position, StimRightPrefab.transform.rotation);
        StimRight.name = "StimRight";
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

        List<GameObject> borderList = new List<GameObject>();

        Borders = new GameObject("Borders");

        //can we replace all this with a single UI square with solid border and no fill?
        
        GameObject topBorder = GameObject.CreatePrimitive(PrimitiveType.Cube);
        topBorder.name = "TopBorder";
        topBorder.transform.parent = Borders.transform;
        topBorder.transform.position = new Vector3(0, -.005f, 0);
        topBorder.transform.eulerAngles = new Vector3(0, 0, 90f);
        topBorder.transform.localScale = new Vector3(.075f, 3.995f, .001f);
        borderList.Add(topBorder);

        GameObject rightBorder = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightBorder.name = "RightBorder";
        rightBorder.transform.parent = Borders.transform;
        rightBorder.transform.position = new Vector3(2.035f, -1.157f, 0);
        rightBorder.transform.eulerAngles = Vector3.zero;
        rightBorder.transform.localScale = new Vector3(.075f, 2.35f, .001f);
        borderList.Add(rightBorder);

        GameObject leftBorder = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftBorder.name = "LeftBorder";
        leftBorder.transform.parent = Borders.transform;
        leftBorder.transform.position = new Vector3(-2.035f, -1.159f, 0);
        leftBorder.transform.eulerAngles = Vector3.zero;
        leftBorder.transform.localScale = new Vector3(.075f, 2.35f, .001f);
        borderList.Add(leftBorder);

        GameObject bottomBorder = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bottomBorder.name = "BottomBorder";
        bottomBorder.transform.parent = Borders.transform;
        bottomBorder.transform.position = new Vector3(0, -2.3f, 0);
        bottomBorder.transform.eulerAngles = new Vector3(0, 0, 90f);
        bottomBorder.transform.localScale = new Vector3(.075f, 3.995f, .001f);
        borderList.Add(bottomBorder);

        Borders.transform.position = new Vector3(0, 1.755f, 0);
        ObjectList.Add(Borders);

        //replace with ui thing
        MiddleBarrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
        MiddleBarrier.name = "MiddleBarrier";
        MiddleBarrier.transform.position = new Vector3(0, .602f, 0);
        MiddleBarrier.transform.localScale = new Vector3(.0125f, 2.22f, .001f);
        ObjectList.Add(MiddleBarrier);
        borderList.Add(MiddleBarrier);

        foreach (GameObject border in borderList)
        {
            Material mat = border.GetComponent<Renderer>().material;
            mat.color = OffWhiteOutlineColor;
            mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            mat.SetFloat("_SpecularHighlights", 0f);
        }

        //pontentially replace with prefab?
        BalloonContainerLeft = new GameObject("BalloonContainerLeft");
        BalloonContainerLeft.transform.position = new Vector3(-1, .15f, .5f);
        BalloonContainerLeft.transform.localScale = new Vector3(1, 1, 1);
        ObjectList.Add(BalloonContainerLeft);

        BalloonContainerRight = new GameObject("BalloonContainerRight");
        BalloonContainerRight.transform.position = new Vector3(1, .15f, .5f);
        BalloonContainerRight.transform.localScale = new Vector3(1, 1, 1);
        ObjectList.Add(BalloonContainerRight);

        RewardContainerLeft = new GameObject("RewardContainerLeft");
        RewardContainerLeft.transform.position = new Vector3(-.85f, 1.6f, 0);
        RewardContainerLeft.transform.localScale = new Vector3(1, 1, 1);
        ObjectList.Add(RewardContainerLeft);

        RewardContainerRight = new GameObject("RewardContainerRight");
        RewardContainerRight.transform.position = new Vector3(.85f, 1.6f, 0);
        RewardContainerRight.transform.localScale = new Vector3(1, 1, 1);
        ObjectList.Add(RewardContainerRight);


        //Wrap Left side objects in container and Center to left side:
        GameObject leftWrapper = new GameObject();
        leftWrapper.name = "LeftWrapper";
        List<GameObject> leftList = new List<GameObject>() { BalloonContainerLeft, RewardContainerLeft, StimLeft };
        SetParents(leftWrapper, leftList);
        leftWrapper.transform.position = new Vector3(-.16f, 0, 0); //Centering on left half of screen. 

        //Wrap Right side objects in container and Center to right side:
        GameObject rightWrapper = new GameObject();
        rightWrapper.name = "RightWrapper";
        List<GameObject> rightList = new List<GameObject>() { BalloonContainerRight, RewardContainerRight, StimRight };
        SetParents(rightWrapper, rightList);
        rightWrapper.transform.position = new Vector3(.16f, 0, 0); //Centering on right half of screen. 

        LeftContainerOriginalPosition = BalloonContainerLeft.transform.position;
        RightContainerOriginalPosition = BalloonContainerRight.transform.position;
        LeftRewardContainerOriginalPosition = RewardContainerLeft.transform.position;
        RightRewardContainerOriginalPosition = RewardContainerRight.transform.position;
        LeftStimOriginalPosition = StimLeft.transform.position;
        RightStimOriginalPosition = StimRight.transform.position;

        //now that positions are set, remove parents so the balloon is clickable. 
        RemoveParents(leftWrapper, leftList);
        RemoveParents(rightWrapper, rightList);

        MiddleBarrier.SetActive(false);
        StartButton.SetActive(false);
        BalloonOutline.SetActive(false);
        Reward.SetActive(false);
        Borders.SetActive(false);

        ObjectsCreated = true;
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

    void CreateBalloonOutlines(int numBalloons, Vector3 ScaleUpAmount, int clickPerOutline, Vector3 pos, GameObject container)
    {
        for (int i = clickPerOutline; i <= numBalloons; i += clickPerOutline)
        {
            GameObject outline = Instantiate(BalloonOutline, pos, BalloonOutline.transform.rotation);
            outline.transform.parent = container.transform;
            outline.name = "Outline" + container.name == "BalloonContainerLeft" ? "Left" : "Right" + (i);
            outline.transform.localScale += (i) * ScaleUpAmount;
            AddRigidBody(outline);
            ObjectList.Add(outline);
        }
    }

    void CreateRewards(int NumRewards, Vector3 pos, GameObject container)
    {
        float width = Reward.GetComponent<Renderer>().bounds.size.x - .035f; //Get Reward width! (-.35f cuz need to be closer together)
        pos -= new Vector3(((NumRewards - 1) * (width / 2)), 0, 0);
        for (int i = 0; i < NumRewards; i++)
        {
            GameObject RewardClone = Instantiate(Reward, pos, Reward.transform.rotation, container.transform);
            RewardClone.transform.Translate(new Vector3(i * width, 0, 0), Space.World);
            RewardClone.name = "Reward" + SideChoice + (i + 1);
            AddRigidBody(RewardClone);
            ObjectList.Add(RewardClone);
        }
    }

    void SetTrialSummaryString()
    {
        TrialSummaryString = ("<b>Trial Info:</b>" +
                            "\nTouches: " + TrialTouches +
                            "\nSide Chosen: " + SideChoice +
                            "\nReward Chosen: " + RewardChoice +
                            "\nEffort Chosen: " + EffortChoice);
    }

    void ClearTrialSummaryString()
    {
        TrialSummaryString = "";
    }

    void DefineTrialData()
    {
        TrialData.AddDatum("ClicksNeeded", () => ClicksNeeded);
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
        TrialData.AddDatum("Trial TrialTouches", () => TrialTouches);
    }

    void DefineFrameData()
    {
        FrameData.AddDatum("TouchPosition", () => InputBroker.mousePosition);
        FrameData.AddDatum("StartButton", () => StartButton.activeInHierarchy);
        FrameData.AddDatum("StimLeft", () => StimLeft.activeInHierarchy);
        FrameData.AddDatum("StimRight", () => StimRight.activeInHierarchy);
    }

}
