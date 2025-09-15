using System.Collections.Generic;
using ConfigDynamicUI;
using THRQ_Namespace;
using UnityEngine;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Trial;
using USE_States;
using USE_UI;


public class THRQ_TrialLevel : ControlLevel_Trial_Template
{
    public THRQ_TrialDef CurrentTrial => GetCurrentTrialDef<THRQ_TrialDef>();
    public THRQ_TaskLevel CurrentTaskLevel => GetTaskLevel<THRQ_TaskLevel>();
    public THRQ_TaskDef CurrentTask => GetTaskDef<THRQ_TaskDef>();


    private USE_Backdrop USE_Backdrop;
    private GameObject BackdropGO;

    private GameObject StartButton;

    //Set in Inspector
    public GameObject THRQ_CanvasGO;
    public GameObject THRQ_BackdropCanvasGO;

    private float TrialStartTime;
    private float TouchStartTime;
    private float? HeldDuration;
    private float BackdropTouchTime;
    private float BackdropTouches;

    private bool GiveTouchReward;
    private bool GiveReleaseReward;
    private bool GiveReward;
    private bool TimeRanOut;

    [HideInInspector] public List<int> RunningAcc;
    [HideInInspector] public int TrialsCompleted_Block;
    [HideInInspector] public int TrialsCorrect_Block;

    //Data variables:
    [HideInInspector] public int BackdropTouches_Trial;
    [HideInInspector] public int SelectObjectTouches_Trial;
    [HideInInspector] public int NumItiTouches_Trial;
    [HideInInspector] public int NumTouchRewards_Trial;
    [HideInInspector] public int NumReleaseRewards_Trial;
    [HideInInspector] public int NumReleasedEarly_Trial;
    [HideInInspector] public int NumReleasedLate_Trial;

    [HideInInspector] public int BackdropTouches_Block;
    [HideInInspector] public int SelectObjectTouches_Block;
    [HideInInspector] public int NumTouchRewards_Block;
    [HideInInspector] public int NumReleaseRewards_Block;
    [HideInInspector] public int NumItiTouches_Block;
    [HideInInspector] public int NumReleasedEarly_Block;
    [HideInInspector] public int NumReleasedLate_Block;


    private bool MainObjectTouched;
    public bool PerfThresholdMet;
    private bool ConfigValuesChangedInPrevTrial;

    private Color32 DarkBlueColor;
    private Color32 LightBlueColor;

    private float SelectObjectStartTime;
    private float ReactionTime
    {
        get
        {
            return TouchStartTime - TrialStartTime;
        }
    }

    private float RewardEarnedTime;
    private float RewardTimer;



    private GameObject StimGO;
    private ProgressBar ProgressBar;

    private bool SelectionMade;
    private bool SuccessfulSelection;
    private string ErrorType;


    public override void DefineControlLevel()
    {
        State InitTrial = new State("InitTrial");
        State SelectObject = new State("SelectObject");
        State Feedback = new State("Feedback");
        State Reward = new State("Reward");
        State ITI = new State("ITI");
        AddActiveStates(new List<State> { InitTrial, SelectObject, Feedback, Reward, ITI });

        Add_ControlLevel_InitializationMethod(() =>
        {
            DarkBlueColor = new Color32(0, 0, 166, 255);
            LightBlueColor = new Color32(38, 188, 250, 255);

            if (StimGO == null)
            {
                StimGO = Instantiate(Resources.Load<GameObject>("Prefabs/STIM_THRQ"));
                StimGO.name = "StimGO";
                StimGO.transform.position = Vector3.zero;
                StimGO.transform.localScale = new Vector3(5f, 5f, 5f);
                StimGO.SetActive(false);
                ProgressBar = StimGO.AddComponent<ProgressBar>();
                ProgressBar.ManualStart(THRQ_CanvasGO.transform, StimGO);

                USE_Backdrop = gameObject.AddComponent<USE_Backdrop>();
                BackdropGO = USE_Backdrop.CreateBackdrop(THRQ_BackdropCanvasGO.GetComponent<Canvas>(), "BackdropGO", new Color32(6, 10, 17, 255), new Vector2(250f, 150f), new Vector3(0f, 0f, 100f));
            }

            if (StartButton == null)
            {
                if (Session.SessionDef.IsHuman)
                {
                    StartButton = Session.HumanStartPanel.StartButtonGO;
                    Session.HumanStartPanel.SetVisibilityOnOffStates(InitTrial, InitTrial);
                }
                else

                {
                    StartButton = Session.USE_StartButton.CreateStartButton(THRQ_CanvasGO.GetComponent<Canvas>(), CurrentTask.StartButtonPosition, CurrentTask.StartButtonScale);
                    Session.USE_StartButton.SetVisibilityOnOffStates(InitTrial, InitTrial);
                }
            }

            SubscribeToTouchFbEvents();
        });

        //SETUP TRIAL state -------------------------------------------------------------------------------------------------------------------------
        SetupTrial.AddSpecificInitializationMethod(() => StimGO.SetActive(false));
        SetupTrial.SpecifyTermination(() => true, InitTrial);
        SetupTrial.AddDefaultTerminationMethod(() =>
        {
            //HaloFBController.DestroyHalos();
        });

        //INIT TRIAL state --------------------------------------------------------------------------------------------------------------------------
        var ShotgunHandler = Session.SelectionTracker.SetupSelectionHandler("trial", "MouseButton0Click", Session.MouseTracker, InitTrial, SelectObject);
        TouchFBController.EnableTouchFeedback(ShotgunHandler, CurrentTask.TouchFeedbackDuration, CurrentTask.TouchFeedbackSize, THRQ_CanvasGO);

        InitTrial.AddSpecificInitializationMethod(() =>
        {
            TouchFBController.SetPrefabSizes(CurrentTask.StartButtonScale * 30f);

            ResetGlobalTrialVariables();
            SetTrialSummaryString();

            if (TrialCount_InBlock == 0)
                SetConfigValuesToTrialValues();

            LoadConfigUIVariables();
            SetSquareSizeAndPosition();

            CurrentTaskLevel.CalculateBlockSummaryString();

            if (TrialCount_InTask != 0)
                CurrentTaskLevel.SetTaskSummaryString();

            if (ShotgunHandler.AllChoices.Count > 0)
                ShotgunHandler.ClearChoices();
            ShotgunHandler.TimeBeforeChoiceStarts = CurrentTrial.MinTouchDuration;
            ShotgunHandler.TotalChoiceDuration = CurrentTrial.MaxTouchDuration;
        });
        InitTrial.SpecifyTermination(() => true && ShotgunHandler.LastSuccessfulSelectionMatchesStartButton(), SelectObject);

        //SELECT OBJECT state -------------------------------------------------------------------------------------------------------------------------
        SelectObject.AddSpecificInitializationMethod(() =>
        {
            Input.ResetInputAxes();

            TouchFBController.SetPrefabSizes(CurrentTrial.ObjectSize * 10f);

            ProgressBar.minHoldDuration = CurrentTrial.MinTouchDuration;
            ProgressBar.SetProgressBarScale(new Vector2(300f, 40f));
            ProgressBar.ResetProgressBarValue();
            ProgressBar.ActivateProgressBar();

            StimGO.SetActive(true);
            BackdropGO.SetActive(true);

            SelectObjectStartTime = Time.time;
            MainObjectTouched = false;
            BackdropTouchTime = 0;
            BackdropTouches = 0;
            HeldDuration = 0;

            if (ShotgunHandler.AllChoices.Count > 0)
                ShotgunHandler.ClearChoices();

            TrialStartTime = Time.time;

            SelectionMade = false;
            SuccessfulSelection = false;
        });
        SelectObject.AddUpdateMethod(() =>
        {
            if (InputBroker.GetMouseButtonDown(0))
            {
                GameObject hitGO = InputBroker.SimpleRaycast(InputBroker.mousePosition);
                if (hitGO != null)
                {
                    if (hitGO.name == "StimGO") //removed !isGrating from both
                    {
                        if (!MainObjectTouched)
                        {
                            TouchStartTime = Time.time;
                            MainObjectTouched = true;
                        }
                    }

                    if (hitGO.name == "BackdropGO" && !MainObjectTouched && !USE_Backdrop.IsGrating && !TouchFBController.FeedbackOn)
                    {
                        if (BackdropTouches == 0)
                        {
                            AudioFBController.Play("Negative");
                            BackdropTouchTime = Time.time;
                            SelectObjectStartTime += CurrentTrial.TimeoutDuration; //add extra second so it doesn't go straight to white after grating
                            Input.ResetInputAxes();
                            StartCoroutine(USE_Backdrop.GratedFlash(BackdropGO, MovedTooFarSquareTexture, CurrentTrial.TimeoutDuration));
                            BackdropTouches++;
                            BackdropTouches_Trial++;
                        }
                    }
                }
            }

            if (BackdropTouchTime != 0 && (Time.time - BackdropTouchTime) > CurrentTrial.TimeoutDuration)
            {
                BackdropTouches = 0;
                BackdropTouchTime = 0;
            }

            if (Time.time - TrialStartTime > CurrentTrial.TimeToAutoEndTrialSec)
            {
                TimeRanOut = true;
                Session.EventCodeManager.SendCodeThisFrame("NoChoice");
                Session.EventCodeManager.SendRangeCodeThisFrame("CustomAbortTrial", AbortCodeDict["NoSelectionMade"]);
                AbortCode = 6;
            }

            if(ShotgunHandler.LastSuccessfulChoice != null && ShotgunHandler.LastSuccessfulChoice.SelectedGameObject != null)
            {
                if(ShotgunHandler.LastSuccessfulSelectionMatches(StimGO))
                {
                    SuccessfulSelection = true;
                    SelectionMade = true;
                }
            }
        });
        SelectObject.SpecifyTermination(() => SelectionMade, Feedback, () =>
        {
            if(SuccessfulSelection)
            {
                RewardEarnedTime = Time.time;

                AudioFBController.Play("Positive");
                HaloFBController.ShowPositive(StimGO);

                if (CurrentTrial.RewardTouch)
                {
                    SelectObjectTouches_Trial++;
                    GiveTouchReward = true;
                }

                if (CurrentTrial.RewardRelease)
                {
                    SelectObjectTouches_Trial++;
                    GiveReleaseReward = true;
                }
            }
            else
            {
                if (ErrorType == "DurationTooShort")
                {
                    NumReleasedEarly_Trial++;
                }
                else if (ErrorType == "DurationTooLong")
                {
                    NumReleasedLate_Trial++;
                }
                else if (ErrorType == "MovedTooFar")
                {
                    Debug.LogWarning("--MOVED TOO FAR BUT HAVENT IMPLEMENTED ANYTHING YET!");
                }
                else
                    Debug.LogWarning("--ELSE STATEMENT OF ERROR TYPES");
            }
        });
        SelectObject.AddTimer(() => CurrentTrial.SelectObjectDuration, Feedback);

        //FEEDBACK state ----------------------------------------------------------------------------------------------------------------------------
        Feedback.AddSpecificInitializationMethod((() =>
        {
            RewardTimer = Time.time - RewardEarnedTime; //start the timer at the difference between rewardtimeEarned and right now.
            GiveReward = false;
        }));
        Feedback.AddUpdateMethod(() =>
        {
            if ((GiveTouchReward || GiveReleaseReward) && Session.SyncBoxController != null)
            {
                if (RewardTimer < (GiveTouchReward ? CurrentTrial.TouchToRewardDelay : CurrentTrial.ReleaseToRewardDelay))
                    RewardTimer += Time.deltaTime;
                else
                    GiveReward = true;
            }
        });
        Feedback.SpecifyTermination(() => GiveReward, Reward); //If they got right, syncbox isn't null, and timer is met.
        Feedback.SpecifyTermination(() => (GiveTouchReward || GiveReleaseReward) && Session.SyncBoxController == null, ITI); //If they got right, syncbox IS null, don't make them wait.  
        Feedback.SpecifyTermination(() => !GiveTouchReward && !GiveReleaseReward && !USE_Backdrop.IsGrating && !TouchFBController.FeedbackOn, ITI); //if didn't get right, so no pulses. 

        Reward.AddSpecificInitializationMethod(() =>
        {
            if (GiveReleaseReward && Session.SyncBoxController != null)
            {
                StartCoroutine(Session.SyncBoxController.SendRewardPulses(CurrentTrial.NumReleasePulses, CurrentTrial.PulseSize));
                NumReleaseRewards_Trial += CurrentTrial.NumReleasePulses;
                CurrentTaskLevel.NumRewardPulses_InBlock += CurrentTrial.NumReleasePulses;
                CurrentTaskLevel.NumRewardPulses_InTask += CurrentTrial.NumReleasePulses;
            }
            if (GiveTouchReward && Session.SyncBoxController != null)
            {
                StartCoroutine(Session.SyncBoxController.SendRewardPulses(CurrentTrial.NumTouchPulses, CurrentTrial.PulseSize));
                NumTouchRewards_Trial += CurrentTrial.NumTouchPulses;
                CurrentTaskLevel.NumRewardPulses_InBlock += CurrentTrial.NumTouchPulses;
                CurrentTaskLevel.NumRewardPulses_InTask += CurrentTrial.NumTouchPulses;
            }
        });
        Reward.SpecifyTermination(() => true, ITI);

        //ITI state ---------------------------------------------------------------------------------------------------------------------------------
        ITI.AddUpdateMethod(() =>
        {
            if (InputBroker.GetMouseButtonUp(0))
                NumItiTouches_Trial++;
        });
        ITI.AddTimer(() => CurrentTrial.ItiDuration, FinishTrial);
        ITI.AddDefaultTerminationMethod(() =>
        {
            StimGO.SetActive(false);
            ProgressBar.DeactivateProgressBar();

            UpdateData();
            CurrentTaskLevel.CalculateBlockSummaryString();
            // CheckIfBlockShouldEnd();
            ConfigValuesChangedInPrevTrial = ConfigValuesChanged();
        });

        DefineTrialData();
        DefineFrameData();
    }


    //HELPER FUNCTIONS ------------------------------------------------------------------------------------------
    public void SubscribeToTouchFbEvents()
    {
        TouchFBController.TouchErrorFeedbackEvent += OnTouchError;
    }

    public void OnTouchError(object sender, TouchFBController.TouchFeedbackArgs e)
    {
        if(e.Selection.SelectedGameObject == StimGO)
        {
            Debug.LogWarning("DURATION: " + e.Selection.Duration + " | ERROR TYPE: " + e.Selection.ErrorType);
            ErrorType = e.Selection.ErrorType;
            SelectionMade = true;
        };
    }

    private void OnDestroy()
    {
        if(TouchFBController != null)
            TouchFBController.TouchErrorFeedbackEvent -= OnTouchError;
    }

    private void UpdateData()
    {
        SelectObjectTouches_Block += SelectObjectTouches_Trial;
        BackdropTouches_Block += BackdropTouches_Trial;
        NumItiTouches_Block += NumItiTouches_Trial;
        NumTouchRewards_Block += NumTouchRewards_Trial;
        NumReleaseRewards_Block += NumReleaseRewards_Trial;
        NumReleasedEarly_Block += NumReleasedEarly_Trial;
        NumReleasedLate_Block += NumReleasedLate_Trial;

        CurrentTaskLevel.SelectObjectTouches_Task += SelectObjectTouches_Trial;
        CurrentTaskLevel.BackdropTouches_Task += BackdropTouches_Trial;
        CurrentTaskLevel.ItiTouches_Task += NumItiTouches_Trial;
        CurrentTaskLevel.TouchRewards_Task += NumTouchRewards_Trial;
        CurrentTaskLevel.ReleaseRewards_Task += NumReleaseRewards_Trial;
        CurrentTaskLevel.ReleasedEarly_Task += NumReleasedEarly_Trial;
        CurrentTaskLevel.ReleasedLate_Task += NumReleasedLate_Trial;

        if (GiveReleaseReward || GiveTouchReward)
        {
            TrialsCorrect_Block++;
            CurrentTaskLevel.TrialsCorrect_Task++;
        }

        if (GiveTouchReward || GiveReleaseReward)
            RunningAcc.Add(1);
        else
            RunningAcc.Add(0);

        TrialsCompleted_Block++;
        CurrentTaskLevel.TrialsCompleted_Task++;
    }

    public void ResetBlockVariables()
    {
        RunningAcc.Clear();
        TrialsCompleted_Block = 0;
        TrialsCorrect_Block = 0;
        BackdropTouches_Block = 0;
        SelectObjectTouches_Block = 0;
        NumItiTouches_Block = 0;
        NumTouchRewards_Block = 0;
        NumReleaseRewards_Block = 0;
        NumReleasedEarly_Block = 0;
        NumReleasedLate_Block = 0;
        PerfThresholdMet = false;
    }

    public override void ResetTrialVariables()
    {
        GiveReleaseReward = false;
        GiveTouchReward = false;
        TimeRanOut = false;
        BackdropTouches_Trial = 0;
        SelectObjectTouches_Trial = 0;
        NumReleasedEarly_Trial = 0;
        NumReleasedLate_Trial = 0;
        NumItiTouches_Trial = 0;
        TouchStartTime = 0;
        HeldDuration = 0;
        NumTouchRewards_Trial = 0;
        NumReleaseRewards_Trial = 0;
    }

    public override void FinishTrialCleanup()
    {
        if (AbortCode != 0)
        {
            CurrentTaskLevel.NumAbortedTrials_InBlock++;
            CurrentTaskLevel.NumAbortedTrials_InTask++;
        }
    }

    private void SetTrialSummaryString()
    {
        TrialSummaryString = "Reward Protocol: " + (CurrentTrial.RewardTouch ? "Touch" : "Release") +
                              "\nRandom Position: " + ((CurrentTrial.RandomObjectPosition ? "True" : "False")) +
                              "\nRandom Size: " + ((CurrentTrial.RandomObjectSize ? "True" : "False"));
    }

    protected override bool CheckBlockEnd()
    {
        // Using Simple Threshold Block End
        return CurrentTaskLevel.TaskLevel_Methods.CheckBlockEnd("SimpleThreshold", RunningAcc,
             CurrentTrial.PerfThresholdEndTrials, CurrentTrial.PerfWindowEndTrials, CurrentTaskLevel.MinTrials_InBlock,
            CurrentTrial.MaxTrials);

    }

    private void LoadConfigUIVariables()
    {
        CurrentTrial.MinTouchDuration = ConfigUiVariables.get<ConfigNumber>("minTouchDuration").value;
        CurrentTrial.MaxTouchDuration = ConfigUiVariables.get<ConfigNumber>("maxTouchDuration").value;
        CurrentTrial.ObjectSize = ConfigUiVariables.get<ConfigNumber>("objectSize").value;
        CurrentTrial.PositionX = (int)ConfigUiVariables.get<ConfigNumber>("positionX").value;
        CurrentTrial.PositionY = (int)ConfigUiVariables.get<ConfigNumber>("positionY").value;
        CurrentTrial.SelectObjectDuration = ConfigUiVariables.get<ConfigNumber>("selectObjectDuration").value;
    }

    private bool ConfigValuesChanged()
    {
        if (CurrentTrial.ObjectSize != ConfigUiVariables.get<ConfigNumber>("objectSize").value
            || CurrentTrial.PositionX != ConfigUiVariables.get<ConfigNumber>("positionX").value
            || CurrentTrial.PositionY != ConfigUiVariables.get<ConfigNumber>("positionY").value)
        {
            return true;
        }
        else
            return false;
    }

    private void SetConfigValuesToTrialValues()
    {
        ConfigUiVariables.get<ConfigNumber>("objectSize").SetValue(CurrentTrial.ObjectSize);
        ConfigUiVariables.get<ConfigNumber>("positionX").SetValue(CurrentTrial.PositionX);
        ConfigUiVariables.get<ConfigNumber>("positionY").SetValue(CurrentTrial.PositionY);
        ConfigUiVariables.get<ConfigNumber>("minTouchDuration").SetValue(CurrentTrial.MinTouchDuration);
        ConfigUiVariables.get<ConfigNumber>("maxTouchDuration").SetValue(CurrentTrial.MaxTouchDuration);
        ConfigUiVariables.get<ConfigNumber>("selectObjectDuration").SetValue(CurrentTrial.SelectObjectDuration);
    }

    private void SetSquareSizeAndPosition()
    {
        if (CurrentTrial.RandomObjectSize && !ConfigValuesChangedInPrevTrial)
        {
            float randomSize = Random.Range(CurrentTrial.ObjectSizeMin, CurrentTrial.ObjectSizeMax);
            StimGO.transform.localScale = new Vector3(randomSize, randomSize, randomSize);
            ConfigUiVariables.get<ConfigNumber>("objectSize").SetValue(randomSize);
            CurrentTrial.ObjectSize = randomSize;
        }
        else
        {
            StimGO.transform.localScale = new Vector3(CurrentTrial.ObjectSize, CurrentTrial.ObjectSize, CurrentTrial.ObjectSize);
        }

        if (CurrentTrial.RandomObjectPosition && !ConfigValuesChangedInPrevTrial)
        {
            int x = Random.Range(CurrentTrial.PositionX_Min, CurrentTrial.PositionX_Max);
            int y = Random.Range(CurrentTrial.PositionY_Min, CurrentTrial.PositionY_Max);
            StimGO.transform.position = new Vector3(x, y, 0);
            ConfigUiVariables.get<ConfigNumber>("positionX").SetValue(x);
            ConfigUiVariables.get<ConfigNumber>("positionY").SetValue(y);
            CurrentTrial.PositionX = x;
            CurrentTrial.PositionY = y;
        }
        else
        {
            StimGO.transform.position = new Vector3(CurrentTrial.PositionX, CurrentTrial.PositionY, 0f);
        }
    }


    private void ResetGlobalTrialVariables()
    {
        GiveReleaseReward = false;
        GiveTouchReward = false;
        TimeRanOut = false;
        BackdropTouches_Trial = 0;
        SelectObjectTouches_Trial = 0;
        NumItiTouches_Trial = 0;
        TouchStartTime = 0;
        HeldDuration = 0;
        NumTouchRewards_Trial = 0;
        NumReleaseRewards_Trial = 0;
    }

    private void DefineTrialData()
    {

        TrialData.AddDatum("TrialID", () => CurrentTrial.TrialID);
        TrialData.AddDatum("ObjectSize", () => CurrentTrial.ObjectSize);
        TrialData.AddDatum("PosX", () => CurrentTrial.PositionX);
        TrialData.AddDatum("PosY", () => CurrentTrial.PositionY);
        TrialData.AddDatum("MinTouchDuration", () => CurrentTrial.MinTouchDuration);
        TrialData.AddDatum("MaxTouchDuration", () => CurrentTrial.MaxTouchDuration);
        TrialData.AddDatum("RewardTouch", () => CurrentTrial.RewardTouch);
        TrialData.AddDatum("RewardRelease", () => CurrentTrial.RewardRelease);
        TrialData.AddDatum("DifficultyLevel", () => CurrentTrial.TrialID);
        TrialData.AddDatum("SelecObjectTouches_Trial", () => SelectObjectTouches_Trial);
        TrialData.AddDatum("BackdropTouches_Trial", () => BackdropTouches_Trial);
        TrialData.AddDatum("ItiTouches_Trial", () => NumItiTouches_Trial);
        TrialData.AddDatum("ReactionTime", () => ReactionTime);
        TrialData.AddDatum("TouchStartTime", () => TouchStartTime);
        TrialData.AddDatum("HeldDuration", () => HeldDuration);
    }

    private void DefineFrameData()
    {
        FrameData.AddDatum("TouchPosition", () => InputBroker.mousePosition);
        FrameData.AddDatum("MainObjectGO", () => StimGO?.activeInHierarchy);
    }

}
