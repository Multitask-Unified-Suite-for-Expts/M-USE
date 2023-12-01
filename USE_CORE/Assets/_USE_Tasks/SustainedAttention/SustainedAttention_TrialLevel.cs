using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using USE_States;
using USE_ExperimentTemplate_Trial;
using SustainedAttention_Namespace;
using ConfigDynamicUI;


public class SustainedAttention_TrialLevel : ControlLevel_Trial_Template
{
    public SustainedAttention_TrialDef CurrentTrial => GetCurrentTrialDef<SustainedAttention_TrialDef>();
    public SustainedAttention_TaskLevel CurrentTaskLevel => GetTaskLevel<SustainedAttention_TaskLevel>();
    public SustainedAttention_TaskDef CurrentTask => GetTaskDef<SustainedAttention_TaskDef>();

    //DATA:
    [HideInInspector] public int TrialCompletions_Block;

    //Set in Inspector:
    public GameObject SustainedAttention_CanvasGO;
    public GameObject BordersGO;

    private ObjectManager ObjectManager;

    private GameObject StartButton;

    private GameObject ChosenGO = null;

    private int SliderGainSteps, SliderLossSteps;

    [HideInInspector] public ConfigNumber itiDuration, minObjectTouchDuration, maxObjectTouchDuration, sliderFlashingDuration, sliderUpdateDuration, sliderSize;

    private bool GiveRewardIfSliderFull = false;

    private readonly float HaloDepth = 10f;
    private float HaloDuration = .15f; //make configurable later


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

            ObjectManager = gameObject.AddComponent<ObjectManager>();
            ObjectManager.SetObjectParent(SustainedAttention_CanvasGO.transform);

            //Create Targets:
            ObjectManager.CreateObjects(true, CurrentTrial.AngleProbs, CurrentTrial.RotateTargets, CurrentTrial.TargetMinAnimGap, CurrentTrial.ResponseWindow, CurrentTrial.TargetCloseDuration, CurrentTrial.TargetSizes, CurrentTrial.TargetSpeeds, CurrentTrial.TargetNextDestDist, CurrentTrial.TargetRatesAndDurations, Color.yellow);
            //Create Distractors:
            ObjectManager.CreateObjects(false, CurrentTrial.AngleProbs, CurrentTrial.RotateDistractors, CurrentTrial.DistractorMinAnimGap, CurrentTrial.ResponseWindow, CurrentTrial.DistractorCloseDuration, CurrentTrial.DistractorSizes, CurrentTrial.DistractorSpeeds, CurrentTrial.DistractorNextDestDist, CurrentTrial.DistractorRatesAndDurations, Color.magenta);

        });
        SetupTrial.SpecifyTermination(() => true, InitTrial);

        var Handler = Session.SelectionTracker.SetupSelectionHandler("trial", "MouseButton0Click", Session.MouseTracker, InitTrial, Play); //Setup Handler
        TouchFBController.EnableTouchFeedback(Handler, CurrentTask.TouchFeedbackDuration, CurrentTask.StartButtonScale * 30, SustainedAttention_CanvasGO, true); //Enable Touch Feedback

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
                    if (obj.SelectedDuringCurrentInterval)
                    {
                        Handler.LastSuccessfulSelection = null;
                        return;
                    }
                    else
                    {
                        obj.PausingWhenBeingSelected = false;
                        obj.SelectedDuringCurrentInterval = true;
                    }

                    HaloFBController.SetHaloSize(.01f * obj.Size);

                    if(obj.IsTarget && obj.WithinDuration)
                    {
                        Debug.LogWarning("CORRECT DURATION: " + (Time.time - obj.AnimStartTime));
                        GiveRewardIfSliderFull = true;
                        HaloFBController.ShowPositive(ChosenGO, HaloDepth, HaloDuration);
                        SliderFBController.UpdateSliderValue(CurrentTrial.SliderGain[0] * (1f / SliderGainSteps)); //eventually change slidergain[0]!!
                    }
                    else
                    {
                        if(obj.IsTarget)
                            Debug.LogWarning("FAILING DURATION: " + (Time.time - obj.AnimStartTime));
                        HaloFBController.ShowNegative(ChosenGO, HaloDepth, HaloDuration);
                        SliderFBController.UpdateSliderValue(CurrentTrial.SliderLoss[0] * (1f / SliderGainSteps));
                    }
                    Handler.LastSuccessfulSelection = null;
                }
            }

            HandleSlider();
        });
        Play.AddTimer(() => CurrentTrial.PlayDuration, ITI);
        Play.SpecifyTermination(() => ObjectManager.DistractorList.Count < 1 && ObjectManager.TargetList.Count < 1, ITI);

        //ITI state ----------------------------------------------------------------------------------------------------------------------------------------------
        ITI.AddTimer(() => itiDuration.value, FinishTrial);

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
    } //muse paper online at bioarchive. 

    public override void FinishTrialCleanup()
    {
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
                             "\nNum Targets: " + CurrentTrial.TargetSizes.Length +
                             "\nNum Distractors: " + CurrentTrial.DistractorSizes.Length;

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
