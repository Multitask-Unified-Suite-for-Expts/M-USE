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

    StimGroup tiles; // top of triallevel with other variable defs

    private static Slider slider;
    private static float sliderValueIncreaseAmount;
    private GameObject startButton;
    [HideInInspector] public ConfigNumber minObjectTouchDuration,
        itiDuration,
        finalFbDuration,
        fbDuration,
        maxObjectTouchDuration,
        selectObjectDuration,
        sliderSize,
        mazeOnsetDelay, tileSize;
    //private Button initButton;
    private Ray mouseRay;
    private int response;

    private GameObject sliderHalo;
    //private SpriteRenderer sr;
    private float startTime;
    private int max, min;
    private static int numReps, curRep;
    private int trialIndex;
    public static int totalErrors = 0, ruleAbidingErrors = 0, ruleBreakingErrors = 0, 
        retouchCorrect = 0, correctTouches = 0;

    public static Color tileColor;
    public string MaterialFilePath, MazeFilePath;
    public Vector3 ButtonPosition, ButtonScale;
    public GameObject mazeBackground;
    public Texture2D backgroundTex;
    private Image sr;
    private Vector3 sliderInitPosition;
    private bool variablesLoaded = false;
    public string mazeDefName;
    
    public int curMDim, curMNumSquares, curMNumTurns;
    public string curMPath;
    public static bool viewPath = false;
    public static bool c;
    private GameObject chosenStim;

    public override void DefineControlLevel()
    {
        //define States within this Control Level
        State StartButton = new State("StartButton");
        State LoadMaze = new State("LoadMaze");
        State GameConf = new State("GameConf");
        State MazeVis = new State("MazeVis");
        State Feedback = new State("Feedback");
        State ITI = new State("ITI");
        AddActiveStates(new List<State> {StartButton, LoadMaze, GameConf, MazeVis, Feedback, ITI});

        string[] stateNames = new string[] {"StartButton", "LoadMaze", "GameConf", "MazeVis", "Feedback", "ITI"};

        SetupTrial.SpecifyTermination(() => true, LoadMaze);

        SelectionHandler<MazeGame_StimDef> mouseHandler = new SelectionHandler<MazeGame_StimDef>();
        
        LoadMaze.AddInitializationMethod(() =>
        {
            if (CurrentTrialDef.viewPath == 1)
            {
                viewPath = true;
            }
            else
            {
                viewPath = false;
            }

            //DataTable tbl = new DataTable();

            //tbl.Columns.Add(new DataColumn("dim"));
            //tbl.Columns.Add(new DataColumn("numSquares"));
            //tbl.Columns.Add(new DataColumn("numTurns"));
            //tbl.Columns.Add(new DataColumn("mPath"));


            //string[] lines = System.IO.File.ReadAllLines(MazeFilePath + Path.DirectorySeparatorChar + "AllMazes.txt");

            //curMDim = CurrentTrialDef.mazeDim;
            //curMNumSquares = CurrentTrialDef.mazeNumSquares;
            //curMNumTurns = CurrentTrialDef.mazeNumTurns;

            //string search = "";
            //string and = "";
            //if (curMDim != null)
            //{
            //    search = search + "dim = " + curMDim.ToString();
            //    and = " AND ";
            //}

            //if (curMNumSquares != null)
            //{
            //    search = search + and + "numSquares = " + curMNumSquares.ToString();
            //    and = " AND ";
            //}

            //if (curMDim != null)
            //{
            //    search = search + and + "numTurns = " + curMDim.ToString();
            //}

            //foreach (string line in lines)
            //{
            //    var cols = line.Split('\t');

            //    DataRow dr = tbl.NewRow();
            //    for (int cIndex = 0; cIndex < 4; cIndex++)
            //    {
            //        dr[cIndex] = cols[cIndex];
            //    }

            //    tbl.Rows.Add(dr);
            //}

            //Debug.Log("TESTROWS");

            ///* DataRow[] testRows = tbl.Select();
            // foreach (DataRow row in testRows)
            // {
            //     Debug.Log(row[0].ToString() + "   " + row[1].ToString() + "   " + row[2].ToString() + "   " + row[3].ToString());
            // }

            // Debug.Log("ROWS");
            // */
            //DataRow[] rows = tbl.Select(search);
            //Debug.Log("TESTROWS" + rows);
            ////WHY DOESNT THIS WORK FOR 3???
            //foreach (DataRow row in rows)
            //{
            //    Debug.Log(row[0].ToString() + "   " + row[1].ToString() + "   " + row[2].ToString() + "   " +
            //              row[3].ToString());
            //    curMPath = row[3].ToString();
            //    ind = tbl.Rows.IndexOf(row);

            //}

            //Debug.Log("LENGTH" + rows.Length);
            trialIndex = CurrentTrialDef.TrialCount - 1;
            Debug.Log("INDEX: " + trialIndex);
            totalErrors = 0;
            ruleAbidingErrors = 0;
            ruleBreakingErrors = 0;
            retouchCorrect = 0;
            correctTouches = 0;

            if (count == 0)
            { //I DON'T THINK ANY OF THIS GETS CALLED TO GENERATE THE MAZE, IT ALL HAPPENS IN INSTANTIATECURRMAZE IN MAZEVIS
                count = 0;
                /*
                // Load maze from JSON
                TextAsset[] textMazes = Resources.LoadAll<TextAsset>("Mazes");
                Debug.Log("TextAssetSize: " + textMazes.Length);
                foreach (TextAsset textMaze in textMazes)
                {
                    string mazeJson = textMaze.text;
                    Maze mazeObj = new Maze(mazeJson);
                    Debug.Log(mazeObj);
                    mazeList.Add(mazeObj);
                }
                */
                //string[] textMazes = System.IO.File.ReadAllLines(MazeFilePath + Path.DirectorySeparatorChar + "Maze.txt");
                //Debug.Log("TextMazesSize: " + textMazes.Length);
                //Debug.Log("Text Maze 1: " + textMazes[0]);
                //string textMaze = CurrentTrialDef.MazeDef;
                //Maze mazeObj = new Maze(textMaze);
                //Debug.Log("Maze Obj:" + mazeObj);
                //mazeList.Add(mazeObj);

                //foreach (string textMaze in textMazes)
                //{
                //    Maze mazeObj = new Maze(textMaze);
                //    Debug.Log("Maze Obj:" + mazeObj);
                //    mazeList.Add(mazeObj);
                //}
                foreach (Maze maze in mazeList)
                {
                    // TODO: Here is where the maze levels can be put in order
                }
            }
            else
            {
                //RenderSettings.skybox = newMat;
            }
        });

        LoadMaze.SpecifyTermination(() => true, GameConf);


        GameConf.AddInitializationMethod(() =>
        {
            if (!variablesLoaded)
            {
                variablesLoaded = true;
                loadVariables();
            }

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

            // Default - Off-white
            // DEFAULT_TILE_COLOR = new Color(0.95f, 0.95f, 0.95f);
            //DEFAULT_TILE_COLOR = new Color(CurrentTrialDef.TileColor[0], CurrentTrialDef.TileColor[1],
             //   CurrentTrialDef.TileColor[2]);
            //---------------------------------------------------------

            DEFAULT_TILE_COLOR = new Color(CurrentTrialDef.TileColor[0], CurrentTrialDef.TileColor[1],
                CurrentTrialDef.TileColor[2]);
            // FEEDBACK LENGTH IN SECONDS

            // Correct - 0.5 seconds
            CORRECT_FEEDBACK_SECONDS = 0.5f;

            // Prev correct - 0.5 seconds
            PREV_CORRECT_FEEDBACK_SECONDS = 0.5f;

            // Incorrect rule-abiding - 0.5 seconds
            INCORRECT_RULEABIDING_SECONDS = 0.5f;

            // Incorrect rule-breaking - 1.0 seconds
            INCORRECT_RULEBREAKING_SECONDS = 1.0f;

            //---------------------------------------------------------

            // TIMEOUT

            TIMEOUT_SECONDS = 10.0f;
            // */
            //gameConfigs.DEFAULT_TILE_COLOR = new Color(CurrentTrialDef.TileColor[0], CurrentTrialDef.TileColor[1], CurrentTrialDef.TileColor[2]);
            tileColor = new Color(CurrentTrialDef.TileColor[0], CurrentTrialDef.TileColor[1],
                CurrentTrialDef.TileColor[2]);

        });

        GameConf.SpecifyTermination(() => true, StartButton);

        // define initScreen state
        MouseTracker.AddSelectionHandler(mouseHandler, StartButton);
        StartButton.AddInitializationMethod(() =>
        {
            curRep = 0;
            startButton.SetActive(true);
            RenderSettings.skybox = CreateSkybox(MaterialFilePath + Path.DirectorySeparatorChar + CurrentTrialDef.ContextName + ".png");
        });
        StartButton.AddUpdateMethod(() =>
        {

            if (InputBroker.GetMouseButtonDown(0))
            {
                mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                //initButton.OnClick
                RaycastHit hit;
                if (Physics.Raycast(mouseRay, out hit))
                {
                    if (hit.transform.name == "StartButton")
                    {
                        response = 0;
                    }
                }
            }

        });
        //  StartButton.SpecifyTermination(() => mouseHandler.SelectionMatches(initButton), MazeVis);
        StartButton.SpecifyTermination(() => response == 0, MazeVis);

        StartButton.AddDefaultTerminationMethod(() =>
        {
            slider.value = 0;
            slider.gameObject.transform.position = sliderInitPosition;
            sliderHalo.gameObject.transform.position = sliderInitPosition;
            slider.transform.localScale = new Vector3(sliderSize.value / 10f, sliderSize.value / 10f, 1f);
            sliderHalo.transform.localScale = new Vector3(sliderSize.value / 10f, sliderSize.value / 10f, 1f);

            startButton.SetActive(false);
        });

        MazeVis.AddInitializationMethod(() =>
        {
            Debug.Log(count);

            end = false;
            Debug.Log("entered inst");
            slider.value = 0;
            InstantiateCurrMaze();
        });
        MazeVis.AddUpdateMethod(() =>
        {
            if (InputBroker.GetMouseButtonDown(0))
            {
                mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(mouseRay, out hit))
                {
                    chosenStim = hit.transform.gameObject;
                    //GameObject testStim = chosenStim.transform.root.gameObject;
                    if (chosenStim.GetComponent<Tile>() != null)
                    {
                        chosenStim.GetComponent<Tile>().OnMouseDown();
                    }
                }
            }
        });
        // MazeVis.SpecifyTermination(() => end == true && count < mazeList.Count, MazeVis);
        //MazeVis.SpecifyTermination(() => end == true && count >= mazeList.Count, Feedback);
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
            sliderHalo.SetActive(true);
            // sphereCount = 0;
            sr.color = new Color(1, 1, 1, 0.2f);

            startTime = Time.time;

        });

        Feedback.AddUpdateMethod(() =>
        {
            if ((int) (10 * (Time.time - startTime)) % 4 == 0)
            {
                sr.color = new Color(1, 1, 1, 0.2f);
            }
            else if ((int) (10 * (Time.time - startTime)) % 2 == 0)
            {
                sr.color = new Color(1, 0, 0, 0.2f);
            }
        });
        Feedback.AddTimer(()=>finalFbDuration.value, ITI, () =>
        {
            sliderHalo.SetActive(false);
            slider.gameObject.SetActive(false);

        });
        //Define iti state
        ITI.AddInitializationMethod(() => { });
        ITI.SpecifyTermination(() => true, FinishTrial, () => Debug.Log("Trial" + " completed"));
        
        Debug.Log("ERRORS: " + totalErrors);
        TrialData.AddDatum("TrialNum", () => CurrentTrialDef.TrialCount);
        TrialData.AddDatum("TotalErrors", () => totalErrors);
        TrialData.AddDatum("Rule-Abiding Errors", () => ruleAbidingErrors);
        TrialData.AddDatum("Rule-Breaking Errors", () => ruleBreakingErrors);
        TrialData.AddDatum("RetouchCorrect", () => retouchCorrect);
    }

    void InstantiateCurrMaze()
    {
        slider.gameObject.SetActive(true);
        
        string[] textMaze = System.IO.File.ReadAllLines(MazeFilePath + Path.DirectorySeparatorChar + mazeDefName);
        Debug.Log("TEXT MAZE: " + textMaze[0]);
        currMaze = new Maze(textMaze[0]);
        Debug.Log("CURRENT MAZE NAME: " + mazeDefName);
        Debug.Log("CURRENT MAZE DIMENSIONS: " + currMaze.mDims);
        Debug.Log("CURRENT MAZE NUM TURNS: " + currMaze.mNumTurns);
        Debug.Log("CURRENT MAZE NUM SQUARES: " + currMaze.mNumSquares);
        sliderValueIncreaseAmount = (100f / (currMaze.mNumSquares)) / 100f;
        dim = currMaze.mDims;
        GameObject mazeCenter = GameObject.FindWithTag("Center");
        mazeLength = dim.x * TILE_WIDTH;
        mazeHeight = dim.y * TILE_WIDTH;
        Vector3 bottomLeftMazePos = mazeCenter.transform.position - (new Vector3(mazeLength / 2, mazeHeight / 2, 0));
        backgroundTex = LoadPNG(MaterialFilePath + Path.DirectorySeparatorChar + "MazeBackground.png");
        mazeBackground = CreateMazeBackground(backgroundTex, new Rect(new Vector2(0,0), new Vector2(1,1)));
        
        GameObject mazeContainer = new GameObject("MazeContainer");
        mazeBackground.transform.SetParent(mazeContainer.transform);
        mazeBackground.transform.localPosition = new Vector3(1, 0.5f, 0);
        mazeBackground.transform.localScale = new Vector3(dim.x/9f, dim.y/9f, 0);
        tile = Resources.Load<Tile>("Tile") as Tile;
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
                    Debug.Log("STARTING MAZE COLOR");
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
            slider.value += sliderValueIncreaseAmount;
        }

        if (i == 99)
        {
            slider.value += sliderValueIncreaseAmount;
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

    private void loadVariables()
    {
        slider = GameObject.Find("Slider").GetComponent<Slider>();
        sliderInitPosition = slider.gameObject.transform.position;
        Texture2D buttonTex = LoadPNG(MaterialFilePath + Path.DirectorySeparatorChar + "StartButtonImage.png");
        startButton = CreateStartButton(buttonTex, new Rect(new Vector2(0,0), new Vector2(1,1)));
        sliderHalo = GameObject.Find("SliderHalo");
        sr = sliderHalo.GetComponent<Image>();
        
        //config UI variables
        //minObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("minObjectTouchDuration");
        //maxObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("maxObjectTouchDuration");
        itiDuration = ConfigUiVariables.get<ConfigNumber>("itiDuration");
        sliderSize = ConfigUiVariables.get<ConfigNumber>("sliderSize");
        tileSize = ConfigUiVariables.get<ConfigNumber>("tileSize");
        finalFbDuration = ConfigUiVariables.get<ConfigNumber>("finalFbDuration");
        selectObjectDuration = ConfigUiVariables.get<ConfigNumber>("selectObjectDuration");
        mazeOnsetDelay = ConfigUiVariables.get<ConfigNumber>("mazeOnsetDelay");
        disableVariables();
    }

    private void disableVariables()
    {
        slider.gameObject.SetActive(false);
        startButton.SetActive(false);
        sliderHalo.SetActive(false);
        sr.gameObject.SetActive(false);
    }
    private GameObject CreateStartButton(Texture2D tex, Rect rect) //creates start button as a sprite
    {
        GameObject startButton = new GameObject("StartButton");
        SpriteRenderer sbSprite = startButton.AddComponent<SpriteRenderer>() as SpriteRenderer;
        sbSprite.sprite = Sprite.Create(tex, new Rect(rect.x, rect.y, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
        startButton.AddComponent<BoxCollider>();
        startButton.transform.localScale = ButtonScale;
        startButton.transform.position = ButtonPosition;
        return startButton;
    }
    private GameObject CreateMazeBackground(Texture2D tex, Rect rect)
    {
        GameObject mazeBackground = new GameObject("MazeBackground");
        SpriteRenderer borderSprite = mazeBackground.AddComponent<SpriteRenderer>() as SpriteRenderer;
        borderSprite.sprite = Sprite.Create(tex, new Rect(rect.x, rect.y, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
        return mazeBackground;
    }

}
