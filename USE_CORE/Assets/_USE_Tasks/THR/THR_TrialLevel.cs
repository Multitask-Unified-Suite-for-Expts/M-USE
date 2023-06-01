using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using ConfigDynamicUI;
using System.Collections;
using USE_ExperimentTemplate_Trial;
using USE_States;
using THR_Namespace;
using UnityEngine.EventSystems;
using USE_UI;
using UnityEngine.UI;

public class THR_TrialLevel : ControlLevel_Trial_Template
{
    public THR_TrialDef currentTrial => GetCurrentTrialDef<THR_TrialDef>();
    public THR_TaskLevel currentTask => GetTaskLevel<THR_TaskLevel>();

    [HideInInspector] public string MaterialFilePath;

    private USE_StartButton USE_Square;
    private GameObject SquareGO;
    private USE_Backdrop USE_Backdrop;
    private GameObject BackdropGO;

    private GameObject StartButton;

    private Renderer BackdropRenderer;
    private Renderer SquareRenderer;
    private Texture SquareTexture;
    private Material SquareMaterial;
    public GameObject THR_CanvasGO;

    private float TrialStartTime;
    private float TouchStartTime;
    private float? HeldDuration;
    private float BackdropTouchTime;
    private float BackdropTouches;

    private bool GiveTouchReward;
    private bool GiveReleaseReward;
    private bool GiveReward;
    private bool TimeRanOut;

    [HideInInspector] public Vector3 ButtonPosition;
    [HideInInspector] public float ButtonScale;

    [HideInInspector] public List<int> TrialCompletionList;
    [HideInInspector] public int TrialsCompleted_Block;
    [HideInInspector] public int TrialsCorrect_Block;

    //Data variables:
    [HideInInspector] public int BackdropTouches_Trial;
    [HideInInspector] public int BlueSquareTouches_Trial;
    [HideInInspector] public int WhiteSquareTouches_Trial;
    [HideInInspector] public int ItiTouches_Trial;
    [HideInInspector] public int TouchRewards_Trial;
    [HideInInspector] public int ReleaseRewards_Trial;
    [HideInInspector] public int NumTouchesMovedOutside_Trial;

    [HideInInspector] public int BackdropTouches_Block;
    [HideInInspector] public int BlueSquareTouches_Block;
    [HideInInspector] public int WhiteSquareTouches_Block;
    [HideInInspector] public int NumTouchRewards_Block;
    [HideInInspector] public int NumReleaseRewards_Block;
    [HideInInspector] public int NumItiTouches_Block;
    [HideInInspector] public int NumReleasedEarly_Block;
    [HideInInspector] public int NumReleasedLate_Block;
    [HideInInspector] public int NumTouchesMovedOutside_Block;


    private bool BlueSquareTouched;
    private bool BlueSquareReleased;
    private bool AudioPlayed;
    private bool HeldTooShort;
    private bool HeldTooLong;
    public bool PerfThresholdMet;
    private bool MovedOutside;
    private bool ConfigValuesChangedInPrevTrial;

    private Color32 GreenColor;

    private float WhiteTimeoutTime;
    private float WhiteStartTime;
    private float BlueStartTime;
    private float ReactionTime
    {
        get
        {
            return TouchStartTime - TrialStartTime;
        }
    }

    private float RewardEarnedTime;
    private float RewardTimer;

    [HideInInspector] public bool StartWithBlueSquare;

    [HideInInspector] public float TouchFeedbackDuration;

    [HideInInspector] public GameObject StartButtonCoverCircle;
    [HideInInspector] public Image StartButtonCoverCircle_Image;
    [HideInInspector] public GameObject StartButtonPlayIcon;


    public override void DefineControlLevel()
    {
        State InitTrial = new State("InitTrial");
        State WhiteSquare = new State("WhiteSquare");
        State BlueSquare = new State("BlueSquare");
        State Feedback = new State("Feedback");
        State Reward = new State("Reward");
        State ITI = new State("ITI");
        AddActiveStates(new List<State> { InitTrial, WhiteSquare, BlueSquare, Feedback, Reward, ITI});

        Add_ControlLevel_InitializationMethod(() =>
        {
            GreenColor = new Color32(0, 175, 0, 255);

            if (SquareGO == null)
            {
                USE_Backdrop = new USE_Backdrop();
                BackdropGO = USE_Backdrop.CreateBackdrop(THR_CanvasGO.GetComponent<Canvas>(), "BackdropGO", new Color32(0, 9, 16, 0));
                USE_Square = new USE_StartButton();
                SquareGO = USE_Square.CreateStartButton(THR_CanvasGO.GetComponent<Canvas>(), "SquareGO", false);
                StartButtonPlayIcon = SquareGO.transform.Find("PlayIcon").gameObject;
                StartButtonCoverCircle = USE_Square.CreateCoverCircle();
                StartButtonCoverCircle_Image = StartButtonCoverCircle.GetComponent<Image>();
            }

            if(StartButton == null && IsHuman)
                StartButton = HumanStartPanel.StartButtonGO;

            THR_CanvasGO.GetComponent<Canvas>().sortingOrder = 0;
        });

        //SETUP TRIAL state -------------------------------------------------------------------------------------------------------------------------
        SetupTrial.SpecifyTermination(() => true, InitTrial);

        //INIT TRIAL state --------------------------------------------------------------------------------------------------------------------------
        var ShotgunHandler = SelectionTracker.SetupSelectionHandler("trial", "MouseButton0Click", MouseTracker, InitTrial, InitTrial);

        InitTrial.AddInitializationMethod(() =>
        {
            BackdropGO.SetActive(IsHuman ? false : true);

            if (IsHuman && TrialCount_InTask == 0)
                HumanStartPanel.HumanStartPanelGO.SetActive(true);
            else
                StartButton = null;

            ResetGlobalTrialVariables();
            SetTrialSummaryString();

            if (TrialCount_InBlock == 0)
                SetConfigValuesToTrialValues();

            LoadConfigUIVariables();
            SetSquareSizeAndPosition();

            currentTask.CalculateBlockSummaryString();

            if (TrialCount_InTask != 0)
                currentTask.SetTaskSummaryString();

            if (ShotgunHandler.AllSelections.Count > 0)
                ShotgunHandler.ClearSelections();

        });
        InitTrial.SpecifyTermination(() => true && ShotgunHandler.LastSuccessfulSelectionMatches(IsHuman ? HumanStartPanel.StartButtonChildren : USE_Square.StartButtonChildren) || StartButton == null, StartWithBlueSquare ? BlueSquare : WhiteSquare);
        InitTrial.AddDefaultTerminationMethod(() => TrialStartTime = Time.time);


        //WHITE SQUARE state ------------------------------------------------------------------------------------------------------------------------
        WhiteSquare.AddInitializationMethod(() =>
        {
            Input.ResetInputAxes();
            if (IsHuman && TrialCount_InTask == 0)
                HumanStartPanel.HumanStartPanelGO.SetActive(false);
            StartButtonCoverCircle_Image.color = Color.white;
            StartButtonCoverCircle.SetActive(true);
            StartButtonPlayIcon.SetActive(false);
            SquareGO.SetActive(true);
            BackdropGO.SetActive(true);
            WhiteStartTime = Time.time;
            WhiteTimeoutTime = 0;
        });
        WhiteSquare.AddUpdateMethod(() =>
        {
            if(WhiteTimeoutTime != 0 && (Time.time - WhiteTimeoutTime) > currentTrial.TimeoutDuration)
                WhiteTimeoutTime = 0;

            if (InputBroker.GetMouseButtonDown(0))
            {
                GameObject hitGO = InputBroker.RaycastBoth(InputBroker.mousePosition);
                if(hitGO != null)
                {
                    if (hitGO.name == "MainCircle" || hitGO.name == "Border")
                    {
                        WhiteSquareTouches_Trial++;
                        if (WhiteTimeoutTime == 0)
                        {
                            AudioFBController.Play("Negative");
                            WhiteTimeoutTime = Time.time;
                            WhiteStartTime = Time.time; //reset original WhiteStartTime so that normal duration resets.
                        }
                    }
                    if (hitGO.name == "BackdropGO")
                    {
                        if(BackdropTouches == 0)
                        {
                            AudioFBController.Play("Negative");
                            BackdropTouchTime = Time.time;
                            WhiteStartTime += currentTrial.TimeoutDuration;
                            Input.ResetInputAxes();
                            StartCoroutine(USE_Backdrop.GratedFlash(BackdropStripesTexture, currentTrial.TimeoutDuration));
                            BackdropTouches++;
                            BackdropTouches_Trial++;
                        }
                    }
                }
            }
            if (BackdropTouchTime != 0 && (Time.time - BackdropTouchTime) > currentTrial.TimeoutDuration)
            {
                BackdropTouches = 0;
                BackdropTouchTime = 0;
            }
        });
        WhiteSquare.SpecifyTermination(() => ((Time.time - WhiteStartTime) > currentTrial.WhiteSquareDuration) && WhiteTimeoutTime == 0, BlueSquare);

        //BLUE SQUARE state -------------------------------------------------------------------------------------------------------------------------
        BlueSquare.AddInitializationMethod(() =>
        {
            Input.ResetInputAxes();
            if (IsHuman && TrialCount_InTask == 0)
                HumanStartPanel.HumanStartPanelGO.SetActive(false);
            StartButtonPlayIcon.SetActive(true);
            StartButtonCoverCircle.SetActive(false);
            SquareGO.SetActive(true);
            BackdropGO.SetActive(true);
            BlueStartTime = Time.time;
            BlueSquareTouched = false;
            BlueSquareReleased = false;
            MovedOutside = false;
            BackdropTouchTime = 0;
            BackdropTouches = 0;
            HeldDuration = 0;
        });
        BlueSquare.AddUpdateMethod(() =>
        {
            if (InputBroker.GetMouseButtonDown(0))
            {
                GameObject hitGO = InputBroker.RaycastBoth(InputBroker.mousePosition);
                if(hitGO != null)
                {
                    if ((hitGO.name == "MainCircle" || hitGO.name == "Border") && !USE_Square.IsGrating && !USE_Backdrop.IsGrating)
                    {
                        if (!BlueSquareTouched)
                        {
                            TouchStartTime = Time.time;
                            BlueSquareTouched = true;
                        }

                        if (currentTrial.RewardTouch)
                        {
                            StartButtonPlayIcon.SetActive(false);
                            StartButtonCoverCircle_Image.color = GreenColor;
                            StartButtonCoverCircle.SetActive(true);
                            BlueSquareTouches_Trial++;
                            GiveTouchReward = true;
                            RewardEarnedTime = Time.time;
                        }
                        else
                        {
                            StartButtonPlayIcon.SetActive(false);
                            StartButtonCoverCircle_Image.color = Color.blue;
                            StartButtonCoverCircle.SetActive(true);
                        }
                        

                    }
                    if (hitGO.name == "BackdropGO" && !BlueSquareTouched && !USE_Backdrop.IsGrating && !USE_Square.IsGrating)
                    {
                        if (BackdropTouches == 0)
                        {
                            AudioFBController.Play("Negative");
                            BackdropTouchTime = Time.time;
                            BlueStartTime += currentTrial.TimeoutDuration; //add extra second so it doesn't go straight to white after grating
                            Input.ResetInputAxes();
                            StartCoroutine(USE_Backdrop.GratedFlash(BackdropStripesTexture, currentTrial.TimeoutDuration));
                            BackdropTouches++;
                            BackdropTouches_Trial++;
                        }
                    }
                }
            }

            if (InputBroker.GetMouseButton(0) && BlueSquareTouched)
            {
                HeldDuration += Time.deltaTime;

                GameObject hitGO = InputBroker.RaycastBoth(InputBroker.mousePosition);
                if(hitGO != null)
                {
                    if (hitGO.name == "BackdropGO")
                    {
                        Input.ResetInputAxes();
                        NumTouchesMovedOutside_Trial++;
                        MovedOutside = true;
                    }
                }
            }

            if (InputBroker.GetMouseButtonUp(0))
            {
                if (BlueSquareTouched && !BlueSquareReleased)
                {
                    if (currentTrial.RewardRelease)
                    {
                        if (HeldDuration >= currentTrial.MinTouchDuration && HeldDuration <= currentTrial.MaxTouchDuration)
                        {
                            BlueSquareTouches_Trial++;
                            GiveReleaseReward = true;
                            RewardEarnedTime = Time.time;
                        }
                        else if (HeldDuration < currentTrial.MinTouchDuration)
                        {
                            NumReleasedEarly_Block++;
                            HeldTooShort = true;
                        }
                        //The Else (Greater than MaxDuration) is handled below where I auto stop them for holding for max dur. 
                    }
                    else
                    {
                        StartButtonPlayIcon.SetActive(false);
                        StartButtonCoverCircle_Image.color = GreenColor;
                        StartButtonCoverCircle.SetActive(true);
                    }
                    BlueSquareReleased = true;
                }
            }

            if (HeldDuration >= currentTrial.MaxTouchDuration && BlueSquareTouched)
            {
                NumReleasedLate_Block++;
                HeldTooLong = true;
            }

            if (Time.time - TrialStartTime > currentTrial.TimeToAutoEndTrialSec)
                TimeRanOut = true;


            if (BackdropTouchTime != 0 && (Time.time - BackdropTouchTime) > currentTrial.TimeoutDuration)
            {
                BackdropTouches = 0;
                BackdropTouchTime = 0;
            }
        });
        BlueSquare.SpecifyTermination(() => (Time.time - BlueStartTime > currentTrial.BlueSquareDuration) && !InputBroker.GetMouseButton(0) && !BlueSquareReleased && !USE_Backdrop.IsGrating && !USE_Square.IsGrating, WhiteSquare); //Go back to white square if bluesquare time lapses (and they aren't already holding down)
        BlueSquare.SpecifyTermination(() => (BlueSquareReleased && !USE_Backdrop.IsGrating && !USE_Square.IsGrating) || MovedOutside || HeldTooLong || HeldTooShort || TimeRanOut || GiveTouchReward, Feedback); //If rewarding touch and they touched, or click the square and release, or run out of time. 
        //FEEDBACK state ----------------------------------------------------------------------------------------------------------------------------
        Feedback.AddInitializationMethod((VoidDelegate)(() =>
        {
            RewardTimer = Time.time - RewardEarnedTime; //start the timer at the difference between rewardtimeEarned and right now.
            AudioPlayed = false;
            GiveReward = false;

            if(GiveTouchReward || GiveReleaseReward)
            {
                AudioFBController.Play("Positive");
                if (GiveReleaseReward)
                {
                    StartButtonPlayIcon.SetActive(false);
                    StartButtonCoverCircle_Image.color = GreenColor;
                    StartButtonCoverCircle.SetActive(true);
                }
            }
            else //held too long, held too short, moved outside, or timeRanOut
            {
                AudioFBController.Play("Negative");
                if (currentTrial.ShowNegFb)
                {
                    if (HeldTooShort)
                        StartCoroutine(USE_Square.GratedFlash((GameObject)this.StartButtonCoverCircle, HeldTooShortTexture, currentTrial.TimeoutDuration, SquareGO));
                    else if (HeldTooLong)
                        StartCoroutine(USE_Square.GratedFlash((GameObject)this.StartButtonCoverCircle, HeldTooLongTexture, currentTrial.TimeoutDuration, SquareGO));
                    else if (MovedOutside)
                        StartCoroutine(USE_Square.GratedFlash((GameObject)this.StartButtonCoverCircle, BackdropStripesTexture, currentTrial.TimeoutDuration, SquareGO));
                }
            }
            AudioPlayed = true;
        }));
        Feedback.AddUpdateMethod(() =>
        {
            if((GiveTouchReward || GiveReleaseReward) && SyncBoxController != null)
            {
                if (RewardTimer < (GiveTouchReward ? currentTrial.TouchToRewardDelay : currentTrial.ReleaseToRewardDelay))
                    RewardTimer += Time.deltaTime;
                else
                    GiveReward = true;
            }
        });
        Feedback.SpecifyTermination(() => GiveReward, Reward); //If they got right, syncbox isn't null, and timer is met.
        Feedback.SpecifyTermination(() => (GiveTouchReward || GiveReleaseReward) && SyncBoxController == null, ITI); //If they got right, syncbox IS null, don't make them wait.  
        Feedback.SpecifyTermination(() => !GiveTouchReward && !GiveReleaseReward && AudioPlayed && !USE_Backdrop.IsGrating && !USE_Square.IsGrating, ITI); //if didn't get right, so no pulses. 

        Reward.AddInitializationMethod(() =>
        {
            if (GiveReleaseReward && SyncBoxController != null)
            {
                SyncBoxController.SendRewardPulses(currentTrial.NumReleasePulses, currentTrial.PulseSize);
                ReleaseRewards_Trial += currentTrial.NumReleasePulses;
                SessionInfoPanel.UpdateSessionSummaryValues(("totalRewardPulses",currentTrial.NumReleasePulses));
            }
            if (GiveTouchReward && SyncBoxController != null)
            {
                SyncBoxController.SendRewardPulses(currentTrial.NumTouchPulses, currentTrial.PulseSize);
                TouchRewards_Trial += currentTrial.NumTouchPulses;
                SessionInfoPanel.UpdateSessionSummaryValues(("totalRewardPulses",currentTrial.NumReleasePulses));
            }
        });
        Reward.SpecifyTermination(() => true, ITI);

        //ITI state ---------------------------------------------------------------------------------------------------------------------------------
        ITI.AddUpdateMethod(() =>
        {
            if(InputBroker.GetMouseButtonUp(0))
                ItiTouches_Trial++;
        });
        ITI.AddTimer(() => currentTrial.ItiDuration, FinishTrial);
        ITI.AddDefaultTerminationMethod(() =>
        {
            SquareGO.SetActive(false);

            if (GiveReleaseReward || GiveTouchReward)
                TrialsCorrect_Block++;

            if (GiveTouchReward || GiveReleaseReward)
                TrialCompletionList.Insert(0, 1);
            else
                TrialCompletionList.Insert(0, 0);
            
            AddTrialTouchNumsToBlock();
            TrialsCompleted_Block++;
            currentTask.CalculateBlockSummaryString();

            CheckIfBlockShouldEnd();

            ConfigValuesChangedInPrevTrial = ConfigValuesChanged();
        });

        LogTrialData();
        LogFrameData();
    }


    //HELPER FUNCTIONS ------------------------------------------------------------------------------------------
    private void AddTrialTouchNumsToBlock()
    {
        BlueSquareTouches_Block += BlueSquareTouches_Trial;
        WhiteSquareTouches_Block += WhiteSquareTouches_Trial;
        BackdropTouches_Block += BackdropTouches_Trial;
        NumItiTouches_Block += ItiTouches_Trial;
        NumTouchRewards_Block += TouchRewards_Trial;
        NumReleaseRewards_Block += ReleaseRewards_Trial;
        NumTouchesMovedOutside_Block += NumTouchesMovedOutside_Trial;
    }

    private void SetTrialSummaryString()
    {
        TrialSummaryString = "Rewarding: " + (currentTrial.RewardTouch ? "Touch" : "Release") +
                              "\nRandomPosition: " + ((currentTrial.RandomSquarePosition ? "True" : "False")) +
                              "\nRandomSize: " + ((currentTrial.RandomSquareSize ? "True" : "False"));
    }

    protected override bool CheckBlockEnd()
    {
        return PerfThresholdMet;
    }

    private void CheckIfBlockShouldEnd()
    {
        if(TrialsCompleted_Block >= currentTrial.PerfWindowEndTrials)
        {
            float sum = 0;
            for(int i = 0; i < currentTrial.PerfWindowEndTrials; i++)
                sum += TrialCompletionList[i];
            float performancePerc = sum / currentTrial.PerfWindowEndTrials;
            if (performancePerc >= currentTrial.PerfThresholdEndTrials)
                PerfThresholdMet = true; //Will trigger CheckBlockEnd function to terminate block
        }
    }

    private void LoadConfigUIVariables()
    {
        currentTrial.MinTouchDuration = ConfigUiVariables.get<ConfigNumber>("minTouchDuration").value;
        currentTrial.MaxTouchDuration = ConfigUiVariables.get<ConfigNumber>("maxTouchDuration").value;
        currentTrial.SquareSize = ConfigUiVariables.get<ConfigNumber>("squareSize").value;
        currentTrial.PositionX = (int)ConfigUiVariables.get<ConfigNumber>("positionX").value;
        currentTrial.PositionY = (int)ConfigUiVariables.get<ConfigNumber>("positionY").value;
        currentTrial.WhiteSquareDuration = ConfigUiVariables.get<ConfigNumber>("whiteSquareDuration").value;
        currentTrial.BlueSquareDuration = ConfigUiVariables.get<ConfigNumber>("blueSquareDuration").value;
    }

    private bool ConfigValuesChanged()
    {
        if (currentTrial.SquareSize != ConfigUiVariables.get<ConfigNumber>("squareSize").value
            || currentTrial.PositionX != ConfigUiVariables.get<ConfigNumber>("positionX").value
            || currentTrial.PositionY != ConfigUiVariables.get<ConfigNumber>("positionY").value)
        {
            return true;
        }
        else
            return false;
    }

    private void SetConfigValuesToTrialValues()
    {
        ConfigUiVariables.get<ConfigNumber>("squareSize").SetValue(currentTrial.SquareSize);
        ConfigUiVariables.get<ConfigNumber>("positionX").SetValue(currentTrial.PositionX);
        ConfigUiVariables.get<ConfigNumber>("positionY").SetValue(currentTrial.PositionY);
        ConfigUiVariables.get<ConfigNumber>("minTouchDuration").SetValue(currentTrial.MinTouchDuration);
        ConfigUiVariables.get<ConfigNumber>("maxTouchDuration").SetValue(currentTrial.MaxTouchDuration);
        ConfigUiVariables.get<ConfigNumber>("whiteSquareDuration").SetValue(currentTrial.WhiteSquareDuration);
        ConfigUiVariables.get<ConfigNumber>("blueSquareDuration").SetValue(currentTrial.BlueSquareDuration);
    }

    private void SetSquareSizeAndPosition()
    {
        if (currentTrial.RandomSquareSize && !ConfigValuesChangedInPrevTrial)
        {
            float randomSize = Random.Range(currentTrial.SquareSizeMin, currentTrial.SquareSizeMax);
            USE_Square.SetButtonSize(randomSize);
            ConfigUiVariables.get<ConfigNumber>("squareSize").SetValue(randomSize);
            currentTrial.SquareSize = randomSize;
        }
        else
            SquareGO.transform.localScale = new Vector2(currentTrial.SquareSize, currentTrial.SquareSize);

        if (currentTrial.RandomSquarePosition && !ConfigValuesChangedInPrevTrial)
        {
            int x = Random.Range(currentTrial.PositionX_Min, currentTrial.PositionX_Max);
            int y = Random.Range(currentTrial.PositionY_Min, currentTrial.PositionY_Max);
            SquareGO.transform.localPosition = new Vector2(x, y);
            ConfigUiVariables.get<ConfigNumber>("positionX").SetValue(x);
            ConfigUiVariables.get<ConfigNumber>("positionY").SetValue(y);
            currentTrial.PositionX = x;
            currentTrial.PositionY = y;
        }
        else
            SquareGO.transform.localPosition = new Vector2(currentTrial.PositionX, currentTrial.PositionY);
    }


    private void ResetGlobalTrialVariables()
    {
        HeldTooLong = false;
        HeldTooShort = false;
        GiveReleaseReward = false;
        GiveTouchReward = false;
        TimeRanOut = false;
        BackdropTouches_Trial = 0;
        BlueSquareTouches_Trial = 0;
        WhiteSquareTouches_Trial = 0;
        NumTouchesMovedOutside_Trial = 0;
        ItiTouches_Trial = 0;
        TouchStartTime = 0;
        HeldDuration = 0;
        TouchRewards_Trial = 0;
        ReleaseRewards_Trial = 0;
    }

    private void LogTrialData()
    {
        TrialData.AddDatum("SquareSize", () => currentTrial.SquareSize);
        TrialData.AddDatum("SquarePosX", () => currentTrial.PositionX);
        TrialData.AddDatum("SquarePosY", () => currentTrial.PositionY);
        TrialData.AddDatum("MinTouchDuration", () => currentTrial.MinTouchDuration);
        TrialData.AddDatum("MaxTouchDuration", () => currentTrial.MaxTouchDuration);
        TrialData.AddDatum("RewardTouch", () => currentTrial.RewardTouch);
        TrialData.AddDatum("RewardRelease", () => currentTrial.RewardRelease);
        TrialData.AddDatum("DifficultyLevel", () => currentTrial.BlockName);
        TrialData.AddDatum("BlueSquareTouches_Trial", () => BlueSquareTouches_Trial);
        TrialData.AddDatum("WhiteSquareTouches_Trial", () => WhiteSquareTouches_Trial);
        TrialData.AddDatum("BackdropTouches_Trial", () => BackdropTouches_Trial);
        TrialData.AddDatum("MovedOutsideSquare_Trial", () => NumTouchesMovedOutside_Trial);
        TrialData.AddDatum("ItiTouches_Trial", () => ItiTouches_Trial);
        TrialData.AddDatum("ReactionTime", () => ReactionTime);
        TrialData.AddDatum("TouchStartTime", () => TouchStartTime);
        TrialData.AddDatum("HeldDuration", () => HeldDuration);
    }

    private void LogFrameData()
    {
        FrameData.AddDatum("TouchPosition", () => InputBroker.mousePosition);
        FrameData.AddDatum("SquareGO", () => SquareGO.activeInHierarchy);
    }

}
