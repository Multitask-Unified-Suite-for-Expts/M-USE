using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using USE_States;
using WhatWhenWhere_Namespace;
using USE_StimulusManagement;
using ConfigDynamicUI;
using USE_Settings;
using USE_DisplayManagement;
using System.Linq;
using System.IO;
using UnityEngine.AI;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Serialization;
using USE_ExperimentTemplate_Trial;
using USE_ExperimentTemplate_Task;
using USE_UI;
using USE_Utilities;

public class WhatWhenWhere_TrialLevel : ControlLevel_Trial_Template
{
    public GameObject WWW_CanvasGO;
    public USE_StartButton USE_StartButton;
    public USE_StartButton USE_FBSquare;

    //This variable is required for most tasks, and is defined as the output of the GetCurrentTrialDef function 
    public WhatWhenWhere_TrialDef CurrentTrialDef => GetCurrentTrialDef<WhatWhenWhere_TrialDef>();

    public WhatWhenWhere_TaskLevel CurrentTaskLevel => GetTaskLevel<WhatWhenWhere_TaskLevel>();
    // game object variables
    private Texture2D texture;
    private static int numObjMax = 100;// need to change if stimulus exceeds this amount, not great
    
    // Config Variables
    public string ContextExternalFilePath;
    public Vector3 ButtonPosition;
    public float ButtonScale;
    public Vector3 FBSquarePosition;
    public float FBSquareScale;
    public bool StimFacingCamera;
    public string ShadowType;
    public bool NeutralITI;
    //stim group
    private StimGroup searchStims, distractorStims;
    private List<int> touchedObjects = new List<int>();
    private bool randomizedLocations = false;

    // feedback variables
    public int numTouchedStims = 0;
    private bool noSelection, trialComplete = false;
    
    //Block Data Logging Variables
    public List<float> searchDurations_InBlock;
    public int numRewardGiven_InBlock;
    public int touchDurationErrorCount_InBlock;
    public int repetitionErrorCount_InBlock;
    public int AbortedTrials_InBlock;
    public int slotErrorCount_InBlock;
    public int distractorSlotErrorCount_InBlock;
    public int numNonStimSelections_InBlock;
    public List<String> ErrorType_InBlock = new List<String> { };
    public int[] numTotal_InBlock = new int[numObjMax];
    public int[] numErrors_InBlock = new int[numObjMax];
    public int[] numCorrect_InBlock = new int[numObjMax];
    
    //Trial Data Logging variables
    private string errorTypeString = "";
    string trialProgress = "";
    private int consecutiveError = 0;
    private float startTime;
    
    public int[] numTotal_InTrial = new int[numObjMax];
    public int[] numErrors_InTrial = new int[numObjMax];
    public int[] numCorrect_InTrial = new int[numObjMax];
    //private List<float> touchDurations  = new List<float> { };
    private List<float> searchDurations = new List<float> { };
    //private List<Vector3> touchedPositionsList = new List<Vector3>(); // empty now
    private List<int> runningAcc = new List<int>();

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
    public ConfigNumber gratingSquareDuration;


    //data logging variables
    private string touchedObjectsCodes, touchDurationTimes, searchDurationTimes, touchedPositions, searchStimsLocations, distractorStimsLocations;
    public string accuracyLog_InSession, accuracyLog_InBlock, accuracyLog_InTrial = "";
    
    private float touchDuration, searchDuration, sbDelay = 0;
    private bool halosDestroyed, slotError, distractorSlotError, touchDurationError, repetitionError, aborted = false;
    public string ContextName = "";
   // private List<int> trialPerformance = new List<int>();
    private int timeoutCondition = 3;
    private float totalFbDuration;

    
    // vector3 variables
    private Vector3 trialStimInitLocalScale, fbInitLocalScale, sliderInitPosition, touchPosition;

    // misc variables
    private Ray mouseRay;
    private Camera cam;
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
    
    //Syncbox variables
    private bool usingSonication = false;
    public int MinTrials;

    // Stimuli Variables
    private GameObject startButton;
    private GameObject fbSquare;
    private bool Grating = false;
    
    // Stim Evaluation Variables
    private GameObject trialStim;
    private GameObject selectedGO = null;
    private bool CorrectSelection;
    private WhatWhenWhere_StimDef selectedSD = null;
    private int? stimIdx; // used to index through the arrays in the config file/mapping different columns
    private Ray ray;
    private RaycastHit hit;
    private bool HeldTooShort;
    private bool HeldTooLong;
    private float gratingDuration;
    private float? selectionDuration = null;
    private bool choiceMade = false;
    
    public override void DefineControlLevel()
    {
        // --------------------------------------ADDING PLAYER VIEW STUFF------------------------------------------------------------------------------------

        //MonitorDetails primaryMonitorDetails = new MonitorDetails(new Vector2(1920, 1080), new Vector2(10, 7), 2);

        //---------------------------------------DEFINING STATES-----------------------------------------------------------------------
        State InitTrial = new State("InitTrial");
        //State StartButtonDelay = new State("StartButtonDelay");
        State ChooseStimulus = new State("ChooseStimulus");
        State ChooseStimulusDelay = new State("ChooseStimulusDelay");
        State SelectionFeedback = new State("SelectionFeedback");
        State FinalFeedback = new State("FinalFeedback");
        State ITI = new State("ITI");
        AddActiveStates(new List<State>
        {
            InitTrial, ChooseStimulus, SelectionFeedback, FinalFeedback, ITI,
            ChooseStimulusDelay
        });

        string[] stateNames = new string[]
            {"InitTrial", "StartButtonDelay", "ChooseStimulus", "SelectionFeedback", "FinalFeedback", "ITI", "ChooseStimulusDelay"};

        //MouseTracker variables
        SelectionHandler<WhatWhenWhere_StimDef> gazeHandler = new SelectionHandler<WhatWhenWhere_StimDef>();
        SelectionHandler<WhatWhenWhere_StimDef> mouseHandler = new SelectionHandler<WhatWhenWhere_StimDef>();
        GazeTracker.SpoofGazeWithMouse = true;

        //player view variables
        
        playerView = new PlayerViewPanel(); //GameObject.Find("PlayerViewCanvas").GetComponent<PlayerViewPanel>()
        playerViewText = new GameObject();
        //EventCodeManager.SendCodeImmediate(3);
        //Trial Completion Feedback Variables
        
        Add_ControlLevel_InitializationMethod(() =>
        {
            SliderFBController.InitializeSlider();
            LoadTextures(ContextExternalFilePath);
// Initialize FB Controller Values
            HaloFBController.SetHaloSize(5f);
            HaloFBController.SetHaloIntensity(5);
            if (startButton == null)
            {
                USE_StartButton = new USE_StartButton(WWW_CanvasGO.GetComponent<Canvas>());
                startButton = USE_StartButton.StartButtonGO;
                USE_StartButton.SetVisibilityOnOffStates(InitTrial, InitTrial);
            }
            if (fbSquare == null)
            {
                USE_FBSquare = new USE_StartButton(WWW_CanvasGO.GetComponent<Canvas>(), ButtonPosition, ButtonScale);
                fbSquare = USE_FBSquare.StartButtonGO;
                fbSquare.name = "FBSquare";
            }

            playerViewParent = GameObject.Find("MainCameraCopy").transform; // sets parent for any playerView elements on experimenter display
        });

        SetupTrial.AddInitializationMethod(() =>
        {
            if (!variablesLoaded)
            {
                variablesLoaded = true;
                LoadConfigUiVariables();
            }
            //Set the Stimuli Light/Shadow settings
            SetShadowType(ShadowType, "WhatWhenWhere_DirectionalLight");
            if (StimFacingCamera)
            {
                MakeStimFaceCamera();
            }
            
            if (consecutiveError >= CurrentTrialDef.ErrorThreshold)
            {
                sbDelay = timeoutDuration.value;
            }
            else
            {
                sbDelay = startButtonDelay.value;
            }
        });
        SetupTrial.AddTimer(()=> sbDelay, InitTrial);
        MouseTracker.AddSelectionHandler(mouseHandler, InitTrial, null, 
            ()=> MouseTracker.ButtonStatus[0] == 1, ()=> MouseTracker.ButtonStatus[0] == 0);        // define StartButton state
        InitTrial.AddInitializationMethod(() =>
        {
            CurrentTaskLevel.SetBlockSummaryString();
            SetTrialSummaryString();
            if (TrialCount_InTask != 0)
                CurrentTaskLevel.SetTaskSummaryString();
        });

        InitTrial.SpecifyTermination(() => mouseHandler.SelectionMatches(startButton), ChooseStimulusDelay, ()=>
        {
            CalculateSliderSteps();
            SliderFBController.ConfigureSlider(new Vector3(0,180,0), sliderSize.value, CurrentTrialDef.SliderInitial*(1f/sliderGainSteps));
            SliderFBController.SliderGO.SetActive(true);

            numNonStimSelections_InBlock += mouseHandler.UpdateNumNonStimSelection();
            EventCodeManager.SendCodeImmediate(SessionEventCodes["StartButtonSelected"]);
            EventCodeManager.SendCodeNextFrame(SessionEventCodes["StimOn"]);
            EventCodeManager.SendCodeNextFrame(SessionEventCodes["SliderFbController_SliderReset"]);
        });
        ChooseStimulusDelay.AddTimer(() => chooseStimOnsetDelay.value, ChooseStimulus, ()=>
        {
            searchStims.ToggleVisibility(true);
            distractorStims.ToggleVisibility(true);
        });
        
       // MouseTracker.AddSelectionHandler(mouseHandler, ChooseStimulus, null, 
         //   ()=> MouseTracker.ButtonStatus[0] == 1, ()=> MouseTracker.ButtonStatus[0] == 0);
        // Define ChooseStimulus state - Stimulus are shown and the user must select the correct object in the correct sequence
        ChooseStimulus.AddInitializationMethod(() =>
        {
            AssignCorrectStim();
            CreateTextOnExperimenterDisplay();
            choiceMade = false;
            if (CurrentTrialDef.LeaveFeedbackOn) HaloFBController.SetLeaveFeedbackOn();
        });
        ChooseStimulus.AddUpdateMethod(() =>
        {
            TouchDurationErrorFeedback(USE_FBSquare, true);
        });
        ChooseStimulus.SpecifyTermination(()=> choiceMade, SelectionFeedback, ()=>
        {
            SetTrialSummaryString();
            CurrentTaskLevel.SetBlockSummaryString();
            if (CorrectSelection)
            {
                UpdateCounters_Correct();
                isSliderValueIncrease = true;
                EventCodeManager.SendCodeImmediate(SessionEventCodes["CorrectResponse"]);
            }
            else
            {
                runningAcc.Add(0);
                UpdateCounters_Incorrect(correctIndex);
                isSliderValueIncrease = false;
                EventCodeManager.SendCodeImmediate(SessionEventCodes["IncorrectResponse"]);

                //Repetition Error
                if (touchedObjects.Contains(selectedSD.StimIndex))
                {
                    repetitionErrorCount_InBlock++;
                    repetitionError = true;
                    EventCodeManager.SendCodeImmediate(TaskEventCodes["RepetitionError"]);
                }
                // Slot Errors
                else
                {
                    //Distractor Error
                    if (selectedSD.IsDistractor)
                    {
                        touchedObjects.Add(selectedSD.StimIndex);
                        distractorSlotErrorCount_InBlock++;
                        distractorSlotError = true;
                        EventCodeManager.SendCodeImmediate(SessionEventCodes["Button0PressedOnDistractorObject"]);//SELECTION STUFF (code may not be exact and/or could be moved to Selection handler)
                    }
                    //Stimuli Slot error
                    else
                    {
                        slotErrorCount_InBlock++;
                        slotError = true;
                        EventCodeManager.SendCodeImmediate(TaskEventCodes["SlotError"]);
                    }
                }
            }
        });
        ChooseStimulus.AddTimer(() => selectObjectDuration.value, ITI);
        // ChooseStimulus.SpecifyTermination(() => trialComplete, FinalFeedback);

        SelectionFeedback.AddInitializationMethod(() =>
        {
            touchedObjects.Add(selectedSD.StimIndex);
            searchDuration = ChooseStimulus.TimingInfo.Duration;
            searchDurations.Add(searchDuration);
            searchDurations_InBlock.Add(searchDuration);
            CurrentTaskLevel.SearchDurations_InTask.Add(searchDuration);
            totalFbDuration = (fbDuration.value + flashingFbDuration.value);
            SliderFBController.SetUpdateDuration(fbDuration.value);
            SliderFBController.SetFlashingDuration(flashingFbDuration.value);

            
            if (CorrectSelection)
            {
                consecutiveError = 0;
                HaloFBController.ShowPositive(selectedGO);
                SliderFBController.UpdateSliderValue(CurrentTrialDef.SliderGain[numTouchedStims]*(1f/sliderGainSteps));
                numTouchedStims += 1;
                if (numTouchedStims == CurrentTrialDef.CorrectObjectTouchOrder.Length)
                {
                    trialComplete = true;
                }

                errorTypeString = "None";
            }
            //Chose Incorrect
            else
            {
                consecutiveError++;
                HaloFBController.ShowNegative(selectedGO);
                if (distractorSlotError)
                    stimIdx = Array.IndexOf(CurrentTrialDef.DistractorStimsIndices, selectedSD.StimIndex); // used to index through the arrays in the config file/mapping different columns
                else
                    stimIdx = Array.IndexOf(CurrentTrialDef.SearchStimsIndices, selectedSD.StimIndex);

                SliderFBController.UpdateSliderValue(-CurrentTrialDef.SliderLoss[(int)stimIdx]*(1f/sliderLossSteps)); // NOT IMPLEMENTED: NEEDS TO CONSIDER SEPARATE LOSS/GAIN FOR DISTRACTOR & TARGET STIMS SEPARATELY
                if (slotError)
                    errorTypeString = "SlotError";
                else if (distractorSlotError)
                    errorTypeString = "DistractorSlotError";
                else
                    errorTypeString = "RepetitionError";
            }
            SetTrialSummaryString();
            selectedGO = null;
        });
        //don't control timing with AddTimer, use slider class SliderUpdateFinished bool 
        SelectionFeedback.AddTimer(()=>totalFbDuration, Delay, () =>
        {
            DelayDuration = 0;
            DestroyTextOnExperimenterDisplay();
            
            if (!CurrentTrialDef.LeaveFeedbackOn) 
                HaloFBController.Destroy();
            
            CurrentTaskLevel.SetBlockSummaryString();
            SetTrialSummaryString();
            
            if (CorrectSelection)
            {
                if(trialComplete)
                    StateAfterDelay = FinalFeedback;
                else
                    StateAfterDelay = ChooseStimulus;
                CorrectSelection = false;
            }
            else 
            {
                StateAfterDelay = ITI;
            }
        });
        FinalFeedback.AddInitializationMethod(() =>
        {
            trialComplete = false;
            startTime = Time.time;
            errorTypeString = "None";
            
            //Destroy all created text objects on Player View of Experimenter Display
            DestroyTextOnExperimenterDisplay();
            runningAcc.Add(1);
            NumSliderBarFilled += 1;
            CurrentTaskLevel.NumSliderBarFilled_InTask++;
            EventCodeManager.SendCodeNextFrame(SessionEventCodes["SliderFbController_SliderCompleteFbOn"]);
            EventCodeManager.SendCodeNextFrame(SessionEventCodes["StimOff"]);
            
            if (SyncBoxController != null)
            {
                SyncBoxController.SendRewardPulses(CurrentTrialDef.NumPulses, CurrentTrialDef.PulseSize); 
                SessionInfoPanel.UpdateSessionSummaryValues(("totalRewardPulses",CurrentTrialDef.NumPulses));
                numRewardGiven_InBlock += CurrentTrialDef.NumPulses;
                CurrentTaskLevel.NumRewardPulses_InTask += CurrentTrialDef.NumPulses;
            }
           
        });
        FinalFeedback.AddTimer(() => flashingFbDuration.value, ITI, () =>
        {
            EventCodeManager.SendCodeImmediate(SessionEventCodes["SliderFbController_SliderCompleteFbOff"]);
            EventCodeManager.SendCodeNextFrame(SessionEventCodes["ContextOff"]);
            CurrentTaskLevel.SetBlockSummaryString();
        });

        //Define iti state
        ITI.AddInitializationMethod(() =>
        {
            searchStims.ToggleVisibility(false);
            distractorStims.ToggleVisibility(false);
            if (!choiceMade)
            {
                consecutiveError++;
                aborted = true;
                errorTypeString = "AbortedTrial";
                AbortedTrials_InBlock++;
                CurrentTaskLevel.AbortedTrials_InTask++;
            }

            if (NeutralITI)
            {
                ContextName = "itiImage";
                RenderSettings.skybox = CreateSkybox(ContextExternalFilePath + Path.DirectorySeparatorChar + ContextName + ".png");
            }
        });
        ITI.AddTimer(() => itiDuration.value, FinishTrial);
        //------------------------------------------------------------------------ADDING VALUES TO DATA FILE--------------------------------------------------------------------------------------------------------------------------------------------------------------

        DefineTrialData();
        DefineFrameData();
    }

    public override void FinishTrialCleanup()
    {
        DestroyTextOnExperimenterDisplay();
        searchStims.ToggleVisibility(false);
        distractorStims.ToggleVisibility(false);
        SliderFBController.SliderGO.SetActive(false);
        SliderFBController.SliderHaloGO.SetActive(false);

        if(AbortCode == 0)
            CurrentTaskLevel.SetBlockSummaryString();

        if (AbortCode == AbortCodeDict["RestartBlock"] || AbortCode == AbortCodeDict["PreviousBlock"] || AbortCode == AbortCodeDict["EndBlock"]) //If used RestartBlock, PreviousBlock, or EndBlock hotkeys
        {
            AbortedTrials_InBlock++;
            CurrentTaskLevel.AbortedTrials_InTask++;
            CurrentTaskLevel.BlockSummaryString.Clear();
            CurrentTaskLevel.BlockSummaryString.AppendLine("");
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
        slotError = false;
        distractorSlotError = false;
        repetitionError = false;
        touchDurationError = false;
        aborted = false;
        
        numTouchedStims = 0;
        searchDuration = 0;
        sliderGainSteps = 0;
        sliderLossSteps = 0;
        stimIdx = null;
        selectedGO = null;
        selectedSD = null;
        CorrectSelection = false;
        choiceMade = false;
        
        searchDurations.Clear();
        touchedObjects.Clear();
        errorTypeString = "";
    }

    public void ResetBlockVariables()
    {
        ErrorType_InBlock.Clear();
        slotErrorCount_InBlock = 0;
        distractorSlotErrorCount_InBlock = 0;
        repetitionErrorCount_InBlock = 0;
        AbortedTrials_InBlock = 0;
        touchDurationErrorCount_InBlock = 0;
        numNonStimSelections_InBlock = 0;
        numRewardGiven_InBlock = 0;
        //comment better here
        Array.Clear(numTotal_InBlock, 0, numTotal_InBlock.Length);
        Array.Clear(numCorrect_InBlock, 0, numCorrect_InBlock.Length);
        Array.Clear(numErrors_InBlock, 0, numErrors_InBlock.Length);
        accuracyLog_InBlock = "";
        searchDurations_InBlock.Clear();
        consecutiveError = 0;
        runningAcc.Clear();
    }
    
    protected override bool CheckBlockEnd()
    {
        TaskLevelTemplate_Methods TaskLevel_Methods = new TaskLevelTemplate_Methods();
        return TaskLevel_Methods.CheckBlockEnd(CurrentTrialDef.BlockEndType, runningAcc,
            CurrentTrialDef.BlockEndThreshold, CurrentTrialDef.BlockEndWindow, MinTrials,
            TrialDefs.Count);
    }
    //-----------------------------------------------------------------METHODS FOR DATA HANDLING----------------------------------------------------------------------
    private void DefineTrialData() //All ".AddDatum" commands for Trial Data
    {
        TrialData.AddDatum("TrialID", () => CurrentTrialDef.BlockName);
        TrialData.AddDatum("Context", () => CurrentTrialDef.ContextName);
        TrialData.AddDatum("SearchStimsLocations", () => searchStimsLocations);
        TrialData.AddDatum("DistractorStimsLocations", () => distractorStimsLocations);
        TrialData.AddDatum("TouchedObjects", () => String.Join(",",touchedObjects));
        TrialData.AddDatum("SearchDurations", () => String.Join(",",searchDurations));
        TrialData.AddDatum("TrialProgress", ()=> trialProgress);
        TrialData.AddDatum("ErrorType", () => errorTypeString);
    }
    private void DefineFrameData() //All ".AddDatum" commands for Frame Data
    {
        FrameData.AddDatum("ErrorType", () => errorTypeString);
        FrameData.AddDatum("StartButton", () => startButton.activeSelf);
        FrameData.AddDatum("SearchStimuliShown", () => searchStims.IsActive);
        FrameData.AddDatum("DistractorStimuliShown", () => distractorStims.IsActive);
        FrameData.AddDatum("Context", () => ContextName);
        
    }

    private void SetTrialSummaryString()
    {
     //   if (TrialCount_InBlock != 0)
       //     trialProgress = (decimal.Divide(numCorrect_InTrial.Sum(), numTotal_InTrial.Sum())).ToString();

        TrialSummaryString = "Selected Object Indices: " + string.Join(",",touchedObjects) +
                             "\nCorrect Selection?: " + CorrectSelection +
                           //  "\nTrial Progress: " + trialProgress + IMPROVE THE LOGIC IS OFF
                             "\n" +
                             "\nError: " + errorTypeString +
                             "\n" +
                             "\nSearch Duration: " + string.Join(",", searchDurations);
    }

    private void DestroyTextOnExperimenterDisplay()
    {
        if (playerViewLoaded)
        {
            foreach (GameObject txt in playerViewTextList)
            {
                txt.SetActive(false);
            }
        }
        playerViewLoaded = false;
        playerViewTextList.Clear();
    }
    
    private void UpdateCounters_Incorrect(int correctIndex) // Updates Progress tracking information for incorrect selection
    {
        numTotal_InBlock[numTouchedStims]++;
        CurrentTaskLevel.NumTotal_InTask[numTouchedStims]++;
        numTotal_InTrial[numTouchedStims]++;
        
        numErrors_InBlock[correctIndex]++;
        CurrentTaskLevel.NumErrors_InTask[correctIndex]++;
        numErrors_InTrial[correctIndex]++;
    }
    private void UpdateCounters_Correct() // Updates Progress tracking information for correct selection
    {
        numCorrect_InBlock[numTouchedStims]++;
        CurrentTaskLevel.NumCorrect_InTask[numTouchedStims]++;
        numCorrect_InTrial[numTouchedStims]++;
        
        numTotal_InBlock[numTouchedStims]++;
        CurrentTaskLevel.NumTotal_InTask[numTouchedStims]++;
        numTotal_InTrial[numTouchedStims]++;
    }
    
    //--------------------------------------------------------------METHODS FOR STIMULUS/OBJECT HANDLING-------------------------------------------------------------
    private void CreateTextOnExperimenterDisplay()
    {
        if (!playerViewLoaded)
        {
            for (int iStim = 0; iStim < CurrentTrialDef.CorrectObjectTouchOrder.Length; ++iStim)
            {
                //Create corresponding text on player view of experimenter display
                textLocation = playerViewPosition(Camera.main.WorldToScreenPoint(searchStims.stimDefs[iStim].StimLocation),
                        playerViewParent);
                textLocation.y += 75;
                Vector2 textSize = new Vector2(200, 200);
                playerViewText = playerView.WriteText(CurrentTrialDef.CorrectObjectTouchOrder[iStim].ToString(), CurrentTrialDef.CorrectObjectTouchOrder[iStim].ToString(),
                    Color.red, textLocation, textSize, playerViewParent);
                playerViewText.GetComponent<RectTransform>().localScale = new Vector3(2, 2, 0);
                //should this ^ line be deleted and text size be congtrolled by textSize variable?
                playerViewTextList.Add(playerViewText);
            }
            playerViewLoaded = true;
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
        gratingSquareDuration = ConfigUiVariables.get<ConfigNumber>("gratingSquareDuration");
        flashingFbDuration = ConfigUiVariables.get<ConfigNumber>("finalFbDuration");
        fbDuration = ConfigUiVariables.get<ConfigNumber>("fbDuration");
        chooseStimOnsetDelay = ConfigUiVariables.get<ConfigNumber>("chooseStimOnsetDelay");
        timeoutDuration = ConfigUiVariables.get<ConfigNumber>("timeoutDuration");
        startButtonDelay = ConfigUiVariables.get<ConfigNumber>("startButtonDelay");
        Debug.Log("Done Loading Variables");
    }
    //-----------------------------------------------------DEFINE QUADDLES-------------------------------------------------------------------------------------
    protected override void DefineTrialStims()
    {
        //Define StimGroups consisting of StimDefs whose gameobjects will be loaded at TrialLevel_SetupTrial and 
        //destroyed at TrialLevel_Finish
        //StimGroup constructor which creates a subset of an already-existing StimGroup 
        searchStims = new StimGroup("SearchStims", ExternalStims, CurrentTrialDef.SearchStimsIndices);
        distractorStims = new StimGroup("DistractorStims", ExternalStims, CurrentTrialDef.DistractorStimsIndices);
       // searchStims.SetVisibilityOnOffStates(GetStateFromName("ChooseStimulus"), GetStateFromName("SelectionFeedback")); MAKING QUADDLES TWITCH BETWEEN STATES
     //   distractorStims.SetVisibilityOnOffStates(GetStateFromName("ChooseStimulus"), GetStateFromName("SelectionFeedback"));

        TrialStims.Add(searchStims);
        TrialStims.Add(distractorStims);
        
        randomizedLocations = CurrentTrialDef.RandomizedLocations; 

        if (randomizedLocations)
        {
            var totalStims = searchStims.stimDefs.Concat(distractorStims.stimDefs);
            var stimLocations = CurrentTrialDef.SearchStimsLocations.Concat(CurrentTrialDef.DistractorStimsLocations);

            int[] positionIndexArray = Enumerable.Range(0, totalStims.Count()).ToArray();
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
    private Vector2 playerViewPosition(Vector3 position, Transform playerViewParent)
    {
        Vector2 pvPosition = new Vector2((position[0] / Screen.width) * playerViewParent.GetComponent<RectTransform>().sizeDelta.x, (position[1] / Screen.height) * playerViewParent.GetComponent<RectTransform>().sizeDelta.y);
        return pvPosition;
    }
    //private void TouchDurationErrorFeedback(SelectionHandler<WhatWhenWhere_StimDef> MouseHandler, bool deactivateAfter)
    //{
        // EventCodeManager.SendCodeImmediate(SessionEventCodes["TouchDurationError"]);
        // EventCodeManager.SendCodeImmediate(SessionEventCodes["SelectionHandler_TouchErrorImageOn"]); //this should be put into selection handler soon. leaving for now. 
        // errorTypeString = "TouchDurationError";
        // AudioFBController.Play("Negative");
        // //eventually replace with state timer logic
        // if (MouseHandler.GetSelectionTooShort())
        //     StartCoroutine(USE_StartButton.GratedStartButtonFlash(HeldTooShortTexture, gratingSquareDuration.value, deactivateAfter));
        // else if (MouseHandler.GetSelectionTooLong())
        //     StartCoroutine(USE_StartButton.GratedStartButtonFlash(HeldTooLongTexture, gratingSquareDuration.value, deactivateAfter));
        // MouseHandler.SetSelectionTooLong(false);
        // MouseHandler.SetSelectionTooShort(false);
        // touchDurationError = false;
        // touchDurationErrorCount_InBlock++;
    //}'
    private void TouchDurationErrorFeedback(USE_StartButton UIElement, bool deactivateAfter)
    {
        if (UIElement.IsGrating)
            {
                gratingDuration -= Time.deltaTime;
                if(HeldTooShort)
                    UIElement.GratedStartButtonFlash(HeldTooShortTexture,gratingDuration,deactivateAfter);
                else
                    UIElement.GratedStartButtonFlash(HeldTooLongTexture,gratingDuration,deactivateAfter);
                return;
            }
            if (InputBroker.GetMouseButtonDown(0) && !USE_FBSquare.IsGrating)
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
                    if ((hit.collider != null) && (hit.collider.gameObject != null) && (selectionDuration >= minObjectTouchDuration.value) && (selectionDuration <= maxObjectTouchDuration.value))
                    {
                        choiceMade = true;
                        selectedGO = hit.collider.gameObject;
                        selectedSD = selectedGO?.GetComponent<StimDefPointer>()?.GetStimDef<WhatWhenWhere_StimDef>();
                        CorrectSelection = selectedSD.IsCurrentTarget;
                    }
                    else if (selectionDuration < minObjectTouchDuration.value)
                    {
                        UIElement.GratedStartButtonFlash(HeldTooShortTexture,gratingSquareDuration.value,deactivateAfter);
                        gratingDuration = gratingSquareDuration.value;
                        touchDurationError = true;
                        HeldTooShort = true;
                        touchDurationErrorCount_InBlock++;
                        CurrentTaskLevel.TouchDurationErrorCount_InTask++;
                        Debug.Log("Didn't select for minimum object touch duration!");
                    }
                    else if (selectionDuration > maxObjectTouchDuration.value)
                    {
                        UIElement.GratedStartButtonFlash(HeldTooLongTexture,gratingSquareDuration.value,deactivateAfter);
                        gratingDuration = gratingSquareDuration.value;
                        touchDurationError = true;
                        HeldTooLong = true;
                        touchDurationErrorCount_InBlock++;
                        CurrentTaskLevel.TouchDurationErrorCount_InTask++;
                        Debug.Log("Didn't select under max object touch duration!");
                    }
                }
                selectionDuration = null; // set this as null to consider multiple selections in a state
                SetTrialSummaryString();
            }
    }
    private void GenerateAccuracyLog()
    {
        // 3/13/23 - CONSIDER FORGOING THIS FORM OF ACCURACY, JUST USING BINARY CORRECT/INCORRECT TRIAL FOR NOW
        
        // looks at the number of opportunities to make a selection and the accuracy per selection 
        // ie. for a 3 object sequence, starts as 0/0, 0/0, 0/0
        // if the first selection is correct, becomes 1/1, 0/0, 0/0
        // if the next one is incorrect, becomes 1/1, 0/1, 0/0 and the trial ends and everything resets

        //progress report for trial
        accuracyLog_InTrial = "";
        for (int i = 0; i < CurrentTrialDef.CorrectObjectTouchOrder.Length; ++i)
        {
            accuracyLog_InTrial = accuracyLog_InTrial + "Slot " + (i + 1) + ": " + numCorrect_InTrial[i] + "/" + numTotal_InTrial[i] + " ";
        }
        
        // progress report for session 
        accuracyLog_InSession = "";
        for (int i = 0; i < CurrentTrialDef.CorrectObjectTouchOrder.Length; ++i)
        {
            accuracyLog_InSession = accuracyLog_InSession + "Slot " + (i + 1) + ": " + CurrentTaskLevel.NumCorrect_InTask[i] + "/" + CurrentTaskLevel.NumTotal_InTask[i] + " ";
        }

        // progress report for block
        accuracyLog_InBlock = "";
        for (int i = 0; i < CurrentTrialDef.CorrectObjectTouchOrder.Length; ++i)
        {
            accuracyLog_InBlock = accuracyLog_InBlock + "Slot " + (i + 1) + ": " + numCorrect_InBlock[i] + "/" + numTotal_InBlock[i] + " ";
        }
    }
}














