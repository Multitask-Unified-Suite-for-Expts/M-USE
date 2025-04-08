/*
MIT License

Copyright (c) 2023 Multitask - Unified - Suite -for-Expts

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files(the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/


using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using ConfigDynamicUI;
using USE_ExperimentTemplate_Trial;
using USE_States;
using THR_Namespace;
using USE_UI;
using USE_ExperimentTemplate_Task;

public class THR_TrialLevel : ControlLevel_Trial_Template
{
    public THR_TrialDef CurrentTrial => GetCurrentTrialDef<THR_TrialDef>();
    public THR_TaskLevel CurrentTaskLevel => GetTaskLevel<THR_TaskLevel>();
    public THR_TaskDef CurrentTask => GetTaskDef<THR_TaskDef>();


    private USE_Square USE_Square;
    private GameObject SquareGO;
    private USE_Backdrop USE_Backdrop;
    private GameObject BackdropGO;

    private GameObject StartButton;

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

    [HideInInspector] public List<int> RunningAcc;
    [HideInInspector] public int TrialsCompleted_Block;
    [HideInInspector] public int TrialsCorrect_Block;

    //Data variables:
    [HideInInspector] public int BackdropTouches_Trial;
    [HideInInspector] public int SelectObjectTouches_Trial;
    [HideInInspector] public int AvoidObjectTouches_Trial;
    [HideInInspector] public int NumItiTouches_Trial;
    [HideInInspector] public int NumTouchRewards_Trial;
    [HideInInspector] public int NumReleaseRewards_Trial;
    [HideInInspector] public int NumReleasedEarly_Trial;
    [HideInInspector] public int NumReleasedLate_Trial;
    [HideInInspector] public int NumTouchesMovedOutside_Trial;

    [HideInInspector] public int BackdropTouches_Block;
    [HideInInspector] public int SelectObjectTouches_Block;
    [HideInInspector] public int AvoidObjectTouches_Block;
    [HideInInspector] public int NumTouchRewards_Block;
    [HideInInspector] public int NumReleaseRewards_Block;
    [HideInInspector] public int NumItiTouches_Block;
    [HideInInspector] public int NumReleasedEarly_Block;
    [HideInInspector] public int NumReleasedLate_Block;
    [HideInInspector] public int NumTouchesMovedOutside_Block;


    private bool MainObjectTouched;
    private bool MainObjectReleased;
    private bool AudioPlayed;
    private bool HeldTooShort;
    private bool HeldTooLong;
    public bool PerfThresholdMet;
    private bool MovedOutside;
    private bool ConfigValuesChangedInPrevTrial;

    private Color32 DarkBlueColor;
    private Color32 LightBlueColor;

    private float AvoidObjectTimeoutTime;
    private float AvoidObjectStartTime;
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


    public override void DefineControlLevel()
    {
        State InitTrial = new State("InitTrial");
        State AvoidObject = new State("AvoidObject");
        State SelectObject = new State("SelectObject");
        State Feedback = new State("Feedback");
        State Reward = new State("Reward");
        State ITI = new State("ITI");
        AddActiveStates(new List<State> { InitTrial, AvoidObject, SelectObject, Feedback, Reward, ITI});

        Add_ControlLevel_InitializationMethod(() =>
        {
            DarkBlueColor = new Color32(0, 0, 166, 255);
            LightBlueColor = new Color32(38, 188, 250, 255);

            if (SquareGO == null)
            {
                USE_Backdrop = gameObject.AddComponent<USE_Backdrop>();
                BackdropGO = USE_Backdrop.CreateBackdrop(THR_CanvasGO.GetComponent<Canvas>(), "BackdropGO", new Color32(6, 10, 17, 255));

                USE_Square = gameObject.AddComponent<USE_Square>();
                SquareGO = USE_Square.CreateSquareStartButton(THR_CanvasGO.GetComponent<Canvas>(), null, null, Color.blue, "StartButtonGO");
            }

            if (StartButton == null && Session.SessionDef.IsHuman)
                StartButton = Session.HumanStartPanel.StartButtonGO;
        });

        //SETUP TRIAL state -------------------------------------------------------------------------------------------------------------------------
        SetupTrial.SpecifyTermination(() => true, InitTrial);
        SetupTrial.AddDefaultTerminationMethod(() =>
        {
            if (Session.SessionDef.IsHuman && TrialCount_InTask == 0)
                Session.HumanStartPanel.HumanStartPanelGO.SetActive(true);
            else
                StartButton = null;
        });

        //INIT TRIAL state --------------------------------------------------------------------------------------------------------------------------
        var ShotgunHandler = Session.SelectionTracker.SetupSelectionHandler("trial", "MouseButton0Click", Session.MouseTracker, InitTrial, InitTrial);

        InitTrial.AddSpecificInitializationMethod(() =>
        {
            ResetGlobalTrialVariables();
            SetTrialSummaryString();

            if (TrialCount_InBlock == 0)
                SetConfigValuesToTrialValues();

            LoadConfigUIVariables();
            SetSquareSizeAndPosition();

            CurrentTaskLevel.CalculateBlockSummaryString();

            if (TrialCount_InTask != 0)
                CurrentTaskLevel.SetTaskSummaryString();

            if (ShotgunHandler.AllSelections.Count > 0)
                ShotgunHandler.ClearSelections();
        });
        InitTrial.SpecifyTermination(() => true && ((Session.SessionDef.IsHuman && ShotgunHandler.LastSuccessfulSelectionMatchesStartButton()) || StartButton == null), CurrentTask.StartWithSelectObjectState ? SelectObject : AvoidObject);
        InitTrial.AddDefaultTerminationMethod(() => TrialStartTime = Time.time);

        //AVOID OBJECT state ------------------------------------------------------------------------------------------------------------------------
        AvoidObject.AddSpecificInitializationMethod(() =>
        {
            Input.ResetInputAxes();
            if (Session.SessionDef.IsHuman && TrialCount_InTask == 0)
                Session.HumanStartPanel.HumanStartPanelGO.SetActive(false);
            USE_Square.SetSquareColor(Color.white);
            SquareGO.SetActive(true);
            BackdropGO.SetActive(true);
            AvoidObjectStartTime = Time.time;
            AvoidObjectTimeoutTime = 0;

            //reset it so the duration is 0 on exp display even if had one last trial
            OngoingSelection = null;
        });
        AvoidObject.AddUpdateMethod(() =>
        {
            if(AvoidObjectTimeoutTime != 0 && (Time.time - AvoidObjectTimeoutTime) > CurrentTrial.TimeoutDuration)
                AvoidObjectTimeoutTime = 0;

            if (InputBroker.GetMouseButtonDown(0))
            {
                GameObject hitGO = InputBroker.SimpleRaycast(InputBroker.mousePosition);
                if(hitGO != null)
                {
                    if (hitGO.name == "StartButtonGO")
                    {
                        AvoidObjectTouches_Trial++;
                        if (AvoidObjectTimeoutTime == 0)
                        {
                            AudioFBController.Play("Negative");
                            AvoidObjectTimeoutTime = Time.time;
                            AvoidObjectStartTime = Time.time; //reset original WhiteStartTime so that normal duration resets.
                        }
                    }
                    if (hitGO.name == "BackdropGO")
                    {
                        if(BackdropTouches == 0)
                        {
                            AudioFBController.Play("Negative");
                            BackdropTouchTime = Time.time;
                            AvoidObjectStartTime += CurrentTrial.TimeoutDuration;
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

            OngoingSelection = ShotgunHandler.OngoingSelection;

            //Update Exp Display with OngoingSelection Duration:
            if (OngoingSelection != null)
            {
                SetTrialSummaryString();
            }

        });
        AvoidObject.SpecifyTermination(() => ((Time.time - AvoidObjectStartTime) > CurrentTrial.AvoidObjectDuration) && AvoidObjectTimeoutTime == 0, SelectObject);

        //SELECT OBJECT state -------------------------------------------------------------------------------------------------------------------------
        SelectObject.AddSpecificInitializationMethod(() =>
        {
            Input.ResetInputAxes();
            if (Session.SessionDef.IsHuman && TrialCount_InTask == 0)
                Session.HumanStartPanel.HumanStartPanelGO.SetActive(false);
            USE_Square.SetSquareColor(Color.blue);
            SquareGO.SetActive(true);
            BackdropGO.SetActive(true);
            SelectObjectStartTime = Time.time;
            MainObjectTouched = false;
            MainObjectReleased = false;
            MovedOutside = false;
            BackdropTouchTime = 0;
            BackdropTouches = 0;
            HeldDuration = 0;

            //reset it so the duration is 0 on exp display even if had one last trial
            OngoingSelection = null;
        });
        SelectObject.AddUpdateMethod(() =>
        {
            if (InputBroker.GetMouseButtonDown(0))
            {
                GameObject hitGO = InputBroker.SimpleRaycast(InputBroker.mousePosition);
                if(hitGO != null)
                {
                    if (hitGO.name == "StartButtonGO" && !USE_Square.IsGrating && !USE_Backdrop.IsGrating)
                    {
                        if (!MainObjectTouched)
                        {
                            TouchStartTime = Time.time;
                            MainObjectTouched = true;
                        }

                        if (CurrentTrial.RewardTouch)
                        {
                            USE_Square.SetSquareColor(LightBlueColor);
                            SelectObjectTouches_Trial++;
                            GiveTouchReward = true;
                            RewardEarnedTime = Time.time;
                        }
                        else
                            USE_Square.SetSquareColor(DarkBlueColor);
                    }

                    if (hitGO.name == "BackdropGO" && !MainObjectTouched && !USE_Backdrop.IsGrating && !USE_Square.IsGrating)
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

            if (InputBroker.GetMouseButton(0) && MainObjectTouched)
            {
                HeldDuration += Time.deltaTime;

                GameObject hitGO = InputBroker.SimpleRaycast(InputBroker.mousePosition);
                if(hitGO != null)
                {
                    if (hitGO.name == "BackdropGO")
                    {
                        NumTouchesMovedOutside_Trial++;
                        MovedOutside = true;
                    }
                }
            }

            if (InputBroker.GetMouseButtonUp(0))
            {
                if (MainObjectTouched && !MainObjectReleased)
                {
                    if (CurrentTrial.RewardRelease)
                    {
                        if (HeldDuration >= CurrentTrial.MinTouchDuration && HeldDuration <= CurrentTrial.MaxTouchDuration)
                        {
                            SelectObjectTouches_Trial++;
                            GiveReleaseReward = true;
                            RewardEarnedTime = Time.time;
                        }
                        else if (HeldDuration < CurrentTrial.MinTouchDuration)
                        {
                            NumReleasedEarly_Trial++;
                            HeldTooShort = true;
                        }
                        //The Else (Greater than MaxDuration) is handled below where I auto stop them for holding for max dur. 
                    }
                    else
                        USE_Square.SetSquareColor(LightBlueColor);
                    
                    MainObjectReleased = true;
                }
            }

            if (HeldDuration >= CurrentTrial.MaxTouchDuration && MainObjectTouched)
            {
                NumReleasedLate_Trial++;
                HeldTooLong = true;
            }

            if (Time.time - TrialStartTime > CurrentTrial.TimeToAutoEndTrialSec)
            {
                TimeRanOut = true;
                Session.EventCodeManager.SendCodeThisFrame("NoChoice");
                Session.EventCodeManager.SendRangeCodeThisFrame("CustomAbortTrial", AbortCodeDict["NoSelectionMade"]);
                AbortCode = 6;
            }


            if (BackdropTouchTime != 0 && (Time.time - BackdropTouchTime) > CurrentTrial.TimeoutDuration)
            {
                BackdropTouches = 0;
                BackdropTouchTime = 0;
            }

            OngoingSelection = ShotgunHandler.OngoingSelection;

            //Update Exp Display with OngoingSelection Duration:
            if (OngoingSelection != null)
            {
                SetTrialSummaryString();
            }

        });
        SelectObject.SpecifyTermination(() => (Time.time - SelectObjectStartTime > CurrentTrial.SelectObjectDuration) && !InputBroker.GetMouseButton(0) && !MainObjectReleased && !USE_Backdrop.IsGrating && !USE_Square.IsGrating, AvoidObject); //Go back to white square if bluesquare time lapses (and they aren't already holding down)
        SelectObject.SpecifyTermination(() => (MainObjectReleased && !USE_Backdrop.IsGrating && !USE_Square.IsGrating) || MovedOutside || HeldTooLong || HeldTooShort || TimeRanOut || GiveTouchReward, Feedback); //If rewarding touch and they touched, or click the square and release, or run out of time. 

        //FEEDBACK state ----------------------------------------------------------------------------------------------------------------------------
        Feedback.AddSpecificInitializationMethod((() =>
        {
            RewardTimer = Time.time - RewardEarnedTime; //start the timer at the difference between rewardtimeEarned and right now.
            AudioPlayed = false;
            GiveReward = false;

            if(GiveTouchReward || GiveReleaseReward)
            {
                AudioFBController.Play("Positive");

                if (GiveReleaseReward)
                    USE_Square.SetSquareColor(LightBlueColor);
            }
            else //held too long, held too short, moved outside, or timeRanOut
            {
                AudioFBController.Play("Negative");
                if (CurrentTrial.ShowNegFb)
                {
                    if (HeldTooShort)
                        StartCoroutine(USE_Square.GratedFlash(SquareGO, HeldTooShortSquareTexture, CurrentTrial.TimeoutDuration, SquareGO));
                    else if (HeldTooLong)
                        StartCoroutine(USE_Square.GratedFlash(SquareGO, HeldTooLongSquareTexture, CurrentTrial.TimeoutDuration, SquareGO));
                    else if (MovedOutside)
                        StartCoroutine(USE_Square.GratedFlash(SquareGO, MovedTooFarSquareTexture, CurrentTrial.TimeoutDuration, SquareGO));
                }
            }
            AudioPlayed = true;
        }));
        Feedback.AddUpdateMethod(() =>
        {
            if((GiveTouchReward || GiveReleaseReward) && Session.SyncBoxController != null)
            {
                if (RewardTimer < (GiveTouchReward ? CurrentTrial.TouchToRewardDelay : CurrentTrial.ReleaseToRewardDelay))
                    RewardTimer += Time.deltaTime;
                else
                    GiveReward = true;
            }
        });
        Feedback.SpecifyTermination(() => GiveReward, Reward); //If they got right, syncbox isn't null, and timer is met.
        Feedback.SpecifyTermination(() => (GiveTouchReward || GiveReleaseReward) && Session.SyncBoxController == null, ITI); //If they got right, syncbox IS null, don't make them wait.  
        Feedback.SpecifyTermination(() => !GiveTouchReward && !GiveReleaseReward && AudioPlayed && !USE_Backdrop.IsGrating && !USE_Square.IsGrating, ITI); //if didn't get right, so no pulses. 

        Reward.AddSpecificInitializationMethod(() =>
        {
            if (GiveReleaseReward && Session.SyncBoxController != null)
            {
                Session.SyncBoxController.SendRewardPulses(CurrentTrial.NumReleasePulses, CurrentTrial.PulseSize);
                NumReleaseRewards_Trial += CurrentTrial.NumReleasePulses;
                CurrentTaskLevel.NumRewardPulses_InBlock += CurrentTrial.NumReleasePulses;
                CurrentTaskLevel.NumRewardPulses_InTask += CurrentTrial.NumReleasePulses;
            }
            if (GiveTouchReward && Session.SyncBoxController != null)
            {
                Session.SyncBoxController.SendRewardPulses(CurrentTrial.NumTouchPulses, CurrentTrial.PulseSize);
                NumTouchRewards_Trial += CurrentTrial.NumTouchPulses;
                CurrentTaskLevel.NumRewardPulses_InBlock += CurrentTrial.NumTouchPulses;
                CurrentTaskLevel.NumRewardPulses_InTask += CurrentTrial.NumTouchPulses;
            }
        });
        Reward.SpecifyTermination(() => true, ITI);

        //ITI state ---------------------------------------------------------------------------------------------------------------------------------
        ITI.AddUpdateMethod(() =>
        {
            if(InputBroker.GetMouseButtonUp(0))
                NumItiTouches_Trial++;
        });
        ITI.AddTimer(() => CurrentTrial.ItiDuration, FinishTrial);
        ITI.AddDefaultTerminationMethod(() =>
        {
            SquareGO.SetActive(false);
            UpdateData();
            CurrentTaskLevel.CalculateBlockSummaryString();
           // CheckIfBlockShouldEnd();
            ConfigValuesChangedInPrevTrial = ConfigValuesChanged();
        });

        DefineTrialData();
        DefineFrameData();
    }


    //HELPER FUNCTIONS ------------------------------------------------------------------------------------------
    //public override void AddToStimLists()
    //{
    //    SessionValues.TargetObjects.Add(SquareGO);
    //    SessionValues.IrrelevantObjects.Add(BackdropGO);
    //}

    private void UpdateData()
    {
        SelectObjectTouches_Block += SelectObjectTouches_Trial;
        AvoidObjectTouches_Block += AvoidObjectTouches_Trial;
        BackdropTouches_Block += BackdropTouches_Trial;
        NumItiTouches_Block += NumItiTouches_Trial;
        NumTouchRewards_Block += NumTouchRewards_Trial;
        NumReleaseRewards_Block += NumReleaseRewards_Trial;
        NumReleasedEarly_Block += NumReleasedEarly_Trial;
        NumReleasedLate_Block += NumReleasedLate_Trial;
        NumTouchesMovedOutside_Block += NumTouchesMovedOutside_Trial;

        CurrentTaskLevel.SelectObjectTouches_Task += SelectObjectTouches_Trial;
        CurrentTaskLevel.AvoidObjectTouches_Task += AvoidObjectTouches_Trial;
        CurrentTaskLevel.BackdropTouches_Task += BackdropTouches_Trial;
        CurrentTaskLevel.ItiTouches_Task += NumItiTouches_Trial;
        CurrentTaskLevel.TouchRewards_Task += NumTouchRewards_Trial;
        CurrentTaskLevel.ReleaseRewards_Task += NumReleaseRewards_Trial;
        CurrentTaskLevel.ReleasedEarly_Task += NumReleasedEarly_Trial;
        CurrentTaskLevel.ReleasedLate_Task += NumReleasedLate_Trial;
        CurrentTaskLevel.TouchesMovedOutside_Task += NumTouchesMovedOutside_Trial;

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
        AvoidObjectTouches_Block = 0;
        NumItiTouches_Block = 0;
        NumTouchRewards_Block = 0;
        NumReleaseRewards_Block = 0;
        NumReleasedEarly_Block = 0;
        NumReleasedLate_Block = 0;
        NumTouchesMovedOutside_Block = 0;
        PerfThresholdMet = false;
    }

    public override void ResetTrialVariables()
    {
        HeldTooLong = false;
        HeldTooShort = false;
        GiveReleaseReward = false;
        GiveTouchReward = false;
        TimeRanOut = false;
        BackdropTouches_Trial = 0;
        SelectObjectTouches_Trial = 0;
        AvoidObjectTouches_Trial = 0;
        NumReleasedEarly_Trial = 0;
        NumReleasedLate_Trial = 0;
        NumTouchesMovedOutside_Trial = 0;
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
                              "\nRandom Size: " + ((CurrentTrial.RandomObjectSize ? "True" : "False")) +
                              "\n" +
                             "\nOngoingSelection: " + (OngoingSelection == null ? "" : OngoingSelection.Duration.Value.ToString("F2") + " s");

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
        CurrentTrial.AvoidObjectDuration = ConfigUiVariables.get<ConfigNumber>("avoidObjectDuration").value;
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
        ConfigUiVariables.get<ConfigNumber>("avoidObjectDuration").SetValue(CurrentTrial.AvoidObjectDuration);
        ConfigUiVariables.get<ConfigNumber>("selectObjectDuration").SetValue(CurrentTrial.SelectObjectDuration);
    }

    private void SetSquareSizeAndPosition()
    {
        if (CurrentTrial.RandomObjectSize && !ConfigValuesChangedInPrevTrial)
        {
            float randomSize = Random.Range(CurrentTrial.ObjectSizeMin, CurrentTrial.ObjectSizeMax);
            USE_Square.SetButtonScale(randomSize);
            ConfigUiVariables.get<ConfigNumber>("objectSize").SetValue(randomSize);
            CurrentTrial.ObjectSize = randomSize;
        }
        else
            SquareGO.transform.localScale = new Vector2(CurrentTrial.ObjectSize, CurrentTrial.ObjectSize);

        if (CurrentTrial.RandomObjectPosition && !ConfigValuesChangedInPrevTrial)
        {
            int x = Random.Range(CurrentTrial.PositionX_Min, CurrentTrial.PositionX_Max);
            int y = Random.Range(CurrentTrial.PositionY_Min, CurrentTrial.PositionY_Max);
            SquareGO.transform.localPosition = new Vector2(x, y);
            ConfigUiVariables.get<ConfigNumber>("positionX").SetValue(x);
            ConfigUiVariables.get<ConfigNumber>("positionY").SetValue(y);
            CurrentTrial.PositionX = x;
            CurrentTrial.PositionY = y;
        }
        else
            SquareGO.transform.localPosition = new Vector2(CurrentTrial.PositionX, CurrentTrial.PositionY);
    }


    private void ResetGlobalTrialVariables()
    {
        HeldTooLong = false;
        HeldTooShort = false;
        GiveReleaseReward = false;
        GiveTouchReward = false;
        TimeRanOut = false;
        BackdropTouches_Trial = 0;
        SelectObjectTouches_Trial = 0;
        AvoidObjectTouches_Trial = 0;
        NumTouchesMovedOutside_Trial = 0;
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
        TrialData.AddDatum("AvoidObjectTouches_Trial", () => AvoidObjectTouches_Trial);
        TrialData.AddDatum("BackdropTouches_Trial", () => BackdropTouches_Trial);
        TrialData.AddDatum("MovedOutsideObject_Trial", () => NumTouchesMovedOutside_Trial);
        TrialData.AddDatum("ItiTouches_Trial", () => NumItiTouches_Trial);
        TrialData.AddDatum("ReactionTime", () => ReactionTime);
        TrialData.AddDatum("TouchStartTime", () => TouchStartTime);
        TrialData.AddDatum("HeldDuration", () => HeldDuration);
    }

    private void DefineFrameData()
    {
        FrameData.AddDatum("TouchPosition", () => InputBroker.mousePosition);
        FrameData.AddDatum("MainObjectGO", () => SquareGO?.activeInHierarchy);
    }

}
