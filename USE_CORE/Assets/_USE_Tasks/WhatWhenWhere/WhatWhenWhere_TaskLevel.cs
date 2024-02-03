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


    [HideInInspector] public int RuleAbidingErrors_InBlock;
    [HideInInspector] public int DistractorRuleAbidingErrors_InBlock;
    [HideInInspector] public int BackTrackErrors_InBlock;
    [HideInInspector] public int RetouchErrors_InBlock;
    [HideInInspector] public int PerseverativeRuleAbidingErrors_InBlock;
    [HideInInspector] public int PerseverativeDistractorRuleAbidingErrors_InBlock;
    [HideInInspector] public int PerseverativeBackTrackErrors_InBlock;
    [HideInInspector] public int PerseverativeRetouchErrors_InBlock;
    [HideInInspector] public int CorrectSelections_InBlock;
    [HideInInspector] public int RetouchCorrect_InBlock;
    [HideInInspector] public int TotalErrors_InBlock;
    [HideInInspector] public int CompletedSequences_InBlock;
    [HideInInspector] public List<float?> SearchDurations_InBlock = new List<float?>();

    [HideInInspector] public int RuleAbidingErrors_InTask;
    [HideInInspector] public int DistractorRuleAbidingErrors_InTask;
    [HideInInspector] public int BackTrackErrors_InTask;
    [HideInInspector] public int RetouchErrors_InTask;
    [HideInInspector] public int PerseverativeRuleAbidingErrors_InTask;
    [HideInInspector] public int PerseverativeDistractorRuleAbidingErrors_InTask;
    [HideInInspector] public int PerseverativeBackTrackErrors_InTask;
    [HideInInspector] public int PerseverativeRetouchErrors_InTask;
    [HideInInspector] public int CorrectSelections_InTask;
    [HideInInspector] public int RetouchCorrect_InTask;
    [HideInInspector] public int TotalErrors_InTask;
    [HideInInspector] public int CompletedSequences_InTask;
    [HideInInspector] public List<float?> SearchDurations_InTask = new List<float?>();
    [HideInInspector] public int NumSliderBarFilled_InTask; 


    private Dictionary<int, WhatWhenWhere_BlockDataSummary> WWW_BlockSummaryData;

    // Block Summary String Variables
    [HideInInspector] public string BlockAveragesString;
    public override void DefineControlLevel()
    {
        wwwTL = (WhatWhenWhere_TrialLevel)TrialLevel;

        DefineBlockData();

        BlockAveragesString = "";

        RunBlock.AddSpecificInitializationMethod(() =>
        {
            MinTrials_InBlock = wwwBD.MinTrials;
            MaxTrials_InBlock = wwwBD.MaxTrials;

            wwwTL.ContextName = wwwBD.ContextName;
            SetSkyBox(wwwBD.ContextName);

            ResetBlockDataVariables();
            SetBlockSummaryString();

        });
    }

    public override OrderedDictionary GetBlockResultsData()
    {
        OrderedDictionary data = new OrderedDictionary
        {
            ["Trials Completed"] = wwwTL.TrialCount_InBlock + 1,
            ["Correct Selections"] = CorrectSelections_InBlock,
            ["Errors"] = TotalErrors_InBlock
        };
        return data;
    }

    public override OrderedDictionary GetTaskSummaryData()
    {
        OrderedDictionary data = base.GetTaskSummaryData();

        data["Completed Sequences"] = CompletedSequences_InTask;
        data["Avg Search Duration"] = CalculateAverageDuration(SearchDurations_InTask);

        data["Correct Selections"] = CorrectSelections_InTask;
        data["Total Errors"] = CorrectSelections_InTask;
        data["Rule Abiding Errors"] = RuleAbidingErrors_InTask;
        data["Distractor Rule Abiding Errors"] = DistractorRuleAbidingErrors_InTask;
        data["Back Track Errors"] = BackTrackErrors_InTask;
        data["Retouch Correct"] = RetouchCorrect_InTask;
        data["Retouch Errors"] = RetouchErrors_InTask;

        return data;
    }
    public void SetBlockSummaryString()
    {
        ClearStrings();
        CurrentBlockSummaryString.AppendLine("<b>\nMin Trials in Block: </b>" + MinTrials_InBlock +
                                            "<b>\nMax Trials in Block: </b>" + MaxTrials_InBlock +
                                      "\n\nAverage Search Duration: " + CalculateAverageDuration(SearchDurations_InBlock) +
                                      "\n" +
                                      "\nCorrect Selections: " + CorrectSelections_InBlock +
                                      "\nTotal Errors: " + TotalErrors_InBlock +
                                      "\n" +
                                      "\nRule Abiding Errors: " + RuleAbidingErrors_InBlock +
                                      "\nDistractor Rule Abiding Errors: " + DistractorRuleAbidingErrors_InBlock +
                                      "\nRetouch Errors: " + RetouchErrors_InBlock +
                                      "\nRetouch Correct: " + RetouchCorrect_InBlock +
                                      "\nBack Track Errors: " + BackTrackErrors_InBlock +
                                      "\nNum Aborted Trials in Block: " + NumAbortedTrials_InBlock);
    }

    public override void SetTaskSummaryString()
    {
        CurrentTaskSummaryString.Clear();
        base.SetTaskSummaryString();

        double avgSearchDuration = 0;
        if (SearchDurations_InTask.Count > 0)
            avgSearchDuration = Math.Round(CalculateAverageDuration(SearchDurations_InTask), 2);


        CurrentTaskSummaryString.Append($"\n# Slider Bar Completions: {NumSliderBarFilled_InTask}" +
                                            $"\nAvg Search Duration: {avgSearchDuration}" +
                                            $"\nCorrect Selections: {CorrectSelections_InTask}" +
                                            $"\nTotal Errors: {TotalErrors_InTask}");
        ;
    }

    private void DefineBlockData()
    {
        BlockData.AddDatum("MinTrials", () => MinTrials_InBlock);
        BlockData.AddDatum("MaxTrials", () => MaxTrials_InBlock);
       // BlockData.AddDatum("Search Durations", () => String.Join(",", SearchDurations_InBlock));
        BlockData.AddDatum("RuleAbidingErrors_InBlock", () => RuleAbidingErrors_InBlock);
        BlockData.AddDatum("DistractorRuleAbidingErrors_InBlock", () => DistractorRuleAbidingErrors_InBlock);
        BlockData.AddDatum("BackTrackErrors_InBlock", () => BackTrackErrors_InBlock);
        BlockData.AddDatum("RetouchErrors_InBlock", () => RetouchErrors_InBlock);
        BlockData.AddDatum("RetouchCorrect_InBlock", () => RetouchCorrect_InBlock);
        BlockData.AddDatum("CorrectSelections_InBlock", () => CorrectSelections_InBlock);
        BlockData.AddDatum("TotalErrors_InBlock", () => TotalErrors_InBlock);
        BlockData.AddDatum("CompletedSequences_InBlock", () => CompletedSequences_InBlock);

    }

    public void ClearStrings()
    {
        CurrentBlockSummaryString.Clear();
    }
    public void ResetBlockDataVariables()
    {
        RuleAbidingErrors_InBlock = 0;
        DistractorRuleAbidingErrors_InBlock = 0;
        BackTrackErrors_InBlock = 0;
        RetouchErrors_InBlock = 0;
        PerseverativeRuleAbidingErrors_InBlock = 0;
        PerseverativeDistractorRuleAbidingErrors_InBlock = 0;
        PerseverativeBackTrackErrors_InBlock = 0;
        PerseverativeRetouchErrors_InBlock = 0;
        CorrectSelections_InBlock = 0;
        RetouchCorrect_InBlock = 0;
        TotalErrors_InBlock = 0;
        CompletedSequences_InBlock = 0;
        SearchDurations_InBlock.Clear();

        wwwTL.SequenceManager?.SetConsecutiveErrorCount(0);

        SearchDurations_InBlock.Clear();
        wwwTL.runningAcc.Clear();
        wwwTL.runningPercentError.Clear();
        wwwTL.runningErrorCount.Clear();

    }

    
}