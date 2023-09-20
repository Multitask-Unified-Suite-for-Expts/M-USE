using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using USE_States;
using USE_ExperimentTemplate_Trial;
using USE_StimulusManagement;
using AntiSaccade_Namespace;
using ConfigDynamicUI;
using UnityEngine.UI;
using System;


public class AntiSaccade_TrialLevel : ControlLevel_Trial_Template
{
    public AntiSaccade_TrialDef CurrentTrial => GetCurrentTrialDef<AntiSaccade_TrialDef>();
    public AntiSaccade_TaskLevel CurrentTaskLevel => GetTaskLevel<AntiSaccade_TaskLevel>();
    public AntiSaccade_TaskDef CurrentTask => GetTaskDef<AntiSaccade_TaskDef>();

    public GameObject AntiSaccade_CanvasGO;

    private GameObject StartButton;

    private GameObject PreCue_GO;
    private GameObject SpatialCue_GO;
    private GameObject Mask_GO;

    private StimGroup targetStim;
    private StimGroup distractorStims;

    [HideInInspector] public ConfigNumber minObjectTouchDuration, maxObjectTouchDuration;

    private GameObject ChosenGO = null;
    private AntiSaccade_StimDef ChosenStim = null;

    //DATA:
    [HideInInspector] public string SaccadeType;
    [HideInInspector] public int TrialCompletions_Block;
    [HideInInspector] public int TrialsCorrect_Block;
    [HideInInspector] public int TokenBarCompletions_Block;


    //for data:
    private string DistractorStimIndices_String;
    private string DistractorStimsChoosePos_String;

    private bool SpinCorrectSelection;

    private bool GotTrialCorrect;

    //MAIN TARGET:
    private GameObject TargetStim_GO;

    private bool IconsLoaded;


    public override void DefineControlLevel()
    {
        State InitTrial = new State("InitTrial");
        State PreCue = new State("PreCue");
        State AlertCue = new State("AlertCue");
        State AlertCueDelay = new State("AlertCueDelay");
        State SpatialCue = new State("SpatialCue");
        State SpatialCueDelay = new State("SpatialCueDelay");
        State DisplayTarget = new State("DisplayTarget");
        State Mask = new State("Mask");
        State PostMaskDelay = new State("PostMaskDelay");
        State ChooseStim = new State("ChooseStim");
        State Feedback = new State("Feedback");
        State ITI = new State("ITI");
        AddActiveStates(new List<State> { InitTrial, PreCue, AlertCue, AlertCueDelay, SpatialCue, SpatialCueDelay, DisplayTarget, Mask, PostMaskDelay, ChooseStim, Feedback, ITI });

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
        });

        //SetupTrial state ----------------------------------------------------------------------------------------------------------------------------------------------
        SetupTrial.AddSpecificInitializationMethod(() =>
        {
            IconsLoaded = false;

            TokenFBController.enabled = false;

            LoadConfigUIVariables();

            TokenFBController.SetTotalTokensNum(CurrentTrial.TokenBarCapacity);
            TokenFBController.SetTokenBarValue(CurrentTrial.NumInitialTokens);

            //Set string of Whether its AntiSaccade or ProSaccade by checking if X values are the same. 
            SaccadeType = CurrentTrial.TargetStim_DisplayPos.x == CurrentTrial.SpatialCue_Pos.x ? "Pro" : "Anti";

            SetDataStrings();

            if (SpatialCue_GO != null && Mask_GO != null)
                StartCoroutine(LoadSpacialCueAndMaskIcons());
            else
                Debug.LogError("CANT SET THE SPATIAL CUE AND MASK ICONS BECAUSE ATLEAST ONE OF THEM IS NULL!");

            //***** SET TARGET STIM *****
            TargetStim_GO = targetStim.stimDefs[0].StimGameObject;
        });
        SetupTrial.SpecifyTermination(() => true && IconsLoaded, InitTrial);

        var Handler = SessionValues.SelectionTracker.SetupSelectionHandler("trial", "MouseButton0Click", SessionValues.MouseTracker, InitTrial, ChooseStim); //Setup Handler (may eventually wanna use shotgun handler)
        TouchFBController.EnableTouchFeedback(Handler, CurrentTask.TouchFeedbackDuration, CurrentTask.StartButtonScale * 30, AntiSaccade_CanvasGO); //Enable Touch Feedback:

        //InitTrial state ----------------------------------------------------------------------------------------------------------------------------------------------
        InitTrial.AddSpecificInitializationMethod(() =>
        {
            Camera.main.gameObject.GetComponent<Skybox>().enabled = false; //Disable cam's skybox so the RenderSettings.Skybox can show the Context background

            if (Handler.AllSelections.Count > 0)
                Handler.ClearSelections();
            Handler.MinDuration = minObjectTouchDuration.value;
            Handler.MaxDuration = maxObjectTouchDuration.value;

            SetTrialSummaryString();
        });
        InitTrial.SpecifyTermination(() => Handler.LastSuccessfulSelectionMatchesStartButton(), PreCue, () => TokenFBController.enabled = true);

        //PreCue state ----------------------------------------------------------------------------------------------------------------------------------------------
        PreCue.AddSpecificInitializationMethod(() =>
        {
            if (CurrentTrial.PreCue_Size > 0)
                PreCue_GO.GetComponent<RectTransform>().sizeDelta = new Vector2(CurrentTrial.PreCue_Size, CurrentTrial.PreCue_Size);
            PreCue_GO.SetActive(true);
            SessionValues.EventCodeManager.SendCodeImmediate(TaskEventCodes["PreCueOn"]);

        });
        PreCue.AddTimer(() => CurrentTrial.PreCueDuration, AlertCue, () =>
        {
            PreCue_GO.SetActive(false);
            SessionValues.EventCodeManager.SendCodeImmediate(TaskEventCodes["PreCueOff"]);
        });

        //AlertCue state ----------------------------------------------------------------------------------------------------------------------------------------------
        AlertCue.AddSpecificInitializationMethod(() => AudioFBController.Play("ContinueBeep"));
        AlertCue.AddTimer(() => CurrentTrial.AlertCueDuration, AlertCueDelay);

        //AlertCueDELAY state ----------------------------------------------------------------------------------------------------------------------------------------------
        AlertCueDelay.AddTimer(() => CurrentTrial.AlertCueDelayDuration, SpatialCue);

        //SpatialCue state ----------------------------------------------------------------------------------------------------------------------------------------------
        SpatialCue.AddSpecificInitializationMethod(() =>
        {
            if (CurrentTrial.RandomSpatialCueColor)
                SpatialCue_GO.GetComponent<Image>().color = GetRandomColor();

            SpatialCue_GO.transform.localPosition = CurrentTrial.SpatialCue_Pos;
            SpatialCue_GO.SetActive(true);
            SessionValues.EventCodeManager.SendCodeImmediate(TaskEventCodes["SpatialCueOn"]);

        });
        SpatialCue.AddTimer(() => CurrentTrial.SpatialCueDuration, SpatialCueDelay, () =>
        {
            if (!CurrentTrial.SpatialCueActiveThroughDisplayTarget)
            {
                SpatialCue_GO.SetActive(false);
                SessionValues.EventCodeManager.SendCodeImmediate(TaskEventCodes["SpatialCueOff"]);
            }
        });

        //SpatialCueDELAY state ----------------------------------------------------------------------------------------------------------------------------------------------
        SpatialCueDelay.AddTimer(() => CurrentTrial.SpatialCueDelayDuration, DisplayTarget);

        //DisplayTarget state ----------------------------------------------------------------------------------------------------------------------------------------------
        DisplayTarget.AddSpecificInitializationMethod(() =>
        {
            SessionValues.EventCodeManager.SendCodeImmediate(TaskEventCodes["TargetOn"]);
        });
        DisplayTarget.AddTimer(() => CurrentTrial.DisplayTargetDuration, Mask, () =>
        {
            SessionValues.EventCodeManager.SendCodeImmediate(TaskEventCodes["TargetOff"]);

            if (CurrentTrial.SpatialCueActiveThroughDisplayTarget)
            {
                SpatialCue_GO.SetActive(false);
                SessionValues.EventCodeManager.SendCodeImmediate(TaskEventCodes["SpatialCueOff"]);
            }
        });

        //Mask state ----------------------------------------------------------------------------------------------------------------------------------------------
        Mask.AddSpecificInitializationMethod(() =>
        {
            if (CurrentTrial.RandomMaskColor)
                Mask_GO.GetComponent<Image>().color = GetRandomColor();

            Mask_GO.transform.localPosition = new Vector3(CurrentTrial.TargetStim_DisplayPos.x, CurrentTrial.TargetStim_DisplayPos.y + 25f, CurrentTrial.TargetStim_DisplayPos.z); //have to adjust the Y cuz pics have padding
            Mask_GO.SetActive(true);
            SessionValues.EventCodeManager.SendCodeImmediate(TaskEventCodes["MaskOn"]);

        });
        Mask.AddTimer(() => CurrentTrial.MaskDuration, PostMaskDelay, () =>
        {
            Mask_GO.SetActive(false);
            SessionValues.EventCodeManager.SendCodeImmediate(TaskEventCodes["MaskOff"]);
        });


        //PostMaskDelay state ----------------------------------------------------------------------------------------------------------------------------------------------
        PostMaskDelay.AddTimer(() => CurrentTrial.PostMaskDelayDuration, ChooseStim);

        //ChooseStim state ----------------------------------------------------------------------------------------------------------------------------------------------
        bool stimChosen = false;
        ChooseStim.AddSpecificInitializationMethod(() =>
        {
            TargetStim_GO.transform.localPosition = CurrentTrial.TargetStim_ChoosePos;
            TargetStim_GO.SetActive(true);
            SessionValues.EventCodeManager.SendCodeImmediate(TaskEventCodes["TargetOn"]);

            ChosenGO = null;
            ChosenStim = null;
            stimChosen = false;

            if (Handler.AllSelections.Count > 0)
                Handler.ClearSelections();
        });
        ChooseStim.AddUpdateMethod(() =>
        {
            ChosenGO = Handler.LastSuccessfulSelection.SelectedGameObject;
            ChosenStim = ChosenGO?.GetComponent<StimDefPointer>()?.GetStimDef<AntiSaccade_StimDef>();
            if (ChosenStim != null)
            {
                stimChosen = true;
                if (CurrentTrial.DeactivateNonSelectedStimOnSel)
                    DeactivateStimNotSelected();
            }
        });
        ChooseStim.SpecifyTermination(() => stimChosen, Feedback);
        ChooseStim.AddTimer(() => CurrentTrial.ChooseStimDuration, Feedback);

        //Feedback state ----------------------------------------------------------------------------------------------------------------------------------------------
        float rotationSpeed = 260f;
        float totalRotation = 0f;
        Feedback.AddSpecificInitializationMethod(() =>
        {
            SpinCorrectSelection = false;
            totalRotation = 0f;

            if (ChosenStim == null)
                return;

            int? haloDepth = SessionValues.Using2DStim ? 10 : (int?)null;

            float? tokenYAdjustment = -5f; //used to adjust where the tokens appear in relation to GameObject

            if (ChosenStim.IsTarget)
            {
                GotTrialCorrect = true;

                if (CurrentTrial.UseSpinAnimation)
                    SpinCorrectSelection = true;

                HaloFBController.ShowPositive(ChosenGO, haloDepth);
                TokenFBController.AddTokens(ChosenGO, CurrentTrial.RewardMag, tokenYAdjustment);
                SessionValues.EventCodeManager.SendCodeImmediate("CorrectResponse");
            }
            else
            {
                HaloFBController.ShowNegative(ChosenGO, haloDepth);
                TokenFBController.RemoveTokens(ChosenGO, CurrentTrial.RewardMag, tokenYAdjustment);
                SessionValues.EventCodeManager.SendCodeImmediate("IncorrectResponse");
            }
        });
        Feedback.AddUpdateMethod(() =>
        {
            if (SpinCorrectSelection)
            {
                float rotationAmount = rotationSpeed * Time.deltaTime;
                if (rotationAmount + totalRotation >= 360)
                    rotationAmount -= (rotationAmount + totalRotation - 360);
                ChosenStim.StimGameObject.transform.Rotate(Vector3.forward, rotationAmount);
                totalRotation += rotationAmount;
                if (totalRotation >= 360f)
                {
                    transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                    SpinCorrectSelection = false;
                }
            }

            if (Time.time - Feedback.TimingInfo.StartTimeAbsolute > CurrentTrial.HaloFbDuration && !HaloFBController.IsHaloGameObjectNull())
                HaloFBController.Destroy();
        });
        Feedback.AddTimer(() => CurrentTrial.FeedbackDuration, ITI);
        Feedback.SpecifyTermination(() => !stimChosen, ITI, () => AudioFBController.Play("Negative"));
        Feedback.AddUniversalTerminationMethod(() =>
        {
            if (TokenFBController.IsTokenBarFull())
            {
                TokenBarCompletions_Block++;
                CurrentTaskLevel.TokenBarsCompleted_Task++;

                if (SessionValues.SyncBoxController != null)
                {
                    SessionValues.SyncBoxController.SendRewardPulses(CurrentTrial.NumPulses, CurrentTrial.PulseSize);
                    CurrentTaskLevel.NumRewardPulses_InBlock += CurrentTrial.NumPulses;
                    CurrentTaskLevel.NumRewardPulses_InTask += CurrentTrial.NumPulses;
                }
                TokenFBController.ResetTokenBarFull();
            }
            TokenFBController.enabled = false;
            TargetStim_GO.SetActive(false);
            SessionValues.EventCodeManager.SendCodeImmediate(TaskEventCodes["TargetOff"]);
        });


        //Feedback state ----------------------------------------------------------------------------------------------------------------------------------------------
        ITI.AddTimer(() => CurrentTrial.ItiDuration, FinishTrial);

        DefineTrialData();
        DefineFrameData();

    }

    //--------------Helper Methods--------------------------------------------------------------------------------------------------------------------
    public override void AddToStimLists()
    {
        foreach(AntiSaccade_StimDef stim in targetStim.stimDefs)
            SessionValues.TargetObjects.Add(stim.StimGameObject);
    }

    private IEnumerator LoadSpacialCueAndMaskIcons()
    {
        Dictionary<string, GameObject> iconDict = new Dictionary<string, GameObject>()
        {
            { DetermineContextFilePath(CurrentTrial.SpatialCue_Icon), SpatialCue_GO },
            { DetermineContextFilePath(CurrentTrial.Mask_Icon), Mask_GO }
        };
        foreach(var entry in iconDict)
        {
            yield return LoadTexture(entry.Key, texResult =>
            {
                if (texResult != null)
                {
                    Sprite sprite = Sprite.Create(texResult, new Rect(0, 0, texResult.width, texResult.height), Vector2.zero);
                    entry.Value.GetComponent<Image>().sprite = sprite;
                }
                else
                    Debug.Log($"{entry.Value.name} TEX RESULT IS NULL!");
            });
        }
        IconsLoaded = true;
    }

    //WILL NEED TO TEST THIS METHOD FOR LOCAL AND SERVER CONFIGS!!!
    private string DetermineContextFilePath(string fileName)
    {
        string filePath = "";
        if (SessionValues.UsingDefaultConfigs)
            filePath = $"{SessionValues.SessionDef.ContextExternalFilePath}/{fileName}";
        else if (SessionValues.UsingServerConfigs)
            filePath = $"{SessionValues.SessionDef.ContextExternalFilePath}/{fileName}.png";
        else if (SessionValues.UsingLocalConfigs)
            filePath = GetContextNestedFilePath(SessionValues.SessionDef.ContextExternalFilePath, fileName);

        return filePath;
    }

    private void CreateGameObjects()
    {
        if (PreCue_GO == null)
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

        if (SpatialCue_GO == null)
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

        if (Mask_GO == null)
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

    private void DeactivateGameObjects()
    {
        if (PreCue_GO != null)
            PreCue_GO.SetActive(false);
        if (SpatialCue_GO != null)
            SpatialCue_GO.SetActive(false);
        if (Mask_GO != null)
            Mask_GO.SetActive(false);
    }

    private void DeactivateStimNotSelected()
    {
        foreach (var group in TrialStims)
            foreach (var stim in group.stimDefs)
                if (stim.StimGameObject != ChosenStim.StimGameObject)
                    stim.StimGameObject.SetActive(false);
    }

    public override void ResetTrialVariables()
    {
        GotTrialCorrect = false;
    }

    public void ResetBlockVariables()
    {
        TrialsCorrect_Block = 0;
        TrialCompletions_Block = 0;
        TokenBarCompletions_Block = 0;
    }

    public override void FinishTrialCleanup()
    {
        DeactivateGameObjects();

        if (AbortCode == 0)
        {
            TrialCompletions_Block++;
            CurrentTaskLevel.TrialsCompleted_Task++;

            if (GotTrialCorrect)
            {
                TrialsCorrect_Block++;
                CurrentTaskLevel.TrialsCorrect_Task++;
            }
            CurrentTaskLevel.CalculateBlockSummaryString();
        }
        else
        {
            CurrentTaskLevel.NumAbortedTrials_InBlock++;
            CurrentTaskLevel.NumAbortedTrials_InTask++;
        }
    }

    private void SetTrialSummaryString()
    {
        TrialSummaryString = "<b>Trial #" + (TrialCount_InBlock + 1) + " In Block" + "</b>" +
                             "\nSaccade Type: " + SaccadeType +
                             "\nNum Distractors: " + CurrentTrial.DistractorStimIndices.Length;
    }

    private void LoadConfigUIVariables()
    {
        minObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("minObjectTouchDuration");
        maxObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("maxObjectTouchDuration");
    }

    protected override void DefineTrialStims()
    {
        StimGroup group = SessionValues.UsingDefaultConfigs ? PrefabStims : ExternalStims;

        targetStim = new StimGroup("TargetStim", group, new List<int> { CurrentTrial.TargetStimIndex });
        foreach (AntiSaccade_StimDef stim in targetStim.stimDefs)
            stim.IsTarget = true;
        targetStim.SetLocations(new List<Vector3> { CurrentTrial.TargetStim_DisplayPos });
        targetStim.SetVisibilityOnOffStates(GetStateFromName("DisplayTarget"), GetStateFromName("DisplayTarget"));
        TrialStims.Add(targetStim);

        distractorStims = new StimGroup("DistractorStims", group, CurrentTrial.DistractorStimIndices);
        foreach (AntiSaccade_StimDef stim in distractorStims.stimDefs)
            stim.IsTarget = false; //just in case one was still true
        distractorStims.SetLocations(CurrentTrial.DistractorStims_ChoosePos);
        distractorStims.SetVisibilityOnOffStates(GetStateFromName("ChooseStim"), GetStateFromName("Feedback"));
        TrialStims.Add(distractorStims);
    }

    private void DefineTrialData()
    {
        TrialData.AddDatum("GotTrialCorrect", () => GotTrialCorrect);
        TrialData.AddDatum("SaccadeType", () => SaccadeType);
        TrialData.AddDatum("RandomSpatialCue", () => CurrentTrial.RandomSpatialCueColor);
        TrialData.AddDatum("TargetStimIndex", () => CurrentTrial.TargetStimIndex);
        TrialData.AddDatum("DistractorStimIndices", () => DistractorStimIndices_String);
        TrialData.AddDatum("SpatialCuePos", () => CurrentTrial.SpatialCue_Pos.ToString());
        TrialData.AddDatum("TargetStimDisplayPos", () => CurrentTrial.TargetStim_DisplayPos.ToString());
        TrialData.AddDatum("TargetStimChoosePos", () => CurrentTrial.TargetStim_ChoosePos.ToString());
        TrialData.AddDatum("DistractorStimsChoosePos", () => DistractorStimsChoosePos_String);
    }

    private void DefineFrameData()
    {
        FrameData.AddDatum("StartButton", () => StartButton.activeInHierarchy);
        FrameData.AddDatum("TargetStimActive", () => TargetStim_GO.activeInHierarchy);
        FrameData.AddDatum("DistractorStimsActive", () => distractorStims.IsActive);
        FrameData.AddDatum("PreCueActive", () => PreCue_GO.activeInHierarchy);
        FrameData.AddDatum("SpatialCueActive", () => SpatialCue_GO.activeInHierarchy);
        FrameData.AddDatum("MaskActive", () => Mask_GO.activeInHierarchy);
    }

    private void SetDataStrings()
    {
        if (CurrentTrial.DistractorStimIndices.Length > 0)
            DistractorStimIndices_String = $"[{string.Join(", ", CurrentTrial.DistractorStimIndices)}]";

        if (CurrentTrial.DistractorStims_ChoosePos.Length > 0)
            DistractorStimsChoosePos_String = $"[{string.Join(", ", CurrentTrial.DistractorStims_ChoosePos)}]";
    }

}


