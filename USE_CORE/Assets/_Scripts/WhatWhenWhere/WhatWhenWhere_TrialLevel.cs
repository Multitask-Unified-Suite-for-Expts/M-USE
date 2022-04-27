using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using USE_States;
using USE_ExperimentTemplate;
using WhatWhenWhere_Namespace;
using USE_StimulusManagement;
using System.Collections;

public class WhatWhenWhere_TrialLevel : ControlLevel_Trial_Template
{
    //This variable is required for most tasks, and is defined as the output of the GetCurrentTrialDef function 
    public WhatWhenWhere_TrialDef CurrentTrialDef => GetCurrentTrialDef<WhatWhenWhere_TrialDef>();
    
    // game object variables
    private GameObject initButton, goCue, chosenStim, halo, sliderHalo, imageIncorrectObject, imageCorrectObject, imageTimingError, txt;

    //stim group
    private StimGroup searchStims;
    private List<string> touchedObjects = new List<string>();

    // feedback variables
    private int clickCount, context;
    public int stimCount = 0;
    private bool correctChoice = false;
    private bool incorrectChoice = false;
    private bool removeFeedback = false;
    private static int numObjMax = 10;
    

    // error data variables
    private int repetitionError = 0;
    private int contextError = 0;
    private int totalErrors = 0;
    private int slotError = 0;
    private int touchDurationError = 0;
    private float startTime;
    private float clickTime;
    private float timeDif;
    private List<Color> contextColors = new List<Color> { };
    private int[] numTotal = new int[numObjMax];
    private int[] numErrors = new int[numObjMax];
    private List<Vector3> touchPositionsList; 
    private string touchPositionsString;

    private bool restart = false;
    private int trialNum = 0;
    private SpriteRenderer sr;
    //private Color originalColor;
    private string touchedObjectsNames;
    private bool timingFail = false;
    private float initialTouchTime = 0;
    private float touchDuration = 0;
    private int min;
    private int max;
    private int tCount = 0;
    private int initialClick = 0;



    [HideInInspector]
    [System.NonSerialized] public int response = -1, trialCount = -1;
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
    //private Slider slider;
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
        //chosewrong state

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
            //++tCount;
            

            Debug.Log("TRIALNUM: " + trialNum);
            Debug.Log("tCount: " + tCount);
            if (restart)
            {
                Debug.Log("TRIALCOUNT: " + TrialCount_InBlock);
                TrialCount_InBlock--;
            }
            ++trialNum;
        });

        SetupTrial.SpecifyTermination(() => true, StartButton);


        // define initScreen state
        StartButton.AddInitializationMethod(() =>
        {
            // min = CurrentTrialDef.nRepetitionsMinMax[0];
            // max = CurrentTrialDef.nRepetitionsMinMax[1];
            min = 3;
            max = 6;

            if (restart)
            {
                trialCount++;
            }

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

            clickCount = 0;
            response = -1;
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
            slider.gameObject.SetActive(true);

            initButton.SetActive(false);
            goCue.SetActive(false);

        });

        // Define stimOn state
        ChooseStimulus.AddInitializationMethod(() =>
        {
            GameObject.Find("Slider").SetActive(true);
            Debug.Log("StimCOUNT: " + stimCount);
            
            int correctIndex = CurrentTrialDef.CorrectObjectTouchOrder[stimCount] - 1;

            Debug.Log("Correct INDEX: " + correctIndex);
            
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
                Debug.Log("NAME OF OBJECTS:" + sd.StimGameObject.name);
                
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
            }
            
            if (Input.GetMouseButtonUp(0) && initialClick == 1)
            {
                Debug.Log("NEW STIMULUS SELECTION");
                touchDuration = Time.time - initialTouchTime;
                Debug.Log("touch duration time: " + touchDuration);
                mouseRay = Camera.main.ScreenPointToRay(InputBroker.mousePosition);
                RaycastHit hit;

                // verify that the hit is on a stimulus
                if (Physics.Raycast(mouseRay, out hit))
                {
                    response = 1;
                    startTime = Time.time;
                    int correctIndex = CurrentTrialDef.CorrectObjectTouchOrder[stimCount] - 1;
                    Debug.Log("index: " + correctIndex);
                    chosenStim = hit.transform.gameObject;
                    GameObject testStim = chosenStim.transform.root.gameObject;
                    
                    //touchPositionsList.Add(Input.mousePosition);
                    
                    if (touchDuration < CurrentTrialDef.MinTouchDuration || touchDuration > CurrentTrialDef.MaxTouchDuration)
                    {
                        //Timing Error
                        timingFail = true;
                        touchDurationError += 1;
                        totalErrors += 1;
                        restart = true;
                        touchedObjects.Add(testStim.name);
                    }

                    else if (testStim.GetComponent<StimDefPointer>().GetStimDef<WhatWhenWhere_StimDef>().IsCurrentTarget)
                    {
                        //Correct Choice
                        numTotal[correctIndex]++;

                        slider.value += sliderValueIncreaseAmount;
                        stimCount += 1;

                        touchedObjects.Add(testStim.name);
                        imageCorrectObject.transform.position = testStim.transform.position;
                        correctChoice = true;
                        restart = false;
                    }
                    else if (touchedObjects.Contains(testStim.name))
                    {
                        //Repetition error
                        
                        imageIncorrectObject.transform.position = testStim.transform.position;
                        touchedObjects.Add(testStim.name);

                        slider.value -= sliderValueIncreaseAmount;
                        repetitionError += 1;
                        totalErrors += 1;

                        numTotal[correctIndex]++;
                        numErrors[correctIndex]++;

                        restart = true;
                        incorrectChoice = true;
                    }
                    else
                    {
                        //Slot error
                        imageIncorrectObject.transform.position = testStim.transform.position;
                        touchedObjects.Add(testStim.name);

                        slider.value -= sliderValueIncreaseAmount;
                        slotError += 1;
                        totalErrors += 1;

                        numTotal[correctIndex]++;
                        numErrors[correctIndex]++;

                        restart = true;
                        incorrectChoice = true;
                    }
                    
                    // progress report
                    Debug.Log(restart);
                    string errLog = "";
                    for (int i = 0; i < CurrentTrialDef.CorrectObjectTouchOrder.Length; ++i)
                    {
                        errLog = errLog + "Slot " + (i + 1) + ": " + numErrors[i] + "/" + numTotal[i] + "\t";

                    }
                    Debug.Log(errLog);

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

                    // touched positions
                    /*touchPositionsString = "[";
                    for (int i = 0; i < touchPositionsList.Count; ++i)
                    {
                        if (i < touchPositionsList.Count - 1)
                        {
                            touchPositionsStrings = touchPositionsString + touchedObjects[i] + ",";
                        }
                        else
                        {
                            touchedObjectsNames = touchedObjectsNames + touchedObjects[i];
                        }

                    }
                    touchedObjectsNames = touchedObjectsNames + "]";

                    Debug.Log("Touched Objects: " + touchedObjectsNames);
                    */
                }                
                else
                {
                        Debug.Log("Didn't click on any stimulus");
                }
            }
            
        });
        
        ChooseStimulus.SpecifyTermination(() => response == 1, StimulusChosen);
        ChooseStimulus.SpecifyTermination(() => stimCount == CurrentTrialDef.CorrectObjectTouchOrder.Length, FinalFeedback);
        
        StimulusChosen.AddUpdateMethod(() =>
        {
            response = -1;
            // Timing Fail
            
            if (timingFail)
            {
                imageTimingError.SetActive(true);
            }

            //Chose Incorrect
            else if (incorrectChoice)
            {
                imageIncorrectObject.SetActive(true);
                sliderHalo.SetActive(true);
                sr.color = new Color(1, 0, 0, 0.2f);
                
            }

            //Chose correct
            else if (correctChoice)
            {
                imageCorrectObject.SetActive(true);
                sliderHalo.SetActive(true);
                sr.color = new Color(0, 1, 0, 0.2f);
            }
            
        });
        
        StimulusChosen.SpecifyTermination(() => (correctChoice && Time.time - StimulusChosen.TimingInfo.StartTimeAbsolute >= 0.75f), ChooseStimulus, () => 
        {
            imageCorrectObject.SetActive(false);
            correctChoice = false;
            sliderHalo.SetActive(false);
        });
       // StimulusChosen.SpecifyTermination(() => (correctChoice && Time.time - StimulusChosen.TimingInfo.StartTimeAbsolute >= 0.75f && stimCount == CurrentTrialDef.CorrectObjectTouchOrder.Length), ITI);
        StimulusChosen.SpecifyTermination(() => (incorrectChoice && Time.time - StimulusChosen.TimingInfo.StartTimeAbsolute >= 0.75f), ITI, () =>
        {
            imageIncorrectObject.SetActive(false);
            incorrectChoice = false;
            sliderHalo.SetActive(false);
        });
        StimulusChosen.SpecifyTermination(() => (timingFail && (Time.time - StimulusChosen.TimingInfo.StartTimeAbsolute) >= 0.75f), ITI, () =>
        {
            imageTimingError.SetActive(false);
            timingFail = false;
        });

        FinalFeedback.AddInitializationMethod(() =>
        {
            sliderHalo.SetActive(true);
            sr.color = new Color(1, 1, 1, 0.2f);
            txt.SetActive(true);
            startTime = Time.time;

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
                sr.color = new Color(1, 0, 0, 0.2f);
            }
        });
        FinalFeedback.AddTimer(2f, ITI, () =>
        {

            txt.SetActive(false);
            sliderHalo.SetActive(false);

            slotError = 0;
            totalErrors = 0;
            repetitionError = 0;
            touchDurationError = 0;
            initialClick = 0;
            stimCount = 0;
        });

        //Define iti state
        ITI.AddInitializationMethod(() =>
        {
            searchStims.ToggleVisibility(false);
            
            Camera.main.backgroundColor = Color.white;
            txt.SetActive(false);
            touchedObjects.Clear();
           /* if (tCount < min)
            {
                restart = true;
            }

            Debug.Log("TC: " + tCount);
            Debug.Log("max: " + max);

            if (tCount == max)
            {
                restart = false;
            }

            if (restart == false)
            {
                tCount = 0;
            }

            */

        });
        ITI.SpecifyTermination(() => true, FinishTrial, () => Debug.Log("Trial" + CurrentTrialDef.TrialNum + " completed"));
        
        TrialData.AddDatum("TrialNum", () => trialNum);
        TrialData.AddDatum("TrialID", () => CurrentTrialDef.TrialID);
        TrialData.AddDatum("TouchedObjects", () => touchedObjectsNames);
        TrialData.AddDatum("TouchDurationError", () => touchDurationError);
        TrialData.AddDatum("SlotError", () => slotError);
        TrialData.AddDatum("RepetitionError", () => repetitionError);
        TrialData.AddDatum("TotalErrors", () => totalErrors);
        //TrialData.AddDatum("TouchPositions", () => touchPositionsString);
    }
    // set all gameobjects to setActive false
    void disableAllGameobjects()
    {
        initButton.SetActive(false);
        goCue.SetActive(false);
        txt.SetActive(false);
        sliderHalo.SetActive(false);
        imageIncorrectObject.SetActive(false);
        imageCorrectObject.SetActive(false);
        imageTimingError.SetActive(false);
        GameObject.Find("Slider").SetActive(false);
        searchStims.ToggleVisibility(false);
    }

    void loadVariables()
    {
        txt = GameObject.Find("FinalText");
        slider = GameObject.Find("Slider").GetComponent<Slider>();
        sliderHalo = GameObject.Find("SliderHalo");
        imageIncorrectObject = GameObject.Find("RedHaloImage");
        imageCorrectObject = GameObject.Find("GreenHaloImage");
        initButton = GameObject.Find("StartButton");
        goCue = GameObject.Find("StartText");
        sr = sliderHalo.GetComponent<SpriteRenderer>();
        imageTimingError = GameObject.Find("VerticalStripesImage");

        //halo = GameObject.Find("Halo0");
        //halo.SetActive(false);

        sliderInitPosition = slider.gameObject.transform.position;

        System.Random rnd = new System.Random();
        
        contextColors.Add(new Color(0f, 0f, 0.5451f)); // dark blue
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
        imageIncorrectObject.SetActive(false);
        imageCorrectObject.SetActive(false);
        imageTimingError.SetActive(false);
        GameObject.Find("Slider").SetActive(false);
       
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














