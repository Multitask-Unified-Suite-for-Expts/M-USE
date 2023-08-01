using ConfigDynamicUI;
using System.Collections.Generic;
using UnityEngine;
using USE_ExperimentTemplate_Trial;
using USE_States;
using USE_StimulusManagement;
using WorkingMemory_Namespace;
using USE_UI;

public class WorkingMemory_TrialLevel : ControlLevel_Trial_Template
{
    public GameObject WM_CanvasGO;

    public WorkingMemory_TrialDef CurrentTrialDef => GetCurrentTrialDef<WorkingMemory_TrialDef>();
    public WorkingMemory_TaskLevel CurrentTaskLevel => GetTaskLevel<WorkingMemory_TaskLevel>();
    // Block End Variables
    public List<int> runningAcc;
    public int MinTrials, MaxTrials;
       
    // Stim Evaluation Variables
    private GameObject trialStim;
    private GameObject selectedGO = null;
    private bool CorrectSelection;
    WorkingMemory_StimDef selectedSD = null;
    
    // Stimuli Variables
    private StimGroup searchStims, sampleStim, postSampleDistractorStims;
    private GameObject StartButton;
       
    // Config Loading Variables
    private bool configUIVariablesLoaded = false;
    [HideInInspector]
    public ConfigNumber minObjectTouchDuration, maxObjectTouchDuration, gratingSquareDuration, tokenRevealDuration, tokenUpdateDuration, tokenFlashingDuration, selectObjectDuration, selectionFbDuration, displaySampleDuration, postSampleDelayDuration, 
        displayPostSampleDistractorsDuration, preTargetDelayDuration, itiDuration;
    private float tokenFbDuration;
 
    public string ContextExternalFilePath;
    public Vector3 StartButtonPosition;
    public float StartButtonScale;
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
    public string ContextName = "";
    public int NumCorrect_InBlock;
    public List<float> SearchDurations_InBlock = new List<float>();
    public int NumErrors_InBlock;
    public int NumRewardPulses_InBlock;
    public int NumTokenBarFull_InBlock;
    public int TotalTokensCollected_InBlock;
    public float Accuracy_InBlock;
    public float AverageSearchDuration_InBlock;
    public int NumAborted_InBlock;
   
    // Trial Data Variables
    private int? SelectedStimIndex = null;
    private string selectedStimName = null;
    private Vector3? SelectedStimLocation = null;
    private float SearchDuration = 0;
    private bool RewardGiven = false;
    private bool TouchDurationError = false;
    private bool aborted = false;
    private float? selectionDuration = null;
    private bool choiceMade = false;

    [HideInInspector] public float TouchFeedbackDuration;

    [HideInInspector] public bool MacMainDisplayBuild;


    public override void DefineControlLevel()
    {
        State InitTrial = new State("InitTrial");
        State DisplaySample = new State("DisplaySample");
        State DisplayDistractors = new State("DisplayDistractors");
        State SearchDisplay = new State("SearchDisplay");
        State SelectionFeedback = new State("SelectionFeedback");
        State TokenFeedback = new State("TokenFeedback");
        State ITI = new State("ITI");

        AddActiveStates(new List<State> { InitTrial, DisplaySample, DisplayDistractors, SearchDisplay, SelectionFeedback, TokenFeedback, ITI });

        
        playerView = new PlayerViewPanel(); //GameObject.Find("PlayerViewCanvas").GetComponent<PlayerViewPanel>()
        playerViewText = new GameObject();

        Add_ControlLevel_InitializationMethod(() =>
        {
            // Initialize FB Controller Values
            HaloFBController.SetHaloSize(5f);
            HaloFBController.SetHaloIntensity(5);
        });
        SetupTrial.AddInitializationMethod(() =>
        {
            //Set the Stimuli Light/Shadow settings
            SetShadowType(ShadowType, "WorkingMemory_DirectionalLight");
            if (StimFacingCamera)
                MakeStimFaceCamera();

            if(StartButton == null)
            {
                if (SessionValues.SessionDef.IsHuman)
                {
                    StartButton = SessionValues.HumanStartPanel.StartButtonGO;
                    SessionValues.HumanStartPanel.SetVisibilityOnOffStates(InitTrial, InitTrial);
                }
                else
                {
                    StartButton = SessionValues.USE_StartButton.CreateStartButton(WM_CanvasGO.GetComponent<Canvas>(), StartButtonPosition, StartButtonScale);
                    SessionValues.USE_StartButton.SetVisibilityOnOffStates(InitTrial, InitTrial);
                }
            }
                        
            if (!configUIVariablesLoaded) LoadConfigUIVariables();
            SetTrialSummaryString();
            CurrentTaskLevel.SetBlockSummaryString();
            TokenFBController.ResetTokenBarFull();
        });

        SetupTrial.SpecifyTermination(() => true, InitTrial);

        var ShotgunHandler = SessionValues.SelectionTracker.SetupSelectionHandler("trial", "TouchShotgun", SessionValues.MouseTracker, InitTrial, SearchDisplay);
        if (!SessionValues.SessionDef.IsHuman)
            TouchFBController.EnableTouchFeedback(ShotgunHandler, TouchFeedbackDuration, StartButtonScale, WM_CanvasGO);

        InitTrial.AddInitializationMethod(() =>
        {
            if (SessionValues.WebBuild)
                TokenFBController.AdjustTokenBarSizing(110);

            if (MacMainDisplayBuild & !Application.isEditor) //adj text positions if running build with mac as main display
                TokenFBController.AdjustTokenBarSizing(200);
            

            if (ShotgunHandler.AllSelections.Count > 0)
                ShotgunHandler.ClearSelections();

            ShotgunHandler.MinDuration = minObjectTouchDuration.value;
            ShotgunHandler.MaxDuration = maxObjectTouchDuration.value;
        });

        InitTrial.SpecifyTermination(() => ShotgunHandler.LastSuccessfulSelectionMatches(SessionValues.SessionDef.IsHuman ? SessionValues.HumanStartPanel.StartButtonChildren : SessionValues.USE_StartButton.StartButtonChildren), DisplaySample, () =>
        {
            //Set the token bar settings
            TokenFBController.enabled = true;
            TokenFBController
                .SetRevealTime(tokenRevealDuration.value)
                .SetUpdateTime(tokenUpdateDuration.value)
                .SetFlashingTime(tokenFlashingDuration.value);

            SessionValues.EventCodeManager.SendCodeImmediate("StartButtonSelected");
                
            CurrentTaskLevel.SetBlockSummaryString();
            if (TrialCount_InTask != 0)
                CurrentTaskLevel.SetTaskSummaryString();
        });
        
        // Show the target/sample by itself for some time
        DisplaySample.AddTimer(() => displaySampleDuration.value, Delay, () =>
          {
              StateAfterDelay = DisplayDistractors;
              DelayDuration = postSampleDelayDuration.value;
          });
        // Show some distractors without the target/sample
        DisplayDistractors.AddTimer(() => displayPostSampleDistractorsDuration.value, Delay, () =>
          {
              StateAfterDelay = SearchDisplay;
              DelayDuration = preTargetDelayDuration.value;
          });

        // Show the target/sample with some other distractors
        // Wait for a click and provide feedback accordingly
        SearchDisplay.AddInitializationMethod(() =>
        {
            if (!SessionValues.WebBuild)
                CreateTextOnExperimenterDisplay();

            searchStims.ToggleVisibility(true);

            SessionValues.EventCodeManager.SendCodeNextFrame("StimOn");
            SessionValues.EventCodeManager.SendCodeNextFrame("TokenBarVisible");
            
            choiceMade = false;

            if (ShotgunHandler.AllSelections.Count > 0)
                ShotgunHandler.ClearSelections();
        });
        SearchDisplay.AddUpdateMethod(() =>
        {
            if (ShotgunHandler.SuccessfulSelections.Count > 0)
            {
                selectedGO = ShotgunHandler.LastSuccessfulSelection.SelectedGameObject;
                selectedSD = selectedGO?.GetComponent<StimDefPointer>()?.GetStimDef<WorkingMemory_StimDef>();
                ShotgunHandler.ClearSelections();
                if (selectedSD != null)
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
                SessionValues.EventCodeManager.SendCodeNextFrame("Button0PressedOnTargetObject");//SELECTION STUFF (code may not be exact and/or could be moved to Selection handler)
                SessionValues.EventCodeManager.SendCodeNextFrame("CorrectResponse");
            }
            else
            {
                NumErrors_InBlock++;
                CurrentTaskLevel.NumErrors_InTask++;
                SessionValues.EventCodeManager.SendCodeNextFrame("Button0PressedOnDistractorObject");//SELECTION STUFF (code may not be exact and/or could be moved to Selection handler)
                SessionValues.EventCodeManager.SendCodeNextFrame("IncorrectResponse");
            }

            if (selectedGO != null)
            {
                SelectedStimIndex = selectedSD.StimIndex;
                SelectedStimLocation = selectedSD.StimLocation;
            }
            Accuracy_InBlock = NumCorrect_InBlock/(TrialCount_InBlock + 1);
            SetTrialSummaryString();
        });
        SearchDisplay.AddTimer(() => selectObjectDuration.value, ITI, () =>
        {
            //means the player got timed out and didn't click on anything

            aborted = true;
            NumAborted_InBlock++;
            CurrentTaskLevel.NumAborted_InTask++;
            AbortCode = 6;
            SessionValues.EventCodeManager.SendCodeNextFrame("NoChoice");
        });

        SelectionFeedback.AddInitializationMethod(() =>
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
        SelectionFeedback.AddTimer(() => selectionFbDuration.value, TokenFeedback, () => 
        {
            HaloFBController.Destroy();
        });


        // The state that will handle the token feedback and wait for any animations
        TokenFeedback.AddInitializationMethod(() =>
        {
            if(!SessionValues.WebBuild)
            {
                if (GameObject.Find("MainCameraCopy").transform.childCount != 0)
                    DestroyChildren(GameObject.Find("MainCameraCopy"));
            }

            searchStims.ToggleVisibility(false);
            if (selectedSD.IsTarget)
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
            if (TokenFBController.IsTokenBarFull())
            {
                NumTokenBarFull_InBlock++;
                CurrentTaskLevel.NumTokenBarFull_InTask++;
                if (SessionValues.SyncBoxController != null)
                {
                    SessionValues.SyncBoxController.SendRewardPulses(CurrentTrialDef.NumPulses, CurrentTrialDef.PulseSize);
                   // SessionInfoPanel.UpdateSessionSummaryValues(("totalRewardPulses",CurrentTrialDef.NumPulses)); moved to syncbox class
                    NumRewardPulses_InBlock += CurrentTrialDef.NumPulses;
                    CurrentTaskLevel.NumRewardPulses_InTask += CurrentTrialDef.NumPulses;
                    RewardGiven = true;
                }
            }
        });
        ITI.AddInitializationMethod(() =>
        {
            if (NeutralITI)
            {
                ContextName = "itiImage";
                RenderSettings.skybox = CreateSkybox(GetContextNestedFilePath(ContextExternalFilePath, ContextName));
                SessionValues.EventCodeManager.SendCodeNextFrame("ContextOff");
            }
        });
        // Wait for some time at the end
        ITI.AddTimer(() => itiDuration.value, FinishTrial);

        //---------------------------------ADD FRAME AND TRIAL DATA TO LOG FILES---------------------------------------
        AssignFrameData();
        AssignTrialData();
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
        if(!SessionValues.WebBuild)
        {
            if (GameObject.Find("MainCameraCopy").transform.childCount != 0)
                DestroyChildren(GameObject.Find("MainCameraCopy"));
        }

        TokenFBController.enabled = false;
        searchStims.ToggleVisibility(false);
        sampleStim.ToggleVisibility(false);
        postSampleDistractorStims.ToggleVisibility(false);
        if (AbortCode == 0)
            CurrentTaskLevel.SetBlockSummaryString();

        if (AbortCode == AbortCodeDict["RestartBlock"] || AbortCode == AbortCodeDict["PreviousBlock"])
        {
            aborted = true;
            NumAborted_InBlock++;
            CurrentTaskLevel.NumAborted_InTask++;
            CurrentTaskLevel.BlockSummaryString.Clear();
            CurrentTaskLevel.BlockSummaryString.AppendLine("");
        }
    }

    public void ResetBlockVariables()
    {
        SearchDurations_InBlock.Clear();
        AverageSearchDuration_InBlock = 0;
        NumErrors_InBlock = 0;
        NumCorrect_InBlock = 0;
        NumRewardPulses_InBlock = 0;
        NumTokenBarFull_InBlock = 0;
        Accuracy_InBlock = 0;
        TotalTokensCollected_InBlock = 0;
        NumAborted_InBlock = 0;
    }

    protected override void DefineTrialStims()
    {
        //Define StimGroups consisting of StimDefs whose gameobjects will be loaded at TrialLevel_SetupTrial and 
        //destroyed at TrialLevel_Finish

        StimGroup group = SessionValues.UsingDefaultConfigs ? PrefabStims : ExternalStims;

        searchStims = new StimGroup("SearchStims", group, CurrentTrialDef.SearchStimIndices);
        //searchStims.SetVisibilityOnOffStates(GetStateFromName("SearchDisplay"), GetStateFromName("TokenFeedback"));
        searchStims.SetLocations(CurrentTrialDef.SearchStimLocations);
        TrialStims.Add(searchStims);

        List<StimDef> rewardedStimdefs = new List<StimDef>();

        sampleStim = new StimGroup("TargetStim", GetStateFromName("DisplaySample"), GetStateFromName("DisplaySample"));
        for (int iStim = 0; iStim < CurrentTrialDef.SearchStimIndices.Length; iStim++)
        {
            WorkingMemory_StimDef sd = (WorkingMemory_StimDef)searchStims.stimDefs[iStim];
            sd.StimTokenRewardMag = chooseReward(CurrentTrialDef.SearchStimTokenReward[iStim]);
            if (sd.StimTokenRewardMag > 0)
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
        TrialStims.Add(sampleStim);

        postSampleDistractorStims = new StimGroup("DisplayDistractors", group, CurrentTrialDef.PostSampleDistractorIndices);
        postSampleDistractorStims.SetVisibilityOnOffStates(GetStateFromName("DisplayDistractors"), GetStateFromName("DisplayDistractors"));
        postSampleDistractorStims.SetLocations(CurrentTrialDef.PostSampleDistractorLocations);
        TrialStims.Add(postSampleDistractorStims);
        
     }
    // protected override bool CheckBlockEnd()
    // {
    //     TaskLevelTemplate_Methods TaskLevel_Methods = new TaskLevelTemplate_Methods();
    //     return TaskLevel_Methods.CheckBlockEnd(CurrentTrialDef.BlockEndType, runningAcc,
    //         CurrentTrialDef.BlockEndThreshold, CurrentTrialDef.BlockEndWindow, MinTrials,
    //         TrialDefs.Count);
    // }

    public void LoadConfigUIVariables()
    {   
        //config UI variables
        minObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("minObjectTouchDuration");
        maxObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("maxObjectTouchDuration"); /*
        trialEndDuration = ConfigUiVariables.get<ConfigNumber>("trialEndDuration"); 
        initTrialDuration = ConfigUiVariables.get<ConfigNumber>("initTrialDuration");
        baselineDuration = ConfigUiVariables.get<ConfigNumber>("baselineDuration"); */
        selectObjectDuration = ConfigUiVariables.get<ConfigNumber>("selectObjectDuration");
        selectionFbDuration = ConfigUiVariables.get<ConfigNumber>("selectionFbDuration");
        displaySampleDuration = ConfigUiVariables.get<ConfigNumber>("displaySampleDuration");
        postSampleDelayDuration = ConfigUiVariables.get<ConfigNumber>("postSampleDelayDuration");
        displayPostSampleDistractorsDuration = ConfigUiVariables.get<ConfigNumber>("displayPostSampleDistractorsDuration");
        preTargetDelayDuration = ConfigUiVariables.get<ConfigNumber>("preTargetDelayDuration");
        itiDuration = ConfigUiVariables.get<ConfigNumber>("itiDuration");
        gratingSquareDuration = ConfigUiVariables.get<ConfigNumber>("gratingSquareDuration");
        tokenRevealDuration = ConfigUiVariables.get<ConfigNumber>("tokenRevealDuration");
        tokenUpdateDuration = ConfigUiVariables.get<ConfigNumber>("tokenUpdateDuration");
        tokenFlashingDuration = ConfigUiVariables.get<ConfigNumber>("tokenFlashingDuration");

        tokenFbDuration = (tokenFlashingDuration.value + tokenUpdateDuration.value + tokenRevealDuration.value);//ensures full flashing duration within
        ////configured token fb duration
        configUIVariablesLoaded = true;
    }
    public override void ResetTrialVariables()
    {
        SelectedStimIndex = null;
        SelectedStimLocation = null;
        SearchDuration = 0;
        CorrectSelection = false;
        RewardGiven = false;
        TouchDurationError = false;
        aborted = false;
        choiceMade = false;

        selectedGO = null;
        selectedSD = null;
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
    void SetTrialSummaryString()
    {
        TrialSummaryString = "Selected Object Code: " + SelectedStimIndex +
                             "\nSelected Object Location: " + SelectedStimLocation +
                             "\n\nCorrect Selection: " + CorrectSelection +
                             "\nTouch Duration Error: " + TouchDurationError +
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
                    textLocation = ScreenToPlayerViewPosition(Camera.main.WorldToScreenPoint(stim.StimLocation), playerViewParent);
                    textLocation.y += 50;
                    Vector2 textSize = new Vector2(200, 200);
                    playerViewText = playerView.CreateTextObject("TargetText","TARGET",
                        Color.red, textLocation, textSize, playerViewParent);
                    playerViewText.GetComponent<RectTransform>().localScale = new Vector3(2, 2, 0);
                    playerViewTextList.Add(playerViewText);
                    playerViewLoaded = true;
                }
            }
        }
    }
}
