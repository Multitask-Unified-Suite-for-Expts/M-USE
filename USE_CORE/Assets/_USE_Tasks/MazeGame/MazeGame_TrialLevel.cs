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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ConfigDynamicUI;
using HiddenMaze;
using MazeGame_Namespace;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.Rendering;
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
    public GameObject MazeContainer;
    private float mazeLength;
    private float mazeHeight;
    private Vector2 mazeDims;

    // Tile objects
   // private Tile tile;
    public StimGroup tiles; // top of trial level with other variable definitions
    public Texture2D tileTex;
    public Texture2D mazeBgTex;

    public bool viewPath;
    public float tileFbDuration;
    private float tileScale;

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
    public Tile CirclePrefab;
    public Tile TileController;
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
    private PlayerViewPanel PlayerViewPanelController;
    private GameObject PlayerViewParent; // Helps set things onto the player view in the experimenter display
    public List<GameObject> playerViewTextList;
    public GameObject playerViewText;
    private Vector2 textLocation;

    // Touch Evaluation Variables
    private GameObject selectedGO;
    // private StimDef selectedSD;

    // Slider & Animation variables
    private float sliderValueChange;
    private float finishedFbDuration;

    public MazeManager mazeManager;

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
            FileLoadingDelegate = LoadTileAndBgTextures; //Set file loading delegate

            if (!Session.WebBuild) //player view variables
            {
                PlayerViewPanelController = gameObject.AddComponent<PlayerViewPanel>();
                PlayerViewParent = GameObject.Find("MainCameraCopy");
            }
        });


        SetupTrial.AddSpecificInitializationMethod(() =>
        {
            if (StartButton == null)
            {
                if (Session.SessionDef.IsHuman)
                {
                    StartButton = Session.HumanStartPanel.StartButtonGO;
                    Session.HumanStartPanel.SetVisibilityOnOffStates(InitTrial, InitTrial);
                }
                else
                {
                    StartButton = Session.USE_StartButton.CreateStartButton(MG_CanvasGO.GetComponent<Canvas>(), currentTaskDef.StartButtonPosition, currentTaskDef.StartButtonScale);
                    Session.USE_StartButton.SetVisibilityOnOffStates(InitTrial, InitTrial);
                }
            }

            CurrentTaskLevel.SetTaskSummaryString();
            Input.ResetInputAxes(); //reset input in case they still touching their selection from last trial!
        });
        SetupTrial.SpecifyTermination(() => true, InitTrial);
        var SelectionHandler = Session.SelectionTracker.SetupSelectionHandler("trial", "MouseButton0Click", Session.MouseTracker, InitTrial, ITI);
        TouchFBController.EnableTouchFeedback(SelectionHandler, currentTaskDef.TouchFeedbackDuration, currentTaskDef.StartButtonScale*15, MG_CanvasGO, false);

        InitTrial.AddSpecificInitializationMethod(() =>
        {
            InitializeSelectionHandler(SelectionHandler);
        });

        InitTrial.SpecifyTermination(() => SelectionHandler.LastSuccessfulSelectionMatchesStartButton(), Delay, () =>
        {
            Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["MazeOn"]);

            if (CurrentTrialDef.GuidedMazeSelection)
                StateAfterDelay = TileFlashFeedback;
            else
                StateAfterDelay = ChooseTile;
            
            DelayDuration = mazeOnsetDelay.value;
            
            SliderFBController.ConfigureSlider(sliderSize.value);
            SliderFBController.SliderGO.SetActive(true);

            if (!Session.WebBuild)
                CreateTextOnExperimenterDisplay();


            CurrentTaskLevel.CalculateBlockSummaryString();
            SetTrialSummaryString();
        });

        ChooseTile.AddSpecificInitializationMethod(() =>
        {
            //TouchFBController.SetPrefabSizes(tileScale);
            if (!tiles.IsActive)
            {
                MazeContainer.SetActive(true);
                tiles.ToggleVisibility(true);
                ActivateChildren(MazeContainer);
            }
            mazeManager.choiceStartTime = Time.unscaledTime;
            if(mazeManager.mazeStartTime == 0)
                mazeManager.mazeStartTime = Time.unscaledTime;

            SelectionHandler.HandlerActive = true;
            if (SelectionHandler.AllSelections.Count > 0)
                SelectionHandler.ClearSelections();
        });
        ChooseTile.AddUpdateMethod(() =>
        {
            SetTrialSummaryString(); // called every frame to update duration info

            if (SelectionHandler.SuccessfulSelections.Count > 0)
            { 
                if (SelectionHandler.LastSuccessfulSelection.SelectedGameObject.GetComponent<Tile>() != null)
                {
                    choiceMade = true;
                    AddChoiceDurationToDataTrackers();
                    selectedGO = SelectionHandler.LastSuccessfulSelection.SelectedGameObject;
                    mazeManager.selectedTilesGO.Add(selectedGO);
                    SelectionHandler.ClearSelections();
                }
            }
        });
        ChooseTile.SpecifyTermination(() =>  choiceMade, SelectionFeedback, () =>
        {
            SelectionHandler.HandlerActive = false;

            if (selectedGO.GetComponent<Tile>().isStartTile)
            {
                //If the tile that is selected is the start tile
                mazeManager.startedMaze = true;
                if (Session.SessionDef.EventCodesActive)
                    Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["MazeStart"]); 
            }

            if (selectedGO.GetComponent<Tile>().isFinishTile && mazeManager.currentMaze.mNextStep == mazeManager.currentMaze.mFinish)
            {
                //if the tile that is selected is the end tile, stop the timer
                AddMazeDurationToDataTrackers();
                if (Session.SessionDef.EventCodesActive)
                    Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["MazeFinish"]);
            }
        });
        ChooseTile.SpecifyTermination(()=> (mazeManager.mazeDuration > CurrentTrialDef.MaxMazeDuration) || (mazeManager.choiceDuration > CurrentTrialDef.MaxChoiceDuration), ()=> FinishTrial, () =>
        {
            // Timeout Termination
            Session.EventCodeManager.AddToFrameEventCodeBuffer("NoChoice");
            Session.EventCodeManager.SendRangeCode("CustomAbortTrial", AbortCodeDict["NoSelectionMade"]);
            AbortCode = 6;

            CurrentTaskLevel.MazeDurations_InBlock.Add(null);
            CurrentTaskLevel.MazeDurations_InTask.Add(null);

            CurrentTaskLevel.ChoiceDurations_InBlock.Add(null);
            CurrentTaskLevel.ChoiceDurations_InTask.Add(null);

            runningPercentError.Add(null);
        }); 
       
        SelectionFeedback.AddSpecificInitializationMethod(() =>
        {
            if (Session.SessionDef.EventCodesActive)
                Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["TileFbOn"]);
            choiceMade = false;
            // This is what actually determines the result of the tile choice
            selectedGO.GetComponent<Tile>().SelectionFeedback();

            Debug.LogWarning("===AFTER===");
            Debug.LogWarning("PATH PROGRESS IDX: " + mazeManager.currentPathIndex + " || MNEXT STEP : " + mazeManager.currentMaze.mNextStep);
            Debug.LogWarning(" CURRENT TIL POS: " + (mazeManager.currentTilePositionGO == null ? "N/A" : mazeManager.currentTilePositionGO.name));
            Debug.LogWarning("Selected Tiles in Path: " + string.Join(", ", mazeManager.selectedTilesInPathGO.Select(go => go.name)));
            Debug.LogWarning("RETURN TO LAST: " + mazeManager.returnToLast + " || ERRONEOUS RETURN TO LAST: " + mazeManager.erroneousReturnToLast);
            Debug.LogWarning("CORRECT SELECTION: " + mazeManager.correctSelection);
            percentError = (float)decimal.Divide(totalErrors_InTrial.Sum(),mazeManager.currentMaze.mNumSquares);

            finishedFbDuration = (tileFbDuration + flashingFbDuration.value);
            SliderFBController.SetUpdateDuration(tileFbDuration);
            SliderFBController.SetFlashingDuration(finishedFbDuration);
            
            if (mazeManager.returnToLast)
            {
                AudioFBController.Play("Positive");
                if (CurrentTrialDef.ErrorPenalty)
                    SliderFBController.UpdateSliderValue(selectedGO.GetComponent<Tile>().sliderValueChange);
            }
            else if (mazeManager.erroneousReturnToLast)
            {
                AudioFBController.Play("Negative");
            }
            else if (mazeManager.correctSelection)
            {
                SliderFBController.UpdateSliderValue(selectedGO.GetComponent<Tile>().sliderValueChange);
                if(!Session.WebBuild)
                    PlayerViewParent.transform.Find((mazeManager.currentPathIndex+1).ToString()).GetComponent<Text>().color = new Color(0, 0.392f, 0);
            }
            else if (selectedGO != null && mazeManager.erroneousReturnToLast)
            {
                
                AudioFBController.Play("Negative");
                if (CurrentTrialDef.ErrorPenalty && mazeManager.consecutiveErrors == 1)
                    SliderFBController.UpdateSliderValue(-selectedGO.GetComponent<Tile>().sliderValueChange);
            }
               
            selectedGO = null; //Reset selectedGO before the next touch evaluation
            mazeManager.ResetSelectionClassifications();
        });
        SelectionFeedback.AddUpdateMethod(() =>
        {
            SetTrialSummaryString(); // called every frame to update duration info
        });
        
        SelectionFeedback.AddTimer(() => mazeManager.finishedMaze? finishedFbDuration:tileFbDuration, Delay, () =>
        {
            if (currentTaskDef.UsingFixedRatioReward)
                HandleFixedRatioReward();
            
            if (mazeManager.finishedMaze) 
            {
                StateAfterDelay = ITI;
                DelayDuration = 0;

                HandleMazeCompletion();
            }
            else if (CheckTileFlash() || (CurrentTrialDef.GuidedMazeSelection && GameObject.Find(mazeManager.currentMaze.mNextStep).GetComponent<Tile>().assignedTileFlash))
                StateAfterDelay = TileFlashFeedback;
            else
                StateAfterDelay = ChooseTile; // could be incorrect or correct but it will still go back

            if (Session.SessionDef.EventCodesActive)
                Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["TileFbOff"]);
            
            
            SetTrialSummaryString(); //Set the Trial Summary String to reflect the results of choice
            CurrentTaskLevel.CalculateBlockSummaryString();
            CurrentTaskLevel.SetTaskSummaryString();
        });
        TileFlashFeedback.AddSpecificInitializationMethod(() =>
        {
            if (Session.SessionDef.EventCodesActive)
                Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["FlashingTileFbOn"]);
            if (!tiles.IsActive)
                tiles.ToggleVisibility(true);
            //MazeBackground.SetActive(true);
            TileController.NextCorrectFlashingFeedback();
        });
        TileFlashFeedback.AddTimer(() => tileBlinkingDuration.value, ChooseTile, () =>
        {
            if (Session.SessionDef.EventCodesActive)
                Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["FlashingTileFbOff"]);
        });
        ITI.AddSpecificInitializationMethod(() =>
        {

            DisableSceneElements();
            if (!Session.WebBuild)
                DestroyChildren(PlayerViewParent);

            Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["MazeOff"]);

            if (mazeManager.finishedMaze)
                Session.EventCodeManager.AddToFrameEventCodeBuffer("SliderFbController_SliderCompleteFbOff");
            
            if (currentTaskDef.NeutralITI)
            {
                ContextName = "NeutralITI";
                CurrentTaskLevel.SetSkyBox(GetContextNestedFilePath(!string.IsNullOrEmpty(currentTaskDef.ContextExternalFilePath) ? currentTaskDef.ContextExternalFilePath : Session.SessionDef.ContextExternalFilePath, "NeutralITI"));
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
            if (mazeManager.currentMaze.mPath.Contains(stim.StimGameObject.name))
                Session.TargetObjects.Add(stim.StimGameObject);
        }
    }

    private IEnumerator LoadTileAndBgTextures()
    {
        if (MazeBackground != null || MazeContainer != null) //since its gonna be called every trial, only want it to load them the first time. 
        {
            TrialFilesLoaded = true; //Setting this to true triggers the LoadTrialTextures state to end
            yield break; 
        }

        string contextPath = !string.IsNullOrEmpty(currentTaskDef.ContextExternalFilePath) ? currentTaskDef.ContextExternalFilePath : Session.SessionDef.ContextExternalFilePath;

        if (Session.UsingServerConfigs)
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
        else if (Session.UsingDefaultConfigs)
        {
            tileTex = Resources.Load<Texture2D>($"{Session.DefaultContextFolderPath}/{currentTaskDef.TileTexture}");
            mazeBgTex = Resources.Load<Texture2D>($"{Session.DefaultContextFolderPath}/{currentTaskDef.MazeBackgroundTexture}");
        }
        else if (Session.UsingLocalConfigs)
        {
            tileTex = LoadExternalPNG(GetContextNestedFilePath(contextPath, currentTaskDef.TileTexture));
            mazeBgTex = LoadExternalPNG(GetContextNestedFilePath(contextPath, currentTaskDef.MazeBackgroundTexture));
        }

        


        if (MazeBackground == null)
            MazeBackground = CreateSquare("MazeBackground", mazeBgTex, currentTaskDef.MazePosition, new Vector3(5, 5, 5));

        TrialFilesLoaded = true; //Setting this to true triggers the LoadTrialTextures state to end
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
        var creatingSquareMaze = false;

        
        List<GameObject> TileGOs = new List<GameObject>();
        
        if (creatingSquareMaze) { 
/*        {
            if (TileController == null)
            {
                TileController = Instantiate(TilePrefab);
                TileController.name = "TileController";
                TileController.mgTL = this;
            }
            else
            {
                TileController.GetComponent<MeshRenderer>().enabled = true;
                TileController.GetComponent<BoxCollider>().enabled = true;
            }

            LoadConfigVariables();
            SetGameConfigs(TileController);

            This will Load all tiles within the maze and the background of the maze
        mazeDims = mazeManager.currentMaze.mDims;
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
                    Instantiate the tile
                   Tile tile = Instantiate(TileController, MazeContainer.transform);

                    StimDef tileStimDef = new StimDef(tiles, tile.gameObject);

                    tile.transform.localScale = new Vector3(currentTaskDef.TileSize, currentTaskDef.TileSize, 0.15f);
                    tileStimDef.StimGameObject.SetActive(false);
                    tileStimDef.StimGameObject.GetComponent<Tile>().enabled = true;
                    tileStimDef.StimGameObject.GetComponent<MeshRenderer>().sharedMaterial.mainTexture = tileTex;
                    var displaceX = (2 * (x - 1) + 1) * (currentTaskDef.TileSize / 2) + currentTaskDef.SpaceBetweenTiles * (x - 1);
                    var displaceY = (2 * (y - 1) + 1) * (currentTaskDef.TileSize / 2) + currentTaskDef.SpaceBetweenTiles * (y - 1);
                    var newTilePosition = bottomLeftMazePos + new Vector3(displaceX, displaceY, 0);
                    tile.transform.position = newTilePosition;

                    Assigns ChessCoordName to the tile
                    string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                    string chessCoordName = $"{alphabet[x - 1]}{y}";
                    tileStimDef.StimGameObject.GetComponent<Tile>().mCoord = new Coords(chessCoordName);
                    tileStimDef.StimGameObject.name = chessCoordName;
                    Assigns Reward magnitude for each tile (set to proportional to the number of squares in path)

                   tileStimDef.StimGameObject.GetComponent<Tile>().sliderValueChange = 1f / mazeManager.currentMaze.mNumSquares; //FIX THE REWARD MAG BELOW USING STIM DEF ???

            if (chessCoordName == mazeManager.currentMaze.mStart)
                        {
                            tileStimDef.StimGameObject.GetComponent<Tile>().setColor(tile.START_COLOR);
                            startTile = tileStimDef.StimGameObject; // Have to define to perform feedback if they haven't selected the start yet 
                            Consider making a separate group for the tiles in the path, this might not improve function that much ?
            }

                        else if (chessCoordName == mazeManager.currentMaze.mFinish)
                        {
                            tileStimDef.StimGameObject.GetComponent<Tile>().setColor(tile.FINISH_COLOR);
                            finishTile = tileStimDef.StimGameObject;
                            tileStimDef.StimGameObject.GetComponent<Tile>().sliderValueChange = (float)tile.GetComponent<Tile>().sliderValueChange; // to ensure it fills all the way up
                        }
                        else if (!CurrentTrialDef.DarkenNonPathTiles || mazeManager.currentMaze.mPath.Contains((chessCoordName)))
                            tileStimDef.StimGameObject.GetComponent<Tile>().setColor(tile.DEFAULT_TILE_COLOR);
                        else
                            tileStimDef.StimGameObject.GetComponent<Tile>().setColor(new Color(0.5f, 0.5f, 0.5f));
                }
            mazeManager.mazeLoaded = true;
            Make sure to reset the maze to start at the start tile
        mazeManager.currentMaze.mNextStep = mazeManager.currentMaze.mStart;

            AssignFlashingTiles();
            TrialStims.Add(tiles);
            TileController.GetComponent<MeshRenderer>().enabled = false;
            TileController.GetComponent<BoxCollider>().enabled = false;*/
        }
        else
        {
            tiles = new StimGroup("Tiles");
            if (TileController == null)
            {
                TileController = Instantiate(CirclePrefab);
                TileController.name = "TileController";
                TileController.mgTL = this;
            }
            else
            {
                TileController.GetComponent<MeshRenderer>().enabled = true;
                TileController.GetComponent<BoxCollider>().enabled = true;
            }

            LoadConfigVariables();
            SetGameConfigs(TileController);



            List<int> orientation = mazeManager.currentMaze.customDims;
            float xOffset = 350;
            float yOffset = 200;

            for (int row = 0; row < orientation.Count; row++) // Three rows in the hexagon
            {
                int numCircles = orientation[row]; // Adjust number of circles per row
                
                for (int col = 0; col < numCircles; col++) // Vary number of circles based on row
                {
                    float x, y;
                    if (numCircles % 2 == 0) // for rows with even number of circles 
                    {
                        x = (-(xOffset*0.5f) - (((numCircles / 2) - 1))*xOffset) + (xOffset * col);
                    }
                    else
                    {
                        x = (-((numCircles/2f)-0.5f)*xOffset) + (xOffset * col);
                    }

                    //float x = col * xOffset + (row % 2 == 1 && col == 1 ? xOffset / 2f : 0f); // Shift middle circle in odd-numbered rows
                    y = (-yOffset * (orientation.Count/2)) + (row * yOffset);

                    
                    GameObject tileGO = new GameObject(GetChessCoordName(row, col));
                    tileGO.SetActive(false);
                    tileGO.AddComponent<SortingGroup>();
                    tileGO.GetComponent<SortingGroup>().sortingOrder = 0;
                    tileGO.transform.parent =  MazeContainer.transform;
                    tileGO.transform.localScale = new Vector3(1.75f, 1.75f, 0.1f);
                    MeshRenderer renderer = tileGO.AddComponent<MeshRenderer>();
                    Tile tile = tileGO.AddComponent<Tile>(); 
                    Image maskImage = tileGO.AddComponent<Image>();
                    maskImage.sprite = Resources.Load<Sprite>("Star");
                    maskImage.color = Color.white;
                    
                    Vector2 tileLocation = new Vector2(x, y);
                    tile.GetComponent<Tile>().mgTL = this;
                    tile.GetComponent<Tile>().mCoord = new Coords(tile.name);

                    tile.transform.localPosition = tileLocation;
                    StimDef tileStimDef = new StimDef(tiles, tile.gameObject);

                    if (tile.name == mazeManager.currentMaze.mStart)
                    {
                        tileStimDef.StimGameObject.GetComponent<Tile>().setColor(tile.START_COLOR);
                        tile.isStartTile = true; 
                    }
                
                    else if (tile.name == mazeManager.currentMaze.mFinish)
                    {
                        tileStimDef.StimGameObject.GetComponent<Tile>().setColor(tile.FINISH_COLOR);
                        tile.isFinishTile = true;
                    }
                    else if (!CurrentTrialDef.DarkenNonPathTiles || mazeManager.currentMaze.mPath.Contains((tile.name)))
                        tileStimDef.StimGameObject.GetComponent<Tile>().setColor(tile.DEFAULT_TILE_COLOR);
                    else
                        tileStimDef.StimGameObject.GetComponent<Tile>().setColor(new Color(0.5f, 0.5f, 0.5f)); 
                    
                    tileStimDef.StimGameObject.GetComponent<Tile>().sliderValueChange = 1f / mazeManager.currentMaze.mNumSquares; //FIX THE REWARD MAG BELOW USING STIM DEF ???

                    tileStimDef.StimGameObject.SetActive(false);

                    TileGOs.Add(tileStimDef.StimGameObject);
                }
            }

            

            TrialStims.Add(tiles);
            TileController.GetComponent<MeshRenderer>().enabled = false;
            TileController.GetComponent<BoxCollider>().enabled = false;


            if (mazeManager.tileConnectorsLoaded)
                return;
            // Define a temporary list to store non-current tiles
            List<GameObject> remainingTiles = new List<GameObject>(TileGOs);


            foreach (GameObject tile in TileGOs)
            {
                // Remove the current tile from the temporary list to avoid self-comparison
                remainingTiles.Remove(tile);

                Vector2 tilePos = tile.transform.localPosition;

                // Find adjacent tiles within the radius of the current tile
                foreach (GameObject otherTile in remainingTiles)
                {
                    Vector2 otherTilePos = otherTile.transform.localPosition;
                    if (Vector2.Distance(tilePos, otherTilePos) <= xOffset)
                    {
                        tile.GetComponent<Tile>().AdjacentTiles.Add(otherTile);

                        USE_Line line;
                        if (MazeContainer.transform.Find($"{otherTile.name}{tile.name}") == null)
                        {
                            line = new USE_Line(MG_CanvasGO.GetComponent<Canvas>(), tilePos, otherTilePos, Color.black, $"{tile.name}{otherTile.name}");
                            line.LineGO.transform.SetParent(MazeContainer.transform);

                            // Set the new game object as the first sibling
                            line.LineGO.transform.SetAsFirstSibling();

                            line.LineGO.SetActive(false);


                        }
                    }
                }

                // Add the current tile back to the temporary list for the next iteration
                remainingTiles.Add(tile);


            }
            mazeManager.tileConnectorsLoaded = true;
        }
    }

    private void InitializeSelectionHandler(SelectionTracking.SelectionTracker.SelectionHandler selectionHandler)
    {
        selectionHandler.HandlerActive = true;
        if (selectionHandler.AllSelections.Count > 0)
            selectionHandler.ClearSelections();
        selectionHandler.MinDuration = minObjectTouchDuration.value;
        selectionHandler.MaxDuration = maxObjectTouchDuration.value;
    }
    string GetChessCoordName(int row, int col)
    {
        string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        return $"{alphabet[col]}{row+1}";
    }
    public bool CheckTileFlash()
    {
        if (mazeManager.consecutiveErrors >= 2)
        {
            // Should provide flashing feedback of the last correct tile
            Debug.Log("*Perseverative Error*");

            if (Session.SessionDef.EventCodesActive)
                Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["PerseverativeError"]);

            perseverativeErrors_InTrial[mazeManager.currentPathIndex + 1] += 1;
            CurrentTaskLevel.PerseverativeErrors_InBlock[mazeManager.currentPathIndex + 1] += 1;
            CurrentTaskLevel.PerseverativeErrors_InTask++;
            return true;
        }
        return false;
    }

    private void HandleFixedRatioReward()
    {
        if (mazeManager.correctSelection && correctTouches_InTrial % CurrentTrialDef.RewardRatio == 0 )
        {
            if (Session.SyncBoxController != null)
            {
                Session.SyncBoxController.SendRewardPulses(1, CurrentTrialDef.PulseSize);
                CurrentTaskLevel.NumRewardPulses_InBlock++;;
                CurrentTaskLevel.NumRewardPulses_InTask++;
            }
        }
    }

    private void HandleMazeCompletion()
    {
        percentError = (float)decimal.Divide(totalErrors_InTrial.Sum(),mazeManager.currentMaze.mNumSquares);
        runningPercentError.Add(percentError);
        CurrentTaskLevel.NumSliderBarFull_InBlock++;
        CurrentTaskLevel.NumSliderBarFull_InTask++;
        Session.EventCodeManager.AddToFrameEventCodeBuffer("SliderFbController_SliderCompleteFbOn");

        if (Session.SyncBoxController != null)
        {
            Session.SyncBoxController.SendRewardPulses(CurrentTrialDef.NumPulses, CurrentTrialDef.PulseSize);
            // SessionInfoPanel.UpdateSessionSummaryValues(("totalRewardPulses",CurrentTrialDef.NumPulses)); moved to syncbox class
            CurrentTaskLevel.NumRewardPulses_InBlock += CurrentTrialDef.NumPulses;
            CurrentTaskLevel.NumRewardPulses_InTask += CurrentTrialDef.NumPulses;
        }
    }

    private void AddChoiceDurationToDataTrackers()
    {
        choiceDurationsList.Add(mazeManager.choiceDuration);
        CurrentTaskLevel.ChoiceDurations_InBlock.Add(mazeManager.choiceDuration);
        CurrentTaskLevel.ChoiceDurations_InTask.Add(mazeManager.choiceDuration);

        mazeManager.choiceDuration = 0;
    }

    private void AddMazeDurationToDataTrackers()
    {
        mazeManager.mazeStartTime = 0;
        CurrentTaskLevel.MazeDurations_InBlock.Add(mazeManager.mazeDuration);
        CurrentTaskLevel.MazeDurations_InTask.Add(mazeManager.mazeDuration);
    }
    public void InitializeTrialArrays()
    {
        ruleAbidingErrors_InTrial = new int[mazeManager.currentMaze.mNumSquares];
        ruleBreakingErrors_InTrial = new int[mazeManager.currentMaze.mNumSquares];
        backtrackErrors_InTrial = new int[mazeManager.currentMaze.mNumSquares];
        perseverativeErrors_InTrial = new int[mazeManager.currentMaze.mNumSquares];
        totalErrors_InTrial = new int[mazeManager.currentMaze.mNumSquares];
        retouchErroneous_InTrial = new int[mazeManager.currentMaze.mNumSquares];
        retouchCorrect_InTrial = new int[mazeManager.currentMaze.mNumSquares];
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

        configVariablesLoaded = true;
    }

    private void SetGameConfigs(Tile tile)
    {

        // Default tile width - edit at the task level def
        //---------------------------------------------------------

        // TILE COLORS
        
        tile.NUM_BLINKS = currentTaskDef.NumBlinks;

        // Default - White
        tile.DEFAULT_TILE_COLOR = new Color(CurrentTrialDef.DefaultTileColor[0], CurrentTrialDef.DefaultTileColor[1], CurrentTrialDef.DefaultTileColor[2], 1);

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
        TrialData.AddDatum("SelectedTiles", ()=> string.Join(",", mazeManager.selectedTilesGO));
        TrialData.AddDatum("TotalErrors", () => $"[{string.Join(", ", totalErrors_InTrial)}]");
        // TrialData.AddDatum("CorrectTouches", () => correctTouches_InTrial); DOESN'T GIVE ANYTHING USEFUL, JUST PATH LENGTH
        TrialData.AddDatum("RetouchCorrect", () => $"[{string.Join(", ", retouchCorrect_InTrial)}]");
        TrialData.AddDatum("RetouchErroneous", () => $"[{string.Join(", ", retouchErroneous_InTrial)}]");
        TrialData.AddDatum("PerseverativeErrors", () => $"[{string.Join(", ", perseverativeErrors_InTrial)}]");
        TrialData.AddDatum("BacktrackingErrors", () => $"[{string.Join(", ", backtrackErrors_InTrial)}]");
        TrialData.AddDatum("Rule-AbidingErrors", () => $"[{string.Join(", ", ruleAbidingErrors_InTrial)}]");
        TrialData.AddDatum("Rule-BreakingErrors", () => $"[{string.Join(", ", ruleBreakingErrors_InTrial)}]");
        TrialData.AddDatum("MazeDuration", ()=> mazeManager.mazeDuration);
        //TrialData.AddDatum("TotalClicks", ()=>MouseTracker.GetClickCount().Length);
    }

    private void DefineFrameData()
    {
        FrameData.AddDatum("ContextName", () => ContextName);
        FrameData.AddDatum("ChoiceMade", ()=> choiceMade);
        FrameData.AddDatum("SelectedObject", () => selectedGO?.name);
        FrameData.AddDatum("StartedMaze", ()=> mazeManager.startedMaze);
    }
    private void DisableSceneElements()
    {
        DeactivateChildren(MazeContainer);
        if (GameObject.Find("SliderCanvas") != null)
            DeactivateChildren(GameObject.Find("SliderCanvas"));
    } 

    private void CreateTextOnExperimenterDisplay()
    {
        // sets parent for any PlayerViewPanelController elements on experimenter display
        for (int i = 0; i < mazeManager.currentMaze.mPath.Count; i++)
        {
            foreach (StimDef sd in tiles.stimDefs)
            {
                Tile tileComponent = sd.StimGameObject.GetComponent<Tile>();
                Vector2 textSize = new Vector2(200, 200);
                
                if (tileComponent.mCoord.chessCoord == mazeManager.currentMaze.mPath[i])
                {
                    textLocation = ScreenToPlayerViewPosition(Camera.main.WorldToScreenPoint(tileComponent.transform.position), PlayerViewParent.transform);
                    playerViewText = PlayerViewPanelController.CreateTextObject((i + 1).ToString(), (i + 1).ToString(),
                        Color.red, textLocation, textSize, PlayerViewParent.transform);
                    playerViewText.GetComponent<RectTransform>().localScale = new Vector3(2, 2, 0);
                    playerViewText.SetActive(true);
                }
            }
        }
    }

    public override void FinishTrialCleanup()
    {
        DisableSceneElements();

        DeactivateChildren(MG_CanvasGO);
        
        if (!Session.WebBuild)
            DestroyChildren(PlayerViewParent);

        
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
        mazeManager.currentMaze.mNextStep = mazeManager.currentMaze.mStart;
    }

    public override void ResetTrialVariables()
    {
        SliderFBController.ResetSliderBarFull();
        selectedGO = null;
        choiceMade = false;
        configVariablesLoaded = false;
        Session.MouseTracker.ResetClicks();
        mazeManager.ResetMazeVariables();
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
                             "\n<b>Choice Duration: </b>" + String.Format("{0:0.0}", mazeManager.choiceDuration) + 
                             "\n<b>Maze Duration: </b>" + String.Format("{0:0.0}", mazeManager.mazeDuration) +
                             "\n<b>Slider Value: </b>" + String.Format("{0:0.00}", SliderFBController.Slider.value);

    }

    private void AssignFlashingTiles()
    {

        if (!CurrentTrialDef.GuidedMazeSelection)
            return;

        for (int i = 0; i < mazeManager.currentMaze.mPath.Count; i++)
        {
            if (i % CurrentTrialDef.TileFlashingRatio == 0)
                tiles.stimDefs.Find(item => item.StimGameObject.name == mazeManager.currentMaze.mPath[i])
                    .StimGameObject.GetComponent<Tile>().assignedTileFlash = true;
        }
    }
    
    public void HandleRuleBreakingError(int currentPathIndex)
    {
        if (Session.SessionDef.EventCodesActive)
            Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["RuleBreakingError"]);
        
        ruleBreakingErrors_InTrial[currentPathIndex + 1] += 1;
        CurrentTaskLevel.RuleBreakingErrors_InBlock[currentPathIndex + 1] += 1;
        CurrentTaskLevel.RuleBreakingErrors_InTask++;

        totalErrors_InTrial[currentPathIndex + 1] += 1;
        CurrentTaskLevel.TotalErrors_InBlock[currentPathIndex + 1] += 1;
        CurrentTaskLevel.TotalErrors_InTask++;

        mazeManager.consecutiveErrors++;
        tileFbDuration = TileController.INCORRECT_RULEBREAKING_SECONDS;
    }

    public void HandleRuleAbidingError(int currentPathIndex)
    {
        if (Session.SessionDef.EventCodesActive)
            Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["RuleAbidingError"]);
        
        totalErrors_InTrial[currentPathIndex + 1] += 1;
        CurrentTaskLevel.TotalErrors_InBlock[currentPathIndex + 1] += 1;
        CurrentTaskLevel.TotalErrors_InTask++;

        ruleAbidingErrors_InTrial[currentPathIndex + 1] += 1;
        CurrentTaskLevel.RuleAbidingErrors_InBlock[currentPathIndex + 1] += 1;
        CurrentTaskLevel.RuleAbidingErrors_InTask++;

        mazeManager.consecutiveErrors++;
        tileFbDuration = TileController.INCORRECT_RULEABIDING_SECONDS;
    }
    
    public void HandleBackTrackError(int currentPathIndex)
    {
        backtrackErrors_InTrial[currentPathIndex + 1] += 1;
        CurrentTaskLevel.BacktrackErrors_InBlock[currentPathIndex + 1] += 1;
        CurrentTaskLevel.BacktrackErrors_InTask++;
    }
    public void HandleRetouchErroneous(int currentPathIndex)
    {
        if (Session.SessionDef.EventCodesActive)
            Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["LastCorrectSelection"]);
        
        mazeManager.erroneousReturnToLast = true;
        retouchErroneous_InTrial[currentPathIndex + 1] += 1;
        CurrentTaskLevel.RetouchErroneous_InBlock[currentPathIndex + 1] += 1;
        CurrentTaskLevel.RetouchErroneous_InTask++;

        mazeManager.consecutiveErrors = 0;
        tileFbDuration = TileController.PREV_CORRECT_FEEDBACK_SECONDS;
    }

    public void HandleRetouchCorrect(int currentPathIndex)
    {
        if (Session.SessionDef.EventCodesActive)
            Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["LastCorrectSelection"]);

        mazeManager.returnToLast = true;
            
        retouchCorrect_InTrial[currentPathIndex + 1] += 1;
        CurrentTaskLevel.RetouchCorrect_InBlock[currentPathIndex + 1] += 1;
        CurrentTaskLevel.RetouchCorrect_InTask++;
           
        mazeManager.consecutiveErrors = 0;
        tileFbDuration = TileController.PREV_CORRECT_FEEDBACK_SECONDS;
    }

    public void HandleCorrectTouch()
    {
        Session.EventCodeManager.AddToFrameEventCodeBuffer("CorrectResponse");

        correctTouches_InTrial++;
        CurrentTaskLevel.CorrectTouches_InBlock++;
        CurrentTaskLevel.CorrectTouches_InTask++;

        mazeManager.correctSelection = true;
        mazeManager.consecutiveErrors = 0;

        tileFbDuration = TileController.CORRECT_FEEDBACK_SECONDS;

    }
    
    
}
