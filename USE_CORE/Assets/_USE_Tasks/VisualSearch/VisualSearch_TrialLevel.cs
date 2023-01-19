using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using ConfigDynamicUI;
using USE_States;
using USE_StimulusManagement;
using USE_ExperimentTemplate_Trial;
using VisualSearch_Namespace;

public class VisualSearch_TrialLevel : ControlLevel_Trial_Template
{
    public VisualSearch_TrialDef CurrentTrialDef => GetCurrentTrialDef<VisualSearch_TrialDef>(); 
    public VisualSearch_TaskLevel CurrentTaskLevel => GetTaskLevel<VisualSearch_TaskLevel>();
    
    // Stimuli Variables
    private StimGroup tStim;
    private GameObject StartButton;
    private GameObject FBSquare;
    public Texture2D HeldTooShortTexture;
    public Texture2D HeldTooLongTexture;
    private Texture2D StartButtonTexture, FBSquareTexture;
    private bool Grating = false;
    private Sprite StartButtonSprite, HeldTooShortSprite, HeldTooLongSprite;
    private GameObject SquareGO;
    public float FBSquareDuration;
    
    // ConfigUI variables
    [HideInInspector]
    public ConfigNumber minObjectTouchDuration, itiDuration, fbDuration, maxObjectTouchDuration, 
        selectObjectDuration, tokenRevealDuration, tokenUpdateDuration, searchDisplayDelay, gratingSquareDuration;
    
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
    public int NumInitialTokens;
    
    //Player View Variables
    private PlayerViewPanel playerView;
    private Transform playerViewParent; // Helps set things onto the player view in the experimenter display
    private List<GameObject> playerViewTextList = new List<GameObject>();
    private GameObject playerViewText;
    private Vector2 textLocation;
    private bool playerViewLoaded;
    
    // Block Data Values
    private string ContextName = "";
    public int NumCorrect_InBlock;
    public List<float?> SearchDurationsList = new List<float?>();
    public int NumErrors_InBlock;
    public int NumRewardGiven_InBlock;
    public int NumTokenBarFull_InBlock;
    public int TotalTokensCollected_InBlock;
    public decimal Accuracy_InBlock;
    public decimal AverageSearchDuration_InBlock;
    public int TouchDurationError_InBlock;
    // Trial Data Values
    private int? SelectedStimCode = null;
    private string selectedStimName = null;
    private Vector3? SelectedStimLocation = null;
    private float? SearchDuration = null;
    private bool RewardGiven = false;
    private bool TouchDurationError = false;

    public override void DefineControlLevel()
    {
        State InitTrial = new State("InitTrial");
        State SearchDisplay = new State("SearchDisplay");
        State SelectionFeedback = new State("SelectionFeedback");
        State TokenFeedback = new State("TokenFeedback");
        State ITI = new State("TrialEnd");
        State SearchDisplayDelay = new State("SearchDisplayDelay");
        State Delay = new State("Delay");
        
        AddActiveStates(new List<State> {InitTrial, SearchDisplay, SelectionFeedback, TokenFeedback, ITI, Delay, SearchDisplayDelay});
        
        // A state that just waits for some time
        State stateAfterDelay = null;
        float delayDuration = 0;
        Delay.AddTimer(() => delayDuration, () => stateAfterDelay);
        
        Text commandText = null;
        
        SelectionHandler<VisualSearch_StimDef> mouseHandler = new SelectionHandler<VisualSearch_StimDef>();
        
        Add_ControlLevel_InitializationMethod(() =>
        {
            playerView = new PlayerViewPanel(); //GameObject.Find("PlayerViewCanvas").GetComponent<PlayerViewPanel>()
            playerViewText = new GameObject();
            CreateStartButton();
            CreateFBSquare();
        });
        
        SetupTrial.AddInitializationMethod(() =>
        {
            if (!configUIVariablesLoaded) LoadConfigUIVariables();
            SetTrialSummaryString();
            CurrentTaskLevel.SetBlockSummaryString();
        });

        SetupTrial.SpecifyTermination(() => true, InitTrial);
        MouseTracker.AddSelectionHandler(mouseHandler, InitTrial);
        
        InitTrial.AddInitializationMethod(() =>
        {
            //Set the context for the upcoming trial with the Start Button visible
            ContextName = CurrentTrialDef.ContextName;
            RenderSettings.skybox = CreateSkybox(MaterialFilePath + Path.DirectorySeparatorChar +  ContextName + ".png");
            StartButton.SetActive(true);
            mouseHandler.SetMinTouchDuration(minObjectTouchDuration.value);
            mouseHandler.SetMaxTouchDuration(maxObjectTouchDuration.value);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["TrlStart"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["ContextOn"]);
        });
        
        InitTrial.AddUpdateMethod(() =>
        {
            if (mouseHandler.GetHeldTooLong() || mouseHandler.GetHeldTooShort())
            {
                TouchDurationError = true;
                SetTrialSummaryString();
                TouchDurationErrorFeedback(mouseHandler, StartButton);
                CurrentTaskLevel.SetBlockSummaryString();
            }
        });
        InitTrial.SpecifyTermination(() => mouseHandler.SelectionMatches(StartButton),
            SearchDisplayDelay, () => 
            { 
                // Turn off start button and set the token bar settings
                StartButton.SetActive(false);
                TokenFBController
                    .SetRevealTime(tokenRevealDuration.value)
                    .SetUpdateTime(tokenUpdateDuration.value);
                NumTokenBarFull_InBlock = TokenFBController.GetNumTokenBarFull();
                TotalTokensCollected_InBlock = TokenFBController.GetTokenBarValue() +
                                       (TokenFBController.GetNumTokenBarFull() * CurrentTrialDef.NumTokenBar);
                EventCodeManager.SendCodeImmediate(TaskEventCodes["StartButtonSelected"]);
                
                // Set Experimenter Display Data Summary Strings
                CurrentTaskLevel.SetBlockSummaryString();
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
        SearchDisplay.AddUpdateMethod(() =>
        {
            if (mouseHandler.GetHeldTooLong() || mouseHandler.GetHeldTooShort())
            {
                TouchDurationError = true;
                FBSquare.SetActive(true);
                SetTrialSummaryString();
                TouchDurationErrorFeedback(mouseHandler, FBSquare);
                CurrentTaskLevel.SetBlockSummaryString();
            }
        });
        SearchDisplay.SpecifyTermination(() => mouseHandler.SelectedStimDef != null, SelectionFeedback, () => {
            
            selected = mouseHandler.SelectedGameObject;
            selectedSD = mouseHandler.SelectedStimDef;
            CorrectSelection = selectedSD.IsTarget;
            if (CorrectSelection)
            {       
                NumCorrect_InBlock++;
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["TouchTargetStart"]);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["CorrectResponse"]);
            }
            else
            {
                NumErrors_InBlock++;
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["TouchDistractorStart"]);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["IncorrectResponse"]);
            }

            if (selected != null)
            {
                SelectedStimCode = selectedSD.StimCode;
                SelectedStimLocation = selectedSD.StimLocation;
            }
            SetTrialSummaryString();
            Accuracy_InBlock = decimal.Round(decimal.Divide(NumCorrect_InBlock, (TrialCount_InBlock + 1)), 6);
        });

        SearchDisplay.AddTimer(() => selectObjectDuration.value, ITI, ()=> 
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
            SearchDurationsList.Add(SearchDuration);
            AverageSearchDuration_InBlock = decimal.Round((decimal)SearchDurationsList.Average(), 6);
            SetTrialSummaryString();
            if (!selected) return;
            else EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionVisualFbOn"]);
            
            if (CorrectSelection) HaloFBController.ShowPositive(selected);
            else HaloFBController.ShowNegative(selected);
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
        TokenFeedback.SpecifyTermination(() => !TokenFBController.IsAnimating(), ITI, () =>
        {
            if (TokenFBController.GetAnimationPhase() == "Flashing")
            {
                NumTokenBarFull_InBlock++;
                if (SyncBoxController != null)
                {
                    SyncBoxController.SendRewardPulses(CurrentTrialDef.NumPulses, CurrentTrialDef.PulseSize);
                    EventCodeManager.SendCodeImmediate(TaskEventCodes["Fluid1Onset"]);
                    NumRewardGiven_InBlock++;
                    RewardGiven = true;
                }
            }
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["TrlEnd"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["ContextOff"]);
            TotalTokensCollected_InBlock = TokenFBController.GetTokenBarValue() +
                                           (TokenFBController.GetNumTokenBarFull() * CurrentTrialDef.NumTokenBar);
            SetTrialSummaryString();
            CurrentTaskLevel.SetBlockSummaryString();
        });
        ITI.AddInitializationMethod(() =>
        {
            ContextName = "itiImage";
            RenderSettings.skybox = CreateSkybox(MaterialFilePath + Path.DirectorySeparatorChar + ContextName + ".png");
            // Remove the Stimuli, Context, and Token Bar from the Player View and move to neutral ITI State
            DestroyTextOnExperimenterDisplay();
            tStim.ToggleVisibility(false);
            TokenFBController.enabled = false;
        });
    
        ITI.AddTimer(() => itiDuration.value, FinishTrial, () =>
        {
            ResetDataTrackingVariables();
        });
        FinishTrial.AddInitializationMethod(() =>
        {
            //Remove any remaining items on player view
            DestroyTextOnExperimenterDisplay();
            TokenFBController.enabled = false;
            ResetDataTrackingVariables();
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
        fbDuration = ConfigUiVariables.get<ConfigNumber>("fbDuration");
        tokenRevealDuration = ConfigUiVariables.get<ConfigNumber>("tokenRevealDuration");
        tokenUpdateDuration = ConfigUiVariables.get<ConfigNumber>("tokenUpdateDuration");
        gratingSquareDuration = ConfigUiVariables.get<ConfigNumber>("buttonGratingDuration");
        //finalFbDuration = ConfigUiVariables.get<ConfigNumber>("finalFbDuration");
        configUIVariablesLoaded = true;
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
                             "\nTouch Duration Error?: " + TouchDurationError +
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
        TouchDurationError = false;
    }
    private void AssignTrialData()
    {
        // All AddDatum commands for the Trial Data
        TrialData.AddDatum("Context", ()=> CurrentTrialDef.ContextName);
        TrialData.AddDatum("SelectedStimCode", () => selectedSD?.StimCode ?? null);
        TrialData.AddDatum("SelectedLocation", () => selectedSD?.StimLocation ?? null);
        TrialData.AddDatum("CorrectSelection", () => CorrectSelection ? 1 : 0);
        TrialData.AddDatum("SearchDuration", ()=> SearchDuration);
        TrialData.AddDatum("RewardGiven", ()=> RewardGiven);
    }
    private void AssignFrameData()
    {
        // All AddDatum commmands from the Frame Data
        FrameData.AddDatum("ContextName", () => ContextName);
        FrameData.AddDatum("StartButtonVisibility", () => StartButton.activeSelf);
        FrameData.AddDatum("TrialStimVisibility", () => tStim.IsActive);
        FrameData.AddDatum("TokenBarVisibility", ()=> TokenFBController.isActiveAndEnabled);
    }
    private void TouchDurationErrorFeedback(SelectionHandler<VisualSearch_StimDef> MouseHandler, GameObject go)
    {
        AudioFBController.Play("Negative");
        if (MouseHandler.GetHeldTooShort())
            StartCoroutine(GratedSquareFlash(HeldTooShortTexture, go));
        else if (MouseHandler.GetHeldTooLong())
            StartCoroutine(GratedSquareFlash(HeldTooLongTexture, go));
        
        MouseHandler.SetHeldTooLong(false);
        MouseHandler.SetHeldTooShort(false);
        TouchDurationError = false;
        TouchDurationError_InBlock++;
    }
    IEnumerator GratedSquareFlash(Texture2D newTexture, GameObject square)
    {
        Grating = true;
        Color32 originalColor = square.GetComponent<Renderer>().material.color;
        Texture originalTexture = square.GetComponent<Renderer>().material.mainTexture;
        square.GetComponent<Renderer>().material.color = new Color32(224, 78, 92, 255);
        square.GetComponent<Renderer>().material.mainTexture = newTexture;
        yield return new WaitForSeconds(gratingSquareDuration.value);
        square.GetComponent<Renderer>().material.mainTexture = originalTexture;
        square.GetComponent<Renderer>().material.color = originalColor;
        Grating = false;
        if (square.name == "FBSquare") square.SetActive(false);
    }
    private GameObject CreateSquare(string name)
    {
        SquareGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        SquareGO.name = name;
        SquareGO.AddComponent<MeshRenderer>();
        SquareGO.AddComponent<Renderer>();
        SquareGO.GetComponent<Renderer>().material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        SquareGO.GetComponent<Renderer>().material.SetFloat("_SpecularHighlights",0f);
        return SquareGO;
    }
    
    private void CreateStartButton()
    {
        StartButtonTexture = LoadPNG(MaterialFilePath + Path.DirectorySeparatorChar + "StartButtonImage.png");
        StartButton = CreateSquare("StartButton");
        StartButton.GetComponent<Renderer>().material.mainTexture = StartButtonTexture;
        StartButton.transform.localScale = ButtonScale;
        StartButton.transform.position = ButtonPosition;
        StartButton.SetActive(false);
    }
    private void CreateFBSquare()
    {
        FBSquare = CreateSquare("FBSquare");
        FBSquare.GetComponent<Renderer>().material.mainTexture = FBSquareTexture;
        FBSquare.transform.localScale = new Vector3(10,10,10);
        FBSquare.transform.position = new Vector3(0,0,0);
        FBSquare.SetActive(false);
    }
}
