using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using USE_States;
using USE_ExperimentTemplate_Trial;
using KeepTrack_Namespace;
using ConfigDynamicUI;
using System.Linq;
using System;
using SelectionTracking;
using TMPro.SpriteAssetUtilities;


public class KeepTrack_TrialLevel : ControlLevel_Trial_Template
{
    public KeepTrack_TrialDef CurrentTrial => GetCurrentTrialDef<KeepTrack_TrialDef>();
    public KeepTrack_TaskLevel CurrentTaskLevel => GetTaskLevel<KeepTrack_TaskLevel>();
    public KeepTrack_TaskDef CurrentTask => GetTaskDef<KeepTrack_TaskDef>();

    //DATA:
    [HideInInspector] public int TrialCompletions_Block;
    [HideInInspector] public int SuccessfulTargetSelections_Block = 0;
    [HideInInspector] public int UnsuccessfulTargetSelections_Block = 0;
    [HideInInspector] public int TargetSelectionsBeforeFirstAnim_Block = 0;
    [HideInInspector] public int TargetAnimsWithoutSelection_Block = 0;
    [HideInInspector] public int AdditionalTargetSelections_Block = 0;
    [HideInInspector] public int DistractorSelections_Block = 0;
    [HideInInspector] public int DistractorRejections_Block = 0;
    [HideInInspector] public int SliderBarCompletions_Block = 0;

    //Set in Inspector:
    public GameObject KeepTrack_CanvasGO;
    public GameObject BordersGO;

    private KT_ObjectManager ObjManager;

    private GameObject StartButton;

    private GameObject ChosenGO = null;
    private KT_Object ChosenObject = null;

    private int SliderGainSteps;
    private bool GiveRewardIfSliderFull = false;

    [HideInInspector] public ConfigNumber itiDuration, minObjectTouchDuration, maxObjectTouchDuration, sliderFlashingDuration, sliderUpdateDuration, sliderSize;


    private float HaloDepth = 15f;
    private float HaloDuration = .15f;

    List<KT_Object> TrialObjects;



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
            SliderFBController.InitializeSlider();

            HaloFBController.SetCircleHaloIntensity(2f);

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
            ObjManager.MaxTouchDuration = maxObjectTouchDuration.value;
            ObjManager.TaskEventCodes = TaskEventCodes;
            ObjManager.SetObjectParent(KeepTrack_CanvasGO.transform);
            ObjManager.OnTargetIntervalMissed += TargetIntervalMissed; //subscribe to MissedInterval Event for data logging purposes
            ObjManager.OnDistractorAvoided += DistractorAvoided; //subscribe to DistractorAvoided Event for data logging purposes

            List<KT_Object_ConfigValues> trialObjectsConfigValues = new List<KT_Object_ConfigValues>();

            foreach (int objIndex in CurrentTrial.TrialObjectIndices)
            {
                trialObjectsConfigValues.Add(CurrentTaskLevel.KT_Objects_ConfigValues[objIndex]);
            }
            
            TrialObjects = ObjManager.CreateObjects(trialObjectsConfigValues);

        });
        SetupTrial.SpecifyTermination(() => true, InitTrial);

        var Handler = Session.SelectionTracker.SetupSelectionHandler("trial", "TouchShotgun", Session.MouseTracker, InitTrial, Play); //Setup Handler
        TouchFBController.EnableTouchFeedback(Handler, CurrentTask.TouchFeedbackDuration, CurrentTask.StartButtonScale * 15, KeepTrack_CanvasGO, false); //Enable Touch Feedback

        //InitTrial state ----------------------------------------------------------------------------------------------------------------------------------------------
        InitTrial.AddSpecificInitializationMethod(() =>
        {
            SetTrialSummaryString();

            if (Handler.AllSelections.Count > 0)
                Handler.ClearSelections();
            Handler.MinDuration = minObjectTouchDuration.value;
            Handler.MaxDuration = maxObjectTouchDuration.value;
        });
        InitTrial.SpecifyTermination(() => CurrentTask.RunSimulation, DisplayTarget);
        InitTrial.SpecifyTermination(() => Handler.LastSuccessfulSelectionMatchesStartButton(), DisplayTarget);
        InitTrial.AddDefaultTerminationMethod(() =>
        {
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
            if (Handler.AllSelections.Count > 0)
                Handler.ClearSelections();


            ObjManager.ActivateInitialObjectsMovement();
            ObjManager.ActivateRemainingObjects();

        });
        Play.AddUpdateMethod(() =>
        {
            ChosenGO = Handler.LastSuccessfulSelection?.SelectedGameObject;
            if (ChosenGO != null)
            {
                if(ChosenGO.TryGetComponent<KT_Object>(out ChosenObject))
                {
                    if (ChosenObject.IsTarget)
                    {
                        if(ChosenObject.WithinDuration && !ChosenObject.CurrentCycle.selectedDuringCurrentInterval)
                        {
                            GiveRewardIfSliderFull = true;
                            HaloFBController.ShowPositive(ChosenGO, CurrentTrial.ParticleHaloActive, CurrentTrial.CircleHaloActive, HaloDuration, HaloDepth);
                            SliderFBController.UpdateSliderValue(ChosenObject.SliderChange * (1f / SliderGainSteps));
                            SuccessfulTargetSelections_Block++;
                            CurrentTaskLevel.SuccessfulTargetSelections_Task++;
                            Session.EventCodeManager.SendCodeImmediate(TaskEventCodes["SuccessfulTargetSelection"]);
                            Session.EventCodeManager.SendCodeImmediate("CorrectResponse");
                        }
                        else
                        {
                            HaloFBController.ShowNegative(ChosenGO, CurrentTrial.ParticleHaloActive, CurrentTrial.CircleHaloActive, HaloDuration, HaloDepth);
                            SliderFBController.UpdateSliderValue(-ChosenObject.SliderChange * (1f / SliderGainSteps));

                            if(ChosenObject.CurrentCycle.selectedDuringCurrentInterval)
                            {
                                AdditionalTargetSelections_Block++;
                                CurrentTaskLevel.AdditionalTargetSelections_Task++;
                                Session.EventCodeManager.SendCodeImmediate(TaskEventCodes["AdditionalTargetSelection"]);
                            }
                            else
                            {
                                if (ChosenObject.CurrentCycle.firstIntervalStarted)
                                {
                                    UnsuccessfulTargetSelections_Block++;
                                    CurrentTaskLevel.UnsuccessfulTargetSelections_Task++;
                                    Session.EventCodeManager.SendCodeImmediate(TaskEventCodes["UnsuccessfulTargetSelection"]);
                                    Session.EventCodeManager.SendCodeImmediate("IncorrectResponse");

                                }
                                else
                                {
                                    TargetSelectionsBeforeFirstAnim_Block++;
                                    CurrentTaskLevel.TargetSelectionsBeforeFirstAnim_Task++;
                                    Session.EventCodeManager.SendCodeImmediate(TaskEventCodes["TargetSelectionBeforeFirstAnim"]);
                                }
                            }
                        }
                    }
                    else //Selected a Distractor
                    {
                        HaloFBController.ShowNegative(ChosenGO, CurrentTrial.ParticleHaloActive, CurrentTrial.CircleHaloActive, HaloDuration, HaloDepth);
                        SliderFBController.UpdateSliderValue(-ChosenObject.SliderChange * (1f / SliderGainSteps));
                        DistractorSelections_Block++;
                        CurrentTaskLevel.DistractorSelections_Task++;
                        Session.EventCodeManager.SendCodeImmediate(TaskEventCodes["DistractorSelection"]);
                    }

                    CurrentTaskLevel.CalculateBlockSummaryString(); //update data on Exp Display

                    if(ChosenObject.CurrentCycle.firstIntervalStarted && !ChosenObject.CurrentCycle.selectedDuringCurrentInterval)
                    {
                        ChosenObject.CurrentCycle.selectedDuringCurrentInterval = true;
                    }

                    Input.ResetInputAxes(); //Reset input?

                    Handler.LastSuccessfulSelection = null;
                }
            }

            HandleSlider();
        });
        Play.SpecifyTermination(() => ObjManager.DistractorList.Count < 1 && ObjManager.TargetList.Count < 1, ITI);

        //ITI state ----------------------------------------------------------------------------------------------------------------------------------------------
        ITI.AddTimer(() => itiDuration.value, FinishTrial);


        DefineTrialData();
        DefineFrameData();
    }

    //Helper Method:
    private void OnDestroy()
    {
        if(ObjManager != null)
        {
            ObjManager.OnTargetIntervalMissed -= TargetIntervalMissed; //UNsubscribe to MissedInterval Event
            ObjManager.OnDistractorAvoided -= DistractorAvoided; //UNsubscribe to DistractorAvoided Event
        }
    }
    private void TargetIntervalMissed()
    {
        TargetAnimsWithoutSelection_Block++;
        CurrentTaskLevel.TargetAnimsWithoutSelection_Task++;
        CurrentTaskLevel.CalculateBlockSummaryString(); //update data on exp display
        Session.EventCodeManager.SendCodeImmediate(TaskEventCodes["TargetAnimWithoutSelection"]);
        Session.EventCodeManager.SendCodeImmediate("NoChoice");

    }
    private void DistractorAvoided()
    {
        DistractorRejections_Block++;
        CurrentTaskLevel.DistractorRejections_Task++;
        CurrentTaskLevel.CalculateBlockSummaryString(); //update data on exp display
        Session.EventCodeManager.SendCodeImmediate(TaskEventCodes["DistractorRejection"]);

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
                SliderBarCompletions_Block++;
                CurrentTaskLevel.SliderBarCompletions_Task++;
            }
        }
    }

    public override void FinishTrialCleanup()
    {
        if(ObjManager != null)
        {
            ObjManager.OnTargetIntervalMissed -= TargetIntervalMissed; //Unsubscribe from MissedInterval Event
            ObjManager.DestroyExistingObjects();
        }

        SliderFBController.SliderGO.SetActive(false);
        SliderFBController.SliderHaloGO.SetActive(false);

        if (AbortCode == 0)
        {
            TrialCompletions_Block++;
            CurrentTaskLevel.TrialsCompleted_Task++;
            CurrentTaskLevel.CalculateBlockSummaryString();
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
    }

    public void ResetBlockVariables()
    {
        TrialCompletions_Block = 0;

        SuccessfulTargetSelections_Block = 0;
        UnsuccessfulTargetSelections_Block = 0;
        TargetSelectionsBeforeFirstAnim_Block = 0;
        DistractorSelections_Block = 0;
        DistractorRejections_Block = 0;
        TargetAnimsWithoutSelection_Block = 0;
        AdditionalTargetSelections_Block = 0;
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
        CurrentTaskLevel.NumRewardPulses_InBlock += CurrentTrial.NumPulses;
        CurrentTaskLevel.NumRewardPulses_InTask += CurrentTrial.NumPulses;
        Session.SyncBoxController?.SendRewardPulses(CurrentTrial.NumPulses, CurrentTrial.PulseSize);
    }

    private void SetTrialSummaryString()
    {
        TrialSummaryString = "Trial #" + (TrialCount_InBlock + 1) + " In Block" +
                             "\nNum Targets: " + TrialObjects.Where(obj => obj.IsTarget).Count() +
                             "\nNum Distractors: " + TrialObjects.Where(obj => !obj.IsTarget).Count();

    }

    private void DefineTrialData()
    {
        TrialData.AddDatum("TrialID", () => CurrentTrial.TrialID);
        TrialData.AddDatum("ObjectIndices", () => String.Join(", ", CurrentTrial.TrialObjectIndices));
        TrialData.AddDatum("DisplayTargetDuration", () => CurrentTrial.DisplayTargetDuration);
        TrialData.AddDatum("DisplayDistractorsDuration", () => CurrentTrial.DisplayDistractorsDuration);
        TrialData.AddDatum("SliderInitialValue", () => CurrentTrial.SliderInitialValue);
        TrialData.AddDatum("SliderGain", () => String.Join(", ", CurrentTrial.SliderGain));
    }

    private void DefineFrameData()
    {
        FrameData.AddDatum("StartButton", () => StartButton != null && StartButton.activeInHierarchy ? "Active" : "NotActive");

        FrameData.AddDatum("ObjectNames", () => GetObjNamesString());
        FrameData.AddDatum("ObjectPositions", () => GetObjPositionsString());
        FrameData.AddDatum("ObjectAnimStatus", () => GetObjAnimStatus());
        FrameData.AddDatum("ObjectMouthAngles", () => GetMouthAnglesString());
        FrameData.AddDatum("ObjectSelections", () => GetObjSelectionString());
        FrameData.AddDatum("SelectionCorrectness", () => GetSelectionCorrectnessString());
    }

    private string GetObjSelectionString()
    {
        if (TrialObjects == null)
            return "[]";
        
        List<string> selections = new List<string>();
        
        // instead of obj.transform.position, get obj.selectionstatus (add this field in KT_objectmanager!)
        foreach (var obj in TrialObjects)
        {
            if (obj != null)
            {
                if (obj.gameObject.activeInHierarchy)
                    selections.Add(obj.ObjectSelected.ToString());
            }
        }
        return selections.Count < 1 ? "[]" : $"[{string.Join(", ", selections)}]";
    }
    
    private string GetSelectionCorrectnessString()
    {
        if (TrialObjects == null)
            return "[]";
        
        List<string> correctnesses = new List<string>();
        
        foreach (var obj in TrialObjects)
        {
            if (obj != null)
            {
                if (obj.gameObject.activeInHierarchy)
                {
                    if (!obj.ObjectSelected)
                        correctnesses.Add("NotSelected");
                    else
                    {
                        if (obj.IsTarget)
                        {
                            if (obj.WithinDuration)
                                correctnesses.Add("CorrectSelection");
                            else
                                correctnesses.Add("Target_OutOfDuration");
                        }
                        else
                        {
                            if (obj.WithinDuration)
                                correctnesses.Add("Distractor_InDuration");
                            else
                                correctnesses.Add("Distractor_OutOfDuration");
                        }
                    }
                }
            }
        }
        return correctnesses.Count < 1 ? "[]" : $"[{string.Join(", ", correctnesses)}]";
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

    private string GetObjNamesString()
    {
        if (TrialObjects == null)
            return "[]";

        List<string> names = new List<string>();

        foreach (var obj in TrialObjects)
        {
            if (obj != null)
            {
                if (obj.gameObject.activeInHierarchy)
                    names.Add(obj.ObjectName);
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
        minObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("minObjectTouchDuration");
        maxObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("maxObjectTouchDuration");
        itiDuration = ConfigUiVariables.get<ConfigNumber>("itiDuration");
        sliderSize = ConfigUiVariables.get<ConfigNumber>("sliderSize");
        sliderFlashingDuration = ConfigUiVariables.get<ConfigNumber>("sliderFlashingDuration");
        sliderUpdateDuration = ConfigUiVariables.get<ConfigNumber>("sliderUpdateDuration");
    }



}
