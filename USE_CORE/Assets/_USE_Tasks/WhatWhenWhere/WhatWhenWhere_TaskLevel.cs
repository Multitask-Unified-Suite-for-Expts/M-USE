﻿/*
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
    public int NumSliderBarFilled_InTask;
    public List<float?> SearchDurations_InTask = new List<float?>();

    
    //Block Data Logging Variables
    public List<float?> SearchDurations_InBlock = new List<float?>();
    public int RepetitionErrorCount_InBlock;
    public int SlotErrorCount_InBlock;
    public int DistractorSlotErrorCount_InBlock;
    public int NumCorrectSelections_InBlock;
    public int NumErrors_InBlock;

    public int retouchErroneousCounter_InTask;
    public int retouchCorrectCounter_InTask;
    public int perseverationCounter_InTask;
    
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
            MinTrials_InBlock = wwwBD.MinTrials;
            MaxTrials_InBlock = wwwBD.MaxTrials;
            
            wwwTL.ContextName = wwwBD.ContextName;
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
        OrderedDictionary data = base.GetTaskSummaryData();

        data["Slider Bar Full"] = NumSliderBarFilled_InTask;
        data["Avg Search Duration"] = CalculateAverageDuration(SearchDurations_InTask);

        data["Retouch Correct"] = retouchCorrectCounter_InTask;
        data["Retouch Erroneous"] = retouchErroneousCounter_InTask;
        data["Perseverations"] = perseverationCounter_InTask;
        
        
        return data;
    }
    public void SetBlockSummaryString()
    {
        ClearStrings();
        CurrentBlockSummaryString.AppendLine( "<b>\nMin Trials in Block: </b>" + MinTrials_InBlock + 
                                            "<b>\nMax Trials in Block: </b>" + MaxTrials_InBlock + 
                                      "\n\nAverage Search Duration: " + CalculateAverageDuration(SearchDurations_InBlock) +
                                      "\n" +
                                      "\nDistractor Slot Error Count: " + DistractorSlotErrorCount_InBlock+
                                      "\nNon-Distractor Slot Error Count: " + SlotErrorCount_InBlock + 
                                      "\nRepetition Error Count: "  + RepetitionErrorCount_InBlock +
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
                                            $"\nRetouch Correct: {retouchCorrectCounter_InTask}" +
                                            $"\nRetouch Erroneous: {retouchErroneousCounter_InTask}");
                                            ;
    }

    private void DefineBlockData()
    {
        BlockData.AddDatum("MinTrials", () => MinTrials_InBlock);
        BlockData.AddDatum("MaxTrials", () => MaxTrials_InBlock);
        BlockData.AddDatum("Search Durations", ()=> String.Join(",", SearchDurations_InBlock));
        BlockData.AddDatum("NumErrors_InBlock", ()=> NumErrors_InBlock);
        BlockData.AddDatum("NumDistractorSlotError", ()=> DistractorSlotErrorCount_InBlock);
        BlockData.AddDatum("NumSearchSlotError", ()=> SlotErrorCount_InBlock);
        BlockData.AddDatum("NumRepetitionError", ()=> RepetitionErrorCount_InBlock);
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
        wwwTL.consecutiveError = 0;

        SearchDurations_InBlock.Clear();
        wwwTL.runningAcc.Clear();
        wwwTL.runningPercentError.Clear();
        wwwTL.runningErrorCount.Clear();

        retouchErroneousCounter_InTask = 0;
        retouchCorrectCounter_InTask = 0;
        perseverationCounter_InTask = 0;

    }
}