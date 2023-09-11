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

    [HideInInspector] public ConfigNumber minObjectTouchDuration, maxObjectTouchDuration, preCueDuration, alertCueDuration, spacialCueDuration, displayTargetDuration, maskDuration, postMaskDelayDuration, chooseStimDuration, feedbackDuration, itiDuration;

    //MAIN TARGET:
    private GameObject TargetStim_GO;

    private GameObject ChosenGO = null;
    private AntiSaccade_StimDef ChosenStim = null;

    private string SaccadeType_Trial;

    //for data:
    private string DistractorStimIndices_String;
    private string DistractorStimsChoosePos_String;

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

            HaloFBController.SetHaloSize(4.25f);
            HaloFBController.SetHaloIntensity(2);

            TokenFBController.AdjustTokenBarSizing(125);
        });

        //SetupTrial state ----------------------------------------------------------------------------------------------------------------------------------------------
        SetupTrial.AddSpecificInitializationMethod(() =>
        {
            TokenFBController.enabled = false;
            LoadConfigUIVariables();

            //Set string of Whether its AntiSaccade or ProSaccade by checking if X values are the same. 
            SaccadeType_Trial = CurrentTrialDef.TargetStim_DisplayPos.x == CurrentTrialDef.TargetStim_DisplayPos.x ? "Pro" : "Anti";

            SetDataStrings();
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
        PreCue.AddSpecificInitializationMethod(() => PreCue_GO.SetActive(true));
        PreCue.AddTimer(() => preCueDuration.value, AlertCue);

        //AlertCue state ----------------------------------------------------------------------------------------------------------------------------------------------
        AlertCue.AddSpecificInitializationMethod(() => AudioFBController.Play("ContinueBeep"));
        AlertCue.AddTimer(() => alertCueDuration.value, SpacialCue);

        //SpacialCue state ----------------------------------------------------------------------------------------------------------------------------------------------
        SpacialCue.AddSpecificInitializationMethod(() =>
        {
            if (CurrentTrialDef.RandomSpatialCueColor)
                SpatialCue_GO.GetComponent<Image>().color = GetRandomColor();

            SpatialCue_GO.transform.localPosition = CurrentTrialDef.SpacialCue_Pos;
            SpatialCue_GO.SetActive(true);
        });
        SpacialCue.AddTimer(() => spacialCueDuration.value, DisplayTarget);

        //DisplayTarget state ----------------------------------------------------------------------------------------------------------------------------------------------
        DisplayTarget.AddSpecificInitializationMethod(() => SpatialCue_GO.SetActive(false));
        DisplayTarget.AddTimer(() => displayTargetDuration.value, Mask);

        //Mask state ----------------------------------------------------------------------------------------------------------------------------------------------
        Mask.AddSpecificInitializationMethod(() =>
        {
            PreCue_GO.SetActive(false);

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
        Feedback.AddUniversalTerminationMethod(() =>
        {
            TokenFBController.enabled = false;
            HaloFBController.Destroy();
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
        PreCue_GO = new GameObject("PreCue");
        PreCue_GO.SetActive(false);
        PreCue_GO.transform.parent = AntiSaccade_CanvasGO.transform;
        PreCue_GO.transform.localPosition = Vector3.zero;
        PreCue_GO.transform.localScale = Vector3.one;
        RectTransform preCueRect = PreCue_GO.AddComponent<RectTransform>();
        preCueRect.sizeDelta = new Vector2(75, 75);
        Image preCueImage = PreCue_GO.AddComponent<Image>();
        preCueImage.sprite = Resources.Load<Sprite>("plus");
        preCueImage.color = new Color32(24, 255, 0, 255);

        SpatialCue_GO = new GameObject("SpacialCue");
        SpatialCue_GO.SetActive(false);
        SpatialCue_GO.transform.parent = AntiSaccade_CanvasGO.transform;
        SpatialCue_GO.transform.localScale = Vector3.one;
        RectTransform spatialCueRect = SpatialCue_GO.AddComponent<RectTransform>();
        spatialCueRect.sizeDelta = new Vector2(300, 300);
        Image spatialCueImage = SpatialCue_GO.AddComponent<Image>();
        spatialCueImage.sprite = Resources.Load<Sprite>("star");

        Mask_GO = new GameObject("Mask");
        Mask_GO.SetActive(false);
        Mask_GO.transform.parent = AntiSaccade_CanvasGO.transform;
        Mask_GO.transform.localScale = Vector3.one;
        RectTransform maskRect = Mask_GO.AddComponent<RectTransform>();
        maskRect.sizeDelta = new Vector2(300, 300);
        Image maskImage = Mask_GO.AddComponent<Image>();
        maskImage.sprite = Resources.Load<Sprite>("questionMark");
        maskImage.color = Color.black;

    }

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
        TrialData.AddDatum("RandomSpacialCue", () => CurrentTrialDef.RandomSpatialCueColor);
        TrialData.AddDatum("TargetStimIndex", () => CurrentTrialDef.TargetStimIndex);
        TrialData.AddDatum("DistractorStimIndices", () => DistractorStimIndices_String);
        TrialData.AddDatum("SpacialCuePos", () => CurrentTrialDef.SpacialCue_Pos.ToString());
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







//Positions dict if ever want to just have them specify index of target:
//ChooseStimPositions = new Dictionary<int, Vector3[]>
//{
//    { 2, new Vector3[] { new Vector3(-400, -25, 0), new Vector3(400, -25, 0) } },
//    { 3, new Vector3[] { new Vector3(-550, -25, 0), new Vector3(0, -25, 0), new Vector3(550, -25, 0) } },
//    { 4, new Vector3[] { new Vector3(-700, -25, 0), new Vector3(-225, -25, 0), new Vector3(225, -25, 0), new Vector3(700, -25, 0)} }
//};
