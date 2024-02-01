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



using UnityEngine;
using System.Collections.Generic;
using USE_States;
using USE_StimulusManagement;
using ContinuousRecognition_Namespace;
using System;
using Random = UnityEngine.Random;
using ConfigDynamicUI;
using USE_ExperimentTemplate_Trial;
using System.Linq;
using TMPro;
using System.Collections;


public class ContinuousRecognition_TrialLevel : ControlLevel_Trial_Template
{
    public ContinuousRecognition_TrialDef CurrentTrial => GetCurrentTrialDef<ContinuousRecognition_TrialDef>();
    public ContinuousRecognition_TaskLevel CurrentTaskLevel => GetTaskLevel<ContinuousRecognition_TaskLevel>();
    public ContinuousRecognition_TaskDef CurrentTask => GetTaskDef<ContinuousRecognition_TaskDef>();

    private GameObject StartButton;

    public TextMeshProUGUI TimerText;
    public GameObject TimerTextGO;
    public GameObject CR_CanvasGO;
    public GameObject ScoreTextGO;
    public GameObject NumTrialsTextGO;
    public GameObject TimerBackdropGO;
    public GameObject GreenBorderPrefab;
    public GameObject RedBorderPrefab;
    public GameObject GreenBorderPrefab_2D;
    public GameObject RedBorderPrefab_2D;
    public GameObject Starfield;
    [HideInInspector] public List<GameObject> BorderList;

    [HideInInspector] public List<int> PC_Stim, PNC_Stim, New_Stim, Unseen_Stim, TrialStimIndices;

    [HideInInspector] public Vector3[] BlockFeedbackLocations;

    [HideInInspector] public bool CompletedAllTrials, EndBlock, StimIsChosen, AdjustedPositionsForMac, ContextActive, VariablesLoaded;

    [HideInInspector] public  List<int> ChosenStimIndices;

    [HideInInspector] public int NonStimTouches_Block, NumCorrect_Block, NumTbCompletions_Block;
    [HideInInspector] public float AvgTimeToChoice_Block, TimeToCompletion_Block, TimeToCompletion_StartTime, TokenUpdateStartTime, TimeRemaining;
    [HideInInspector] public List <float> TimeToChoice_Block;

    [HideInInspector] public int RecencyInterference_Block;

    private int score;
    [HideInInspector] public int Score
    {
        get
        {
            return score;
        }
    }

    private Vector3 OriginalTimerPosition;

    private StimGroup trialStims, RightGroup, WrongGroup;

    [HideInInspector] GameObject ChosenGO;
    [HideInInspector] ContinuousRecognition_StimDef ChosenStim;

    private int NumPC_Trial, NumNew_Trial, NumPNC_Trial;

    private PlayerViewPanel playerView;
    private GameObject playerViewParent, playerViewText;
    [HideInInspector] public List<GameObject> playerViewTextList;
    
    //Config Variables
    [HideInInspector]
    public ConfigNumber minObjectTouchDuration, maxObjectTouchDuration, displayStimDuration, chooseStimDuration, itiDuration, touchFbDuration, displayResultsDuration, tokenUpdateDuration, tokenRevealDuration;

    public GameObject DisplayResults2DContainerGO, DisplayResultsPanelGO;

    [HideInInspector] public int WrongStimIndex;
    [HideInInspector] public bool GotTrialCorrect;
    [HideInInspector] public float TimeChosen_Trial, TimeToChoice_Trial;
    [HideInInspector] public string Locations_String, PC_String, New_String, PNC_String, PC_Percentage_String;



    public override void DefineControlLevel()
    {
        State InitTrial = new State("InitTrial");
        State DisplayStims = new State("DisplayStims");
        State ChooseStim = new State("ChooseStim");
        State TouchFeedback = new State("TouchFeedback");
        State TokenUpdate = new State("TokenUpdate");
        State DisplayResults = new State("DisplayResults");
        State ITI = new State("ITI");
        AddActiveStates(new List<State> { InitTrial, DisplayStims, ChooseStim, TouchFeedback, TokenUpdate, DisplayResults, ITI });


        Add_ControlLevel_InitializationMethod(() =>
        {
            OriginalTimerPosition = TimerBackdropGO.transform.position;

            if (!Session.WebBuild)
            {
                playerView = gameObject.AddComponent<PlayerViewPanel>();
                playerViewParent = GameObject.Find("MainCameraCopy");
                playerViewTextList = new List<GameObject>();
            }

            SetControllerBlockValues();

            if (StartButton == null)
            {
                if (Session.SessionDef.IsHuman)
                {
                    StartButton = Session.HumanStartPanel.StartButtonGO;
                    Session.HumanStartPanel.SetVisibilityOnOffStates(InitTrial, InitTrial);
                }
                else
                {
                    StartButton = Session.USE_StartButton.CreateStartButton(CR_CanvasGO.GetComponent<Canvas>(), CurrentTask.StartButtonPosition, CurrentTask.StartButtonScale);
                    Session.USE_StartButton.SetVisibilityOnOffStates(InitTrial, InitTrial);
                }
            }

            PC_Stim = new List<int>();
            PNC_Stim = new List<int>();
            New_Stim = new List<int>();
            Unseen_Stim = new List<int>();
            TrialStimIndices = new List<int>();
        });

        //SETUP TRIAL state ------------------------------------------------------------------------------------------------------
        SetupTrial.SpecifyTermination(() => true, InitTrial);

        //------------------------------------------------------------------------------------------------------------------------
        var ShotgunHandler = Session.SelectionTracker.SetupSelectionHandler("trial", "TouchShotgun", Session.MouseTracker, InitTrial, ChooseStim);
        TouchFBController.EnableTouchFeedback(ShotgunHandler, CurrentTask.TouchFeedbackDuration, CurrentTask.StartButtonScale*15, CR_CanvasGO, true);

        //INIT Trial state -------------------------------------------------------------------------------------------------------
        InitTrial.AddSpecificInitializationMethod(() =>
        {
            if (TrialCount_InBlock == 0)
                CalculateBlockFeedbackLocations();

            CalculatePercentagePC();

            if (Session.SessionDef.MacMainDisplayBuild & !Application.isEditor && !AdjustedPositionsForMac) //adj text positions if running build with mac as main display
                AdjustTextPosForMac();

            if (!VariablesLoaded)
                LoadConfigUIVariables();
            
            SetTrialSummaryString();

            CurrentTaskLevel.CalculateBlockSummaryString();

            if (TrialCount_InTask != 0)
                CurrentTaskLevel.SetTaskSummaryString();

            if (CurrentTrial.UseStarfield)
                Starfield.SetActive(true);

            //Should add this to other tasks as well!
            if (Session.SessionDef.MacMainDisplayBuild && !Application.isEditor)
                TokenFBController.AdjustTokenBarSizing(100);

            TokenFBController.enabled = false;

            TimerText = TimerTextGO.GetComponent<TextMeshProUGUI>();

            SetTokenFeedbackTimes();
            SetStimStrings();
            SetShadowType(CurrentTask.ShadowType, "ContinuousRecognition_DirectionalLight");

            if (ShotgunHandler.AllSelections.Count > 0)
                ShotgunHandler.ClearSelections();
            ShotgunHandler.MinDuration = minObjectTouchDuration.value;
            ShotgunHandler.MaxDuration = maxObjectTouchDuration.value;
        });
        InitTrial.SpecifyTermination(() => ShotgunHandler.LastSuccessfulSelectionMatchesStartButton(), DisplayStims);
        InitTrial.AddDefaultTerminationMethod(() =>
        {
            if (Session.SessionDef.IsHuman)
            {
                CR_CanvasGO.SetActive(true);
                SetScoreAndTrialsText();
                ScoreTextGO.SetActive(true);
                NumTrialsTextGO.SetActive(true);
                TimerBackdropGO.SetActive(true);
            }

            TokenFBController.SetTotalTokensNum(CurrentTrial.TokenBarCapacity);
            TokenFBController.enabled = true;
            Session.EventCodeManager.AddToFrameEventCodeBuffer("TokenBarVisible");

            if (CurrentTask.StimFacingCamera)
                MakeStimsFaceCamera(trialStims);

            if(CurrentTrial.ShakeStim)
                AddShakeStimScript(trialStims);

            if (CurrentTask.MakeStimPopOut)
                PopStimOut();
        });

        //DISPLAY STIMs state -----------------------------------------------------------------------------------------------------
        DisplayStims.AddTimer(() => displayStimDuration.value, ChooseStim, () => TimeRemaining = chooseStimDuration.value);

        //CHOOSE STIM state -------------------------------------------------------------------------------------------------------
        ChooseStim.AddSpecificInitializationMethod(() =>
        {
            if (!Session.WebBuild)
                CreateTextOnExperimenterDisplay();

            ChosenGO = null;
            ChosenStim = null;
            StimIsChosen = false;

            if (TrialCount_InBlock == 0)
                TimeToCompletion_StartTime = Time.time;

            if (ShotgunHandler.AllSelections.Count > 0)
                ShotgunHandler.ClearSelections();
        });
        ChooseStim.AddUpdateMethod(() =>
        {
            if (TimeRemaining > 0)
                TimeRemaining -= Time.deltaTime;

            TimerText.text = TimeRemaining.ToString("0");

            ChosenGO = ShotgunHandler.LastSuccessfulSelection.SelectedGameObject;
            ChosenStim = ChosenGO?.GetComponent<StimDefPointer>()?.GetStimDef<ContinuousRecognition_StimDef>();

            if (ChosenStim != null) //They Clicked a Stim
            {
                TimeChosen_Trial = Time.time;
                TimeToChoice_Trial = TimeChosen_Trial - ChooseStim.TimingInfo.StartTimeAbsolute;
                TimeToChoice_Block.Add(TimeToChoice_Trial);
                CalculateBlockAvgTimeToChoice();

                if (!ChosenStimIndices.Contains(ChosenStim.StimIndex)) //THEY GUESSED RIGHT
                {
                    GotTrialCorrect = true;

                    Session.EventCodeManager.SendCodeImmediate("CorrectResponse");

                    //If chose a PNC Stim, remove it from PNC list.
                    if (PNC_Stim.Contains(ChosenStim.StimIndex))
                        PNC_Stim.Remove(ChosenStim.StimIndex);
                    //If Chose a New Stim, remove it from New list.
                    if (New_Stim.Contains(ChosenStim.StimIndex))
                        New_Stim.Remove(ChosenStim.StimIndex);

                    ChosenStim.PreviouslyChosen = true;
                    PC_Stim.Add(ChosenStim.StimIndex);
                    ChosenStimIndices.Add(ChosenStim.StimIndex); //also adding to chosenIndices so I can keep them in order for display results. 

                    //REMOVE ALL NEW STIM THAT WEREN'T CHOSEN, FROM NEW STIM AND INTO PNC STIM. 
                    List<int> newStimToRemove = New_Stim.ToList();
                    foreach (var stim in newStimToRemove)
                    {
                        if (New_Stim.Contains(stim) && stim != ChosenStim.StimIndex)
                        {
                            New_Stim.Remove(stim);
                            PNC_Stim.Add(stim);
                        }
                    }

                    //SINCE THEY GOT IT RIGHT, CHECK IF LAST TRIAL IN BLOCK OR IF THEY FOUND ALL THE STIM. 
                    if(PNC_Stim.Count == 0 || TrialCount_InBlock == CurrentTrial.MaxNumTrials-1)
                    {
                        TimeToCompletion_Block = Time.time - TimeToCompletion_StartTime;
                        CompletedAllTrials = true;
                        EndBlock = true;
                    }
                }

                else //THEY GUESSED WRONG
                {
                    RecencyInterference_Block = (TrialCount_InBlock + 1) - ChosenStim.TrialNumFirstShownOn;
                    WrongStimIndex = ChosenStim.StimIndex; //identifies the stim they got wrong for Block FB purposes. 
                    TimeToCompletion_Block = Time.time - TimeToCompletion_StartTime;
                    Session.EventCodeManager.SendCodeImmediate("IncorrectResponse");
                }
            }

            if (ChosenGO != null && ChosenStim != null && ShotgunHandler.SuccessfulSelections.Count > 0) //if they chose a stim 
                StimIsChosen = true;

            //Count NonStim Clicks:
            if (InputBroker.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(InputBroker.mousePosition);
                if (!Physics.Raycast(ray, out RaycastHit hit))
                {
                    NonStimTouches_Block++;
                    CurrentTaskLevel.NonStimTouches_Task++;
                }
            }
        });
        ChooseStim.SpecifyTermination(() => StimIsChosen, TouchFeedback);
        ChooseStim.SpecifyTermination(() => (Time.time - ChooseStim.TimingInfo.StartTimeAbsolute > chooseStimDuration.value) && !TouchFBController.FeedbackOn, TokenUpdate, () =>
        {
            Session.EventCodeManager.SendCodeImmediate("NoChoice");
            Session.EventCodeManager.SendRangeCode("CustomAbortTrial", AbortCodeDict["NoSelectionMade"]);
            AbortCode = 6;
            AudioFBController.Play("Negative");
            EndBlock = true;
        });

        //TOUCH FEEDBACK state -------------------------------------------------------------------------------------------------------
        TouchFeedback.AddSpecificInitializationMethod(() =>
        {
            if (!StimIsChosen)
                return;
            
            int? depth = Session.Using2DStim ? 10 : (int?)null;

            if (GotTrialCorrect)
                HaloFBController.ShowPositive(ChosenGO, depth);
            else
                HaloFBController.ShowNegative(ChosenGO, depth);
        });
        TouchFeedback.AddTimer(() => touchFbDuration.value, TokenUpdate);
        TouchFeedback.SpecifyTermination(() => !StimIsChosen, TokenUpdate);

        //TOKEN UPDATE state ---------------------------------------------------------------------------------------------------------
        TokenUpdate.AddSpecificInitializationMethod(() =>
        {
            TokenUpdateStartTime = Time.time;
            HaloFBController.DestroyAllHalos();

            if (!StimIsChosen)
                return;

            if (GotTrialCorrect)
            {
                if(TrialCount_InBlock == CurrentTrial.MaxTrials-1 || PNC_Stim.Count == 0) //If they get the last trial right (or find all stim), fill up bar!
                {
                    int numToFillBar = CurrentTrial.TokenBarCapacity - TokenFBController.GetTokenBarValue();
                    TokenFBController.AddTokens(ChosenGO, numToFillBar);
                }
                else
                    TokenFBController.AddTokens(ChosenGO, CurrentTrial.TokenGain);
            }
            else //Got wrong
            {
                TokenFBController.RemoveTokens(ChosenGO,CurrentTrial.TokenLoss);
                EndBlock = true;
            }
        });
        TokenUpdate.SpecifyTermination(() => Time.time - TokenUpdateStartTime > (tokenRevealDuration.value + tokenUpdateDuration.value) && !TokenFBController.IsAnimating(), DisplayResults);
        TokenUpdate.SpecifyTermination(() => !StimIsChosen, DisplayResults);
        TokenUpdate.AddDefaultTerminationMethod(() =>
        {
            HandleTokenUpdate();
            DeactivatePlayerViewText();

            if (CurrentTrial.ShakeStim)
                RemoveShakeStimScript(trialStims);

            if (Session.SessionDef.IsHuman)
            {
                TimerBackdropGO.SetActive(false);
                ScoreTextGO.SetActive(false);
                NumTrialsTextGO.SetActive(false);
            }
        });

        //DISPLAY RESULTS state --------------------------------------------------------------------------------------------------------
        DisplayResults.AddSpecificInitializationMethod(() =>
        {
            if (displayResultsDuration.value == 0 || ChosenStimIndices.Count < 1) 
                return;

            if (EndBlock || CompletedAllTrials)
            {
                StartCoroutine(GenerateBlockFeedback());

                if (Session.SessionDef.IsHuman)
                    AudioFBController.Play(CompletedAllTrials ? "CR_BlockCompleted" : "CR_BlockFailed");
            }
        });
        DisplayResults.AddTimer(() => displayResultsDuration.value, ITI); //If EndBlock or CompletedAllTrials
        DisplayResults.SpecifyTermination(() => displayResultsDuration.value == 0 || ChosenStimIndices.Count < 1, ITI); //If want to skip results, or they didnt make a single selection
        DisplayResults.SpecifyTermination(() => !EndBlock && !CompletedAllTrials, ITI); //Most trials (If not endblock or all trials completed, skip results)
        DisplayResults.AddDefaultTerminationMethod(() =>
        {
            DisplayResultsPanelGO.SetActive(false);
            DisplayResults2DContainerGO.SetActive(false);

            if(CurrentTrial.ShakeStim)
                RemoveShakeStimScript(trialStims);

            TokenFBController.enabled = false;
        });

        //ITI State----------------------------------------------------------------------------------------------------------------------
        ITI.AddTimer(() => itiDuration.value, FinishTrial);
        //---------------------------------------------------------------------------------------------------------------------------------
        DefineTrialData();
        DefineFrameData();
    }


    //HELPER FUNCTIONS --------------------------------------------------------------------------------------------------------------------
    public override void FinishTrialCleanup()
    {
        if (GotTrialCorrect)
            score += (TrialCount_InBlock + 1) * 100;

        if (DisplayResultsPanelGO.activeInHierarchy)
            DisplayResultsPanelGO.SetActive(false);

        DeactivateTextObjects();
        if (playerViewTextList != null && playerViewTextList.Count > 0)
            DeactivatePlayerViewText();
        DestroyFeedbackBorders();
        ContextActive = false;
        Session.EventCodeManager.AddToFrameEventCodeBuffer("ContextOff");

        if (AbortCode == 0)
        {
            CurrentTaskLevel.TrialsCompleted_Task++;

            if (GotTrialCorrect)
            {
                NumCorrect_Block++;
                CurrentTaskLevel.TrialsCorrect_Task++;
            }

            CurrentTaskLevel.CalculateBlockSummaryString();
        }
        else
        {
            CurrentTaskLevel.NumAbortedTrials_InBlock++;
            CurrentTaskLevel.NumAbortedTrials_InTask++;

            if (AbortCode == AbortCodeDict["EndTrial"])
                EndBlock = true;
        }

        TokenFBController.ResetTokenBarFull();
    }

    private void HandleTokenUpdate()
    {
        if (TokenFBController.IsTokenBarFull())
        {
            NumTbCompletions_Block++;
            CurrentTaskLevel.TokenBarCompletions_Task++;

            CurrentTaskLevel.NumRewardPulses_InBlock += CurrentTrial.NumPulses;
            CurrentTaskLevel.NumRewardPulses_InTask += CurrentTrial.NumPulses;

            Session.SyncBoxController?.SendRewardPulses(CurrentTrial.NumPulses, CurrentTrial.PulseSize);
        }
    }

    public override void AddToStimLists() //For EventCodes:
    {
        foreach (ContinuousRecognition_StimDef stim in trialStims.stimDefs)
        {
            if (stim.PreviouslyChosen)
                Session.DistractorObjects.Add(stim.StimGameObject);
            else
                Session.TargetObjects.Add(stim.StimGameObject);   
        }
    }

    private void CalculateBlockFeedbackLocations()
    {
        BlockFeedbackLocations = new Vector3[CurrentTrial.X_FbLocations.Length * CurrentTrial.Y_FbLocations.Length];
        int feedbackIndex = 0;
        for (int i = 0; i < CurrentTrial.Y_FbLocations.Length; i++)
        {
            float y = CurrentTrial.Y_FbLocations[i];
            for (int j = 0; j < CurrentTrial.X_FbLocations.Length; j++)
            {
                float x = CurrentTrial.X_FbLocations[j];
                BlockFeedbackLocations[feedbackIndex] = new Vector3(x, y, 0);
                feedbackIndex++;
            }
        }
    }

    private void DeactivatePlayerViewText()
    {
        foreach (GameObject textGO in playerViewTextList)
            Destroy(textGO);
    }

    private void CreateTextOnExperimenterDisplay()
    {
        for(int i=0; i < CurrentTrial.NumTrialStims; ++i)
        {
            Vector2 textLocation = ScreenToPlayerViewPosition(Camera.main.WorldToScreenPoint(trialStims.stimDefs[i].StimLocation), playerViewParent.transform);
            textLocation.y += 50;
            Vector2 textSize = new Vector2(200, 200);
            string stimString = "Target";
            ContinuousRecognition_StimDef currentStim = (ContinuousRecognition_StimDef)trialStims.stimDefs[i];
            if (currentStim.PreviouslyChosen)
                stimString = "PC";

            playerViewText = playerView.CreateTextObject(stimString, stimString, stimString == "PC" ? Color.red : Color.green, textLocation, textSize, playerViewParent.transform);
            playerViewText.GetComponent<RectTransform>().localScale = new Vector3(1.1f, 1.1f, 0);
            playerViewTextList.Add(playerViewText);
            playerViewText.SetActive(true);
        }
    }

    public override void ResetTrialVariables()
    {
        Locations_String = null;
        PC_String = null;
        New_String = null;
        PNC_String = null;

        WrongStimIndex = -1;
        GotTrialCorrect = false;
        TimeChosen_Trial = -1;
        TimeToChoice_Trial = -1;
        CompletedAllTrials = false;
        EndBlock = false;
        StimIsChosen = false;
    }

    public void ResetBlockVariables()
    {
        AdjustedPositionsForMac = false;
        ChosenStimIndices.Clear();
        NonStimTouches_Block = 0;
        NumCorrect_Block = 0;
        NumTbCompletions_Block = 0;
        TimeToChoice_Block.Clear();
        AvgTimeToChoice_Block = 0;
        TimeToCompletion_Block = 0;
        RecencyInterference_Block = 0;
        score = 0;
    }

    public void SetControllerBlockValues()
    {
        TokenFBController.SetFlashingTime(1f);
        HaloFBController.SetPositiveHaloColor(Color.yellow);
        HaloFBController.SetNegativeHaloColor(Color.gray);
        HaloFBController.SetParticleHaloSize(.65f);
        //HaloFBController.SetCircleHaloSize(1f);
        HaloFBController.SetCircleHaloIntensity(1.5f);
    }

    void RemoveShakeStimScript(StimGroup stimGroup)
    {
        foreach (var stim in stimGroup.stimDefs)
        {
            Destroy(stim.StimGameObject.GetComponent<ShakeStim>());
            Destroy(stim.StimGameObject.GetComponent<Rigidbody>());
        }
    }

    void AddShakeStimScript(StimGroup stimGroup)
    {
        foreach (var stim in stimGroup.stimDefs)
        {
            AddRigidBody(stim.StimGameObject);
            stim.StimGameObject.AddComponent<ShakeStim>();
            if (Session.Using2DStim)
                stim.StimGameObject.GetComponent<ShakeStim>().Radius = .004f;
        }
    }

    void DeactivateTextObjects()
    {
        if (ScoreTextGO.activeInHierarchy)
            ScoreTextGO.SetActive(false);

        if (NumTrialsTextGO.activeInHierarchy)
            NumTrialsTextGO.SetActive(false);

        if (TimerBackdropGO.activeInHierarchy)
            TimerBackdropGO.SetActive(false);
    }

    void PopStimOut() //Method used to make the game easier for debugging purposes
    {
        foreach(ContinuousRecognition_StimDef stim in trialStims.stimDefs)
        {
            if (!stim.PreviouslyChosen)
                stim.StimGameObject.transform.localScale *= 1.25f;
        }
    }

    void AdjustTextPosForMac() //When running a build instead of hitting play in editor:
    {
        TokenFBController.AdjustTokenBarSizing(200);

        Vector3 Pos = OriginalTimerPosition;
        Pos.y -= .02f;
        TimerBackdropGO.transform.position = Pos;

        AdjustedPositionsForMac = true;
    }

    void SetScoreAndTrialsText()
    {
        //Set the Score and NumTrials texts at the beginning of the trial. 
        ScoreTextGO.GetComponent<TextMeshProUGUI>().text = $"SCORE: {score}";
        NumTrialsTextGO.GetComponent<TextMeshProUGUI>().text = $"TRIAL: {TrialCount_InBlock + 1}";
    }

    void SetTrialSummaryString()
    {
        TrialSummaryString = "<b>Trial #" + (TrialCount_InBlock + 1) + " In Block" + "</b>" +
                             "\nPC_Stim: " + NumPC_Trial +
                             "\nNew_Stim: " + NumNew_Trial +
                             "\nPNC_Stim: " + NumPNC_Trial;
    }

    Vector3[] CenterFeedbackLocations(Vector3[] locations, int numLocations)
    {
        if (numLocations > 24)
            Debug.LogError("You are attempting to generate " + numLocations + " feedback Locations, However the maximum is 24");

        int MaxNumPerRow = 6;
        float max = 2.25f;

        float horizontalMax = 4;

        int numRows = 1;
        if (numLocations > 6) numRows++;
        if (numLocations > 12) numRows++;
        if (numLocations > 18) numRows++;
        if (numLocations > 24) numRows++;

        int R1_Length = 0;
        int R2_Length = 0;
        int R3_Length = 0;
        int R4_Length = 0;
        int R5_Length = 0;

        //Calculate num stim in each row. 
        switch (numRows)
        {
            case 1:
                R1_Length = numLocations;
                break;
            case 2:
                if (numLocations % 2 == 0)
                    R1_Length = R2_Length = numLocations / 2;
                else
                {
                    R1_Length = (int)Math.Floor((decimal)numLocations / 2); //round it down and increase next row by 1.
                    R2_Length = (int)Math.Ceiling((decimal)numLocations / 2); //make last row have one more than first row. 
                }
                break;
            case 3:
                if (numLocations % 3 == 0)
                    R1_Length = R2_Length = R3_Length = numLocations / 3;
                else
                {
                    R1_Length = R2_Length = (int)Math.Floor((decimal)numLocations / 3);
                    R3_Length = (int)Math.Ceiling((decimal)numLocations / 3);

                    int diff = numLocations - (R1_Length + R2_Length + R3_Length);
                    if (diff == 1) R2_Length++;
                }
                break;
            case 4:
                if (numLocations % 4 == 0)
                    R1_Length = R2_Length = R3_Length = R4_Length = numLocations / 4;
                else
                {
                    R1_Length = R2_Length = R3_Length = (int)Math.Floor((decimal)numLocations / 4);
                    R4_Length = (int)Math.Ceiling((decimal)numLocations / 4);

                    int diff = numLocations - (R1_Length + R2_Length + R3_Length + R4_Length);
                    if (diff == 1) R3_Length++;
                    else if (diff == 2)
                    {
                        if (R4_Length == MaxNumPerRow)
                        {
                            R2_Length++;
                            R3_Length++;
                        }
                        else
                        {
                            R3_Length++;
                            R4_Length++;
                        }
                    }
                }
                break;
            case 5:
                if (numLocations % 5 == 0)
                    R1_Length = R2_Length = R3_Length = R4_Length = R5_Length = numLocations / 5;
                else
                {
                    R1_Length = R2_Length = R3_Length = R4_Length = (int)Math.Floor((decimal)numLocations / 5);
                    R5_Length = (int)Math.Ceiling((decimal)numLocations / 5);

                    int diff = numLocations - (R1_Length + R2_Length + R3_Length + R4_Length + R5_Length);
                    if (diff == 1) R4_Length++;
                    else if (diff == 2)
                    {
                        R3_Length++;
                        R4_Length++;
                    }
                    else if (diff == 3)
                    {
                        R2_Length++;
                        R3_Length++;
                        R4_Length++;
                    }
                }
                break;
        }


        float leftMargin;
        float rightMargin;

        int index = 0;
        int difference = 0;
        Vector3 currentShiftedLoc;
        List<Vector3> locList = new List<Vector3>();

        //----- CENTER HORIZONTALLY--------------------------------
        //Center ROW 1:
        if (R1_Length > 0)
        {
            leftMargin = horizontalMax - Math.Abs(locations[0].x);
            rightMargin = horizontalMax - locations[R1_Length - 1].x;
            for (int i = index; i < R1_Length; i++)
            {
                currentShiftedLoc = ShiftLocationHorizontally(leftMargin, rightMargin, locations[i]);
                locList.Add(currentShiftedLoc);
                index++;
            }
            if (R2_Length > 0)
                difference = MaxNumPerRow - R1_Length;
        }

        //Center ROW 2:
        if (R2_Length > 0)
        {
            index += difference;
            leftMargin = horizontalMax - Math.Abs(locations[index].x);
            rightMargin = horizontalMax - locations[index + R2_Length - 1].x;
            int indy = index;
            for (int i = index; i < (indy + R2_Length); i++)
            {
                currentShiftedLoc = ShiftLocationHorizontally(leftMargin, rightMargin, locations[i]);
                locList.Add(currentShiftedLoc);
                index++;
            }
            if (R3_Length > 0)
                difference = MaxNumPerRow - R2_Length;
        }

        //Center ROW 3:
        if (R3_Length > 0)
        {
            index += difference;
            leftMargin = horizontalMax - Math.Abs(locations[index].x);
            rightMargin = horizontalMax - locations[index + R3_Length - 1].x;
            int indy = index;
            for (int i = index; i < (indy + R3_Length); i++)
            {
                currentShiftedLoc = ShiftLocationHorizontally(leftMargin, rightMargin, locations[i]);
                locList.Add(currentShiftedLoc);
                index++;
            }
            if (R4_Length > 0)
                difference = MaxNumPerRow - R3_Length;
        }

        //Center ROW 4:
        if (R4_Length > 0)
        {
            index += difference;
            leftMargin = horizontalMax - Math.Abs(locations[index].x);
            rightMargin = horizontalMax - locations[index + R4_Length - 1].x;
            int indy = index;
            for (int i = index; i < (indy + R4_Length); i++)
            {
                currentShiftedLoc = ShiftLocationHorizontally(leftMargin, rightMargin, locations[i]);
                locList.Add(currentShiftedLoc);
                index++;
            }
            if (R5_Length > 0)
                difference = MaxNumPerRow - R4_Length;
        }

        //Center ROW 5:
        if (R5_Length > 0)
        {
            index += difference;
            leftMargin = 4 - Math.Abs(locations[index].x);
            rightMargin = 4f - locations[index + R5_Length - 1].x;
            int indy = index;
            for (int i = index; i < (indy + R5_Length); i++)
            {
                currentShiftedLoc = ShiftLocationHorizontally(leftMargin, rightMargin, locations[i]);
                locList.Add(currentShiftedLoc);
                index++;
            }
        }

        Vector3[] FinalLocations = locList.ToArray();

        //----- CENTER VERTICALLY-----------------------------------------------
        if (numRows > 3)
            max /= 2;

        float topMargin = max - FinalLocations[0].y;
        float bottomMargin = FinalLocations[FinalLocations.Length - 1].y + max;

        float shiftDownNeeded = (topMargin + bottomMargin) / 2;
        float shiftDownAmount = shiftDownNeeded - topMargin;

        for (int i = 0; i < FinalLocations.Length; i++)
            FinalLocations[i].y -= shiftDownAmount;

        return FinalLocations;
    }

    public Vector3 ShiftLocationHorizontally(float leftMarg, float rightMarg, Vector3 currentLoc)
    {
        float leftMarginNeeded = (leftMarg + rightMarg) / 2;
        float leftshiftAmount = leftMarginNeeded - leftMarg;
        currentLoc.x += leftshiftAmount;
        return currentLoc;
    }


    protected override void DefineTrialStims()
    {
        NumPC_Trial = 0;
        NumNew_Trial = 0;
        NumPNC_Trial = 0;

        StimGroup group = Session.UsingDefaultConfigs ? PrefabStims : ExternalStims;

        if (TrialCount_InBlock == 0)
        {
            trialStims = null;
            score = 0;

            //clear stim lists in case it's NOT the first block!
            ClearCurrentTrialStimLists();

            //Add each block stim to unseen list.
            var numBlockStims = CurrentTrial.BlockStimIndices.Length;
            for (int i = 0; i < numBlockStims; i++)
                Unseen_Stim.Add(CurrentTrial.BlockStimIndices[i]);
            
            //Pick 2 random New stim and add to TrialStimIndices and NewStim. Also remove from UnseenStim.
            int[] tempArray = new int[CurrentTrial.NumObjectsMinMax[0]];
            for (int i = 0; i < CurrentTrial.NumObjectsMinMax[0]; i++) 
            {
                int ranNum = Random.Range(0, numBlockStims);
                while (Array.IndexOf(tempArray, ranNum) != -1)
                {
                    ranNum = Random.Range(0, numBlockStims);
                }
                tempArray[i] = ranNum;
                TrialStimIndices.Add(ranNum);
                Unseen_Stim.Remove(ranNum);
                New_Stim.Add(ranNum);
                NumNew_Trial++;
            }

            trialStims = new StimGroup("TrialStims", group, TrialStimIndices);
            foreach (ContinuousRecognition_StimDef stim in trialStims.stimDefs)
            {
                stim.PreviouslyChosen = false;
            }
            trialStims.SetLocations(CurrentTrial.TrialStimLocations);
            TrialStims.Add(trialStims);

        }
        else if((TrialCount_InBlock > 0 && TrialCount_InBlock <= (CurrentTrial.NumObjectsMinMax[1] - 2)) || TrialCount_InBlock > 0 && !CurrentTrial.FindAllStim)
        {
            TrialStimIndices.Clear();

            float[] stimPercentages = GetStimRatioPercentages(CurrentTrial.InitialStimRatio);
            int[] stimNumbers = GetStimNumbers(stimPercentages);

            int PC_Num = stimNumbers[0];
            int New_Num = stimNumbers[1];
            int PNC_Num = stimNumbers[2];

            List<int> NewStim;

            if (TrialCount_InBlock == 1)
                NewStim = ShuffleList(Unseen_Stim).ToList(); //shuffle unseen list during first (second overall) trial! (only needed once). 
            else NewStim = Unseen_Stim.ToList();

            if (NewStim.Count > 1)
                    NewStim = NewStim.GetRange(0, New_Num);

            for (int i = 0; i < NewStim.Count; i++)
            {
                int current = NewStim[i];
                TrialStimIndices.Add(current);
                Unseen_Stim.Remove(current);
                New_Stim.Add(current);
                NumNew_Trial++;
            }

            List<int> PC_Copy = ShuffleList(PC_Stim).ToList();
            if (PC_Copy.Count > 1)
                PC_Copy = PC_Copy.GetRange(0, PC_Num);
            for (int i = 0; i < PC_Copy.Count; i++)
            {
                TrialStimIndices.Add(PC_Copy[i]);
                NumPC_Trial++;
            }
            

            List<int> PNC_Copy = ShuffleList(PNC_Stim).ToList();
            if (PNC_Copy.Count > 1)
                PNC_Copy = PNC_Copy.GetRange(0, PNC_Num);
            for (int i = 0; i < PNC_Copy.Count; i++)
            {
                TrialStimIndices.Add(PNC_Copy[i]);
                NumPNC_Trial++;
            }

            trialStims = new StimGroup($"TrialStims", group, TrialStimIndices);
            trialStims.SetLocations(CurrentTrial.TrialStimLocations);
            TrialStims.Add(trialStims);
        }

        else //The Non-Increasing trials
        {
            TrialStimIndices.Clear();

            var totalNeeded = CurrentTrial.NumObjectsMinMax[1];
            var num_PNC = PNC_Stim.Count;
            var num_PC = totalNeeded - num_PNC;

            //Add PNC Stim to trialIndices
            foreach (int num in PNC_Stim)
                TrialStimIndices.Add(num);

            //Add PC Stim to trialIndices.
            for(int i = 0; i < num_PC; i++)
                TrialStimIndices.Add(PC_Stim[i]);
            
            trialStims = new StimGroup($"TrialStims", group, TrialStimIndices);
            trialStims.SetLocations(CurrentTrial.TrialStimLocations);
            TrialStims.Add(trialStims);
        }

        foreach (ContinuousRecognition_StimDef stim in trialStims.stimDefs)
        {
            if (stim.TrialNumFirstShownOn == -1)
                stim.TrialNumFirstShownOn = TrialCount_InBlock + 1;
        }

        trialStims.SetVisibilityOnOffStates(GetStateFromName("DisplayStims"), GetStateFromName("TokenUpdate"));

    }

    public void CalculateBlockAvgTimeToChoice()
    {
        if (TimeToChoice_Block.Count == 0)
            AvgTimeToChoice_Block = 0;

        float sum = 0;
        foreach (float choice in TimeToChoice_Block)
            sum += choice;
        AvgTimeToChoice_Block = sum / TimeToChoice_Block.Count;
    }

    private IEnumerator GenerateBlockFeedback()
    {
        DisplayResultsPanelGO.SetActive(true);
        Starfield.SetActive(false);
        TokenFBController.enabled = false;

        if (!StimIsChosen && ChosenStimIndices.Count < 1)
            yield break;

        StimGroup group = Session.UsingDefaultConfigs ? PrefabStims : ExternalStims;

        if(Session.Using2DStim)
            DisplayResults2DContainerGO.SetActive(true);
        Transform gridParent = DisplayResults2DContainerGO.transform.Find("Grid");

        if (CompletedAllTrials || !StimIsChosen) //!stimchosen means time ran out. 
        {
            RightGroup = new StimGroup("Right", group, ChosenStimIndices);
            Vector3[] FeedbackLocations = new Vector3[ChosenStimIndices.Count];
            FeedbackLocations = CenterFeedbackLocations(BlockFeedbackLocations, FeedbackLocations.Length);

            if(Session.Using2DStim)
                yield return StartCoroutine(LoadGridStims(RightGroup, gridParent));
            else
                yield return StartCoroutine(Load3DStims(RightGroup, FeedbackLocations));
        }
        else
        {
            RightGroup = new StimGroup("Right", group, ChosenStimIndices);
            Vector3[] FeedbackLocations = new Vector3[ChosenStimIndices.Count + 1];
            FeedbackLocations = CenterFeedbackLocations(BlockFeedbackLocations, FeedbackLocations.Length);

            if (Session.Using2DStim)
                yield return StartCoroutine(LoadGridStims(RightGroup, gridParent));
            else
                yield return StartCoroutine(Load3DStims(RightGroup, FeedbackLocations.Take(FeedbackLocations.Length - 1).ToArray()));

            WrongGroup = new StimGroup("Wrong");
            group.stimDefs[WrongStimIndex].CopyStimDef(WrongGroup); //copy wrong stim into WrongGroup
            if(Session.Using2DStim)
                yield return StartCoroutine(LoadGridStims(WrongGroup, gridParent));
            else
                yield return StartCoroutine(Load3DStims(WrongGroup, FeedbackLocations.Skip(FeedbackLocations.Length - 1).Take(1).ToArray()));
        }

    }

    private IEnumerator LoadGridStims(StimGroup group, Transform gridParent)
    {
        TrialStims.Add(group);

        yield return StartCoroutine(group.LoadStims());

        foreach (var stim in group.stimDefs)
            CreateGridItem(gridParent, stim);

        group.ToggleVisibility(true);
    }

    private IEnumerator Load3DStims(StimGroup group, Vector3[] locations)
    {
        TrialStims.Add(group);
        group.SetLocations(locations); //sets the stimLocation but they dont seem to spawn there
        yield return StartCoroutine(group.LoadStims());
        foreach (var stim in group.stimDefs)
            stim.StimGameObject.transform.localPosition = stim.StimLocation; //Manually setting pos since stimLocation isn't doing anything
        Generate3DBorders(group);
        if (CurrentTask.StimFacingCamera)
            MakeStimsFaceCamera(group);
        group.ToggleVisibility(true);
    }

    private void Generate3DBorders(StimGroup group)
    {
        if (BorderList.Count == 0)
            BorderList = new List<GameObject>();

        //Default stim require border to be moved up by .1
        Vector3 adjustment = Session.UsingDefaultConfigs ? new Vector3(0, .1f, 0) : Vector3.zero;

        foreach (var stim in group.stimDefs)
        {
            GameObject border = Instantiate(stim.StimIndex == WrongStimIndex ? RedBorderPrefab : GreenBorderPrefab, stim.StimGameObject.transform.position + adjustment, Quaternion.identity);
            BorderList.Add(border);
        }
    }

    private void CreateGridItem(Transform gridParent, StimDef stim)
    {
        GameObject gridItem = Create2DBorder(stim);
        gridItem.name = $"GridItem_" + stim.StimIndex;
        gridItem.transform.SetParent(gridParent);
        gridItem.transform.localScale = Vector3.one;
        gridItem.transform.localPosition = new Vector3(gridItem.transform.localPosition.x, gridItem.transform.localPosition.y, 0);

        GameObject gridStim = stim.StimGameObject;
        gridStim.name = "Stim_Index_ " + stim.StimIndex;
        gridStim.transform.SetParent(gridItem.transform);
        gridStim.transform.localPosition = Vector3.zero;
    }

    private GameObject Create2DBorder(StimDef stim)
    {
        GameObject border = Instantiate(stim.StimIndex == WrongStimIndex ? RedBorderPrefab_2D : GreenBorderPrefab_2D);
        BorderList.Add(border);
        return border;
    }

    private void MakeStimsFaceCamera(StimGroup stims)
    {
        foreach (var stim in stims.stimDefs)
            stim.StimGameObject.AddComponent<FaceCamera>();
    }

    private void DestroyFeedbackBorders()
    {
        foreach (GameObject border in BorderList)
        {
            if (border != null)
                Destroy(border);
        }
        BorderList.Clear();

        
    }

    private List<int> ShuffleList(List<int> list)
    {
        if (list.Count == 1)
            return list;
        
        else
        {
            int n = list.Count;
            while(n > 1)
            {
                n--;
                int k = Random.Range(0, n + 1);
                int temp = list[k];
                list[k] = list[n];
                list[n] = temp;
            }
            return list;
        }
    }

    protected override bool CheckBlockEnd()
    {
        return EndBlock;
    }

    private void SetStimStrings()
    {
        if (CurrentTrial.TrialStimLocations.Length > 0)
            Locations_String = $"[{string.Join(", ", CurrentTrial.TrialStimLocations)}]";
        if (Locations_String == null)
            PNC_String = "-";

        if (PC_Stim.Count > 0)
            PC_String = $"[{string.Join(", ", PC_Stim)}]";
        PC_String ??= "-";

        if (PNC_Stim.Count > 0)
            PNC_String = $"[{string.Join(", ", PNC_Stim)}]";
        PNC_String ??= "-";

        if (New_Stim.Count > 0)
            New_String = $"[{string.Join(", ", New_Stim)}]";
        New_String ??= "-";

    }

    private string CalculatePercentagePC()
    {
        return (GetStimPercentages()[0] * 100).ToString() + "%";
    }

    private float[] GetStimPercentages()
    {
        var ratio = CurrentTrial.InitialStimRatio;
        float sum = 0;
        float[] stimPercentages = new float[ratio.Length];

        foreach (var num in ratio)
            sum += num;
        for (int i = 0; i < ratio.Length; i++)
            stimPercentages[i] = ratio[i] / sum;

        return stimPercentages;
    }

    private void DefineTrialData()
    {
        TrialData.AddDatum("Context", () => CurrentTrial.ContextName);
        TrialData.AddDatum("Starfield", () => CurrentTrial.UseStarfield);
        TrialData.AddDatum("Num_UnseenStim", () => Unseen_Stim.Count);
        TrialData.AddDatum("PC_Stim", () => PC_String);
        TrialData.AddDatum("New_Stim", () => New_String);
        TrialData.AddDatum("PNC_Stim", () => PNC_String);
        TrialData.AddDatum("StimLocations", () => Locations_String);
        TrialData.AddDatum("ChoseCorrectly", () => GotTrialCorrect);
        TrialData.AddDatum("CurrentTrialStims", () => TrialStimIndices);
        TrialData.AddDatum("PC_Percentage", () => CalculatePercentagePC());
    }

    private void DefineFrameData()
    {
        FrameData.AddDatum("ContextActive", () => ContextActive);
        FrameData.AddDatum("StartButton", () => StartButton != null && StartButton.activeInHierarchy ? "Active" : "NotActive");
        FrameData.AddDatum("TrialStimShown", () => trialStims?.IsActive);
        FrameData.AddDatum("StarfieldActive", () => Starfield != null && Starfield.activeInHierarchy ? "Active" : "NotActive");
    }

    private void ClearCurrentTrialStimLists()
    {
        ChosenStimIndices.Clear();
        TrialStimIndices.Clear();
        New_Stim.Clear();
        PNC_Stim.Clear();
        PC_Stim.Clear();
        Unseen_Stim.Clear();

    }


    private float[] GetStimRatioPercentages(int[] ratioArray)
    {
        //Takes the initial stim ratio (ex: 2PC, 1New, 2PNC), and
        //outputs their percentages of the total 
        float sum = 0;
        float[] stimPercentages = new float[ratioArray.Length];

        foreach(var num in ratioArray)
            sum += num;
        for(int i = 0; i < ratioArray.Length; i++)
            stimPercentages[i] = ratioArray[i] / sum;
      
        return stimPercentages;
    }

    private int[] GetStimNumbers(float[] stimPercentages)
    {
        //Function to calculate the correct num of stim for a trial.
        //Starts by understating each num, then it makes sure there are enough available (some ratios could overstate the stim),
        //then it adjusts the stim category that will make the PC% the closest to its supposed percentage. 
        int PC_Num = (int)Math.Floor(stimPercentages[0] * CurrentTrial.NumTrialStims);
        int New_Num = (int)Math.Floor(stimPercentages[1] * CurrentTrial.NumTrialStims);
        int PNC_Num = (int)Math.Floor(stimPercentages[2] * CurrentTrial.NumTrialStims);
        if (PC_Num == 0) PC_Num = 1;
        if (New_Num == 0) New_Num = 1;
        if (PNC_Num == 0) PNC_Num = 1;

        int[] available = new int[3] { PC_Stim.Count, Unseen_Stim.Count, PNC_Stim.Count };

        //Ensure a crazy stim ratio doesn't calculate more stim than available in that category.
        while (PC_Num > available[0]) PC_Num--;
        while (New_Num > available[1]) New_Num--;
        while (PNC_Num > available[2]) PNC_Num--;

        float PC_TargetPerc = stimPercentages[0];
        while((PC_Num + New_Num + PNC_Num) < CurrentTrial.NumTrialStims)
        {
            //determine whether 1)Adding to PC, or 2)Adding to New/PNC makes the PercDiff smaller.
            float PC_AddPerc = (PC_Num + 1) / (PC_Num + 1 + New_Num + PNC_Num);
            float PC_AddDiff = PC_AddPerc - PC_TargetPerc;

            float NonPC_AddPerc = PC_Num / (PC_Num + 1 + New_Num + PNC_Num);
            float NonPC_AddDiff = NonPC_AddPerc - PC_TargetPerc;

            if(PC_AddDiff < NonPC_AddDiff)
                PC_Num++;
            else
            {
                if (PNC_Num < New_Num)
                    PNC_Num++;
                else
                    New_Num++;
            }
        }

        while(PC_Num + New_Num + PNC_Num > CurrentTrial.NumTrialStims)
        {
            if (New_Num > 1)
                New_Num--;
            else
            {
                if (PC_Num > PNC_Num || PC_Num == PNC_Num)
                    PC_Num--;
                else
                    PNC_Num--;
            }
        }
        return new[] { PC_Num, New_Num, PNC_Num };
    }

    private void LoadConfigUIVariables()
    {
        minObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("minObjectTouchDuration");
        maxObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("maxObjectTouchDuration");
        displayStimDuration = ConfigUiVariables.get<ConfigNumber>("displayStimDuration");
        chooseStimDuration = ConfigUiVariables.get<ConfigNumber>("chooseStimDuration");
        touchFbDuration = ConfigUiVariables.get<ConfigNumber>("touchFbDuration");
        tokenRevealDuration = ConfigUiVariables.get<ConfigNumber>("tokenRevealDuration");
        tokenUpdateDuration = ConfigUiVariables.get<ConfigNumber>("tokenUpdateDuration");
        displayResultsDuration = ConfigUiVariables.get<ConfigNumber>("displayResultsDuration");
        itiDuration = ConfigUiVariables.get<ConfigNumber>("itiDuration");
        VariablesLoaded = true;
    }

    private void SetTokenFeedbackTimes()
    {
        TokenFBController.SetRevealTime(tokenRevealDuration.value);
        TokenFBController.SetUpdateTime(tokenUpdateDuration.value);
    }

}
