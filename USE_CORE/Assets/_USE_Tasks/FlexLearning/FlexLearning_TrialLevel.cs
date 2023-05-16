using System.Collections.Generic;
using UnityEngine;
using USE_States;
using USE_StimulusManagement;
using FlexLearning_Namespace;
using USE_ExperimentTemplate_Trial;
using USE_UI;
using System.Linq;
using ConfigDynamicUI;
using USE_ExperimentTemplate_Task;

public class FlexLearning_TrialLevel : ControlLevel_Trial_Template
{
    public FlexLearning_TrialDef CurrentTrialDef => GetCurrentTrialDef<FlexLearning_TrialDef>();
    public FlexLearning_TaskLevel CurrentTaskLevel => GetTaskLevel<FlexLearning_TaskLevel>();

    public GameObject FL_CanvasGO;
    public USE_StartButton USE_StartButton;

    // Block End Variables
    public List<int> runningAcc;
    public int? MinTrials, MaxTrials;
    
    // Stimuli Variables
    private StimGroup tStim;
    private GameObject StartButton;

    // ConfigUI Variables
    private bool configUIVariablesLoaded;
    [HideInInspector]
    public ConfigNumber minObjectTouchDuration, itiDuration, 
        fbDuration, gratingSquareDuration, maxObjectTouchDuration, selectObjectDuration, tokenRevealDuration, tokenUpdateDuration, tokenFlashingDuration, 
        searchDisplayDelay;

    private float tokenFbDuration;
    

    // Set in the Task Level
    [HideInInspector] public string ContextExternalFilePath;
    [HideInInspector] public Vector3 StartButtonPosition;
    [HideInInspector] public float StartButtonScale;
    [HideInInspector] public bool StimFacingCamera;
    [HideInInspector] public string ShadowType;
    [HideInInspector] public bool NeutralITI;
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
    [HideInInspector] public List<float> SearchDurationsList = new List<float>();
    [HideInInspector] public int NumErrors_InBlock;
    [HideInInspector] public int NumRewardPulses_InBlock;
    [HideInInspector] public int NumTokenBarFull_InBlock;
    [HideInInspector] public int TotalTokensCollected_InBlock;
    [HideInInspector] public decimal Accuracy_InBlock;
    [HideInInspector] public float AverageSearchDuration_InBlock;
    [HideInInspector] public int AbortedTrials_InBlock;
   
    // Trial Data Variables
    private int? SelectedStimIndex = null;
    private Vector3? SelectedStimLocation = null;
    private float SearchDuration = 0;
    private bool RewardGiven = false;
    private bool TouchDurationError = false;
    private bool aborted = false;

    [HideInInspector] public float TouchFeedbackDuration;

    [HideInInspector] public int PreSearch_TouchFbErrorCount;

    [HideInInspector] public bool MacMainDisplayBuild;
    [HideInInspector] public bool AdjustedPositionsForMac;


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
            LoadTextures(ContextExternalFilePath);
            playerView = new PlayerViewPanel(); //GameObject.Find("PlayerViewCanvas").GetComponent<PlayerViewPanel>()
            playerViewText = new GameObject();
            playerViewParent = GameObject.Find("MainCameraCopy");     
            
            // Initialize FB Controller Values
            HaloFBController.SetHaloSize(5f);
            HaloFBController.SetHaloIntensity(5);

        });
        
        SetupTrial.AddInitializationMethod(() =>
        {
            TokenFBController.ResetTokenBarFull();
            //Set the context for the upcoming trial
            ContextName = CurrentTrialDef.ContextName;

            //Set the Stimuli Light/Shadow settings
            SetShadowType(ShadowType, "FlexLearning_DirectionalLight");
            if (StimFacingCamera)
                MakeStimFaceCamera();
            
            if (StartButton == null)
            {
                USE_StartButton = new USE_StartButton(FL_CanvasGO.GetComponent<Canvas>(), StartButtonPosition, StartButtonScale);
                StartButton = USE_StartButton.StartButtonGO;
                USE_StartButton.SetVisibilityOnOffStates(InitTrial, InitTrial);
            }

            DeactivateChildren(FL_CanvasGO);
            
            if (!configUIVariablesLoaded)
                LoadConfigUIVariables();
            if (!playerViewLoaded)
                CreateTextOnExperimenterDisplay();

            SetTrialSummaryString();
            MaxTrials = CurrentTrialDef.MaxTrials;

        });
        SetupTrial.SpecifyTermination(() => true, InitTrial);

        //INIT TRIAL STATE ----------------------------------------------------------------------------------------------
        var ShotgunHandler = SelectionTracker.SetupSelectionHandler("trial", "TouchShotgun", InitTrial, SearchDisplay);
        ShotgunHandler.shotgunRaycast.SetShotgunVariables(ShotgunRaycastCircleSize_DVA, ParticipantDistance_CM, ShotgunRaycastSpacing_DVA);
        TouchFBController.EnableTouchFeedback(ShotgunHandler, .3f, StartButtonScale, FL_CanvasGO);

        InitTrial.AddInitializationMethod(() =>
        {
            CurrentTaskLevel.SetBlockSummaryString();
            if (TrialCount_InTask != 0)
                CurrentTaskLevel.SetTaskSummaryString();

            if (MacMainDisplayBuild & !UnityEngine.Application.isEditor && !AdjustedPositionsForMac) //adj text positions if running build with mac as main display
            {
                Vector3 biggerScale = TokenFBController.transform.localScale * 2f;
                TokenFBController.transform.localScale = biggerScale;
                TokenFBController.tokenSize = 200;
                TokenFBController.RecalculateTokenBox();
                AdjustedPositionsForMac = true;
            }


            TokenFBController.SetRevealTime(tokenRevealDuration.value);
            TokenFBController.SetUpdateTime(tokenUpdateDuration.value);
            TokenFBController.SetFlashingTime(tokenFlashingDuration.value);

            if (ShotgunHandler.AllSelections.Count > 0)
                ShotgunHandler.ClearSelections();

            ShotgunHandler.MinDuration = minObjectTouchDuration.value;
            ShotgunHandler.MaxDuration = maxObjectTouchDuration.value;
        });
        InitTrial.SpecifyTermination(() => ShotgunHandler.LastSuccessfulSelectionMatches(StartButton),
            SearchDisplayDelay, () => EventCodeManager.SendCodeImmediate(SessionEventCodes["StartButtonSelected"]));

        // Provide delay following start button selection and before stimuli onset
        SearchDisplayDelay.AddTimer(() => searchDisplayDelay.value, SearchDisplay);
        
        // SEARCH DISPLAY STATE ----------------------------------------------------------------------------------------
        SearchDisplay.AddInitializationMethod(() =>
        {
            Input.ResetInputAxes(); //reset input in case they holding down
            TokenFBController.enabled = true;
            ActivateChildren(playerViewParent);
            EventCodeManager.SendCodeNextFrame(SessionEventCodes["StimOn"]);
            EventCodeManager.SendCodeNextFrame(SessionEventCodes["TokenBarVisible"]);

            if (ShotgunHandler.AllSelections.Count > 0)
                ShotgunHandler.ClearSelections();

            PreSearch_TouchFbErrorCount = TouchFBController.ErrorCount;
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
            if (TouchFBController.ErrorCount > PreSearch_TouchFbErrorCount)
                TouchDurationError = true;
            else
                TouchDurationError = false;

            CorrectSelection = selectedSD.IsTarget;
            if (CorrectSelection)
            {       
                NumCorrect_InBlock++;
                CurrentTaskLevel.NumCorrect_InTask++;
                runningAcc.Add(1);
                EventCodeManager.SendCodeNextFrame(SessionEventCodes["Button0PressedOnTargetObject"]);//SELECTION STUFF (code may not be exact and/or could be moved to Selection handler)
                EventCodeManager.SendCodeNextFrame(SessionEventCodes["CorrectResponse"]);
            }
            else
            {
                NumErrors_InBlock++;
                CurrentTaskLevel.NumErrors_InTask++;
                runningAcc.Add(0);
                EventCodeManager.SendCodeNextFrame(SessionEventCodes["Button0PressedOnDistractorObject"]);//SELECTION STUFF (code may not be exact and/or could be moved to Selection handler)
                EventCodeManager.SendCodeNextFrame(SessionEventCodes["IncorrectResponse"]);
            }

            if (selectedGO != null)
            {
                SelectedStimIndex = selectedSD.StimIndex;
                SelectedStimLocation = selectedSD.StimLocation;
            }
            Accuracy_InBlock = decimal.Divide(NumCorrect_InBlock,(TrialCount_InBlock + 1));
            SetTrialSummaryString();
        });

        SearchDisplay.AddTimer(() => selectObjectDuration.value, ITI, () =>
        {
            runningAcc.Add(0);
            AbortedTrials_InBlock++;
            CurrentTaskLevel.AbortedTrials_InTask++;
            AbortCode = 6;
            aborted = true;  
            SetTrialSummaryString();
            EventCodeManager.SendCodeNextFrame(SessionEventCodes["NoChoice"]);
        });
        
        // SELECTION FEEDBACK STATE ---------------------------------------------------------------------------------------   
        SelectionFeedback.AddInitializationMethod(() =>
        {
            SearchDuration = SearchDisplay.TimingInfo.Duration;
            SearchDurationsList.Add(SearchDuration);
            CurrentTaskLevel.SearchDurationsList_InTask.Add(SearchDuration);
            AverageSearchDuration_InBlock = SearchDurationsList.Average();
            SetTrialSummaryString();
            
            if (CorrectSelection) 
                HaloFBController.ShowPositive(selectedGO);
            else 
                HaloFBController.ShowNegative(selectedGO);
        });

        SelectionFeedback.AddTimer(() => fbDuration.value, TokenFeedback, () =>
        {
            HaloFBController.Destroy();
            choiceMade = false;
        });
       
        // TOKEN FEEDBACK STATE ------------------------------------------------------------------------------------------------
        TokenFeedback.AddInitializationMethod(() =>
        {
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
                TotalTokensCollected_InBlock -= selectedSD.StimTokenRewardMag;
                CurrentTaskLevel.TotalTokensCollected_InTask -= selectedSD.StimTokenRewardMag;
            }
        });
        TokenFeedback.AddTimer(() => tokenFbDuration, ITI, () =>
        {
            if (TokenFBController.isTokenBarFull())
            {
                NumTokenBarFull_InBlock++;
                CurrentTaskLevel.NumTokenBarFull_InTask++;

                if (SyncBoxController != null)
                {
                    int NumPulses = chooseReward(CurrentTrialDef.PulseReward);
                    SyncBoxController.SendRewardPulses(NumPulses, CurrentTrialDef.PulseSize);
                    SessionInfoPanel.UpdateSessionSummaryValues(("totalRewardPulses", NumPulses));
                    NumRewardPulses_InBlock += NumPulses;
                    CurrentTaskLevel.NumRewardPulses_InTask += NumPulses;
                    RewardGiven = true;
                }
            }
        });
        // ITI STATE ---------------------------------------------------------------------------------------------------
        ITI.AddInitializationMethod(() =>
        {
            if (NeutralITI)
            {
                ContextName = "itiImage";
                RenderSettings.skybox = CreateSkybox(GetContextNestedFilePath(ContextExternalFilePath, "itiImage"), UseDefaultConfigs);
                EventCodeManager.SendCodeNextFrame(SessionEventCodes["ContextOff"]);
            }
        });
        ITI.AddTimer(() => itiDuration.value, FinishTrial);
        //---------------------------------ADD FRAME AND TRIAL DATA TO LOG FILES---------------------------------------
        AssignTrialData();
        AssignFrameData();
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
        DestroyTextOnExperimenterDisplay();
        tStim.ToggleVisibility(false);
        
        if (TokenFBController.isActiveAndEnabled)
            TokenFBController.enabled = false;
        
        if (AbortCode == 0)
            CurrentTaskLevel.SetBlockSummaryString();

        if (AbortCode == AbortCodeDict["RestartBlock"] || AbortCode == AbortCodeDict["PreviousBlock"] || AbortCode == AbortCodeDict["EndBlock"]) //If used RestartBlock, PreviousBlock, or EndBlock hotkeys
        {
            aborted = true;
            AbortedTrials_InBlock++;
            CurrentTaskLevel.AbortedTrials_InTask++;
            CurrentTaskLevel.ClearStrings();
            CurrentTaskLevel.BlockSummaryString.AppendLine("");
        }
    }

    protected override void DefineTrialStims()
    {
        //Define StimGroups consisting of StimDefs whose gameobjects will be loaded at TrialLevel_SetupTrial and 
        //destroyed at TrialLevel_Finish
        int temp = 0;
        tStim = new StimGroup("SearchStimuli", ExternalStims, CurrentTrialDef.TrialStimIndices);

        if(TokensWithStimOn?? false)
            tStim.SetVisibilityOnOffStates(GetStateFromName("SearchDisplay"), GetStateFromName("ITI"));
        else
            tStim.SetVisibilityOnOffStates(GetStateFromName("SearchDisplay"),GetStateFromName("SelectionFeedback"));
        TrialStims.Add(tStim);
        for (int i = 0; i < CurrentTrialDef.TrialStimIndices.Length; i++)
        {
            FlexLearning_StimDef sd = (FlexLearning_StimDef)tStim.stimDefs[i];
            sd.StimTokenRewardMag = chooseReward(CurrentTrialDef.TrialStimTokenReward[i]);
            if (sd.StimTokenRewardMag > 0) sd.IsTarget = true; //CHECK THIS IMPLEMENTATION!!! only works if the target stim has a non-zero, positive reward
            else sd.IsTarget = false;
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
        TouchDurationError = false;
        aborted = false;
        MouseTracker.ResetClicks();
    }
    private void AssignTrialData()
    {
        // All AddDatum commands for the Trial Data
        TrialData.AddDatum("Context", ()=> CurrentTrialDef.ContextName);
        TrialData.AddDatum("SelectedStimIndex", () => selectedSD?.StimIndex ?? null);
        TrialData.AddDatum("SelectedLocation", () => selectedSD?.StimLocation ?? null);
        TrialData.AddDatum("CorrectSelection", () => CorrectSelection ? 1 : 0);
        TrialData.AddDatum("SearchDuration", ()=> SearchDuration);
        TrialData.AddDatum("RewardGiven", ()=> RewardGiven? 1 : 0);
        TrialData.AddDatum("TotalClicks", ()=> MouseTracker.GetClickCount());
        TrialData.AddDatum("AbortedTrial", ()=> aborted);
    }
    private void AssignFrameData()
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
                textLocation.y += 75;
                Vector3 textSize = new Vector3(2, 2,1);
                playerViewText = playerView.CreateTextObject("TargetText","TARGET",
                    Color.red, textLocation, textSize, playerViewParent.transform);
                
            }
        }
        playerViewLoaded = true;
        DeactivateChildren(playerViewParent);
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
        gratingSquareDuration = ConfigUiVariables.get<ConfigNumber>("gratingSquareDuration");
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
                             "\nTouch Duration Error: " + TouchDurationError +
                             "\n" +
                             "\nSearch Duration: " + SearchDuration +
                             "\n" + 
                             "\nToken Bar Value: " + TokenFBController.GetTokenBarValue();
    }
    protected override bool CheckBlockEnd()
    {
        TaskLevelTemplate_Methods TaskLevel_Methods = new TaskLevelTemplate_Methods();
        return (TaskLevel_Methods.CheckBlockEnd(CurrentTrialDef.BlockEndType, runningAcc,
            CurrentTrialDef.BlockEndThreshold, CurrentTrialDef.BlockEndWindow, MinTrials,
            MaxTrials) || TrialCount_InBlock == MaxTrials);
        
    }
}