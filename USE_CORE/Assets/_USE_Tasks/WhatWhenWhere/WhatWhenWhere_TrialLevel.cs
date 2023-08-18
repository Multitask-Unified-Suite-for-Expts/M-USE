
using System;
using System.Collections.Generic;
using UnityEngine;
using USE_States;
using WhatWhenWhere_Namespace;
using USE_StimulusManagement;
using ConfigDynamicUI;
using System.Linq;
using System.IO;
using USE_ExperimentTemplate_Trial;
using USE_ExperimentTemplate_Task;


public class WhatWhenWhere_TrialLevel : ControlLevel_Trial_Template
{
    public GameObject WWW_CanvasGO;

    //This variable is required for most tasks, and is defined as the output of the GetCurrentTrialDef function 
    public WhatWhenWhere_TrialDef CurrentTrialDef => GetCurrentTrialDef<WhatWhenWhere_TrialDef>();
    public WhatWhenWhere_TaskLevel CurrentTaskLevel => GetTaskLevel<WhatWhenWhere_TaskLevel>();
    public WhatWhenWhere_TaskDef currentTaskDef => GetTaskDef<WhatWhenWhere_TaskDef>();

    //stim group
    private StimGroup searchStims, distractorStims;
    private List<int> TouchedObjects = new List<int>();

    // feedback variables
    public int numTouchedStims = 0;
    private bool trialComplete = false;
    

    
    //Trial Data Logging variables
    private string errorTypeString = "";
    public int consecutiveError = 0;
    private List<float?> SearchDurations_InTrial = new List<float?> { };
    public List<int> runningAcc = new List<int>();

    [HideInInspector]
    public ConfigNumber flashingFbDuration;
    public ConfigNumber fbDuration;
    public ConfigNumber minObjectTouchDuration;
    public ConfigNumber maxObjectTouchDuration;
    public ConfigNumber selectObjectDuration;
    public ConfigNumber itiDuration;
    public ConfigNumber sliderSize;
    public ConfigNumber chooseStimOnsetDelay; 
    public ConfigNumber startButtonDelay;
    public ConfigNumber timeoutDuration;


    //data logging variables
    private string searchStimsLocations, distractorStimsLocations;
    
    private float searchDuration = 0;
    private float sbDelay = 0;
    
    // misc variables
    private bool variablesLoaded;
    private int correctIndex;
    public int NumSliderBarFilled = 0;
    private int sliderGainSteps, sliderLossSteps;
    private bool isSliderValueIncrease = false;
    

    //Player View Variables
    private PlayerViewPanel playerView;
    private Transform playerViewParent; // Helps set things onto the player view in the experimenter display
    public List<GameObject> playerViewTextList;
    public GameObject playerViewText;
    private Vector2 textLocation;
    private bool playerViewLoaded;

    // Stimuli Variables
    private GameObject StartButton;
    
    // Stim Evaluation Variables
    private GameObject selectedGO = null;
    private WhatWhenWhere_StimDef selectedSD = null;
    private bool CorrectSelection;
    private int? stimIdx; // used to index through the arrays in the config file/mapping different columns
    private bool choiceMade = false;

    private SelectionTracking.SelectionTracker.SelectionHandler ShotgunHandler;

    public override void DefineControlLevel()
    {
        //---------------------------------------DEFINING STATES-----------------------------------------------------------------------
        State InitTrial = new State("InitTrial");
        State ChooseStimulus = new State("ChooseStimulus");
        State ChooseStimulusDelay = new State("ChooseStimulusDelay");
        State SelectionFeedback = new State("SelectionFeedback");
        State FinalFeedback = new State("FinalFeedback");
        State ITI = new State("ITI");
        AddActiveStates(new List<State>
        {
            InitTrial, ChooseStimulus, SelectionFeedback, FinalFeedback, ITI, ChooseStimulusDelay
        });

        string[] stateNames = new string[]
            {"InitTrial", "ChooseStimulus", "ChooseStimulusDelay", "SelectionFeedback", "FinalFeedback", "ITI", "ChooseStimulusDelay"};

        Add_ControlLevel_InitializationMethod(() =>
        {
            SliderFBController.InitializeSlider();
            
            // Initialize FB Controller Values
            HaloFBController.SetHaloSize(12);
            HaloFBController.SetHaloIntensity(5);
            
            if (StartButton == null)
                InitializeStartButton(InitTrial, InitTrial);

            if (!SessionValues.WebBuild)
            {
                //player view variables
                playerView = new PlayerViewPanel(); //GameObject.Find("PlayerViewCanvas").GetComponent<PlayerViewPanel>()
                playerViewParent = GameObject.Find("MainCameraCopy").transform; // sets parent for any playerView elements on experimenter display
            }
        });

        SetupTrial.AddSpecificInitializationMethod(() =>
        {
            if (!variablesLoaded)
            {
                variablesLoaded = true;
                LoadConfigUiVariables();

            }

            //Set the Stimuli Light/Shadow settings
            SetShadowType(currentTaskDef.ShadowType, "WhatWhenWhere_DirectionalLight");
            if (currentTaskDef.StimFacingCamera)
                MakeStimFaceCamera();

            UpdateExperimenterDisplaySummaryStrings();

            // Determine Start Button onset if the participant has made consecutive errors that exceed the error threshold
            if (consecutiveError >= CurrentTrialDef.ErrorThreshold)
                sbDelay = timeoutDuration.value;
            else
                sbDelay = startButtonDelay.value;
        });

        SetupTrial.AddTimer(()=> sbDelay, InitTrial);

        ShotgunHandler = SessionValues.SelectionTracker.SetupSelectionHandler("trial", "TouchShotgun", SessionValues.MouseTracker, InitTrial, FinalFeedback);

        TouchFBController.EnableTouchFeedback(ShotgunHandler, currentTaskDef.TouchFeedbackDuration, currentTaskDef.StartButtonScale * 10, WWW_CanvasGO);

        InitTrial.AddSpecificInitializationMethod(() =>
        {
            Camera.main.gameObject.GetComponent<Skybox>().enabled = false; //Disable cam's skybox so the RenderSettings.Skybox can show the Context background

            InitializeShotgunHandler();
        });
        InitTrial.SpecifyTermination(() => ShotgunHandler.LastSuccessfulSelectionMatches(SessionValues.SessionDef.IsHuman ? SessionValues.HumanStartPanel.StartButtonChildren : SessionValues.USE_StartButton.StartButtonChildren), ChooseStimulusDelay, ()=>
        {
            CalculateSliderSteps();
            SliderFBController.ConfigureSlider(sliderSize.value, CurrentTrialDef.SliderInitial*(1f/sliderGainSteps));
            SliderFBController.SliderGO.SetActive(true);
            
            SessionValues.EventCodeManager.SendCodeImmediate("StartButtonSelected");
            SessionValues.EventCodeManager.SendCodeNextFrame("StimOn");
            SessionValues.EventCodeManager.SendCodeNextFrame("SliderFbController_SliderReset");
        });
        ChooseStimulusDelay.AddTimer(() => chooseStimOnsetDelay.value, ChooseStimulus);
        
        // Define ChooseStimulus state - Stimulus are shown and the user must select the correct object in the correct sequence
        ChooseStimulus.AddSpecificInitializationMethod(() =>
        {
            AssignCorrectStim();
            searchDuration = 0;

            if(!SessionValues.WebBuild)
            {
                if (GameObject.Find("MainCameraCopy").transform.childCount == 0)
                    CreateTextOnExperimenterDisplay();
            }

            choiceMade = false;
            if (CurrentTrialDef.LeaveFeedbackOn)
                HaloFBController.SetLeaveFeedbackOn();

            ShotgunHandler.HandlerActive = true;
            if (ShotgunHandler.AllSelections.Count > 0)
                ShotgunHandler.ClearSelections();
        });
        ChooseStimulus.AddUpdateMethod(() =>
        {
            searchDuration += Time.deltaTime;
            if (ShotgunHandler.SuccessfulSelections.Count > 0)
            {
                selectedGO = ShotgunHandler.LastSuccessfulSelection.SelectedGameObject;
                selectedSD = selectedGO?.GetComponent<StimDefPointer>()?.GetStimDef<WhatWhenWhere_StimDef>();
                ShotgunHandler.ClearSelections();
                if (selectedSD != null)
                    choiceMade = true;
            }
        });
        ChooseStimulus.SpecifyTermination(()=> choiceMade, SelectionFeedback, ()=>
        {
            UpdateExperimenterDisplaySummaryStrings();
            CorrectSelection = selectedSD.IsCurrentTarget;

            if (CorrectSelection)
            {
                // UpdateCounters_Correct();
                CurrentTaskLevel.NumCorrectSelections_InBlock++;
                isSliderValueIncrease = true;
                SessionValues.EventCodeManager.SendCodeImmediate("CorrectResponse");
            }
            else
            {
                runningAcc.Add(0);
                CurrentTaskLevel.NumErrors_InBlock++;
                //UpdateCounters_Incorrect(correctIndex);
                isSliderValueIncrease = false;
                SessionValues.EventCodeManager.SendCodeImmediate("IncorrectResponse");

                //Repetition Error
                if (TouchedObjects.Contains(selectedSD.StimIndex))
                {
                    CurrentTaskLevel.RepetitionErrorCount_InBlock++;
                    errorTypeString = "RepetitionError";
                    SessionValues.EventCodeManager.SendCodeImmediate(TaskEventCodes["RepetitionError"]);
                }
                // Slot Errors
                else
                {
                    //Distractor Error
                    if (selectedSD.IsDistractor)
                    {
                        CurrentTaskLevel.DistractorSlotErrorCount_InBlock++;
                        errorTypeString = "DistractorSlotError";
                        SessionValues.EventCodeManager.SendCodeImmediate("Button0PressedOnDistractorObject");//SELECTION STUFF (code may not be exact and/or could be moved to Selection handler)
                    }
                    //Stimuli Slot error
                    else
                    {
                        CurrentTaskLevel.SlotErrorCount_InBlock++;
                        errorTypeString = "SlotError";
                        SessionValues.EventCodeManager.SendCodeImmediate(TaskEventCodes["SlotError"]);
                    }
                }
            }
        });
        ChooseStimulus.AddTimer(() => selectObjectDuration.value, ITI, () =>
        {
            consecutiveError++;
            runningAcc.Add(0);
            SearchDurations_InTrial.Add(null);
            CurrentTaskLevel.SearchDurations_InBlock.Add(null);
            CurrentTaskLevel.SearchDurations_InTask.Add(null);
            errorTypeString = "AbortedTrial";
            AbortCode = 6;
        });
        // ChooseStimulus.SpecifyTermination(() => trialComplete, FinalFeedback);

        SelectionFeedback.AddSpecificInitializationMethod(() =>
        {
            ShotgunHandler.HandlerActive = false;
            TouchedObjects.Add(selectedSD.StimIndex);
            SearchDurations_InTrial.Add(searchDuration);
            CurrentTaskLevel.SearchDurations_InBlock.Add(searchDuration);
            CurrentTaskLevel.SearchDurations_InTask.Add(searchDuration);
           // totalFbDuration = (fbDuration.value + flashingFbDuration.value);
            SliderFBController.SetUpdateDuration(fbDuration.value);
            SliderFBController.SetFlashingDuration(flashingFbDuration.value);

            int? depth = SessionValues.Using2DStim ? 50 : (int?)null;

            if (CorrectSelection)
            {
                consecutiveError = 0;
                HaloFBController.ShowPositive(selectedGO, depth);
                SliderFBController.UpdateSliderValue(CurrentTrialDef.SliderGain[numTouchedStims]*(1f/sliderGainSteps));
                numTouchedStims += 1;
                if (numTouchedStims == CurrentTrialDef.CorrectObjectTouchOrder.Length)
                    trialComplete = true;
                
                errorTypeString = "None";
            }
            else //Chose Incorrect
            {
                consecutiveError++;
                HaloFBController.ShowNegative(selectedGO, depth);
                if (errorTypeString.Equals("DistractorSlotError"))
                    stimIdx = Array.IndexOf(CurrentTrialDef.DistractorStimsIndices, selectedSD.StimIndex); // used to index through the arrays in the config file/mapping different columns
                else
                    stimIdx = Array.IndexOf(CurrentTrialDef.SearchStimsIndices, selectedSD.StimIndex);

                SliderFBController.UpdateSliderValue(-CurrentTrialDef.SliderLoss[(int)stimIdx]*(1f/sliderLossSteps)); // NOT IMPLEMENTED: NEEDS TO CONSIDER SEPARATE LOSS/GAIN FOR DISTRACTOR & TARGET STIMS SEPARATELY
            }
            selectedGO = null;
        });
        
        //don't control timing with AddTimer, use slider class SliderUpdateFinished bool 
        SelectionFeedback.AddTimer(()=>fbDuration.value, Delay, () =>
        {
            DelayDuration = 0;
            
            if (!CurrentTrialDef.LeaveFeedbackOn) 
                HaloFBController.Destroy();
            
            UpdateExperimenterDisplaySummaryStrings();

            if (CorrectSelection)
            {
                if(trialComplete)
                    StateAfterDelay = FinalFeedback;
                else
                    StateAfterDelay = ChooseStimulus;
                CorrectSelection = false;
            }
            else 
                StateAfterDelay = ITI;
            
        });
        FinalFeedback.AddSpecificInitializationMethod(() =>
        {
            ShotgunHandler.HandlerActive = false;

            trialComplete = false;
            errorTypeString = "None";

            //Destroy all created text objects on Player View of Experimenter Display
            if(!SessionValues.WebBuild)
                DestroyChildren(GameObject.Find("MainCameraCopy"));

            runningAcc.Add(1);
            NumSliderBarFilled += 1;
            CurrentTaskLevel.NumSliderBarFilled_InTask++;

            SessionValues.EventCodeManager.SendCodeNextFrame("SliderFbController_SliderCompleteFbOn");
            SessionValues.EventCodeManager.SendCodeNextFrame("StimOff");
                        
            if (SessionValues.SyncBoxController != null)
            {
                SessionValues.SyncBoxController.SendRewardPulses(CurrentTrialDef.NumPulses, CurrentTrialDef.PulseSize); 
               // SessionInfoPanel.UpdateSessionSummaryValues(("totalRewardPulses",CurrentTrialDef.NumPulses)); //moved to syncbox class
                CurrentTaskLevel.NumRewardPulses_InBlock += CurrentTrialDef.NumPulses;
                CurrentTaskLevel.NumRewardPulses_InTask += CurrentTrialDef.NumPulses;
            }
           
        });
        FinalFeedback.AddTimer(() => flashingFbDuration.value, ITI, () =>
        {
            SessionValues.EventCodeManager.SendCodeImmediate("SliderFbController_SliderCompleteFbOff");
            SessionValues.EventCodeManager.SendCodeNextFrame("ContextOff");
            
            CurrentTaskLevel.SetBlockSummaryString();
        });

        //Define iti state
        ITI.AddSpecificInitializationMethod(() =>
        {
            float latestAccuracy;

            if (runningAcc.Count > 10)
            {
                latestAccuracy = ((runningAcc.Skip(Math.Max(0, runningAcc.Count - 10)).Sum() / 10f)*100);
                if (latestAccuracy > 70 && CurrentTaskLevel.LearningSpeed == -1)
                    CurrentTaskLevel.LearningSpeed = TrialCount_InBlock + 1;
            }
            
            if (currentTaskDef.NeutralITI)
            {
                string path = !string.IsNullOrEmpty(currentTaskDef.ContextExternalFilePath) ? currentTaskDef.ContextExternalFilePath : SessionValues.SessionDef.ContextExternalFilePath;
                CurrentTaskLevel.SetSkyBox(path + Path.DirectorySeparatorChar + "NeutralITI" + ".png");
            }

            // GenerateAccuracyLog();
        });
        ITI.AddTimer(() => itiDuration.value, FinishTrial);
        //------------------------------------------------------------------------ADDING VALUES TO DATA FILE--------------------------------------------------------------------------------------------------------------------------------------------------------------

        DefineTrialData();
        DefineFrameData();
    }

    protected override bool CheckBlockEnd()
    {
        TaskLevelTemplate_Methods TaskLevel_Methods = new TaskLevelTemplate_Methods();
        return TaskLevel_Methods.CheckBlockEnd(CurrentTrialDef.BlockEndType, runningAcc,
            CurrentTrialDef.BlockEndThreshold, CurrentTrialDef.BlockEndWindow, CurrentTrialDef.BlockEndWindow,
            CurrentTrialDef.MaxTrials);
    }
    public override void FinishTrialCleanup()
    {
        if(!SessionValues.WebBuild)
        {
            if (playerViewParent.transform.childCount != 0)
                DestroyChildren(GameObject.Find("MainCameraCopy"));
        }

        searchStims.ToggleVisibility(false);
        distractorStims.ToggleVisibility(false);
        SliderFBController.SliderGO.SetActive(false);
        SliderFBController.SliderHaloGO.SetActive(false);

        if(AbortCode == 0)
            CurrentTaskLevel.SetBlockSummaryString();
        else
        {
            CurrentTaskLevel.NumAbortedTrials_InBlock++;
            CurrentTaskLevel.NumAbortedTrials_InTask++;
        }
            
    }

    public void MakeStimFaceCamera()
    {
        foreach (StimGroup group in TrialStims)
        foreach (var stim in group.stimDefs)
        {
            stim.StimGameObject.transform.LookAt(Camera.main.transform);
        }
    }

    public override void ResetTrialVariables()
    {
        numTouchedStims = 0;
        searchDuration = 0;
        sliderGainSteps = 0;
        sliderLossSteps = 0;
        stimIdx = null;
        selectedGO = null;
        selectedSD = null;
        CorrectSelection = false;
        choiceMade = false;
        
        SearchDurations_InTrial.Clear();
        TouchedObjects.Clear();
        errorTypeString = "";
        SliderFBController.ResetSliderBarFull();
    }

    


    //-----------------------------------------------------------------METHODS FOR DATA HANDLING----------------------------------------------------------------------
    private void DefineTrialData() //All ".AddDatum" commands for Trial Data
    {
        TrialData.AddDatum("TrialID", () => CurrentTrialDef.TrialID); //NaN if only using blockdef structure
        TrialData.AddDatum("Context", () => CurrentTrialDef.ContextName);
        TrialData.AddDatum("SearchStimsLocations", () => searchStimsLocations);
        TrialData.AddDatum("DistractorStimsLocations", () => distractorStimsLocations);
        TrialData.AddDatum("TouchedObjects", () => String.Join(",",TouchedObjects));
        TrialData.AddDatum("SearchDurations", () => String.Join(",",SearchDurations_InTrial));
        TrialData.AddDatum("ErrorType", () => errorTypeString);
    }
    private void DefineFrameData() //All ".AddDatum" commands for Frame Data
    {
        FrameData.AddDatum("StartButton", () => StartButton.activeSelf);
        FrameData.AddDatum("SearchStimuliShown", () => searchStims.IsActive);
        FrameData.AddDatum("DistractorStimuliShown", () => distractorStims.IsActive);
    }

    private void SetTrialSummaryString()
    {
        TrialSummaryString = "Selected Object Indices: " + string.Join(",",TouchedObjects) +
                             "\nCorrect Selection? : " + CorrectSelection +
                             "\n" +
                             "\nError: " + errorTypeString +
                             "\n" +
                             "\nAvg Search Duration: " + CurrentTaskLevel.CalculateAverageDuration(SearchDurations_InTrial);
    }

    
    private void CreateTextOnExperimenterDisplay()
    {
        for (int iStim = 0; iStim < CurrentTrialDef.CorrectObjectTouchOrder.Length; ++iStim)
        {
            //Create corresponding text on player view of experimenter display
            textLocation = ScreenToPlayerViewPosition(Camera.main.WorldToScreenPoint(searchStims.stimDefs[iStim].StimLocation), playerViewParent);
            textLocation.y += 75;
            playerViewText = playerView.CreateTextObject(CurrentTrialDef.CorrectObjectTouchOrder[iStim].ToString(),
                CurrentTrialDef.CorrectObjectTouchOrder[iStim].ToString(),
                Color.red, textLocation, new Vector2(200, 200), playerViewParent);
            playerViewText.SetActive(true);
            playerViewText.GetComponent<RectTransform>().localScale = new Vector3(2, 2, 0);
        }
    }
    void LoadConfigUiVariables()
    {
        //config UI variables
        minObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("minObjectTouchDuration");
        maxObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("maxObjectTouchDuration");
        itiDuration = ConfigUiVariables.get<ConfigNumber>("itiDuration");
        sliderSize = ConfigUiVariables.get<ConfigNumber>("sliderSize");
        selectObjectDuration = ConfigUiVariables.get<ConfigNumber>("selectObjectDuration");
        flashingFbDuration = ConfigUiVariables.get<ConfigNumber>("finalFbDuration");
        fbDuration = ConfigUiVariables.get<ConfigNumber>("fbDuration");
        chooseStimOnsetDelay = ConfigUiVariables.get<ConfigNumber>("chooseStimOnsetDelay");
        timeoutDuration = ConfigUiVariables.get<ConfigNumber>("timeoutDuration");
        startButtonDelay = ConfigUiVariables.get<ConfigNumber>("startButtonDelay");
    }
    //-----------------------------------------------------DEFINE QUADDLES-------------------------------------------------------------------------------------
    protected override void DefineTrialStims()
    {
        StimGroup group = SessionValues.UsingDefaultConfigs ? PrefabStims : ExternalStims;

        //Define StimGroups consisting of StimDefs whose gameobjects will be loaded at TrialLevel_SetupTrial and 
        //destroyed at TrialLevel_Finish
        //StimGroup constructor which creates a subset of an already-existing StimGroup 
        searchStims = new StimGroup("SearchStims", group, CurrentTrialDef.SearchStimsIndices);
        distractorStims = new StimGroup("DistractorStims", group, CurrentTrialDef.DistractorStimsIndices);
       
        searchStims.SetVisibilityOnOffStates(GetStateFromName("ChooseStimulus"), GetStateFromName("ITI"));
        distractorStims.SetVisibilityOnOffStates(GetStateFromName("ChooseStimulus"), GetStateFromName("ITI"));

        TrialStims.Add(searchStims);
        TrialStims.Add(distractorStims);
        
        if (CurrentTrialDef.RandomizedLocations)
        {
            var totalStims = searchStims.stimDefs.Concat(distractorStims.stimDefs);
            var stimLocations = CurrentTrialDef.SearchStimsLocations.Concat(CurrentTrialDef.DistractorStimsLocations);

            int[] positionIndexArray = Enumerable.Range(0, stimLocations.Count()).ToArray();
            System.Random random = new System.Random();
            positionIndexArray = positionIndexArray.OrderBy(x => random.Next()).ToArray();

            for (int i = 0; i < totalStims.Count(); i++)
            {
                totalStims.ElementAt(i).StimLocation = stimLocations.ElementAt(positionIndexArray[i]);
            }
        }
        else
        {
            searchStims.SetLocations(CurrentTrialDef.SearchStimsLocations);
            distractorStims.SetLocations(CurrentTrialDef.DistractorStimsLocations);
        }

        searchStimsLocations = String.Join(",", searchStims.stimDefs.Select(s => s.StimLocation));
        distractorStimsLocations = String.Join(",", distractorStims.stimDefs.Select(d => d.StimLocation));
    }
    
    //-------------------------------------------------------------MISCELLANEOUS METHODS--------------------------------------------------------------------------
    private void AssignCorrectStim()
    {
        //if we haven't finished touching all stims
        if (numTouchedStims < CurrentTrialDef.CorrectObjectTouchOrder.Length)
        {
            //find which stimulus is currently target
            correctIndex = CurrentTrialDef.CorrectObjectTouchOrder[numTouchedStims] - 1;
        
            for (int iStim = 0; iStim < CurrentTrialDef.CorrectObjectTouchOrder.Length; iStim++)
            {
                WhatWhenWhere_StimDef sd = (WhatWhenWhere_StimDef) searchStims.stimDefs[iStim];
                if (iStim == correctIndex) sd.IsCurrentTarget = true;
                else sd.IsCurrentTarget = false;
            }
        
            for (int iDist = 0; iDist < CurrentTrialDef.DistractorStimsIndices.Length; ++iDist)
            {
                WhatWhenWhere_StimDef sd = (WhatWhenWhere_StimDef) distractorStims.stimDefs[iDist];
                sd.IsDistractor = true;
            }
        }
    }

    private void CalculateSliderSteps()
    {
        //Configure the Slider Steps for each Stim
        foreach (int sliderGain in CurrentTrialDef.SliderGain)
        {
            sliderGainSteps += sliderGain;
        }
        sliderGainSteps += CurrentTrialDef.SliderInitial;
        foreach (int sliderLoss in CurrentTrialDef.SliderLoss)
        {
            sliderLossSteps += sliderLoss;
        }
        sliderLossSteps += CurrentTrialDef.SliderInitial;
    }

    private void InitializeStartButton(State visOnState, State visOffState)
    {
        if (SessionValues.SessionDef.IsHuman)
        {
            StartButton = SessionValues.HumanStartPanel.StartButtonGO;
            SessionValues.HumanStartPanel.SetVisibilityOnOffStates(visOnState, visOffState);
        }
        else
        {
            StartButton = SessionValues.USE_StartButton.CreateStartButton(WWW_CanvasGO.GetComponent<Canvas>(), currentTaskDef.StartButtonPosition, currentTaskDef.StartButtonScale);
            SessionValues.USE_StartButton.SetVisibilityOnOffStates(visOnState, visOffState);
        }
    }

    private void InitializeShotgunHandler()
    {
        ShotgunHandler.HandlerActive = true;
        if (ShotgunHandler.AllSelections.Count > 0)
            ShotgunHandler.ClearSelections();
        ShotgunHandler.MinDuration = minObjectTouchDuration.value;
        ShotgunHandler.MaxDuration = maxObjectTouchDuration.value;
        ShotgunHandler.MaxPixelDisplacement = 50;
    }
    private void UpdateExperimenterDisplaySummaryStrings()
    {
        CurrentTaskLevel.SetBlockSummaryString();
        SetTrialSummaryString();
        if (TrialCount_InTask != 0)
            CurrentTaskLevel.SetTaskSummaryString();
    }
}














