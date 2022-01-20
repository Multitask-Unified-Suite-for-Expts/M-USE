using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using USE_States;
using USE_ExperimentTemplate;
using WhatWhenWhere_Namespace;
using System.Collections;

public class WhatWhenWhere_TrialLevel : ControlLevel_Trial_Template
{
    //This variable is required for most tasks, and is defined as the output of the GetCurrentTrialDef function 
    public WhatWhenWhere_TrialDef CurrentTrialDef => GetCurrentTrialDef<WhatWhenWhere_TrialDef>();

    // game object variables
    private GameObject initButton, goCue, trialStim, halo, sliderHalo, imageIncorrectObject, imageCorrectObject, txt;

    //  private GameObject[] halos;
    private GameObject[] totalObjects;
    private GameObject[] currentObjects;
    private List<string> touchedObjects = new List<string>();

    // effort reward variables
    private int clickCount, context;
    public int sphereCount = 0;
    private int numChosenLeft, numChosenRight;
    private bool incorrectChoice = false;
    private bool correctChoice = false;
    private static int numObjMax = 10;
    private int repetitionError = 0;
    private int contextError = 0;
    private int totalErrors = 0;
    private int slotError = 0;
    private float startTime;
    private float clickTime;
    private float timeDif;
    private List<Color> contextColors = new List<Color> { };
    private List<Color> objectColors = new List<Color> { };
    private int[] numTotal = new int[numObjMax];
    private int[] numErrors = new int[numObjMax];
    private bool restart = false;
    private int trialNum = 0;
    private SpriteRenderer sr;
    private Color originalColor;
    private string touchedObj = "[";
    private int min;
    private int max;
    private int tCount = 0;


    [HideInInspector]
    public String sphereChoice;
    [System.NonSerialized] public int response = -1, trialCount = -1;
    // vector3 variables
    private Vector3 trialStimInitLocalScale;
    private Vector3 fbInitLocalScale;
    private Vector3 sliderInitPosition;

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
        State ChooseSphere = new State("ChooseSphere");
        State ChoseWrong = new State("ChoseWrong");
        State ChoseRight = new State("ChoseRight");
        State Feedback = new State("Feedback");
        State ITI = new State("ITI");
        AddActiveStates(new List<State> { StartButton, ChooseSphere, ChoseWrong, ChoseRight, Feedback, ITI });
        //chosewrong state

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
            ++tCount;
            sphereCount = 0;

            Debug.Log("TRIALNUM: " + trialNum);
            Debug.Log("tCount: " + tCount);
            if (restart)
            {
                Debug.Log("TRIALCOUNT: " + TrialCount_InBlock);
                TrialCount_InBlock--;
            }
            ++trialNum;
            touchedObj = "[";
        });

        SetupTrial.SpecifyTermination(() => true, StartButton);


        // define initScreen state
        StartButton.AddInitializationMethod(() =>
        {
            // min = CurrentTrialDef.nRepetitionsMinMax[0];
            // max = CurrentTrialDef.nRepetitionsMinMax[1];
            min = 3;
            max = 6;

            if (incorrectChoice == false)
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

            sphereCount = 0;
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

        StartButton.SpecifyTermination(() => response == 0, ChooseSphere);
        StartButton.AddDefaultTerminationMethod(() =>
        {
            sliderValueIncreaseAmount = (100f / CurrentTrialDef.ObjectNums.Length) / 100f;
            slider.gameObject.SetActive(true);

            initButton.SetActive(false);
            goCue.SetActive(false);

        });

        // Define stimOn state
        ChooseSphere.AddInitializationMethod(() =>
        {
            currentObjects = new GameObject[CurrentTrialDef.ObjectNums.Length];
            // halos = new GameObject[CurrentTrialDef.ObjectNums.Length];
            GameObject.Find("Slider").SetActive(true);
            for (int i = 0; i < CurrentTrialDef.ObjectNums.Length; ++i)
            {
                currentObjects[i] = totalObjects[CurrentTrialDef.ObjectNums[i] - 1];
                currentObjects[i].SetActive(true);

                placeSphere(currentObjects[i], CurrentTrialDef.ObjectXLocations[i], CurrentTrialDef.ObjectYLocations[i], 0f);
                //string h = "Halo" + (i).ToString();

            }

            trialStim = null;

        });

        ChooseSphere.AddUpdateMethod(() =>
        {
            // check if user clicks on left or right
            if (InputBroker.GetMouseButtonDown(0))
            {
                mouseRay = Camera.main.ScreenPointToRay(InputBroker.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(mouseRay, out hit))
                {

                    int correctIndex = CurrentTrialDef.CorrectObjectTouchOrder[sphereCount] - 1;
                    Debug.Log("index: " + correctIndex);
                    trialStim = hit.transform.gameObject;

                    if (trialStim.name == currentObjects[correctIndex].name)
                    {
                        //Correct Choice
                        numTotal[correctIndex]++;

                        slider.value += sliderValueIncreaseAmount;
                        sphereCount += 1;
                        response = 1;
                        touchedObjects.Add(trialStim.name);
                        sphereChoice = trialStim.name;
                        imageCorrectObject.transform.position = trialStim.transform.position;

                        correctChoice = true;

                        restart = false; 

                    }
                    else if (touchedObjects.Contains(trialStim.name))
                    {
                        //Repetition error
                        sphereChoice = trialStim.name;
                        imageIncorrectObject.transform.position = trialStim.transform.position;
                        touchedObjects.Add(trialStim.name);

                        response = 1;

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
                        sphereChoice = trialStim.name;
                        imageIncorrectObject.transform.position = trialStim.transform.position;
                        touchedObjects.Add(trialStim.name);

                        response = 1;

                        slider.value -= sliderValueIncreaseAmount;
                        slotError += 1;
                        totalErrors += 1;

                        numTotal[correctIndex]++;
                        numErrors[correctIndex]++;
                        
                        restart = true;
                        incorrectChoice = true;

                    }
                    Debug.Log(restart);
                    string errLog = "";
                    for (int i = 0; i < currentObjects.Length; ++i)
                    {
                        errLog = errLog + "Slot " + (i + 1) + ": " + numErrors[i] + "/" + numTotal[i] + "\t";

                    }
                    Debug.Log(errLog);


                }
                else
                {
                    Debug.Log("Didn't click on any sphere");
                }
            }
           // Debug.Log("COND " + (sphereCount == CurrentTrialDef.ObjectNums.Length));
           // Debug.Log("SC: " + sphereCount);
           // Debug.Log("LEN " + CurrentTrialDef.ObjectNums.Length);

        });

        ChooseSphere.SpecifyTermination(() => incorrectChoice, ChoseWrong);
        //ChooseSphere.SpecifyTermination(() => correctChoice, ChoseRight);
        ChooseSphere.SpecifyTermination(() => sphereCount == CurrentTrialDef.ObjectNums.Length, Feedback);

        ChoseWrong.AddInitializationMethod(() =>
        {
            //halo.SetActive(true);
            originalColor = trialStim.GetComponent<Renderer>().material.color;
            trialStim.GetComponent<Renderer>().material.SetColor("_Color", new Color(0.5020f, 0.5020f, 0.5020f));

            imageIncorrectObject.SetActive(true);
            sliderHalo.SetActive(true);
            sr.color = new Color(1, 0, 0, 0.2f);
            for (int i = 0; i < touchedObjects.Count; ++i)
            {
                touchedObj = touchedObj + touchedObjects[i];
                if (i < touchedObjects.Count - 1)
                {
                    touchedObj = touchedObj + ", ";
                }
            }
            touchedObj = touchedObj + "]";
        });

        ChoseWrong.AddTimer(0.75f, ITI, () =>
        {
            //halo.SetActive(false);
            imageIncorrectObject.SetActive(false);
            incorrectChoice = false;

            sliderHalo.SetActive(false);
            trialStim.GetComponent<Renderer>().material.SetColor("_Color", originalColor);
        });

        ChoseRight.AddInitializationMethod(() =>
        {
            //halo.SetActive(true);
            imageCorrectObject.SetActive(true);
            sliderHalo.SetActive(true);
            sr.color = new Color(0, 1, 0, 0.2f);
        });

        ChoseRight.AddTimer(0.5f, ChooseSphere, () =>
        {
            //halo.SetActive(false);
            imageCorrectObject.SetActive(false);
            correctChoice = false;
            sliderHalo.SetActive(false);
        });

        Feedback.AddInitializationMethod(() =>
        {
            sliderHalo.SetActive(true);
           // sphereCount = 0;
            sr.color = new Color(1, 1, 1, 0.2f);
            txt.SetActive(true);
            startTime = Time.time;

            foreach (GameObject obj in currentObjects)
            {
                obj.SetActive(false);
            }

            for (int i = 0; i < touchedObjects.Count; ++i)
            {
                touchedObj = touchedObj + touchedObjects[i];
            }
            touchedObj = touchedObj + "]";

       
        });

        Feedback.AddUpdateMethod(() =>
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
        Feedback.AddTimer(2f, ITI, () =>
        {

            txt.SetActive(false);
            sliderHalo.SetActive(false);

            slotError = 0;
            totalErrors = 0;
            repetitionError = 0;
        });

        //Define iti state
        ITI.AddInitializationMethod(() =>
        {
            foreach (GameObject obj in currentObjects)
            {
                obj.SetActive(false);
            }
            foreach (GameObject obj in totalObjects)
            {
                obj.SetActive(false);
            }
            Camera.main.backgroundColor = Color.white;
            txt.SetActive(false);
            for (var i = 0; i < touchedObjects.Count; i++)
            {
                touchedObjects.RemoveAt(i);
            }
            Debug.Log(restart);
            if (tCount < min)
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

        });
        ITI.SpecifyTermination(() => true, FinishTrial, () => Debug.Log("Trial" + CurrentTrialDef.TrialNum + " completed"));

        TrialData.AddDatum("TrialNum", () => trialNum);
        TrialData.AddDatum("TrialID", () => CurrentTrialDef.TrialID);
        TrialData.AddDatum("TouchedObjects", () => touchedObj);
        TrialData.AddDatum("SlotError", () => slotError);
        TrialData.AddDatum("RepetitionError", () => repetitionError);
        TrialData.AddDatum("TotalErrors", () => totalErrors);


    }
    // set all gameobjects to setActive false
    void disableAllGameobjects()
    {
        initButton.SetActive(false);
        goCue.SetActive(false);
        slider.gameObject.SetActive(false);
        foreach (GameObject obj in currentObjects)
        {
            obj.SetActive(false);
        }


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

        //halo = GameObject.Find("Halo0");
        //halo.SetActive(false);

        sliderInitPosition = slider.gameObject.transform.position;


        totalObjects = new GameObject[numObjMax];
        System.Random rnd = new System.Random();

        objectColors.Add(new Color(1f, 0f, 0f)); // red
        objectColors.Add(new Color(0f, 1f, 0f)); // lime
        objectColors.Add(new Color(0f, 0f, 1f)); // blue
        objectColors.Add(new Color(1f, 0f, 1f)); //fuschia
        objectColors.Add(new Color(0f, 1f, 1f)); // cyan
        objectColors.Add(new Color(0.5412f, 0.1686f, 0.8863f)); // blue-violet
        objectColors.Add(new Color(0.1686f, 0.5412f, 0.8863f)); // blue-violet
        objectColors.Add(new Color(0.2f, 0.1686f, 0.1f)); // blue-violet
        objectColors.Add(new Color(0.1686f, 0.1686f, 0.5412f)); // blue-violet
        objectColors.Add(new Color(0.8863f, 0.1686f, 0.8863f)); // blue-violet


        contextColors.Add(new Color(0f, 0f, 0.5451f)); // dark blue
        contextColors.Add(new Color(0.3922f, 0.5843f, 0.9294f)); // cornflower blue
        contextColors.Add(new Color(0.3725f, 0.6196f, 0.6275f)); // cadet blue
        contextColors.Add(new Color(0.6902f, 0.7686f, 0.8706f)); // light steel blue
        contextColors.Add(new Color(0.6275f, 0.3216f, 0.1765f)); // sienna
        contextColors.Add(new Color(0.8275f, 0.8275f, 0.8275f)); // light gray
        contextColors.Add(new Color(0.8471f, 0.7490f, 0.8471f)); // thistle
        contextColors.Add(new Color(0.9020f, 0.9020f, 0.9804f)); // lavender
        contextColors.Add(new Color(0f, 0f, 0f)); // black

        for (int i = 0; i < numObjMax; ++i)
        {
            string s = (i + 1).ToString();
            Debug.Log(s);
            totalObjects[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            totalObjects[i].GetComponent<Renderer>().material.SetColor("_Color", objectColors[i]);
            totalObjects[i].name = s;
            // totalObjects[i].AddComponent<Light>()
            totalObjects[i].SetActive(false);
        }

        initButton.SetActive(false);
        goCue.SetActive(false);
        txt.SetActive(false);
        sliderHalo.SetActive(false);
        imageIncorrectObject.SetActive(false);
        imageCorrectObject.SetActive(false);
        GameObject.Find("Slider").SetActive(false);


        Debug.Log("Done Loading Variables");

    }

    void placeSphere(GameObject sphere, float x, float y, float z)
    {
        //set the position of the sphere 0z in front of the camera
        sphere.transform.position = new Vector3(x, y, z);

    }


    void ChangeColor(GameObject obj, Color color)
    {
        var material = obj.GetComponent<Renderer>().material;
        material.color = color;
    }
}














