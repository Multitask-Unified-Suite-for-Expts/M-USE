using UnityEngine;
using System.Collections.Generic;
using USE_States;
using USE_StimulusManagement;
using ContinuousRecognition_Namespace;
using System;
using Random = UnityEngine.Random;
using ConfigDynamicUI;
using USE_Settings;
using USE_ExperimentTemplate_Trial;
using System.Collections;
using System.Linq;
using UnityEngine.UI;
using USE_ExperimentTemplate_Block;
using System.IO;
using UnityEngine.Profiling;
using TMPro;
using USE_UI;
using USE_Data;


public class ContinuousRecognition_TrialLevel : ControlLevel_Trial_Template
{
    public ContinuousRecognition_TrialDef currentTrial => GetCurrentTrialDef<ContinuousRecognition_TrialDef>();
    public ContinuousRecognition_TaskLevel currentTask => GetTaskLevel<ContinuousRecognition_TaskLevel>();

    public USE_StartButton USE_StartButton;
    public GameObject StartButton;

    public TextMeshProUGUI TimerText;
    public GameObject TimerTextGO;
    public GameObject TitleTextGO;
    public GameObject CR_CanvasGO;
    public GameObject YouWinTextGO;
    public GameObject YouLoseTextGO;
    public GameObject ScoreTextGO;
    public GameObject NumTrialsTextGO;
    public GameObject TimerBackdropGO;
    public GameObject GreenBorderPrefab;
    public GameObject RedBorderPrefab;
    public GameObject Starfield;
    [HideInInspector] public List<GameObject> BorderPrefabList;

    [HideInInspector] public bool IsHuman;

    [HideInInspector] public bool CompletedAllTrials;
    [HideInInspector] public bool EndBlock;
    [HideInInspector] public bool StimIsChosen;
    [HideInInspector] public bool MacMainDisplayBuild;
    [HideInInspector] public bool AdjustedPositionsForMac;
    [HideInInspector] public bool ContextActive;
    [HideInInspector] public bool VariablesLoaded;

    [HideInInspector] public StimGroup trialStims;
    [HideInInspector] public List<int> ChosenStimIndices;
    [HideInInspector] public string MaterialFilePath;

    [HideInInspector] public int NonStimTouches_Block;
    [HideInInspector] public int NumTrials_Block;
    [HideInInspector] public int NumCorrect_Block;
    [HideInInspector] public int NumTbCompletions_Block;
    [HideInInspector] public int NumRewards_Block;
    [HideInInspector] public float AvgTimeToChoice_Block;
    [HideInInspector] public float TimeToCompletion_Block;
    [HideInInspector] public float TimeToCompletion_StartTime;
    [HideInInspector] public float TokenUpdateStartTime;
    [HideInInspector] public float TimeRemaining;
    [HideInInspector] public List <float> TimeToChoice_Block;

    [HideInInspector] public int TokenCount;
    [HideInInspector] public int NumFeedbackRows;
    [HideInInspector] public int Score;

    [HideInInspector] public Vector3 OriginalFbTextPosition;
    [HideInInspector] public Vector3 OriginalTitleTextPosition;
    [HideInInspector] public Vector3 OriginalStartButtonPosition;
    [HideInInspector] public Vector3 OriginalTimerPosition;

    [HideInInspector] public StimGroup RightGroup;
    [HideInInspector] public StimGroup WrongGroup;

    [HideInInspector] GameObject chosenStimObj;
    [HideInInspector] ContinuousRecognition_StimDef chosenStimDef;


    //Config Variables
    [HideInInspector]
    public ConfigNumber displayStimDuration, chooseStimDuration, itiDuration, touchFbDuration, displayResultsDuration, tokenUpdateDuration, tokenRevealDuration;

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
            SetControllerBlockValues();

            OriginalFbTextPosition = YouLoseTextGO.transform.position;
            OriginalTitleTextPosition = TitleTextGO.transform.position;
            OriginalTimerPosition = TimerBackdropGO.transform.position;

            LoadTextures(MaterialFilePath);

            if (StartButton == null)
            {
                USE_StartButton = new USE_StartButton(CR_CanvasGO.GetComponent<Canvas>());
                StartButton = USE_StartButton.StartButtonGO;
                OriginalStartButtonPosition = StartButton.transform.position;
                USE_StartButton.SetVisibilityOnOffStates(InitTrial, InitTrial);
            }
        });

        //SETUP TRIAL state -----------------------------------------------------------------------------------------------------
        SetupTrial.AddInitializationMethod(() =>
        {
            if (!CR_CanvasGO.activeInHierarchy)
                CR_CanvasGO.SetActive(true);

            NumFeedbackRows = 0;

            if (!VariablesLoaded)
                LoadConfigUIVariables();

            SetTrialSummaryString();

            Input.ResetInputAxes(); //reset input in case they still touching their selection from last trial!
        });
        SetupTrial.SpecifyTermination(() => true, InitTrial);

        SelectionHandler<ContinuousRecognition_StimDef> mouseHandler = new SelectionHandler<ContinuousRecognition_StimDef>();
        MouseTracker.AddSelectionHandler(mouseHandler, InitTrial);

        //INIT Trial state -------------------------------------------------------------------------------------------------------
        InitTrial.AddInitializationMethod(() =>
        {
            StartButton.transform.position = OriginalStartButtonPosition;

            CompletedAllTrials = false;
            EndBlock = false;
            StimIsChosen = false;
            currentTrial.GotTrialCorrect = false;

            if (TrialCount_InBlock == 0)
            {
                currentTask.CalculateBlockSummaryString();
                if(IsHuman)
                {
                    AdjustStartButtonPos(); //Adjust startButton position (move down) to make room for Title text. 
                    TitleTextGO.SetActive(true);    //Add title text above StartButton if first trial in block and Human is playing.
                }
            }

            if (MacMainDisplayBuild & !Debug.isDebugBuild && !AdjustedPositionsForMac) //adj text positions if running build with mac as main display
            {
                AdjustTextPosForMac();
                AdjustedPositionsForMac = true;
            }

            if (currentTrial.UseStarfield)
                Starfield.SetActive(true);
            
            TokenFBController.enabled = false;

            TimerText = TimerTextGO.GetComponent<TextMeshProUGUI>();

            SetTokenFeedbackTimes();
            SetStimStrings();
            SetShadowType(currentTrial.ShadowType, "ContinuousRecognition_DirectionalLight");
        });
        InitTrial.SpecifyTermination(() => mouseHandler.SelectionMatches(StartButton), DisplayStims);
        InitTrial.AddDefaultTerminationMethod(() =>
        {
            if (TitleTextGO.activeInHierarchy)
            {
                TitleTextGO.SetActive(false);
                TitleTextGO.transform.position = OriginalTitleTextPosition; //Reset Title Position for next block (in case its not a human block). 
            }

            if (IsHuman)
            {
                CR_CanvasGO.SetActive(true);
                SetScoreAndTrialsText();
                ScoreTextGO.SetActive(true);
                NumTrialsTextGO.SetActive(true);
                TimerBackdropGO.SetActive(true);
            }

            TokenFBController.SetTotalTokensNum(currentTrial.NumTokenBar);
            TokenFBController.enabled = true;

            EventCodeManager.SendCodeImmediate(TaskEventCodes["StartButtonSelected"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["StimOn"]);

            //MAKE EACH STIM GAME OBJECT FACE THE CAMERA WHILE SPAWNED
            if (currentTrial.StimFacingCamera)
                MakeStimsFaceCamera(trialStims);  

            if(currentTrial.ShakeStim)
                AddShakeStimScript(trialStims);
        });

        //DISPLAY STIMs state -----------------------------------------------------------------------------------------------------
        //Stim are turned on as soon as it enters DisplayStims state. no initialization method needed.
        DisplayStims.AddTimer(() => displayStimDuration.value, ChooseStim, () => TimeRemaining = chooseStimDuration.value);

        //CHOOSE STIM state -------------------------------------------------------------------------------------------------------
        MouseTracker.AddSelectionHandler(mouseHandler, ChooseStim);

        ChooseStim.AddInitializationMethod(() =>
        {
            chosenStimObj = null;
            chosenStimDef = null;
            StimIsChosen = false;

            if (TrialCount_InBlock == 0)
                TimeToCompletion_StartTime = Time.time;
        });

        ChooseStim.AddUpdateMethod(() =>
        {
            if (TimeRemaining > 0)
                TimeRemaining -= Time.deltaTime;

            TimerText.text = TimeRemaining.ToString("0");

            chosenStimObj = mouseHandler.SelectedGameObject;
            chosenStimDef = mouseHandler.SelectedStimDef;

            if (chosenStimDef != null) //They Clicked a Stim
            {
                currentTrial.TimeChosen = Time.time;
                currentTrial.TimeToChoice = currentTrial.TimeChosen - ChooseStim.TimingInfo.StartTimeAbsolute;
                TimeToChoice_Block.Add(currentTrial.TimeToChoice);
                CalculateBlockAvgTimeToChoice();

                if (!ChosenStimIndices.Contains(chosenStimDef.StimIndex)) //THEY GUESSED RIGHT
                {
                    currentTrial.GotTrialCorrect = true;

                    EventCodeManager.SendCodeImmediate(TaskEventCodes["CorrectResponse"]);
                    EventCodeManager.SendCodeImmediate(TaskEventCodes["TouchTargetStart"]);

                    //If chose a PNC Stim, remove it from PNC list.
                    if (currentTrial.PNC_Stim.Contains(chosenStimDef.StimIndex))
                        currentTrial.PNC_Stim.Remove(chosenStimDef.StimIndex);
                    //If Chose a New Stim, remove it from New list.
                    if (currentTrial.New_Stim.Contains(chosenStimDef.StimIndex))
                        currentTrial.New_Stim.Remove(chosenStimDef.StimIndex);

                    chosenStimDef.PreviouslyChosen = true;
                    currentTrial.PC_Stim.Add(chosenStimDef.StimIndex);
                    ChosenStimIndices.Add(chosenStimDef.StimIndex); //also adding to chosenIndices so I can keep them in order for display results. 

                    //REMOVE ALL NEW STIM THAT WEREN'T CHOSEN, FROM NEW STIM AND INTO PNC STIM. 
                    List<int> newStimToRemove = currentTrial.New_Stim.ToList();
                    foreach (var stim in newStimToRemove)
                    {
                        if (currentTrial.New_Stim.Contains(stim) && stim != chosenStimDef.StimIndex)
                        {
                            currentTrial.New_Stim.Remove(stim);
                            currentTrial.PNC_Stim.Add(stim);
                        }
                    }

                    //SINCE THEY GOT IT RIGHT, CHECK IF LAST TRIAL IN BLOCK OR IF THEY FOUND ALL THE STIM. 
                    if(currentTrial.PNC_Stim.Count == 0 || TrialCount_InBlock == currentTrial.MaxNumTrials-1)
                    {
                        TimeToCompletion_Block = Time.time - TimeToCompletion_StartTime;
                        CompletedAllTrials = true;
                        EndBlock = true;
                    }
                }

                else //THEY GUESSED WRONG
                {
                    currentTrial.WrongStimIndex = chosenStimDef.StimIndex; //identifies the stim they got wrong for Block FB purposes. 
                    TimeToCompletion_Block = Time.time - TimeToCompletion_StartTime;
                    EventCodeManager.SendCodeImmediate(TaskEventCodes["TouchDistractorStart"]);
                    EventCodeManager.SendCodeImmediate(TaskEventCodes["IncorrectResponse"]);
                }
            }

            if (chosenStimObj != null && chosenStimDef != null) //if they chose a stim 
                StimIsChosen = true;

            //Count NonStim Clicks:
            if (InputBroker.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(InputBroker.mousePosition);
                RaycastHit hit;
                if (!Physics.Raycast(ray, out hit))
                    NonStimTouches_Block++;
            }
        });
        ChooseStim.SpecifyTermination(() => StimIsChosen, TouchFeedback);
        ChooseStim.AddTimer(() => chooseStimDuration.value, TokenUpdate, () =>  //if time runs out
        {
            AudioFBController.Play("Negative");
            EndBlock = true;
            EventCodeManager.SendCodeImmediate(TaskEventCodes["NoChoice"]);
        });

        //TOUCH FEEDBACK state -------------------------------------------------------------------------------------------------------
        TouchFeedback.AddInitializationMethod(() =>
        {
            if (!StimIsChosen)
                return;

            if (currentTrial.GotTrialCorrect)
                HaloFBController.ShowPositive(chosenStimObj);
            else
                HaloFBController.ShowNegative(chosenStimObj);
            
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionVisualFbOn"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["SelectionAuditoryFbOn"]);
        });
        TouchFeedback.AddTimer(() => touchFbDuration.value, TokenUpdate);
        TouchFeedback.SpecifyTermination(() => !StimIsChosen, TokenUpdate);
        TouchFeedback.AddDefaultTerminationMethod(() =>
        {
            EventCodeManager.SendCodeImmediate(TaskEventCodes["SelectionVisualFbOff"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["StimOff"]);
        });

        //TOKEN UPDATE state ---------------------------------------------------------------------------------------------------------
        TokenUpdate.AddInitializationMethod(() =>
        {
            TokenUpdateStartTime = Time.time;
            HaloFBController.Destroy();

            if (!StimIsChosen)
                return;

            if (currentTrial.GotTrialCorrect)
            {
                if(TrialCount_InBlock == currentTrial.MaxNumTrials-1 || currentTrial.PNC_Stim.Count == 0) //If they get the last trial right (or find all stim), fill up bar!
                {
                    currentTrial.NumRewardPulses++;
                    int numToFillBar = currentTrial.NumTokenBar - TokenCount;
                    TokenFBController.AddTokens(chosenStimObj, numToFillBar);
                    TokenCount += numToFillBar;
                }
                else
                {
                    TokenFBController.AddTokens(chosenStimObj, currentTrial.RewardMag);
                    TokenCount++;
                }
                HandleTokenUpdate();
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["Rewarded"]);
            }
            else //Got wrong
            {
                TokenFBController.RemoveTokens(chosenStimObj, 1);
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["Unrewarded"]);
                TokenCount--;
                HandleTokenUpdate();
                EndBlock = true;
            }
        });
        TokenUpdate.SpecifyTermination(() => (Time.time - TokenUpdateStartTime > (tokenRevealDuration.value + tokenUpdateDuration.value + .05f) && !TokenFBController.IsAnimating()), DisplayResults);
        TokenUpdate.SpecifyTermination(() => !StimIsChosen, DisplayResults);
        TokenUpdate.AddDefaultTerminationMethod(() =>
        {
            if (currentTrial.ShakeStim)
                RemoveShakeStimScript(trialStims);

            if (IsHuman)
            {
                TimerBackdropGO.SetActive(false);
                ScoreTextGO.SetActive(false);
                NumTrialsTextGO.SetActive(false);
            }
        });
        //DISPLAY RESULTS state --------------------------------------------------------------------------------------------------------
        DisplayResults.AddInitializationMethod(() =>
        {
            if (currentTrial.GotTrialCorrect)
                Score += ((TrialCount_InBlock + 1) * 100);

            if (EndBlock)
            {
                GenerateBlockFeedback();

                if (IsHuman)
                {
                    float Y_Offset = GetOffsetY();

                    if (CompletedAllTrials)
                    {
                        YouWinTextGO.transform.localPosition = new Vector3(YouWinTextGO.transform.localPosition.x, YouWinTextGO.transform.localPosition.y - Y_Offset, YouWinTextGO.transform.localPosition.z);
                        YouWinTextGO.GetComponent<TextMeshProUGUI>().text = $"WINNER! \n New HighScore: {Score} xp";
                        YouWinTextGO.SetActive(true);
                        AudioFBController.Play("CR_BlockCompleted");
                    }
                    else
                    {
                        YouLoseTextGO.transform.localPosition = new Vector3(YouLoseTextGO.transform.localPosition.x, YouLoseTextGO.transform.localPosition.y - Y_Offset, YouLoseTextGO.transform.localPosition.z);
                        YouLoseTextGO.GetComponent<TextMeshProUGUI>().text = $"Game Over \n HighScore: {Score} xp";
                        YouLoseTextGO.SetActive(true);
                        //AudioFBController.Play("CR_BlockFailed");
                        AudioFBController.Play("CR_SouthParkFail");
                    }
                }
            }
        });
        DisplayResults.AddTimer(() => displayResultsDuration.value, ITI, () =>
        {
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["StimOff"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["ContextOff"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["TrlEnd"]);
        });
        DisplayResults.SpecifyTermination(() => !EndBlock && !CompletedAllTrials, ITI);
        DisplayResults.AddDefaultTerminationMethod(() =>
        {
            if(currentTrial.ShakeStim)
                RemoveShakeStimScript(trialStims);

            TokenFBController.enabled = false;
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["StimOff"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["ContextOff"]);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["TrlEnd"]);
        });

        //ITI State----------------------------------------------------------------------------------------------------------------------
        ITI.AddTimer(() => itiDuration.value, FinishTrial);

        //FinishTrial State (default state) ----------------------------------------------------------------------------------------------------------------------
        FinishTrial.AddInitializationMethod(() =>
        {
            if(AbortCode == 0) //Normal
            {
                NumTrials_Block++;
                if(currentTrial.GotTrialCorrect)
                    NumCorrect_Block++;

                currentTask.CalculateBlockSummaryString();
            }
            else if (AbortCode == AbortCodeDict["Pause"]) //If used Pause hotkey to end trial, end entire Block
                EndBlock = true;
        });
        DefineTrialData();
        DefineFrameData();
    }


    //HELPER FUNCTIONS -----------------------------------------------------------------------------------------
    public override void FinishTrialCleanup()
    {
        DeactivateTextObjects();
        DestroyFeedbackBorders();
        ContextActive = false;
        EventCodeManager.SendCodeNextFrame(TaskEventCodes["TrlStart"]); //next trial starts next frame
    }

    public void ResetBlockVariables()
    {
        AdjustedPositionsForMac = false;
        ChosenStimIndices.Clear();
        NonStimTouches_Block = 0;
        NumTrials_Block = 0;
        NumCorrect_Block = 0;
        NumTbCompletions_Block = 0;
        TimeToChoice_Block.Clear();
        AvgTimeToChoice_Block = 0;
        TimeToCompletion_Block = 0;
        NumRewards_Block = 0;
        Score = 0;
    }

    public void SetControllerBlockValues()
    {
        TokenFBController.SetFlashingTime(1f);
        HaloFBController.SetHaloIntensity(2);
        HaloFBController.SetHaloSize(1);
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

        if (YouWinTextGO.activeInHierarchy)
        {
            YouWinTextGO.SetActive(false);
            YouWinTextGO.transform.position = OriginalFbTextPosition; //Reset position for next Block;  
        }
        if (YouLoseTextGO.activeInHierarchy)
        {
            YouLoseTextGO.SetActive(false);
            YouLoseTextGO.transform.position = OriginalFbTextPosition; //Reset position for next Block;
        }
    }

    void MakeStimPopOut()
    {
        foreach(ContinuousRecognition_StimDef stim in trialStims.stimDefs)
        {
            if (!stim.PreviouslyChosen)
                stim.StimGameObject.transform.localScale *= 1.35f;
        }
    } //can use to make game easier if need to debug. 

    void AdjustStartButtonPos()
    {
        Vector3 buttonPos = StartButton.transform.position;
        buttonPos.y -= 1f;
        StartButton.transform.position = buttonPos;
    }

    void AdjustTextPosForMac() //When running a build instead of hitting play in editor:
    {
        Vector3 biggerScale = TokenFBController.transform.localScale * 2f;
        TokenFBController.transform.localScale = biggerScale;
        TokenFBController.tokenSize = 200;

        //move Timer up
        Vector3 Pos = OriginalTimerPosition;
        Pos.y -= 1.5f;
        TimerBackdropGO.transform.position = Pos;

        //move TitleText down
        Vector3 Position = TitleTextGO.transform.position;
        Position.y -= 1f;
        TitleTextGO.transform.position = Position;
    }

    float GetOffsetY()
    {
        //Function used to adjust the YouWin/YouLost text positioning for the human version. 
        float yOffset = 0;
        switch (NumFeedbackRows)
        {
            case 1:
                if (MacMainDisplayBuild && !Debug.isDebugBuild)
                    yOffset = 95f;
                else
                    yOffset = 65f;
                break;
            case 2:
                if (MacMainDisplayBuild && !Debug.isDebugBuild)
                    yOffset = 80f;
                else
                    yOffset = 55f;
                break;
            case 3:
                if (MacMainDisplayBuild && !Debug.isDebugBuild)
                    yOffset = 30f;
                else
                    yOffset = 15f;
                break;
            case 4:
                if (MacMainDisplayBuild && !Debug.isDebugBuild)
                    yOffset = 0f;
                else
                    yOffset = -25f;
                break;
            //case 5:
            //    if (MacMainDisplayBuild && !Debug.isDebugBuild)
            //        yOffset = -30f; //Check!
            //    else
            //        yOffset = -35f; //Check! (not checked but could be close)
            //    break;
        }
        return yOffset;
    }

    void SetScoreAndTrialsText()
    {
        //Set the Score and NumTrials texts at the beginning of the trial. 
        ScoreTextGO.GetComponent<TextMeshProUGUI>().text = $"SCORE: {Score}";
        NumTrialsTextGO.GetComponent<TextMeshProUGUI>().text = $"TRIAL: {TrialCount_InBlock + 1}";
    }

    void SetTrialSummaryString()
    {
        TrialSummaryString = "<b>Trial #" + (TrialCount_InBlock + 1) + " In Block" + "</b>" +
                             "\nPC_Stim: " + currentTrial.PC_Stim.Count +
                             "\nNew_Stim: " + currentTrial.New_Stim.Count +
                             "\nPNC_Stim: " + currentTrial.PNC_Stim.Count;
    }

    Vector3[] CenterFeedbackLocations(Vector3[] locations, int numLocations)
    {
        int MaxNumPerRow = 6;
        float max = 2.25f;
        
        int numRows = 1;
        if (numLocations > 6) numRows++;
        if (numLocations > 12) numRows++;
        if (numLocations > 18) numRows++;
        //if (numLocations > 24) numRows++;

        NumFeedbackRows = numRows; //Setting Global variable for use in centering the feedback text above the stim (for human version)

        int R1_Length = 0;
        int R2_Length = 0;
        int R3_Length = 0;
        int R4_Length = 0;
        //int R5_Length = 0;

        //Calculate num stim in each row. 
        switch (numRows)
        {
            case 1:
                R1_Length = numLocations;
                break;
            case 2:
                if(numLocations % 2 == 0)
                    R1_Length = R2_Length = numLocations / 2;
                else
                {
                    R1_Length = (int) Math.Floor((decimal)numLocations / 2); //round it down and increase next row by 1.
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
                if(numLocations % 4 == 0)
                    R1_Length = R2_Length = R3_Length = R4_Length = numLocations / 4;
                else
                {
                    R1_Length = R2_Length = R3_Length = (int)Math.Floor((decimal)numLocations / 4);
                    R4_Length = (int)Math.Ceiling((decimal)numLocations / 4);

                    int diff = numLocations - (R1_Length + R2_Length + R3_Length + R4_Length);
                    if (diff == 1) R3_Length++;
                    else if (diff == 2)
                    {
                        if(R4_Length == MaxNumPerRow)
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
            //case 5:
            //    if (numLocations % 5 == 0)
            //        R1_Length = R2_Length = R3_Length = R4_Length = R5_Length = numLocations / 5;
            //    else
            //    {
            //        R1_Length = R2_Length = R3_Length = R4_Length = (int)Math.Floor((decimal)numLocations / 5);
            //        R5_Length = (int)Math.Ceiling((decimal)numLocations / 5);

            //        int diff = numLocations - (R1_Length + R2_Length + R3_Length + R4_Length + R5_Length);
            //        if (diff == 1) R4_Length++;
            //        else if (diff == 2)
            //        {
            //            R3_Length++;
            //            R4_Length++;
            //        }
            //        else if (diff == 3)
            //        {
            //            R2_Length++;
            //            R3_Length++;
            //            R4_Length++;
            //        }
            //    }
            //    break;
        }

        float leftMargin;
        float rightMargin;
        float leftMarginNeeded;
        float leftShiftAmount;

        int index = 0;
        int difference = 0;
        Vector3 current;
        List<Vector3> locList = new List<Vector3>();

        //----- CENTER HORIZONTALLY--------------------------------
        //Center ROW 1:
        if (R1_Length > 0)
        {
            leftMargin = 4 - Math.Abs(locations[0].x);
            rightMargin = 4f - locations[R1_Length - 1].x;
            for (int i = index; i < R1_Length; i++)
            {
                leftMarginNeeded = (leftMargin + rightMargin) / 2;
                leftShiftAmount = leftMarginNeeded - leftMargin;
                current = locations[i];
                current.x += leftShiftAmount;
                locList.Add(current);
                index++;
            }
            if (R2_Length > 0)
                difference = MaxNumPerRow - R1_Length;
        }

        //Center ROW 2:
        if (R2_Length > 0)
        {
            index += difference;
            leftMargin = 4 - Math.Abs(locations[index].x);
            rightMargin = 4f - locations[index + R2_Length - 1].x;
            int indy = index;
            for (int i = index; i < (indy + R2_Length); i++)
            {
                leftMarginNeeded = (leftMargin + rightMargin) / 2;
                leftShiftAmount = leftMarginNeeded - leftMargin;
                current = locations[i];
                current.x += leftShiftAmount;
                locList.Add(current);
                index++;
            }
            if(R3_Length > 0)
                difference = MaxNumPerRow - R2_Length;
        }

        //Center ROW 3:
        if (R3_Length > 0)
        {
            index += difference;
            leftMargin = 4 - Math.Abs(locations[index].x);
            rightMargin = 4f - locations[index + R3_Length - 1].x;
            int indy = index;
            for (int i = index; i < (indy + R3_Length); i++)
            {
                leftMarginNeeded = (leftMargin + rightMargin) / 2;
                leftShiftAmount = leftMarginNeeded - leftMargin;
                current = locations[i];
                current.x += leftShiftAmount;
                locList.Add(current);
                index++;
            }
            if (R4_Length > 0)
                difference = MaxNumPerRow - R3_Length;
        }

        //Center ROW 4:
        if (R4_Length > 0)
        {
            index += difference;
            leftMargin = 4 - Math.Abs(locations[index].x);
            rightMargin = 4f - locations[index + R4_Length - 1].x;
            int indy = index;
            for (int i = index; i < (indy + R4_Length); i++)
            {
                leftMarginNeeded = (leftMargin + rightMargin) / 2;
                leftShiftAmount = leftMarginNeeded - leftMargin;
                current = locations[i];
                current.x += leftShiftAmount;
                locList.Add(current);
                index++;
            }
            //if (R5_Length > 0)
            //    difference = MaxNumPerRow - R4_Length;
        }

        //Center ROW 5:
        //if (R5_Length > 0)
        //{
        //    index += difference;
        //    leftMargin = 4 - Math.Abs(locations[index].x);
        //    rightMargin = 4f - locations[index + R5_Length - 1].x;
        //    int indy = index;
        //    for (int i = index; i < (indy + R5_Length); i++)
        //    {
        //        leftMarginNeeded = (leftMargin + rightMargin) / 2;
        //        leftShiftAmount = leftMarginNeeded - leftMargin;
        //        current = locations[i];
        //        current.x += leftShiftAmount;
        //        locList.Add(current);
        //        index++;
        //    }
        //}

        Vector3[] FinalLocations = locList.ToArray();

        //----- CENTER VERTICALLY-----------------------------------------------
        if (numRows > 3)
            max /= 2;

        float topMargin = max - FinalLocations[0].y;
        float bottomMargin = FinalLocations[FinalLocations.Length - 1].y + max;

        float shiftDownNeeded = (topMargin + bottomMargin) / 2;
        float shiftDownAmount = shiftDownNeeded - topMargin;

        if (IsHuman && NumFeedbackRows > 1) //shift down more if human playing cuz text above stim 
        {
            for (int i = 0; i < FinalLocations.Length; i++)
                FinalLocations[i].y -= (shiftDownAmount + .25f);
        }
        else
        {
            for (int i = 0; i < FinalLocations.Length; i++)
                FinalLocations[i].y -= shiftDownAmount;
        }

        return FinalLocations;
    }

    //Generate the correct number of New, PC, and PNC stim for each trial. Called when the trial is defined!
    //The TrialStims group are auto loaded in the SetupTrial StateInitialization, and destroyed in the FinishTrial StateTermination
    protected override void DefineTrialStims()
    {
        if (TrialCount_InBlock == 0)
        {
            trialStims = null;
            Score = 0;

            //clear stim lists in case it's NOT the first block!
            ClearCurrentTrialStimLists();

            //Add each block stim to unseen list.
            var numBlockStims = currentTrial.BlockStimIndices.Length;
            for (int i = 0; i < numBlockStims; i++) currentTrial.Unseen_Stim.Add(currentTrial.BlockStimIndices[i]);
            
            //Pick 2 random New stim and add to TrialStimIndices and NewStim. Also remove from UnseenStim.
            int[] tempArray = new int[currentTrial.NumObjectsMinMax[0]];
            for (int i = 0; i < currentTrial.NumObjectsMinMax[0]; i++) 
            {
                int ranNum = Random.Range(0, numBlockStims);
                while (Array.IndexOf(tempArray, ranNum) != -1)
                {
                    ranNum = Random.Range(0, numBlockStims);
                }
                tempArray[i] = ranNum;
                currentTrial.TrialStimIndices.Add(ranNum);
                currentTrial.Unseen_Stim.Remove(ranNum);
                currentTrial.New_Stim.Add(ranNum);
            }

            trialStims = new StimGroup("TrialStims", ExternalStims, currentTrial.TrialStimIndices);
            foreach (ContinuousRecognition_StimDef stim in trialStims.stimDefs)
                stim.PreviouslyChosen = false;
            trialStims.SetLocations(currentTrial.TrialStimLocations);
            TrialStims.Add(trialStims);

        }
        else if((TrialCount_InBlock > 0 && TrialCount_InBlock <= (currentTrial.MaxNumStim-2)) || TrialCount_InBlock > 0 && !currentTrial.FindAllStim)
        {
            currentTrial.TrialStimIndices.Clear();

            float[] stimPercentages = GetStimRatioPercentages(currentTrial.InitialStimRatio);
            int[] stimNumbers = GetStimNumbers(stimPercentages);

            int PC_Num = stimNumbers[0];
            int New_Num = stimNumbers[1];
            int PNC_Num = stimNumbers[2];

            List<int> NewStim;

            if (TrialCount_InBlock == 1)
                NewStim = ShuffleList(currentTrial.Unseen_Stim).ToList(); //shuffle unseen list during first (second overall) trial! (only needed once). 
            else NewStim = currentTrial.Unseen_Stim.ToList();

            if (NewStim.Count > 1)
                    NewStim = NewStim.GetRange(0, New_Num);

            for (int i = 0; i < NewStim.Count; i++)
            {
                int current = NewStim[i];
                currentTrial.TrialStimIndices.Add(current);
                currentTrial.Unseen_Stim.Remove(current);
                currentTrial.New_Stim.Add(current);
            }

            List<int> PC_Copy = ShuffleList(currentTrial.PC_Stim).ToList();
            if (PC_Copy.Count > 1)
                PC_Copy = PC_Copy.GetRange(0, PC_Num);
            for (int i = 0; i < PC_Copy.Count; i++)
                currentTrial.TrialStimIndices.Add(PC_Copy[i]);
            

            List<int> PNC_Copy = ShuffleList(currentTrial.PNC_Stim).ToList();
            if (PNC_Copy.Count > 1)
                PNC_Copy = PNC_Copy.GetRange(0, PNC_Num);
            for (int i = 0; i < PNC_Copy.Count; i++)
                currentTrial.TrialStimIndices.Add(PNC_Copy[i]);

            trialStims = new StimGroup($"TrialStims", ExternalStims, currentTrial.TrialStimIndices);
            trialStims.SetLocations(currentTrial.TrialStimLocations);
            TrialStims.Add(trialStims);
        }

        else //The Non-Increasing trials
        {
            currentTrial.TrialStimIndices.Clear();

            var totalNeeded = currentTrial.NumObjectsMinMax[1];
            var num_PNC = currentTrial.PNC_Stim.Count;
            var num_PC = totalNeeded - num_PNC;

            //Add PNC Stim to trialIndices
            foreach (int num in currentTrial.PNC_Stim)
                currentTrial.TrialStimIndices.Add(num);

            //Add PC Stim to trialIndices.
            for(int i = 0; i < num_PC; i++)
                currentTrial.TrialStimIndices.Add(currentTrial.PC_Stim[i]);
            
            trialStims = new StimGroup($"TrialStims", ExternalStims, currentTrial.TrialStimIndices);
            trialStims.SetLocations(currentTrial.TrialStimLocations);
            TrialStims.Add(trialStims);
        }

        trialStims.SetVisibilityOnOffStates(GetStateFromName("DisplayStims"), GetStateFromName("TokenUpdate"));
    }

    void CalculateBlockAvgTimeToChoice()
    {
        if (TimeToChoice_Block.Count == 0)
            AvgTimeToChoice_Block = 0;

        float sum = 0;
        foreach (float choice in TimeToChoice_Block)
            sum += choice;
        AvgTimeToChoice_Block = sum / TimeToChoice_Block.Count;
    }

    void GenerateBlockFeedback()
    {
        Starfield.SetActive(false);
        TokenFBController.enabled = false;

        if (!StimIsChosen && ChosenStimIndices.Count < 1)
            return;

        if (CompletedAllTrials || !StimIsChosen) //!stimchosen means time ran out. 
        {
            RightGroup = new StimGroup("Right");
            Vector3[] FeedbackLocations = new Vector3[ChosenStimIndices.Count];
            FeedbackLocations = CenterFeedbackLocations(currentTrial.TrialFeedbackLocations, FeedbackLocations.Length);

            RightGroup = new StimGroup("Right", ExternalStims, ChosenStimIndices);
            GenerateFeedbackStim(RightGroup, FeedbackLocations);
            GenerateFeedbackBorders(RightGroup);

            if (currentTrial.StimFacingCamera)
                MakeStimsFaceCamera(RightGroup);
        }
        else
        {
            RightGroup = new StimGroup("Right");
            Vector3[] FeedbackLocations = new Vector3[ChosenStimIndices.Count + 1];
            FeedbackLocations = CenterFeedbackLocations(currentTrial.TrialFeedbackLocations, FeedbackLocations.Length);

            RightGroup = new StimGroup("Right", ExternalStims, ChosenStimIndices);
            GenerateFeedbackStim(RightGroup, FeedbackLocations.Take(FeedbackLocations.Length - 1).ToArray());
            GenerateFeedbackBorders(RightGroup);

            if (currentTrial.StimFacingCamera)
                MakeStimsFaceCamera(RightGroup);

            WrongGroup = new StimGroup("Wrong");
            StimDef wrongStim = ExternalStims.stimDefs[currentTrial.WrongStimIndex].CopyStimDef(WrongGroup);
            wrongStim.StimGameObject = null;
            GenerateFeedbackStim(WrongGroup, FeedbackLocations.Skip(FeedbackLocations.Length - 1).Take(1).ToArray());
            GenerateFeedbackBorders(WrongGroup);

            if (currentTrial.StimFacingCamera)
                wrongStim.StimGameObject.AddComponent<FaceCamera>();
        }
    }

    void MakeStimsFaceCamera(StimGroup stims)
    {
        foreach (var stim in stims.stimDefs)
            stim.StimGameObject.AddComponent<FaceCamera>();
    }

    void GenerateFeedbackStim(StimGroup group, Vector3[] locations)
    {
        TrialStims.Add(group);
        group.SetLocations(locations);
        group.LoadStims();
        group.ToggleVisibility(true);
    }

    void GenerateFeedbackBorders(StimGroup group)
    {
        if (BorderPrefabList.Count == 0)
            BorderPrefabList = new List<GameObject>();

        foreach (var stim in group.stimDefs)
        {
            if (stim.StimIndex == currentTrial.WrongStimIndex)
            {
                GameObject redBorder = Instantiate(RedBorderPrefab, stim.StimGameObject.transform.position, Quaternion.identity);
                BorderPrefabList.Add(redBorder); //Add each to list so I can destroy them together
            }
            else
            {
                GameObject greenBorder = Instantiate(GreenBorderPrefab, stim.StimGameObject.transform.position, Quaternion.identity);
                BorderPrefabList.Add(greenBorder);
            }
        }

    }

    void DestroyFeedbackBorders()
    {
        foreach (GameObject border in BorderPrefabList)
        {
            if (border != null)
                Destroy(border);
        }
        BorderPrefabList.Clear();
    }

    void HandleTokenUpdate()
    {
        if (TokenCount == currentTrial.NumTokenBar)
        {
            NumTbCompletions_Block++;
            NumRewards_Block += currentTrial.NumRewardPulses;
            TokenCount = 0;

            if (SyncBoxController != null)
            {
                SyncBoxController.SendRewardPulses(currentTrial.NumRewardPulses, currentTrial.PulseSize);
                EventCodeManager.SendCodeImmediate(TaskEventCodes["Fluid1Onset"]);
            }
        }
    }

    List<int> ShuffleList(List<int> list)
    {
        if (list.Count == 1)
            return list;
        
        else
        {
            for (int i = 0; i < list.Count - 1; i++)
            {
                int temp = list[i];
                int rand = Random.Range(1, list.Count);
                list[i] = list[rand];
                list[rand] = temp;
            }
            return list;
        }
    }

    protected override bool CheckBlockEnd()
    {
        return EndBlock;
    }

    void SetStimStrings()
    {
        //Locations String;
        string s = "";
        if (currentTrial.TrialStimLocations.Length > 0)
        {
            s += "[";
            foreach (var trial in currentTrial.TrialStimLocations)
            {
                s += trial + ", ";
            }
            s = s.Substring(0, s.Length - 2);
            s += "]";
            currentTrial.Locations_String = s;
        }
        if (currentTrial.Locations_String == null)
            currentTrial.PNC_String = "-";

        //PC String
        s = "";
        if (currentTrial.PC_Stim.Count > 0)
        {
            s += "[";
            foreach (var trial in currentTrial.PC_Stim)
            {
                s += trial + ", ";
            }
            s = s.Substring(0, s.Length - 2);
            s += "]";
            currentTrial.PC_String = s;
        }
        if (currentTrial.PC_String == null)
            currentTrial.PC_String = "-";

        //New String
        s = "";
        if (currentTrial.PNC_Stim.Count > 0)
        {
            s += "[";
            foreach (var trial in currentTrial.PNC_Stim)
            {
                s += trial + ", ";
            }
            s = s.Substring(0, s.Length - 2);
            s += "]";
            currentTrial.PNC_String = s;
        }
        if (currentTrial.PNC_String == null)
            currentTrial.PNC_String = "-";

        //PNC String
        s = "";
        if (currentTrial.New_Stim.Count > 0)
        {
            s += "[";
            foreach (var trial in currentTrial.New_Stim)
            {
                s += trial + ", ";
            }
            s = s.Substring(0, s.Length - 2);
            s += "]";
            currentTrial.New_String = s;
        }
        if (currentTrial.New_String == null)
            currentTrial.New_String = "-";

    }

    void DefineTrialData()
    {
        TrialData.AddDatum("Context", () => currentTrial.ContextName);
        TrialData.AddDatum("Starfield", () => currentTrial.UseStarfield);
        TrialData.AddDatum("Num_UnseenStim", () => currentTrial.Unseen_Stim.Count);
        TrialData.AddDatum("PC_Stim", () => currentTrial.PC_String);
        TrialData.AddDatum("New_Stim", () => currentTrial.New_String);
        TrialData.AddDatum("PNC_Stim", () => currentTrial.PNC_String);
        TrialData.AddDatum("StimLocations", () => currentTrial.Locations_String);
        TrialData.AddDatum("ChoseCorrectly", () => currentTrial.GotTrialCorrect);
        TrialData.AddDatum("CurrentTrialStims", () => currentTrial.TrialStimIndices);
        TrialData.AddDatum("PC_Percentage", () => currentTrial.PC_Percentage_String);
    }

    void DefineFrameData()
    {
        FrameData.AddDatum("TouchPosition", () => InputBroker.mousePosition);
        FrameData.AddDatum("ContextActive", () => ContextActive);
        FrameData.AddDatum("StartButton", () => StartButton.activeInHierarchy);
        FrameData.AddDatum("TrialStimShown", () => trialStims.IsActive);
        FrameData.AddDatum("StarfieldActive", () => Starfield.activeInHierarchy);
    }

    void ClearCurrentTrialStimLists()
    {
        ChosenStimIndices.Clear();
        currentTrial.TrialStimIndices.Clear();
        currentTrial.New_Stim.Clear();
        currentTrial.PNC_Stim.Clear();
        currentTrial.PC_Stim.Clear();
        currentTrial.Unseen_Stim.Clear();

    }

    float[] GetStimRatioPercentages(int[] ratioArray)
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

    int[] GetStimNumbers(float[] stimPercentages)
    {
        //Function to calculate the correct num of stim for a trial.
        //Starts by understating each num, then it makes sure there are enough available (some ratios could overstate the stim),
        //then it adjusts the stim category that will make the PC% the closest to its supposed percentage. 
        int PC_Num = (int)Math.Floor(stimPercentages[0] * currentTrial.NumTrialStims);
        int New_Num = (int)Math.Floor(stimPercentages[1] * currentTrial.NumTrialStims);
        int PNC_Num = (int)Math.Floor(stimPercentages[2] * currentTrial.NumTrialStims);
        if (PC_Num == 0) PC_Num = 1;
        if (New_Num == 0) New_Num = 1;
        if (PNC_Num == 0) PNC_Num = 1;

        int PC_Available = currentTrial.PC_Stim.Count;
        int New_Available = currentTrial.Unseen_Stim.Count;
        int PNC_Available = currentTrial.PNC_Stim.Count;

        //Ensure a crazy stim ratio doesn't calculate more stim than available in that category.
        while (PC_Num > PC_Available) PC_Num--;
        while (New_Num > New_Available) New_Num--;
        while (PNC_Num > PNC_Available) PNC_Num--;

        float PC_TargetPerc = stimPercentages[0];
        int temp = 2;
        while((PC_Num + New_Num + PNC_Num) < currentTrial.NumTrialStims)
        {
            //calculate PC Percentage difference.
            float currentPerc = PC_Num / (PC_Num + New_Num + PNC_Num);
            float percDiff = currentPerc - PC_TargetPerc;

            //determine whether 1)Adding to PC, or 2)Adding to New/PNC makes the PercDiff smaller.
            float PC_AddPerc = (PC_Num + 1) / (PC_Num + 1 + New_Num + PNC_Num);
            float PC_AddDiff = PC_AddPerc - PC_TargetPerc;

            float NonPC_AddPerc = PC_Num / (PC_Num + 1 + New_Num + PNC_Num);
            float NonPC_AddDiff = NonPC_AddPerc - PC_TargetPerc;

            if(PC_AddDiff < NonPC_AddDiff)
            {
                PC_Num++;
            }
            else
            {
                if (temp % 2 == 0)
                    New_Num++;
                else
                    PNC_Num++;
            }
        }
        return new[] { PC_Num, New_Num, PNC_Num };
    }

    void LoadConfigUIVariables()
    {
        displayStimDuration = ConfigUiVariables.get<ConfigNumber>("displayStimDuration");
        chooseStimDuration = ConfigUiVariables.get<ConfigNumber>("chooseStimDuration");
        touchFbDuration = ConfigUiVariables.get<ConfigNumber>("touchFbDuration");
        tokenRevealDuration = ConfigUiVariables.get<ConfigNumber>("tokenRevealDuration");
        tokenUpdateDuration = ConfigUiVariables.get<ConfigNumber>("tokenUpdateDuration");
        displayResultsDuration = ConfigUiVariables.get<ConfigNumber>("displayResultsDuration");
        itiDuration = ConfigUiVariables.get<ConfigNumber>("itiDuration");
        VariablesLoaded = true;
    }

    void SetTokenFeedbackTimes()
    {
        TokenFBController.SetRevealTime(tokenRevealDuration.value);
        TokenFBController.SetUpdateTime(tokenUpdateDuration.value);
    }

}
