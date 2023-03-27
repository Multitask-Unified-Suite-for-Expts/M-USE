using System;
using ConfigDynamicUI;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EffortControl_Namespace;
using UnityEngine;
using UnityEngine.Serialization;
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
    public USE_StartButton USE_FBSquare;

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
    private GameObject FBSquare;

    
       
    // Config Loading Variables
    private bool configUIVariablesLoaded = false;
    [HideInInspector]
    public ConfigNumber minObjectTouchDuration, maxObjectTouchDuration, gratingSquareDuration, tokenRevealDuration, tokenUpdateDuration, tokenFlashingDuration, selectObjectDuration, selectionFbDuration, displaySampleDuration, postSampleDelayDuration, 
        displayPostSampleDistractorsDuration, preTargetDelayDuration, itiDuration;
    private float tokenFbDuration;
 
    public string ContextExternalFilePath;
    public Vector3 StartButtonPosition;
    public float StartButtonScale;
    public Vector3 FBSquarePosition;
    public float FBSquareScale;
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
    public int TouchDurationError_InBlock;
    public int NumAborted_InBlock;
   
    // Trial Data Variables
    private int? SelectedStimIndex = null;
    private string selectedStimName = null;
    private Vector3? SelectedStimLocation = null;
    private float SearchDuration = 0;
    private bool RewardGiven = false;
    private bool TouchDurationError = false;
    private Ray ray;
    private RaycastHit hit;
    private bool HeldTooShort;
    private bool HeldTooLong;
    private float gratingDuration;
    private bool aborted = false;
    private float? selectionDuration = null;
    private bool choiceMade = false;

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

        
        Text commandText = null;
        playerView = new PlayerViewPanel(); //GameObject.Find("PlayerViewCanvas").GetComponent<PlayerViewPanel>()
        playerViewText = new GameObject();
        SelectionHandler<WorkingMemory_StimDef> mouseHandler = new SelectionHandler<WorkingMemory_StimDef>();

        Add_ControlLevel_InitializationMethod(() =>
        {
            LoadTextures(ContextExternalFilePath);
            // Initialize FB Controller Values
            HaloFBController.SetHaloSize(5f);
            HaloFBController.SetHaloIntensity(5);
        });
        SetupTrial.AddInitializationMethod(() =>
        {
            //Set the Stimuli Light/Shadow settings
            SetShadowType(ShadowType, "WorkingMemory_DirectionalLight");
            if (StimFacingCamera)
            {
                MakeStimFaceCamera();
            }

            if(StartButton == null)
            {
                USE_StartButton = new USE_StartButton(WM_CanvasGO.GetComponent<Canvas>(), StartButtonPosition, StartButtonScale);
                StartButton = USE_StartButton.StartButtonGO;
                USE_StartButton.SetVisibilityOnOffStates(InitTrial, InitTrial);
            }
            if (FBSquare == null)
            {
                USE_FBSquare = new USE_StartButton(WM_CanvasGO.GetComponent<Canvas>(), FBSquarePosition, FBSquareScale);
                FBSquare = USE_FBSquare.StartButtonGO;
                FBSquare.name = "FBSquare";
            }
            
            DeactivateChildren(WM_CanvasGO);  
            
            if (!configUIVariablesLoaded) LoadConfigUIVariables();
            SetTrialSummaryString();
            CurrentTaskLevel.SetBlockSummaryString();
            TokenFBController.ResetTokenBarFull();
        });

        SetupTrial.SpecifyTermination(() => true, InitTrial);
        MouseTracker.AddSelectionHandler(mouseHandler, InitTrial, null, 
            ()=> MouseTracker.ButtonStatus[0] == 1, ()=> MouseTracker.ButtonStatus[0] == 0);
    
        InitTrial.SpecifyTermination(() => mouseHandler.SelectionMatches(StartButton),
            DisplaySample, () => {
                // Turn off start button and set the token bar settings
                StartButton.SetActive(false);
                TokenFBController.enabled = true;
                TokenFBController
                    .SetRevealTime(tokenRevealDuration.value)
                    .SetUpdateTime(tokenUpdateDuration.value)
                    .SetFlashingTime(tokenFlashingDuration.value);
                EventCodeManager.SendCodeImmediate(SessionEventCodes["StartButtonSelected"]);
                
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
        MouseTracker.AddSelectionHandler(mouseHandler, SearchDisplay, null, 
            ()=> MouseTracker.ButtonStatus[0] == 1, ()=> MouseTracker.ButtonStatus[0] == 0);
        SearchDisplay.AddInitializationMethod(() =>
        {
            CreateTextOnExperimenterDisplay();
            searchStims.ToggleVisibility(true);
            EventCodeManager.SendCodeNextFrame(SessionEventCodes["StimOn"]);
            EventCodeManager.SendCodeNextFrame(SessionEventCodes["TokenBarVisible"]);
            choiceMade = false;
        });
        SearchDisplay.AddUpdateMethod(() =>
        {
            TouchDurationErrorFeedback(USE_FBSquare, true);
        });
        SearchDisplay.SpecifyTermination(() => choiceMade, SelectionFeedback, () =>
        {
            if (CorrectSelection)
            {       
                NumCorrect_InBlock++;
                CurrentTaskLevel.NumCorrect_InTask++;
                EventCodeManager.SendCodeNextFrame(SessionEventCodes["Button0PressedOnTargetObject"]);//SELECTION STUFF (code may not be exact and/or could be moved to Selection handler)
                EventCodeManager.SendCodeNextFrame(SessionEventCodes["CorrectResponse"]);
            }
            else
            {
                NumErrors_InBlock++;
                CurrentTaskLevel.NumErrors_InTask++;
                EventCodeManager.SendCodeNextFrame(SessionEventCodes["Button0PressedOnDistractorObject"]);//SELECTION STUFF (code may not be exact and/or could be moved to Selection handler)
                EventCodeManager.SendCodeNextFrame(SessionEventCodes["IncorrectResponse"]);
            }

            if (selectedGO != null)
            {
                SelectedStimIndex = selectedSD.StimIndex;
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
                aborted = true;
                NumAborted_InBlock++;
                CurrentTaskLevel.NumAborted_InTask++;
                EventCodeManager.SendCodeNextFrame(SessionEventCodes["NoChoice"]);
            }
        });

        SelectionFeedback.AddInitializationMethod(() =>
        {
            SearchDuration = SearchDisplay.TimingInfo.Duration;
            SearchDurations_InBlock.Add(SearchDuration);
            CurrentTaskLevel.SearchDurations_InTask.Add(SearchDuration);
            SetTrialSummaryString();
            
            if (CorrectSelection) 
                HaloFBController.ShowPositive(selectedGO);
            else 
                HaloFBController.ShowNegative(selectedGO);
        });
        SelectionFeedback.AddTimer(() => selectionFbDuration.value, TokenFeedback, () => 
        {
            HaloFBController.Destroy();
        });


        // The state that will handle the token feedback and wait for any animations
        TokenFeedback.AddInitializationMethod(() =>
        {
            if (GameObject.Find("MainCameraCopy").transform.childCount != 0)
                DestroyChildren(GameObject.Find("MainCameraCopy"));
            searchStims.ToggleVisibility(false);
            if (selectedSD.IsTarget)
            {
                TokenFBController.AddTokens(selectedGO, selectedSD.StimTrialRewardMag);
                TotalTokensCollected_InBlock += selectedSD.StimTrialRewardMag;
                CurrentTaskLevel.TotalTokensCollected_InTask += selectedSD.StimTrialRewardMag;
            }
            else
            {
                TokenFBController.RemoveTokens(selectedGO, -selectedSD.StimTrialRewardMag);
                TotalTokensCollected_InBlock -= selectedSD.StimTrialRewardMag;
                CurrentTaskLevel.TotalTokensCollected_InTask -= selectedSD.StimTrialRewardMag;
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
                    SessionInfoPanel.UpdateSessionSummaryValues(("totalRewardPulses",CurrentTrialDef.NumPulses));
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
                EventCodeManager.SendCodeNextFrame(SessionEventCodes["ContextOff"]);
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
        if (GameObject.Find("MainCameraCopy").transform.childCount != 0)
            DestroyChildren(GameObject.Find("MainCameraCopy"));
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
        TouchDurationError_InBlock = 0;
        Accuracy_InBlock = 0;
        TotalTokensCollected_InBlock = 0;
        NumAborted_InBlock = 0;
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
        FrameData.AddDatum("FBSquareVisibility", () => FBSquare == null ? false:FBSquare.activeSelf); // CHECK THE DATA!
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
                    textLocation = playerViewPosition(Camera.main.WorldToScreenPoint(stim.StimLocation), playerViewParent);
                    textLocation.y += 50;
                    Vector2 textSize = new Vector2(200, 200);
                    playerViewText = playerView.WriteText("TargetText","TARGET",
                        Color.red, textLocation, textSize, playerViewParent);
                    playerViewText.GetComponent<RectTransform>().localScale = new Vector3(2, 2, 0);
                    playerViewTextList.Add(playerViewText);
                    playerViewLoaded = true;
                }
            }
        }
    }
    private void TouchDurationErrorFeedback(USE_StartButton UIElement, bool deactivateAfter)
     {
         if (UIElement.IsGrating)
         {
             gratingDuration -= Time.deltaTime;
             if (HeldTooShort)
                 UIElement.GratedStartButtonFlash(HeldTooShortTexture, gratingDuration, deactivateAfter);
             else
                 UIElement.GratedStartButtonFlash(HeldTooLongTexture, gratingDuration, deactivateAfter);
             return;
         }

         if (InputBroker.GetMouseButtonDown(0) && !UIElement.IsGrating)
         {
             ray = Camera.main.ScreenPointToRay(InputBroker.mousePosition);
             selectionDuration = 0;
             HeldTooLong = false;
             HeldTooShort = false;
             //record start position as well
         }

         selectionDuration += Time.deltaTime;
         if (InputBroker.GetMouseButtonUp(0) && selectionDuration != null)
         {
             if (Physics.Raycast(ray, out hit))
             {
                 if ((hit.collider != null) && (hit.collider.gameObject != null) &&
                     (selectionDuration >= minObjectTouchDuration.value) &&
                     (selectionDuration <= maxObjectTouchDuration.value))
                 {
                     choiceMade = true;
                     TouchDurationError = false;
                     selectedGO = hit.collider.gameObject;
                     selectedSD = selectedGO?.GetComponent<StimDefPointer>()?.GetStimDef<WorkingMemory_StimDef>();
                     CorrectSelection = selectedSD.IsTarget;
                 }
                 else if (selectionDuration < minObjectTouchDuration.value)
                 {
                     UIElement.GratedStartButtonFlash(HeldTooShortTexture, gratingSquareDuration.value,
                         deactivateAfter);
                     gratingDuration = gratingSquareDuration.value;
                     TouchDurationError = true;
                     HeldTooShort = true;
                     TouchDurationError_InBlock++;
                     CurrentTaskLevel.TouchDurationError_InTask++;
                     Debug.Log("Didn't select for minimum object touch duration!");
                 }
                 else if (selectionDuration > maxObjectTouchDuration.value)
                 {
                     UIElement.GratedStartButtonFlash(HeldTooLongTexture, gratingSquareDuration.value, deactivateAfter);
                     gratingDuration = gratingSquareDuration.value;
                     TouchDurationError = true;
                     HeldTooLong = true;
                     TouchDurationError_InBlock++;
                     CurrentTaskLevel.TouchDurationError_InTask++;
                     Debug.Log("Didn't select under max object touch duration!");
                 }
             }

             selectionDuration = null; // set this as null to consider multiple selections in a state
         }

         SetTrialSummaryString();
    }
}
