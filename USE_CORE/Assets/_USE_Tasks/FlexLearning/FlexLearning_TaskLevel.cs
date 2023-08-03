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
    [HideInInspector] public int NumRewardPulses_InTask = 0;
    [HideInInspector] public int NumTokenBarFull_InTask = 0;
    [HideInInspector] public int TotalTokensCollected_InTask = 0;
    [HideInInspector] public int AbortedTrials_InTask = 0;
    [HideInInspector] public int NumCorrect_InTask = 0;
    [HideInInspector] public int NumErrors_InTask = 0;
    [HideInInspector] public List<float> SearchDurationsList_InTask;
    private double avgSearchDuration = 0;
    
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

        RunBlock.AddInitializationMethod(() =>
        {
            flTL.MinTrials = flBD.MinMaxTrials[0];
            flTL.TokensWithStimOn = flBD.TokensWithStimOn;
            flTL.ContextName = flBD.ContextName;
            
            ResetBlockVariables();

            SetSkyBox(flBD.ContextName, TaskCam.gameObject.GetComponent<Skybox>());
            
            //Set the Initial Token Values for the Block
            flTL.TokenFBController.SetTotalTokensNum(flBD.NumTokenBar);
            flTL.TokenFBController.SetTokenBarValue(flBD.NumInitialTokens);
            SetBlockSummaryString();
            
        });
        BlockFeedback.AddInitializationMethod(() =>
        {
            if(!SessionValues.WebBuild)
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
        flTL.SearchDurationsList.Clear();
        flTL.runningAcc.Clear();
        flTL.Accuracy_InBlock = 0;
        flTL.AverageSearchDuration_InBlock = 0;
        flTL.NumErrors_InBlock = 0;
        flTL.NumCorrect_InBlock = 0;
        flTL.NumRewardPulses_InBlock = 0;
        flTL.NumTokenBarFull_InBlock = 0;
        flTL.TotalTokensCollected_InBlock = 0;
    }

    public override OrderedDictionary GetBlockResultsData()
    {
        OrderedDictionary data = new OrderedDictionary
        {
            ["Trials Completed"] = flTL.TrialCount_InBlock + 1,
            ["Trials Correct"] = flTL.NumCorrect_InBlock,
            ["Accuracy"] = flTL.Accuracy_InBlock.ToString("0.00") + "%",
            ["Errors"] = flTL.NumErrors_InBlock,
            ["TokenBar Completions"] = flTL.NumTokenBarFull_InBlock,
        };
        return data;
    }

    public override OrderedDictionary GetTaskSummaryData()
    {
        OrderedDictionary data = new OrderedDictionary();

        data["Reward Pulses"] = NumRewardPulses_InTask;
        data["Token Bar Full"] = NumTokenBarFull_InTask;
        data["Total Tokens Collected"] = TotalTokensCollected_InTask;
        if(SearchDurationsList_InTask.Count > 0)
            data["Average Search Duration"] = SearchDurationsList_InTask.Average();
        if(flTL.TrialCount_InTask != 0)
            data["Accuracy"] = decimal.Divide(NumCorrect_InTask, (flTL.TrialCount_InTask));
        
        return data;
    }

    public void SetBlockSummaryString()
    {
        ClearStrings();
        BlockSummaryString.AppendLine("<b>Max Trials in Block: </b>" + flTL.MaxTrials + 
                                      "\nAccuracy: " + String.Format("{0:0.000}", (float)flTL.Accuracy_InBlock) +  
                                      "\n" + 
                                      "\nAvg Search Duration: " + String.Format("{0:0.000}", flTL.AverageSearchDuration_InBlock) +
                                      "\n" +
                                      "\nNum Reward Given: " + flTL.NumRewardPulses_InBlock + 
                                      "\nNum Token Bar Filled: " + flTL.NumTokenBarFull_InBlock +
                                      "\nTotal Tokens Collected: " + flTL.TotalTokensCollected_InBlock);
        BlockSummaryString.AppendLine(CurrentBlockString).ToString();
        /*if (PreviousBlocksString.Length > 0)
            BlockSummaryString.AppendLine(PreviousBlocksString.ToString());*/
    }
    public override void SetTaskSummaryString()
    {
        
        if (SearchDurationsList_InTask.Count > 0)
            avgSearchDuration = Math.Round(SearchDurationsList_InTask.Average(), 2);
        if (flTL.TrialCount_InTask != 0)
        {
            CurrentTaskSummaryString.Clear();
            CurrentTaskSummaryString.Append($"\n<b>{ConfigFolderName}</b>" + 
                                                        $"\n<b># Trials:</b> {flTL.TrialCount_InTask} ({(Math.Round(decimal.Divide(AbortedTrials_InTask,(flTL.TrialCount_InTask)),2))*100}% aborted)" + 
                                                        $"\t<b># Blocks:</b> {BlockCount}" + 
                                                        $"\t<b># Reward Pulses:</b> {NumRewardPulses_InTask}" +
                                                        $"\nAccuracy: {(Math.Round(decimal.Divide(NumCorrect_InTask,(flTL.TrialCount_InTask)),2))*100}%" + 
                                                        $"\tAvg Search Duration: {avgSearchDuration}" +
                                                        $"\n# Token Bar Filled: {NumTokenBarFull_InTask}" +
                                                        $"\n# Tokens Collected: {TotalTokensCollected_InTask}");
        }
        else
        {
            CurrentTaskSummaryString.Append($"\n<b>{ConfigFolderName}</b>");
        }
    }

    public void AssignBlockData()
    {
        BlockData.AddDatum("BlockAccuracy", ()=> (float)flTL.Accuracy_InBlock);
        BlockData.AddDatum("AvgSearchDuration", ()=> flTL.AverageSearchDuration_InBlock);
        BlockData.AddDatum("NumRewardGiven", ()=> flTL.NumRewardPulses_InBlock);
        BlockData.AddDatum("NumTokenBarFilled", ()=> flTL.NumTokenBarFull_InBlock);
        BlockData.AddDatum("TotalTokensCollected", ()=> flTL.TotalTokensCollected_InBlock);
    }
    public void ClearStrings()
    {
        CurrentBlockString = "";
        BlockSummaryString.Clear();
    }
    public void ResetTaskVariables()
    {
        NumCorrect_InTask = 0;
        NumErrors_InTask = 0;
        NumRewardPulses_InTask = 0;
        NumTokenBarFull_InTask = 0;
        TotalTokensCollected_InTask = 0;
        AbortedTrials_InTask = 0;
        SearchDurationsList_InTask.Clear();
    }
}