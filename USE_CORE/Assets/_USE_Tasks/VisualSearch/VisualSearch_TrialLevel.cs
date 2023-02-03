using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using ConfigDynamicUI;
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
    private GameObject FBSquare;/*
    public Texture2D HeldTooShortTexture;
    public Texture2D HeldTooLongTexture;
    private Texture2D StartButtonTexture;
    private Texture2D FBSquareTexture;*/
    private bool Grating = false;
    private TaskHelperFunctions taskHelper;
    
    // ConfigUI variables
    [HideInInspector]
    public ConfigNumber minObjectTouchDuration, itiDuration, fbDuration, maxObjectTouchDuration, 
        selectObjectDuration, tokenRevealDuration, tokenUpdateDuration, searchDisplayDelay, gratingSquareDuration, tokenFbDuration;
    
    // Stim Evaluation Variables
    private GameObject trialStim;
    private GameObject selected = null;
    private bool CorrectSelection = false;
    VisualSearch_StimDef selectedSD = null;

    // Config Loading Variables
    private bool configUIVariablesLoaded;
    public string ContextExternalFilePath;
    public Vector3 ButtonPosition, ButtonScale;
    public Vector3 FBSquarePosition, FBSquareScale;
    public bool StimFacingCamera;
    public string ShadowType;
    public bool NeutralITI;
    public int InitialTokens_InBlock;
    
    //Player View Variables
    private PlayerViewPanel playerView;
    private Transform playerViewParent; // Helps set things onto the player view in the experimenter display
    private List<GameObject> playerViewTextList = new List<GameObject>();
    private GameObject playerViewText;
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
        State SearchDisplay = new State("SearchDisplay");
        State SelectionFeedback = new State("SelectionFeedback");
        State TokenFeedback = new State("TokenFeedback");
        State ITI = new State("ITI");
        State SearchDisplayDelay = new State("SearchDisplayDelay");
        State Delay = new State("Delay");
        
        AddActiveStates(new List<State> {InitTrial, SearchDisplay, SelectionFeedback, TokenFeedback, ITI, Delay, SearchDisplayDelay});
        
        // A state that just waits for some time
        State stateAfterDelay = null;
        float delayDuration = 0;
        Delay.AddTimer(() => delayDuration, () => stateAfterDelay);
        
        Text commandText = null;
        playerView = new PlayerViewPanel(); //GameObject.Find("PlayerViewCanvas").GetComponent<PlayerViewPanel>()
        playerViewText = new GameObject();
        taskHelper = new TaskHelperFunctions();
        SelectionHandler<VisualSearch_StimDef> mouseHandler = new SelectionHandler<VisualSearch_StimDef>();
        
        Add_ControlLevel_InitializationMethod(() =>
        {
            ResetTrialVariables();
            LoadTextures(ContextExternalFilePath);
            HaloFBController.SetHaloSize(5);
            if(playerViewText==null)
                CreateTextOnExperimenterDisplay();
            if(StartButton == null)
                StartButton = CreateSquare("StartButton", StartButtonTexture, ButtonPosition, ButtonScale);
            if(FBSquare == null)
                FBSquare = CreateSquare("FBSquare", FBSquareTexture, FBSquarePosition, FBSquareScale);
        });
        
        SetupTrial.AddInitializationMethod(() =>
        {
            //Set the context for the upcoming trial with the Start Button visible
            ContextName = CurrentTrialDef.ContextName;
            RenderSettings.skybox = CreateSkybox(ContextExternalFilePath + Path.DirectorySeparatorChar + ContextName + ".png");


            if (TrialCount_InBlock == 0)
            {
                playerViewText.SetActive(false);
            }
                
            
            if (!configUIVariablesLoaded) LoadConfigUIVariables();
            SetTrialSummaryString();
            CurrentTaskLevel.SetBlockSummaryString();
            TokenFBController.SetTokenBarFull(false);
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
                TouchDurationErrorFeedback(mouseHandler, StartButton);
                CurrentTaskLevel.SetBlockSummaryString();
            }
        });
        InitTrial.SpecifyTermination(() => mouseHandler.SelectionMatches(StartButton),
            SearchDisplayDelay, () => 
            { 
                // Turn off start button and set the token bar settings
                StartButton.SetActive(false);
                TokenFBController
                    .SetRevealTime(tokenRevealDuration.value)
                    .SetUpdateTime(tokenUpdateDuration.value);
                EventCodeManager.SendCodeImmediate(TaskEventCodes["StartButtonSelected"]);
                
                // Set Experimenter Display Data Summary Strings
                CurrentTaskLevel.SetBlockSummaryString();
                SetTrialSummaryString();
            });
        
        // Show the target/sample with some other distractors
        SearchDisplayDelay.AddTimer(() => searchDisplayDelay.value, SearchDisplay);
        // Wait for a click and provide feedback accordingly
        MouseTracker.AddSelectionHandler(mouseHandler, SearchDisplay);
        SearchDisplay.AddInitializationMethod(() =>
        {
            TokenFBController.enabled = true;
            tStim.ToggleVisibility(true);
            CreateTextOnExperimenterDisplay();
            if (StimFacingCamera)
            {
                foreach (var stim in tStim.stimDefs) stim.StimGameObject.AddComponent<FaceCamera>();
            }
            SetShadowType(ShadowType, "VisualSearch_DirectionalLight");
            
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
                SelectedStimCode = selectedSD.StimCode;
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

        SelectionFeedback.AddTimer(() => fbDuration.value, TokenFeedback,()=>
        {   
            HaloFBController.Destroy();
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["StimOff"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionVisualFbOff"]);
        });

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
                    NumRewardGiven_InBlock++;
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
            Debug.Log("SEARCH DURATION : " + SearchDuration);
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
        TrialSummaryString = "\n" +
                             "Trial Count in Block: " + (TrialCount_InBlock + 1) +
                             "\nTrial Count in Task: " + (TrialCount_InTask + 1) +
                             "\n" +
                             "\nSelected Object Code: " + SelectedStimCode +
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
        SelectedStimCode = null;
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
        TrialData.AddDatum("SelectedStimCode", () => selectedSD?.StimCode ?? null);
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
