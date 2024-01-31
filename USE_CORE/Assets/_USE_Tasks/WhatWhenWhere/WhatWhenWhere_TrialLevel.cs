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


public class WhatWhenWhere_TrialLevel : ControlLevel_Trial_Template
{
    public GameObject WWW_CanvasGO;

    //This variable is required for most tasks, and is defined as the output of the GetCurrentTrialDef function 
    public WhatWhenWhere_TrialDef CurrentTrialDef => GetCurrentTrialDef<WhatWhenWhere_TrialDef>();
    public WhatWhenWhere_TaskLevel CurrentTaskLevel => GetTaskLevel<WhatWhenWhere_TaskLevel>();
    public WhatWhenWhere_TaskDef currentTaskDef => GetTaskDef<WhatWhenWhere_TaskDef>();

    // Block Ending Variable
    public List<float?> runningPercentError = new List<float?>();
    public List<float?> runningErrorCount = new List<float?>();
    private float percentError;

    //stim group
    private StimGroup searchStims, distractorStims;
    private List<int> TouchedObjects = new List<int>();

    // feedback variables
    public int numTouchedStims = 0;
    private bool trialComplete = false;
    private GameObject targetStimGameObject;

    
    //Trial Data Logging variables
    private string selectionClassification = "";
    private List<string> selectionClassifications_InTrial = new List<string>();
    public int consecutiveError = 0;
    private List<float?> SearchDurations_InTrial = new List<float?> { };
    public List<int> runningAcc = new List<int>();
    public string ContextName;
    private bool retouchErroneous = false;
    private int retouchErroneousCounter_InTrial;
    private bool retouchCorrect;
    private int retouchCorrectCounter_InTrial;
    private int NumErrors_InTrial;
    private int NumCorrect_InTrial;

    [HideInInspector]
    public ConfigNumber flashingFbDuration;
    public ConfigNumber fbDuration;
    public ConfigNumber minObjectTouchDuration;
    public ConfigNumber maxObjectTouchDuration;
    public ConfigNumber selectObjectDuration;
    public ConfigNumber itiDuration;
    public ConfigNumber sliderSize;
    public ConfigNumber chooseStimOnsetDelay; 
    public ConfigNumber startButtonDelay;
    public ConfigNumber timeoutDuration;


    //data logging variables
    private string searchStimsLocations, distractorStimsLocations;
    
    private float searchDuration = 0;
    private float sbDelay = 0;
    
    // misc variables
    private bool variablesLoaded;
    private int correctIndex;
    public int NumSliderBarFilled = 0;
    private int sliderGainSteps, sliderLossSteps;
    private bool isSliderValueIncrease = false;
    private int perseverationCounter_InTrial;
    private bool startedSequence;
    

    //Player View Variables
    private PlayerViewPanel playerView;
    private GameObject playerViewParent; // Helps set things onto the player view in the experimenter display
    public List<GameObject> playerViewTextList;
    public GameObject playerViewText;
    private Vector2 textLocation;
    private bool playerViewLoaded;

    // Stimuli Variables
    private GameObject StartButton;

    // Stim Evaluation Variables
    private GameObject selectedGO = null;
    private WhatWhenWhere_StimDef selectedSD = null;
    private GameObject LastCorrectStimGO;

    private bool CorrectSelection;
    private int? stimIdx; // used to index through the arrays in the config file/mapping different columns
    private bool choiceMade = false;

    private SelectionTracking.SelectionTracker.SelectionHandler ShotgunHandler;

    public override void DefineControlLevel()
    {
        //---------------------------------------DEFINING STATES-----------------------------------------------------------------------
        State InitTrial = new State("InitTrial");
        State ChooseStimulus = new State("ChooseStimulus");
        State FlashNextCorrectStim = new State("FlashNextCorrectStim");
        State SelectionFeedback = new State("SelectionFeedback");
        State FinalFeedback = new State("FinalFeedback");
        State ITI = new State("ITI");
        AddActiveStates(new List<State>
        {
            InitTrial, ChooseStimulus, SelectionFeedback, FinalFeedback, ITI, FlashNextCorrectStim
        });

        string[] stateNames = new string[]
            {"InitTrial", "ChooseStimulus", "FlashNextCorrectStim", "SelectionFeedback", "FinalFeedback", "ITI"};

        Add_ControlLevel_InitializationMethod(() =>
        {
            SliderFBController.InitializeSlider();
            
            
            // Initialize FB Controller Values
            HaloFBController.SetCircleHaloSize(3);
            HaloFBController.SetCircleHaloIntensity(5);
            
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
            if (!variablesLoaded)
            {
                variablesLoaded = true;
                LoadConfigUiVariables();

            }

            //Set the Stimuli Light/Shadow settings
            SetShadowType(currentTaskDef.ShadowType, "WhatWhenWhere_DirectionalLight");
            if (currentTaskDef.StimFacingCamera)
                MakeStimFaceCamera();

            UpdateExperimenterDisplaySummaryStrings();

            // Determine Start Button onset if the participant has made consecutive errors that exceed the error threshold
            if (consecutiveError >= CurrentTrialDef.ErrorThreshold)
                sbDelay = timeoutDuration.value;
            else
                sbDelay = startButtonDelay.value;
        });

        SetupTrial.AddTimer(()=> sbDelay, InitTrial);

        ShotgunHandler = Session.SelectionTracker.SetupSelectionHandler("trial", "TouchShotgun", Session.MouseTracker, InitTrial, FinalFeedback);

        TouchFBController.EnableTouchFeedback(ShotgunHandler, currentTaskDef.TouchFeedbackDuration, currentTaskDef.StartButtonScale * 10, WWW_CanvasGO, true);

        InitTrial.AddSpecificInitializationMethod(() => InitializeShotgunHandler());
        InitTrial.SpecifyTermination(() => ShotgunHandler.LastSuccessfulSelectionMatchesStartButton(), Delay, ()=>
        {
            CalculateSliderSteps();
            SliderFBController.ConfigureSlider(sliderSize.value, CurrentTrialDef.SliderInitialValue*(1f/sliderGainSteps));
            SliderFBController.SliderGO.SetActive(true);
            SliderFBController.SetUpdateDuration(fbDuration.value);
            SliderFBController.SetFlashingDuration(flashingFbDuration.value);
            
            DelayDuration = chooseStimOnsetDelay.value;
            if (CurrentTrialDef.GuidedSequenceLearning)
                StateAfterDelay = FlashNextCorrectStim;
            else
                StateAfterDelay = ChooseStimulus;
            
        });
        
        FlashNextCorrectStim.AddSpecificInitializationMethod(() =>
        {
            //previousTargetStimGameObject = targetStimGameObject;
            AssignCorrectStim();
            Debug.LogWarning("IN THE FLASH NEXT CORRECT STIM INIT");
            HaloFBController.StartFlashingHalo(1f, 2, targetStimGameObject);
        });
        
        FlashNextCorrectStim.SpecifyTermination(()=> !HaloFBController.GetIsFlashing(), ChooseStimulus);
       
        // Define ChooseStimulus state - Stimulus are shown and the user must select the correct object in the correct sequence
        ChooseStimulus.AddSpecificInitializationMethod(() =>
        {
            if (!CurrentTrialDef.GuidedSequenceLearning)
                AssignCorrectStim();
           
            searchDuration = 0;

            if(!Session.WebBuild)
            {
                if (GameObject.Find("MainCameraCopy").transform.childCount == 0)
                    CreateTextOnExperimenterDisplay();
            }

            choiceMade = false;
            if (CurrentTrialDef.LeaveFeedbackOn)
                HaloFBController.SetLeaveFeedbackOn(true);

            ShotgunHandler.HandlerActive = true;
            if (ShotgunHandler.AllSelections.Count > 0)
                ShotgunHandler.ClearSelections();
        });
        ChooseStimulus.AddUpdateMethod(() =>
        {
            searchDuration += Time.deltaTime;
            if (ShotgunHandler.SuccessfulSelections.Count > 0)
            {
                selectedGO = ShotgunHandler.LastSuccessfulSelection.SelectedGameObject;
                selectedSD = selectedGO?.GetComponent<StimDefPointer>()?.GetStimDef<WhatWhenWhere_StimDef>();
                ShotgunHandler.ClearSelections();
                if (selectedSD != null)
                    choiceMade = true;
            }
        });
        ChooseStimulus.SpecifyTermination(()=> choiceMade, SelectionFeedback, ()=>
        {
            CorrectSelection = selectedSD.IsCurrentTarget;

            if (CorrectSelection)
            {
                startedSequence = true;

                selectionClassification = "Correct";
                // UpdateCounters_Correct();
                if (selectedGO == LastCorrectStimGO)
                {
                    retouchCorrect = true;
                    retouchCorrectCounter_InTrial++;
                    CurrentTaskLevel.retouchCorrectCounter_InTask++;
                    selectionClassification = "RetouchCorrect";
                }

                LastCorrectStimGO = selectedGO;
                CurrentTaskLevel.NumCorrectSelections_InBlock++;
                NumCorrect_InTrial++;
                isSliderValueIncrease = true;
                Session.EventCodeManager.AddToFrameEventCodeBuffer("CorrectResponse");
                
            }
            else
            {
                
                isSliderValueIncrease = false;
                Session.EventCodeManager.AddToFrameEventCodeBuffer("IncorrectResponse");
                //Repetition Error
                if (TouchedObjects.Contains(selectedSD.StimIndex))
                {
                    CurrentTaskLevel.RepetitionErrorCount_InBlock++;
                    selectionClassification = "RepetitionError";
                    Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["RepetitionError"]);

                    if (selectedGO == LastCorrectStimGO && consecutiveError == 0)
                    {
                        retouchErroneous = true;
                        retouchErroneousCounter_InTrial++;
                        CurrentTaskLevel.retouchErroneousCounter_InTask++;
                        selectionClassification = "RetouchErroneous";
                    }

                }
                
                // Slot Errors
                else
                {
                    //Distractor Error
                    if (selectedSD.IsDistractor)
                    {
                        CurrentTaskLevel.DistractorSlotErrorCount_InBlock++;
                        selectionClassification = "DistractorSlotError";
                        Session.EventCodeManager.AddToFrameEventCodeBuffer("Button0PressedOnDistractorObject");//SELECTION STUFF (code may not be exact and/or could be moved to Selection handler)
                    }
                    //Stimuli Slot error
                    else
                    {
                        CurrentTaskLevel.SlotErrorCount_InBlock++;
                        selectionClassification = "SlotError";
                        Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["SlotError"]);
                    }
                }
                
                runningAcc.Add(0);
                if (!retouchErroneous)
                {
                    CurrentTaskLevel.NumErrors_InBlock++;
                    NumErrors_InTrial++;
                }
            
            }
            selectionClassifications_InTrial.Add(selectionClassification);

            UpdateExperimenterDisplaySummaryStrings();
        });
        ChooseStimulus.AddTimer(() => selectObjectDuration.value, ITI, () =>
        {
            Session.EventCodeManager.AddToFrameEventCodeBuffer("NoChoice");
            Session.EventCodeManager.SendRangeCode("CustomAbortTrial", AbortCodeDict["NoSelectionMade"]);
            AbortCode = 6;

            consecutiveError++;
            runningAcc.Add(0);
            SearchDurations_InTrial.Add(null);
            CurrentTaskLevel.SearchDurations_InBlock.Add(null);
            CurrentTaskLevel.SearchDurations_InTask.Add(null);
            selectionClassification = "AbortedTrial";
            selectionClassifications_InTrial.Add(selectionClassification);
            
            runningPercentError.Add(null);
            runningErrorCount.Add(null);
        });
        // ChooseStimulus.SpecifyTermination(() => trialComplete, FinalFeedback);

        SelectionFeedback.AddSpecificInitializationMethod(() =>
        {
            ShotgunHandler.HandlerActive = false;
            TouchedObjects.Add(selectedSD.StimIndex);
            SearchDurations_InTrial.Add(searchDuration);
            CurrentTaskLevel.SearchDurations_InBlock.Add(searchDuration);
            CurrentTaskLevel.SearchDurations_InTask.Add(searchDuration);
           // totalFbDuration = (fbDuration.value + flashingFbDuration.value);
            
            int? depth = Session.Using2DStim ? 50 : (int?)null;

            if (CorrectSelection)
            {
                consecutiveError = 0;
                // Only show positive if there isn't an existing halo around the object
                
                HaloFBController.ShowPositive(selectedGO, depth);
                Debug.LogWarning("IS THIS BEING REGISTERED AS A CORRECT SELECTION? ");
                if (HaloFBController.GetNegativeCircleHalos().Count > 0)
                    HaloFBController.DestroyNegativeCircleHalos();
                

                SliderFBController.UpdateSliderValue(CurrentTrialDef.SliderGain[numTouchedStims]*(1f/sliderGainSteps));
                numTouchedStims += 1;
                if (numTouchedStims == CurrentTrialDef.CorrectObjectTouchOrder.Length)
                    trialComplete = true;
                
                selectionClassification = "None";
            }
            else //Chose Incorrect
            {
                AudioFBController.Play("Negative");
                if (retouchErroneous)
                    return;
                
                
                // ERRONEOUS RETOUCH LAST CORRECT doesn't INCREMENT CONSECUTIVE ERROR
                if (consecutiveError >= 1)
                {
                    perseverationCounter_InTrial++;
                    CurrentTaskLevel.perseverationCounter_InTask++;
                }
                consecutiveError++;
                
                    HaloFBController.ShowNegative(selectedGO, depth);
                if (selectedSD.IsDistractor)
                    stimIdx = Array.IndexOf(CurrentTrialDef.DistractorStimIndices, selectedSD.StimIndex); // used to index through the arrays in the config file/mapping different columns
                else
                    stimIdx = Array.IndexOf(CurrentTrialDef.SearchStimIndices, selectedSD.StimIndex);

                
                if (CurrentTrialDef.BlockEndType.Contains("CurrentTrial") && numTouchedStims != 0 && consecutiveError == 1)
                {
                    SliderFBController.UpdateSliderValue(-CurrentTrialDef.SliderLoss[(int)stimIdx]*(1f/sliderLossSteps)); // NOT IMPLEMENTED: NEEDS TO CONSIDER SEPARATE LOSS/GAIN FOR DISTRACTOR & TARGET STIMS SEPARATELY
                    numTouchedStims -= 1;
                }
                else if (CurrentTrialDef.BlockEndType == "SimpleThreshold")
                    SliderFBController.UpdateSliderValue(-CurrentTrialDef.SliderLoss[(int)stimIdx]*(1f/sliderLossSteps)); // NOT IMPLEMENTED: NEEDS TO CONSIDER SEPARATE LOSS/GAIN FOR DISTRACTOR & TARGET STIMS SEPARATELy
            }
            
            
            selectedGO = null;
        });
         
        SelectionFeedback.AddTimer(()=> fbDuration.value, Delay, () =>
        {
            DelayDuration = 0;
            
            if (!CurrentTrialDef.LeaveFeedbackOn) 
                HaloFBController.DestroyAllHalos();
            

            // If the sequence has been completed, send to slider feedback state
            if (trialComplete)
                StateAfterDelay = FinalFeedback;
            else
            {
                // If there is a MaxTrialErrors defined in the BlockDef and the number of errors in the trial exceed that value, send to ITI
                if(CurrentTrialDef.MaxTrialErrors != null && NumErrors_InTrial >= CurrentTrialDef.MaxTrialErrors)
                {
                    StateAfterDelay = ITI;
                }

                // If there is either no MaxTrialErrors or the error threshold hasn't been met, either move onto the next stim in the sequence or terminate the trial for an incorrect choice
                else if (CurrentTrialDef.BlockEndType == "SimpleThreshold")
                {
                    if (CorrectSelection)
                    {
                        if (CurrentTrialDef.GuidedSequenceLearning)
                            StateAfterDelay = FlashNextCorrectStim;
                        else
                            StateAfterDelay = ChooseStimulus;
                    }
                    else
                        StateAfterDelay = ITI;
                }

                // If there is either no MaxTrialErrors or the error threshold hasn't been met, move onto the next stim in the sequence (aborting is handled in ChooseStim.AddTimer)
                else if (CurrentTrialDef.BlockEndType.Contains("CurrentTrial"))
                {
                    if (CurrentTrialDef.GuidedSequenceLearning || (consecutiveError >= 1 && startedSequence))
                        StateAfterDelay = FlashNextCorrectStim;
                    else
                        StateAfterDelay = ChooseStimulus;
                }
            }

            CorrectSelection = false;
            
            retouchErroneous = false;
            retouchCorrect = false;
            
            UpdateExperimenterDisplaySummaryStrings();
        });
        FinalFeedback.AddSpecificInitializationMethod(() =>
        {
            ShotgunHandler.HandlerActive = false;
            
            trialComplete = false;
            selectionClassification = "None";

            //Destroy all created text objects on Player View of Experimenter Display
            if(!Session.WebBuild)
                DestroyChildren(GameObject.Find("MainCameraCopy"));

            runningAcc.Add(1);
            NumSliderBarFilled += 1;
            CurrentTaskLevel.NumSliderBarFilled_InTask++;
            
            percentError = (float)decimal.Divide(NumErrors_InTrial, CurrentTrialDef.CorrectObjectTouchOrder.Length);
            runningPercentError.Add(percentError);
            
            runningErrorCount.Add(NumErrors_InTrial);
                                    
            if (Session.SyncBoxController != null)
            {
                Session.SyncBoxController.SendRewardPulses(CurrentTrialDef.NumPulses, CurrentTrialDef.PulseSize); 
                CurrentTaskLevel.NumRewardPulses_InBlock += CurrentTrialDef.NumPulses;
                CurrentTaskLevel.NumRewardPulses_InTask += CurrentTrialDef.NumPulses;
            }
           
        });
        FinalFeedback.AddTimer(() => flashingFbDuration.value, ITI, () =>
        {
            Session.EventCodeManager.AddToFrameEventCodeBuffer("ContextOff");
            
            CurrentTaskLevel.SetBlockSummaryString();
        });

        //Define iti state
        ITI.AddSpecificInitializationMethod(() =>
        {
            float latestAccuracy;

            if (runningAcc.Count > 10)
            {
                latestAccuracy = ((runningAcc.Skip(Math.Max(0, runningAcc.Count - 10)).Sum() / 10f)*100);
                if (latestAccuracy > 70 && CurrentTaskLevel.LearningSpeed == -1)
                    CurrentTaskLevel.LearningSpeed = TrialCount_InBlock + 1;
            }
            
            if (currentTaskDef.NeutralITI)
            {
                ContextName = "NeutralITI";
                string path = !string.IsNullOrEmpty(currentTaskDef.ContextExternalFilePath) ? currentTaskDef.ContextExternalFilePath : Session.SessionDef.ContextExternalFilePath;
                CurrentTaskLevel.SetSkyBox(path + Path.DirectorySeparatorChar + "NeutralITI" + ".png");
            }

            // GenerateAccuracyLog();
        });
        ITI.AddTimer(() => itiDuration.value, FinishTrial);
        //------------------------------------------------------------------------ADDING VALUES TO DATA FILE--------------------------------------------------------------------------------------------------------------------------------------------------------------

        DefineTrialData();
        DefineFrameData();
    }


    //This method is for EventCodes and gets called automatically at end of SetupTrial:
    public override void AddToStimLists()
    {
        foreach (WhatWhenWhere_StimDef stim in searchStims.stimDefs)
            Session.TargetObjects.Add(stim.StimGameObject);
        
        foreach (WhatWhenWhere_StimDef stim in distractorStims.stimDefs)
            Session.DistractorObjects.Add(stim.StimGameObject);
    }

    protected override bool CheckBlockEnd()
    {
        TaskLevelTemplate_Methods TaskLevel_Methods = new TaskLevelTemplate_Methods();
       
        // If there is a MaxCorrectTrials defined, end the block when the minimum number of trials is run and the maximum number of correct trials is achieved
        if (CurrentTrialDef.MaxCorrectTrials != 0)
            return ( TrialCount_InBlock >= CurrentTaskLevel.MinTrials_InBlock && runningAcc.Count(num => num == 1) >= CurrentTrialDef.MaxCorrectTrials);
        
        // If using the SimpleThreshold block end, use the following CheckBlockEnd method
        if (CurrentTrialDef.BlockEndType == "SimpleThreshold")
            return TaskLevel_Methods.CheckBlockEnd(CurrentTrialDef.BlockEndType, runningAcc,
                CurrentTrialDef.BlockEndThreshold, CurrentTrialDef.BlockEndWindow, CurrentTaskLevel.MinTrials_InBlock,
                CurrentTrialDef.MaxTrials);

        // If using the CurrentTrialPercentError block end, use the following CheckBlockEnd method
        if (CurrentTrialDef.BlockEndType == "CurrentTrialPercentError")
            return TaskLevel_Methods.CheckBlockEnd(CurrentTrialDef.BlockEndType, runningPercentError,
                CurrentTrialDef.BlockEndThreshold, CurrentTaskLevel.MinTrials_InBlock,
                CurrentTaskLevel.MaxTrials_InBlock);
        
        // If using the CurrentTrialErrorCount block end, use the following CheckBlockEnd method
        if (CurrentTrialDef.BlockEndType == "CurrentTrialErrorCount")
            return TaskLevel_Methods.CheckBlockEnd(CurrentTrialDef.BlockEndType, runningErrorCount,
                CurrentTrialDef.BlockEndThreshold, CurrentTaskLevel.MinTrials_InBlock,
                CurrentTaskLevel.MaxTrials_InBlock);

         
        Debug.Log($"Cannot handle {CurrentTrialDef.BlockEndType} Block End Type. Forced block switch not applied.");
         return false;
        
         
    }
    public override void FinishTrialCleanup()
    {
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
        foreach (StimGroup group in TrialStims)
        foreach (var stim in group.stimDefs)
        {
            stim.StimGameObject.transform.LookAt(Camera.main.transform);
        }
    }

    public override void ResetTrialVariables()
    {
        NumErrors_InTrial = 0;
        NumCorrect_InTrial = 0;

        retouchCorrectCounter_InTrial = 0;
        retouchErroneousCounter_InTrial = 0;
        perseverationCounter_InTrial = 0;
        numTouchedStims = 0;
        searchDuration = 0;
        sliderGainSteps = 0;
        sliderLossSteps = 0;
        stimIdx = null;
        selectedGO = null;
        selectedSD = null;
        LastCorrectStimGO = null;
        CorrectSelection = false;
        choiceMade = false;
        retouchErroneous = false;
        retouchCorrect = false;
        startedSequence = false;
        perseverationCounter_InTrial = 0;
        SearchDurations_InTrial.Clear();
        TouchedObjects.Clear();
        selectionClassification = "";
        selectionClassifications_InTrial.Clear();
        SliderFBController.ResetSliderBarFull();
    }

    


    //-----------------------------------------------------------------METHODS FOR DATA HANDLING----------------------------------------------------------------------
    private void DefineTrialData() //All ".AddDatum" commands for Trial Data
    {
        TrialData.AddDatum("TrialID", () => CurrentTrialDef.TrialID); //NaN if only using blockdef structure
        TrialData.AddDatum("ContextName", () => CurrentTrialDef.ContextName);
        TrialData.AddDatum("SearchStimLocations", () => searchStimsLocations);
        TrialData.AddDatum("DistractorStimLocations", () => distractorStimsLocations);
        TrialData.AddDatum("TouchedObjects", () => String.Join(",",TouchedObjects));
        TrialData.AddDatum("SelectionClassifications", () => String.Join(",", selectionClassifications_InTrial));
        TrialData.AddDatum("SearchDurations", () => String.Join(",",SearchDurations_InTrial));
        TrialData.AddDatum("NumErrors", () => NumErrors_InTrial);
        TrialData.AddDatum("RetouchCorrect", () => retouchCorrectCounter_InTrial);
        TrialData.AddDatum("RetouchErroneous", () => retouchErroneousCounter_InTrial);
    }
    private void DefineFrameData() //All ".AddDatum" commands for Frame Data
    {
        FrameData.AddDatum("ContextName", () => ContextName);
        FrameData.AddDatum("StartButton", () => StartButton != null && StartButton.activeSelf ? "Active" : "NotActive");
        FrameData.AddDatum("SearchStimuliShown", () => searchStims?.IsActive);
        FrameData.AddDatum("DistractorStimuliShown", () => distractorStims?.IsActive);
    }

    private void SetTrialSummaryString()
    {
        TrialSummaryString = "Selected Object Indices: " + string.Join(",",TouchedObjects) +
                             "\nCorrect Selection? : " + CorrectSelection +
                             "\nLast Trial's Percent Error : " + percentError +
                             "\nNum Errors in Trial : " + NumErrors_InTrial +
                             "\n" +
                             "\nError: " + selectionClassification +
                             "\n" +
                             "\nAvg Search Duration: " + CurrentTaskLevel.CalculateAverageDuration(SearchDurations_InTrial) +
                             "\nRetouch Correct: " + retouchCorrectCounter_InTrial +
                             "\nRetouch Erroneous: " + retouchErroneousCounter_InTrial;
    }

    
    private void CreateTextOnExperimenterDisplay()
    {
        for (int iStim = 0; iStim < CurrentTrialDef.CorrectObjectTouchOrder.Length; ++iStim)
        {
            //Create corresponding text on player view of experimenter display
            textLocation = ScreenToPlayerViewPosition(Camera.main.WorldToScreenPoint(searchStims.stimDefs[iStim].StimLocation), playerViewParent.transform);
            textLocation.y += 75;
            playerViewText = playerView.CreateTextObject(CurrentTrialDef.CorrectObjectTouchOrder[iStim].ToString(),
                CurrentTrialDef.CorrectObjectTouchOrder[iStim].ToString(),
                Color.red, textLocation, new Vector2(200, 200), playerViewParent.transform);
            playerViewText.SetActive(true);
            playerViewText.GetComponent<RectTransform>().localScale = new Vector3(2, 2, 0);
        }
    }
    void LoadConfigUiVariables()
    {
        //config UI variables
        minObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("minObjectTouchDuration");
        
        maxObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("maxObjectTouchDuration");
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
        StimGroup group = Session.UsingDefaultConfigs ? PrefabStims : ExternalStims;

        //Define StimGroups consisting of StimDefs whose gameobjects will be loaded at TrialLevel_SetupTrial and 
        //destroyed at TrialLevel_Finish
        //StimGroup constructor which creates a subset of an already-existing StimGroup 
        searchStims = new StimGroup("SearchStims", group, CurrentTrialDef.SearchStimIndices);
        distractorStims = new StimGroup("DistractorStims", group, CurrentTrialDef.DistractorStimIndices);

        if (CurrentTrialDef.GuidedSequenceLearning)
        {
            searchStims.SetVisibilityOnOffStates(GetStateFromName("FlashNextCorrectStim"), GetStateFromName("ITI"));
            distractorStims.SetVisibilityOnOffStates(GetStateFromName("FlashNextCorrectStim"), GetStateFromName("ITI"));
        }
        else
        {
            searchStims.SetVisibilityOnOffStates(GetStateFromName("ChooseStimulus"), GetStateFromName("ITI"));
            distractorStims.SetVisibilityOnOffStates(GetStateFromName("ChooseStimulus"), GetStateFromName("ITI"));
        }
        
        TrialStims.Add(searchStims);
        TrialStims.Add(distractorStims);
        
        if (CurrentTrialDef.RandomizedLocations)
        {
            var totalStims = searchStims.stimDefs.Concat(distractorStims.stimDefs);
            var stimLocations = CurrentTrialDef.SearchStimLocations.Concat(CurrentTrialDef.DistractorStimLocations);

            int[] positionIndexArray = Enumerable.Range(0, stimLocations.Count()).ToArray();
            System.Random random = new System.Random();
            positionIndexArray = positionIndexArray.OrderBy(x => random.Next()).ToArray();

            for (int i = 0; i < totalStims.Count(); i++)
            {
                totalStims.ElementAt(i).StimLocation = stimLocations.ElementAt(positionIndexArray[i]);
            }
        }
        else
        {
            searchStims.SetLocations(CurrentTrialDef.SearchStimLocations);
            distractorStims.SetLocations(CurrentTrialDef.DistractorStimLocations);
        }

        searchStimsLocations = String.Join(",", searchStims.stimDefs.Select(s => s.StimLocation));
        distractorStimsLocations = String.Join(",", distractorStims.stimDefs.Select(d => d.StimLocation));
    }
    
    //-------------------------------------------------------------MISCELLANEOUS METHODS--------------------------------------------------------------------------
    private void AssignCorrectStim()
    {
        targetStimGameObject = null;
        
        //if we haven't finished touching all stims
        if (numTouchedStims < CurrentTrialDef.CorrectObjectTouchOrder.Length)
        {
            //find which stimulus is currently target
            correctIndex = CurrentTrialDef.CorrectObjectTouchOrder[numTouchedStims] - 1;

            for (int iStim = 0; iStim < CurrentTrialDef.CorrectObjectTouchOrder.Length; iStim++)
            {
                WhatWhenWhere_StimDef sd = (WhatWhenWhere_StimDef) searchStims.stimDefs[iStim];
                if (iStim == correctIndex)
                {
                    sd.IsCurrentTarget = true;
                    targetStimGameObject = sd.StimGameObject;
                }
                else 
                    sd.IsCurrentTarget = false;
            }
        
            for (int iDist = 0; iDist < CurrentTrialDef.DistractorStimIndices.Length; ++iDist)
            {
                WhatWhenWhere_StimDef sd = (WhatWhenWhere_StimDef) distractorStims.stimDefs[iDist];
                sd.IsDistractor = true;
            }
        }
    }

    private void CalculateSliderSteps()
    {
        //Configure the Slider Steps for each Stim
        foreach (int sliderGain in CurrentTrialDef.SliderGain)
        {
            sliderGainSteps += sliderGain;
        }
        sliderGainSteps += CurrentTrialDef.SliderInitialValue;
        foreach (int sliderLoss in CurrentTrialDef.SliderLoss)
        {
            sliderLossSteps += sliderLoss;
        }
        sliderLossSteps += CurrentTrialDef.SliderInitialValue;
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
            StartButton = Session.USE_StartButton.CreateStartButton(WWW_CanvasGO.GetComponent<Canvas>(), currentTaskDef.StartButtonPosition, currentTaskDef.StartButtonScale);
            Session.USE_StartButton.SetVisibilityOnOffStates(visOnState, visOffState);
        }
    }

    private void InitializeShotgunHandler()
    {
        ShotgunHandler.HandlerActive = true;
        if (ShotgunHandler.AllSelections.Count > 0)
            ShotgunHandler.ClearSelections();
        ShotgunHandler.MinDuration = minObjectTouchDuration.value;
        ShotgunHandler.MaxDuration = maxObjectTouchDuration.value;
        ShotgunHandler.MaxPixelDisplacement = 50;
    }
    private void UpdateExperimenterDisplaySummaryStrings()
    {
        CurrentTaskLevel.SetBlockSummaryString();
        SetTrialSummaryString();
        if (TrialCount_InTask != 0)
            CurrentTaskLevel.SetTaskSummaryString();
    }
}














