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
    public GameObject MG_CanvasGO;
    public USE_StartButton USE_StartButton;

    private static bool end;

    // TILE FLASH VARIABLES
    public static bool TileFlash;

    private static float flashDuration;
    /*public float CORRECT_FEEDBACK_SECONDS, PREV_CORRECT_FEEDBACK_SECONDS, 
        INCORRECT_RULEABIDING_SECONDS, INCORRECT_RULEBREAKING_SECONDS, TIMEOUT_SECONDS;*/

    //MazeVis Variables
    // public TileRow[] tileRows;
    public static GameObject mazeListObj;
    // public LoadMazeList mazeListScript;

    // public static GameConfigs gameConfigs = new GameConfigs();
    public static Maze currMaze;
    private static int count;

    public static float fbDuration;

    // TRIAL DATA TRACKING VARIABLES
    private static int totalErrors_InTrial,
        ruleAbidingErrors_InTrial,
        ruleBreakingErrors_InTrial,
        retouchCorrect_InTrial,
        correctTouches_InTrial,
        backtrackErrors_InTrial,
        perseverativeErrors_InTrial;

    public static int consecutiveErrors; // only evaluates, not really useful to log
    private static readonly List<Coords> pathProgress = new List<Coords>();
    public static List<GameObject> pathProgressGO = new List<GameObject>();
    private static int pathProgressIndex = 0;
    public static bool viewPath;
    private static bool CorrectSelection, ReturnToLast;
    public List<Maze> mazeList = new List<Maze>();
    public int ind;

    //game configs variables
    [HideInInspector]public float SCREEN_WIDTH, TILE_WIDTH;

    // TASK LEVEL DEFINED COLOR VARIABLES
    [HideInInspector]public float[] startColor,
        finishColor,
        correctColor,
        lastCorrectColor,
        incorrectRuleAbidingColor,
        incorrectRuleBreakingColor,
        defaultTileColor;

    [HideInInspector]public int NumBlinks;
    [HideInInspector]public Tile TilePrefab;
    [HideInInspector]public float TileSize;
    [HideInInspector]public string TileTexture;
    private Texture2D tileTex;
    
    //private float spaceBetweenSquares;

    [HideInInspector] public ConfigNumber minObjectTouchDuration,
        itiDuration,
        finalFbDuration,
        maxObjectTouchDuration,
        selectObjectDuration,
        sliderSize;

    [FormerlySerializedAs("tileFlashDuration")] [HideInInspector]
    public ConfigNumber tileBlinkingDuration;

    [HideInInspector] public ConfigNumber spaceBetweenTiles,
        mazeOnsetDelay,
        correctFbDuration,
        previousCorrectFbDuration,
        incorrectRuleAbidingFbDuration,
        incorrectRuleBreakingFbDuration;

    //Block Data Variables
    //public int NumRewardPulses_InBlock, NonStimTouches_InBlock = 0;


    public string ContextExternalFilePath, MazeFilePath;
    public Vector3 ButtonPosition, ButtonScale;
    public GameObject MazeBackground;
    public string mazeDefName;
    public GameObject SliderPrefab, SliderHaloPrefab;
    public List<GameObject> playerViewTextList;
    public GameObject playerViewText;
    private bool choiceMade;
    private GameObject chosenStim;


    // Data Tracking Variables
    private string contextName = "";

    private Vector2 dim;

//update slider variables
    private float endupdatetime;
    private RaycastHit hit;
    private float incrementalVal;
    private bool isContextActive;

    // Slider Variables
    private bool isSliderValueIncrease;
    private int max, min;
    private GameObject MazeContainer;
    private float mazeLength, mazeHeight;
    private float mazeLengthDimensions, mazeHeightDimensions;
    private bool mazeLoaded = false;

    //private Button initButton;
    private Ray mouseRay;

    //Player View Variables
    private PlayerViewPanel playerView;
    private bool playerViewLoaded;
    private GameObject playerViewParent; // Helps set things onto the player view in the experimenter display
    private int response;

    // Touch Evaluation Variables
    private GameObject selectedGO;
    private MazeGame_StimDef selectedSD = null;
    private Slider Slider;
    private GameObject SliderGo, SliderHaloGo;
    private Image SliderHaloImage;
    private Vector3 SliderInitPosition;
    private float sliderValueChange;

    // Nonstim Scene Elements
    private GameObject StartButton;

    private float flashingStartTime;
    private static float mazeStartTime;
    private static float mazeEndTime;
    private Vector2 textLocation;
    private Tile tile = new Tile();
    private GameObject tileGO;
    private StimGroup tiles; // top of triallevel with other variable defs

    private int trialIndex;
    private float valueRemaining;
    private float valueToAdd;

    public bool ContextActive;

    private bool variablesLoaded;
    public MazeGame_TrialDef CurrentTrialDef => GetCurrentTrialDef<MazeGame_TrialDef>();
    public MazeGame_TaskLevel CurrentTaskLevel => GetTaskLevel<MazeGame_TaskLevel>();

    public override void DefineControlLevel()
    {
        //define States within this Control Level
        State InitTrial = new State("InitTrial");
        State ChooseTile = new State("ChooseTile");
        State SelectionFeedback = new State("SelectionFeedback");
        State TileFlashFeedback = new State("TileFlashFeedback");
        State FinalFeedback = new State("FinalFeedback");
        State ITI = new State("ITI");
        State delay = new State("Delay");
        AddActiveStates(new List<State>
            { InitTrial, ChooseTile, SelectionFeedback, TileFlashFeedback, FinalFeedback, ITI, delay });

        string[] stateNames =
            { "StartButton", "ChooseTile", "SelectionFeedback", "TileFlashFeedback", "FinalFeedback", "ITI", "Delay" };

        // A state that just waits for some time
        State stateAfterDelay = null;
        float delayDuration = 0;
        delay.AddTimer(() => delayDuration, () => stateAfterDelay);

        var mouseHandler = new SelectionHandler<MazeGame_StimDef>();
        
        // define initScreen state*/
        Add_ControlLevel_InitializationMethod(() =>
        {
            InitializeSlider();
            LoadTextures(ContextExternalFilePath);
            HaloFBController.SetHaloSize(5);
            StartButton = CreateSquare("StartButton", StartButtonTexture, ButtonPosition, ButtonScale);
            MazeContainer = new GameObject("MazeContainer");
            MazeBackground = CreateSquare("MazeBackground", MazeBackgroundTexture, new Vector3(0, 0, 0),
                new Vector3(5, 5, 5));
            tileTex = LoadPNG(ContextExternalFilePath + Path.DirectorySeparatorChar + TileTexture + ".png");

            //player view variables
            
            playerViewParent = GameObject.Find("MainCameraCopy");
        });
        SetupTrial.AddInitializationMethod(() =>
        {
            isContextActive = true;
            viewPath = CurrentTrialDef.ViewPath;
            contextName = CurrentTrialDef.ContextName;
            RenderSettings.skybox = CreateSkybox(ContextExternalFilePath + Path.DirectorySeparatorChar +
                                                 CurrentTrialDef.ContextName + ".png");
            if (!variablesLoaded)
                loadVariables();
            //read maze value for maze def
            var textMaze = File.ReadAllLines(MazeFilePath + Path.DirectorySeparatorChar + mazeDefName);
            currMaze = new Maze(textMaze[0]);
            ResetTrialTrackingVariables();
            AssignBlockVariables();
            Input.ResetInputAxes(); //reset input in case they still touching their selection from last trial!
        });
        SetupTrial.SpecifyTermination(() => true, InitTrial);
        MouseTracker.AddSelectionHandler(mouseHandler, InitTrial);
        InitTrial.AddInitializationMethod(() => { StartButton.SetActive(true); });
        //  StartButton.SpecifyTermination(() => mouseHandler.SelectionMatches(initButton), MazeVis);
        InitTrial.SpecifyTermination(() => mouseHandler.SelectionMatches(StartButton), delay, () =>
        {
            stateAfterDelay = ChooseTile;
            delayDuration = mazeOnsetDelay.value;
            SliderGo.SetActive(true);
            StartButton.SetActive(false);

            ConfigureSlider();
            SetTrialSummaryString();

            //NonStimTouches_InBlock += mouseHandler.GetNumNonStimSelection(); COUNT ALL TOUCHES BETTER OR CHANGE NAME
            InstantiateCurrMaze();
            if(TrialCount_InBlock==0) CreateTextOnExperimenterDisplay();
            if(!playerViewLoaded) ActivateChildren(playerViewParent);
//            EventCodeManager.SendCodeNextFrame(TaskEventCodes["SliderReset"]);
        });
        MouseTracker.AddSelectionHandler(mouseHandler, ChooseTile);
        ChooseTile.AddUpdateMethod(() =>
        {
            //SELECTION HANDLER ISN'T WORKING, GIVES THE MAZE CONTAINER AS .SELECTEDGAMEOBJECT & CHILDREN ARE ALL TILES
            //Input.ResetInputAxes(); //reset input in case they holding down
            if (InputBroker.GetMouseButtonDown(0))
            {
                var ray = Camera.main.ScreenPointToRay(InputBroker.mousePosition);
                if (Physics.Raycast(ray, out hit))
                {
                    selectedGO = hit.transform.gameObject;
                    if (selectedGO.GetComponent<Tile>() != null) choiceMade = true;
                }
            }
        });
        ChooseTile.SpecifyTermination(() => choiceMade, SelectionFeedback);
        SelectionFeedback.AddInitializationMethod(() =>
        {
            SetTrialSummaryString();
            choiceMade = false;
            selectedGO.GetComponent<Tile>().OnMouseDown();
            endupdatetime = Time.time + fbDuration;
            if (CorrectSelection)
            {
                isSliderValueIncrease = true;
                valueToAdd = sliderValueChange;
                playerViewParent.transform.Find((pathProgressIndex + 1).ToString()).GetComponent<Text>().color = new Color(0,0.392f,0);
                //ADD ANYTHING ELSE THAT OCCURS DURING CORRECT SELECTION FEEDBACK
                AudioFBController.Play("Positive");
            }
            else if (ReturnToLast)
            {
                valueToAdd = 0f;
                AudioFBController.Play("Positive");
            }
            else
            {
                valueToAdd = 0f;
                AudioFBController.Play("Negative");
            }

            incrementalVal = valueToAdd / (fbDuration * 60);
            valueRemaining = valueToAdd;
        });
        SelectionFeedback.AddUpdateMethod(() =>
        {
            if (valueRemaining > 0)
            {
                Slider.value += incrementalVal;
                valueRemaining -= incrementalVal;
            }
        });
        SelectionFeedback.AddTimer(() => fbDuration, delay, () =>
        {
            delayDuration = 0;
            valueRemaining = 0;
            SliderHaloGo.SetActive(false);
            CorrectSelection = false;
            ReturnToLast = false;
            if (end)
            {
                stateAfterDelay = FinalFeedback;
            }
            else if (CheckTileFlash())
            {
                stateAfterDelay = TileFlashFeedback;
            }
            else
            {
                stateAfterDelay = ChooseTile; // could be incorrect or correct but it will still go back
            }
        });
        TileFlashFeedback.AddInitializationMethod(() => { tile.StartCoroutine(tile.FlashingFeedback()); });
        TileFlashFeedback.AddTimer(() => tileBlinkingDuration.value, ChooseTile);
        FinalFeedback.AddInitializationMethod(() =>
        {
            Debug.Log("the end");
            SliderHaloGo.SetActive(true);
            SliderHaloImage.color = new Color(1, 1, 1, 0.2f);
            flashingStartTime = Time.time;
            if (SyncBoxController != null)
            {
                SyncBoxController.SendRewardPulses(CurrentTrialDef.NumPulses, CurrentTrialDef.PulseSize);
                CurrentTaskLevel.numRewardPulses_InBlock += CurrentTrialDef.NumPulses;
            }
        });

        FinalFeedback.AddUpdateMethod(() =>
        {
            if ((int)(10 * (Time.time - flashingStartTime)) % 4 == 0)
                SliderHaloImage.color = new Color(1, 1, 1, 0.2f);
            else if ((int)(10 * (Time.time - flashingStartTime)) % 2 == 0) SliderHaloImage.color = new Color(1, 1, 0, 0.2f);
        });
        FinalFeedback.AddTimer(() => finalFbDuration.value, ITI, () =>
        {
            DisableSceneElements();
            DestroyCurrMaze();
        });
        //Define iti state
        ITI.SpecifyTermination(() => true, FinishTrial);
        FinishTrial.AddInitializationMethod(() => { AssignBlockVariables(); });
        AssignTrialData();
    }

    private void InstantiateCurrMaze()
    {
        // This will Load all text 
        
        sliderValueChange = 100f / currMaze.mNumSquares / 100f;
        dim = currMaze.mDims;
        var mazeCenter = new Vector3(0, 0, 0);

        mazeLength = dim.x * TileSize + (dim.x - 1) * spaceBetweenTiles.value;
        mazeHeight = dim.y * TileSize + (dim.y - 1) * spaceBetweenTiles.value;
        MazeBackground.transform.SetParent(MazeContainer.transform); // setting it last so that it doesn't cover tiles
        MazeBackground.transform.localScale = new Vector3(mazeLength + 2 * spaceBetweenTiles.value,
            mazeHeight + 2 * spaceBetweenTiles.value, 0.1f);
        MazeBackground.SetActive(true);
        var bottomLeftMazePos = mazeCenter - new Vector3(mazeLength / 2, mazeHeight / 2, 0);

        tiles = new StimGroup("Tiles");

        for (var x = 1; x <= dim.x; x++)
        for (var y = 1; y <= dim.y; y++)
        {
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
            tile.mCoord = new Coords(x, y);

            if (x == currMaze.mStart.x && y == currMaze.mStart.y)
                tile.gameObject.GetComponent<Tile>().setColor(tile.START_COLOR);
            else if (x == currMaze.mFinish.x && y == currMaze.mFinish.y)
                tile.gameObject.GetComponent<Tile>().setColor(tile.FINISH_COLOR);
            else
                tile.gameObject.GetComponent<Tile>().setColor(tile.DEFAULT_TILE_COLOR);

            tiles.AddStims(tile.gameObject);
        }
    }

    private void DestroyCurrMaze()
    {
        tiles.DestroyStimGroup();
    }

    public static void setEnd(int i)
    {
        if (i == 0 || i == 1)
        {
            // slider.value += sliderValueIncreaseAmount;
        }

        if (i == 99)
        {
            //slider.value += sliderValueIncreaseAmount;
            ++count;
            end = true;
        }

        var progress = correctTouches_InTrial / (float)currMaze.mNumSquares;
        var ratio = correctTouches_InTrial / (float)totalErrors_InTrial;
        Debug.Log("Progress: " + progress);
        Debug.Log("Accuracy: " + ratio);
    }

    public bool CheckTileFlash()
    {
        if (consecutiveErrors >= 2)
        {
            // Should provide flashing feedback of the last correct tile
            Debug.Log("*Perseverative Error*");
            perseverativeErrors_InTrial++;
            return true;
        }

        return false;
    }


    public static int ManageTileTouch(Tile tile)
    {
        var touchedCoord = tile.mCoord;

        // ManageTileTouch - Returns correctness code
        // Return values:
        // 1 - correct tile touch
        // 2 - last correct retouch

        // 10 - rule-abiding incorrect

        // 20 - rule-breaking incorrect (failed to start on start tile, failed to return to last correct after error, diagonal/skips)

        // CORRECT TILE TOUCH (then narrow down if its is start, finish, or other)
        if (currMaze.mNextStep == currMaze.mStart && touchedCoord != currMaze.mStart)
        {
            Debug.Log("*Rule Breaking Error - Not Pressing the Start Tile to Begin the Maze*");
            totalErrors_InTrial++;
            ruleBreakingErrors_InTrial++;

            fbDuration = tile.INCORRECT_RULEBREAKING_SECONDS;
            return 20;
        }

        if ((touchedCoord == currMaze.mNextStep || touchedCoord.isAdjacentTo(currMaze.mPath[currMaze.mPath.FindIndex(
                pathCoord =>
                    pathCoord == currMaze.mNextStep) - 1])) && consecutiveErrors != 0)
        {
            Debug.Log(
                "*Rule-Breaking Error - Didn't return to previously correct tile after error, but the tile is in the hidden path*");
            totalErrors_InTrial++;
            ruleBreakingErrors_InTrial++;
            consecutiveErrors++;
            fbDuration = tile.INCORRECT_RULEBREAKING_SECONDS;
            return 20;
        }

        if (touchedCoord == currMaze.mNextStep && consecutiveErrors == 0)
        {
            Debug.Log("*Correct Tile Touch*");
            correctTouches_InTrial++;
            CorrectSelection = true;
            pathProgress.Add(touchedCoord);
            pathProgressGO.Add(tile.gameObject);
            pathProgressIndex = currMaze.mPath.FindIndex(pathCoord => pathCoord == touchedCoord);
            fbDuration = tile.CORRECT_FEEDBACK_SECONDS;

            
            // Sets the NextStep if the maze isn't finished
            if (touchedCoord != currMaze.mFinish)
            {
                currMaze.mNextStep =
                    currMaze.mPath[currMaze.mPath.FindIndex(pathCoord => pathCoord == touchedCoord) + 1];
                if (touchedCoord == currMaze.mStart)
                {
                    mazeStartTime = Time.time;
                }
            }
                
            else
            {
                mazeEndTime = Time.time - mazeStartTime;
                end = true; // Finished the Maze
            }

            return 1;
        }

        // LAST CORRECT TILE TOUCH - idk what kind of error feedback it gives?? just makes dark green tile
        if (currMaze.mPath[currMaze.mPath.FindIndex(pathCoord => pathCoord == currMaze.mNextStep) - 1] ==
            touchedCoord)
        {
            Debug.Log("*Last Correct Tile Touch*");
            ReturnToLast = true;
            fbDuration = tile.PREV_CORRECT_FEEDBACK_SECONDS;
            retouchCorrect_InTrial++;
            consecutiveErrors = 0;
            return 2;
        }

        // RULE ABIDING TOUCH 
        if (currMaze.mNextStep != currMaze.mStart && touchedCoord.isAdjacentTo(currMaze.mPath[
                currMaze.mPath.FindIndex(pathCoord =>
                    pathCoord == currMaze.mNextStep) - 1]) && !pathProgress.Contains(touchedCoord))
        {
            consecutiveErrors++;
            Debug.Log("*Rule-Abiding Incorrect Error*");
            totalErrors_InTrial++;
            ruleAbidingErrors_InTrial++;
            fbDuration = tile.INCORRECT_RULEABIDING_SECONDS;
            return 10;
        }

        // RULE BREAKING TOUCH
        Debug.Log("*Rule-Breaking Incorrect Error*");
        totalErrors_InTrial++;
        ruleBreakingErrors_InTrial++;
        consecutiveErrors++;
        fbDuration = tile.INCORRECT_RULEBREAKING_SECONDS;
        if (pathProgress.Contains(touchedCoord))
        {
            Debug.Log("*Rule-Breaking Backtrack Error*");
            backtrackErrors_InTrial++;
        }
        return 20;
    }

    private void InitializeSlider()
    {
        var sliderCanvas = GameObject.Find("SliderCanvas").transform;
        SliderGo = Instantiate(SliderPrefab, sliderCanvas);
        SliderHaloGo = Instantiate(SliderHaloPrefab, sliderCanvas);
        SliderGo.SetActive(false);
        SliderHaloGo.SetActive(false);
    }

    private void ConfigureSlider()
    {
        SliderHaloImage = SliderHaloGo.GetComponent<Image>();
        Slider = SliderGo.GetComponent<Slider>();
        //SliderGo.GetComponent<RectTransform>().
        SliderGo.transform.localPosition = new Vector3(0, 470, 0);
        SliderInitPosition = SliderGo.transform.position;
        //consider making slider stuff into USE level class
        Slider.value = 0;
        SliderHaloGo.transform.position = SliderInitPosition;
//        int numSliderSteps = CurrentTrialDef.SliderGain.Sum() + CurrentTrialDef.SliderInitial;
        Slider.transform.localScale = new Vector3(sliderSize.value / 10f, sliderSize.value / 10f, 1f);
        SliderHaloGo.transform.localScale = new Vector3(sliderSize.value / 10f, sliderSize.value / 10f, 1f);

        // if (CurrentTrialDef.SliderInitial != 0)  REIMPLEMENT LATER, HAVE TO DEFINE PATH BETTER WITH ACCESS 
        // {                                                TO ALL TILES IN PATH
        //     Slider.value += sliderValueIncreaseAmount * (CurrentTrialDef.SliderInitial);
        // }
        // isSliderValueIncrease = false;
    }

    private void loadVariables()
    {
        //config UI variables
        //minObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("minObjectTouchDuration");
        //maxObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("maxObjectTouchDuration");
        itiDuration = ConfigUiVariables.get<ConfigNumber>("itiDuration");
        sliderSize = ConfigUiVariables.get<ConfigNumber>("sliderSize");
        spaceBetweenTiles = ConfigUiVariables.get<ConfigNumber>("spaceBetweenTiles");
        finalFbDuration = ConfigUiVariables.get<ConfigNumber>("finalFbDuration");
        selectObjectDuration = ConfigUiVariables.get<ConfigNumber>("selectObjectDuration");
        mazeOnsetDelay = ConfigUiVariables.get<ConfigNumber>("mazeOnsetDelay");
        correctFbDuration = ConfigUiVariables.get<ConfigNumber>("correctFbDuration");
        previousCorrectFbDuration = ConfigUiVariables.get<ConfigNumber>("previousCorrectFbDuration");
        incorrectRuleAbidingFbDuration = ConfigUiVariables.get<ConfigNumber>("incorrectRuleAbidingFbDuration");
        incorrectRuleBreakingFbDuration = ConfigUiVariables.get<ConfigNumber>("incorrectRuleBreakingFbDuration");
        tileBlinkingDuration = ConfigUiVariables.get<ConfigNumber>("tileBlinkingDuration");
        variablesLoaded = true;
        //disableVariables();
    }

    private void SetGameConfigs()
    {
// MAZE GAME WIDTHS
        ///*
        // TODO: Not implemented, but this should be the maximum screen width that tiles can take up without overfilling the screen
        SCREEN_WIDTH = 4;

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
    }

    private void AssignTrialData()
    {
        TrialData.AddDatum("MazeDefName", ()=> mazeDefName);
        TrialData.AddDatum("TotalErrors", () => totalErrors_InTrial);
       // TrialData.AddDatum("CorrectTouches", () => correctTouches_InTrial); DOESN'T GIVE ANYTHING USEFUL, JUST PATH LENGTH
        TrialData.AddDatum("RetouchCorrect", () => retouchCorrect_InTrial);
        TrialData.AddDatum("PerseverativeErrors", () => perseverativeErrors_InTrial);
        TrialData.AddDatum("BacktrackingErrors", () => backtrackErrors_InTrial);
        TrialData.AddDatum("Rule-AbidingErrors", () => ruleAbidingErrors_InTrial);
        TrialData.AddDatum("Rule-BreakingErrors", () => ruleBreakingErrors_InTrial);
        TrialData.AddDatum("MazeDuration", ()=> mazeEndTime);
    }
    private void ResetTrialTrackingVariables()
    {
        totalErrors_InTrial = 0;
        correctTouches_InTrial = 0;
        retouchCorrect_InTrial = 0;
        perseverativeErrors_InTrial = 0;
        backtrackErrors_InTrial = 0;
        ruleAbidingErrors_InTrial = 0;
        ruleBreakingErrors_InTrial = 0;

        //playerViewLoaded = false;
        pathProgress.Clear();
        pathProgressGO.Clear();
        end = false;
        choiceMade = false;
        valueToAdd = 0;
    }
    private void DisableSceneElements()
    {
        SliderGo.SetActive(false);
        StartButton.SetActive(false);
        SliderHaloGo.SetActive(false);
        SliderHaloImage.gameObject.SetActive(false);
        MazeBackground.SetActive(false);
    } 
    public void AssignBlockVariables()
    {
        // All my variables are used as statics, so I need to store them somewhere so I can move them to the task level
        CurrentTaskLevel.totalErrors_InBlock += totalErrors_InTrial;
        CurrentTaskLevel.correctTouches_InBlock += correctTouches_InTrial;
        CurrentTaskLevel.retouchCorrect_InBlock += retouchCorrect_InTrial;
        CurrentTaskLevel.perseverativeErrors_InBlock += perseverativeErrors_InTrial;
        CurrentTaskLevel.backtrackErrors_InBlock += backtrackErrors_InTrial;
        CurrentTaskLevel.ruleAbidingErrors_InBlock += ruleAbidingErrors_InTrial;
        CurrentTaskLevel.ruleBreakingErrors_InBlock += ruleBreakingErrors_InTrial;
    }
    private void CreateTextOnExperimenterDisplay()
    {
         // sets parent for any playerView elements on experimenter display
        playerView = new PlayerViewPanel(); //GameObject.Find("PlayerViewCanvas").GetComponent<PlayerViewPanel>()
        playerViewText = new GameObject("PlayerViewText");
        if (!playerViewLoaded)
        {
            
            for (int i = 0; i < currMaze.mPath.Count; i++)
            {
                foreach (StimDef sd in tiles.stimDefs)
                {
                    Tile tileComponent = sd.StimGameObject.GetComponent<Tile>();
                    Vector2 textSize = new Vector2(200, 200);
                    if(tileComponent.mCoord == currMaze.mPath[i])
                    {
                        textLocation = playerViewPosition(Camera.main.WorldToScreenPoint(tileComponent.transform.position), playerViewParent.transform);
                        playerViewText = playerView.writeText((i+1).ToString(),(i+1).ToString(),
                            Color.red, textLocation, textSize, playerViewParent.transform);
                        playerViewText.GetComponent<RectTransform>().localScale = new Vector3(2, 2, 0);
                    }
                }
            }
            
            playerViewTextList.Add(playerViewText);
            playerViewLoaded = true;
        }
    }
    private void DestroyTextOnExperimenterDisplay()
    {
        DeactivateChildren(playerViewParent);
        foreach (var txt in playerViewTextList)
        {
            txt.GetComponent<Text>().color = Color.red; //resets the color if we repeat the sequence in the block
        }
        playerViewLoaded = false;
    }
    public override void FinishTrialCleanup()
    {
        DestroyTextOnExperimenterDisplay();
        tiles.DestroyStimGroup();
        MazeBackground.SetActive(false);
        SliderGo.SetActive(false);
        SliderHaloGo.SetActive(false);
        if (TokenFBController.isActiveAndEnabled)
            TokenFBController.enabled = false;

        if(AbortCode == 0)
            CurrentTaskLevel.CalculateBlockSummaryString();

        if(AbortCode == AbortCodeDict["RestartBlock"])
        {
            CurrentTaskLevel.ClearStrings();
            CurrentTaskLevel.BlockSummaryString.AppendLine("");
        }
    }
    void SetTrialSummaryString()
    {


        //TrialSummaryString = "<b>Task Name: " + CurrentTaskLevel.TaskName+ "</b>" + 
        //                     "\n"+
        //                     "<b>\nTrial Count in Block: " + (TrialCount_InBlock + 1) + "</b>" +
        //                     "\nTrial Count in Task: " + (TrialCount_InTask + 1) +
        //                     "\n" +
        //                     "\nTotal Errors: " + totalErrors_InTrial +
        //                     //"\nCorrect Touches: " + correctTouches_InBlock + COME UP WITH SOMETHING MORE USEFUL
        //                     "\nRule-Abiding Errors: " + ruleAbidingErrors_InTrial +
        //                     "\nRule-Breaking Errors: " + ruleBreakingErrors_InTrial + 
        //                     "\nPerseverative Errors: " + perseverativeErrors_InTrial +
        //                     "\nBacktrack Errors: " + backtrackErrors_InTrial +
        //                     "\nRetouch Correct: " + retouchCorrect_InTrial+ 
        //                     "\nMaze Duration: " + mazeEndTime +
        //                     "\n" +
        //                     "\nSlider Value: " + Slider.value;

        Debug.Log("TASK NAME = " + CurrentTaskLevel.TaskName);
        Debug.Log("TCIB: " + TrialCount_InBlock);
        Debug.Log(TrialCount_InTask);
        Debug.Log("TOTAL ERRORS: " + totalErrors_InTrial);

        Debug.Log(ruleAbidingErrors_InTrial);
        Debug.Log(ruleBreakingErrors_InTrial);
        Debug.Log(perseverativeErrors_InTrial);
        Debug.Log(backtrackErrors_InTrial);
        Debug.Log(retouchCorrect_InTrial);
        Debug.Log(mazeEndTime);
        Debug.Log(Slider.value);
    }
}