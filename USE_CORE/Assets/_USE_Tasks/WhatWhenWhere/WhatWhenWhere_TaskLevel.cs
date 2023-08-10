using WhatWhenWhere_Namespace;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using UnityEngine;
using USE_ExperimentTemplate_Task;


public class WhatWhenWhere_TaskLevel : ControlLevel_Task_Template
{
    WhatWhenWhere_BlockDef wwwBD => GetCurrentBlockDef<WhatWhenWhere_BlockDef>();
    WhatWhenWhere_TrialLevel wwwTL;
    public int[] NumTotal_InTask = new int[100]; // hard coding 100 to instantiate array, only an issue if more than 100 obj seq, not great
    public int[] NumErrors_InTask = new int[100];
    public int[] NumCorrect_InTask = new int[100];    
    public List<string> ErrorType_InTask = new List<string>();
    public int AbortedTrials_InTask = 0;
    public int NumRewardPulses_InTask;
    public int NumSliderBarFilled_InTask;

    public int LearningSpeed = -1;
    public List<float> SearchDurations_InTask;

    // Block Summary String Variables
    [HideInInspector] public string BlockAveragesString;
    [HideInInspector] public string CurrentBlockString;
    [HideInInspector] public StringBuilder PreviousBlocksString;
    private int blocksAdded = 0;
    public override void DefineControlLevel()
    {
        wwwTL = (WhatWhenWhere_TrialLevel)TrialLevel;

        DefineBlockData();
        
        BlockAveragesString = "";
        CurrentBlockString = "";
        PreviousBlocksString = new StringBuilder();
        
        RunBlock.AddInitializationMethod(() =>
        {
            LearningSpeed = -1;

            wwwTL.ContextName = wwwBD.ContextName;

            SetSkyBox(wwwBD.ContextName);

            ErrorType_InTask.Add(string.Join(",",wwwTL.ErrorType_InBlock));
            wwwTL.ResetBlockVariables();
            //wwwTL.MinTrials = wwwBD.nRepetitionsMinMax[0];
            SetBlockSummaryString();
        });
    }

    public override OrderedDictionary GetBlockResultsData()
    {
        OrderedDictionary data = new OrderedDictionary
        {
            ["Trials Completed"] = wwwTL.TrialCount_InBlock + 1,
            ["Stim Chosen Correctly"] = wwwTL.numCorrect_InBlock.Sum(),
            ["Errors"] = wwwTL.numErrors_InBlock.Sum()
        };
        return data;
    }

    public override OrderedDictionary GetTaskSummaryData()
    {
        OrderedDictionary data = new OrderedDictionary
        {
            ["Trial Count In Task"] = wwwTL.TrialCount_InTask,
            ["Num Reward Pulses"] = NumRewardPulses_InTask,
            ["Slider Bar Full"] = NumSliderBarFilled_InTask,
            ["Aborted Trials In Task"] = AbortedTrials_InTask
        };
        if (SearchDurations_InTask.Count > 0)
            data["Average Search Duration"] = SearchDurations_InTask.Average();
        
        return data;
    }
    public void SetBlockSummaryString()
    {
        ClearStrings();
        float avgBlockSearchDuration = 0;
        if (wwwTL.searchDurations_InBlock.Count > 0)
            avgBlockSearchDuration = (float)Math.Round(wwwTL.searchDurations_InBlock.Average(), 2);

        BlockSummaryString.AppendLine( "<b>\nMax Trials in Block: </b>" + wwwTL.CurrentTrialDef.MaxTrials + 
                                      "\n\nAverage Search Duration: " + avgBlockSearchDuration +
                                      "\n" +
                                      "\nDistractor Slot Error Count: " + wwwTL.distractorSlotErrorCount_InBlock+
                                      "\nNon-Distractor Slot Error Count: " + wwwTL.slotErrorCount_InBlock + 
                                      "\nRepetition Error Count: "  + wwwTL.repetitionErrorCount_InBlock +
                                      //   "\nNon-Stim Touch Error Count: " + wwwTL.numNonStimSelections_InBlock+
                                      "\nNo Selection Error Count: " + wwwTL.AbortedTrials_InBlock);
         
        BlockSummaryString.AppendLine(CurrentBlockString).ToString();
        if (PreviousBlocksString.Length > 0)
            BlockSummaryString.AppendLine(PreviousBlocksString.ToString());

    }
    public override void SetTaskSummaryString()
    {
        float avgTaskSearchDuration = 0;
        if (SearchDurations_InTask.Count > 0)
            avgTaskSearchDuration = (float)Math.Round(SearchDurations_InTask.Average(), 2);
        if (wwwTL.TrialCount_InTask != 0)
        {
            CurrentTaskSummaryString.Clear();

            decimal percentAbortedTrials = (Math.Round(decimal.Divide(AbortedTrials_InTask, (wwwTL.TrialCount_InTask)), 2)) * 100;

            CurrentTaskSummaryString.Append($"\n<b>{ConfigFolderName}</b>" +
                                            $"\n<b># Trials:</b> {wwwTL.TrialCount_InTask} ({percentAbortedTrials}% aborted)" +
                                            $"\t<b># Blocks:</b> {BlockCount}" +
                                            $"\t<b># Reward Pulses:</b> {NumRewardPulses_InTask}" +
                                            $"\n# Slider Bar Completions: {NumSliderBarFilled_InTask}" + 
                                            $"\nAvg Search Duration: {avgTaskSearchDuration}");
        }
        else
        {
            CurrentTaskSummaryString.Append($"\n<b>{ConfigFolderName}</b>");
        }
            
    }

    private void DefineBlockData()
    {
        BlockData.AddDatum("LearningSpeed", () => LearningSpeed);
        BlockData.AddDatum("BlockAccuracyLog", ()=> wwwTL.accuracyLog_InBlock);
        BlockData.AddDatum("AvgSearchDuration", ()=> wwwTL.searchDurations_InBlock.Average());
        BlockData.AddDatum("NumDistractorSlotError", ()=> wwwTL.distractorSlotErrorCount_InBlock);
        BlockData.AddDatum("NumSearchSlotError", ()=> wwwTL.slotErrorCount_InBlock);
        BlockData.AddDatum("NumRepetitionError", ()=> wwwTL.repetitionErrorCount_InBlock);
        //BlockData.AddDatum("Num Non Stim Selections", ()=> wwwTL.numNonStimSelections_InBlock); USE MOUSE TRACKER AND VALIDATE
        BlockData.AddDatum("NumAbortedTrials", ()=> wwwTL.AbortedTrials_InBlock);
        BlockData.AddDatum("NumRewardGiven", ()=> wwwTL.numRewardGiven_InBlock);
    }

    public void ClearStrings()
    {
        BlockAveragesString = "";
        CurrentBlockString = "";
        BlockSummaryString.Clear();
    }

}