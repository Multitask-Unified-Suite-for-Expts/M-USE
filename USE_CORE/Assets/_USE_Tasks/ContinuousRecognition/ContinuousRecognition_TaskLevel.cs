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
using System.Text;
using System.Collections.Generic;
using System.Collections.Specialized;
using ContinuousRecognition_Namespace;
using UnityEngine;
using USE_ExperimentTemplate_Task;
using System.Linq;


public class ContinuousRecognition_TaskLevel : ControlLevel_Task_Template
{
    ContinuousRecognition_BlockDef CurrentBlock => GetCurrentBlockDef<ContinuousRecognition_BlockDef>();
    ContinuousRecognition_TrialLevel trialLevel;

    [HideInInspector] public int TrialsCompleted_Task;
    [HideInInspector] public int TrialsCorrect_Task;
    [HideInInspector] public int TokenBarCompletions_Task;
    [HideInInspector] public float NonStimTouches_Task;
    [HideInInspector] public int SliderBarCompletions_Task = 0;


    [HideInInspector] public int NumNew_Picked_Task;
    [HideInInspector] public int NumPNC_Picked_Task;

    [HideInInspector] public List<int> RecencyInterference_Task;

    [HideInInspector] public Dictionary<int, PerceptualInterferance_BlockData> PerceptualInterference_Task; //access its index for that Score's data

    [HideInInspector] public string CurrentBlockString;

    public int blocksAdded;

    //Data for Task Summary at end of session:
    [HideInInspector] public int LongestStreak = 0;
    [HideInInspector] public float AverageStreak = 0f;



    public override void DefineControlLevel()
    {
        trialLevel = (ContinuousRecognition_TrialLevel) TrialLevel;

        PerceptualInterference_Task = new Dictionary<int, PerceptualInterferance_BlockData>();

        CurrentBlockString = "";
        DefineBlockData();
        blocksAdded = 0;

        RunBlock.AddSpecificInitializationMethod(() =>
        {
            SetSkyBox(CurrentBlock.ContextName);
            trialLevel.ContextActive = true;
            trialLevel.TokenFBController.SetTotalTokensNum(CurrentBlock.TokenBarCapacity);
            trialLevel.TokenFBController.SetTokenBarValue(CurrentBlock.NumInitialTokens);
            trialLevel.ResetBlockVariables();
            CalculateBlockSummaryString();

            //SET STIMULATION CODE FOR THE BLOCK:
            if (currentBlockDef.StimulationConditionCodes != null && currentBlockDef.StimulationConditionCodes.Length > 0)
            {
                int indexNum = currentBlockDef.StimulationConditionCodes.Length == 1 ? 0 : UnityEngine.Random.Range(0, currentBlockDef.StimulationConditionCodes.Length);
                BlockStimulationCode = currentBlockDef.StimulationConditionCodes[indexNum];
                trialLevel.TrialStimulationCode = BlockStimulationCode;
            }
        });

        BlockFeedback.AddSpecificInitializationMethod(() =>
        {
            if (trialLevel.TrialCount_InBlock > LongestStreak)
                LongestStreak = trialLevel.TrialCount_InBlock;

            //Recency Data:
            RecencyInterference_Task.Add(trialLevel.RecencyInterference_Block);

            //Similarity Data:
            AddSimilarityScoreBlockData();


            if(!Session.WebBuild && trialLevel.AbortCode == 0)
            {
                CurrentBlockString += "\n" + "\n";
                CurrentBlockString = CurrentBlockString.Replace("Current Block", $"Block {blocksAdded + 1}");
                blocksAdded++;     
            }
        });        
    }

    private float GetAvgStreak()
    {
        return TrialsCompleted_Task / (BlockCount + 1);
    }

    private void AddSimilarityScoreBlockData()
    {
        if (!PerceptualInterference_Task.ContainsKey(CurrentBlock.PerceptualSimilarity))
            PerceptualInterference_Task[CurrentBlock.PerceptualSimilarity] = new PerceptualInterferance_BlockData();
        
        PerceptualInterference_Task[CurrentBlock.PerceptualSimilarity].TotalBlocks++;
        PerceptualInterference_Task[CurrentBlock.PerceptualSimilarity].TrialsCorrect += trialLevel.NumCorrect_Block;
    }


    public override void SetTaskSummaryString()
    {
        CurrentTaskSummaryString.Clear();
        base.SetTaskSummaryString();
        
        CurrentTaskSummaryString.Append($"\t# TbFilled: {TokenBarCompletions_Task}");
        
    }

    public override OrderedDictionary GetTaskSummaryData()
    {
        OrderedDictionary data = base.GetTaskSummaryData();
        
        data["Trials Correct"] = TrialsCorrect_Task;

        data["TokenBar Completions"] = TokenBarCompletions_Task;
        data["SliderBar Completions"] = SliderBarCompletions_Task;

        data["Perceptual Interference"] = GetPerceptualInterferanceString();
        if(RecencyInterference_Task.Count > 0)
            data["Avg RecencyInterference"] = RecencyInterference_Task.Average();

        data["New Objects Picked"] = NumNew_Picked_Task;
        data["PNC Objects Picked"] = NumPNC_Picked_Task;

        data["Stimulation Pulses Given"] = StimulationPulsesGiven_Task;


        return data;
    }

    public override OrderedDictionary GetTaskResultsData()
    {
        OrderedDictionary data = base.GetTaskResultsData();
        data["Longest Streak"] = LongestStreak;
        data["Average Streak"] = GetAvgStreak();
        data["Trials Correct"] = TrialsCorrect_Task;

        if (Session.SessionDef.IsHuman)
            data["TokenBar Completions"] = TokenBarCompletions_Task;
        else
            data["SliderBar Completions"] = SliderBarCompletions_Task;

        data["New Objects Picked"] = NumNew_Picked_Task;
        data["PNC Objects Picked"] = NumPNC_Picked_Task;

        data["Stimulation Pulses Given"] = StimulationPulsesGiven_Task;

        return data;
    }

    private string GetPerceptualInterferanceString()
    {
        string summary = string.Join(", ",
        PerceptualInterference_Task.Select(kvp =>
        {
            int similarity = kvp.Key;
            PerceptualInterferance_BlockData data = kvp.Value;
            float avg = data.TrialsCorrect / data.TotalBlocks;

            return $"\n(Similarity {similarity} - Blocks {data.TotalBlocks} - Avg Correct {avg:0.00})";
        }));
        return summary;
    }

    private void DefineBlockData()
    {
        BlockData.AddDatum("BlockName", () => CurrentBlock.BlockName);
        BlockData.AddDatum("NonStimTouches", () => trialLevel.NonStimTouches_Block);
        BlockData.AddDatum("NumCorrect", () => trialLevel.NumCorrect_Block);

        BlockData.AddDatum("TokenBarCompletions", () => trialLevel.NumTbCompletions_Block);
        BlockData.AddDatum("SliderBarCompletions", () => trialLevel.SliderBarCompletions_Block);

        BlockData.AddDatum("TimeToChoice", () => trialLevel.AvgTimeToChoice_Block);
        BlockData.AddDatum("TimeToCompletion", () => trialLevel.TimeToCompletion_Block);
        BlockData.AddDatum("MaxTrials", () => CurrentBlock.MaxTrials);

        BlockData.AddDatum("New_Objects_Picked", () => trialLevel.NumNew_Picked_Block);
        BlockData.AddDatum("PNC_Objects_Picked", () => trialLevel.NumPNC_Picked_Block);

        BlockData.AddDatum("StimulationPulsesGiven", () => trialLevel.StimulationPulsesGiven_Block);


    }

    public void CalculateBlockSummaryString()
    {
        CurrentBlockString = "";
        CurrentBlockSummaryString.Clear();

        CurrentBlockString =
                "\nCorrect: " + trialLevel.NumCorrect_Block +
                (Session.SessionDef.IsHuman ? ("\nTbCompletions: " + trialLevel.NumTbCompletions_Block) : ("\nSliderCompletions: " + trialLevel.SliderBarCompletions_Block)) +
                "\nAvgTimeToChoice: " + trialLevel.AvgTimeToChoice_Block.ToString("0.00") + "s" +
                "\nTimeToCompletion: " + trialLevel.TimeToCompletion_Block.ToString("0.00") + "s" +
                "\nReward Pulses: " + NumRewardPulses_InBlock +
                "\nNonStimTouches: " + trialLevel.NonStimTouches_Block +
                "\nStimulationPulsesGiven: " + trialLevel.StimulationPulsesGiven_Block;


        if (BlockStimulationCode > 0)
        {
            CurrentBlockString += "\n\nStimulationCode: " + BlockStimulationCode.ToString();
            CurrentBlockString += "\nStimulationType: " + currentBlockDef.StimulationType.ToString();
        }

        if (blocksAdded > 1)
            CurrentBlockString += "\n";

        //Add CurrentBlockString if block wasn't aborted:
        if (trialLevel.AbortCode == 0)
            CurrentBlockSummaryString.AppendLine(CurrentBlockString.ToString());
    }

}


public class PerceptualInterferance_BlockData
{
    public int TotalBlocks;
    public int TrialsCorrect; //confirm with thilo if want total correct or total completed.

}