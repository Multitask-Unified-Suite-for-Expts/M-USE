using System.Collections.Generic;
using UnityEngine;
using USE_ExperimentTemplate;
using USE_States;
using UnityEngine.UI;
using USE_StimulusManagement;
using VisualSearch_Namespace;
using System;
using Random = UnityEngine.Random;
using USE_UI;

public class VisualSearch_TrialLevel : ControlLevel_Trial_Template
{
    public VisualSearch_TrialDef CurrentTrialDef => GetCurrentTrialDef<VisualSearch_TrialDef>();

    private StimGroup targetStims;
    private StimGroup distractorStims1;
    private StimGroup distractorStims2;
    //private StimGroup distractorStims3;
    
    private USE_Button testButton;

    public float 
        DisplayStimsDuration = 5f, 
        TrialEndDuration = 5f;

    // game obeject variables
    private GameObject initButton, trialStim, clickMarker;
    private GameObject[] totalObjects;
    private GameObject[] currentObjects;
    public GameObject YellowHaloPrefab;
    public GameObject GrayHaloPrefab;

    //effort reward variables
    private int clickCount, context;
    [System.NonSerialized] public int response = -1, trialCount = -1, numTrials = 5;

    // vector3 variables
    private Vector3 sliderInitPosition;

    // misc variables
    private Slider slider;
    private float value = 0.0f;
    private float sliderValueIncreaseAmount;
    private Ray mouseRay;
    private bool variablesLoaded;
    
    private StimGroup externalStimsA, externalStimsB, externalStimsC;

    private int numDistractor = 0;

    private bool fam = true;

    private Color[] colors = new[]
    {
        new Color(0.1f, 0.59f, 0.28f),
        new Color(0.54f, 0.18f, 0.18f),
        new Color(0.6275f, 0.3216f, 0.1765f),
        new Color(0.8275f, 0.3f, 0.5275f),
        new Color(0.46f, 0.139f, 0.5471f)
    };
    
    public override void DefineControlLevel()
    {
        State initTrial = new State("InitTrial");
        State searchDisplay = new State("SearchDisplay");
        State selectionFeedback = new State("SelectionFeedback");
        State tokenFeedback = new State("TokenFeedback");
        State trialEnd = new State("TrialEnd");

        Text commandText = null;
        //CurrentTrialDef.initTrialDuration = 20f;

        AddActiveStates(new List<State> {initTrial, searchDisplay, selectionFeedback, tokenFeedback, trialEnd});

        bool firstTrial = true;

        //adapt StartButton from whatwhenwhere task
        AddInitializationMethod(() =>
        {           
            if (!variablesLoaded)
            {
                variablesLoaded = true;
                loadVariables();
            }
            if (firstTrial){
                //TokenFeedbackController.Initialize(5, CurrentTrialDef.tokenRevealDuration, CurrentTrialDef.tokenUpdateDuration);
                firstTrial = false;
            }
        });

        SetupTrial.SpecifyTermination(() => true, initTrial);

        initTrial.AddInitializationMethod(() =>
        {
            trialCount++;

            if (trialCount != numTrials)
            {
                changeContext(colors);
            }

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
            slider.value = value;
        });

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
                        response = 0;
                        initButton.SetActive(false);
                        slider.gameObject.SetActive(true);
                        //fb.gameObject.SetActive(true);

                    }
                }
            }
        });

        initTrial.SpecifyTermination(() => response == 0, searchDisplay);

        //initTrial.AddTimer(()=>CurrentTrialDef.initTrialDuration, searchDisplay);

        /* work!
        searchDisplay.AddDefaultTerminationMethod(() =>
        {
            commandText = GameObject.Find("CommandText").GetComponent<Text>();
            commandText.text = "pressed button";
        });
        searchDisplay.SpecifyTermination(()=>InputBroker.GetMouseButtonDown(0), selectionFeedback);
        //displayStims.AddTimer(() => DisplayStimsDuration, chooseStim);

        
        bool responseMade = false;
        selectionFeedback.AddInitializationMethod(() => responseMade = false);
        */

        // whether this is the familization trial

        bool responseMade = false;

        bool correct = false;
        GameObject selected = null;
        int maxClick = 3;
        int click = 0;

        searchDisplay.AddInitializationMethod(() => responseMade = false);

        searchDisplay.AddUpdateMethod(() =>
        {
            correct = false;
            GameObject clicked = GetClickedObj();
            if (!clicked) return;
            StimDefPointer sdPointer = clicked.GetComponent<StimDefPointer>();
            if (!sdPointer) return;

            click++;

            VisualSearch_StimDef sd = sdPointer.GetStimDef<VisualSearch_StimDef>();
            selected = clicked;
            correct = sd.IsTarget;

            if(correct){
                Debug.Log("correct");
            }
            else{
                Debug.Log("NO");
            }
        });
        searchDisplay.SpecifyTermination(() => selected!=null, selectionFeedback);

        GameObject halo = null;
        selectionFeedback.AddInitializationMethod(() =>
        {
            if (!selected) return;
            if (correct)
            {
                halo = Instantiate(YellowHaloPrefab, selected.transform);
            }
            else
            {
                halo = Instantiate(GrayHaloPrefab, selected.transform);
            }

        });
        selectionFeedback.AddTimer(() => CurrentTrialDef.selectionFbDuration, tokenFeedback);

        tokenFeedback.AddInitializationMethod(() =>
        {
            //Destroy(halo);
            if (correct){
                slider.value += (float)0.25;
                value += (float)0.25;
            }
            else{
                Debug.Log("he?");
            }
        });
        //tokenFeedback.SpecifyTermination(() => !correct, trialEnd);
        tokenFeedback.AddTimer(0.5f, trialEnd);

        // Wait for some time at the end
        trialEnd.AddInitializationMethod(() =>
        {
            //disableAllGameobjects();
            Debug.Log("helooo?");
        });
        trialEnd.AddTimer(0.5f, initTrial, () => trialCount++);
        trialEnd.AddTimer(() => CurrentTrialDef.trialEndDuration, FinishTrial);
        this.AddTerminationSpecification(() => trialCount > numTrials, ()=> Debug.Log(trialCount + " " + numTrials));
 
       /*
       bool responseMade = false;
        searchDisplay.AddInitializationMethod(() => responseMade = false);
        //add update function where choice is made
        searchDisplay.SpecifyTermination(()=> responseMade, selectionFeedback);
        searchDisplay.AddTimer(()=>CurrentTrialDef.maxSearchDuration, FinishTrial);

        
        selectionFeedback.AddInitializationMethod(() => { });
        //adapt from ChoseWrong/Right in whatwhenwhere task
        selectionFeedback.AddTimer(()=> CurrentTrialDef.selectionFbDuration, tokenFeedback);
        
        
        bool tokenUpdated = false;
        tokenFeedback.AddInitializationMethod(() => tokenUpdated = false);
        //wait for Marcus to integrate token fb
        tokenFeedback.SpecifyTermination(() => true, trialEnd); //()=> tokenUpdated, tokenFeedback);

        trialEnd.AddTimer(()=>CurrentTrialDef.trialEndDuration, FinishTrial);
        */
    }

    protected override void DefineTrialStims()
    {
        //Define StimGroups consisting of StimDefs whose gameobjects will be loaded at TrialLevel_SetupTrial and 
        //destroyed at TrialLevel_Finish
        targetStims = new StimGroup("TargetStims", ExternalStims, CurrentTrialDef.GroupAIndices);
        targetStims.SetVisibilityOnOffStates(GetStateFromName("SearchDisplay"), GetStateFromName("TokenFeedback"));
        targetStims.SetLocations(CurrentTrialDef.GroupALocations);
        foreach (VisualSearch_StimDef sd in targetStims.stimDefs)
            sd.IsTarget = true;
        TrialStims.Add(targetStims);

        /*for(int i = 0; i <= trialCount; i++){
            StimGroup temp = new StimGroup("TargetStims", ExternalStims, CurrentTrialDef.GroupBIndices);
            distractorStims = temp;
            distractorStims.SetVisibilityOnOffStates(GetStateFromName("SearchDisplay"), GetStateFromName("TokenFeedback"));
            distractorStims.SetLocations(CurrentTrialDef.GroupBLocations);
            foreach (VisualSearch_StimDef sd in distractorStims.stimDefs)
                sd.IsTarget = false;
            TrialStims.Add(distractorStims);
        }*/

        // If fam, only show target, else also show distractors
        
        if(fam == true){
            fam = false;
            numDistractor = 3;
        }
        else{
            distractorStims1 = new StimGroup("TargetStims", ExternalStims, CurrentTrialDef.GroupBIndices);
            distractorStims1.SetVisibilityOnOffStates(GetStateFromName("SearchDisplay"), GetStateFromName("TokenFeedback"));
            distractorStims1.SetLocations(CurrentTrialDef.GroupBLocations);
            foreach (VisualSearch_StimDef sd in distractorStims1.stimDefs)
                sd.IsTarget = false;
            TrialStims.Add(distractorStims1);

        }
        
    }

    protected override void test(){
        Debug.Log("reached");
        /*Vector3 ButtonPosition = new Vector3(0f, 0f, 0f);
		Vector3 ButtonScale = new Vector3(1f, 1f, 1f);
        //testButton = sttartButton;
        testButton = new USE_Button(ButtonPosition, ButtonScale);
        testButton.defineButton();
        testButton.SetVisibilityOnOffStates(GetStateFromName("InitTrial"), GetStateFromName("InitTrial"));*/
    }

    void disableAllGameobjects()
    {
        initButton.SetActive(false);
        //fb.SetActive(false);
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
        //fb = GameObject.Find("FB");
        clickMarker = GameObject.Find("ClickMarker");
        slider = GameObject.Find("Slider").GetComponent<Slider>();

        sliderInitPosition = slider.gameObject.transform.position;

        initButton.SetActive(false);
        //fb.SetActive(false);
        clickMarker.SetActive(false);
        GameObject.Find("Slider").SetActive(false);
    }
    
    private GameObject GetClickedObj()
    {
        if (!InputBroker.GetMouseButtonDown(0)) return null;
        Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(mouseRay, out RaycastHit hit)) return hit.transform.root.gameObject;
        return null;
    }

    private void changeContext(Color[] colors)
    {
        int num = Random.Range(0, colors.Length - 1);
        Camera.main.backgroundColor = colors[num];
    }
}
