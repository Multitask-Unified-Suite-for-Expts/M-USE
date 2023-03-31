using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using USE_States;
using USE_Settings;
using USE_ExperimentTemplate_Trial;
using USE_StimulusManagement;
using ChaseReactionTest_Namespace;
using USE_UI;

public class ChaseReactionTest_TrialLevel : ControlLevel_Trial_Template
{
    public ChaseReactionTest_TrialDef CurrentTrialDef => GetCurrentTrialDef<ChaseReactionTest_TrialDef>();
    // Generic Task Variables
    public GameObject MG_CanvasGO;
    public USE_StartButton USE_StartButton;
    private GameObject StartButton;
    
    // Block Ending Variable
    public List<float> runningTrialPerformance = new List<float>();
    private float trialPerformance;
    public int MinTrials;
    
    // Maze Object Variables
    public string mazeDefName;
    public GameObject MazeBackground;
    private GameObject MazeContainer;
    private float mazeLength;
    private float mazeHeight;
    private Vector2 mazeDims;
    private bool mazeLoaded = false;

    // Tile objects
    private Tile tile = new Tile();
    private GameObject tileGO;
    public StimGroup tiles; // top of trial level with other variable definitions
    private Texture2D tileTex;
    private Texture2D mazeBgTex;
    
    // Task Level Defined Variables
    [HideInInspector]
    public float[] startColor;
    public float[] finishColor;
    public float[] correctColor;
    public float[] lastCorrectColor;
    public float[] incorrectRuleAbidingColor;
    public float[] incorrectRuleBreakingColor;
    public float[] defaultTileColor;
    public int NumBlinks;
    public Tile TilePrefab;
    public float TileSize;
    public string TileTexture;
    public string MazeBackgroundTextureName;
    public string ContextExternalFilePath;
    public string MazeFilePath;
    public Vector3 StartButtonPosition;
    public float StartButtonScale;
    public bool NeutralITI;
    public bool UsingFixedRatioReward;
    public override void DefineControlLevel()
    {
        

    }

    protected override void DefineTrialStims()
    {
        //Define StimGroups consisting of StimDefs whose gameobjects will be loaded at TrialLevel_SetupTrial and 
        //destroyed at TrialLevel_Finish
    }
    
}
