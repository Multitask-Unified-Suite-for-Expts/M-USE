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



using USE_ExperimentTemplate_Task;
using AntiSaccade_Namespace;
using UnityEngine;
using System.Text;
using System.Collections.Specialized;
using System;
using System.Collections.Generic;
using System.Linq;

public class AntiSaccade_TaskLevel : ControlLevel_Task_Template
{
    AntiSaccade_BlockDef CurrentBlock => GetCurrentBlockDef<AntiSaccade_BlockDef>();
    AntiSaccade_TrialLevel trialLevel;

    [HideInInspector] public string CurrentBlockString;
    [HideInInspector] public int BlockStringsAdded = 0;

    //Task Values used for SummaryData file
    [HideInInspector] public int TrialsCompleted_Task = 0;
    [HideInInspector] public int TrialsCorrect_Task = 0;
    [HideInInspector] public int TokenBarsCompleted_Task = 0;
    [HideInInspector] public List<float> PS_AvgCalcThresh_Task; 
    [HideInInspector] public List<float> AS_AvgCalcThresh_Task;
    
    

    public override void DefineControlLevel()
    {
        trialLevel = (AntiSaccade_TrialLevel)TrialLevel;
        CurrentBlockString = "";

        AS_AvgCalcThresh_Task = new List<float>();
        PS_AvgCalcThresh_Task = new List<float>();
        
        DefineBlockData();
        

        RunBlock.AddSpecificInitializationMethod(() =>
        {
            trialLevel.ResetBlockVariables();
            CurrentBlock.ContextName = CurrentBlock.ContextName.Trim();
            SetSkyBox(CurrentBlock.ContextName);
        });
        
        BlockFeedback.AddSpecificInitializationMethod(() =>
        {
            AddToThresholdCategory();
            HandleBlockStrings();
        });

    }

    private void AddToThresholdCategory()
    {
        string[] splitArray = CurrentBlock.BlockName.Split('.');

        if (splitArray.Length < 2)
            return;

        string split = splitArray[1].ToLower();

        if (split.Contains("as"))
        {
            AS_AvgCalcThresh_Task.Add(trialLevel.calculatedThreshold_timing);
            Debug.Log("AS_AvgCalcThresh_Task: " + string.Join(", ", AS_AvgCalcThresh_Task));
        }
        else if (split.Contains("ps"))
            PS_AvgCalcThresh_Task.Add(trialLevel.calculatedThreshold_timing);
        else
            Debug.LogWarning("BlockName includes neither AS nor PS!");
    }
    
    private void HandleBlockStrings()
    {
        if (!Session.WebBuild)
        {
            if (BlockStringsAdded > 0)
                CurrentBlockString += "\n";
            BlockStringsAdded++;
        }
    }

    
    public override OrderedDictionary GetTaskSummaryData()
    {
        OrderedDictionary data = base.GetTaskSummaryData();
        data["Trials Completed"] = TrialsCompleted_Task;
        data["Trials Correct"] = TrialsCorrect_Task;
        data["% Trials Correct"] = ((float)TrialsCorrect_Task / TrialsCompleted_Task * 100).ToString("F1") + "%";
        if (AS_AvgCalcThresh_Task != null && AS_AvgCalcThresh_Task.Any())
            data["AvgCalcThresh_AS"] = 2.0 * AS_AvgCalcThresh_Task.Average(); // "* 2.0" because we're lumping displaytargetduration and spatialcuedelay together for our metric
        if (PS_AvgCalcThresh_Task != null && PS_AvgCalcThresh_Task.Any())
            data["AvgCalcThresh_PS"] = 2.0 * PS_AvgCalcThresh_Task.Average(); // "* 2.0" because we're lumping displaytargetduration and spatialcuedelay together for our metric
        return data;
    }

    public override OrderedDictionary GetTaskResultsData()
    {
        OrderedDictionary data = base.GetTaskResultsData();
        //data["Longest Streak"] = LongestStreak;
        //data["Average Streak"] = GetAvgStreak();
        //data["Trials Correct"] = TrialsCorrect_Task;
        //data["TokenBar Completions"] = TokenBarCompletions_Task;

        return data;
    }

    private void DefineBlockData()
    {
        BlockData.AddDatum("BlockName", () => CurrentBlock.BlockName);
        BlockData.AddDatum("TrialsCompleted", () => trialLevel.TrialCompletions_Block);
        BlockData.AddDatum("TrialsCorrect", () => trialLevel.TrialsCorrect_Block);
        BlockData.AddDatum("TokenBarCompletions", () => trialLevel.TokenBarCompletions_Block);
        BlockData.AddDatum("ContextName", () => CurrentBlock.ContextName);
        BlockData.AddDatum("CalculatedThreshold", () => trialLevel.calculatedThreshold_timing);
        BlockData.AddDatum("DiffLevelsSummary", () => trialLevel.DiffLevelsSummary);
        BlockData.AddDatum("BlockAccuracy", () => (float)trialLevel.TrialsCorrect_Block / trialLevel.TrialCompletions_Block);
        BlockData.AddDatum("AvgReactionTime", () => CalculateAverageDuration(trialLevel.ReactionTimes_InBlock));
        BlockData.AddDatum("StdDevReactionTime", () => CalculateStdDevDuration(trialLevel.ReactionTimes_InBlock));

    }

    public void CalculateBlockSummaryString()
    {
        ClearStrings();

        CurrentBlockString = "\nTrials Completed: " + trialLevel.TrialCompletions_Block +
                             "\nTrials Correct: " + trialLevel.TrialsCorrect_Block +
                             "\nTokenBar Completions: " + trialLevel.TokenBarCompletions_Block +
                             "\nReward Pulses: " + NumRewardPulses_InBlock +
                             "\nReversal Count: " + trialLevel.reversalsCount +
                             "\nReversals Necessary for Termination: " + trialLevel.NumReversalsUntilTerm;


        if (AS_AvgCalcThresh_Task != null && AS_AvgCalcThresh_Task.Any()) 
            CurrentBlockString = CurrentBlockString + "\nAverage AS Convergence Value: " + AS_AvgCalcThresh_Task.Average();
        if (PS_AvgCalcThresh_Task != null && PS_AvgCalcThresh_Task.Any()) 
            CurrentBlockString = CurrentBlockString + "\nAverage PS Convergence Value: " + PS_AvgCalcThresh_Task.Average();

        CurrentBlockSummaryString.AppendLine(CurrentBlockString).ToString();
    }

    public void ClearStrings()
    {
        CurrentBlockString = "";
        CurrentBlockSummaryString.Clear();
    }


}