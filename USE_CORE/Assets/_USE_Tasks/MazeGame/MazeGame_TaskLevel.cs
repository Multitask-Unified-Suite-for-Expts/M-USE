/*
MIT License

Copyright (c) 2023 Multitask - Unified - Suite -for-Expts

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files(the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/


using System;
using System.Collections;
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
using static UnityEngine.LightProbeProxyVolume;
using Random = UnityEngine.Random;

public class MazeGame_TaskLevel : ControlLevel_Task_Template
{
    // Config Loading Variables
    //public MazeDef[] MazeDefs;

    
    
    
    // Block Data Tracking Variables
    [HideInInspector]
    public int TotalErrors_InBlock;
    public int PerseverativeErrors_InBlock;
    public int BacktrackErrors_InBlock;
    public int RuleAbidingErrors_InBlock;
    public int RuleBreakingErrors_InBlock;
    public int RetouchCorrect_InBlock;
    public int RetouchErroneous_InBlock;
    public int CorrectTouches_InBlock; 
    public int NumSliderBarFull_InBlock;
    public List<float?> MazeDurations_InBlock = new List<float?>();
    public List<float?> ChoiceDurations_InBlock = new List<float?>();

    // Task Data Tracking Variables
    [HideInInspector]
    public int TotalErrors_InTask;
    public int PerseverativeErrors_InTask;
    public int BacktrackErrors_InTask;
    public int RuleAbidingErrors_InTask;
    public int RuleBreakingErrors_InTask;
    public int RetouchCorrect_InTask;
    public int RetouchErroneous_InTask;
    public int CorrectTouches_InTask;
    public int NumSliderBarFull_InTask;
    public List<float?> MazeDurations_InTask;
    public List<float?> ChoiceDurations_InTask;

    
    // Block Summary String Variables
    [HideInInspector] public string BlockAveragesString;
    [HideInInspector] public string CurrentBlockString;
    [HideInInspector] public StringBuilder PreviousBlocksString;
    private int blocksAdded = 0;
    public MazeGame_BlockDef mgBD => GetCurrentBlockDef<MazeGame_BlockDef>();
    private MazeGame_TaskDef currentTaskDef => GetTaskDef<MazeGame_TaskDef>();
    private MazeGame_TrialLevel mgTL;

    public override void DefineControlLevel()
    {
        MazeDurations_InTask = new List<float?>();
        ChoiceDurations_InTask = new List<float?>();
        
        mgTL = (MazeGame_TrialLevel)TrialLevel;
        
        
        //SetMazePaths();

        DefineBlockData();
        
        BlockAveragesString = "";
        CurrentBlockString = "";
        PreviousBlocksString = new StringBuilder();

        
        blocksAdded = 0;
        //LoadMazeDef();

        RunBlock.AddSpecificInitializationMethod(() =>
        {
            //MazeDefs = customSettings[0].AssignCustomSetting<MazeDef[]>();
            //InitializeMazeSearchingArrays();

            SetSkyBox(mgBD.ContextName);
            
            mgTL.ContextName = mgBD.ContextName;
            MinTrials_InBlock = mgBD.MinTrials;
            MaxTrials_InBlock = mgBD.MaxTrials;

            
            //FindMaze();
            mgTL.MazeManager.LoadTextMaze(mgBD);
            //StartCoroutine(LoadTextMaze()); // need currMaze here to set all the arrays

            CalculateBlockSummaryString();
            ResetBlockVariables();

        });
        RunBlock.AddDefaultTerminationMethod(() =>
        {
            mgTL.MazeManager.MazeCleanUp();
        });
    }


    public void DefineBlockData()
    {
        BlockData.AddDatum("BlockName", () => mgBD.BlockName);
        BlockData.AddDatum("MinTrials", () => MinTrials_InBlock);
        BlockData.AddDatum("MaxTrials", () => MaxTrials_InBlock);
        BlockData.AddDatum("TotalErrors", () => $"[{string.Join(", ", TotalErrors_InBlock)}]");
        BlockData.AddDatum("CorrectTouches", () => CorrectTouches_InBlock);
        BlockData.AddDatum("RetouchCorrect",() => $"[{string.Join(", ", RetouchCorrect_InBlock)}]");
        BlockData.AddDatum("RetouchErroneous",() => $"[{string.Join(", ", RetouchErroneous_InBlock)}]");
        BlockData.AddDatum("PerseverativeErrors",() => $"[{string.Join(", ", PerseverativeErrors_InBlock)}]");
        BlockData.AddDatum("BacktrackErrors", () => $"[{string.Join(", ", BacktrackErrors_InBlock)}]");
        BlockData.AddDatum("RuleAbidingErrors", () => $"[{string.Join(", ", RuleAbidingErrors_InBlock)}]");
        BlockData.AddDatum("RuleBreakingErrors", () => $"[{string.Join(", ", RuleBreakingErrors_InBlock)}]");
        BlockData.AddDatum("NumSliderBarFull", ()=>NumSliderBarFull_InBlock);
        BlockData.AddDatum("MazeDurations", () => string.Join(",",MazeDurations_InBlock));
        BlockData.AddDatum("ChoiceDurations", () => string.Join(",", ChoiceDurations_InBlock));
       // BlockData.AddDatum("NumNonStimSelections", () => mgTrialLevel.NonStimTouches_InBlock);
    }
    public override OrderedDictionary GetBlockResultsData()
    {
        OrderedDictionary data = new OrderedDictionary
        {
            ["Maze Duration"] = mgTL.MazeManager.mazeDuration.ToString("0.0") + "s",
            ["Correct Touches"] = CorrectTouches_InBlock,
            ["Total Errors"] = TotalErrors_InBlock,
            ["Retouched Correct"] = RetouchCorrect_InBlock,
            ["Retouched Erroneous"] = RetouchErroneous_InBlock,
        };
        return data;
    }

    public override OrderedDictionary GetTaskSummaryData()
    {

        OrderedDictionary data = base.GetTaskSummaryData();
        data["Num Slider Bar Full"] = NumSliderBarFull_InTask;
        data["Total Errors"] = TotalErrors_InTask;
        data["Correct Touches"] = CorrectTouches_InTask;
        data["Retouch Correct"] = RetouchCorrect_InTask;
        data["Retouch Erroneous"] = RetouchErroneous_InTask;
        data["Perseverative Errors"] = PerseverativeErrors_InTask;
        data["Backtrack Errors"] = BacktrackErrors_InTask;
        data["Rule-Abiding Errors"] = RuleAbidingErrors_InTask;
        data["Rule-Breaking Errors"] = RuleBreakingErrors_InTask;
        data["Average Maze Durations"] = CalculateAverageDuration(MazeDurations_InTask);
        data["Average Choice Duration"] = CalculateAverageDuration(ChoiceDurations_InTask);

        return data;
    }
    private void ResetBlockVariables()
    {
        CorrectTouches_InBlock = 0;
        RuleAbidingErrors_InBlock = 0;
        RuleBreakingErrors_InBlock = 0;
        BacktrackErrors_InBlock = 0;
        PerseverativeErrors_InBlock = 0;
        RetouchCorrect_InBlock = 0;
        RetouchErroneous_InBlock = 0;
        TotalErrors_InBlock = 0;
        NumSliderBarFull_InBlock = 0;
        MazeDurations_InBlock.Clear();
        ChoiceDurations_InBlock.Clear();
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
        CurrentBlockString = "<b>\nMin Trials in Block: </b>" + MinTrials_InBlock +
                             "<b>\nMax Trials in Block: </b>" + MaxTrials_InBlock +
                             "<b>\nLearning Criterion: </b>" + String.Format("{0:0.00}%", mgBD.BlockEndThreshold*100) +
                             "\n\nLast Trial's Percent Error: " + (latestPercentError == null ?
                                 ("N/A"):String.Format("{0:0.00}%", latestPercentError)) +
                             "\nTotal Errors: " + TotalErrors_InBlock +
                             "\nRule-Abiding Errors: " + RuleAbidingErrors_InBlock +
                             "\nRule-Breaking Errors: " + RuleBreakingErrors_InBlock +
                             "\nPerseverative Errors: " + PerseverativeErrors_InBlock+
                             "\nBacktrack Errors: " + BacktrackErrors_InBlock +
                             "\nRetouch Correct: " + RetouchCorrect_InBlock +
                             "\nRetouch Erroneous: " + RetouchErroneous_InBlock +
                             "\n\nRewards: " + NumRewardPulses_InBlock +
                             "\nAverage Choice Duration: " +
                             String.Format("{0:0.00}", CalculateAverageDuration(ChoiceDurations_InBlock)) +
                             "\nAverage Maze Duration: " +
                             String.Format("{0:0.00}", CalculateAverageDuration(MazeDurations_InBlock));
        
        CurrentBlockSummaryString.AppendLine(CurrentBlockString).ToString();
        if (PreviousBlocksString.Length > 0)
            CurrentBlockSummaryString.AppendLine(PreviousBlocksString.ToString());
    }
    public override void SetTaskSummaryString()
    {
        CurrentTaskSummaryString.Clear();
        base.SetTaskSummaryString();
        
        CurrentTaskSummaryString.Append($"\n<b># Rule-Break Errors:</b> {RuleBreakingErrors_InTask}" +
                                        $"\t<b># Rule-Abiding Errors:</b> {RuleAbidingErrors_InTask}" +
                                        $"\nAvg Maze Duration: {CalculateAverageDuration(MazeDurations_InTask)}" +
                                        $"\n# Slider Bar Filled: {NumSliderBarFull_InTask}");

    }
    public void ClearStrings()
    {
        BlockAveragesString = "";
        CurrentBlockString = "";
        CurrentBlockSummaryString.Clear();
    }


/*    private void FindMaze()
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

        mgTrialLevel.mazeDefName = MazeName[mIndex];
    }*/

    


    /*public override List<CustomSettings> DefineCustomSettings()
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
    }*/

    
}