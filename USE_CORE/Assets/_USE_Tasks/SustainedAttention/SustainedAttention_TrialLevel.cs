using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using USE_States;
using USE_Settings;
using USE_ExperimentTemplate_Trial;
using SustainedAttention_Namespace;
using UnityEngine.UI;
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

    [HideInInspector] public ConfigNumber minObjectTouchDuration, maxObjectTouchDuration;


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
            ObjectManager.CreateObjects(true, CurrentTrial.RotateTowardsDest, CurrentTrial.TargetSizes, CurrentTrial.TargetSpeeds, CurrentTrial.TargetNextDestDist, CurrentTrial.TargetAnimationIntervals, CurrentTrial.TargetRewards, Color.yellow);
            //Create Distractors:
            ObjectManager.CreateObjects(false, CurrentTrial.RotateTowardsDest, CurrentTrial.DistractorSizes, CurrentTrial.DistractorSpeeds, CurrentTrial.DistractorNextDestDist, CurrentTrial.DistractorAnimationIntervals, CurrentTrial.DistractorRewards, Color.magenta);

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
        InitTrial.SpecifyTermination(() => Handler.LastSuccessfulSelectionMatchesStartButton(), DisplayTarget, () => BordersGO.SetActive(true));

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
            ChosenGO = Handler.LastSuccessfulSelection.SelectedGameObject;
            if (ChosenGO != null)
                HandleFeedback();
        });
        Play.AddTimer(() => CurrentTrial.PlayDuration, ITI);

        //ITI state ----------------------------------------------------------------------------------------------------------------------------------------------
        ITI.AddDefaultInitializationMethod(() =>
        {
            ObjectManager.DeactivateTargets();
            ObjectManager.DeactivateDistractors();
        });
        ITI.AddTimer(() => CurrentTrial.ItiDuration, FinishTrial);
    }


    void HandleFeedback()
    {
        //if (ChosenGO == Target) //Chose Target
        //{
        //    AudioFBController.Play("Positive");
        //    HaloFBController.ShowPositive(ChosenGO);
        //    //increment slider:
        //    //check if slider full, if it is: GiveReward():

        //}
        ////else if (Distractors.Contains(ChosenGO)) //Chose A distractor
        //else if (ChosenGO == Distractor) //Chose A distractor
        //{
        //    AudioFBController.Play("Negative");
        //    HaloFBController.ShowNegative(ChosenGO);
        //    //decrement slider:

        //}
        //else
        //    Debug.LogWarning("SELECTED NEITHER A TARGET NOR DISTRACTOR!");
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
    }



}
