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


using ConfigDynamicUI;
using System.Collections.Generic;
using UnityEngine;
using USE_ExperimentTemplate_Trial;
using USE_States;
using USE_StimulusManagement;
using WorkingMemory_Namespace;
using static SelectionTracking.SelectionTracker;


public class WorkingMemory_TrialLevel : ControlLevel_Trial_Template
{
    public GameObject WM_CanvasGO;

    public WorkingMemory_TrialDef CurrentTrial => GetCurrentTrialDef<WorkingMemory_TrialDef>();
    public WorkingMemory_TaskLevel CurrentTaskLevel => GetTaskLevel<WorkingMemory_TaskLevel>();
    public WorkingMemory_TaskDef CurrentTask => GetTaskDef<WorkingMemory_TaskDef>();

    // Block End Variables
    public List<int> runningAcc;
    public int MinTrials, MaxTrials;
       
    // Stim Evaluation Variables
    private GameObject selectedGO = null;
    private bool CorrectSelection;
    WorkingMemory_StimDef selectedSD = null;
    
    // Stimuli Variables
    private StimGroup searchStims, sampleStim, postSampleDistractorStims;
    private GameObject StartButton;
       
    // Config Loading Variables
    private bool configUIVariablesLoaded = false;
    private float tokenFbDuration;

    [HideInInspector] public ConfigNumber timeBeforeChoiceStarts;
    [HideInInspector] public ConfigNumber totalChoiceDuration;
    [HideInInspector] public ConfigNumber maxSearchDuration;
    [HideInInspector] public ConfigNumber tokenRevealDuration;
    [HideInInspector] public ConfigNumber tokenUpdateDuration;
    [HideInInspector] public ConfigNumber tokenFlashingDuration;
    [HideInInspector] public ConfigNumber selectObjectDuration;
    [HideInInspector] public ConfigNumber fbDuration;
    [HideInInspector] public ConfigNumber itiDuration;
    
    //Player View Variables
    private PlayerViewPanel playerView;
    private GameObject playerViewParent; // Helps set things onto the player view in the experimenter display
    public List<GameObject> playerViewTextList = new List<GameObject>();
    public GameObject playerViewText;
    private Vector2 textLocation;
    private bool playerViewLoaded;
    
    // Block Data Variables
    public string ContextName = "";
    public int NumCorrect_InBlock;
    public List<float?> SearchDurations_InBlock = new List<float?>();
    public int NumErrors_InBlock;
    public int NumTokenBarFull_InBlock;
    public int TotalTokensCollected_InBlock;
    public float Accuracy_InBlock;
   
    // Trial Data Variables
    private int? SelectedStimIndex = null;
    private Vector3? SelectedStimLocation = null;
    private float SearchDuration = 0;
    private bool TouchDurationError = false;
    private bool choiceMade = false;


    public override void DefineControlLevel()
    {
        State InitTrial = new State("InitTrial");
        State DisplaySample = new State("DisplaySample");
        State DisplayDistractors = new State("DisplayDistractors");
        State SearchDisplay = new State("SearchDisplay");
        State SelectionFeedback = new State("SelectionFeedback");
        State TokenFeedback = new State("TokenFeedback");
        State ITI = new State("ITI");

        AddActiveStates(new List<State> { InitTrial, DisplaySample, DisplayDistractors, SearchDisplay, SelectionFeedback, TokenFeedback, ITI });


        Add_ControlLevel_InitializationMethod(() =>
        {
            if (!Session.WebBuild) //player view variables
            {
                playerView = gameObject.AddComponent<PlayerViewPanel>();
                playerViewParent = GameObject.Find("MainCameraCopy");
            }

            if (Session.SessionDef.IsHuman)
            {
                Session.TimerController.CreateTimer(WM_CanvasGO.transform);
                Session.TimerController.SetVisibilityOnOffStates(SearchDisplay, SearchDisplay);
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
                    StartButton = Session.USE_StartButton.CreateStartButton(WM_CanvasGO.GetComponent<Canvas>(), CurrentTask.StartButtonPosition, CurrentTask.StartButtonScale);
                    Session.USE_StartButton.SetVisibilityOnOffStates(InitTrial, InitTrial);
                }
            }

            // Initialize FB Controller Values
            HaloFBController.SetCircleHaloIntensity(3f);
        });
        SetupTrial.AddSpecificInitializationMethod(() =>
        {
            //Set the Stimuli Light/Shadow settings
            SetShadowType(CurrentTask.ShadowType, "WorkingMemory_DirectionalLight");
            if (CurrentTask.StimFacingCamera)
                MakeStimFaceCamera();


                        
            if (!configUIVariablesLoaded) 
                LoadConfigUIVariables();

            UpdateExperimenterDisplaySummaryStrings();
        });

        SetupTrial.SpecifyTermination(() => true, InitTrial);

        //------------------------------------------------------------------------------------------------------------------------
        if (Session.SessionDef.SelectionType?.ToLower() == "gaze")
            SelectionHandler = Session.SelectionTracker.SetupSelectionHandler("trial", "GazeShotgun", Session.GazeTracker, InitTrial, SearchDisplay);
        else
            SelectionHandler = Session.SelectionTracker.SetupSelectionHandler("trial", Session.SessionDef.SelectionType, Session.MouseTracker, InitTrial, SearchDisplay);
        
        TouchFBController.EnableTouchFeedback(SelectionHandler, CurrentTask.TouchFeedbackDuration, CurrentTask.TouchFeedbackSize, WM_CanvasGO);

        InitTrial.AddSpecificInitializationMethod(() =>
        {
            //Set timer duration for the trial:
            if (Session.SessionDef.IsHuman)
                Session.TimerController.SetDuration(selectObjectDuration.value);


            if (Session.WebBuild)
                TokenFBController.AdjustTokenBarSizing(110);

            if (Session.SessionDef.MacMainDisplayBuild & !Application.isEditor) //adj text positions if running build with mac as main display
                TokenFBController.AdjustTokenBarSizing(200);

            TokenFBController.SetRevealTime(tokenRevealDuration.value);
            TokenFBController.SetUpdateTime(tokenUpdateDuration.value);
            TokenFBController.SetFlashingTime(tokenFlashingDuration.value);

            if (SelectionHandler.AllChoices.Count > 0)
                SelectionHandler.ClearSelections();

            SelectionHandler.TimeBeforeChoiceStarts = Session.SessionDef.StartButtonSelectionDuration;
            SelectionHandler.TotalChoiceDuration = Session.SessionDef.StartButtonSelectionDuration;
        });

        InitTrial.SpecifyTermination(() => SelectionHandler.LastSuccessfulSelectionMatchesStartButton(), DisplaySample, () =>
        {
            SelectionHandler.TimeBeforeChoiceStarts = timeBeforeChoiceStarts.value;
            SelectionHandler.TotalChoiceDuration = totalChoiceDuration.value;

            //Set the token bar settings
            TokenFBController.enabled = true;
            SelectionHandler.HandlerActive = false;
           
            Session.EventCodeManager.SendCodeThisFrame("TokenBarVisible");
        });
        
        // Show the target/sample by itself for some time
        DisplaySample.AddTimer(() => CurrentTrial.DisplaySampleDuration, Delay, () =>
        {
            if (postSampleDistractorStims.stimDefs.Count != 0)
                StateAfterDelay = DisplayDistractors;
            else
                StateAfterDelay = SearchDisplay;
            DelayDuration = CurrentTrial.PostSampleDelayDuration;
        });

        // Show some distractors without the target/sample
        DisplayDistractors.AddTimer(() => CurrentTrial.DisplayPostSampleDistractorsDuration, Delay, () =>
          {
              StateAfterDelay = SearchDisplay;
              DelayDuration = CurrentTrial.PostSampleDelayDuration;
          });

        // Show the target/sample with some other distractors
        // Wait for a click and provide feedback accordingly
        SearchDisplay.AddSpecificInitializationMethod(() =>
        {
            Session.EventCodeManager.SendCodeThisFrame("TokenBarVisible");
            
            SelectionHandler.HandlerActive = true;

            choiceMade = false;
            ChoiceFailed_Trial = false;

            if (!Session.WebBuild)
                CreateTextOnExperimenterDisplay();

            if (SelectionHandler.AllChoices.Count > 0)
                SelectionHandler.ClearSelections();

            //reset it so the duration is 0 on exp display even if had one last trial
            OngoingSelection = null;
        });
        SearchDisplay.AddUpdateMethod(() =>
        {
            if (SelectionHandler.UnsuccessfulChoices.Count > 0 && !ChoiceFailed_Trial)
            {
                ChoiceFailed_Trial = true;
            }

            if (SelectionHandler.SuccessfulChoices.Count > 0)
            {
                selectedGO = SelectionHandler.LastSuccessfulChoice.SelectedGameObject;
                selectedSD = selectedGO?.GetComponent<StimDefPointer>()?.GetStimDef<WorkingMemory_StimDef>();
                SelectionHandler.ClearSelections();
                if (selectedSD != null)
                    choiceMade = true;
            }

            OngoingSelection = SelectionHandler.OngoingSelection;

            //Update Exp Display with OngoingSelection Duration:
            if (OngoingSelection != null)
            {
                SetTrialSummaryString();
            }
        });
        SearchDisplay.SpecifyTermination(() => choiceMade, SelectionFeedback, () =>
        {
            choiceMade = false;
            CorrectSelection = selectedSD.IsTarget;

            if (CorrectSelection)
            {       
                NumCorrect_InBlock++;
                CurrentTaskLevel.NumCorrect_InTask++;
                Session.EventCodeManager.SendCodeThisFrame("CorrectResponse");
            }
            else
            {
                NumErrors_InBlock++;
                CurrentTaskLevel.NumErrors_InTask++;
                Session.EventCodeManager.SendCodeThisFrame("IncorrectResponse");
            }

            if (selectedGO != null)
            {
                SelectedStimIndex = selectedSD.StimIndex;
                SelectedStimLocation = selectedSD.StimLocation;
            }
            Accuracy_InBlock = NumCorrect_InBlock/(TrialCount_InBlock + 1);
            UpdateExperimenterDisplaySummaryStrings();
        });
        SearchDisplay.SpecifyTermination(() => ChoiceFailed_Trial && !TouchFBController.FeedbackOn, ITI, () =>
        {
            AbortCode = 8;
            HandleAbortedTrialData();
        });
        SearchDisplay.AddTimer(() => selectObjectDuration.value, ITI, () =>
        {
            Session.EventCodeManager.SendCodeThisFrame("NoChoice");
            AbortCode = 6;
            AudioFBController.Play(Session.SessionDef.IsHuman ? "TimeRanOut" : "Negative");
            HandleAbortedTrialData();
        });

        SelectionFeedback.AddSpecificInitializationMethod(() =>
        {
            SearchDuration = SearchDisplay.TimingInfo.Duration;
            SearchDurations_InBlock.Add(SearchDuration);
            CurrentTaskLevel.SearchDurations_InTask.Add(SearchDuration);
            UpdateExperimenterDisplaySummaryStrings();

            int? depth = Session.Using2DStim ? 50 : (int?)null;

            if (CorrectSelection) 
                HaloFBController.ShowPositive(selectedGO, particleHaloActive: CurrentTrial.ParticleHaloActive, circleHaloActive: CurrentTrial.CircleHaloActive, depth: depth);
            else 
                HaloFBController.ShowNegative(selectedGO, particleHaloActive: CurrentTrial.ParticleHaloActive, circleHaloActive: CurrentTrial.CircleHaloActive, depth: depth);
        });

        SelectionFeedback.AddTimer(() => fbDuration.value, TokenFeedback, () => { HaloFBController.DestroyAllHalos(); });



        // The state that will handle the token feedback and wait for any animations
        TokenFeedback.AddSpecificInitializationMethod(() =>
        {
            /*if (!SessionValues.WebBuild)
            {
                if (playerViewParent.transform.childCount != 0)
                    DestroyChildren(playerViewParent.gameObject);
            }*/

            //searchStims.ToggleVisibility(false);
            if (selectedSD.IsTarget)
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

        ITI.AddSpecificInitializationMethod(() =>
        {
            if (CurrentTask.NeutralITI)
            {
                ContextName = "NeutralITI";
                CurrentTaskLevel.SetSkyBox(GetContextNestedFilePath(!string.IsNullOrEmpty(CurrentTask.ContextExternalFilePath) ? CurrentTask.ContextExternalFilePath : Session.SessionDef.ContextExternalFilePath, "NeutralITI"));
                Session.EventCodeManager.SendCodeThisFrame("ContextOff");
            }
        });
        // Wait for some time at the end
        ITI.AddTimer(() => itiDuration.value, FinishTrial, () =>
        {
            UpdateExperimenterDisplaySummaryStrings();
        });

        //---------------------------------ADD FRAME AND TRIAL DATA TO LOG FILES---------------------------------------
        DefineFrameData();
        DefineTrialData();
    }

    private void HandleAbortedTrialData()
    {
        SearchDurations_InBlock.Add(null);
        CurrentTaskLevel.SearchDurations_InTask.Add(null);
    }

    public override void OnTokenBarFull()
    {
        NumTokenBarFull_InBlock++;
        CurrentTaskLevel.NumTokenBarFull_InTask++;

        if(Session.SyncBoxController != null)
            StartCoroutine(Session.SyncBoxController.SendRewardPulses(CurrentTrial.NumPulses, CurrentTrial.PulseSize));

        CurrentTaskLevel.NumRewardPulses_InBlock += CurrentTrial.NumPulses;
        CurrentTaskLevel.NumRewardPulses_InTask += CurrentTrial.NumPulses;
        
    }

    //This method is for EventCodes and gets called automatically at end of SetupTrial:
    public override void AddToStimLists()
    {
        
        foreach (WorkingMemory_StimDef stim in searchStims.stimDefs)
        {
            if (stim.IsTarget)
                Session.TargetObjects.Add(stim.StimGameObject);
            else
                Session.DistractorObjects.Add(stim.StimGameObject);   
        }

        foreach (WorkingMemory_StimDef stim in postSampleDistractorStims.stimDefs)
        {
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
        if(!Session.WebBuild)
        {
            if (GameObject.Find("MainCameraCopy").transform.childCount != 0)
                DestroyChildren(GameObject.Find("MainCameraCopy"));
        }

        TokenFBController.enabled = false;


        if (AbortCode == 0)
            CurrentTaskLevel.SetBlockSummaryString();
        else
        {
            CurrentTaskLevel.NumAbortedTrials_InBlock++;
            CurrentTaskLevel.NumAbortedTrials_InTask++;
            CurrentTaskLevel.CurrentBlockSummaryString.Clear();
            CurrentTaskLevel.CurrentBlockSummaryString.AppendLine("");
        }
    }

    public void ResetBlockVariables()
    {
        SearchDurations_InBlock.Clear();
        NumErrors_InBlock = 0;
        NumCorrect_InBlock = 0;
        NumTokenBarFull_InBlock = 0;
        Accuracy_InBlock = 0;
        TotalTokensCollected_InBlock = 0;
    }

    protected override void DefineTrialStims()
    {
        //Define StimGroups consisting of StimDefs whose gameobjects will be loaded at TrialLevel_SetupTrial and 
        //destroyed at TrialLevel_Finish

        StimGroup group = Session.UsingDefaultConfigs ? PrefabStims : ExternalStims;

        searchStims = new StimGroup("SearchStims", group, CurrentTrial.SearchStimIndices);
        searchStims.SetVisibilityOnOffStates(GetStateFromName("SearchDisplay"), GetStateFromName("ITI"));
        searchStims.SetLocations(CurrentTrial.SearchStimLocations);
        TrialStims.Add(searchStims);

        List<StimDef> rewardedStimdefs = new List<StimDef>();

        sampleStim = new StimGroup("SampleStim", GetStateFromName("DisplaySample"), GetStateFromName("DisplaySample"));
        for (int iStim = 0; iStim < CurrentTrial.SearchStimIndices.Length; iStim++)
        {
            WorkingMemory_StimDef sd = (WorkingMemory_StimDef)searchStims.stimDefs[iStim];
            if (CurrentTrial.ProbabilisticSearchStimTokenReward != null)
                sd.StimTokenRewardMag = chooseReward(CurrentTrial.ProbabilisticSearchStimTokenReward[iStim]);
            else
                sd.StimTokenRewardMag = CurrentTrial.SearchStimTokenReward[iStim];

            if (sd.StimTokenRewardMag > 0)
            {
                WorkingMemory_StimDef newTarg = sd.CopyStimDef<WorkingMemory_StimDef>() as WorkingMemory_StimDef;
                sampleStim.AddStims(newTarg);
                newTarg.IsTarget = true;//Holds true if the target stim receives non-zero reward
                sd.IsTarget = true; //sets the isTarget value to true in the SearchStim Group
            } 
            else sd.IsTarget = false;
        }

        // for (int iT)
        sampleStim.SetLocations(new Vector3[]{ CurrentTrial.SampleStimLocation});
        TrialStims.Add(sampleStim);

        postSampleDistractorStims = new StimGroup("DisplayDistractors", group, CurrentTrial.PostSampleDistractorStimIndices);
        postSampleDistractorStims.SetVisibilityOnOffStates(GetStateFromName("DisplayDistractors"), GetStateFromName("DisplayDistractors"));
        postSampleDistractorStims.SetLocations(CurrentTrial.PostSampleDistractorStimLocations);
        TrialStims.Add(postSampleDistractorStims);
        
     }

    public void LoadConfigUIVariables()
    {   
        //config UI variables
        timeBeforeChoiceStarts = ConfigUiVariables.get<ConfigNumber>("timeBeforeChoiceStarts");
        totalChoiceDuration = ConfigUiVariables.get<ConfigNumber>("totalChoiceDuration"); 
        maxSearchDuration = ConfigUiVariables.get<ConfigNumber>("maxSearchDuration"); 
        selectObjectDuration = ConfigUiVariables.get<ConfigNumber>("selectObjectDuration");
        fbDuration = ConfigUiVariables.get<ConfigNumber>("fbDuration");
        itiDuration = ConfigUiVariables.get<ConfigNumber>("itiDuration");
        tokenRevealDuration = ConfigUiVariables.get<ConfigNumber>("tokenRevealDuration");
        tokenUpdateDuration = ConfigUiVariables.get<ConfigNumber>("tokenUpdateDuration");
        tokenFlashingDuration = ConfigUiVariables.get<ConfigNumber>("tokenFlashingDuration");

        tokenFbDuration = (tokenFlashingDuration.value + tokenUpdateDuration.value + tokenRevealDuration.value);//ensures full flashing duration within
        ////configured token fb duration
        configUIVariablesLoaded = true;
    }
    public override void ResetTrialVariables()
    {
        SelectedStimIndex = null;
        SelectedStimLocation = null;
        SearchDuration = 0;
        CorrectSelection = false;
        TouchDurationError = false;
        choiceMade = false;

        selectedGO = null;
        selectedSD = null;
    }
    private void DefineTrialData()
    {
        // All AddDatum commands for the Trial Data

        TrialData.AddDatum("TrialID", () => CurrentTrial.TrialID);
        TrialData.AddDatum("ContextName", () => ContextName);
        TrialData.AddDatum("SelectedStimCode", () => selectedSD?.StimCode ?? null);
        TrialData.AddDatum("SelectedLocation", () => selectedSD?.StimLocation ?? null);
        TrialData.AddDatum("CorrectSelection", () => CorrectSelection ? 1 : 0);
        TrialData.AddDatum("SearchDuration", ()=> SearchDuration);
    }
    private void DefineFrameData()
    {
        // All AddDatum commmands from the Frame Data
        FrameData.AddDatum("ContextName", () => ContextName);
        FrameData.AddDatum("ChoiceMade", () => choiceMade);
        FrameData.AddDatum("StartButtonVisibility", () => StartButton?.activeSelf);
        FrameData.AddDatum("DistractorStimVisibility", () => postSampleDistractorStims?.IsActive);
        FrameData.AddDatum("SearchStimVisibility", ()=> searchStims?.IsActive );
        FrameData.AddDatum("SampleStimVisibility", ()=> sampleStim?.IsActive );
    }
    void SetTrialSummaryString()
    {
        TrialSummaryString = "Selected Object Index: " + SelectedStimIndex +
                             "\nSelected Object Location: " + SelectedStimLocation +
                             "\nCorrect Selection: " + CorrectSelection +
                             "\nTouch Duration Error: " + TouchDurationError +
                             "\nSearch Duration: " + SearchDuration +
                             "\nToken Bar Value: " + TokenFBController.GetTokenBarValue() +
                             "\nOngoingSelection: " + (OngoingSelection == null ? "" : OngoingSelection.Duration.Value.ToString("F2") + " s");

    }
    private void CreateTextOnExperimenterDisplay()
    {
        if (!playerViewLoaded)
        {
            //Create corresponding text on player view of experimenter display
            foreach (WorkingMemory_StimDef stim in searchStims.stimDefs)
            {
                if (stim.IsTarget)
                {
                    textLocation = ScreenToPlayerViewPosition(Camera.main.WorldToScreenPoint(stim.StimLocation), playerViewParent.transform);
                    textLocation.y += 50;
                    Vector2 textSize = new Vector2(200, 200);
                    playerViewText = playerView.CreateTextObject("TargetText","TARGET",
                        Color.red, textLocation, textSize, playerViewParent.transform);
                    playerViewText.GetComponent<RectTransform>().localScale = new Vector3(2, 2, 0);
                    playerViewTextList.Add(playerViewText);
                    playerViewLoaded = true;
                }
            }
        }
    }
    private void UpdateExperimenterDisplaySummaryStrings()
    {
        if (TrialCount_InTask != 0)
            CurrentTaskLevel.SetTaskSummaryString();
        SetTrialSummaryString();
        CurrentTaskLevel.SetBlockSummaryString();
    }
}
