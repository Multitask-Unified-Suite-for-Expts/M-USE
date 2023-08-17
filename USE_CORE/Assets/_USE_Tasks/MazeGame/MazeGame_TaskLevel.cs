using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using HiddenMaze;
using MazeGame_Namespace;
using TriLib.Samples;
using UnityEngine;
using USE_ExperimentTemplate_Task;
using USE_Settings;
using USE_Utilities;
using static UnityEngine.LightProbeProxyVolume;
using Random = UnityEngine.Random;

public class MazeGame_TaskLevel : ControlLevel_Task_Template
{
    // Config Loading Variables
    public MazeDef[] MazeDefs;

    // Maze Loading Variables
    [HideInInspector] public int[] MazeNumSquares;
    [HideInInspector] public int[] MazeNumTurns;
    [HideInInspector] public Vector2[] MazeDims;
    [HideInInspector] public string[] MazeStart;
    [HideInInspector] public string[] MazeFinish;
    [HideInInspector] public string[] MazeName;
    [HideInInspector] public string[] MazeString;
    [HideInInspector] public Maze currMaze;
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
    public MazeGame_BlockDef mgBD => GetCurrentBlockDef<MazeGame_BlockDef>();
    private MazeGame_TaskDef currentTaskDef => GetTaskDef<MazeGame_TaskDef>();

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

        //SetMazePaths();

        AssignBlockData();
        
        BlockAveragesString = "";
        CurrentBlockString = "";
        PreviousBlocksString = new StringBuilder();

        
        blocksAdded = 0;
        //LoadMazeDef();

        RunBlock.AddSpecificInitializationMethod(() =>
        {
            MazeDefs = customSettings[0].AssignCustomSetting<MazeDef[]>();
            SetSkyBox(mgBD.ContextName);
            InitializeMazeSearchingArrays();

            FindMaze();
            LoadTextMaze();
            //StartCoroutine(LoadTextMaze()); // need currMaze here to set all the arrays

            mgTL.contextName = mgBD.ContextName;
            mgTL.MinTrials = mgBD.MinMaxTrials[0];
            
            CalculateBlockSummaryString();
            ResetBlockVariables();

        });
    }


    private void InitializeBlockArrays()
    {
        ruleAbidingErrors_InBlock = new int[currMaze.mNumSquares];
        ruleBreakingErrors_InBlock = new int[currMaze.mNumSquares];
        backtrackErrors_InBlock = new int[currMaze.mNumSquares];
        perseverativeErrors_InBlock = new int[currMaze.mNumSquares];
        retouchCorrect_InBlock = new int[currMaze.mNumSquares];
        retouchErroneous_InBlock = new int[currMaze.mNumSquares];
        totalErrors_InBlock = new int[currMaze.mNumSquares];
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
        BlockData.AddDatum("MinTrials", () => mgBD.MinMaxTrials[0]);
        BlockData.AddDatum("MaxTrials", () => mgTL.CurrentTrialDef.MaxTrials);
       // BlockData.AddDatum("NumNonStimSelections", () => mgTL.NonStimTouches_InBlock);
    }
    public override OrderedDictionary GetBlockResultsData()
    {
        OrderedDictionary data = new OrderedDictionary
        {
            ["Maze Duration"] = mgTL.mazeDuration.ToString("0.0") + "s",
            ["Correct Touches"] = correctTouches_InBlock,
            ["Total Errors"] = totalErrors_InBlock.Sum(),
            ["Retouched Correct"] = retouchCorrect_InBlock.Sum(),
            ["Retouched Erroneous"] = retouchErroneous_InBlock.Sum(),
        };
        return data;
    }

    public override OrderedDictionary GetTaskSummaryData()
    {
        OrderedDictionary data = new OrderedDictionary
        {
            ["Trial Count In Task"] = mgTL.TrialCount_InTask + 1,
            ["Num Reward Pulses"] = numRewardPulses_InTask,
            ["Total Errors"] = totalErrors_InTask,
            ["Correct Touches"] = correctTouches_InTask,
            ["Retouch Correct"] = retouchCorrect_InTask,
            ["Retouch Erroneous"] = retouchErroneous_InTask,
            ["Perseverative Errors"] = perseverativeErrors_InTask,
            ["Backtrack Errors"] = backtrackErrors_InTask,
            ["Rule-Abiding Errors"] = ruleAbidingErrors_InTask,
            ["Rule-Breaking Errors"] = ruleBreakingErrors_InTask,
            ["Num Aborted Trials"] = numAbortedTrials_InTask,
            ["Num Slider Bar Full"] = numSliderBarFull_InTask,
            ["Average Maze Durations"] = mazeDurationsList_InTask.Average(),
            ["Average Choice Duration"] = choiceDurationsList_InTask.Average()
        };
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
        float? latestPercentError = null;
        if (mgTL.runningPercentError.Count > 0 && mgTL.runningPercentError[mgTL.runningPercentError.Count - 1] != null) //confirm last trial wasn't aborted/incomplet
        {
            latestPercentError = (mgTL.runningPercentError[mgTL.runningPercentError.Count - 1]) * 100;
        }
        CurrentBlockString = "<b>\nMin Trials in Block: </b>" + mgBD.MinMaxTrials[0] +
                             "<b>\nMax Trials in Block: </b>" + mgBD.MaxTrials +
                             "<b>\nLearning Criterion: </b>" + String.Format("{0:0.00}%", mgBD.BlockEndThreshold*100) +
                             "\n\nLast Trial's Percent Error: " + (latestPercentError == null ?
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
        
        CurrentBlockSummaryString.AppendLine(CurrentBlockString).ToString();
        if (PreviousBlocksString.Length > 0)
            CurrentBlockSummaryString.AppendLine(PreviousBlocksString.ToString());
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
        CurrentTaskSummaryString.Append($"\n<b>{ConfigFolderName}</b>" +
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
        CurrentBlockSummaryString.Clear();
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
        currMaze = new Maze(MazeString[mIndex]);
        mgTL.InitializeTrialArrays();
        InitializeBlockArrays();
    }


    public override List<CustomSettings> DefineCustomSettings()
    {
       // customSettings.Add(new CustomSettings("MazeDef", typeof(MazeDef), "array", parsed => MazeDefs = (MazeDef[])parsed));
        customSettings.Add(new CustomSettings("MazeDef", typeof(MazeDef), "array", MazeDefs));
        return customSettings;
    }

    private void InitializeMazeSearchingArrays()
    {
        MazeDims = new Vector2[MazeDefs.Length];
        MazeNumSquares = new int[MazeDefs.Length];
        MazeNumTurns = new int[MazeDefs.Length];
        MazeStart = new string[MazeDefs.Length];
        MazeFinish = new string[MazeDefs.Length];
        MazeName = new string[MazeDefs.Length];
        MazeString = new string[MazeDefs.Length];
        for (var iMaze = 0; iMaze < MazeDefs.Length; iMaze++)
        {
            MazeDims[iMaze] = MazeDefs[iMaze].mDims;
            MazeNumSquares[iMaze] = MazeDefs[iMaze].mNumSquares;
            MazeNumTurns[iMaze] = MazeDefs[iMaze].mNumTurns;
            MazeStart[iMaze] = MazeDefs[iMaze].mStart;
            MazeFinish[iMaze] = MazeDefs[iMaze].mFinish;
            MazeName[iMaze] = MazeDefs[iMaze].mName;
            MazeString[iMaze] = MazeDefs[iMaze].mString;
        }
    }
}