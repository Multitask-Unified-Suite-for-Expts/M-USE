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

public class WhatWhenWhere_TrialLevel : ControlLevel_Trial_Template
{
    //This variable is required for most tasks, and is defined as the output of the GetCurrentTrialDef function 
    public WhatWhenWhere_TrialDef CurrentTrialDef => GetCurrentTrialDef<WhatWhenWhere_TrialDef>();
    
    // game object variables
    private GameObject initButton, goCue, chosenStim, grayHalo, grayHaloScreen, yellowHalo, sliderHalo, imageTimingError, txt;

    //stim group
    private StimGroup searchStims;
    private List<string> touchedObjects = new List<string>();

    // feedback variables
    private int context;
    public int stimCount = 0;
    private bool correctChoice = false;
    private bool incorrectChoice = false;
    private bool timingFail = false;
    private bool irrelevantSelection = false;
    private bool noSelection = false;

    private static int numObjMax = 20;


    // error data variables
    private int slotError = 0;
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
    private int[] numTotal = new int[numObjMax];
    private int[] numErrors = new int[numObjMax];
    private int[] numCorrect = new int[numObjMax];
    private List<float> touchDurations = new List<float> { };
    private List<float> choiceDurations = new List<float> { };
    private List<Vector3> touchedPositionsList = new List<Vector3>(); // empty now
    
    private SpriteRenderer sr;

    //UI VARIABLES
    public ConfigUI configUI;
    public JsonSaveLoad jsonSaveLoad;
    public WhatWhenWhere_TrialLevel mainLevel;
    private ExperimentInfoController experimenterInfo;
    private bool storeData;
    private ConfigVarStore configStore = new ConfigVarStore();
    
    //UI Config Timing Variables
    //[HideInInspector]
    //public ConfigNumberRanged itiDuration, baselineDuration, covertPrepDuration, freeGazeDuration, choiceToFbDuration, fBDuration; // = 0.8f;
    [HideInInspector]
    public ConfigNumber minObjectTouchDuration, itiDuration, finalFbDuration, fbDuration, maxObjectTouchDuration, selectObjectDuration, sliderSize, CentralCueSelectionDuration, CentralCueSelectionRadius, blinkOnDuration, blinkOffDuration, ObjectSelectionRadius, MinObjectSelectionTime, MaxReachTime;
    [HideInInspector]
    private float sampleDuration, delayDuration;

    //data logging variables
    private string touchedObjectsNames;
    private string touchDurationTimes;
    private string choiceDurationTimes;
    private string touchedPositions;
    private string accuracyLog = "";
    private float initialTouchTime = 0;
    private float touchDuration = 0;
    private float choiceDuration = 0;
    private int initialClick = 0;

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



    public override void DefineControlLevel()
    {

        //define States within this Control Level
        State StartButton = new State("StartButton");
        State ChooseStimulus = new State("ChooseStimulus");
        State StimulusChosen = new State("StimulusChosen");
        State FinalFeedback = new State("FinalFeedback");
        State ITI = new State("ITI");
        State StimulusChosenSuccesorState = new State("StimulusChosenSuccesorState");

        AddActiveStates(new List<State> { StartButton, ChooseStimulus, StimulusChosen, FinalFeedback, ITI, StimulusChosenSuccesorState });
        

        string[] stateNames = new string[] { "StartButton", "ChooseStimulus", "StimulusChosen", "FinalFeedback", "ITI" };

        AddInitializationMethod(() =>
        {
            if (!variablesLoaded)
            {
                variablesLoaded = true;
                loadVariables();
            }
        });

        SetupTrial.AddInitializationMethod(() =>
        {
            slotError = 0;
            repetitionError = 0;
            touchDurationError = 0;
            irrelevantSelectionError = 0;
            noScreenTouchError = 0;
            initialClick = 0;
            stimCount = 0;
            response = -1;
            touchedObjects.Clear();
            touchDurations.Clear();
            choiceDurations.Clear();
            touchedPositionsList.Clear();

            touchedObjectsNames = "[]";
            touchDurationTimes = "[]";
            choiceDurationTimes = "[]";
            touchedPositions = "[]";
            touchedObjectsNames = "[]";
            errorTypeString = "";
        });

        SetupTrial.SpecifyTermination(() => true, StartButton);


        // define initScreen state
        StartButton.AddInitializationMethod(() =>
        {
            Camera.main.backgroundColor = new Color(1f, 1f, 1f);

            ResetRelativeStartTime();
            if (context != 0)
            {
                Debug.Log(context);
                disableAllGameobjects();
            }

            context = CurrentTrialDef.Context;

            initButton.SetActive(true);
            goCue.SetActive(true);
            
            slider.gameObject.transform.position = sliderInitPosition;
            slider.value = 0;
           
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
                        Camera.main.backgroundColor = contextColors[context];
                    }
                }
            }
        });

        StartButton.SpecifyTermination(() => response == 0, ChooseStimulus);
        StartButton.AddDefaultTerminationMethod(() =>
        {
            sliderValueIncreaseAmount = (100f / CurrentTrialDef.CorrectObjectTouchOrder.Length) / 100f;
            slider.transform.localScale = new Vector3(sliderSize.value / 10f, sliderSize.value / 10f, 1f);
            sliderHalo.transform.localScale = new Vector3(sliderSize.value / 0.38f, sliderSize.value / 2f, 1f);

            slider.gameObject.SetActive(true);

            initButton.SetActive(false);
            goCue.SetActive(false);
        });

        // Define stimOn state
        ChooseStimulus.AddInitializationMethod(() =>
        {
            GameObject.Find("Slider").SetActive(true);
            int correctIndex = CurrentTrialDef.CorrectObjectTouchOrder[stimCount] - 1;
            Debug.Log("Correct Index: " + correctIndex);
            
            for (int i = 0; i < CurrentTrialDef.CorrectObjectTouchOrder.Length; ++i)
            {
                WhatWhenWhere_StimDef sd = (WhatWhenWhere_StimDef)searchStims.stimDefs[i];
                
                if (i == correctIndex)
                {
                    sd.IsCurrentTarget = true;
                }
                else
                {
                    sd.IsCurrentTarget = false;
                }
            }

            foreach (StimDef thing in searchStims.stimDefs)
            {
                Debug.Log(thing.StimName);
            }
            
            searchStims.ToggleVisibility(true);
            chosenStim = null;
            initialClick = 0;
        });

        ChooseStimulus.AddUpdateMethod(() =>
        {
            // check if user clicks on left or right
            if (Input.GetMouseButtonDown(0))
            {
                initialTouchTime = Time.time;
                initialClick += 1;
                choiceDuration = initialTouchTime - ChooseStimulus.TimingInfo.StartTimeAbsolute;
                choiceDurations.Add(choiceDuration);
            }
            
            if (Input.GetMouseButtonUp(0) && initialClick == 1)
            {
                touchDuration = Time.time - initialTouchTime;
                touchDurations.Add(touchDuration);
                
                mouseRay = Camera.main.ScreenPointToRay(InputBroker.mousePosition);

                var clickPoint = Input.mousePosition;
                touchedPositionsList.Add(new Vector3(clickPoint[0], clickPoint[1], clickPoint[2]));
                
                RaycastHit hit;
                // verify that the hit is on a stimulus
                if (Physics.Raycast(mouseRay, out hit))
                {
                    response = 1;
                    int correctIndex = CurrentTrialDef.CorrectObjectTouchOrder[stimCount] - 1;
                    Debug.Log("index: " + correctIndex);
                    chosenStim = hit.transform.gameObject;
                    GameObject testStim = chosenStim.transform.root.gameObject;

                    if (touchDuration < minObjectTouchDuration.value || touchDuration > maxObjectTouchDuration.value)
                    {
                        //Timing Error
                        timingFail = true;
                        touchDurationError += 1;
                        totalErrors_InSession += 1;
                        totalErrors_InBlock += 1;
                        touchedObjects.Add(testStim.name);
                        
                        //numTotal[correctIndex]++;
                        numErrors[correctIndex]++;
                    }

                    else if (testStim.GetComponent<StimDefPointer>().GetStimDef<WhatWhenWhere_StimDef>().IsCurrentTarget)
                    {
                        //Correct Choice
                        numCorrect[stimCount]++;
                        numTotal[stimCount]++;
                        slider.value += sliderValueIncreaseAmount;
                        stimCount += 1;

                        touchedObjects.Add(testStim.name);
                        yellowHalo.transform.position = testStim.transform.position;
                        correctChoice = true;
                    }
                    else if (touchedObjects.Contains(testStim.name))
                    {
                        //Repetition error
                        grayHalo.transform.position = testStim.transform.position;
                        touchedObjects.Add(testStim.name);

                        slider.value -= sliderValueIncreaseAmount;
                        repetitionError += 1;
                        totalErrors_InSession += 1;
                        totalErrors_InBlock += 1;

                        numTotal[stimCount]++;
                        numErrors[stimCount]++;
                        
                        incorrectChoice = true;
                    }
                    else
                    {
                        //Slot error
                        grayHalo.transform.position = testStim.transform.position;
                        touchedObjects.Add(testStim.name);
                        slider.value -= sliderValueIncreaseAmount;
                        slotError += 1;
                        totalErrors_InSession += 1;
                        totalErrors_InBlock += 1;

                        numTotal[stimCount]++;
                        numErrors[stimCount]++;
                        
                        incorrectChoice = true;
                    }

                    // progress report
                    accuracyLog = "";
                    for (int i = 0; i < CurrentTrialDef.CorrectObjectTouchOrder.Length; ++i)
                    {
                        accuracyLog = accuracyLog + "Slot " + (i + 1) + ": " + numCorrect[i] + "/" + numTotal[i] + " ";
                    }
                    Debug.Log("Progress: "+ accuracyLog);
                    
                }                
                else
                {
                    //Irrelevant Selection Error
                    Debug.Log("Clicked within the scene, but not on a stimulus");
                    response = 2;
                    var screenPoint = Input.mousePosition;
                    screenPoint.z = 100.0f; //distance of the plane from the camera
                    grayHaloScreen.transform.position = Camera.main.ScreenToWorldPoint(screenPoint);
                    irrelevantSelectionError += 1;
                    totalErrors_InSession += 1;
                    totalErrors_InBlock += 1;
                    irrelevantSelection = true;
                }
            }
            else
            {
                Debug.Log ("Didn't click on any stimulus");
                response = 0;
            }
            
        });

        ChooseStimulus.AddTimer(selectObjectDuration.value, ITI);
        ChooseStimulus.SpecifyTermination(() => response == 1, StimulusChosen);
        ChooseStimulus.SpecifyTermination(() => response == 2, StimulusChosen);
        ChooseStimulus.SpecifyTermination(() => stimCount == CurrentTrialDef.CorrectObjectTouchOrder.Length, FinalFeedback);
        
        StimulusChosen.AddUpdateMethod(() =>
        {
            response = -1;
            
            // Timing Fail
            if (timingFail)
            {
                imageTimingError.transform.SetAsLastSibling();
                imageTimingError.SetActive(true);
                errorTypeString = "TouchDurationError";
            }
           
            //Chose Incorrect
            else if (incorrectChoice)
            {
                grayHalo.SetActive(true);
                sliderHalo.SetActive(true);
                sr.color = new Color(0.6627f, 0.6627f, 0.6627f, 0.2f);

                if (slotError == 1)
                    errorTypeString = "SlotError";
                else
                    errorTypeString = "RepetitionError";
            }

            //Irrelevant Selection
            else if (irrelevantSelection)
            {
                grayHaloScreen.SetActive(true);
                errorTypeString = "IrrelevantSelectionError";
            }

            //Chose correct
            else if (correctChoice)
            {
                yellowHalo.SetActive(true);
                sliderHalo.SetActive(true);
                sr.color = new Color(1, 0.8431f, 0, 0.2f);
                errorTypeString = "None";
            }
            
        });
        
        StimulusChosen.SpecifyTermination(() => (correctChoice && Time.time - StimulusChosen.TimingInfo.StartTimeAbsolute >= fbDuration.value), ChooseStimulus, () => 
        {
            yellowHalo.SetActive(false);
            correctChoice = false;
            sliderHalo.SetActive(false);
        });
       
        StimulusChosen.SpecifyTermination(() => (incorrectChoice && Time.time - StimulusChosen.TimingInfo.StartTimeAbsolute >= fbDuration.value), ITI, () =>
        {
            grayHalo.SetActive(false);
            incorrectChoice = false;
            sliderHalo.SetActive(false);
        });

        StimulusChosen.SpecifyTermination(() => (timingFail && (Time.time - StimulusChosen.TimingInfo.StartTimeAbsolute) >= fbDuration.value), ITI, () =>
        {
            imageTimingError.SetActive(false);
            timingFail = false;
        });

        StimulusChosen.SpecifyTermination(() => (irrelevantSelection && (Time.time - StimulusChosen.TimingInfo.StartTimeAbsolute) >= fbDuration.value), ITI, () =>
        {
            grayHaloScreen.SetActive(false);
            irrelevantSelection = false;
        });

        FinalFeedback.AddInitializationMethod(() =>
        {

            sliderHalo.SetActive(true);
            sr.color = new Color(1, 1, 1, 0.2f);
            txt.SetActive(true);
            startTime = Time.time;
            errorTypeString = "None";
            searchStims.ToggleVisibility(false);
        });

        FinalFeedback.AddUpdateMethod(() =>
        {
            if ((int)(10 * (Time.time - startTime)) % 4 == 0)
            {
                sr.color = new Color(1, 1, 1, 0.2f);
            }
            else if ((int)(10 * (Time.time - startTime)) % 2 == 0)
            {
                sr.color = new Color(0, 0, 0, 0.2f);
            }
        });
        FinalFeedback.AddTimer(finalFbDuration.value, ITI, () =>
        {
            txt.SetActive(false);
            sliderHalo.SetActive(false);
        });

        //Define iti state
        ITI.AddInitializationMethod(() =>
        {
            searchStims.ToggleVisibility(false);
            Camera.main.backgroundColor = Color.white;
            txt.SetActive(false);
            
            GameObject.Find("Slider").SetActive(false);

            if(stimCount == CurrentTrialDef.CorrectObjectTouchOrder.Length)
            {
                response = -1;
            }
            else if (response == 0)
            {
                noScreenTouchError++;
                totalErrors_InSession += 1;
                totalErrors_InBlock += 1;
                errorTypeString = "NoSelectionMade";
                response = -1;
            }
            
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

            Debug.Log("Touched Objects: " + touchedObjectsNames);

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

            Debug.Log("Touch Durations: " + touchDurationTimes);

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

            Debug.Log("Choice Durations: " + choiceDurationTimes);

            // touch position data 
            touchedPositions = "[";
            for (int i = 0; i<touchedPositionsList.Count; ++i)
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
            Debug.Log("Touched Positions: " + touchedPositions);

            // error names data
            errorType_InBlock.Add(errorTypeString);
            errorType_InSession.Add(errorTypeString);
            errorType_InBlockString = "[";
            //errorType_InSessionString = "[";
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
            Debug.Log("ErrorType" + errorTypeString);
            Debug.Log("ErrorTypes_InBlock " + errorType_InBlockString);
            Debug.Log("ErrorTypes_InSession " + errorType_InSessionString);
            Debug.Log("Response" +response);
        });
        ITI.AddTimer(() => itiDuration.value, FinishTrial, () => Debug.Log("Trial " + TrialCount_InTask + " completed"));
        
        TrialData.AddDatum("TrialID", () => CurrentTrialDef.TrialID);
        TrialData.AddDatum("Context", () => context);
        TrialData.AddDatum("TouchedObjects", () => touchedObjectsNames);
        TrialData.AddDatum("ErrorType", () => errorTypeString);
        TrialData.AddDatum("ErrorType_InBlock", () => errorType_InBlockString);
        TrialData.AddDatum("ErrorType_InSession", () => errorType_InSessionString);
        TrialData.AddDatum("TotalErrors_InBlock", () => totalErrors_InBlock);
        TrialData.AddDatum("TotalErrors_InSession", () => totalErrors_InSession);
        TrialData.AddDatum("TouchDurations", () => touchDurationTimes);
        TrialData.AddDatum("ChoiceDurations", () => choiceDurationTimes); 
        TrialData.AddDatum("Progress", () => accuracyLog);
        TrialData.AddDatum("TouchPositions", () => touchedPositions);

        FrameData.AddDatum("TouchPosition", () => Input.mousePosition);
        FrameData.AddDatum("ErrorType", () => errorTypeString);
        FrameData.AddDatum("Touch", () => response);
        FrameData.AddDatum("StartButton", () => initButton.activeSelf);
        FrameData.AddDatum("StartText", () => goCue.activeSelf);
        FrameData.AddDatum("CompletionText", () => txt.activeSelf);
        FrameData.AddDatum("GrayHaloFeedback", () => (grayHalo.activeSelf||grayHaloScreen.activeSelf));
        FrameData.AddDatum("YellowHaloFeedback", () => yellowHalo.activeSelf);
        FrameData.AddDatum("TimingErrorFeedback", () => imageTimingError.activeSelf);
        FrameData.AddDatum("SliderHalo", () => sliderHalo.activeSelf);
        FrameData.AddDatum("StimuliShown", () => searchStims.IsActive);
        FrameData.AddDatum("SliderValue", () => slider.normalizedValue);
    }

    public void CreateConfigUI()
    {
        configUI.clear();
        configStore = ConfigUiVariables;
        configUI.store = configStore;

        minObjectTouchDuration = configStore.get<ConfigNumber>("minObjectTouchDuration");
        maxObjectTouchDuration = configStore.get<ConfigNumber>("maxObjectTouchDuration");
        itiDuration = configStore.get<ConfigNumber>("itiDuration");
        sliderSize = configStore.get<ConfigNumber>("sliderSize");
        //CentralCueSelectionRadius = configStore.get<ConfigNumber>("CentralCueSelectionRadius");
        //CentralCueSelectionDuration = configStore.get<ConfigNumber>("CentralCueSelectionDuration");
        //blinkOnDuration = configStore.get<ConfigNumber>("blinkOnDuration");
        //blinkOffDuration = configStore.get<ConfigNumber>("blinkOffDuration");
        //baselineDuration = configStore.get<ConfigNumberRanged>("baselineDuration");
        //covertPrepDuration = configStore.get<ConfigNumberRanged>("covertPrepDuration");
        //freeGazeDuration = configStore.get<ConfigNumberRanged>("freeGazeDuration");
        selectObjectDuration = configStore.get<ConfigNumber>("selectObjectDuration");
        finalFbDuration = configStore.get<ConfigNumber>("finalFbDuration");
        //ObjectSelectionRadius = configStore.get<ConfigNumber>("ObjectSelectionRadius");
        fbDuration = configStore.get<ConfigNumber>("fbDuration");
        //MaxReachTime = configStore.get<ConfigNumber>("MaxReachTime");


        configUI.GenerateUI();
    }

    // set all gameobjects to setActive false
    void disableAllGameobjects()
    {
        initButton.SetActive(false);
        goCue.SetActive(false);
        txt.SetActive(false);
        sliderHalo.SetActive(false);
        grayHalo.SetActive(false);
        yellowHalo.SetActive(false);
        grayHaloScreen.SetActive(false);
        imageTimingError.SetActive(false);
        searchStims.ToggleVisibility(false);
    }

    void loadVariables()
    {
        txt = GameObject.Find("FinalText");
        slider = GameObject.Find("Slider").GetComponent<Slider>();
        sliderHalo = GameObject.Find("SliderHalo");
        grayHalo = GameObject.Find("GrayHalo");
        grayHaloScreen = GameObject.Find("GrayHaloScreen");
        yellowHalo = GameObject.Find("YellowHalo");
        initButton = GameObject.Find("StartButton");
        goCue = GameObject.Find("StartText");
        sr = sliderHalo.GetComponent<SpriteRenderer>();
        imageTimingError = GameObject.Find("VerticalStripesImage");

        experimenterInfo = GameObject.Find("ExperimenterInfo").GetComponent<ExperimentInfoController>();
        sliderInitPosition = slider.gameObject.transform.position;
        
        contextColors.Add(new Color(0f, 0f, 0.5451f)); // dark blue
        contextColors.Add(new Color(0.5294f, 0.8078f, 0.9804f)); // light sky blue
        contextColors.Add(new Color(0.9961f, 0.850f, 0.850f)); // light yellow
        contextColors.Add(new Color(0.3922f, 0.5843f, 0.9294f)); // cornflower blue
        contextColors.Add(new Color(0.3725f, 0.6196f, 0.6275f)); // cadet blue
        contextColors.Add(new Color(0.6902f, 0.7686f, 0.8706f)); // light steel blue
        contextColors.Add(new Color(0.6275f, 0.3216f, 0.1765f)); // sienna
        contextColors.Add(new Color(0.8275f, 0.8275f, 0.8275f)); // light gray
        contextColors.Add(new Color(0.8471f, 0.7490f, 0.8471f)); // thistle
        contextColors.Add(new Color(0.9020f, 0.9020f, 0.9804f)); // lavender
        contextColors.Add(new Color(0f, 0f, 0f)); // black
        
        initButton.SetActive(false);
        goCue.SetActive(false);
        txt.SetActive(false);
        sliderHalo.SetActive(false);
        grayHalo.SetActive(false);
        grayHaloScreen.SetActive(false);
        yellowHalo.SetActive(false);
        imageTimingError.SetActive(false);
        GameObject.Find("Slider").SetActive(false);

        if (jsonSaveLoad == null)
            jsonSaveLoad = FindObjectOfType<JsonSaveLoad>();
        if (configUI == null)
            configUI = FindObjectOfType<ConfigUI>();
        CreateConfigUI();

        Debug.Log("Done Loading Variables");
    }

    protected override void DefineTrialStims()
    {
        //Define StimGroups consisting of StimDefs whose gameobjects will be loaded at TrialLevel_SetupTrial and 
        //destroyed at TrialLevel_Finish
        //StimGroup constructor which creates a subset of an already-existing StimGroup 
        searchStims = new StimGroup("SearchStims", ExternalStims, CurrentTrialDef.SearchStimsIndices);
        searchStims.SetLocations(CurrentTrialDef.SearchStimsLocations);       
        TrialStims.Add(searchStims);
    }

}














