using System;
using System.Collections.Generic;
using System.Linq;
using ConfigDynamicUI;
using KeepTrack_Namespace;
using UnityEngine;
using USE_ExperimentTemplate_Trial;
using USE_States;
using static SelectionTracking.SelectionTracker;


public class KeepTrack_TrialLevel : ControlLevel_Trial_Template
{
    public KeepTrack_TrialDef CurrentTrial => GetCurrentTrialDef<KeepTrack_TrialDef>();
    public KeepTrack_TaskLevel CurrentTaskLevel => GetTaskLevel<KeepTrack_TaskLevel>();
    public KeepTrack_TaskDef CurrentTask => GetTaskDef<KeepTrack_TaskDef>();

    //TRIAL DATA:
    [HideInInspector] public int SectionChanges_Trial = 0;
    [HideInInspector] public int TargetSel_BeforeFirstAnim_Trial = 0;
    [HideInInspector] public int TargetSel_BeforeResponseWindow_Trial = 0;
    [HideInInspector] public int TargetSel_WithinResponseWindow_Trial = 0;
    [HideInInspector] public int TargetSel_AfterResponseWindow_Trial = 0;
    [HideInInspector] public int AdditionalTargetSel_Trial = 0;

    [HideInInspector] public int TargetAnimations_Trial = 0;
    [HideInInspector] public int DistractorAnimations_Trial = 0;
    [HideInInspector] public int TargetIntervalsMissed_Trial = 0;
    [HideInInspector] public int DistractorSelections_Trial = 0;
    [HideInInspector] public int DistractorRejections_Trial = 0;
    [HideInInspector] public int SliderBarCompletions_Trial = 0;

    //BLOCK DATA:
    [HideInInspector] public int SectionChanges_Block = 0;
    [HideInInspector] public int TargetSel_BeforeFirstAnim_Block = 0;
    [HideInInspector] public int TargetSel_BeforeResponseWindow_Block = 0;
    [HideInInspector] public int TargetSel_WithinResponseWindow_Block = 0;
    [HideInInspector] public int TargetSel_AfterResponseWindow_Block = 0;
    [HideInInspector] public int AdditionalTargetSel_Block = 0;
    [HideInInspector] public int TargetAnimations_Block = 0;
    [HideInInspector] public int DistractorAnimations_Block = 0;
    [HideInInspector] public int TargetIntervalsMissed_Block = 0;
    [HideInInspector] public int DistractorSelections_Block = 0;
    [HideInInspector] public int DistractorRejections_Block = 0;
    [HideInInspector] public int SliderBarCompletions_Block = 0;
    [HideInInspector] public int TrialCompletions_Block;

    //Set in Inspector:
    public GameObject KeepTrack_CanvasGO;
    public GameObject BordersGO;

    private KT_ObjectManager ObjManager;

    private GameObject StartButton;

    private GameObject ChosenGO = null;
    private KT_Object ChosenObject = null;

    private int SliderGainSteps;
    private bool GiveRewardIfSliderFull = false;

    [HideInInspector] public ConfigNumber itiDuration, timeBeforeChoiceStarts, totalChoiceDuration, sliderFlashingDuration, sliderUpdateDuration, sliderSize;

    List<KT_Object> TrialObjects;

    private Dictionary<int, int> SectionChanged_Frames = new Dictionary<int, int>();
    private Dictionary<int, int> TargetAnimations_Frames = new Dictionary<int, int>();
    private Dictionary<int, int> DistractorAnimations_Frames = new Dictionary<int, int>();
    private Dictionary<int, int> TargetAnimationsMissed_Frames = new Dictionary<int, int>();
    private Dictionary<int, int> DistractorsAvoided_Frames = new Dictionary<int, int>();



    public override void DefineControlLevel()
    {
        State InitTrial = new State("InitTrial");
        State DisplayTarget = new State("DisplayTarget");
        State DisplayDistractors = new State("DisplayDistractors");
        State Play = new State("Play");
        State ITI = new State("ITI");
        AddActiveStates(new List<State> { InitTrial, DisplayTarget, DisplayDistractors, Play, ITI });

        Add_ControlLevel_InitializationMethod(() =>
        {
            if (SliderFBController != null && SliderFBController.SliderGO == null)
                SliderFBController.InitializeSlider();

            if (StartButton == null)
            {
                if (Session.SessionDef.IsHuman)
                {
                    StartButton = Session.HumanStartPanel.StartButtonGO;
                    Session.HumanStartPanel.SetVisibilityOnOffStates(InitTrial, InitTrial);
                }
                else
                {
                    StartButton = Session.USE_StartButton.CreateStartButton(KeepTrack_CanvasGO.GetComponent<Canvas>(), CurrentTask.StartButtonPosition, CurrentTask.StartButtonScale);
                    Session.USE_StartButton.SetVisibilityOnOffStates(InitTrial, InitTrial);
                }
            }
        });

        //SetupTrial state ----------------------------------------------------------------------------------------------------------------------------------------------
        SetupTrial.AddSpecificInitializationMethod(() =>
        {
            BordersGO.SetActive(false);
            LoadConfigUIVariables();

            if (ObjManager != null)
                Destroy(ObjManager);

            ObjManager = gameObject.AddComponent<KT_ObjectManager>();
            ObjManager.MaxTouchDuration = totalChoiceDuration.value;
            ObjManager.SetObjectParent(KeepTrack_CanvasGO.transform);

            SubscribeToObjectEvents();

            List<KT_Object_ConfigValues> trialObjectsConfigValues = new List<KT_Object_ConfigValues>();

            foreach (int objIndex in CurrentTrial.TrialObjectIndices)
            {
                if (objIndex >= 0 && objIndex < CurrentTaskLevel.KT_Objects_ConfigValues.Count())
                {
                    var configValue = CurrentTaskLevel.KT_Objects_ConfigValues[objIndex];
                    if (configValue != null)
                        trialObjectsConfigValues.Add(configValue);
                    else
                        Debug.LogWarning($"NO OBJECT FOUND AT INDEX {objIndex} in KT_Objects_ConfigValues.");
                }
                else
                {
                    Debug.LogWarning($"INDEX NUM {objIndex} IS OUT OF BOUNDS FOR KT OBJECTS CONFIG VALUES");
                }
            }

            //CREATE THE SPECIFIC OBJECTS FOR THIS TRIAL:
            TrialObjects = ObjManager.CreateObjects(trialObjectsConfigValues);

        });
        SetupTrial.SpecifyTermination(() => true, InitTrial);


        //Setup Shotgun Handler ---------------------------------------------------------------------
        if (Session.SessionDef.SelectionType.ToLower().Contains("gaze"))
            SelectionHandler = Session.SelectionTracker.SetupSelectionHandler("trial", "GazeShotgun", Session.GazeTracker, InitTrial, Play);
        else
            SelectionHandler = Session.SelectionTracker.SetupSelectionHandler("trial", Session.SessionDef.SelectionType, Session.MouseTracker, InitTrial, Play);
        //--------------------------------------------------------------------------------------------
        TouchFBController.EnableTouchFeedback(SelectionHandler, CurrentTask.TouchFeedbackDuration, CurrentTask.TouchFeedbackSize, KeepTrack_CanvasGO); //Enable Touch Feedback

        //InitTrial state ----------------------------------------------------------------------------------------------------------------------------------------------
        InitTrial.AddSpecificInitializationMethod(() =>
        {
            SetTrialSummaryString();

            if (SelectionHandler.AllChoices.Count > 0)
                SelectionHandler.ClearChoices();

            SelectionHandler.TimeBeforeChoiceStarts = Session.SessionDef.StartButtonSelectionDuration;
            SelectionHandler.TotalChoiceDuration = Session.SessionDef.StartButtonSelectionDuration;
        });
        InitTrial.SpecifyTermination(() => CurrentTask.RunSimulation, DisplayTarget);
        InitTrial.SpecifyTermination(() => SelectionHandler.LastSuccessfulSelectionMatchesStartButton(), DisplayTarget);
        InitTrial.AddDefaultTerminationMethod(() =>
        {
            SelectionHandler.TimeBeforeChoiceStarts = timeBeforeChoiceStarts.value;
            SelectionHandler.TotalChoiceDuration = totalChoiceDuration.value;

            BordersGO.SetActive(true);

            CalculateSliderSteps();
            SliderFBController.ConfigureSlider(sliderSize.value, CurrentTrial.SliderInitialValue * (1f / SliderGainSteps), new Vector3(0f, -43f, 0f));
            SliderFBController.SetSliderRectSize(new Vector2(400f, 25f));
            SliderFBController.SetUpdateDuration(sliderUpdateDuration.value);
            SliderFBController.SetFlashingDuration(sliderFlashingDuration.value);
            SliderFBController.SliderGO.SetActive(true);
        });

        //DisplayTarget state ----------------------------------------------------------------------------------------------------------------------------------------------
        DisplayTarget.AddSpecificInitializationMethod(() =>
        {
            ObjManager.ActivateInitialTargets();
            AudioFBController.Play("ContinueBeep");
        });
        DisplayTarget.AddTimer(() => CurrentTrial.DisplayTargetDuration, DisplayDistractors);

        //DisplayDistractors state ----------------------------------------------------------------------------------------------------------------------------------------------
        DisplayDistractors.AddSpecificInitializationMethod(() =>
        {
            ObjManager.ActivateInitialDistractors();
            AudioFBController.Play("ContinueBeep");
        });
        DisplayDistractors.AddTimer(() => CurrentTrial.DisplayDistractorsDuration, Play);

        //Play state ----------------------------------------------------------------------------------------------------------------------------------------------
        Play.AddSpecificInitializationMethod(() =>
        {
            GiveRewardIfSliderFull = false;

            if (SelectionHandler.AllChoices.Count > 0)
                SelectionHandler.ClearChoices();


            ObjManager.ActivateInitialObjectsMovement();
            ObjManager.ActivateRemainingObjects();

            //reset it so the duration is 0 on exp display even if had one last trial
            OngoingSelection = null;
        });
        Play.AddUpdateMethod(() =>
        {
            ChosenGO = SelectionHandler.LastSuccessfulChoice?.SelectedGameObject;
            if (ChosenGO != null)
            {
                if(ChosenGO.TryGetComponent<KT_Object>(out ChosenObject))
                {
                    if (ChosenObject.IsTarget) //Selected a target
                    {
                        if(ChosenObject.WithinResponseWindow && ChosenObject.CurrentCycle.AfterFirstAnimation && !ChosenObject.SelectedDuringCurrentInterval) //Correct Selection
                        {
                            GiveRewardIfSliderFull = true;
                            StartCoroutine(ObjManager.ChangeObjColorCoroutine(ChosenObject, CurrentTask.CorrectSelectionColor, CurrentTask.ColorChangeDuration));
                            SliderFBController.UpdateSliderValue(ChosenObject.SliderChange * (1f / SliderGainSteps));
                            TargetSel_WithinResponseWindow_Trial++;
                            TargetSel_WithinResponseWindow_Block++;
                            CurrentTaskLevel.TargetSel_WithinResponseWindow_Task++;
                            Session.EventCodeManager.SendCodeThisFrame(TaskEventCodes["TargetSelectionWithinResponseWindow"]);

                            ChosenObject.CurrentIntervalSuccessful = true;
                        }
                        else //Incorrect Selection
                        {
                            StartCoroutine(ObjManager.ChangeObjColorCoroutine(ChosenObject, CurrentTask.IncorrectSelectionColor, CurrentTask.ColorChangeDuration));
                            SliderFBController.UpdateSliderValue(-ChosenObject.SliderChange * (1f / SliderGainSteps));

                            if(!ChosenObject.CurrentCycle.AfterFirstAnimation)
                            {
                                TargetSel_BeforeFirstAnim_Trial++;
                                TargetSel_BeforeFirstAnim_Block++;
                                CurrentTaskLevel.TargetSel_BeforeFirstAnim_Task++;
                                Session.EventCodeManager.SendCodeThisFrame(TaskEventCodes["TargetSelectionBeforeFirstAnim"]);
                            }
                            else if(ChosenObject.SelectedDuringCurrentInterval)
                            {
                                AdditionalTargetSel_Trial++;
                                AdditionalTargetSel_Block++;
                                CurrentTaskLevel.AdditionalTargetSel_Task++;
                            }
                            else if (ChosenObject.BeforeResponseWindow)
                            {
                                TargetSel_BeforeResponseWindow_Trial++;
                                TargetSel_BeforeResponseWindow_Block++;
                                CurrentTaskLevel.TargetSel_BeforeResponseWindow_Task++;
                                Session.EventCodeManager.SendCodeThisFrame(TaskEventCodes["TargetSelectionBeforeResponseWindow"]);

                            }
                            else if(ChosenObject.AfterResponseWindow)
                            {
                                TargetSel_AfterResponseWindow_Trial++;
                                TargetSel_AfterResponseWindow_Block++;
                                CurrentTaskLevel.TargetSel_AfterResponseWindow_Task++;
                                Session.EventCodeManager.SendCodeThisFrame(TaskEventCodes["TargetSelectionAfterResponseWindow"]);
                            }
                            else
                            {
                                Debug.LogWarning("ELSE STATEMENT");
                            }

                        }
                    }
                    else //Selected a Distractor
                    {
                        StartCoroutine(ObjManager.ChangeObjColorCoroutine(ChosenObject, CurrentTask.IncorrectSelectionColor, CurrentTask.ColorChangeDuration));
                        SliderFBController.UpdateSliderValue(-ChosenObject.SliderChange * (1f / SliderGainSteps));

                        DistractorSelections_Trial++;
                        DistractorSelections_Block++;
                        CurrentTaskLevel.DistractorSelections_Task++;
                        Session.EventCodeManager.SendCodeThisFrame(TaskEventCodes["DistractorSelection"]);
                    }



                    if(ChosenObject.CurrentCycle.AfterFirstAnimation && !ChosenObject.SelectedDuringCurrentInterval)
                    {
                        ChosenObject.SelectedDuringCurrentInterval = true;
                    }

                    Input.ResetInputAxes(); //Reset input?

                    CurrentTaskLevel.SetBlockSummaryString(); //update data on Exp Display

                    SelectionHandler.LastSuccessfulChoice = null;
                }
            }

            HandleSlider();

            OngoingSelection = SelectionHandler.OngoingSelection;

            //Update Exp Display with OngoingSelection Duration:
            if (OngoingSelection != null)
            {
                SetTrialSummaryString();
            }

            if (SelectionHandler.UnsuccessfulChoices.Count > 0 && !ChoiceFailed_Trial)
            {
                ChoiceFailed_Trial = true;
            }

        });
        Play.SpecifyTermination(() => ObjManager.DistractorList.Count < 1 && ObjManager.TargetList.Count < 1, ITI);
        Play.SpecifyTermination(() => ChoiceFailed_Trial, ITI, () =>
        {
            AbortCode = 8;
            Debug.LogWarning("Trial aborted due to unsuccessful selection");
        });

        //ITI state ----------------------------------------------------------------------------------------------------------------------------------------------
        ITI.AddTimer(() => itiDuration.value, FinishTrial);


        DefineTrialData();
        DefineFrameData();
    }

    private void SectionChanged()
    {
        if (SectionChanged_Frames == null)
            Debug.LogError("SECTION CHANGED FRAMES DICT IS NULL");

        int currentFrame = Time.frameCount;

        if (SectionChanged_Frames.ContainsKey(currentFrame)) //Increment if already had one this frame
            SectionChanged_Frames[currentFrame]++;
        else
            SectionChanged_Frames.Add(currentFrame, 1);


        Session.EventCodeManager.SendCodeThisFrame(TaskEventCodes["SectionChanged"]);

        SectionChanges_Trial++;
        SectionChanges_Block++;
        CurrentTaskLevel.SectionChanges_Task++;
        CurrentTaskLevel.SetBlockSummaryString(); //update data on exp display
    }

    private void TargetAnimationBegins()
    {
        if (TargetAnimations_Frames == null)
            Debug.LogWarning("TARGET ANIMATIONS FRAMES DICTIONARY IS NULL");

        int currentFrame = Time.frameCount;

        if(TargetAnimations_Frames.ContainsKey(currentFrame)) //Increment if already had one this frame
            TargetAnimations_Frames[currentFrame]++;
        else
            TargetAnimations_Frames.Add(currentFrame, 1);
        
        Session.EventCodeManager.SendCodeThisFrame(TaskEventCodes["TargetAnimationBegins"]);

        TargetAnimations_Trial++;
        TargetAnimations_Block++;
        CurrentTaskLevel.TargetAnimations_Task++;
        CurrentTaskLevel.SetBlockSummaryString(); //update data on exp display
    }

    private void DistractorAnimationBegins()
    {
        if (DistractorAnimations_Frames == null)
            Debug.LogWarning("DISTRACTOR ANIMATIONS FRAMES DICTIONARY IS NULL");
        
        int currentFrame = Time.frameCount;

        if (DistractorAnimations_Frames.ContainsKey(currentFrame)) //Increment if already had one this frame
            DistractorAnimations_Frames[currentFrame]++;
        else
            DistractorAnimations_Frames.Add(currentFrame, 1);
        
        Session.EventCodeManager.SendCodeThisFrame(TaskEventCodes["DistractorAnimationBegins"]);

        DistractorAnimations_Trial++;
        DistractorAnimations_Block++;
        CurrentTaskLevel.DistractorAnimations_Task++;
        CurrentTaskLevel.SetBlockSummaryString();
    }

    private void TargetAnimationMissed()
    {
        if (TargetAnimationsMissed_Frames == null)
            Debug.LogError("TARGET ANIMATIONS FRAMES DICTIONARY IS NULL");
        
        int currentFrame = Time.frameCount;

        //Debug.LogWarning("TARGET ANIMATION MISSED ON FRAME: " + Time.frameCount);

        if (TargetAnimationsMissed_Frames.ContainsKey(currentFrame)) //Increment if already had one this frame
            TargetAnimationsMissed_Frames[currentFrame]++;
        else
            TargetAnimationsMissed_Frames.Add(currentFrame, 1);
        
        Session.EventCodeManager.SendCodeThisFrame(TaskEventCodes["TargetAnimWithoutSelection"]);

        TargetIntervalsMissed_Trial++;
        TargetIntervalsMissed_Block++;
        CurrentTaskLevel.TargetIntervalsMissed_Task++;
        CurrentTaskLevel.SetBlockSummaryString(); //update data on exp display

    }

    private void DistractorAvoided()
    {
        if (DistractorsAvoided_Frames == null)
            Debug.LogError("DISTRACTORS AVOIDED FRAMES DICTIONARY IS NULL");
        
        int currentFrame = Time.frameCount;

        if (DistractorsAvoided_Frames.ContainsKey(currentFrame)) //Increment if already had one this frame
            DistractorsAvoided_Frames[currentFrame]++;
        else
            DistractorsAvoided_Frames.Add(currentFrame, 1);
        

        Session.EventCodeManager.SendCodeThisFrame(TaskEventCodes["DistractorRejection"]);

        DistractorRejections_Trial++;
        DistractorRejections_Block++;
        CurrentTaskLevel.DistractorRejections_Task++;
        CurrentTaskLevel.SetBlockSummaryString(); //update data on exp display

    }

    private string CheckIfSectionChanged()
    {
        string result = "0";

        if(SectionChanged_Frames != null)
        {
            if (SectionChanged_Frames.ContainsKey(Time.frameCount))
                result = SectionChanged_Frames[Time.frameCount].ToString();
        }

        return result;
    }

    private string CheckIfTargetAnimThisFrame()
    {
        string result = "0";

        if (TargetAnimations_Frames != null)
        {
            if (TargetAnimations_Frames.ContainsKey(Time.frameCount))
                result = TargetAnimations_Frames[Time.frameCount].ToString();
        }

        return result;
    }

    private string CheckIfDistractorAnimThisFrame()
    {
        string result = "0";

        if (DistractorAnimations_Frames != null)
        {
            if (DistractorAnimations_Frames.ContainsKey(Time.frameCount))
                result = DistractorAnimations_Frames[Time.frameCount].ToString();
        }

        return result;
    }

    private string CheckIfTargetAnimMissedThisFrame()
    {
        string result = "0";

        if (TargetAnimationsMissed_Frames != null)
        {
            if (TargetAnimationsMissed_Frames.ContainsKey(Time.frameCount))
                result = TargetAnimationsMissed_Frames[Time.frameCount].ToString();
        }

        return result;

    }

    private string CheckIfDistractorAvoidedThisFrame()
    {
        string result = "0";

        if (DistractorsAvoided_Frames != null)
        {
            if (DistractorsAvoided_Frames.ContainsKey(Time.frameCount))
                result = DistractorsAvoided_Frames[Time.frameCount].ToString();
        }

        return result;
    }

    private void HandleSlider()
    {
        if (GiveRewardIfSliderFull)
        {
            if (SliderFBController.isSliderBarFull() && !AudioFBController.IsPlaying())
            {
                GiveReward();

                SliderFBController.ResetSliderBarFull();
                SliderFBController.ConfigureSlider(sliderSize.value, CurrentTrial.SliderInitialValue * (1f / SliderGainSteps));
                GiveRewardIfSliderFull = false;
                SliderBarCompletions_Trial++;
                SliderBarCompletions_Block++;
                CurrentTaskLevel.SliderBarCompletions_Task++;
            }
        }
    }

    public override void FinishTrialCleanup()
    {
        if(ObjManager != null)
        {
            UnsubscribeFromObjEvents();
            ObjManager.DestroyExistingObjects();
        }

        SliderFBController.SliderGO.SetActive(false);
        SliderFBController.SliderHaloGO.SetActive(false);

        if (AbortCode == 0)
        {
            TrialCompletions_Block++;
            CurrentTaskLevel.TrialsCompleted_Task++;
            CurrentTaskLevel.SetBlockSummaryString();
        }
        else
        {
            CurrentTaskLevel.NumAbortedTrials_InBlock++;
            CurrentTaskLevel.NumAbortedTrials_InTask++;
        }
    }

    public override void ResetTrialVariables()
    {
        SliderGainSteps = 0;
        SliderFBController.ResetSliderBarFull();

        DistractorAnimations_Frames = new Dictionary<int, int>();
        DistractorsAvoided_Frames = new Dictionary<int, int>();
        TargetAnimationsMissed_Frames = new Dictionary<int, int>();
        TargetAnimations_Frames = new Dictionary<int, int>();

        AdditionalTargetSel_Trial = 0;
        TargetSel_BeforeResponseWindow_Trial = 0;
        SectionChanges_Trial = 0;
        TargetAnimations_Trial = 0;
        DistractorAnimations_Trial = 0;
        TargetSel_WithinResponseWindow_Trial = 0;
        TargetSel_AfterResponseWindow_Trial = 0;
        TargetSel_BeforeFirstAnim_Trial = 0;
        TargetIntervalsMissed_Trial = 0;
        DistractorSelections_Trial = 0;
        DistractorRejections_Trial = 0;
        SliderBarCompletions_Trial = 0;
    }

    public void ResetBlockVariables()
    {
        TrialCompletions_Block = 0;

        AdditionalTargetSel_Block = 0;
        TargetSel_BeforeResponseWindow_Block = 0;
        SectionChanges_Block = 0;
        TargetAnimations_Block = 0;
        DistractorAnimations_Block = 0;
        TargetSel_WithinResponseWindow_Block = 0;
        TargetSel_AfterResponseWindow_Block = 0;
        TargetSel_BeforeFirstAnim_Block = 0;
        DistractorSelections_Block = 0;
        DistractorRejections_Block = 0;
        TargetIntervalsMissed_Block = 0;
        SliderBarCompletions_Block = 0;
    }

    private void CalculateSliderSteps()
    {
        foreach (int sliderGain in CurrentTrial.SliderGain)
        {
            SliderGainSteps += sliderGain;
        }
        SliderGainSteps += CurrentTrial.SliderInitialValue;
    }


    void GiveReward()
    {

        if (ChosenObject != null)
        {
            if(Session.SyncBoxController != null)
            {
                CurrentTaskLevel.NumRewardPulses_InBlock += CurrentTrial.NumPulses;
                CurrentTaskLevel.NumRewardPulses_InTask += CurrentTrial.NumPulses;

                StartCoroutine(Session.SyncBoxController.SendRewardPulses(ChosenObject.RewardPulsesBySec != null ? ChosenObject.CurrentRewardValue : CurrentTrial.NumPulses, CurrentTrial.PulseSize));
            }
        }
        else
            Debug.Log("CHOSEN OBJ IS NULL WHEN TRYING TO SEND REWARD");

    }

    private void SetTrialSummaryString()
    {
        TrialSummaryString = "Trial #" + (TrialCount_InBlock + 1) + " In Block" +
                             "\nNum Targets: " + TrialObjects.Where(obj => obj != null && obj.IsTarget && obj.gameObject.activeInHierarchy).Count() +
                             "\nNum Distractors: " + TrialObjects.Where(obj => obj != null && !obj.IsTarget && obj.gameObject.activeInHierarchy).Count() +
                             "\nOngoingSelection: " + (OngoingSelection == null ? "" : OngoingSelection.Duration.Value.ToString("F2") + " s");
    }

    private void DefineTrialData()
    {
        TrialData.AddDatum("TrialID", () => CurrentTrial.TrialID);
        TrialData.AddDatum("ObjectIndices", () => String.Join(", ", CurrentTrial.TrialObjectIndices));
        TrialData.AddDatum("DisplayTargetDuration", () => CurrentTrial.DisplayTargetDuration);
        TrialData.AddDatum("DisplayDistractorsDuration", () => CurrentTrial.DisplayDistractorsDuration);
        TrialData.AddDatum("SliderInitialValue", () => CurrentTrial.SliderInitialValue);
        TrialData.AddDatum("SliderGain", () => String.Join(", ", CurrentTrial.SliderGain));
        TrialData.AddDatum("SliderBarCompletions", () => SliderBarCompletions_Trial);

        TrialData.AddDatum("SectionChanges", () => SectionChanges_Trial);
        TrialData.AddDatum("TargetSel_BeforeFirstAnim", () => TargetSel_BeforeFirstAnim_Trial);
        TrialData.AddDatum("TargetSel_BeforeResponseWindow", () => TargetSel_BeforeResponseWindow_Trial);
        TrialData.AddDatum("TargetSel_WithinResponseWindow", () => TargetSel_WithinResponseWindow_Trial);
        TrialData.AddDatum("TargetSel_AfterResponseWindow", () => TargetSel_AfterResponseWindow_Trial);
        TrialData.AddDatum("AdditionalTargetSel_WithinResponseWindow", () => TargetSel_AfterResponseWindow_Trial);
        TrialData.AddDatum("TargetIntervalsMissed", () => TargetIntervalsMissed_Trial);
        TrialData.AddDatum("DistractorSelections", () => DistractorSelections_Trial);
        TrialData.AddDatum("DistractorRejections", () => DistractorRejections_Trial);
    }

    private void DefineFrameData()
    {
        FrameData.AddDatum("StartButton", () => StartButton != null && StartButton.activeInHierarchy ? "Active" : "NotActive");

        FrameData.AddDatum("Section", () => GetSection());
        FrameData.AddDatum("TimeSinceLastSectionChange", () => GetTimeSinceLastSectionChange());
        FrameData.AddDatum("SectionChanged", () => CheckIfSectionChanged());

        FrameData.AddDatum("TargetAnimationsStarted", () => CheckIfTargetAnimThisFrame());
        FrameData.AddDatum("DistractorAnimationsStarted", () => CheckIfDistractorAnimThisFrame());

        FrameData.AddDatum("TargetAnimationsMissed", () => CheckIfTargetAnimMissedThisFrame());
        FrameData.AddDatum("DistractorsAvoided", () => CheckIfDistractorAvoidedThisFrame());

        FrameData.AddDatum("ActiveObjectIndices", () => GetObjIndicesString());
        FrameData.AddDatum("ObjectPositions", () => GetObjPositionsString());
        FrameData.AddDatum("ObjectAnimStatus", () => GetObjAnimStatus());
        FrameData.AddDatum("ObjectMouthAngles", () => GetMouthAnglesString());
    }

    private string GetSection()
    {
        string result = "";

        if (ObjManager != null)
            result = ObjManager.GetSection();
        
        return result;
    }

    private string GetTimeSinceLastSectionChange()
    {
        string result = "-1";

        if (ObjManager != null)
            result = ObjManager.GetTimeSinceLastSectionChange().ToString("F2");

        return result;
    }



    private string GetMouthAnglesString()
    {
        if (TrialObjects == null)
            return "[]";

        List<string> angles = new List<string>();

        foreach(KT_Object obj in TrialObjects)
        {
            if(obj != null && obj.gameObject.activeInHierarchy)
            {
                angles.Add(obj.CurrentMouthAngle.ToString());
            }
        }
        return angles.Count < 1 ? "[]" : $"[{string.Join(", ", angles)}]";
    }

    private string GetObjAnimStatus()
    {
        if (TrialObjects == null)
            return "[]";

        List<string> statuses = new List<string>();

        foreach (var obj in TrialObjects)
        {
            if (obj != null && obj.gameObject.activeInHierarchy)
            {
                statuses.Add(obj.CurrentAnimationStatus.ToString());
            }
        }
        return statuses.Count < 1 ? "[]" : $"[{string.Join(", ", statuses)}]";
    }

    private string GetObjIndicesString()
    {
        if (TrialObjects == null)
            return "[]";

        List<string> names = new List<string>();

        foreach (var obj in TrialObjects)
        {
            if (obj != null)
            {
                if (obj.gameObject.activeInHierarchy)
                    names.Add(obj.Index.ToString());
            }
        }
        return names.Count < 1 ? "[]" : $"[{string.Join(", ", names)}]";
    }

    private string GetObjPositionsString()
    {
        if (TrialObjects == null)
            return "[]";

        List<string> positions = new List<string>();

        foreach (var obj in TrialObjects)
        {
            if (obj != null)
            {
                if (obj.gameObject.activeInHierarchy)
                    positions.Add(obj.transform.position.ToString());
            }
        }
        return positions.Count < 1 ? "[]" : $"[{string.Join(", ", positions)}]";
    }


    private void LoadConfigUIVariables()
    {
        timeBeforeChoiceStarts = ConfigUiVariables.get<ConfigNumber>("timeBeforeChoiceStarts");
        totalChoiceDuration = ConfigUiVariables.get<ConfigNumber>("totalChoiceDuration");
        itiDuration = ConfigUiVariables.get<ConfigNumber>("itiDuration");
        sliderSize = ConfigUiVariables.get<ConfigNumber>("sliderSize");
        sliderFlashingDuration = ConfigUiVariables.get<ConfigNumber>("sliderFlashingDuration");
        sliderUpdateDuration = ConfigUiVariables.get<ConfigNumber>("sliderUpdateDuration");
    }


    private void SubscribeToObjectEvents()
    {
        if (ObjManager != null)
        {
            ObjManager.OnSectionChanged += SectionChanged;
            ObjManager.OnTargetMissed += TargetAnimationMissed; //subscribe to MissedInterval Event for data logging purposes
            ObjManager.OnDistractorAvoided += DistractorAvoided; //subscribe to DistractorAvoided Event for data logging purposes

            ObjManager.OnTargetAnimationStarted += TargetAnimationBegins;
            ObjManager.OnDistractorAnimationStarted += DistractorAnimationBegins;
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromObjEvents();
    }

    private void UnsubscribeFromObjEvents()
    {
        if (ObjManager != null)
        {
            ObjManager.OnSectionChanged -= SectionChanged;
            ObjManager.OnTargetMissed -= TargetAnimationMissed;
            ObjManager.OnDistractorAvoided -= DistractorAvoided;
            ObjManager.OnTargetAnimationStarted -= TargetAnimationBegins;
            ObjManager.OnDistractorAnimationStarted -= DistractorAnimationBegins;
        }
    }



}
