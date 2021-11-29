using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using USE_States;
using USE_ExperimentTemplate;
using WhatWhenWhere_Namespace;

public class WhatWhenWhere_TrialLevel : ControlLevel_Trial_Template
{
    //This variable is required for most tasks, and is defined as the output of the GetCurrentTrialDef function 
    public WhatWhenWhere_TrialDef CurrentTrialDef => GetCurrentTrialDef<WhatWhenWhere_TrialDef>();

    // game object variables
    private GameObject initButton, fb, goCue, trialStim, clickMarker;
    private GameObject[] totalObjects;
    private GameObject[] currentObjects;

    // effort reward variables
    private int clickCount, context;
    public int sphereCount = 0;
    private int numChosenLeft, numChosenRight;
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
    private Color color1 = new Color(0.7f, 0.5f, 0.96f);
    private Color color2 = new Color(0.31f, 0.69f, 0.88f);
    private Color color3 = new Color(0.54f, 0.18f, 0.18f);
    

    private Camera cam;
    //private Slider slider;
    private bool variablesLoaded;


    public override void DefineControlLevel()
    {

        //define States within this Control Level
        State StartButton = new State("StartButton");
        State ChooseSphere = new State("ChooseSphere");
        State ChangeContext = new State("ChangeContext");
        State Feedback = new State("Feedback");
        State ITI = new State("ITI");
        AddActiveStates(new List<State> { StartButton, ChooseSphere, ChangeContext, Feedback, ITI });


        AddInitializationMethod(() =>
        {
            if (!variablesLoaded)
            {
                variablesLoaded = true;
                loadVariables();
            }
        });

        SetupTrial.SpecifyTermination(() => true, StartButton);

        // define initScreen state
        StartButton.AddInitializationMethod(() =>
        {
            trialCount++;

            ResetRelativeStartTime();
            if (context != 0)
            {
                Debug.Log(context);
                disableAllGameobjects();
            }
            context = CurrentTrialDef.Context;

            initButton.SetActive(true);

            context = CurrentTrialDef.Context;


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
                        if (context == 1)
                        {
                            Camera.main.backgroundColor = color1;
                        }
                        else if (context == 2)
                        {
                            Camera.main.backgroundColor = color2;
                        }
                        else
                        {
                            Camera.main.backgroundColor = color3;
                        }
                    }
                }
            }
        });

        StartButton.SpecifyTermination(() => response == 0, ChooseSphere);
        StartButton.AddDefaultTerminationMethod(() => {
            sliderValueIncreaseAmount = (100f / CurrentTrialDef.ObjectNums.Length) / 100f;
            slider.gameObject.SetActive(true);

            initButton.SetActive(false);

        });

        // Define stimOn state
        ChooseSphere.AddInitializationMethod(() =>
        {
            currentObjects = new GameObject[CurrentTrialDef.ObjectNums.Length];
            GameObject.Find("Slider").SetActive(true);
            for (int i = 0; i < CurrentTrialDef.ObjectNums.Length; ++i)
            {
                currentObjects[i] = totalObjects[CurrentTrialDef.ObjectNums[i] - 1];
                currentObjects[i].SetActive(true);
                // float height = 2f * cam.orthographicSize;
                // float w = height * cam.aspect;
                // float w = Screen.width;
                //float step = w / (CurrentTrialDef.ObjectNums.Length); 
                float step = 21.0f / (float)(CurrentTrialDef.ObjectNums.Length - 1);
                currentObjects[i].transform.position = new Vector3(-10f + step * i, 0, 0);
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
                    if (hit.transform.name == currentObjects[correctIndex].name)
                    {
                        slider.value += sliderValueIncreaseAmount;
                        currentObjects[correctIndex].SetActive(false);
                        trialStim = hit.transform.gameObject;
                        sphereCount += 1;
                        response = 1;
                        fb.GetComponent<RawImage>().color = Color.green;
                        fb.SetActive(true);
                    }
                    else
                    {
                        sphereChoice = hit.transform.name;
                        trialStim = hit.transform.gameObject;
                        response = 1;
                        fb.GetComponent<RawImage>().color = Color.red;
                        fb.SetActive(true);

                        //subtractToken

                        /*   numChosenRight++;
                             Debug.Log("Chose 2");
                             sphereChoice = "2";
                             objects[1].SetActive(false);
                         */
                    }

                }
                else
                {
                    Debug.Log("Didn't click on any sphere");
                }
            }
        });
        ChooseSphere.SpecifyTermination(() => sphereCount == CurrentTrialDef.ObjectNums.Length, Feedback);
        ChooseSphere.AddDefaultTerminationMethod(() =>
        {
            foreach (GameObject obj in currentObjects)
            {
                obj.SetActive(false);
            }
            foreach (GameObject obj in totalObjects)    //reset each time??
            {
                obj.SetActive(false);
            }

            sphereCount = 0;

            Debug.Log("Changed Context");
         
        });


        Feedback.AddInitializationMethod(() =>
        {
            if (response == 1)
            {
                fb.GetComponent<RawImage>().color = Color.green;
            }
            else
            {
                fb.GetComponent<RawImage>().color = Color.red;
            }
            fb.SetActive(true);
        });

        Feedback.AddTimer(1f, ITI, () => {
            fb.SetActive(false);
            slider.gameObject.SetActive(false);
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

        });

        ITI.AddTimer(1f, FinishTrial, () =>
        {
            Debug.Log("Trial" + trialCount + " completed");

            //trialData.AppendData(); 
            //trialData.WriteData();
        });
        TrialData.AddDatum("ClickCount", () => clickCount);

    }
    // set all gameobjects to setActive false
    void disableAllGameobjects()
    {
        initButton.SetActive(false);
        fb.SetActive(false);
        goCue.SetActive(false);
        slider.gameObject.SetActive(false);
        foreach (GameObject obj in currentObjects)
        {
            obj.SetActive(false);
        }
        foreach (GameObject obj in totalObjects)
        {
            obj.SetActive(false);
        }
        clickMarker.SetActive(false);

    }

    void loadVariables()
    {

        initButton = GameObject.Find("StartButton");
        fb = GameObject.Find("FB");
        goCue = GameObject.Find("ResponseCue");
        clickMarker = GameObject.Find("ClickMarker");
        slider = GameObject.Find("Slider").GetComponent<Slider>();

        sliderInitPosition = slider.gameObject.transform.position;
        int len = 6;
        totalObjects = new GameObject[len];
        System.Random rnd = new System.Random();
        for (int i = 0; i < len; ++i)
        {
            string s = "Sphere" + (i + 1).ToString();
            Debug.Log(s);
            totalObjects[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            totalObjects[i].GetComponent<Renderer>().material.SetColor("_Color", new Color((float)rnd.NextDouble(), (float)rnd.NextDouble(), (float)rnd.NextDouble()));
            totalObjects[i].name = s;
            totalObjects[i].SetActive(false);
        }

        initButton.SetActive(false);
        fb.SetActive(false);
        goCue.SetActive(false);
        clickMarker.SetActive(false);
        GameObject.Find("Slider").SetActive(false);
    }
    void placeSphere(GameObject sphere)
    {
        // set the position of the balloon 1z in front of the camera
        sphere.transform.position = new Vector3(sphere.transform.position.x, sphere.transform.position.y, 1f);

    }


    void ChangeColor(GameObject obj, Color color)
    {
        var material = obj.GetComponent<Renderer>().material;
        material.color = color;
    }


}











