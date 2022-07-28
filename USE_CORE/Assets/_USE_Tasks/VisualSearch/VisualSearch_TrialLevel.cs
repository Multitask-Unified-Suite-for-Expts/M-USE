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
using System.IO;

public class VisualSearch_TrialLevel : ControlLevel_Trial_Template
{
    public VisualSearch_TrialDef CurrentTrialDef => GetCurrentTrialDef<VisualSearch_TrialDef>();

    private StimGroup targetStims, distractorStims;
    public GameObject StartButton;

    private string targetName;
    private Vector3 targetLocation;
    private List<string> distractorName;
    private List<Vector3> distractorLocations;
/*
    public float 
        DisplayStimsDuration = 5f, 
        TrialEndDuration = 5f;
*/
    // game obeject variables
    private GameObject trialStim, clickMarker;
    private GameObject startButton, startText;
    private GameObject[] totalObjects;
    private GameObject[] currentObjects;
    //public Canvas canvas;
    private int num_distractors = 0;
    private int response;
    private bool correct;
    private GameObject selected;
    VisualSearch_StimDef selectedSD = null;

    // misc variables
    private Slider slider;
    private float value = 0.0f;
    private float sliderValueIncreaseAmount;
    private Ray mouseRay;
    private bool variablesLoaded;
    public string MaterialFilePath;

    //private StimGroup externalStimsA, externalStimsB, externalStimsC;
    private StimGroup TargetStims, DistractorStims;
    private int numDistractor = 0;
    private USE_Button testButton;

    public override void DefineControlLevel()
    {
        State InitTrial = new State("InitTrial");
        State SearchDisplay = new State("SearchDisplay");
        State SelectionFeedback = new State("SelectionFeedback");
        State TokenFeedback = new State("TokenFeedback");
        State TrialEnd = new State("TrialEnd");

        Text commandText = null;

        SelectionHandler<VisualSearch_StimDef> mouseHandler = new SelectionHandler<VisualSearch_StimDef>();
        MouseTracker.AddSelectionHandler(mouseHandler, SetupTrial);
        AddActiveStates(new List<State> {InitTrial, SearchDisplay, SelectionFeedback, TokenFeedback, TrialEnd});
        SetupTrial.AddInitializationMethod(() =>
        {
            RenderSettings.skybox = CreateSkybox(MaterialFilePath + "\\Blank.png");
            Debug.Log("FilePath: " + MaterialFilePath);
            TokenFBController
                .SetRevealTime(CurrentTrialDef.TokenRevealDuration)
                .SetUpdateTime(CurrentTrialDef.TokenUpdateDuration);
            StartButton.SetActive(true);
            //loadVariables();
            //testButton.ToggleVisibility(true);
            
        });
        SetupTrial.SpecifyTermination(() => mouseHandler.SelectionMatches(StartButton),
            SearchDisplay, () => StartButton.SetActive(false));
        MouseTracker.AddSelectionHandler(mouseHandler, SearchDisplay);
        // Show the target/sample with some other distractors
        // Wait for a click and provide feedback accordingly
        bool correct = false;
        GameObject selected = null;
        VisualSearch_StimDef selectedSD = null;
        MouseTracker.AddSelectionHandler(mouseHandler, SearchDisplay);

        SearchDisplay.AddInitializationMethod(() => selected = null);


        SearchDisplay.SpecifyTermination(() => mouseHandler.SelectedStimDef != null, SelectionFeedback, () => {
            //testButton.pressed = false;
            selected = mouseHandler.SelectedGameObject;
            selectedSD = mouseHandler.SelectedStimDef;
            correct = selectedSD.IsTarget;
        });

        SearchDisplay.AddTimer(() => CurrentTrialDef.MaxSearchDuration, FinishTrial);

        GameObject halo = null;
        SelectionFeedback.AddInitializationMethod(() =>
        {
            if (!selected) return;
            if (correct) HaloFBController.ShowPositive(selected);
            else HaloFBController.ShowNegative(selected);

        });

        SelectionFeedback.AddTimer(() => 0.5f, TokenFeedback);

        TokenFeedback.AddInitializationMethod(() =>
        {

            HaloFBController.Destroy();
            if (correct){
                AudioFBController.Play("Positive");
                TokenFBController.AddTokens(selected, 2);
                //slider.value += (float)0.25;
                value += (float)0.25;
            }
            else{
                AudioFBController.Play("Negative");
                Debug.Log("he?");
            }
        });
        TokenFeedback.SpecifyTermination(() => !TokenFBController.IsAnimating(), TrialEnd);
        TrialEnd.AddTimer(0.5f, FinishTrial);
        
        TrialData.AddDatum("TargetName", () => targetName);
        TrialData.AddDatum("TargetLocation", () => targetLocation);
        TrialData.AddDatum("SelectedName", () => selected != null ? selected.name : null);
        TrialData.AddDatum("SelectedLocation", () => selectedSD?.StimLocation ?? null);
        TrialData.AddDatum("SelectionCorrect", () => correct ? 1 : 0);
        TrialData.AddDatum("NumDistractors", () => num_distractors);
        TrialData.AddDatum("DistractorNames", () => String.Join(", ", distractorName.ToArray()));
        TrialData.AddDatum("DistractorLocations", () => String.Join(", ", distractorLocations.ToArray()));
        
        
        //this.AddTerminationSpecification(() => trialCount > numTrials, ()=> Debug.Log(trialCount + " " + numTrials));
 
    }

    protected override void DefineTrialStims()
    {
        //Define StimGroups consisting of StimDefs whose gameobjects will be loaded at TrialLevel_SetupTrial and 
        //destroyed at TrialLevel_Finish
        int temp = 0;
        distractorName = new List<string>();
        distractorLocations = new List<Vector3>();
        targetStims = new StimGroup("SearchStims", ExternalStims, CurrentTrialDef.SearchStimsIndices);
        targetStims.SetVisibilityOnOffStates(GetStateFromName("SearchDisplay"), GetStateFromName("TokenFeedback"));
        targetStims.SetLocations(CurrentTrialDef.SearchStimsLocations);
        foreach (VisualSearch_StimDef sd in targetStims.stimDefs){
            sd.IsTarget = true;
            targetName = sd.StimName;
            targetLocation = sd.StimLocation;
        } 
        TrialStims.Add(targetStims);
        bool fam = false;
        if(fam == true){
            fam = false;
            numDistractor = 3;
        }
        else{
            distractorStims = new StimGroup("TargetStims", ExternalStims, CurrentTrialDef.DistractorStimsIndices);
            distractorStims.SetVisibilityOnOffStates(GetStateFromName("SearchDisplay"), GetStateFromName("TokenFeedback"));
            distractorStims.SetLocations(CurrentTrialDef.DistractorStimsLocations);
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

    private USE_Button DefineStartButton(Transform parent)
    {
        /*
        if (random == 1)
        {
            return;
        }*/
        Vector3 buttonPosition = new Vector3(0f, 0f, 0f);
        Vector3 buttonScale = new Vector3(1f, 1f, 1f);
        Color buttonColor = new Color(0.1f, 0.1f, 0.1f);
        Vector3 tempColor = new Vector3(0f, 0f, 0f);
        string buttonText = "";
        Canvas canvas = parent.GetComponent<Canvas>();

        //testButton = sttartButton;
        string TaskName = "VisualSearch";
        if (SessionSettings.SettingClassExists(TaskName + "_TaskSettings"))
        {
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonPosition"))
                buttonPosition = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonPosition");
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonScale"))
                buttonScale = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonScale");
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonColor"))
                tempColor = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonColor");
            buttonColor = new Color(tempColor[0], tempColor[1], tempColor[2]);
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonText"))
                buttonText = (string)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonText");
        }
        testButton = new USE_Button(buttonPosition, buttonScale, canvas, buttonColor, buttonText);
        testButton.defineButton();
        return (testButton);

        //testButton.SetVisibilityOnOffStates(GetStateFromName("InitTrial"), GetStateFromName("SearchDisplay"));
        //random = 1;
    }
    void disableAllGameobjects()
    {
       // testButton.ToggleVisibility(false);
       // clickMarker.SetActive(false);
        //startButton.SetActive(false);
        //slider.gameObject.SetActive(false);
        //distractorStims.ToggleVisibility(false);
        //GameObject.Find("Slider").SetActive(false);
    }

    void loadVariables()
    {
        /*
        GameObject.Find("Stimuli").AddComponent<Canvas>();
        GameObject.Find("Stimuli").AddComponent<GraphicRaycaster>();
        
        */
        //clickMarker = GameObject.Find("ClickMarker");
        //startButton = GameObject.Find("StartButton");
        //startButton.GetComponent<Button>().onClick.AddListener(StartClick);
        //slider = GameObject.Find("Slider").GetComponent<Slider>();
        //sliderInitPosition = slider.gameObject.transform.position;
        /*
        GameObject startButtonCanvas = GameObject.Find("StartButtonCanvas");
        Transform parent = startButtonCanvas.GetComponent<Transform>();
        startButtonCanvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceCamera;
        startButtonCanvas.GetComponent<Canvas>().worldCamera = GameObject.Find("VisualSearch_Camera").GetComponent<Camera>();
        testButton = DefineStartButton(parent);

        var newButton = DefaultControls.CreateButton(new DefaultControls.Resources());
        newButton.transform.SetParent(parent);
        */
        disableAllGameobjects();
    }
    
    public static Texture2D LoadPNG(string filePath)
    {

        Texture2D tex = null;
        byte[] fileData;

        if (File.Exists(filePath))
        {
            fileData = File.ReadAllBytes(filePath);
            tex = new Texture2D(2, 2);
            tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
        }
        return tex;
    }
    public Material CreateSkybox(string filePath)
    {
        Texture2D tex = null;
        Material materialSkybox = new Material(Shader.Find("Skybox/6 Sided"));

        tex = LoadPNG(filePath); // load the texture from a PNG -> Texture2D

        //Set the textures of the skybox to that of the PNG
        materialSkybox.SetTexture("_FrontTex", tex);
        materialSkybox.SetTexture("_BackTex", tex);
        materialSkybox.SetTexture("_LeftTex", tex);
        materialSkybox.SetTexture("_RightTex", tex);
        materialSkybox.SetTexture("_UpTex", tex);
        materialSkybox.SetTexture("_DownTex", tex);

        return materialSkybox;
    }

}
