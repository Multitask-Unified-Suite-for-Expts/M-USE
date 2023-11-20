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

    //Set in Inspector:
    public GameObject SustainedAttention_CanvasGO;
    public GameObject BordersGO;

    private ObjectManager ObjectManager;

    private GameObject StartButton;

    private GameObject ChosenGO = null;

    [HideInInspector] public ConfigNumber itiDuration, sliderFlashingDuration, sliderUpdateDuration, sliderSize, minObjectTouchDuration, maxObjectTouchDuration;

    private int SliderGainSteps, SliderLossSteps;



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
            ObjectManager.CreateObjects(true, CurrentTrial.RotateTowardsDest, CurrentTrial.ResponseWindow, CurrentTrial.TargetSizes, CurrentTrial.TargetSpeeds, CurrentTrial.TargetNextDestDist, CurrentTrial.TargetAnimationIntervals, CurrentTrial.TargetRewards, Color.yellow);
            //Create Distractors:
            ObjectManager.CreateObjects(false, CurrentTrial.RotateTowardsDest, CurrentTrial.ResponseWindow, CurrentTrial.DistractorSizes, CurrentTrial.DistractorSpeeds, CurrentTrial.DistractorNextDestDist, CurrentTrial.DistractorAnimationIntervals, CurrentTrial.DistractorRewards, Color.magenta);

        });
        SetupTrial.SpecifyTermination(() => true, InitTrial);

        var Handler = Session.SelectionTracker.SetupSelectionHandler("trial", "MouseButton0Click", Session.MouseTracker, InitTrial, Play); //Setup Handler
        TouchFBController.EnableTouchFeedback(Handler, CurrentTask.TouchFeedbackDuration, CurrentTask.StartButtonScale * 30, SustainedAttention_CanvasGO, true); //Enable Touch Feedback

        //InitTrial state ----------------------------------------------------------------------------------------------------------------------------------------------
        InitTrial.AddSpecificInitializationMethod(() =>
        {
            if (Handler.AllSelections.Count > 0)
                Handler.ClearSelections();
            Handler.MinDuration = minObjectTouchDuration.value;
            Handler.MaxDuration = maxObjectTouchDuration.value;
        });
        InitTrial.SpecifyTermination(() => Handler.LastSuccessfulSelectionMatchesStartButton(), DisplayTarget, () =>
        {
            BordersGO.SetActive(true);

            CalculateSliderSteps();
            Vector3 sliderPosAdj = new Vector3(0f, -43f, 0f);
            SliderFBController.ConfigureSlider(sliderSize.value, CurrentTrial.SliderInitialValue * (1f / SliderGainSteps), sliderPosAdj);
            SliderFBController.SetSliderRectSize(new Vector2(400f, 23f));
            SliderFBController.SliderGO.SetActive(true);
            SliderFBController.SetUpdateDuration(sliderUpdateDuration.value);
            SliderFBController.SetFlashingDuration(sliderFlashingDuration.value);

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
                if(obj != null && obj.IsTarget)
                {
                    float depth = 10f; //make configurable later
                    float haloDuration = .15f; //make configurable later

                    if(obj.WithinDuration)
                    {
                        Debug.LogWarning("WITHIN DURATION!");
                        AudioFBController.Play("Positive");
                        HaloFBController.ShowPositive(ChosenGO, depth, haloDuration);
                        //increment slider:
                        //check if slider full, if it is: GiveReward():
                    }
                    else
                    {
                        AudioFBController.Play("Negative");
                        HaloFBController.ShowNegative(ChosenGO, depth, haloDuration);
                    }

                    Handler.LastSuccessfulSelection = null;
                }
            }
        });
        Play.AddTimer(() => CurrentTrial.PlayDuration, ITI);

        //ITI state ----------------------------------------------------------------------------------------------------------------------------------------------
        ITI.AddTimer(() => itiDuration.value, FinishTrial);

    }

    public override void FinishTrialCleanup()
    {
        ObjectManager.DestroyExistingObjects();

        SliderFBController.SliderGO.SetActive(false);
        SliderFBController.SliderHaloGO.SetActive(false);
    }

    public override void ResetTrialVariables()
    {
        SliderGainSteps = 0;
        SliderLossSteps = 0;
        SliderFBController.ResetSliderBarFull();
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
