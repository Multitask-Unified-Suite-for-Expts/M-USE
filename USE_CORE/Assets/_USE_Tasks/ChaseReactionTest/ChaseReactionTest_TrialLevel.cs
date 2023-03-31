using System;
using System.Collections.Generic;
using System.Linq;
using ConfigDynamicUI;
using HiddenMaze;
using ChaseReactionTest_Namespace;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Trial;
using USE_States;
using USE_StimulusManagement;
using USE_UI;

public class ChaseReactionTest_TrialLevel : ControlLevel_Trial_Template
{
    // Generic Task Variables
    public GameObject CRT_CanvasGO;
    public USE_StartButton USE_StartButton;
    private GameObject StartButton;
    
    // Block Ending Variable
    public List<float> runningTrialPerformance = new List<float>();
    private float trialPerformance;
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
    List<GameObject> totalTilesGO = new List<GameObject>();
    
    // Maze Progress Variables
    public GameObject startTile;
    public bool viewPath;
    
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
    
    // Player View Variables
    private PlayerViewPanel playerView;
    private GameObject playerViewParent; // Helps set things onto the player view in the experimenter display
    public List<GameObject> playerViewTextList;
    public GameObject playerViewText;
    private Vector2 textLocation;
    private int pathProgressIndex;
    
    // Trial Data Variables
    private int[] totalErrors_InTrial;
    
    public ChaseReactionTest_TrialDef CurrentTrialDef => GetCurrentTrialDef<ChaseReactionTest_TrialDef>();
    public ChaseReactionTest_TaskLevel CurrentTaskLevel => GetTaskLevel<ChaseReactionTest_TaskLevel>();
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

        SelectionHandler<ChaseReactionTest_StimDef> mouseHandler = new SelectionHandler<ChaseReactionTest_StimDef>();
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
                MazeBackground = CreateSquare("MazeBackground", mazeBgTex, new Vector3(0, 0, 0),
                    new Vector3(5, 5, 5));
            
            //player view variables
            playerViewParent = GameObject.Find("MainCameraCopy");
        });
        SetupTrial.AddInitializationMethod(() =>
        {
            if(StartButton == null)
            {
                USE_StartButton = new USE_StartButton(CRT_CanvasGO.GetComponent<Canvas>(), StartButtonPosition, StartButtonScale);
                StartButton = USE_StartButton.StartButtonGO;
                USE_StartButton.SetVisibilityOnOffStates(InitTrial, InitTrial);
            }

            if (!configVariablesLoaded)
                LoadConfigVariables();
            CurrentTaskLevel.LoadTextMaze();
            pathProgressIndex = 0;
            Input.ResetInputAxes(); //reset input in case they still touching their selection from last trial!
        });
        SetupTrial.SpecifyTermination(() => true, InitTrial);
        MouseTracker.AddSelectionHandler(mouseHandler, InitTrial, null, 
            ()=> MouseTracker.ButtonStatus[0] == 1, ()=> MouseTracker.ButtonStatus[0] == 0);
        InitTrial.SpecifyTermination(() => mouseHandler.SelectionMatches(StartButton), Delay, () =>
        {
            EventCodeManager.SendCodeImmediate(SessionEventCodes["StartButtonSelected"]);

            StateAfterDelay = ChooseTile;
            DelayDuration = mazeOnsetDelay.value;
            SliderFBController.ConfigureSlider(new Vector3(0,180,0), sliderSize.value);
            SliderFBController.SliderGO.SetActive(true);
            SetTrialSummaryString();
            
            InstantiateCurrMaze();
            tiles.ToggleVisibility(true);
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["MazeOn"]);
            
            if (playerViewParent.transform.childCount == 0)
                CreateTextOnExperimenterDisplay();
            else
                ActivateChildren(playerViewParent);
        });
        TileFlash.AddInitializationMethod(() =>
        {
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["FlashingTileFbOn"]);
            tile = totalTilesGO[pathProgressIndex+1].GetComponent<Tile>();
            tile.StartCoroutine(tile.FlashingFeedback());
        });
    }

    protected override void DefineTrialStims()
    {
        //Define StimGroups consisting of StimDefs whose gameobjects will be loaded at TrialLevel_SetupTrial and 
        //destroyed at TrialLevel_Finish
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
    }
    void SetTrialSummaryString()
    {
        TrialSummaryString = "<b>Maze Name: </b>" + mazeDefName +
                             "\n" + 
                             "\nTotal Errors: " + totalErrors_InTrial.Sum() +
                             "\nTrial Performance: " + trialPerformance + 
                            // "\nMaze Duration: " + mazeDuration +
                             "\n" +
                             "\nSlider Value: " + SliderFBController.Slider.value;

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

        if (AbortCode == AbortCodeDict["RestartBlock"] || AbortCode == AbortCodeDict["PreviousBlock"] || AbortCode == AbortCodeDict["EndBlock"]) //If used RestartBlock, PreviousBlock, or EndBlock hotkeys
        {
            CurrentTaskLevel.numAbortedTrials_InBlock++;
            CurrentTaskLevel.numAbortedTrials_InTask++;
            CurrentTaskLevel.ClearStrings();
            CurrentTaskLevel.BlockSummaryString.AppendLine("");
        }
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
            tile.GetComponent<Tile>().sliderValueChange = 1f / CurrentTaskLevel.currMaze.mNumSquares; //FIX THE REWARD MAG BELOW USING STIM DEF ???

            if (chessCoordName == CurrentTaskLevel.currMaze.mStart)
            {
                tile.gameObject.GetComponent<Tile>().setColor(tile.START_COLOR);
                startTile = tile.gameObject; // Have to define to perform feedback if they haven't selected the start yet 
                //Consider making a separate group for the tiles in the path, this might not improve function that much?
            }
                
            else if (chessCoordName == CurrentTaskLevel.currMaze.mFinish)
                tile.gameObject.GetComponent<Tile>().setColor(tile.FINISH_COLOR);
            else
                tile.gameObject.GetComponent<Tile>().setColor(tile.DEFAULT_TILE_COLOR);
            
            tiles.AddStims(tile.gameObject);
        }
        mazeLoaded = true;
        foreach (StimDef sd in tiles.stimDefs)
        {
            GameObject tileGO = sd.StimGameObject;
            Tile tileComponent = sd.StimGameObject.GetComponent<Tile>();
            totalTilesGO.Add(tileGO);
        }
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
