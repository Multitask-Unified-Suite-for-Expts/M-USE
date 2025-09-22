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


using System;
using System.Collections.Generic;
using UnityEngine;
using USE_States;
using WhatWhenWhere_Namespace;
using USE_StimulusManagement;
using ConfigDynamicUI;
using System.Linq;
using System.IO;
using USE_ExperimentTemplate_Trial;
using USE_ExperimentTemplate_Task;
using System.Collections;

public class WhatWhenWhere_TrialLevel : ControlLevel_Trial_Template
{
    public GameObject WWW_CanvasGO;
    public WhatWhenWhere_SequenceManager SequenceManager; // Set in Inspector;

    //This variable is required for most tasks, and is defined as the output of the GetCurrentTrialDef function 
    public WhatWhenWhere_TrialDef CurrentTrial => GetCurrentTrialDef<WhatWhenWhere_TrialDef>();
    public WhatWhenWhere_TaskLevel CurrentTaskLevel => GetTaskLevel<WhatWhenWhere_TaskLevel>();
    public WhatWhenWhere_TaskDef CurrentTask => GetTaskDef<WhatWhenWhere_TaskDef>();

    // Block Ending Variable
    public List<float?> runningPercentError = new List<float?>();
    public List<float?> runningErrorCount = new List<float?>();
    private float percentError;

    //stim group
    private StimGroup searchStims, distractorStims;


    public string ContextName;
    public List<int> runningAcc = new List<int>();
    private string selectionType;

    private int ruleAbidingErrors_InTrial;
    private int ruleBreakingErrors_InTrial;
    private int distractorRuleAbidingErrors_InTrial;
    private int backTrackErrors_InTrial;
    private int retouchErrors_InTrial;    
    private int perseverativeRuleBreakingErrors_InTrial;
    private int perseverativeRuleAbidingErrors_InTrial;
    private int perseverativeDistractorRuleAbidingErrors_InTrial;
    private int perseverativeBackTrackErrors_InTrial;
    private int perseverativeRetouchErrors_InTrial;
    private int correctSelections_InTrial;
    private int retouchCorrect_InTrial;
    private int totalErrors_InTrial;
    private int completedSequences_InTrial;
    private List<float?> searchDurations_InTrial = new List<float?>();

    [HideInInspector]
    public ConfigNumber flashingFbDuration;
    public ConfigNumber fbDuration;
    public ConfigNumber timeBeforeChoiceStarts;
    public ConfigNumber totalChoiceDuration;
    public ConfigNumber selectObjectDuration;
    public ConfigNumber itiDuration;
    public ConfigNumber sliderSize;
    public ConfigNumber chooseStimOnsetDelay; 
    public ConfigNumber startButtonDelay;
    public ConfigNumber timeoutDuration;


    //data logging variables
    private string searchStimsLocations, distractorStimsLocations;
    
    private float searchDurationStartTime;
    
    // misc variables
    private bool variablesLoaded;
    private bool choiceMade = false;
    private int sliderGainSteps, sliderLossSteps;
    

    //Player View Variables
    private PlayerViewPanel playerView;
    private GameObject playerViewParent; // Helps set things onto the player view in the experimenter display
    public List<GameObject> playerViewTextList;
    public GameObject playerViewText;
    private Vector2 textLocation;
    private bool playerViewLoaded;

    // Stimuli Variables
    private GameObject StartButton;
    private float startButtonPresentationDelay;

    private Vector3? MaskValues_CurrentTrial;

    private bool Masking = false;
    private int MaskingErrors_Trial;

    [HideInInspector] WhatWhenWhere_StimDef OngoingSelectionStim;




    public override void DefineControlLevel()
    {
        //---------------------------------------DEFINING STATES-----------------------------------------------------------------------
        State InitTrial = new State("InitTrial");
        State ChooseStimulus = new State("ChooseStimulus");
        State FlashNextCorrectStim = new State("FlashNextCorrectStim");
        State SelectionFeedback = new State("SelectionFeedback");
        State SliderFlashingFeedback = new State("SliderFlashingFeedback");
        State ITI = new State("ITI");

        AddActiveStates(new List<State>
        {
            InitTrial, ChooseStimulus, SelectionFeedback, SliderFlashingFeedback, ITI, FlashNextCorrectStim
        });

        string[] stateNames = new string[]
            {"InitTrial", "ChooseStimulus", "FlashNextCorrectStim", "SelectionFeedback", "SliderFlashingFeedback", "ITI"};

        Add_ControlLevel_InitializationMethod(() =>
        {
           if (SliderFBController != null && SliderFBController.SliderGO == null)
                    SliderFBController.InitializeSlider();
            

            HaloFBController.SetCircleHaloIntensity(5);

            //if (Session.SessionDef.IsHuman)
            //{
            //    Session.TimerController.CreateTimer(WWW_CanvasGO.transform);
            //    Session.TimerController.SetVisibilityOnOffStates(ChooseStimulus, ChooseStimulus);
            //}

            if (StartButton == null)
                InitializeStartButton(InitTrial, InitTrial);

            if (!Session.WebBuild) //player view variables
            {
                playerView = gameObject.AddComponent<PlayerViewPanel>();
                playerViewParent = GameObject.Find("MainCameraCopy");
            }

        });

        SetupTrial.AddSpecificInitializationMethod(() =>
        {
            SequenceManager.ResetNumCorrectChoices();

            if (!variablesLoaded)
            {
                variablesLoaded = true;
                LoadConfigUiVariables();
            }

            if (CurrentTrial.LeaveFeedbackOn)
                HaloFBController.SetLeaveFeedbackOn(true);

            //Set the Stimuli Light/Shadow settings
            SetShadowType(CurrentTask.ShadowType, "WhatWhenWhere_DirectionalLight");
            if (CurrentTask.StimFacingCamera)
                MakeStimFaceCamera();

            UpdateExperimenterDisplaySummaryStrings();

            // Determine Start Button onset if the participant has made consecutive errors that exceed the error threshold
            if (SequenceManager.GetBlockSpecificConsecutiveErrorCount() >= CurrentTrial.ErrorThreshold)
                startButtonPresentationDelay = timeoutDuration.value;
            else
                startButtonPresentationDelay = startButtonDelay.value;


            if(CurrentTrial.MaskValues != null)
            {
                Vector3 val = CurrentTrial.MaskValues.FirstOrDefault(num => num.x == TrialCount_InBlock + 1);
                if (val != null)
                    MaskValues_CurrentTrial = val;
                else
                    MaskValues_CurrentTrial = null;

            }

            CanStimulateThisTrial = false;
            if (CurrentTrial.TrialsToStimulateOn != null)
            {
                if (CurrentTrial.TrialsToStimulateOn.Contains(TrialCount_InBlock + 1) && !string.IsNullOrEmpty(CurrentTrial.StimulationType))
                    CanStimulateThisTrial = true;
            }

            if (CanStimulateThisTrial)
                Session.EventCodeManager.SendRangeCodeThisFrame("StimulationCondition", TrialStimulationCode);

        });

        SetupTrial.AddTimer(()=> startButtonPresentationDelay, InitTrial);

        //-------------------------------------------------------------------------------------------------------------------------------
        if (Session.SessionDef.SelectionType.ToLower().Contains("gaze"))
            SelectionHandler = Session.SelectionTracker.SetupSelectionHandler("trial", "GazeShotgun", Session.GazeTracker, InitTrial, ITI);
        else
            SelectionHandler = Session.SelectionTracker.SetupSelectionHandler("trial", Session.SessionDef.SelectionType, Session.MouseTracker, InitTrial, ITI);

        TouchFBController.EnableTouchFeedback(SelectionHandler, CurrentTask.TouchFeedbackDuration, CurrentTask.TouchFeedbackSize, WWW_CanvasGO);
        //-------------------------------------------------------------------------------------------------------------------------------

        InitTrial.AddSpecificInitializationMethod(() =>
        {
            SelectionHandler.HandlerActive = true; //Turn back on manually in case turned off after last selection

            //Set to start button durations
            SelectionHandler.TimeBeforeChoiceStarts = Session.SessionDef.StartButtonSelectionDuration;
            SelectionHandler.TotalChoiceDuration = Session.SessionDef.StartButtonSelectionDuration;

            SelectionHandler.MaxPixelDisplacement = 50;

            SelectionHandler.ClearChoices();

        });
        InitTrial.SpecifyTermination(() => SelectionHandler.LastSuccessfulSelectionMatchesStartButton(), Delay, ()=>
        {
            SelectionHandler.HandlerActive = false; //Turn OFF manually so cant make selections before choose stim state.

            //Set back to values for chooseStim state
            SelectionHandler.TimeBeforeChoiceStarts = timeBeforeChoiceStarts.value;
            SelectionHandler.TotalChoiceDuration = totalChoiceDuration.value;

            PrepareSliderForTrial();

            //Set timer duration for the trial:
            //if (Session.SessionDef.IsHuman)
            //    Session.TimerController.SetDuration(selectObjectDuration.value);

            DelayDuration = chooseStimOnsetDelay.value;
            if (CurrentTrial.GuidedSequenceLearning)
                StateAfterDelay = FlashNextCorrectStim;
            else
                StateAfterDelay = ChooseStimulus;
            RemoveRoughnessFromStimMaterial();


            //reset at start of each trial:
            foreach (WhatWhenWhere_StimDef stim in searchStims.stimDefs)
                stim.WasCorrectlyChosen = false;
        });
        
        FlashNextCorrectStim.AddSpecificInitializationMethod(() =>
        {
            if (SequenceManager.GetTargetStimGO() == null)
                SequenceManager.AssignStimClassifiers(CurrentTrial.CorrectObjectTouchOrder,searchStims, distractorStims);
            HaloFBController.StartFlashingHalo(1f, 2, SequenceManager.GetTargetStimGO());
        });
        
        FlashNextCorrectStim.SpecifyTermination(()=> !HaloFBController.GetIsFlashing(), ChooseStimulus);
       
        // Define ChooseStimulus state - Stimulus are shown and the user must select the correct object in the correct sequence
        ChooseStimulus.AddSpecificInitializationMethod(() =>
        {

            //For testing the dialogue controller:
            //DialogueController.CreateDialogueBox("Last Trial!", 500f);

            Input.ResetInputAxes(); //reset input in case they holding down

            if (SequenceManager.GetTargetStimGO() == null)
                SequenceManager.AssignStimClassifiers(CurrentTrial.CorrectObjectTouchOrder, searchStims, distractorStims);

            searchDurationStartTime = Time.time;

            if (!Session.WebBuild)
            {
                if(playerViewParent != null)
                {
                    if(playerViewParent.transform.childCount == 0)
                        CreateTextOnExperimenterDisplay();
                    else
                        Debug.LogWarning("MAINCAMERACOPY HAS " + playerViewParent.transform.childCount.ToString() + " CHILDREN ");
                }
                else
                    Debug.LogWarning("COULD NOT FIND MainCameraCopy GAME OBJECT");
            }


            //Create the masks if neccessary:
            if (MaskValues_CurrentTrial != null && MaskValues_CurrentTrial.HasValue)
            {
                if(SequenceManager.NumCorrectChoicesInTrial > 0)
                {
                    if (SequenceManager.NumCorrectChoicesInTrial >= MaskValues_CurrentTrial.Value.y - 1)
                    {
                        foreach (WhatWhenWhere_StimDef stim in searchStims.stimDefs)
                        {
                            if (!stim.WasCorrectlyChosen && stim.MaskGameObject == null)
                            {
                                MaskFBController.CreateMask(stim.StimGameObject, CurrentTrial.MaskColor, MaskValues_CurrentTrial.Value.z, CurrentTrial.MaskFadeInDuration);
                            }
                        };

                        foreach (WhatWhenWhere_StimDef distractorStim in distractorStims.stimDefs)
                        {
                            if(distractorStim.MaskGameObject == null)
                                MaskFBController.CreateMask(distractorStim.StimGameObject, CurrentTrial.MaskColor, MaskValues_CurrentTrial.Value.z, CurrentTrial.MaskFadeInDuration);
                        }
                    }
                }
            }


            StimulatedThisTrial = false; //have to reset after every object for WWW
            StimulateOnCurrentObject = false;

            if (CanStimulateThisTrial)
            {
                int index = Array.IndexOf(CurrentTrial.TrialsToStimulateOn, TrialCount_InBlock + 1);
                int[] trialObjectsToStimOn = CurrentTrial.ObjectsToStimulateOn[index];

                if (trialObjectsToStimOn != null && trialObjectsToStimOn.Contains(SequenceManager.GetSeqIdx() + 1))
                {
                    StimulateOnCurrentObject = true;
                }
            }


            choiceMade = false;
            ChoiceFailed_Trial = false;


            SelectionHandler.HandlerActive = true; //Turn back on manually in case turned off after last selection

            SelectionHandler.ClearChoices();

            OngoingSelection = null; //reset internal WWW ongoingSelection
        });
        ChooseStimulus.AddUpdateMethod(() =>
        {
            OngoingSelection = SelectionHandler.OngoingSelection;


            if (OngoingSelection != null && StimulateOnCurrentObject && !StimulatedThisTrial)
            {
                if (OngoingSelection.Duration >= CurrentTrial.InitialFixationDuration)
                {
                    GameObject ongoingSelectionGO = OngoingSelection.SelectedGameObject;
                    OngoingSelectionStim = ongoingSelectionGO.GetComponent<StimDefPointer>()?.GetStimDef<WhatWhenWhere_StimDef>();

                    if (OngoingSelectionStim != null)
                    {
                        string stimulationType = CurrentTrial.StimulationType.Trim();
                        if (stimulationType == "FixationChoice_Target" && OngoingSelectionStim.IsCurrentTarget)
                            StartCoroutine(StimulationCoroutine());
                        else if (stimulationType == "FixationChoice_Distractor" && OngoingSelectionStim.IsDistractor)
                            StartCoroutine(StimulationCoroutine());
                    }

                }
            }



            if (SelectionHandler.SuccessfulChoices.Count > 0)
            {
                GameObject selectedGO = SelectionHandler.LastSuccessfulChoice.SelectedGameObject.transform.root.gameObject;
                WhatWhenWhere_StimDef selectedSD = selectedGO?.GetComponent<StimDefPointer>()?.GetStimDef<WhatWhenWhere_StimDef>();

                if (selectedSD != null)
                {
                    choiceMade = true;
                    Debug.LogWarning("--- WWW CHOICE MADE ---");

                    SequenceManager.SetSelectedGO(selectedGO);
                    SequenceManager.SetSelectedSD(selectedSD);
                    if (!SequenceManager.GetStartedSequence())
                    {
                        SequenceManager.SetStartedSequence(true);
                        SequenceManager.SetSequenceStartTime(Time.time);
                    }
                }
                else
                    Debug.LogWarning("SUCCESSFULLY SELECTED SOMETHING THAT WAS NOT A STIM *********");

            }
            else if (SelectionHandler.UnsuccessfulChoices.Count > 0 && !ChoiceFailed_Trial)
            {
                GameObject selectedGO = SelectionHandler.LastUnsuccessfulChoice.SelectedGameObject.transform.root.gameObject;
                WhatWhenWhere_StimDef selectedSD = selectedGO?.GetComponent<StimDefPointer>()?.GetStimDef<WhatWhenWhere_StimDef>();

                if (selectedSD != null)
                {
                    ChoiceFailed_Trial = true;
                    Debug.LogWarning("--- WWW CHOICE FAILED (trial aborted) DUE TO NOT SELECTING LONG ENOUGH ---");
                }
                else
                    Debug.LogWarning("SELECTED SOMETHING THAT WAS NOT A STIM ***********");
            }



            SetTrialSummaryString(); //Update Exp Display with OngoingSelection Duration:
        });
        ChooseStimulus.SpecifyTermination(() => choiceMade, SelectionFeedback, () =>
        {
            SelectionHandler.HandlerActive = false; //Turn OFF manually so cant make additional selections


            HandleSearchDurationData(Time.time - searchDurationStartTime);
        });
        ChooseStimulus.SpecifyTermination(() => ChoiceFailed_Trial, ITI, () =>
        {
            SelectionHandler.HandlerActive = false; //Turn OFF manually so cant make additional selections

            AbortCode = 8;
            HandleAbortedTrialData();
        });
        ChooseStimulus.AddTimer(() => selectObjectDuration.value, ITI, () =>
        {
            SelectionHandler.HandlerActive = false; //Turn OFF manually so cant make additional selections

            Session.EventCodeManager.SendCodeThisFrame("NoChoice");
            AbortCode = 6;
            HandleAbortedTrialData();
            AudioFBController.Play(Session.SessionDef.IsHuman ? "TimeRanOut" : "Negative");
        });

        SelectionFeedback.AddSpecificInitializationMethod(() =>
        {
            SequenceManager.ManageSelection();
            ManageDataHandlers();


            int? depth = Session.Using2DStim ? 50 : (int?)null;
            int? stimIdx = null;

            GameObject selectedGO = SequenceManager.GetSelectedGO();
            WhatWhenWhere_StimDef selectedSD = SequenceManager.GetSelectedSD();

            if (selectedSD.IsDistractor)
                stimIdx = Array.IndexOf(CurrentTrial.DistractorStimIndices, selectedSD.StimIndex); // used to index through the arrays in the config file/mapping different columns
            else
                stimIdx = Array.IndexOf(CurrentTrial.SearchStimIndices, selectedSD.StimIndex);


            //Destroy the mask once they click it so that they can see their choice (and the pos/neg halo)
            if (selectedSD.MaskGameObject != null)
            {
                MaskFBController.DestroyMask(selectedSD.MaskGameObject);
            }

            if (selectionType.ToLower().Contains("correct"))
            {
                //Set the stim to ChosenCorrectly:
                selectedSD.WasCorrectlyChosen = true;

                AudioFBController.Play("Positive");
                HaloFBController.ShowPositive(selectedGO, particleHaloActive: CurrentTrial.ParticleHaloActive, circleHaloActive: true, destroyTime: CurrentTrial.LeaveFeedbackOn ?  (float?)null : (CurrentTrial.ParticleHaloActive ? 0.76f : 1.06f), depth: depth);

                if(SequenceManager.GetFinishedSequence())
                    SliderFBController.UpdateSliderValue(1);
                else
                    SliderFBController.UpdateSliderValue(CurrentTrial.SliderGain[(int)stimIdx] * (1f / sliderGainSteps));
            }
            else //Chose Incorrect
            {
                AudioFBController.Play("Negative");

                if (selectionType.ToLower().Contains("retoucherror"))
                {
                    HaloFBController.ShowNegative(selectedGO, particleHaloActive: CurrentTrial.ParticleHaloActive, circleHaloActive:true, destroyTime: CurrentTrial.ParticleHaloActive ? 0.76f : 1.26f, depth: depth);
                    SequenceManager.ResetSelectionClassifications();
                    return;
                }
                else if (selectionType.ToLower().Contains("backtrackerror"))
                    HaloFBController.ShowNegative(selectedGO, particleHaloActive: CurrentTrial.ParticleHaloActive, circleHaloActive:true, destroyTime: CurrentTrial.ParticleHaloActive ? 0.76f : 1.26f, depth: depth);
                else
                    HaloFBController.ShowNegative(selectedGO, particleHaloActive: CurrentTrial.ParticleHaloActive, circleHaloActive: true, destroyTime: CurrentTrial.ParticleHaloActive ? 0.76f : 1.26f, depth: depth);


                if (CurrentTrial.LeaveFeedbackOn && SequenceManager.GetTrialSpecificConsecutiveErrorCount() == 1 && SequenceManager.GetSelectedFirstStimInSequence())
                    SequenceManager.GetLastCorrectStimGO().GetComponent<CircleHalo>()?.DeactivateInstantiatedCircleHalo();

                if (SliderFBController.GetSliderValue() != 0 && SequenceManager.GetTrialSpecificConsecutiveErrorCount() == 1)
                {
                    SliderFBController.UpdateSliderValue(-CurrentTrial.SliderLoss[(int)stimIdx] * (1f / sliderLossSteps));
                    
                }
            }

            SequenceManager.ResetSelectionClassifications();
        });
         
        SelectionFeedback.AddTimer(()=> fbDuration.value, Delay, () =>
        {
            DelayDuration = 0;
            
            if (!CurrentTrial.LeaveFeedbackOn) 
                HaloFBController.DestroyAllHalos();
            
            // If the sequence has been completed, send to slider feedback state
            if (SequenceManager.GetFinishedSequence())
                StateAfterDelay = SliderFlashingFeedback;
            else
            {
                // If there is a MaxTrialErrors defined in the BlockDef and the number of errors in the trial exceed that value, send to ITI
                if((Masking && MaskingErrors_Trial -1 == CurrentTrial.MaskErrorsAllowed_Trial) || (CurrentTrial.MaxTrialErrors != null && totalErrors_InTrial >= CurrentTrial.MaxTrialErrors))
                {
                    StateAfterDelay = ITI;
                }

                // If there is either no MaxTrialErrors or the error threshold hasn't been met, either move onto the next stim in the sequence or terminate the trial for an incorrect choice
                else if (CurrentTrial.BlockEndType == "SimpleThreshold")
                {
                    if (selectionType.ToLower().Contains("correct"))
                    {
                        if (CurrentTrial.GuidedSequenceLearning)
                            StateAfterDelay = FlashNextCorrectStim;
                        else
                            StateAfterDelay = ChooseStimulus;
                    }
                    else
                        StateAfterDelay = ITI;
                }

                // If there is either no MaxTrialErrors or the error threshold hasn't been met, move onto the next stim in the sequence (aborting is handled in ChooseStim.AddTimer)
                else if (CurrentTrial.BlockEndType.Contains("CurrentTrial"))
                {
                    if (CurrentTrial.GuidedSequenceLearning || (SequenceManager.GetTrialSpecificConsecutiveErrorCount() >= 2 && SequenceManager.GetSelectedFirstStimInSequence()))
                        StateAfterDelay = FlashNextCorrectStim;
                    else
                        StateAfterDelay = ChooseStimulus;
                }
            }

            UpdateExperimenterDisplaySummaryStrings();
        });
        SliderFlashingFeedback.AddSpecificInitializationMethod(() =>
        {
            //Destroy all created text objects on Player View of Experimenter Display
            if(!Session.WebBuild)
                DestroyChildren(GameObject.Find("MainCameraCopy"));

            HandleCompletedSequence();
        });
        SliderFlashingFeedback.AddTimer(() => flashingFbDuration.value, ITI, () =>
        {
            Session.EventCodeManager.SendCodeThisFrame("ContextOff");
            
            CurrentTaskLevel.SetBlockSummaryString();
        });

        //Define iti state
        ITI.AddSpecificInitializationMethod(() =>
        {
            if (CurrentTask.NeutralITI)
            {
                ContextName = "NeutralITI";
                string path = !string.IsNullOrEmpty(CurrentTask.ContextExternalFilePath) ? CurrentTask.ContextExternalFilePath : Session.SessionDef.ContextExternalFilePath;
                CurrentTaskLevel.SetSkyBox(path + Path.DirectorySeparatorChar + "NeutralITI" + ".png");
            }

            // GenerateAccuracyLog();
        });
        ITI.AddTimer(() => itiDuration.value, FinishTrial);
        //------------------------------------------------------------------------ADDING VALUES TO DATA FILE--------------------------------------------------------------------------------------------------------------------------------------------------------------

        DefineTrialData();
        DefineFrameData();
    }


    private void HandleCompletedSequence()
    {
        runningAcc.Add(1);
        completedSequences_InTrial += 1;
        CurrentTaskLevel.CompletedSequences_InBlock++;
        CurrentTaskLevel.CompletedSequences_InTask++;

        percentError = (float)decimal.Divide(totalErrors_InTrial, CurrentTrial.CorrectObjectTouchOrder.Length);
        runningPercentError.Add(percentError);

        runningErrorCount.Add(totalErrors_InTrial);

        if (Session.SyncBoxController != null)
        {
            StartCoroutine(Session.SyncBoxController.SendRewardPulses(CurrentTrial.NumPulses, CurrentTrial.PulseSize));
            CurrentTaskLevel.NumRewardPulses_InBlock += CurrentTrial.NumPulses;
            CurrentTaskLevel.NumRewardPulses_InTask += CurrentTrial.NumPulses;
        }
    }
    private void HandleSearchDurationData(float? searchDuration)
    {
        searchDurations_InTrial.Add(searchDuration);
        CurrentTaskLevel.SearchDurations_InBlock.Add(searchDuration);
        CurrentTaskLevel.SearchDurations_InTask.Add(searchDuration);
        searchDurationStartTime = 0;
    }
    //This method is for EventCodes and gets called automatically at end of SetupTrial:
    private void HandleAbortedTrialData()
    {
        HandleSearchDurationData(null);
        runningAcc.Add(0);

        runningPercentError.Add(null);
        runningErrorCount.Add(null);
    }
    public override void AddToStimLists()
    {
        foreach (WhatWhenWhere_StimDef stim in searchStims.stimDefs)
            Session.TargetObjects.Add(stim.StimGameObject);
        
        foreach (WhatWhenWhere_StimDef stim in distractorStims.stimDefs)
            Session.DistractorObjects.Add(stim.StimGameObject);
    }

    protected override bool CheckBlockEnd()
    {
        // If there is a MaxCorrectTrials defined, end the block when the minimum number of trials is run and the maximum number of correct trials is achieved
        if (CurrentTrial.MaxCorrectTrials != 0)
            return ( TrialCount_InBlock >= CurrentTaskLevel.MinTrials_InBlock && runningAcc.Count(num => num == 1) >= CurrentTrial.MaxCorrectTrials);
        
        // If using the SimpleThreshold block end, use the following CheckBlockEnd method
        if (CurrentTrial.BlockEndType == "SimpleThreshold")
            return CurrentTaskLevel.TaskLevel_Methods.CheckBlockEnd(CurrentTrial.BlockEndType, runningAcc,
                CurrentTrial.BlockEndThreshold, CurrentTrial.BlockEndWindow, CurrentTaskLevel.MinTrials_InBlock,
                CurrentTrial.MaxTrials);

        // If using the CurrentTrialPercentError block end, use the following CheckBlockEnd method
        if (CurrentTrial.BlockEndType == "CurrentTrialPercentError")
            return CurrentTaskLevel.TaskLevel_Methods.CheckBlockEnd(CurrentTrial.BlockEndType, runningPercentError,
                CurrentTrial.BlockEndThreshold, CurrentTaskLevel.MinTrials_InBlock,
                CurrentTaskLevel.MaxTrials_InBlock);
        
        // If using the CurrentTrialErrorCount block end, use the following CheckBlockEnd method
        if (CurrentTrial.BlockEndType == "CurrentTrialErrorCount")
            return CurrentTaskLevel.TaskLevel_Methods.CheckBlockEnd(CurrentTrial.BlockEndType, runningErrorCount,
                CurrentTrial.BlockEndThreshold, CurrentTaskLevel.MinTrials_InBlock,
                CurrentTaskLevel.MaxTrials_InBlock);

         
        Debug.Log($"Cannot handle {CurrentTrial.BlockEndType} Block End Type. Forced block switch not applied.");
         return false;
        
         
    }
    public override void FinishTrialCleanup()
    {
        MaskFBController.DestroyAllMasks();

        if(!Session.WebBuild)
        {
            if (playerViewParent.transform.childCount != 0)
                DestroyChildren(GameObject.Find("MainCameraCopy"));
        }

        searchStims.ToggleVisibility(false);
        distractorStims.ToggleVisibility(false);
        SliderFBController.SliderGO.SetActive(false);
        SliderFBController.SliderHaloGO.SetActive(false);

        
        if(AbortCode == 0)
            CurrentTaskLevel.SetBlockSummaryString();
        else
        {
            CurrentTaskLevel.NumAbortedTrials_InBlock++;
            CurrentTaskLevel.NumAbortedTrials_InTask++;
        }
            
    }

    public void MakeStimFaceCamera()
    {
        if(TrialStims != null && TrialStims.Count > 0)
        {
            foreach (StimGroup group in TrialStims)
            {
                foreach (var stim in group.stimDefs)
                {
                    if(stim.StimGameObject != null)
                        stim.StimGameObject.transform.LookAt(Camera.main.transform);
                }
            }
        }
    }

    private void AdjustQuaddleBodyRoughness(float roughnessValue, GameObject parentGameObject)
    {
        // Recursively search for the "Body" and "Head" GameObject among all children
        FindAndSetRoughnessRecursively(roughnessValue, parentGameObject.transform);
    }

    private Transform FindTransformRecursively(string transformName, Transform parentTransform)
    {
        Transform transform = parentTransform.Find(transformName);
        foreach (Transform childTransform in parentTransform)
        {
            Transform foundTransform = FindTransformRecursively(transformName, childTransform);
            if (foundTransform != null)
                return foundTransform;
        }
        return transform;
    }

    private void FindAndSetRoughnessRecursively(float roughnessValue, Transform parentTransform)
    {
        // Check if the current transform has a child named "Body" and "Head"
        Transform bodyTransform = FindTransformRecursively("Body", parentTransform);
        Transform headTransform = FindTransformRecursively("Head", parentTransform);

        if (bodyTransform != null)
        {
            MeshRenderer bodyRenderer = bodyTransform.GetComponent<MeshRenderer>();
            Material bodyMaterial = bodyRenderer.material;
            bodyMaterial.SetFloat("roughnessFactor", roughnessValue);
        }
        if (headTransform != null)
        {
            MeshRenderer headRenderer = headTransform.GetComponent<MeshRenderer>();
            Material headMaterial = headRenderer.material;
            headMaterial.SetFloat("roughnessFactor", roughnessValue);
        }

    }

    private void RemoveRoughnessFromStimMaterial()
    {
        foreach (var sd in searchStims.stimDefs)
            AdjustQuaddleBodyRoughness(0, sd.StimGameObject);
        foreach (var sd in distractorStims.stimDefs)
            AdjustQuaddleBodyRoughness(0, sd.StimGameObject);
    }

    public override void ResetTrialVariables()
    {
        MaskingErrors_Trial = 0;
        Masking = false;
        searchDurationStartTime = 0;
        sliderGainSteps = 0;
        sliderLossSteps = 0;
        choiceMade = false;
        SequenceManager?.ResetSequenceManagerVariables();

        searchDurations_InTrial.Clear();
        ruleBreakingErrors_InTrial = 0;
        ruleAbidingErrors_InTrial = 0;
        distractorRuleAbidingErrors_InTrial = 0;
        backTrackErrors_InTrial = 0;
        retouchErrors_InTrial = 0;
        correctSelections_InTrial = 0;
        retouchCorrect_InTrial = 0;
        totalErrors_InTrial = 0;
        completedSequences_InTrial = 0;
        SliderFBController.ResetSliderBarFull();
    }

    


    //-----------------------------------------------------------------METHODS FOR DATA HANDLING----------------------------------------------------------------------
    private void DefineTrialData() //All ".AddDatum" commands for Trial Data
    {
        TrialData.AddDatum("TrialID", () => CurrentTrial.TrialID); //NaN if only using blockdef structure
        TrialData.AddDatum("ContextName", () => CurrentTrial.ContextName);
        TrialData.AddDatum("SearchStimLocations", () => searchStimsLocations);
        TrialData.AddDatum("DistractorStimLocations", () => distractorStimsLocations);
        TrialData.AddDatum("TouchedObjects", () => string.Join(",", SequenceManager.GetAllSelectedSDs().Select(sd => sd.StimIndex)));
        TrialData.AddDatum("SelectionClassifications", () => string.Join(",", SequenceManager.GetAllSelectionClassifications()));
        TrialData.AddDatum("SearchDurations", () => String.Join(",",searchDurations_InTrial));
        TrialData.AddDatum("RuleBreakingErrors", () => ruleBreakingErrors_InTrial);
        TrialData.AddDatum("RuleAbidingErrors", () => ruleAbidingErrors_InTrial);
        TrialData.AddDatum("DistractorRuleAbidingErrors", () => distractorRuleAbidingErrors_InTrial);
        TrialData.AddDatum("BackTrackErrors", () => backTrackErrors_InTrial);
        TrialData.AddDatum("RetouchErrors", () => retouchErrors_InTrial);
        TrialData.AddDatum("PerseverativeRuleBreakingErrors", () => perseverativeRuleBreakingErrors_InTrial);
        TrialData.AddDatum("PerseverativeRuleAbidingErrors", () => perseverativeRuleAbidingErrors_InTrial);
        TrialData.AddDatum("PerseverativeDistractorRuleAbidingErrors", () => perseverativeDistractorRuleAbidingErrors_InTrial);
        TrialData.AddDatum("PerseverativeBackTrackErrors", () => perseverativeBackTrackErrors_InTrial);
        TrialData.AddDatum("PerseverativeRetouchErrors", () => perseverativeRetouchErrors_InTrial);
        TrialData.AddDatum("CorrectSelections", () => correctSelections_InTrial);
        TrialData.AddDatum("RetouchCorrect", () => retouchCorrect_InTrial);
        TrialData.AddDatum("TotalErrors", () => totalErrors_InTrial);
    }
    private void DefineFrameData() //All ".AddDatum" commands for Frame Data
    {
        FrameData.AddDatum("OngoingSelectionDuration", () => OngoingSelection == null ? "" : OngoingSelection.Duration.Value.ToString("F2") + " s");
        FrameData.AddDatum("StartButton", () => StartButton != null && StartButton.activeSelf ? "Active" : "NotActive");
        FrameData.AddDatum("SearchStimuliShown", () => searchStims?.IsActive);
        FrameData.AddDatum("DistractorStimuliShown", () => distractorStims?.IsActive);
        FrameData.AddDatum("MaskVisibilities", () => MaskFBController.GetMaskVisibilityString());
        FrameData.AddDatum("MaskPositions", () => MaskFBController.GetMaskPosString());

    }

    private void SetTrialSummaryString()
    {
        TrialSummaryString = "Selected Object Indices: " + string.Join(",",SequenceManager.GetAllSelectedSDs().Select(obj => obj.StimIndex)) +
                             "\nSelection Type : " + selectionType +
                             "\nLast Trial's Percent Error : " + percentError +
                             "\nTotal Correct Selections in Trial : " + correctSelections_InTrial +
                             "\nTotal Errors in Trial : " + totalErrors_InTrial +
                             "\nAvg Search Duration: " + CurrentTaskLevel.CalculateAverageDuration(searchDurations_InTrial) +
                             "\nOngoingSelection: " + (OngoingSelection == null ? "" : OngoingSelection.Duration.Value.ToString("F2") + " s");

    }


    private void CreateTextOnExperimenterDisplay()
    {
        Debug.LogWarning("CREATING TEXT ON EXPERIMENTER DISPLAY");

        for (int iStim = 0; iStim < CurrentTrial.CorrectObjectTouchOrder.Length; ++iStim)
        {
            //Create corresponding text on player view of experimenter display
            textLocation = ScreenToPlayerViewPosition(Camera.main.WorldToScreenPoint(searchStims.stimDefs[iStim].StimLocation), playerViewParent.transform);

            if (!Session.SessionDef.SelectionType.ToLower().Contains("gaze"))
                textLocation.y += 75;

            if(iStim == 1)
                Debug.LogWarning("TEXT POSITION = " + textLocation.ToString());

            playerViewText = playerView.CreateTextObject(CurrentTrial.CorrectObjectTouchOrder[iStim].ToString(),
                (CurrentTrial.CorrectObjectTouchOrder[iStim]).ToString(),
                Color.red, textLocation, new Vector2(200, 200), playerViewParent.transform);
            playerViewText.SetActive(true);
            playerViewText.GetComponent<RectTransform>().localScale = new Vector3(2, 2, 0);
        }
    }
    void LoadConfigUiVariables()
    {
        //config UI variables
        timeBeforeChoiceStarts = ConfigUiVariables.get<ConfigNumber>("timeBeforeChoiceStarts");
        totalChoiceDuration = ConfigUiVariables.get<ConfigNumber>("totalChoiceDuration");
        itiDuration = ConfigUiVariables.get<ConfigNumber>("itiDuration");
        sliderSize = ConfigUiVariables.get<ConfigNumber>("sliderSize");
        selectObjectDuration = ConfigUiVariables.get<ConfigNumber>("selectObjectDuration");
        flashingFbDuration = ConfigUiVariables.get<ConfigNumber>("finalFbDuration");
        fbDuration = ConfigUiVariables.get<ConfigNumber>("fbDuration");
        chooseStimOnsetDelay = ConfigUiVariables.get<ConfigNumber>("chooseStimOnsetDelay");
        timeoutDuration = ConfigUiVariables.get<ConfigNumber>("timeoutDuration");
        startButtonDelay = ConfigUiVariables.get<ConfigNumber>("startButtonDelay");
    }
    //-----------------------------------------------------DEFINE QUADDLES-------------------------------------------------------------------------------------

    protected override void DefineTrialStims()
    {
        searchStims = SetupStimGroup("SearchStims", CurrentTrial.SearchStimIndices);
        distractorStims = SetupStimGroup("DistractorStims", CurrentTrial.DistractorStimIndices);

        if (CurrentTrial.RandomizedLocations)
        {
            //Combine the lists and randomize them:
            Vector3[] allLocationsRandomized = CurrentTrial.SearchStimLocations.Concat(CurrentTrial.DistractorStimLocations).ToArray().OrderBy(x => Guid.NewGuid()).ToArray();

            int numSearchStim = CurrentTrial.SearchStimIndices.Count();

            Vector3[] searchStimLocations = allLocationsRandomized.Take(numSearchStim).ToArray();
            Vector3[] distractorStimLocations = allLocationsRandomized.Skip(numSearchStim).ToArray();

            searchStims.SetLocations(searchStimLocations);
            distractorStims.SetLocations(distractorStimLocations);
        }
        else
        {
            searchStims.SetLocations(CurrentTrial.SearchStimLocations);
            distractorStims.SetLocations(CurrentTrial.DistractorStimLocations);
        }



        TrialStims.Add(searchStims);
        TrialStims.Add(distractorStims);

        searchStimsLocations = string.Join(",", searchStims.stimDefs.Select(s => s.StimLocation));
        distractorStimsLocations = string.Join(",", distractorStims.stimDefs.Select(d => d.StimLocation));
    }

    private StimGroup SetupStimGroup(string name, IEnumerable<int> indices)
    {
        var stimGroup = new StimGroup(name, Session.UsingDefaultConfigs ? PrefabStims : ExternalStims, indices);

        if (CurrentTrial.GuidedSequenceLearning)
            stimGroup.SetVisibilityOnOffStates(GetStateFromName("FlashNextCorrectStim"), GetStateFromName("ITI"));
        else
            stimGroup.SetVisibilityOnOffStates(GetStateFromName("ChooseStimulus"), GetStateFromName("ITI"));
       
        return stimGroup;
    }
    
    //-------------------------------------------------------------MISCELLANEOUS METHODS--------------------------------------------------------------------------
    

    private void CalculateSliderSteps()
    {
        //Configure the Slider Steps for each Stim
        foreach (int sliderGain in CurrentTrial.SliderGain)
        {
            sliderGainSteps += sliderGain;
        }
        sliderGainSteps += CurrentTrial.SliderInitialValue;
        foreach (int sliderLoss in CurrentTrial.SliderLoss)
        {
            sliderLossSteps += sliderLoss;
        }
        sliderLossSteps += CurrentTrial.SliderInitialValue;
    }

    private void InitializeStartButton(State visOnState, State visOffState)
    {
        if (Session.SessionDef.IsHuman)
        {
            StartButton = Session.HumanStartPanel.StartButtonGO;
            Session.HumanStartPanel.SetVisibilityOnOffStates(visOnState, visOffState);
        }
        else
        {
            StartButton = Session.USE_StartButton.CreateStartButton(WWW_CanvasGO.GetComponent<Canvas>(), CurrentTask.StartButtonPosition, CurrentTask.StartButtonScale);
            Session.USE_StartButton.SetVisibilityOnOffStates(visOnState, visOffState);
        }
    }


    private void UpdateExperimenterDisplaySummaryStrings()
    {
        CurrentTaskLevel.SetBlockSummaryString();
        SetTrialSummaryString();
        if (TrialCount_InTask != 0)
            CurrentTaskLevel.SetTaskSummaryString();
    }
    private void PrepareSliderForTrial()
    {
        CalculateSliderSteps();
        SliderFBController.ConfigureSlider(sliderSize.value, CurrentTrial.SliderInitialValue * (1f / sliderGainSteps));
        SliderFBController.SetSliderRectSize(new Vector2(400f, 25f));
        SliderFBController.SliderGO.SetActive(true);
        SliderFBController.SetUpdateDuration(fbDuration.value);
        SliderFBController.SetFlashingDuration(flashingFbDuration.value);
    }
    private void HandleRuleBreakingErrorData()
    {
        if (Session.SessionDef.EventCodesActive)
            Session.EventCodeManager.SendCodeThisFrame(TaskEventCodes["RuleBreakingError"]);

        ruleBreakingErrors_InTrial++;
        CurrentTaskLevel.RuleBreakingErrors_InBlock++;
        CurrentTaskLevel.RuleBreakingErrors_InTask++;
    }
    private void HandleRuleAbidingErrorData()
    {
        if (Session.SessionDef.EventCodesActive)
            Session.EventCodeManager.SendCodeThisFrame(TaskEventCodes["RuleAbidingError"]);

        ruleAbidingErrors_InTrial++;
        CurrentTaskLevel.RuleAbidingErrors_InBlock++;
        CurrentTaskLevel.RuleAbidingErrors_InTask++;
    }
    private void HandleDistractorRuleAbidingErrorData()
    {
        if (Session.SessionDef.EventCodesActive)
            Session.EventCodeManager.SendCodeThisFrame(TaskEventCodes["DistractorRuleAbidingError"]);

        ruleAbidingErrors_InTrial++;
        CurrentTaskLevel.RuleAbidingErrors_InBlock++;
        CurrentTaskLevel.RuleAbidingErrors_InTask++;
    }

    private void HandleBackTrackErrorData()
    {
        Session.EventCodeManager.SendCodeThisFrame(TaskEventCodes["BackTrackError"]);

        backTrackErrors_InTrial++;
        CurrentTaskLevel.BackTrackErrors_InBlock++;
        CurrentTaskLevel.BackTrackErrors_InTask++;
    }
    private void HandleRetouchErrorData()
    {
        if (Session.SessionDef.EventCodesActive)
            Session.EventCodeManager.SendCodeThisFrame(TaskEventCodes["RetouchError"]);

        retouchErrors_InTrial++;
        CurrentTaskLevel.RetouchErrors_InBlock++;
        CurrentTaskLevel.RetouchErrors_InTask++;
    }

    private void HandleRetouchCorrectData()
    {
        if (Session.SessionDef.EventCodesActive)
            Session.EventCodeManager.SendCodeThisFrame(TaskEventCodes["RetouchCorrect"]);

        retouchCorrect_InTrial++;
        CurrentTaskLevel.RetouchCorrect_InBlock++;
        CurrentTaskLevel.RetouchCorrect_InTask++;
    }

    private void HandleCorrectSelectionData()
    {
        if (Session.SessionDef.EventCodesActive)
            Session.EventCodeManager.SendCodeThisFrame("CorrectResponse");

        correctSelections_InTrial++;
        CurrentTaskLevel.CorrectSelections_InBlock++;
        CurrentTaskLevel.CorrectSelections_InTask++;
    }

    private void HandlePerseverativeRetouchErrorData()
    {
        perseverativeRetouchErrors_InTrial++;
        CurrentTaskLevel.PerseverativeRetouchErrors_InBlock++;
        CurrentTaskLevel.PerseverativeRetouchErrors_InTask++;
    }
    private void HandlePerseverativeBackTrackErrorData()
    {
        perseverativeBackTrackErrors_InTrial++;
        CurrentTaskLevel.PerseverativeBackTrackErrors_InBlock++;
        CurrentTaskLevel.PerseverativeBackTrackErrors_InTask++;
    }
    private void HandlePerseverativeRuleBreakingErrorData()
    {
        perseverativeRuleBreakingErrors_InTrial++;
        CurrentTaskLevel.PerseverativeRuleBreakingErrors_InBlock++;
        CurrentTaskLevel.PerseverativeRuleBreakingErrors_InTask++;
    }
    private void HandlePerseverativeRuleAbidingErrorData()
    {
        perseverativeRuleAbidingErrors_InTrial++;
        CurrentTaskLevel.PerseverativeRuleAbidingErrors_InBlock++;
        CurrentTaskLevel.PerseverativeRuleAbidingErrors_InTask++;
    }
    private void HandlePerseverativeDistractorRuleAbidingErrorData()
    {
        perseverativeDistractorRuleAbidingErrors_InTrial++;
        CurrentTaskLevel.PerseverativeDistractorRuleAbidingErrors_InBlock++;
        CurrentTaskLevel.PerseverativeDistractorRuleAbidingErrors_InTask++;
    }

    private void HandleErrorData()
    {
        totalErrors_InTrial++;
        CurrentTaskLevel.TotalErrors_InBlock++;
        CurrentTaskLevel.TotalErrors_InTask++;
    }
    private void ManageDataHandlers()
    {
        selectionType = SequenceManager.DetermineErrorType();
        switch (selectionType)
        {
            case "retouchCorrect":
                HandleRetouchCorrectData();
                HandleCorrectSelectionData();
                break;
            case "correctSelection":
                HandleCorrectSelectionData();
                break;
            case "backTrackError":
                HandleBackTrackErrorData();
                HandleRuleBreakingErrorData();
                HandleErrorData();
                break;
            case "retouchError":
                HandleRetouchErrorData();
                HandleErrorData();
                break;
            case "ruleAbidingError":
                HandleRuleAbidingErrorData();
                HandleErrorData();
                break;
            case "ruleBreakingError":
                HandleRuleBreakingErrorData();
                HandleErrorData();
                break;
            case "distractorRuleAbidingError":
                HandleDistractorRuleAbidingErrorData();
                HandleErrorData();
                break;
            case "perseverativeBackTrackError":
                HandleBackTrackErrorData();
                HandlePerseverativeBackTrackErrorData();
                HandlePerseverativeRuleBreakingErrorData();
                HandleRuleBreakingErrorData();
                HandleErrorData();
                break;
            case "perseverativeRetouchError":
                HandlePerseverativeRetouchErrorData();
                HandleRetouchErrorData();
                HandleErrorData();
                break;
            case "perseverativeRuleBreakingError":
                HandlePerseverativeRuleBreakingErrorData();
                HandleRuleBreakingErrorData();
                HandleErrorData();
                break;
            case "perseverativeRuleAbidingError":
                HandlePerseverativeRuleAbidingErrorData();
                HandleRuleAbidingErrorData();
                HandleErrorData();
                break;
            case "perseverativeDistractorRuleAbidingError":
                HandlePerseverativeDistractorRuleAbidingErrorData();
                HandleDistractorRuleAbidingErrorData();
                HandleErrorData();
                break;

        }



    }
}














