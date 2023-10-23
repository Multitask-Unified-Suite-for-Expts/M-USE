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
using System.Linq;
using UnityEngine;
using USE_Settings;
using USE_ExperimentTemplate_Task;
using FeatureUncertaintyWM_Namespace;

public class FeatureUncertaintyWM_TaskLevel : ControlLevel_Task_Template
{
    FeatureUncertaintyWM_BlockDef fuWMBD => GetCurrentBlockDef<FeatureUncertaintyWM_BlockDef>();
    FeatureUncertaintyWM_TrialLevel fuWMTL;
    public int NumCorrect_InTask = 0;
    public List<float> SearchDurations_InTask = new List<float>();
    public int NumErrors_InTask = 0;
    //public int NumRewardPulses_InTask = 0;
    public int NumTokenBarFull_InTask = 0;
    public int TotalTokensCollected_InTask = 0;
    public float Accuracy_InTask = 0;
    public float AverageSearchDuration_InTask = 0;
    public int NumAborted_InTask = 0;
    public override void DefineControlLevel()
    {
        fuWMTL = (FeatureUncertaintyWM_TrialLevel)TrialLevel;
        StimCanvas_2D = GameObject.Find("FeatureUncertaintyWM_Canvas");

        AssignBlockData();

        RunBlock.AddSpecificInitializationMethod(() =>
        {
            fuWMTL.ContextName = fuWMBD.ContextName;
            StartCoroutine(HandleSkybox(fuWMTL.GetContextNestedFilePath(SessionValues.SessionDef.ContextExternalFilePath, fuWMTL.ContextName)));
            fuWMTL.ResetBlockVariables();
            fuWMTL.TokenFBController.SetTotalTokensNum(fuWMBD.NumTokenBar);
            fuWMTL.TokenFBController.SetTokenBarValue(fuWMBD.NumInitialTokens);
            SetBlockSummaryString();
        });
    }

    public void SetBlockSummaryString()
    {
        CurrentBlockSummaryString.Clear();
        float avgBlockSearchDuration = 0;
        if (fuWMTL.SearchDurations_InBlock.Count != 0)
            avgBlockSearchDuration = fuWMTL.SearchDurations_InBlock.Average();
        CurrentBlockSummaryString.AppendLine("Accuracy: " + String.Format("{0:0.000}", fuWMTL.Accuracy_InBlock) +
                                      "\n" +
                                      "\nAvg Search Duration: " + String.Format("{0:0.000}", avgBlockSearchDuration) +
                                      "\n" +
                                      "\nNum Reward Given: " + fuWMTL.NumRewardPulses_InBlock +
                                      "\nNum Token Bar Filled: " + fuWMTL.NumTokenBarFull_InBlock +
                                      "\nTotal Tokens Collected: " + fuWMTL.TotalTokensCollected_InBlock);
    }
    public override void SetTaskSummaryString()
    {
        float avgTaskSearchDuration = 0;
        if (SearchDurations_InTask.Count > 0)
            avgTaskSearchDuration = (float)Math.Round(SearchDurations_InTask.Average(), 2);
        if (fuWMTL.TrialCount_InTask != 0)
        {
            CurrentTaskSummaryString.Clear();
            CurrentTaskSummaryString.Append($"\n<b>{ConfigFolderName}</b>" +
                                            $"\n<b># Trials:</b> {fuWMTL.TrialCount_InTask} ({(Math.Round(decimal.Divide(NumAborted_InTask, (fuWMTL.TrialCount_InTask)), 2)) * 100}% aborted)" +
                                            $"\t<b># Blocks:</b> {BlockCount}" +
                                            $"\t<b># Reward Pulses:</b> {NumRewardPulses_InTask}" +
                                            $"\nAccuracy: {(Math.Round(decimal.Divide(NumCorrect_InTask, (fuWMTL.TrialCount_InTask)), 2)) * 100}%" +
                                            $"\tAvg Search Duration: {avgTaskSearchDuration}" +
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
        BlockData.AddDatum("Block Accuracy", () => fuWMTL.Accuracy_InBlock);
        BlockData.AddDatum("Avg Search Duration", () => fuWMTL.AverageSearchDuration_InBlock);
        BlockData.AddDatum("Num Reward Given", () => fuWMTL.NumRewardPulses_InBlock);
        BlockData.AddDatum("Num Token Bar Filled", () => fuWMTL.NumTokenBarFull_InBlock);
        BlockData.AddDatum("Total Tokens Collected", () => fuWMTL.TotalTokensCollected_InBlock);
    }


}