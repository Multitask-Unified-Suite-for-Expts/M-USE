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
using UnityEngine.Serialization;

public class VisualSearch_TrialLevel : ControlLevel_Trial_Template
{
    public VisualSearch_TrialDef CurrentTrialDef => GetCurrentTrialDef<VisualSearch_TrialDef>();

    private StimGroup tStim;
    private GameObject startButton;
    //configui variables
    [HideInInspector]
    public ConfigNumber minObjectTouchDuration, itiDuration, finalFbDuration, fbDuration, maxObjectTouchDuration, selectObjectDuration, tokenRevealDuration, tokenUpdateDuration, searchDisplayDelay;
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
    private int response;
    private bool correct;
    private GameObject selected;
    VisualSearch_StimDef selectedSD = null;
    public int tokenBarComplete = 0;

    // misc variables
    private Slider slider;
    private float value = 0.0f;
    private float sliderValueIncreaseAmount;
    private Ray mouseRay;
    private bool variablesLoaded;
    public string MaterialFilePath;
    public Vector3 buttonPosition, buttonScale;
    [FormerlySerializedAs("TaskTokenNum")] public int NumTokenBar;
    public int NumInitialTokens;

    //Player View Variables
    private PlayerViewPanel playerView;
    private Transform playerViewParent; // Helps set things onto the player view in the experimenter display
    public List<GameObject> playerViewTextList;
    public GameObject playerViewText;
    private Vector2 textLocation;
    private bool playerViewLoaded;
    
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
        
        AddActiveStates(new List<State> {initTrial, SearchDisplay, SelectionFeedback, TokenFeedback, TrialEnd, delay, SearchDisplayDelay});
        
        // A state that just waits for some time
        State stateAfterDelay = null;
        float delayDuration = 0;
        delay.AddTimer(() => delayDuration, () => stateAfterDelay);
        
        Text commandText = null;
        
        SelectionHandler<VisualSearch_StimDef> mouseHandler = new SelectionHandler<VisualSearch_StimDef>();
        
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
            RenderSettings.skybox = CreateSkybox(MaterialFilePath + Path.DirectorySeparatorChar +  CurrentTrialDef.ContextName + ".png");
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
                EventCodeManager.SendCodeImmediate(TaskEventCodes["StartButtonSelected"]);
               startButton.SetActive(false);
            });
        
        // Show the target/sample with some other distractors
        SearchDisplayDelay.AddTimer(() => searchDisplayDelay.value, delay, () =>
        {
            stateAfterDelay = SearchDisplay;
            TokenFBController.enabled = true;
            TokenFBController.SetTotalTokensNum(NumTokenBar); 
            
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["StimOn"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["ContextOn"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["TokenBarReset"]);
        });
        // Wait for a click and provide feedback accordingly
        bool correct = false;
        GameObject selected = null;
        VisualSearch_StimDef selectedSD = null;
        MouseTracker.AddSelectionHandler(mouseHandler, SearchDisplay);
        SearchDisplay.AddInitializationMethod(() =>
        {
            selected = null;
            tStim.ToggleVisibility(true);
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
                  
            }
            else
            {
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["TouchDistractorStart"]);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["IncorrectResponse"]);
            }
            string touchedObjectsNames = "";
            if (selected != null) touchedObjectsNames = selected.name;

            TrialSummaryString = "Trial Num: " + TrialCount_InTask+1 
             + "\nTouched Object Names: " +
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
                TokenFBController.RemoveTokens(selected, -selectedSD.StimTrialRewardMag);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["Unrewarded"]);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionAuditoryFbOn"]);
            }
        });
        TokenFeedback.SpecifyTermination(() => !TokenFBController.IsAnimating(), TrialEnd, () =>
        {
            tStim.ToggleVisibility(false);
            foreach (GameObject txt in playerViewTextList)
            {
                txt.SetActive(false);
            }
            playerViewLoaded = false;
        });
        TrialEnd.AddTimer(()=> itiDuration.value, FinishTrial, ()=> 
        {
            EventCodeManager.SendCodeImmediate(TaskEventCodes["TrlEnd"]);
            
        });
        
        // trial data
        TrialData.AddDatum("SelectedName", () => selected != null ? selected.name : null);
        TrialData.AddDatum("SelectedLocation", () => selectedSD?.StimLocation ?? null);
        TrialData.AddDatum("SelectionCorrect", () => correct ? 1 : 0);
        
        // frame date
        FrameData.AddDatum("TouchPosition", () => InputBroker.mousePosition);
        FrameData.AddDatum("Touch", () => response);
        FrameData.AddDatum("StartButton", () => startButton.activeSelf);/*
        FrameData.AddDatum("TimingErrorFeedback", () => imageTimingError.activeSelf);
        FrameData.AddDatum("TokenBarFlashing", () => sliderHalo.activeSelf);
        FrameData.AddDatum("Slider", () => slider.gameObject.activeSelf);*/
        FrameData.AddDatum("TrialStimuliShown", () => tStim.IsActive);
        /*FrameData.AddDatum("TokenBarValue", () => slider.normalizedValue);
        FrameData.AddDatum("Context", () => contextName);
        FrameData.AddDatum("ContextActive", () => contextActive);*/
        
        
        //this.AddTerminationSpecification(() => trialCount > numTrials, ()=> Debug.Log(trialCount + " " + numTrials));
 
    }

    protected override void DefineTrialStims()
    {
        //Define StimGroups consisting of StimDefs whose gameobjects will be loaded at TrialLevel_SetupTrial and 
        //destroyed at TrialLevel_Finish
        int temp = 0;
        tStim = new StimGroup("SearchStimuli", ExternalStims, CurrentTrialDef.TrialStimIndices);
        TrialStims.Add(tStim);
        for (int i = 0; i < CurrentTrialDef.TrialStimIndices.Length; i++)
        {
            VisualSearch_StimDef sd = (VisualSearch_StimDef)tStim.stimDefs[i];
            sd.StimTrialRewardMag = ChooseTokenReward(CurrentTrialDef.TrialStimTokenReward[i]);
            if (sd.StimTrialRewardMag > 0) sd.IsTarget = true; //CHECK THIS IMPLEMENTATION!!!
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
        startButton = CreateStartButton(buttonTex, new Rect(new Vector2(0,0), new Vector2(1,1)));

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
    }
    private GameObject CreateStartButton(Texture2D tex, Rect rect)
    {
        GameObject startButton = new GameObject("StartButton");
        SpriteRenderer sr = startButton.AddComponent<SpriteRenderer>() as SpriteRenderer;
        sr.sprite = Sprite.Create(tex, new Rect(rect.x, rect.y, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
        startButton.AddComponent<BoxCollider>();
        startButton.transform.localScale = buttonScale;
        Debug.Log("button scale: " + buttonScale);
        startButton.transform.position = buttonPosition;
        Debug.Log("button position: " + buttonPosition);
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

}
