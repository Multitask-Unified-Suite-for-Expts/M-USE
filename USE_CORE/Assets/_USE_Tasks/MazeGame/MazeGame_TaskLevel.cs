using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using HiddenMaze;
using MazeGame_Namespace;
using UnityEngine;
using USE_ExperimentTemplate_Task;
using USE_Settings;
using USE_Utilities;
using Random = UnityEngine.Random;

public class MazeGame_TaskLevel : ControlLevel_Task_Template
{
    // Maze Loading Variables
    [HideInInspector] public int[] MazeNumSquares;
    [HideInInspector] public int[] MazeNumTurns;
    [HideInInspector] public Vector2[] MazeDims;
    [HideInInspector] public string[] MazeStart;
    [HideInInspector] public string[] MazeFinish;
    [HideInInspector] public string[] MazeName;
    [HideInInspector] public Maze currMaze;
    private MazeDef[] MazeDefs;
    private string mazeKeyFilePath;
    private MazeGame_TrialLevel mgTL;
    private int mIndex;
    
    // Block Data Tracking Variables
    [HideInInspector]
    public int[] totalErrors_InBlock;
    public int[] perseverativeErrors_InBlock;
    public int[] backtrackErrors_InBlock;
    public int[] ruleAbidingErrors_InBlock;
    public int[] ruleBreakingErrors_InBlock;
    public int[] retouchCorrect_InBlock;
    public int[] retouchErroneous_InBlock;
    public int correctTouches_InBlock; 
    public int numRewardPulses_InBlock;
    public int numAbortedTrials_InBlock;
    public int nonStimTouches_InBlock;
    public int numSliderBarFull_InBlock;
    public List<float?> mazeDurationsList_InBlock = new List<float?>();
    public List<float?> choiceDurationsList_InBlock = new List<float?>();

    // Task Data Tracking Variables
    [HideInInspector]
    public int totalErrors_InTask;
    public int perseverativeErrors_InTask;
    public int backtrackErrors_InTask;
    public int ruleAbidingErrors_InTask;
    public int ruleBreakingErrors_InTask;
    public int retouchCorrect_InTask;
    public int retouchErroneous_InTask;
    public int correctTouches_InTask;
    public int numRewardPulses_InTask;
    public int numAbortedTrials_InTask;
    public int numSliderBarFull_InTask;
    public List<float?> mazeDurationsList_InTask;
    public List<float?> choiceDurationsList_InTask;

    // Average Variables
    private float AvgTotalErrors;
    private float AvgPerseverativeErrors;
    private float AvgBacktrackErrors;
    private float AvgRuleAbidingErrors;
    private float AvgRuleBreakingErrors;
    private float AvgRetouchCorrect; 
    private float AvgCorrectTouches;
    private float AvgMazeDuration;
    private float AvgReward;
    
    // Block Summary String Variables
    [HideInInspector] public string BlockAveragesString;
    [HideInInspector] public string CurrentBlockString;
    [HideInInspector] public StringBuilder PreviousBlocksString;
    private int blocksAdded = 0;
    private MazeGame_BlockDef mgBD => GetCurrentBlockDef<MazeGame_BlockDef>();


    public override void DefineControlLevel()
    {/*
        totalErrors_InTask = new List<int>();
        perseverativeErrors_InTask = new List<int>();
        backtrackErrors_InTask = new List<int>();
        ruleAbidingErrors_InTask = new List<int>();
        ruleBreakingErrors_InTask = new List<int>();
        retouchCorrect_InTask = new List<int>();
        correctTouches_InTask = new List<int>();
        numRewardPulses_InTask = new List<int>();
        numSliderBarFull_InTask = new List<int>();
        numAbortedTrials_InTask = new List<int>();*/
        mazeDurationsList_InTask = new List<float?>();
        choiceDurationsList_InTask = new List<float?>();
        
        mgTL = (MazeGame_TrialLevel)TrialLevel;
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
            
            RenderSettings.skybox = CreateSkybox(mgTL.GetContextNestedFilePath(ContextExternalFilePath, mgBD.ContextName, "LinearDark"));
            mgTL.contextName = mgBD.ContextName;
            mgTL.MinTrials = mgBD.MinMaxTrials[0];
            EventCodeManager.SendCodeNextFrame(SessionEventCodes["ContextOn"]);
            
            //instantiate arrays
            ruleAbidingErrors_InBlock = new int[currMaze.mNumSquares];
            ruleBreakingErrors_InBlock = new int[currMaze.mNumSquares];
            backtrackErrors_InBlock = new int[currMaze.mNumSquares];
            perseverativeErrors_InBlock = new int[currMaze.mNumSquares];
            retouchCorrect_InBlock = new int[currMaze.mNumSquares];
            retouchErroneous_InBlock = new int[currMaze.mNumSquares];
            totalErrors_InBlock = new int[currMaze.mNumSquares];
            
            ResetBlockVariables();
            CalculateBlockSummaryString();
        });
    }
    public void AssignBlockData()
    {
        BlockData.AddDatum("TotalErrors", () => $"[{string.Join(", ", totalErrors_InBlock)}]");
        BlockData.AddDatum("CorrectTouches", () => correctTouches_InBlock);
        BlockData.AddDatum("RetouchCorrect",() => $"[{string.Join(", ", retouchCorrect_InBlock)}]");
        BlockData.AddDatum("RetouchErroneous",() => $"[{string.Join(", ", retouchErroneous_InBlock)}]");
        BlockData.AddDatum("PerseverativeErrors",() => $"[{string.Join(", ", perseverativeErrors_InBlock)}]");
        BlockData.AddDatum("BacktrackErrors", () => $"[{string.Join(", ", backtrackErrors_InBlock)}]");
        BlockData.AddDatum("RuleAbidingErrors", () => $"[{string.Join(", ", ruleAbidingErrors_InBlock)}]");
        BlockData.AddDatum("RuleBreakingErrors", () => $"[{string.Join(", ", ruleBreakingErrors_InBlock)}]");
        BlockData.AddDatum("NumRewardPulses", () => numRewardPulses_InBlock);
        BlockData.AddDatum("NumSliderBarFull", ()=>numSliderBarFull_InBlock);
        BlockData.AddDatum("NumAbortedTrials", ()=> numAbortedTrials_InBlock);
        BlockData.AddDatum("MazeDurations", () => string.Join(",",mazeDurationsList_InBlock));
        BlockData.AddDatum("ChoiceDurations", () => string.Join(",", choiceDurationsList_InBlock));
        BlockData.AddDatum("MaxTrials", () => mgBD.MinMaxTrials[0]);
        BlockData.AddDatum("MinTrials", () => mgBD.MaxTrials);
       // BlockData.AddDatum("NumNonStimSelections", () => mgTL.NonStimTouches_InBlock);
    }
    // public void AddBlockValuesToTaskValues()
    // {
    //     numRewardPulses_InTask.Add(numRewardPulses_InBlock);
    //     totalErrors_InTask.Add(totalErrors_InBlock);
    //     correctTouches_InTask.Add(correctTouches_InBlock);
    //     retouchCorrect_InTask.Add(retouchCorrect_InBlock);
    //     perseverativeErrors_InTask.Add(perseverativeErrors_InBlock);
    //     backtrackErrors_InTask.Add(backtrackErrors_InBlock);
    //     ruleAbidingErrors_InTask.Add(ruleAbidingErrors_InBlock);
    //     ruleBreakingErrors_InTask.Add(ruleBreakingErrors_InBlock);
    //     numAbortedTrials_InTask.Add(numAbortedTrials_InBlock);
    //     numSliderBarFull_InTask.Add(numSliderBarFull_InBlock);
    //     mazeDurationsList_InTask.Add(string.Join(",",mazeDurationsList_InBlock));
    //     List<float> allDurations = mazeDurationsList_InTask
    //         .SelectMany(str => str.Split(','))
    //         .Select(str => float.Parse(str))
    //         .ToList();
    //     Debug.Log("MAZE DURATIONS IN TASK: " + string.Join(",", allDurations));
    // }
    public override OrderedDictionary GetSummaryData()
    {
        OrderedDictionary data = new OrderedDictionary();
        data["Trial Count In Task"] = mgTL.TrialCount_InTask;
        data["Num Reward Pulses"] = numRewardPulses_InTask;
        data["Total Errors"] = totalErrors_InTask;
        data["Correct Touches"] = correctTouches_InTask;
        data["Retouch Correct"] = retouchCorrect_InTask;
        data["Retouch Erroneous"] = retouchErroneous_InTask;
        data["Perseverative Errors"] = perseverativeErrors_InTask;
        data["Backtrack Errors"] = backtrackErrors_InTask;
        data["Rule-Abiding Errors"] = ruleAbidingErrors_InTask;
        data["Rule-Breaking Errors"] = ruleBreakingErrors_InTask;
        data["Num Aborted Trials"] = numAbortedTrials_InTask;
        data["Num Slider Bar Full"] = numSliderBarFull_InTask;
        data["Average Maze Durations"] = mazeDurationsList_InTask.Average();
        data["Average Choice Duration"] = choiceDurationsList_InTask.Average();
        return data;
    }
    private void ResetBlockVariables()
    {
        correctTouches_InBlock = 0;
        Array.Clear(perseverativeErrors_InBlock, 0, perseverativeErrors_InBlock.Length);
        Array.Clear(backtrackErrors_InBlock, 0, backtrackErrors_InBlock.Length);
        Array.Clear(ruleAbidingErrors_InBlock, 0, ruleAbidingErrors_InBlock.Length);
        Array.Clear(ruleBreakingErrors_InBlock, 0, ruleBreakingErrors_InBlock.Length);
        Array.Clear(totalErrors_InBlock, 0, totalErrors_InBlock.Length);
        Array.Clear(retouchCorrect_InBlock, 0, retouchCorrect_InBlock.Length);
        Array.Clear(retouchErroneous_InBlock, 0, retouchErroneous_InBlock.Length);
        numRewardPulses_InBlock = 0;
        nonStimTouches_InBlock = 0;
        numAbortedTrials_InBlock = 0;
        numSliderBarFull_InBlock = 0;
        mazeDurationsList_InBlock.Clear();
        choiceDurationsList_InBlock.Clear();
        mgTL.runningPercentError.Clear();
    }
    public void CalculateBlockSummaryString()
    {
        ClearStrings();
        float latestPercentError = -1;
        if (mgTL.runningPercentError.Count > 0)
            latestPercentError = (mgTL.runningPercentError[mgTL.runningPercentError.Count - 1])*100;

        CurrentBlockString = "<b>\nMin Trials in Block: </b>" + mgTL.CurrentTrialDef.MinMaxTrials[0] +
                             "<b>\nMax Trials in Block: </b>" + mgTL.CurrentTrialDef.MaxTrials +
                             "<b>\nLearning Criterion: </b>" + String.Format("{0:0.00}%", mgTL.CurrentTrialDef.BlockEndThreshold*100) +
                             "\n\nLast Trial's Percent Error: " + (latestPercentError == -1 ?
                                 ("N/A"):String.Format("{0:0.00}%", latestPercentError)) +
                             "\nTotal Errors: " + totalErrors_InBlock.Sum() +
                             "\nRule-Abiding Errors: " + ruleAbidingErrors_InBlock.Sum() +
                             "\nRule-Breaking Errors: " + ruleBreakingErrors_InBlock.Sum() +
                             "\nPerseverative Errors: " + perseverativeErrors_InBlock.Sum() +
                             "\nBacktrack Errors: " + backtrackErrors_InBlock.Sum() +
                             "\nRetouch Correct: " + retouchCorrect_InBlock.Sum() +
                             "\nRetouch Erroneous: " + retouchErroneous_InBlock.Sum() +
                             "\n\nRewards: " + numRewardPulses_InBlock +
                             "\nAverage Choice Duration: " +
                             String.Format("{0:0.00}", choiceDurationsList_InBlock.Average()) +
                             "\nAverage Maze Duration: " +
                             String.Format("{0:0.00}", mazeDurationsList_InBlock.Average());
        
        BlockSummaryString.AppendLine(CurrentBlockString).ToString();
        if (PreviousBlocksString.Length > 0)
            BlockSummaryString.AppendLine(PreviousBlocksString.ToString());
    }
    public override void SetTaskSummaryString()
    {
        float percentAborted = 0;
        if (mazeDurationsList_InTask.Count != 0)
            AvgMazeDuration = (float)mazeDurationsList_InTask.Average();
        else
            AvgMazeDuration = 0;
        if (mgTL.TrialCount_InTask != 0)
            percentAborted = (float)(Math.Round(decimal.Divide(numAbortedTrials_InTask, (mgTL.TrialCount_InTask)), 2)) * 100;
        else
            percentAborted = 0;
    
        CurrentTaskSummaryString.Clear();
        CurrentTaskSummaryString.Append($"\n<b>{ConfigName}</b>" +
                                        $"\n<b># Trials:</b> {mgTL.TrialCount_InTask} ({percentAborted}% aborted)" +
                                        $"\t<b># Blocks:</b> {BlockCount}" +
                                        $"\t<b># Reward Pulses:</b> {numRewardPulses_InTask}" +
                                        $"\n<b># Rule-Break Errors:</b> {ruleBreakingErrors_InTask}" +
                                        $"\t<b># Rule-Abiding Errors:</b> {ruleAbidingErrors_InTask}" +
                                        $"\nAvg Maze Duration: {AvgMazeDuration}" +
                                        $"\n# Slider Bar Filled: {numSliderBarFull_InTask}");

    }
    public void ClearStrings()
    {
        BlockAveragesString = "";
        CurrentBlockString = "";
        BlockSummaryString.Clear();
    }/*
    private void CalculateBlockAverages()
    {
        if (totalErrors_InTask.Count >= 1)
            AvgTotalErrors = (float)totalErrors_InTask.AsQueryable().Average();
        
        if (correctTouches_InTask.Count >= 1)
            AvgCorrectTouches = (float)correctTouches_InTask.AsQueryable().Average();

        if (retouchCorrect_InTask.Count >= 1)
            AvgRetouchCorrect = (float)retouchCorrect_InTask.AsQueryable().Average();

        if (perseverativeErrors_InTask.Count >= 1)
            AvgPerseverativeErrors = (float)perseverativeErrors_InTask.AsQueryable().Average();

        if (backtrackErrors_InTask.Count >= 1)
            AvgBacktrackErrors = (float)backtrackErrors_InTask.AsQueryable().Average();
        
        if (ruleAbidingErrors_InTask.Count >= 1)
            AvgRuleAbidingErrors = (float)ruleAbidingErrors_InTask.AsQueryable().Average();
        
        if (ruleBreakingErrors_InTask.Count >= 1)
            AvgRuleBreakingErrors = (float)ruleBreakingErrors_InTask.AsQueryable().Average();

        if (numRewardPulses_InTask.Count >= 1)
            AvgReward = (float)numRewardPulses_InTask.AsQueryable().Average();

        if (mazeDurationsList_InTask.Count >= 1)
        {
            List<float> allDurations = mazeDurationsList_InTask
                .SelectMany(str => str.Split(','))
                .Select(str => float.Parse(str))
                .ToList();
            AvgMazeDuration = allDurations.Average();
        }
    }*/
    private void SetSettings()
    {
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ContextExternalFilePath"))
            mgTL.ContextExternalFilePath =
                (string)SessionSettings.Get(TaskName + "_TaskSettings", "ContextExternalFilePath");
        else mgTL.ContextExternalFilePath = ContextExternalFilePath;

        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "MazeKeyFilePath"))
            mazeKeyFilePath = (string)SessionSettings.Get(TaskName + "_TaskSettings", "MazeKeyFilePath");
        else Debug.LogError("Maze key file path settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "MazeFilePath"))
            mgTL.MazeFilePath = (string)SessionSettings.Get(TaskName + "_TaskSettings", "MazeFilePath");
        else Debug.LogError("Maze File Path not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "StartButtonPosition"))
            mgTL.StartButtonPosition = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "StartButtonPosition");
        else Debug.LogError("Start Button Position settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "StartButtonScale"))
            mgTL.StartButtonScale = (float)SessionSettings.Get(TaskName + "_TaskSettings", "StartButtonScale");
        else Debug.LogError("Start Button Scale settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "NeutralITI"))
            mgTL.NeutralITI = (bool)SessionSettings.Get(TaskName + "_TaskSettings", "NeutralITI");
        else
        {
            mgTL.NeutralITI = false;
            Debug.Log("Neutral ITI settings not defined in the TaskDef. Default Setting of false is used instead");
        }
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "TileSize"))
        {
            mgTL.TileSize = (float)SessionSettings.Get(TaskName + "_TaskSettings", "TileSize");
        }
        else
        {
            mgTL.TileSize = 0.5f; // default value in the case it isn't specified
            Debug.Log("Tile Size settings not defined in the TaskDef. Default setting of " + mgTL.TileSize +
                      " is used instead.");
        }

        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "TileTexture"))
        {
            mgTL.TileTexture = (string)SessionSettings.Get(TaskName + "_TaskSettings", "TileTexture");
        }
        else
        {
            mgTL.TileTexture = "Tile"; // default value in the case it isn't specified
            Debug.Log("Tile Texture settings not defined in the TaskDef. Default setting of " + mgTL.TileTexture +
                      " is used instead.");
        }

        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "NumBlinks"))
            mgTL.NumBlinks = (int)SessionSettings.Get(TaskName + "_TaskSettings", "NumBlinks");
        else Debug.LogError("Num Blinks settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "StartColor"))
            mgTL.startColor = (float[])SessionSettings.Get(TaskName + "_TaskSettings", "StartColor");
        else Debug.LogError("Start Color settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "FinishColor"))
            mgTL.finishColor = (float[])SessionSettings.Get(TaskName + "_TaskSettings", "FinishColor");
        else Debug.LogError("Finish Color settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "CorrectColor"))
            mgTL.correctColor = (float[])SessionSettings.Get(TaskName + "_TaskSettings", "CorrectColor");
        else Debug.LogError("Correct Color settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "LastCorrectColor"))
            mgTL.lastCorrectColor = (float[])SessionSettings.Get(TaskName + "_TaskSettings", "LastCorrectColor");
        else Debug.LogError("Last Correct Color settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "IncorrectRuleAbidingColor"))
            mgTL.incorrectRuleAbidingColor =
                (float[])SessionSettings.Get(TaskName + "_TaskSettings", "IncorrectRuleAbidingColor");
        else Debug.LogError("Incorrect Rule Abiding Color settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "IncorrectRuleBreakingColor"))
            mgTL.incorrectRuleBreakingColor =
                (float[])SessionSettings.Get(TaskName + "_TaskSettings", "IncorrectRuleBreakingColor");
        else Debug.LogError("Incorrect Rule Breaking Color settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "DefaultTileColor"))
            mgTL.defaultTileColor = (float[])SessionSettings.Get(TaskName + "_TaskSettings", "DefaultTileColor");
        else Debug.LogError("Default Tile Color settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "UsingFixedRatioReward"))
            mgTL.UsingFixedRatioReward = (bool)SessionSettings.Get(TaskName + "_TaskSettings", "UsingFixedRatioReward");
        else
        {
            mgTL.UsingFixedRatioReward = false;
            Debug.Log("Using Fixed Ratio Reward settings not defined in the TaskDef, set as default of false");
        }
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "MazeBackground"))
            mgTL.MazeBackgroundTextureName = (string)SessionSettings.Get(TaskName + "_TaskSettings", "MazeBackgroundTexture");
        else
        {
            mgTL.MazeBackgroundTextureName = "MazeBackground";
            Debug.Log("Maze Background Texture settings not defined in the TaskDef, set as default of MazeBackground");
        }

        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "TouchFeedbackDuration"))
            mgTL.TouchFeedbackDuration = (float)SessionSettings.Get(TaskName + "_TaskSettings", "TouchFeedbackDuration");
        else
            mgTL.TouchFeedbackDuration = .3f;

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

        if (mgBD.MazeName != null)
        {
            mIndex = MazeName.FindAllIndexof(mgBD.MazeName)[0];
        }
        else
        {
            var mdIndices = MazeDims.FindAllIndexof(mgBD.MazeDims);
            var mnsIndices = MazeNumSquares.FindAllIndexof(mgBD.MazeNumSquares);
            var mntIndices = MazeNumTurns.FindAllIndexof(mgBD.MazeNumTurns);
            var msIndices = MazeStart.FindAllIndexof(mgBD.MazeStart);
            var mfIndices = MazeFinish.FindAllIndexof(mgBD.MazeFinish);
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

        mgTL.mazeDefName = MazeName[mIndex];
    }
    public void LoadTextMaze()
    {
        // textMaze will load the text file containing the full Maze path of the intended mazeDef for the block/trial
        string mazeFilePath = "";

        string[] filePaths = Directory.GetFiles(mgTL.MazeFilePath, $"{mgTL.mazeDefName}*", SearchOption.AllDirectories);

        if (filePaths.Length >= 1)
            mazeFilePath = filePaths[0];
        else
            Debug.LogError($"Maze not found within the given file path ({mazeFilePath}) or in any nested folders");
        
        var textMaze = File.ReadAllLines(mazeFilePath);
        currMaze = new Maze(textMaze[0]);
    }
}