using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ConfigDynamicUI;
using HiddenMaze;
using MazeReactionTest_Namespace;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Trial;
using USE_States;
using USE_StimulusManagement;
using USE_UI;
using Application = UnityEngine.Application;

public class MazeReactionTest_TrialLevel : ControlLevel_Trial_Template
{
    // Generic Task Variables
    public GameObject MRT_CanvasGO;
    public USE_StartButton USE_StartButton;
    private GameObject StartButton;
    
    // Block Ending Variable
    public List<float> runningPercentError = new List<float>();
    private float percentError;
    public int MinTrials;
    
    // Maze Object Variables
    public string mazeDefName;
    public GameObject MazeBackground;
    private GameObject MazeContainer;
    private float mazeLength;
    private float mazeHeight;
    private Vector2 mazeDims;
    private bool mazeLoaded = false;

    // Tile objects
    private Tile tile = new Tile();
    private GameObject tileGO;
    public StimGroup tiles; // top of trial level with other variable definitions
    private Texture2D tileTex;
    private Texture2D mazeBgTex;
    public List<GameObject> totalTilesGO = new List<GameObject>();
    
    // Maze Progress Variables
    private bool finishedMaze;
    private bool startedMaze;
    public int consecutiveErrors; // only evaluates, not really useful to log
    private List<Coords> pathProgress = new List<Coords>();
    public List<GameObject> pathProgressGO = new List<GameObject>();
    public int pathProgressIndex = 0;
    public bool viewPath;
    private bool CorrectSelection;
    private bool ReturnToLast;
    private bool ErroneousReturnToLast;
    public float tileFbDuration;
    public GameObject startTile;
    private float finishedFbDuration;

    // Task Level Defined Variables
    [HideInInspector]
    public float[] startColor;
    public float[] finishColor;
    public float[] correctColor;
    public float[] lastCorrectColor;
    public float[] incorrectRuleAbidingColor;
    public float[] incorrectRuleBreakingColor;
    public float[] defaultTileColor;
    public int NumBlinks;
    public Tile TilePrefab;
    public float TileSize;
    public string TileTexture;
    public string MazeBackgroundTextureName;
    public string ContextExternalFilePath;
    public string MazeFilePath;
    public Vector3 StartButtonPosition;
    public float StartButtonScale;
    public bool NeutralITI;
    public bool UsingFixedRatioReward;
    
    // Config UI Variables
    private bool configVariablesLoaded;
    [HideInInspector]
    public ConfigNumber spaceBetweenTiles;
    public ConfigNumber mazeOnsetDelay;
    public ConfigNumber correctFbDuration;
    public ConfigNumber previousCorrectFbDuration;
    public ConfigNumber incorrectRuleAbidingFbDuration;
    public ConfigNumber incorrectRuleBreakingFbDuration;
    public ConfigNumber itiDuration;
    public ConfigNumber flashingFbDuration;
    public ConfigNumber sliderSize;
    public ConfigNumber tileBlinkingDuration;
    public ConfigNumber maxMazeDuration;
    public ConfigNumber minObjectTouchDuration;
    public ConfigNumber maxObjectTouchDuration;
    
    // Player View Variables
    private PlayerViewPanel playerView;
    private GameObject playerViewParent; // Helps set things onto the player view in the experimenter display
    public List<GameObject> playerViewTextList;
    public GameObject playerViewText;
    private Vector2 textLocation;
    
    // Touch Evaluation Variables
    private GameObject selectedGO;
    
    // Trial Data Tracking Variables
    private float mazeDuration;
    private float mazeStartTime;
    private float choiceDuration;
    private float choiceStartTime;
    private int[] totalErrors_InTrial;
    private int[] ruleAbidingErrors_InTrial;
    private int[] ruleBreakingErrors_InTrial;
    private int retouchCorrect_InTrial;
    private int retouchErroneous_InTrial;
    private int correctTouches_InTrial;
    private int[] backtrackErrors_InTrial;
    private int[] perseverativeErrors_InTrial;
    private bool aborted;
    private bool choiceMade;
    public List<float> choiceDurationsList = new List<float>();
    
    // Frame Data Variables
    public string contextName = "";
    [HideInInspector] public float TouchFeedbackDuration;
    private float tileScale;

    public MazeReactionTest_TrialDef CurrentTrialDef => GetCurrentTrialDef<MazeReactionTest_TrialDef>();
    public MazeReactionTest_TaskLevel CurrentTaskLevel => GetTaskLevel<MazeReactionTest_TaskLevel>();
    public override void DefineControlLevel()
    {
        //define States within this Control Level
        State InitTrial = new State("InitTrial");
        State TileFlash = new State("TileFlash");
        State ChooseTile = new State("ChooseTile");
        State SelectionFeedback = new State("SelectionFeedback");
        State ITI = new State("ITI");
        
        AddActiveStates(new List<State>
            { InitTrial, TileFlash, ChooseTile, SelectionFeedback, ITI  });
        string[] stateNames =
            { "InitTrial", "TileFlash", "ChooseTile", "SelectionFeedback",  "ITI"};

        Add_ControlLevel_InitializationMethod(() =>
        {
            SliderFBController.InitializeSlider();
            HaloFBController.SetHaloSize(5);
            LoadTextures(ContextExternalFilePath);
            tileTex = LoadPNG(GetContextNestedFilePath(ContextExternalFilePath, TileTexture));
            mazeBgTex = LoadPNG(GetContextNestedFilePath(ContextExternalFilePath, MazeBackgroundTextureName));
            if (MazeContainer == null)
                MazeContainer = new GameObject("MazeContainer"); 
            if (MazeBackground == null)
                MazeBackground = CreateSquare("MazeBackground", mazeBgTex, new Vector3(0, 0.42f, 0),
                    new Vector3(5, 5, 5));
            // instantiate array
            totalErrors_InTrial = new int[CurrentTaskLevel.currMaze.mNumSquares];
            backtrackErrors_InTrial = new int[CurrentTaskLevel.currMaze.mNumSquares];

            //player view variables
            playerViewParent = GameObject.Find("MainCameraCopy");
        });
        SetupTrial.AddInitializationMethod(() =>
        {
            CurrentTaskLevel.LoadTextMaze();
            if(StartButton == null)
            {
                USE_StartButton = new USE_StartButton(MRT_CanvasGO.GetComponent<Canvas>(), StartButtonPosition, StartButtonScale);
                StartButton = USE_StartButton.StartButtonGO;
                USE_StartButton.SetVisibilityOnOffStates(InitTrial, InitTrial);
            }

            if (!configVariablesLoaded)
                LoadConfigVariables();
            
            CurrentTaskLevel.SetTaskSummaryString();
            CurrentTaskLevel.CalculateBlockSummaryString();
            Input.ResetInputAxes(); //reset input in case they still touching their selection from last trial!
        });
        SetupTrial.SpecifyTermination(() => true, InitTrial);

        var SelectionHandler = SelectionTracker.SetupSelectionHandler("trial", "MouseButton0Click", InitTrial, ITI);
        TouchFBController.EnableTouchFeedback(SelectionHandler, TouchFeedbackDuration, StartButtonScale, MRT_CanvasGO);

        InitTrial.AddInitializationMethod(() =>
        {
            TouchFBController.DestroyTouchFeedback();
            TouchFBController.SetPrefabSizes(StartButtonScale);
            SelectionHandler.HandlerActive = true;
            if (SelectionHandler.AllSelections.Count > 0)
                SelectionHandler.ClearSelections();
            SelectionHandler.MinDuration = minObjectTouchDuration.value;
            SelectionHandler.MaxDuration = maxObjectTouchDuration.value;
        });
        InitTrial.SpecifyTermination(() => SelectionHandler.LastSuccessfulSelectionMatches(StartButton), Delay, () =>
        {
            EventCodeManager.SendCodeImmediate(SessionEventCodes["StartButtonSelected"]);

            StateAfterDelay = TileFlash;
            DelayDuration = mazeOnsetDelay.value;
            
            SliderFBController.ConfigureSlider(new Vector3(0,209,0), sliderSize.value);
            SliderFBController.SliderGO.SetActive(true);
            SetTrialSummaryString();
            
            InstantiateCurrMaze();
            tiles.ToggleVisibility(true);
            mazeStartTime = Time.unscaledTime;
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["MazeOn"]);
            
            CreateTextOnExperimenterDisplay();
        });
        TileFlash.AddInitializationMethod(() =>
        {
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["FlashingTileFbOn"]);
            tile.NextCorrectFlashingFeedback();
        });
        TileFlash.AddTimer(()=>tileBlinkingDuration.value, ChooseTile, () =>
        {
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["FlashingTileFbOff"]);
            
        });
        ChooseTile.AddInitializationMethod(() =>
        {
            TouchFBController.DestroyTouchFeedback(); // destroys prefab of previous sizing
            tileScale = 26.25f * TileSize;
            TouchFBController.SetPrefabSizes(tileScale);
            choiceStartTime = Time.unscaledTime;
            SelectionHandler.HandlerActive = true;
            if (SelectionHandler.AllSelections.Count > 0)
                SelectionHandler.ClearSelections();
        });
        ChooseTile.AddUpdateMethod(() =>
        {
            mazeDuration = Time.unscaledTime - mazeStartTime;
            choiceDuration = Time.unscaledTime - choiceStartTime;
            SetTrialSummaryString(); // called every frame to update duration info
            
            if (SelectionHandler.SuccessfulSelections.Count > 0)
            { 
                if (SelectionHandler.LastSuccessfulSelection.SelectedGameObject.GetComponent<Tile>() != null)
                {
                    choiceMade = true;
                    choiceDurationsList.Add(choiceDuration);
                    CurrentTaskLevel.choiceDurationsList_InBlock.Add(choiceDuration);
                    CurrentTaskLevel.choiceDurationsList_InTask.Add(choiceDuration);
                    selectedGO = SelectionHandler.LastSuccessfulSelection.SelectedGameObject;
                    SelectionHandler.ClearSelections();
                }
            }
        });
        ChooseTile.SpecifyTermination(() =>  choiceMade, SelectionFeedback, () =>
        {
            SelectionHandler.HandlerActive = false;

            if (selectedGO.GetComponent<Tile>().mCoord.chessCoord ==  CurrentTaskLevel.currMaze.mStart)
            {
                //If the tile that is selected is the start tile, begin the timer for the maze
                startedMaze = true;
                EventCodeManager.SendCodeImmediate(TaskEventCodes["MazeStart"]); 
            }

            if (selectedGO.GetComponent<Tile>().mCoord.chessCoord == CurrentTaskLevel.currMaze.mFinish && CurrentTaskLevel.currMaze.mNextStep == CurrentTaskLevel.currMaze.mFinish)
            {
                mazeDuration = Time.unscaledTime - mazeStartTime;
                CurrentTaskLevel.mazeDurationsList_InBlock.Add(mazeDuration);
                CurrentTaskLevel.mazeDurationsList_InTask.Add(mazeDuration);
                EventCodeManager.SendCodeImmediate(TaskEventCodes["MazeFinish"]);
            }
        });
        ChooseTile.SpecifyTermination(()=> mazeDuration > maxMazeDuration.value, ()=> FinishTrial, () =>
        {
            aborted = true;
            EventCodeManager.SendCodeImmediate(SessionEventCodes["NoChoice"]);
            AbortCode = 6;
            CurrentTaskLevel.numAbortedTrials_InBlock++;
            CurrentTaskLevel.numAbortedTrials_InTask++;
        }); // Timeout Termination
        SelectionFeedback.AddInitializationMethod(() =>
        {
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["TileFbOn"]);

            // This is what actually determines the result of the tile choice
            selectedGO.GetComponent<Tile>().SelectionFeedback();
            percentError = (float)decimal.Divide(totalErrors_InTrial.Sum(),CurrentTaskLevel.currMaze.mNumSquares);

            finishedFbDuration = (tileFbDuration + flashingFbDuration.value);
            SliderFBController.SetUpdateDuration(tileFbDuration);
            SliderFBController.SetFlashingDuration(finishedFbDuration);
                
                
            if (CorrectSelection)
            {
                SliderFBController.UpdateSliderValue(selectedGO.GetComponent<Tile>().sliderValueChange);
                playerViewParent.transform.Find((pathProgressIndex + 1).ToString()).GetComponent<Text>().color =
                    new Color(0, 0.392f, 0);
                // EventCodeManager.SendCodeNextFrame(SessionEventCodes["Rewarded"]);
            }
            else if (selectedGO != null)
            {
                AudioFBController.Play("Negative");
                // EventCodeManager.SendCodeNextFrame(SessionEventCodes["Unrewarded"]);
            }
               
            selectedGO = null; //Reset selectedGO before the next touch evaluation
        });
        SelectionFeedback.AddUpdateMethod(() =>
        {
            mazeDuration = Time.unscaledTime - mazeStartTime;
            SetTrialSummaryString(); // called every frame to update duration info
        });
        SelectionFeedback.AddTimer(() => finishedMaze? finishedFbDuration:tileFbDuration, Delay, () =>
        {
            SetTrialSummaryString(); //Set the Trial Summary String to reflect the results of choice
            CurrentTaskLevel.CalculateBlockSummaryString();
            choiceMade = false;

            if (UsingFixedRatioReward)
            {
                if (CorrectSelection && (correctTouches_InTrial % CurrentTrialDef.RewardRatio == 0 ))
                {
                    if (SyncBoxController != null)
                    {
                        SyncBoxController.SendRewardPulses(1, CurrentTrialDef.PulseSize);
                        SessionInfoPanel.UpdateSessionSummaryValues(("totalRewardPulses",CurrentTrialDef.NumPulses));
                        CurrentTaskLevel.numRewardPulses_InBlock += CurrentTrialDef.NumPulses;
                        //CurrentTaskLevel.numRewardPulses_InTask += CurrentTrialDef.NumPulses;
                    }
                }
            }
            else if (finishedMaze) 
            {
                StateAfterDelay = ITI;
                DelayDuration = 0;
                
                percentError = (float)decimal.Divide(totalErrors_InTrial.Sum(),CurrentTaskLevel.currMaze.mNumSquares);
                runningPercentError.Add(percentError);
                CurrentTaskLevel.numSliderBarFull_InBlock++;
                CurrentTaskLevel.numSliderBarFull_InTask++;
                EventCodeManager.SendCodeNextFrame(SessionEventCodes["SliderFbController_SliderCompleteFbOn"]);

                if (SyncBoxController != null)
                {
                    SyncBoxController.SendRewardPulses(CurrentTrialDef.NumPulses, CurrentTrialDef.PulseSize);
                    SessionInfoPanel.UpdateSessionSummaryValues(("totalRewardPulses",CurrentTrialDef.NumPulses));
                    CurrentTaskLevel.numRewardPulses_InBlock += CurrentTrialDef.NumPulses;
                }
            }
            else
            {
                StateAfterDelay = TileFlash; // could be incorrect or correct but it will still go back to the tile flash
            }
            
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["TileFbOff"]);
            CorrectSelection = false;
            ReturnToLast = false;
            ErroneousReturnToLast = false;
        });
        ITI.AddInitializationMethod(() =>
        {
            DisableSceneElements();
            DestroyChildren(playerViewParent);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["MazeOff"]);
            if (finishedMaze)
                EventCodeManager.SendCodeNextFrame(SessionEventCodes["SliderFbController_SliderCompleteFbOff"]);

            if (NeutralITI)
            {
                contextName = "itiImage";
                RenderSettings.skybox = CreateSkybox(GetContextNestedFilePath(ContextExternalFilePath, "itiImage"), UseDefaultConfigs);
            }
        });
        ITI.AddTimer(() => itiDuration.value, FinishTrial);
        DefineFrameData();
        DefineTrialData();
    }
    private void DefineTrialData()
    {
        TrialData.AddDatum("MazeDefName", ()=> mazeDefName);
        TrialData.AddDatum("TotalErrors", () => $"[{string.Join(", ", totalErrors_InTrial)}]");
        TrialData.AddDatum("BacktrackingErrors", () => $"[{string.Join(", ", backtrackErrors_InTrial)}]");
        TrialData.AddDatum("MazeDuration", ()=> mazeDuration);
        //TrialData.AddDatum("TotalClicks", ()=>MouseTracker.GetClickCount().Length);
    }

    private void DefineFrameData()
    {
        FrameData.AddDatum("Context", ()=> contextName);
        FrameData.AddDatum("ChoiceMade", ()=> choiceMade);
    //    FrameData.AddDatum("SelectedObject", () => selectedGO.name);
        FrameData.AddDatum("StartedMaze", ()=> startedMaze);
    }
    protected override bool CheckBlockEnd()
    {
        TaskLevelTemplate_Methods TaskLevel_Methods = new TaskLevelTemplate_Methods();
        return TaskLevel_Methods.CheckBlockEnd(CurrentTrialDef.BlockEndType, runningPercentError,
            CurrentTrialDef.BlockEndThreshold, MinTrials,
            CurrentTrialDef.MaxTrials);
    }
    public int ManageTileTouch(Tile tile)
    {
        var touchedCoord = tile.mCoord;
        if (touchedCoord.chessCoord == CurrentTaskLevel.currMaze.mNextStep)
        {
            Debug.Log("*Correct Tile Touch*");
            EventCodeManager.SendCodeImmediate(SessionEventCodes["CorrectResponse"]);
            correctTouches_InTrial++;
            CorrectSelection = true;
            
            // Helps set progress on the experimenter display
            // pathProgessIndex represents the index of the player's progress along the path, 0 -> they are currently on & have selected the first tile
            pathProgress.Add(touchedCoord);
            pathProgressGO.Add(tile.gameObject);
            pathProgressIndex = CurrentTaskLevel.currMaze.mPath.FindIndex(pathCoord => pathCoord == touchedCoord.chessCoord);
            
            // Sets the NextStep if the maze isn't finished
            if (touchedCoord.chessCoord != CurrentTaskLevel.currMaze.mFinish)
            {
                CurrentTaskLevel.currMaze.mNextStep = CurrentTaskLevel.currMaze.mPath[CurrentTaskLevel.currMaze.mPath.FindIndex(pathCoord => pathCoord == touchedCoord.chessCoord) + 1];
            }
            else
            {
                finishedMaze = true; // Finished the Maze
            }
            //sets the duration of tile feedback
            tileFbDuration = tile.CORRECT_FEEDBACK_SECONDS;
            return 1;
        }
        // RULE BREAKING BACKTRACK ERROR OR ERRONEOUS RETOUCH OF LAST CORRECT TILE
        if (pathProgress.Contains(touchedCoord))
        {
            Debug.Log("*Rule-Breaking Backtrack Error*");
            EventCodeManager.SendCodeImmediate(TaskEventCodes["RuleBreakingError"]);
            backtrackErrors_InTrial[pathProgressIndex + 1] += 1;
            CurrentTaskLevel.backtrackErrors_InBlock[pathProgressIndex + 1] += 1;
            CurrentTaskLevel.backtrackErrors_InTask++;
            
            totalErrors_InTrial[pathProgressIndex + 1] += 1;
            CurrentTaskLevel.totalErrors_InBlock[pathProgressIndex + 1] += 1;
            CurrentTaskLevel.totalErrors_InTask++;

            /*if (viewPath)
                tile.originalTileColor = tile.CORRECT_COLOR;*/
            tileFbDuration = tile.INCORRECT_RULEBREAKING_SECONDS;
            return 20;
        }
        
        // Any selection that is neither backtrack nor correct (rule-breaking)
        Debug.Log("*Rule-Breaking Error*");
        EventCodeManager.SendCodeImmediate(TaskEventCodes["RuleBreakingError"]);
            
        totalErrors_InTrial[pathProgressIndex + 1] += 1;
        CurrentTaskLevel.totalErrors_InBlock[pathProgressIndex + 1] += 1;
        CurrentTaskLevel.totalErrors_InTask++;
        
        tileFbDuration = tile.INCORRECT_RULEBREAKING_SECONDS;
        return 20; 
    }

    private void LoadConfigVariables()
    {
        //config UI variables
        itiDuration = ConfigUiVariables.get<ConfigNumber>("itiDuration");
        sliderSize = ConfigUiVariables.get<ConfigNumber>("sliderSize");
        spaceBetweenTiles = ConfigUiVariables.get<ConfigNumber>("spaceBetweenTiles");
        flashingFbDuration = ConfigUiVariables.get<ConfigNumber>("flashingFbDuration");
        mazeOnsetDelay = ConfigUiVariables.get<ConfigNumber>("mazeOnsetDelay");
        correctFbDuration = ConfigUiVariables.get<ConfigNumber>("correctFbDuration");
        previousCorrectFbDuration = ConfigUiVariables.get<ConfigNumber>("previousCorrectFbDuration");
        incorrectRuleAbidingFbDuration = ConfigUiVariables.get<ConfigNumber>("incorrectRuleAbidingFbDuration");
        incorrectRuleBreakingFbDuration = ConfigUiVariables.get<ConfigNumber>("incorrectRuleBreakingFbDuration");
        tileBlinkingDuration = ConfigUiVariables.get<ConfigNumber>("tileBlinkingDuration");
        maxMazeDuration = ConfigUiVariables.get<ConfigNumber>("maxMazeDuration");
        minObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("minObjectTouchDuration");
        maxObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("maxObjectTouchDuration");
        configVariablesLoaded = true;
    }
    void SetTrialSummaryString()
    {
        TrialSummaryString = "<b>Maze Name: </b>" + mazeDefName +
                             "\n" + 
                             "\n<b>Percent Error: </b>" +  String.Format("{0:0.00}%", percentError*100) +
                             "\n<b>Total Errors: </b>" + totalErrors_InTrial.Sum() +
                             "\n<b>Total Backtrack Errors: </b>" + backtrackErrors_InTrial.Sum() +
                             "\n" +
                             "\n<b>Choice Duration: </b>" + String.Format("{0:0.0}", choiceDuration) + 
                             "\n<b>Maze Duration: </b>" + String.Format("{0:0.0}", mazeDuration) +
                             "\n<b>Slider Value: </b>" + String.Format("{0:0.00}", SliderFBController.Slider.value);

    }
    private void CreateTextOnExperimenterDisplay()
    {
        // sets parent for any playerView elements on experimenter display
        playerView = new PlayerViewPanel(); //GameObject.Find("PlayerViewCanvas").GetComponent<PlayerViewPanel>()
        for (int i = 0; i < CurrentTaskLevel.currMaze.mPath.Count; i++)
        {
            foreach (StimDef sd in tiles.stimDefs)
            {
                Tile tileComponent = sd.StimGameObject.GetComponent<Tile>();
                Vector2 textSize = new Vector2(200, 200);
                
                if (tileComponent.mCoord.chessCoord == CurrentTaskLevel.currMaze.mPath[i])
                {
                    textLocation = playerViewPosition(Camera.main.WorldToScreenPoint(tileComponent.transform.position), playerViewParent.transform);
                    playerViewText = playerView.WriteText((i + 1).ToString(), (i + 1).ToString(),
                        Color.red, textLocation, textSize, playerViewParent.transform);
                    playerViewText.GetComponent<RectTransform>().localScale = new Vector3(2, 2, 0);
                    playerViewTextList.Add(playerViewText);
                }
            }
        }
    }
   public override void FinishTrialCleanup()
    {
        DisableSceneElements();
        DestroyChildren(playerViewParent);
        if (mazeLoaded)
        {
            tiles.DestroyStimGroup();
            mazeLoaded = false;
        }
        
        if (TokenFBController.isActiveAndEnabled)
            TokenFBController.enabled = false;
        
        if(AbortCode == 0)
            CurrentTaskLevel.CalculateBlockSummaryString();

        if (AbortCode == AbortCodeDict["RestartBlock"] || AbortCode == AbortCodeDict["PreviousBlock"] || AbortCode == AbortCodeDict["EndBlock"]) //If used RestartBlock, PreviousBlock, or EndBlock hotkeys
        {
            CurrentTaskLevel.numAbortedTrials_InBlock++;
            CurrentTaskLevel.numAbortedTrials_InTask++;
       //     CurrentTaskLevel.ClearStrings();
       //     CurrentTaskLevel.BlockSummaryString.AppendLine("");
        }
    }

    public override void ResetTrialVariables()
    {
        SliderFBController.ResetSliderBarFull();
        mazeDuration = 0;
        mazeStartTime = 0;
        choiceDuration = 0;
        choiceStartTime = 0;
        finishedMaze = false;
        startedMaze = false;
        selectedGO = null;
        aborted = false;
        choiceMade = false;
        CorrectSelection = false;
        ReturnToLast = false;
        ErroneousReturnToLast = false;
        
        MouseTracker.ResetClicks();
        
        if (TrialCount_InBlock != 0)
        {
            Array.Clear(totalErrors_InTrial, 0, totalErrors_InTrial.Length);
            Array.Clear(backtrackErrors_InTrial, 0, backtrackErrors_InTrial.Length);
        }
        pathProgress.Clear();
        pathProgressGO.Clear();
        pathProgressIndex = 0;
        consecutiveErrors = 0;
        correctTouches_InTrial = 0;
    }
    private void DisableSceneElements()
    {
        StartButton.SetActive(false);
        DeactivateChildren(MazeContainer);
        DeactivateChildren(GameObject.Find("SliderCanvas"));
    } 
    private void InstantiateCurrMaze()
    {
        // This will Load all tiles within the maze and the background of the maze

        mazeDims = CurrentTaskLevel.currMaze.mDims;
        var mazeCenter = MazeBackground.transform.localPosition;

        mazeLength = mazeDims.x * TileSize + (mazeDims.x - 1) * spaceBetweenTiles.value;
        mazeHeight = mazeDims.y * TileSize + (mazeDims.y - 1) * spaceBetweenTiles.value;
        MazeBackground.transform.SetParent(MazeContainer.transform); // setting it last so that it doesn't cover tiles
        MazeBackground.transform.localScale = new Vector3(mazeLength + 2 * spaceBetweenTiles.value,
            mazeHeight + 2 * (spaceBetweenTiles.value/4f), 0.1f);
        MazeBackground.SetActive(true);
        var bottomLeftMazePos = mazeCenter - new Vector3(mazeLength / 2, mazeHeight / 2, 0);

        tiles = new StimGroup("Tiles");

        for (var x = 1; x <= mazeDims.x; x++)
        for (var y = 1; y <= mazeDims.y; y++)
        {
            // Configures Tile objects and Prefab within the maze container
            tile = Instantiate(TilePrefab, MazeContainer.transform);
            SetGameConfigs();
            tile.transform.localScale = new Vector3(TileSize, TileSize, 0.15f);
            tile.gameObject.SetActive(true);
            tile.gameObject.GetComponent<Tile>().enabled = true;
            tile.gameObject.GetComponent<MeshRenderer>().sharedMaterial.mainTexture = tileTex;
            var displaceX = (2 * (x - 1) + 1) * (TileSize / 2) + spaceBetweenTiles.value * (x - 1);
            var displaceY = (2 * (y - 1) + 1) * (TileSize / 2) + spaceBetweenTiles.value * (y - 1);
            var newTilePosition = bottomLeftMazePos + new Vector3(displaceX, displaceY, 0);
            tile.transform.position = newTilePosition;
            // Assigns ChessCoordName to the tile 
            string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string chessCoordName = $"{alphabet[x-1]}{y}";
            tile.mCoord = new Coords(chessCoordName);
            tile.gameObject.name = chessCoordName;
            // Assigns Reward magnitude for each tile (set to proportional to the number of squares in path)
            tile.GetComponent<Tile>().sliderValueChange = 1f / CurrentTaskLevel.currMaze.mNumSquares; //FIX THE REWARD MAG BELOW USING STIM DEF ???

            if (chessCoordName == CurrentTaskLevel.currMaze.mStart)
            {
                tile.gameObject.GetComponent<Tile>().setColor(tile.START_COLOR);
                startTile = tile.gameObject; // Have to define to perform feedback if they haven't selected the start yet 
                //Consider making a separate group for the tiles in the path, this might not improve function that much?
            }
                
            else if (chessCoordName == CurrentTaskLevel.currMaze.mFinish)
            {                
                tile.gameObject.GetComponent<Tile>().setColor(tile.FINISH_COLOR);
            }
            else
            {
                tile.gameObject.GetComponent<Tile>().setColor(tile.DEFAULT_TILE_COLOR);
            }
            tiles.AddStims(tile.gameObject);
        }
        mazeLoaded = true;
        TrialStims.Add(tiles);
    }
    private void SetGameConfigs()
    {

        // Default tile width - edit at the task level def
        //---------------------------------------------------------

        // TILE COLORS

        // Start - Light yellow

        tile.NUM_BLINKS = NumBlinks;

        tile.START_COLOR = new Color(startColor[0], startColor[1], startColor[2], 1);

        // Finish - Light blue
        tile.FINISH_COLOR = new Color(finishColor[0], finishColor[1], finishColor[2], 1);

        // Correct - Light green
        tile.CORRECT_COLOR = new Color(correctColor[0], correctColor[1], correctColor[2]);

        // Prev correct - Darker green
        tile.PREV_CORRECT_COLOR = new Color(lastCorrectColor[0], lastCorrectColor[1], lastCorrectColor[2]);

        // Incorrect rule-abiding - Orange
        tile.INCORRECT_RULEABIDING_COLOR = new Color(incorrectRuleAbidingColor[0], incorrectRuleAbidingColor[1],
            incorrectRuleAbidingColor[2]);

        // Incorrect rule-breaking - Black
        tile.INCORRECT_RULEBREAKING_COLOR = new Color(incorrectRuleBreakingColor[0], incorrectRuleBreakingColor[1],
            incorrectRuleBreakingColor[2]);

        tile.DEFAULT_TILE_COLOR = new Color(defaultTileColor[0], defaultTileColor[1], defaultTileColor[2], 1);

        // FEEDBACK LENGTH IN SECONDS

        // Correct - 0.5 seconds
        tile.CORRECT_FEEDBACK_SECONDS = correctFbDuration.value;

        // Prev correct - 0.5 seconds
        tile.PREV_CORRECT_FEEDBACK_SECONDS = previousCorrectFbDuration.value;

        // Incorrect rule-abiding - 0.5 seconds
        tile.INCORRECT_RULEABIDING_SECONDS = incorrectRuleAbidingFbDuration.value;

        // Incorrect rule-breaking - 1.0 seconds
        tile.INCORRECT_RULEBREAKING_SECONDS = incorrectRuleBreakingFbDuration.value;

        tile.TILE_BLINKING_DURATION = tileBlinkingDuration.value;

        //---------------------------------------------------------

        // TIMEOUT

        tile.TIMEOUT_SECONDS = 10.0f;
        
        //Trial Def Configs
        viewPath = CurrentTrialDef.ViewPath;
        
    }
}