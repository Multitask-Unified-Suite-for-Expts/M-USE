using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using USE_States;
using USE_Settings;
using USE_ExperimentTemplate_Trial;
using USE_StimulusManagement;
using AntiSaccade_Namespace;
using ConfigDynamicUI;
using ContinuousRecognition_Namespace;

public class AntiSaccade_TrialLevel : ControlLevel_Trial_Template
{
    public AntiSaccade_TrialDef CurrentTrialDef => GetCurrentTrialDef<AntiSaccade_TrialDef>();
    public AntiSaccade_TaskLevel CurrentTaskLevel => GetTaskLevel<AntiSaccade_TaskLevel>();
    public AntiSaccade_TaskDef CurrentTask => GetTaskDef<AntiSaccade_TaskDef>();

    public GameObject AntiSaccade_CanvasGO;

    private GameObject StartButton;

    //Set In Inspector:
    public GameObject PreCue_GO;
    public GameObject SpatialCue_GO;
    public GameObject Mask_Target_GO;
    public GameObject Mask_Cue_GO;

    private StimGroup targetStim;
    private StimGroup distractorStims;

    [HideInInspector] public ConfigNumber minObjectTouchDuration, maxObjectTouchDuration, preCueDuration, alertCueDuration, spacialCueDuration, displayTargetDuration, maskDuration, postMaskDelayDuration, chooseStimDuration, feedbackDuration, itiDuration;

    //MAIN TARGET:
    private GameObject TargetStim_GO;

    private GameObject ChosenGO = null;
    private AntiSaccade_StimDef ChosenStim = null;


    public override void DefineControlLevel()
    {
        State InitTrial = new State("InitTrial");
        State PreCue = new State("PreCue");
        State AlertCue = new State("AlertCue");
        State SpacialCue = new State("SpacialCue");
        State DisplayTarget = new State("DisplayTarget");
        State Mask = new State("Mask");
        State PostMaskDelay = new State("PostMaskDelay");
        State ChooseStim = new State("ChooseStim");
        State Feedback = new State("Feedback");
        State ITI = new State("ITI");
        AddActiveStates(new List<State> { InitTrial, PreCue, AlertCue, SpacialCue, DisplayTarget, Mask, PostMaskDelay, ChooseStim, Feedback, ITI });

        Add_ControlLevel_InitializationMethod(() =>
        {
            PreCue_GO.transform.parent = AntiSaccade_CanvasGO.transform;
            SpatialCue_GO.transform.parent = AntiSaccade_CanvasGO.transform;

            if (StartButton == null)
            {
                if (SessionValues.SessionDef.IsHuman)
                {
                    StartButton = SessionValues.HumanStartPanel.StartButtonGO;
                    SessionValues.HumanStartPanel.SetVisibilityOnOffStates(InitTrial, InitTrial);
                }
                else
                {
                    StartButton = SessionValues.USE_StartButton.CreateStartButton(AntiSaccade_CanvasGO.GetComponent<Canvas>(), CurrentTask.StartButtonPosition, CurrentTask.StartButtonScale);
                    SessionValues.USE_StartButton.SetVisibilityOnOffStates(InitTrial, InitTrial);
                }
            }

            HaloFBController.SetHaloSize(4f);
            HaloFBController.SetHaloIntensity(2);
        });

        //SetupTrial state ----------------------------------------------------------------------------------------------------------------------------------------------
        SetupTrial.AddSpecificInitializationMethod(() =>
        {
            TokenFBController.enabled = false;
            LoadConfigUIVariables();
        });
        SetupTrial.SpecifyTermination(() => true, InitTrial);

        //Setup Handler:
        var Handler = SessionValues.SelectionTracker.SetupSelectionHandler("trial", "MouseButton0Click", SessionValues.MouseTracker, InitTrial, ChooseStim);
        //Enable Touch Feedback:
        TouchFBController.EnableTouchFeedback(Handler, CurrentTask.TouchFeedbackDuration, CurrentTask.StartButtonScale * 10, AntiSaccade_CanvasGO);

        //InitTrial state ----------------------------------------------------------------------------------------------------------------------------------------------
        InitTrial.AddSpecificInitializationMethod(() =>
        {
            Camera.main.gameObject.GetComponent<Skybox>().enabled = false; //Disable cam's skybox so the RenderSettings.Skybox can show the Context background

            if (Handler.AllSelections.Count > 0)
                Handler.ClearSelections();
            Handler.MinDuration = minObjectTouchDuration.value;
            Handler.MaxDuration = maxObjectTouchDuration.value;

            TargetStim_GO = targetStim.stimDefs[0].StimGameObject;
        });
        InitTrial.SpecifyTermination(() => Handler.LastSuccessfulSelectionMatchesStartButton(), PreCue);

        //PreCue state ----------------------------------------------------------------------------------------------------------------------------------------------
        PreCue.AddSpecificInitializationMethod(() =>
        {
            TokenFBController.enabled = true;
            PreCue_GO.SetActive(true);
        });
        PreCue.AddTimer(() => preCueDuration.value, AlertCue);

        //AlertCue state ----------------------------------------------------------------------------------------------------------------------------------------------
        AlertCue.AddSpecificInitializationMethod(() => AudioFBController.Play("ContinueBeep"));
        AlertCue.AddTimer(() => alertCueDuration.value, SpacialCue);

        //SpacialCue state ----------------------------------------------------------------------------------------------------------------------------------------------
        SpacialCue.AddSpecificInitializationMethod(() =>
        {
            SpatialCue_GO.transform.localPosition = CurrentTrialDef.SpacialCuePosition;
            SpatialCue_GO.SetActive(true);
        });
        SpacialCue.AddTimer(() => spacialCueDuration.value, DisplayTarget);

        //DisplayTarget state ----------------------------------------------------------------------------------------------------------------------------------------------
        DisplayTarget.AddTimer(() => displayTargetDuration.value, Mask);

        //Mask state ----------------------------------------------------------------------------------------------------------------------------------------------
        Mask.AddSpecificInitializationMethod(() =>
        {
            PreCue_GO.SetActive(false);
            SpatialCue_GO.SetActive(false);

            Mask_Target_GO.transform.localPosition = CurrentTrialDef.TargetStimPosition;
            Mask_Cue_GO.transform.localPosition = SpatialCue_GO.transform.localPosition;
            Mask_Target_GO.SetActive(true);
            Mask_Cue_GO.SetActive(true);
        });
        Mask.AddTimer(() => maskDuration.value, PostMaskDelay);

        //PostMaskDelay state ----------------------------------------------------------------------------------------------------------------------------------------------
        PostMaskDelay.AddSpecificInitializationMethod(() =>
        {
            Mask_Target_GO.SetActive(false);
            Mask_Cue_GO.SetActive(false);
        });
        PostMaskDelay.AddTimer(() => postMaskDelayDuration.value, ChooseStim);

        //ChooseStim state ----------------------------------------------------------------------------------------------------------------------------------------------
        bool stimChosen = false;
        ChooseStim.AddSpecificInitializationMethod(() =>
        {
            TargetStim_GO.transform.localPosition = CurrentTrialDef.TargetStimFbPosition;
            TargetStim_GO.SetActive(true);

            PreCue_GO.SetActive(false);
            SpatialCue_GO.SetActive(false);

            ChosenGO = null;
            ChosenStim = null;
            stimChosen = false;

            if (Handler.AllSelections.Count > 0)
                Handler.ClearSelections();
        });
        ChooseStim.AddUpdateMethod(() =>
        {
            ChosenGO = Handler.LastSelection.SelectedGameObject;
            ChosenStim = ChosenGO?.GetComponent<StimDefPointer>()?.GetStimDef<AntiSaccade_StimDef>();
            if(ChosenStim != null)
                stimChosen = true;
        });
        ChooseStim.SpecifyTermination(() => stimChosen, Feedback);
        ChooseStim.AddTimer(() => chooseStimDuration.value, Feedback);

        //Feedback state ----------------------------------------------------------------------------------------------------------------------------------------------
        Feedback.AddSpecificInitializationMethod(() =>
        {
            if(ChosenStim == null)
                return;
            
            int? depth = SessionValues.Using2DStim ? 10 : (int?)null;

            if (ChosenStim.IsTarget)
            {
                HaloFBController.ShowPositive(ChosenGO, depth);
                TokenFBController.AddTokens(ChosenGO, CurrentTrialDef.RewardMag);
                SessionValues.EventCodeManager.SendCodeImmediate("CorrectResponse");
            }
            else
            {
                HaloFBController.ShowNegative(ChosenGO, depth);
                TokenFBController.RemoveTokens(ChosenGO, CurrentTrialDef.RewardMag);
                SessionValues.EventCodeManager.SendCodeImmediate("IncorrectResponse");
            }
            
        });
        Feedback.AddTimer(() => feedbackDuration.value, ITI);
        Feedback.SpecifyTermination(() => !stimChosen, ITI, () => AudioFBController.Play("Negative"));
        Feedback.AddDefaultTerminationMethod(() =>
        {
            HaloFBController.Destroy();
            TargetStim_GO.SetActive(false);
        });


        //Feedback state ----------------------------------------------------------------------------------------------------------------------------------------------
        ITI.AddTimer(() => itiDuration.value, FinishTrial);

        //DefineTrialData();
        //DefineFrameData();
    }

    //--------------Helper Methods--------------------------------------------------------------------------------------------------------------------
    private void LoadConfigUIVariables()
    {
        minObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("minObjectTouchDuration");
        maxObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("maxObjectTouchDuration");
        preCueDuration = ConfigUiVariables.get<ConfigNumber>("preCueDuration");
        alertCueDuration = ConfigUiVariables.get<ConfigNumber>("alertCueDuration");
        spacialCueDuration = ConfigUiVariables.get<ConfigNumber>("spacialCueDuration");
        displayTargetDuration = ConfigUiVariables.get<ConfigNumber>("displayTargetDuration");
        maskDuration = ConfigUiVariables.get<ConfigNumber>("maskDuration");
        postMaskDelayDuration = ConfigUiVariables.get<ConfigNumber>("postMaskDelayDuration");
        chooseStimDuration = ConfigUiVariables.get<ConfigNumber>("chooseStimDuration");
        feedbackDuration = ConfigUiVariables.get<ConfigNumber>("feedbackDuration");
        itiDuration = ConfigUiVariables.get<ConfigNumber>("itiDuration");
    }

    protected override void DefineTrialStims()
    {
        StimGroup group = SessionValues.UsingDefaultConfigs ? PrefabStims : ExternalStims;

        targetStim = new StimGroup("TargetStim", group, new List<int> { CurrentTrialDef.TargetStimIndex });
        foreach (AntiSaccade_StimDef stim in targetStim.stimDefs)
            stim.IsTarget = true;
        targetStim.SetLocations(new List<Vector3> { CurrentTrialDef.TargetStimPosition });
        targetStim.SetVisibilityOnOffStates(GetStateFromName("DisplayTarget"), GetStateFromName("DisplayTarget"));
        TrialStims.Add(targetStim);

        distractorStims = new StimGroup("DistractorStims", group, CurrentTrialDef.DistractorStimIndices);
        distractorStims.SetLocations(CurrentTrialDef.DistractorStimFbPositions);
        distractorStims.SetVisibilityOnOffStates(GetStateFromName("ChooseStim"), GetStateFromName("Feedback"));
        TrialStims.Add(distractorStims);
    }

}
