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


using System.Text;
using THR_Namespace;
using USE_ExperimentTemplate_Task;
using System.Collections.Specialized;
using System.Collections;
using UnityEngine;
using System;

public class THR_TaskLevel : ControlLevel_Task_Template
{
    THR_BlockDef CurrentBlock => GetCurrentBlockDef<THR_BlockDef>();
    THR_TrialLevel trialLevel;

    [HideInInspector] public int TrialsCompleted_Task = 0;
    [HideInInspector] public int TrialsCorrect_Task = 0;
    [HideInInspector] public int SelectObjectTouches_Task = 0;
    [HideInInspector] public int AvoidObjectTouches_Task = 0;
    [HideInInspector] public int BackdropTouches_Task = 0;
    [HideInInspector] public int ItiTouches_Task = 0;
    [HideInInspector] public int TouchRewards_Task = 0;
    [HideInInspector] public int ReleaseRewards_Task = 0;
    [HideInInspector] public int ReleasedEarly_Task = 0;
    [HideInInspector] public int ReleasedLate_Task = 0;
    [HideInInspector] public int TouchesMovedOutside_Task = 0;



    public override void DefineControlLevel()
    {
        trialLevel = (THR_TrialLevel)TrialLevel;
        DefineBlockData();

        RunBlock.AddSpecificInitializationMethod(() =>
        {
            MinTrials_InBlock = CurrentBlock.MinTrials;
            MaxTrials_InBlock = CurrentBlock.MaxTrials;
            trialLevel.ResetBlockVariables();
            SetBlockSummaryString();
        });
    }

    public override OrderedDictionary GetTaskSummaryData()
    {
        OrderedDictionary data = base.GetTaskSummaryData();
        data["Trials Completed"] = TrialsCompleted_Task;
        data["Trials Correct"] = TrialsCorrect_Task;
        data["Touch Rewards"] = TouchRewards_Task;
        data["Release Rewards"] = ReleaseRewards_Task;
        data["Select Object Touches"] = SelectObjectTouches_Task;
        data["Avoid Object Touches"] = AvoidObjectTouches_Task;
        data["Backdrop Touches"] = BackdropTouches_Task;
        data["ITI Touches"] = ItiTouches_Task;
        data["Released Early"] = ReleasedEarly_Task;
        data["Released Late"] = ReleasedLate_Task;
        data["Touches Moved Outside"] = TouchesMovedOutside_Task;
       
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

    public override void SetBlockSummaryString()
    {
        CurrentBlockSummaryString.Clear();

        CurrentBlockSummaryString.AppendLine("\nMin Trials in Block: " + MinTrials_InBlock +
                             "\nMax Trials in Block: " + MaxTrials_InBlock +
                                "\nBlock Name: " + CurrentBlock.BlockName  +
                        "\nTrials Correct: " + trialLevel.TrialsCorrect_Block + 
                        "\nReleased Early: " + trialLevel.NumReleasedEarly_Block +
                        "\nReleased Late: " + trialLevel.NumReleasedLate_Block +
                        "\nMoved Outside Object: " + trialLevel.NumTouchesMovedOutside_Block +
                        "\n\nAvoid Object Touches: " + trialLevel.AvoidObjectTouches_Block +
                        "\nSelect Object Touches: " + trialLevel.SelectObjectTouches_Block +
                        "\nBackdrop Touches: " + trialLevel.BackdropTouches_Block +
                        "\nNum Pulses: " + (trialLevel.NumTouchRewards_Block + trialLevel.NumReleaseRewards_Block)
                        );
    }
    public override void SetTaskSummaryString()
    {
        base.SetTaskSummaryString();

        if (trialLevel.TrialCount_InTask != 0)
        {
            CurrentTaskSummaryString.Append($"\nAccuracy: {(Math.Round(decimal.Divide(TrialsCorrect_Task, (trialLevel.TrialCount_InTask)), 2)) * 100}%" +
                                                    $"\n# Released Early: {ReleasedEarly_Task}" +
                                                    $"\n# Released Late: {ReleasedLate_Task}" +
                                                    $"\n# Backdrop Touches: {BackdropTouches_Task}");
        }

    }
    private void DefineBlockData()
    {
        BlockData.AddDatum("NumTrialsCompleted", () => trialLevel.TrialsCompleted_Block);
        BlockData.AddDatum("NumTrialsCorrect", () => trialLevel.TrialsCorrect_Block);
        BlockData.AddDatum("AvoidObjectTouches_Block", () => trialLevel.AvoidObjectTouches_Block);
        BlockData.AddDatum("SelectObjectTouches_Block", () => trialLevel.SelectObjectTouches_Block);
        BlockData.AddDatum("BackdropTouches_Block", () => trialLevel.BackdropTouches_Block);
        BlockData.AddDatum("ItiTouches_Block", () => trialLevel.NumItiTouches_Block);
        BlockData.AddDatum("NumTouchRewards", () => trialLevel.NumTouchRewards_Block);
        BlockData.AddDatum("NumReleaseRewards", () => trialLevel.NumReleaseRewards_Block);
        BlockData.AddDatum("DifficultyLevel", () => CurrentBlock.BlockName);
        BlockData.AddDatum("NumReleasedEarly", () => trialLevel.NumReleasedEarly_Block);
        BlockData.AddDatum("NumReleasedLate", () => trialLevel.NumReleasedLate_Block);
        BlockData.AddDatum("NumTouchesMovedOutside", () => trialLevel.NumTouchesMovedOutside_Block);
    }


}