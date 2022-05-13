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
using USE_Settings;

public class VisualSearch_TrialLevel : ControlLevel_Trial_Template
{
    public VisualSearch_TrialDef CurrentTrialDef => GetCurrentTrialDef<VisualSearch_TrialDef>();

    private StimGroup targetStims;
    private StimGroup distractorStims;

    private string targetName;
    private Vector3 targetLocation;
    private List<string> distractorName;
    private List<Vector3> distractorLocations;

    private int random = 0;
    
    private USE_Button testButton;

    public float 
        DisplayStimsDuration = 5f, 
        TrialEndDuration = 5f;

    // game obeject variables
    private GameObject trialStim, clickMarker;
    private GameObject[] totalObjects;
    private GameObject[] currentObjects;
    public GameObject YellowHaloPrefab;
    public GameObject GrayHaloPrefab;
    public Canvas canvas;
    private int num_distractors = 0;

    //effort reward variables
    private int clickCount, context;
    [System.NonSerialized] public int trialCount = -1, numTrials = 4;

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

        SelectionHandler<VisualSearch_StimDef> mouseHandler = new SelectionHandler<VisualSearch_StimDef>();

        AddActiveStates(new List<State> {initTrial, searchDisplay, selectionFeedback, tokenFeedback, trialEnd});

        AddInitializationMethod(() =>
        {           
            DefineStartButton();
            /*if (!variablesLoaded)
            {
                variablesLoaded = true;
                loadVariables();
            }*/
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

            clickCount = 0;

            //slider.gameObject.transform.position = sliderInitPosition;
            //slider.value = value;
        });

        initTrial.SpecifyTermination(() => testButton.pressed, searchDisplay);

        bool responseMade = false;

        bool correct = false;
        GameObject selected = null;
        VisualSearch_StimDef selectedSD = null;
        int maxClick = 3;
        int click = 0;

        MouseTracker.AddSelectionHandler(mouseHandler, searchDisplay);
        searchDisplay.AddInitializationMethod(() => selected = null);

        /*searchDisplay.AddUpdateMethod(() =>
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
        });*/
        searchDisplay.SpecifyTermination(() => mouseHandler.SelectedStimDef != null, selectionFeedback, () => {
            testButton.pressed = false;
            selected = mouseHandler.SelectedGameObject;
            selectedSD = mouseHandler.SelectedStimDef;
            correct = selectedSD.IsTarget;
        });
        //searchDisplay.SpecifyTermination(() => selected!=null, selectionFeedback);

        GameObject halo = null;
        selectionFeedback.AddInitializationMethod(() =>
        {
            if (!selected) return;
            if (correct) HaloFBController.ShowPositive(selected);
            else HaloFBController.ShowNegative(selected);

        });
        selectionFeedback.AddTimer(() => 1.0f, tokenFeedback);

        tokenFeedback.AddInitializationMethod(() =>
        {

            HaloFBController.Destroy();
            if (correct){
                AudioFBController.Play("Positive");
                TokenFBController.AddTokens(selected, 1);
                //slider.value += (float)0.25;
                value += (float)0.25;
            }
            else{
                AudioFBController.Play("Negative");
                Debug.Log("he?");
            }
        });
        tokenFeedback.SpecifyTermination(() => !TokenFBController.IsAnimating(), trialEnd);
        //tokenFeedback.SpecifyTermination(() => !correct, trialEnd);
        //tokenFeedback.AddTimer(0.5f, trialEnd);

        // Wait for some time at the end
        trialEnd.AddInitializationMethod(() =>
        {
            //disableAllGameobjects();
        });
        trialEnd.AddTimer(0.5f, initTrial, () => trialCount++);
        trialEnd.AddTimer(() => CurrentTrialDef.trialEndDuration, FinishTrial);

        TrialData.AddDatum("TargetName", () => targetName);
        TrialData.AddDatum("TargetLocation", () => targetLocation);
        TrialData.AddDatum("SelectedName", () => selected != null ? selected.name : null);
        TrialData.AddDatum("SelectedLocation", () => selectedSD?.StimLocation ?? null);
        TrialData.AddDatum("SelectionCorrect", () => correct ? 1 : 0);
        TrialData.AddDatum("NumDistractors", () => num_distractors);
        TrialData.AddDatum("DistarctorNames", () => String.Join(", ", distractorName.ToArray()));
        TrialData.AddDatum("DistarctorLocations", () => String.Join(", ", distractorLocations.ToArray()));
        
        
        this.AddTerminationSpecification(() => trialCount > numTrials, ()=> Debug.Log(trialCount + " " + numTrials));
 
    }

    protected override void DefineTrialStims()
    {
        //Define StimGroups consisting of StimDefs whose gameobjects will be loaded at TrialLevel_SetupTrial and 
        //destroyed at TrialLevel_Finish
        int temp = 0;
        distractorName = new List<string>();
        distractorLocations = new List<Vector3>();
        targetStims = new StimGroup("TargetStims", ExternalStims, CurrentTrialDef.GroupAIndices);
        targetStims.SetVisibilityOnOffStates(GetStateFromName("SearchDisplay"), GetStateFromName("TokenFeedback"));
        targetStims.SetLocations(CurrentTrialDef.GroupALocations);
        foreach (VisualSearch_StimDef sd in targetStims.stimDefs){
            sd.IsTarget = true;
            targetName = sd.StimName;
            targetLocation = sd.StimLocation;
        } 
        TrialStims.Add(targetStims);
        
        if(fam == true){
            fam = false;
            numDistractor = 3;
        }
        else{
            distractorStims = new StimGroup("TargetStims", ExternalStims, CurrentTrialDef.GroupBIndices);
            distractorStims.SetVisibilityOnOffStates(GetStateFromName("SearchDisplay"), GetStateFromName("TokenFeedback"));
            distractorStims.SetLocations(CurrentTrialDef.GroupBLocations);
            foreach (VisualSearch_StimDef sd in distractorStims.stimDefs){
                sd.IsTarget = false;
                temp++;
                distractorName.Add(sd.StimName);
                distractorLocations.Add(sd.StimLocation);
            }
            num_distractors = temp;      
            TrialStims.Add(distractorStims);

        }
        
    }

    private void DefineStartButton(){
        if(random == 1){
            return;
        }
        Vector3 buttonPosition = new Vector3(0f, 0f, 0f);
		Vector3 buttonScale = new Vector3(1f, 1f, 1f);
        Color buttonColor = new Color(0.1f, 0.1f, 0.1f);
        Vector3 tempColor = new Vector3(0f, 0f, 0f);
        string buttonText = "";
        //testButton = sttartButton;
        string TaskName = "VisualSearch";
        if (SessionSettings.SettingClassExists(TaskName + "_TaskSettings"))
			{
				if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonPosition"))
					buttonPosition = (Vector3) SessionSettings.Get(TaskName + "_TaskSettings", "ButtonPosition");
				if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonScale"))
					buttonScale = (Vector3) SessionSettings.Get(TaskName + "_TaskSettings", "ButtonScale");
                if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonColor"))
					tempColor = (Vector3) SessionSettings.Get(TaskName + "_TaskSettings", "ButtonColor");
                    buttonColor = new Color(tempColor[0], tempColor[1], tempColor[2]);
                if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonText"))
					buttonText = (string) SessionSettings.Get(TaskName + "_TaskSettings", "ButtonText");
	    }

        testButton = new USE_Button(buttonPosition, buttonScale, canvas, buttonColor, buttonText);
        testButton.defineButton();
        testButton.SetVisibilityOnOffStates(GetStateFromName("InitTrial"), GetStateFromName("InitTrial"));
        random = 1;
    }

    void disableAllGameobjects()
    {

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

        clickMarker = GameObject.Find("ClickMarker");
        slider = GameObject.Find("Slider").GetComponent<Slider>();

        sliderInitPosition = slider.gameObject.transform.position;

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
