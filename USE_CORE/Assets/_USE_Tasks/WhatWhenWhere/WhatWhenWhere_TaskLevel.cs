using WhatWhenWhere_Namespace;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;
using USE_ExperimentTemplate_Task;


public class WhatWhenWhere_TaskLevel : ControlLevel_Task_Template
{
    WhatWhenWhere_BlockDef wwwBD => GetCurrentBlockDef<WhatWhenWhere_BlockDef>();
    WhatWhenWhere_TrialLevel wwwTL;
    public int NumSliderBarFilled_InTask;
    public List<float?> SearchDurations_InTask = new List<float?>();

    
    //Block Data Logging Variables
    public List<float?> SearchDurations_InBlock = new List<float?>();
    public int RepetitionErrorCount_InBlock;
    public int SlotErrorCount_InBlock;
    public int DistractorSlotErrorCount_InBlock;
    public int NumCorrectSelections_InBlock;
    public int NumErrors_InBlock;
    
    
    public int LearningSpeed = -1;

    // Block Summary String Variables
    [HideInInspector] public string BlockAveragesString;
    public override void DefineControlLevel()
    {
        wwwTL = (WhatWhenWhere_TrialLevel)TrialLevel;

        DefineBlockData();
        
        BlockAveragesString = "";
        
        RunBlock.AddSpecificInitializationMethod(() =>
        {
            LearningSpeed = -1;
            
            MinTrials_InBlock = wwwBD.MinMaxTrials[0];
            MaxTrials_InBlock = wwwBD.MaxTrials;
            
            SetSkyBox(wwwBD.ContextName);

            ResetBlockVariables();
            SetBlockSummaryString();
        });
    }

    public override OrderedDictionary GetBlockResultsData()
    {
        OrderedDictionary data = new OrderedDictionary
        {
            ["Trials Completed"] = wwwTL.TrialCount_InBlock + 1,
            ["Stim Chosen Correctly"] = NumCorrectSelections_InBlock,
            ["Errors"] = NumErrors_InBlock
        };
        return data;
    }

    public override OrderedDictionary GetTaskSummaryData()
    {
        OrderedDictionary data = new OrderedDictionary
        {
            ["Trial Count In Task"] = wwwTL.TrialCount_InTask + 1,
            ["Num Reward Pulses"] = NumRewardPulses_InTask,
            ["Slider Bar Full"] = NumSliderBarFilled_InTask,
            ["Aborted Trials In Task"] = NumAbortedTrials_InTask,
            ["Avg Search Duraiton"] = CalculateAverageSearchDuration(SearchDurations_InTask)
        };
        
        
        return data;
    }
    public void SetBlockSummaryString()
    {
        ClearStrings();
        CurrentBlockSummaryString.AppendLine( "<b>\nMax Trials in Block: </b>" + wwwTL.CurrentTrialDef.MaxTrials + 
                                      "\n\nAverage Search Duration: " + CalculateAverageSearchDuration(SearchDurations_InBlock) +
                                      "\n" +
                                      "\nDistractor Slot Error Count: " + DistractorSlotErrorCount_InBlock+
                                      "\nNon-Distractor Slot Error Count: " + SlotErrorCount_InBlock + 
                                      "\nRepetition Error Count: "  + RepetitionErrorCount_InBlock +
                                      "\nNum Aborted Trials in Block: " + NumAbortedTrials_InBlock);
    }
    public override void SetTaskSummaryString()
    {
        if (wwwTL.TrialCount_InTask != 0)
        {
            CurrentTaskSummaryString.Clear();

            decimal percentAbortedTrials = (Math.Round(decimal.Divide(NumAbortedTrials_InTask, (wwwTL.TrialCount_InTask)), 2)) * 100;

            CurrentTaskSummaryString.Append($"\n<b>{ConfigFolderName}</b>" +
                                            $"\n<b># Trials:</b> {wwwTL.TrialCount_InTask} ({percentAbortedTrials}% aborted)" +
                                            $"\t<b># Blocks:</b> {BlockCount}" +
                                            $"\t<b># Reward Pulses:</b> {NumRewardPulses_InTask}" +
                                            $"\n# Slider Bar Completions: {NumSliderBarFilled_InTask}" + 
                                            $"\nAvg Search Duration: {CalculateAverageSearchDuration(SearchDurations_InTask)}");
        }
        else
        {
            CurrentTaskSummaryString.Append($"\n<b>{ConfigFolderName}</b>");
        }
            
    }

    private void DefineBlockData()
    {
        BlockData.AddDatum("MinTrials", () => MinTrials_InBlock);
        BlockData.AddDatum("MaxTrials", () => MaxTrials_InBlock);
        BlockData.AddDatum("LearningSpeed", () => LearningSpeed);
        BlockData.AddDatum("AvgSearchDuration", ()=> CalculateAverageSearchDuration(SearchDurations_InBlock));
        BlockData.AddDatum("NumDistractorSlotError", ()=> DistractorSlotErrorCount_InBlock);
        BlockData.AddDatum("NumSearchSlotError", ()=> SlotErrorCount_InBlock);
        BlockData.AddDatum("NumRepetitionError", ()=> RepetitionErrorCount_InBlock);
        BlockData.AddDatum("NumAbortedTrials", ()=> NumAbortedTrials_InBlock);
        BlockData.AddDatum("NumRewardPulses", ()=> NumRewardPulses_InTask);
    }

    public void ClearStrings()
    {
        CurrentBlockSummaryString.Clear();
    }
    public void ResetBlockVariables()
    {
        SlotErrorCount_InBlock = 0;
        DistractorSlotErrorCount_InBlock = 0;
        RepetitionErrorCount_InBlock = 0;
        NumCorrectSelections_InBlock = 0;
        NumErrors_InBlock = 0;
        SearchDurations_InBlock.Clear();
        wwwTL.consecutiveError = 0;
        
        wwwTL.runningAcc.Clear();
    }
    public float CalculateAverageSearchDuration(List<float?> searchDurations)
    {
        float avgSearchDuration;
        if (searchDurations.Any(item => item.HasValue))
        {
            avgSearchDuration = (float)searchDurations
                .Where(item => item.HasValue)
                .Average(item => item.Value);
        }
        else
        {
            avgSearchDuration = 0f;
        }

        return avgSearchDuration;
    }
}