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
    private GameObject initButton, goCue, trialStim, clickMarker, halo, sliderHalo;
    private GameObject txt;

    //  private GameObject[] halos;
    private GameObject[] totalObjects;
    private GameObject[] currentObjects;

    // effort reward variables
    private int clickCount, context;
    public int sphereCount = 0;
    private int numChosenLeft, numChosenRight;
    private bool incorrectChoice = false;
    private int numObjMax = 10;
    private float startTime;
    private float timeDif;
    private List<Color> Colors = new List<Color> { };
    private SpriteRenderer sr;

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
        State ChoseWrong = new State("ChoseWrong");
        State Feedback = new State("Feedback");
        State ITI = new State("ITI");
        AddActiveStates(new List<State> { StartButton, ChooseSphere, ChoseWrong, Feedback, ITI });
        //chosewrong state
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
            if (incorrectChoice == false)
            {
                trialCount++;
            }

            Camera.main.backgroundColor = new Color(0.5f, 0.4f, 0.96f); ;

            ResetRelativeStartTime();
            if (context != 0)
            {
                Debug.Log(context);
                disableAllGameobjects();
            }

            context = CurrentTrialDef.Context;

            initButton.SetActive(true);
            goCue.SetActive(true);

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
                        Camera.main.backgroundColor = Colors[context];
                    }
                }
            }
        });

        StartButton.SpecifyTermination(() => response == 0, ChooseSphere);
        StartButton.AddDefaultTerminationMethod(() => {
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
                Debug.Log(CurrentTrialDef.ObjectXLocations[i]);
                currentObjects[i].transform.position = new Vector3(CurrentTrialDef.ObjectXLocations[i], CurrentTrialDef.ObjectYLocations[i], 0);
                string h = "Halo" + (i).ToString();

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
                    }
                    else
                    {
                        halo.transform.position = hit.transform.position;
                        sphereChoice = hit.transform.name;
                        trialStim = hit.transform.gameObject;
                        response = 1;
                        TrialData.AddDatum("TrialID", () => CurrentTrialDef.TrialID);
                        incorrectChoice = true;
                    }

                }
                else
                {
                    Debug.Log("Didn't click on any sphere");
                }
            }
        });

        ChooseSphere.SpecifyTermination(() => incorrectChoice, ChoseWrong);
        ChooseSphere.SpecifyTermination(() => sphereCount == CurrentTrialDef.ObjectNums.Length, Feedback);
        ChooseSphere.AddDefaultTerminationMethod(() =>
        {
            if (!incorrectChoice)
            {
                foreach (GameObject obj in currentObjects)
                {
                    obj.SetActive(false);
                }
                foreach (GameObject obj in totalObjects)    //reset each time??
                {
                    obj.SetActive(false);
                }
            }
            sphereCount = 0;

        });

        ChoseWrong.AddInitializationMethod(() =>
        {
            halo.SetActive(true);

        });

        ChoseWrong.AddTimer(0.75f, StartButton, () => {
            halo.SetActive(false);
            incorrectChoice = false;
        });

        Feedback.AddInitializationMethod(() =>
        {
            sliderHalo.SetActive(true);
            sr = sliderHalo.GetComponent<SpriteRenderer>();
            sr.color = new Color(1, 1, 1, 0.2f);
            txt.SetActive(true);
            startTime = Time.time;
        });
        Feedback.AddUpdateMethod(() =>
        {
            Debug.Log(Time.time);

            if ((int)(10 * (Time.time - startTime)) % 4 == 0)
            {
                sr.color = new Color(1, 1, 1, 0.2f);
                Debug.Log("Entered if");
            }
            else if ((int)(10 * (Time.time - startTime)) % 2 == 0)
            {
                sr.color = new Color(1, 0, 0, 0.2f);
            }
        });
        Feedback.AddTimer(2f, ITI, () => {

            txt.SetActive(false);
            sliderHalo.SetActive(false);


            //After we have waited 5 seconds print the time again.
            Debug.Log("Finished test : " + Time.time);
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

        /*     ITI.AddTimer(0.2f, FinishTrial, () =>
             {
                 Debug.Log("Trial" + trialCount + " completed");

                 //trialData.AppendData(); 
                 //trialData.WriteData();
             });
             */
        ITI.SpecifyTermination(() => true, FinishTrial, () => Debug.Log("Trial" + trialCount + " completed"));

        TrialData.AddDatum("ClickCount", () => clickCount);

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
        foreach (GameObject obj in totalObjects)
        {
            obj.SetActive(false);
        }
        clickMarker.SetActive(false);

    }

    void loadVariables()
    {
        txt = GameObject.Find("FinalText");
        txt.SetActive(false);
        halo = GameObject.Find("Halo0");
        halo.SetActive(false);
        initButton = GameObject.Find("StartButton");
        goCue = GameObject.Find("StartText");
        clickMarker = GameObject.Find("ClickMarker");
        slider = GameObject.Find("Slider").GetComponent<Slider>();
        sliderHalo = GameObject.Find("SliderHalo");
        sliderHalo.SetActive(false);
        sliderInitPosition = slider.gameObject.transform.position;
        totalObjects = new GameObject[numObjMax];
        System.Random rnd = new System.Random();
        for (int i = 0; i < numObjMax; ++i)
        {
            string s = "Sphere" + (i + 1).ToString();
            Debug.Log(s);
            totalObjects[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            totalObjects[i].GetComponent<Renderer>().material.SetColor("_Color", new Color((float)rnd.NextDouble(), (float)rnd.NextDouble(), (float)rnd.NextDouble()));
            totalObjects[i].name = s;
            // totalObjects[i].AddComponent<Light>()
            totalObjects[i].SetActive(false);


        }

        initButton.SetActive(false);
        goCue.SetActive(false);

        clickMarker.SetActive(false);
        GameObject.Find("Slider").SetActive(false);

        Colors.Add(new Color(0.815f, 0.105f, 0.105f));
        Colors.Add(new Color(0.929f, 0.874f, 0.270f));
        Colors.Add(new Color(0.117f, 0.815f, 0.086f));
        Colors.Add(new Color(0.086f, 0.0705f, 0.815f));
        Colors.Add(new Color(0.388f, 0.501f, 0.811f));
        Colors.Add(new Color(0.705f, 0.094f, 0.631f));
        Colors.Add(new Color(0.705f, 0.333f, 0.094f));
        Colors.Add(new Color(0.658f, 0.505f, 0.913f));
        Colors.Add(new Color(0f, 0f, 0f));

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













