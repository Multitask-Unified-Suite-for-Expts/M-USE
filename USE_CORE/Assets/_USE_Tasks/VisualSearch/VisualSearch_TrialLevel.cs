using System;
using System.Collections.Generic;
using UnityEngine;
using USE_States;
using UnityEngine.UI;
using USE_StimulusManagement;
using VisualSearch_Namespace;
using USE_UI;
using USE_Settings;
using System.Linq;
using ConfigDynamicUI;
using USE_ExperimentTemplate_Trial;
using System.IO;
using ConfigParsing;
using UnityEngine.Serialization;

public class VisualSearch_TrialLevel : ControlLevel_Trial_Template
{
    public VisualSearch_TrialDef CurrentTrialDef => GetCurrentTrialDef<VisualSearch_TrialDef>();
    private StimGroup tStim;
    private GameObject StartButton;
    //configui variables
    [HideInInspector]
    public ConfigNumber minObjectTouchDuration, itiDuration, finalFbDuration, fbDuration, maxObjectTouchDuration, 
        selectObjectDuration, tokenRevealDuration, tokenUpdateDuration, searchDisplayDelay;

    // Stim Evaluation Variables
    private GameObject trialStim;
    private bool CorrectSelection = false;
    private GameObject selected = null;
    VisualSearch_StimDef selectedSD = null;

    // Config Loading Variables
    private bool configUIVariablesLoaded;
    public string MaterialFilePath;
    public Vector3 ButtonPosition, ButtonScale;
    public bool StimFacingCamera;
    public string ShadowType;
    private bool RandomizedLocations = false;
    
    //Player View Variables
    private PlayerViewPanel playerView;
    private Transform playerViewParent; // Helps set things onto the player view in the experimenter display
    private List<GameObject> playerViewTextList = new List<GameObject>();
    private GameObject playerViewText;
    private Vector2 textLocation;
    private bool playerViewLoaded;
    
    //Data Values
    private string ContextName = "";
    private bool RewardGiven = false;
    public int NumRewardGiven, NumTokenBarFull = 0;
    private int? SelectedStimCode = null;
    private string selectedStimName = null;
    private Vector3? SelectedStimLocation = null;
    private float? SearchDuration = null;
    public int TotalTokensCollected = 0;

    public override void DefineControlLevel()
    {
        State InitTrial = new State("InitTrial");
        State SearchDisplay = new State("SearchDisplay");
        State SelectionFeedback = new State("SelectionFeedback");
        State TokenFeedback = new State("TokenFeedback");
        State TrialEnd = new State("TrialEnd");
        State SearchDisplayDelay = new State("SearchDisplayDelay");
        State delay = new State("Delay");
        
        AddActiveStates(new List<State> {InitTrial, SearchDisplay, SelectionFeedback, TokenFeedback, TrialEnd, delay, SearchDisplayDelay});
        
        // A state that just waits for some time
        State stateAfterDelay = null;
        float delayDuration = 0;
        delay.AddTimer(() => delayDuration, () => stateAfterDelay);
        
        Text commandText = null;
        
        SelectionHandler<VisualSearch_StimDef> mouseHandler = new SelectionHandler<VisualSearch_StimDef>();
        
        Add_ControlLevel_InitializationMethod(() =>
        {
            playerView = new PlayerViewPanel(); //GameObject.Find("PlayerViewCanvas").GetComponent<PlayerViewPanel>()
            playerViewText = new GameObject();
            Texture2D buttonTex = LoadPNG(MaterialFilePath + Path.DirectorySeparatorChar + "StartButtonImage.png");
            StartButton = CreateStartButton(buttonTex, new Rect(new Vector2(0,0), new Vector2(1,1)));
            SetTrialSummaryString();
        });
        
        SetupTrial.AddInitializationMethod(() =>
        {
            if (!configUIVariablesLoaded)
            {
                configUIVariablesLoaded = true;
                LoadConfigUIVariables();
            }          
        });

        SetupTrial.SpecifyTermination(() => true, InitTrial);
        MouseTracker.AddSelectionHandler(mouseHandler, InitTrial);
        
        InitTrial.AddInitializationMethod(() =>
        {
            //Set the context for the upcoming trial with the Start Button visible
            ContextName = CurrentTrialDef.ContextName;
            RenderSettings.skybox = CreateSkybox(MaterialFilePath + Path.DirectorySeparatorChar +  ContextName + ".png");
            StartButton.SetActive(true);
            
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["TrlStart"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["ContextOn"]);
            
        });
        InitTrial.SpecifyTermination(() => mouseHandler.SelectionMatches(StartButton),
            SearchDisplayDelay, () => 
            { 
                // Turn off start button and set the token bar settings
                StartButton.SetActive(false);
                TokenFBController
                    .SetRevealTime(tokenRevealDuration.value)
                    .SetUpdateTime(tokenUpdateDuration.value);
                NumTokenBarFull = TokenFBController.GetNumTokenBarFull();
                TotalTokensCollected = TokenFBController.GetTokenBarValue() +
                                       (TokenFBController.GetNumTokenBarFull() * CurrentTrialDef.NumTokenBar);
                
                EventCodeManager.SendCodeImmediate(TaskEventCodes["StartButtonSelected"]);
                SetTrialSummaryString();
            });
        
        // Show the target/sample with some other distractors
        SearchDisplayDelay.AddTimer(() => searchDisplayDelay.value, SearchDisplay, () =>
        {
            TokenFBController.enabled = true;
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["StimOn"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["TokenBarVisible"]);
        });
        // Wait for a click and provide feedback accordingly
        MouseTracker.AddSelectionHandler(mouseHandler, SearchDisplay);
        SearchDisplay.AddInitializationMethod(() =>
        {
            tStim.ToggleVisibility(true);
            CreateTextOnExperimenterDisplay();
            if (StimFacingCamera)
            {
                foreach (var stim in tStim.stimDefs) stim.StimGameObject.AddComponent<FaceCamera>();
            }
            SetShadowType();
        });
        
        SearchDisplay.SpecifyTermination(() => mouseHandler.SelectedStimDef != null, SelectionFeedback, () => {
            
            selected = mouseHandler.SelectedGameObject;
            selectedSD = mouseHandler.SelectedStimDef;
            CorrectSelection = selectedSD.IsTarget;
            if (CorrectSelection)
            {       
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["TouchTargetStart"]);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["CorrectResponse"]);
            }
            else
            {
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["TouchDistractorStart"]);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["IncorrectResponse"]);
            }

            if (selected != null)
            {
                SelectedStimCode = selectedSD.StimCode;
                SelectedStimLocation = selectedSD.StimLocation;
            }
            SetTrialSummaryString();
        });

        SearchDisplay.AddTimer(() => selectObjectDuration.value, TrialEnd, ()=> 
        {
            if (mouseHandler.SelectedStimDef == null)   //means the player got timed out and didn't click on anything
            {
                Debug.Log("Timed out of selection state before making a choice");
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["NoChoice"]);
            }
        });

        SelectionFeedback.AddInitializationMethod(() =>
        {
            SearchDuration = SearchDisplay.TimingInfo.Duration;
            SetTrialSummaryString();
            if (!selected) return;
            else
            {
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionVisualFbOn"]);
            }
            if (CorrectSelection)
            {
                HaloFBController.ShowPositive(selected);
            }
            else
            {
                HaloFBController.ShowNegative(selected);
            }
        });

        SelectionFeedback.AddTimer(() => fbDuration.value, TokenFeedback,()=>
        {   
            HaloFBController.Destroy();
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["StimOff"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionVisualFbOff"]);
        });

        TokenFeedback.AddInitializationMethod(() =>
        {
            if (selectedSD.StimTrialRewardMag > 0)
            {
                AudioFBController.Play("Positive");
                TokenFBController.AddTokens(selected, selectedSD.StimTrialRewardMag);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionAuditoryFbOn"]);
                
            }
            else
            {
                AudioFBController.Play("Negative");
                TokenFBController.RemoveTokens(selected, -selectedSD.StimTrialRewardMag);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionAuditoryFbOn"]);
            }
            
        });
        TokenFeedback.SpecifyTermination(() => !TokenFBController.IsAnimating(), TrialEnd, () =>
        {
            // Remove the Stimuli, Context, and Token Bar from the Player View and move to neutral ITI State
            DestroyTextOnExperimenterDisplay();
            tStim.ToggleVisibility(false);
            TokenFBController.enabled = false;
            ContextName = "itiImage";
            RenderSettings.skybox = CreateSkybox(MaterialFilePath + Path.DirectorySeparatorChar + ContextName + ".png");
            if (TokenFBController.GetAnimationPhase() == "Flashing")
            {
                NumTokenBarFull++;
                if (SyncBoxController != null)
                {
                    SyncBoxController.SendRewardPulses(CurrentTrialDef.NumPulses, CurrentTrialDef.PulseSize);
                    EventCodeManager.SendCodeImmediate(TaskEventCodes["Fluid1Onset"]);
                    NumRewardGiven++;
                    RewardGiven = true;
                }
            }
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["TrlEnd"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["ContextOff"]);
            SetTrialSummaryString();
        });
        TrialEnd.AddTimer(() => itiDuration.value, FinishTrial, () =>
        {
            ResetDataTrackingVariables();
        });
        FinishTrial.AddInitializationMethod(() =>
        {
            //Remove any remaining items on player view
            DestroyTextOnExperimenterDisplay();
        });
        //---------------------------------ADD FRAME AND TRIAL DATA TO LOG FILES---------------------------------------
        AssignTrialData();
        AssignFrameData();
    }

    protected override void DefineTrialStims()
    {
        //Define StimGroups consisting of StimDefs whose gameobjects will be loaded at TrialLevel_SetupTrial and 
        //destroyed at TrialLevel_Finish
        tStim = new StimGroup("SearchStimuli", ExternalStims, CurrentTrialDef.TrialStimIndices);
        TrialStims.Add(tStim);
        for (int i = 0; i < CurrentTrialDef.TrialStimIndices.Length; i++)
        {
            VisualSearch_StimDef sd = (VisualSearch_StimDef)tStim.stimDefs[i];
            sd.StimTrialRewardMag = ChooseTokenReward(CurrentTrialDef.TrialStimTokenReward[i]);
            if (sd.StimTrialRewardMag > 0) sd.IsTarget = true; //CHECK THIS IMPLEMENTATION!!!
            else sd.IsTarget = false;
        }
        
        RandomizedLocations = CurrentTrialDef.RandomizedLocations; 

        if (RandomizedLocations)
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
    void LoadConfigUIVariables()
    {
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
        configUIVariablesLoaded = true;
    }
    private GameObject CreateStartButton(Texture2D tex, Rect rect)
    {
        GameObject startButton = new GameObject("StartButton");
        SpriteRenderer sr = startButton.AddComponent<SpriteRenderer>() as SpriteRenderer;
        sr.sprite = Sprite.Create(tex, new Rect(rect.x, rect.y, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
        startButton.AddComponent<BoxCollider>();
        startButton.transform.localScale = ButtonScale;
        startButton.transform.position = ButtonPosition;
        return startButton;
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
        switch (ShadowType)
        {
            case "None":
                GameObject.Find("Directional Light").GetComponent<Light>().shadows = LightShadows.None;
                GameObject.Find("VisualSearch_DirectionalLight").GetComponent<Light>().shadows = LightShadows.None;
                break;
            case "Soft":
                GameObject.Find("Directional Light").GetComponent<Light>().shadows = LightShadows.Soft;
                GameObject.Find("VisualSearch_DirectionalLight").GetComponent<Light>().shadows = LightShadows.Soft;
                break;
            case "Hard":
                GameObject.Find("Directional Light").GetComponent<Light>().shadows = LightShadows.Hard;
                GameObject.Find("VisualSearch_DirectionalLight").GetComponent<Light>().shadows = LightShadows.Hard;
                break;
            default:
                Debug.Log("User did not Input None, Soft, or Hard for the Shadow Type");
                break;
        }
    }
    void SetTrialSummaryString()
    {
        TrialSummaryString = "\n" +
                             "Trial Count in Block: " + (TrialCount_InBlock + 1) +
                             "\nTrial Count in Task: " + (TrialCount_InTask + 1) +
                             "\n" +
                             "\nSelected Object Code: " + SelectedStimCode +
                             "\nSelected Object Location: " + SelectedStimLocation +
                             "\nCorrect Selection?: " + CorrectSelection +
                             "\n" +
                             "\nSearch Duration: " + SearchDuration +
                             "\n" + 
                             "\nToken Bar Value: " + TokenFBController.GetTokenBarValue();
    }
    private void CreateTextOnExperimenterDisplay()
    {
        playerViewParent = GameObject.Find("MainCameraCopy").transform; // sets parent for any playerView elements on experimenter display
        if (!playerViewLoaded)
        {
            //Create corresponding text on player view of experimenter display
            foreach (VisualSearch_StimDef stim in tStim.stimDefs)
            {
                if (stim.IsTarget)
                {
                    textLocation =
                        playerViewPosition(Camera.main.WorldToScreenPoint(stim.StimLocation),
                            playerViewParent);
                    textLocation.y += 50;
                    Vector2 textSize = new Vector2(200, 200);
                    playerViewText = playerView.writeText("TARGET",
                        Color.red, textLocation, textSize, playerViewParent);
                    playerViewText.GetComponent<RectTransform>().localScale = new Vector3(2, 2, 0);
                    playerViewTextList.Add(playerViewText);
                    playerViewLoaded = true;
                }
            }
                
        }
    }
    private void DestroyTextOnExperimenterDisplay()
    {
        if (playerViewLoaded)
        {
            foreach (GameObject txt in playerViewTextList)
            {
                txt.SetActive(false);
            }
        }
        playerViewLoaded = false;
    }

    private void ResetDataTrackingVariables()
    {
        SelectedStimCode = null;
        SelectedStimLocation = null;
        SearchDuration = 0;
        CorrectSelection = false;
        RewardGiven = false;
    }
    private void AssignTrialData()
    {
        // All AddDatum commands for the Trial Data
        TrialData.AddDatum("SelectedStimCode", () => selectedSD?.StimCode ?? null);
        TrialData.AddDatum("SelectedLocation", () => selectedSD?.StimLocation ?? null);
        TrialData.AddDatum("CorrectSelection", () => CorrectSelection ? 1 : 0);
        TrialData.AddDatum("SearchDuration", ()=> SearchDuration);
        TrialData.AddDatum("RewardGiven", ()=> RewardGiven);
    }

    private void AssignFrameData()
    {
        // All AddDatum commmands from the Frame Data
        FrameData.AddDatum("MousePosition", () => InputBroker.mousePosition);
        FrameData.AddDatum("StartButtonVisibility", () => StartButton.activeSelf);
        FrameData.AddDatum("TrialStimVisibility", () => tStim.IsActive);
        FrameData.AddDatum("ContextName", () => ContextName);
        FrameData.AddDatum("TokenBarValue", ()=> TokenFBController.GetTokenBarValue());
    }
}
