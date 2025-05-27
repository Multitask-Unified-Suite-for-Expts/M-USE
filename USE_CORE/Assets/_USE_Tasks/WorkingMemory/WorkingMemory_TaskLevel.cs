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
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using UnityEngine;
using USE_ExperimentTemplate_Task;
using USE_Settings;
using WorkingMemory_Namespace;


public class WorkingMemory_TaskLevel : ControlLevel_Task_Template
{
    WorkingMemory_BlockDef wmBD => GetCurrentBlockDef<WorkingMemory_BlockDef>();
    WorkingMemory_TrialLevel wmTL;
    public int NumCorrect_InTask = 0;
    public List<float?> SearchDurations_InTask = new List<float?>();
    public int NumErrors_InTask = 0;
    public int NumTokenBarFull_InTask = 0;
    public int TotalTokensCollected_InTask = 0;
    public float Accuracy_InTask = 0;
    public override void DefineControlLevel()
    {
        wmTL = (WorkingMemory_TrialLevel)TrialLevel;

        AssignBlockData();

        RunBlock.AddSpecificInitializationMethod(() =>
        {
            wmTL.ContextName = wmBD.ContextName;

            SetSkyBox(wmBD.ContextName);

            wmTL.ResetBlockVariables();
            wmTL.TokenFBController.SetTotalTokensNum(wmBD.TokenBarCapacity);
            wmTL.TokenFBController.SetTokenBarValue(wmBD.NumInitialTokens);
            SetBlockSummaryString();
        });
    }
    public override OrderedDictionary GetTaskSummaryData()
    {
        OrderedDictionary data = base.GetTaskSummaryData();

        data["Token Bar Full"] = NumTokenBarFull_InTask;
        
        if (SearchDurations_InTask.Count > 0)
            data["Average Search Duration"] = CalculateAverageDuration(SearchDurations_InTask);
        
        return data;
    }


    public override OrderedDictionary GetTaskResultsData()
    {
        OrderedDictionary data = base.GetTaskResultsData();

        data["TokenBar Completions"] = NumTokenBarFull_InTask;

        if (SearchDurations_InTask.Count > 0)
            data["Average Search Duration"] = String.Format("{0:0.0} " + "Seconds", CalculateAverageDuration(SearchDurations_InTask));

        return data;
    }


    public void SetBlockSummaryString()
    {
        CurrentBlockSummaryString.Clear();
        float avgBlockSearchDuration = 0;
        if (wmTL.SearchDurations_InBlock.Count != 0)
            avgBlockSearchDuration = CalculateAverageDuration(wmTL.SearchDurations_InBlock);
        CurrentBlockSummaryString.AppendLine("Accuracy: " + String.Format("{0:0.000}", wmTL.Accuracy_InBlock) +  
                                      "\nAvg Search Duration: " + String.Format("{0:0.000}", avgBlockSearchDuration) +
                                      "\nNum Reward Given: " + NumRewardPulses_InBlock + 
                                      "\nNum Token Bar Filled: " + wmTL.NumTokenBarFull_InBlock +
                                      "\nTotal Tokens Collected: " + wmTL.TotalTokensCollected_InBlock);
    }
    public override void SetTaskSummaryString()
    {
        CurrentTaskSummaryString.Clear();
        base.SetTaskSummaryString();

        double avgSearchDuration = 0;
        if (SearchDurations_InTask.Count > 0)
            avgSearchDuration = Math.Round(CalculateAverageDuration(SearchDurations_InTask), 2);

        if (wmTL.TrialCount_InTask != 0)
        {

            CurrentTaskSummaryString.Append( $"\nAccuracy: {(Math.Round(decimal.Divide(NumCorrect_InTask,(wmTL.TrialCount_InTask)),2))*100}%" + 
                                            $"\nAvg Search Duration: {avgSearchDuration}" +
                                            $"\n# Token Bar Filled: {NumTokenBarFull_InTask}" +
                                            $"\n# Tokens Collected: {TotalTokensCollected_InTask}");
        }
    }
    public void AssignBlockData()
    {
        BlockData.AddDatum("Block Accuracy", ()=> wmTL.Accuracy_InBlock);
        BlockData.AddDatum("Search Durations", ()=> String.Join(",", wmTL.SearchDurations_InBlock));
        BlockData.AddDatum("Num Token Bar Filled", ()=> wmTL.NumTokenBarFull_InBlock);
        BlockData.AddDatum("Total Tokens Collected", ()=> wmTL.TotalTokensCollected_InBlock);
    }
}