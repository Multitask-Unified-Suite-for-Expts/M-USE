using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using HiddenMaze;
using MazeGame_Namespace;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using USE_StimulusManagement;
using USE_UI;

public class MazeManager:MonoBehaviour 
{
    // This script is attached to the MazeContainer GameObject

    // M-USE Fields
    private MazeGame_TrialLevel mgTrialLevel;
    private MazeGame_TaskDef mgTaskDef;
    private MazeGame_TrialDef mgTrialDef;


    public GameObject tilePrefab; // Set in Editor

    // Maze GameObjects
    public TileSettings tileSettings; // Set in Editor
    public GameObject tileContainerGO; // Set in Editor
    public GameObject tileConnectorsContainerGO; // Set in Editor
    public GameObject landmarksContainerGO; // Set in Editor
    public GameObject mazeBackgroundGO; // Set in Editor
    
    
    [HideInInspector] private int currentPathIndex;
    [HideInInspector] private int consecutiveErrors;

    [HideInInspector] private List<GameObject> selectedTilesInPathGO = new List<GameObject>();
    [HideInInspector] private List<GameObject> selectedTilesGO = new List<GameObject>();
    [HideInInspector] private List<float> selectedTilesRxnTimes = new List<float>();

    [HideInInspector] private GameObject startTileGO;
    [HideInInspector] private GameObject finishTileGO;

    [HideInInspector] private bool createGridMaze;
    [HideInInspector] private bool viewPath;
    [HideInInspector] private bool darkenNonPathTiles;
    [HideInInspector] private bool retouchCurrentTilePositionError;
    [HideInInspector] private bool retouchCurrentTilePositionCorrect;
    [HideInInspector] private bool ruleAbidingError;
    [HideInInspector] private bool ruleBreakingError;
    [HideInInspector] private bool correctNextTileChoice;
    [HideInInspector] private bool freePlay;
    [HideInInspector] private bool outOfMoves;
    [HideInInspector] private bool startedMaze;
    [HideInInspector] private bool finishedMaze;
    [HideInInspector] private bool backTrackError;
    [HideInInspector] private bool tileConnectorsLoaded;
    [HideInInspector] private bool mazeManagerInitialized;
    [HideInInspector] private Maze currentMaze;

    [HideInInspector] private GameObject latestConnection;
    [HideInInspector] private GameObject currentTilePositionGO;
    [HideInInspector] private GameObject selectedTileGO;
    [HideInInspector] private GameObject lastErrorTileGO;

    [HideInInspector] private float mazeDuration;
    [HideInInspector] private float choiceDuration;
    [HideInInspector] private float mazeStartTime;
    [HideInInspector] private float choiceStartTime;

    public GridLayoutGroup tileContainerGridLayoutGroup;
    public Vector2 tileContainerInitialSpacing;
    public Vector2 tileContainerInitialCellSizing;
    private bool initializedGridLayoutDimensions;


    // Update is called once per frame
    void Update()
    {
        if (mazeStartTime != 0)
            mazeDuration = Time.unscaledTime - mazeStartTime;
        if (choiceStartTime != 0)
            choiceDuration = Time.unscaledTime - choiceStartTime;
    }
    public void InitializeMazeManager(MazeGame_TrialLevel trialLevel, MazeGame_TrialDef trialDef, MazeGame_TaskDef taskDef)
    {
        mgTrialLevel = trialLevel;
        mgTrialDef = trialDef;
        mgTaskDef = taskDef;

        if (!initializedGridLayoutDimensions)
        {
        tileContainerInitialSpacing = tileContainerGridLayoutGroup.spacing;
        tileContainerInitialCellSizing = tileContainerGridLayoutGroup.cellSize;
        initializedGridLayoutDimensions = true;
        }
    }
    public StimGroup CreateMaze()
    {
        StimGroup tiles = new StimGroup("Tiles");

        
        if(tileSettings == null) // Instantiate a single instance of tile Settings that is applied to every tile
            tileSettings = ScriptableObject.CreateInstance<TileSettings>();
        
        SetTileSettings();
        if (createGridMaze)
        {
            tileContainerGridLayoutGroup.enabled = true;
            tileContainerGridLayoutGroup.constraintCount = (int)currentMaze.mDims.x; // Restrict the grid layout by number of columns
            for (var x = (int)currentMaze.mDims.x -1; x >= 0 ; x--)
            for (var y = (int)currentMaze.mDims.y -1; y >= 0; y--)
            { 
                GameObject tileGO = InitializeTile(x, y, tiles);
            }

            tileContainerGridLayoutGroup.spacing = new Vector2(
                mgTaskDef.SpaceBetweenTiles * tileContainerInitialSpacing.x,
                mgTaskDef.SpaceBetweenTiles * tileContainerInitialSpacing.y);
            float mazeWidth = ((tileContainerGridLayoutGroup.cellSize.x + tileContainerGridLayoutGroup.spacing.x) * currentMaze.mDims.x) - tileContainerGridLayoutGroup.spacing.x +
                              (2*tileContainerInitialSpacing.x);
            float mazeHeight = ((tileContainerGridLayoutGroup.cellSize.y + tileContainerGridLayoutGroup.spacing.y) * currentMaze.mDims.y) - tileContainerGridLayoutGroup.spacing.y +
                               (2* tileContainerInitialSpacing.y);

            InitializeMazeBackground(mazeWidth, mazeHeight);
            AssignAdjacentTiles(tiles, tileContainerGridLayoutGroup.cellSize.x + tileContainerGridLayoutGroup.spacing.x, tileContainerGridLayoutGroup.cellSize.y + tileContainerGridLayoutGroup.spacing.y);

        }
        else if (currentMaze.mCustomDims != null)
        {
            tileContainerGridLayoutGroup.enabled = false;
            List<int> customMazeDims = currentMaze.mCustomDims;
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
                    
                    GameObject tileGO = InitializeTile(col, row, tiles);
                    tileGO.transform.localPosition = new Vector2(x, y);
                }
            }

            AssignAdjacentTiles(tiles, xOffset, yOffset);
            mgTrialLevel.DeactivateChildren(tileConnectorsContainerGO);

            if (mgTrialDef.Landmarks?.Count > 0)
                CreateLandmarks(mgTrialDef.Landmarks);

        }
        
        
        
        currentMaze.mName = $"{currentMaze.mStart}_{currentMaze.mFinish}";
        AssignFlashingTiles(currentMaze, tiles);
        return tiles;
    }

    // Initialize Maze GameObjects
    private void InitializeMazeBackground(float mazeWidth, float mazeHeight)
    {
        mazeBackgroundGO.GetComponent<RectTransform>().sizeDelta = new Vector2(mazeWidth,mazeHeight);
        mazeBackgroundGO.SetActive(false);
    }
    private GameObject InitializeTile(int col, int row, StimGroup tiles)
    {
        GameObject tileGO = Instantiate(tilePrefab, tileContainerGO.transform);
        string tileName = GetChessCoordName(col, row);
        tileGO.name = tileName;
        
        if (createGridMaze)
            tileGO.GetComponent<Image>().sprite =  Resources.Load<Sprite>("Tile");
        else
        {
            tileGO.GetComponent<Image>().sprite =  Resources.Load<Sprite>("Star");
            tileGO.transform.localScale = new Vector3(1.25f, 1.25f, 1);
        }
        //tileGO.transform.localScale = mgTaskDef.TileSize * tileGO.transform.localScale;
        Tile tile = tileGO.AddComponent<Tile>();
        tile.Initialize(tileSettings, this);

        StimDef tileStimDef = new StimDef(tiles, tileGO);
        tile.SetCoord(new Coords(tileGO.name));

        if(Session.SessionDef.IsHuman)
            tileGO.AddComponent<HoverEffect>();
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
        tileSettings.SetTileColor("default", mgTrialDef.DefaultTileColor != null ?  new Color(mgTrialDef.DefaultTileColor[0], mgTrialDef.DefaultTileColor[1], mgTrialDef.DefaultTileColor[2], 1) : new Color(mgTaskDef.DefaultTileColor[0], mgTaskDef.DefaultTileColor[1], mgTaskDef.DefaultTileColor[2], 1));

        // Start - Light yellow
        tileSettings.SetTileColor("start", new Color(mgTaskDef.StartColor[0], mgTaskDef.StartColor[1], mgTaskDef.StartColor[2], 1));

        // Finish - Light blue
        tileSettings.SetTileColor("finish", new Color(mgTaskDef.FinishColor[0], mgTaskDef.FinishColor[1], mgTaskDef.FinishColor[2], 1));

        // Correct - Light green
        tileSettings.SetTileColor("correct", new Color(mgTaskDef.CorrectColor[0], mgTaskDef.CorrectColor[1], mgTaskDef.CorrectColor[2]));

        // Prev correct - Darker green
        tileSettings.SetTileColor("prevCorrect", new Color(mgTaskDef.LastCorrectColor[0], mgTaskDef.LastCorrectColor[1], mgTaskDef.LastCorrectColor[2]));

        // Incorrect rule-abiding - Orange
        tileSettings.SetTileColor("incorrectRuleAbiding", new Color(mgTaskDef.IncorrectRuleAbidingColor[0], mgTaskDef.IncorrectRuleAbidingColor[1],
            mgTaskDef.IncorrectRuleAbidingColor[2]));

        // Incorrect rule-breaking - Black
        tileSettings.SetTileColor("incorrectRuleBreaking", new Color(mgTaskDef.IncorrectRuleBreakingColor[0], mgTaskDef.IncorrectRuleBreakingColor[1],
            mgTaskDef.IncorrectRuleBreakingColor[2]));

        // FEEDBACK LENGTH IN SECONDS

        // Correct - 0.5 seconds
        tileSettings.SetFeedbackDuration("correct", mgTrialLevel.correctFbDuration.value);

        // Prev correct - 0.5 seconds
        tileSettings.SetFeedbackDuration("prevCorrect", mgTrialLevel.previousCorrectFbDuration.value);

        // Incorrect rule-abiding - 0.5 seconds
        tileSettings.SetFeedbackDuration("incorrectRuleAbiding",  mgTrialLevel.incorrectRuleAbidingFbDuration.value);

        // Incorrect rule-breaking - 1.0 seconds
        tileSettings.SetFeedbackDuration("incorrectRuleBreaking", mgTrialLevel.incorrectRuleBreakingFbDuration.value);

        tileSettings.SetFeedbackDuration("blinking", mgTrialLevel.tileBlinkingDuration.value);

        tileSettings.SetFeedbackDuration("timeoutDuration", 10.0f);

        //Trial Def Configs
        viewPath = mgTrialDef.ViewPath;

        darkenNonPathTiles = mgTrialDef.DarkenNonPathTiles;
        
        tileSettings.SetNumBlinks(mgTaskDef.NumBlinks);


    }
    private void AssignAdjacentTiles(StimGroup tiles, float xOffset, float yOffset)
    {
        foreach (StimDef tileStimDef in tiles.stimDefs)
        {
            GameObject tileGO = tileStimDef.StimGameObject;
            Tile tile = tileGO.GetComponent<Tile>();
            Vector2 tilePos = tileGO.transform.localPosition;

            if (createGridMaze)
            {
                tile.SetAdjacentTiles(tiles.stimDefs
                    .Where(otherStimDef => otherStimDef != tileStimDef && otherStimDef.StimGameObject.GetComponent<Tile>().GetCoord().IsAdjacent(tile.GetCoord()))
                    .Select(otherStimDef => otherStimDef.StimGameObject)
                    .ToList()) ;
            }
            //Find adjacent tiles within the radius of the current tile
            else
            {
                tile.SetAdjacentTiles(tiles.stimDefs
                .Where(otherStimDef =>
                {
                    float distance = Vector2.Distance(tilePos, otherStimDef.StimGameObject.transform.localPosition);
                    float xDisplacement = Math.Abs(tilePos.x - otherStimDef.StimGameObject.transform.localPosition.x);
                    float yDisplacement = Math.Abs(tilePos.y - otherStimDef.StimGameObject.transform.localPosition.y);

                    float xLengthSquared = (xOffset / 2) * (xOffset / 2);
                    float yLengthSquared = yOffset * yOffset;
                    float distanceSquared = distance * distance;
                    double sqrtCalculation = Math.Ceiling(Math.Sqrt(xLengthSquared + yLengthSquared));

                    return distance != 0 && (distance <= sqrtCalculation || (xDisplacement == xOffset && yDisplacement == 0));
                })
                .Select(otherStimDef => otherStimDef.StimGameObject)
                .ToList());


            }

            if (createGridMaze)
                continue;

            foreach (GameObject adjTile in tile.GetAdjacentTiles())
                CreateTileConnectors(adjTile, tileGO, adjTile.transform.localPosition,
                    tileGO.transform.localPosition);
        }

        tileConnectorsLoaded = true;
    }
    private void AssignInitialTileColor(Tile tile, Maze maze)
    {
        if (tile.GetChessCoord() == maze.mStart)
        {
            tile.SetColor(tileSettings.GetTileColor("start"));
            tile.initialTileColor = tileSettings.GetTileColor("start");
            startTileGO = tile.gameObject;
        }
        else if (tile.GetChessCoord() == maze.mFinish)
        {
            tile.SetColor(tileSettings.GetTileColor("finish"));
            tile.initialTileColor = tileSettings.GetTileColor("finish");
            finishTileGO = tile.gameObject;
        }
        else if (mgTrialDef.Blockades != null  && mgTrialDef.Blockades.Contains(tile.GetChessCoord()))
        {
            tile.SetColor(new Color(0,0,0));
            tile.initialTileColor = new Color(0, 0, 0);
            tile.gameObject.GetComponent<Image>().raycastTarget = false;
        }
        else if (!darkenNonPathTiles || maze.mPath.Contains(tile.GetChessCoord()))
        {
            tile.SetColor(tileSettings.GetTileColor("default"));
            tile.initialTileColor = tileSettings.GetTileColor("default");
        }
        else
        {
            tile.SetColor(new Color(0.15f, 0.15f, 0.15f));
            tile.initialTileColor = new Color(0.15f, 0.15f, 0.15f);
            tile.GetComponent<Image>().raycastTarget = false;
        }

    }
    private void AssignFlashingTiles(Maze maze, StimGroup tiles)
    {

        if (mgTrialDef.TileFlashingRatio == 0)
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
        {
            if(createGridMaze)
                tile.SetSliderValueChange(1f / (currentMaze.mDims.x * currentMaze.mDims.y));
            else
              tile.SetSliderValueChange(1f / currentMaze.mCustomDims.Sum());
        }
        else
            tile.SetSliderValueChange(1f / currentMaze.mNumSquares); 
    }

    // Methods that handles the feedback for a given tile selection
    public int ManageHiddenPathTileTouch(Tile tile)
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
            ruleBreakingError = true;
            consecutiveErrors++;
            return 20;
            
        }
        
        if (tile.GetChessCoord() == currentMaze.mNextStep)
        {
            // Provides feedback for last correct tile touch and updates next tile step
            if (TileGO == currentTilePositionGO)
            {
                retouchCurrentTilePositionCorrect = true;
                consecutiveErrors = 0;
                selectedTileGO.GetComponent<Tile>().SetTileFbDuration(tileSettings.GetFeedbackDuration("prevCorrect"));

                currentMaze.mNextStep = currentMaze.mPath[currentPathIndex+1];
                
                if(latestConnection != null)
                    latestConnection.GetComponent<UILineRenderer>().material = Resources.Load<Material>("SelectedPath");

                return 2;
            }

            if(currentTilePositionGO != null && tileConnectorsContainerGO.transform.childCount != 0)
            {
                Transform connectorTransform = tileConnectorsContainerGO.transform.Find($"{currentTilePositionGO.name}{TileGO.name}") ?? tileConnectorsContainerGO.transform.Find($"{TileGO.name}{currentTilePositionGO.name}");
                latestConnection = connectorTransform?.gameObject;
                latestConnection.GetComponent<UILineRenderer>().material = Resources.Load<Material>("SelectedPath");
            }
            
            correctNextTileChoice = true;
            consecutiveErrors = 0;

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
        if ( currentTilePositionGO.GetComponent<Tile>().GetAdjacentTiles().Contains(TileGO) && !selectedTilesInPathGO.Contains(TileGO))
        {
            if (consecutiveErrors > 0) // Fail to return to last correct after error
            {
                ruleBreakingError = true;
                consecutiveErrors++;

                return 20;
            }
            
            ruleAbidingError = true;
            consecutiveErrors++;

            RemovePathProgressFollowingError();
            return 10;
        }

        // RULE BREAKING BACKTRACK ERROR OR ERRONEOUS RETOUCH OF LAST CORRECT TILE
        if (selectedTilesInPathGO.Contains(TileGO))
        {
            if (TileGO.Equals(currentTilePositionGO))
            {
                retouchCurrentTilePositionError = true;
                return 2;
            }
            
            backTrackError = true;
            consecutiveErrors++;

            RemovePathProgressFollowingError();
            return 20;
        }

        ruleBreakingError = true;
        consecutiveErrors++;
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
            ruleBreakingError = true;
            consecutiveErrors++;

            return 20;
        }


        if (currentTilePositionGO == null || (consecutiveErrors == 0 ? (currentTilePositionGO.GetComponent<Tile>().GetAdjacentTiles().Contains(TileGO) && !selectedTilesInPathGO.Contains(TileGO)) : (TileGO.name == currentMaze.mNextStep)))
        {
            if (TileGO == currentTilePositionGO)
            {
                // maze.mNextStep = maze.mPath[maze.mPath.FindIndex(pathCoord => pathCoord == tile.mCoord.chessCoord) + 1];
                consecutiveErrors = 0;
                selectedTileGO.GetComponent<Tile>().SetTileFbDuration(tileSettings.GetFeedbackDuration("prevCorrect"));

                retouchCurrentTilePositionCorrect = true;

                if (latestConnection != null)
                    latestConnection.GetComponent<UILineRenderer>().material = Resources.Load<Material>("SelectedPath");
                return 2;
            }
            
            if(currentTilePositionGO != null)
            {
                Transform connectorTransform = tileConnectorsContainerGO.transform.Find($"{currentTilePositionGO.name}{TileGO.name}") ?? tileConnectorsContainerGO.transform.Find($"{TileGO.name}{currentTilePositionGO.name}");
                latestConnection = connectorTransform?.gameObject;
                latestConnection.GetComponent<UILineRenderer>().material = Resources.Load<Material>("SelectedPath");
            }
            
            // Helps set progress on the experimenter display
            selectedTilesInPathGO.Add(TileGO);
            currentTilePositionGO = TileGO;
            currentPathIndex++;
            correctNextTileChoice = true;

            // Sets the NextStep if the maze isn't finished
            if (tile.gameObject == finishTileGO)
                finishedMaze = true; // Finished the Maze
            if (tile.GetAdjacentTiles().All(adjTile => selectedTilesInPathGO.Contains(adjTile)))
                outOfMoves = true;
            return 1;

        }

        // RULE BREAKING BACKTRACK ERROR OR ERRONEOUS RETOUCH OF LAST CORRECT TILE
        if (selectedTilesInPathGO.Contains(TileGO))
        {
            if (TileGO.Equals(currentTilePositionGO))
            {
                retouchCurrentTilePositionError = true;
                return 2;
            }
            
            backTrackError = true; // Back Track Error
            consecutiveErrors++;
            RemovePathProgressFollowingError();
            return 20;
        }

        // Set the correct next step to the last correct tile touch
        ruleBreakingError = true;
        consecutiveErrors++;

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

            // Check if landmark.Key contains a certain keyword
            if (landmark.Key.ToLower().Contains("house"))
                landmarkGO.transform.localScale = new Vector3(24, 24, 1.5f);
            
            else if (landmark.Key.ToLower().Contains("tree"))
                landmarkGO.transform.localScale = new Vector3(22, 22, 1.5f);
            
            else if (landmark.Key.ToLower().Contains("car"))
                landmarkGO.transform.localScale = new Vector3(22, 22, 1.5f);

            landmarkGO.SetActive(false);
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
        freePlay = currentMaze.freePlay;
        createGridMaze = currentMaze.loadingSquareMaze;
    }
    private void RemovePathProgressFollowingError()
    {
        if (consecutiveErrors == 1)
        {
            currentMaze.mNextStep = currentTilePositionGO.name;
            currentTilePositionGO.GetComponent<Tile>().SetColor(currentTilePositionGO.GetComponent<Tile>().initialTileColor);
            if (latestConnection != null)
                latestConnection.GetComponent<UILineRenderer>().material = null;
        }
    }
    public void ActivateMazeElements()
    {
        mgTrialLevel.tiles.ToggleVisibility(true);
        tileConnectorsContainerGO.SetActive(true);
        landmarksContainerGO.SetActive(true);

        if (createGridMaze)
            mazeBackgroundGO.SetActive(true);
        else
        {
            mgTrialLevel.ActivateChildren(tileConnectorsContainerGO);
            mgTrialLevel.ActivateChildren(landmarksContainerGO);
        }
    }
    public void DeactivateMazeElements()
    {
        mazeBackgroundGO.SetActive(false);
        mgTrialLevel.DeactivateChildren(tileConnectorsContainerGO);
        mgTrialLevel.DeactivateChildren(landmarksContainerGO);
    }
    public void MazeCleanUp()
    {
        mgTrialLevel.DestroyChildren(tileConnectorsContainerGO);
        mgTrialLevel.DestroyChildren(landmarksContainerGO);

        currentMaze.mNextStep = currentMaze.mStart;
        selectedTileGO = null;
        lastErrorTileGO = null;
    }
    public void ResetSelectionClassifications()
    {
        correctNextTileChoice = false;
        retouchCurrentTilePositionCorrect = false;
        retouchCurrentTilePositionError = false;
        backTrackError = false;
        ruleAbidingError = false;
        ruleBreakingError = false;
        selectedTileGO = null;
    }
    public void ResetMazeVariables()
    {
        currentPathIndex = -1;
        consecutiveErrors = 0;

        selectedTilesInPathGO.Clear();
        selectedTilesRxnTimes.Clear();
        selectedTilesGO.Clear();

        mazeManagerInitialized = false;
        startedMaze = false;
        finishedMaze = false;
        outOfMoves = false;

        mazeDuration = 0;
        choiceDuration = 0;
        mazeStartTime = 0;
        choiceStartTime = 0;
        
        correctNextTileChoice = false;
        retouchCurrentTilePositionCorrect = false;
        retouchCurrentTilePositionError = false;
        backTrackError = false;
        ruleAbidingError = false;
        ruleBreakingError = false;

    }

    public void ActivateMazeBackground()
    {
        mazeBackgroundGO.SetActive(true);
    }
    public void FlashNextCorrectTile(GameObject nextCorrectTile)
    {
        nextCorrectTile.GetComponent<Tile>().FlashTile();
    }
    public void TerminateNextCorrectTileFlashing(GameObject nextCorrectTile)
    {
        nextCorrectTile.GetComponent<Tile>().TerminateTileFlashing();
    }
    public Maze GetCurrentMaze()
    {
        return currentMaze;
    }
    public GameObject GetSelectedTile()
    {
        return selectedTileGO;
    }
    public List<GameObject> GetAllSelectedTiles()
    {
        return selectedTilesGO;
    }
    public bool IsMazeStarted()
    {
        return startedMaze;
    }
    public bool IsMazeFinished()
    {
        return finishedMaze;
    }
    public bool IsOutOfMoves()
    {
        return outOfMoves;
    }
    public bool IsCorrectSelection()
    {
        return correctNextTileChoice;
    } 
    public bool IsErroneousReturnToLast()
    {
        return retouchCurrentTilePositionError;
    }
    public bool IsCorrectReturnToLast()
    {
        return retouchCurrentTilePositionCorrect;
    }
    public bool IsBacktrack()
    {
        return backTrackError;
    }
    public bool IsPathVisible()
    {
        return viewPath;
    }   
    public bool IsFreePlay()
    {
        return freePlay;
    }
    public int GetConsecutiveErrorCount()
    {
        return consecutiveErrors;
    }
    public GameObject GetCurrentTilePosition()
    {
        return currentTilePositionGO;
    }
    public GameObject GetStartTile()
    {
        return startTileGO;
    }
    public GameObject GetFinishTile()
    {
        return finishTileGO;
    }
    public bool GetMazeManagerInitialized()
    {
        return mazeManagerInitialized;
    }
    public void SetSelectedTileGO(GameObject? tileGO)
    {
        selectedTileGO = tileGO;
        selectedTilesGO.Add(tileGO);
    }
    public int GetCurrentPathIndex()
    {
        return currentPathIndex;
    }
    public void SetMazeFinished(bool finishedStatus)
    {
        finishedMaze = finishedStatus;
    }
    public void SetMazeStarted(bool startedStatus)
    {
        startedMaze = startedStatus;
    }
    public void AddToAllSelectedTiles(GameObject tileGO)
    {
        selectedTilesGO.Add(tileGO);
    }

    public float GetChoiceDuration()
    {
        return choiceDuration;
    }
    public float GetMazeDuration()
    {
        return mazeDuration;
    }

    public void SetChoiceStartTime(float startTime)
    {
        choiceStartTime = startTime;
    }
    public void SetMazeStartTime(float startTime)
    {
        mazeStartTime = startTime;
    }
    public float GetMazeStartTime()
    {
        return mazeStartTime;
    }
    public float GetChoiceStartTime()
    {
        return choiceStartTime;
    }

    public void AddReactionTime()
    {
        selectedTilesRxnTimes.Add(choiceDuration);
        choiceDuration = 0;
        choiceStartTime = 0;
    }

    public List<float> GetAllSelectionRxnTimes()
    {
        return selectedTilesRxnTimes;
    }
    public string DetermineErrorType()
    {
        string errorType = "";
        if (lastErrorTileGO != null && lastErrorTileGO == selectedTileGO && !correctNextTileChoice) // Checks for Perseverative Error
        {
            if (backTrackError)
                errorType = "perseverativeBackTrackError";
            else if (retouchCurrentTilePositionError)
                errorType = "perseverativeRetouchCurrentTilePositionError";
            else if (ruleAbidingError)
                errorType = "perseverativeRuleAbidingError";
            else
                errorType = "perseverativeRuleBreakingError";
        }
        else
        {
            if (retouchCurrentTilePositionCorrect)
                errorType = "retouchCurrentTilePositionCorrect";
            else if (correctNextTileChoice)
                errorType = "correctNextTileChoice";
            else if (backTrackError)
                errorType = "backTrackError";
            else if (retouchCurrentTilePositionError)
                errorType = "retouchCurrentTilePositionError";
            else if (ruleAbidingError)
                errorType = "ruleAbidingError";
            else
                errorType = "ruleBreakingError";
        }

        if (ruleBreakingError || ruleAbidingError || backTrackError || retouchCurrentTilePositionError)
            lastErrorTileGO = selectedTileGO;
        
        return errorType;
    }
}
