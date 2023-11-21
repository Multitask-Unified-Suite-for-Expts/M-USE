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

public class AntiSaccade_TaskLevel : ControlLevel_Task_Template
{
    AntiSaccade_BlockDef CurrentBlock => GetCurrentBlockDef<AntiSaccade_BlockDef>();
    AntiSaccade_TrialLevel trialLevel;

    [HideInInspector] public string CurrentBlockString;
    [HideInInspector] public StringBuilder PreviousBlocksString;
    [HideInInspector] public int BlockStringsAdded = 0;

    //Task Values used for SummaryData file
    [HideInInspector] public int TrialsCompleted_Task = 0;
    [HideInInspector] public int TrialsCorrect_Task = 0;
    [HideInInspector] public int TokenBarsCompleted_Task = 0;


    public override void DefineControlLevel()
    {
        trialLevel = (AntiSaccade_TrialLevel)TrialLevel;

        CurrentBlockString = "";
        PreviousBlocksString = new StringBuilder();

        DefineBlockData();

        RunBlock.AddSpecificInitializationMethod(() =>
        {
            trialLevel.ResetBlockVariables();
            CurrentBlock.ContextName = CurrentBlock.ContextName.Trim();
            SetSkyBox(CurrentBlock.ContextName);
        });

        BlockFeedback.AddSpecificInitializationMethod(() => HandleBlockStrings());

    }

    private void HandleBlockStrings()
    {
        if (!Session.WebBuild)
        {
            if (BlockStringsAdded > 0)
                CurrentBlockString += "\n";
            PreviousBlocksString.Insert(0, CurrentBlockString);
            BlockStringsAdded++;
        }
    }

    public override OrderedDictionary GetBlockResultsData()
    {
        OrderedDictionary data = new OrderedDictionary
        {
            ["Trials Correct"] = trialLevel.TrialsCorrect_Block,
            ["Trials Completed"] = trialLevel.TrialCompletions_Block,
            ["TokenBar Completions"] = trialLevel.TokenBarCompletions_Block,
        };
        return data;
    }

    private void DefineBlockData()
    {
        BlockData.AddDatum("BlockName", () => CurrentBlock.BlockName);
        BlockData.AddDatum("TrialsCompleted", () => trialLevel.TrialCompletions_Block);
        BlockData.AddDatum("TrialsCorrect", () => trialLevel.TrialsCorrect_Block);
        BlockData.AddDatum("TokenBarCompletions", () => trialLevel.TokenBarCompletions_Block);
        BlockData.AddDatum("ContextName", () => CurrentBlock.ContextName);
        BlockData.AddDatum("CalculatedThreshold", () => trialLevel.calculatedThreshold);
        BlockData.AddDatum("DiffLevelsSummary", () => trialLevel.DiffLevelsSummary);
    }

    public void CalculateBlockSummaryString()
    {
        ClearStrings();

        CurrentBlockString = "\nTrials Completed: " + trialLevel.TrialCompletions_Block +
                        "\nTrials Correct: " + trialLevel.TrialsCorrect_Block +
                        "\nTokenBar Completions: " + trialLevel.TokenBarCompletions_Block +
                        "\nReward Pulses: " + NumRewardPulses_InBlock;

        CurrentBlockSummaryString.AppendLine(CurrentBlockString).ToString();
    }

    public void ClearStrings()
    {
        CurrentBlockString = "";
        CurrentBlockSummaryString.Clear();
    }


}