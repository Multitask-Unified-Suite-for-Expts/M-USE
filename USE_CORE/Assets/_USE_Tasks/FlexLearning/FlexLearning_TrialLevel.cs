using System.Collections.Generic;
using UnityEngine;
using USE_ExperimentTemplate;
using USE_States;
using UnityEngine.UI;
using USE_StimulusManagement;
using FlexLearning_Namespace;
using System;
using Random = UnityEngine.Random;
using USE_UI;
using USE_Settings;
using System.IO;
using System.Linq;
using ConfigDynamicUI;
using USE_ExperimentTemplate_Classes;
using USE_Utilities;

public class FlexLearning_TrialLevel : ControlLevel_Trial_Template
{
    public FlexLearning_TrialDef CurrentTrialDef => GetCurrentTrialDef<FlexLearning_TrialDef>();

    private StimGroup tStim;
    private GameObject startButton;
    private string targetName;
    private Vector3 targetLocation;

    //block end variables
    public List<int> runningAcc;
    public int MinTrials, MaxTrials;

    //configui variables
    [HideInInspector]
    public ConfigNumber minObjectTouchDuration, itiDuration, finalFbDuration, fbDuration, maxObjectTouchDuration, selectObjectDuration, tokenRevealDuration, tokenUpdateDuration, searchDisplayDelay;

    // game object variables
    private GameObject trialStim, clickMarker, selected;
    private GameObject[] totalObjects, currentObjects;
    private int response, num_distractors = 0, tokenBarComplete = 0;
    private bool correct;
    FlexLearning_StimDef selectedSD = null;

    // misc variables
    private Slider slider;
    private float value = 0.0f, sliderValueIncreaseAmount;
    private Ray mouseRay;
    private bool variablesLoaded;
    public string MaterialFilePath;

    //Player View Variables
    private PlayerViewPanel playerView;
    private Transform playerViewParent; // Helps set things onto the player view in the experimenter display
    public List<GameObject> playerViewTextList;
    public GameObject playerViewText;
    private Vector2 textLocation;
    private bool playerViewLoaded;

    //private StimGroup externalStimsA, externalStimsB, externalStimsC;
    private StimGroup TargetStims, DistractorStims;
    private int numDistractor = 0;
    private USE_Button testButton;
    private GameObject sbSprite;
    private bool randomizedLocations = false;

    public override void DefineControlLevel()
    {
        State initTrial = new State("InitTrial");
        State SearchDisplay = new State("SearchDisplay");
        State SelectionFeedback = new State("SelectionFeedback");
        State TokenFeedback = new State("TokenFeedback");
        State TrialEnd = new State("TrialEnd");
        State SearchDisplayDelay = new State("SearchDisplayDelay");
        State delay = new State("Delay");

        AddActiveStates(new List<State> { initTrial, SearchDisplay, SelectionFeedback, TokenFeedback, TrialEnd, delay, SearchDisplayDelay });

        // A state that just waits for some time
        State stateAfterDelay = null;
        float delayDuration = 0;
        delay.AddTimer(() => delayDuration, () => stateAfterDelay);

        Text commandText = null;

        SelectionHandler<FlexLearning_StimDef> mouseHandler = new SelectionHandler<FlexLearning_StimDef>();

        AddInitializationMethod(() =>
        {
            playerView = new PlayerViewPanel(); //GameObject.Find("PlayerViewCanvas").GetComponent<PlayerViewPanel>()
            playerViewText = new GameObject();
        });

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
            RenderSettings.skybox = CreateSkybox(MaterialFilePath + Path.DirectorySeparatorChar + CurrentTrialDef.ContextName + ".png");
            Debug.Log("FilePath: " + MaterialFilePath);
            TokenFBController
                .SetRevealTime(tokenRevealDuration.value)
                .SetUpdateTime(tokenUpdateDuration.value);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["TrlStart"]);
            startButton.SetActive(true);
            TokenFBController.enabled = false;

        });
        initTrial.SpecifyTermination(() => mouseHandler.SelectionMatches(startButton),
            SearchDisplayDelay, () =>
            {
                startButton.SetActive(false);
            });

        // Show the target/sample with some other distractors
        SearchDisplayDelay.AddTimer(() => searchDisplayDelay.value, delay, () =>
        {
            stateAfterDelay = SearchDisplay;
            TokenFBController.enabled = true;
            TokenFBController.SetTotalTokensNum(CurrentTrialDef.NumTokens);
            EventCodeManager.SendCodeImmediate(TaskEventCodes["StartButtonSelected"]); //CHECK THIS TIMING MIGHT BE OFF
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["StimOn"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["ContextOn"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["TokenBarReset"]);
        });
        // Wait for a click and provide feedback accordingly
        bool correct = false;
        GameObject selected = null;
        FlexLearning_StimDef selectedSD = null;
        MouseTracker.AddSelectionHandler(mouseHandler, SearchDisplay);
        SearchDisplay.AddInitializationMethod(() =>
        {
            selected = null;
            if (!playerViewLoaded)
            {
                /*
                //Create corresponding text on player view of experimenter display
                textLocation =
                    playerViewPosition(Camera.main.WorldToScreenPoint(targetStim.stimDefs[0].StimLocation),
                        playerViewParent);
                textLocation.y += 50;
                Vector2 textSize = new Vector2(200, 200);
                playerViewText = playerView.writeText("TARGET",
                    Color.red, textLocation, textSize, playerViewParent);
                playerViewText.GetComponent<RectTransform>().localScale = new Vector3(2, 2, 0);
                playerViewTextList.Add(playerViewText);
                playerViewLoaded = true;
                */
            }
        });

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
                runningAcc.Add(1);
            }
            else
            {
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["TouchDistractorStart"]);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["IncorrectResponse"]);
                runningAcc.Add(0);
            }
            string touchedObjectsNames = "";
            if (selected != null) touchedObjectsNames = selected.name;

            TrialSummaryString = "Trial Num: " + TrialCount_InTask + 1 +  "\nTouched Object Names: " +
            touchedObjectsNames;
        });

        SearchDisplay.AddTimer(() => selectObjectDuration.value, FinishTrial, () =>
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

        SelectionFeedback.AddTimer(() => fbDuration.value, TokenFeedback, () =>
        {
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["StimOff"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionVisualFbOff"]);
        });

        TokenFeedback.AddInitializationMethod(() =>
        {
            HaloFBController.Destroy();
            if (selectedSD.TokenUpdate == 0)
            {
                if (correct) AudioFBController.Play("Positive");
                else AudioFBController.Play("Negative");
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionAuditoryFbOn"]);
                return;
            }
            if (selectedSD.TokenUpdate > 0)
            {
                TokenFBController.AddTokens(selected, selectedSD.TokenUpdate);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["Rewarded"]);
            }

            else
            {
                TokenFBController.RemoveTokens(selected, -selectedSD.TokenUpdate);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["Unrewarded"]);
            }
        });
        TokenFeedback.SpecifyTermination(() => !TokenFBController.IsAnimating(), TrialEnd, () =>
        {
            foreach (GameObject txt in playerViewTextList)
            {
                txt.SetActive(false);
            }
            playerViewLoaded = false;
        });
        TrialEnd.AddTimer(() => itiDuration.value, FinishTrial, () =>
        {
            EventCodeManager.SendCodeImmediate(TaskEventCodes["TrlEnd"]);

        });

        TrialData.AddDatum("SelectedName", () => selected != null ? selected.name : null);
        TrialData.AddDatum("SelectedLocation", () => selectedSD?.StimLocation ?? null);
        TrialData.AddDatum("SelectionCorrect", () => correct ? 1 : 0);
        TrialData.AddDatum("NumDistractors", () => num_distractors);


        //this.AddTerminationSpecification(() => trialCount > numTrials, ()=> Debug.Log(trialCount + " " + numTrials));

    }

    protected override void DefineTrialStims()
    {
        //Define StimGroups consisting of StimDefs whose gameobjects will be loaded at TrialLevel_SetupTrial and 
        //destroyed at TrialLevel_Finish
        int temp = 0;
        //Debug.Log("CURRENT TRIAL DEF: " + CurrentTrialDef);
        //Debug.Log("trial count in block: " + TrialCount_InBlock);
        //Debug.Log("trial LENGTH: " + TrialDefs.Length);
        //Debug.Log("TRIAL STIM INDICES: " + CurrentTrialDef.TrialStimIndices[0]);
        tStim = new StimGroup("SearchStimuli", ExternalStims, CurrentTrialDef.TrialStimIndices);
        tStim.SetVisibilityOnOffStates(GetStateFromName("SearchDisplay"), GetStateFromName("TokenFeedback"));
        TrialStims.Add(tStim);
        for (int i = 0; i < CurrentTrialDef.TrialStimIndices.Length; i++)
        {
            FlexLearning_StimDef sd = (FlexLearning_StimDef)tStim.stimDefs[i];
            //sd.StimTrialRewardMag = ChooseTokenReward(CurrentTrialDef.TrialStimTokenReward);
            //if (sd.StimTrialRewardMag > 0) sd.IsTarget = true; //CHECK THIS IMPLEMENTATION!!!
            //else sd.IsTarget = false;

        }

        randomizedLocations = CurrentTrialDef.RandomizedLocations;

        if (randomizedLocations)
        {
            int[] positionIndexArray = Enumerable.Range(0, CurrentTrialDef.TrialStimIndices.Length).ToArray();
            System.Random random = new System.Random();
            positionIndexArray = positionIndexArray.OrderBy(x => random.Next()).ToArray();

            for (int i = 0; i < CurrentTrialDef.TrialStimIndices.Length; i++)
            {
                tStim.stimDefs[i].StimLocation = CurrentTrialDef.TrialStimLocations.ElementAt(positionIndexArray[i]);
            }
        }
        else
        {
            tStim.SetLocations(CurrentTrialDef.TrialStimLocations);
        }
    }
    void loadVariables()
    {
        Texture2D buttonTex = LoadPNG(MaterialFilePath + "\\StartButtonImage.png");
        startButton = CreateStartButton(buttonTex, new Rect(new Vector2(0, 0), new Vector2(1, 1)));

        playerViewParent = GameObject.Find("MainCameraCopy").transform; // sets parent for any playerView elements on experimenter display

        //config UI variables
        minObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("minObjectTouchDuration");
        maxObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("maxObjectTouchDuration");
        itiDuration = ConfigUiVariables.get<ConfigNumber>("itiDuration");
        searchDisplayDelay = ConfigUiVariables.get<ConfigNumber>("searchDisplayDelay");
        selectObjectDuration = ConfigUiVariables.get<ConfigNumber>("selectObjectDuration");
        finalFbDuration = ConfigUiVariables.get<ConfigNumber>("finalFbDuration");
        fbDuration = ConfigUiVariables.get<ConfigNumber>("fbDuration");
        tokenRevealDuration = ConfigUiVariables.get<ConfigNumber>("tokenRevealDuration");
        tokenUpdateDuration = ConfigUiVariables.get<ConfigNumber>("tokenRevealDuration");
        variablesLoaded = true;
        //disableAllGameobjects();
    }
    protected override bool CheckBlockEnd()
    {
        TaskLevelTemplate_Methods TaskLevel_Methods = new TaskLevelTemplate_Methods();
        return (TaskLevel_Methods.CheckBlockEnd(CurrentTrialDef.BlockEndType, runningAcc,
            CurrentTrialDef.BlockEndThreshold, CurrentTrialDef.BlockEndWindow, MinTrials,
            TrialDefs.Length) || TrialCount_InBlock == MaxTrials);
        
    }
    private GameObject CreateStartButton(Texture2D tex, Rect rect)
    {
        Vector3 buttonPosition = Vector3.zero;
        Vector3 buttonScale = Vector3.zero;
        string TaskName = "FlexLearning";
        if (SessionSettings.SettingClassExists(TaskName + "_TaskSettings"))
        {
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonPosition"))
                buttonPosition = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonPosition");
            else Debug.Log("[ERROR] Start Button Position settings not defined in the TaskDef");
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonScale"))
                buttonScale = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonScale");
            else Debug.Log("[ERROR] Start Button Position settings not defined in the TaskDef");
        }
        else
        {
            Debug.Log("[ERROR] TaskDef is not in config folder");
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
    private Vector2 playerViewPosition(Vector3 position, Transform playerViewParent)
    {
        Vector2 pvPosition = new Vector2((position[0] / Screen.width) * playerViewParent.GetComponent<RectTransform>().sizeDelta.x, (position[1] / Screen.height) * playerViewParent.GetComponent<RectTransform>().sizeDelta.y);
        return pvPosition;
    }
    public int ChooseTokenReward(TokenReward[] tokenRewards)
    {
        float totalProbability = 0;
        for (int i = 0; i < tokenRewards.Length; i++)
        {
            totalProbability += tokenRewards[i].Probability;
        }

        if (Math.Abs(totalProbability - 1) > 0.001)
            Debug.LogError("Sum of token reward probabilities on this trial is " + totalProbability + ", probabilities will be scaled to sum to 1.");

        float randomNumber = UnityEngine.Random.Range(0, totalProbability);

        TokenReward selectedReward = tokenRewards[0];
        float curProbSum = 0;
        foreach (TokenReward tr in tokenRewards)
        {
            curProbSum += tr.Probability;
            if (curProbSum >= randomNumber)
            {
                selectedReward = tr;
                break;
            }
        }
        return selectedReward.NumTokens;
    }
}
