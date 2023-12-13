using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using USE_States;
using USE_ExperimentTemplate_Trial;
using SustainedAttention_Namespace;
using ConfigDynamicUI;
using System.Linq;


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
    [HideInInspector] public int DistractorSelections_Block = 0;
    [HideInInspector] public int DistractorRejections_Block = 0;
    [HideInInspector] public int AdditionalTargetSelections_Block = 0;

    //Set in Inspector:
    public GameObject SustainedAttention_CanvasGO;
    public GameObject BordersGO;

    private SA_ObjectManager ObjectManager;

    private GameObject StartButton;

    private GameObject ChosenGO = null;

    private int SliderGainSteps, SliderLossSteps;

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

            HaloFBController.SetHaloIntensity(1f);

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

            if (ObjectManager != null)
                Destroy(ObjectManager);

            ObjectManager = gameObject.AddComponent<SA_ObjectManager>();
            ObjectManager.SetObjectParent(SustainedAttention_CanvasGO.transform);
            ObjectManager.OnTargetIntervalMissed += TargetIntervalMissed; //subscribe to MissedInterval Event for data logging purposes
            ObjectManager.OnDistractorAvoided += DistractorAvoided; //subscribe to DistractorAvoided Event for data logging purposes

            List<SA_Object_ConfigValues> trialObjectsConfigValues = new List<SA_Object_ConfigValues>();

            foreach (int objIndex in CurrentTrial.TrialObjectIndices)
                trialObjectsConfigValues.Add(CurrentTaskLevel.SA_Objects_ConfigValues[objIndex]);
            
            TrialObjects = ObjectManager.CreateObjects(trialObjectsConfigValues);

        });
        SetupTrial.SpecifyTermination(() => true, InitTrial);

        var Handler = Session.SelectionTracker.SetupSelectionHandler("trial", "MouseButton0Click", Session.MouseTracker, InitTrial, Play); //Setup Handler
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
            Session.EventCodeManager.AddToFrameEventCodeBuffer("SliderFbController_SliderReset");
        });

        //DisplayTarget state ----------------------------------------------------------------------------------------------------------------------------------------------
        DisplayTarget.AddSpecificInitializationMethod(() =>
        {
            ObjectManager.ActivateTargets();
            AudioFBController.Play("ContinueBeep");
        });
        DisplayTarget.AddTimer(() => CurrentTrial.DisplayTargetDuration, DisplayDistractors);

        //DisplayDistractors state ----------------------------------------------------------------------------------------------------------------------------------------------
        DisplayDistractors.AddSpecificInitializationMethod(() =>
        {
            ObjectManager.ActivateDistractors();
            AudioFBController.Play("ContinueBeep");
        });
        DisplayDistractors.AddTimer(() => CurrentTrial.DisplayDistractorsDuration, Play);

        //Play state ----------------------------------------------------------------------------------------------------------------------------------------------
        Play.AddSpecificInitializationMethod(() =>
        {
            GiveRewardIfSliderFull = false;
            if (Handler.AllSelections.Count > 0)
                Handler.ClearSelections();

            AudioFBController.Play("EC_BalloonChosen");
            ObjectManager.ActivateObjectMovement();
        });
        Play.AddUpdateMethod(() =>
        {
            ChosenGO = Handler.LastSuccessfulSelection?.SelectedGameObject;
            if (ChosenGO != null)
            {
                SA_Object obj = ChosenGO.GetComponent<SA_Object>();
                if(obj != null)
                {
                    HaloFBController.SetHaloSize(.01f * obj.Size);

                    if (obj.IsTarget)
                    {
                        if(obj.WithinDuration && !obj.CurrentCycle.selectedDuringCurrentInterval)
                        {
                            Debug.LogWarning("CORRECT DURATION: " + (Time.time - obj.AnimStartTime));
                            GiveRewardIfSliderFull = true;
                            HaloFBController.ShowPositive(ChosenGO, HaloDepth, HaloDuration);
                            SliderFBController.UpdateSliderValue(CurrentTrial.SliderGain[0] * (1f / SliderGainSteps)); //eventually change slidergain[0]!!
                            SuccessfulTargetSelections_Block++;
                            CurrentTaskLevel.SuccessfulTargetSelections_Task++;
                        }
                        else
                        {
                            HaloFBController.ShowNegative(ChosenGO, HaloDepth, HaloDuration);
                            SliderFBController.UpdateSliderValue(CurrentTrial.SliderLoss[0] * (1f / SliderGainSteps));

                            if(obj.CurrentCycle.selectedDuringCurrentInterval)
                            {
                                Debug.LogWarning("SELECTED TARGET AGAIN AFTER ALREADY SELECTING ONCE!");
                                AdditionalTargetSelections_Block++;
                                CurrentTaskLevel.AdditionalTargetSelections_Task++;
                            }
                            else
                            {
                                if (obj.CurrentCycle.firstIntervalStarted)
                                {
                                    Debug.LogWarning("FAILED DURATION: " + (Time.time - obj.AnimStartTime));
                                    UnsuccessfulTargetSelections_Block++;
                                    CurrentTaskLevel.UnsuccessfulTargetSelections_Task++;
                                }
                                else
                                {
                                    Debug.LogWarning("SELECTED BEFORE FIRST INTERVAL STARTS!");
                                    TargetSelectionsBeforeFirstAnim_Block++;
                                    CurrentTaskLevel.TargetSelectionsBeforeFirstAnim_Task++;
                                }
                            }
                        }
                    }
                    else //Selected a Distractor
                    {
                        HaloFBController.ShowNegative(ChosenGO, HaloDepth, HaloDuration);
                        SliderFBController.UpdateSliderValue(CurrentTrial.SliderLoss[0] * (1f / SliderGainSteps));
                        DistractorSelections_Block++;
                        CurrentTaskLevel.DistractorSelections_Task++;
                    }

                    CurrentTaskLevel.CalculateBlockSummaryString(); //update data on Exp Display

                    if(obj.CurrentCycle.firstIntervalStarted)
                        obj.CurrentCycle.selectedDuringCurrentInterval = true;

                    Input.ResetInputAxes(); //Reset input?

                    Handler.LastSuccessfulSelection = null;
                }
            }

            HandleSlider();
        });
        Play.SpecifyTermination(() => ObjectManager.DistractorList.Count < 1 && ObjectManager.TargetList.Count < 1, ITI);

        //ITI state ----------------------------------------------------------------------------------------------------------------------------------------------
        ITI.AddTimer(() => itiDuration.value, FinishTrial);


        DefineTrialData();
        DefineFrameData();
    }

    private void OnDestroy()
    {
        if(ObjectManager != null)
        {
            ObjectManager.OnTargetIntervalMissed += TargetIntervalMissed; //UNsubscribe to MissedInterval Event
            ObjectManager.OnDistractorAvoided += DistractorAvoided; //UNsubscribe to DistractorAvoided Event
        }
    }
    private void TargetIntervalMissed()
    {
        TargetAnimsWithoutSelection_Block++;
        CurrentTaskLevel.TargetAnimsWithoutSelection_Task++;
        CurrentTaskLevel.CalculateBlockSummaryString(); //update data on exp display
    }
    private void DistractorAvoided()
    {
        DistractorRejections_Block++;
        CurrentTaskLevel.DistractorRejections_Task++;
        CurrentTaskLevel.CalculateBlockSummaryString(); //update data on exp display
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
                //increment slider completions data??
            }
        }
    }

    public override void FinishTrialCleanup()
    {
        ObjectManager.OnTargetIntervalMissed -= TargetIntervalMissed; //Unsubscribe from MissedInterval Event

        ObjectManager.DestroyExistingObjects();

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
        SliderLossSteps = 0;
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
    }

    private void CalculateSliderSteps()
    {
        foreach (int sliderGain in CurrentTrial.SliderGain)
        {
            SliderGainSteps += sliderGain;
        }
        SliderGainSteps += CurrentTrial.SliderInitialValue;
        foreach (int sliderLoss in CurrentTrial.SliderLoss)
        {
            SliderLossSteps += sliderLoss;
        }
        SliderLossSteps += CurrentTrial.SliderInitialValue;
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
        TrialData.AddDatum("Objects", () => CurrentTrial.TrialObjectIndices);
        TrialData.AddDatum("DisplayTargetDuration", () => CurrentTrial.DisplayTargetDuration);
        TrialData.AddDatum("DisplayDistractorsDuration", () => CurrentTrial.DisplayDistractorsDuration);

        //TrialData.AddDatum("RotateTargets", () => CurrentTrial.RotateTargets);
        //TrialData.AddDatum("RotateDistractors", () => CurrentTrial.RotateDistractors);
        //TrialData.AddDatum("ResponseWindow", () => CurrentTrial.ResponseWindow);
        //TrialData.AddDatum("NumTargets", () => CurrentTrial.TargetSizes.Length);
        //TrialData.AddDatum("NumDistractors", () => CurrentTrial.DistractorSizes.Length);
        //TrialData.AddDatum("TargetCloseDuration", () => CurrentTrial.TargetCloseDuration);
        //TrialData.AddDatum("DistractorCloseDuration", () => CurrentTrial.DistractorCloseDuration);
        //TrialData.AddDatum("TargetMinAnimGap", () => CurrentTrial.TargetMinAnimGap);
        //TrialData.AddDatum("DistractorMinAnimGap", () => CurrentTrial.DistractorMinAnimGap);
        //TrialData.AddDatum("AngleProbabilities", () => CurrentTrial.AngleProbs);
    }

    private void DefineFrameData()
    {
        FrameData.AddDatum("StartButton", () => StartButton != null && StartButton.activeInHierarchy ? "Active" : "NotActive");
        //what else to track?
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
