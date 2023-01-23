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
using USE_ExperimentTemplate_Trial;
using USE_ExperimentTemplate_Task;

public class WhatWhenWhere_TrialLevel : ControlLevel_Trial_Template
{
    //This variable is required for most tasks, and is defined as the output of the GetCurrentTrialDef function 
    public WhatWhenWhere_TrialDef CurrentTrialDef => GetCurrentTrialDef<WhatWhenWhere_TrialDef>();

    public WhatWhenWhere_TaskLevel CurrentTaskLevel => GetTaskLevel<WhatWhenWhere_TaskLevel>();
    // game object variables
    private GameObject chosenStim, grayHaloScene, haloContainer, haloClone, sliderGO, sliderHaloGO;
    public GameObject SliderPrefab, SliderHaloPrefab;
    private Image sr;
    private Texture2D texture;
    private static int numObjMax = 100;// need to change if stimulus exceeds this amount, not great
    
    // Config Variables
    public string ContextExternalFilePath;
    public Vector3 ButtonPosition, ButtonScale;
    public Vector3 FBSquarePosition, FBSquareScale;
    public bool StimFacingCamera;
    public string ShadowType;
    //stim group
    private StimGroup searchStims, distractorStims;
    private List<int> touchedObjects = new List<int>();
    private bool randomizedLocations = false;

    // feedback variables
    public int stimCount = 0;
    private bool correctChoice, incorrectChoice, noSelection, trialComplete = false;
    
    
    // error data variables
    public int slotErrorCount, distractorSlotErrorCount, touchDurationErrorCount, irrelevantSelectionErrorCount, repetitionErrorCount, totalErrors_InSession, noScreenTouchErrorCount = 0;
    public int totalErrors_InBlock = 0;
    private string errorTypeString = "";
    public List<String> errorType_InBlock = new List<String> { };
    public List<String> errorType_InSession = new List<String> { };
    public string errorType_InBlockString = "";
    private string errorType_InSessionString = "";
    private float startTime;
    private int TouchDurationError_InBlock;
    private int[] numTotal_InSession = new int[numObjMax];
    private int[] numErrors_InSession = new int[numObjMax];
    private int[] numCorrect_InSession = new int[numObjMax];
    public int[] numTotal_InBlock = new int[numObjMax];
    public int[] numTotal_InTrial = new int[numObjMax];
    public int[] numErrors_InBlock = new int[numObjMax];
    public int[] numErrors_InTrial = new int[numObjMax];
    public int[] numCorrect_InBlock = new int[numObjMax];
    public int[] numCorrect_InTrial = new int[numObjMax];
    private List<float> touchDurations  = new List<float> { };
    private List<float> choiceDurations = new List<float> { };
    //private List<Vector3> touchedPositionsList = new List<Vector3>(); // empty now
    public List<int> runningAcc;
    [HideInInspector] public ConfigNumber minObjectTouchDuration,
        itiDuration,
        finalFbDuration,
        fbDuration,
        maxObjectTouchDuration,
        selectObjectDuration,
        sliderSize,
        chooseStimOnsetDelay, 
        startButtonDelay,
        timeoutDuration, gratingSquareDuration, sliderUpdateTime;
    //data logging variables
    private string touchedObjectsCodes, touchDurationTimes, choiceDurationTimes, touchedPositions, searchStimsLocations, distractorStimsLocations;
    public string accuracyLog_InSession, accuracyLog_InBlock, accuracyLog_InTrial = "";
    private float initialTouchTime, touchDuration, choiceDuration, sbDelay = 0;
    private bool initialTouch, choiceMade, contextActive,halosDestroyed, slotError, distractorSlotError, touchDurationError, irrelevantSelectionError, repetitionError, noScreenTouchError = false;
    private String contextName = "";
   // private List<int> trialPerformance = new List<int>();
    private int timeoutCondition = 3;

    [HideInInspector]
    [System.NonSerialized] public int response = -1;
    // vector3 variables
    private Vector3 trialStimInitLocalScale, fbInitLocalScale, sliderInitPosition, touchPosition;

    // misc variables
    private Ray mouseRay;
    private Slider slider;
    private float sliderValueIncreaseAmount;
    private Camera cam;
    private bool variablesLoaded;
    private int correctIndex;
    public int sliderCompleteQuantity = 0;
    
    private bool isSliderValueIncrease = false;
    private bool isCorrectChoice = false;
    

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
    private GameObject FBSquare;
    public Texture2D HeldTooShortTexture;
    public Texture2D HeldTooLongTexture;
    private Texture2D StartButtonTexture;
    private Texture2D FBSquareTexture;
    private bool Grating = false;
    private TaskHelperFunctions taskHelper;
    
    // Stim Evaluation Variables
    private GameObject trialStim;
    private GameObject selected = null;
    private bool CorrectSelection;
    private WhatWhenWhere_StimDef selectedSD = null;
    //update slider variables
    float endupdatetime = 0f;
    float valueRemaining = 0f;
    float valueToAdd = 0f;
    public override void DefineControlLevel()
    {
        // --------------------------------------ADDING PLAYER VIEW STUFF------------------------------------------------------------------------------------

        //MonitorDetails primaryMonitorDetails = new MonitorDetails(new Vector2(1920, 1080), new Vector2(10, 7), 2);

        //---------------------------------------DEFINING STATES-----------------------------------------------------------------------
        State StartButton = new State("StartButton");
        //State StartButtonDelay = new State("StartButtonDelay");
        State ChooseStimulus = new State("ChooseStimulus");
        State ChooseStimulusDelay = new State("ChooseStimulusDelay");
        State SelectionFeedback = new State("StimulusChosen");
        State FinalFeedback = new State("FinalFeedback");
        State ITI = new State("ITI");
        State StimulusChosenSuccesorState = new State("StimulusChosenSuccesorState");
        State delay = new State("Delay");
        State UpdateSlider = new State("UpdateSlider");

        AddActiveStates(new List<State>
        {
            StartButton, ChooseStimulus, SelectionFeedback, FinalFeedback, ITI, StimulusChosenSuccesorState,
            ChooseStimulusDelay, delay, UpdateSlider
        });

        string[] stateNames = new string[]
            {"StartButton", "StartButtonDelay", "ChooseStimulus", "StimulusChosen", "FinalFeedback", "ITI", "Delay", "ChooseStimulusDelay", "UpdateSlide"};

        // A state that just waits for some time
        State stateAfterDelay = null;
        float delayDuration = 0;
        delay.AddTimer(() => delayDuration, () => stateAfterDelay);

        //MouseTracker variables
        SelectionHandler<WhatWhenWhere_StimDef> gazeHandler = new SelectionHandler<WhatWhenWhere_StimDef>();
        SelectionHandler<WhatWhenWhere_StimDef> mouseHandler = new SelectionHandler<WhatWhenWhere_StimDef>();
        taskHelper = new TaskHelperFunctions();
        GazeTracker.SpoofGazeWithMouse = true;

        //EventCodeManager.SendCodeImmediate(3);
        //Trial Completion Feedback Variables
        
        Add_ControlLevel_InitializationMethod(() =>
        {
            playerView = new PlayerViewPanel(); //GameObject.Find("PlayerViewCanvas").GetComponent<PlayerViewPanel>()
            playerViewText = new GameObject();
            LoadTextures();
            //HaloFBController.SetHaloSize(2);
            startButton = taskHelper.CreateStartButton(StartButtonTexture, ButtonPosition, ButtonScale);
            FBSquare = taskHelper.CreateFBSquare(FBSquareTexture, FBSquarePosition, FBSquareScale);
            playerViewParent = GameObject.Find("MainCameraCopy").transform; // sets parent for any playerView elements on experimenter display

        });

        SetupTrial.AddInitializationMethod(() =>
        {
            // Set the background texture to that of specified context
            contextActive = true;
            contextName = CurrentTrialDef.ContextName;
            RenderSettings.skybox = CreateSkybox(ContextExternalFilePath + Path.DirectorySeparatorChar + CurrentTrialDef.ContextName + ".png");
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["ContextOn"]);
            if (!variablesLoaded)
            {
                variablesLoaded = true;
                LoadTrialVariables();
            }
            ClearDataLogging();
            SetTrialSummaryString();
            CurrentTaskLevel.SetBlockSummaryString();
            if (slotErrorCount >= CurrentTrialDef.ErrorThreshold || distractorSlotErrorCount > CurrentTrialDef.ErrorThreshold || touchDurationErrorCount > CurrentTrialDef.ErrorThreshold || irrelevantSelectionErrorCount > CurrentTrialDef.ErrorThreshold || repetitionErrorCount > CurrentTrialDef.ErrorThreshold || noScreenTouchErrorCount > CurrentTrialDef.ErrorThreshold)
            {
                sbDelay = timeoutDuration.value;
            }
            else
            {
                sbDelay = startButtonDelay.value;
            }
        });
        SetupTrial.AddTimer(()=> sbDelay, StartButton);
        MouseTracker.AddSelectionHandler(mouseHandler, StartButton);
        // define StartButton state
        StartButton.AddInitializationMethod(() =>
        {
            InitializeSlider();
            mouseHandler.SetMinTouchDuration(minObjectTouchDuration.value);
            mouseHandler.SetMaxTouchDuration(maxObjectTouchDuration.value);
            startButton.SetActive(true);
        });
        StartButton.AddUpdateMethod(() =>
        {
            if (mouseHandler.GetHeldTooLong() || mouseHandler.GetHeldTooShort())
            {
                touchDurationError = true;
                SetTrialSummaryString();
                TouchDurationErrorFeedback(mouseHandler, startButton);
                TaskLevel.BlockSummaryString = CurrentTaskLevel.SetBlockSummaryString();
            }
        });

        StartButton.SpecifyTermination(() => mouseHandler.SelectionMatches(startButton), ChooseStimulusDelay, ()=>
        {
            sliderGO.SetActive(true);
            startButton.SetActive(false);
            
            isCorrectChoice = false;
            
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["StimOn"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["SliderReset"]);
        });
        ChooseStimulusDelay.AddTimer(() => chooseStimOnsetDelay.value, ChooseStimulus);
        GazeTracker.AddSelectionHandler(gazeHandler, ChooseStimulus);
        MouseTracker.AddSelectionHandler(mouseHandler, ChooseStimulus);

        // Define ChooseStimulus state - Stimulus are shown and the user must select the correct object in the correct sequence
        ChooseStimulus.AddInitializationMethod(() =>
        {
            AssignCorrectStim();
            //#################################################################################################
            //WE GOT HERE
            CreateTextOnExperimenterDisplay();
            searchStims.ToggleVisibility(true);
            distractorStims.ToggleVisibility(true);
            chosenStim = null;
            initialTouch = false;
            choiceMade = false;
        });
        ChooseStimulus.AddUpdateMethod(() =>
        { 
            // Evaluates whether or not the player selects the stimulus for long enough
            if (mouseHandler.GetHeldTooLong() || mouseHandler.GetHeldTooShort())
            {
                touchDurationError = true;
                FBSquare.SetActive(true);
                SetTrialSummaryString();
                TouchDurationErrorFeedback(mouseHandler, FBSquare);
                CurrentTaskLevel.SetBlockSummaryString();
            }
        });
        ChooseStimulus.SpecifyTermination(()=> mouseHandler.SelectedStimDef != null, SelectionFeedback, ()=>
        {
            selected = mouseHandler.SelectedGameObject;
            selectedSD = mouseHandler.SelectedStimDef;
            CorrectSelection = selectedSD.IsCurrentTarget;
            touchedObjects.Add(selectedSD.StimCode);
            
            if (CorrectSelection)
            {
                runningAcc.Add(1);
                CorrectSelectionProgressData();
                isSliderValueIncrease = true;
                EventCodeManager.SendCodeImmediate(TaskEventCodes["CorrectResponse"]);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionVisualFbOn"]);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionAuditoryFbOn"]);
            }
            else
            {
                runningAcc.Add(0);
                IncorrectSelectionProgressData(correctIndex);
                isSliderValueIncrease = false;
                EventCodeManager.SendCodeImmediate(TaskEventCodes["IncorrectResponse"]);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionVisualFbOn"]);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionAuditoryFbOn"]);

                //Repetition Error
                if (touchedObjects.Contains(selectedSD.StimCode))
                {
                    repetitionErrorCount += 1;
                    repetitionError = true;
                    EventCodeManager.SendCodeImmediate(TaskEventCodes["RepetitionError"]);
                }
                // Slot Errors
                else
                {
                    //Distractor Error
                    if (selectedSD.IsDistractor)
                    {
                        touchedObjects.Add(selectedSD.StimCode);
                        distractorSlotErrorCount += 1;
                        distractorSlotError = true;
                        EventCodeManager.SendCodeImmediate(TaskEventCodes["TouchDistractorStart"]);
                    }
                    //Stimuli Slot error
                    else
                    {
                        slotErrorCount += 1;
                        slotError = true;
                        EventCodeManager.SendCodeImmediate(TaskEventCodes["SlotError"]);
                    }
                }
            }
        });
        ChooseStimulus.AddTimer(() => selectObjectDuration.value, ITI);
        //ChooseStimulus.SpecifyTermination(() => response == 1,
          //  SelectionFeedback); // Response ==1 means "Clicked on a stimulus" and is evaluated for errors
        //ChooseStimulus.SpecifyTermination(() => response == 2,
          //  SelectionFeedback); // Response == 2 means "Clicked within the scene, but not on a stimulus"
        ChooseStimulus.SpecifyTermination(() => trialComplete, FinalFeedback);

        SelectionFeedback.AddInitializationMethod(() =>
        {
            endupdatetime = Time.time + fbDuration.value;
            valueToAdd = sliderValueIncreaseAmount * (CurrentTrialDef.SliderGain[stimCount]);
            valueRemaining = valueToAdd;
            if (isSliderValueIncrease) stimCount += 1;
            //Chose Incorrect
            if (incorrectChoice)
            {
                HaloFBController.ShowNegative(selected);
                AudioFBController.Play("Negative");
                sr.color = new Color(0.6627f, 0.6627f, 0.6627f, 0.2f);
                if (slotError)
                    errorTypeString = "SlotError";
                else if (distractorSlotError)
                    errorTypeString = "DistractorSlotError";
                else
                    errorTypeString = "RepetitionError";
            }
            //Chose correct
            else if (correctChoice)
            {
                HaloFBController.ShowPositive(selected);
                AudioFBController.Play("Positive");
                sliderHaloGO.SetActive(true);
                sr.color = new Color(1, 0.8431f, 0, 0.2f);
                errorTypeString = "None";
            }
            GenerateUpdatingTrialData();
            SetTrialSummaryString();
        });
        SelectionFeedback.AddUpdateMethod(() =>
        {
            float incrementalVal = valueToAdd/fbDuration.value;
            if (valueRemaining >= 0)
            {
                if (isSliderValueIncrease == false)
                {
                    slider.value -= incrementalVal;
                    valueRemaining -= incrementalVal;
                }
                else
                {
                    slider.value += incrementalVal;
                    valueRemaining -= incrementalVal;
                }
            }
        });
        SelectionFeedback.AddTimer(()=>fbDuration.value, delay, () =>
        {
            delayDuration = 0;
            sliderHaloGO.SetActive(false);
            CurrentTaskLevel.SetBlockSummaryString();
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionVisualFbOff"]);
            if (correctChoice)
            {
                stateAfterDelay = ChooseStimulus;
                correctChoice = false;
            }
            else if (incorrectChoice)
            {
                stateAfterDelay = ITI;
                incorrectChoice = false;
            }
            
        });
        FinalFeedback.AddInitializationMethod(() =>
        {
            trialComplete = false;
            sliderHaloGO.SetActive(true);
            sr.color = new Color(1, 1, 1, 0.2f);
            startTime = Time.time;
            errorTypeString = "None";
            searchStims.ToggleVisibility(false);
            distractorStims.ToggleVisibility(false);
            response = -1;
            
            runningAcc.Add(1);
            sliderCompleteQuantity += 1;/*
            if (CurrentTrialDef.LeaveFeedbackOn)
            {
                foreach (Transform child in GameObject.Find("HaloContainer").transform)
                {
                    GameObject.Destroy(child.gameObject);
                    halosDestroyed = true;
                }
            }*/
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["SliderCompleteFbOn"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["StimOff"]);
            
            if (SyncBoxController != null)
            {
                SyncBoxController.SendRewardPulses(CurrentTrialDef.NumPulses, CurrentTrialDef.PulseSize); 
                EventCodeManager.SendCodeImmediate(TaskEventCodes["Fluid1Onset"]);
            }
           
        });

        FinalFeedback.AddUpdateMethod(() =>
        {
            if ((int) (10 * (Time.time - startTime)) % 4 == 0)
            {
                sr.color = new Color(1, 1, 1, 0.2f);
            }
            else if ((int) (10 * (Time.time - startTime)) % 2 == 0)
            {
                sr.color = new Color(0, 0, 0, 0.2f);
            }
        });
        FinalFeedback.AddTimer(() => finalFbDuration.value, ITI, () =>
        {
            sliderHaloGO.SetActive(false);
            EventCodeManager.SendCodeImmediate(TaskEventCodes["SliderCompleteFbOff"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["ContextOff"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["TrlEnd"]);
        });

        //Define iti state
        ITI.AddInitializationMethod(() =>
        {
            searchStims.ToggleVisibility(false);
            distractorStims.ToggleVisibility(false);
            contextName = "itiImage";
            RenderSettings.skybox = CreateSkybox(ContextExternalFilePath + Path.DirectorySeparatorChar + contextName + ".png");

            //Destroy all created text objects on Player View of Experimenter Display
            DestroyTextOnExperimenterDisplay();
            sliderGO.SetActive(false);
/*
            if (response == 0)
            {
                noScreenTouchErrorCount++;
                totalErrors_InSession += 1;
                totalErrors_InBlock += 1;
                errorTypeString = "NoSelectionMade";
                Debug.Log("Didn't click on any stimulus");
                response = -1;
                runningAcc.Add(0);

                EventCodeManager.SendCodeImmediate(TaskEventCodes["NoChoice"]);
            }*/

            GenerateFinalTrialData();
        });
        ITI.AddTimer(() => itiDuration.value, FinishTrial, () =>
        {
            CurrentTaskLevel.SetBlockSummaryString();
            DestroyTextOnExperimenterDisplay();
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["TrlStart"]);
            
        });
    

    // FinishTrial.SpecifyTermination(
        //     () => TaskLevel_Methods.CheckBlockEnd("SimpleThreshold", runningAcc, 1, 5, MinTrials, TrialDefs.Length),
        //     () => null);



        //------------------------------------------------------------------------ADDING VALUES TO DATA FILE--------------------------------------------------------------------------------------------------------------------------------------------------------------

        LogTrialData();
        LogFrameData();
        ClearDataLogging();
    }
    
    protected override bool CheckBlockEnd()
    {
        TaskLevelTemplate_Methods TaskLevel_Methods = new TaskLevelTemplate_Methods();
        return TaskLevel_Methods.CheckBlockEnd(CurrentTrialDef.BlockEndType, runningAcc,
            CurrentTrialDef.BlockEndThreshold, CurrentTrialDef.BlockEndWindow, MinTrials,
            TrialDefs.Count);
    }
    //-----------------------------------------------------------------METHODS FOR DATA HANDLING----------------------------------------------------------------------
    private void LogTrialData() //All ".AddDatum" commands for Trial Data
    {
        TrialData.AddDatum("TrialID", () => CurrentTrialDef.BlockName);
        TrialData.AddDatum("Context", () => CurrentTrialDef.ContextName);
        TrialData.AddDatum("SearchStimsLocations", () => searchStimsLocations);
        TrialData.AddDatum("DistractorStimsLocations", () => distractorStimsLocations);
        TrialData.AddDatum("TouchedObjects", () => touchedObjectsCodes);
      //  TrialData.AddDatum("TouchPositions", () => touchedPositions);
        //TrialData.AddDatum("TrialPerformance", () => trialPerformance);
        TrialData.AddDatum("ErrorType", () => errorTypeString);
        TrialData.AddDatum("ErrorType_InBlock", () => errorType_InBlockString);
        TrialData.AddDatum("ErrorType_InSession", () => errorType_InSessionString);
        TrialData.AddDatum("TotalErrors_InBlock", () => totalErrors_InBlock);
        TrialData.AddDatum("TotalErrors_InSession", () => totalErrors_InSession);
        TrialData.AddDatum("TouchDurations", () => touchDurationTimes);
        TrialData.AddDatum("ChoiceDurations", () => choiceDurationTimes);
        TrialData.AddDatum("ProgressInSession", () => accuracyLog_InSession);
        TrialData.AddDatum("ProgressInTrial", () => accuracyLog_InTrial);
        TrialData.AddDatum("ProgressInBlock", () => accuracyLog_InBlock);
        
    }
    private void LogFrameData() //All ".AddDatum" commands for Frame Data
    {
        FrameData.AddDatum("TouchPosition", () => InputBroker.mousePosition);
        FrameData.AddDatum("ErrorType", () => errorTypeString);
        FrameData.AddDatum("Touch", () => response);
        FrameData.AddDatum("StartButton", () => startButton.activeSelf);
        //FrameData.AddDatum("GrayHaloFeedback", () => (grayHalo.activeSelf || grayHaloScene.activeSelf));
        //FrameData.AddDatum("YellowHaloFeedback", () => yellowHalo.activeSelf);
       //FrameData.AddDatum("TimingErrorFeedback", () => imageTimingError.activeSelf);
        //FrameData.AddDatum("SliderHalo", () => sliderHalo.activeSelf);
        //FrameData.AddDatum("Slider", () => slider.gameObject.activeSelf);
        FrameData.AddDatum("SearchStimuliShown", () => searchStims.IsActive);
        FrameData.AddDatum("DistractorStimuliShown", () => distractorStims.IsActive);
        //FrameData.AddDatum("SliderValue", () => slider.normalizedValue);
        FrameData.AddDatum("Context", () => contextName);
        FrameData.AddDatum("ContextActive", () => contextActive);
        
    }

    private void SetTrialSummaryString()
    {
        TrialSummaryString = "Trial Num: " + (TrialCount_InTask + 1) + "\nError Type: " +
                             errorTypeString + "\nTouch Duration: " + touchDurationTimes + "\nChoice Duration: " +
                             choiceDurationTimes + "\nProgress: " + accuracyLog_InTrial + "\nSession Progress: " +
                             accuracyLog_InSession;
    }
    private void GenerateUpdatingTrialData() //Creates strings of data to be actively displayed on panels in experimenter view
    {
        // progress report for session
        accuracyLog_InSession = "";
        for (int i = 0; i < CurrentTrialDef.CorrectObjectTouchOrder.Length; ++i)
        {
            accuracyLog_InSession = accuracyLog_InSession + "Slot " + (i + 1) + ": " + numCorrect_InSession[i] + "/" + numTotal_InSession[i] + " ";
        }

        // progress report for block
        accuracyLog_InBlock = "";
        for (int i = 0; i < CurrentTrialDef.CorrectObjectTouchOrder.Length; ++i)
        {
            accuracyLog_InBlock = accuracyLog_InBlock + "Slot " + (i + 1) + ": " + numCorrect_InBlock[i] + "/" + numTotal_InBlock[i] + " ";
        }

        //progress report for trial
        accuracyLog_InTrial = "";
        for (int i = 0; i < CurrentTrialDef.CorrectObjectTouchOrder.Length; ++i)
        {
            accuracyLog_InTrial = accuracyLog_InTrial + "Slot " + (i + 1) + ": " + numCorrect_InTrial[i] + "/" + numTotal_InTrial[i] + " ";
        }
        
        // search stims locations data 
        searchStimsLocations = "[";
        for (int i = 0; i < searchStims.stimDefs.Count; ++i)
        {
            if (i < searchStims.stimDefs.Count - 1)
            {
                searchStimsLocations = searchStimsLocations + (searchStims.stimDefs[i].StimLocation).ToString() + ",";
            }
            else
            {
                searchStimsLocations = searchStimsLocations + searchStims.stimDefs[i].StimLocation.ToString();
            }
        }
        searchStimsLocations = searchStimsLocations + "]";
        Debug.Log("SEARCH STIMS LOCATIONS FOR DATA LOGGING" + searchStimsLocations);
        
        //distractors stims locations
        distractorStimsLocations = "[";
        for (int i = 0; i < distractorStims.stimDefs.Count; ++i)
        {
            if (i < distractorStims.stimDefs.Count - 1)
            {
                distractorStimsLocations = distractorStimsLocations + distractorStims.stimDefs[i].StimLocation.ToString() + ",";
            }
            else
            {
                distractorStimsLocations = distractorStimsLocations + distractorStims.stimDefs[i].StimLocation.ToString();
            }
        }
        distractorStimsLocations = distractorStimsLocations + "]";

        // touched objects data 
        touchedObjectsCodes = "[";
        for (int i = 0; i < touchedObjects.Count; ++i)
        {
            if (i < touchedObjects.Count - 1)
            {
                touchedObjectsCodes = touchedObjectsCodes + touchedObjects[i] + ",";
            }
            else
            {
                touchedObjectsCodes = touchedObjectsCodes + touchedObjects[i];
            }
        }
        touchedObjectsCodes = touchedObjectsCodes + "]";

        // touch duration data
        touchDurationTimes = "[";
        for (int i = 0; i < touchDurations.Count; ++i)
        {
            if (i < touchDurations.Count - 1)
            {
                touchDurationTimes = touchDurationTimes + touchDurations[i] + ",";
            }
            else
            {
                touchDurationTimes = touchDurationTimes + touchDurations[i];
            }
        }
        touchDurationTimes = touchDurationTimes + "]";

        // choice duration data
        choiceDurationTimes = "[";
        for (int i = 0; i < choiceDurations.Count; ++i)
        {
            if (i < choiceDurations.Count - 1)
            {
                choiceDurationTimes = choiceDurationTimes + choiceDurations[i] + ",";
            }
            else
            {
                choiceDurationTimes = choiceDurationTimes + choiceDurations[i];
            }
        }
        choiceDurationTimes = choiceDurationTimes + "]";
        
        // touch position data 
        /*
        touchedPositions = "[";
        for (int i = 0; i < touchedPositionsList.Count; ++i)
        {
            if (i < touchedPositionsList.Count - 1)
            {
                touchedPositions = touchedPositions + "(" + touchedPositionsList[i][0] + "," + touchedPositionsList[i][1] + "," + touchedPositionsList[i][2] + "),";
            }
            else
            {
                touchedPositions = touchedPositions + "(" + touchedPositionsList[i][0] + "," + touchedPositionsList[i][1] + "," + touchedPositionsList[i][2] + ")";
            }
        }
        touchedPositions = touchedPositions + "]";*/
    } 
    private void GenerateFinalTrialData() //Creates final strings of data concerning block/session error types that do not need to be updated during the task
    {
        // error names data
            errorType_InBlock.Add(errorTypeString);
            errorType_InSession.Add(errorTypeString);
            errorType_InBlockString = "[";

            // session error data
            errorType_InSessionString = "[";
            for (int i = 0; i < errorType_InSession.Count; ++i)
            {
                if (i < errorType_InSession.Count - 1)
                {
                    errorType_InSessionString = errorType_InSessionString + errorType_InSession[i] + ",";
                }
                else
                {
                    errorType_InSessionString = errorType_InSessionString + errorType_InSession[i];
                }
            }
            errorType_InSessionString = errorType_InSessionString + "]";
            Debug.Log("ErrorTypes_InSession " + errorType_InSessionString);
            // generate error type data for the block
            for (int i = 0; i < errorType_InBlock.Count; ++i)
            {
                if (i < errorType_InBlock.Count - 1)
                {
                    errorType_InBlockString = errorType_InBlockString + errorType_InBlock[i] + ",";
                }
                else
                {
                    errorType_InBlockString = errorType_InBlockString + errorType_InBlock[i];
                }
            }
            errorType_InBlockString = errorType_InBlockString + "]";
            Debug.Log("ErrorType" + errorTypeString);
            Debug.Log("ErrorTypes_InBlock " + errorType_InBlockString);
    } 
    private void ClearDataLogging() //Sets data collecting variables to 0
    {
        slotError = false;
        distractorSlotError = false;
        repetitionError = false;
        touchDurationError = false;
        irrelevantSelectionError = false;
        noScreenTouchError = false; 

        initialTouch = false;
        choiceMade = false;
        contextActive = false;
        halosDestroyed = false;
        stimCount = 0;
        response = -1;
        touchedObjects.Clear();
        touchDurations.Clear();
        choiceDurations.Clear();
       // touchedPositionsList.Clear();
        //trialPerformance.Clear();

        touchedObjectsCodes = "[]";
        touchDurationTimes = "[]";
        choiceDurationTimes = "[]";
       // touchedPositions = "[]";
        touchedObjectsCodes = "[]";
        searchStimsLocations = "[]";
        distractorStimsLocations = "[]";
        errorTypeString = "";
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

    private void InitializeSlider()
    {
        //NOT GOOD BUT I NEED THE SLIDER TO APPEAR AFTER THE CONTEXT APPEARS
        Transform sliderCanvas = GameObject.Find("SliderCanvas").transform;
        sliderGO = Instantiate(SliderPrefab, sliderCanvas);
        sliderHaloGO = Instantiate(SliderHaloPrefab, sliderCanvas);
        //sliderHalo = GameObject.Find("SliderHalo");
        sr = sliderHaloGO.GetComponent<Image>();
        slider = sliderGO.GetComponent<Slider>();
        sliderInitPosition = sliderGO.transform.position;
        //consider making slider stuff into USE level class
        slider.value = 0;
        sliderHaloGO.transform.position = sliderInitPosition;
        int numSliderSteps = CurrentTrialDef.SliderGain.Sum() + CurrentTrialDef.SliderInitial;
        sliderValueIncreaseAmount = (100f / numSliderSteps) / 100f;
        slider.transform.localScale = new Vector3(sliderSize.value / 10f, sliderSize.value / 10f, 1f);
        sliderHaloGO.transform.localScale = new Vector3(sliderSize.value / 10f, sliderSize.value / 10f, 1f);

        if (CurrentTrialDef.SliderInitial != 0)
        {
            slider.value += sliderValueIncreaseAmount * (CurrentTrialDef.SliderInitial);
        }

        sliderGO.SetActive(false);
        sliderHaloGO.SetActive(false);
        isSliderValueIncrease = false;
    }
    private void DataConsoleMessages() //Generates Debug.Log messages of the data strings
    {
        Debug.Log("Progress_InSession: " + accuracyLog_InSession);
        Debug.Log("Progress_InBlock: " + accuracyLog_InBlock);
        Debug.Log("Progress_InTrial: " + accuracyLog_InTrial);
        Debug.Log("Touched Objects: " + touchedObjectsCodes);
        Debug.Log("Touch Durations: " + touchDurationTimes);
        Debug.Log("Choice Durations: " + choiceDurationTimes);
      //  Debug.Log("Touched Positions: " + touchedPositions);
    }
    private void IncorrectSelectionProgressData(int correctIndex) // Updates Progress tracking information for incorrect selection
    {
        totalErrors_InSession += 1;
        totalErrors_InBlock += 1;
        numTotal_InBlock[stimCount]++;
        numTotal_InSession[stimCount]++;
        numTotal_InTrial[stimCount]++;
        numErrors_InBlock[correctIndex]++;
        numErrors_InSession[correctIndex]++;
        numErrors_InTrial[correctIndex]++;

        incorrectChoice = true;
    }
    private void CorrectSelectionProgressData() // Updates Progress tracking information for correct selection
    {
        numCorrect_InBlock[stimCount]++;
        numCorrect_InSession[stimCount]++;
        numCorrect_InTrial[stimCount]++;
        numTotal_InBlock[stimCount]++;
        numTotal_InSession[stimCount]++;
        numTotal_InTrial[stimCount]++;

        correctChoice = true;
    }
    
    //--------------------------------------------------------------METHODS FOR STIMULUS/OBJECT HANDLING-------------------------------------------------------------
    private void CreateTextOnExperimenterDisplay()
    {
        if (!playerViewLoaded)
        {
            for (int i = 0; i < CurrentTrialDef.CorrectObjectTouchOrder.Length; ++i)
            {
                //Create corresponding text on player view of experimenter display
                textLocation =
                    playerViewPosition(Camera.main.WorldToScreenPoint(searchStims.stimDefs[i].StimLocation),
                        playerViewParent);
                textLocation.y += 50;
                Vector2 textSize = new Vector2(200, 200);
                playerViewText = playerView.writeText(CurrentTrialDef.CorrectObjectTouchOrder[i].ToString(),
                    Color.red, textLocation, textSize, playerViewParent);
                playerViewText.GetComponent<RectTransform>().localScale = new Vector3(2, 2, 0);
                playerViewTextList.Add(playerViewText);
            }

            playerViewLoaded = true;
        }
    }
    void disableAllGameobjects()
    {
        startButton.SetActive(false);
        //sliderHalo.SetActive(false);
        //grayHaloScene.SetActive(false);
       // imageTimingError.SetActive(false);
        searchStims.ToggleVisibility(false);
        distractorStims.ToggleVisibility(false);
        //slider.gameObject.SetActive(false);
    }
    void LoadTrialVariables()
    {
        /*
        if (CurrentTrialDef.LeaveFeedbackOn)
        {
            haloContainer = new GameObject("HaloContainer");
            haloContainer.transform.parent = GameObject.Find("Canvas").transform;
        }
        */
        //config UI variables
        minObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("minObjectTouchDuration");
        maxObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("maxObjectTouchDuration");
        itiDuration = ConfigUiVariables.get<ConfigNumber>("itiDuration");
        sliderSize = ConfigUiVariables.get<ConfigNumber>("sliderSize");
        selectObjectDuration = ConfigUiVariables.get<ConfigNumber>("selectObjectDuration");
        gratingSquareDuration = ConfigUiVariables.get<ConfigNumber>("gratingSquareDuration");
        finalFbDuration = ConfigUiVariables.get<ConfigNumber>("finalFbDuration");
        fbDuration = ConfigUiVariables.get<ConfigNumber>("fbDuration");
        chooseStimOnsetDelay = ConfigUiVariables.get<ConfigNumber>("chooseStimOnsetDelay");
        timeoutDuration = ConfigUiVariables.get<ConfigNumber>("timeoutDuration");
        startButtonDelay = ConfigUiVariables.get<ConfigNumber>("startButtonDelay");
        //sliderUpdateTime = ConfigUiVariables.get<ConfigNumber>("sliderUpdateTime");

        disableAllGameobjects();
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
    }
    
    //-------------------------------------------------------------MISCELLANEOUS METHODS--------------------------------------------------------------------------
    private void AssignCorrectStim()
    {
        //if we haven't finished touching all stims
                    if (stimCount < CurrentTrialDef.CorrectObjectTouchOrder.Length)
                    {
                        //find which stimulus is currently target
                        correctIndex = CurrentTrialDef.CorrectObjectTouchOrder[stimCount] - 1;
        
                        for (int i = 0; i < CurrentTrialDef.CorrectObjectTouchOrder.Length; i++)
                        {
                            WhatWhenWhere_StimDef sd = (WhatWhenWhere_StimDef) searchStims.stimDefs[i];
        
                            if (i == correctIndex) sd.IsCurrentTarget = true;
                            else sd.IsCurrentTarget = false;
                        }
        
                        for (int i = 0; i < CurrentTrialDef.DistractorStimsIndices.Length; ++i)
                        {
                            WhatWhenWhere_StimDef sd = (WhatWhenWhere_StimDef) distractorStims.stimDefs[i];
                            sd.IsDistractor = true;
                        }
                    }
                    else
                    {
                        trialComplete = true;
                    }
    }
    private Vector2 playerViewPosition(Vector3 position, Transform playerViewParent)
    {
        Vector2 pvPosition = new Vector2((position[0] / Screen.width) * playerViewParent.GetComponent<RectTransform>().sizeDelta.x, (position[1] / Screen.height) * playerViewParent.GetComponent<RectTransform>().sizeDelta.y);
        return pvPosition;
    }
   /* private GameObject CreateStartButton(Texture2D tex, Rect rect) //creates start button as a sprite
    {
        Vector3 buttonPosition = Vector3.zero;
        Vector3 buttonScale = Vector3.zero;
        string TaskName = "WhatWhenWhere";
        if (SessionSettings.SettingClassExists(TaskName + "_TaskSettings"))
        {
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonPosition"))
                buttonPosition = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonPosition");
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonScale"))
                buttonScale = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonScale");
        }
        else
        {
            Debug.Log("[ERROR] Start Button Image settings not defined in the TaskDef");
        }

        GameObject startButton = new GameObject("StartButton");
        SpriteRenderer sr = startButton.AddComponent<SpriteRenderer>() as SpriteRenderer;
        sr.sprite = Sprite.Create(tex, new Rect(rect.x, rect.y, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
        startButton.AddComponent<BoxCollider>();
        startButton.transform.localScale = buttonScale;
        startButton.transform.position = buttonPosition;
        return startButton;
    }*/
    private void TouchDurationErrorFeedback(SelectionHandler<WhatWhenWhere_StimDef> MouseHandler, GameObject go)
    {
        EventCodeManager.SendCodeImmediate(TaskEventCodes["TouchDurationError"]);
        EventCodeManager.SendCodeImmediate(TaskEventCodes["TouchErrorImageOn"]);
        EventCodeManager.SendCodeImmediate(TaskEventCodes["SelectionAuditoryFbOn"]);
        errorTypeString = "TouchDurationError";
        AudioFBController.Play("Negative");
        if (MouseHandler.GetHeldTooShort())
            StartCoroutine(taskHelper.GratedSquareFlash(HeldTooShortTexture, go, gratingSquareDuration.value));
        else if (MouseHandler.GetHeldTooLong())
            StartCoroutine(taskHelper.GratedSquareFlash(HeldTooLongTexture, go, gratingSquareDuration.value));
        MouseHandler.SetHeldTooLong(false);
        MouseHandler.SetHeldTooShort(false);
        touchDurationError = false;
        TouchDurationError_InBlock++;
        totalErrors_InBlock++;
        totalErrors_InSession++;
    }
    private void LoadTextures()
    {
        StartButtonTexture = LoadPNG(ContextExternalFilePath + Path.DirectorySeparatorChar + "StartButtonImage.png");
        FBSquareTexture = LoadPNG(ContextExternalFilePath + Path.DirectorySeparatorChar + "Grey.png");
        HeldTooLongTexture = LoadPNG(ContextExternalFilePath + Path.DirectorySeparatorChar + "HorizontalStripes.png");
        HeldTooShortTexture = LoadPNG(ContextExternalFilePath + Path.DirectorySeparatorChar + "VerticalStripes.png");
    }

}














