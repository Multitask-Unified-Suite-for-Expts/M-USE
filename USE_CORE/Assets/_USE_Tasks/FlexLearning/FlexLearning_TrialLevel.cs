using System.Collections.Generic;
using UnityEngine;
using USE_States;
using USE_StimulusManagement;
using FlexLearning_Namespace;
using USE_ExperimentTemplate_Trial;
using Random = UnityEngine.Random;
using USE_UI;
using USE_Settings;
using System.IO;
using System.Linq;
using ConfigDynamicUI;
using UnityEngine.UI;
using USE_ExperimentTemplate_Task;
using System;
using ConfigParsing;

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
    private GameObject trialStim, selected;
    private GameObject[] totalObjects, currentObjects;
    private int response, num_distractors = 0;
    public int numReward = 0;
    private bool correct;
    FlexLearning_StimDef selectedSD = null;
    int touchedObjectsCodes = -1;

    // misc variables
    private bool variablesLoaded;
    public string MaterialFilePath;
    public int NumTokenBar;
    public int NumInitialTokens;
    public Vector3 buttonPosition, buttonScale;
    public string shadowType, context; 
    
    //Player View Variables
    private PlayerViewPanel playerView;
    private Transform playerViewParent; // Helps set things onto the player view in the experimenter display
    public List<GameObject> playerViewTextList;
    public GameObject playerViewText;
    private Vector2 textLocation;
    private bool playerViewLoaded;
    public bool stimFacingCamera;
    private bool randomizedLocations = false;
    public bool usingRewardPump;
    public int numTokenBarFull;
    public int totalTokensCollected;
    private Vector3 location;

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

        Add_ControlLevel_InitializationMethod(() =>
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
            context = CurrentTrialDef.ContextName;
            RenderSettings.skybox = CreateSkybox(MaterialFilePath + Path.DirectorySeparatorChar + CurrentTrialDef.ContextName + ".png");
            TokenFBController
                .SetRevealTime(tokenRevealDuration.value)
                .SetUpdateTime(tokenUpdateDuration.value);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["TrlStart"]);
            startButton.SetActive(true);
            TokenFBController.enabled = false;
            numTokenBarFull = TokenFBController.GetNumTokenBarFull();
            TrialSummaryString = "Trial Num: " + (TrialCount_InTask + 1) + "\nTouched Object Codes: " + touchedObjectsCodes + "\nToken Bar Value: " +  TokenFBController.GetTokenBarValue();
        });
        initTrial.SpecifyTermination(() => mouseHandler.SelectionMatches(startButton),
            SearchDisplayDelay, () =>
            {
                Input.ResetInputAxes();
                startButton.SetActive(false);
                totalTokensCollected = TokenFBController.GetTokenBarValue() +
                                       (TokenFBController.GetNumTokenBarFull() * CurrentTrialDef.NumTokenBar);
            });

        // Show the target/sample with some other distractors
        SearchDisplayDelay.AddTimer(() => searchDisplayDelay.value, delay, () =>
        {
            stateAfterDelay = SearchDisplay;
            TokenFBController.enabled = true;
            TokenFBController.SetTotalTokensNum(NumTokenBar);
            Debug.Log("TokenBarValue: " + TokenFBController.GetTokenBarValue());
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
                
                //Create corresponding text on player view of experimenter display
                foreach (FlexLearning_StimDef sd in tStim.stimDefs)
                {
                    if (sd.IsTarget) location = sd.StimLocation;
                }
                textLocation =
                    playerViewPosition(Camera.main.WorldToScreenPoint(location), 
                        playerViewParent); // only really works if the Target is the first stim in the group
                textLocation.y += 50;
                Vector2 textSize = new Vector2(200, 200);
                playerViewText = playerView.writeText("TARGET",
                    Color.red, textLocation, textSize, playerViewParent);
                playerViewText.GetComponent<RectTransform>().localScale = new Vector3(2, 2, 0);
                playerViewTextList.Add(playerViewText);
                playerViewLoaded = true;
                
            }
            //stim facing camera
            if (stimFacingCamera)
            {
                foreach (var stim in tStim.stimDefs)
                {
                    stim.StimGameObject.AddComponent<FaceCamera>();
                   // stim.StimGameObject.transform.rotation = Quaternion.Euler(0,0,0);
                }
            }
            SetShadowType();
        });

        SearchDisplay.SpecifyTermination(() => mouseHandler.SelectedStimDef != null, SelectionFeedback, () => {
            
            //testButton.pressed = false;
            selected = mouseHandler.SelectedGameObject;
            selectedSD = mouseHandler.SelectedStimDef;
            correct = selectedSD.IsTarget;
            if (correct)
            {
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["TouchTargetStart"]);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["CorrectResponse"]);
                runningAcc.Add(1);
                if (usingRewardPump)
                {
                    SyncBoxController.SendRewardPulses(CurrentTrialDef.NumPulses, CurrentTrialDef.PulseSize); //USE THIS LINE WHEN CONNECTED TO A SYNCBOX
                    SyncBoxController.SendRewardPulses(3, 500);
                    numReward++;
                }
            }
            else
            {
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["TouchDistractorStart"]);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["IncorrectResponse"]);
                runningAcc.Add(0);
            }
            if (selected != null) touchedObjectsCodes = selectedSD.StimCode;
        });

        SearchDisplay.AddTimer(() => selectObjectDuration.value, TrialEnd, () =>
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
            if (selectedSD.StimTrialRewardMag > 0)
            {
                TokenFBController.AddTokens(selected, selectedSD.StimTrialRewardMag);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["Rewarded"]);
                AudioFBController.Play("Positive");
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionAuditoryFbOn"]);
            }

            else
            {
                AudioFBController.Play("Negative");
                TokenFBController.RemoveTokens(selected, -selectedSD.StimTrialRewardMag, Color.grey);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["Unrewarded"]);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionAuditoryFbOn"]);
            }
            TrialSummaryString = "Trial Num: " + (TrialCount_InTask + 1) + "\nTouched Object Codes: " + touchedObjectsCodes + "\nToken Bar Value: " +  TokenFBController.GetTokenBarValue();
        });
        TokenFeedback.SpecifyTermination(() => !TokenFBController.IsAnimating(), TrialEnd, () =>
        {
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["TrlEnd"]); //NOT SURE ABOUT THIS BUT TRIALEND IS THE ITI
            context = "itiImage";
            RenderSettings.skybox = CreateSkybox(MaterialFilePath + Path.DirectorySeparatorChar + context + ".png");
        });
        TrialEnd.AddTimer(() => itiDuration.value, FinishTrial, () =>
        {
            foreach (GameObject txt in playerViewTextList)
            {
                txt.SetActive(false);
            }
            playerViewLoaded = false;
        });
        // trial data
        TrialData.AddDatum("SelectedStimCode", () => selectedSD?.StimCode ?? null);
        TrialData.AddDatum("SelectedLocation", () => selectedSD?.StimLocation ?? null);
        TrialData.AddDatum("SelectionCorrect", () => correct ? 1 : 0);
        TrialData.AddDatum("NumRewardGiven", () => numReward);
        TrialData.AddDatum("TotalTokensCollected", ()=> totalTokensCollected);
        // frame data
        FrameData.AddDatum("TouchPosition", () => InputBroker.mousePosition);
        FrameData.AddDatum("StartButton", () => startButton.activeSelf);
        FrameData.AddDatum("TrialStimuliShown", () => tStim.IsActive);
        FrameData.AddDatum("Context", () => context);


        //this.AddTerminationSpecification(() => trialCount > numTrials, ()=> Debug.Log(trialCount + " " + numTrials));

    }

    protected override void DefineTrialStims()
    {
        //Define StimGroups consisting of StimDefs whose gameobjects will be loaded at TrialLevel_SetupTrial and 
        //destroyed at TrialLevel_Finish
        int temp = 0;
        tStim = new StimGroup("SearchStimuli", ExternalStims, CurrentTrialDef.TrialStimIndices);
        tStim.SetVisibilityOnOffStates(GetStateFromName("SearchDisplay"), GetStateFromName("TokenFeedback"));
        TrialStims.Add(tStim);
        for (int i = 0; i < CurrentTrialDef.TrialStimIndices.Length; i++)
        {
            FlexLearning_StimDef sd = (FlexLearning_StimDef)tStim.stimDefs[i];
            sd.StimTrialRewardMag = ChooseTokenReward(CurrentTrialDef.TrialStimTokenReward[i]);
            if (sd.StimTrialRewardMag > 0) sd.IsTarget = true; //CHECK THIS IMPLEMENTATION!!! only works if the target stim has a non-zero, positive reward
            else sd.IsTarget = false;
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
        Texture2D buttonTex = LoadPNG(MaterialFilePath + Path.DirectorySeparatorChar + "StartButtonImage.png");
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
            TrialDefs.Count) || TrialCount_InBlock == MaxTrials);
        
    }
    private GameObject CreateStartButton(Texture2D tex, Rect rect)
    {
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
    private void SetShadowType()
    {
        //User options are None, Soft, Hard
        switch (shadowType)
        {
            case "None":
                GameObject.Find("Directional Light").GetComponent<Light>().shadows = LightShadows.None;
                GameObject.Find("FlexLearning_DirectionalLight").GetComponent<Light>().shadows = LightShadows.None;
                break;
            case "Soft":
                GameObject.Find("Directional Light").GetComponent<Light>().shadows = LightShadows.Soft;
                GameObject.Find("FlexLearning_DirectionalLight").GetComponent<Light>().shadows = LightShadows.Soft;
                break;
            case "Hard":
                GameObject.Find("Directional Light").GetComponent<Light>().shadows = LightShadows.Hard;
                GameObject.Find("FlexLearning_DirectionalLight").GetComponent<Light>().shadows = LightShadows.Hard;
                break;
            default:
                Debug.Log("User did not Input None, Soft, or Hard for the Shadow Type");
                break;
        }
    }
}
