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
using System.Windows.Forms;
using Screen = UnityEngine.Screen;

public class MazeGame_TrialLevel : ControlLevel_Trial_Template
{
    public MazeGame_TrialDef CurrentTrialDef => GetCurrentTrialDef<MazeGame_TrialDef>();
    
    public List<Maze> mazeList = new List<Maze>();
    static bool end;
    private Vector2 dim;
    private float mazeLength, mazeHeight;
    private float mazeLengthDimensions, mazeHeightDimensions;
    public int ind;

    //game configs variables
    public float SCREEN_WIDTH, TILE_WIDTH;
    public Color START_COLOR, FINISH_COLOR, CORRECT_COLOR, LAST_CORRECT_COLOR, 
        INCORRECT_RULEABIDING_COLOR, INCORRECT_RULEBREAKING_COLOR, DEFAULT_TILE_COLOR;
    public float CORRECT_FEEDBACK_SECONDS, PREV_CORRECT_FEEDBACK_SECONDS, 
        INCORRECT_RULEABIDING_SECONDS, INCORRECT_RULEBREAKING_SECONDS, TIMEOUT_SECONDS;
    
    //MazeVis Variables
    public TileRow[] tileRows;
    public static GameObject mazeListObj;
   // public LoadMazeList mazeListScript;

    // public static GameConfigs gameConfigs = new GameConfigs();
    public static Maze currMaze;
    private bool mazeLoaded = false;
    private static int count;
    private Tile tile;
    private GameObject tileGO;
    StimGroup tiles; // top of triallevel with other variable defs
    public Tile TilePrefab;
    [HideInInspector] public ConfigNumber minObjectTouchDuration,
        itiDuration,
        finalFbDuration,
        fbDuration,
        maxObjectTouchDuration,
        selectObjectDuration,
        sliderSize,
        mazeOnsetDelay, 
        tileSize,
        correctFbDuration,
        previousCorrectFbDuration,
        incorrectRuleAbidingFbDuration,
        incorrectRuleBreakingFbDuration;
    //private Button initButton;
    private Ray mouseRay;
    private int response;
    private RaycastHit hit;
    
    private float startTime;
    private int max, min;
    private int trialIndex;
    public static int totalErrors = 0, ruleAbidingErrors = 0, ruleBreakingErrors = 0, 
        retouchCorrect = 0, correctTouches = 0;

    public static Color TileColor;
    public string ContextExternalFilePath, MazeFilePath;
    public Vector3 ButtonPosition, ButtonScale;
    public GameObject mazeBackground;
    public Texture2D backgroundTex;
    
    
    private bool variablesLoaded = false;
    public string mazeDefName;
    public static bool viewPath = false;
    private GameObject chosenStim;
    
    // Touch Evaluation Variables
    private GameObject selectedGO = null;
    private bool CorrectSelection;
    private MazeGame_StimDef selectedSD = null;
    private bool choiceMade = false;
    
    //Block Data Variables
    private int numNonStimSelections_InBlock = 0;
    
    // Data Tracking Variables
    private string contextName = "";
    private bool isContextActive = false;
    
    // Nonstim Scene Elements
    private GameObject StartButton;
    
    // Slider Variables
    private bool isSliderValueIncrease = false;
    private Vector3 SliderInitPosition;
    private GameObject SliderHalo;
    private Image SliderHaloImage;
    private Slider Slider;
    private GameObject SliderGo, SliderHaloGo;
    public GameObject SliderPrefab, SliderHaloPrefab;
    private float sliderValueIncreaseAmount;
    
    //Player View Variables
    private PlayerViewPanel playerView;
    private Transform playerViewParent; // Helps set things onto the player view in the experimenter display
    public List<GameObject> playerViewTextList;
    public GameObject playerViewText;
    private Vector2 textLocation;
    private bool playerViewLoaded;
    public override void DefineControlLevel()
    {
        //define States within this Control Level
        State InitTrial = new State("InitTrial");
        State LoadMaze = new State("LoadMaze");
        State GameConf = new State("GameConf");
        State MazeVis = new State("MazeVis");
        State Feedback = new State("Feedback");
        State ITI = new State("ITI");
        AddActiveStates(new List<State> {InitTrial, LoadMaze, GameConf, MazeVis, Feedback, ITI});

        string[] stateNames = new string[] {"StartButton", "LoadMaze", "GameConf", "MazeVis", "Feedback", "ITI"};
        //player view variables
        
        playerView = new PlayerViewPanel(); //GameObject.Find("PlayerViewCanvas").GetComponent<PlayerViewPanel>()
        playerViewText = new GameObject(); 

        SelectionHandler<MazeGame_StimDef> mouseHandler = new SelectionHandler<MazeGame_StimDef>();
        
        // define initScreen state*/
        Add_ControlLevel_InitializationMethod(() =>
        {
            InitializeSlider();
            LoadTextures(ContextExternalFilePath);
            HaloFBController.SetHaloSize(5);
            StartButton = CreateSquare("StartButton", StartButtonTexture, ButtonPosition, ButtonScale);
            playerViewParent = GameObject.Find("MainCameraCopy").transform; // sets parent for any playerView elements on experimenter display
        });
        SetupTrial.AddInitializationMethod(() =>
        {
            isContextActive = true;
            contextName = CurrentTrialDef.ContextName;
            RenderSettings.skybox = CreateSkybox(ContextExternalFilePath + Path.DirectorySeparatorChar + CurrentTrialDef.ContextName + ".png");
            if (!variablesLoaded) loadVariables();
            if (CurrentTrialDef.viewPath == 1)
            {
                viewPath = true;
            }
            else
            {
                viewPath = false;
            }
            Input.ResetInputAxes(); //reset input in case they still touching their selection from last trial!
        });
        SetupTrial.SpecifyTermination(() => true, InitTrial);
        MouseTracker.AddSelectionHandler(mouseHandler, InitTrial);
        InitTrial.AddInitializationMethod(() =>
        {
            SetGameConfigs();
            StartButton.SetActive(true);
        });
        //  StartButton.SpecifyTermination(() => mouseHandler.SelectionMatches(initButton), MazeVis);
        InitTrial.SpecifyTermination(()=> mouseHandler.SelectionMatches(StartButton), MazeVis, () =>
        {
            SliderGo.SetActive(true);
            StartButton.SetActive(false);
            ConfigureSlider();
            numNonStimSelections_InBlock += mouseHandler.GetNumNonStimSelection();
//            EventCodeManager.SendCodeNextFrame(TaskEventCodes["SliderReset"]);
        });
        MouseTracker.AddSelectionHandler(mouseHandler, MazeVis);
        MazeVis.AddInitializationMethod(() =>
        {
            InstantiateCurrMaze();
        });
        MazeVis.AddUpdateMethod(()=>
        { //SELECTION HANDLER ISN'T WORKING, GIVES THE MAZE CONTAINER AS .SELECTEDGAMEOBJECT & CHILDREN ARE ALL TILES
            if (InputBroker.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(InputBroker.mousePosition);
                if (Physics.Raycast(ray, out hit))
                {
                    choiceMade = true;
                    selectedGO = hit.transform.gameObject;
                    if (selectedGO.GetComponent<Tile>() != null) selectedGO.GetComponent<Tile>().OnMouseDown();
                }
            }
            
        });
        MazeVis.SpecifyTermination(() => end == true, Feedback);

        MazeVis.AddDefaultTerminationMethod(() =>
        {
            DestroyCurrMaze();
            // end = false;
            mazeBackground.SetActive(false);
        });

        Feedback.AddInitializationMethod(() =>
        {
            Debug.Log("the end");
            SliderHalo.SetActive(true);
            // sphereCount = 0;
            SliderHaloImage.color = new Color(1, 1, 1, 0.2f);
            startTime = Time.time;
        });

        Feedback.AddUpdateMethod(() =>
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
        Feedback.AddTimer(()=>finalFbDuration.value, ITI, () =>
        {
            SliderHalo.SetActive(false);
            Slider.gameObject.SetActive(false);
        });
        //Define iti state
        ITI.SpecifyTermination(() => true, FinishTrial, () =>
        {
            ResetDataTrackingVariables();
        });
        
        Debug.Log("ERRORS: " + totalErrors);
        TrialData.AddDatum("TrialNum", () => CurrentTrialDef.TrialCount);
        TrialData.AddDatum("TotalErrors", () => totalErrors);
        TrialData.AddDatum("Rule-Abiding Errors", () => ruleAbidingErrors);
        TrialData.AddDatum("Rule-Breaking Errors", () => ruleBreakingErrors);
        TrialData.AddDatum("RetouchCorrect", () => retouchCorrect);
    }

    void InstantiateCurrMaze()
    {
        Slider.gameObject.SetActive(true);
        // This will Load all text 
        string[] textMaze = System.IO.File.ReadAllLines(MazeFilePath + Path.DirectorySeparatorChar + mazeDefName);
        Debug.Log("TEXT MAZE: " + textMaze[0]);
        currMaze = new Maze(textMaze[0]);
        Debug.Log("CURRENT MAZE NAME: " + mazeDefName);
        Debug.Log("CURRENT MAZE DIMENSIONS: " + currMaze.mDims);
        Debug.Log("CURRENT MAZE NUM TURNS: " + currMaze.mNumTurns);
        Debug.Log("CURRENT MAZE NUM SQUARES: " + currMaze.mNumSquares);
        sliderValueIncreaseAmount = (100f / (currMaze.mNumSquares)) / 100f;
        dim = currMaze.mDims;
       // GameObject mazeCenter = GameObject.FindWithTag("Center");
       Vector3 mazeCenter = new Vector3(0, 0, 0);
        mazeLength = (TILE_WIDTH*tileSize.value) * ((3*dim.x + 1)/2);
        Debug.Log("MAZE LENGTH: " + mazeLength);
        Debug.Log("TILE WIDTH: " + TILE_WIDTH);
        
        mazeHeight = (TILE_WIDTH*tileSize.value) * ((3*dim.y + 1)/2);
        Debug.Log("MAZE HEIGHT: " + mazeHeight);
        Vector3 bottomLeftMazePos = mazeCenter - (new Vector3(mazeLength / 2, mazeHeight / 2, 0));
        backgroundTex = LoadPNG(ContextExternalFilePath + Path.DirectorySeparatorChar + "MazeBackground.png");
        mazeBackground = CreateMazeBackground(backgroundTex, new Rect(new Vector2(0,0), new Vector2(1,1)));

        GameObject mazeContainer = new GameObject("MazeContainer");
        mazeBackground.transform.SetParent(mazeContainer.transform);
        mazeBackground.transform.localPosition = new Vector3(0, 0, 0);
        mazeBackground.transform.localScale = new Vector3(dim.x/9f, dim.y/9f, 0);
        tile = Instantiate(TilePrefab, mazeContainer.transform);
        Texture2D tileTex = LoadPNG(ContextExternalFilePath + Path.DirectorySeparatorChar + "Tile.png");
        Debug.Log("TILE GAME OBJECT TEX: " + tileTex);
        tile.gameObject.GetComponent<MeshRenderer>().sharedMaterial.mainTexture = tileTex;
        tiles = new StimGroup("Tiles"); //in DefineTrialStims
        // tiles.DestroyStimGroup(); //when tiles should be destroyed

        for (int x = 0; x < dim.x; ++x)
        {
            for (int y = 0; y < dim.y; ++y)
            {
                tile.gameObject.SetActive(true);
                tile.gameObject.GetComponent<Tile>().enabled = true;
                
                float displaceX = x * TILE_WIDTH;
                float displaceY = y * TILE_WIDTH;
                Vector3 newTilePosition = bottomLeftMazePos + new Vector3(displaceX, displaceY, 0);
                // Instantiate the tile

                tile.transform.position = newTilePosition;
                tile.transform.localScale = new Vector3(TILE_WIDTH*tileSize.value, TILE_WIDTH*tileSize.value, 0.1f);
                tile.mCoord = new Coords(x, y);

                if (x == currMaze.mStart.X && y == currMaze.mStart.Y)
                {
                    tile.gameObject.GetComponent<Tile>().setColor(START_COLOR);
                }
                else if (x == currMaze.mFinish.X && y == currMaze.mFinish.Y)
                {
                    tile.gameObject.GetComponent<Tile>().setColor(FINISH_COLOR);
                }
                else
                {
                    tile.gameObject.GetComponent<Tile>().setColor(DEFAULT_TILE_COLOR);
                }
                
                Tile instTile = Instantiate(tile, mazeContainer.transform);
                tiles.AddStims(instTile.gameObject); //on creation of tile GameObject
            }
        }
    }
    void DestroyCurrMaze()
    {
        Debug.Log("entered destroy");
        Debug.Log("dim: " + dim);
        tile.gameObject.SetActive(false);
        tile.gameObject.GetComponent<Tile>().enabled = false;

        for (int x = 0; x < dim.x; ++x)
        {
            for (int y = 0; y < dim.y; ++y)
            {
                Debug.Log("FIRST: " + x + ", " + y);
                tiles.DestroyStimGroup();
                Debug.Log(x + ", " + y);
            }
        }
        Debug.Log("maze should be gone");
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


    // ManageTileTouch - Returns correctness code
    // Return values:
    // 0 - correct and regular tile
    // 1 - correct and start tile
    // 99 - correct and finish (maze is complete)

    // 30 - previous correct tile
    // 31 - previous correct tile and start

    // 10 - rule-abiding incorrect
    // 11 - rule-abiding incorrect and start
    // 12 - rule-abiding incorrect and finish

    // 20 - rule-breaking incorrect
    // 21 - rule-breaking incorrect and start
    // 22 - rule-breaking incorrect and finish
    public static int ManageTileTouch(Tile tile)
    {
        Coords touchedCoord = tile.mCoord;

        // CORRECT DEFAULT
        Debug.Log("TOUCHED COORD: " + touchedCoord);
        Debug.Log("CURRMAZE NEXT STEP: " + currMaze.mNextStep);
        Debug.Log("CURRMAZE START: " + currMaze.mStart);
        Debug.Log("CURRMAZE FINISH: " + currMaze.mFinish);
        
        if (touchedCoord == currMaze.mNextStep && touchedCoord != currMaze.mStart && touchedCoord != currMaze.mFinish)
        {
            Debug.Log("correct");
            correctTouches++;
            // Every tile in maze is unique in path, path should NOT contain same tile twice
            currMaze.mNextStep = currMaze.mPath[currMaze.mPath.FindIndex(pathCoord => pathCoord == touchedCoord) + 1];
            return 0;
        }

        // CORRECT and START
        else if (touchedCoord == currMaze.mStart && touchedCoord == currMaze.mNextStep)
        {
            Debug.Log("**** started maze ****");
            correctTouches++;

            currMaze.mNextStep = currMaze.mPath[currMaze.mPath.FindIndex(pathCoord => pathCoord == touchedCoord) + 1];
            return 1;
        }
        
        else if (currMaze.mNextStep == currMaze.mStart && touchedCoord != currMaze.mStart)
        {
            Debug.Log("**** not pressing start tile to start maze! ****"); 

            return 2;
        }

        // CORRECT and FINISH
        else if (touchedCoord == currMaze.mFinish && touchedCoord == currMaze.mNextStep)
        {
            Debug.Log("**** finished maze! ****");
            // ++count;
            /*   Debug.Log("Changing scene to ?...");
               SceneManager.UnloadSceneAsync("MazeScene");
               SceneManager.LoadSceneAsync("MazeScene");
               SceneManager.SetActiveScene(SceneManager.GetSceneByName("MazeScene")); */
            Debug.Log("restarted maze");
            // TODO: add maze finish operations
            currMaze.mNextStep = currMaze.mStart;
            //   mazeLoaded = false;
            //  end = true; 
            correctTouches++;
            return 99;
        }

        // LAST CORRECT STEP DEFAULT
        else if (touchedCoord != currMaze.mStart && touchedCoord ==
                 currMaze.mPath[currMaze.mPath.FindIndex(pathCoord => pathCoord == currMaze.mNextStep) - 1])
        {
            Debug.Log("last correct step");
            retouchCorrect++;

            return 30;
        }

        // LAST CORRECT STEP and START
        else if (touchedCoord == currMaze.mStart && touchedCoord ==
                 currMaze.mPath[currMaze.mPath.FindIndex(pathCoord => pathCoord == currMaze.mNextStep) - 1])
        {
            Debug.Log("last correct step");
            retouchCorrect++;

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

            return 10;
        }

        // RULE-ABIDING INCORRECT and START
        else if ((currMaze.mNextStep != currMaze.mStart) && touchedCoord.isAdjacentTo(
                                                             currMaze.mPath[
                                                                 currMaze.mPath.FindIndex(pathCoord =>
                                                                     pathCoord == currMaze.mNextStep) - 1])
                                                         && touchedCoord != currMaze.mFinish)
        {
            Debug.Log("rule-abiding incorrect");
            totalErrors++;
            ruleAbidingErrors++;
            return 11;
        }

        // RULE-ABIDING INCORRECT and FINISH
        else if ((currMaze.mNextStep != currMaze.mStart) &&
                 touchedCoord.isAdjacentTo(
                     currMaze.mPath[currMaze.mPath.FindIndex(pathCoord => pathCoord == currMaze.mNextStep) - 1]))
        {
            Debug.Log("rule-abiding incorrect");
            totalErrors++;
            ruleAbidingErrors++;
            return 12;
        }

        // RULE-BREAKING INCORRECT DEFAULT
        else if (touchedCoord != currMaze.mStart && touchedCoord != currMaze.mFinish)
        {
            Debug.Log("rule-breaking incorrect");
            totalErrors++;
            ruleBreakingErrors++;
            return 20;
        }

        // RULE-BREAKING INCORRECT and START
        else if (touchedCoord != currMaze.mFinish)
        {
            Debug.Log("rule-breaking incorrect");
            totalErrors++;
            ruleBreakingErrors++;
            return 21;
        }

        // RULE-BREAKING INCORRECT and FINISH
        else
        {
            Debug.Log("rule-breaking incorrect");
            totalErrors++;
            ruleBreakingErrors++;
            return 22;
        }
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
        Debug.Log("SLIDER Y: " + SliderGo.transform.position.y);
        //SliderGo.GetComponent<RectTransform>().
        SliderGo.transform.localPosition = new Vector3(0, 450, 0);
        Debug.Log("SLIDER Y AFTER?: " + SliderGo.transform.position.y);
        SliderInitPosition = SliderGo.transform.position;
        //consider making slider stuff into USE level class
        Slider.value = 0;
        SliderHaloGo.transform.position = SliderInitPosition;
//        int numSliderSteps = CurrentTrialDef.SliderGain.Sum() + CurrentTrialDef.SliderInitial;
        sliderValueIncreaseAmount = (100f / CurrentTrialDef.MazeNumSquares) / 100f;
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
        tileSize = ConfigUiVariables.get<ConfigNumber>("tileSize");
        finalFbDuration = ConfigUiVariables.get<ConfigNumber>("finalFbDuration");
        selectObjectDuration = ConfigUiVariables.get<ConfigNumber>("selectObjectDuration");
        mazeOnsetDelay = ConfigUiVariables.get<ConfigNumber>("mazeOnsetDelay");
        correctFbDuration = ConfigUiVariables.get<ConfigNumber>("correctFbDuration");
        previousCorrectFbDuration = ConfigUiVariables.get<ConfigNumber>("previousCorrectFbDuration");
        incorrectRuleAbidingFbDuration = ConfigUiVariables.get<ConfigNumber>("incorrectRuleAbidingFbDuration");
        incorrectRuleBreakingFbDuration = ConfigUiVariables.get<ConfigNumber>("incorrectRuleBreakingFbDuration");
        variablesLoaded = true;
        //disableVariables();
    }

    private void SetGameConfigs()
    {
// MAZE GAME WIDTHS
        ///*
        // TODO: Not implemented, but this should be the maximum screen width that tiles can take up without overfilling the screen
        SCREEN_WIDTH = 4;

        // Default tile width
        TILE_WIDTH = 3f;

        //---------------------------------------------------------

        // TILE COLORS

        // Start - Light yellow
        START_COLOR = new Color(0.94f, 0.93f, 0.48f);

        // Finish - Light blue
        FINISH_COLOR = new Color(0.37f, 0.59f, 0.94f);

        // Correct - Light green
        CORRECT_COLOR = new Color(0.62f, 1f, 0.5f);

        // Prev correct - Darker green
        LAST_CORRECT_COLOR = new Color(0.2f, 0.7f, 0.5f);

        // Incorrect rule-abiding - Orange
        INCORRECT_RULEABIDING_COLOR = new Color(1f, 0.5f, 0.25f);

        // Incorrect rule-breaking - Black
        INCORRECT_RULEBREAKING_COLOR = new Color(0f, 0f, 0f);

        DEFAULT_TILE_COLOR = new Color(1, 1, 1); //MAKE CONFIGURABLE

        // FEEDBACK LENGTH IN SECONDS

        // Correct - 0.5 seconds
        CORRECT_FEEDBACK_SECONDS = correctFbDuration.value;

        // Prev correct - 0.5 seconds
        PREV_CORRECT_FEEDBACK_SECONDS = previousCorrectFbDuration.value;

        // Incorrect rule-abiding - 0.5 seconds
        INCORRECT_RULEABIDING_SECONDS = incorrectRuleAbidingFbDuration.value;

        // Incorrect rule-breaking - 1.0 seconds
        INCORRECT_RULEBREAKING_SECONDS = incorrectRuleBreakingFbDuration.value;

        //---------------------------------------------------------

        // TIMEOUT

        TIMEOUT_SECONDS = 10.0f;
        // */
        //gameConfigs.DEFAULT_TILE_COLOR = new Color(CurrentTrialDef.TileColor[0], CurrentTrialDef.TileColor[1], CurrentTrialDef.TileColor[2]);
        TileColor = new Color(1, 1, 1); //MAKE MORE CONFIGURABLE    }
    }

    private void ResetDataTrackingVariables()
    {
        totalErrors = 0;
        ruleAbidingErrors = 0;
        ruleBreakingErrors = 0;
        retouchCorrect = 0;
        correctTouches = 0;
    }
    private void disableVariables()
    {
        SliderGo.SetActive(false);
        StartButton.SetActive(false);
        SliderHalo.SetActive(false);
        SliderHaloImage.gameObject.SetActive(false);
    }
    private GameObject CreateMazeBackground(Texture2D tex, Rect rect)
    {
        GameObject mazeBackground = new GameObject("MazeBackground");
        SpriteRenderer borderSprite = mazeBackground.AddComponent<SpriteRenderer>() as SpriteRenderer;
        borderSprite.sprite = Sprite.Create(tex, new Rect(rect.x, rect.y, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
        return mazeBackground;
    }

}
