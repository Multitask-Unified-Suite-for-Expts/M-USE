using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using USE_States;
using USE_ExperimentTemplate_Trial;
using USE_StimulusManagement;
using AntiSaccade_Namespace;
using ConfigDynamicUI;
using UnityEngine.UI;
using UnityEditor.UIElements;
using System;

public class AntiSaccade_TrialLevel : ControlLevel_Trial_Template
{
    public AntiSaccade_TrialDef CurrentTrialDef => GetCurrentTrialDef<AntiSaccade_TrialDef>();
    public AntiSaccade_TaskLevel CurrentTaskLevel => GetTaskLevel<AntiSaccade_TaskLevel>();
    public AntiSaccade_TaskDef CurrentTask => GetTaskDef<AntiSaccade_TaskDef>();

    public GameObject AntiSaccade_CanvasGO;

    private GameObject StartButton;

    private GameObject PreCue_GO;
    private GameObject SpatialCue_GO;
    private GameObject Mask_GO;

    private StimGroup targetStim;
    private StimGroup distractorStims;

    [HideInInspector] public ConfigNumber minObjectTouchDuration, maxObjectTouchDuration, preCueDuration, alertCueDuration, spatialCueDuration, displayTargetDuration, maskDuration, postMaskDelayDuration, chooseStimDuration, feedbackDuration, itiDuration;

    //MAIN TARGET:
    private GameObject TargetStim_GO;

    private GameObject ChosenGO = null;
    private AntiSaccade_StimDef ChosenStim = null;

    private string SaccadeType_Trial;

    //for data:
    private string DistractorStimIndices_String;
    private string DistractorStimsChoosePos_String;


    private bool SpinCorrectSelection;

    private List<String> IconsList;



    public override void DefineControlLevel()
    {
        State InitTrial = new State("InitTrial");
        State PreCue = new State("PreCue");
        State AlertCue = new State("AlertCue");
        State SpatialCue = new State("SpatialCue");
        State DisplayTarget = new State("DisplayTarget");
        State Mask = new State("Mask");
        State PostMaskDelay = new State("PostMaskDelay");
        State ChooseStim = new State("ChooseStim");
        State Feedback = new State("Feedback");
        State ITI = new State("ITI");
        AddActiveStates(new List<State> { InitTrial, PreCue, AlertCue, SpatialCue, DisplayTarget, Mask, PostMaskDelay, ChooseStim, Feedback, ITI });

        Add_ControlLevel_InitializationMethod(() =>
        {
            if (PreCue_GO == null || Mask_GO == null || SpatialCue_GO == null)
                CreateGameObjects();

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

            HaloFBController.SetHaloSize(3.5f);
            HaloFBController.SetHaloIntensity(.75f);

            TokenFBController.AdjustTokenBarSizing(145);

            IconsList = new List<string>() { "Star", "Heart", "Circle", "QuestionMark" };
        });

        //SetupTrial state ----------------------------------------------------------------------------------------------------------------------------------------------
        SetupTrial.AddSpecificInitializationMethod(() =>
        {
            TokenFBController.enabled = false;
            LoadConfigUIVariables();

            //Set string of Whether its AntiSaccade or ProSaccade by checking if X values are the same. 
            SaccadeType_Trial = CurrentTrialDef.TargetStim_DisplayPos.x == CurrentTrialDef.TargetStim_DisplayPos.x ? "Pro" : "Anti";

            SetDataStrings();

            //Set SpatialCue Icon:
            if (!string.IsNullOrEmpty(CurrentTrialDef.SpatialCue_Icon) && IconsList.Contains(CurrentTrialDef.SpatialCue_Icon))
                SpatialCue_GO.GetComponent<Image>().sprite = Resources.Load<Sprite>(CurrentTrialDef.SpatialCue_Icon);

            //Set Mask Icon:
            if(!string.IsNullOrEmpty(CurrentTrialDef.Mask_Icon) && IconsList.Contains(CurrentTrialDef.Mask_Icon))
                Mask_GO.GetComponent<Image>().sprite = Resources.Load<Sprite>(CurrentTrialDef.Mask_Icon);
        });
        SetupTrial.SpecifyTermination(() => true, InitTrial);

        var Handler = SessionValues.SelectionTracker.SetupSelectionHandler("trial", "MouseButton0Click", SessionValues.MouseTracker, InitTrial, ChooseStim); //Setup Handler
        TouchFBController.EnableTouchFeedback(Handler, CurrentTask.TouchFeedbackDuration, CurrentTask.StartButtonScale * 25, AntiSaccade_CanvasGO); //Enable Touch Feedback:

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
        InitTrial.SpecifyTermination(() => Handler.LastSuccessfulSelectionMatchesStartButton(), PreCue, () => TokenFBController.enabled = true);

        //PreCue state ----------------------------------------------------------------------------------------------------------------------------------------------
        PreCue.AddSpecificInitializationMethod(() =>
        {
            if(CurrentTrialDef.PreCue_Size > 0)
                PreCue_GO.GetComponent<RectTransform>().sizeDelta = new Vector2(CurrentTrialDef.PreCue_Size, CurrentTrialDef.PreCue_Size);
            PreCue_GO.SetActive(true);
        });

        PreCue.AddTimer(() => preCueDuration.value, AlertCue);

        //AlertCue state ----------------------------------------------------------------------------------------------------------------------------------------------
        AlertCue.AddSpecificInitializationMethod(() => AudioFBController.Play("ContinueBeep"));
        AlertCue.AddTimer(() => alertCueDuration.value, SpatialCue);

        //SpatialCue state ----------------------------------------------------------------------------------------------------------------------------------------------
        SpatialCue.AddSpecificInitializationMethod(() =>
        {
            if (CurrentTrialDef.RandomSpatialCueColor)
                SpatialCue_GO.GetComponent<Image>().color = GetRandomColor();

            SpatialCue_GO.transform.localPosition = CurrentTrialDef.SpatialCue_Pos;
            SpatialCue_GO.SetActive(true);
        });
        SpatialCue.AddTimer(() => spatialCueDuration.value, DisplayTarget);

        //DisplayTarget state ----------------------------------------------------------------------------------------------------------------------------------------------
        DisplayTarget.AddSpecificInitializationMethod(() => SpatialCue_GO.SetActive(false));
        DisplayTarget.AddTimer(() => displayTargetDuration.value, Mask);

        //Mask state ----------------------------------------------------------------------------------------------------------------------------------------------
        Mask.AddSpecificInitializationMethod(() =>
        {
            PreCue_GO.SetActive(false);

            if (CurrentTrialDef.RandomMaskColor)
                Mask_GO.GetComponent<Image>().color = GetRandomColor();

            Mask_GO.transform.localPosition = new Vector3(CurrentTrialDef.TargetStim_DisplayPos.x, CurrentTrialDef.TargetStim_DisplayPos.y + 25f, CurrentTrialDef.TargetStim_DisplayPos.z); //have to adjust the Y cuz pics have padding
            Mask_GO.SetActive(true);
        });
        Mask.AddTimer(() => maskDuration.value, PostMaskDelay);

        //PostMaskDelay state ----------------------------------------------------------------------------------------------------------------------------------------------
        PostMaskDelay.AddSpecificInitializationMethod(() => Mask_GO.SetActive(false));
        PostMaskDelay.AddTimer(() => postMaskDelayDuration.value, ChooseStim);

        //ChooseStim state ----------------------------------------------------------------------------------------------------------------------------------------------
        bool stimChosen = false;
        ChooseStim.AddSpecificInitializationMethod(() =>
        {
            TargetStim_GO.transform.localPosition = CurrentTrialDef.TargetStim_ChoosePos;
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
        float rotationSpeed = 260f;
        float totalRotation = 0f;

        Feedback.AddSpecificInitializationMethod(() =>
        {
            SpinCorrectSelection = false;
            totalRotation = 0f;

            if(ChosenStim == null)
                return;

            int? depth = SessionValues.Using2DStim ? 10 : (int?)null;

            if (ChosenStim.IsTarget)
            {
                if(CurrentTrialDef.UseSpinAnimation)
                    SpinCorrectSelection = true;
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
        Feedback.AddUpdateMethod(() =>
        {
            if(SpinCorrectSelection)
            {
                float rotationAmount = rotationSpeed * Time.deltaTime;
                if (rotationAmount + totalRotation >= 360)
                    rotationAmount -= (rotationAmount + totalRotation - 360);
                ChosenStim.StimGameObject.transform.Rotate(Vector3.forward, rotationAmount);
                totalRotation += rotationAmount;
                if(totalRotation >= 360f)
                {
                    transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                    SpinCorrectSelection = false;
                }
            }
        });
        Feedback.AddTimer(() => feedbackDuration.value, ITI);
        Feedback.SpecifyTermination(() => !stimChosen, ITI, () => AudioFBController.Play("Negative"));
        Feedback.AddUniversalTerminationMethod(() =>
        {
            if(TokenFBController.IsTokenBarFull())
            {
                if(SessionValues.SyncBoxController != null)
                {
                    SessionValues.SyncBoxController.SendRewardPulses(CurrentTrialDef.NumPulses, CurrentTrialDef.PulseSize);
                    CurrentTaskLevel.NumRewardPulses_InBlock += CurrentTrialDef.NumPulses;
                    CurrentTaskLevel.NumRewardPulses_InTask += CurrentTrialDef.NumPulses;
                }
                TokenFBController.ResetTokenBarFull();
            }
            TokenFBController.enabled = false;
            //HaloFBController.Destroy(); //moving up
            TargetStim_GO.SetActive(false);
        });


        //Feedback state ----------------------------------------------------------------------------------------------------------------------------------------------
        ITI.AddTimer(() => itiDuration.value, FinishTrial);

        DefineTrialData();
        DefineFrameData();

}

    //--------------Helper Methods--------------------------------------------------------------------------------------------------------------------
    private void CreateGameObjects()
    {
        if(PreCue_GO == null)
        {
            PreCue_GO = new GameObject("PreCue");
            PreCue_GO.SetActive(false);
            PreCue_GO.transform.parent = AntiSaccade_CanvasGO.transform;
            PreCue_GO.transform.localPosition = Vector3.zero;
            PreCue_GO.transform.localScale = Vector3.one;
            RectTransform preCueRect = PreCue_GO.AddComponent<RectTransform>();
            preCueRect.sizeDelta = new Vector2(75, 75);
            Image preCueImage = PreCue_GO.AddComponent<Image>();
            preCueImage.sprite = Resources.Load<Sprite>("PlusSign");
            preCueImage.color = new Color32(24, 255, 0, 255);
        }

        if(SpatialCue_GO == null)
        {
            SpatialCue_GO = new GameObject("SpatialCue");
            SpatialCue_GO.SetActive(false);
            SpatialCue_GO.transform.parent = AntiSaccade_CanvasGO.transform;
            SpatialCue_GO.transform.localScale = Vector3.one;
            RectTransform spatialCueRect = SpatialCue_GO.AddComponent<RectTransform>();
            spatialCueRect.sizeDelta = new Vector2(300, 300);
            Image spatialCueImage = SpatialCue_GO.AddComponent<Image>();
            spatialCueImage.sprite = Resources.Load<Sprite>("Star"); //initially using Star as default
        }

        if(Mask_GO == null)
        {
            Mask_GO = new GameObject("Mask");
            Mask_GO.SetActive(false);
            Mask_GO.transform.parent = AntiSaccade_CanvasGO.transform;
            Mask_GO.transform.localScale = Vector3.one;
            RectTransform maskRect = Mask_GO.AddComponent<RectTransform>();
            maskRect.sizeDelta = new Vector2(300, 300);
            Image maskImage = Mask_GO.AddComponent<Image>();
            maskImage.sprite = Resources.Load<Sprite>("QuestionMark");
            maskImage.color = Color.black;
        }

    }

    private void LoadConfigUIVariables()
    {
        minObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("minObjectTouchDuration");
        maxObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("maxObjectTouchDuration");
        preCueDuration = ConfigUiVariables.get<ConfigNumber>("preCueDuration");
        alertCueDuration = ConfigUiVariables.get<ConfigNumber>("alertCueDuration");
        spatialCueDuration = ConfigUiVariables.get<ConfigNumber>("spatialCueDuration");
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
        targetStim.SetLocations(new List<Vector3> { CurrentTrialDef.TargetStim_DisplayPos });
        targetStim.SetVisibilityOnOffStates(GetStateFromName("DisplayTarget"), GetStateFromName("DisplayTarget"));
        TrialStims.Add(targetStim);

        distractorStims = new StimGroup("DistractorStims", group, CurrentTrialDef.DistractorStimIndices);
        distractorStims.SetLocations(CurrentTrialDef.DistractorStims_ChoosePos);
        distractorStims.SetVisibilityOnOffStates(GetStateFromName("ChooseStim"), GetStateFromName("Feedback"));
        TrialStims.Add(distractorStims);
    }

    private void DefineTrialData()
    {
        TrialData.AddDatum("SaccadeType", () => SaccadeType_Trial);
        TrialData.AddDatum("RandomSpatialCue", () => CurrentTrialDef.RandomSpatialCueColor);
        TrialData.AddDatum("TargetStimIndex", () => CurrentTrialDef.TargetStimIndex);
        TrialData.AddDatum("DistractorStimIndices", () => DistractorStimIndices_String);
        TrialData.AddDatum("SpatialCuePos", () => CurrentTrialDef.SpatialCue_Pos.ToString());
        TrialData.AddDatum("TargetStimDisplayPos", () => CurrentTrialDef.TargetStim_DisplayPos.ToString());
        TrialData.AddDatum("TargetStimChoosePos", () => CurrentTrialDef.TargetStim_ChoosePos.ToString());
        TrialData.AddDatum("DistractorStimsChoosePos", () => DistractorStimsChoosePos_String);
        TrialData.AddDatum("Context", () => CurrentTrialDef.ContextName);
    }

    private void DefineFrameData()
    {
        FrameData.AddDatum("StartButton", () => StartButton.activeInHierarchy);
        FrameData.AddDatum("TargetStimActive", () => targetStim.IsActive);
        FrameData.AddDatum("DistractorStimsActive", () => distractorStims.IsActive);
    }

    private void SetDataStrings()
    {
        if(CurrentTrialDef.DistractorStimIndices.Length > 0)
            DistractorStimIndices_String = TurnIntArrayIntoString(CurrentTrialDef.DistractorStimIndices);

        if(CurrentTrialDef.DistractorStims_ChoosePos.Length > 0)
            DistractorStimsChoosePos_String = TurnVectorArrayIntoString(CurrentTrialDef.DistractorStims_ChoosePos);
    }

}

