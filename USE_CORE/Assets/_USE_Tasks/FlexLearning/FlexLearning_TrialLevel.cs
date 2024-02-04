/*
MIT License

Copyright (c) 2023 Multitask - Unified - Suite -for-Expts

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files(the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/


using System.Collections.Generic;
using UnityEngine;
using USE_States;
using USE_StimulusManagement;
using FlexLearning_Namespace;
using USE_ExperimentTemplate_Trial;
using System.Linq;
using ConfigDynamicUI;
using USE_ExperimentTemplate_Task;
// #if (!UNITY_WEBGL)
// using static System.Windows.Forms.VisualStyles.VisualStyleElement;
// #endif  


public class FlexLearning_TrialLevel : ControlLevel_Trial_Template
{
    public FlexLearning_TrialDef CurrentTrialDef => GetCurrentTrialDef<FlexLearning_TrialDef>();
    public FlexLearning_TaskLevel CurrentTaskLevel => GetTaskLevel<FlexLearning_TaskLevel>();
    public FlexLearning_TaskDef currentTaskDef => GetTaskDef<FlexLearning_TaskDef>();

    public GameObject FL_CanvasGO;

    // Block End Variables
    public List<int> runningAcc;
    
    // Stimuli Variables
    private StimGroup searchStim;
    private GameObject StartButton;

    // ConfigUI Variables
    private bool configUIVariablesLoaded;
    [HideInInspector]
    public ConfigNumber minObjectTouchDuration, itiDuration, 
        fbDuration, maxObjectTouchDuration, selectObjectDuration, tokenRevealDuration, tokenUpdateDuration, tokenFlashingDuration, 
        searchDisplayDelay;

    private float tokenFbDuration;
    

    // Set in the Task Level
    [HideInInspector] public bool? TokensWithStimOn;

    
    // Stim Evaluation Variables
    private GameObject selectedGO = null;
    private bool CorrectSelection;
    FlexLearning_StimDef selectedSD = null;
    private bool choiceMade = false;
    
    
    
    //Player View Variables
    private PlayerViewPanel playerView;
    private GameObject playerViewParent; // Helps set things onto the player view in the experimenter display
    private GameObject playerViewText;
    private Vector2 textLocation;
    private bool playerViewLoaded;
   
    // Block Data Variables
    [HideInInspector] public string ContextName = "";
    [HideInInspector] public int NumCorrect_InBlock;
    [HideInInspector] public List<float?> SearchDurations_InBlock = new List<float?>();
    [HideInInspector] public int NumErrors_InBlock;
    [HideInInspector] public int NumTokenBarFull_InBlock;
    [HideInInspector] public int TotalTokensCollected_InBlock;
    [HideInInspector] public decimal Accuracy_InBlock;
   
    // Trial Data Variables
    private int? SelectedStimIndex = null;
    private Vector3? SelectedStimLocation = null;
    private float SearchDuration = 0;
    private bool RewardGiven = false;

    [HideInInspector] public int PreSearch_TouchFbErrorCount;


    public override void DefineControlLevel()
    {
        State InitTrial = new State("InitTrial");
        State SearchDisplay = new State("SearchDisplay");
        State SearchDisplayDelay = new State("SearchDisplayDelay");
        State SelectionFeedback = new State("SelectionFeedback");
        State TokenFeedback = new State("TokenFeedback");
        State ITI = new State("ITI");

        AddActiveStates(new List<State> { InitTrial, SearchDisplay, SelectionFeedback, TokenFeedback, ITI, SearchDisplayDelay });
        
        Add_ControlLevel_InitializationMethod(() =>
        {
            if(!Session.WebBuild)
            {
                playerView = gameObject.AddComponent<PlayerViewPanel>();
                playerViewParent = GameObject.Find("MainCameraCopy");     
            }
            
            // Initialize FB Controller Values
            HaloFBController.SetCircleHaloIntensity(1.5f);
            HaloFBController.SetCircleHaloIntensity(5);
        });
        
        SetupTrial.AddSpecificInitializationMethod(() =>
        {
            //Set the Stimuli Light/Shadow settings
            SetShadowType(currentTaskDef.ShadowType, "FlexLearning_DirectionalLight");
            if (currentTaskDef.StimFacingCamera)
                MakeStimFaceCamera();
            
            if (StartButton == null)
            {
                if (Session.SessionDef.IsHuman)
                {
                    StartButton = Session.HumanStartPanel.StartButtonGO;
                    Session.HumanStartPanel.SetVisibilityOnOffStates(InitTrial, InitTrial);
                }
                else
                {
                    StartButton = Session.USE_StartButton.CreateStartButton(FL_CanvasGO.GetComponent<Canvas>(), currentTaskDef.StartButtonPosition, currentTaskDef.StartButtonScale);
                    Session.USE_StartButton.SetVisibilityOnOffStates(InitTrial, InitTrial);
                }
            }

            if (!configUIVariablesLoaded)
                LoadConfigUIVariables();

            UpdateExperimenterDisplaySummaryStrings();
        });
        SetupTrial.SpecifyTermination(() => true, InitTrial);

        //INIT TRIAL STATE ----------------------------------------------------------------------------------------------
        var ShotgunHandler = Session.SelectionTracker.SetupSelectionHandler("trial", "TouchShotgun", Session.MouseTracker, InitTrial, SearchDisplay);
        TouchFBController.EnableTouchFeedback(ShotgunHandler, currentTaskDef.TouchFeedbackDuration, currentTaskDef.StartButtonScale *10, FL_CanvasGO, true);
        InitTrial.AddSpecificInitializationMethod(() =>
        {
            if (Session.SessionDef.MacMainDisplayBuild & !Application.isEditor) //adj text positions if running build with mac as main display
                TokenFBController.AdjustTokenBarSizing(200);

            TokenFBController.SetRevealTime(tokenRevealDuration.value);
            TokenFBController.SetUpdateTime(tokenUpdateDuration.value);
            TokenFBController.SetFlashingTime(tokenFlashingDuration.value);

            if (ShotgunHandler.AllSelections.Count > 0)
                ShotgunHandler.ClearSelections();
            ShotgunHandler.MinDuration = minObjectTouchDuration.value;
            ShotgunHandler.MaxDuration = maxObjectTouchDuration.value;
        });
        InitTrial.SpecifyTermination(() => ShotgunHandler.LastSuccessfulSelectionMatchesStartButton(), SearchDisplayDelay);

        // Provide delay following start button selection and before stimuli onset
        SearchDisplayDelay.AddTimer(() => searchDisplayDelay.value, SearchDisplay);
        
        // SEARCH DISPLAY STATE ----------------------------------------------------------------------------------------
        SearchDisplay.AddSpecificInitializationMethod(() =>
        {
            Input.ResetInputAxes(); //reset input in case they holding down
            TokenFBController.enabled = true;

            if (!Session.WebBuild)
                ActivateChildren(playerViewParent);

            Session.EventCodeManager.AddToFrameEventCodeBuffer("TokenBarVisible");
            
            if (ShotgunHandler.AllSelections.Count > 0)
                ShotgunHandler.ClearSelections();

            PreSearch_TouchFbErrorCount = TouchFBController.ErrorCount;

            if (!Session.WebBuild)
                CreateTextOnExperimenterDisplay();
        });
        SearchDisplay.AddUpdateMethod(() =>
        {
            if(ShotgunHandler.SuccessfulSelections.Count > 0)
            {
                selectedGO = ShotgunHandler.LastSuccessfulSelection.SelectedGameObject;
                selectedSD = selectedGO?.GetComponent<StimDefPointer>()?.GetStimDef<FlexLearning_StimDef>();
                ShotgunHandler.ClearSelections();
                if(selectedSD != null)
                    choiceMade = true;
            }
        });
        SearchDisplay.SpecifyTermination(() => choiceMade, SelectionFeedback, () =>
        {

            CorrectSelection = selectedSD.IsTarget;
            if (CorrectSelection)
            {       
                NumCorrect_InBlock++;
                CurrentTaskLevel.NumCorrect_InTask++;
                runningAcc.Add(1);
                Session.EventCodeManager.AddToFrameEventCodeBuffer("CorrectResponse");
            }
            else
            {
                NumErrors_InBlock++;
                CurrentTaskLevel.NumErrors_InTask++;
                runningAcc.Add(0);
                Session.EventCodeManager.AddToFrameEventCodeBuffer("IncorrectResponse");
            }

        if (selectedGO != null)
        {
            SelectedStimIndex = selectedSD.StimIndex;
            SelectedStimLocation = selectedSD.StimLocation;
        }
        Accuracy_InBlock = decimal.Divide(NumCorrect_InBlock, (TrialCount_InBlock + 1));
        UpdateExperimenterDisplaySummaryStrings();
        });

        SearchDisplay.AddTimer(() => selectObjectDuration.value, ITI, () =>
        {
            Session.EventCodeManager.AddToFrameEventCodeBuffer("NoChoice");
            Session.EventCodeManager.SendRangeCode("CustomAbortTrial", AbortCodeDict["NoSelectionMade"]);
            AbortCode = 6;

            runningAcc.Add(0);

            CurrentTaskLevel.SearchDurations_InTask.Add(null);
            SearchDurations_InBlock.Add(null);
            SetTrialSummaryString();
        });
        
        // SELECTION FEEDBACK STATE ---------------------------------------------------------------------------------------   
        SelectionFeedback.AddSpecificInitializationMethod(() =>
        {
            SearchDuration = SearchDisplay.TimingInfo.Duration;
            SearchDurations_InBlock.Add(SearchDuration);
            CurrentTaskLevel.SearchDurations_InTask.Add(SearchDuration);

            SetTrialSummaryString();

            int? depth = Session.Using2DStim ? 50 : (int?)null;

            if (CorrectSelection) 
                HaloFBController.ShowPositive(selectedGO, particleHaloActive:CurrentTrialDef.ParticleHaloActive, circleHaloActive:CurrentTrialDef.CircleHaloActive, depth: depth);
            else 
                HaloFBController.ShowNegative(selectedGO, particleHaloActive: CurrentTrialDef.ParticleHaloActive, circleHaloActive: CurrentTrialDef.CircleHaloActive, depth: depth);
        });

        SelectionFeedback.AddTimer(() => fbDuration.value, TokenFeedback, () =>
        {
            HaloFBController.DestroyAllHalos();
            choiceMade = false;
        });
       
        // TOKEN FEEDBACK STATE ------------------------------------------------------------------------------------------------
        TokenFeedback.AddSpecificInitializationMethod(() =>
        {
            if (!Session.WebBuild)
                DestroyTextOnExperimenterDisplay();

            if (selectedSD.StimTokenRewardMag > 0)
            {
                TokenFBController.AddTokens(selectedGO, selectedSD.StimTokenRewardMag);
                TotalTokensCollected_InBlock += selectedSD.StimTokenRewardMag;
                CurrentTaskLevel.TotalTokensCollected_InTask += selectedSD.StimTokenRewardMag;
            }
            else
            {
                TokenFBController.RemoveTokens(selectedGO, -selectedSD.StimTokenRewardMag);
                TotalTokensCollected_InBlock += selectedSD.StimTokenRewardMag;
                CurrentTaskLevel.TotalTokensCollected_InTask += selectedSD.StimTokenRewardMag;
            }
        });
        TokenFeedback.AddTimer(() => tokenFbDuration, ITI, () =>
        {
            if (TokenFBController.IsTokenBarFull())
            {
                NumTokenBarFull_InBlock++;
                CurrentTaskLevel.NumTokenBarFull_InTask++;

                if (Session.SyncBoxController != null)
                {
                    int NumPulses;
                    if (CurrentTrialDef.ProbablisticNumPulses != null)
                        NumPulses = chooseReward(CurrentTrialDef.ProbablisticNumPulses);
                    else
                        NumPulses = CurrentTrialDef.NumPulses;
                    Session.SyncBoxController.SendRewardPulses(NumPulses, CurrentTrialDef.PulseSize);
                    //SessionInfoPanel.UpdateSessionSummaryValues(("totalRewardPulses",CurrentTrialDef.NumPulses)); moved to syncbox class
                    CurrentTaskLevel.NumRewardPulses_InBlock += NumPulses;
                    CurrentTaskLevel.NumRewardPulses_InTask += NumPulses;
                    RewardGiven = true;
                }
            }
        });
        // ITI STATE ---------------------------------------------------------------------------------------------------
        ITI.AddSpecificInitializationMethod(() =>
        {
            if (currentTaskDef.NeutralITI)
            {
                ContextName = "NeutralITI";
                StartCoroutine(HandleSkybox(GetContextNestedFilePath(!string.IsNullOrEmpty(currentTaskDef.ContextExternalFilePath) ? currentTaskDef.ContextExternalFilePath : Session.SessionDef.ContextExternalFilePath, "NeutralITI")));
                Session.EventCodeManager.AddToFrameEventCodeBuffer("ContextOff");
            }
        });
        ITI.AddTimer(() => itiDuration.value, FinishTrial, () =>
        {
            UpdateExperimenterDisplaySummaryStrings();
        });

        //---------------------------------ADD FRAME AND TRIAL DATA TO LOG FILES---------------------------------------
        DefineTrialData();
        DefineFrameData();
    }


    //This method is for EventCodes and gets called automatically at end of SetupTrial:
    public override void AddToStimLists()
    {
        foreach (FlexLearning_StimDef stim in searchStim.stimDefs)
        {
            if (stim.IsTarget)
                Session.TargetObjects.Add(stim.StimGameObject);
            else
                Session.DistractorObjects.Add(stim.StimGameObject);   
        }
    }

    public void MakeStimFaceCamera()
    {
        foreach (StimGroup group in TrialStims)
        foreach (var stim in group.stimDefs)
        {
            stim.StimGameObject.transform.LookAt(Camera.main.transform);
        }
    }
    public override void FinishTrialCleanup()
    {
        // Remove the Stimuli, Context, and Token Bar from the Player View and move to neutral ITI State
        if (!Session.WebBuild)
            DestroyTextOnExperimenterDisplay();

        searchStim.ToggleVisibility(false);
        
        if (TokenFBController.isActiveAndEnabled)
            TokenFBController.enabled = false;
        
        if (AbortCode == 0)
            CurrentTaskLevel.SetBlockSummaryString();
        else
        {
            CurrentTaskLevel.NumAbortedTrials_InBlock++;
            CurrentTaskLevel.NumAbortedTrials_InTask++;
            CurrentTaskLevel.ClearStrings();
            CurrentTaskLevel.CurrentBlockSummaryString.AppendLine("");
        }

        TokenFBController.ResetTokenBarFull();

    }

    protected override void DefineTrialStims()
    {
        //Define StimGroups consisting of StimDefs whose gameobjects will be loaded at TrialLevel_SetupTrial and 
        //destroyed at TrialLevel_Finish

        StimGroup group = Session.UsingDefaultConfigs ? PrefabStims : ExternalStims;

        searchStim = new StimGroup("SearchStimuli", group, CurrentTrialDef.TrialStimIndices);

        if(TokensWithStimOn?? false)
            searchStim.SetVisibilityOnOffStates(GetStateFromName("SearchDisplay"), GetStateFromName("ITI"));
        else
            searchStim.SetVisibilityOnOffStates(GetStateFromName("SearchDisplay"),GetStateFromName("SelectionFeedback"));
        TrialStims.Add(searchStim);

        for (int iStim = 0; iStim < CurrentTrialDef.TrialStimIndices.Length; iStim++)
        {
            FlexLearning_StimDef sd = (FlexLearning_StimDef)searchStim.stimDefs[iStim];

            if (CurrentTrialDef.ProbabilisticTrialStimTokenReward != null)
                sd.StimTokenRewardMag = chooseReward(CurrentTrialDef.ProbabilisticTrialStimTokenReward[iStim]);
            else
                sd.StimTokenRewardMag = CurrentTrialDef.TrialStimTokenReward[iStim];


            if (sd.StimTokenRewardMag > 0) 
                sd.IsTarget = true; //CHECK THIS IMPLEMENTATION!!! only works if the target stim has a non-zero, positive reward
            else 
                sd.IsTarget = false;
        }
        
        if (CurrentTrialDef.RandomizedLocations)
        {
            int[] positionIndexArray = Enumerable.Range(0, CurrentTrialDef.TrialStimIndices.Length).ToArray();
            System.Random random = new System.Random();
            positionIndexArray = positionIndexArray.OrderBy(x => random.Next()).ToArray();

            for (int i = 0; i < CurrentTrialDef.TrialStimIndices.Length; i++)
            {
                searchStim.stimDefs[i].StimLocation = CurrentTrialDef.TrialStimLocations.ElementAt(positionIndexArray[i]);
            }
        }
        else
        {
            searchStim.SetLocations(CurrentTrialDef.TrialStimLocations);
        }
    }
    public override void ResetTrialVariables()
    {
        choiceMade = false;
        selectedGO = null;
        selectedSD = null;
        SelectedStimIndex = null;
        SelectedStimLocation = null;
        SearchDuration = 0;
        CorrectSelection = false;
        RewardGiven = false;
    }
    private void DefineTrialData()
    {
        // All AddDatum commands for the Trial Data
        TrialData.AddDatum("TrialID", () => CurrentTrialDef.TrialID);
        TrialData.AddDatum("ContextName", () => CurrentTrialDef.ContextName);
        TrialData.AddDatum("SelectedStimIndex", () => selectedSD?.StimIndex ?? null);
        TrialData.AddDatum("SelectedLocation", () => selectedSD?.StimLocation ?? null);
        TrialData.AddDatum("CorrectSelection", () => CorrectSelection ? 1 : 0);
        TrialData.AddDatum("SearchDuration", ()=> SearchDuration);
        TrialData.AddDatum("RewardGiven", ()=> RewardGiven? 1 : 0);
        TrialData.AddDatum("TotalClicks", ()=> Session.MouseTracker.GetClickCount()[0]);
    }
    private void DefineFrameData()
    {
        // All AddDatum commmands from the Frame Data
        FrameData.AddDatum("ContextName", () => ContextName);
        FrameData.AddDatum("StartButtonVisibility", () => StartButton == null ? false:StartButton.activeSelf); // CHECK THE DATA!
        FrameData.AddDatum("TrialStimVisibility", () => searchStim?.IsActive);
    }

    private void CreateTextOnExperimenterDisplay()
    {
        //Create corresponding text on player view of experimenter display
        foreach (FlexLearning_StimDef stim in searchStim.stimDefs)
        {
            if (stim.IsTarget)
            {
                textLocation = ScreenToPlayerViewPosition(Camera.main.WorldToScreenPoint(stim.StimLocation), playerViewParent.transform);
                textLocation.y += 50;
                Vector3 textSize = new Vector3(2, 2,1);
                playerViewText = playerView.CreateTextObject("TargetText","TARGET",
                    Color.red, textLocation, textSize, playerViewParent.transform);                
                playerViewText.SetActive(true);
            }
        }
        playerViewLoaded = true;
    }
    private void DestroyTextOnExperimenterDisplay()
    {
        DestroyChildren(playerViewParent);
        playerViewText = null;
        playerViewLoaded = false;
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
        tokenFlashingDuration = ConfigUiVariables.get<ConfigNumber>("tokenFlashingDuration");

        tokenFbDuration = (tokenFlashingDuration.value + tokenUpdateDuration.value + tokenRevealDuration.value);//ensures full flashing duration within
        ////configured token fb duration
        configUIVariablesLoaded = true;
    }
    void SetTrialSummaryString()
    {
        TrialSummaryString = "Selected Object Index: " + SelectedStimIndex +
                             "\nSelected Object Location: " + SelectedStimLocation +
                             "\n" +
                             "\nCorrect Selection: " + CorrectSelection +
                             "\n" +
                             "\nSearch Duration: " + SearchDuration +
                             "\n" + 
                             "\nToken Bar Value: " + TokenFBController.GetTokenBarValue();
    }
    protected override bool CheckBlockEnd()
    {
        return (CurrentTaskLevel.TaskLevel_Methods.CheckBlockEnd(CurrentTrialDef.BlockEndType, runningAcc,
            CurrentTrialDef.BlockEndThreshold, CurrentTrialDef.BlockEndWindow, CurrentTaskLevel.MinTrials_InBlock,
            CurrentTaskLevel.MaxTrials_InBlock) || TrialCount_InBlock == CurrentTaskLevel.MaxTrials_InBlock);
        
    }

    private void UpdateExperimenterDisplaySummaryStrings()
    {
        if (TrialCount_InTask != 0)
            CurrentTaskLevel.SetTaskSummaryString();
        CurrentTaskLevel.SetBlockSummaryString();
        SetTrialSummaryString();
    }
}
