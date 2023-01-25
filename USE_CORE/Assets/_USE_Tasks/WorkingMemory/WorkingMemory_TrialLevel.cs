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

public class WorkingMemory_TrialLevel : ControlLevel_Trial_Template
{
    public WorkingMemory_TrialDef CurrentTrialDef => GetCurrentTrialDef<WorkingMemory_TrialDef>();
    public WorkingMemory_TaskLevel CurrentTaskLevel => GetTaskLevel<WorkingMemory_TaskLevel>();
    // Block End Variables
    public List<int> runningAcc;
    public int MinTrials, MaxTrials;
       
    //REFACTOR VARIABLES 
    private StimGroup searchStims, targetStim, postSampleDistractorStims; // targetDistractorStims, sampleStims;
    private GameObject startButton;
    
    // Stim Evaluation Variables
    private GameObject trialStim;
    private GameObject selected = null;
    private bool CorrectSelection;
    WorkingMemory_StimDef selectedSD = null;
    
    // Stimuli Variables
    private StimGroup tStim;
    private GameObject StartButton;
    private GameObject FBSquare;
    private GameObject SquareGO;
    public Texture2D HeldTooShortTexture;
    public Texture2D HeldTooLongTexture;
    private Texture2D StartButtonTexture;
    private Texture2D FBSquareTexture;
    private bool Grating = false;
    private TaskHelperFunctions taskHelper;

    
    //configUI variables
    [HideInInspector]
    public ConfigNumber minObjectTouchDuration, maxObjectTouchDuration, gratingSquareDuration, tokenRevealDuration, tokenUpdateDuration, trialEndDuration, initTrialDuration, baselineDuration, 
        maxSearchDuration, selectionFbDuration, displaySampleDuration, postSampleDelayDuration, 
        displayPostSampleDistractorsDuration, preTargetDelayDuration, itiDuration;
    
    // Config Loading Variables
    private bool configUIVariablesLoaded = false;
    public string ContextExternalFilePath;
    public Vector3 ButtonPosition, ButtonScale;
    public Vector3 FBSquarePosition, FBSquareScale;
    public bool StimFacingCamera;
    public string ShadowType;
    
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
    public decimal Accuracy_InBlock;
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
        State DisplayPostSampleDistractors = new State("DisplayPostSampleDistractors");
        State SearchDisplay = new State("SearchDisplay");
        State SelectionFeedback = new State("SelectionFeedback");
        State TokenFeedback = new State("TokenFeedback");
        State ITI = new State("ITI");
        State Delay = new State("Delay");

        AddActiveStates(new List<State> { InitTrial, Delay, DisplaySample, DisplayPostSampleDistractors, SearchDisplay, SelectionFeedback, TokenFeedback, ITI });

        // A state that just waits for some time
        State stateAfterDelay = null;
        float delayDuration = 0;
        Delay.AddTimer(() => delayDuration, () => stateAfterDelay);
        
        Text commandText = null;
        playerView = new PlayerViewPanel(); //GameObject.Find("PlayerViewCanvas").GetComponent<PlayerViewPanel>()
        playerViewText = new GameObject();
        taskHelper = new TaskHelperFunctions();
        SelectionHandler<WorkingMemory_StimDef> mouseHandler = new SelectionHandler<WorkingMemory_StimDef>();

        Add_ControlLevel_InitializationMethod(() =>
        {
            taskHelper.LoadTextures(ContextExternalFilePath);
            HaloFBController.SetHaloSize(5);
            StartButton = taskHelper.CreateStartButton(taskHelper.StartButtonTexture, ButtonPosition, ButtonScale);
            FBSquare = taskHelper.CreateFBSquare(taskHelper.FBSquareTexture, FBSquarePosition, FBSquareScale);
        });
        SetupTrial.AddInitializationMethod(() =>
        {
            if (!configUIVariablesLoaded) LoadConfigUIVariables();
            SetTrialSummaryString();
            CurrentTaskLevel.SetBlockSummaryString();
            TokenFBController.SetTokenBarFull(false);
        });

        SetupTrial.SpecifyTermination(() => true, InitTrial);
        MouseTracker.AddSelectionHandler(mouseHandler, InitTrial);
        InitTrial.AddInitializationMethod(() =>
        {
            ContextName = CurrentTrialDef.ContextName;
            RenderSettings.skybox = CreateSkybox(ContextExternalFilePath + Path.DirectorySeparatorChar +  ContextName + ".png");
            StartButton.SetActive(true);
            mouseHandler.SetMinTouchDuration(minObjectTouchDuration.value);
            mouseHandler.SetMaxTouchDuration(maxObjectTouchDuration.value);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["TrlStart"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["ContextOn"]);
        });
        InitTrial.SpecifyTermination(() => mouseHandler.SelectionMatches(StartButton),
            DisplaySample, () => {
                // Turn off start button and set the token bar settings
                StartButton.SetActive(false);
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
              stateAfterDelay = DisplayPostSampleDistractors;
              delayDuration = postSampleDelayDuration.value;
          });
        // Show some distractors without the target/sample
        DisplayPostSampleDistractors.AddTimer(() => displayPostSampleDistractorsDuration.value, Delay, () =>
          {
              stateAfterDelay = SearchDisplay;
              delayDuration = preTargetDelayDuration.value;
          });

        // Show the target/sample with some other distractors
        // Wait for a click and provide feedback accordingly
        MouseTracker.AddSelectionHandler(mouseHandler, SearchDisplay);
        SearchDisplay.AddInitializationMethod(() =>
        {
            TokenFBController.enabled = true;
            CreateTextOnExperimenterDisplay();
            if (StimFacingCamera)
            {
                foreach (var stim in tStim.stimDefs) stim.StimGameObject.AddComponent<FaceCamera>();
            }
            taskHelper.SetShadowType(ShadowType, "FlexLearning_DirectionalLight");
            
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["StimOn"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["TokenBarVisible"]);
        
        });
        SearchDisplay.SpecifyTermination(() => mouseHandler.SelectedStimDef != null, SelectionFeedback, () => {
            selected = mouseHandler.SelectedGameObject;
            selectedSD = mouseHandler.SelectedStimDef;
            CorrectSelection = selectedSD.IsTarget;
        });
        SearchDisplay.AddTimer(() => maxSearchDuration.value, FinishTrial);

        SelectionFeedback.AddInitializationMethod(() =>
        {
            if (!selected) return;
            else
            {//CHECK THIS
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionVisualFbOn"]);
            }
            //if (correct)
            {
                HaloFBController.ShowPositive(selected);
                runningAcc.Add(1);
            }
          //  else
            {
                HaloFBController.ShowNegative(selected);
                runningAcc.Add(0);
            };
        });
        SelectionFeedback.AddTimer(() => selectionFbDuration.value, TokenFeedback, () => 
        {
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["StimOff"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionVisualFbOff"]);
            TrialSummaryString = "Trial Num: " + (TrialCount_InTask + 1) + "\nBlock Accuracy: " + 
                                 (runningAcc.Sum(x => Convert.ToSingle(x))/(TrialCount_InBlock+1)) +
                                 "\nToken Bar Value: " +  TokenFBController.GetTokenBarValue();
        });


        // The state that will handle the token feedback and wait for any animations
        TokenFeedback.AddInitializationMethod(() =>
        {
            HaloFBController.Destroy();
            if (selectedSD.StimTrialRewardMag == 0)
            {
                //if (correct) AudioFBController.Play("Positive");
                //else AudioFBController.Play("Negative");
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionAuditoryFbOn"]);
                return;
            }
            if (selectedSD.StimTrialRewardMag > 0)
            {
                TokenFBController.AddTokens(selected, selectedSD.StimTrialRewardMag);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["Rewarded"]);
            }
            else
            {
                TokenFBController.RemoveTokens(selected, -selectedSD.StimTrialRewardMag);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["Unrewarded"]);
            }
        });
        TokenFeedback.SpecifyTermination(() => !TokenFBController.IsAnimating(), ITI);

        // Wait for some time at the end
        ITI.AddTimer(() => itiDuration.value, FinishTrial);

        TrialData.AddDatum("SelectedName", () => selected != null ? selected.name : null);
        TrialData.AddDatum("SelectedLocation", () => selectedSD?.StimLocation ?? null);
        //TrialData.AddDatum("SelectionCorrect", () => correct ? 1 : 0);
    }

    protected override void DefineTrialStims()
    {
        //Define StimGroups consisting of StimDefs whose gameobjects will be loaded at TrialLevel_SetupTrial and 
        //destroyed at TrialLevel_Finish

        searchStims = new StimGroup("SearchStims", ExternalStims, CurrentTrialDef.SearchStimIndices);
        searchStims.SetVisibilityOnOffStates(GetStateFromName("SearchDisplay"), GetStateFromName("TokenFeedback"));
        searchStims.SetLocations(CurrentTrialDef.SearchStimLocations);

        List<StimDef> rewardedStimdefs = new List<StimDef>();

        targetStim = new StimGroup("TargetStim", GetStateFromName("DisplaySample"), GetStateFromName("DisplaySample"));
        for (int iStim = 0; iStim < CurrentTrialDef.SearchStimIndices.Length; iStim++)
        {
            WorkingMemory_StimDef sd = (WorkingMemory_StimDef)searchStims.stimDefs[iStim];
            sd.StimTrialRewardMag = ChooseTokenReward(CurrentTrialDef.SearchStimTokenReward[iStim]);
            if (sd.StimTrialRewardMag > 0)
            {
                // StimDef tempsd = sd.CopyStimDef();
                WorkingMemory_StimDef newTarg = sd.CopyStimDef<WorkingMemory_StimDef>() as WorkingMemory_StimDef;
                targetStim.AddStims(newTarg);
                newTarg.IsTarget = true;//Holds true if the target stim receives non-zero reward
                // targetStim = new StimGroup("TargetStim", ExternalStims, new int[] {CurrentTrialDef.SearchStimIndices[iStim]});
                // targetStim.SetVisibilityOnOffStates(GetStateFromName("DisplaySample"), GetStateFromName("DisplaySample"));
                // targetStim.SetLocations(CurrentTrialDef.TargetSampleLocation);
            } 
            else sd.IsTarget = false;
        }
        
        // for (int iT)
        targetStim.SetLocations(CurrentTrialDef.TargetSampleLocation);
        targetStim.SetVisibilityOnOffStates(GetStateFromName("DisplaySample"), GetStateFromName("DisplaySample"));
        TrialStims.Add(searchStims);
        TrialStims.Add(targetStim);

        postSampleDistractorStims = new StimGroup("PostSampleDistractor", ExternalStims, CurrentTrialDef.PostSampleDistractorIndices);
        postSampleDistractorStims.SetVisibilityOnOffStates(GetStateFromName("DisplayPostSampleDistractors"), GetStateFromName("DisplayPostSampleDistractors"));
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
        maxSearchDuration = ConfigUiVariables.get<ConfigNumber>("maxSearchDuration");
        selectionFbDuration = ConfigUiVariables.get<ConfigNumber>("selectionFbDuration");
        displaySampleDuration = ConfigUiVariables.get<ConfigNumber>("displaySampleDuration");
        postSampleDelayDuration = ConfigUiVariables.get<ConfigNumber>("postSampleDelayDuration");
        displayPostSampleDistractorsDuration = ConfigUiVariables.get<ConfigNumber>("displayPostSampleDistractorsDuration");
        preTargetDelayDuration = ConfigUiVariables.get<ConfigNumber>("preTargetDelayDuration");
        itiDuration = ConfigUiVariables.get<ConfigNumber>("itiDuration");
        gratingSquareDuration = ConfigUiVariables.get<ConfigNumber>("gratingSquareDuration");
        configUIVariablesLoaded = true;
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
    private void SetShadowType()
    {
        //User options are None, Soft, Hard
        switch (ShadowType)
        {
            case "None":
                GameObject.Find("Directional Light").GetComponent<Light>().shadows = LightShadows.None;
                GameObject.Find("WorkingMemory_DirectionalLight").GetComponent<Light>().shadows = LightShadows.None;
                break;
            case "Soft":
                GameObject.Find("Directional Light").GetComponent<Light>().shadows = LightShadows.Soft;
                GameObject.Find("WorkingMemory_DirectionalLight").GetComponent<Light>().shadows = LightShadows.Soft;
                break;
            case "Hard":
                GameObject.Find("Directional Light").GetComponent<Light>().shadows = LightShadows.Hard;
                GameObject.Find("WorkingMemory_DirectionalLight").GetComponent<Light>().shadows = LightShadows.Hard;
                break;
            default:
                Debug.Log("User did not Input None, Soft, or Hard for the Shadow Type");
                break;
        }
    }
    void SetTrialSummaryString()
    {
        TrialSummaryString = "\n" +
                             "Trial Count in Block: " + (TrialCount_InBlock + 1) +
                             "\nTrial Count in Task: " + (TrialCount_InTask + 1) +
                             "\n" +
                            // "\nSelected Object Code: " + SelectedStimCode +
                             //"\nSelected Object Location: " + SelectedStimLocation +
                             //"\nCorrect Selection?: " + CorrectSelection +
                             //"\nTouch Duration Error?: " + TouchDurationError +
                             //"\n" +
                             //"\nSearch Duration: " + SearchDuration +
                             "\n" + 
                             "\nToken Bar Value: " + TokenFBController.GetTokenBarValue();
    }
    private void CreateTextOnExperimenterDisplay()
    {
        playerViewParent = GameObject.Find("MainCameraCopy").transform; // sets parent for any playerView elements on experimenter display
        if (!playerViewLoaded)
        {
            //Create corresponding text on player view of experimenter display
            foreach (WorkingMemory_StimDef stim in tStim.stimDefs)
            {
                if (stim.IsTarget)
                {
                    textLocation =
                        taskHelper.playerViewPosition(Camera.main.WorldToScreenPoint(stim.StimLocation),
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
}
