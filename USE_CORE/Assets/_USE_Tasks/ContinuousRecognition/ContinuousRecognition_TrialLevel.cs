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
using USE_UI;


public class ContinuousRecognition_TrialLevel : ControlLevel_Trial_Template
{
    public ContinuousRecognition_TrialDef currentTrial => GetCurrentTrialDef<ContinuousRecognition_TrialDef>();
    public ContinuousRecognition_TaskLevel currentTask => GetTaskLevel<ContinuousRecognition_TaskLevel>();

    [HideInInspector] public USE_StartButton USE_StartButton;
    [HideInInspector] public GameObject StartButton;

    public TextMeshProUGUI TimerText;
    public GameObject TimerTextGO;
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

    private StimGroup trialStims;
    [HideInInspector] public  List<int> ChosenStimIndices;
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

    private int NumFeedbackRows;
    private int Score;

    private Vector3 OriginalFbTextPosition;
    private Vector3 OriginalTitleTextPosition;
    private Vector3 OriginalStartButtonPosition;
    private Vector3 OriginalTimerPosition;

    private StimGroup RightGroup;
    private StimGroup WrongGroup;

    [HideInInspector] public float ButtonScale;
    [HideInInspector] public Vector3 ButtonPosition;

    [HideInInspector] GameObject ChosenGO;
    [HideInInspector] ContinuousRecognition_StimDef ChosenStim;

    private int NumPC_Trial;
    private int NumNew_Trial;
    private int NumPNC_Trial;

    [HideInInspector] public bool MakeStimPopOut;

    private PlayerViewPanel playerView;
    private Transform playerViewParent;
    private GameObject playerViewText;
    public List<GameObject> playerViewTextList;
    
    [HideInInspector] public float TouchFeedbackDuration;

    //Config Variables
    [HideInInspector]
    public ConfigNumber minObjectTouchDuration, maxObjectTouchDuration, displayStimDuration, chooseStimDuration, itiDuration, touchFbDuration, displayResultsDuration, tokenUpdateDuration, tokenRevealDuration;

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

        OriginalFbTextPosition = YouLoseTextGO.transform.position;
        OriginalTimerPosition = TimerBackdropGO.transform.position;

        playerView = new PlayerViewPanel();
        playerViewText = new GameObject();
        playerViewTextList = new List<GameObject>();

        Add_ControlLevel_InitializationMethod(() =>
        {
            SetControllerBlockValues();

            if (StartButton == null)
            {
                USE_StartButton = new USE_StartButton(CR_CanvasGO.GetComponent<Canvas>(), ButtonPosition, ButtonScale);
                StartButton = USE_StartButton.StartButtonGO;
                USE_StartButton.SetVisibilityOnOffStates(InitTrial, InitTrial);
            }
            #if (!UNITY_WEBGL)
                playerViewParent = GameObject.Find("MainCameraCopy").transform;
            #endif
        });

        //SETUP TRIAL state -----------------------------------------------------------------------------------------------------
        SetupTrial.AddInitializationMethod(() =>
        {
            if (!CR_CanvasGO.activeInHierarchy)
                CR_CanvasGO.SetActive(true);
        });
        SetupTrial.SpecifyTermination(() => true, InitTrial);

        //INIT Trial state -------------------------------------------------------------------------------------------------------
        var ShotgunHandler = SelectionTracker.SetupSelectionHandler("trial", "TouchShotgun", InitTrial, ChooseStim);
        ShotgunHandler.shotgunRaycast.SetShotgunVariables(ShotgunRaycastCircleSize_DVA, ParticipantDistance_CM, ShotgunRaycastSpacing_DVA);
        TouchFBController.EnableTouchFeedback(ShotgunHandler, TouchFeedbackDuration, ButtonScale, CR_CanvasGO);

        InitTrial.AddInitializationMethod(() =>
        {
            NumFeedbackRows = 0;

            if (!VariablesLoaded)
                LoadConfigUIVariables();

            SetTrialSummaryString();

            currentTask.CalculateBlockSummaryString();

            if (TrialCount_InTask != 0)
                currentTask.SetTaskSummaryString();

            if (MacMainDisplayBuild & !Application.isEditor && !AdjustedPositionsForMac) //adj text positions if running build with mac as main display
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

            if (ShotgunHandler.AllSelections.Count > 0)
                ShotgunHandler.ClearSelections();
            ShotgunHandler.MinDuration = minObjectTouchDuration.value;
            ShotgunHandler.MaxDuration = maxObjectTouchDuration.value;
        });
        InitTrial.SpecifyTermination(() => ShotgunHandler.LastSuccessfulSelectionMatches(StartButton), DisplayStims);
        InitTrial.AddDefaultTerminationMethod(() =>
        {
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

            if (currentTrial.StimFacingCamera)
                MakeStimsFaceCamera(trialStims);  

            if(currentTrial.ShakeStim)
                AddShakeStimScript(trialStims);

            EventCodeManager.SendCodeImmediate(SessionEventCodes["StartButtonSelected"]);
            EventCodeManager.SendCodeNextFrame(SessionEventCodes["StimOn"]);

            if(MakeStimPopOut)
                PopStimOut();
        });

        //DISPLAY STIMs state -----------------------------------------------------------------------------------------------------
        DisplayStims.AddTimer(() => displayStimDuration.value, ChooseStim, () => TimeRemaining = chooseStimDuration.value);

        //CHOOSE STIM state -------------------------------------------------------------------------------------------------------
        ChooseStim.AddInitializationMethod(() =>
        {
            #if (!UNITY_WEBGL)
                CreateTextOnExperimenterDisplay();
            #endif

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
            if(InputBroker.GetMouseButtonDown(0))
            {
                GameObject go = InputBroker.RaycastBoth(InputBroker.mousePosition);
                if (go != null)
                    Debug.Log("HIT: " + go.name);
            }

            if (TimeRemaining > 0)
                TimeRemaining -= Time.deltaTime;

            TimerText.text = TimeRemaining.ToString("0");

            ChosenGO = ShotgunHandler.LastSelection.SelectedGameObject;
            ChosenStim = ChosenGO?.GetComponent<StimDefPointer>()?.GetStimDef<ContinuousRecognition_StimDef>();

            if (ChosenStim != null) //They Clicked a Stim
            {
                currentTrial.TimeChosen = Time.time;
                currentTrial.TimeToChoice = currentTrial.TimeChosen - ChooseStim.TimingInfo.StartTimeAbsolute;
                TimeToChoice_Block.Add(currentTrial.TimeToChoice);
                CalculateBlockAvgTimeToChoice();

                if (!ChosenStimIndices.Contains(ChosenStim.StimIndex)) //THEY GUESSED RIGHT
                {
                    currentTrial.GotTrialCorrect = true;

                    EventCodeManager.SendCodeImmediate(SessionEventCodes["CorrectResponse"]);

                    //If chose a PNC Stim, remove it from PNC list.
                    if (currentTrial.PNC_Stim.Contains(ChosenStim.StimIndex))
                        currentTrial.PNC_Stim.Remove(ChosenStim.StimIndex);
                    //If Chose a New Stim, remove it from New list.
                    if (currentTrial.New_Stim.Contains(ChosenStim.StimIndex))
                        currentTrial.New_Stim.Remove(ChosenStim.StimIndex);

                    ChosenStim.PreviouslyChosen = true;
                    currentTrial.PC_Stim.Add(ChosenStim.StimIndex);
                    ChosenStimIndices.Add(ChosenStim.StimIndex); //also adding to chosenIndices so I can keep them in order for display results. 

                    //REMOVE ALL NEW STIM THAT WEREN'T CHOSEN, FROM NEW STIM AND INTO PNC STIM. 
                    List<int> newStimToRemove = currentTrial.New_Stim.ToList();
                    foreach (var stim in newStimToRemove)
                    {
                        if (currentTrial.New_Stim.Contains(stim) && stim != ChosenStim.StimIndex)
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
                    currentTrial.WrongStimIndex = ChosenStim.StimIndex; //identifies the stim they got wrong for Block FB purposes. 
                    TimeToCompletion_Block = Time.time - TimeToCompletion_StartTime;
                    EventCodeManager.SendCodeImmediate(SessionEventCodes["IncorrectResponse"]);
                }
            }

            if (ChosenGO != null && ChosenStim != null && ShotgunHandler.SuccessfulSelections.Count > 0) //if they chose a stim 
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
        //ChooseStim.SpecifyTermination(() => (Time.time - ChooseStim.TimingInfo.StartTimeAbsolute > chooseStimDuration.value) && !TouchFBController.FeedbackOn, TokenUpdate, () =>
        //{
        //    AudioFBController.Play("Negative");
        //    EndBlock = true;
        //    EventCodeManager.SendCodeImmediate(SessionEventCodes["NoChoice"]);
        //    AbortCode = 6;
        //});

        //TOUCH FEEDBACK state -------------------------------------------------------------------------------------------------------
        TouchFeedback.AddInitializationMethod(() =>
        {
            if (!StimIsChosen)
                return;

            if (currentTrial.GotTrialCorrect)
                HaloFBController.ShowPositive(ChosenGO);
            else
                HaloFBController.ShowNegative(ChosenGO);
        });
        TouchFeedback.AddTimer(() => touchFbDuration.value, TokenUpdate);
        TouchFeedback.SpecifyTermination(() => !StimIsChosen, TokenUpdate);

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
                    int numToFillBar = currentTrial.NumTokenBar - TokenFBController.GetTokenBarValue();
                    TokenFBController.AddTokens(ChosenGO, numToFillBar);
                }
                else
                    TokenFBController.AddTokens(ChosenGO, currentTrial.RewardMag);
            }
            else //Got wrong
            {
                TokenFBController.RemoveTokens(ChosenGO,currentTrial.RewardMag);
                EndBlock = true;
            }
        });
        TokenUpdate.SpecifyTermination(() => (Time.time - TokenUpdateStartTime > (tokenRevealDuration.value + tokenUpdateDuration.value + .05f) && !TokenFBController.IsAnimating()), DisplayResults);
        TokenUpdate.SpecifyTermination(() => !StimIsChosen, DisplayResults);
        TokenUpdate.AddDefaultTerminationMethod(() =>
        {
            HandleTokenUpdate();

            DeactivatePlayerViewText();

            if (currentTrial.ShakeStim)
                RemoveShakeStimScript(trialStims);

            if (IsHuman)
            {
                TimerBackdropGO.SetActive(false);
                ScoreTextGO.SetActive(false);
                NumTrialsTextGO.SetActive(false);
            }
            EventCodeManager.SendCodeNextFrame(SessionEventCodes["StimOff"]);

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
                        AudioFBController.Play("CR_BlockFailed");
                        //AudioFBController.Play("CR_SouthParkFail");
                    }
                }
            }
        });
        DisplayResults.AddTimer(() => displayResultsDuration.value, ITI);
        DisplayResults.SpecifyTermination(() => !EndBlock && !CompletedAllTrials, ITI);
        DisplayResults.AddDefaultTerminationMethod(() =>
        {
            if(currentTrial.ShakeStim)
                RemoveShakeStimScript(trialStims);

            TokenFBController.enabled = false;
        });

        //ITI State----------------------------------------------------------------------------------------------------------------------
        ITI.AddInitializationMethod(() =>
        {
            if (AbortCode == 0) //Normal
            {
                NumTrials_Block++;
                if (currentTrial.GotTrialCorrect)
                    NumCorrect_Block++;

                currentTask.CalculateBlockSummaryString();
            }
            else if (AbortCode == AbortCodeDict["Pause"]) //If used Pause hotkey to end trial, end entire Block
                EndBlock = true;
        });
        ITI.AddTimer(() => itiDuration.value, FinishTrial);

        //FinishTrial State (default state) ----------------------------------------------------------------------------------------------------------------------
        FinishTrial.AddDefaultTerminationMethod(() => EventCodeManager.SendCodeNextFrame(SessionEventCodes["ContextOff"]));

        //----------------------------------------------------------------------------------------------------------------------
        DefineTrialData();
        DefineFrameData();
    }


    //HELPER FUNCTIONS -----------------------------------------------------------------------------------------
    private void DeactivatePlayerViewText()
    {
        foreach (GameObject textGO in playerViewTextList)
            Destroy(textGO);
    }

    private void CreateTextOnExperimenterDisplay()
    {
        Vector2 textLocation = new Vector2();

        for(int i=0; i < currentTrial.NumTrialStims; ++i)
        {
            textLocation = playerViewPosition(Camera.main.WorldToScreenPoint(trialStims.stimDefs[i].StimLocation), playerViewParent);
            textLocation.y += 50;
            Vector2 textSize = new Vector2(200, 200);
            string stimString = "Target";
            ContinuousRecognition_StimDef currentStim = (ContinuousRecognition_StimDef)trialStims.stimDefs[i];
            if (currentStim.PreviouslyChosen)
                stimString = "PC";

            playerViewText = playerView.WriteText(stimString, stimString, stimString == "PC" ? Color.red : Color.green, textLocation, textSize, playerViewParent);
            playerViewText.GetComponent<RectTransform>().localScale = new Vector3(1.1f, 1.1f, 0);
            playerViewTextList.Add(playerViewText);
        }
    }

    public override void ResetTrialVariables()
    {
        CompletedAllTrials = false;
        EndBlock = false;
        StimIsChosen = false;
        currentTrial.GotTrialCorrect = false;
    }

    public override void FinishTrialCleanup()
    {
        DeactivatePlayerViewText();
        DeactivateTextObjects();
        DestroyFeedbackBorders();
        ContextActive = false;
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
        HaloFBController.SetPositiveHaloColor(Color.yellow);
        HaloFBController.SetNegativeHaloColor(Color.gray);
        HaloFBController.SetHaloSize(1f);
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

    void PopStimOut() //Method used to make the game easier for debugging purposes
    {
        foreach(ContinuousRecognition_StimDef stim in trialStims.stimDefs)
        {
            if (!stim.PreviouslyChosen)
                stim.StimGameObject.transform.localScale *= 1.35f;
        }
    }

    void AdjustTextPosForMac() //When running a build instead of hitting play in editor:
    {
        Vector3 biggerScale = TokenFBController.transform.localScale * 2f;
        TokenFBController.transform.localScale = biggerScale;
        TokenFBController.tokenSize = 200;
        TokenFBController.RecalculateTokenBox();

        Vector3 Pos = OriginalTimerPosition;
        Pos.y -= .02f;
        TimerBackdropGO.transform.position = Pos;
    }

    float GetOffsetY()
    {
        //Function used to adjust the YouWin/YouLost text positioning for the human version. 
        float yOffset = 0;
        switch (NumFeedbackRows)
        {
            case 1:
                if (MacMainDisplayBuild && !Application.isEditor)
                    yOffset = 90f; //not checked
                else
                    yOffset = 60f;
                break;
            case 2:
                if (MacMainDisplayBuild && !Application.isEditor)
                    yOffset = 75f; //not checked
                else
                    yOffset = 50f;
                break;
            case 3:
                if (MacMainDisplayBuild && !Application.isEditor)
                    yOffset = 25f; //not checked
                else
                    yOffset = 10f;
                break;
            case 4:
                if (MacMainDisplayBuild && !Application.isEditor)
                    yOffset = -5f; //not checked
                else
                    yOffset = -30f;
                break;
            //case 5:
            //    if (MacMainDisplayBuild && Application.isEditor)
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
                currentShiftedLoc = ShiftLocationHorizontally(horizontalMax, leftMargin, rightMargin, locations[i]);
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
                currentShiftedLoc = ShiftLocationHorizontally(horizontalMax, leftMargin, rightMargin, locations[i]);
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
                currentShiftedLoc = ShiftLocationHorizontally(horizontalMax, leftMargin, rightMargin, locations[i]);
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
                currentShiftedLoc = ShiftLocationHorizontally(horizontalMax, leftMargin, rightMargin, locations[i]);
                locList.Add(currentShiftedLoc);
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
        //        currentShiftedLoc = ShiftLocationHorizontally(horizontalMax, leftMargin, rightMargin, locations[i]);
        //        locList.Add(currentShiftedLoc);
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

    public Vector3 ShiftLocationHorizontally(float horizMax, float leftMarg, float rightMarg, Vector3 currentLoc)
    {
        float leftMarginNeeded = (leftMarg + rightMarg) / 2;
        float leftshiftAmount = leftMarginNeeded - leftMarg;
        currentLoc.x += leftshiftAmount;
        return currentLoc;
    }

    //Generate the correct number of New, PC, and PNC stim for each trial. Called when the trial is defined!
    protected override void DefineTrialStims()
    {
        NumPC_Trial = 0;
        NumNew_Trial = 0;
        NumPNC_Trial = 0;

        StimGroup group = UseDefaultConfigs ? PrefabStims : ExternalStims;


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
                NumNew_Trial++;
            }

            trialStims = new StimGroup("TrialStims", group, currentTrial.TrialStimIndices);
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
                NumNew_Trial++;
            }

            List<int> PC_Copy = ShuffleList(currentTrial.PC_Stim).ToList();
            if (PC_Copy.Count > 1)
                PC_Copy = PC_Copy.GetRange(0, PC_Num);
            for (int i = 0; i < PC_Copy.Count; i++)
            {
                currentTrial.TrialStimIndices.Add(PC_Copy[i]);
                NumPC_Trial++;
            }
            

            List<int> PNC_Copy = ShuffleList(currentTrial.PNC_Stim).ToList();
            if (PNC_Copy.Count > 1)
                PNC_Copy = PNC_Copy.GetRange(0, PNC_Num);
            for (int i = 0; i < PNC_Copy.Count; i++)
            {
                currentTrial.TrialStimIndices.Add(PNC_Copy[i]);
                NumPNC_Trial++;
            }

            trialStims = new StimGroup($"TrialStims", group, currentTrial.TrialStimIndices);
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
            
            trialStims = new StimGroup($"TrialStims", group, currentTrial.TrialStimIndices);
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

        StimGroup group = UseDefaultConfigs ? PrefabStims : ExternalStims;

        if (!StimIsChosen && ChosenStimIndices.Count < 1)
            return;

        if (CompletedAllTrials || !StimIsChosen) //!stimchosen means time ran out. 
        {
            RightGroup = new StimGroup("Right");
            Vector3[] FeedbackLocations = new Vector3[ChosenStimIndices.Count];
            FeedbackLocations = CenterFeedbackLocations(currentTrial.TrialFeedbackLocations, FeedbackLocations.Length);

            RightGroup = new StimGroup("Right", group, ChosenStimIndices);
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

            RightGroup = new StimGroup("Right", group, ChosenStimIndices);
            GenerateFeedbackStim(RightGroup, FeedbackLocations.Take(FeedbackLocations.Length - 1).ToArray());
            GenerateFeedbackBorders(RightGroup);

            if (currentTrial.StimFacingCamera)
                MakeStimsFaceCamera(RightGroup);

            WrongGroup = new StimGroup("Wrong");
            StimDef wrongStim = group.stimDefs[currentTrial.WrongStimIndex].CopyStimDef(WrongGroup);
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
                GameObject redBorder = Instantiate(RedBorderPrefab, (stim.StimGameObject.transform.position + new Vector3(0, .1f, 0)), Quaternion.identity);
                BorderPrefabList.Add(redBorder); //Add each to list so I can destroy them together
            }
            else
            {
                GameObject greenBorder = Instantiate(GreenBorderPrefab, (stim.StimGameObject.transform.position + new Vector3(0, .1f, 0)), Quaternion.identity);
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
        if(TokenFBController.isTokenBarFull())
        {
            NumTbCompletions_Block++;
            NumRewards_Block += currentTrial.NumRewardPulses;
            TokenFBController.ResetTokenBarFull();

            if (SyncBoxController != null)
            {
                SyncBoxController.SendRewardPulses(currentTrial.NumRewardPulses, currentTrial.PulseSize);
                SessionInfoPanel.UpdateSessionSummaryValues(("totalRewardPulses",currentTrial.NumRewardPulses));
            }
        }
    }

    List<int> ShuffleList(List<int> list)
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

    void SetTokenFeedbackTimes()
    {
        TokenFBController.SetRevealTime(tokenRevealDuration.value);
        TokenFBController.SetUpdateTime(tokenUpdateDuration.value);
    }

}
