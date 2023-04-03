using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using USE_Settings;
using USE_StimulusManagement;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Block;
using ChaseReactionTest_Namespace;
using HiddenMaze;
using UnityEngine;
using USE_Utilities;

public class ChaseReactionTest_TaskLevel : ControlLevel_Task_Template
{
    [HideInInspector] public int[] MazeNumSquares;
    [HideInInspector] public int[] MazeNumTurns;
    [HideInInspector] public Vector2[] MazeDims;
    [HideInInspector] public string[] MazeStart;
    [HideInInspector] public string[] MazeFinish;
    [HideInInspector] public string[] MazeName;
    [HideInInspector]public Maze currMaze;
    
    // Block Data Tracking Variables
    [HideInInspector]
    public int[] totalErrors_InBlock;
    public int numRewardPulses_InBlock;
    public int numAbortedTrials_InBlock;
    public List<float?> mazeDurationsList_InBlock = new List<float?>();
    public List<float?> choiceDurationsList_InBlock = new List<float?>();
    public int numSliderBarFull_InBlock;
    
    // Task Data Tracking Variables
    [HideInInspector]
    public int numRewardPulses_InTask;
    public int numAbortedTrials_InTask;
    public int numSliderBarFull_InTask;
    
    [HideInInspector] public string BlockAveragesString;
    [HideInInspector] public string CurrentBlockString;
    [HideInInspector] public StringBuilder PreviousBlocksString;
    
    private int blocksAdded = 0;
    private MazeDef[] MazeDefs;
    private string mazeKeyFilePath;
    private ChaseReactionTest_TrialLevel crtTL;
    private int mIndex;
    private ChaseReactionTest_BlockDef crtBD => GetCurrentBlockDef<ChaseReactionTest_BlockDef>();
    public override void DefineControlLevel()
    {
        crtTL = (ChaseReactionTest_TrialLevel)TrialLevel;
        SetSettings();
        crtTL = (ChaseReactionTest_TrialLevel)TrialLevel;
        AssignBlockData();
        
        BlockAveragesString = "";
        CurrentBlockString = "";
        PreviousBlocksString = new StringBuilder();

        
        blocksAdded = 0;
        LoadMazeDef();
        
        RunBlock.AddInitializationMethod(() =>
        {
            FindMaze();
            LoadTextMaze();
                
            RenderSettings.skybox = CreateSkybox(crtTL.GetContextNestedFilePath(ContextExternalFilePath, crtBD.ContextName, "LinearDark"));
            crtTL.contextName = crtBD.ContextName;
            crtTL.MinTrials = crtBD.MinMaxTrials[0];
            EventCodeManager.SendCodeNextFrame(SessionEventCodes["ContextOn"]);
            
            //instantiate arrays
            totalErrors_InBlock = new int[currMaze.mNumSquares];
            crtTL.DestroyChildren(GameObject.Find("MainCameraCopy"));
            
            ResetBlockVariables();
            CalculateBlockSummaryString();
        });
        BlockFeedback.AddInitializationMethod(() =>
        {
            if (crtTL.AbortCode == 0)
            {
                CurrentBlockString += "\n" + "\n";
                CurrentBlockString = CurrentBlockString.Replace("Current Block", $"Block {blocksAdded + 1}");
                PreviousBlocksString.Insert(0,CurrentBlockString); //Add current block string to full list of previous blocks. 
                blocksAdded++;
            }
            // CalculateBlockAverages();
        });

    }

    public void CalculateBlockSummaryString()
    {
        
    }
    public void ClearStrings()
    {
        BlockAveragesString = "";
        CurrentBlockString = "";
        BlockSummaryString.Clear();
    }

    private void ResetBlockVariables()
    {
        numRewardPulses_InBlock = 0;
        numAbortedTrials_InBlock = 0;
        crtTL.runningTrialPerformance.Clear();

    }
    private void SetSettings()
    {
        Debug.Log("CHASE REACTION TASK NAME: "  + TaskName);
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ContextExternalFilePath"))
            crtTL.ContextExternalFilePath =
                (string)SessionSettings.Get(TaskName + "_TaskSettings", "ContextExternalFilePath");
        else crtTL.ContextExternalFilePath = ContextExternalFilePath;

        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "MazeKeyFilePath"))
            mazeKeyFilePath = (string)SessionSettings.Get(TaskName + "_TaskSettings", "MazeKeyFilePath");
        else Debug.LogError("Maze key file path settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "MazeFilePath"))
            crtTL.MazeFilePath = (string)SessionSettings.Get(TaskName + "_TaskSettings", "MazeFilePath");
        else Debug.LogError("Maze File Path not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "StartButtonPosition"))
            crtTL.StartButtonPosition = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "StartButtonPosition");
        else Debug.LogError("Start Button Position settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "StartButtonScale"))
            crtTL.StartButtonScale = (float)SessionSettings.Get(TaskName + "_TaskSettings", "StartButtonScale");
        else Debug.LogError("Start Button Scale settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "NeutralITI"))
            crtTL.NeutralITI = (bool)SessionSettings.Get(TaskName + "_TaskSettings", "NeutralITI");
        else
        {
            crtTL.NeutralITI = false;
            Debug.Log("Neutral ITI settings not defined in the TaskDef. Default Setting of false is used instead");
        }
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "TileSize"))
        {
            crtTL.TileSize = (float)SessionSettings.Get(TaskName + "_TaskSettings", "TileSize");
        }
        else
        {
            crtTL.TileSize = 0.5f; // default value in the case it isn't specified
            Debug.Log("Tile Size settings not defined in the TaskDef. Default setting of " + crtTL.TileSize +
                      " is used instead.");
        }

        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "TileTexture"))
        {
            crtTL.TileTexture = (string)SessionSettings.Get(TaskName + "_TaskSettings", "TileTexture");
        }
        else
        {
            crtTL.TileTexture = "Tile"; // default value in the case it isn't specified
            Debug.Log("Tile Texture settings not defined in the TaskDef. Default setting of " + crtTL.TileTexture +
                      " is used instead.");
        }

        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "NumBlinks"))
            crtTL.NumBlinks = (int)SessionSettings.Get(TaskName + "_TaskSettings", "NumBlinks");
        else Debug.LogError("Num Blinks settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "StartColor"))
            crtTL.startColor = (float[])SessionSettings.Get(TaskName + "_TaskSettings", "StartColor");
        else Debug.LogError("Start Color settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "FinishColor"))
            crtTL.finishColor = (float[])SessionSettings.Get(TaskName + "_TaskSettings", "FinishColor");
        else Debug.LogError("Finish Color settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "CorrectColor"))
            crtTL.correctColor = (float[])SessionSettings.Get(TaskName + "_TaskSettings", "CorrectColor");
        else Debug.LogError("Correct Color settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "LastCorrectColor"))
            crtTL.lastCorrectColor = (float[])SessionSettings.Get(TaskName + "_TaskSettings", "LastCorrectColor");
        else Debug.LogError("Last Correct Color settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "IncorrectRuleAbidingColor"))
            crtTL.incorrectRuleAbidingColor =
                (float[])SessionSettings.Get(TaskName + "_TaskSettings", "IncorrectRuleAbidingColor");
        else Debug.LogError("Incorrect Rule Abiding Color settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "IncorrectRuleBreakingColor"))
            crtTL.incorrectRuleBreakingColor =
                (float[])SessionSettings.Get(TaskName + "_TaskSettings", "IncorrectRuleBreakingColor");
        else Debug.LogError("Incorrect Rule Breaking Color settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "DefaultTileColor"))
            crtTL.defaultTileColor = (float[])SessionSettings.Get(TaskName + "_TaskSettings", "DefaultTileColor");
        else Debug.LogError("Default Tile Color settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "FixedRatioReward"))
            crtTL.UsingFixedRatioReward = (bool)SessionSettings.Get(TaskName + "_TaskSettings", "FixedRatioReward");
        else
        {
            crtTL.UsingFixedRatioReward = false;
            Debug.Log("Fixed Ratio Reward settings not defined in the TaskDef, set as default of false");
        }
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "MazeBackground"))
            crtTL.MazeBackgroundTextureName = (string)SessionSettings.Get(TaskName + "_TaskSettings", "MazeBackgroundTexture");
        else
        {
            crtTL.MazeBackgroundTextureName = "MazeBackground";
            Debug.Log("Maze Background Texture settings not defined in the TaskDef, set as default of MazeBackground");
        }
        
    }
    private void LoadMazeDef()
    {
        SessionSettings.ImportSettings_SingleTypeArray<MazeDef>("MazeDefs", mazeKeyFilePath);
        MazeDefs = (MazeDef[])SessionSettings.Get("MazeDefs");
        MazeDims = new Vector2[MazeDefs.Length];
        MazeNumSquares = new int[MazeDefs.Length];
        MazeNumTurns = new int[MazeDefs.Length];
        MazeStart = new string[MazeDefs.Length];
        MazeFinish = new string[MazeDefs.Length];
        MazeName = new string[MazeDefs.Length];
        for (var iMaze = 0; iMaze < MazeDefs.Length; iMaze++)
        {
            MazeDims[iMaze] = MazeDefs[iMaze].mDims;
            MazeNumSquares[iMaze] = MazeDefs[iMaze].mNumSquares;
            MazeNumTurns[iMaze] = MazeDefs[iMaze].mNumTurns;
            MazeStart[iMaze] = MazeDefs[iMaze].mStart;
            MazeFinish[iMaze] = MazeDefs[iMaze].mFinish;
            MazeName[iMaze] = MazeDefs[iMaze].mName;
        }
    }
    private void FindMaze()
    {
        //for given block MazeDims, MazeNumSquares, MazeNumTurns, get all indices of that value, find intersect
        //then choose random member of intersect and assign to this block's trials

        if (crtBD.MazeName != null)
        {
            mIndex = MazeName.FindAllIndexof(crtBD.MazeName)[0];
        }
        else
        {
            var mdIndices = MazeDims.FindAllIndexof(crtBD.MazeDims);
            var mnsIndices = MazeNumSquares.FindAllIndexof(crtBD.MazeNumSquares);
            var mntIndices = MazeNumTurns.FindAllIndexof(crtBD.MazeNumTurns);
            var msIndices = MazeStart.FindAllIndexof(crtBD.MazeStart);
            var mfIndices = MazeFinish.FindAllIndexof(crtBD.MazeFinish);
            var possibleMazeDefIndices = mfIndices
                .Intersect(msIndices.Intersect(mntIndices.Intersect(mdIndices.Intersect(mnsIndices)))).ToArray();

            mIndex = possibleMazeDefIndices[Random.Range(0, possibleMazeDefIndices.Length)];

            //remove the maze specifications from all of the arrays so the maze won't repeat on subsequent blocks of the same conditions
            MazeDefs = MazeDefs.Where((source, index) => index != mIndex).ToArray();
            MazeDims = MazeDims.Where((source, index) => index != mIndex).ToArray();
            MazeNumSquares = MazeNumSquares.Where((source, index) => index != mIndex).ToArray();
            MazeNumTurns = MazeNumTurns.Where((source, index) => index != mIndex).ToArray();
            MazeStart = MazeStart.Where((source, index) => index != mIndex).ToArray();
            MazeFinish = MazeFinish.Where((source, index) => index != mIndex).ToArray();
            MazeName = MazeName.Where((source, index) => index != mIndex).ToArray();
        }

        crtTL.mazeDefName = MazeName[mIndex];
    }
    public void LoadTextMaze()
    {
        // textMaze will load the text file containing the full Maze path of the intended mazeDef for the block/trial
        string mazeFilePath = "";

        string[] filePaths = Directory.GetFiles(crtTL.MazeFilePath, $"{crtTL.mazeDefName}*", SearchOption.AllDirectories);

        if (filePaths.Length >= 1)
            mazeFilePath = filePaths[0];
        else
            Debug.LogError($"Maze not found within the given file path ({mazeFilePath}) or in any nested folders");
        
        var textMaze = File.ReadAllLines(mazeFilePath);
        currMaze = new Maze(textMaze[0]);
    }

    private void AssignBlockData()
    {
        BlockData.AddDatum("TotalErrors", () => $"[{string.Join(", ", totalErrors_InBlock)}]");
        BlockData.AddDatum("NumRewardPulses", () => numRewardPulses_InBlock);
        BlockData.AddDatum("NumSliderBarFull", ()=>numSliderBarFull_InBlock);
        BlockData.AddDatum("NumAbortedTrials", ()=> numAbortedTrials_InBlock);
        BlockData.AddDatum("MazeDurations", () => string.Join(",",mazeDurationsList_InBlock));
        BlockData.AddDatum("ChoiceDurations", () => string.Join(",", choiceDurationsList_InBlock));
    }
}