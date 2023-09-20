using System;
using System.Collections.Generic;
using UnityEngine;
using USE_States;
using JoystickWWW_Namespace;
using USE_StimulusManagement;
using ConfigDynamicUI;
using System.Linq;
using System.IO;
using SelectionTracking;
using UnityEngine.Serialization;
using USE_ExperimentTemplate_Trial;
using USE_ExperimentTemplate_Task;

public class JoystickWWW_TrialLevel : ControlLevel_Trial_Template
{
    public GameObject JoystickWWW_CanvasGO;

    //This variable is required for most tasks, and is defined as the output of the GetCurrentTrialDef function 
    public JoystickWWW_TrialDef CurrentTrialDef => GetCurrentTrialDef<JoystickWWW_TrialDef>();
    public JoystickWWW_TaskLevel CurrentTaskLevel => GetTaskLevel<JoystickWWW_TaskLevel>();
    public JoystickWWW_TaskDef currentTaskDef => GetTaskDef<JoystickWWW_TaskDef>();
   
    // Block Ending Variable
    public List<float?> runningPercentError = new List<float?>();
    private float percentError;
    
    // game object variables
    private Texture2D texture;
    private static int numObjMax = 100;// need to change if stimulus exceeds this amount, not great
    
    // Config Variables
    public string ContextExternalFilePath;
    [FormerlySerializedAs("ButtonPosition")] public Vector3 ButtonPosition;
    [FormerlySerializedAs("ButtonScale")] public float ButtonScale;
    public string ShadowType;
    public bool NeutralITI;
    
    //stim group
    private StimGroup searchStims, distractorStims;
    private List<int> TouchedObjects = new List<int>();

    // feedback variables
    public int numTouchedStims = 0;
    private bool noSelection, trialComplete = false;
    private GameObject targetStimGameObject;
    private List<GameObject> GrayHalos = new List<GameObject>();


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
    public int consecutiveError = 0;
    private List<float?> SearchDurations_InTrial = new List<float?> { };
    private bool retouchLastCorrect = false;
    private int NumErrors_InTrial;
    private int NumCorrect_InTrial;
    
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
    private string touchedObjectsCodes, touchDurationTimes, searchDurationTimes, touchedPositions, searchStimLocations, distractorStimLocations;
    public string accuracyLog_InSession, accuracyLog_InBlock, accuracyLog_InTrial = "";
    
    private float touchDuration, searchDuration, sbDelay = 0;
    private bool halosDestroyed, slotError, distractorSlotError, touchDurationError, repetitionError, aborted = false;
    public string ContextName = "";
   // private List<int> trialPerformance = new List<int>();
    private int timeoutCondition = 3;
    private float totalFbDuration;
    
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
    public float ExternalStimScale;
    public GameObject instantiatedArena;
    //public GameObject instantiatedPlayer;

    
    // Stim Evaluation Variables
    private GameObject selectedGO = null;
    private bool CorrectSelection;
    private JoystickWWW_StimDef selectedSD = null;
    private int? stimIdx; // used to index through the arrays in the config file/mapping different columns
    private GameObject LastCorrectStimGO;
    private bool choiceMade = false;
    
    private SelectionTracking.SelectionTracker.SelectionHandler ShotgunHandler;



    public override void DefineControlLevel()
    {
        //---------------------------------------DEFINING STATES-----------------------------------------------------------------------
        State InitTrial = new State("InitTrial");
        State ChooseStimulus = new State("ChooseStimulus");
        State FlashNextCorrectStim = new State("FlashNextCorrectStim");
        State SelectionFeedback = new State("SelectionFeedback");
        State FinalFeedback = new State("FinalFeedback");
        State ITI = new State("ITI");
        AddActiveStates(new List<State>
        {
            InitTrial, ChooseStimulus, SelectionFeedback, FinalFeedback, ITI, FlashNextCorrectStim
        });

        string[] stateNames = new string[]
            {"InitTrial", "ChooseStimulus", "ChooseStimulusDelay", "SelectionFeedback", "FinalFeedback", "ITI", "ChooseStimulusDelay"};
        

        //player view variables
        playerView = new PlayerViewPanel(); //GameObject.Find("PlayerViewCanvas").GetComponent<PlayerViewPanel>()
        playerViewText = new GameObject();

        
        Add_ControlLevel_InitializationMethod(() =>
        {
            SliderFBController.InitializeSlider();
            
            
            // Initialize FB Controller Values
            HaloFBController.SetHaloSize(12);
            HaloFBController.SetHaloIntensity(5);

            instantiatedArena = Instantiate(Resources.Load<GameObject>("ArenaPrefab"));
            //instantiatedPlayer = Instantiate(Resources.Load<GameObject>("Player"));
            SessionValues.JoystickTracker.Player = GameObject.Find("Player");
            //SessionValues.JoystickTracker.Player = instantiatedPlayer;
            //SessionValues.JoystickTracker.playerCamTransform = TaskLevel.TaskCam.transform;
            instantiatedArena.SetActive(true);
            //instantiatedPlayer.SetActive(true);
            //Debug.Log("camera: " + instantiatedPlayer.transform.Find("JoystickWWW_Camera"));
            
            instantiatedArena.transform.position = new Vector3(0, 8, 0);
            instantiatedArena.transform.rotation = Quaternion.Euler(0, 0, 0);
            instantiatedArena.transform.localScale = new Vector3(2, 2, 2);
            
            //SessionValues.JoystickTracker.Player.transform.position = new Vector3(0, 0, 0);
            //instantiatedPlayer.transform.position = new Vector3(0, 0, 0);
            //instantiatedPlayer.transform.Find("Cylinder").transform.localPosition = new Vector3(0, 0, 0);
            //instantiatedPlayer.transform.Find("Cylinder").transform.localRotation = Quaternion.Euler(0, 0, 0);
            //CurrentTaskLevel.TaskCam.transform.SetParent(instantiatedPlayer.transform, false);
            //SessionValues.JoystickTracker.Player.transform.Find("JoystickWWW_Camera").transform.localPosition = new Vector3(0, 0, 0);
            //SessionValues.JoystickTracker.Player.transform.Find("JoystickWWW_Camera").transform.localRotation = Quaternion.Euler(0, 0, 0);
            //instantiatedPlayer.transform.Find("JoystickWWW_Camera").transform.localPosition = new Vector3(0, 0, 0);
            //instantiatedPlayer.transform.Find("JoystickWWW_Camera").transform.localRotation = Quaternion.Euler(0, 0, 0);
            
            SessionValues.JoystickTracker.isActive = true;
            
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
            SetShadowType(ShadowType, "JoystickWWW_DirectionalLight");
            
            UpdateExperimenterDisplaySummaryStrings();
            
            // Determine Start Button onset if the participant has made consecutive errors that exceed the error threshold
            if (consecutiveError >= CurrentTrialDef.ErrorThreshold)
                sbDelay = timeoutDuration.value;
            else
                sbDelay = startButtonDelay.value;
        });
        SetupTrial.AddTimer(()=> sbDelay, InitTrial);

        ShotgunHandler = SessionValues.SelectionTracker.SetupSelectionHandler("trial", "JoystickHandler", SessionValues.JoystickTracker, InitTrial, FinalFeedback);

        InitTrial.AddSpecificInitializationMethod(() =>
        {
            Camera.main.gameObject.GetComponent<Skybox>().enabled = false; //Disable cam's skybox so the RenderSettings.Skybox can show the Context background

            InitializeShotgunHandler();
        });
        InitTrial.SpecifyTermination(() => ShotgunHandler.LastSuccessfulSelectionMatchesStartButton(), Delay, ()=>
        {
            CalculateSliderSteps();
            SliderFBController.ConfigureSlider(sliderSize.value, CurrentTrialDef.SliderInitial*(1f/sliderGainSteps));
            SliderFBController.SliderGO.SetActive(true);
            SliderFBController.SetUpdateDuration(fbDuration.value);
            SliderFBController.SetFlashingDuration(flashingFbDuration.value);
            
            SessionValues.EventCodeManager.SendCodeNextFrame("SliderFbController_SliderReset");

            DelayDuration = chooseStimOnsetDelay.value;
            if (CurrentTrialDef.GuidedSequenceLearning)
                StateAfterDelay = FlashNextCorrectStim;
            else
                StateAfterDelay = ChooseStimulus;
            
        });
        
        FlashNextCorrectStim.AddSpecificInitializationMethod(() =>
        {
            AssignCorrectStim();
            HaloFBController.StartFlashingHalo(1f, 2, targetStimGameObject);
        });
        
        FlashNextCorrectStim.SpecifyTermination(()=> !HaloFBController.IsFlashing, ChooseStimulus);
       
        // Define ChooseStimulus state - Stimulus are shown and the user must select the correct object in the correct sequence
        ChooseStimulus.AddSpecificInitializationMethod(() =>
        {
            if (!CurrentTrialDef.GuidedSequenceLearning)
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
                selectedSD = selectedGO?.GetComponent<StimDefPointer>()?.GetStimDef<JoystickWWW_StimDef>();
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
                LastCorrectStimGO = selectedGO;
                CurrentTaskLevel.NumCorrectSelections_InBlock++;
                NumCorrect_InTrial++;
                isSliderValueIncrease = true;
                SessionValues.EventCodeManager.SendCodeImmediate("CorrectResponse");
            }
            else
            {
                runningAcc.Add(0);
                CurrentTaskLevel.NumErrors_InBlock++;
                NumErrors_InTrial++;
                //UpdateCounters_Incorrect(correctIndex);
                isSliderValueIncrease = false;
                SessionValues.EventCodeManager.SendCodeImmediate("IncorrectResponse");

                //Repetition Error
                if (TouchedObjects.Contains(selectedSD.StimIndex))
                {
                    CurrentTaskLevel.RepetitionErrorCount_InBlock++;
                    errorTypeString = "RepetitionError";
                    SessionValues.EventCodeManager.SendCodeImmediate(TaskEventCodes["RepetitionError"]);

                    if (selectedGO == LastCorrectStimGO)
                        retouchLastCorrect = true;
                    
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
            SessionValues.EventCodeManager.SendCodeNextFrame("NoChoice");
            SessionValues.EventCodeManager.SendRangeCode("CustomAbortTrial", AbortCodeDict["NoSelectionMade"]);
            AbortCode = 6;

            consecutiveError++;
            runningAcc.Add(0);
            SearchDurations_InTrial.Add(null);
            CurrentTaskLevel.SearchDurations_InBlock.Add(null);
            CurrentTaskLevel.SearchDurations_InTask.Add(null);
            errorTypeString = "AbortedTrial";
            
            runningPercentError.Add(null);
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
            
            int? depth = SessionValues.Using2DStim ? 50 : (int?)null;

            if (CorrectSelection)
            {
                if (GrayHalos.Count > 0)
                {
                    // if correcting a previous error, delete all the existing gray halos
                    foreach (GameObject grayHalo in GrayHalos)
                    {
                        Destroy(grayHalo);
                    }
                }
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
                
                // RETOUCH LAST CORRECT doesn't INCREMENT CONSECUTIVE ERROR
                if(!retouchLastCorrect)
                    consecutiveError++;

                if (GetRootObject(selectedGO.transform).transform.Find("NegativeHaloLight(Clone)")?.gameObject == null)
                {
                    HaloFBController.ShowNegative(selectedGO, depth);
                    GrayHalos.Add(GetRootObject(selectedGO.transform).transform.Find("NegativeHaloLight(Clone)").gameObject);
                }

                if (selectedSD.IsDistractor)
                    stimIdx = Array.IndexOf(CurrentTrialDef.DistractorStimIndices, selectedSD.StimIndex); // used to index through the arrays in the config file/mapping different columns
                else
                    stimIdx = Array.IndexOf(CurrentTrialDef.SearchStimIndices, selectedSD.StimIndex);

                
                if (CurrentTrialDef.BlockEndType == "CurrentTrialPerformance" && numTouchedStims != 0 && consecutiveError == 1)
                {
                    SliderFBController.UpdateSliderValue(-CurrentTrialDef.SliderLoss[(int)stimIdx]*(1f/sliderLossSteps)); // NOT IMPLEMENTED: NEEDS TO CONSIDER SEPARATE LOSS/GAIN FOR DISTRACTOR & TARGET STIMS SEPARATELY
                    numTouchedStims -= 1;
                }
                else if (CurrentTrialDef.BlockEndType == "SimpleThreshold")
                    SliderFBController.UpdateSliderValue(-CurrentTrialDef.SliderLoss[(int)stimIdx]*(1f/sliderLossSteps)); // NOT IMPLEMENTED: NEEDS TO CONSIDER SEPARATE LOSS/GAIN FOR DISTRACTOR & TARGET STIMS SEPARATELy
                

            }
            
            selectedGO = null;
        });
        
        //don't control timing with AddTimer, use slider class SliderUpdateFinished bool 
        SelectionFeedback.AddTimer(()=> fbDuration.value, Delay, () =>
        {
            DelayDuration = 0;
            
            if (!CurrentTrialDef.LeaveFeedbackOn) 
                HaloFBController.Destroy();
            
            UpdateExperimenterDisplaySummaryStrings();

            if (trialComplete)
                StateAfterDelay = FinalFeedback;
            else
            {
                // Condition where the trial ends after an error is made
                if (CurrentTrialDef.BlockEndType == "SimpleThreshold")
                {
                    if (CorrectSelection)
                    {
                        if (CurrentTrialDef.GuidedSequenceLearning)
                            StateAfterDelay = FlashNextCorrectStim;
                        else
                            StateAfterDelay = ChooseStimulus;
                    }
                    else
                        StateAfterDelay = ITI;
                }
                
                // Condition where the trial continues even if an error is made
                else if (CurrentTrialDef.BlockEndType == "CurrentTrialPerformance")
                {
                    if (CurrentTrialDef.GuidedSequenceLearning)
                        StateAfterDelay = FlashNextCorrectStim;
                    else
                        StateAfterDelay = ChooseStimulus;
                }
            }

            CorrectSelection = false;
            
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
            
            percentError = (float)decimal.Divide(NumErrors_InTrial, CurrentTrialDef.CorrectObjectTouchOrder.Length);
            runningPercentError.Add(percentError);
            
            SessionValues.EventCodeManager.SendCodeNextFrame("SliderFbController_SliderCompleteFbOn");
                        
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
                ContextName = "NeutralITI";
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
       
        // If there is a MaxCorrectTrials defined, end the block when the minimum number of trials is run and the maximum number of correct trials is achieved
        if (CurrentTrialDef.MaxCorrectTrials != 0)
            return ( TrialCount_InBlock >= CurrentTaskLevel.MinTrials_InBlock && runningAcc.Count(num => num == 1) >= CurrentTrialDef.MaxCorrectTrials);
        
        // If using the SimpleThreshold block end, use the following CheckBlockEnd method
        if (CurrentTrialDef.BlockEndType == "SimpleThreshold")
            return TaskLevel_Methods.CheckBlockEnd(CurrentTrialDef.BlockEndType, runningAcc,
                CurrentTrialDef.BlockEndThreshold, CurrentTrialDef.BlockEndWindow, CurrentTaskLevel.MinTrials_InBlock,
                CurrentTrialDef.MaxTrials);
        
        // If using the CurrentTrialPerformance block end, use the following CheckBlockEnd method
        if (CurrentTrialDef.BlockEndType == "CurrentTrialPerformance")
            return TaskLevel_Methods.CheckBlockEnd(CurrentTrialDef.BlockEndType, runningPercentError,
                CurrentTrialDef.BlockEndThreshold, CurrentTaskLevel.MinTrials_InBlock,
                CurrentTaskLevel.MaxTrials_InBlock);
         
        Debug.Log($"Cannot handle {CurrentTrialDef.BlockEndType} Block End Type. Forced block switch not applied.");
         return false;
        
         
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
        NumErrors_InTrial = 0;
        NumCorrect_InTrial = 0;

        numTouchedStims = 0;
        searchDuration = 0;
        sliderGainSteps = 0;
        sliderLossSteps = 0;
        stimIdx = null;
        selectedGO = null;
        selectedSD = null;
        LastCorrectStimGO = null;
        CorrectSelection = false;
        choiceMade = false;
        retouchLastCorrect = false;
        
        SearchDurations_InTrial.Clear();
        TouchedObjects.Clear();
        errorTypeString = "";
        SliderFBController.ResetSliderBarFull();
        GrayHalos.Clear();
    }

    


    //-----------------------------------------------------------------METHODS FOR DATA HANDLING----------------------------------------------------------------------
    private void DefineTrialData() //All ".AddDatum" commands for Trial Data
    {
        TrialData.AddDatum("TrialID", () => CurrentTrialDef.TrialID); //NaN if only using blockdef structure
        TrialData.AddDatum("ContextName", () => CurrentTrialDef.ContextName);
        TrialData.AddDatum("SearchStimLocations", () => searchStimLocations);
        TrialData.AddDatum("DistractorStimLocations", () => distractorStimLocations);
        TrialData.AddDatum("TouchedObjects", () => String.Join(",",TouchedObjects));
        TrialData.AddDatum("SearchDurations", () => String.Join(",",SearchDurations_InTrial));
        TrialData.AddDatum("ErrorType", () => errorTypeString);
    }
    private void DefineFrameData() //All ".AddDatum" commands for Frame Data
    {
        FrameData.AddDatum("ContextName", () => ContextName);
        FrameData.AddDatum("StartButton", () => StartButton.activeSelf);
        FrameData.AddDatum("SearchStimuliShown", () => searchStims.IsActive);
        FrameData.AddDatum("DistractorStimuliShown", () => distractorStims.IsActive);
    }

    private void SetTrialSummaryString()
    {
        TrialSummaryString = "Selected Object Indices: " + string.Join(",",TouchedObjects) +
                             "\nCorrect Selection? : " + CorrectSelection +
                             "\nPercent Error : " + percentError +
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
        searchStims = new StimGroup("SearchStims", group, CurrentTrialDef.SearchStimIndices);
        distractorStims = new StimGroup("DistractorStims", group, CurrentTrialDef.DistractorStimIndices);

        if (CurrentTrialDef.GuidedSequenceLearning)
        {
            searchStims.SetVisibilityOnOffStates(GetStateFromName("FlashNextCorrectStim"), GetStateFromName("ITI"));
            distractorStims.SetVisibilityOnOffStates(GetStateFromName("FlashNextCorrectStim"), GetStateFromName("ITI"));
        }
        else
        {
            searchStims.SetVisibilityOnOffStates(GetStateFromName("ChooseStimulus"), GetStateFromName("ITI"));
            distractorStims.SetVisibilityOnOffStates(GetStateFromName("ChooseStimulus"), GetStateFromName("ITI"));
        }
        
        TrialStims.Add(searchStims);
        TrialStims.Add(distractorStims);
        
        if (CurrentTrialDef.RandomizedLocations)
        {
            var totalStims = searchStims.stimDefs.Concat(distractorStims.stimDefs);
            var stimLocations = CurrentTrialDef.SearchStimLocations.Concat(CurrentTrialDef.DistractorStimLocations);

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
            searchStims.SetLocations(CurrentTrialDef.SearchStimLocations);
            distractorStims.SetLocations(CurrentTrialDef.DistractorStimLocations);
        }

        searchStimLocations = String.Join(",", searchStims.stimDefs.Select(s => s.StimLocation));
        distractorStimLocations = String.Join(",", distractorStims.stimDefs.Select(d => d.StimLocation));
    }
    
    //-------------------------------------------------------------MISCELLANEOUS METHODS--------------------------------------------------------------------------
    private void AssignCorrectStim()
    {
        targetStimGameObject = null;
        
        //if we haven't finished touching all stims
        if (numTouchedStims < CurrentTrialDef.CorrectObjectTouchOrder.Length)
        {
            //find which stimulus is currently target
            correctIndex = CurrentTrialDef.CorrectObjectTouchOrder[numTouchedStims] - 1;

            for (int iStim = 0; iStim < CurrentTrialDef.CorrectObjectTouchOrder.Length; iStim++)
            {
                JoystickWWW_StimDef sd = (JoystickWWW_StimDef) searchStims.stimDefs[iStim];
                if (iStim == correctIndex)
                {
                    sd.IsCurrentTarget = true;
                    targetStimGameObject = sd.StimGameObject;
                }
                else 
                    sd.IsCurrentTarget = false;
            }
        
            for (int iDist = 0; iDist < CurrentTrialDef.DistractorStimIndices.Length; ++iDist)
            {
                JoystickWWW_StimDef sd = (JoystickWWW_StimDef) distractorStims.stimDefs[iDist];
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
            StartButton = SessionValues.USE_StartButton.CreateStartButton(JoystickWWW_CanvasGO.GetComponent<Canvas>(), currentTaskDef.StartButtonPosition, currentTaskDef.StartButtonScale);
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
    GameObject GetRootObject(Transform childTransform)
    {
        Transform currentTransform = childTransform;

        // Traverse up the hierarchy until we find the root object.
        while (currentTransform.parent != null)
        {
            currentTransform = currentTransform.parent;
        }

        // The currentTransform now points to the root object's transform.
        return currentTransform.gameObject;
    }
}