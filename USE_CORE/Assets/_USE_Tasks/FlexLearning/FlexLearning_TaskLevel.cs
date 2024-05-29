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


using FlexLearning_Namespace;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using UnityEngine;
using USE_Settings;
using USE_ExperimentTemplate_Task;


public class FlexLearning_TaskLevel : ControlLevel_Task_Template
{
   // [HideInInspector] public int NumRewardPulses_InTask = 0;
    [HideInInspector] public int NumTokenBarFull_InTask = 0;
    [HideInInspector] public int TotalTokensCollected_InTask = 0;
    [HideInInspector] public int NumCorrect_InTask = 0;
    [HideInInspector] public int NumErrors_InTask = 0;
    [HideInInspector] public List<float?> SearchDurations_InTask = new List<float?>();
    
    [HideInInspector] public string CurrentBlockString;
    [HideInInspector] public StringBuilder PreviousBlocksString;
    [HideInInspector] public int BlockStringsAdded = 0;
    FlexLearning_BlockDef flBD => GetCurrentBlockDef<FlexLearning_BlockDef>();
    FlexLearning_TrialLevel flTL;

    public override void DefineControlLevel()
    {   
        flTL = (FlexLearning_TrialLevel)TrialLevel;
        
        CurrentBlockString = "";
        PreviousBlocksString = new StringBuilder();
        
        Add_ControlLevel_InitializationMethod(() =>
        {
            ResetTaskVariables();
        });

        RunBlock.AddSpecificInitializationMethod(() =>
        {
            MinTrials_InBlock = flBD.RandomMinMaxTrials[0];
            MaxTrials_InBlock = flBD.MaxTrials;
            flTL.TokensWithStimOn = flBD.TokensWithStimOn;
            flTL.ContextName = flBD.ContextName;

            ResetBlockVariables();

            SetSkyBox(flBD.ContextName);
            
            //Set the Initial Token Values for the Block
            flTL.TokenFBController.SetTotalTokensNum(flBD.TokenBarCapacity);
            flTL.TokenFBController.SetTokenBarValue(flBD.NumInitialTokens);
            SetBlockSummaryString();
            
        });
        BlockFeedback.AddSpecificInitializationMethod(() =>
        {
            if(!Session.WebBuild)
            {
                if (BlockStringsAdded > 0)
                    CurrentBlockString += "\n";
                BlockStringsAdded++;
                PreviousBlocksString.Insert(0, CurrentBlockString);
            }
        });
        AssignBlockData();
    }

    private void ResetBlockVariables()
    {
        flTL.SearchDurations_InBlock.Clear();
        flTL.runningAcc.Clear();
        flTL.Accuracy_InBlock = 0;
        flTL.NumErrors_InBlock = 0;
        flTL.NumCorrect_InBlock = 0;
        flTL.NumTokenBarFull_InBlock = 0;
        flTL.TotalTokensCollected_InBlock = 0;
    }

    public override OrderedDictionary GetTaskSummaryData()
    {
        OrderedDictionary data = base.GetTaskSummaryData();
        data["Token Bar Full"] = NumTokenBarFull_InTask;
        data["Total Tokens Collected"] = TotalTokensCollected_InTask;
        if(flTL.TrialCount_InTask != 0)
            data["Accuracy"] = decimal.Divide(NumCorrect_InTask, (flTL.TrialCount_InTask));
        if(SearchDurations_InTask.Count > 0)
            data["Avg Search Duration"] = CalculateAverageDuration(SearchDurations_InTask);
        
        return data;
    }
    // public string CreateTaskDataSummary()
    // {
    //     FlexLearning_TaskDataSummary taskDataSummary = new FlexLearning_TaskDataSummary();
    //     string taskDataSummaryString;
    //     // Pre-calculate common values used in multiple calculations
    //     var validTrials = AllTrialDataSummaries.Where(trial => trial.ReactionTime.HasValue && trial.SelectionPrecision.HasValue && trial.CorrectSelection.HasValue).ToList();
    //     var totalTrials = validTrials.Count;
    //     var totalCorrectSelections = validTrials.Count(trial => trial.CorrectSelection == 1);
    //
    //     // Single iteration for reaction time and selection precision calculations
    //     if (totalTrials > 0)
    //     {
    //         taskDataSummary.AvgReactionTime = validTrials.Average(trial => trial.ReactionTime.Value);
    //         taskDataSummary.AvgSelectionPrecision = validTrials.Average(trial => trial.SelectionPrecision.Value);
    //         taskDataSummary.TotalAccuracy = (float)totalCorrectSelections / totalTrials;
    //     }
    //     else
    //     {
    //         taskDataSummary.AvgReactionTime = -1f;
    //         taskDataSummary.AvgSelectionPrecision = -1f;
    //         taskDataSummary.TotalAccuracy = 0f;
    //     }
    //
    //     // Calculate Median Feature Similarity, High/Low Similarity Accuracy, and Distractor Interference
    //     CalculatePerceptualInterference(taskDataSummary, validTrials);
    //     CalculateDistractorInterference(taskDataSummary, validTrials);
    //
    //     taskDataSummaryString = $"\nTotal Accuracy: {taskDataSummary.TotalAccuracy:F4}" +
    //                     $"\nAverage Reaction Time: {taskDataSummary.AvgReactionTime:F4}" +
    //                     $"\nAverage Selection Precision: {taskDataSummary.AvgSelectionPrecision:F4}" +
    //                     $"\n\nDistractor Interference on Reaction Time: {taskDataSummary.DistractorInterferenceReactionTime:F4}" +
    //                     $"\nDistractor Interference on Accuracy: {taskDataSummary.DistractorInterferenceAccuracy:F4}" +
    //                     $"\n\nMedian Feature Similarity: {taskDataSummary.MedianFeatureSimilarity}" +
    //                     $"\nHigh Feature Similarity Accuracy: {taskDataSummary.HighFeatureSimilarityAccuracy:F4}" +
    //                     $"\nLow Feature Similarity Accuracy: {taskDataSummary.LowFeatureSimilarityAccuracy:F4}\n";
    //
    //     return taskDataSummaryString;
    // }

    public override OrderedDictionary GetTaskResultsData()
    {
        OrderedDictionary data = base.GetTaskResultsData();

        if (flTL.TrialCount_InTask != 0)
            data["Accuracy"] = String.Format("{0:0.0}" + "%", decimal.Divide(NumCorrect_InTask, flTL.TrialCount_InTask) * 100);

        if (SearchDurations_InTask.Count > 0)
            data["Avg Search Duration"] = String.Format("{0:0.0} " + "Seconds", CalculateAverageDuration(SearchDurations_InTask));

        data["TokenBar Completions"] = NumTokenBarFull_InTask;

        return data;
    }

    public void SetBlockSummaryString()
    {
        ClearStrings();
        CurrentBlockSummaryString.AppendLine("<b>Max Trials in Block: </b>" + MaxTrials_InBlock + 
                                      "\nAccuracy: " + String.Format("{0:0.000}", (float)flTL.Accuracy_InBlock) +  
                                      "\n" + 
                                      "\nAvg Search Duration: " + String.Format("{0:0.000}", CalculateAverageDuration(flTL.SearchDurations_InBlock)) +
                                      "\n" +
                                      "\nNum Reward Given: " + NumRewardPulses_InBlock + 
                                      "\nNum Token Bar Filled: " + flTL.NumTokenBarFull_InBlock +
                                      "\nTotal Tokens Collected: " + flTL.TotalTokensCollected_InBlock);
        CurrentBlockSummaryString.AppendLine(CurrentBlockString).ToString();
        /*if (PreviousBlocksString.Length > 0)
            CurrentBlockSummaryString.AppendLine(PreviousBlocksString.ToString());*/
    }
    public override void SetTaskSummaryString()
    {
        CurrentTaskSummaryString.Clear();
        base.SetTaskSummaryString();

        double avgSearchDuration = 0;
        if (SearchDurations_InTask.Count > 0)
            avgSearchDuration = Math.Round(CalculateAverageDuration(SearchDurations_InTask), 2);

        if (flTL.TrialCount_InTask != 0)
        {
            CurrentTaskSummaryString.Append($"\nAccuracy: {(Math.Round(decimal.Divide(NumCorrect_InTask,(flTL.TrialCount_InTask)),2))*100}%" + 
                                                        $"\tAvg Search Duration: {avgSearchDuration}" +
                                                        $"\n# Token Bar Filled: {NumTokenBarFull_InTask}" +
                                                        $"\n# Tokens Collected: {TotalTokensCollected_InTask}");
        }
    }

    public void AssignBlockData()
    {
        BlockData.AddDatum("MinTrials", () => MinTrials_InBlock);
        BlockData.AddDatum("MaxTrials", () => MaxTrials_InBlock);
        BlockData.AddDatum("BlockAccuracy", ()=> (float)flTL.Accuracy_InBlock);
        BlockData.AddDatum("SearchDurations", ()=> String.Join(",", flTL.SearchDurations_InBlock));
        BlockData.AddDatum("NumTokenBarFilled", ()=> flTL.NumTokenBarFull_InBlock);
        BlockData.AddDatum("TotalTokensCollected", ()=> flTL.TotalTokensCollected_InBlock);
    }
    public void ClearStrings()
    {
        CurrentBlockString = "";
        CurrentBlockSummaryString.Clear();
    }
    public void ResetTaskVariables()
    {
        NumCorrect_InTask = 0;
        NumErrors_InTask = 0;
        NumRewardPulses_InTask = 0;
        NumTokenBarFull_InTask = 0;
        TotalTokensCollected_InTask = 0;
        SearchDurations_InTask.Clear();
    }
}