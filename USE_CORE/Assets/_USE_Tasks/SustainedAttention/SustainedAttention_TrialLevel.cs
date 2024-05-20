using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using USE_States;
using USE_ExperimentTemplate_Trial;
using SustainedAttention_Namespace;
using ConfigDynamicUI;
using System.Linq;
using System;
using UnityEditor;
using UnityEngine.UIElements;

public class SustainedAttention_TrialLevel : ControlLevel_Trial_Template
{
    public SustainedAttention_TrialDef CurrentTrial => GetCurrentTrialDef<SustainedAttention_TrialDef>();
    public SustainedAttention_TaskLevel CurrentTaskLevel => GetTaskLevel<SustainedAttention_TaskLevel>();
    public SustainedAttention_TaskDef CurrentTask => GetTaskDef<SustainedAttention_TaskDef>();

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
    public GameObject SustainedAttention_CanvasGO;
    public GameObject BordersGO;

    private SA_ObjectManager ObjManager;

    private GameObject StartButton;

    private GameObject ChosenGO = null;
    private SA_Object ChosenObject = null;

    private int SliderGainSteps;

    [HideInInspector] public ConfigNumber itiDuration, minObjectTouchDuration, maxObjectTouchDuration, sliderFlashingDuration, sliderUpdateDuration, sliderSize;

    private bool GiveRewardIfSliderFull = false;

    private readonly float HaloDepth = 10f;
    private float HaloDuration = .15f; //make configurable later

    List<SA_Object> TrialObjects;



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
                    StartButton = Session.USE_StartButton.CreateStartButton(SustainedAttention_CanvasGO.GetComponent<Canvas>(), CurrentTask.StartButtonPosition, CurrentTask.StartButtonScale);
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

            ObjManager = gameObject.AddComponent<SA_ObjectManager>();
            ObjManager.MaxTouchDuration = maxObjectTouchDuration.value;
            ObjManager.TaskEventCodes = TaskEventCodes;
            ObjManager.SetObjectParent(SustainedAttention_CanvasGO.transform);
            ObjManager.OnTargetIntervalMissed += TargetIntervalMissed; //subscribe to MissedInterval Event for data logging purposes
            ObjManager.OnDistractorAvoided += DistractorAvoided; //subscribe to DistractorAvoided Event for data logging purposes

            List<SA_Object_ConfigValues> trialObjectsConfigValues = new List<SA_Object_ConfigValues>();

            foreach (int objIndex in CurrentTrial.TrialObjectIndices)
                trialObjectsConfigValues.Add(CurrentTaskLevel.SA_Objects_ConfigValues[objIndex]);
            
            TrialObjects = ObjManager.CreateObjects(trialObjectsConfigValues);

        });
        SetupTrial.SpecifyTermination(() => true, InitTrial);

        var Handler = Session.SelectionTracker.SetupSelectionHandler("trial", "TouchShotgun", Session.MouseTracker, InitTrial, Play); //Setup Handler
        TouchFBController.EnableTouchFeedback(Handler, CurrentTask.TouchFeedbackDuration, CurrentTask.StartButtonScale * 15, SustainedAttention_CanvasGO, false); //Enable Touch Feedback

        //InitTrial state ----------------------------------------------------------------------------------------------------------------------------------------------
        InitTrial.AddSpecificInitializationMethod(() =>
        {
            SetTrialSummaryString();

            if (Handler.AllSelections.Count > 0)
                Handler.ClearSelections();
            Handler.MinDuration = minObjectTouchDuration.value;
            Handler.MaxDuration = maxObjectTouchDuration.value;
        });
        InitTrial.SpecifyTermination(() => Handler.LastSuccessfulSelectionMatchesStartButton(), DisplayTarget, () =>
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
            ObjManager.ActivateTargets();
            AudioFBController.Play("ContinueBeep");
        });
        DisplayTarget.AddTimer(() => CurrentTrial.DisplayTargetDuration, DisplayDistractors);

        //DisplayDistractors state ----------------------------------------------------------------------------------------------------------------------------------------------
        DisplayDistractors.AddSpecificInitializationMethod(() =>
        {
            ObjManager.ActivateDistractors();
            AudioFBController.Play("ContinueBeep");
        });
        DisplayDistractors.AddTimer(() => CurrentTrial.DisplayDistractorsDuration, Play);

        //Play state ----------------------------------------------------------------------------------------------------------------------------------------------
        Play.AddSpecificInitializationMethod(() =>
        {
            GiveRewardIfSliderFull = false;
            if (Handler.AllSelections.Count > 0)
                Handler.ClearSelections();

            ObjManager.ActivateObjectMovement();
        });
        Play.AddUpdateMethod(() =>
        {
            ChosenGO = Handler.LastSuccessfulSelection?.SelectedGameObject;
            if (ChosenGO != null)
            {
                ChosenObject = ChosenGO.GetComponent<SA_Object>();
                if(ChosenObject != null)
                {
                    HaloFBController.SetParticleHaloSize(.5f);
                    HaloFBController.SetCircleHaloSize(.01f * ChosenObject.Size);

                    if (ChosenObject.IsTarget)
                    {
                        if(ChosenObject.WithinDuration && !ChosenObject.CurrentCycle.selectedDuringCurrentInterval)
                        {
                            GiveRewardIfSliderFull = true;
                            HaloFBController.ShowPositive(ChosenGO, CurrentTrial.ParticleHaloActive, CurrentTrial.CircleHaloActive, HaloDuration, HaloDepth);
                            SliderFBController.UpdateSliderValue(ChosenObject.SliderChange * (1f / SliderGainSteps));
                            SuccessfulTargetSelections_Block++;
                            CurrentTaskLevel.SuccessfulTargetSelections_Task++;
                            Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["SuccessfulTargetSelection"]);
                            Session.EventCodeManager.AddToFrameEventCodeBuffer("CorrectResponse");
                        }
                        else
                        {
                            HaloFBController.ShowNegative(ChosenGO, CurrentTrial.ParticleHaloActive, CurrentTrial.CircleHaloActive, HaloDuration, HaloDepth);
                            SliderFBController.UpdateSliderValue(-ChosenObject.SliderChange * (1f / SliderGainSteps));

                            if(ChosenObject.CurrentCycle.selectedDuringCurrentInterval)
                            {
                                AdditionalTargetSelections_Block++;
                                CurrentTaskLevel.AdditionalTargetSelections_Task++;
                                Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["AdditionalTargetSelection"]);
                            }
                            else
                            {
                                if (ChosenObject.CurrentCycle.firstIntervalStarted)
                                {
                                    UnsuccessfulTargetSelections_Block++;
                                    CurrentTaskLevel.UnsuccessfulTargetSelections_Task++;
                                    Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["UnsuccessfulTargetSelection"]);
                                    Session.EventCodeManager.AddToFrameEventCodeBuffer("IncorrectResponse");

                                }
                                else
                                {
                                    TargetSelectionsBeforeFirstAnim_Block++;
                                    CurrentTaskLevel.TargetSelectionsBeforeFirstAnim_Task++;
                                    Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["TargetSelectionBeforeFirstAnim"]);

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
                        Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["DistractorSelection"]);
                    }

                    CurrentTaskLevel.CalculateBlockSummaryString(); //update data on Exp Display

                    if(ChosenObject.CurrentCycle.firstIntervalStarted)
                        ChosenObject.CurrentCycle.selectedDuringCurrentInterval = true;

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
        Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["TargetAnimWithoutSelection"]);
        Session.EventCodeManager.AddToFrameEventCodeBuffer("NoChoice");

    }
    private void DistractorAvoided()
    {
        DistractorRejections_Block++;
        CurrentTaskLevel.DistractorRejections_Task++;
        CurrentTaskLevel.CalculateBlockSummaryString(); //update data on exp display
        Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["DistractorRejection"]);

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
        TrialSummaryString = "<b>Trial #" + (TrialCount_InBlock + 1) + " In Block" + "</b>" +
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
