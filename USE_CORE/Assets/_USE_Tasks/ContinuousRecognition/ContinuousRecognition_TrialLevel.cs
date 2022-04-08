using UnityEngine;
using System.Collections.Generic;
using USE_ExperimentTemplate;
using USE_States;
using UnityEngine.UI;
using USE_StimulusManagement;
using ContinuousRecognition_Namespace;
using System;
// using Unity.UNetWeaver;

public class ContinuousRecognition_TrialLevel : ControlLevel_Trial_Template
{
    public ContinuousRecognition_TrialDef CurrentTrialDef => GetCurrentTrialDef<ContinuousRecognition_TrialDef>();

    private StimGroup currentTrialStims;
    
    public float 
        DisplayStimsDuration = 5f, 
        TrialEndDuration = 5f;

    // game object variables
    private GameObject initButton, fb, trialStim, clickMarker;
    private GameObject[] totalObjects;
    private GameObject[] currentObjects;

    // effort reward variables
    private int clickCount, context;
    [System.NonSerialized] public int response = -1, trialCount = -1;

    // vector3 variables
    private Vector3 sliderInitPosition;

    // misc variables
    private Slider slider;
    private float sliderValueIncreaseAmount;
    private Ray mouseRay;
    private bool variablesLoaded;

    public override void DefineControlLevel()
    {
        // define States 
        State initTrial = new State("InitTrial");
        State displayStims = new State("DisplayStims");
        State chooseStim = new State("ChooseStim");
        State touchFeedback = new State("TouchFeedback");
        State tokenFeedback = new State("TokenFeedback");
        State trialEnd = new State("TrialEnd");
        AddActiveStates(new List<State> {initTrial, displayStims, chooseStim, touchFeedback, tokenFeedback, trialEnd});

        //TODO testing
        Text commandText = null;
        
        // --------------SetupTrial-----------------
        AddInitializationMethod(() =>
        {
            if (!variablesLoaded)
            {
                variablesLoaded = true;
                loadVariables();
            }
        });

        SetupTrial.SpecifyTermination(() => true, initTrial);

        // --------------Initialize InitTrial -----------------
        //initTrial follows same logic as StartButton in WhatWhenWhere task (see Seema/Nicole)
        initTrial.AddInitializationMethod(() =>
        {
            trialCount++;
            
            //TODO ??
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
        
        // --------------update InitTrial -----------------
        initTrial.AddUpdateMethod(() =>
        {
            if (InputBroker.GetMouseButtonDown(0))
            {
                mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(mouseRay, out hit))
                {
                    if (hit.transform.name == "StartButton")
                    {
                        Debug.Log("mousehit");
                        response = 0; // got response
                        initButton.SetActive(false);
                        slider.gameObject.SetActive(true);
                        fb.gameObject.SetActive(true);
                    }
                }
            }
        });
        initTrial.SpecifyTermination(() => response == 0, displayStims);

        // --------------Initialize displayStims State -----------------
        //displayStims.SpecifyTermination(()=>InputBroker.GetMouseButtonDown(0), chooseStim);
        displayStims.AddTimer(() => DisplayStimsDuration, chooseStim);

        
        // --------------chooseStims State -----------------
        bool StimIsChosen = false;
        chooseStim.AddUpdateMethod(() =>
        {
            if (InputBroker.GetMouseButtonDown(0))
            {
                mouseRay = Camera.main.ScreenPointToRay(InputBroker.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(mouseRay, out hit))
                {
                    if (currentTrialStims.stimDefs.Exists(sd =>
                            ReferenceEquals(sd.StimGameObject, hit.transform.root.gameObject)))
                    {
                        Debug.Log("rachelrachel");
                    }
                }
            }
            /*
            // just for testing
            commandText = GameObject.Find("CommandText").GetComponent<Text>();
            commandText.text = "pressed button";
            GameObject.Find("CommandText").SetActive(true);
            */
            
            //add something that checks for click on object,
            //checks its PreviouslyChosen bool for later token feedback,
            //sets PreviouslyChosen and StimIsChosen to true; - see WWW
        });
        chooseStim.SpecifyTermination(() => StimIsChosen, touchFeedback);

        /*
        //touchfeedback performs touch feedback - see WWW
        bool touchFeedbackFinished = false;
        touchFeedback.SpecifyTermination(() => touchFeedbackFinished, tokenFeedback);

        //tokenfeedback - make empty for now, automatically jump to trialEnd
        tokenFeedback.SpecifyTermination(() => true, trialEnd);

        trialEnd.AddTimer(() => TrialEndDuration, FinishTrial);*/
    }

    protected override void DefineTrialStims()
    {
        //Define StimGroups consisting of StimDefs whose gameobjects will be loaded at TrialLevel_SetupTrial and 
        //destroyed at TrialLevel_Finish
        //ExternalStims in this call will be replaced with CurrentBlockDef.BlockStims once Marcus gets that working
        //StimGroup currentTrialStims = new StimGroup("CurrentTrialStims", ExternalStims, CurrentTrialDef.TrialStimIndices);
        currentTrialStims = new StimGroup("StimGroupA", ExternalStims, CurrentTrialDef.GroupAIndices); 
        currentTrialStims.SetLocations(CurrentTrialDef.GroupALocations);
        currentTrialStims.SetVisibilityOnOffStates(GetStateFromName("DisplayStims"), GetStateFromName("TokenFeedback"));
        TrialStims.Add(currentTrialStims);
    }
    
    void disableAllGameobjects()
    {
        initButton.SetActive(false);
        fb.SetActive(false);
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
            totalObjects[i].GetComponent<Renderer>().material.SetColor("_Color",
                new Color((float) rnd.NextDouble(), (float) rnd.NextDouble(), (float) rnd.NextDouble()));
            totalObjects[i].name = s;
            totalObjects[i].SetActive(false);
        }

        initButton.SetActive(false);
        fb.SetActive(false);
        clickMarker.SetActive(false);
        GameObject.Find("Slider").SetActive(false);
    }
    
    
    
}