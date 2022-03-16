using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using USE_States;
using USE_ExperimentTemplate;
using CognitiveMaze_Namespace;


using HiddenMaze;
using UnityEngine.SceneManagement;

public class CognitiveMaze_TrialLevel : ControlLevel_Trial_Template
{
    public List<Maze> mazeList = new List<Maze>();
    static bool end; 
    private int dim;

    //game configs variables
    public float SCREEN_WIDTH;
    public float TILE_WIDTH;

    public Color START_COLOR;
    public Color FINISH_COLOR;
    public Color CORRECT_COLOR;
    public Color LAST_CORRECT_COLOR;
    public Color INCORRECT_RULEABIDING_COLOR;
    public Color INCORRECT_RULEBREAKING_COLOR;
    public Color DEFAULT_TILE_COLOR;

    public float CORRECT_FEEDBACK_SECONDS;
    public float PREV_CORRECT_FEEDBACK_SECONDS;
    public float INCORRECT_RULEABIDING_SECONDS;
    public float INCORRECT_RULEBREAKING_SECONDS;

    public float TIMEOUT_SECONDS;


    //MazeVis Variables
    public TileRow[] tileRows;

    public static GameObject mazeListObj;
    public LoadMazeList mazeListScript;
    public GameConfigs gameConfigs;
    public static Maze currMaze;
    private bool mazeLoaded = false;
    private static int count;
    private Tile tile;

    public override void DefineControlLevel()
    {

        //define States within this Control Level
        State StartButton = new State("StartButton");
        State LoadMaze = new State("LoadMaze");
        State GameConf = new State("GameConf");
        State MazeVis = new State("MazeVis");
        State Feedback = new State("Feedback");
        State ITI = new State("ITI");
        AddActiveStates(new List<State> { StartButton, LoadMaze, GameConf, MazeVis, Feedback, ITI });

        string[] stateNames = new string[] { "StartButton", "LoadMaze", "GameConf", "MazeVis", "Feedback", "ITI" };

     /*   AddInitializationMethod(() =>
        {
            if (!variablesLoaded)
            {
                variablesLoaded = true;
                loadVariables();
            }
        }); 

        SetupTrial.AddInitializationMethod(() =>
        {

        });
        */
        SetupTrial.SpecifyTermination(() => true, StartButton);


        // define initScreen state
        StartButton.AddInitializationMethod(() =>
        {
          
        });


        StartButton.AddUpdateMethod(() =>
        {
           
        });

        StartButton.SpecifyTermination(() => true, LoadMaze);
        StartButton.AddDefaultTerminationMethod(() =>
        {

        });

        // Define stimOn state
        LoadMaze.AddInitializationMethod(() =>
        {


        // Load maze from JSON
        TextAsset[] textMazes = Resources.LoadAll<TextAsset>("Mazes");

        foreach (TextAsset textMaze in textMazes)
        {
            string mazeJson = textMaze.text;
            Maze mazeObj = new Maze(mazeJson);
            Debug.Log(mazeObj);
            mazeList.Add(mazeObj);
        }

        foreach (Maze maze in mazeList)
        {
            // TODO: Here is where the maze levels can be put in order
        }
        });


        // LoadMaze.AddUpdateMethod(() =>
        // {
        //
        // });

        LoadMaze.SpecifyTermination(() => true, GameConf);


        GameConf.AddInitializationMethod(() =>
        {
            // MAZE GAME WIDTHS

            // TODO: Not implemented, but this should be the maximum screen width that tiles can take up without overfilling the screen
            SCREEN_WIDTH = 4;

            // Default tile width
            TILE_WIDTH = 0.5f;

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
            DEFAULT_TILE_COLOR = new Color(0.95f, 0.95f, 0.95f);

            //---------------------------------------------------------

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
        });

        GameConf.SpecifyTermination(() => true, MazeVis);

        MazeVis.AddInitializationMethod(() =>
        {
            //  if (!mazeLoaded)
            // {
            //   mazeLoaded = true;
                 end = false; 
                InstantiateCurrMaze();
                // TODO: Currently, this will only load one maze
           // }
        });

         MazeVis.AddUpdateMethod(() =>
         {
            
         });
        MazeVis.SpecifyTermination(() => end == true && count < mazeList.Count, MazeVis);
        MazeVis.SpecifyTermination(() => end == true && count >= mazeList.Count, Feedback);
        MazeVis.AddDefaultTerminationMethod(() =>
        {
            DestroyCurrMaze();
            end = false;
        });

        Feedback.AddInitializationMethod(() =>
        {
            Debug.Log("the end");

        });

        Feedback.AddUpdateMethod(() =>
        {

        });
        Feedback.AddTimer(2f, ITI, () =>
        {

        });

        //Define iti state
        ITI.AddInitializationMethod(() =>
        {

        });
        ITI.SpecifyTermination(() => true, FinishTrial, () => Debug.Log("Trial" + " completed"));
/*
        TrialData.AddDatum("TrialNum", () => trialNum);
        TrialData.AddDatum("TrialID", () => CurrentTrialDef.TrialID);
        TrialData.AddDatum("TouchedObjects", () => touchedObj);
        TrialData.AddDatum("SlotError", () => slotError);
        TrialData.AddDatum("RepetitionError", () => repetitionError);
        TrialData.AddDatum("TotalErrors", () => totalErrors); */


    }

    void InstantiateCurrMaze()
    {
        currMaze = mazeList[count];
        Debug.Log(count);

        if (count != 0)
        {
            //   DestroyCurrMaze();
           // tile.gameObject.SetActive(true);

        }

        dim = currMaze.mConfigs.dim;

        tileRows = new TileRow[dim];
        Debug.Log("DIM: " + dim);


        GameObject mazeCenter = GameObject.FindWithTag("Center");

        float mazeWidth = dim * TILE_WIDTH;
        Vector3 bottomLeftMazePos = mazeCenter.transform.position - (new Vector3(mazeWidth / 2, mazeWidth / 2, 0));
        tile = Resources.Load<Tile>("Prefabs/Tile") as Tile;

        for (int x = 0; x < dim; ++x)
        {
            for (int y = 0; y < dim; ++y)
            {
                if(count != 0)
                {
                    //tile.gameObject.SetActive(true);
                   // tile.gameObject.GetComponent<Tile>().enabled = true;
                }


            //    if (count == 0)
              //  {
                    float displaceX = x * TILE_WIDTH;
                    float displaceY = y * TILE_WIDTH;
                    Vector3 newTilePosition = bottomLeftMazePos + new Vector3(displaceX, displaceY, 0);
                    // Instantiate the tile

                    tile.transform.position = newTilePosition;
                    tile.mCoord = new Coords(x, y);

                    // Instantiate the row and assign the tile in the row
                    tileRows[x] = new TileRow(dim);
                   // tileRows[x].mTiles[y] = tile;

               // }

                Tile instTile = Instantiate(tile);
                Renderer tileRend = instTile.GetComponent<Renderer>();
                


                Color tileColor;

                if (x == currMaze.mStart.X && y == currMaze.mStart.Y)
                {
                    tileColor = START_COLOR;

                }
                else if (x == currMaze.mFinish.X && y == currMaze.mFinish.Y)
                {
                    tileColor = FINISH_COLOR;

                }
                else
                {
                    tileColor = DEFAULT_TILE_COLOR;
                }

                tileRend.material.SetColor("_BaseColor", tileColor);

                instTile.transform.SetParent(this.transform);
              //  instTile.gameObject.SetActive(false);
                tileRows[x].mTiles[y] = instTile;
            }
        }
    }


    void DestroyCurrMaze()
    {
        Debug.Log("entered destroy");
        Debug.Log("dim: " + dim);
        tile.gameObject.SetActive(false);
        tile.gameObject.GetComponent<Tile>().enabled = false;
      //  Destroy(tile);

        for (int x = 0; x < dim; ++x)
        {
            for (int y = 0; y < dim; ++y)
            {
                Debug.Log("FIRST: " + x + ", " + y);
                // tileRows[x].mTiles[y].GetComponent<Tile>().enabled = false;
               // Tile temp = tileRows[x].mTiles[y];
               // temp.gameObject.SetActive(false);
                Destroy(tileRows[x].mTiles[y]);
                Debug.Log(x + ", " + y);

            }
            //tileRows[x].gameObject.SetActive(false);
        }
        Debug.Log("maze should be gone");
    }

    public static void setEnd(int i)
    {
        if(i == 99)
        {
            ++count;
            end = true; 
        }
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
        if (touchedCoord == currMaze.mNextStep && touchedCoord != currMaze.mStart && touchedCoord != currMaze.mFinish)
        {
            Debug.Log("correct");

            // Every tile in maze is unique in path, path should NOT contain same tile twice
            currMaze.mNextStep = currMaze.mPath[currMaze.mPath.FindIndex(pathCoord => pathCoord == touchedCoord) + 1];
            return 0;
        }

        // CORRECT and START
        else if (touchedCoord == currMaze.mStart && touchedCoord == currMaze.mNextStep)
        {
            Debug.Log("**** started maze ****");

            currMaze.mNextStep = currMaze.mPath[currMaze.mPath.FindIndex(pathCoord => pathCoord == touchedCoord) + 1];
            return 1;
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
            return 99;
        }

        // LAST CORRECT STEP DEFAULT
        else if (touchedCoord != currMaze.mStart && touchedCoord == currMaze.mPath[currMaze.mPath.FindIndex(pathCoord => pathCoord == currMaze.mNextStep) - 1])
        {
            Debug.Log("last correct step");
            return 30;
        }

        // LAST CORRECT STEP and START
        else if (touchedCoord == currMaze.mStart && touchedCoord == currMaze.mPath[currMaze.mPath.FindIndex(pathCoord => pathCoord == currMaze.mNextStep) - 1])
        {
            Debug.Log("last correct step");
            return 31;
        }

        // RULE-ABIDING INCORRECT DEFAULT
        // Check if this isn't the first touch and the touch was adjacent to the previous correct tile
        // In order for something to be a rule-abiding touch, there must have already been at least one correct touch on the start tile
        else if ((currMaze.mNextStep != currMaze.mStart) && touchedCoord.isAdjacentTo(currMaze.mPath[currMaze.mPath.FindIndex(pathCoord => pathCoord == currMaze.mNextStep) - 1])
                  && touchedCoord != currMaze.mStart
                  && touchedCoord != currMaze.mFinish)
        {
            Debug.Log("rule-abiding incorrect");
            return 10;
        }

        // RULE-ABIDING INCORRECT and START
        else if ((currMaze.mNextStep != currMaze.mStart) && touchedCoord.isAdjacentTo(currMaze.mPath[currMaze.mPath.FindIndex(pathCoord => pathCoord == currMaze.mNextStep) - 1])
                  && touchedCoord != currMaze.mFinish)
        {
            Debug.Log("rule-abiding incorrect");
            return 11;
        }

        // RULE-ABIDING INCORRECT and FINISH
        else if ((currMaze.mNextStep != currMaze.mStart) && touchedCoord.isAdjacentTo(currMaze.mPath[currMaze.mPath.FindIndex(pathCoord => pathCoord == currMaze.mNextStep) - 1]))
        {
            Debug.Log("rule-abiding incorrect");
            return 12;
        }

        // RULE-BREAKING INCORRECT DEFAULT
        else if (touchedCoord != currMaze.mStart && touchedCoord != currMaze.mFinish)
        {
            Debug.Log("rule-breaking incorrect");
            return 20;
        }

        // RULE-BREAKING INCORRECT and START
        else if (touchedCoord != currMaze.mFinish)
        {
            Debug.Log("rule-breaking incorrect");
            return 21;
        }

        // RULE-BREAKING INCORRECT and FINISH
        else
        {
            Debug.Log("rule-breaking incorrect");
            return 22;
        }
    }

}

    /*  public override void DefineControlLevel()
      {
          Debug.Log("test");
      }

      public TileRow[] tileRows;
      public static GameObject mazeListObj;
      public LoadMazeList mazeListScript;
      public GameConfigs gameConfigs;
      public Maze currMaze;
      private bool mazeLoaded = false;
      private int count;
      private Tile tile;

      void Awake()
      {
          mazeListObj = GameObject.FindWithTag("Maze List");
          mazeListScript = mazeListObj.GetComponent<LoadMazeList>();
          count = 0;
      }

      void Start()
      {
          GameObject gameConfigsObj = GameObject.FindWithTag("Game Configs");
          gameConfigs = gameConfigsObj.GetComponent<GameConfigs>();

          // A way to make levels would be to create an "update curr maze" func 
          //  that deletes the current visible maze, moves curr maze to next maze, and creates next maze
          // TODO: Currently, this will only load one maze
          //  currMaze = mazeListScript.mazeList[0];
      }

      void Update()
      {
          // If there isn't currently a maze laoded, load a maze
          // This is necessary due to colors not being instantiated in Start(), 
          //  so the maze cannot be properly constructed until Update()
          if (!mazeLoaded)
          {
              mazeLoaded = true;
              InstantiateCurrMaze();
              // TODO: Currently, this will only load one maze
          }
      }

      // InstantiateCurrMaze - Calculates maze tile positions given dimension of maze, tile width, maze center position
      //                       Instantiates tile prefabs and colors them according to start, finish, and default colors
      //                       Tiles are put into tileRows array (an array of TileRow objects, which are arrays of Tiles)
      void InstantiateCurrMaze()
      {
          currMaze = mazeListScript.mazeList[count];
          Debug.Log(count);

          if (count != 0)
          {
              DestroyCurrMaze();
          }

          int dim = currMaze.mConfigs.dim;

          float TILE_WIDTH = gameConfigs.TILE_WIDTH;
          tileRows = new TileRow[dim];

          GameObject mazeCenter = GameObject.FindWithTag("Center");

          float mazeWidth = dim * TILE_WIDTH;
          Vector3 bottomLeftMazePos = mazeCenter.transform.position - (new Vector3(mazeWidth / 2, mazeWidth / 2, 0));

          for (int x = 0; x < dim; ++x)
          {
              for (int y = 0; y < dim; ++y)
              {



                  if (count == 0)
                  {
                      float displaceX = x * TILE_WIDTH;
                      float displaceY = y * TILE_WIDTH;
                      Vector3 newTilePosition = bottomLeftMazePos + new Vector3(displaceX, displaceY, 0);
                      // Instantiate the tile
                      tile = Resources.Load<Tile>("Prefabs/Tile") as Tile;

                      tile.transform.position = newTilePosition;
                      tile.mCoord = new Coords(x, y);

                      // Instantiate the row and assign the tile in the row
                      tileRows[x] = new TileRow(dim);
                      tileRows[x].mTiles[y] = tile;

                  }

                  Tile instTile = Instantiate(tile);
                  Renderer tileRend = instTile.GetComponent<Renderer>();



                  Color tileColor;

                  if (x == currMaze.mStart.X && y == currMaze.mStart.Y)
                  {
                      tileColor = gameConfigs.START_COLOR;

                  }
                  else if (x == currMaze.mFinish.X && y == currMaze.mFinish.Y)
                  {
                      tileColor = gameConfigs.FINISH_COLOR;

                  }
                  else
                  {
                      tileColor = gameConfigs.DEFAULT_TILE_COLOR;
                  }

                  tileRend.material.SetColor("_BaseColor", tileColor);

                  instTile.transform.SetParent(this.transform);
              }
          }
      }


      void DestroyCurrMaze()
      {

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
      public int ManageTileTouch(Tile tile)
      {
          Coords touchedCoord = tile.mCoord;

          // CORRECT DEFAULT
          if (touchedCoord == currMaze.mNextStep && touchedCoord != currMaze.mStart && touchedCoord != currMaze.mFinish)
          {
              Debug.Log("correct");

              // Every tile in maze is unique in path, path should NOT contain same tile twice
              currMaze.mNextStep = currMaze.mPath[currMaze.mPath.FindIndex(pathCoord => pathCoord == touchedCoord) + 1];
              return 0;
          }

          // CORRECT and START
          else if (touchedCoord == currMaze.mStart && touchedCoord == currMaze.mNextStep)
          {
              Debug.Log("**** started maze ****");

              currMaze.mNextStep = currMaze.mPath[currMaze.mPath.FindIndex(pathCoord => pathCoord == touchedCoord) + 1];
              return 1;
          }

          // CORRECT and FINISH
          else if (touchedCoord == currMaze.mFinish && touchedCoord == currMaze.mNextStep)
          {
              Debug.Log("**** finished maze! ****");
              ++count;
              //   Debug.Log("Changing scene to ?...");
                // SceneManager.UnloadSceneAsync("MazeScene");
                 //SceneManager.LoadSceneAsync("MazeScene");
                 //SceneManager.SetActiveScene(SceneManager.GetSceneByName("MazeScene")); 
              Debug.Log("restarted maze");
              // TODO: add maze finish operations
              currMaze.mNextStep = currMaze.mStart;
              mazeLoaded = false;
              return 99;
          }

          // LAST CORRECT STEP DEFAULT
          else if (touchedCoord != currMaze.mStart && touchedCoord == currMaze.mPath[currMaze.mPath.FindIndex(pathCoord => pathCoord == currMaze.mNextStep) - 1])
          {
              Debug.Log("last correct step");
              return 30;
          }

          // LAST CORRECT STEP and START
          else if (touchedCoord == currMaze.mStart && touchedCoord == currMaze.mPath[currMaze.mPath.FindIndex(pathCoord => pathCoord == currMaze.mNextStep) - 1])
          {
              Debug.Log("last correct step");
              return 31;
          }

          // RULE-ABIDING INCORRECT DEFAULT
          // Check if this isn't the first touch and the touch was adjacent to the previous correct tile
          // In order for something to be a rule-abiding touch, there must have already been at least one correct touch on the start tile
          else if ((currMaze.mNextStep != currMaze.mStart) && touchedCoord.isAdjacentTo(currMaze.mPath[currMaze.mPath.FindIndex(pathCoord => pathCoord == currMaze.mNextStep) - 1])
                    && touchedCoord != currMaze.mStart
                    && touchedCoord != currMaze.mFinish)
          {
              Debug.Log("rule-abiding incorrect");
              return 10;
          }

          // RULE-ABIDING INCORRECT and START
          else if ((currMaze.mNextStep != currMaze.mStart) && touchedCoord.isAdjacentTo(currMaze.mPath[currMaze.mPath.FindIndex(pathCoord => pathCoord == currMaze.mNextStep) - 1])
                    && touchedCoord != currMaze.mFinish)
          {
              Debug.Log("rule-abiding incorrect");
              return 11;
          }

          // RULE-ABIDING INCORRECT and FINISH
          else if ((currMaze.mNextStep != currMaze.mStart) && touchedCoord.isAdjacentTo(currMaze.mPath[currMaze.mPath.FindIndex(pathCoord => pathCoord == currMaze.mNextStep) - 1]))
          {
              Debug.Log("rule-abiding incorrect");
              return 12;
          }

          // RULE-BREAKING INCORRECT DEFAULT
          else if (touchedCoord != currMaze.mStart && touchedCoord != currMaze.mFinish)
          {
              Debug.Log("rule-breaking incorrect");
              return 20;
          }

          // RULE-BREAKING INCORRECT and START
          else if (touchedCoord != currMaze.mFinish)
          {
              Debug.Log("rule-breaking incorrect");
              return 21;
          }

          // RULE-BREAKING INCORRECT and FINISH
          else
          {
              Debug.Log("rule-breaking incorrect");
              return 22;
          }
      }
      
}

*/




