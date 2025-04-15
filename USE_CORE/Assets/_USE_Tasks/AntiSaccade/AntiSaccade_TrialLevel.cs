/*
MIT License

Copyright (c) 2023 Multitask - Unified - Suite -for-Expts

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files(the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/



using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using USE_States;
using USE_ExperimentTemplate_Trial;
using USE_StimulusManagement;
using AntiSaccade_Namespace;
using ConfigDynamicUI;
using UnityEngine.UI;
using System.Linq;
using static SelectionTracking.SelectionTracker;


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

    private float ChosenDisplayTargetDuration;
    private float ChosenSpatialCueDelayDuration;

    [HideInInspector] public ConfigNumber minObjectTouchDuration, maxObjectTouchDuration;

    private GameObject ChosenGO = null;
    private AntiSaccade_StimDef ChosenStim = null;

    //DATA:
    [HideInInspector] public int TrialCompletions_Block;
    [HideInInspector] public int TrialsCorrect_Block;
    [HideInInspector] public int TokenBarCompletions_Block;


    //for data:
    private string DistractorStimIndices_String;
    private string DistractorStimsChoosePos_String;
    private bool SpinCorrectSelection;
    private bool GotTrialCorrect;
    private float ReactionTime;
    [HideInInspector] public List<float?> ReactionTimes_InBlock = new List<float?>();


    //MAIN TARGET:
    private GameObject TargetStim_GO;
    

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
            if (StartButton == null)
            {
                if (Session.SessionDef.IsHuman)
                {
                    StartButton = Session.HumanStartPanel.StartButtonGO;
                    Session.HumanStartPanel.SetVisibilityOnOffStates(InitTrial, InitTrial);
                }
                else
                {
                    StartButton = Session.USE_StartButton.CreateStartButton(AntiSaccade_CanvasGO.GetComponent<Canvas>(), CurrentTask.StartButtonPosition, CurrentTask.StartButtonScale);
                    Session.USE_StartButton.SetVisibilityOnOffStates(InitTrial, InitTrial);
                }
            }

            HaloFBController.SetPositiveHaloColor(Color.yellow);
            HaloFBController.SetNegativeHaloColor(Color.gray);
            HaloFBController.SetCircleHaloIntensity(3f);
        });

        //SetupTrial state ----------------------------------------------------------------------------------------------------------------------------------------------
        SetupTrial.AddSpecificInitializationMethod(() =>
        {
            TokenFBController.enabled = false;
            LoadConfigUIVariables();

            SetDataStrings();
            
            CreateIcons();
            //CreateGameObjects();

            //***** SET TARGET STIM *****
            TargetStim_GO = targetStim.stimDefs[0].StimGameObject;
        });
        SetupTrial.SpecifyTermination(() => true, InitTrial);

        //------------------------------------------------------------------------------------------------------------------------
        // The code below allows the SelectionHandler to switch on the basis of the SelectionType in the SessionConfig
        SelectionHandler ShotgunHandler;

        if (Session.SessionDef.SelectionType?.ToLower() == "gaze")
            ShotgunHandler = Session.SelectionTracker.SetupSelectionHandler("trial", "GazeShotgun", Session.GazeTracker, InitTrial, ChooseStim);
        else
            ShotgunHandler = Session.SelectionTracker.SetupSelectionHandler("trial", "TouchShotgun", Session.MouseTracker, InitTrial, ChooseStim);

        TouchFBController.EnableTouchFeedback(ShotgunHandler, CurrentTask.TouchFeedbackDuration, CurrentTask.StartButtonScale * 30, AntiSaccade_CanvasGO, true); //Enable Touch Feedback:

        //InitTrial state ----------------------------------------------------------------------------------------------------------------------------------------------
        InitTrial.AddSpecificInitializationMethod(() =>
        {
            SetTrialSummaryString();

            if (CurrentTask.StimFacingCamera)
            {
                MakeStimFaceCamera(targetStim);
                MakeStimFaceCamera(distractorStims);
            }

            SetShadowType(CurrentTask.ShadowType, "AntiSaccade_DirectionalLight");

            if (ShotgunHandler.AllSelections.Count > 0)
                ShotgunHandler.ClearSelections();
            ShotgunHandler.MinDuration = minObjectTouchDuration.value;
            ShotgunHandler.MaxDuration = maxObjectTouchDuration.value;
        });
        InitTrial.SpecifyTermination(() => ShotgunHandler.LastSuccessfulSelectionMatchesStartButton(), PreCue, () => TokenFBController.enabled = true);

        //PreCue state ----------------------------------------------------------------------------------------------------------------------------------------------
        PreCue.AddSpecificInitializationMethod(() =>
        {
            if (CurrentTrial.PreCue_Size > 0)
                PreCue_GO.GetComponent<RectTransform>().sizeDelta = new Vector2(CurrentTrial.PreCue_Size, CurrentTrial.PreCue_Size);
            PreCue_GO.SetActive(true);
            Session.EventCodeManager.SendCodeThisFrame(TaskEventCodes["PreCueOn"]);

        });
        PreCue.AddTimer(() => CurrentTrial.PreCueDuration, AlertCue, () =>
        {
            PreCue_GO.SetActive(false);
            Session.EventCodeManager.SendCodeThisFrame(TaskEventCodes["PreCueOff"]);
        });

        //AlertCue state ----------------------------------------------------------------------------------------------------------------------------------------------
        AlertCue.AddSpecificInitializationMethod(() => AudioFBController.Play("ContinueBeep"));
        AlertCue.AddTimer(() => CurrentTrial.AlertCueDuration, AlertCueDelay);

        //AlertCueDELAY state ----------------------------------------------------------------------------------------------------------------------------------------------
        AlertCueDelay.AddTimer(() => CurrentTrial.AlertCueDelayDuration, SpatialCue);

        //SpatialCue state ----------------------------------------------------------------------------------------------------------------------------------------------
        SpatialCue.AddSpecificInitializationMethod(() =>
        {
            //if (CurrentTrial.RandomSpatialCueColor)
                //SpatialCue_GO.GetComponent<Image>().color = GetRandomColor();

            SpatialCue_GO.transform.localPosition = CurrentTrial.SpatialCue_Pos;
            SpatialCue_GO.SetActive(true);
            Session.EventCodeManager.SendCodeThisFrame(TaskEventCodes["SpatialCueOn"]);
        });
        SpatialCue.AddTimer(() => CurrentTrial.SpatialCueDuration, SpatialCueDelay, () =>
        {
            if (!CurrentTrial.SpatialCueActiveThroughDisplayTarget)
            {
                SpatialCue_GO.SetActive(false);
                Session.EventCodeManager.SendCodeThisFrame(TaskEventCodes["SpatialCueOff"]);
            }

            CalculateDelayAndDuration();
        });

        //SpatialCueDELAY state ----------------------------------------------------------------------------------------------------------------------------------------------
        SpatialCueDelay.AddTimer(() => ChosenSpatialCueDelayDuration, DisplayTarget);

        //DisplayTarget state ----------------------------------------------------------------------------------------------------------------------------------------------
        DisplayTarget.AddSpecificInitializationMethod(() =>
        {
            Session.EventCodeManager.SendCodeThisFrame(TaskEventCodes["TargetOn"]);
        });

        DisplayTarget.AddTimer(() => ChosenDisplayTargetDuration, Mask, () =>
        {
            Session.EventCodeManager.SendCodeThisFrame(TaskEventCodes["TargetOff"]);

            if (CurrentTrial.SpatialCueActiveThroughDisplayTarget)
            {
                SpatialCue_GO.SetActive(false);
                Session.EventCodeManager.SendCodeThisFrame(TaskEventCodes["SpatialCueOff"]);
            }
        });

        //Mask state ----------------------------------------------------------------------------------------------------------------------------------------------
        Mask.AddSpecificInitializationMethod(() =>
        {
            //if (CurrentTrial.RandomMaskColor)
                //Mask_GO.GetComponent<Image>().color = GetRandomColor();

            Mask_GO.transform.localPosition = CurrentTrial.Mask_Pos;
            Mask_GO.SetActive(true);
            Session.EventCodeManager.SendCodeThisFrame(TaskEventCodes["MaskOn"]);
        });
        Mask.AddTimer(() => CurrentTrial.MaskDuration, PostMaskDelay, () =>
        {
            Mask_GO.SetActive(false);
            Session.EventCodeManager.SendCodeThisFrame(TaskEventCodes["MaskOff"]);
        });


        //PostMaskDelay state ----------------------------------------------------------------------------------------------------------------------------------------------
        PostMaskDelay.AddTimer(() => CurrentTrial.PostMaskDelayDuration, ChooseStim);

        //ChooseStim state ----------------------------------------------------------------------------------------------------------------------------------------------
        bool stimChosen = false;
        ChooseStim.AddSpecificInitializationMethod(() =>
        {
            TargetStim_GO.transform.localPosition = CurrentTrial.TargetStim_ChoosePos;
            TargetStim_GO.SetActive(true);
            Session.EventCodeManager.SendCodeThisFrame(TaskEventCodes["TargetOn"]);

            ChosenGO = null;
            ChosenStim = null;
            stimChosen = false;

            if (ShotgunHandler.AllSelections.Count > 0)
                ShotgunHandler.ClearSelections();

            //reset it so the duration is 0 on exp display even if had one last trial
            OngoingSelection = null;
        });
        ChooseStim.AddUpdateMethod(() =>
        {
            ChosenGO = ShotgunHandler.LastSuccessfulSelection.SelectedGameObject;
            ChosenStim = ChosenGO?.GetComponent<StimDefPointer>()?.GetStimDef<AntiSaccade_StimDef>();
            if (ChosenStim != null)
            {
                stimChosen = true;
                if (CurrentTrial.DeactivateNonSelectedStimOnSel)
                    DeactivateStimNotSelected();
            }

            OngoingSelection = ShotgunHandler.OngoingSelection;

            //Update Exp Display with OngoingSelection Duration:
            if(OngoingSelection != null)
            {
                SetTrialSummaryString();
            }

        });
        ChooseStim.SpecifyTermination(() => stimChosen, Feedback);
        ChooseStim.AddTimer(() => CurrentTrial.ChooseStimDuration, Feedback);

        //Feedback state ----------------------------------------------------------------------------------------------------------------------------------------------
        float rotationSpeed = 260f;
        float totalRotation = 0f;
        Feedback.AddSpecificInitializationMethod(() =>
        {
            ReactionTime = ChooseStim.TimingInfo.Duration;
            ReactionTimes_InBlock.Add(ReactionTime);
            
            SpinCorrectSelection = false;
            totalRotation = 0f;

            if (ChosenStim == null)
                return;

            int? haloDepth = Session.Using2DStim ? 10 : (int?)null;

            float? tokenYAdjustment = Session.Using2DStim ? -5f : (float?)null; //used to adjust where the tokens appear in relation to GameObject

            if (ChosenStim.IsTarget)
            {
                GotTrialCorrect = true;

                if (CurrentTrial.UseSpinAnimation)
                    SpinCorrectSelection = true;

                HaloFBController.ShowPositive(ChosenGO, particleHaloActive: CurrentTrial.ParticleHaloActive, circleHaloActive: CurrentTrial.CircleHaloActive, depth: haloDepth);
                TokenFBController.AddTokens(ChosenGO, CurrentTrial.TokenGain, tokenYAdjustment);
                Session.EventCodeManager.SendCodeThisFrame("CorrectResponse");
                
                runningPerformance.Add(0);
            }
            else
            {
                HaloFBController.ShowNegative(ChosenGO, particleHaloActive: CurrentTrial.ParticleHaloActive, circleHaloActive: CurrentTrial.CircleHaloActive, depth: haloDepth);
                TokenFBController.RemoveTokens(ChosenGO, CurrentTrial.TokenLoss, tokenYAdjustment);
                Session.EventCodeManager.SendCodeThisFrame("IncorrectResponse");
                
                runningPerformance.Add(1);
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

            if (Time.time - Feedback.TimingInfo.StartTimeAbsolute > CurrentTrial.HaloFbDuration)
                HaloFBController.DestroyAllHalos();
        });
        Feedback.AddTimer(() => CurrentTrial.FeedbackDuration, ITI);
        Feedback.SpecifyTermination(() => !stimChosen, ITI, () => AudioFBController.Play("Negative"));
        Feedback.AddUniversalTerminationMethod(() =>
        {
            TokenFBController.enabled = false;
            TargetStim_GO.SetActive(false);
            Session.EventCodeManager.SendCodeThisFrame(TaskEventCodes["TargetOff"]);
        });


        //ITI state ----------------------------------------------------------------------------------------------------------------------------------------------
        ITI.AddTimer(() => CurrentTrial.ItiDuration, FinishTrial);

        DefineTrialData();
        DefineFrameData();

    }

    //--------------Helper Methods--------------------------------------------------------------------------------------------------------------------
    private void CalculateDelayAndDuration()
    {
        System.Random random = new System.Random();
        
        int randomIndex = random.Next(CurrentTrial.SpatialCueDelayDuration.Length);
        ChosenSpatialCueDelayDuration = CurrentTrial.SpatialCueDelayDuration[randomIndex];
        Debug.Log(("CHOSEN SPATIALCUEDELAYDURATION: " + ChosenSpatialCueDelayDuration));
        
        int randomIndex2 = random.Next(CurrentTrial.DisplayTargetDuration.Length);
        ChosenDisplayTargetDuration = CurrentTrial.DisplayTargetDuration[randomIndex2];
        Debug.Log(("CHOSEN DISPLAYTARGETDURATION: " + ChosenDisplayTargetDuration));
    }
    public override void OnTokenBarFull()
    {
        TokenBarCompletions_Block++;
        CurrentTaskLevel.TokenBarsCompleted_Task++;

        StartCoroutine(Session.SyncBoxController?.SendRewardPulses(CurrentTrial.NumPulses, CurrentTrial.PulseSize));
        CurrentTaskLevel.NumRewardPulses_InBlock += CurrentTrial.NumPulses;
        CurrentTaskLevel.NumRewardPulses_InTask += CurrentTrial.NumPulses;

        StartCoroutine(ResetTbAfterFilled());

    }

    private IEnumerator ResetTbAfterFilled()
    {
        yield return new WaitForSeconds(1.5f); //reset tb after waiting a second
        TokenFBController.SetTokenBarValue(CurrentTaskLevel.currentBlockDef.NumInitialTokens);
    }

    private void MakeStimFaceCamera(StimGroup stims)
    {
        foreach (var stim in stims.stimDefs)
            stim.StimGameObject.AddComponent<FaceCamera>();
    }

    public override void AddToStimLists()
    {
        foreach(AntiSaccade_StimDef stim in targetStim.stimDefs)
            Session.TargetObjects.Add(stim.StimGameObject);
    }

    private void CreateIcons()
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
            preCueImage.color = new Color32(255, 255, 185, 255);
        }
        
        SpatialCue_GO = Instantiate(Resources.Load<GameObject>("asterisk_black"));
        SpatialCue_GO.name = "SpatialCue";
        SpatialCue_GO.SetActive(false);
        SpatialCue_GO.transform.localPosition = Vector3.zero;
        SpatialCue_GO.transform.localScale *= 0.75f; // adjusting to match scale to 0.6 externalstimscale
        SpatialCue_GO.AddComponent<FaceCamera>();
        
        Mask_GO = Instantiate(Resources.Load<GameObject>("hashtag_black_old"));
        Mask_GO.name = "Mask";
        Mask_GO.SetActive(false);
        Mask_GO.transform.localPosition = Vector3.zero;
        SpatialCue_GO.AddComponent<FaceCamera>();
        Mask_GO.transform.localScale *= 0.75f; // adjusting to match scale to 0.6 externalstimscale
        if (CurrentTrial.SpatialCue_Pos[0] < 0)
            Mask_GO.transform.localRotation = Quaternion.Euler(0f, 85f, 0f);
        else
            Mask_GO.transform.localRotation = Quaternion.Euler(0f, 95f, 0f);
    }

    private void CreateGameObjects()
    {


        if (SpatialCue_GO == null)
        {
            SpatialCue_GO = new GameObject("SpatialCue");
            SpatialCue_GO.SetActive(false);
            SpatialCue_GO.transform.parent = AntiSaccade_CanvasGO.transform;
            SpatialCue_GO.transform.localScale = Vector3.one;
            RectTransform spatialCueRect = SpatialCue_GO.AddComponent<RectTransform>();
            spatialCueRect.sizeDelta = new Vector2(200, 200);
            Image spatialCueImage = SpatialCue_GO.AddComponent<Image>();
            spatialCueImage.sprite = Resources.Load<Sprite>("Star"); //initially using Star as default
            spatialCueImage.color = Color.black;
        }

        if (Mask_GO == null)
        {
            Mask_GO = new GameObject("Mask");
            Mask_GO.SetActive(false);
            Mask_GO.transform.parent = AntiSaccade_CanvasGO.transform;
            Mask_GO.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
            RectTransform maskRect = Mask_GO.AddComponent<RectTransform>();
            maskRect.sizeDelta = new Vector2(200, 200);
            Image maskImage = Mask_GO.AddComponent<Image>();
            maskImage.sprite = Resources.Load<Sprite>("hashtag_black");
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
        ReactionTime = 0;
    }

    public void ResetBlockVariables()
    {


        TrialsCorrect_Block = 0;
        TrialCompletions_Block = 0;
        TokenBarCompletions_Block = 0;
        calculatedThreshold_timing = 0;
        reversalsCount = 0;
        blockAccuracy = 0;
        
        DiffLevelsSummary.Clear();
        DiffLevelsAtReversals.Clear();
        TimingValuesAtReversals.Clear();
        runningPerformance.Clear();
        ReactionTimes_InBlock.Clear();
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
        TrialSummaryString = "Trial #" + (TrialCount_InBlock + 1) + " In Block" +
                             "\nNum Distractors: " + CurrentTrial.DistractorStimIndices.Length +
                             "\nDifficulty Level: " + difficultyLevel +
                             "\nDisplay Target Duration (sec): " + ChosenDisplayTargetDuration +
                             "\nSpatial Cue Delay Duration (sec): " + ChosenSpatialCueDelayDuration +
                             "\n" +
                             "\nOngoingSelection: " + (OngoingSelection == null ? "" : OngoingSelection.Duration.Value.ToString("F2") + " s");
    }

    private void LoadConfigUIVariables()
    {
        minObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("minObjectTouchDuration");
        maxObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("maxObjectTouchDuration");
    }

    protected override void DefineTrialStims()
    {
        StimGroup group = Session.UsingDefaultConfigs ? PrefabStims : ExternalStims;

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
        TrialData.AddDatum("TrialID", () => CurrentTrial.TrialID);
        TrialData.AddDatum("GotTrialCorrect", () => GotTrialCorrect);
        TrialData.AddDatum("RandomSpatialCue", () => CurrentTrial.RandomSpatialCueColor);
        TrialData.AddDatum("TargetStimIndex", () => CurrentTrial.TargetStimIndex);
        TrialData.AddDatum("DistractorStimIndices", () => DistractorStimIndices_String);
        TrialData.AddDatum("SpatialCuePos", () => CurrentTrial.SpatialCue_Pos.ToString());
        TrialData.AddDatum("TargetStimDisplayPos", () => CurrentTrial.TargetStim_DisplayPos.ToString());
        TrialData.AddDatum("TargetStimChoosePos", () => CurrentTrial.TargetStim_ChoosePos.ToString());
        TrialData.AddDatum("DistractorStimsChoosePos", () => DistractorStimsChoosePos_String);
        TrialData.AddDatum("ReactionTime", ()=> ReactionTime);

    }

    private void DefineFrameData()
    {
        FrameData.AddDatum("StartButton", () => StartButton != null && StartButton.activeInHierarchy ? "Active" : "NotActive");
        FrameData.AddDatum("TargetStimActive", () => TargetStim_GO != null && TargetStim_GO.activeInHierarchy ? "Active" : "NotActive");
        FrameData.AddDatum("DistractorStimsActive", () => distractorStims?.IsActive);
        FrameData.AddDatum("PreCueActive", () => PreCue_GO != null && PreCue_GO.activeInHierarchy ? "Active" : "NotActive");
        FrameData.AddDatum("SpatialCueActive", () => SpatialCue_GO != null && SpatialCue_GO.activeInHierarchy ? "Active" : "NotActive");
        FrameData.AddDatum("MaskActive", () => Mask_GO != null && Mask_GO.activeInHierarchy ? "Active" : "NotActive");
    }

    private void SetDataStrings()
    {
        if (CurrentTrial.DistractorStimIndices.Length > 0)
            DistractorStimIndices_String = $"[{string.Join(", ", CurrentTrial.DistractorStimIndices)}]";

        if (CurrentTrial.DistractorStims_ChoosePos.Length > 0)
            DistractorStimsChoosePos_String = $"[{string.Join(", ", CurrentTrial.DistractorStims_ChoosePos)}]";
    }
    
    public override void DefineCustomTrialDefSelection()
    {
        TrialDefSelectionStyle = CurrentTrial.TrialDefSelectionStyle;
        posStep = CurrentTrial.PosStep;
        negStep = CurrentTrial.NegStep;
        maxDiffLevel = CurrentTrial.MaxDiffLevel;
        avgDiffLevel = CurrentTrial.AvgDiffLevel;
        diffLevelJitter = CurrentTrial.DiffLevelJitter;
        NumReversalsUntilTerm = CurrentTrial.NumReversalsUntilTerm;
        MinTrialsBeforeTermProcedure = CurrentTrial.MinTrialsBeforeTermProcedure;
        MaxTrialsInBlock = CurrentTrial.MaxTrialsInBlock;
        TerminationWindowSize = CurrentTrial.TerminationWindowSize;
        //BlockCount = CurrentTaskLevel.currentBlockDef.BlockCount;

        if (TrialDefSelectionStyle == "adaptive")
        {
            int randomDouble = avgDiffLevel + Random.Range(-diffLevelJitter, diffLevelJitter);
            difficultyLevel = randomDouble;
        }
        else if (TrialDefSelectionStyle == "default")
        {
            difficultyLevel = 0;
        }
    }
    
    protected override bool CheckBlockEnd()
    {
        switch (TrialDefSelectionStyle)
        {
            case "adaptive":
                int prevResult = -1;
                DiffLevelsSummary.Add(CurrentTrial.DifficultyLevel);

                Debug.Log("runningPerformance.Count: " + runningPerformance.Count + "/ mintrialsbeforeterm: " + MinTrialsBeforeTermProcedure);
                if (MinTrialsBeforeTermProcedure < 0 || runningPerformance.Count < MinTrialsBeforeTermProcedure + 1)
                    return false;

                if (runningPerformance.Count > 1) {
                    prevResult = runningPerformance[runningPerformance.Count - 2];
                    //prevResult = runningPerformance[^2];
                }

                if (runningPerformance.Last() == 1) {
                    if (prevResult == 0) {
                        DiffLevelsAtReversals.Add(CurrentTrial.DifficultyLevel);
                        TimingValuesAtReversals.Add(ChosenDisplayTargetDuration);
                
                        reversalsCount++;
                    }
                }
                else if (runningPerformance.Last() == 0) {
                    if (prevResult == 1) {
                        DiffLevelsAtReversals.Add(CurrentTrial.DifficultyLevel);
                        TimingValuesAtReversals.Add(ChosenDisplayTargetDuration);
                        reversalsCount++;
                    }
                }

                //TaskLevelTemplate_Methods TaskLevel_Methods = new TaskLevelTemplate_Methods();
                Debug.Log("reversalsCount: " + reversalsCount + " / NumReversalsUntilTerm: " + NumReversalsUntilTerm);
                Debug.Log("SpatialCueDelayDuration: " + ChosenSpatialCueDelayDuration + " / DisplayTargetDuration: " + ChosenDisplayTargetDuration);
                if (NumReversalsUntilTerm != -1 && reversalsCount >= NumReversalsUntilTerm) { //ensure that early reversals don't mess with termination
                    List<int> lastElements = DiffLevelsAtReversals.Skip(DiffLevelsAtReversals.Count - NumReversalsUntilTerm).ToList();
                    calculatedThreshold_timing = (int)lastElements.Average();
                    Debug.Log("The average DL at the last " + NumReversalsUntilTerm + " reversals is " + calculatedThreshold_timing);
            
                    List<float> lastElements_timing = TimingValuesAtReversals.Skip(TimingValuesAtReversals.Count - NumReversalsUntilTerm).ToList();
                    Debug.Log("lastElements_timing: " + string.Join(", ", lastElements_timing));
            
                    calculatedThreshold_timing = lastElements_timing.Average();
                    Debug.Log("calculatedThreshold_timing: " + calculatedThreshold_timing);
            
                    return true;
                }
                if (TrialCount_InBlock >= MaxTrialsInBlock)
                    return true;
                return false;
            
            default:
                if (TrialCount_InBlock == maxDiffLevel - 1)
                    return true;
                return false;
        }
    }

}


