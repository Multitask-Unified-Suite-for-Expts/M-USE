using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
using Random = UnityEngine.Random;

public class ChaseReactionTest_TaskLevel : ControlLevel_Task_Template
{
    [HideInInspector] public int[] MazeNumSquares;
    [HideInInspector] public int[] MazeNumTurns;
    [HideInInspector] public Vector2[] MazeDims;
    [HideInInspector] public string[] MazeStart;
    [HideInInspector] public string[] MazeFinish;
    [HideInInspector] public string[] MazeName;
    [HideInInspector]public Maze currMaze;
    private MazeDef[] MazeDefs;
    private string mazeKeyFilePath;
    private ChaseReactionTest_TrialLevel crtTL;
    private int mIndex;
    
    // Block Data Tracking Variables
    [HideInInspector]
    public int[] totalErrors_InBlock;
    public int[] backtrackErrors_InBlock;
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
    public int backtrackErrors_InTask;
    public int totalErrors_InTask;
    public List<float?> mazeDurationsList_InTask;
    public List<float?> choiceDurationsList_InTask;
    
    // Average Variables
    private float AvgMazeDuration = 0;
    
    [HideInInspector] public string BlockAveragesString;
    [HideInInspector] public string CurrentBlockString;
    [HideInInspector] public StringBuilder PreviousBlocksString;
    private int blocksAdded = 0;
    
    private ChaseReactionTest_BlockDef crtBD => GetCurrentBlockDef<ChaseReactionTest_BlockDef>();
    public override void DefineControlLevel()
    {
        mazeDurationsList_InTask = new List<float?>();
        choiceDurationsList_InTask = new List<float?>();
        
        crtTL = (ChaseReactionTest_TrialLevel)TrialLevel;
        SetSettings();
        AssignBlockData();
        
        BlockAveragesString = "";
        CurrentBlockString = "";
        PreviousBlocksString = new StringBuilder();
        
        blocksAdded = 0;
        LoadMazeDef();
        
        RunBlock.AddInitializationMethod(() =>
        {
            FindMaze();
            LoadTextMaze(); // need currMaze here to set all the arrays
                
            RenderSettings.skybox = CreateSkybox(crtTL.GetContextNestedFilePath(ContextExternalFilePath, crtBD.ContextName, "LinearDark"));
            crtTL.contextName = crtBD.ContextName;
            crtTL.MinTrials = crtBD.MinMaxTrials[0];
            EventCodeManager.SendCodeNextFrame(SessionEventCodes["ContextOn"]);
            
            //instantiate arrays
            totalErrors_InBlock = new int[currMaze.mNumSquares];
            backtrackErrors_InBlock = new int[currMaze.mNumSquares];
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
        
        ClearStrings();
        CurrentBlockString = "<b>Min Trials in Block: </b>" + crtTL.CurrentTrialDef.MinMaxTrials[0] +
                             "<b>\nMax Trials in Block: </b>" + crtTL.CurrentTrialDef.MaxTrials +
                             "<b>\nLearning Criterion: </b>" + crtTL.CurrentTrialDef.BlockEndThreshold +
                             "\n\nTotal Errors: " + totalErrors_InBlock.Sum() +
                             "\nBacktrack Errors: " + backtrackErrors_InBlock.Sum() +
                             "\n\nRewards: " + numRewardPulses_InBlock +
                             "\nAverage Choice Duration: " +
                             String.Format("{0:0.00}", choiceDurationsList_InBlock.Average()) +
                             "\nAverage Maze Duration: " +
                             String.Format("{0:0.00}", mazeDurationsList_InBlock.Average());
        
        if (blocksAdded > 1)
            CurrentBlockString += "\n";

        //Add CurrentBlockString if block wasn't aborted:
        if (crtTL.AbortCode == 0)
            BlockSummaryString.AppendLine(CurrentBlockString);


        /*if (blocksAdded > 1) //If atleast 2 blocks to average, set Averages string and add to BlockSummaryString:
        {
            BlockAveragesString = "-------------------------------------------------" +
                              "\n" +
                              "\n<b>Block Averages (" + blocksAdded + " blocks):" + "</b>" +
                              "\nAvg Total Errors: " + AvgTotalErrors.ToString("0.00") +
                              "\nAvg Correct Touches: " + AvgCorrectTouches.ToString("0.00") +
                              "\nAvg Rule-Abiding Errors: " + AvgRuleAbidingErrors.ToString("0.00") + "s" +
                              "\nAvg Rule-Breaking Errors: " + AvgRuleBreakingErrors.ToString("0.00") +
                              "\nAvg Preservative Errors: " + AvgPerseverativeErrors.ToString("0.00") +
                              "\nAvg Backtrack Errors: " + AvgBacktrackErrors.ToString("0.00") + "s" +
                              "\nAvg Retouch Correct: " + AvgRetouchCorrect.ToString("0.00") +
                              "\nAvg Reward: " + AvgReward.ToString("0.00") +
                              "\nAvg Maze Duration: " + AvgMazeDuration.ToString("0.00");;
            
            BlockSummaryString.AppendLine(BlockAveragesString);
        }*/

        //Add Previous blocks string:
        if(PreviousBlocksString.Length > 0)
        {
            BlockSummaryString.AppendLine("\n" + PreviousBlocksString);
        }
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
        numSliderBarFull_InBlock = 0;
        mazeDurationsList_InBlock.Clear();
        choiceDurationsList_InBlock.Clear();
        crtTL.runningTrialPerformance.Clear();
    }
    private void SetSettings()
    {
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
        BlockData.AddDatum("BacktrackErrors", () => $"[{string.Join(", ", backtrackErrors_InBlock)}]");
        BlockData.AddDatum("NumRewardPulses", () => numRewardPulses_InBlock);
        BlockData.AddDatum("NumSliderBarFull", ()=>numSliderBarFull_InBlock);
        BlockData.AddDatum("NumAbortedTrials", ()=> numAbortedTrials_InBlock);
        BlockData.AddDatum("MazeDurations", () => string.Join(",",mazeDurationsList_InBlock));
        BlockData.AddDatum("ChoiceDurations", () => string.Join(",", choiceDurationsList_InBlock));
    }
    public override OrderedDictionary GetSummaryData()
    {
        OrderedDictionary data = new OrderedDictionary();

        data["Num Reward Pulses"] = numRewardPulses_InTask;
        data["Total Errors"] = totalErrors_InTask;
        data["Backtrack Errors"] = backtrackErrors_InTask;
        data["Num Aborted Trials"] = numAbortedTrials_InTask;
        data["Num Slider Bar Full"] = numSliderBarFull_InTask;
        data["Average Maze Durations"] = mazeDurationsList_InTask.Average();
        data["Average Choice Duration"] = choiceDurationsList_InTask.Average();
        return data;
    }
    public override void SetTaskSummaryString()
    {
        float percentAborted = 0;
        if (mazeDurationsList_InTask.Count != 0)
            AvgMazeDuration = (float)mazeDurationsList_InTask.Average();
        else
            AvgMazeDuration = 0;
        if (crtTL.TrialCount_InTask != 0)
            percentAborted = (float)(Math.Round(decimal.Divide(numAbortedTrials_InTask, (crtTL.TrialCount_InTask)), 2)) * 100;
        else
            percentAborted = 0;
    
        CurrentTaskSummaryString.Clear();
        CurrentTaskSummaryString.Append($"\n<b>{ConfigName}</b>" +
                                        $"\n<b># Trials:</b> {crtTL.TrialCount_InTask} ({percentAborted}% aborted)" +
                                        $"\t<b># Blocks:</b> {BlockCount}" +
                                        $"\t<b># Reward Pulses:</b> {numRewardPulses_InTask}" +
                                        $"\nAvg Maze Duration: {AvgMazeDuration}" +
                                        $"\n# Slider Bar Filled: {numSliderBarFull_InTask}");

    }
}