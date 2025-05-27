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


using VisualSearch_Namespace;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using UnityEngine;
using USE_Settings;
using USE_ExperimentTemplate_Task;


public class VisualSearch_TaskLevel : ControlLevel_Task_Template
{
    [HideInInspector] public int NumTokenBarFull_InTask;
    [HideInInspector] public int TotalTokensCollected_InTask;
    [HideInInspector] public int NumCorrect_InTask;
    [HideInInspector] public int NumErrors_InTask;
    [HideInInspector] public List<float?> SearchDurations_InTask = new List<float?>();
    [HideInInspector] public string CurrentBlockString;
    [HideInInspector] public StringBuilder PreviousBlocksString;
    [HideInInspector] public int BlockStringsAdded;

    public List<VisualSearch_TrialDataSummary> AllTrialDataSummaries = new List<VisualSearch_TrialDataSummary>();
    VisualSearch_BlockDef vsBD => GetCurrentBlockDef<VisualSearch_BlockDef>();
    VisualSearch_TrialLevel vsTL;


    public override void DefineControlLevel()
    {
        vsTL = (VisualSearch_TrialLevel)TrialLevel;
        //vsTD = (VisualSearch_TrialDef)vsTL.GetCurrentTrialDef<VisualSearch_TrialDef>();
        CurrentBlockString = "";
        PreviousBlocksString = new StringBuilder();
        
        Add_ControlLevel_InitializationMethod(() =>
        {
            ResetTaskVariables();
        });
        
        RunBlock.AddSpecificInitializationMethod(() =>
        {
            vsBD.ContextName = vsBD.ContextName.Trim();
            SetSkyBox(vsBD.ContextName);
            vsTL.ContextName = vsBD.ContextName;
            vsTL.ResetBlockVariables();
            //Set the Initial Token Values for the Block
            vsTL.TokenFBController.SetTotalTokensNum(vsBD.TokenBarCapacity);
            vsTL.TokenFBController.SetTokenBarValue(vsBD.NumInitialTokens);

            MinTrials_InBlock = vsBD.MinTrials;
            MaxTrials_InBlock = vsBD.MaxTrials;
            
            SetBlockSummaryString();
        });
        AssignBlockData();
    }

    public override OrderedDictionary GetTaskSummaryData()
    {
        OrderedDictionary data = base.GetTaskSummaryData();

        data["Token Bar Full"] = NumTokenBarFull_InTask;
        data["Total Tokens Collected"] = TotalTokensCollected_InTask;

        data["\nPerformance Metrics"] = CreateTaskDataSummary();
        return data;
    }

    public override OrderedDictionary GetTaskResultsData()
    {
        OrderedDictionary data = base.GetTaskResultsData();

        int totalAttempts = NumCorrect_InTask + NumErrors_InTask;
        if (totalAttempts != 0)
            data["Accuracy"] = String.Format("{0:0.0}%", (float)NumCorrect_InTask / totalAttempts * 100);
        else
            data["Accuracy"] = "N/A"; // or "0.0%" or any other default value you prefer

        data["TokenBar Completions"] = NumTokenBarFull_InTask;
        data["Avg Search Duration"] = String.Format("{0:0.0} Seconds", SearchDurations_InTask.Average());

        return data;
    }

    public void SetBlockSummaryString()
    {
        ClearStrings();
        CurrentBlockSummaryString.AppendLine("\nMin Trials in Block: " + MinTrials_InBlock +
                                            "\nMax Trials in Block: " + MaxTrials_InBlock +
                                            "\nAccuracy: " + string.Format("{0:0.00}", (float)vsTL.Accuracy_InBlock) +  
                                      "\nAvg Search Duration: " + string.Format("{0:0.00}", CalculateAverageDuration(vsTL.SearchDurations_InBlock)) +
                                      "\nNum Aborted Trials: " + + NumAbortedTrials_InBlock + 
                                      "\nNum Reward Given: " + NumRewardPulses_InBlock + 
                                      "\nNum Token Bar Filled: " + vsTL.NumTokenBarFull_InBlock +
                                      "\nTotal Tokens Collected: " + vsTL.TotalTokensCollected_InBlock);
    }

    public override void SetTaskSummaryString()
    {
        CurrentTaskSummaryString.Clear();
        base.SetTaskSummaryString();

        double avgSearchDuration = 0;
        if (SearchDurations_InTask.Count > 0)
            avgSearchDuration = Math.Round(CalculateAverageDuration(SearchDurations_InTask), 2);

        if (vsTL.TrialCount_InTask != 0)
        {
            CurrentTaskSummaryString.Append($"\nAccuracy: {(Math.Round(decimal.Divide(NumCorrect_InTask,(vsTL.TrialCount_InTask)),2))*100}%" + 
                                                    $"\tAvg Search Duration: {avgSearchDuration}" +
                                                    $"\n# Token Bar Filled: {NumTokenBarFull_InTask}" +
                                                    $"\n# Tokens Collected: {TotalTokensCollected_InTask}");
        }
            
    }
    

    public void AssignBlockData()
    {
        BlockData.AddDatum("Block Accuracy", ()=> (float)vsTL.Accuracy_InBlock);
        BlockData.AddDatum("Search Durations", ()=> String.Join(",", vsTL.SearchDurations_InBlock));
        BlockData.AddDatum("Num Token Bar Filled", ()=> vsTL.NumTokenBarFull_InBlock);
        BlockData.AddDatum("Total Tokens Collected", ()=> vsTL.TotalTokensCollected_InBlock);
    }
    public void ClearStrings()
    {
        CurrentBlockSummaryString.Clear();
    }
    public void ResetTaskVariables()
    {
        NumCorrect_InTask = 0;
        NumErrors_InTask = 0;
        NumTokenBarFull_InTask = 0;
        TotalTokensCollected_InTask = 0;
        SearchDurations_InTask.Clear();
    }

    public string CreateTaskDataSummary()
    {
        VisualSearch_TaskDataSummary taskDataSummary = new VisualSearch_TaskDataSummary();
        string taskDataSummaryString;
        // Pre-calculate common values used in multiple calculations
        var validTrials = AllTrialDataSummaries.Where(trial => trial.ReactionTime.HasValue && trial.SelectionPrecision.HasValue && trial.CorrectSelection.HasValue).ToList();
        var totalTrials = validTrials.Count;
        var totalCorrectSelections = validTrials.Count(trial => trial.CorrectSelection == 1);

        // Single iteration for reaction time and selection precision calculations
        if (totalTrials > 0)
        {
            taskDataSummary.AvgReactionTime = validTrials.Average(trial => trial.ReactionTime.Value);
            taskDataSummary.AvgSelectionPrecision = validTrials.Average(trial => trial.SelectionPrecision.Value);
            taskDataSummary.TotalAccuracy = (float)totalCorrectSelections / totalTrials;
        }
        else
        {
            taskDataSummary.AvgReactionTime = -1f;
            taskDataSummary.AvgSelectionPrecision = -1f;
            taskDataSummary.TotalAccuracy = 0f;
        }

        // Calculate Median Feature Similarity, High/Low Similarity Accuracy, and Distractor Interference
        CalculatePerceptualInterference(taskDataSummary, validTrials);
        CalculateDistractorInterference(taskDataSummary, validTrials);

        taskDataSummaryString = $"\nTotal Accuracy: {taskDataSummary.TotalAccuracy:F4}" +
                        $"\nAverage Reaction Time: {taskDataSummary.AvgReactionTime:F4}" +
                        $"\nAverage Selection Precision: {taskDataSummary.AvgSelectionPrecision:F4}" +
                        $"\nDistractor Interference on Reaction Time: {taskDataSummary.DistractorInterferenceReactionTime:F4}" +
                        $"\nDistractor Interference on Accuracy: {taskDataSummary.DistractorInterferenceAccuracy:F4}" +
                        $"\nMedian Feature Similarity: {taskDataSummary.MedianFeatureSimilarity}" +
                        $"\nHigh Feature Similarity Accuracy: {taskDataSummary.HighFeatureSimilarityAccuracy:F4}" +
                        $"\nLow Feature Similarity Accuracy: {taskDataSummary.LowFeatureSimilarityAccuracy:F4}\n";

        return taskDataSummaryString;
    }

    private void CalculatePerceptualInterference(VisualSearch_TaskDataSummary taskDataSummary, List<VisualSearch_TrialDataSummary> validTrials)
    {
        // Filter out the trials that only contain a single stim, indcated by a feature similarity of -1
        validTrials = validTrials.Where(trialDataSummary => trialDataSummary.FeatureSimilarity.HasValue && trialDataSummary.FeatureSimilarity.Value != -1).ToList();

        var featureSimilarityValues = validTrials
            .Where(trialDataSummary => trialDataSummary.FeatureSimilarity != -1)
            .Select(trialDataSummary => trialDataSummary.FeatureSimilarity.Value)
            .OrderBy(value => value)
            .ToList();

        int count = featureSimilarityValues.Count;
        if (count > 0)
        {
            if (count % 2 == 0)
                taskDataSummary.MedianFeatureSimilarity = (featureSimilarityValues[count / 2 - 1] + featureSimilarityValues[count / 2]) / 2f;
            else
                taskDataSummary.MedianFeatureSimilarity = featureSimilarityValues[count / 2];
        }
        else
        {
            taskDataSummary.MedianFeatureSimilarity = -1f; // Or another default/error value
            return;
        }

        // Filter trials into high and low similarity based on the median
        var highSimilarityTrials = validTrials
            .Where(trial => trial.FeatureSimilarity.Value >= taskDataSummary.MedianFeatureSimilarity);

        var lowSimilarityTrials = validTrials
            .Where(trial => trial.FeatureSimilarity.Value < taskDataSummary.MedianFeatureSimilarity);

        // Calculate proportion of correct selections for high similarity
        taskDataSummary.HighFeatureSimilarityAccuracy = highSimilarityTrials.Any() ?
            highSimilarityTrials.Average(trial => trial.CorrectSelection) : 0f;

        // Calculate proportion of correct selections for low similarity
        taskDataSummary.LowFeatureSimilarityAccuracy = lowSimilarityTrials.Any() ?
            lowSimilarityTrials.Average(trial => trial.CorrectSelection) : 0f;
    }


    private void CalculateDistractorInterference(VisualSearch_TaskDataSummary taskDataSummary, List<VisualSearch_TrialDataSummary> validTrials)
    {
        // Calculate slope for Reaction Time vs. Number of Distractors
        var N = validTrials.Count;
        var sumX = validTrials.Sum(trial => (double)trial.NumDistractors);
        var sumYReactionTime = validTrials.Sum(trial => trial.ReactionTime.Value);
        var sumXYReactionTime = validTrials.Sum(trial => trial.NumDistractors * trial.ReactionTime.Value);
        var sumX2 = validTrials.Sum(trial => Math.Pow(trial.NumDistractors, 2));

        taskDataSummary.DistractorInterferenceReactionTime = (N * sumXYReactionTime - sumX * sumYReactionTime) / (N * sumX2 - Math.Pow(sumX, 2));

        // Calculate slope for Accuracy vs. Number of Distractors
        var sumYAccuracy = validTrials.Sum(trial => (double)trial.CorrectSelection);
        var sumXYAccuracy = validTrials.Sum(trial => trial.NumDistractors * trial.CorrectSelection);

        taskDataSummary.DistractorInterferenceAccuracy = (N * sumXYAccuracy - sumX * sumYAccuracy) / (N * sumX2 - Math.Pow(sumX, 2));

    }


}