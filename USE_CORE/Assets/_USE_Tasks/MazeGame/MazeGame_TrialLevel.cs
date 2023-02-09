using UnityEngine;
using USE_States;
using USE_StimulusManagement;
using MazeGame_Namespace;


using HiddenMaze;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Data;
using ConfigDynamicUI;
using USE_Settings;
using USE_ExperimentTemplate_Trial;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.UI.Extensions;
using Screen = UnityEngine.Screen;

public class MazeGame_TrialLevel : ControlLevel_Trial_Template
{
    public MazeGame_TrialDef CurrentTrialDef => GetCurrentTrialDef<MazeGame_TrialDef>();
    public MazeGame_TaskLevel CurrentTaskLevel => GetTaskLevel<MazeGame_TaskLevel>();
    public List<Maze> mazeList = new List<Maze>();
    static bool end;
    
    private Vector2 dim;
    private float mazeLength, mazeHeight;
    private float mazeLengthDimensions, mazeHeightDimensions;
    public int ind;

    //game configs variables
    public float SCREEN_WIDTH, TILE_WIDTH;
    
    // TILE FLASH VARIABLES
    public static bool TileFlash;
    private static float flashDuration;
    
    // TASK LEVEL DEFINED COLOR VARIABLES
    public float[] startColor,
        finishColor,
        correctColor,
        lastCorrectColor,
        incorrectRuleAbidingColor,
        incorrectRuleBreakingColor,
        defaultTileColor;
    public int NumBlinks;
    /*public float CORRECT_FEEDBACK_SECONDS, PREV_CORRECT_FEEDBACK_SECONDS, 
        INCORRECT_RULEABIDING_SECONDS, INCORRECT_RULEBREAKING_SECONDS, TIMEOUT_SECONDS;*/
    
    //MazeVis Variables
   // public TileRow[] tileRows;
    public static GameObject mazeListObj;
   // public LoadMazeList mazeListScript;

    // public static GameConfigs gameConfigs = new GameConfigs();
    public static Maze currMaze;
    private bool mazeLoaded = false;
    private static int count;
    private Tile tile = new Tile();
    private GameObject tileGO;
    StimGroup tiles; // top of triallevel with other variable defs
    public Tile TilePrefab;
    public float TileSize;
    //private float spaceBetweenSquares;

    [HideInInspector] public ConfigNumber minObjectTouchDuration,
        itiDuration,
        finalFbDuration,
        maxObjectTouchDuration,
        selectObjectDuration,
        sliderSize;

    [FormerlySerializedAs("tileFlashDuration")] [HideInInspector] public ConfigNumber tileBlinkingDuration;

    [HideInInspector] public ConfigNumber spaceBetweenTiles,
        mazeOnsetDelay, correctFbDuration,
        previousCorrectFbDuration,
        incorrectRuleAbidingFbDuration,
        incorrectRuleBreakingFbDuration;

    public static float fbDuration;

    //private Button initButton;
    private Ray mouseRay;
    private int response;
    private RaycastHit hit;
    
    private float startTime;
    private int max, min;
    private int trialIndex;
    // TRIAL DATA TRACKING VARIABLES
    private static int totalErrors = 0,
        ruleAbidingErrors = 0,
        ruleBreakingErrors = 0,
        retouchCorrect = 0,
        correctTouches = 0,
        backtrackErrors = 0;
    public static int consecutiveErrors = 0, perseverativeErrors = 0;
    
        

    // Data Tracking Variables
    private string contextName = "";
    private bool isContextActive = false;
    private static List<Coords> pathProgress = new List<Coords>();
    public static List<GameObject> pathProgressGO = new List<GameObject>(); 
    
    //Block Data Variables
    private int numNonStimSelections_InBlock = 0;
    public int NumRewardPulses_InBlock = 0;
    public static int totalErrors_InBlock = 0,
        ruleAbidingErrors_InBlock = 0,
        ruleBreakingErrors_InBlock = 0,
        retouchCorrect_InBlock = 0,
        correctTouches_InBlock = 0,
        backtrackErrors_InBlock = 0,
        perseverativeErrors_InBlock = 0;

    

    public string ContextExternalFilePath, MazeFilePath;
    public Vector3 ButtonPosition, ButtonScale;
    public GameObject MazeBackground;
    private GameObject MazeContainer;
    
    
    private bool variablesLoaded = false;
    public string mazeDefName;
    public static bool viewPath = false;
    private GameObject chosenStim;
    
    // Touch Evaluation Variables
    private GameObject selectedGO = null;
    private static bool CorrectSelection;
    private MazeGame_StimDef selectedSD = null;
    private bool choiceMade = false;
    
    // Nonstim Scene Elements
    private GameObject StartButton;
    
    // Slider Variables
    private bool isSliderValueIncrease = false;
    private Vector3 SliderInitPosition;
    private Image SliderHaloImage;
    private Slider Slider;
    private GameObject SliderGo, SliderHaloGo;
    public GameObject SliderPrefab, SliderHaloPrefab;
    
    //Player View Variables
    private PlayerViewPanel playerView;
    private Transform playerViewParent; // Helps set things onto the player view in the experimenter display
    public List<GameObject> playerViewTextList;
    public GameObject playerViewText;
    private Vector2 textLocation;
    private bool playerViewLoaded;
    
//update slider variables
    private float endupdatetime = 0f;
    private float valueRemaining = 0f;
    private float valueToAdd = 0f;
    private float incrementalVal = 0f;
    private float sliderValueChange;
    public override void DefineControlLevel()
    {
        //define States within this Control Level
        State InitTrial = new State("InitTrial");
        State LoadMaze = new State("LoadMaze");
        State GameConf = new State("GameConf");
        State ChooseTile = new State("ChooseTile");
        State SelectionFeedback = new State("SelectionFeedback");
        State TileFlashFeedback = new State("TileFlashFeedback");
        State FinalFeedback = new State("FinalFeedback");
        State delay = new State("Delay");
        State ITI = new State("ITI");
        AddActiveStates(new List<State> {InitTrial, LoadMaze, GameConf, ChooseTile, SelectionFeedback, TileFlashFeedback, FinalFeedback, ITI, delay});

        string[] stateNames = new string[] {"StartButton", "LoadMaze", "GameConf", "ChooseTile", "SelectionFeedback", "TileFlashFeedback", "FinalFeedback", "ITI", "Delay"};
        
        // A state that just waits for some time
        State stateAfterDelay = null;
        float delayDuration = 0;
        delay.AddTimer(() => delayDuration, () => stateAfterDelay);
        
        
        // maze varialbes
        
        SelectionHandler<MazeGame_StimDef> mouseHandler = new SelectionHandler<MazeGame_StimDef>();
        
        // define initScreen state*/
        Add_ControlLevel_InitializationMethod(() =>
        {
            InitializeSlider();
            LoadTextures(ContextExternalFilePath);
            HaloFBController.SetHaloSize(5);
            StartButton = CreateSquare("StartButton", StartButtonTexture, ButtonPosition, ButtonScale);
            MazeContainer = new GameObject("MazeContainer");
            MazeBackground = CreateSquare("MazeBackground", MazeBackgroundTexture, new Vector3(0,0,0), new Vector3(5, 5, 5));
            
            
            /*
            //player view variables
            playerView = new PlayerViewPanel(); //GameObject.Find("PlayerViewCanvas").GetComponent<PlayerViewPanel>()
            playerViewText = new GameObject("PlayerViewText");
            playerViewParent = GameObject.Find("MainCameraCopy").transform; // sets parent for any playerView elements on experimenter display*/
        });
        SetupTrial.AddInitializationMethod(() =>
        {
            isContextActive = true;
            contextName = CurrentTrialDef.ContextName;
            RenderSettings.skybox = CreateSkybox(ContextExternalFilePath + Path.DirectorySeparatorChar + CurrentTrialDef.ContextName + ".png");
            if (!variablesLoaded) loadVariables();
            viewPath = CurrentTrialDef.ViewPath;
           // if(TrialCount_InBlock == 0) TaskLevel.ResetBlockVariables();
            
            ResetTrialTrackingVariables();
            Input.ResetInputAxes(); //reset input in case they still touching their selection from last trial!
        });
        SetupTrial.SpecifyTermination(() => true, InitTrial);
        MouseTracker.AddSelectionHandler(mouseHandler, InitTrial);
        InitTrial.AddInitializationMethod(() =>
        {
            StartButton.SetActive(true);
        });
        //  StartButton.SpecifyTermination(() => mouseHandler.SelectionMatches(initButton), MazeVis);
        InitTrial.SpecifyTermination(()=> mouseHandler.SelectionMatches(StartButton), delay, () =>
        {
            stateAfterDelay = ChooseTile;
            delayDuration = mazeOnsetDelay.value;
            SliderGo.SetActive(true);
            StartButton.SetActive(false);
            
            ConfigureSlider();
            
            numNonStimSelections_InBlock += mouseHandler.GetNumNonStimSelection();
            InstantiateCurrMaze();
//            EventCodeManager.SendCodeNextFrame(TaskEventCodes["SliderReset"]);
        });
        MouseTracker.AddSelectionHandler(mouseHandler, ChooseTile);
        ChooseTile.AddUpdateMethod(()=>
        { //SELECTION HANDLER ISN'T WORKING, GIVES THE MAZE CONTAINER AS .SELECTEDGAMEOBJECT & CHILDREN ARE ALL TILES
            //Input.ResetInputAxes(); //reset input in case they holding down
            if (InputBroker.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(InputBroker.mousePosition);
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
            choiceMade = false;
            selectedGO.GetComponent<Tile>().OnMouseDown();
            endupdatetime = Time.time + fbDuration;
            if (CorrectSelection)
            {
                isSliderValueIncrease = true;
                valueToAdd = sliderValueChange;
                //ADD ANYTHING ELSE THAT OCCURS DURING CORRECT SELECTION FEEDBACK
                AudioFBController.Play("Positive");
            }
            else
            {
                valueToAdd = 0f;
                AudioFBController.Play("Negative");
            }
            
            incrementalVal = valueToAdd/(fbDuration*60);
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
        SelectionFeedback.AddTimer(()=>fbDuration, delay, () =>
        {
            
            delayDuration = 0;
            valueRemaining = 0;
            SliderHaloGo.SetActive(false);
            if (end)
            {
                stateAfterDelay = FinalFeedback;
                CorrectSelection = false;
            }
            else if (CheckTileFlash())
            {
                stateAfterDelay = TileFlashFeedback;
                CorrectSelection = false; // could be incorrect or correct but it will still go back
            }
            else
            {
                stateAfterDelay = ChooseTile;
                CorrectSelection = false; // could be incorrect or correct but it will still go back
            }
        });
        TileFlashFeedback.AddInitializationMethod(() =>
        {
            
            tile.StartCoroutine(tile.FlashingFeedback());
        });
        TileFlashFeedback.AddTimer(() => tileBlinkingDuration.value, ChooseTile);
            FinalFeedback.AddInitializationMethod(() =>
        {
            Debug.Log("the end");
            SliderHaloGo.SetActive(true);
            SliderHaloImage.color = new Color(1, 1, 1, 0.2f);
            startTime = Time.time;
            if (SyncBoxController != null)
            {
                SyncBoxController.SendRewardPulses(CurrentTrialDef.NumPulses, CurrentTrialDef.PulseSize);
                NumRewardPulses_InBlock += CurrentTrialDef.NumPulses;
            }
        });

        FinalFeedback.AddUpdateMethod(() =>
        {
            if ((int) (10 * (Time.time - startTime)) % 4 == 0)
            {
                SliderHaloImage.color = new Color(1, 1, 1, 0.2f);
            }
            else if ((int) (10 * (Time.time - startTime)) % 2 == 0)
            {
                SliderHaloImage.color = new Color(1, 0, 0, 0.2f);
            }
        });
        FinalFeedback.AddTimer(()=>finalFbDuration.value, ITI, () =>
        {
            DisableSceneElements();
            DestroyCurrMaze();
            
        });
        //Define iti state
        ITI.SpecifyTermination(() => true, FinishTrial, () =>
        {
            AssignTrialData();
        });
        FinishTrial.AddInitializationMethod(() =>
        {
            UpdateBlockDictionary(CurrentTaskLevel.BlockDataDictionary);
        });
        
    }

    void InstantiateCurrMaze()
    {
        
        Slider.gameObject.SetActive(true);
        // This will Load all text 
        string[] textMaze = System.IO.File.ReadAllLines(MazeFilePath + Path.DirectorySeparatorChar + mazeDefName);
        currMaze = new Maze(textMaze[0]);
        sliderValueChange = (100f / (currMaze.mNumSquares))/100f ;
        dim = currMaze.mDims;
       Vector3 mazeCenter = new Vector3(0, 0, 0);
       
       mazeLength = (dim.x * TileSize) + ((dim.x - 1) * (spaceBetweenTiles.value));
       mazeHeight = (dim.y * TileSize) + ((dim.y - 1) * (spaceBetweenTiles.value));
       MazeBackground.transform.SetParent(MazeContainer.transform); // setting it last so that it doesn't cover tiles
       MazeBackground.transform.localScale = new Vector3(mazeLength + (2*spaceBetweenTiles.value), mazeHeight + (2*spaceBetweenTiles.value), 0.1f);
       /*SortingGroup mazeSG = MazeBackground.AddComponent<SortingGroup>() as SortingGroup;
       mazeSG.sortingOrder = 0;*/
       MazeBackground.SetActive(true);
       Vector3 bottomLeftMazePos = mazeCenter - (new Vector3(mazeLength / 2, mazeHeight / 2, 0));
        
        tiles = new StimGroup("Tiles");
        
        for (int x = 1; x <= dim.x; x++)
        {
            for (int y = 1; y <= dim.y; y++)
            {
                tile = Instantiate(TilePrefab, MazeContainer.transform);
                SetGameConfigs();
                tile.transform.localScale = new Vector3(TileSize, TileSize, 0.5f);
                tile.gameObject.SetActive(true);
                tile.gameObject.GetComponent<Tile>().enabled = true;
                Texture2D tileTex = LoadPNG(ContextExternalFilePath + Path.DirectorySeparatorChar + "Tile.png");
                tile.gameObject.GetComponent<MeshRenderer>().sharedMaterial.mainTexture = tileTex;
                float displaceX = (((2*(x-1))+1) * (TileSize/2)) + (spaceBetweenTiles.value*(x-1));
                float displaceY = (((2*(y-1))+1) * (TileSize/2))  + (spaceBetweenTiles.value*(y-1));
                Vector3 newTilePosition = bottomLeftMazePos + new Vector3(displaceX, displaceY, 0);

                tile.transform.position = newTilePosition;
                tile.mCoord = new Coords(x, y);

                if (x == currMaze.mStart.x && y == currMaze.mStart.y)
                {
                    tile.gameObject.GetComponent<Tile>().setColor(tile.START_COLOR);
                }
                else if (x == currMaze.mFinish.x && y == currMaze.mFinish.y)
                {
                    tile.gameObject.GetComponent<Tile>().setColor(tile.FINISH_COLOR);
                }
                else
                {
                    tile.gameObject.GetComponent<Tile>().setColor(tile.DEFAULT_TILE_COLOR);
                }
                tiles.AddStims(tile.gameObject); 
            }
        }
        //MazeBackground.GetComponent<Renderer>().sortingOrder = 0; // sends maze background to behind the tiles
    }
    void DestroyCurrMaze()
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

        float progress = (float) correctTouches / (float) currMaze.mNumSquares;
        float ratio = (float) correctTouches / (float) totalErrors;
        Debug.Log("Progress: " + progress);
        Debug.Log("Accuracy: " + ratio);

    }

    public bool CheckTileFlash()
    {
        if (consecutiveErrors >= 2)
        {
            // Should provide flashing feedback of the last correct tile
            Debug.Log("*Perseverative Error*");
            perseverativeErrors++;
            perseverativeErrors_InBlock++;
            return true;
        }
        else return false;
    }
    

    public static int ManageTileTouch(Tile tile)
    {
        Coords touchedCoord = tile.mCoord;

        // ManageTileTouch - Returns correctness code
        // Return values:
        // 0 - correct and finish tile (maze is complete)
        // 1 - correct and start tile
        // 2 - correct regular tile along path
        // 3 - rule-breaking backtrack
        // 99 - correct and finish (maze is complete)

        // 30 - previous correct tile
        // 31 - previous correct tile and start

        // 10 - rule-abiding incorrect
        // 12 - rule-abiding incorrect and finish
    
        // 21 - rule-breaking incorrect and start
        // 22 - rule-breaking incorrect and finish
        
        // CORRECT TILE TOUCH (then narrow down if its is start, finish, or other)
        if ((currMaze.mNextStep == currMaze.mStart && touchedCoord != currMaze.mStart))
        {
            Debug.Log("*Rule Breaking Error - Not Pressing the Start Tile to Begin the Maze*");
            totalErrors++;
            totalErrors_InBlock++;
            ruleBreakingErrors++; 
            ruleBreakingErrors_InBlock++;
            
            fbDuration = tile.INCORRECT_RULEBREAKING_SECONDS;
            return 20;
        }
        if ((touchedCoord == currMaze.mNextStep || touchedCoord.isAdjacentTo(currMaze.mPath[currMaze.mPath.FindIndex(pathCoord => 
                pathCoord == currMaze.mNextStep) - 1]) )&& consecutiveErrors != 0)
        {
            Debug.Log("*Rule-Breaking Error - Didn't return to previously correct tile after error, but the tile is in the hidden path*");
            totalErrors++;
            totalErrors_InBlock++;
            ruleBreakingErrors++;
            ruleBreakingErrors++;
            consecutiveErrors++;
            fbDuration = tile.INCORRECT_RULEBREAKING_SECONDS;
            return 20;
        }
        if (touchedCoord == currMaze.mNextStep && consecutiveErrors == 0)
        {
            Debug.Log("*Correct Tile Touch*");
            correctTouches++;
            correctTouches_InBlock++;
            CorrectSelection = true;
            pathProgress.Add(touchedCoord);
            pathProgressGO.Add(tile.gameObject);
            fbDuration = tile.CORRECT_FEEDBACK_SECONDS;
            
            // Sets the NextStep if the maze isn't finished
            if (touchedCoord != currMaze.mFinish)
                currMaze.mNextStep =
                    currMaze.mPath[currMaze.mPath.FindIndex(pathCoord => pathCoord == touchedCoord) + 1];
            else end = true;// Finished the Maze
           
            return 1; 
        }

        // LAST CORRECT TILE TOUCH - idk what kind of error feedback it gives?? just makes dark green tile
        if (currMaze.mPath[currMaze.mPath.FindIndex(pathCoord => pathCoord == currMaze.mNextStep) - 1] ==
                 touchedCoord)
        {
            Debug.Log("*Last Correct Tile Touch*");
            fbDuration = tile.PREV_CORRECT_FEEDBACK_SECONDS;
            retouchCorrect++;
            retouchCorrect_InBlock++;
            consecutiveErrors = 0;
            return 2;
        }
        // RULE ABIDING TOUCH 
        if ((currMaze.mNextStep != currMaze.mStart) && touchedCoord.isAdjacentTo(currMaze.mPath[currMaze.mPath.FindIndex(pathCoord => 
                     pathCoord == currMaze.mNextStep) - 1]) && !pathProgress.Contains(touchedCoord))
        {
            consecutiveErrors++;
            Debug.Log("*Rule-Abiding Incorrect Error*");
            totalErrors++;
            totalErrors_InBlock++;
            ruleAbidingErrors++;
            ruleAbidingErrors_InBlock++;
            fbDuration = tile.INCORRECT_RULEABIDING_SECONDS;
            return 10;
        }
        else
        {
            // RULE BREAKING TOUCH
            Debug.Log("*Rule-Breaking Incorrect Error*");
            totalErrors++;
            totalErrors_InBlock++;
            ruleBreakingErrors++;
            ruleBreakingErrors_InBlock++;
            ruleAbidingErrors_InBlock++;
            consecutiveErrors++;
            fbDuration = tile.INCORRECT_RULEBREAKING_SECONDS;
            if (pathProgress.Contains(touchedCoord))
            {
                Debug.Log("*Rule-Breaking Backtrack Error*");
                backtrackErrors++;
                backtrackErrors_InBlock++;
            }
            return 20;
        }
        // ManageTileTouch - Returns correctness code
        // Return values:
        // 0 - correct and regular tile
        // 1 - correct and start tile
        // 2 - rule-breaking incorrect
        // 3 - rule-breaking backtrack
        // 99 - correct and finish (maze is complete)

        // 30 - previous correct tile
        // 31 - previous correct tile and start

        // 10 - rule-abiding incorrect
        // 12 - rule-abiding incorrect and finish
    
        // 21 - rule-breaking incorrect and start
        // 22 - rule-breaking incorrect and finish
        
        /*// CORRECT DEFAULT
        if (touchedCoord == currMaze.mNextStep && touchedCoord != currMaze.mStart && touchedCoord != currMaze.mFinish)
        {
            Debug.Log("correct");
            correctTouches++;
            CorrectSelection = true;
            fbDuration = tile.CORRECT_FEEDBACK_SECONDS;
            // Every tile in maze is unique in path, path should NOT contain same tile twice
            currMaze.mNextStep = currMaze.mPath[currMaze.mPath.FindIndex(pathCoord => pathCoord == touchedCoord) + 1];
            PreviouslySelectedPath.Add(touchedCoord);
            return 0;
        }

        // CORRECT and START
        else if (touchedCoord == currMaze.mStart && touchedCoord == currMaze.mNextStep)
        {
            Debug.Log("**** started maze ****");
            correctTouches++;
            CorrectSelection = true;
            fbDuration = tile.CORRECT_FEEDBACK_SECONDS;
            currMaze.mNextStep = currMaze.mPath[currMaze.mPath.FindIndex(pathCoord => pathCoord == touchedCoord) + 1];
            PreviouslySelectedPath.Add(touchedCoord);
            return 1;
        }
        // RULE-BREAKING BACKTRACK
        else if (PreviouslySelectedPath.Contains(touchedCoord))
        {
            Debug.Log("rule-breaking incorrect backtrack");
            totalErrors++;
            ruleBreakingErrors++;
            fbDuration = tile.INCORRECT_RULEBREAKING_SECONDS;
            return 2;
        }
        // RULE-BREAKING INCORRECT DEFAULT & NOT START
        else if (touchedCoord != currMaze.mStart && touchedCoord != currMaze.mFinish || touchedCoord != currMaze.mStart && currMaze.mNextStep == currMaze.mStart)
        {
            Debug.Log("rule-breaking incorrect or not start");
            totalErrors++;
            ruleBreakingErrors++;
            fbDuration = tile.INCORRECT_RULEBREAKING_SECONDS;
            return 3;
        }
        
        
        // CORRECT and FINISH
        else if (touchedCoord == currMaze.mFinish && touchedCoord == currMaze.mNextStep)
        {
            Debug.Log("**** Finished the maze! ****");
            // TODO: add maze finish operations
            CorrectSelection = true;
            fbDuration = tile.CORRECT_FEEDBACK_SECONDS;
            currMaze.mNextStep = currMaze.mStart;
            return 99;
        }

        // LAST CORRECT STEP DEFAULT
        else if (touchedCoord != currMaze.mStart && touchedCoord ==
                 currMaze.mPath[currMaze.mPath.FindIndex(pathCoord => pathCoord == currMaze.mNextStep) - 1])
        {
            Debug.Log("last correct step");
            retouchCorrect++;
            fbDuration = tile.PREV_CORRECT_FEEDBACK_SECONDS;
            return 30;
        }

        // LAST CORRECT STEP and START
        else if (touchedCoord == currMaze.mStart && touchedCoord ==
                 currMaze.mPath[currMaze.mPath.FindIndex(pathCoord => pathCoord == currMaze.mNextStep) - 1])
        {
            Debug.Log("last correct step");
            retouchCorrect++;
            fbDuration = tile.PREV_CORRECT_FEEDBACK_SECONDS;
            return 31;
        }

        // RULE-ABIDING INCORRECT DEFAULT
        // Check if this isn't the first touch and the touch was adjacent to the previous correct tile
        // In order for something to be a rule-abiding touch, there must have already been at least one correct touch on the start tile
        else if ((currMaze.mNextStep != currMaze.mStart) && touchedCoord.isAdjacentTo(
                                                             currMaze.mPath[
                                                                 currMaze.mPath.FindIndex(pathCoord =>
                                                                     pathCoord == currMaze.mNextStep) - 1])
                                                         && touchedCoord != currMaze.mStart
                                                         && touchedCoord != currMaze.mFinish)
        {
            Debug.Log("rule-abiding incorrect");
            totalErrors++;
            ruleAbidingErrors++;
            fbDuration = tile.INCORRECT_RULEABIDING_SECONDS;
            return 10;
        }

        /#1#/ RULE-ABIDING INCORRECT and START
        else if ((currMaze.mNextStep != currMaze.mStart) && touchedCoord.isAdjacentTo(
                                                             currMaze.mPath[
                                                                 currMaze.mPath.FindIndex(pathCoord =>
                                                                     pathCoord == currMaze.mNextStep) - 1])
                                                         && touchedCoord != currMaze.mFinish)
        {
            Debug.Log("rule-abiding incorrect");
            totalErrors++;
            ruleAbidingErrors++;
            fbDuration = tile.INCORRECT_RULEABIDING_SECONDS;
            return 11;
        }#1#

        // RULE-ABIDING INCORRECT and FINISH
        else if ((currMaze.mNextStep != currMaze.mStart) &&
                 touchedCoord.isAdjacentTo(
                     currMaze.mPath[currMaze.mPath.FindIndex(pathCoord => pathCoord == currMaze.mNextStep) - 1]))
        {
            Debug.Log("rule-abiding incorrect");
            totalErrors++;
            ruleAbidingErrors++;
            fbDuration = tile.INCORRECT_RULEABIDING_SECONDS;
            return 12;
        }
        
        // RULE-BREAKING INCORRECT and START
        else if (touchedCoord != currMaze.mFinish)
        {
            Debug.Log("rule-breaking incorrect");
            totalErrors++;
            ruleBreakingErrors++;
            fbDuration = tile.INCORRECT_RULEBREAKING_SECONDS;
            return 21;
        }
        
        // RULE-BREAKING INCORRECT and FINISH
        else
        {
            Debug.Log("rule-breaking incorrect");
            totalErrors++;
            ruleBreakingErrors++;
            fbDuration = tile.INCORRECT_RULEBREAKING_SECONDS;
            return 22;
        }*/
        
    }
    private void InitializeSlider()
    {
        Transform sliderCanvas = GameObject.Find("SliderCanvas").transform;
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
        
        tile.START_COLOR = new Color(startColor[0], startColor[1], startColor[2], 1);;

        // Finish - Light blue
        tile.FINISH_COLOR = new Color(finishColor[0], finishColor[1], finishColor[2], 1);;

        // Correct - Light green
        tile.CORRECT_COLOR = new Color(correctColor[0], correctColor[1], correctColor[2]);;

        // Prev correct - Darker green
        tile.PREV_CORRECT_COLOR = new Color(lastCorrectColor[0], lastCorrectColor[1], lastCorrectColor[2]);;

        // Incorrect rule-abiding - Orange
        tile.INCORRECT_RULEABIDING_COLOR = new Color(incorrectRuleAbidingColor[0], incorrectRuleAbidingColor[1], incorrectRuleAbidingColor[2]);;

        // Incorrect rule-breaking - Black
        tile.INCORRECT_RULEBREAKING_COLOR = new Color(incorrectRuleBreakingColor[0], incorrectRuleBreakingColor[1], incorrectRuleBreakingColor[2]);;
        

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
        TrialData.AddDatum("Total Errors", ()=> totalErrors);
        TrialData.AddDatum("Perseverative Errors", () => perseverativeErrors);
        TrialData.AddDatum("Backtracking Errors", ()=> backtrackErrors);
        TrialData.AddDatum("Rule-Abiding Errors", () => ruleAbidingErrors);
        TrialData.AddDatum("Rule-Breaking Errors", () => ruleBreakingErrors);
        TrialData.AddDatum("RetouchCorrect", () => retouchCorrect);
    }


    private void ResetTrialTrackingVariables()
    {
        totalErrors = 0;
        ruleAbidingErrors = 0;
        ruleBreakingErrors = 0;
        retouchCorrect = 0;
        correctTouches = 0;
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
    }/*
    private GameObject CreateMazeBackground(Texture2D tex, Rect rect)
    {
        GameObject mazeBackground = new GameObject("MazeBackground");
        SpriteRenderer borderSprite = mazeBackground.AddComponent<SpriteRenderer>() as SpriteRenderer;
        borderSprite.sprite = Sprite.Create(tex, new Rect(rect.x, rect.y, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
        return mazeBackground;
    }*/

    public void UpdateBlockDictionary(IDictionary<string,int> dataDict)
    {
        // All my variables are used as statics, so I need to store them somwhere so I can move them to the task level
        dataDict["Total Errors in Block"] = totalErrors_InBlock;
        dataDict["Perseverative Errors in Block"] = perseverativeErrors_InBlock;
        dataDict["Backtracking Errors in Block"] =  backtrackErrors_InBlock;
        dataDict["Rule-Abiding Errors in Block"] =  ruleAbidingErrors_InBlock;
        dataDict["Rule-Breaking Errors in Block"] = ruleBreakingErrors_InBlock;
    }

}
