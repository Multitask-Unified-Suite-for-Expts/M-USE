using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ConfigDynamicUI;
using HiddenMaze;
using MazeGame_Namespace;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.UI;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Trial;
using USE_States;
using USE_StimulusManagement;
using USE_UI;

public class MazeGame_TrialLevel : ControlLevel_Trial_Template
{
    // Generic Task Variables
    public GameObject MG_CanvasGO;
    private GameObject StartButton;
    [HideInInspector] public string ContextName;
    // Block Ending Variable
    public List<float?> runningPercentError = new List<float?>();
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
    public Texture2D tileTex;
    public Texture2D mazeBgTex;

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
    private bool ErroneousReturnToLast;
    public float tileFbDuration;
    public GameObject startTile;
    public GameObject finishTile;
    private float tileScale;

    // Trial Data Tracking Variables
    public float mazeDuration;
    private float mazeStartTime;
    private float choiceDuration;
    private float choiceStartTime;
    private int[] totalErrors_InTrial;
    private int[] ruleAbidingErrors_InTrial;
    private int[] ruleBreakingErrors_InTrial;
    private int[] retouchCorrect_InTrial;
    private int[] retouchErroneous_InTrial;
    private int correctTouches_InTrial;
    private int[] backtrackErrors_InTrial;
    private int[] perseverativeErrors_InTrial;
    private bool choiceMade;
    public List<float> choiceDurationsList = new List<float>();
    private int flashingCounter;

    // Task Level Defined Color Variables
    [HideInInspector]
    public int NumBlinks;
    public Tile TilePrefab;
    public string MazeFilePath;

    // Config UI Variables
    private bool configVariablesLoaded;
    [HideInInspector]
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
    private List<string> SelectedTiles_InTrial = new List<string>();
    // private StimDef selectedSD;

    // Slider & Animation variables
    private float sliderValueChange;
    private float finishedFbDuration;

    public MazeGame_TrialDef CurrentTrialDef => GetCurrentTrialDef<MazeGame_TrialDef>();
    public MazeGame_TaskLevel CurrentTaskLevel => GetTaskLevel<MazeGame_TaskLevel>();
    public MazeGame_TaskDef currentTaskDef => GetTaskDef<MazeGame_TaskDef>();


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
            { "InitTrial", "ChooseTile", "SelectionFeedback", "TileFlashFeedback", "ITI"};
        
        Add_ControlLevel_InitializationMethod(() =>
        {
            SliderFBController.InitializeSlider();
            HaloFBController.SetHaloSize(5);

            FileLoadingDelegate = LoadTileAndBgTextures; //Set file loading delegate

            if(!SessionValues.WebBuild) //player view variables
            {
                playerView = gameObject.AddComponent<PlayerViewPanel>();
                playerViewParent = GameObject.Find("MainCameraCopy");
            }
        });


        SetupTrial.AddSpecificInitializationMethod(() =>
        {
            if (StartButton == null)
            {
                if (SessionValues.SessionDef.IsHuman)
                {
                    StartButton = SessionValues.HumanStartPanel.StartButtonGO;
                    SessionValues.HumanStartPanel.SetVisibilityOnOffStates(InitTrial, InitTrial);
                }
                else
                {
                    StartButton = SessionValues.USE_StartButton.CreateStartButton(MG_CanvasGO.GetComponent<Canvas>(), currentTaskDef.StartButtonPosition, currentTaskDef.StartButtonScale);
                    SessionValues.USE_StartButton.SetVisibilityOnOffStates(InitTrial, InitTrial);
                }
            }

            if (!configVariablesLoaded)
                LoadConfigVariables();

            // Load Maze at the start of every trial to keep the mNextStep consistent
            CurrentTaskLevel.SetTaskSummaryString();
            Input.ResetInputAxes(); //reset input in case they still touching their selection from last trial!
        });
        SetupTrial.SpecifyTermination(() => true, InitTrial);
        var SelectionHandler = SessionValues.SelectionTracker.SetupSelectionHandler("trial", "MouseButton0Click", SessionValues.MouseTracker, InitTrial, ITI);
        TouchFBController.EnableTouchFeedback(SelectionHandler, currentTaskDef.TouchFeedbackDuration, currentTaskDef.StartButtonScale*10, MG_CanvasGO);

        InitTrial.AddSpecificInitializationMethod(() =>
        {
            Camera.main.gameObject.GetComponent<Skybox>().enabled = false; //Disable cam's skybox so the RenderSettings.Skybox can show the Context background

            SelectionHandler.HandlerActive = true;
            if (SelectionHandler.AllSelections.Count > 0)
                SelectionHandler.ClearSelections();
            SelectionHandler.MinDuration = minObjectTouchDuration.value;
            SelectionHandler.MaxDuration = maxObjectTouchDuration.value;
        });

        InitTrial.SpecifyTermination(() => SelectionHandler.LastSuccessfulSelectionMatchesStartButton(), Delay, () =>
        {
            SessionValues.EventCodeManager.SendCodeNextFrame(TaskEventCodes["MazeOn"]);

            if (CurrentTrialDef.GuidedMazeSelection)
                StateAfterDelay = TileFlashFeedback;
            else
                StateAfterDelay = ChooseTile;
            
            DelayDuration = mazeOnsetDelay.value;
            
            SliderFBController.ConfigureSlider(sliderSize.value);
            SliderFBController.SliderGO.SetActive(true);

            if (!SessionValues.WebBuild)
                CreateTextOnExperimenterDisplay();

            mazeStartTime = Time.unscaledTime;

            CurrentTaskLevel.CalculateBlockSummaryString();
            SetTrialSummaryString();

        });

        ChooseTile.AddSpecificInitializationMethod(() =>
        {
            //TouchFBController.SetPrefabSizes(tileScale);
            MazeBackground.SetActive(true);
            if(!tiles.IsActive)
                tiles.ToggleVisibility(true);
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
                    CurrentTaskLevel.ChoiceDurations_InBlock.Add(choiceDuration);
                    CurrentTaskLevel.ChoiceDurations_InTask.Add(choiceDuration);
                    selectedGO = SelectionHandler.LastSuccessfulSelection.SelectedGameObject;
                    SelectedTiles_InTrial.Add(selectedGO.name);
                    SelectionHandler.ClearSelections();
                }
            }
        });
        ChooseTile.SpecifyTermination(() =>  choiceMade, SelectionFeedback, () =>
        {
            SelectionHandler.HandlerActive = false;

            if (selectedGO.GetComponent<Tile>().mCoord.chessCoord ==  CurrentTaskLevel.currMaze.mStart)
            {
                //If the tile that is selected is the start tile
                startedMaze = true;
                if (SessionValues.SessionDef.EventCodesActive)
                    SessionValues.EventCodeManager.SendCodeImmediate(TaskEventCodes["MazeStart"]); 
            }

            if (selectedGO.GetComponent<Tile>().mCoord.chessCoord == CurrentTaskLevel.currMaze.mFinish && CurrentTaskLevel.currMaze.mNextStep == CurrentTaskLevel.currMaze.mFinish)
            {
                //if the tile that is selected is the end tile, stop the timer
                mazeDuration = Time.unscaledTime - mazeStartTime;
                CurrentTaskLevel.MazeDurations_InBlock.Add(mazeDuration);
                CurrentTaskLevel.MazeDurations_InTask.Add(mazeDuration);
                if (SessionValues.SessionDef.EventCodesActive)
                    SessionValues.EventCodeManager.SendCodeImmediate(TaskEventCodes["MazeFinish"]);
            }
        });
        ChooseTile.SpecifyTermination(()=> (mazeDuration > CurrentTrialDef.MaxMazeDuration) || (choiceDuration > CurrentTrialDef.MaxChoiceDuration), ()=> FinishTrial, () =>
        {
            // Timeout Termination
            SessionValues.EventCodeManager.SendCodeImmediate("NoChoice");
            SessionValues.EventCodeManager.SendRangeCode("CustomAbortTrial", AbortCodeDict["NoSelectionMade"]);
            AbortCode = 6;

            CurrentTaskLevel.MazeDurations_InBlock.Add(null);
            CurrentTaskLevel.MazeDurations_InTask.Add(null);

            CurrentTaskLevel.ChoiceDurations_InBlock.Add(null);
            CurrentTaskLevel.ChoiceDurations_InTask.Add(null);

            runningPercentError.Add(null);
        }); 
       
        SelectionFeedback.AddSpecificInitializationMethod(() =>
        {
            if (SessionValues.SessionDef.EventCodesActive)
                SessionValues.EventCodeManager.SendCodeNextFrame(TaskEventCodes["TileFbOn"]);
            choiceMade = false;
            // This is what actually determines the result of the tile choice
            selectedGO.GetComponent<Tile>().SelectionFeedback();
            percentError = (float)decimal.Divide(totalErrors_InTrial.Sum(),CurrentTaskLevel.currMaze.mNumSquares);

            finishedFbDuration = (tileFbDuration + flashingFbDuration.value);
            SliderFBController.SetUpdateDuration(tileFbDuration);
            SliderFBController.SetFlashingDuration(finishedFbDuration);
            
            if (ReturnToLast)
            {
                AudioFBController.Play("Positive");
                if (CurrentTrialDef.ErrorPenalty)
                    SliderFBController.UpdateSliderValue(selectedGO.GetComponent<Tile>().sliderValueChange);
            }
            else if (ErroneousReturnToLast)
            {
                AudioFBController.Play("Negative");
            }
            else if (CorrectSelection)
            {
                SliderFBController.UpdateSliderValue(selectedGO.GetComponent<Tile>().sliderValueChange);
                if(!SessionValues.WebBuild)
                    playerViewParent.transform.Find((pathProgressIndex + 1).ToString()).GetComponent<Text>().color = new Color(0, 0.392f, 0);
            }
            else if (selectedGO != null && !ErroneousReturnToLast)
            {
                AudioFBController.Play("Negative");
                if (CurrentTrialDef.ErrorPenalty && consecutiveErrors == 1)
                    SliderFBController.UpdateSliderValue(-selectedGO.GetComponent<Tile>().sliderValueChange);
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
            if (currentTaskDef.UsingFixedRatioReward)
            {
                if (CorrectSelection && correctTouches_InTrial % CurrentTrialDef.RewardRatio == 0 )
                {
                    if (SessionValues.SyncBoxController != null)
                    {
                        SessionValues.SyncBoxController.SendRewardPulses(1, CurrentTrialDef.PulseSize);
                        CurrentTaskLevel.NumRewardPulses_InBlock++;;
                        CurrentTaskLevel.NumRewardPulses_InTask++;
                    }
                }
            }

            if (finishedMaze) 
            {
                StateAfterDelay = ITI;
                DelayDuration = 0;

                percentError = (float)decimal.Divide(totalErrors_InTrial.Sum(),CurrentTaskLevel.currMaze.mNumSquares);
                runningPercentError.Add(percentError);
                CurrentTaskLevel.NumSliderBarFull_InBlock++;
                CurrentTaskLevel.NumSliderBarFull_InTask++;
                SessionValues.EventCodeManager.SendCodeNextFrame("SliderFbController_SliderCompleteFbOn");

                if (SessionValues.SyncBoxController != null)
                {
                    SessionValues.SyncBoxController.SendRewardPulses(CurrentTrialDef.NumPulses, CurrentTrialDef.PulseSize);
                   // SessionInfoPanel.UpdateSessionSummaryValues(("totalRewardPulses",CurrentTrialDef.NumPulses)); moved to syncbox class
                    CurrentTaskLevel.NumRewardPulses_InBlock += CurrentTrialDef.NumPulses;
                    CurrentTaskLevel.NumRewardPulses_InTask += CurrentTrialDef.NumPulses;
                }
            }
            else if (CheckTileFlash() || (CurrentTrialDef.GuidedMazeSelection && GameObject.Find(CurrentTaskLevel.currMaze.mNextStep).GetComponent<Tile>().assignedTileFlash))
            {
                StateAfterDelay = TileFlashFeedback;
            }
            else
            {
                StateAfterDelay = ChooseTile; // could be incorrect or correct but it will still go back
            }

            if (SessionValues.SessionDef.EventCodesActive)
                SessionValues.EventCodeManager.SendCodeNextFrame(TaskEventCodes["TileFbOff"]);
            CorrectSelection = false;
            ReturnToLast = false;
            ErroneousReturnToLast = false;

            SetTrialSummaryString(); //Set the Trial Summary String to reflect the results of choice
            CurrentTaskLevel.CalculateBlockSummaryString();
            CurrentTaskLevel.SetTaskSummaryString();
        });
        TileFlashFeedback.AddSpecificInitializationMethod(() =>
        {
            if (SessionValues.SessionDef.EventCodesActive)
                SessionValues.EventCodeManager.SendCodeNextFrame(TaskEventCodes["FlashingTileFbOn"]);
            if (!tiles.IsActive)
                tiles.ToggleVisibility(true);
            MazeBackground.SetActive(true);
            tile.NextCorrectFlashingFeedback();
        });
        TileFlashFeedback.AddTimer(() => tileBlinkingDuration.value, ChooseTile, () =>
        {
            if (SessionValues.SessionDef.EventCodesActive)
                SessionValues.EventCodeManager.SendCodeNextFrame(TaskEventCodes["FlashingTileFbOff"]);
        });
        ITI.AddSpecificInitializationMethod(() =>
        {

            DisableSceneElements();
            if (!SessionValues.WebBuild)
                DestroyChildren(playerViewParent);
            MazeBackground.SetActive(false);
            tiles.ToggleVisibility(false);
            SessionValues.EventCodeManager.SendCodeNextFrame(TaskEventCodes["MazeOff"]);

            if (finishedMaze)
                SessionValues.EventCodeManager.SendCodeNextFrame("SliderFbController_SliderCompleteFbOff");
            
            if (currentTaskDef.NeutralITI)
            {
                ContextName = "NeutralITI";
                CurrentTaskLevel.SetSkyBox(GetContextNestedFilePath(!string.IsNullOrEmpty(currentTaskDef.ContextExternalFilePath) ? currentTaskDef.ContextExternalFilePath : SessionValues.SessionDef.ContextExternalFilePath, "NeutralITI"));
            }
        });
        ITI.AddTimer(() => itiDuration.value, FinishTrial);
        DefineTrialData();
        DefineFrameData();
    }


    //This method is for EventCodes and gets called automatically at end of SetupTrial:
    public override void AddToStimLists()
    {
        foreach (StimDef stim in tiles.stimDefs)
        {
            if (CurrentTaskLevel.currMaze.mPath.Contains(stim.StimGameObject.name))
                SessionValues.TargetObjects.Add(stim.StimGameObject);
        }
    }

    private IEnumerator LoadTileAndBgTextures()
    {
        if (MazeBackground != null || MazeContainer != null) //since its gonna be called every trial, only want it to load them the first time. 
        {
            TrialFilesLoaded = true; //Setting this to true triggers the LoadTrialTextures state to end
            yield break; 
        }

        string contextPath = !string.IsNullOrEmpty(currentTaskDef.ContextExternalFilePath) ? currentTaskDef.ContextExternalFilePath : SessionValues.SessionDef.ContextExternalFilePath;

        if (SessionValues.UsingServerConfigs)
        {
            yield return StartCoroutine(LoadTexture(contextPath + "/" + currentTaskDef.TileTexture + ".png", textureResult =>
            {
                if (textureResult != null)
                    tileTex = textureResult;
                else
                    Debug.LogWarning("TILE TEX RESULT IS NULL!");
            }));

            yield return StartCoroutine(LoadTexture(contextPath + "/" + currentTaskDef.MazeBackgroundTexture + ".png", textureResult =>
            {
                if (textureResult != null)
                    mazeBgTex = textureResult;
                else
                    Debug.LogWarning("MAZE BACKGROUND TEXTURE RESULT IS NULL!");
            }));
        }
        else if (SessionValues.UsingDefaultConfigs)
        {
            tileTex = Resources.Load<Texture2D>($"{SessionValues.DefaultContextFolderPath}/{currentTaskDef.TileTexture}");
            mazeBgTex = Resources.Load<Texture2D>($"{SessionValues.DefaultContextFolderPath}/{currentTaskDef.MazeBackgroundTexture}");
        }
        else if (SessionValues.UsingLocalConfigs)
        {
            tileTex = LoadPNG(GetContextNestedFilePath(contextPath, currentTaskDef.TileTexture));
            mazeBgTex = LoadPNG(GetContextNestedFilePath(contextPath, currentTaskDef.MazeBackgroundTexture));
        }

        if (MazeContainer == null)
            MazeContainer = new GameObject("MazeContainer");

        if (MazeBackground == null)
            MazeBackground = CreateSquare("MazeBackground", mazeBgTex, currentTaskDef.MazePosition, new Vector3(5, 5, 5));

        TrialFilesLoaded = true; //Setting this to true triggers the LoadTrialTextures state to end
    }

    public void InitializeTrialArrays()
    {
        ruleAbidingErrors_InTrial = new int[CurrentTaskLevel.currMaze.mNumSquares];
        ruleBreakingErrors_InTrial = new int[CurrentTaskLevel.currMaze.mNumSquares];
        backtrackErrors_InTrial = new int[CurrentTaskLevel.currMaze.mNumSquares];
        perseverativeErrors_InTrial = new int[CurrentTaskLevel.currMaze.mNumSquares];
        totalErrors_InTrial = new int[CurrentTaskLevel.currMaze.mNumSquares];
        retouchErroneous_InTrial = new int[CurrentTaskLevel.currMaze.mNumSquares];
        retouchCorrect_InTrial = new int[CurrentTaskLevel.currMaze.mNumSquares];
    }

    protected override bool CheckBlockEnd()
    {
        TaskLevelTemplate_Methods TaskLevel_Methods = new TaskLevelTemplate_Methods();
        return TaskLevel_Methods.CheckBlockEnd(CurrentTrialDef.BlockEndType, runningPercentError,
            CurrentTrialDef.BlockEndThreshold, CurrentTaskLevel.MinTrials_InBlock,
            CurrentTaskLevel.MaxTrials_InBlock);
    }
    protected override void DefineTrialStims()
    {
        // This will Load all tiles within the maze and the background of the maze
        mazeDims = CurrentTaskLevel.currMaze.mDims;
        var mazeCenter = MazeBackground.transform.localPosition;

        mazeLength = mazeDims.x * currentTaskDef.TileSize + (mazeDims.x - 1) * currentTaskDef.SpaceBetweenTiles;
        mazeHeight = mazeDims.y * currentTaskDef.TileSize + (mazeDims.y - 1) * currentTaskDef.SpaceBetweenTiles;
        MazeBackground.transform.SetParent(MazeContainer.transform); // setting it last so that it doesn't cover tiles
        MazeBackground.transform.localScale = new Vector3(mazeLength + 2 * currentTaskDef.SpaceBetweenTiles,
            mazeHeight + 2 * (currentTaskDef.SpaceBetweenTiles), 0.1f);
        MazeBackground.SetActive(false);
        var bottomLeftMazePos = mazeCenter - new Vector3(mazeLength / 2, mazeHeight / 2, 0);

        tiles = new StimGroup("Tiles");

        
        for (var x = 1; x <= mazeDims.x; x++)
        for (var y = 1; y <= mazeDims.y; y++)
        {
            // Instantiate the tile
            tile = Instantiate(TilePrefab, MazeContainer.transform);
            tile.mgTL = this;

            StimDef tileStimDef = new StimDef(tiles, tile.gameObject);
            
            tile.transform.localScale = new Vector3(currentTaskDef.TileSize, currentTaskDef.TileSize, 0.15f);
            tileStimDef.StimGameObject.SetActive(false);
            tileStimDef.StimGameObject.GetComponent<Tile>().enabled = true;
            tileStimDef.StimGameObject.GetComponent<MeshRenderer>().sharedMaterial.mainTexture = tileTex;
            var displaceX = (2 * (x - 1) + 1) * (currentTaskDef.TileSize / 2) + currentTaskDef.SpaceBetweenTiles * (x - 1);
            var displaceY = (2 * (y - 1) + 1) * (currentTaskDef.TileSize / 2) + currentTaskDef.SpaceBetweenTiles * (y - 1);
            var newTilePosition = bottomLeftMazePos + new Vector3(displaceX, displaceY, 0);
            tile.transform.position = newTilePosition;
            
            // Assigns ChessCoordName to the tile 
            string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string chessCoordName = $"{alphabet[x-1]}{y}";
            tileStimDef.StimGameObject.GetComponent<Tile>().mCoord = new Coords(chessCoordName);
            tileStimDef.StimGameObject.name = chessCoordName;
            // Assigns Reward magnitude for each tile (set to proportional to the number of squares in path)
            tileStimDef.StimGameObject.GetComponent<Tile>().sliderValueChange = 1f / CurrentTaskLevel.currMaze.mNumSquares; //FIX THE REWARD MAG BELOW USING STIM DEF ???

            if (chessCoordName == CurrentTaskLevel.currMaze.mStart)
            {
                tileStimDef.StimGameObject.GetComponent<Tile>().setColor(tile.START_COLOR);
                startTile = tileStimDef.StimGameObject; // Have to define to perform feedback if they haven't selected the start yet 
                //Consider making a separate group for the tiles in the path, this might not improve function that much?
            }
                
            else if (chessCoordName == CurrentTaskLevel.currMaze.mFinish)
            {
                tileStimDef.StimGameObject.GetComponent<Tile>().setColor(tile.FINISH_COLOR);
                finishTile = tileStimDef.StimGameObject;
                tileStimDef.StimGameObject.GetComponent<Tile>().sliderValueChange = (float)tile.GetComponent<Tile>().sliderValueChange; // to ensure it fills all the way up
            }
            else if (!CurrentTrialDef.DarkenNonPathTiles || CurrentTaskLevel.currMaze.mPath.Contains((chessCoordName)))
                tileStimDef.StimGameObject.GetComponent<Tile>().setColor(tile.DEFAULT_TILE_COLOR);
            else
                tileStimDef.StimGameObject.GetComponent<Tile>().setColor(new Color(0.5f, 0.5f, 0.5f));
            
            tiles.AddStims(tileStimDef);
            
        }
        mazeLoaded = true;
        //Make sure to reset the maze to start at the start tile
        CurrentTaskLevel.currMaze.mNextStep = CurrentTaskLevel.currMaze.mStart;

        AssignFlashingTiles();
        TrialStims.Add(tiles);
    }
    public bool CheckTileFlash()
    {
        if (consecutiveErrors >= 2)
        {
            // Should provide flashing feedback of the last correct tile
            Debug.Log("*Perseverative Error*");
            if (SessionValues.SessionDef.EventCodesActive)
                SessionValues.EventCodeManager.SendCodeImmediate(TaskEventCodes["PerseverativeError"]);

            perseverativeErrors_InTrial[pathProgressIndex + 1] += 1;
            CurrentTaskLevel.PerseverativeErrors_InBlock[pathProgressIndex + 1] += 1;
            CurrentTaskLevel.PerseverativeErrors_InTask++;
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

        Debug.Log($"TOUCHED COORD: {touchedCoord.chessCoord}, NEXT COORD: {CurrentTaskLevel.currMaze.mNextStep}, END TILE: {CurrentTaskLevel.currMaze.mFinish}");
        if (!startedMaze)
        {
            if (SessionValues.SessionDef.EventCodesActive)
                SessionValues.EventCodeManager.SendCodeImmediate(TaskEventCodes["RuleBreakingError"]);
            
            totalErrors_InTrial[0] += 1;
            CurrentTaskLevel.TotalErrors_InBlock[0] += 1;
            CurrentTaskLevel.TotalErrors_InTask++;

            ruleBreakingErrors_InTrial[0] += 1;
            CurrentTaskLevel.RuleBreakingErrors_InBlock[0] += 1;
            CurrentTaskLevel.RuleBreakingErrors_InTask++;

            consecutiveErrors++;

            tileFbDuration = tile.INCORRECT_RULEBREAKING_SECONDS;
            return 20;
        }
        
        if (touchedCoord.chessCoord == CurrentTaskLevel.currMaze.mNextStep)
        {

            // Provides feedback for last correct tile touch and updates next tile step
            if (pathProgress.Contains(touchedCoord))
            {
                if (SessionValues.SessionDef.EventCodesActive)
                    SessionValues.EventCodeManager.SendCodeImmediate(TaskEventCodes["LastCorrectSelection"]);

                CurrentTaskLevel.currMaze.mNextStep = CurrentTaskLevel.currMaze.mPath[CurrentTaskLevel.currMaze.mPath.FindIndex(pathCoord => pathCoord == touchedCoord.chessCoord) + 1];

                ReturnToLast = true;
            
                retouchCorrect_InTrial[pathProgressIndex + 1] += 1;
                CurrentTaskLevel.RetouchCorrect_InBlock[pathProgressIndex + 1] += 1;
                CurrentTaskLevel.RetouchCorrect_InTask++;
           
                consecutiveErrors = 0;
                tileFbDuration = tile.PREV_CORRECT_FEEDBACK_SECONDS;
                return 2;
            }

            if (!ReturnToLast)
            {
                SessionValues.EventCodeManager.SendCodeImmediate("CorrectResponse");

                correctTouches_InTrial++;
                CurrentTaskLevel.CorrectTouches_InBlock++;
                CurrentTaskLevel.CorrectTouches_InTask++;

                CorrectSelection = true;
            }
            // Helps set progress on the experimenter display
            pathProgress.Add(touchedCoord);
            pathProgressGO.Add(tile.gameObject);
            pathProgressIndex = CurrentTaskLevel.currMaze.mPath.FindIndex(pathCoord => pathCoord == touchedCoord.chessCoord);
            
            // Sets the NextStep if the maze isn't finished
            if (touchedCoord.chessCoord != CurrentTaskLevel.currMaze.mFinish)
            {
                CurrentTaskLevel.currMaze.mNextStep = CurrentTaskLevel.currMaze.mPath[CurrentTaskLevel.currMaze.mPath.FindIndex(pathCoord => pathCoord == touchedCoord.chessCoord) + 1];
                if (touchedCoord.chessCoord == CurrentTaskLevel.currMaze.mStart && consecutiveErrors != 0)
                {
                    // resetting consecutive errors if they made the error of not starting on start (can't give return to last correct yet)
                    consecutiveErrors = 0;
                }
            }
            else
            {
                finishedMaze = true; // Finished the Maze
            }
            
            //sets the duration of tile feedback
            tileFbDuration = tile.CORRECT_FEEDBACK_SECONDS;
            return 1;
            
        }
        // RULE-ABIDING ERROR ( and RULE ABIDING, BUT PERSEVERATIVE)
        if (touchedCoord.IsAdjacent(pathProgress[pathProgressIndex]) && !pathProgress.Contains(touchedCoord))
        {
            if (consecutiveErrors > 0)
            {
                if (SessionValues.SessionDef.EventCodesActive)
                    SessionValues.EventCodeManager.SendCodeImmediate(TaskEventCodes["RuleBreakingError"]);
                
                totalErrors_InTrial[pathProgressIndex + 1] += 1;
                CurrentTaskLevel.TotalErrors_InBlock[pathProgressIndex + 1] += 1;
                CurrentTaskLevel.TotalErrors_InTask++;

                ruleBreakingErrors_InTrial[pathProgressIndex + 1] += 1;
                CurrentTaskLevel.RuleBreakingErrors_InBlock[pathProgressIndex + 1] += 1;
                CurrentTaskLevel.RuleBreakingErrors_InTask++;
            
                consecutiveErrors++;
                return 20;
            }
            if (SessionValues.SessionDef.EventCodesActive)
                SessionValues.EventCodeManager.SendCodeImmediate(TaskEventCodes["RuleAbidingError"]);

            totalErrors_InTrial[pathProgressIndex + 1] += 1;
            CurrentTaskLevel.TotalErrors_InBlock[pathProgressIndex + 1] += 1;
            CurrentTaskLevel.TotalErrors_InTask++;

            ruleAbidingErrors_InTrial[pathProgressIndex + 1] += 1;
            CurrentTaskLevel.RuleAbidingErrors_InBlock[pathProgressIndex + 1] += 1;
            CurrentTaskLevel.RuleAbidingErrors_InTask++;
            
            consecutiveErrors++;
            
            // Set the correct next step to the last correct tile touch, only when this is the first time off path
            if (consecutiveErrors == 1)
            {
                CurrentTaskLevel.currMaze.mNextStep = CurrentTaskLevel.currMaze.mPath[CurrentTaskLevel.currMaze.mPath.FindIndex(pathCoord => pathCoord == CurrentTaskLevel.currMaze.mNextStep) - 1];
                if (CurrentTaskLevel.currMaze.mNextStep == CurrentTaskLevel.currMaze.mStart)
                    GameObject.Find(CurrentTaskLevel.currMaze.mNextStep).GetComponent<Renderer>().material.color = tile.START_COLOR;
                else
                    GameObject.Find(CurrentTaskLevel.currMaze.mNextStep).GetComponent<Renderer>().material.color = tile.DEFAULT_TILE_COLOR;
            }

            tileFbDuration = tile.INCORRECT_RULEABIDING_SECONDS;
            return 10;
        }

        // RULE BREAKING BACKTRACK ERROR OR ERRONEOUS RETOUCH OF LAST CORRECT TILE
        if (pathProgress.Contains(touchedCoord))
        {
            if (touchedCoord.Equals(pathProgress[pathProgress.Count - 1]))
            {
                if (SessionValues.SessionDef.EventCodesActive)
                    SessionValues.EventCodeManager.SendCodeImmediate(TaskEventCodes["LastCorrectSelection"]);

                ErroneousReturnToLast = true;
                retouchErroneous_InTrial[pathProgressIndex + 1] += 1;
                CurrentTaskLevel.RetouchErroneous_InBlock[pathProgressIndex + 1] += 1;
                CurrentTaskLevel.RetouchErroneous_InTask++;

                consecutiveErrors = 0;
                tileFbDuration = tile.PREV_CORRECT_FEEDBACK_SECONDS;
                return 2;
            }

            if (SessionValues.SessionDef.EventCodesActive)
                SessionValues.EventCodeManager.SendCodeImmediate(TaskEventCodes["RuleBreakingError"]);

            backtrackErrors_InTrial[pathProgressIndex + 1] += 1;
            CurrentTaskLevel.BacktrackErrors_InBlock[pathProgressIndex + 1] += 1;
            CurrentTaskLevel.BacktrackErrors_InTask++;

            ruleBreakingErrors_InTrial[pathProgressIndex + 1] += 1;
            CurrentTaskLevel.RuleBreakingErrors_InBlock[pathProgressIndex + 1] += 1;
            CurrentTaskLevel.RuleBreakingErrors_InTask++;
            
            totalErrors_InTrial[pathProgressIndex + 1] += 1;
            CurrentTaskLevel.TotalErrors_InBlock[pathProgressIndex + 1] += 1;
            CurrentTaskLevel.TotalErrors_InTask++;
            
            consecutiveErrors++;
            
            // Set the correct next step to the last correct tile touch
            if (consecutiveErrors == 1)
            {
                CurrentTaskLevel.currMaze.mNextStep = CurrentTaskLevel.currMaze.mPath[CurrentTaskLevel.currMaze.mPath.FindIndex(pathCoord => pathCoord == CurrentTaskLevel.currMaze.mNextStep) - 1];
                if(CurrentTaskLevel.currMaze.mNextStep == CurrentTaskLevel.currMaze.mStart)
                    GameObject.Find(CurrentTaskLevel.currMaze.mNextStep).GetComponent<Renderer>().material.color = tile.START_COLOR;
                else
                    GameObject.Find(CurrentTaskLevel.currMaze.mNextStep).GetComponent<Renderer>().material.color = tile.DEFAULT_TILE_COLOR;
            }


            tileFbDuration = tile.INCORRECT_RULEBREAKING_SECONDS;
            return 20;
        }

        // RULE BREAKING TOUCH
      
        if (SessionValues.SessionDef.EventCodesActive)
            SessionValues.EventCodeManager.SendCodeImmediate(TaskEventCodes["RuleBreakingError"]);
            
        totalErrors_InTrial[pathProgressIndex + 1] += 1;
        CurrentTaskLevel.TotalErrors_InBlock[pathProgressIndex + 1] += 1;
        CurrentTaskLevel.TotalErrors_InTask++;

        ruleBreakingErrors_InTrial[pathProgressIndex + 1] += 1;
        CurrentTaskLevel.RuleBreakingErrors_InBlock[pathProgressIndex + 1] += 1;
        CurrentTaskLevel.RuleBreakingErrors_InTask++;
           
        consecutiveErrors++;
            
        // Set the correct next step to the last correct tile touch
        if (consecutiveErrors == 1)
        {
            CurrentTaskLevel.currMaze.mNextStep = CurrentTaskLevel.currMaze.mPath[CurrentTaskLevel.currMaze.mPath.FindIndex(pathCoord => pathCoord == CurrentTaskLevel.currMaze.mNextStep) - 1];
            if (CurrentTaskLevel.currMaze.mNextStep == CurrentTaskLevel.currMaze.mStart)
                GameObject.Find(CurrentTaskLevel.currMaze.mNextStep).GetComponent<Renderer>().material.color = tile.START_COLOR;
            else
                GameObject.Find(CurrentTaskLevel.currMaze.mNextStep).GetComponent<Renderer>().material.color = tile.DEFAULT_TILE_COLOR;
        }

        tileFbDuration = tile.INCORRECT_RULEBREAKING_SECONDS;
        return 20;
    }
    
    private void LoadConfigVariables()
    {
        //config UI variables
        itiDuration = ConfigUiVariables.get<ConfigNumber>("itiDuration");
        sliderSize = ConfigUiVariables.get<ConfigNumber>("sliderSize");
        flashingFbDuration = ConfigUiVariables.get<ConfigNumber>("flashingFbDuration");
        mazeOnsetDelay = ConfigUiVariables.get<ConfigNumber>("mazeOnsetDelay");
        correctFbDuration = ConfigUiVariables.get<ConfigNumber>("correctFbDuration");
        previousCorrectFbDuration = ConfigUiVariables.get<ConfigNumber>("previousCorrectFbDuration");
        incorrectRuleAbidingFbDuration = ConfigUiVariables.get<ConfigNumber>("incorrectRuleAbidingFbDuration");
        incorrectRuleBreakingFbDuration = ConfigUiVariables.get<ConfigNumber>("incorrectRuleBreakingFbDuration");
        tileBlinkingDuration = ConfigUiVariables.get<ConfigNumber>("tileBlinkingDuration");
        maxMazeDuration = ConfigUiVariables.get<ConfigNumber>("maxMazeDuration");
        maxMazeDuration = ConfigUiVariables.get<ConfigNumber>("maxMazeDuration");
        minObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("minObjectTouchDuration");
        maxObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("maxObjectTouchDuration");

        SetGameConfigs();
        configVariablesLoaded = true;
    }

    private void SetGameConfigs()
    {

        // Default tile width - edit at the task level def
        //---------------------------------------------------------

        // TILE COLORS
        
        tile.NUM_BLINKS = currentTaskDef.NumBlinks;

        // Default - White
        tile.DEFAULT_TILE_COLOR = new Color(currentTaskDef.DefaultTileColor[0], currentTaskDef.DefaultTileColor[1], currentTaskDef.DefaultTileColor[2], 1);

        // Start - Light yellow
        tile.START_COLOR = new Color(currentTaskDef.StartColor[0], currentTaskDef.StartColor[1], currentTaskDef.StartColor[2], 1);

        // Finish - Light blue
        tile.FINISH_COLOR = new Color(currentTaskDef.FinishColor[0], currentTaskDef.FinishColor[1], currentTaskDef.FinishColor[2], 1);

        // Correct - Light green
        tile.CORRECT_COLOR = new Color(currentTaskDef.CorrectColor[0], currentTaskDef.CorrectColor[1], currentTaskDef.CorrectColor[2]);

        // Prev correct - Darker green
        tile.PREV_CORRECT_COLOR = new Color(currentTaskDef.LastCorrectColor[0], currentTaskDef.LastCorrectColor[1], currentTaskDef.LastCorrectColor[2]);

        // Incorrect rule-abiding - Orange
        tile.INCORRECT_RULEABIDING_COLOR = new Color(currentTaskDef.IncorrectRuleAbidingColor[0], currentTaskDef.IncorrectRuleAbidingColor[1],
            currentTaskDef.IncorrectRuleAbidingColor[2]);

        // Incorrect rule-breaking - Black
        tile.INCORRECT_RULEBREAKING_COLOR = new Color(currentTaskDef.IncorrectRuleBreakingColor[0], currentTaskDef.IncorrectRuleBreakingColor[1],
            currentTaskDef.IncorrectRuleBreakingColor[2]);
        
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
        TrialData.AddDatum("ContextName", () => CurrentTrialDef.ContextName);
        TrialData.AddDatum("MazeDefName", ()=> mazeDefName);
        TrialData.AddDatum("SelectedTiles", ()=> string.Join(",", SelectedTiles_InTrial));
        TrialData.AddDatum("TotalErrors", () => $"[{string.Join(", ", totalErrors_InTrial)}]");
        // TrialData.AddDatum("CorrectTouches", () => correctTouches_InTrial); DOESN'T GIVE ANYTHING USEFUL, JUST PATH LENGTH
        TrialData.AddDatum("RetouchCorrect", () => $"[{string.Join(", ", retouchCorrect_InTrial)}]");
        TrialData.AddDatum("RetouchErroneous", () => $"[{string.Join(", ", retouchErroneous_InTrial)}]");
        TrialData.AddDatum("PerseverativeErrors", () => $"[{string.Join(", ", perseverativeErrors_InTrial)}]");
        TrialData.AddDatum("BacktrackingErrors", () => $"[{string.Join(", ", backtrackErrors_InTrial)}]");
        TrialData.AddDatum("Rule-AbidingErrors", () => $"[{string.Join(", ", ruleAbidingErrors_InTrial)}]");
        TrialData.AddDatum("Rule-BreakingErrors", () => $"[{string.Join(", ", ruleBreakingErrors_InTrial)}]");
        TrialData.AddDatum("MazeDuration", ()=> mazeDuration);
        //TrialData.AddDatum("TotalClicks", ()=>MouseTracker.GetClickCount().Length);
    }

    private void DefineFrameData()
    {
        FrameData.AddDatum("ContextName", () => ContextName);
        FrameData.AddDatum("ChoiceMade", ()=> choiceMade);
        FrameData.AddDatum("SelectedObject", () => selectedGO.name);
        FrameData.AddDatum("StartedMaze", ()=> startedMaze);
    }
    private void DisableSceneElements()
    {
        DeactivateChildren(MazeContainer);
        if (GameObject.Find("SliderCanvas") != null)
            DeactivateChildren(GameObject.Find("SliderCanvas"));
    } 

    private void CreateTextOnExperimenterDisplay()
    {
        // sets parent for any playerView elements on experimenter display
        for (int i = 0; i < CurrentTaskLevel.currMaze.mPath.Count; i++)
        {
            foreach (StimDef sd in tiles.stimDefs)
            {
                Tile tileComponent = sd.StimGameObject.GetComponent<Tile>();
                Vector2 textSize = new Vector2(200, 200);
                
                if (tileComponent.mCoord.chessCoord == CurrentTaskLevel.currMaze.mPath[i])
                {
                    textLocation = ScreenToPlayerViewPosition(Camera.main.WorldToScreenPoint(tileComponent.transform.position), playerViewParent.transform);
                    playerViewText = playerView.CreateTextObject((i + 1).ToString(), (i + 1).ToString(),
                        Color.red, textLocation, textSize, playerViewParent.transform);
                    playerViewText.GetComponent<RectTransform>().localScale = new Vector3(2, 2, 0);
                    playerViewText.SetActive(true);
                }
            }
        }
    }

    public override void FinishTrialCleanup()
    {
        DisableSceneElements();

        if (!SessionValues.WebBuild)
            DestroyChildren(playerViewParent);

        /*if (mazeLoaded)
        {
            tiles.DestroyStimGroup();
            mazeLoaded = false;
        }*/
        
        if (TokenFBController.isActiveAndEnabled)
            TokenFBController.enabled = false;

        if(AbortCode == 0)
            CurrentTaskLevel.CalculateBlockSummaryString();
        else
        {
            CurrentTaskLevel.NumAbortedTrials_InBlock++;
            CurrentTaskLevel.NumAbortedTrials_InTask++;
        //    CurrentTaskLevel.ClearStrings();
        //    CurrentTaskLevel.CurrentBlockSummaryString.AppendLine("");
        }

        // Reset the maze so that the correct next step is the start
        CurrentTaskLevel.currMaze.mNextStep = CurrentTaskLevel.currMaze.mStart;
    }

    public override void ResetTrialVariables()
    {
        SliderFBController.ResetSliderBarFull();
        mazeDuration = 0;
        mazeStartTime = 0;
        mazeLoaded = false;
        choiceDuration = 0;
        choiceStartTime = 0;
        finishedMaze = false;
        startedMaze = false;
        selectedGO = null;
        choiceMade = false;
        CorrectSelection = false;
        ReturnToLast = false;
        ErroneousReturnToLast = false;
        configVariablesLoaded = false;
        SessionValues.MouseTracker.ResetClicks();
        
        correctTouches_InTrial = 0;
        if (TrialCount_InBlock != 0)
        {
            Array.Clear(perseverativeErrors_InTrial, 0, perseverativeErrors_InTrial.Length);
            Array.Clear(backtrackErrors_InTrial, 0, backtrackErrors_InTrial.Length);
            Array.Clear(ruleAbidingErrors_InTrial, 0, ruleAbidingErrors_InTrial.Length);
            Array.Clear(ruleBreakingErrors_InTrial, 0, ruleBreakingErrors_InTrial.Length);
            Array.Clear(totalErrors_InTrial, 0, totalErrors_InTrial.Length);
            Array.Clear(retouchCorrect_InTrial, 0, retouchCorrect_InTrial.Length);
            Array.Clear(retouchErroneous_InTrial, 0, retouchErroneous_InTrial.Length);
        }
        SelectedTiles_InTrial.Clear();
        pathProgress.Clear();
        pathProgressGO.Clear();
        pathProgressIndex = 0;
        consecutiveErrors = 0;
    }
    void SetTrialSummaryString()
    {
        TrialSummaryString = "<b>Maze Name: </b>" + mazeDefName +
                             "\n<b>Guided Selection: </b>" + CurrentTrialDef.GuidedMazeSelection +
                             "\n" + 
                             "\n<b>Percent Error: </b>" +  String.Format("{0:0.00}%", percentError*100) +
                             "\n<b>Total Errors: </b>" + totalErrors_InTrial.Sum() +
                             "\n" +
                             "\n<b>Rule-Abiding Errors: </b>" + ruleAbidingErrors_InTrial.Sum() +
                             "\n<b>Rule-Breaking Errors: </b>" + ruleBreakingErrors_InTrial.Sum() + 
                             "\n<b>Perseverative Errors: </b>" + perseverativeErrors_InTrial.Sum() +
                             "\n<b>Backtrack Errors: </b>" + backtrackErrors_InTrial.Sum() +
                             "\n<b>Retouch Correct: </b>" + retouchCorrect_InTrial.Sum()+ 
                             "\n<b>Retouch Erroneous: </b>" + retouchErroneous_InTrial.Sum()+ 
                             "\n" +
                             "\n<b>Choice Duration: </b>" + String.Format("{0:0.0}", choiceDuration) + 
                             "\n<b>Maze Duration: </b>" + String.Format("{0:0.0}", mazeDuration) +
                             "\n<b>Slider Value: </b>" + String.Format("{0:0.00}", SliderFBController.Slider.value);

    }

    private void AssignFlashingTiles()
    {

        if (!CurrentTrialDef.GuidedMazeSelection)
            return;

        for (int i = 0; i < CurrentTaskLevel.currMaze.mPath.Count; i++)
        {
            if (i % CurrentTrialDef.TileFlashingRatio == 0)
                tiles.stimDefs.Find(item => item.StimGameObject.name == CurrentTaskLevel.currMaze.mPath[i])
                    .StimGameObject.GetComponent<Tile>().assignedTileFlash = true;
        }
    }
}
