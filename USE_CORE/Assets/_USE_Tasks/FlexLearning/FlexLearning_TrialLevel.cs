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
using System.Collections;
using static SelectionTracking.SelectionTracker;
// #if (!UNITY_WEBGL)
// using static System.Windows.Forms.VisualStyles.VisualStyleElement;
// #endif  


public class FlexLearning_TrialLevel : ControlLevel_Trial_Template
{
    public FlexLearning_TrialDef CurrentTrial => GetCurrentTrialDef<FlexLearning_TrialDef>();
    public FlexLearning_TaskLevel CurrentTaskLevel => GetTaskLevel<FlexLearning_TaskLevel>();
    public FlexLearning_TaskDef CurrentTask => GetTaskDef<FlexLearning_TaskDef>();

    public GameObject FL_CanvasGO;

    // Block End Variables
    public List<int> runningAcc;
    
    // Stimuli Variables
    private StimGroup searchStim;
    private GameObject StartButton;

    // ConfigUI Variables
    [HideInInspector]
    public ConfigNumber timeBeforeChoiceStarts, totalChoiceDuration, itiDuration, 
        fbDuration, selectObjectDuration, tokenRevealDuration, tokenUpdateDuration, tokenFlashingDuration, 
        searchDisplayDelay;

    private float tokenFbDuration;
    

    // Set in the Task Level
    [HideInInspector] public bool? TokensWithStimOn;

    
    // Stim Evaluation Variables
    private GameObject selectedGO = null;
    private bool CorrectSelection;
    FlexLearning_StimDef selectedSD = null;
    private bool ChoiceMade = false;
    private USE_Selection lastSelection = null;
    
    
    //Player View Variables
    private GameObject playerViewText;
    private Vector2 textLocation;
   
    // Block Data Variables
    [HideInInspector] public string ContextName = "";
    [HideInInspector] public int NumCorrect_InBlock;
    [HideInInspector] public int NumErrors_InBlock;
    [HideInInspector] public List<float?> SearchDurations_InBlock = new List<float?>();
    [HideInInspector] public int TrialsWithTokenGain_InBlock;

    [HideInInspector] public int NumTokenBarFull_InBlock;
    [HideInInspector] public int TotalTokensCollected_InBlock;
    [HideInInspector] public decimal ChoiceAccuracy_InBlock;
    [HideInInspector] public decimal PercentRewarded_InBlock;


    // Trial Data Variables
    private int? SelectedStimIndex = null;
    private Vector3? SelectedStimLocation = null;
    private float SearchDuration = 0;
    private bool RewardGiven = false;

    [HideInInspector] public int PreSearch_TouchFbErrorCount;



    public override void DefineControlLevel()
    {
        State InitTrial = new State("InitTrial");
        State SearchDisplayDelay = new State("SearchDisplayDelay");
        State SearchDisplay = new State("SearchDisplay");
        State SelectionFeedback = new State("SelectionFeedback");
        State TokenFeedback = new State("TokenFeedback");
        State ITI = new State("ITI");

        AddActiveStates(new List<State> { InitTrial, SearchDisplay, SelectionFeedback, TokenFeedback, ITI, SearchDisplayDelay });
        
        Add_ControlLevel_InitializationMethod(() =>
        {
            if(!Session.WebBuild)
            {
                PlayerViewPanel = gameObject.AddComponent<PlayerViewPanel>();
                PlayerViewGO = GameObject.Find("MainCameraCopy");     
            }

            if (StartButton == null)
            {
                if (Session.SessionDef.IsHuman)
                {
                    StartButton = Session.HumanStartPanel.StartButtonGO;
                    Session.HumanStartPanel.SetVisibilityOnOffStates(InitTrial, InitTrial);
                }
                else
                {
                    StartButton = Session.USE_StartButton.CreateStartButton(FL_CanvasGO.GetComponent<Canvas>(), CurrentTask.StartButtonPosition, CurrentTask.StartButtonScale);
                    Session.USE_StartButton.SetVisibilityOnOffStates(InitTrial, InitTrial);
                }
            }

            // Initialize FB Controller Values
            HaloFBController.SetCircleHaloIntensity(Session.UsingDefaultConfigs ? 1.75f : 2.5f);
        });
        
        SetupTrial.AddSpecificInitializationMethod(() =>
        {
            //SET AND SEND STIMULATION CODE FOR THE TRIAL:
            CanStimulateThisTrial = false;
            if (CurrentTrial.StimulationConditionCodes != null && CurrentTrial.StimulationConditionCodes.Length > 0)
            {
                CanStimulateThisTrial = true;

                int randomIndex = Random.Range(0, CurrentTrial.StimulationConditionCodes.Length);
                TrialStimulationCode = CurrentTrial.StimulationConditionCodes[randomIndex];
                Session.EventCodeManager.SendRangeCodeThisFrame("StimulationCondition", TrialStimulationCode);
            }

            //Set the Stimuli Light/Shadow settings
            SetShadowType(CurrentTask.ShadowType, "FlexLearning_DirectionalLight");
            if (CurrentTask.StimFacingCamera)
                MakeStimFaceCamera();
            
            LoadConfigUIVariables();

            UpdateExperimenterDisplaySummaryStrings();
        });
        SetupTrial.SpecifyTermination(() => true, InitTrial);

        
        // The code below allows the SelectionHandler to switch on the basis of the SelectionType in the SessionConfig
        if (Session.SessionDef.SelectionType.ToLower().Contains("gaze"))
            SelectionHandler = Session.SelectionTracker.SetupSelectionHandler("trial", "GazeShotgun", Session.GazeTracker, InitTrial, SearchDisplay);
        else
            SelectionHandler = Session.SelectionTracker.SetupSelectionHandler("trial", Session.SessionDef.SelectionType, Session.MouseTracker, InitTrial, SearchDisplay);
        
        TouchFBController.EnableTouchFeedback(SelectionHandler, CurrentTask.TouchFeedbackDuration, CurrentTask.TouchFeedbackSize, FL_CanvasGO);

        //INIT TRIAL STATE ----------------------------------------------------------------------------------------------
        InitTrial.AddSpecificInitializationMethod(() =>
        {
            if (Session.SessionDef.MacMainDisplayBuild & !Application.isEditor) //adj text positions if running build with mac as main display
                TokenFBController.AdjustTokenBarSizing(200);


            if (CurrentTask.UseTimer)
            {
                Session.TimerController.CreateTimer(FL_CanvasGO.transform);
                Session.TimerController.SetVisibilityOnOffStates(SearchDisplay, SearchDisplay);
                Session.TimerController.SetDuration(selectObjectDuration.value);
            }


            TokenFBController.SetRevealTime(tokenRevealDuration.value);
            TokenFBController.SetUpdateTime(tokenUpdateDuration.value);
            TokenFBController.SetFlashingTime(tokenFlashingDuration.value);

            if (SelectionHandler.AllChoices.Count > 0)
                SelectionHandler.ClearChoices();

            SelectionHandler.TimeBeforeChoiceStarts = Session.SessionDef.StartButtonSelectionDuration;
            SelectionHandler.TotalChoiceDuration = Session.SessionDef.StartButtonSelectionDuration;

            Input.ResetInputAxes(); //reset input in case they holding down
        });
        InitTrial.SpecifyTermination(() => SelectionHandler.LastSuccessfulSelectionMatchesStartButton(), SearchDisplayDelay, () =>
        {
            SelectionHandler.TimeBeforeChoiceStarts = timeBeforeChoiceStarts.value;
            SelectionHandler.TotalChoiceDuration = totalChoiceDuration.value;
        });

        // Provide delay following start button selection and before stimuli onset
        SearchDisplayDelay.AddTimer(() => searchDisplayDelay.value, SearchDisplay);
        
        // SEARCH DISPLAY STATE ----------------------------------------------------------------------------------------
        SearchDisplay.AddSpecificInitializationMethod(() =>
        {
            Input.ResetInputAxes(); //reset input in case they holding down
            TokenFBController.enabled = true;

            if (!Session.WebBuild)
                ActivateChildren(PlayerViewGO);

            Session.EventCodeManager.SendCodeThisFrame("TokenBarVisible");
            
            if (SelectionHandler.AllChoices.Count > 0)
                SelectionHandler.ClearChoices();

            PreSearch_TouchFbErrorCount = TouchFBController.ErrorCount;

            if (!Session.WebBuild)
                CreateTextOnExperimenterDisplay();

            ChoiceFailed_Trial = false;

            if (SelectionHandler.AllChoices.Count > 0)
                SelectionHandler.ClearChoices();

            //reset it so the duration is 0 on exp display even if had one last trial
            OngoingSelection = null;

        });
        SearchDisplay.AddUpdateMethod(() =>
        {
            OngoingSelection = SelectionHandler.OngoingSelection;

            if (OngoingSelection != null)
            {
                if (!StimulatedThisTrial && !string.IsNullOrEmpty(CurrentTrial.StimulationType))
                {
                    if (OngoingSelection.Duration >= CurrentTrial.InitialFixationDuration)
                    {
                        GameObject GoSelected = OngoingSelection.SelectedGameObject;
                        var SdSelected = GoSelected?.GetComponent<StimDefPointer>()?.GetStimDef<FlexLearning_StimDef>();

                        if (SdSelected != null)
                        {
                            if (CurrentTrial.StimulationType == "FixationChoice_Target" && SdSelected.IsTarget)
                            {
                                StartCoroutine(StimulationCoroutine());
                            }
                            else if (CurrentTrial.StimulationType == "FixationChoice_Distractor" && !SdSelected.IsTarget)
                            {
                                StartCoroutine(StimulationCoroutine());
                            }
                        }
                    }
                }
            }

            SetTrialSummaryString();

            if (SelectionHandler.UnsuccessfulChoices.Count > 0 && !ChoiceFailed_Trial)
            {
                ChoiceFailed_Trial = true;
            }

            if (SelectionHandler.SuccessfulChoices.Count > 0)
            {
                lastSelection = SelectionHandler.LastSuccessfulChoice;
                selectedGO = lastSelection.SelectedGameObject;
                selectedSD = selectedGO?.GetComponent<StimDefPointer>()?.GetStimDef<FlexLearning_StimDef>();

                if(selectedSD != null)
                {
                    ChoiceMade = true;
                    Session.EventCodeManager.SendCodeThisFrame(selectedSD.IsTarget ? "CorrectResponse" : "IncorrectResponse");
                }
            }

        });
        SearchDisplay.SpecifyTermination(() => ChoiceMade, SelectionFeedback, () =>
        {
            CorrectSelection = selectedSD.IsTarget;

            if (CorrectSelection)
            {       
                NumCorrect_InBlock++;
                CurrentTaskLevel.NumCorrect_InTask++;
                runningAcc.Add(1);
            }
            else
            {
                NumErrors_InBlock++;
                CurrentTaskLevel.NumErrors_InTask++;
                runningAcc.Add(0);
            }

            if (selectedGO != null)
            {
                SelectedStimIndex = selectedSD.StimIndex;
                SelectedStimLocation = selectedSD.StimLocation;
            }

            if(selectedSD.StimTokenRewardMag > 0)
            {
                TrialsWithTokenGain_InBlock++;
                CurrentTaskLevel.TrialsWithTokenGain_InTask++;
            }

            ChoiceAccuracy_InBlock = decimal.Divide(NumCorrect_InBlock, TrialCount_InBlock + 1);
            PercentRewarded_InBlock = decimal.Divide(TrialsWithTokenGain_InBlock, TrialCount_InBlock + 1);

            UpdateExperimenterDisplaySummaryStrings();
        });
        SearchDisplay.SpecifyTermination(() => ChoiceFailed_Trial, Delay, () =>
        {
            AbortCode = 8;
            DelayDuration = .5f; //50ms delay for stimulation to finish
            StateAfterDelay = ITI;

            runningAcc.Add(0);
            CurrentTaskLevel.SearchDurations_InTask.Add(null);
            SearchDurations_InBlock.Add(null);
            SetTrialSummaryString();
        });
        SearchDisplay.AddTimer(() => selectObjectDuration.value, ITI, () =>
        {
            Session.EventCodeManager.SendCodeThisFrame("NoChoice");
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

            //TEMPORARY:
            if (!Session.UsingDefaultConfigs)
            {
                HaloFBController.SetCircleHaloPositions(new Vector3(0f, 0f, -1f));
            }

            int? depthFor2D = Session.Using2DStim ? 50 : (int?) null;

            if (selectedSD.StimTokenRewardMag > 0)
                HaloFBController.ShowPositive(selectedGO, CurrentTrial.ParticleHaloActive, CurrentTrial.CircleHaloActive, depthFor2D);
            else
                HaloFBController.ShowNegative(selectedGO, CurrentTrial.ParticleHaloActive, CurrentTrial.CircleHaloActive, depthFor2D);
        });

        SelectionFeedback.AddTimer(() => fbDuration.value, TokenFeedback, () =>
        {
            HaloFBController.DestroyAllHalos();
            ChoiceMade = false;
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
        TokenFeedback.AddTimer(() => tokenFbDuration, ITI);

        // ITI STATE ---------------------------------------------------------------------------------------------------
        ITI.AddSpecificInitializationMethod(() =>
        {
            if (CurrentTask.NeutralITI)
            {
                ContextName = "NeutralITI";
                StartCoroutine(HandleSkybox(GetContextNestedFilePath(!string.IsNullOrEmpty(CurrentTask.ContextExternalFilePath) ? CurrentTask.ContextExternalFilePath : Session.SessionDef.ContextExternalFilePath, "NeutralITI")));
                Session.EventCodeManager.SendCodeThisFrame("ContextOff");
            }
        });
        ITI.AddTimer(() => itiDuration.value, FinishTrial);

        //---------------------------------ADD FRAME AND TRIAL DATA TO LOG FILES---------------------------------------
        DefineTrialData();
        DefineFrameData();
    }


    public override void OnTokenBarFull()
    {
        NumTokenBarFull_InBlock++;
        CurrentTaskLevel.NumTokenBarFull_InTask++;

        if (Session.SyncBoxController != null)
        {
            int numPulses;
            if (CurrentTrial.ProbablisticNumPulses != null)
                numPulses = chooseReward(CurrentTrial.ProbablisticNumPulses);
            else
                numPulses = CurrentTrial.NumPulses;

            StartCoroutine(Session.SyncBoxController.SendRewardPulses(numPulses, CurrentTrial.PulseSize));

            CurrentTaskLevel.NumRewardPulses_InBlock += numPulses;
            CurrentTaskLevel.NumRewardPulses_InTask += numPulses;

            RewardGiven = true;
        }
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
    }

    protected override void DefineTrialStims()
    {
        //Define StimGroups consisting of StimDefs whose gameobjects will be loaded at TrialLevel_SetupTrial and 
        //destroyed at TrialLevel_Finish

        StimGroup group = Session.UsingDefaultConfigs ? PrefabStims : ExternalStims;

        searchStim = new StimGroup("SearchStimuli", group, CurrentTrial.TrialStimIndices);

        if(TokensWithStimOn?? false)
            searchStim.SetVisibilityOnOffStates(GetStateFromName("SearchDisplay"), GetStateFromName("ITI"));
        else
            searchStim.SetVisibilityOnOffStates(GetStateFromName("SearchDisplay"),GetStateFromName("SelectionFeedback"));

        TrialStims.Add(searchStim);


        float highestOverallProb = -1;

        for (int iStim = 0; iStim < CurrentTrial.TrialStimIndices.Length; iStim++)
        {
            FlexLearning_StimDef sd = (FlexLearning_StimDef)searchStim.stimDefs[iStim];

            var stimRewardList = CurrentTrial.ProbabilisticTrialStimTokenReward[iStim];

            sd.StimTokenRewardMag = chooseReward(CurrentTrial.ProbabilisticTrialStimTokenReward[iStim]);

            foreach(var reward in stimRewardList)
            {
                if(reward.NumReward > 0 && reward.Probability > highestOverallProb)
                    highestOverallProb = reward.Probability;
            }
        }

        for (int iStim = 0; iStim < CurrentTrial.TrialStimIndices.Length; iStim++)
        {
            FlexLearning_StimDef sd = (FlexLearning_StimDef)searchStim.stimDefs[iStim];

            var stimRewardList = CurrentTrial.ProbabilisticTrialStimTokenReward[iStim];

            float highestStimProb = -1;
            foreach (var reward in stimRewardList)
            {
                if (reward.NumReward > 0 && reward.Probability > highestStimProb)
                    highestStimProb = reward.Probability;
            }

            sd.IsTarget = highestStimProb == highestOverallProb;
        }


        if (CurrentTrial.RandomizedLocations)
        {
            int[] positionIndexArray = Enumerable.Range(0, CurrentTrial.TrialStimIndices.Length).ToArray();
            System.Random random = new System.Random();
            positionIndexArray = positionIndexArray.OrderBy(x => random.Next()).ToArray();

            for (int i = 0; i < CurrentTrial.TrialStimIndices.Length; i++)
            {
                searchStim.stimDefs[i].StimLocation = CurrentTrial.TrialStimLocations.ElementAt(positionIndexArray[i]);
            }
        }
        else
        {
            searchStim.SetLocations(CurrentTrial.TrialStimLocations);
        }
    }

    public override void ResetTrialVariables()
    {
        ChoiceMade = false;
        selectedGO = null;
        selectedSD = null;
        SelectedStimIndex = null;
        SelectedStimLocation = null;
        SearchDuration = 0;
        CorrectSelection = false;
        RewardGiven = false;
        ChoiceFailed_Trial = false;

    }
    private void DefineTrialData()
    {
        // All AddDatum commands for the Trial Data
        TrialData.AddDatum("TrialID", () => CurrentTrial.TrialID);
        TrialData.AddDatum("ContextName", () => CurrentTrial.ContextName);
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
                textLocation = ScreenToPlayerViewPosition(Camera.main.WorldToScreenPoint(stim.StimLocation), PlayerViewGO.transform);
                textLocation.y += 50;
                Vector3 textSize = new Vector3(2, 2,1);
                playerViewText = PlayerViewPanel.CreateTextObject("TargetText","TARGET",
                    Color.red, textLocation, textSize, PlayerViewGO.transform);                
                playerViewText.SetActive(true);
            }
        }
    }
    private void DestroyTextOnExperimenterDisplay()
    {
        DestroyChildren(PlayerViewGO);
        playerViewText = null;
    }
    void LoadConfigUIVariables()
    {
        //config UI variables
        timeBeforeChoiceStarts = ConfigUiVariables.get<ConfigNumber>("timeBeforeChoiceStarts");
        totalChoiceDuration = ConfigUiVariables.get<ConfigNumber>("totalChoiceDuration");
        itiDuration = ConfigUiVariables.get<ConfigNumber>("itiDuration");
        searchDisplayDelay = ConfigUiVariables.get<ConfigNumber>("searchDisplayDelay");
        selectObjectDuration = ConfigUiVariables.get<ConfigNumber>("selectObjectDuration");
        fbDuration = ConfigUiVariables.get<ConfigNumber>("fbDuration");
        tokenRevealDuration = ConfigUiVariables.get<ConfigNumber>("tokenRevealDuration");
        tokenUpdateDuration = ConfigUiVariables.get<ConfigNumber>("tokenUpdateDuration");
        tokenFlashingDuration = ConfigUiVariables.get<ConfigNumber>("tokenFlashingDuration");

        tokenFbDuration = (tokenFlashingDuration.value + tokenUpdateDuration.value + tokenRevealDuration.value);//ensures full flashing duration within
        ////configured token fb duration
    }
    void SetTrialSummaryString()
    {
        TrialSummaryString = "Selected Object Index: " + SelectedStimIndex +
                             "\nSelected Object Location: " + SelectedStimLocation +
                             "\nCorrect Selection: " + CorrectSelection +
                             "\nSearch Duration: " + SearchDuration +
                             "\nToken Bar Value: " + TokenFBController.GetTokenBarValue() +
                             "\nOngoingSelection: " + (OngoingSelection == null ? "" : OngoingSelection.Duration.Value.ToString("F2") + " s");

        if (TrialStimulationCode > 0)
        {
            TrialSummaryString += "\nStimulationCode: " + TrialStimulationCode.ToString();
            TrialSummaryString += "\nStimulationType: " + CurrentTrial.StimulationType;
        }
    }
    protected override bool CheckBlockEnd()
    {
        return (CurrentTaskLevel.TaskLevel_Methods.CheckBlockEnd(CurrentTrial.BlockEndType, runningAcc,
            CurrentTrial.BlockEndThreshold, CurrentTrial.BlockEndWindow, CurrentTaskLevel.MinTrials_InBlock,
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
