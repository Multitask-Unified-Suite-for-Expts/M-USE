
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
using UnityEngine.Serialization;
using USE_ExperimentTemplate_Trial;
using USE_ExperimentTemplate_Task;
using USE_UI;
using USE_Utilities;

public class WhatWhenWhere_TrialLevel : ControlLevel_Trial_Template
{
    public GameObject WWW_CanvasGO;

    //This variable is required for most tasks, and is defined as the output of the GetCurrentTrialDef function 
    public WhatWhenWhere_TrialDef CurrentTrialDef => GetCurrentTrialDef<WhatWhenWhere_TrialDef>();

    public WhatWhenWhere_TaskLevel CurrentTaskLevel => GetTaskLevel<WhatWhenWhere_TaskLevel>();
    // game object variables
    private Texture2D texture;
    private static int numObjMax = 100;// need to change if stimulus exceeds this amount, not great
    
    // Config Variables
    public string ContextExternalFilePath;
    [FormerlySerializedAs("ButtonPosition")] public Vector3 ButtonPosition;
    [FormerlySerializedAs("ButtonScale")] public float ButtonScale;
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
  //  public int MinTrials;

    // Stimuli Variables
    private GameObject StartButton;
    public float ExternalStimScale;
    
    // Stim Evaluation Variables
    private GameObject trialStim;
    private GameObject selectedGO = null;
    private bool CorrectSelection;
    private WhatWhenWhere_StimDef selectedSD = null;
    private int? stimIdx; // used to index through the arrays in the config file/mapping different columns
    private float? selectionDuration = null;
    private bool choiceMade = false;

    [HideInInspector] public float TouchFeedbackDuration;


    public override void DefineControlLevel()
    {
        // --------------------------------------ADDING PLAYER VIEW STUFF------------------------------------------------------------------------------------

        //MonitorDetails primaryMonitorDetails = new MonitorDetails(new Vector2(1920, 1080), new Vector2(10, 7), 2);

        //---------------------------------------DEFINING STATES-----------------------------------------------------------------------
        State InitTrial = new State("InitTrial");
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
            {"InitTrial", "ChooseStimulus", "ChooseStimulusDelay", "SelectionFeedback", "FinalFeedback", "ITI", "ChooseStimulusDelay"};

        /*//MouseTracker variables
        SelectionHandler<WhatWhenWhere_StimDef> gazeHandler = new SelectionHandler<WhatWhenWhere_StimDef>();
        GazeTracker.SpoofGazeWithMouse = true;*/

        //player view variables
        playerView = new PlayerViewPanel(); //GameObject.Find("PlayerViewCanvas").GetComponent<PlayerViewPanel>()
        playerViewText = new GameObject();

        
        Add_ControlLevel_InitializationMethod(() =>
        {
            SliderFBController.InitializeSlider();
            // Initialize FB Controller Values
            HaloFBController.SetHaloSize(12);
            HaloFBController.SetHaloIntensity(5);
            if (StartButton == null)
            {
                if (SessionValues.SessionDef.IsHuman)
                {
                    StartButton = SessionValues.HumanStartPanel.StartButtonGO;
                    SessionValues.HumanStartPanel.SetVisibilityOnOffStates(InitTrial, InitTrial);
                }
                else
                {
                    StartButton = SessionValues.USE_StartButton.CreateStartButton(WWW_CanvasGO.GetComponent<Canvas>(), ButtonPosition, ButtonScale);
                    SessionValues.USE_StartButton.SetVisibilityOnOffStates(InitTrial, InitTrial);
                }
            }
            #if (!UNITY_WEBGL)
                playerViewParent = GameObject.Find("MainCameraCopy").transform; // sets parent for any playerView elements on experimenter display
            #endif
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
                MakeStimFaceCamera();
            
            if (consecutiveError >= CurrentTrialDef.ErrorThreshold)
                sbDelay = timeoutDuration.value;
            else
                sbDelay = startButtonDelay.value;
        });
        SetupTrial.AddTimer(()=> sbDelay, InitTrial);

        var ShotgunHandler = SessionValues.SelectionTracker.SetupSelectionHandler("trial", "TouchShotgun", SessionValues.MouseTracker, InitTrial, FinalFeedback);

        if (!SessionValues.SessionDef.IsHuman)
            TouchFBController.EnableTouchFeedback(ShotgunHandler, TouchFeedbackDuration, ButtonScale * 10, WWW_CanvasGO);

        InitTrial.AddInitializationMethod(() =>
        {
            CurrentTaskLevel.SetBlockSummaryString();
            SetTrialSummaryString();
            if (TrialCount_InTask != 0)
                CurrentTaskLevel.SetTaskSummaryString();

            ShotgunHandler.HandlerActive = true;
            if (ShotgunHandler.AllSelections.Count > 0)
                ShotgunHandler.ClearSelections();
            ShotgunHandler.MinDuration = minObjectTouchDuration.value;
            ShotgunHandler.MaxDuration = maxObjectTouchDuration.value;
            ShotgunHandler.MaxPixelDisplacement = 50;

        });
        InitTrial.SpecifyTermination(() => ShotgunHandler.LastSuccessfulSelectionMatches(SessionValues.SessionDef.IsHuman ? SessionValues.HumanStartPanel.StartButtonChildren : SessionValues.USE_StartButton.StartButtonChildren), ChooseStimulusDelay, ()=>
        {
            CalculateSliderSteps();
            SliderFBController.ConfigureSlider(sliderSize.value, CurrentTrialDef.SliderInitial*(1f/sliderGainSteps));
            SliderFBController.SliderGO.SetActive(true);

            //numNonStimSelections_InBlock += mouseHandler.UpdateNumNonStimSelection(); //NT: Commented this out. not yet sure where we're gonna implement nonstim touches. Current method doesnt exist in new selection tracker.  

            SessionValues.EventCodeManager.SendCodeImmediate("StartButtonSelected");
            SessionValues.EventCodeManager.SendCodeNextFrame("StimOn");
            SessionValues.EventCodeManager.SendCodeNextFrame("SliderFbController_SliderReset");
            
        });
        ChooseStimulusDelay.AddTimer(() => chooseStimOnsetDelay.value, ChooseStimulus, ()=>
        {
            searchStims.ToggleVisibility(true);
            distractorStims.ToggleVisibility(true);
        });
        
        // Define ChooseStimulus state - Stimulus are shown and the user must select the correct object in the correct sequence
        ChooseStimulus.AddInitializationMethod(() =>
        {
            AssignCorrectStim();

            #if (!UNITY_WEBGL)
                if (GameObject.Find("MainCameraCopy").transform.childCount == 0)
                    CreateTextOnExperimenterDisplay();
            #endif

            choiceMade = false;
            if (CurrentTrialDef.LeaveFeedbackOn)
                HaloFBController.SetLeaveFeedbackOn();

            ShotgunHandler.HandlerActive = true;
            if (ShotgunHandler.AllSelections.Count > 0)
                ShotgunHandler.ClearSelections();
        });
        ChooseStimulus.AddUpdateMethod(() =>
        {
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
            CurrentTaskLevel.SetBlockSummaryString();

            CorrectSelection = selectedSD.IsCurrentTarget;

            if (CorrectSelection)
            {
                UpdateCounters_Correct();
                isSliderValueIncrease = true;
                SessionValues.EventCodeManager.SendCodeImmediate("CorrectResponse");
            }
            else
            {
                runningAcc.Add(0);
                UpdateCounters_Incorrect(correctIndex);
                isSliderValueIncrease = false;
                SessionValues.EventCodeManager.SendCodeImmediate("IncorrectResponse");

                //Repetition Error
                if (touchedObjects.Contains(selectedSD.StimIndex))
                {
                    repetitionErrorCount_InBlock++;
                    repetitionError = true;
                    SessionValues.EventCodeManager.SendCodeImmediate(TaskEventCodes["RepetitionError"]);
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
                        SessionValues.EventCodeManager.SendCodeImmediate("Button0PressedOnDistractorObject");//SELECTION STUFF (code may not be exact and/or could be moved to Selection handler)
                    }
                    //Stimuli Slot error
                    else
                    {
                        slotErrorCount_InBlock++;
                        slotError = true;
                        SessionValues.EventCodeManager.SendCodeImmediate(TaskEventCodes["SlotError"]);
                    }
                }
            }
        });
        ChooseStimulus.AddTimer(() => selectObjectDuration.value, ITI, () =>
        {
            consecutiveError++;
            aborted = true;
            runningAcc.Add(0);
            errorTypeString = "AbortedTrial";
            AbortedTrials_InBlock++;
            CurrentTaskLevel.AbortedTrials_InTask++;
            AbortCode = 6;

        });
        // ChooseStimulus.SpecifyTermination(() => trialComplete, FinalFeedback);

        SelectionFeedback.AddInitializationMethod(() =>
        {
            ShotgunHandler.HandlerActive = false;
            touchedObjects.Add(selectedSD.StimIndex);
            searchDuration = ChooseStimulus.TimingInfo.Duration;
            searchDurations.Add(searchDuration);
            searchDurations_InBlock.Add(searchDuration);
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
        SelectionFeedback.AddTimer(()=>fbDuration.value, Delay, () =>
        {
            DelayDuration = 0;
            
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
            ShotgunHandler.HandlerActive = false;

            trialComplete = false;
            startTime = Time.time;
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
                numRewardGiven_InBlock += CurrentTrialDef.NumPulses;
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
        ITI.AddInitializationMethod(() =>
        {
            searchStims.ToggleVisibility(false);
            distractorStims.ToggleVisibility(false);

            float latestAccuracy = -1;

            if (runningAcc.Count > 10)
            {
                latestAccuracy = ((runningAcc.Skip(Math.Max(0, runningAcc.Count - 10)).Sum() / 10f)*100);
                if (latestAccuracy > 70 && CurrentTaskLevel.LearningSpeed == -1)
                    CurrentTaskLevel.LearningSpeed = TrialCount_InBlock;
            }

            if(!SessionValues.WebBuild)
            {
                if (GameObject.Find("MainCameraCopy").transform.childCount != 0)
                    DestroyChildren(GameObject.Find("MainCameraCopy"));
            }
            
            if (NeutralITI)
            {
                ContextName = "itiImage";
                StartCoroutine(HandleSkybox(ContextExternalFilePath + Path.DirectorySeparatorChar + ContextName + ".png"));
                //RenderSettings.skybox = CreateSkybox(ContextExternalFilePath + Path.DirectorySeparatorChar + ContextName + ".png");
            }

            GenerateAccuracyLog();
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

        if (AbortCode == AbortCodeDict["RestartBlock"] || AbortCode == AbortCodeDict["PreviousBlock"] || AbortCode == AbortCodeDict["EndBlock"]) //If used RestartBlock, PreviousBlock, or EndBlock hotkeys
        {
            AbortedTrials_InBlock++;
            CurrentTaskLevel.AbortedTrials_InTask++;
           // CurrentTaskLevel.BlockSummaryString.Clear();
          //  CurrentTaskLevel.BlockSummaryString.AppendLine("");
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
        SliderFBController.ResetSliderBarFull();
    }

    public void ResetBlockVariables()
    {
        ErrorType_InBlock.Clear();
        slotErrorCount_InBlock = 0;
        distractorSlotErrorCount_InBlock = 0;
        repetitionErrorCount_InBlock = 0;
        AbortedTrials_InBlock = 0;
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
        FrameData.AddDatum("StartButton", () => StartButton.activeSelf);
        FrameData.AddDatum("SearchStimuliShown", () => searchStims.IsActive);
        FrameData.AddDatum("DistractorStimuliShown", () => distractorStims.IsActive);
        FrameData.AddDatum("Context", () => ContextName);
        
    }

    private void SetTrialSummaryString()
    {
     //   if (TrialCount_InBlock != 0)
       //     trialProgress = (decimal.Divide(numCorrect_InTrial.Sum(), numTotal_InTrial.Sum())).ToString();

        TrialSummaryString = "Selected Object Indices: " + string.Join(",",touchedObjects) +
                             "\nCorrect Selection? : " + CorrectSelection +
                           //  "\nTrial Progress: " + trialProgress + IMPROVE THE LOGIC IS OFF
                             "\n" +
                             "\nError: " + errorTypeString +
                             "\n" +
                             "\nSearch Duration: " + string.Join(",", searchDurations);
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
        for (int iStim = 0; iStim < CurrentTrialDef.CorrectObjectTouchOrder.Length; ++iStim)
        {
            //Create corresponding text on player view of experimenter display
            textLocation = ScreenToPlayerViewPosition(Camera.main.WorldToScreenPoint(searchStims.stimDefs[iStim].StimLocation),
                playerViewParent);
            textLocation.y += 75;
            Vector2 textSize = new Vector2(200, 200);
            playerViewText = playerView.CreateTextObject(CurrentTrialDef.CorrectObjectTouchOrder[iStim].ToString(),
                CurrentTrialDef.CorrectObjectTouchOrder[iStim].ToString(),
                Color.red, textLocation, textSize, playerViewParent);
            playerViewText.SetActive(true);
            playerViewText.GetComponent<RectTransform>().localScale = new Vector3(2, 2, 0);
            //should this ^ line be deleted and text size be congtrolled by textSize variable?
            playerViewTextList.Add(playerViewText);
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
       // searchStims.SetVisibilityOnOffStates(GetStateFromName("ChooseStimulus"), GetStateFromName("SelectionFeedback")); MAKING QUADDLES TWITCH BETWEEN STATES
     //   distractorStims.SetVisibilityOnOffStates(GetStateFromName("ChooseStimulus"), GetStateFromName("SelectionFeedback"));

        TrialStims.Add(searchStims);
        TrialStims.Add(distractorStims);
        
        randomizedLocations = CurrentTrialDef.RandomizedLocations; 

        if (randomizedLocations)
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














