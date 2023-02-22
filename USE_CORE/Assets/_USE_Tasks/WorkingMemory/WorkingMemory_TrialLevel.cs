using System;
using ConfigDynamicUI;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EffortControl_Namespace;
using UnityEngine;
using UnityEngine.UI;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Trial;
using USE_Settings;
using USE_States;
using USE_StimulusManagement;
using WorkingMemory_Namespace;
using USE_UI;

public class WorkingMemory_TrialLevel : ControlLevel_Trial_Template
{
    public GameObject WM_CanvasGO;
    public USE_StartButton USE_StartButton;

    public WorkingMemory_TrialDef CurrentTrialDef => GetCurrentTrialDef<WorkingMemory_TrialDef>();
    public WorkingMemory_TaskLevel CurrentTaskLevel => GetTaskLevel<WorkingMemory_TaskLevel>();
    // Block End Variables
    public List<int> runningAcc;
    public int MinTrials, MaxTrials;
       
    // Stim Evaluation Variables
    private GameObject trialStim;
    private GameObject selected = null;
    private bool CorrectSelection;
    WorkingMemory_StimDef selectedSD = null;
    
    // Stimuli Variables
    private StimGroup searchStims, sampleStim, postSampleDistractorStims;
    private GameObject StartButton;
    private GameObject SquareGO;
    // public Texture2D HeldTooShortTexture;
    // public Texture2D HeldTooLongTexture;
    // private Texture2D StartButtonTexture;
    private bool Grating = false;

    
    //configUI variables
    [HideInInspector]
    public ConfigNumber minObjectTouchDuration, maxObjectTouchDuration, gratingSquareDuration, tokenRevealDuration, tokenUpdateDuration, trialEndDuration, initTrialDuration, baselineDuration, 
        selectObjectDuration, selectionFbDuration, displaySampleDuration, postSampleDelayDuration, 
        displayPostSampleDistractorsDuration, preTargetDelayDuration, itiDuration, tokenFbDuration;
    
    // Config Loading Variables
    private bool configUIVariablesLoaded = false;
    public string ContextExternalFilePath;
    public Vector3 ButtonPosition, ButtonScale;
    public bool StimFacingCamera;
    public string ShadowType;
    public bool NeutralITI;
    
    //Player View Variables
    private PlayerViewPanel playerView;
    private Transform playerViewParent; // Helps set things onto the player view in the experimenter display
    public List<GameObject> playerViewTextList = new List<GameObject>();
    public GameObject playerViewText;
    private Vector2 textLocation;
    private bool playerViewLoaded;
    
    // Block Data Variables
    private string ContextName = "";
    public int NumCorrect_InBlock;
    public List<float> SearchDurationsList = new List<float>();
    public int NumErrors_InBlock;
    public int NumRewardGiven_InBlock;
    public int NumTokenBarFull_InBlock;
    public int TotalTokensCollected_InBlock;
    public float Accuracy_InBlock;
    public float AverageSearchDuration_InBlock;
    public int TouchDurationError_InBlock;
   
    // Trial Data Variables
    private int? SelectedStimCode = null;
    private string selectedStimName = null;
    private Vector3? SelectedStimLocation = null;
    private float SearchDuration = 0;
    private bool RewardGiven = false;
    private bool TouchDurationError = false;


    public override void DefineControlLevel()
    {
        State InitTrial = new State("InitTrial");
        State DisplaySample = new State("DisplaySample");
        State DisplayDistractors = new State("DisplayDistractors");
        State SearchDisplay = new State("SearchDisplay");
        State SelectionFeedback = new State("SelectionFeedback");
        State TokenFeedback = new State("TokenFeedback");
        State ITI = new State("ITI");
        State Delay = new State("Delay");

        AddActiveStates(new List<State> { InitTrial, Delay, DisplaySample, DisplayDistractors, SearchDisplay, SelectionFeedback, TokenFeedback, ITI });

        // A state that just waits for some time
        State stateAfterDelay = null;
        float delayDuration = 0;
        Delay.AddTimer(() => delayDuration, () => stateAfterDelay);
        
        Text commandText = null;
        playerView = new PlayerViewPanel(); //GameObject.Find("PlayerViewCanvas").GetComponent<PlayerViewPanel>()
        playerViewText = new GameObject();
        SelectionHandler<WorkingMemory_StimDef> mouseHandler = new SelectionHandler<WorkingMemory_StimDef>();

        Add_ControlLevel_InitializationMethod(() =>
        {
            LoadTextures(ContextExternalFilePath);
            HaloFBController.SetHaloSize(5);
            if (StartButton == null)
            {
                USE_StartButton = new USE_StartButton(WM_CanvasGO.GetComponent<Canvas>());
                StartButton = USE_StartButton.StartButtonGO;
            }
        });
        SetupTrial.AddInitializationMethod(() =>
        {
            ContextName = CurrentTrialDef.ContextName;
            RenderSettings.skybox = CreateSkybox(ContextExternalFilePath + Path.DirectorySeparatorChar + ContextName + ".png");
            if (!configUIVariablesLoaded) LoadConfigUIVariables();
            SetTrialSummaryString();
            CurrentTaskLevel.SetBlockSummaryString();
            TokenFBController.SetTokenBarFull(false);
            SetShadowType(ShadowType, "WorkingMemory_DirectionalLight");
            if (StimFacingCamera)
            {
                foreach (var stim in searchStims.stimDefs) stim.StimGameObject.AddComponent<FaceCamera>();
                foreach (var stim in sampleStim.stimDefs) stim.StimGameObject.AddComponent<FaceCamera>();
                foreach (var stim in postSampleDistractorStims.stimDefs) stim.StimGameObject.AddComponent<FaceCamera>();
            }
        });

        SetupTrial.SpecifyTermination(() => true, InitTrial);
        MouseTracker.AddSelectionHandler(mouseHandler, InitTrial);
        InitTrial.AddInitializationMethod(() =>
        {
            StartButton.SetActive(true);
            mouseHandler.SetMinTouchDuration(minObjectTouchDuration.value);
            mouseHandler.SetMaxTouchDuration(maxObjectTouchDuration.value);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["TrlStart"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["ContextOn"]);
        });
        InitTrial.AddUpdateMethod(() =>
        {
            if (mouseHandler.GetHeldTooLong() || mouseHandler.GetHeldTooShort())
            {
                TouchDurationError = true;
                SetTrialSummaryString();
                TouchDurationErrorFeedback(mouseHandler, false);
                CurrentTaskLevel.SetBlockSummaryString();
            }
        });
        InitTrial.SpecifyTermination(() => mouseHandler.SelectionMatches(StartButton),
            DisplaySample, () => {
                // Turn off start button and set the token bar settings
                StartButton.SetActive(false);
                TokenFBController.enabled = true;
                TokenFBController
                    .SetRevealTime(tokenRevealDuration.value)
                    .SetUpdateTime(tokenUpdateDuration.value);
                TotalTokensCollected_InBlock = TokenFBController.GetTokenBarValue() +
                                               (NumTokenBarFull_InBlock * CurrentTrialDef.NumTokenBar);
                EventCodeManager.SendCodeImmediate(TaskEventCodes["StartButtonSelected"]);
                
                // Set Experimenter Display Data Summary Strings
                CurrentTaskLevel.SetBlockSummaryString();
                SetTrialSummaryString();
            });
        
        // Show the target/sample by itself for some time
        DisplaySample.AddTimer(() => displaySampleDuration.value, Delay, () =>
          {
              stateAfterDelay = DisplayDistractors;
              delayDuration = postSampleDelayDuration.value;
          });
        // Show some distractors without the target/sample
        DisplayDistractors.AddTimer(() => displayPostSampleDistractorsDuration.value, Delay, () =>
          {
              stateAfterDelay = SearchDisplay;
              delayDuration = preTargetDelayDuration.value;
              
          });

        // Show the target/sample with some other distractors
        // Wait for a click and provide feedback accordingly
        MouseTracker.AddSelectionHandler(mouseHandler, SearchDisplay);
        SearchDisplay.AddInitializationMethod(() =>
        {
            CreateTextOnExperimenterDisplay();
            searchStims.ToggleVisibility(true);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["StimOn"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["TokenBarVisible"]);        
        });
        SearchDisplay.AddUpdateMethod(() =>
        {
            if (mouseHandler.GetHeldTooLong() || mouseHandler.GetHeldTooShort())
            {
                TouchDurationError = true;
                SetTrialSummaryString();
                TouchDurationErrorFeedback(mouseHandler, true);
                CurrentTaskLevel.SetBlockSummaryString();
            }
        });
        SearchDisplay.SpecifyTermination(() => mouseHandler.SelectedStimDef != null, SelectionFeedback, () =>
        {
            selected = mouseHandler.SelectedGameObject; 
            selectedSD = mouseHandler.SelectedStimDef;
            CorrectSelection = selectedSD.IsTarget;
            if (CorrectSelection)
            {       
                NumCorrect_InBlock++;
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["TouchTargetStart"]);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["CorrectResponse"]);
            }
            else
            {
                NumErrors_InBlock++;
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["TouchDistractorStart"]);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["IncorrectResponse"]);
            }

            if (selected != null)
            {
                SelectedStimCode = selectedSD.StimCode;
                SelectedStimLocation = selectedSD.StimLocation;
            }
            SetTrialSummaryString();
            Accuracy_InBlock = NumCorrect_InBlock/(TrialCount_InBlock + 1);
        });
        SearchDisplay.AddTimer(() => selectObjectDuration.value, ITI, () =>
        {
            if (mouseHandler.SelectedStimDef == null)   //means the player got timed out and didn't click on anything
            {
                Debug.Log("Timed out of selection state before making a choice");
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["NoChoice"]);
            }
        });

        SelectionFeedback.AddInitializationMethod(() =>
        {
            SearchDuration = SearchDisplay.TimingInfo.Duration;
            SearchDurationsList.Add(SearchDuration);
            AverageSearchDuration_InBlock = SearchDurationsList.Average();
            SetTrialSummaryString();
            if (!selected) return;
            else EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionVisualFbOn"]);
            
            if (CorrectSelection) HaloFBController.ShowPositive(selected);
            else HaloFBController.ShowNegative(selected);
        });
        SelectionFeedback.AddTimer(() => selectionFbDuration.value, TokenFeedback, () => 
        {
            HaloFBController.Destroy();
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["StimOff"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionVisualFbOff"]);
        });


        // The state that will handle the token feedback and wait for any animations
        TokenFeedback.AddInitializationMethod(() =>
        {
            searchStims.ToggleVisibility(false);
            if (selectedSD.IsTarget)
            {
                AudioFBController.Play("Positive");
                TokenFBController.AddTokens(selected, selectedSD.StimTrialRewardMag);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionAuditoryFbOn"]);
                
            }
            else
            {
                AudioFBController.Play("Negative");
                TokenFBController.RemoveTokens(selected, -selectedSD.StimTrialRewardMag);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionAuditoryFbOn"]);
            }
        });
        TokenFeedback.SpecifyTermination(() => !TokenFBController.IsAnimating(), Delay, () =>
        {
            if (TokenFBController.isTokenBarFull())
            {
                NumTokenBarFull_InBlock++;
                if (SyncBoxController != null)
                {
                    SyncBoxController.SendRewardPulses(CurrentTrialDef.NumPulses, CurrentTrialDef.PulseSize);
                    EventCodeManager.SendCodeImmediate(TaskEventCodes["Fluid1Onset"]);
                    NumRewardGiven_InBlock++;
                    RewardGiven = true;
                }
            }
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["TrlEnd"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["ContextOff"]);
            TotalTokensCollected_InBlock = TokenFBController.GetTokenBarValue() +
                                           (NumTokenBarFull_InBlock * CurrentTrialDef.NumTokenBar);
            SetTrialSummaryString();
            delayDuration = tokenFbDuration.value;
            stateAfterDelay = ITI;
        });
        ITI.AddInitializationMethod(() =>
        {
            if (NeutralITI)
            {
                ContextName = "itiImage";
                RenderSettings.skybox = CreateSkybox(ContextExternalFilePath + Path.DirectorySeparatorChar + ContextName + ".png");
            }
        });
        // Wait for some time at the end
        ITI.AddTimer(() => itiDuration.value, FinishTrial);

        //---------------------------------ADD FRAME AND TRIAL DATA TO LOG FILES---------------------------------------
        AssignFrameData();
        AssignTrialData();
    }

    public override void FinishTrialCleanup()
    {
        // Remove the Stimuli, Context, and Token Bar from the Player View and move to neutral ITI State
        DestroyTextOnExperimenterDisplay();
        ResetDataTrackingVariables();
        TokenFBController.enabled = false;
        searchStims.ToggleVisibility(false);
        sampleStim.ToggleVisibility(false);
        postSampleDistractorStims.ToggleVisibility(false);
        if (AbortCode == 0)
            CurrentTaskLevel.SetBlockSummaryString();

        if (AbortCode == AbortCodeDict["RestartBlock"] || AbortCode == AbortCodeDict["PreviousBlock"])
        {
            CurrentTaskLevel.BlockSummaryString.Clear();
            CurrentTaskLevel.BlockSummaryString.AppendLine("");
        }
    }

    public void ResetBlockVariables()
    {
        SearchDurationsList.Clear();
        AverageSearchDuration_InBlock = 0;
        NumErrors_InBlock = 0;
        NumCorrect_InBlock = 0;
        NumRewardGiven_InBlock = 0;
        NumTokenBarFull_InBlock = 0;
        TouchDurationError_InBlock = 0;
        Accuracy_InBlock = 0;
        TotalTokensCollected_InBlock = 0;
    }

    protected override void DefineTrialStims()
    {
        //Define StimGroups consisting of StimDefs whose gameobjects will be loaded at TrialLevel_SetupTrial and 
        //destroyed at TrialLevel_Finish

        searchStims = new StimGroup("SearchStims", ExternalStims, CurrentTrialDef.SearchStimIndices);
        //searchStims.SetVisibilityOnOffStates(GetStateFromName("SearchDisplay"), GetStateFromName("TokenFeedback"));
        searchStims.SetLocations(CurrentTrialDef.SearchStimLocations);

        List<StimDef> rewardedStimdefs = new List<StimDef>();

        sampleStim = new StimGroup("TargetStim", GetStateFromName("DisplaySample"), GetStateFromName("DisplaySample"));
        for (int iStim = 0; iStim < CurrentTrialDef.SearchStimIndices.Length; iStim++)
        {
            WorkingMemory_StimDef sd = (WorkingMemory_StimDef)searchStims.stimDefs[iStim];
            sd.StimTrialRewardMag = ChooseTokenReward(CurrentTrialDef.SearchStimTokenReward[iStim]);
            if (sd.StimTrialRewardMag > 0)
            {
                WorkingMemory_StimDef newTarg = sd.CopyStimDef<WorkingMemory_StimDef>() as WorkingMemory_StimDef;
                sampleStim.AddStims(newTarg);
                newTarg.IsTarget = true;//Holds true if the target stim receives non-zero reward
                sd.IsTarget = true; //sets the isTarget value to true in the SearchStim Group
            } 
            else sd.IsTarget = false;
        }
        
        // for (int iT)
        sampleStim.SetLocations(CurrentTrialDef.TargetSampleLocation);
        sampleStim.SetVisibilityOnOffStates(GetStateFromName("DisplaySample"), GetStateFromName("DisplaySample"));
        TrialStims.Add(searchStims);
        TrialStims.Add(sampleStim);

        postSampleDistractorStims = new StimGroup("DisplayDistractors", ExternalStims, CurrentTrialDef.PostSampleDistractorIndices);
        postSampleDistractorStims.SetVisibilityOnOffStates(GetStateFromName("DisplayDistractors"), GetStateFromName("DisplayDistractors"));
        postSampleDistractorStims.SetLocations(CurrentTrialDef.PostSampleDistractorLocations);
        TrialStims.Add(postSampleDistractorStims);
        
    }
    protected override bool CheckBlockEnd()
    {
        TaskLevelTemplate_Methods TaskLevel_Methods = new TaskLevelTemplate_Methods();
        return TaskLevel_Methods.CheckBlockEnd(CurrentTrialDef.BlockEndType, runningAcc,
            CurrentTrialDef.BlockEndThreshold, CurrentTrialDef.BlockEndWindow, MinTrials,
            TrialDefs.Count);
    }

    public void LoadConfigUIVariables()
    {   
        //config UI variables
        minObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("minObjectTouchDuration");
        maxObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("maxObjectTouchDuration"); 
        tokenRevealDuration = ConfigUiVariables.get<ConfigNumber>("tokenRevealDuration");
        tokenUpdateDuration = ConfigUiVariables.get<ConfigNumber>("tokenUpdateDuration"); 
        trialEndDuration = ConfigUiVariables.get<ConfigNumber>("trialEndDuration"); 
        initTrialDuration = ConfigUiVariables.get<ConfigNumber>("initTrialDuration");
        baselineDuration = ConfigUiVariables.get<ConfigNumber>("baselineDuration"); 
        selectObjectDuration = ConfigUiVariables.get<ConfigNumber>("selectObjectDuration");
        selectionFbDuration = ConfigUiVariables.get<ConfigNumber>("selectionFbDuration");
        displaySampleDuration = ConfigUiVariables.get<ConfigNumber>("displaySampleDuration");
        postSampleDelayDuration = ConfigUiVariables.get<ConfigNumber>("postSampleDelayDuration");
        displayPostSampleDistractorsDuration = ConfigUiVariables.get<ConfigNumber>("displayPostSampleDistractorsDuration");
        preTargetDelayDuration = ConfigUiVariables.get<ConfigNumber>("preTargetDelayDuration");
        itiDuration = ConfigUiVariables.get<ConfigNumber>("itiDuration");
        gratingSquareDuration = ConfigUiVariables.get<ConfigNumber>("gratingSquareDuration");
        tokenFbDuration = ConfigUiVariables.get<ConfigNumber>("tokenFbDuration");
        configUIVariablesLoaded = true;
    }
    private void ResetDataTrackingVariables()
    {
        SelectedStimCode = null;
        SelectedStimLocation = null;
        SearchDuration = 0;
        CorrectSelection = false;
        RewardGiven = false;
        TouchDurationError = false;
    }
    private void AssignTrialData()
    {
        // All AddDatum commands for the Trial Data
        TrialData.AddDatum("Context", ()=> CurrentTrialDef.ContextName);
        TrialData.AddDatum("SelectedStimCode", () => selectedSD?.StimCode ?? null);
        TrialData.AddDatum("SelectedLocation", () => selectedSD?.StimLocation ?? null);
        TrialData.AddDatum("CorrectSelection", () => CorrectSelection ? 1 : 0);
        TrialData.AddDatum("SearchDuration", ()=> SearchDuration);
        TrialData.AddDatum("RewardGiven", ()=> RewardGiven);
    }
    private void AssignFrameData()
    {
        // All AddDatum commmands from the Frame Data
        FrameData.AddDatum("ContextName", () => ContextName);
        FrameData.AddDatum("StartButtonVisibility", () => StartButton.activeSelf);
        FrameData.AddDatum("DistractorStimVisibility", () => postSampleDistractorStims.IsActive);
        FrameData.AddDatum("SearchStimVisibility", ()=> searchStims.IsActive );
        FrameData.AddDatum("SampleStimVisibility", ()=> sampleStim.IsActive );
    }
    public int ChooseTokenReward(TokenReward[] tokenRewards)
    {
        float totalProbability = 0;
        for (int i = 0; i < tokenRewards.Length; i++)
        {
            totalProbability += tokenRewards[i].Probability;
        } 

        if (Math.Abs(totalProbability - 1) > 0.001)
            Debug.LogError("Sum of token reward probabilities on this trial is " + totalProbability + ", probabilities will be scaled to sum to 1.");

        float randomNumber = UnityEngine.Random.Range(0, totalProbability);

        TokenReward selectedReward = tokenRewards[0];
        float curProbSum = 0;
        foreach (TokenReward tr in tokenRewards)
        {
            curProbSum += tr.Probability;
            if (curProbSum >= randomNumber)
            {
                selectedReward = tr;
                break;
            }
        }
        return selectedReward.NumTokens;
    }
    void SetTrialSummaryString()
    {
        TrialSummaryString = "\n" +
                             "Trial Count in Block: " + (TrialCount_InBlock + 1) +
                             "\nTrial Count in Task: " + (TrialCount_InTask + 1) +
                             "\n" +
                             "\nSelected Object Code: " + SelectedStimCode +
                             "\nSelected Object Location: " + SelectedStimLocation +
                             "\nCorrect Selection?: " + CorrectSelection +
                             "\nTouch Duration Error?: " + TouchDurationError +
                             "\n" +
                             "\nSearch Duration: " + SearchDuration +
                             "\n" + 
                             "\nToken Bar Value: " + TokenFBController.GetTokenBarValue();
    }
    private void CreateTextOnExperimenterDisplay()
    {
        playerViewParent = GameObject.Find("MainCameraCopy").transform; // sets parent for any playerView elements on experimenter display
        if (!playerViewLoaded)
        {
            //Create corresponding text on player view of experimenter display
            foreach (WorkingMemory_StimDef stim in searchStims.stimDefs)
            {
                if (stim.IsTarget)
                {
                    textLocation = playerViewPosition(Camera.main.WorldToScreenPoint(stim.StimLocation), playerViewParent);
                    textLocation.y += 50;
                    Vector2 textSize = new Vector2(200, 200);
                    playerViewText = playerView.writeText("TargetText","TARGET",
                        Color.red, textLocation, textSize, playerViewParent);
                    playerViewText.GetComponent<RectTransform>().localScale = new Vector3(2, 2, 0);
                    playerViewTextList.Add(playerViewText);
                    playerViewLoaded = true;
                }
            }
        }
    }
    private void DestroyTextOnExperimenterDisplay()
    {
        if (playerViewLoaded)
            foreach (GameObject txt in playerViewTextList) txt.SetActive(false);
        playerViewLoaded = false;
    }
    private void TouchDurationErrorFeedback(SelectionHandler<WorkingMemory_StimDef> MouseHandler, bool deactivateAfter)
    {
        AudioFBController.Play("Negative");
        if (MouseHandler.GetHeldTooShort())
            StartCoroutine(USE_StartButton.GratedStartButtonFlash(HeldTooShortTexture, gratingSquareDuration.value, deactivateAfter));
        else if (MouseHandler.GetHeldTooLong())
            StartCoroutine(USE_StartButton.GratedStartButtonFlash(HeldTooLongTexture, gratingSquareDuration.value, deactivateAfter));
        MouseHandler.SetHeldTooLong(false);
        MouseHandler.SetHeldTooShort(false);
        TouchDurationError = false;
        TouchDurationError_InBlock++;
    }
}
