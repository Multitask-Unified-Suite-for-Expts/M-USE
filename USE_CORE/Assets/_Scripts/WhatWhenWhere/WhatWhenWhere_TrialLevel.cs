using System;
using System.Collections.Generic;
using System.Collections;
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
    private GameObject initButton, fb, trialStim, clickMarker, goCue, halo, sliderHalo, txt;
    private GameObject[] totalObjects;
    private GameObject[] currentObjects;
    

    // effort reward variables
    private int clickCount, context;
    public int sphereCount = 0;
    private int numChosenLeft, numChosenRight;
    private bool selectedIncorrect = false;
    private int numObjMax = 10;
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
    private List<Color> Colors = new List<Color> { };
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
            TrialData.AddDatum("TrialID", () => CurrentTrialDef.TrialID);

            ResetRelativeStartTime();
            if (context != 0)
            {
                Debug.Log(context);
                disableAllGameobjects();
            }
            context = CurrentTrialDef.Context;

            initButton.SetActive(true);
            goCue.SetActive(true);

            Camera.main.backgroundColor = new Color(255, 255, 255);

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
                        fb.GetComponent<RawImage>().color = Color.green;
                        fb.SetActive(true);
                    }
                    else
                    {
                        halo.transform.position = hit.transform.position;
                        StartCoroutine(showHalo());
                        sphereChoice = hit.transform.name;
                        trialStim = hit.transform.gameObject;
                        response = 1;
                        fb.GetComponent<RawImage>().color = Color.red;
                        fb.SetActive(true);

                        sphereCount = 0;
                        SpecifyCurrentState(StartButton);

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
            StartCoroutine(flashingSlider());
            Debug.Log("Changed Context");

            fb.GetComponent<RawImage>().color = new Color(0, 0, 0, 0);
            fb.SetActive(false);

            foreach (GameObject obj in currentObjects)
            {
                obj.SetActive(false);
            }
            foreach (GameObject obj in totalObjects)    //reset each time??
            {
                obj.SetActive(false);
            }

            sphereCount = 0;

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
        slider.gameObject.SetActive(false);
        goCue.SetActive(false);
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
        initButton = GameObject.Find("StartButton");
        fb = GameObject.Find("FB");
        clickMarker = GameObject.Find("ClickMarker");
        slider = GameObject.Find("Slider").GetComponent<Slider>();
        goCue = GameObject.Find("StartText");
        halo = GameObject.Find("Halo0");
        sliderHalo = GameObject.Find("SliderHalo");

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
        clickMarker.SetActive(false);
        goCue.SetActive(false);
        GameObject.Find("Slider").SetActive(false);
        halo.SetActive(false);
        sliderHalo.SetActive(false);
        txt.SetActive(false);


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

    IEnumerator showHalo()
    {
        Time.timeScale = 0;
        halo.SetActive(true);

        yield return new WaitForSecondsRealtime(0.25f);

        halo.SetActive(false);
        Time.timeScale = 1;

    }

    IEnumerator flashingSlider()
    {
        //Print the time of when the function is first called.
        Debug.Log("TESTING: " + Time.time);
        SpriteRenderer sr = sliderHalo.GetComponent<SpriteRenderer>();
        sliderHalo.SetActive(true);
        txt.SetActive(true);
        for (int i = 0; i < 2; ++i)
        {
            sr.color = new Color(1, 0, 0, 0.2f);
            // txt.GetComponent<Renderer>().material.SetColor("_Color", Color.red);

            yield return new WaitForSeconds(0.2f);

            sr.color = new Color(1, 1, 1, 0.2f);
            //  txt.GetComponent<Renderer>().material.SetColor("_Color", Color.white);


            yield return new WaitForSeconds(0.2f);
        }
        txt.SetActive(false);
        sliderHalo.SetActive(false);


        //After we have waited 5 seconds print the time again.
        Debug.Log("Finished test : " + Time.time);
    }


}











