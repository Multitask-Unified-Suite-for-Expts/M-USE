using System.Collections;
using System.Collections.Generic;
using HiddenMaze;
using MazeGame_Namespace;
using UnityEngine;
using UnityEngine.UI;

public class MazeManager:MonoBehaviour
{
    public int currentPathIndex;
    public int consecutiveErrors;

    public List<GameObject> selectedTilesInPathGO = new List<GameObject>();
    public List<GameObject> selectedTilesGO = new List<GameObject>();

    public bool returnToLast;
    public bool erroneousReturnToLast;
    public bool correctSelection;

    public bool startedMaze;
    public bool finishedMaze;
    public Maze currentMaze;

    public float mazeDuration;
    public float choiceDuration;
    public float mazeStartTime;
    public float choiceStartTime;

    public GameObject currentTilePositionGO;
    
    
    // Maze Loading Variables
    [HideInInspector] public int[] MazeNumSquares;
    [HideInInspector] public int[] MazeNumTurns;
    [HideInInspector] public Vector2[] MazeDims;
    [HideInInspector] public string[] MazeStart;
    [HideInInspector] public string[] MazeFinish;
    [HideInInspector] public string[] MazeName;
    [HideInInspector] public string[] MazeString;

    //
    
    
    
    public MazeGame_TrialLevel mgTL;
    private int mIndex;
    
    
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
            if(mazeStartTime != 0)
                mazeDuration = Time.unscaledTime - mazeStartTime;
            if(choiceStartTime != 0)
                choiceDuration = Time.unscaledTime - choiceStartTime;


    }

    private void CreateMaze(MazeDef CurrentMazeDef)
    {
        
    }

    public void ResetSelectionClassifications()
    {
        correctSelection = false;
        returnToLast = false;
        erroneousReturnToLast = false;
    }
    public void ResetMazeVariables()
    {
        currentPathIndex = 0;
        consecutiveErrors = 0;

        selectedTilesInPathGO.Clear();
        selectedTilesGO.Clear();
        
        startedMaze = false;
        finishedMaze = false;

        mazeDuration = 0;
        choiceDuration = 0;
        mazeStartTime = 0;
        
        correctSelection = false;
        returnToLast = false;
        erroneousReturnToLast = false;
    }
    public void LoadTextMaze(MazeGame_BlockDef mgBD)
    {
        currentMaze = new Maze(mgBD.MazeDef);
        
    }

    private void RemovePathProgressFollowingError()
    {
        if (consecutiveErrors == 1)
        {
            Debug.LogWarning("CURRENT POS: " + currentTilePositionGO.name + " mnextstep: " + currentMaze.mNextStep);
            currentMaze.mNextStep = currentMaze.mPath[currentPathIndex];
            currentTilePositionGO.GetComponent<Tile>().setColor(currentTilePositionGO.GetComponent<Tile>().InitialTileColor);
            selectedTilesInPathGO.RemoveAt(selectedTilesInPathGO.Count - 1);
            if (selectedTilesInPathGO.Count > 0)
                currentTilePositionGO = selectedTilesInPathGO.Count > 0 ? selectedTilesInPathGO[selectedTilesInPathGO.Count - 1] : null;
            
            Debug.LogWarning("CURRENT POS: " + currentTilePositionGO.name + " mnextstep: " + currentMaze.mNextStep);

        }
    }
    public int ManageTileTouch(Tile tile)
    {
        GameObject TileGO = tile.gameObject;
        
        //var touchedCoord = tile.mCoord;
        // ManageTileTouch - Returns correctness code
        // Return values:
        // 1 - correct tile touch
        // 2 - last correct retouch
        // 10 - rule-abiding incorrect
        // 20 - rule-breaking incorrect (failed to start on start tile, failed to return to last correct after error, diagonal/skips)

        // RULE - BREAKING ERROR : NOT PRESSING START

        if (!startedMaze)
        {
            mgTL.HandleRuleBreakingError(0);
            return 20;
        }
        
        if (tile.mCoord.chessCoord == currentMaze.mNextStep)
        {

            // Provides feedback for last correct tile touch and updates next tile step
            if (TileGO == currentTilePositionGO)
            {
               // currentMaze.mNextStep = currentMaze.mPath[currentMaze.mPath.FindIndex(pathCoord => pathCoord == tile.mCoord.chessCoord) + 1];
                mgTL.HandleRetouchCorrect(currentPathIndex);
                return 2;
            }

            mgTL.HandleCorrectTouch();
            
            // Helps set progress on the experimenter display
            selectedTilesInPathGO.Add(TileGO);
            currentTilePositionGO = TileGO;
            currentPathIndex++;
            
            // Sets the NextStep if the maze isn't finished
            if (!tile.isFinishTile)
                currentMaze.mNextStep = currentMaze.mPath[currentPathIndex];
            else
                finishedMaze = true; // Finished the Maze

            return 1;
            
        }
        // RULE-ABIDING ERROR ( and RULE ABIDING, BUT PERSEVERATIVE)
        Debug.Log("CURRENT PATH INDEX BEFORE RULE ABIDING: " + currentPathIndex);
        if ( currentTilePositionGO.GetComponent<Tile>().AdjacentTiles.Contains(TileGO) && !selectedTilesInPathGO.Contains(TileGO))
        {
            if (consecutiveErrors > 0) //Perseverative Error
            {
                mgTL.HandleRuleBreakingError(currentPathIndex);
                return 20;
            }
            
            mgTL.HandleRuleAbidingError(currentPathIndex);
            RemovePathProgressFollowingError();
            return 10;
        }

        // RULE BREAKING BACKTRACK ERROR OR ERRONEOUS RETOUCH OF LAST CORRECT TILE
        if (selectedTilesInPathGO.Contains(TileGO))
        {
            if (tile.gameObject.Equals(selectedTilesInPathGO[currentPathIndex]))
            {
                mgTL.HandleRetouchErroneous(currentPathIndex);
                return 2;
            }

            mgTL.HandleBackTrackError(currentPathIndex);
            mgTL.HandleRuleBreakingError(currentPathIndex);

            // Set the correct next step to the last correct tile touch
            RemovePathProgressFollowingError();
            return 20;
        }

        // RULE BREAKING TOUCH
        mgTL.HandleRuleBreakingError(currentPathIndex);
        RemovePathProgressFollowingError();
        return 20;
    }
    
    
    
}
