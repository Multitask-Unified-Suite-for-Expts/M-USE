using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms.VisualStyles;
using HiddenMaze;
using MazeGame_Namespace;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using UnityEngine.WSA;
using USE_StimulusManagement;
using USE_UI;

public class MazeManager:MonoBehaviour 
{
    // This script is attached to the MazeContainer GameObject

    // M-USE Fields
    public MazeGame_TrialLevel mgTrialLevel;
    public MazeGame_TaskDef mgTaskDef;
    public MazeGame_TrialDef mgTrialDef;

    
    public GameObject tilePrefab; // Set in Editor
    
    // Tile Container
    public TileSettings tileSettings; // Set in Editor
    public GameObject tileContainerGO; // Set in Editor
    public GameObject tileConnectorsContainerGO; // Set in Editor
    public GameObject landmarksContainerGO; // Set in Editor


    // Maze GameObjects
    public GameObject mazeBackgroundGO; // Set in Editor
    
    
    [HideInInspector] public int currentPathIndex;
    [HideInInspector] public int consecutiveErrors;

    [HideInInspector] public List<GameObject> selectedTilesInPathGO = new List<GameObject>();
    [HideInInspector] public List<GameObject> selectedTilesGO = new List<GameObject>();

    [HideInInspector] public GameObject startTileGO;
    [HideInInspector] public GameObject finishTileGO;

    [HideInInspector] public bool creatingSquareMaze;
    [HideInInspector] public bool viewPath;
    [HideInInspector] public bool darkenNonPathTiles;
    [HideInInspector] public bool returnToLast;
    [HideInInspector] public bool erroneousReturnToLast;
    [HideInInspector] public bool correctSelection;
    [HideInInspector] public bool freePlay;
    [HideInInspector] public bool outOfMoves;
    [HideInInspector] public bool startedMaze;
    [HideInInspector] public bool finishedMaze;
    [HideInInspector] public bool tileConnectorsLoaded;
    [HideInInspector] public Maze currentMaze;

    [HideInInspector] public GameObject latestConnection;

    [HideInInspector] public float mazeDuration;
    [HideInInspector] public float choiceDuration;
    [HideInInspector] public float mazeStartTime;
    [HideInInspector] public float choiceStartTime;

    [HideInInspector] public GameObject currentTilePositionGO;
    public GridLayoutGroup tileContainerGridLayoutGroup; 


    // Update is called once per frame
    void Update()
    {
        if (mazeStartTime != 0)
            mazeDuration = Time.unscaledTime - mazeStartTime;
        if (choiceStartTime != 0)
            choiceDuration = Time.unscaledTime - choiceStartTime;
    }

    public void Initialize(MazeGame_TrialLevel trialLevel, MazeGame_TrialDef trialDef, MazeGame_TaskDef taskDef)
    {
        mgTrialLevel = trialLevel;
        mgTrialDef = trialDef;
        mgTaskDef = taskDef;
    }
    public StimGroup CreateMaze(Texture2D tileTex, Texture2D mazeBgTex)
    {
        StimGroup tiles = new StimGroup("Tiles");

        
        if(tileSettings == null) // Instantiate a single instance of tile Settings that is applied to every tile
            tileSettings = ScriptableObject.CreateInstance<TileSettings>();
        
        SetTileSettings();
        freePlay = currentMaze.freePlay;

        
        if (creatingSquareMaze)
        {
            tileContainerGridLayoutGroup.enabled = true;
            tileContainerGridLayoutGroup.constraintCount = (int)currentMaze.mDims.x; // Restrict the grid layout by number of columns
            for (var x = (int)currentMaze.mDims.x -1; x >= 0 ; x--)
            for (var y = (int)currentMaze.mDims.y -1; y >= 0; y--)
            {
                GameObject tileGO = InitializeTile(tileTex, x, y, tiles);
            }
            
            float mazeWidth = ((tileContainerGridLayoutGroup.cellSize.x + tileContainerGridLayoutGroup.spacing.x) * currentMaze.mDims.x) + tileContainerGridLayoutGroup.spacing.x;
            float mazeHeight = ((tileContainerGridLayoutGroup.cellSize.y + tileContainerGridLayoutGroup.spacing.y) * currentMaze.mDims.y) + tileContainerGridLayoutGroup.spacing.y;
            
            InitializeMazeBackground(mazeBgTex, mazeWidth, mazeHeight);
            AssignAdjacentTiles(tiles, tileContainerGridLayoutGroup.cellSize.x + tileContainerGridLayoutGroup.spacing.x, tileContainerGridLayoutGroup.cellSize.y + tileContainerGridLayoutGroup.spacing.y);

        }
        else
        {
            tileContainerGridLayoutGroup.enabled = false;
            List<int> customMazeDims = currentMaze.customDims;
            tileContainerGO.SetActive(true);
            tileContainerGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
            
            float xOffset = tileContainerGO.GetComponent<RectTransform>().rect.width/(customMazeDims.Max()+1);
            float yOffset = tileContainerGO.GetComponent<RectTransform>().rect.height/(customMazeDims.Count+1);



            for (int row = 0; row < customMazeDims.Count; row++)
            {
                int numCircles = customMazeDims[row]; // Adjust number of circles per row

                for (int col = 0; col < numCircles; col++) // Vary number of circles based on row
                {
                    float x, y;
                    if (numCircles % 2 == 0) // for rows with even number of circles 
                         x = (-(xOffset * 0.5f) - (((numCircles / 2) - 1)) * xOffset) + (xOffset * col);
                    else
                        x = (-((numCircles / 2f) - 0.5f) * xOffset) + (xOffset * col);
                    if (customMazeDims.Count % 2 == 0)
                        y = -(yOffset * 0.5f) + (-yOffset * ((customMazeDims.Count / 2)-1)) + (row * yOffset);
                    else
                         y = (-yOffset * (customMazeDims.Count / 2)) + (row * yOffset);


                    string tileName = GetChessCoordName(col, row);
                    Debug.LogWarning("TILE NAME: " +  tileName);
                    if (mgTrialDef.Blockades.Contains(tileName))
                    {
                        Debug.LogWarning("this is being skipped: " +  tileName);
                        continue;
                    }
                    GameObject tileGO = InitializeTile(tileTex, col, row, tiles);
                    tileGO.transform.localPosition = new Vector2(x, y);
                }
            }

            

            AssignAdjacentTiles(tiles, xOffset, yOffset);
            mgTrialLevel.DeactivateChildren(tileConnectorsContainerGO);

            if (mgTrialDef.Landmarks.Count > 0)
                CreateLandmarks(mgTrialDef.Landmarks);

        }
        currentMaze.mName = $"{currentMaze.mStart}_{currentMaze.mFinish}";
        AssignFlashingTiles(currentMaze, tiles);
        return tiles;
    }

    // Initialize Maze GameObjects
    private void InitializeMazeBackground(Texture2D mazeBgTex, float mazeWidth, float mazeHeight)
    {
        mazeBackgroundGO.GetComponent<RectTransform>().sizeDelta = new Vector2(mazeWidth,mazeHeight);
        Material mazeBgMaterial = new Material(mazeBackgroundGO.GetComponent<Image>().material);
        mazeBgMaterial.mainTexture = mazeBgTex;
        mazeBackgroundGO.GetComponent<Image>().material = mazeBgMaterial;
        mazeBackgroundGO.SetActive(false);
    }
    private GameObject InitializeTile(Texture2D tileTex, int col, int row, StimGroup tiles)
    {
        GameObject tileGO = Instantiate(tilePrefab, tileContainerGO.transform);
        if(!creatingSquareMaze) 
            tileGO.GetComponent<SpriteRenderer>().sprite =  Resources.Load<Sprite>("Star");
        
        tileGO.name = GetChessCoordName(col, row);
        tileGO.transform.localScale = mgTaskDef.TileSize * tileGO.transform.localScale;
        Tile tile = tileGO.AddComponent<Tile>();
        tile.Initialize(tileSettings, this);

        StimDef tileStimDef = new StimDef(tiles, tileGO);
        tile.mCoord = new Coords(tileGO.name);

        
        AssignInitialTileColor(tile, currentMaze);
        AssignSliderValue(tile);

        tileGO.SetActive(false);
        return tileGO;
    }
    private void CreateTileConnectors(GameObject startTile, GameObject endTile, Vector2 startTilePos, Vector2 endTilePos)
    {
        if (tileConnectorsContainerGO.transform.Find($"{startTile.name}{endTile.name}") == null)
        {
            USE_Line line = new USE_Line(mgTrialLevel.MG_CanvasGO.GetComponent<Canvas>(), endTilePos, startTilePos, Color.white, $"{endTile.name}{startTile.name}");
            line.LineRenderer.sprite = Resources.Load<Sprite>("dotted_road"); 
            line.LineRenderer.LineThickness = 30;
            line.LineGO.transform.SetParent(tileConnectorsContainerGO.transform);
            line.LineGO.SetActive(true);
            line.LineGO.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            // Set the new game object as the first sibling
            line.LineGO.transform.SetAsFirstSibling();
        }
    }

    
    // Configure Tile Fields
    public void SetTileSettings()
    {
        // Default - White
        tileSettings.defaultTileColor = new Color(mgTrialDef.DefaultTileColor[0], mgTrialDef.DefaultTileColor[1], mgTrialDef.DefaultTileColor[2], 1);

        // Start - Light yellow
        tileSettings.startColor = new Color(mgTaskDef.StartColor[0], mgTaskDef.StartColor[1], mgTaskDef.StartColor[2], 1);

        // Finish - Light blue
        tileSettings.finishColor = new Color(mgTaskDef.FinishColor[0], mgTaskDef.FinishColor[1], mgTaskDef.FinishColor[2], 1);

        // Correct - Light green
        tileSettings.correctColor = new Color(mgTaskDef.CorrectColor[0], mgTaskDef.CorrectColor[1], mgTaskDef.CorrectColor[2]);

        // Prev correct - Darker green
        tileSettings.prevCorrectColor = new Color(mgTaskDef.LastCorrectColor[0], mgTaskDef.LastCorrectColor[1], mgTaskDef.LastCorrectColor[2]);

        // Incorrect rule-abiding - Orange
        tileSettings.incorrectRuleAbidingColor = new Color(mgTaskDef.IncorrectRuleAbidingColor[0], mgTaskDef.IncorrectRuleAbidingColor[1],
            mgTaskDef.IncorrectRuleAbidingColor[2]);

        // Incorrect rule-breaking - Black
        tileSettings.incorrectRuleBreakingColor = new Color(mgTaskDef.IncorrectRuleBreakingColor[0], mgTaskDef.IncorrectRuleBreakingColor[1],
            mgTaskDef.IncorrectRuleBreakingColor[2]);

        // FEEDBACK LENGTH IN SECONDS

        // Correct - 0.5 seconds
        tileSettings.correctFeedbackSeconds = mgTrialLevel.correctFbDuration.value;

        // Prev correct - 0.5 seconds
        tileSettings.prevCorrectFeedbackSeconds = mgTrialLevel.previousCorrectFbDuration.value;

        // Incorrect rule-abiding - 0.5 seconds
        tileSettings.incorrectRuleAbidingSeconds = mgTrialLevel.incorrectRuleAbidingFbDuration.value;

        // Incorrect rule-breaking - 1.0 seconds
        tileSettings.incorrectRuleBreakingSeconds = mgTrialLevel.incorrectRuleBreakingFbDuration.value;

        tileSettings.tileBlinkingDuration = mgTrialLevel.tileBlinkingDuration.value;

        //---------------------------------------------------------

        // TIMEOUT

        tileSettings.timeoutSeconds = 10.0f;

        //Trial Def Configs
        viewPath = mgTrialDef.ViewPath;

        darkenNonPathTiles = mgTrialDef.DarkenNonPathTiles;
        
        tileSettings.numBlinks = mgTaskDef.NumBlinks;


    }
    private void AssignAdjacentTiles(StimGroup tiles, float xOffset, float yOffset)
    {
        foreach (StimDef tileStimDef in tiles.stimDefs)
        {
            GameObject tileGO = tileStimDef.StimGameObject;
            Tile tile = tileGO.GetComponent<Tile>();
            Vector2 tilePos = tileGO.transform.localPosition;

            if (creatingSquareMaze)
            {
                tile.AdjacentTiles = tiles.stimDefs
                    .Where(otherStimDef => otherStimDef != tileStimDef && otherStimDef.StimGameObject.GetComponent<Tile>().mCoord.IsAdjacent(tile.mCoord))
                    .Select(otherStimDef => otherStimDef.StimGameObject)
                    .ToList();
            }
            //Find adjacent tiles within the radius of the current tile
            else
            {
                tile.AdjacentTiles = tiles.stimDefs
                .Where(otherStimDef =>
                {
                    float distance = Vector2.Distance(tilePos, otherStimDef.StimGameObject.transform.localPosition);
                    float xDisplacement = Math.Abs(tilePos.x - otherStimDef.StimGameObject.transform.localPosition.x);
                    float yDisplacement = Math.Abs(tilePos.y - otherStimDef.StimGameObject.transform.localPosition.y);

                    float xLengthSquared = (xOffset / 2) * (xOffset / 2);
                    float yLengthSquared = yOffset * yOffset;
                    float distanceSquared = distance * distance;
                    double sqrtCalculation = Math.Ceiling(Math.Sqrt(xLengthSquared + yLengthSquared));

                    return distance != 0 && (distance <= sqrtCalculation ||( xDisplacement == xOffset && yDisplacement == 0));
                })
                .Select(otherStimDef => otherStimDef.StimGameObject)
                .ToList();


            }

            if (creatingSquareMaze)
                continue;

            foreach (GameObject adjTile in tile.AdjacentTiles)
                CreateTileConnectors(adjTile, tileGO, adjTile.transform.localPosition,
                    tileGO.transform.localPosition);
        }

        tileConnectorsLoaded = true;
    }
    private void AssignInitialTileColor(Tile tile, Maze maze)
    {
        if (tile.mCoord.chessCoord == maze.mStart)
        {
            tile.setColor(tile.startColor);
            startTileGO = tile.gameObject;
        }

        else if (tile.mCoord.chessCoord == maze.mFinish)
        {
            tile.setColor(tile.finishColor);
            finishTileGO = tile.gameObject;
        }
        else if (!darkenNonPathTiles || maze.mPath.Contains((tile.mCoord.chessCoord)))
            tile.setColor(tile.defaultTileColor);
        else
            tile.setColor(new Color(0.5f, 0.5f, 0.5f));

    }
    private void AssignFlashingTiles(Maze maze, StimGroup tiles)
    {

        if (!mgTrialDef.GuidedMazeSelection)
            return;

        for (int i = 0; i < maze.mPath.Count; i++)
        {
            if (i % mgTrialDef.TileFlashingRatio == 0)
                tiles.stimDefs.Find(item => item.StimGameObject.name == maze.mPath[i])
                    .StimGameObject.GetComponent<Tile>().assignedTileFlash = true;
        }
    }
    private void AssignSliderValue(Tile tile)
    {
        if (freePlay)
            tile.sliderValueChange = 1f / (currentMaze.mDims.x * currentMaze.mDims.y);
        else
            tile.sliderValueChange = 1f / currentMaze.mNumSquares; 
    }

    // Methods that handles the feedback for a given tile selection
    public int ManageHiddenPathTileTouch(Tile tile)
    {
        GameObject TileGO = tile.gameObject;

        Debug.LogWarning("NEXT STEP: " + currentMaze.mNextStep);
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
            mgTrialLevel.HandleRuleBreakingError(0);
            return 20;
        }
        
        if (tile.mCoord.chessCoord == currentMaze.mNextStep)
        {


            // Provides feedback for last correct tile touch and updates next tile step
            if (TileGO == currentTilePositionGO)
            {
               // maze.mNextStep = maze.mPath[maze.mPath.FindIndex(pathCoord => pathCoord == tile.mCoord.chessCoord) + 1];
                mgTrialLevel.HandleRetouchCorrect(currentPathIndex);
                currentMaze.mNextStep = currentMaze.mPath[currentPathIndex+1];
                
                if(latestConnection != null)
                    latestConnection.GetComponent<UILineRenderer>().material = Resources.Load<Material>("SelectedPath");
                return 2;
            }

            if(currentTilePositionGO != null)
            {
                Transform connectorTransform = tileConnectorsContainerGO.transform.Find($"{currentTilePositionGO.name}{TileGO.name}") ?? tileConnectorsContainerGO.transform.Find($"{TileGO.name}{currentTilePositionGO.name}");
                latestConnection = connectorTransform?.gameObject;
                latestConnection.GetComponent<UILineRenderer>().material = Resources.Load<Material>("SelectedPath");
            }

            mgTrialLevel.HandleCorrectTouch();
            
            // Helps set progress on the experimenter display
            selectedTilesInPathGO.Add(TileGO);
            currentTilePositionGO = TileGO;
            currentPathIndex++;
            
            // Sets the NextStep if the maze isn't finished
            if (tile.gameObject != finishTileGO)
                currentMaze.mNextStep = currentMaze.mPath[currentPathIndex+1];
            else
                finishedMaze = true; // Finished the Maze

            return 1;
            
        }
        // RULE-ABIDING ERROR ( and RULE ABIDING, BUT PERSEVERATIVE)
        if ( currentTilePositionGO.GetComponent<Tile>().AdjacentTiles.Contains(TileGO) && !selectedTilesInPathGO.Contains(TileGO))
        {
            if (consecutiveErrors > 0) //Perseverative Error
            {
                mgTrialLevel.HandleRuleBreakingError(currentPathIndex);
                return 20;
            }
            
            mgTrialLevel.HandleRuleAbidingError(currentPathIndex);
            RemovePathProgressFollowingError();
            return 10;
        }

        // RULE BREAKING BACKTRACK ERROR OR ERRONEOUS RETOUCH OF LAST CORRECT TILE
        if (selectedTilesInPathGO.Contains(TileGO))
        {
            if (TileGO.Equals(currentTilePositionGO))
            {
                mgTrialLevel.HandleRetouchErroneous(currentPathIndex);
                return 2;
            }

            mgTrialLevel.HandleBackTrackError(currentPathIndex);
            mgTrialLevel.HandleRuleBreakingError(currentPathIndex);

            // Set the correct next step to the last correct tile touch
            RemovePathProgressFollowingError();
            return 20;
        }

        // RULE BREAKING TOUCH
        mgTrialLevel.HandleRuleBreakingError(currentPathIndex);
        RemovePathProgressFollowingError();
        return 20;
    }
    public int ManageFreePlayTileTouch(Tile tile)
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
            Debug.LogWarning("**RULE-BREAK NOT PRESSING START ERROR**");

            mgTrialLevel.HandleRuleBreakingError(currentPathIndex);

            return 20;
        }


        if (currentTilePositionGO == null || (consecutiveErrors == 0 ? (currentTilePositionGO.GetComponent<Tile>().AdjacentTiles.Contains(TileGO) && !selectedTilesInPathGO.Contains(TileGO)) : (TileGO.name == currentMaze.mNextStep)))
        {
            Debug.LogWarning("**CORRECT TOUCH**");

            mgTrialLevel.HandleCorrectTouch();
            

            // Helps set progress on the experimenter display
            selectedTilesInPathGO.Add(TileGO);
            currentTilePositionGO = TileGO;
            currentPathIndex++;

            // Sets the NextStep if the maze isn't finished
            if (tile.gameObject == finishTileGO)
                finishedMaze = true; // Finished the Maze
            if (tile.AdjacentTiles.All(adjTile => selectedTilesInPathGO.Contains(adjTile)))
                outOfMoves = true;
            return 1;

        }

        // RULE BREAKING BACKTRACK ERROR OR ERRONEOUS RETOUCH OF LAST CORRECT TILE
        if (selectedTilesInPathGO.Contains(TileGO))
        {
            if (TileGO.Equals(currentTilePositionGO))
            {
                mgTrialLevel.HandleRetouchErroneous(currentPathIndex);
                Debug.LogWarning("**RETOUCH ERRONEOUS**");

                return 2;
            }

            mgTrialLevel.HandleBackTrackError(currentPathIndex);
            mgTrialLevel.HandleRuleBreakingError(currentPathIndex);

            // Set the correct next step to the last correct tile touch
            Debug.LogWarning("**BACKTRACK ERROR**");
            RemovePathProgressFollowingError();
            return 20;
        }

        // Set the correct next step to the last correct tile touch
        Debug.LogWarning("**RULE-BREAK NON-CONNECTING TILE ERROR**");
        mgTrialLevel.HandleRuleBreakingError(currentPathIndex);
        RemovePathProgressFollowingError();
        return 20;
    }

    public void CreateLandmarks(Dictionary <string, string> landmarks)
    {
        foreach (var landmark in landmarks)
        {
            string[] positions = landmark.Value.Split('_');
            Vector3 positionA = tileContainerGO.transform.Find(positions[0]).localPosition;
            Vector3 positionB = tileContainerGO.transform.Find(positions[1]).localPosition;
            Vector3 positionC = tileContainerGO.transform.Find(positions[2]).localPosition;


            Vector3 centroid = CalculateCentroid(positionA, positionB, positionC);

            GameObject landmarkGO = new GameObject(landmark.Key, typeof(SpriteRenderer));
            landmarkGO.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>(landmarkGO.name);
            landmarkGO.transform.SetParent(landmarksContainerGO.transform);
            landmarkGO.transform.localPosition = centroid;

            switch (landmark.Key)
            {
                case ("House"):
                    landmarkGO.transform.localScale = new Vector3(50, 50, 1.5f);
                    break;
                case ("Tree"):
                    landmarkGO.transform.localScale = new Vector3(20, 20, 1.5f);
                    break;
            }
        }
    }

    
    // Helper Functions
    Vector3 CalculateCentroid(Vector3 positionA, Vector3 positionB, Vector3 positionC)
    {
        return new Vector3(
            (positionA.x + positionB.x + positionC.x) / 3f,
            (positionA.y + positionB.y + positionC.y) / 3f,
            (positionA.z + positionB.z + positionC.z) / 3f
        );
    }
    string GetChessCoordName(int col, int row)
    {
        string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        return $"{alphabet[col]}{row + 1}";
    }
    public void LoadTextMaze(MazeGame_BlockDef mgBD)
    {
        currentMaze = new Maze(mgBD.MazeDef);
        creatingSquareMaze = currentMaze.loadingSquareMaze;
    }
    private void RemovePathProgressFollowingError()
    {
        if (consecutiveErrors == 1)
        {
            currentMaze.mNextStep = currentTilePositionGO.name;
            currentTilePositionGO.GetComponent<Tile>().setColor(currentTilePositionGO.GetComponent<Tile>().initialTileColor);
            if (latestConnection != null)
                latestConnection.GetComponent<UILineRenderer>().material = null;
        }
    }
    public void ActivateMazeElements()
    {
        tileContainerGO.SetActive(true);
        tileConnectorsContainerGO.SetActive(true);
        landmarksContainerGO.SetActive(true);
        
        mgTrialLevel.ActivateChildren(tileContainerGO);
        
        if (creatingSquareMaze)
            mazeBackgroundGO.SetActive(true);
        else
        {
            mgTrialLevel.ActivateChildren(tileConnectorsContainerGO);
            mgTrialLevel.ActivateChildren(landmarksContainerGO);
        }
    }
    public void ResetSelectionClassifications()
    {
        correctSelection = false;
        returnToLast = false;
        erroneousReturnToLast = false;
    }
    public void ResetMazeVariables()
    {
        currentPathIndex = -1;
        consecutiveErrors = 0;

        selectedTilesInPathGO.Clear();
        selectedTilesGO.Clear();
        
        startedMaze = false;
        finishedMaze = false;
        outOfMoves = false;

        mazeDuration = 0;
        choiceDuration = 0;
        mazeStartTime = 0;
        
        correctSelection = false;
        returnToLast = false;
        erroneousReturnToLast = false;
    }
    
}
