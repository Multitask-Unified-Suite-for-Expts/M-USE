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
    private GameObject[] objects;
    //  private string[] correctNames;
    /// public int[] tmpNums;
    // private GameObject[] objects = new GameObject[4];
    //sphere1, sphere2, sphere3, sphere4,
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

    // misc variables
    private Ray mouseRay;


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
            disableAllGameobjects();
            context = CurrentTrialDef.Context;

            initButton.SetActive(true);

            context = CurrentTrialDef.Context;


            clickCount = 0;
            response = -1;
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
                            Camera.main.backgroundColor = Color.yellow;
                        }
                        else if (context == 2)
                        {
                            Camera.main.backgroundColor = Color.cyan;
                        }
                        else
                        {
                            Camera.main.backgroundColor = Color.green;
                        }
                    }
                }
            }
        });

        StartButton.SpecifyTermination(() => response == 0, ChooseSphere);
        StartButton.AddDefaultTerminationMethod(() => initButton.SetActive(false));

        // Define stimOn state
        ChooseSphere.AddInitializationMethod(() =>
        {
            //foreach (GameObject obj in objects)
            //{
            //   obj.SetActive(true);
            //}
            int j = 0;
            for (int i = 0; i < CurrentTrialDef.ObjectNums.Length; ++i)
            {
                objects[i].SetActive(true);
            }

            //  sphere1.SetActive(true);
            //  sphere2.SetActive(true);
            //  sphere3.SetActive(true);
            //  sphere4.SetActive(true);

            trialStim = null;
            //trialStim2 = null;
            //trialStim3 = null;
            //trialStim4 = null;
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
                    // if (hit.transform.name == objects[0].name)
                    //  {
                    //    numChosenLeft++;
                    //   Debug.Log("Chose 1");
                    //   sphereChoice = "1";
                    Debug.Log("SC: " + sphereCount);
                    int correctIndex = CurrentTrialDef.ObjectNums[CurrentTrialDef.CorrectObjectTouchOrder[sphereCount]-1]-1;
                  //  Debug.Log("preIndex: " + correctIndex);

                  //  correctIndex -= 1;
                    Debug.Log("index: " + correctIndex);
                    if (hit.transform.name == objects[correctIndex].name)
                {
                        Debug.Log(sphereCount);
                        //addToken
                        objects[correctIndex].SetActive(false);
                        //  if (CurrentTrialDef.CorrectObjectTouchOrder[sphereCount] == objects[sphereCount]{

                        // }
                        trialStim = hit.transform.gameObject;
                        sphereCount += 1;
                        response = 1;
                    }
                    else
                    {
                        //subtractToken
                    }

                }
                /*  else if (hit.transform.name == objects[1].name)
                  {
                      numChosenRight++;
                      Debug.Log("Chose 2");
                      sphereChoice = "2";

                      objects[1].SetActive(false);


                      trialStim = hit.transform.gameObject;
                      sphereCount += 1;
                      response = 1;
                  }
                  else if (hit.transform.name == objects[2].name)
                  {
                      numChosenRight++;
                      Debug.Log("Chose 3");
                      sphereChoice = "3";

                      objects[2].SetActive(false);

                      //trialStim3 = hit.transform.gameObject;
                      trialStim = hit.transform.gameObject;
                      sphereCount += 1;
                      response = 1;
                  }
                  else if (hit.transform.name == objects[3].name)
                  {
                      numChosenRight++;
                      Debug.Log("Chose 4");
                      sphereChoice = "4";

                      objects[3].SetActive(false);


                      // trialStim4 = hit.transform.gameObject;
                      trialStim = hit.transform.gameObject;
                      sphereCount += 1;
                      response = 1;
                  } */
                else
                {
                    Debug.Log("Didn't click on any sphere");
                }
            }
        });
        ChooseSphere.SpecifyTermination(() => sphereCount == CurrentTrialDef.ObjectNums.Length, Feedback);
        ChooseSphere.AddDefaultTerminationMethod(() =>
        {
            foreach (GameObject obj in objects)
            {
                obj.SetActive(false);
            }
            // sphere1.SetActive(false);
            // sphere2.SetActive(false);
            // sphere3.SetActive(false);
            // sphere4.SetActive(false);
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

        Feedback.AddTimer(1f, ITI, () => fb.SetActive(false));

        //Define iti state
        ITI.AddInitializationMethod(() =>
        {
            foreach (GameObject obj in objects)
            {
                obj.SetActive(false);
            }
            // sphere1.SetActive(false);
            // sphere2.SetActive(false);
            // sphere3.SetActive(false);
            // sphere4.SetActive(false);
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
        foreach (GameObject obj in objects)
        {
            obj.SetActive(false);
        }
        //sphere1.SetActive(false);
        //sphere2.SetActive(false);
        //sphere3.SetActive(false);
        //sphere4.SetActive(false);
        clickMarker.SetActive(false);

    }

    void loadVariables()
    {

        initButton = GameObject.Find("StartButton");
        fb = GameObject.Find("FB");
        goCue = GameObject.Find("ResponseCue");
        /* int len;
         len = order.Length;
         Debug.Log("test2");
         Debug.Log("length" + len.ToString());
         for (int i = 0; i < len; ++i)
         {
             string s = "Sphere" + order[i].ToString();
             Debug.Log(s);
             objects[i] = GameObject.Find(s);
             objects[i].name = s;
         }*/
        //  Debug.Log("test1");
        int len = 4;
        objects = new GameObject[len];
        for (int i = 0; i < len; ++i)
        {
            string s = "Sphere" + (i + 1).ToString();

            // string s = "Sphere" + CurrentTrialDef.ObjectNums[i].ToString();
            Debug.Log(s);
            objects[i] = GameObject.Find(s);
            objects[i].name = s;
        }
        //       sphereLeft = GameObject.Find("SphereLeft");
        //       sphereRight = GameObject.Find("SphereRight");
        //      sphere3 = GameObject.Find("Sphere3");

        //  sphere1 = GameObject.Find("Sphere1");
        //  sphere2 = GameObject.Find("Sphere2");
        //  sphere3 = GameObject.Find("Sphere3");
        //  sphere4 = GameObject.Find("Sphere4");


        clickMarker = GameObject.Find("ClickMarker");
        initButton.SetActive(false);
        fb.SetActive(false);
        goCue.SetActive(false);
        clickMarker.SetActive(false);



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











