using System.Collections.Generic;
using UnityEngine;
using USE_States;
using USE_StimulusManagement;
using FlexLearning_Namespace;
using USE_ExperimentTemplate_Trial;
using System.Linq;
using ConfigDynamicUI;
using USE_ExperimentTemplate_Task;
// #if (!UNITY_WEBGL)
// using static System.Windows.Forms.VisualStyles.VisualStyleElement;
// #endif  


public class FlexLearning_TrialLevel : ControlLevel_Trial_Template
{
    public FlexLearning_TrialDef CurrentTrialDef => GetCurrentTrialDef<FlexLearning_TrialDef>();
    public FlexLearning_TaskLevel CurrentTaskLevel => GetTaskLevel<FlexLearning_TaskLevel>();
    public FlexLearning_TaskDef currentTaskDef => GetTaskDef<FlexLearning_TaskDef>();

    public GameObject FL_CanvasGO;

    // Block End Variables
    public List<int> runningAcc;
    
    // Stimuli Variables
    private StimGroup tStim;
    private GameObject StartButton;

    // ConfigUI Variables
    private bool configUIVariablesLoaded;
    [HideInInspector]
    public ConfigNumber minObjectTouchDuration, itiDuration, 
        fbDuration, maxObjectTouchDuration, selectObjectDuration, tokenRevealDuration, tokenUpdateDuration, tokenFlashingDuration, 
        searchDisplayDelay;

    private float tokenFbDuration;
    

    // Set in the Task Level
    [HideInInspector] public bool? TokensWithStimOn;

    
    // Stim Evaluation Variables
    private GameObject selectedGO = null;
    private bool CorrectSelection;
    FlexLearning_StimDef selectedSD = null;
    private bool choiceMade = false;
    
    
    
    //Player View Variables
    private PlayerViewPanel playerView;
    private GameObject playerViewParent; // Helps set things onto the player view in the experimenter display
    private GameObject playerViewText;
    private Vector2 textLocation;
    private bool playerViewLoaded;
   
    // Block Data Variables
    [HideInInspector] public string ContextName = "";
    [HideInInspector] public int NumCorrect_InBlock;
    [HideInInspector] public List<float?> SearchDurations_InBlock = new List<float?>();
    [HideInInspector] public int NumErrors_InBlock;
    [HideInInspector] public int NumTokenBarFull_InBlock;
    [HideInInspector] public int TotalTokensCollected_InBlock;
    [HideInInspector] public decimal Accuracy_InBlock;
   
    // Trial Data Variables
    private int? SelectedStimIndex = null;
    private Vector3? SelectedStimLocation = null;
    private float SearchDuration = 0;
    private bool RewardGiven = false;

    [HideInInspector] public int PreSearch_TouchFbErrorCount;


    public override void DefineControlLevel()
    {
        State InitTrial = new State("InitTrial");
        State SearchDisplay = new State("SearchDisplay");
        State SearchDisplayDelay = new State("SearchDisplayDelay");
        State SelectionFeedback = new State("SelectionFeedback");
        State TokenFeedback = new State("TokenFeedback");
        State ITI = new State("ITI");

        AddActiveStates(new List<State> { InitTrial, SearchDisplay, SelectionFeedback, TokenFeedback, ITI, SearchDisplayDelay });
        
        Add_ControlLevel_InitializationMethod(() =>
        {
            playerView = new PlayerViewPanel(); //GameObject.Find("PlayerViewCanvas").GetComponent<PlayerViewPanel>()
            playerViewParent = GameObject.Find("MainCameraCopy");     
            
            // Initialize FB Controller Values
            HaloFBController.SetHaloSize(6f);
            HaloFBController.SetHaloIntensity(5);
        });
        
        SetupTrial.AddSpecificInitializationMethod(() =>
        {
            //Set the Stimuli Light/Shadow settings
            SetShadowType(currentTaskDef.ShadowType, "FlexLearning_DirectionalLight");
            if (currentTaskDef.StimFacingCamera)
                MakeStimFaceCamera();
            
            if (StartButton == null)
            {
                if (SessionValues.SessionDef.IsHuman)
                {
                    StartButton = SessionValues.HumanStartPanel.StartButtonGO;
                    SessionValues.HumanStartPanel.SetVisibilityOnOffStates(InitTrial, InitTrial);
                }
                else
                {
                    StartButton = SessionValues.USE_StartButton.CreateStartButton(FL_CanvasGO.GetComponent<Canvas>(), currentTaskDef.StartButtonPosition, currentTaskDef.StartButtonScale);
                    SessionValues.USE_StartButton.SetVisibilityOnOffStates(InitTrial, InitTrial);
                }
            }

            if (!configUIVariablesLoaded)
                LoadConfigUIVariables();

            UpdateExperimenterDisplaySummaryStrings();
        });
        SetupTrial.SpecifyTermination(() => true, InitTrial);

        //INIT TRIAL STATE ----------------------------------------------------------------------------------------------
        var ShotgunHandler = SessionValues.SelectionTracker.SetupSelectionHandler("trial", "TouchShotgun", SessionValues.MouseTracker, InitTrial, SearchDisplay);
        TouchFBController.EnableTouchFeedback(ShotgunHandler, currentTaskDef.TouchFeedbackDuration, currentTaskDef.StartButtonScale *10, FL_CanvasGO);

        InitTrial.AddSpecificInitializationMethod(() =>
        {
            Camera.main.gameObject.GetComponent<Skybox>().enabled = false; //Disable cam's skybox so the RenderSettings.Skybox can show the Context background

            if (SessionValues.SessionDef.MacMainDisplayBuild & !Application.isEditor) //adj text positions if running build with mac as main display
                TokenFBController.AdjustTokenBarSizing(200);

            TokenFBController.SetRevealTime(tokenRevealDuration.value);
            TokenFBController.SetUpdateTime(tokenUpdateDuration.value);
            TokenFBController.SetFlashingTime(tokenFlashingDuration.value);

            if (ShotgunHandler.AllSelections.Count > 0)
                ShotgunHandler.ClearSelections();

            ShotgunHandler.MinDuration = minObjectTouchDuration.value;
            ShotgunHandler.MaxDuration = maxObjectTouchDuration.value;

        });
        InitTrial.SpecifyTermination(() => ShotgunHandler.LastSuccessfulSelectionMatches(SessionValues.SessionDef.IsHuman ? SessionValues.HumanStartPanel.StartButtonChildren : SessionValues.USE_StartButton.StartButtonChildren), SearchDisplayDelay, () =>
        {
            SessionValues.EventCodeManager.SendCodeImmediate("StartButtonSelected");
        });


        // Provide delay following start button selection and before stimuli onset
        SearchDisplayDelay.AddTimer(() => searchDisplayDelay.value, SearchDisplay);
        
        // SEARCH DISPLAY STATE ----------------------------------------------------------------------------------------
        SearchDisplay.AddSpecificInitializationMethod(() =>
        {
            Input.ResetInputAxes(); //reset input in case they holding down
            TokenFBController.enabled = true;

            if (!SessionValues.WebBuild)
                ActivateChildren(playerViewParent);

            SessionValues.EventCodeManager.SendCodeNextFrame("StimOn");
            SessionValues.EventCodeManager.SendCodeNextFrame("TokenBarVisible");
            
            if (ShotgunHandler.AllSelections.Count > 0)
                ShotgunHandler.ClearSelections();

            PreSearch_TouchFbErrorCount = TouchFBController.ErrorCount;

            if (!SessionValues.WebBuild)
                CreateTextOnExperimenterDisplay();
        });
        SearchDisplay.AddUpdateMethod(() =>
        {
            if(ShotgunHandler.SuccessfulSelections.Count > 0)
            {
                selectedGO = ShotgunHandler.LastSuccessfulSelection.SelectedGameObject;
                selectedSD = selectedGO?.GetComponent<StimDefPointer>()?.GetStimDef<FlexLearning_StimDef>();
                ShotgunHandler.ClearSelections();
                if(selectedSD != null)
                    choiceMade = true;
            }
        });
        SearchDisplay.SpecifyTermination(() => choiceMade, SelectionFeedback, () =>
        {

        CorrectSelection = selectedSD.IsTarget;
        if (CorrectSelection)
        {
            NumCorrect_InBlock++;
            CurrentTaskLevel.NumCorrect_InTask++;
            runningAcc.Add(1);
            SessionValues.EventCodeManager.SendCodeNextFrame("Button0PressedOnTargetObject");//SELECTION STUFF (code may not be exact and/or could be moved to Selection handler)
            SessionValues.EventCodeManager.SendCodeNextFrame("CorrectResponse");
        }
        else
        {
            NumErrors_InBlock++;
            CurrentTaskLevel.NumErrors_InTask++;
            runningAcc.Add(0);
            SessionValues.EventCodeManager.SendCodeNextFrame("Button0PressedOnDistractorObject");//SELECTION STUFF (code may not be exact and/or could be moved to Selection handler)
            SessionValues.EventCodeManager.SendCodeNextFrame("IncorrectResponse");
        }

        if (selectedGO != null)
        {
            SelectedStimIndex = selectedSD.StimIndex;
            Debug.Log("SELECTED STIM INDEX: " + SelectedStimIndex);
            SelectedStimLocation = selectedSD.StimLocation;
        }
        Accuracy_InBlock = decimal.Divide(NumCorrect_InBlock, (TrialCount_InBlock + 1));
        UpdateExperimenterDisplaySummaryStrings();
        });

        SearchDisplay.AddTimer(() => selectObjectDuration.value, ITI, () =>
        {
            runningAcc.Add(0);
            
            AbortCode = 6;
            SearchDurations_InBlock.Add(null);
            CurrentTaskLevel.SearchDurations_InTask.Add(null);
            
            SetTrialSummaryString();
            SessionValues.EventCodeManager.SendCodeNextFrame("NoChoice");
        });
        
        // SELECTION FEEDBACK STATE ---------------------------------------------------------------------------------------   
        SelectionFeedback.AddSpecificInitializationMethod(() =>
        {
            SearchDuration = SearchDisplay.TimingInfo.Duration;
            SearchDurations_InBlock.Add(SearchDuration);
            CurrentTaskLevel.SearchDurations_InTask.Add(SearchDuration);

            SetTrialSummaryString();

            int? depth = SessionValues.Using2DStim ? 50 : (int?)null;

            if (CorrectSelection) 
                HaloFBController.ShowPositive(selectedGO, depth);
            else 
                HaloFBController.ShowNegative(selectedGO, depth);
        });

        SelectionFeedback.AddTimer(() => fbDuration.value, TokenFeedback, () =>
        {
            HaloFBController.Destroy();
            choiceMade = false;
        });
       
        // TOKEN FEEDBACK STATE ------------------------------------------------------------------------------------------------
        TokenFeedback.AddSpecificInitializationMethod(() =>
        {
            if (!SessionValues.WebBuild)
                DestroyTextOnExperimenterDisplay();

            if (selectedSD.StimTokenRewardMag > 0)
            {
                TokenFBController.AddTokens(selectedGO, selectedSD.StimTokenRewardMag);
                TotalTokensCollected_InBlock += selectedSD.StimTokenRewardMag;
                CurrentTaskLevel.TotalTokensCollected_InTask += selectedSD.StimTokenRewardMag;
            }
            else
            {
                TokenFBController.RemoveTokens(selectedGO, -selectedSD.StimTokenRewardMag);
                TotalTokensCollected_InBlock += selectedSD.StimTokenRewardMag;
                CurrentTaskLevel.TotalTokensCollected_InTask += selectedSD.StimTokenRewardMag;
            }

            
        });
        TokenFeedback.AddTimer(() => tokenFbDuration, ITI, () =>
        {
            if (TokenFBController.IsTokenBarFull())
            {
                NumTokenBarFull_InBlock++;
                CurrentTaskLevel.NumTokenBarFull_InTask++;

                if (SessionValues.SyncBoxController != null)
                {
                    int NumPulses;
                    if (CurrentTrialDef.ProbablisticNumPulses != null)
                        NumPulses = chooseReward(CurrentTrialDef.ProbablisticNumPulses);
                    else
                        NumPulses = CurrentTrialDef.NumPulses;
                    SessionValues.SyncBoxController.SendRewardPulses(NumPulses, CurrentTrialDef.PulseSize);
                    //SessionInfoPanel.UpdateSessionSummaryValues(("totalRewardPulses",CurrentTrialDef.NumPulses)); moved to syncbox class
                    CurrentTaskLevel.NumRewardPulses_InBlock += NumPulses;
                    CurrentTaskLevel.NumRewardPulses_InTask += NumPulses;
                    RewardGiven = true;
                    TokenFBController.ResetTokenBarFull();
                }
            }
        });
        // ITI STATE ---------------------------------------------------------------------------------------------------
        ITI.AddSpecificInitializationMethod(() =>
        {
            if (currentTaskDef.NeutralITI)
            {
                ContextName = "NeutralITI";
                StartCoroutine(HandleSkybox(GetContextNestedFilePath(!string.IsNullOrEmpty(currentTaskDef.ContextExternalFilePath) ? currentTaskDef.ContextExternalFilePath : SessionValues.SessionDef.ContextExternalFilePath, "NeutralITI")));
                SessionValues.EventCodeManager.SendCodeNextFrame("ContextOff");
            }
        });
        ITI.AddTimer(() => itiDuration.value, FinishTrial, () =>
        {
            UpdateExperimenterDisplaySummaryStrings();
        });

        //---------------------------------ADD FRAME AND TRIAL DATA TO LOG FILES---------------------------------------
        DefineTrialData();
        DefineFrameData();
    }


    public void MakeStimFaceCamera()
    {
        foreach (StimGroup group in TrialStims)
        foreach (var stim in group.stimDefs)
        {
            stim.StimGameObject.transform.LookAt(Camera.main.transform);
        }
    }
    public override void FinishTrialCleanup()
    {
        // Remove the Stimuli, Context, and Token Bar from the Player View and move to neutral ITI State
        if (!SessionValues.WebBuild)
            DestroyTextOnExperimenterDisplay();

        tStim.ToggleVisibility(false);
        
        if (TokenFBController.isActiveAndEnabled)
            TokenFBController.enabled = false;
        
        if (AbortCode == 0)
            CurrentTaskLevel.SetBlockSummaryString();
        else
        {
            CurrentTaskLevel.NumAbortedTrials_InBlock++;
            CurrentTaskLevel.NumAbortedTrials_InTask++;
            CurrentTaskLevel.ClearStrings();
            CurrentTaskLevel.CurrentBlockSummaryString.AppendLine("");
        }

        TokenFBController.ResetTokenBarFull();

    }

    protected override void DefineTrialStims()
    {
        //Define StimGroups consisting of StimDefs whose gameobjects will be loaded at TrialLevel_SetupTrial and 
        //destroyed at TrialLevel_Finish

        StimGroup group = SessionValues.UsingDefaultConfigs ? PrefabStims : ExternalStims;

        tStim = new StimGroup("SearchStimuli", group, CurrentTrialDef.TrialStimIndices);

        if(TokensWithStimOn?? false)
            tStim.SetVisibilityOnOffStates(GetStateFromName("SearchDisplay"), GetStateFromName("ITI"));
        else
            tStim.SetVisibilityOnOffStates(GetStateFromName("SearchDisplay"),GetStateFromName("SelectionFeedback"));
        TrialStims.Add(tStim);

        for (int iStim = 0; iStim < CurrentTrialDef.TrialStimIndices.Length; iStim++)
        {
            FlexLearning_StimDef sd = (FlexLearning_StimDef)tStim.stimDefs[iStim];

            if (CurrentTrialDef.ProbabilisticTrialStimTokenReward != null)
                sd.StimTokenRewardMag = chooseReward(CurrentTrialDef.ProbabilisticTrialStimTokenReward[iStim]);
            else
                sd.StimTokenRewardMag = CurrentTrialDef.TrialStimTokenReward[iStim];


            if (sd.StimTokenRewardMag > 0) 
                sd.IsTarget = true; //CHECK THIS IMPLEMENTATION!!! only works if the target stim has a non-zero, positive reward
            else 
                sd.IsTarget = false;
        }
        
        if (CurrentTrialDef.RandomizedLocations)
        {
            int[] positionIndexArray = Enumerable.Range(0, CurrentTrialDef.TrialStimIndices.Length).ToArray();
            System.Random random = new System.Random();
            positionIndexArray = positionIndexArray.OrderBy(x => random.Next()).ToArray();

            for (int i = 0; i < CurrentTrialDef.TrialStimIndices.Length; i++)
            {
                tStim.stimDefs[i].StimLocation = CurrentTrialDef.TrialStimLocations.ElementAt(positionIndexArray[i]);
            }
        }
        else
        {
            tStim.SetLocations(CurrentTrialDef.TrialStimLocations);
        }
    }
    public override void ResetTrialVariables()
    {
        choiceMade = false;
        selectedGO = null;
        selectedSD = null;
        SelectedStimIndex = null;
        SelectedStimLocation = null;
        SearchDuration = 0;
        CorrectSelection = false;
        RewardGiven = false;
        SessionValues.MouseTracker.ResetClicks();
    }
    private void DefineTrialData()
    {
        // All AddDatum commands for the Trial Data
        TrialData.AddDatum("TrialID", () => CurrentTrialDef.TrialID);
        TrialData.AddDatum("ContextName", () => CurrentTrialDef.ContextName);
        TrialData.AddDatum("SelectedStimIndex", () => selectedSD?.StimIndex ?? null);
        TrialData.AddDatum("SelectedLocation", () => selectedSD?.StimLocation ?? null);
        TrialData.AddDatum("CorrectSelection", () => CorrectSelection ? 1 : 0);
        TrialData.AddDatum("SearchDuration", ()=> SearchDuration);
        TrialData.AddDatum("RewardGiven", ()=> RewardGiven? 1 : 0);
        TrialData.AddDatum("TotalClicks", ()=> SessionValues.MouseTracker.GetClickCount()[0]);
    }
    private void DefineFrameData()
    {
        // All AddDatum commmands from the Frame Data
        FrameData.AddDatum("ContextName", () => ContextName);
        FrameData.AddDatum("StartButtonVisibility", () => StartButton == null ? false:StartButton.activeSelf); // CHECK THE DATA!
        FrameData.AddDatum("TrialStimVisibility", () => tStim == null? false:tStim.IsActive);
    }

    private void CreateTextOnExperimenterDisplay()
    {
        //Create corresponding text on player view of experimenter display
        foreach (FlexLearning_StimDef stim in tStim.stimDefs)
        {
            if (stim.IsTarget)
            {
                textLocation = ScreenToPlayerViewPosition(Camera.main.WorldToScreenPoint(stim.StimLocation), playerViewParent.transform);
                textLocation.y += 50;
                Vector3 textSize = new Vector3(2, 2,1);
                playerViewText = playerView.CreateTextObject("TargetText","TARGET",
                    Color.red, textLocation, textSize, playerViewParent.transform);                
                playerViewText.SetActive(true);
            }
        }
        playerViewLoaded = true;
    }
    private void DestroyTextOnExperimenterDisplay()
    {
        DestroyChildren(playerViewParent);
        playerViewText = null;
        playerViewLoaded = false;
    }
    void LoadConfigUIVariables()
    {
        //config UI variables
        minObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("minObjectTouchDuration");
        maxObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("maxObjectTouchDuration");
        itiDuration = ConfigUiVariables.get<ConfigNumber>("itiDuration");
        searchDisplayDelay = ConfigUiVariables.get<ConfigNumber>("searchDisplayDelay");
        selectObjectDuration = ConfigUiVariables.get<ConfigNumber>("selectObjectDuration");
        fbDuration = ConfigUiVariables.get<ConfigNumber>("fbDuration");
        tokenRevealDuration = ConfigUiVariables.get<ConfigNumber>("tokenRevealDuration");
        tokenUpdateDuration = ConfigUiVariables.get<ConfigNumber>("tokenUpdateDuration");
        tokenFlashingDuration = ConfigUiVariables.get<ConfigNumber>("tokenFlashingDuration");

        tokenFbDuration = (tokenFlashingDuration.value + tokenUpdateDuration.value + tokenRevealDuration.value);//ensures full flashing duration within
        ////configured token fb duration
        configUIVariablesLoaded = true;
    }
    void SetTrialSummaryString()
    {
        TrialSummaryString = "Selected Object Index: " + SelectedStimIndex +
                             "\nSelected Object Location: " + SelectedStimLocation +
                             "\n" +
                             "\nCorrect Selection: " + CorrectSelection +
                             "\n" +
                             "\nSearch Duration: " + SearchDuration +
                             "\n" + 
                             "\nToken Bar Value: " + TokenFBController.GetTokenBarValue();
    }
    protected override bool CheckBlockEnd()
    {
        TaskLevelTemplate_Methods TaskLevel_Methods = new TaskLevelTemplate_Methods();
        return (TaskLevel_Methods.CheckBlockEnd(CurrentTrialDef.BlockEndType, runningAcc,
            CurrentTrialDef.BlockEndThreshold, CurrentTrialDef.BlockEndWindow, CurrentTaskLevel.MinTrials_InBlock,
            CurrentTaskLevel.MaxTrials_InBlock) || TrialCount_InBlock == CurrentTaskLevel.MaxTrials_InBlock);
        
    }

    private void UpdateExperimenterDisplaySummaryStrings()
    {
        if (TrialCount_InTask != 0)
            CurrentTaskLevel.SetTaskSummaryString();
        CurrentTaskLevel.SetBlockSummaryString();
        SetTrialSummaryString();
    }
}