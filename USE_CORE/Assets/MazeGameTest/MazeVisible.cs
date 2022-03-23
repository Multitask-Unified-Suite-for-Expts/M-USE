using UnityEngine;
using System;
using HiddenMaze;
using UnityEngine.SceneManagement;


public class MazeVisible : MonoBehaviour
{
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
        if (!mazeLoaded) {
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

         if(count != 0)
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

          

                if(count == 0)
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
        if (touchedCoord == currMaze.mNextStep && touchedCoord != currMaze.mStart && touchedCoord != currMaze.mFinish) {
            Debug.Log("correct");
            
            // Every tile in maze is unique in path, path should NOT contain same tile twice
            currMaze.mNextStep = currMaze.mPath[currMaze.mPath.FindIndex(pathCoord => pathCoord == touchedCoord) + 1];
            return 0;
        }

        // CORRECT and START
        else if (touchedCoord == currMaze.mStart && touchedCoord == currMaze.mNextStep) {
            Debug.Log("**** started maze ****");
            
            currMaze.mNextStep = currMaze.mPath[currMaze.mPath.FindIndex(pathCoord => pathCoord == touchedCoord) + 1];
            return 1;
        }

        // CORRECT and FINISH
        else if (touchedCoord == currMaze.mFinish && touchedCoord == currMaze.mNextStep) {
            Debug.Log("**** finished maze! ****");
            ++count;
         /*   Debug.Log("Changing scene to ?...");
            SceneManager.UnloadSceneAsync("MazeScene");
            SceneManager.LoadSceneAsync("MazeScene");
            SceneManager.SetActiveScene(SceneManager.GetSceneByName("MazeScene")); */
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

