using System.Collections.Generic;
using System.IO;
using ConfigDynamicUI;
using HiddenMaze;
using MazeGame_Namespace;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using USE_ExperimentTemplate_Trial;
using USE_States;
using USE_StimulusManagement;
using USE_UI;

public class MazeGame_TrialLevel : ControlLevel_Trial_Template
{
    // Generic Task Variables
    public GameObject MG_CanvasGO;
    public USE_StartButton USE_StartButton;
    private GameObject StartButton;

    // Maze Object Variables
    public static Maze currMaze;
    public string mazeDefName;
    public GameObject MazeBackground;
    private GameObject MazeContainer;
    private float mazeLength;
    private float mazeHeight;
    private Vector2 mazeDims;
    private string mazeStart;
    private string mazeFinish;
    private bool mazeLoaded = false;

    // Tile objects
    private Tile tile = new Tile();
    private GameObject tileGO;
    public StimGroup tiles; // top of trial level with other variable definitions
    private Texture2D tileTex;

    // Maze Progress Variables
    private bool finishedMaze;
    private bool startedMaze;
    public int consecutiveErrors; // only evaluates, not really useful to log
    private List<Coords> pathProgress = new List<Coords>();
    public List<GameObject> pathProgressGO = new List<GameObject>();
    private int pathProgressIndex = 0;
    public bool viewPath;
    private bool CorrectSelection;
    private bool ReturnToLast;
    public float tileFbDuration;

    // Trial Data Tracking Variables
    private float? mazeDuration;
    private float? mazeStartTime;
    private int totalErrors_InTrial;
    private int ruleAbidingErrors_InTrial;
    private int ruleBreakingErrors_InTrial;
    private int retouchCorrect_InTrial;
    private int correctTouches_InTrial;
    private int backtrackErrors_InTrial;
    private int perseverativeErrors_InTrial;
    private bool aborted;

    // Frame Data Variables
    public string contextName = "";

    // Task Level Defined Color Variables
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
    public string ContextExternalFilePath;
    public string MazeFilePath;
    public Vector3 ButtonPosition;
    public float ButtonScale;
    public bool NeutralITI;
    [FormerlySerializedAs("fixedRatioReward")] public bool UsingFixedRatioReward;

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

    // Player View Variables
    private PlayerViewPanel playerView;
    private GameObject playerViewParent; // Helps set things onto the player view in the experimenter display
    public List<GameObject> playerViewTextList;
    public GameObject playerViewText;
    private Vector2 textLocation;
    public bool playerViewLoaded;

    // Touch Evaluation Variables
    private GameObject selectedGO;
    private StimDef selectedSD;

    // Slider & Animation variables
    private float sliderValueChange;
    private float fbDuration;

    public MazeGame_TrialDef CurrentTrialDef => GetCurrentTrialDef<MazeGame_TrialDef>();
    public MazeGame_TaskLevel CurrentTaskLevel => GetTaskLevel<MazeGame_TaskLevel>();

    public override void DefineControlLevel()
    {
        //define States within this Control Level
        State InitTrial = new State("InitTrial");
        State ChooseTile = new State("ChooseTile");
        State SelectionFeedback = new State("SelectionFeedback");
        State TileFlashFeedback = new State("TileFlashFeedback");
        State ITI = new State("ITI");
        
        AddActiveStates(new List<State>
            { InitTrial, ChooseTile, SelectionFeedback, TileFlashFeedback, ITI  });
        string[] stateNames =
            { "StartButton", "ChooseTile", "SelectionFeedback", "TileFlashFeedback", "ITI"};

        SelectionHandler<MazeGame_StimDef> mouseHandler = new SelectionHandler<MazeGame_StimDef>();
        
        Add_ControlLevel_InitializationMethod(() =>
        {
            SliderFBController.InitializeSlider();
            HaloFBController.SetHaloSize(5);
            LoadTextures(ContextExternalFilePath);
            tileTex = LoadPNG(ContextExternalFilePath + Path.DirectorySeparatorChar + TileTexture + ".png");

            if (MazeContainer == null)
                MazeContainer = new GameObject("MazeContainer"); 
            if (MazeBackground == null)
                MazeBackground = CreateSquare("MazeBackground", MazeBackgroundTexture, new Vector3(0, 0, 0),
                    new Vector3(5, 5, 5));
           
            //player view variables
            playerViewParent = GameObject.Find("MainCameraCopy");
        });
        SetupTrial.AddInitializationMethod(() =>
        {
            if(StartButton == null)
            {
                USE_StartButton = new USE_StartButton(MG_CanvasGO.GetComponent<Canvas>(), ButtonPosition, ButtonScale);
                StartButton = USE_StartButton.StartButtonGO;
                USE_StartButton.SetVisibilityOnOffStates(InitTrial, InitTrial);
            }

            if (!configVariablesLoaded)
                LoadConfigVariables();
            LoadMazeDef();
            Input.ResetInputAxes(); //reset input in case they still touching their selection from last trial!
            
            if (TrialCount_InTask != 0)
                CurrentTaskLevel.SetTaskSummaryString();
        });
        SetupTrial.SpecifyTermination(() => true, InitTrial);
        MouseTracker.AddSelectionHandler(mouseHandler, InitTrial, null, 
            ()=> MouseTracker.ButtonStatus[0] == 1, ()=> MouseTracker.ButtonStatus[0] == 0);
        InitTrial.SpecifyTermination(() => mouseHandler.SelectionMatches(StartButton), Delay, () =>
        {
            EventCodeManager.SendCodeImmediate(TaskEventCodes["StartButtonSelected"]);

            StateAfterDelay = ChooseTile;
            DelayDuration = mazeOnsetDelay.value;
            SliderFBController.ConfigureSlider(new Vector3(0,180,0), sliderSize.value);
            SliderFBController.SliderGO.SetActive(true);
            SetTrialSummaryString();
            
            InstantiateCurrMaze();
            tiles.ToggleVisibility(true);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["MazeOn"]);

            if (!playerViewLoaded)
                CreateTextOnExperimenterDisplay();
            else
                ActivateChildren(playerViewParent);

        });
        MouseTracker.AddSelectionHandler(mouseHandler, ChooseTile, null, 
            ()=> MouseTracker.ButtonStatus[0] == 1, ()=> MouseTracker.ButtonStatus[0] == 0);
        ChooseTile.AddUpdateMethod(() =>
        {
            if (startedMaze)
                mazeDuration += Time.deltaTime;
        });
       ChooseTile.SpecifyTermination(() =>  mouseHandler.SelectedGameObject?.GetComponent<Tile>() != null, SelectionFeedback, () =>
        {
            selectedGO = mouseHandler.SelectedGameObject;
            selectedSD = mouseHandler.SelectedStimDef;
            if (selectedGO.GetComponent<Tile>().mCoord.chessCoord == currMaze.mStart)
            {
                //If the tile that is selected is the start tile, begin the timer for the maze
                mazeStartTime = Time.time;
                startedMaze = true;
                EventCodeManager.SendCodeImmediate(TaskEventCodes["MazeStart"]); 
            }

            if (selectedGO.GetComponent<Tile>().mCoord.chessCoord == currMaze.mFinish && pathProgressIndex + 1 == currMaze.mNumSquares)
            {
                //if the tile that is selected is the end tile, stop the timer
                mazeDuration = Time.time - (float)mazeStartTime;
                CurrentTaskLevel.mazeDurationsList_InBlock.Add(mazeDuration);
                mazeStartTime = null;
                EventCodeManager.SendCodeImmediate(TaskEventCodes["MazeFinish"]);
            }
        });
       ChooseTile.SpecifyTermination(()=> mazeDuration > maxMazeDuration.value, ()=> FinishTrial); // Timeout Termination
       
        SelectionFeedback.AddInitializationMethod(() =>
        {
            if (selectedGO != null)
            {
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["TileFbOn"]);
                
                selectedGO.GetComponent<Tile>().OnMouseDown();
                fbDuration = (tileFbDuration + flashingFbDuration.value);
                SliderFBController.SetUpdateDuration(tileFbDuration);
                SliderFBController.SetFlashingDuration(flashingFbDuration.value);
                
                if (CorrectSelection)
                {
                    SliderFBController.UpdateSliderValue(selectedGO.GetComponent<Tile>().sliderValueChange);
                    playerViewParent.transform.Find((pathProgressIndex + 1).ToString()).GetComponent<Text>().color =
                        new Color(0, 0.392f, 0);
                    EventCodeManager.SendCodeNextFrame(TaskEventCodes["Rewarded"]);
                    EventCodeManager.SendCodeNextFrame(TaskEventCodes["AuditoryFbOn"]);
                }
                else if (ReturnToLast)
                {
                    AudioFBController.Play("Positive");
                    EventCodeManager.SendCodeNextFrame(TaskEventCodes["Rewarded"]); // MAYBE JUST CHANGE TO NEUTRAL TONE??
                    EventCodeManager.SendCodeNextFrame(TaskEventCodes["AuditoryFbOn"]);
                }
                else if (selectedGO != null)
                {
                    AudioFBController.Play("Negative");
                    EventCodeManager.SendCodeNextFrame(TaskEventCodes["Unrewarded"]);
                    EventCodeManager.SendCodeNextFrame(TaskEventCodes["AuditoryFbOn"]);
                }
               
                selectedGO = null; //Reset selectedGO before the next touch evaluation
            }
            else
            {
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["NoChoice"]);
                aborted = true;
                CurrentTaskLevel.numAbortedTrials_InBlock++;
            }
        });
        SelectionFeedback.AddUpdateMethod(() =>
        {
            mazeDuration += Time.deltaTime;
        });
        SelectionFeedback.AddTimer(() => fbDuration, Delay, () =>
        {
            SetTrialSummaryString(); //Set the Trial Summary String to reflect the results of choice

            if (UsingFixedRatioReward)
            {
                if (CorrectSelection && correctTouches_InTrial % CurrentTrialDef.RewardRatio == 0 )
                {
                    if (SyncBoxController != null)
                    {
                        SyncBoxController.SendRewardPulses(CurrentTrialDef.NumPulses, CurrentTrialDef.PulseSize);
                        EventCodeManager.SendCodeNextFrame(TaskEventCodes["Fluid1Onset"]);
                        SessionInfoPanel.UpdateSessionSummaryValues(("totalRewardPulses",CurrentTrialDef.NumPulses));
                        CurrentTaskLevel.numRewardPulses_InBlock += CurrentTrialDef.NumPulses;
                    }
                }
            }
            else if (finishedMaze) 
            {
                StateAfterDelay = ITI;
                DelayDuration = 0;
                
                SliderFBController.ResetSliderBarFull();
                CurrentTaskLevel.numSliderBarFull_InBlock++;
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["SliderCompleteFbOn"]);

                if (SyncBoxController != null)
                {
                    SyncBoxController.SendRewardPulses(CurrentTrialDef.NumPulses, CurrentTrialDef.PulseSize);
                    EventCodeManager.SendCodeNextFrame(TaskEventCodes["Fluid1Onset"]);
                    SessionInfoPanel.UpdateSessionSummaryValues(("totalRewardPulses",CurrentTrialDef.NumPulses));
                    CurrentTaskLevel.numRewardPulses_InBlock += CurrentTrialDef.NumPulses;
                }
            }
            else if (CheckTileFlash())
            {
                StateAfterDelay = TileFlashFeedback;
            }
            else
            {
                StateAfterDelay = ChooseTile; // could be incorrect or correct but it will still go back
            }
            
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["TileFbOff"]);
            CorrectSelection = false;
            ReturnToLast = false;
        });
        TileFlashFeedback.AddInitializationMethod(() =>
        {
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["TileFbOn"]);
            tile.StartCoroutine(tile.FlashingFeedback());
        });
        TileFlashFeedback.AddTimer(() => tileBlinkingDuration.value, ChooseTile, () =>
        {
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["TileFbOff"]);
        });
        ITI.AddInitializationMethod(() =>
        {
            DisableSceneElements();
            DeactivateChildren(playerViewParent);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["MazeOff"]);
            if (finishedMaze)
                EventCodeManager.SendCodeNextFrame(TaskEventCodes["SliderCompleteFbOff"]);

            if (NeutralITI)
            {
                contextName = "itiImage";
                RenderSettings.skybox = CreateSkybox(ContextExternalFilePath + Path.DirectorySeparatorChar + contextName + ".png");
            }
            
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["TrlEnd"]); // ITI begins next frame
        });
        ITI.AddTimer(() => itiDuration.value, FinishTrial);
        
        DefineTrialData();
        DefineFrameData();
    }

    private void InstantiateCurrMaze()
    {
        // This will Load all tiles within the maze and the background of the maze
        
        mazeDims = currMaze.mDims;
        mazeStart = currMaze.mStart;
        mazeFinish = currMaze.mFinish;
        var mazeCenter = new Vector3(0, 0, 0);

        mazeLength = mazeDims.x * TileSize + (mazeDims.x - 1) * spaceBetweenTiles.value;
        mazeHeight = mazeDims.y * TileSize + (mazeDims.y - 1) * spaceBetweenTiles.value;
        MazeBackground.transform.SetParent(MazeContainer.transform); // setting it last so that it doesn't cover tiles
        MazeBackground.transform.localScale = new Vector3(mazeLength + 2 * spaceBetweenTiles.value,
            mazeHeight + 2 * spaceBetweenTiles.value, 0.1f);
        MazeBackground.SetActive(true);
        var bottomLeftMazePos = mazeCenter - new Vector3(mazeLength / 2, mazeHeight / 2, 0);

        tiles = new StimGroup("Tiles");

        for (var x = 1; x <= mazeDims.x; x++)
        for (var y = 1; y <= mazeDims.y; y++)
        {
            // Configures Tile objects and Prefab within the maze container
            tile = Instantiate(TilePrefab, MazeContainer.transform);
            SetGameConfigs();
            tile.transform.localScale = new Vector3(TileSize, TileSize, 0.5f);
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
            tile.GetComponent<Tile>().sliderValueChange = 1f / currMaze.mNumSquares; //FIX THE REWARD MAG BELOW USING STIM DEF ???
            
            if (chessCoordName == currMaze.mStart)
                tile.gameObject.GetComponent<Tile>().setColor(tile.START_COLOR);
            else if (chessCoordName == currMaze.mFinish)
                tile.gameObject.GetComponent<Tile>().setColor(tile.FINISH_COLOR);
            else
                tile.gameObject.GetComponent<Tile>().setColor(tile.DEFAULT_TILE_COLOR);
            
            tiles.AddStims(tile.gameObject);
        }
        mazeLoaded = true;
        TrialStims.Add(tiles);
    }
    public bool CheckTileFlash()
    {
        if (consecutiveErrors >= 2)
        {
            // Should provide flashing feedback of the last correct tile
            Debug.Log("*Perseverative Error*");
            perseverativeErrors_InTrial++;
            CurrentTaskLevel.perseverativeErrors_InBlock++;
            return true;
        }
        return false;
    }


    public int ManageTileTouch(Tile tile)
    {
        var touchedCoord = tile.mCoord;

        // ManageTileTouch - Returns correctness code
        // Return values:
        // 1 - correct tile touch
        // 2 - last correct retouch
        // 10 - rule-abiding incorrect
        // 20 - rule-breaking incorrect (failed to start on start tile, failed to return to last correct after error, diagonal/skips)

        // RULE - BREAKING ERROR : NOT PRESSING START
        if (currMaze.mNextStep == currMaze.mStart && touchedCoord.chessCoord != currMaze.mStart)
        {
            Debug.Log("*Rule Breaking Error - Not Pressing the Start Tile to Begin the Maze*");
            EventCodeManager.SendCodeImmediate(TaskEventCodes["RuleBreakingError"]);

            totalErrors_InTrial++;
            CurrentTaskLevel.totalErrors_InBlock++;
            
            ruleBreakingErrors_InTrial++;
            CurrentTaskLevel.ruleBreakingErrors_InBlock++;

            tileFbDuration = tile.INCORRECT_RULEBREAKING_SECONDS;
            return 20;
        }

        // RULE - BREAKING ERROR : PERSEVERATIVE ERROR WITH TILE IN HIDDEN PATH
        if ((touchedCoord.chessCoord == currMaze.mNextStep || touchedCoord.IsAdjacent(currMaze.mPath[currMaze.mPath.FindIndex( pathCoord =>
                    pathCoord == currMaze.mNextStep) - 1])) && consecutiveErrors != 0)
        {
            Debug.Log("*Rule-Breaking Error - Didn't return to previously correct tile after error, but the tile is in the hidden path*");
            EventCodeManager.SendCodeImmediate(TaskEventCodes["RuleBreakingError"]);
            
            totalErrors_InTrial++;
            CurrentTaskLevel.totalErrors_InBlock++;
            
            ruleBreakingErrors_InTrial++;
            CurrentTaskLevel.ruleBreakingErrors_InBlock++;
            
            consecutiveErrors++;
            
            tileFbDuration = tile.INCORRECT_RULEBREAKING_SECONDS;
            return 20;
        }
        
        // CORRECT TILE TOUCH
        if (touchedCoord.chessCoord == currMaze.mNextStep && consecutiveErrors == 0)
        {
            Debug.Log("*Correct Tile Touch*");
            EventCodeManager.SendCodeImmediate(TaskEventCodes["CorrectResponse"]);

            correctTouches_InTrial++;
            CurrentTaskLevel.correctTouches_InBlock++;
            
            CorrectSelection = true;
            
            // Helps set progress on the experimenter display
            pathProgress.Add(touchedCoord);
            pathProgressGO.Add(tile.gameObject);
            pathProgressIndex = currMaze.mPath.FindIndex(pathCoord => pathCoord == touchedCoord.chessCoord);
            
            //sets the duration of tile feedback
            tileFbDuration = tile.CORRECT_FEEDBACK_SECONDS;
            
            // Sets the NextStep if the maze isn't finished
            if (touchedCoord.chessCoord != currMaze.mFinish)
            {
                currMaze.mNextStep =
                    currMaze.mPath[currMaze.mPath.FindIndex(pathCoord => pathCoord == touchedCoord.chessCoord) + 1];
            }
                
            else
            {
                finishedMaze = true; // Finished the Maze
            }

            return 1;
        }

        // LAST CORRECT TILE TOUCH - idk what kind of error feedback it gives?? just makes dark green tile
        if (currMaze.mPath[currMaze.mPath.FindIndex(pathCoord => pathCoord == currMaze.mNextStep) - 1] ==
            touchedCoord.chessCoord)
        {
            Debug.Log("*Last Correct Tile Touch*");
            EventCodeManager.SendCodeImmediate(TaskEventCodes["LastCorrectSelection"]);

            ReturnToLast = true;
            tileFbDuration = tile.PREV_CORRECT_FEEDBACK_SECONDS;
            
            retouchCorrect_InTrial++;
            CurrentTaskLevel.retouchCorrect_InBlock++;
           
            consecutiveErrors = 0;
            return 2;
        }

        // RULE ABIDING INCORRECT TOUCH 
        if (currMaze.mNextStep != currMaze.mStart && touchedCoord.IsAdjacent(currMaze.mPath[
                currMaze.mPath.FindIndex(pathCoord =>
                    pathCoord == currMaze.mNextStep) - 1]) && !pathProgress.Contains(touchedCoord))
        {
            Debug.Log("*Rule-Abiding Incorrect Error*");
            consecutiveErrors++;
            
            totalErrors_InTrial++;
            CurrentTaskLevel.totalErrors_InBlock++;
            
            ruleAbidingErrors_InTrial++;
            CurrentTaskLevel.ruleAbidingErrors_InBlock++;
            
            tileFbDuration = tile.INCORRECT_RULEABIDING_SECONDS;
            return 10;
        }

        // RULE BREAKING BACKGTRACK ERROR
        if (pathProgress.Contains(touchedCoord))
        {
            Debug.Log("*Rule-Breaking Backtrack Error*");
            EventCodeManager.SendCodeImmediate(TaskEventCodes["RuleBreakingError"]);
            
            backtrackErrors_InTrial++;
            CurrentTaskLevel.backtrackErrors_InBlock++;
        }

        // RULE BREAKING TOUCH
        else
        {
            Debug.Log("*Rule-Breaking Incorrect Error*");
            EventCodeManager.SendCodeImmediate(TaskEventCodes["RuleBreakingError"]);

            totalErrors_InTrial++;
            CurrentTaskLevel.totalErrors_InBlock++;
            
            ruleBreakingErrors_InTrial++;
            CurrentTaskLevel.ruleBreakingErrors_InBlock++;
            
            consecutiveErrors++;
            tileFbDuration = tile.INCORRECT_RULEBREAKING_SECONDS;
        }
        return 20;
    }
    private void LoadMazeDef()
    {
        // textMaze will load the text file containing the full Maze path of the intended mazeDef for the block/trial
        var textMaze = File.ReadAllLines(MazeFilePath + Path.DirectorySeparatorChar + mazeDefName);
        currMaze = new Maze(textMaze[0]);
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
        configVariablesLoaded = true;
        //disableVariables();
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

    private void DefineTrialData()
    {
        TrialData.AddDatum("MazeDefName", ()=> mazeDefName);
        TrialData.AddDatum("TotalErrors", () => totalErrors_InTrial);
        // TrialData.AddDatum("CorrectTouches", () => correctTouches_InTrial); DOESN'T GIVE ANYTHING USEFUL, JUST PATH LENGTH
        TrialData.AddDatum("RetouchCorrect", () => retouchCorrect_InTrial);
        TrialData.AddDatum("PerseverativeErrors", () => perseverativeErrors_InTrial);
        TrialData.AddDatum("BacktrackingErrors", () => backtrackErrors_InTrial);
        TrialData.AddDatum("Rule-AbidingErrors", () => ruleAbidingErrors_InTrial);
        TrialData.AddDatum("Rule-BreakingErrors", () => ruleBreakingErrors_InTrial);
        TrialData.AddDatum("MazeDuration", ()=> mazeDuration);
    }

    private void DefineFrameData()
    {
        FrameData.AddDatum("Context", ()=> contextName);
    }
    private void DisableSceneElements()
    {
        StartButton.SetActive(false);
        DeactivateChildren(MazeContainer);
        DeactivateChildren(GameObject.Find("SliderCanvas"));
    } 

    private void CreateTextOnExperimenterDisplay()
    {
        // sets parent for any playerView elements on experimenter display
        playerView = new PlayerViewPanel(); //GameObject.Find("PlayerViewCanvas").GetComponent<PlayerViewPanel>()
        for (int i = 0; i < currMaze.mPath.Count; i++)
        {
            foreach (StimDef sd in tiles.stimDefs)
            {
                Tile tileComponent = sd.StimGameObject.GetComponent<Tile>();
                Vector2 textSize = new Vector2(200, 200);
                
                if (tileComponent.mCoord.chessCoord == currMaze.mPath[i])
                {
                    textLocation = playerViewPosition(Camera.main.WorldToScreenPoint(tileComponent.transform.position), playerViewParent.transform);
                    playerViewText = playerView.WriteText((i + 1).ToString(), (i + 1).ToString(),
                        Color.red, textLocation, textSize, playerViewParent.transform);
                    playerViewText.GetComponent<RectTransform>().localScale = new Vector3(2, 2, 0);
                    playerViewTextList.Add(playerViewText);
                }
            }
        }
        playerViewLoaded = true;
    }

    public override void FinishTrialCleanup()
    {
        DisableSceneElements();
        DeactivateChildren(playerViewParent);
        if (mazeLoaded)
        {
            tiles.DestroyStimGroup();
            mazeLoaded = false;
        }
        
        if (TokenFBController.isActiveAndEnabled)
            TokenFBController.enabled = false;

        if(AbortCode == 0)
            CurrentTaskLevel.CalculateBlockSummaryString();

        if(AbortCode == AbortCodeDict["RestartBlock"])
        {
            CurrentTaskLevel.ClearStrings();
            CurrentTaskLevel.BlockSummaryString.AppendLine("");
        }
        
        EventCodeManager.SendCodeNextFrame(TaskEventCodes["TrlStart"]); //next trial starts next frame
    }

    public override void ResetTrialVariables()
    {
        mazeDuration = null;
        mazeStartTime = null;
        finishedMaze = false;
        startedMaze = false;
        selectedGO = null;
        selectedSD = null;
        aborted = false;
        
        totalErrors_InTrial = 0;
        correctTouches_InTrial = 0;
        retouchCorrect_InTrial = 0;
        perseverativeErrors_InTrial = 0;
        backtrackErrors_InTrial = 0;
        ruleAbidingErrors_InTrial = 0;
        ruleBreakingErrors_InTrial = 0;

        pathProgress.Clear();
        pathProgressGO.Clear();
        consecutiveErrors = 0;

        if (playerViewTextList.Count > 0)
        {
            foreach (var txt in playerViewTextList)
            {
                txt.GetComponent<Text>().color = Color.red; //resets the color if we repeat the sequence in the block
            }
        }
    }
    void SetTrialSummaryString()
    {


        TrialSummaryString = "<b>Trial Count in Block: " + (TrialCount_InBlock + 1) + "</b>" +
                             "\nTrial Count in Task: " + (TrialCount_InTask + 1) +
                             "\n" +
                             "\nTotal Errors: " + totalErrors_InTrial +
                             //"\nCorrect Touches: " + correctTouches_InBlock + COME UP WITH SOMETHING MORE USEFUL
                             "\nRule-Abiding Errors: " + ruleAbidingErrors_InTrial +
                             "\nRule-Breaking Errors: " + ruleBreakingErrors_InTrial + 
                             "\nPerseverative Errors: " + perseverativeErrors_InTrial +
                             "\nBacktrack Errors: " + backtrackErrors_InTrial +
                             "\nRetouch Correct: " + retouchCorrect_InTrial+ 
                             "\nMaze Duration: " + mazeDuration +
                             "\n" +
                             "\nSlider Value: " + SliderFBController.Slider.value;

    }
}