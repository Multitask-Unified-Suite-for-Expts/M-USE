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

    private StimGroup targetStims;
    private StimGroup distractorStims;

    private string targetName;
    private Vector3 targetLocation;
    private List<string> distractorName;
    private List<Vector3> distractorLocations;

    private int random = 0;
    private Button button;
    private USE_Button testButton;

    public float 
        DisplayStimsDuration = 5f, 
        TrialEndDuration = 5f;

    // game obeject variables
    private GameObject trialStim, clickMarker;
    private GameObject startButton;
    private GameObject[] totalObjects;
    private GameObject[] currentObjects;
    public GameObject YellowHaloPrefab;
    public GameObject GrayHaloPrefab;
    //public Canvas canvas;
    private int num_distractors = 0;
    private int response;
    private bool correct;
    private GameObject selected;
    VisualSearch_StimDef selectedSD = null;

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
    public string MaterialFilePath;

    //private StimGroup externalStimsA, externalStimsB, externalStimsC;
    private StimGroup TargetStims, DistractorStims;
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
    private bool pressed;

    public override void DefineControlLevel()
    {
        State InitTrial = new State("InitTrial");
        State StartButton = new State("StartButton");
        State SearchDisplay = new State("SearchDisplay");
        State SelectionFeedback = new State("SelectionFeedback");
        State TokenFeedback = new State("TokenFeedback");
        State TrialEnd = new State("TrialEnd");

        Text commandText = null;

        SelectionHandler<VisualSearch_StimDef> mouseHandler = new SelectionHandler<VisualSearch_StimDef>();

        AddActiveStates(new List<State> {InitTrial, StartButton, SearchDisplay, SelectionFeedback, TokenFeedback, TrialEnd});

        AddInitializationMethod(() =>
        {           
            if (!variablesLoaded)
            {
                variablesLoaded = true;
                loadVariables();
                response = -1;
            }
            /*
            var newButton = DefaultControls.CreateButton(new DefaultControls.Resources());
            button = newButton.GetComponent<Button>();
            button.transform.SetParent(GameObject.Find("Stimuli").GetComponent<Transform>());
            button.gameObject.transform.position = new Vector3 (0, 0, 0);
            button.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            button.interactable = true;
            button.onClick.AddListener(ClickEvent);
            */
            
        });

        SetupTrial.SpecifyTermination(() => true, StartButton);
        
        // define StartButton state
        StartButton.AddInitializationMethod(() =>
        {
            //EventCodeManager.SendCodeImmediate(300);
            startButton.SetActive(true);
            RenderSettings.skybox = CreateSkybox(MaterialFilePath + "\\Blank.png");
            //ResetRelativeStartTime();
            Debug.Log("Current Block Context: " + CurrentTrialDef.ContextName);
            //testButton.ToggleVisibility(true);
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
                        //EventCodeManager.SendCodeImmediate(TaskEventCodes["StartButtonSelected"]);
                        // Set the background texture to that of specified context
                        //RenderSettings.skybox = CreateSkybox(MaterialFilePath + "\\" + CurrentTrialDef.ContextName + ".png");
                    }
                }
            }
        });

        StartButton.SpecifyTermination(() => response == 0, SearchDisplay);

        MouseTracker.AddSelectionHandler(mouseHandler, SearchDisplay);
        SearchDisplay.AddInitializationMethod(() => 
        {
            //RenderSettings.skybox = CreateSkybox(MaterialFilePath + "\\" + CurrentTrialDef.ContextName + ".png");
            startButton.SetActive(false);
            correct = false;
            selected = null;
        });

        SearchDisplay.AddUpdateMethod(() =>
        {
            correct = false;
            GameObject clicked = GetClickedObj();
            if (!clicked) return;
            StimDefPointer sdPointer = clicked.GetComponent<StimDefPointer>();
            if (!sdPointer) return;

            response = 1; //selected an object

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
        SearchDisplay.SpecifyTermination(() => mouseHandler.SelectedStimDef != null, SelectionFeedback, () => {
            //testButton.pressed = false;
            selected = mouseHandler.SelectedGameObject;
            selectedSD = mouseHandler.SelectedStimDef;
            correct = selectedSD.IsTarget;
        });
        //searchDisplay.SpecifyTermination(() => selected!=null, selectionFeedback);

        GameObject halo = null;
        SelectionFeedback.AddInitializationMethod(() =>
        {
            if (!selected) return;
            if (correct) HaloFBController.ShowPositive(selected);
            else HaloFBController.ShowNegative(selected);

        });
        SelectionFeedback.AddTimer(() => 1.0f, TokenFeedback);

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
        //tokenFeedback.SpecifyTermination(() => !correct, trialEnd);
        //tokenFeedback.AddTimer(0.5f, trialEnd);

        // Wait for some time at the end
        TrialEnd.AddInitializationMethod(() =>
        {
            //disableAllGameobjects();
            response = -1;
        });
        TrialEnd.AddTimer(0.5f, FinishTrial);
        //TrialEnd.AddTimer(() => CurrentTrialDef.trialEndDuration, FinishTrial);

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

    private USE_Button DefineStartButton(Transform parent){
        /*
        if(random == 1){
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
        return (testButton);
        //testButton.SetVisibilityOnOffStates(GetStateFromName("InitTrial"), GetStateFromName("SearchDisplay"));
        //random = 1;
    }

    void disableAllGameobjects()
    {
       // testButton.ToggleVisibility(false);
        clickMarker.SetActive(false);
        startButton.SetActive(false);
        //slider.gameObject.SetActive(false);
        //distractorStims.ToggleVisibility(false);
        //GameObject.Find("Slider").SetActive(false);
    }

    void loadVariables()
    {
        GameObject.Find("Stimuli").AddComponent<Canvas>();
        GameObject.Find("Stimuli").AddComponent<GraphicRaycaster>();
        Transform parent = GameObject.Find("Stimuli").GetComponent<Transform>();
        clickMarker = GameObject.Find("ClickMarker");
        startButton = GameObject.Find("StartButton");
        //slider = GameObject.Find("Slider").GetComponent<Slider>();
        //sliderInitPosition = slider.gameObject.transform.position;
        
        //testButton = DefineStartButton(parent);
        disableAllGameobjects();
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
    private void ClickEvent()
    {
        Debug.Log("hehehehe");
    }
}
