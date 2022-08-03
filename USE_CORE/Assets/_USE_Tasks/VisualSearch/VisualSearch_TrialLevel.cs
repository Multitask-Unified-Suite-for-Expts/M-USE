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
using ConfigDynamicUI;

public class VisualSearch_TrialLevel : ControlLevel_Trial_Template
{
    public VisualSearch_TrialDef CurrentTrialDef => GetCurrentTrialDef<VisualSearch_TrialDef>();

    private StimGroup targetStims, distractorStims;
    private GameObject startButton;

    private string targetName;
    private Vector3 targetLocation;
    private List<string> distractorName;
    private List<Vector3> distractorLocations;

    //configui variables
    [HideInInspector]
    public ConfigNumber minObjectTouchDuration, itiDuration, finalFbDuration, fbDuration, maxObjectTouchDuration, selectObjectDuration, tokenRevealDuration, tokenUpdateDuration;
    /*
        public float 
            DisplayStimsDuration = 5f, 
            TrialEndDuration = 5f;
    */
    // game obeject variables
    private GameObject trialStim, clickMarker;
    //private GameObject startButton, startText;
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
    private GameObject sbSprite;

    public override void DefineControlLevel()
    {
        State initTrial = new State("InitTrial");
        State SearchDisplay = new State("SearchDisplay");
        State SelectionFeedback = new State("SelectionFeedback");
        State TokenFeedback = new State("TokenFeedback");
        State TrialEnd = new State("TrialEnd");

        Text commandText = null;
        AddActiveStates(new List<State> {initTrial, SearchDisplay, SelectionFeedback, TokenFeedback, TrialEnd});
        SelectionHandler<VisualSearch_StimDef> mouseHandler = new SelectionHandler<VisualSearch_StimDef>();
        SetupTrial.AddInitializationMethod(() =>
        {
            if (!variablesLoaded)
            {
                variablesLoaded = true;
                loadVariables();
            }          
        });

        SetupTrial.SpecifyTermination(() => true, initTrial);
        MouseTracker.AddSelectionHandler(mouseHandler, initTrial);
        
        initTrial.AddInitializationMethod(() =>
        {
            RenderSettings.skybox = CreateSkybox(MaterialFilePath + "\\" + CurrentTrialDef.ContextName + ".png");
            Debug.Log("FilePath: " + MaterialFilePath);
            TokenFBController
                .SetRevealTime(tokenRevealDuration.value)
                .SetUpdateTime(tokenUpdateDuration.value);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["TrlStart"]);
            startButton.SetActive(true);
            TokenFBController.enabled = false;

        });
        initTrial.SpecifyTermination(() => mouseHandler.SelectionMatches(startButton),
            SearchDisplay, () => 
            {
               startButton.SetActive(false);
               TokenFBController.enabled = true;
               EventCodeManager.SendCodeImmediate(TaskEventCodes["StartButtonSelected"]); //CHECK THIS TIMING MIGHT BE OFF
               EventCodeManager.SendCodeNextFrame(TaskEventCodes["StimOn"]);
               EventCodeManager.SendCodeNextFrame(TaskEventCodes["ContextOn"]);
               EventCodeManager.SendCodeNextFrame(TaskEventCodes["TokenBarReset"]);
            });
        MouseTracker.AddSelectionHandler(mouseHandler, SearchDisplay);
        // Show the target/sample with some other distractors
        // Wait for a click and provide feedback accordingly
        bool correct = false;
        GameObject selected = null;
        VisualSearch_StimDef selectedSD = null;
        MouseTracker.AddSelectionHandler(mouseHandler, SearchDisplay);

        SearchDisplay.AddInitializationMethod(() => selected = null);
        
        SearchDisplay.SpecifyTermination(() => mouseHandler.SelectedStimDef != null, SelectionFeedback, () => {
            Debug.Log("SELECT: " + mouseHandler.SelectedStimDef.StimName);
            //testButton.pressed = false;
            selected = mouseHandler.SelectedGameObject;
            selectedSD = mouseHandler.SelectedStimDef;
            correct = selectedSD.IsTarget;
            if (correct)
            {       
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["TouchTargetStart"]);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["CorrectResponse"]);
                  
            }
            else
            {
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["TouchDistractorStart"]);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["IncorrectResponse"]);
            }
            string touchedObjectsNames = "";
            if (selected != null) touchedObjectsNames = selected.name;

            TrialSummaryString = "Trial Num: " + (TrialCount_InTask).ToString() + "\nTarget Name: " +
            targetName + "\nTouched Object Names: " +
            touchedObjectsNames;
        });

        SearchDisplay.AddTimer(() => selectObjectDuration.value, FinishTrial, ()=> 
        {
            if (mouseHandler.SelectedStimDef == null)   //means the player got timed out and didn't click on anything
            {
                Debug.Log("Timed out of selection state before making a choice");
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["NoChoice"]);
            }
        });

        GameObject halo = null;
        SelectionFeedback.AddInitializationMethod(() =>
        {
            if (!selected) return;
            else
            {
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionAuditoryFbOn"]);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionVisualFbOn"]);
            }
            if (correct)
            {
                HaloFBController.ShowPositive(selected);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["Rewarded"]);
            }
            else
            {
                HaloFBController.ShowNegative(selected);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["Unrewarded"]);
            }
        });

        SelectionFeedback.AddTimer(() => fbDuration.value, TokenFeedback,()=>
        {   
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["StimOff"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionVisualFbOff"]);
        });

        TokenFeedback.AddInitializationMethod(() =>
        {

            HaloFBController.Destroy();
            if (correct){
                AudioFBController.Play("Positive");
                TokenFBController.AddTokens(selected, 2);
                //slider.value += (float)0.25;
                value += (float)0.25;
            }
            else AudioFBController.Play("Negative");
        });
        TokenFeedback.SpecifyTermination(() => !TokenFBController.IsAnimating(), TrialEnd);
        TrialEnd.AddTimer(()=> itiDuration.value, FinishTrial, ()=> 
        {
            EventCodeManager.SendCodeImmediate(TaskEventCodes["TrlEnd"]);
        });
        
        TrialData.AddDatum("TargetName", () => targetName);
        TrialData.AddDatum("TargetLocation", () => targetLocation);
        TrialData.AddDatum("SelectedName", () => selected != null ? selected.name : null);
        TrialData.AddDatum("SelectedLocation", () => selectedSD?.StimLocation ?? null);
        TrialData.AddDatum("SelectionCorrect", () => correct ? 1 : 0);
        TrialData.AddDatum("NumDistractors", () => num_distractors);
        TrialData.AddDatum("DistractorNames", () => string.Join(", ", distractorName.ToArray()));
        TrialData.AddDatum("DistractorLocations", () => string.Join(", ", distractorLocations.ToArray()));
        
        
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
        if (CurrentTrialDef.DistractorStimsIndices.Length != 0)
        {
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
        startButton.SetActive(false);
    }

    void loadVariables()
    {
        Texture2D buttonTex = LoadPNG(MaterialFilePath + "\\StartButtonImage.png");
        startButton = CreateStartButton(buttonTex, new Rect(new Vector2(0,0), new Vector2(1,1)));

        //config UI variables
        minObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("minObjectTouchDuration");
        maxObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("maxObjectTouchDuration");
        itiDuration = ConfigUiVariables.get<ConfigNumber>("itiDuration");
        selectObjectDuration = ConfigUiVariables.get<ConfigNumber>("selectObjectDuration");
        finalFbDuration = ConfigUiVariables.get<ConfigNumber>("finalFbDuration");
        fbDuration = ConfigUiVariables.get<ConfigNumber>("fbDuration");
        tokenRevealDuration = ConfigUiVariables.get<ConfigNumber>("tokenRevealDuration");
        tokenUpdateDuration = ConfigUiVariables.get<ConfigNumber>("tokenRevealDuration");
        variablesLoaded = true;
        //disableAllGameobjects();
    }
    private GameObject CreateStartButton(Texture2D tex, Rect rect)
    {
        Vector3 buttonPosition = Vector3.zero;
        Vector3 buttonScale = Vector3.zero;
        string TaskName = "VisualSearch";
        if (SessionSettings.SettingClassExists(TaskName + "_TaskSettings"))
        {
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonPosition"))
                buttonPosition = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonPosition");
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonScale"))
                buttonScale = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonScale");
        }
        else
        {
            Debug.Log("[ERROR] Start Button Image settings not defined in the TaskDef");
        }

        GameObject startButton = new GameObject("StartButton");
        SpriteRenderer sr = startButton.AddComponent<SpriteRenderer>() as SpriteRenderer;
        sr.sprite = Sprite.Create(tex, new Rect(rect.x, rect.y, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
        startButton.AddComponent<BoxCollider>();
        startButton.transform.localScale = buttonScale;
        startButton.transform.position = buttonPosition;
        return startButton;
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
