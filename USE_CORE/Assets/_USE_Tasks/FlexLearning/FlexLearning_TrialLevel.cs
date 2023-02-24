using System.Collections.Generic;
using UnityEngine;
using USE_States;
using USE_StimulusManagement;
using FlexLearning_Namespace;
using USE_ExperimentTemplate_Trial;
using Random = UnityEngine.Random;
using USE_UI;
using USE_Settings;
using System.IO;
using System.Linq;
using ConfigDynamicUI;
using UnityEngine.UI;
using USE_ExperimentTemplate_Task;
using System;
using ConfigParsing;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class FlexLearning_TrialLevel : ControlLevel_Trial_Template
{
    public FlexLearning_TrialDef CurrentTrialDef => GetCurrentTrialDef<FlexLearning_TrialDef>();
    public FlexLearning_TaskLevel CurrentTaskLevel => GetTaskLevel<FlexLearning_TaskLevel>();

    public GameObject FL_CanvasGO;
    public USE_StartButton USE_StartButton;
    public USE_StartButton USE_FBSquare;

    // Block End Variables
    public List<int> runningAcc;
    public int MinTrials, MaxTrials;
    
    // Stimuli Variables
    private StimGroup tStim;
    private GameObject StartButton;
    private GameObject FBSquare;

    // ConfigUI Variables
    private bool configUIVariablesLoaded;
    [HideInInspector]
    public ConfigNumber minObjectTouchDuration, itiDuration, 
        fbDuration, gratingSquareDuration, maxObjectTouchDuration, selectObjectDuration, tokenRevealDuration, tokenUpdateDuration, tokenFlashingDuration, 
        searchDisplayDelay;

    private float tokenFbDuration;
    
    // Set in the Task Level
    [HideInInspector] public string ContextExternalFilePath;
    [HideInInspector] public Vector3 ButtonPosition, ButtonScale;
    [HideInInspector] public bool StimFacingCamera;
    [HideInInspector] public string ShadowType;
    [HideInInspector] public bool NeutralITI;
    [HideInInspector] public bool? TokensWithStimOn;
    
    // Stim Evaluation Variables
    private GameObject trialStim;
    private GameObject selected = null;
    private bool CorrectSelection;
    FlexLearning_StimDef selectedSD = null;
    private bool ObjectsCreated = false;
    
    
    
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
    [HideInInspector] public int TouchDurationError_InBlock;
    [HideInInspector] public int AbortedTrials_InBlock;
   
    // Trial Data Variables
    private int? SelectedStimIndex = null;
    private string selectedStimName = null;
    private Vector3? SelectedStimLocation = null;
    private float SearchDuration = 0;
    private bool RewardGiven = false;
    private bool TouchDurationError = false;
    private bool aborted = false;
    
    public override void DefineControlLevel()
    {
        State InitTrial = new State("InitTrial");
        State SearchDisplay = new State("SearchDisplay");
        State SearchDisplayDelay = new State("SearchDisplayDelay");
        State SelectionFeedback = new State("SelectionFeedback");
        State TokenFeedback = new State("TokenFeedback");
        State ITI = new State("ITI");
        State Delay = new State("Delay");

        AddActiveStates(new List<State> { InitTrial, SearchDisplay, SelectionFeedback, TokenFeedback, ITI, Delay, SearchDisplayDelay });
        SelectionHandler<FlexLearning_StimDef> mouseHandler = new SelectionHandler<FlexLearning_StimDef>();

        // A state that just waits for some time
        State stateAfterDelay = null;
        float delayDuration = 0;
        Delay.AddTimer(() => delayDuration, () => stateAfterDelay);
        
        Add_ControlLevel_InitializationMethod(() =>
        {
            LoadTextures(ContextExternalFilePath);
            Text commandText = null;
            playerView = new PlayerViewPanel(); //GameObject.Find("PlayerViewCanvas").GetComponent<PlayerViewPanel>()
            playerViewText = new GameObject();
            playerViewParent = GameObject.Find("MainCameraCopy");     
            
            // Initialize FB Controller Values
            HaloFBController.SetHaloSize(5f);
            HaloFBController.SetHaloIntensity(5);
        });
        
        SetupTrial.AddInitializationMethod(() =>
        {
            ResetTrialVariables();
            TokenFBController.ResetTokenBarFull();
            //Set the context for the upcoming trial
            ContextName = CurrentTrialDef.ContextName;
            RenderSettings.skybox =
                CreateSkybox(ContextExternalFilePath + Path.DirectorySeparatorChar + ContextName + ".png");

            //Set the Stimuli Light/Shadow settings
            SetShadowType(ShadowType, "FlexLearning_DirectionalLight");
            if (StimFacingCamera)
            {
                foreach (var stim in tStim.stimDefs) stim.StimGameObject.AddComponent<FaceCamera>();
            }

            if (StartButton == null)
            {
                USE_StartButton = new USE_StartButton(FL_CanvasGO.GetComponent<Canvas>());
                StartButton = USE_StartButton.StartButtonGO;
                USE_StartButton.SetButtonSize(80);
                USE_StartButton.SetButtonPosition(new Vector3(0, 0, -400));
            }
            if (FBSquare == null)
            {
                USE_FBSquare = new USE_StartButton(FL_CanvasGO.GetComponent<Canvas>());
                FBSquare = USE_FBSquare.StartButtonGO;
                FBSquare.name = "FBSquare";
            }

            if (!configUIVariablesLoaded)
                LoadConfigUIVariables();
            if (!playerViewLoaded)
                CreateTextOnExperimenterDisplay();

            SetTrialSummaryString();
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["TrlStart"]);
        });
        SetupTrial.SpecifyTermination(() => true, InitTrial);
        
        //INIT TRIAL STATE ----------------------------------------------------------------------------------------------
        MouseTracker.AddSelectionHandler(mouseHandler, InitTrial);
        InitTrial.AddInitializationMethod(() =>
        {
            CurrentTaskLevel.SetBlockSummaryString();
            CurrentTaskLevel.SetTaskSummaryString();
            
            //Initialize FB Controller Variables
            mouseHandler.SetMinTouchDuration(minObjectTouchDuration.value);
            mouseHandler.SetMaxTouchDuration(maxObjectTouchDuration.value);
            TokenFBController.SetRevealTime(tokenRevealDuration.value);
            TokenFBController.SetUpdateTime(tokenUpdateDuration.value);
            TokenFBController.SetFlashingTime(tokenFlashingDuration.value);

            StartButton.SetActive(true);
        });
        InitTrial.AddUpdateMethod(() =>
        {
            if (mouseHandler.GetHeldTooLong() || mouseHandler.GetHeldTooShort())
            {
                TouchDurationError = true;
                TouchDurationErrorFeedback(mouseHandler, false);
                SetTrialSummaryString();
                CurrentTaskLevel.SetBlockSummaryString();
            }
        });
        InitTrial.SpecifyTermination(() => mouseHandler.SelectionMatches(StartButton),
            SearchDisplayDelay, () =>
            {
                // Turn off start button
                StartButton.SetActive(false);
                EventCodeManager.SendCodeImmediate(TaskEventCodes["StartButtonSelected"]);
            });

        // Provide delay following start button selection and before stimuli onset
        SearchDisplayDelay.AddTimer(() => searchDisplayDelay.value, SearchDisplay);
        
        // SEARCH DISPLAY STATE ----------------------------------------------------------------------------------------
        MouseTracker.AddSelectionHandler(mouseHandler, SearchDisplay);
        SearchDisplay.AddInitializationMethod(() =>
        {
            Input.ResetInputAxes(); //reset input in case they holding down
            // Toggle TokenBar and Stim to be visible
            TokenFBController.enabled = true;
            ActivateChildren(playerViewParent);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["StimOn"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["TokenBarVisible"]);
        });
        SearchDisplay.AddUpdateMethod(() =>
        {
            if (mouseHandler.GetHeldTooLong() || mouseHandler.GetHeldTooShort())
            {
                TouchDurationError = true;
                SetTrialSummaryString();
                TouchDurationErrorFeedback(mouseHandler, false);
                CurrentTaskLevel.SetBlockSummaryString();
            }
        });
        SearchDisplay.SpecifyTermination(() => mouseHandler.SelectedStimDef != null, SelectionFeedback, () => {
            selected = mouseHandler.SelectedGameObject;
            selectedSD = mouseHandler.SelectedStimDef;
            CorrectSelection = selectedSD.IsTarget;
            if (CorrectSelection)
            {       
                NumCorrect_InBlock++;
                CurrentTaskLevel.NumCorrect_InTask++;
                runningAcc.Add(1);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["TouchTargetStart"]);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["CorrectResponse"]);
            }
            else
            {
                NumErrors_InBlock++;
                CurrentTaskLevel.NumErrors_InTask++;
                runningAcc.Add(0);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["TouchDistractorStart"]);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["IncorrectResponse"]);
            }

            if (selected != null)
            {
                SelectedStimIndex = selectedSD.StimIndex;
                SelectedStimLocation = selectedSD.StimLocation;
            }
            Accuracy_InBlock = decimal.Divide(NumCorrect_InBlock,(TrialCount_InBlock + 1));
        });

        SearchDisplay.AddTimer(() => selectObjectDuration.value, ITI, () =>
        {
            if (mouseHandler.SelectedStimDef == null)   //means the player got timed out and didn't click on anything
            {
                runningAcc.Add(0);
                Debug.Log("Timed out of selection state before making a choice");
                AbortedTrials_InBlock++;
                CurrentTaskLevel.AbortedTrials_InTask++;
                aborted = true;  
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["NoChoice"]);
            }
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
                HaloFBController.ShowPositive(selected);
            else 
                HaloFBController.ShowNegative(selected);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionVisualFbOn"]);
        });

        SelectionFeedback.AddTimer(() => fbDuration.value, TokenFeedback, () =>
        {
            HaloFBController.Destroy();
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionVisualFbOff"]);
        });
       
        // TOKEN FEEDBACK STATE ------------------------------------------------------------------------------------------------
        TokenFeedback.AddInitializationMethod(() =>
        {
            DestroyTextOnExperimenterDisplay();
            if (selectedSD.StimTrialRewardMag > 0)
            {
                TokenFBController.AddTokens(selected, selectedSD.StimTrialRewardMag);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionAuditoryFbOn"]);
            }
            else
            {
                TokenFBController.RemoveTokens(selected, -selectedSD.StimTrialRewardMag);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionAuditoryFbOn"]);
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
                    SyncBoxController.SendRewardPulses(CurrentTrialDef.NumPulses, CurrentTrialDef.PulseSize);
                    EventCodeManager.SendCodeImmediate(TaskEventCodes["Fluid1Onset"]);
                    SessionInfoPanel.UpdateSessionSummaryValues(("totalRewardPulses",CurrentTrialDef.NumPulses));
                    NumRewardPulses_InBlock += CurrentTrialDef.NumPulses;
                    CurrentTaskLevel.NumRewardPulses_InTask += CurrentTrialDef.NumPulses;
                    RewardGiven = true;
                }
            }
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["TrlEnd"]);
            TotalTokensCollected_InBlock = TokenFBController.GetTokenBarValue() +
                                           (NumTokenBarFull_InBlock* CurrentTrialDef.NumTokenBar);
            CurrentTaskLevel.TotalTokensCollected_InTask = TokenFBController.GetTokenBarValue() +
                                                           (CurrentTaskLevel.NumTokenBarFull_InTask * CurrentTrialDef.NumTokenBar);
        });
        // ITI STATE ---------------------------------------------------------------------------------------------------
        ITI.AddInitializationMethod(() =>
        {
            if (NeutralITI)
            {
                ContextName = "itiImage";
                RenderSettings.skybox = CreateSkybox(ContextExternalFilePath + Path.DirectorySeparatorChar + ContextName + ".png");
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["ContextOff"]);
            }
        });
        ITI.AddTimer(() => itiDuration.value, FinishTrial);
        //---------------------------------ADD FRAME AND TRIAL DATA TO LOG FILES---------------------------------------
        AssignTrialData();
        AssignFrameData();
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

        if (AbortCode == AbortCodeDict["RestartBlock"] || AbortCode == AbortCodeDict["PreviousBlock"])
        {
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
            sd.StimTrialRewardMag = ChooseTokenReward(CurrentTrialDef.TrialStimTokenReward[i]);
            if (sd.StimTrialRewardMag > 0) sd.IsTarget = true; //CHECK THIS IMPLEMENTATION!!! only works if the target stim has a non-zero, positive reward
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
    private void ResetTrialVariables()
    {
        SelectedStimIndex = null;
        SelectedStimLocation = null;
        SearchDuration = 0;
        CorrectSelection = false;
        RewardGiven = false;
        TouchDurationError = false;
        MouseTracker.ResetClickCount();
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
        FrameData.AddDatum("FBSquareVisibility", () => FBSquare == null ? false:FBSquare.activeSelf); // CHECK THE DATA!
        FrameData.AddDatum("TrialStimVisibility", () => tStim == null? false:tStim.IsActive);
    }

    private void CreateTextOnExperimenterDisplay()
    {
        //Create corresponding text on player view of experimenter display
        foreach (FlexLearning_StimDef stim in tStim.stimDefs)
        {
            if (stim.IsTarget)
            {
                textLocation = playerViewPosition(Camera.main.WorldToScreenPoint(stim.StimLocation), playerViewParent.transform);
                textLocation.y += 50;
                Vector3 textSize = new Vector3(2, 2,1);
                playerViewText = playerView.WriteText("TargetText","TARGET",
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
        TrialSummaryString = "<b>Task Name: " + CurrentTaskLevel.TaskName+ "</b>" + 
                             "\n<b>Trial Count in Task: </b>" + (TrialCount_InTask + 1) +
                             "\n" +
                             "\nSelected Object Index: " + SelectedStimIndex +
                             "\nSelected Object Location: " + SelectedStimLocation +
                             "\n" +
                             "\nCorrect Selection?: " + CorrectSelection +
                             "\nTouch Duration Error?: " + TouchDurationError +
                             "\nAborted Trial? : " + aborted +
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
            TrialDefs.Count) || TrialCount_InBlock == MaxTrials);
        
    }
    private void TouchDurationErrorFeedback(SelectionHandler<FlexLearning_StimDef> MouseHandler, bool deactivateAfter)
    {
        AudioFBController.Play("Negative");
        if (MouseHandler.GetHeldTooShort())
        {
            if (StartButton.activeInHierarchy)
                StartCoroutine(USE_StartButton.GratedStartButtonFlash(HeldTooShortTexture, gratingSquareDuration.value, deactivateAfter));
            else if (FBSquare.activeInHierarchy)
                StartCoroutine(USE_FBSquare.GratedStartButtonFlash(HeldTooShortTexture, gratingSquareDuration.value, deactivateAfter));
        }
        else if (MouseHandler.GetHeldTooLong())
        {
            if (StartButton.activeInHierarchy)
                StartCoroutine(USE_FBSquare.GratedStartButtonFlash(HeldTooLongTexture, gratingSquareDuration.value, deactivateAfter));
            else if (FBSquare.activeInHierarchy)
                StartCoroutine(USE_FBSquare.GratedStartButtonFlash(HeldTooShortTexture, gratingSquareDuration.value, deactivateAfter));
        }
        MouseHandler.SetHeldTooLong(false);
        MouseHandler.SetHeldTooShort(false);
        TouchDurationError = false;
        TouchDurationError_InBlock++;
    }
}