using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using USE_States;
using USE_ExperimentTemplate;
using WhatWhenWhere_Namespace;
using USE_StimulusManagement;
using ConfigDynamicUI;
using System.Collections;
using USE_Settings;
using USE_DisplayManagement;
using System.Linq;
using System.IO;
using USE_ExperimentTemplate_Classes;
using TriLib;

public class WhatWhenWhere_TrialLevel : ControlLevel_Trial_Template
{
    //This variable is required for most tasks, and is defined as the output of the GetCurrentTrialDef function 
    public WhatWhenWhere_TrialDef CurrentTrialDef => GetCurrentTrialDef<WhatWhenWhere_TrialDef>();
    
    // game object variables
    private GameObject initButton, chosenStim, grayHalo, grayHaloScene, yellowHalo, sliderHalo, imageTimingError;
    private Image sr;
    private Texture2D texture;
    private static int numObjMax = 50;// need to change if stimulus exceeds this amount, not great

    //stim group
    private StimGroup searchStims, distractorStims;
    private List<string> touchedObjects = new List<string>();
    private bool randomizedLocations = false;

    // feedback variables
    public int stimCount = 0;
    private bool correctChoice = false;
    private bool incorrectChoice = false;
    private bool timingFail = false;
    private bool irrelevantSelection = false;
    private bool noSelection = false;
    private bool trialComplete = false;
    public List<int> runningAcc;
    
    // error data variables
    private int slotError = 0;
    private int distractorSlotError = 0;
    private int touchDurationError = 0;
    private int irrelevantSelectionError = 0;
    private int repetitionError = 0;
    private int totalErrors_InSession = 0;
    public int totalErrors_InBlock = 0;
    private int noScreenTouchError = 0;
    private string errorTypeString = "";
    public List<String> errorType_InBlock = new List<String> { };
    public List<String> errorType_InSession = new List<String> { };
    public string errorType_InBlockString = "";
    private string errorType_InSessionString = "";
    private float startTime;
    private List<Color> contextColors = new List<Color> { };
    private int[] numTotal_InSession = new int[numObjMax];
    public int[] numTotal_InBlock = new int[numObjMax];
    public int[] numTotal_InTrial = new int[numObjMax];
    private int[] numErrors_InSession = new int[numObjMax];
    public int[] numErrors_InBlock = new int[numObjMax];
    private int[] numErrors_InTrial = new int[numObjMax];
    private int[] numCorrect_InSession = new int[numObjMax];
    public int[] numCorrect_InBlock = new int[numObjMax];
    private int[] numCorrect_InTrial = new int[numObjMax];
    private List<float> touchDurations = new List<float> { };
    private List<float> choiceDurations = new List<float> { };
    private List<Vector3> touchedPositionsList = new List<Vector3>(); // empty now

    [HideInInspector] public ConfigNumber minObjectTouchDuration,
        itiDuration,
        finalFbDuration,
        fbDuration,
        maxObjectTouchDuration,
        selectObjectDuration,
        sliderSize,
        chooseStimOnsetDelay;
    //data logging variables
    private string touchedObjectsNames;
    private string touchDurationTimes;
    private string choiceDurationTimes;
    private string touchedPositions;
    private string searchStimsLocations;
    private string distractorStimsLocations;
    public string accuracyLog_InSession = "";
    public string accuracyLog_InBlock = "";
    public string accuracyLog_InTrial = "";
    private float initialTouchTime = 0;
    private float touchDuration = 0;
    private float choiceDuration = 0;
    private bool initialTouch = false;
    private bool choiceMade = false;
    private bool contextActive = false;
    private String contextName = "";
    private List<int> trialPerformance = new List<int>();

    [HideInInspector]
    [System.NonSerialized] public int response = -1;
    // vector3 variables
    private Vector3 trialStimInitLocalScale;
    private Vector3 fbInitLocalScale;
    private Vector3 sliderInitPosition;
    private Vector3 touchPosition;

    // misc variables
    private Ray mouseRay;
    private Slider slider;
    private float sliderValueIncreaseAmount;
    private Camera cam;
    private bool variablesLoaded;
    public string MaterialFilePath;
    private int correctIndex;
    private GameObject sbOther;
    public int sliderCompleteQuantity = 0;

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

    public override void DefineControlLevel()
    {
        // --------------------------------------ADDING PLAYER VIEW STUFF------------------------------------------------------------------------------------

        MonitorDetails primaryMonitorDetails = new MonitorDetails(new Vector2(1920, 1080), new Vector2(10, 7), 2);

        //---------------------------------------DEFINING STATES-----------------------------------------------------------------------
        State StartButton = new State("StartButton");
        State ChooseStimulus = new State("ChooseStimulus");
        State ChooseStimulusDelay = new State("ChooseStimulusDelay");
        State StimulusChosen = new State("StimulusChosen");
        State FinalFeedback = new State("FinalFeedback");
        State ITI = new State("ITI");
        State StimulusChosenSuccesorState = new State("StimulusChosenSuccesorState");
        State delay = new State("Delay");

        AddActiveStates(new List<State>
        {
            StartButton, ChooseStimulus, StimulusChosen, FinalFeedback, ITI, StimulusChosenSuccesorState,
            ChooseStimulusDelay, delay
        });

        string[] stateNames = new string[]
            {"StartButton", "ChooseStimulus", "StimulusChosen", "FinalFeedback", "ITI", "Delay", "ChooseStimulusDelay"};

        // A state that just waits for some time
        State stateAfterDelay = null;
        float delayDuration = 0;
        delay.AddTimer(() => delayDuration, () => stateAfterDelay);

        //MouseTracker variables
        SelectionHandler<WhatWhenWhere_StimDef> gazeSelectionHandler = new SelectionHandler<WhatWhenWhere_StimDef>();
        SelectionHandler<WhatWhenWhere_StimDef> mouseSelectionHandler = new SelectionHandler<WhatWhenWhere_StimDef>();
        GazeTracker.SpoofGazeWithMouse = true;

        //EventCodeManager.SendCodeImmediate(3);

        AddInitializationMethod(() =>
        {
            playerView = new PlayerViewPanel(); //GameObject.Find("PlayerViewCanvas").GetComponent<PlayerViewPanel>()
            playerViewText = new GameObject();
        });

        SetupTrial.AddInitializationMethod(() =>
        {
            ClearDataLogging();

            if (!variablesLoaded)
            {
                variablesLoaded = true;
                loadVariables();
            }

            // resetting data information on the Experimenter Display Trial Info Panel
            TrialSummaryString = "Trial Num: " + (TrialCount_InTask + 1) + "\nError Type: " +
                                 errorTypeString + "\nTouch Duration: " + touchDurationTimes + "\nChoice Duration: " +
                                 choiceDurationTimes + "\nProgress: " + accuracyLog_InTrial + "\nSession Progress: " +
                                 accuracyLog_InSession;
            ;
        });

        SetupTrial.SpecifyTermination(() => true, StartButton);

        // define StartButton state
        StartButton.AddInitializationMethod(() =>
        {
            // Set the background texture to that of specified context
            contextActive = true;
            contextName = CurrentTrialDef.ContextName;
            RenderSettings.skybox = CreateSkybox(MaterialFilePath + "\\" + CurrentTrialDef.ContextName + ".png");
            ResetRelativeStartTime();
            Debug.Log("Current Block Context: " + CurrentTrialDef.ContextName);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["ContextOn"]);
            initButton.SetActive(true);
            // goCue.SetActive(true);
        });
        StartButton.AddUpdateMethod(() =>
        {
            if (InputBroker.GetMouseButtonDown(0))
            {
                mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(mouseRay, out hit))
                {
                    if (hit.transform.name == "StartButton")
                    {
                        response = 0;
                        EventCodeManager.SendCodeImmediate(TaskEventCodes["StartButtonSelected"]);
                    }
                }
            }
        });
        StartButton.SpecifyTermination(() => response == 0, ChooseStimulusDelay);
        StartButton.AddDefaultTerminationMethod(() =>
        {
            slider.value = 0;
            slider.gameObject.transform.position = sliderInitPosition;
            sliderHalo.gameObject.transform.position = sliderInitPosition;
            int totalNumSteps = CurrentTrialDef.SliderGain.Sum() + CurrentTrialDef.SliderInitial;
            sliderValueIncreaseAmount = (100f / totalNumSteps) / 100f;
            slider.transform.localScale = new Vector3(sliderSize.value / 10f, sliderSize.value / 10f, 1f);
            sliderHalo.transform.localScale = new Vector3(sliderSize.value / 10f, sliderSize.value / 10f, 1f);

            if (CurrentTrialDef.SliderInitial != 0)
            {
                slider.value += sliderValueIncreaseAmount * (CurrentTrialDef.SliderInitial);
            }

            slider.gameObject.SetActive(true);
            initButton.SetActive(false);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["StimOn"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["SliderReset"]);
        });
        ChooseStimulusDelay.AddTimer(() => chooseStimOnsetDelay.value, delay,
            () => { stateAfterDelay = ChooseStimulus; });
        GazeTracker.AddSelectionHandler(gazeSelectionHandler, ChooseStimulus);
        MouseTracker.AddSelectionHandler(mouseSelectionHandler, ChooseStimulus);

        // Define ChooseStimulus state - Stimulus are shown and the user must select the correct object in the correct sequence
        ChooseStimulus.AddInitializationMethod(() =>
        {
            
            if (stimCount < CurrentTrialDef.CorrectObjectTouchOrder.Length)
            {
                correctIndex = CurrentTrialDef.CorrectObjectTouchOrder[stimCount] - 1;

                for (int i = 0; i < CurrentTrialDef.CorrectObjectTouchOrder.Length; ++i)
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
                // foreach (StimDef sd in searchStims.stimDefs) Debug.Log(sd.StimCode);
                // foreach (StimDef sd in distractorStims.stimDefs) Debug.Log(sd.StimCode);
            }
            else
            {
                trialComplete = true;
                slider.value += sliderValueIncreaseAmount * (CurrentTrialDef.SliderGain[stimCount - 1]);
            }

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

            searchStims.ToggleVisibility(true);
            distractorStims.ToggleVisibility(true);
            chosenStim = null;
            initialTouch = false;
            choiceMade = false;
        });
        ChooseStimulus.AddUpdateMethod(() =>
        {
            // Check if user makes a selection
            if (Input.GetMouseButtonDown(0))
            {
                initialTouchTime = Time.time;
                initialTouch = true;
                choiceDuration = initialTouchTime - ChooseStimulus.TimingInfo.StartTimeAbsolute;
                choiceDurations.Add(choiceDuration);
            }

            if (Input.GetMouseButtonUp(0) && initialTouch)
            {
                touchDuration = Time.time - initialTouchTime;
                touchDurations.Add(touchDuration);
                mouseRay = Camera.main.ScreenPointToRay(InputBroker.mousePosition);
                var clickPoint = Input.mousePosition;
                touchedPositionsList.Add(new Vector3(clickPoint[0], clickPoint[1], clickPoint[2]));

                RaycastHit hit;
                if (Physics.Raycast(mouseRay, out hit))
                {
                    choiceMade = true;
                    correctIndex = CurrentTrialDef.CorrectObjectTouchOrder[stimCount] - 1;
                    chosenStim = hit.transform.gameObject;
                }
                else
                {
                    Debug.Log("Clicked within the scene, but not on a stimulus");

                    response = 2;
                    var screenPoint = Input.mousePosition;
                    screenPoint.z = 100.0f; //distance of the plane from the camera
                    grayHaloScene.transform.position = Camera.main.ScreenToWorldPoint(screenPoint);
                    irrelevantSelectionError += 1;
                    totalErrors_InSession += 1;
                    totalErrors_InBlock += 1;
                    irrelevantSelection = true;
                    slider.value -= sliderValueIncreaseAmount;

                    EventCodeManager.SendCodeImmediate(TaskEventCodes["TouchIrrelevantStart"]);
                    EventCodeManager.SendCodeNextFrame(TaskEventCodes["Unrewarded"]);
                    EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionVisualFbOn"]);
                    EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionAuditoryFbOn"]);
                }
            }

            if (choiceMade)
            {
                GameObject testStim = chosenStim.transform.root.gameObject;
                response = 1;
                //Timing Error
                if (touchDuration < minObjectTouchDuration.value || touchDuration > maxObjectTouchDuration.value)
                {
                    Debug.Log("Did not click on stimulus for long enough");
                    timingFail = true;
                    touchDurationError += 1;
                    totalErrors_InSession += 1;
                    totalErrors_InBlock += 1;
                    touchedObjects.Add(testStim.name);
                    //slider.value -= sliderValueIncreaseAmount;
                    numErrors_InBlock[correctIndex]++;
                    numErrors_InSession[correctIndex]++;

                    EventCodeManager.SendCodeImmediate(TaskEventCodes["TouchDurationError"]);
                    EventCodeManager.SendCodeNextFrame(TaskEventCodes["TouchErrorImageOn"]);
                    EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionAuditoryFbOn"]);
                }
                //Correct Selection
                else if (testStim.GetComponent<StimDefPointer>().GetStimDef<WhatWhenWhere_StimDef>().IsCurrentTarget)
                {
                    Debug.Log("Clicked on the correct stimulus within the sequence");
                    CorrectSelectionProgressData();
                    slider.value += sliderValueIncreaseAmount * (CurrentTrialDef.SliderGain[stimCount]);
                    stimCount += 1;
                    touchedObjects.Add(testStim.name);
                    yellowHalo.transform.position = testStim.transform.position;
                    EventCodeManager.SendCodeImmediate(TaskEventCodes["CorrectResponse"]);
                    EventCodeManager.SendCodeImmediate(TaskEventCodes["TouchTargetStart"]);
                    EventCodeManager.SendCodeNextFrame(TaskEventCodes["Rewarded"]);
                    EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionVisualFbOn"]);
                    EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionAuditoryFbOn"]);
                }
                //Repetition Error
                else if (touchedObjects.Contains(testStim.name))
                {
                    Debug.Log("Clicked on a stimulus, but repeated a previous selection");
                    grayHalo.transform.position = testStim.transform.position;
                    touchedObjects.Add(testStim.name);

                    IncorrectSelectionProgressData(correctIndex);

                    slider.value -= sliderValueIncreaseAmount * (CurrentTrialDef.SliderGain[stimCount]);
                    repetitionError += 1;

                    EventCodeManager.SendCodeImmediate(TaskEventCodes["IncorrectResponse"]);
                    EventCodeManager.SendCodeImmediate(TaskEventCodes["RepetitionError"]);
                    EventCodeManager.SendCodeImmediate(TaskEventCodes["TouchTargetStart"]);
                    EventCodeManager.SendCodeNextFrame(TaskEventCodes["Unrewarded"]);
                    EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionVisualFbOn"]);
                    EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionAuditoryFbOn"]);
                }
                //Slot Errors
                else
                {
                    //Distractor Error
                    if (testStim.GetComponent<StimDefPointer>().GetStimDef<WhatWhenWhere_StimDef>().IsDistractor)
                    {
                        Debug.Log("Clicked on a distractor");
                        grayHalo.transform.position = testStim.transform.position;
                        touchedObjects.Add(testStim.name);
                        slider.value -= sliderValueIncreaseAmount * (CurrentTrialDef.SliderGain[stimCount]);
                        distractorSlotError += 1;

                        IncorrectSelectionProgressData(correctIndex);

                        EventCodeManager.SendCodeImmediate(TaskEventCodes["IncorrectResponse"]);
                        EventCodeManager.SendCodeImmediate(TaskEventCodes["TouchDistractorStart"]);

                        EventCodeManager.SendCodeNextFrame(TaskEventCodes["Unrewarded"]);
                        EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionVisualFbOn"]);
                        EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionAuditoryFbOn"]);
                    }

                    //Stimuli Slot error
                    else
                    {
                        Debug.Log("Clicked on a stimulus, but not within the correct sequence");
                        grayHalo.transform.position = testStim.transform.position;
                        touchedObjects.Add(testStim.name);
                        slider.value -= sliderValueIncreaseAmount * (CurrentTrialDef.SliderGain[stimCount]);
                        slotError += 1;
                        IncorrectSelectionProgressData(correctIndex);

                        EventCodeManager.SendCodeImmediate(TaskEventCodes["IncorrectResponse"]);
                        EventCodeManager.SendCodeImmediate(TaskEventCodes["SlotError"]);
                        EventCodeManager.SendCodeImmediate(TaskEventCodes["TouchTargetStart"]);

                        EventCodeManager.SendCodeNextFrame(TaskEventCodes["Unrewarded"]);
                        EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionVisualFbOn"]);
                        EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionAuditoryFbOn"]);

                    }

                }
            }
        });
        ChooseStimulus.AddTimer(() => selectObjectDuration.value, ITI);
        ChooseStimulus.SpecifyTermination(() => response == 1,
            StimulusChosen); // Response ==1 means "Clicked on a stimulus" and is evaluated for errors
        ChooseStimulus.SpecifyTermination(() => response == 2,
            StimulusChosen); // Response == 2 means "Clicked within the scene, but not on a stimulus"
        ChooseStimulus.SpecifyTermination(() => trialComplete, FinalFeedback);

        StimulusChosen.AddInitializationMethod(() =>
        {
            response = -1;

            // Timing Fail
            if (timingFail)
            {
                imageTimingError.transform.SetAsLastSibling();
                imageTimingError.SetActive(true);
                errorTypeString = "TouchDurationError";
                runningAcc.Add(0);
                trialPerformance.Add(0);
            }

            //Chose Incorrect
            else if (incorrectChoice)
            {
                grayHalo.SetActive(true);
                sliderHalo.SetActive(true);
                sr.color = new Color(0.6627f, 0.6627f, 0.6627f, 0.2f);
                runningAcc.Add(0);
                if (slotError == 1)
                    errorTypeString = "SlotError";
                else if (distractorSlotError == 1)
                    errorTypeString = "DistractorSlotError";
                else
                    errorTypeString = "RepetitionError";
                trialPerformance.Add(0);
            }

            //Irrelevant Selection
            else if (irrelevantSelection)
            {
                errorTypeString = "IrrelevantSelectionError";
                runningAcc.Add(0);
                trialPerformance.Add(0);
            }

            //Chose correct
            else if (correctChoice)
            {
                yellowHalo.SetActive(true);
                sliderHalo.SetActive(true);
                sr.color = new Color(1, 0.8431f, 0, 0.2f);
                errorTypeString = "None";
                trialPerformance.Add(1);
            }

            GenerateUpdatingTrialData();
            // logging data information on the Experimenter Display Trial Info Panel
            TrialSummaryString = "Trial Num: " + (TrialCount_InTask + 1) + "\nError Type: " + errorTypeString +
                                 "\nTouch Duration: " +
                                 touchDurationTimes +
                                 "\nChoice Duration: " + choiceDurationTimes + "\nPerformance: " + accuracyLog_InTrial +
                                 "\nSession Performance: " + accuracyLog_InSession;

        });

        StimulusChosen.SpecifyTermination(
            () => (correctChoice && Time.time - StimulusChosen.TimingInfo.StartTimeAbsolute >= fbDuration.value),
            ChooseStimulus, () =>
            {
                yellowHalo.SetActive(false);
                correctChoice = false;
                sliderHalo.SetActive(false);
                EventCodeManager.SendCodeImmediate(TaskEventCodes["SelectionVisualFbOff"]);
            });

        StimulusChosen.SpecifyTermination(
            () => (incorrectChoice && Time.time - StimulusChosen.TimingInfo.StartTimeAbsolute >= fbDuration.value), ITI,
            () =>
            {
                grayHalo.SetActive(false);
                incorrectChoice = false;
                sliderHalo.SetActive(false);
                EventCodeManager.SendCodeImmediate(TaskEventCodes["SelectionVisualFbOff"]);
            });

        StimulusChosen.SpecifyTermination(
            () => (timingFail && (Time.time - StimulusChosen.TimingInfo.StartTimeAbsolute) >= fbDuration.value), ITI,
            () =>
            {
                imageTimingError.SetActive(false);
                timingFail = false;
                EventCodeManager.SendCodeImmediate(TaskEventCodes["TouchErrorImageOff"]);
            });

        StimulusChosen.SpecifyTermination(
            () => (irrelevantSelection &&
                   (Time.time - StimulusChosen.TimingInfo.StartTimeAbsolute) >= fbDuration.value), ITI, () =>
            {
                grayHaloScene.SetActive(false);
                irrelevantSelection = false;
                EventCodeManager.SendCodeImmediate(TaskEventCodes["SelectionVisualFbOff"]);
            });

        FinalFeedback.AddInitializationMethod(() =>
        {
            trialComplete = false;
            sliderHalo.SetActive(true);
            sr.color = new Color(1, 1, 1, 0.2f);
            startTime = Time.time;
            errorTypeString = "None";
            searchStims.ToggleVisibility(false);
            distractorStims.ToggleVisibility(false);
            response = -1;
            
            runningAcc.Add(1);
            sliderCompleteQuantity += 1;
            foreach (GameObject txt in playerViewTextList)
            {
                txt.SetActive(false);
            }

            EventCodeManager.SendCodeNextFrame(TaskEventCodes["SliderCompleteFbOn"]);
            SyncBoxController.SendRewardPulses(CurrentTrialDef.NumPulses, CurrentTrialDef.PulseSize);
            //SyncBoxController.SendRewardPulses(3, 500);
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
            sliderHalo.SetActive(false);
            EventCodeManager.SendCodeImmediate(TaskEventCodes["SliderCompleteFbOff"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["ContextOff"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["StimOff"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["TrlEnd"]);
        });

        //Define iti state
        ITI.AddInitializationMethod(() =>
        {
            searchStims.ToggleVisibility(false);
            distractorStims.ToggleVisibility(false);
            contextActive = false;
            contextName = "itiImage";
            RenderSettings.skybox = CreateSkybox(MaterialFilePath + "\\itiImage.png");
            playerViewLoaded = false;
            //Destroy all created text objects on Player View of Experimenter Display
            foreach (GameObject txt in playerViewTextList)
            {
                Destroy(txt);
            }

            playerViewTextList.Clear();
            slider.gameObject.SetActive(false);

            if (response == 0)
            {
                noScreenTouchError++;
                totalErrors_InSession += 1;
                totalErrors_InBlock += 1;
                errorTypeString = "NoSelectionMade";
                Debug.Log("Didn't click on any stimulus");
                response = -1;
                runningAcc.Add(0);

                EventCodeManager.SendCodeImmediate(TaskEventCodes["NoChoice"]);
            }

            GenerateFinalTrialData();
        });
        ITI.AddTimer(() => itiDuration.value, FinishTrial, () =>
        {
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["TrlStart"]);
            Debug.Log("Trial " + TrialCount_InTask + " completed");
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
            TrialDefs.Length);
    }
    //-----------------------------------------------------------------METHODS FOR DATA HANDLING----------------------------------------------------------------------
    private void LogTrialData() //All ".AddDatum" commands for Trial Data
    {
        TrialData.AddDatum("TrialID", () => CurrentTrialDef.TrialID);
        TrialData.AddDatum("Context", () => CurrentTrialDef.ContextName);
        TrialData.AddDatum("SearchStimsLocations", () => searchStimsLocations);
        TrialData.AddDatum("DistractorStimsLocations", () => distractorStimsLocations);
        TrialData.AddDatum("TouchedObjects", () => touchedObjectsNames);
        TrialData.AddDatum("TouchPositions", () => touchedPositions);
        TrialData.AddDatum("TrialPerformance", () => trialPerformance);
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
        FrameData.AddDatum("StartButton", () => initButton.activeSelf);
        FrameData.AddDatum("GrayHaloFeedback", () => (grayHalo.activeSelf || grayHaloScene.activeSelf));
        FrameData.AddDatum("YellowHaloFeedback", () => yellowHalo.activeSelf);
        FrameData.AddDatum("TimingErrorFeedback", () => imageTimingError.activeSelf);
        FrameData.AddDatum("SliderHalo", () => sliderHalo.activeSelf);
        FrameData.AddDatum("Slider", () => slider.gameObject.activeSelf);
        FrameData.AddDatum("SearchStimuliShown", () => searchStims.IsActive);
        FrameData.AddDatum("DistractorStimuliShown", () => distractorStims.IsActive);
        FrameData.AddDatum("SliderValue", () => slider.normalizedValue);
        FrameData.AddDatum("Context", () => contextName);
        FrameData.AddDatum("ContextActive", () => contextActive);
        
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
        touchedObjectsNames = "[";
        for (int i = 0; i < touchedObjects.Count; ++i)
        {
            if (i < touchedObjects.Count - 1)
            {
                touchedObjectsNames = touchedObjectsNames + touchedObjects[i] + ",";
            }
            else
            {
                touchedObjectsNames = touchedObjectsNames + touchedObjects[i];
            }
        }
        touchedObjectsNames = touchedObjectsNames + "]";

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
        touchedPositions = touchedPositions + "]";
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
        slotError = 0;
        distractorSlotError = 0;
        repetitionError = 0;
        touchDurationError = 0;
        irrelevantSelectionError = 0;
        noScreenTouchError = 0;
        initialTouch = false;
        choiceMade = false;
        contextActive = false;
        stimCount = 0;
        response = -1;
        touchedObjects.Clear();
        touchDurations.Clear();
        choiceDurations.Clear();
        touchedPositionsList.Clear();
        trialPerformance.Clear();

        touchedObjectsNames = "[]";
        touchDurationTimes = "[]";
        choiceDurationTimes = "[]";
        touchedPositions = "[]";
        touchedObjectsNames = "[]";
        searchStimsLocations = "[]";
        distractorStimsLocations = "[]";
        errorTypeString = "";
    } 
    private void DataConsoleMessages() //Generates Debug.Log messages of the data strings
    {
        Debug.Log("Progress_InSession: " + accuracyLog_InSession);
        Debug.Log("Progress_InBlock: " + accuracyLog_InBlock);
        Debug.Log("Progress_InTrial: " + accuracyLog_InTrial);
        Debug.Log("Touched Objects: " + touchedObjectsNames);
        Debug.Log("Touch Durations: " + touchDurationTimes);
        Debug.Log("Choice Durations: " + choiceDurationTimes);
        Debug.Log("Touched Positions: " + touchedPositions);
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
    void disableAllGameobjects()
    {
        initButton.SetActive(false);
        sliderHalo.SetActive(false);
        grayHalo.SetActive(false);
        yellowHalo.SetActive(false);
        grayHaloScene.SetActive(false);
        imageTimingError.SetActive(false);
        searchStims.ToggleVisibility(false);
        distractorStims.ToggleVisibility(false);
        slider.gameObject.SetActive(false);
    }
    void loadVariables()
    {
        //Feedback Variables
        grayHalo = GameObject.Find("GrayHalo");
        grayHaloScene = GameObject.Find("GrayHaloScreen");
        yellowHalo = GameObject.Find("YellowHalo");
        imageTimingError = GameObject.Find("VerticalStripesImage");
        Texture2D buttonTex = LoadPNG(MaterialFilePath + Path.DirectorySeparatorChar + "StartButtonImage.png");
        initButton = CreateStartButton(buttonTex, new Rect(new Vector2(0,0), new Vector2(1,1)));

        //Trial Completion Feedback Variables
        sliderHalo = GameObject.Find("SliderHalo");
        sr = sliderHalo.GetComponent<Image>();
        slider = GameObject.Find("Slider").GetComponent<Slider>();
        sliderInitPosition = slider.gameObject.transform.position;

        playerViewParent = GameObject.Find("MainCameraCopy").transform; // sets parent for any playerView elements on experimenter display

        //config UI variables
        minObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("minObjectTouchDuration");
        maxObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("maxObjectTouchDuration");
        itiDuration = ConfigUiVariables.get<ConfigNumber>("itiDuration");
        sliderSize = ConfigUiVariables.get<ConfigNumber>("sliderSize");
        selectObjectDuration = ConfigUiVariables.get<ConfigNumber>("selectObjectDuration");
        finalFbDuration = ConfigUiVariables.get<ConfigNumber>("finalFbDuration");
        fbDuration = ConfigUiVariables.get<ConfigNumber>("fbDuration");
        chooseStimOnsetDelay = ConfigUiVariables.get<ConfigNumber>("chooseStimOnsetDelay");

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

            for (int i = 0; i < totalStims.Count(); i++)
            {
                Debug.Log("STIM CODE: " + totalStims.ElementAt(i).StimCode + "STIM POSITION: " + totalStims.ElementAt(i).StimLocation);
            }
        }
        else
        {
            searchStims.SetLocations(CurrentTrialDef.SearchStimsLocations);
            distractorStims.SetLocations(CurrentTrialDef.DistractorStimsLocations);
        }
/*
        foreach (StimDef sd in searchStims.stimDefs)
        {
            Debug.Log("SEARCH STIM CODE: " + sd.StimCode + "STIM POSITION: " + sd.StimLocation);
        }
        foreach (StimDef sd in distractorStims.stimDefs)
        {
            Debug.Log("DISTRACTOR STIM CODE: " + sd.StimCode + "STIM POSITION: " + sd.StimLocation);
        }*/
        
    }
    
    //-------------------------------------------------------------MISCELLANEOUS METHODS--------------------------------------------------------------------------
    private Vector2 playerViewPosition(Vector3 position, Transform playerViewParent)
    {
        Vector2 pvPosition = new Vector2((position[0] / Screen.width) * playerViewParent.GetComponent<RectTransform>().sizeDelta.x, (position[1] / Screen.height) * playerViewParent.GetComponent<RectTransform>().sizeDelta.y);
        return pvPosition;
    }
    private GameObject CreateStartButton(Texture2D tex, Rect rect) //creates start button as a sprite
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
    }

}














