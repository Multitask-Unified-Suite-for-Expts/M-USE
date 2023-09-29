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
    [HideInInspector] public int NumTokenBarFull_InTask = 0;
    [HideInInspector] public int TotalTokensCollected_InTask = 0;
    [HideInInspector] public int NumCorrect_InTask = 0;
    [HideInInspector] public int NumErrors_InTask = 0;
    [HideInInspector] public List<float?> SearchDurations_InTask = new List<float?>();
    [HideInInspector] public string CurrentBlockString;
    [HideInInspector] public StringBuilder PreviousBlocksString;
    [HideInInspector] public int BlockStringsAdded = 0;
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
        BlockFeedback.AddSpecificInitializationMethod(() =>
        {
            if(!SessionValues.WebBuild)
            {/*
                if (BlockStringsAdded > 0)
                    CurrentBlockString += "\n";
                BlockStringsAdded++;
                PreviousBlocksString.Insert(0, CurrentBlockString);*/
            }
        });
        AssignBlockData();
    }

    public override OrderedDictionary GetBlockResultsData()
    {
        OrderedDictionary data = new OrderedDictionary
        {
            ["Accuracy"] = string.Format("{0:0.00}", (float)vsTL.Accuracy_InBlock),
            ["Trials Completed"] = vsTL.TrialCount_InBlock + 1,
            ["Avg Search Duration"] = CalculateAverageDuration(vsTL.SearchDurations_InBlock).ToString("0.0") + "s",
            ["TokenBar Completions"] = vsTL.NumTokenBarFull_InBlock
        };
        return data;
    }

    public override OrderedDictionary GetTaskSummaryData()
    {
        OrderedDictionary data = base.GetTaskSummaryData();

        data["Token Bar Full"] = NumTokenBarFull_InTask;
        data["Total Tokens Collected"] = TotalTokensCollected_InTask;
        
        if (SearchDurations_InTask.Count > 0)
            data["Average Search Duration"] = CalculateAverageDuration(SearchDurations_InTask);
        if(vsTL.TrialCount_InTask != 0)
            data["Accuracy"] = decimal.Divide(NumCorrect_InTask, (vsTL.TrialCount_InTask));
        
        return data;
    }
    public void SetBlockSummaryString()
    {
        ClearStrings();
        CurrentBlockSummaryString.AppendLine("<b>\nMin Trials in Block: </b>" + MinTrials_InBlock +
                                            "<b>\nMax Trials in Block: </b>" + MaxTrials_InBlock +
                                            "\n\nAccuracy: " + string.Format("{0:0.00}", (float)vsTL.Accuracy_InBlock) +  
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
}