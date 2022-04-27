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
        ChooseStimDuration = 5f,
        TrialEndDuration = 2f,
        TouchFeedbackDuration = 1f;

    // game object variables
    public GameObject StartButton;
    private GameObject trialStim;
    private GameObject YellowHaloPrefab;
    private GameObject GrayHaloPrefab;

    // effort reward variables
    private int context;
    //[System.NonSerialized] public int trialCount = -1;
    public int trialCount;
    
    // misc variables
    private Ray mouseRay;
    private bool variablesLoaded;
    
    // trial variables
    public int numTrials = 10;

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
        bool started = false;
        SetupTrial.AddUpdateMethod(() =>
        {
            if (!variablesLoaded)
            {
                variablesLoaded = true;
                loadVariables();
            }
        });
        SetupTrial.SpecifyTermination(() => true, initTrial);
        
        initTrial.AddInitializationMethod(() =>
        {
            trialCount++;
            StartButton.SetActive(true);
        });
        
        // --------------update InitTrial -----------------
        initTrial.AddUpdateMethod(() =>
        {
            StartButton.SetActive(true);
            if (InputBroker.GetMouseButtonDown(0))
            {
                mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(mouseRay, out hit))
                {
                    if (hit.transform.name == "StartButton")
                    {
                        Debug.Log("pressed start button");
                        started = true;
                    }
                }
            }
        });
        initTrial.SpecifyTermination(() => started, displayStims, ()=>StartButton.SetActive(false));

        // --------------Initialize displayStims State -----------------
        displayStims.AddTimer(() => DisplayStimsDuration, chooseStim);

        // --------------chooseStims State -----------------
        bool StimIsChosen = false;
        bool isNew = false;
        GameObject chosen = null;
        chooseStim.AddUpdateMethod(() =>
        {
            StimIsChosen = false;
            isNew = false;
            chosen = null;
            if (InputBroker.GetMouseButtonDown(0))
            {
                chosen = GetClickedObj();
                int curStimCount = currentTrialStims.stimDefs.Count;

                StimDefPointer sdPointer = chosen.GetComponent<StimDefPointer>();
                if (!sdPointer)
                {
                    return;
                }
                else
                {
                    StimIsChosen = true;
                }

                ContinuousRecognition_StimDef sd = sdPointer.GetStimDef<ContinuousRecognition_StimDef>();
                bool correct = false;
                correct = sd.PreviouslyChosen;
                if (sd.PreviouslyChosen == false)
                {
                    Debug.Log("new stimuli");
                    sd.PreviouslyChosen = true;
                    Debug.Log(sd.PreviouslyChosen);
                    isNew = true;
                }
                else
                {
                    isNew = false;
                    Debug.Log("chosen before");
                }
            }
        });
        chooseStim.SpecifyTermination(() => StimIsChosen, touchFeedback);
        chooseStim.AddTimer(() => ChooseStimDuration, FinishTrial);
        
        GameObject halo = null;
        bool touchFeedbackFinished = false;
        touchFeedback.AddInitializationMethod(() =>
        {
            if (!StimIsChosen) return;
            Debug.Log("message: trialCount is " + trialCount);
            if (isNew)
            {
                halo = YellowHaloPrefab;
                halo.transform.position = chosen.transform.position;
                halo.SetActive(true);
                touchFeedbackFinished = true;
            }
            else
            {
                halo = GrayHaloPrefab;
                halo.transform.position = chosen.transform.position;
                halo.SetActive(true);
                touchFeedbackFinished = true;
            }
        });
        touchFeedback.AddTimer(() => TouchFeedbackDuration, trialEnd, ()=>halo.SetActive(false));
        //TODO: if selected wrong stimuli, do i display token? do I go to feed back? just terminate?

        //TODO: cannot write this termination method in touchFeedback, or else it will just directly terminate
        //tokenFeedback.SpecifyTermination(()=> !isNew, trialEnd);
        
        /*
        touchFeedback.SpecifyTermination(() => touchFeedbackFinished, tokenFeedback);
        tokenFeedback.AddTimer(()=>2f, trialEnd);
        //tokenFeedback.SpecifyTermination(() => true, trialEnd, ()=>trialCount++); // from marcus*/
        trialEnd.AddTimer(() => TrialEndDuration, FinishTrial);
        this.AddTerminationSpecification(()=> trialCount > numTrials, ()=> Debug.Log("Current Trial Count is "+ trialCount));
        
    }
    
    

    protected override void DefineTrialStims()
    {
        //Define StimGroups consisting of StimDefs whose gameobjects will be loaded at TrialLevel_SetupTrial and 
        //destroyed at TrialLevel_Finish
        //ExternalStims in this call will be replaced with CurrentBlockDef.BlockStims once Marcus gets that working
        //StimGroup currentTrialStims = new StimGroup("CurrentTrialStims", ExternalStims, CurrentTrialDef.TrialStimIndices);
        
        //add all previously chosen stimuli to current trial 
        
        //create new list consisting of PreviouslyChosenStimuli + one random non previously chosen stim from BlockStimIndices;
        List<int> trialStimIndices = CurrentTrialDef.PreviouslyChosenStimuli;
        //if currentTrialDef.TrialCountInBlock == 0
        //choose two random stims
        //if currenTrialDef.TrialCountInBlock % 2 == 0
        //trialStimIndices.Add(random non previously-chosen stim);
        //else
        //trialStimIndices.Add(previously non chosen object);
        
        currentTrialStims = new StimGroup("TrialStims", ExternalStims, CurrentTrialDef.GroupAIndices); //replace groupAIndices with trialStimIndices
        currentTrialStims.SetLocations(CurrentTrialDef.GroupALocations); 
        currentTrialStims.SetVisibilityOnOffStates(GetStateFromName("DisplayStims"), GetStateFromName("TokenFeedback"));
        TrialStims.Add(currentTrialStims);
    }

    void loadVariables()
    {
        StartButton = GameObject.Find("StartButton");
        YellowHaloPrefab = GameObject.Find("YellowHalo");
        GrayHaloPrefab = GameObject.Find("GrayHalo");
        StartButton.SetActive(true);
        YellowHaloPrefab.SetActive(false);
        GrayHaloPrefab.SetActive(false);
    }
    
    private GameObject GetClickedObj()
    {
        if (!InputBroker.GetMouseButtonDown(0)) return null;
        Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(mouseRay, out RaycastHit hit))
        {
            return hit.transform.root.gameObject;
        }
        return null;
    }
    
    
    
}