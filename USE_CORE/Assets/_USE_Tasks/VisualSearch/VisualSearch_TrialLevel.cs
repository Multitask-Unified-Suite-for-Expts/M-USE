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
using System.Linq;
using UnityEngine;
using ConfigDynamicUI;
using USE_States;
using USE_StimulusManagement;
using USE_ExperimentTemplate_Trial;
using VisualSearch_Namespace;
using USE_UI;
using TMPro;

public class VisualSearch_TrialLevel : ControlLevel_Trial_Template
{
    public VisualSearch_TrialDef CurrentTrialDef => GetCurrentTrialDef<VisualSearch_TrialDef>(); 
    public VisualSearch_TaskLevel CurrentTaskLevel => GetTaskLevel<VisualSearch_TaskLevel>();
    public VisualSearch_TaskDef currentTaskDef => GetTaskDef<VisualSearch_TaskDef>();

    public GameObject VS_CanvasGO;
    public SelectionTracking.SelectionTracker.SelectionHandler ShotgunHandler;
    
    // Stimuli Variables
    private StimGroup searchStim;
    private GameObject StartButton;
    
    // ConfigUI variables / Timing Variable
    private bool configUIVariablesLoaded;
    [HideInInspector]
    public ConfigNumber minObjectTouchDuration, itiDuration, fbDuration, maxObjectTouchDuration, 
        selectObjectDuration, tokenRevealDuration, tokenUpdateDuration, tokenFlashingDuration, searchDisplayDelay;
    private float tokenFbDuration;

    // Stim Evaluation Variables
    private GameObject selectedGO;
    private bool CorrectSelection;
    VisualSearch_StimDef selectedSD;
    
    //Player View Variables
    private PlayerViewPanel playerView;
    private GameObject playerViewParent; // Helps set things onto the player view in the experimenter display
    private GameObject playerViewText;
    private Vector2 textLocation;

    // Block Data Variables
    [HideInInspector] public string ContextName;
    [HideInInspector] public int NumCorrect_InBlock;
    [HideInInspector] public int NumErrors_InBlock;
    [HideInInspector] public List<float?> SearchDurations_InBlock = new List<float?>();
    [HideInInspector] public int NumTokenBarFull_InBlock;
    [HideInInspector] public int TotalTokensCollected_InBlock;
    [HideInInspector] public decimal Accuracy_InBlock;

    // Trial Data Variables
    private int? SelectedStimIndex;
    private Vector3? SelectedStimLocation;
    private float searchStartTime;
    private bool RewardGiven;
    private bool choiceMade;
    private VisualSearch_TrialDataSummary TrialDataSummary;
    [HideInInspector] public int PreSearch_TouchFbErrorCount;


    public override void DefineControlLevel()
    {
        State InitTrial = new State("InitTrial");
        State SearchDisplay = new State("SearchDisplay");
        State SearchDisplayDelay = new State("SearchDisplayDelay");
        State SelectionFeedback = new State("SelectionFeedback");
        State TokenFeedback = new State("TokenFeedback");
        State ITI = new State("ITI");
        
        AddActiveStates(new List<State> {InitTrial, SearchDisplay, SelectionFeedback, TokenFeedback, ITI, SearchDisplayDelay});
        
        Add_ControlLevel_InitializationMethod(() =>
        {
            if (!Session.WebBuild)
            {
                playerView = gameObject.AddComponent<PlayerViewPanel>();
                playerViewParent = GameObject.Find("MainCameraCopy");
            }

            // Initialize FB Controller Values
            HaloFBController.SetCircleHaloRange(2.5f);
            HaloFBController.SetCircleHaloIntensity(3f);
        });

        SetupTrial.AddSpecificInitializationMethod(() =>
        {
            TrialDataSummary = new VisualSearch_TrialDataSummary();
            CurrentTaskLevel.AllTrialDataSummaries.Add(TrialDataSummary);
            TrialDataSummary.FeatureSimilarity = CurrentTrialDef.FeatureSimilarity;
            //Set the Stimuli Light/Shadow settings
            SetShadowType(currentTaskDef.ShadowType, "VisualSearch_DirectionalLight");
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
                    StartButton = Session.USE_StartButton.CreateStartButton(VS_CanvasGO.GetComponent<Canvas>(), currentTaskDef.StartButtonPosition, currentTaskDef.StartButtonScale);
                    Session.USE_StartButton.SetVisibilityOnOffStates(InitTrial, InitTrial);
                }
            }

            if (!configUIVariablesLoaded)
                LoadConfigUIVariables();

            UpdateExperimenterDisplaySummaryStrings();
        });
        
        SetupTrial.SpecifyTermination(() => true, InitTrial);

        //INIT TRIAL STATE ----------------------------------------------------------------------------------------------
        if (!Session.SessionDef.EyeTrackerActive)
            ShotgunHandler = Session.SelectionTracker.SetupSelectionHandler("trial", "TouchShotgun", Session.MouseTracker, InitTrial, SearchDisplay);
        else
            ShotgunHandler = Session.SelectionTracker.SetupSelectionHandler("trial", "GazeShotgun", Session.GazeTracker, InitTrial, SearchDisplay);

        TouchFBController.EnableTouchFeedback(ShotgunHandler, currentTaskDef.TouchFeedbackDuration, currentTaskDef.StartButtonScale *10, VS_CanvasGO, true);

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

        InitTrial.SpecifyTermination(() => ShotgunHandler.LastSuccessfulSelectionMatchesStartButton(),
            SearchDisplayDelay, () => 
            {
                choiceMade = false;
            });
        
        // Provide delay following start button selection and before stimuli onset
        SearchDisplayDelay.AddTimer(() => searchDisplayDelay.value, SearchDisplay);
        
        // SEARCH DISPLAY STATE ----------------------------------------------------------------------------------------
        SearchDisplay.AddSpecificInitializationMethod(() =>
        {
            Input.ResetInputAxes(); //reset input in case they holding down
            // Toggle TokenBar and Stim to be visible
            TokenFBController.enabled = true;
            searchStartTime = Time.time;
            

            Session.EventCodeManager.AddToFrameEventCodeBuffer("TokenBarVisible");
            
            if (ShotgunHandler.AllSelections.Count > 0)
                ShotgunHandler.ClearSelections();

            PreSearch_TouchFbErrorCount = TouchFBController.ErrorCount;

            if (!Session.WebBuild)
                CreateTextOnExperimenterDisplay();
        });
        SearchDisplay.AddUpdateMethod(() =>
        {
            if (ShotgunHandler.SuccessfulSelections.Count > 0)
            {

                selectedGO = ShotgunHandler.LastSuccessfulSelection.SelectedGameObject;
                selectedSD = selectedGO?.GetComponent<StimDefPointer>()?.GetStimDef<VisualSearch_StimDef>();
                if (selectedSD != null)
                {
                    float searchDuration = Time.time - searchStartTime;
                    SearchDurations_InBlock.Add(searchDuration);
                    CurrentTaskLevel.SearchDurations_InTask.Add(searchDuration);
                    TrialDataSummary.ReactionTime = searchDuration;
                    TrialDataSummary.SelectionPrecision = ShotgunHandler.LastSuccessfulSelection.SelectionPrecision;
                    choiceMade = true;
                }
                ShotgunHandler.ClearSelections();
            }
        });
        
        SearchDisplay.SpecifyTermination(() => choiceMade, SelectionFeedback, () =>
        {
            CorrectSelection = selectedSD.IsTarget;

            if (CorrectSelection)
            {
                TrialDataSummary.CorrectSelection = 1;
                NumCorrect_InBlock++;
                CurrentTaskLevel.NumCorrect_InTask++;
                Session.EventCodeManager.AddToFrameEventCodeBuffer("CorrectResponse");
            }
            else
            {
                TrialDataSummary.CorrectSelection = 0;
                NumErrors_InBlock++;
                CurrentTaskLevel.NumErrors_InTask++;
                Session.EventCodeManager.AddToFrameEventCodeBuffer("IncorrectResponse");
            }

            if (selectedGO != null)
            {
                SelectedStimIndex = selectedSD.StimIndex;
                SelectedStimLocation = selectedSD.StimLocation;
            }
            UpdateExperimenterDisplaySummaryStrings();
        });
        SearchDisplay.AddTimer(() => selectObjectDuration.value, ITI, () =>
        {
            Session.EventCodeManager.AddToFrameEventCodeBuffer("NoChoice");
            Session.EventCodeManager.SendRangeCode("CustomAbortTrial", AbortCodeDict["NoSelectionMade"]);
            AbortCode = 6;

            CurrentTaskLevel.SearchDurations_InTask.Add(null);
            SearchDurations_InBlock.Add(null);
            TrialDataSummary.ReactionTime = null;
            TrialDataSummary.CorrectSelection = null;
            TrialDataSummary.SelectionPrecision = null;


            SetTrialSummaryString();

        });

        // SELECTION FEEDBACK STATE ---------------------------------------------------------------------------------------   
        SelectionFeedback.AddSpecificInitializationMethod(() =>
        {
            Accuracy_InBlock = decimal.Divide(NumCorrect_InBlock, (TrialCount_InBlock + 1));

            
            UpdateExperimenterDisplaySummaryStrings();

            int? depth = Session.Using2DStim ? 50 : (int?)null;

            if (CorrectSelection) 
                HaloFBController.ShowPositive(selectedGO, particleHaloActive: CurrentTrialDef.ParticleHaloActive, circleHaloActive: CurrentTrialDef.CircleHaloActive, depth: depth);
            else 
                HaloFBController.ShowNegative(selectedGO, particleHaloActive: CurrentTrialDef.ParticleHaloActive, circleHaloActive: CurrentTrialDef.CircleHaloActive, depth: depth);
        });

        SelectionFeedback.AddTimer(() => fbDuration.value, TokenFeedback, () => HaloFBController.DestroyAllHalos());

        
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
       
        TokenFeedback.AddTimer(() => tokenFbDuration, () => ITI, () =>
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
                CurrentTaskLevel.SetSkyBox(GetContextNestedFilePath(!string.IsNullOrEmpty(currentTaskDef.ContextExternalFilePath) ? currentTaskDef.ContextExternalFilePath : Session.SessionDef.ContextExternalFilePath, "NeutralITI"));
                Session.EventCodeManager.AddToFrameEventCodeBuffer("ContextOff");
            }

            var test = CurrentTaskLevel.CreateTaskDataSummary();
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
        foreach (VisualSearch_StimDef stim in searchStim.stimDefs)
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
        if(!Session.WebBuild)
        {
            if (playerViewParent.transform.childCount != 0)
                DestroyChildren(playerViewParent);
        }

        searchStim.ToggleVisibility(false);
        
        if (TokenFBController.isActiveAndEnabled)
            TokenFBController.enabled = false;

        if(AbortCode != 0) 
        {
            CurrentTaskLevel.NumAbortedTrials_InBlock++;
            CurrentTaskLevel.NumAbortedTrials_InTask++;
            CurrentTaskLevel.ClearStrings();
            CurrentTaskLevel.CurrentBlockSummaryString.AppendLine("");
        }

        CurrentTaskLevel.SetBlockSummaryString();
        TokenFBController.ResetTokenBarFull();

    }
    private void DestroyTextOnExperimenterDisplay()
    {
        DestroyChildren(playerViewParent);
    }

    public void ResetBlockVariables()
    {
        SearchDurations_InBlock.Clear();
        NumCorrect_InBlock = 0;
        NumErrors_InBlock = 0;
        NumTokenBarFull_InBlock = 0;
        TotalTokensCollected_InBlock = 0;
        Accuracy_InBlock = 0;
    }

    protected override void DefineTrialStims()
    {
        //Define StimGroups consisting of StimDefs whose gameobjects will be loaded at TrialLevel_SetupTrial and 
        //destroyed at TrialLevel_Finish

        StimGroup group = Session.UsingDefaultConfigs ? PrefabStims : ExternalStims;

        searchStim = new StimGroup("SearchStimuli", group, CurrentTrialDef.TrialStimIndices);
        if(CurrentTrialDef.TokensWithStimOn)
            searchStim.SetVisibilityOnOffStates(GetStateFromName("SearchDisplay"), GetStateFromName("ITI"));
        else
            searchStim.SetVisibilityOnOffStates(GetStateFromName("SearchDisplay"),GetStateFromName("SelectionFeedback"));
        TrialStims.Add(searchStim);
        for (int iStim = 0; iStim < CurrentTrialDef.TrialStimIndices.Length; iStim++)
        {
            VisualSearch_StimDef sd = (VisualSearch_StimDef)searchStim.stimDefs[iStim];

            if (CurrentTrialDef.ProbabilisticTrialStimTokenReward != null)
                sd.StimTokenRewardMag = chooseReward(CurrentTrialDef.ProbabilisticTrialStimTokenReward[iStim]);
            else
                sd.StimTokenRewardMag = CurrentTrialDef.TrialStimTokenReward[iStim];


            if (sd.StimTokenRewardMag > 0)
                sd.IsTarget = true; //ONLY HOLDS TRUE IF POSITIVE REWARD GIVEN TO TARGET
            else
            {
                TrialDataSummary.NumDistractors++;
                sd.IsTarget = false;
            }

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
        SelectedStimIndex = null;
        SelectedStimLocation = null;
        searchStartTime = 0;
        CorrectSelection = false;
        RewardGiven = false;
        choiceMade = false;
    }
    private void DefineTrialData()
    {
        // All AddDatum commands for the Trial Data
        TrialData.AddDatum("TrialID", ()=> CurrentTrialDef.TrialID);
        TrialData.AddDatum("ContextName", ()=> CurrentTrialDef.ContextName);
        TrialData.AddDatum("SelecteStimIndex", () => selectedSD?.StimIndex ?? null);
        TrialData.AddDatum("SelectedLocation", () => selectedSD?.StimLocation ?? null);
        TrialData.AddDatum("CorrectSelection", () => CorrectSelection ? 1 : 0);
        TrialData.AddDatum("SearchDuration", ()=> searchStartTime);
        TrialData.AddDatum("RewardGiven", ()=> RewardGiven? 1 : 0);
        TrialData.AddDatum("TotalClicks", ()=> Session.MouseTracker.GetClickCount()[0]);
    }
    private void DefineFrameData()
    {
        // All AddDatum commmands from the Frame Data
        FrameData.AddDatum("ContextName", () => ContextName);
        FrameData.AddDatum("StartButtonVisibility", () => StartButton?.activeSelf); // CHECK THE DATA!
        FrameData.AddDatum("TrialStimVisibility", () => searchStim?.IsActive);
    }

    private void CreateTextOnExperimenterDisplay()
    { // sets parent for any playerView elements on experimenter display
        
        //Create corresponding text on player view of experimenter display
        foreach (VisualSearch_StimDef stim in searchStim.stimDefs)
        {
            if (stim.IsTarget)
            {
                textLocation = ScreenToPlayerViewPosition(Camera.main.WorldToScreenPoint(stim.StimLocation), playerViewParent.transform);
                textLocation.y += 50;
                Vector3 textSize = new Vector3(2,2,1);
                playerViewText = playerView.CreateTextObject("TargetText","TARGET",
                    Color.red, textLocation, textSize, playerViewParent.transform);
                playerViewText.SetActive(true);
            }
        }
    }
    private void LoadConfigUIVariables()
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
    private void SetTrialSummaryString()
    {
        TrialSummaryString = "Selected Object Index: " + SelectedStimIndex +
                             "\nSelected Object Location: " + SelectedStimLocation +
                             "\n" +
                             "\nCorrect Selection: " + CorrectSelection +
                             "\n" +
                             "\nSearch Duration: " + searchStartTime +
                             "\n" + 
                             "\nToken Bar Value: " + TokenFBController.GetTokenBarValue();
    }

    private void UpdateExperimenterDisplaySummaryStrings()
    {
        if (TrialCount_InTask != 0)
            CurrentTaskLevel.SetTaskSummaryString();
        SetTrialSummaryString();
        CurrentTaskLevel.SetBlockSummaryString();
    }

}
