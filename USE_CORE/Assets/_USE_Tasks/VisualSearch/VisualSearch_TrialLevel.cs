using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using ConfigDynamicUI;
using UnityEngine.Serialization;
using USE_States;
using USE_StimulusManagement;
using USE_ExperimentTemplate_Trial;
using VisualSearch_Namespace;

public class VisualSearch_TrialLevel : ControlLevel_Trial_Template
{
    public VisualSearch_TrialDef CurrentTrialDef => GetCurrentTrialDef<VisualSearch_TrialDef>(); 
    public VisualSearch_TaskLevel CurrentTaskLevel => GetTaskLevel<VisualSearch_TaskLevel>();
    
    // Stimuli Variables
    private StimGroup tStim;
    private GameObject StartButton;
    private GameObject FBSquare;
    private bool Grating = false;
    private TaskHelperFunctions taskHelper;
    
    // ConfigUI variables
    private bool configUIVariablesLoaded;
    [HideInInspector]
    public ConfigNumber minObjectTouchDuration, itiDuration, fbDuration, maxObjectTouchDuration, 
        selectObjectDuration, tokenRevealDuration, tokenUpdateDuration, searchDisplayDelay, gratingSquareDuration, tokenFbDuration;
    
    // Set in the Task Level
    [HideInInspector] public string ContextExternalFilePath;
    [HideInInspector] public Vector3 ButtonPosition, ButtonScale;
    [HideInInspector] public Vector3 FBSquarePosition, FBSquareScale;
    [HideInInspector] public bool StimFacingCamera;
    [HideInInspector] public string ShadowType;
    [HideInInspector] public bool NeutralITI;
    
    // Stim Evaluation Variables
    private GameObject trialStim;
    private GameObject selected = null;
    private bool CorrectSelection = false;
    VisualSearch_StimDef selectedSD = null;
    private bool ObjectsCreated = false;
    
    //Player View Variables
    private PlayerViewPanel playerView;
    private Transform playerViewParent; // Helps set things onto the player view in the experimenter display
    private List<GameObject> playerViewTextList = new List<GameObject>();
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
    [HideInInspector] public float Accuracy_InBlock;
    [HideInInspector] public float AverageSearchDuration_InBlock;
    [HideInInspector] public int TouchDurationError_InBlock;
   
    // Trial Data Variables
    private int? SelectedStimIndex = null;
    private string selectedStimName = null;
    private Vector3? SelectedStimLocation = null;
    private float SearchDuration = 0;
    private bool RewardGiven = false;
    private bool TouchDurationError = false;

    public override void DefineControlLevel()
    {
        State InitTrial = new State("InitTrial");
        State SearchDisplay = new State("SearchDisplay");
        State SelectionFeedback = new State("SelectionFeedback");
        State TokenFeedback = new State("TokenFeedback");
        State ITI = new State("ITI");
        State SearchDisplayDelay = new State("SearchDisplayDelay");
        State Delay = new State("Delay");
        
        AddActiveStates(new List<State> {InitTrial, SearchDisplay, SelectionFeedback, TokenFeedback, ITI, Delay, SearchDisplayDelay});
        SelectionHandler<VisualSearch_StimDef> mouseHandler = new SelectionHandler<VisualSearch_StimDef>();
        
        // Initialize FB Controller Values
        HaloFBController.SetHaloSize(5);
        
        // A state that just waits for some time
        State stateAfterDelay = null;
        float delayDuration = 0;
        Delay.AddTimer(() => delayDuration, () => stateAfterDelay);
        
        LoadTextures(ContextExternalFilePath);
        Text commandText = null;
        playerView = new PlayerViewPanel(); //GameObject.Find("PlayerViewCanvas").GetComponent<PlayerViewPanel>()
        playerViewText = new GameObject();
        taskHelper = new TaskHelperFunctions();
        
        
        
        SetupTrial.AddInitializationMethod(() =>
        {
            ResetTrialVariables();
            TokenFBController.SetTokenBarFull(false);
            //Set the context for the upcoming trial with the Start Button visible
            ContextName = CurrentTrialDef.ContextName;
            RenderSettings.skybox = CreateSkybox(ContextExternalFilePath + Path.DirectorySeparatorChar + ContextName + ".png");
            
            //Set the Stimuli Light/Shadow settings
            if (StimFacingCamera)
            {
                foreach (var stim in tStim.stimDefs) stim.StimGameObject.AddComponent<FaceCamera>();
            }
            SetShadowType(ShadowType, "VisualSearch_DirectionalLight");
            
            //Create and Load variables needed at the start of the trial
            if (!ObjectsCreated)
                CreateObjects();
            if (!configUIVariablesLoaded) 
                LoadConfigUIVariables();
            if (playerViewText == null)
                CreateTextOnExperimenterDisplay();
            if (TrialCount_InBlock == 0)
            {
                playerViewText.SetActive(false);
            }
            
            SetTrialSummaryString();
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["TrlStart"]);
        });

        SetupTrial.SpecifyTermination(() => true, InitTrial);
        MouseTracker.AddSelectionHandler(mouseHandler, InitTrial);
        
        InitTrial.AddInitializationMethod(() =>
        {
            //Initialize FB Controller Variables
            mouseHandler.SetMinTouchDuration(minObjectTouchDuration.value);
            mouseHandler.SetMaxTouchDuration(maxObjectTouchDuration.value);
            TokenFBController.SetRevealTime(tokenRevealDuration.value);
            TokenFBController.SetUpdateTime(tokenUpdateDuration.value);

            StartButton.SetActive(true);
        });
        
        //INIT TRIAL STATE ----------------------------------------------------------------------------------------------
        InitTrial.AddUpdateMethod(() =>
        {
            if (mouseHandler.GetHeldTooLong() || mouseHandler.GetHeldTooShort())
            {
                TouchDurationError = true;
                SetTrialSummaryString();
                TouchDurationErrorFeedback(mouseHandler, StartButton);
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
            tStim.ToggleVisibility(true);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["StimOn"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["TokenBarVisible"]);
        });
        SearchDisplay.AddUpdateMethod(() =>
        {
            if (mouseHandler.GetHeldTooLong() || mouseHandler.GetHeldTooShort())
            {
                TouchDurationError = true;
                FBSquare.SetActive(true);
                SetTrialSummaryString();
                TouchDurationErrorFeedback(mouseHandler, FBSquare);
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
                SelectedStimIndex = selectedSD.StimIndex;
                SelectedStimLocation = selectedSD.StimLocation;
            }
            SetTrialSummaryString();
            Accuracy_InBlock = NumCorrect_InBlock/(TrialCount_InBlock + 1);
        });

        SearchDisplay.AddTimer(() => selectObjectDuration.value, ITI, ()=> 
        {
            if (mouseHandler.SelectedStimDef == null)   //means the player got timed out and didn't click on anything
            {
                Debug.Log("Timed out of selection state before making a choice");
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["NoChoice"]);
            }
        });
        
        // SELECTION FEEDBACK STATE ---------------------------------------------------------------------------------------   
        SelectionFeedback.AddInitializationMethod(() =>
        {
            SearchDuration = SearchDisplay.TimingInfo.Duration;
            SearchDurationsList.Add(SearchDuration);
            AverageSearchDuration_InBlock = SearchDurationsList.Average();
            SetTrialSummaryString();
            if (CorrectSelection) HaloFBController.ShowPositive(selected);
            else HaloFBController.ShowNegative(selected);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionVisualFbOn"]);
        });

        SelectionFeedback.AddTimer(() => fbDuration.value, TokenFeedback,()=>
        {   
            HaloFBController.Destroy();
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["StimOff"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionVisualFbOff"]);
        });
        
        // TOKEN FEEDBACK STATE ------------------------------------------------------------------------------------------------
        TokenFeedback.AddInitializationMethod(() =>
        {
            if (selectedSD.StimTrialRewardMag > 0)
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
                    NumRewardPulses_InBlock += CurrentTrialDef.NumPulses;
                    RewardGiven = true;
                }
            }
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["TrlEnd"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["ContextOff"]);
            TotalTokensCollected_InBlock = TokenFBController.GetTokenBarValue() +
                                           (NumTokenBarFull_InBlock * CurrentTrialDef.NumTokenBar);
            SetTrialSummaryString();
            CurrentTaskLevel.SetBlockSummaryString();
            stateAfterDelay = ITI;
            delayDuration = tokenFbDuration.value;
        });
        // ITI STATE ---------------------------------------------------------------------------------------------------
        ITI.AddInitializationMethod(() =>
        {
            if (NeutralITI)
            {
                ContextName = "itiImage";
                RenderSettings.skybox = CreateSkybox(ContextExternalFilePath + Path.DirectorySeparatorChar + ContextName + ".png");
            }
            // Remove the Stimuli, Context, and Token Bar from the Player View and move to neutral ITI State
            DestroyTextOnExperimenterDisplay();
            tStim.ToggleVisibility(false);
            TokenFBController.enabled = false;
        });
    
        ITI.AddTimer(() => itiDuration.value, FinishTrial);
        //---------------------------------ADD FRAME AND TRIAL DATA TO LOG FILES---------------------------------------
        AssignTrialData();
        AssignFrameData();
    }

    protected override void DefineTrialStims()
    {
        //Define StimGroups consisting of StimDefs whose gameobjects will be loaded at TrialLevel_SetupTrial and 
        //destroyed at TrialLevel_Finish
        tStim = new StimGroup("SearchStimuli", ExternalStims, CurrentTrialDef.TrialStimIndices);
        TrialStims.Add(tStim);
        for (int i = 0; i < CurrentTrialDef.TrialStimIndices.Length; i++)
        {
            VisualSearch_StimDef sd = (VisualSearch_StimDef)tStim.stimDefs[i];
            sd.StimTrialRewardMag = ChooseTokenReward(CurrentTrialDef.TrialStimTokenReward[i]);
            if (sd.StimTrialRewardMag > 0) sd.IsTarget = true; //ONLY HOLDS TRUE IF POSITIVE REWARD GIVEN TO TARGET
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
        gratingSquareDuration = ConfigUiVariables.get<ConfigNumber>("gratingSquareDuration");
        tokenFbDuration = ConfigUiVariables.get<ConfigNumber>("tokenFbDuration");
        //finalFbDuration = ConfigUiVariables.get<ConfigNumber>("finalFbDuration");
        configUIVariablesLoaded = true;
    }
    void SetTrialSummaryString()
    {
        TrialSummaryString = "<b>Trial Count in Block: " + (TrialCount_InBlock + 1) + "</b>"+
                             "\nTrial Count in Task: " + (TrialCount_InTask + 1) +
                             "\n" +
                             "\nSelected Object Index: " + SelectedStimIndex +
                             "\nSelected Object Location: " + SelectedStimLocation +
                             "\n" + 
                             "\nCorrect Selection?: " + CorrectSelection +
                             "\nTouch Duration Error?: " + TouchDurationError +
                             "\n" +
                             "\nSearch Duration: " + SearchDuration +
                             "\n" + 
                             "\nToken Bar Value: " + TokenFBController.GetTokenBarValue();
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
        TrialData.AddDatum("SelecteStimIndex", () => selectedSD?.StimIndex ?? null);
        TrialData.AddDatum("SelectedLocation", () => selectedSD?.StimLocation ?? null);
        TrialData.AddDatum("CorrectSelection", () => CorrectSelection ? 1 : 0);
        TrialData.AddDatum("SearchDuration", ()=> SearchDuration);
        TrialData.AddDatum("RewardGiven", ()=> RewardGiven? 1 : 0);
        TrialData.AddDatum("TotalClicks", ()=> MouseTracker.GetClickCount());
    }
    private void AssignFrameData()
    {
        // All AddDatum commmands from the Frame Data
        FrameData.AddDatum("ContextName", () => ContextName);
        FrameData.AddDatum("StartButtonVisibility", () => StartButton.activeSelf);
        FrameData.AddDatum("TrialStimVisibility", () => tStim.IsActive);
    }
    private void CreateObjects()
    {
        StartButton = CreateSquare("StartButton", StartButtonTexture, ButtonPosition, ButtonScale);
        FBSquare = CreateSquare("FBSquare", FBSquareTexture, FBSquarePosition, FBSquareScale);
        ObjectsCreated = true;
    }
    private void CreateTextOnExperimenterDisplay()
    {
        playerViewParent = GameObject.Find("MainCameraCopy").transform; // sets parent for any playerView elements on experimenter display
        if (!playerViewLoaded)
        {
            //Create corresponding text on player view of experimenter display
            foreach (VisualSearch_StimDef stim in tStim.stimDefs)
            {
                if (stim.IsTarget)
                {
                    textLocation =
                        playerViewPosition(Camera.main.WorldToScreenPoint(stim.StimLocation),
                            playerViewParent);
                    textLocation.y += 50;
                    Vector2 textSize = new Vector2(200, 200);
                    playerViewText = playerView.writeText("TARGET",
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
            foreach (GameObject txt in playerViewTextList) Destroy(txt);
        playerViewLoaded = false;
    }
    private void TouchDurationErrorFeedback(SelectionHandler<VisualSearch_StimDef> MouseHandler, GameObject go)
    {///CANT FIGURE OUT WHY I CANT USE TEMPLATE, ANYWAYS MAKE A SEPARATE FEEDBACK SCRIPT
        AudioFBController.Play("Negative");
        if (MouseHandler.GetHeldTooShort())
            StartCoroutine(taskHelper.GratedSquareFlash(HeldTooShortTexture, go, gratingSquareDuration.value));
        else if (MouseHandler.GetHeldTooLong())
            StartCoroutine(taskHelper.GratedSquareFlash(HeldTooLongTexture, go, gratingSquareDuration.value));
        MouseHandler.SetHeldTooLong(false);
        MouseHandler.SetHeldTooShort(false);
        TouchDurationError = false;
        TouchDurationError_InBlock++;
    }

}