using System;
using System.Text;
using System.Collections.Generic;
using System.Collections.Specialized;
using ContinuousRecognition_Namespace;
using UnityEngine;
using USE_ExperimentTemplate_Task;


public class ContinuousRecognition_TaskLevel : ControlLevel_Task_Template
{
    ContinuousRecognition_BlockDef CurrentBlock => GetCurrentBlockDef<ContinuousRecognition_BlockDef>();
    ContinuousRecognition_TrialLevel trialLevel;

    [HideInInspector] public int TrialsCompleted_Task;
    [HideInInspector] public int TrialsCorrect_Task;
    [HideInInspector] public int TokenBarCompletions_Task;
    [HideInInspector] public float NonStimTouches_Task;

    [HideInInspector] public string CurrentBlockString;
    [HideInInspector] public StringBuilder PreviousBlocksString;

    public int blocksAdded;


    public override void SpecifyTypes()
    {
        TaskLevelType = typeof(ContinuousRecognition_TaskLevel);
        TrialLevelType = typeof(ContinuousRecognition_TrialLevel);
        TaskDefType = typeof(ContinuousRecognition_TaskDef);
        BlockDefType = typeof(ContinuousRecognition_BlockDef);
        TrialDefType = typeof(ContinuousRecognition_TrialDef);
        StimDefType = typeof(ContinuousRecognition_StimDef);
    }

    public override void DefineControlLevel()
    {
        trialLevel = (ContinuousRecognition_TrialLevel) TrialLevel;
        CurrentBlockString = "";
        PreviousBlocksString = new StringBuilder();
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
        });

        BlockFeedback.AddSpecificInitializationMethod(() =>
        {
            if(!SessionValues.WebBuild && trialLevel.AbortCode == 0)
            {
                CurrentBlockString += "\n" + "\n";
                CurrentBlockString = CurrentBlockString.Replace("Current Block", $"Block {blocksAdded + 1}");
                PreviousBlocksString.Insert(0, CurrentBlockString); //Add current block string to full list of previous blocks. 
                blocksAdded++;     
            }
        });        
    }

    public override void SetTaskSummaryString()
    {
        CurrentTaskSummaryString.Clear();
        base.SetTaskSummaryString();
        
        CurrentTaskSummaryString.Append($"\t<b># TbFilled:</b> {TokenBarCompletions_Task}");
        
    }

    public override OrderedDictionary GetBlockResultsData()
    {
        OrderedDictionary data = new OrderedDictionary
        {
            ["Score"] = trialLevel.Score + "XP",
            ["Trials Correct"] = trialLevel.NumCorrect_Block,
            ["Trials Completed"] = trialLevel.TrialCount_InBlock + 1,
            ["Completion Time"] = trialLevel.TimeToCompletion_Block.ToString("0") + "s",
            ["TokenBar Completions"] = trialLevel.NumTbCompletions_Block
        };
        return data;
    }

    public override OrderedDictionary GetTaskSummaryData()
    {
        OrderedDictionary data = base.GetTaskSummaryData();
        
        data["Trials Completed"] = TrialsCompleted_Task;
        data["Trials Correct"] = TrialsCorrect_Task;
        data["TokenBar Completions"] = TokenBarCompletions_Task;

        return data;
    }

    private void DefineBlockData()
    {
        BlockData.AddDatum("BlockName", () => CurrentBlock.BlockName);
        BlockData.AddDatum("NonStimTouches", () => trialLevel.NonStimTouches_Block);
        BlockData.AddDatum("NumCorrect", () => trialLevel.NumCorrect_Block);
        BlockData.AddDatum("TokenBarCompletions", () => trialLevel.NumTbCompletions_Block);
        BlockData.AddDatum("TimeToChoice", () => trialLevel.AvgTimeToChoice_Block);
        BlockData.AddDatum("TimeToCompletion", () => trialLevel.TimeToCompletion_Block);
        BlockData.AddDatum("MaxTrials", () => CurrentBlock.MaxTrials);
    }

    public void CalculateBlockSummaryString()
    {
        CurrentBlockString = "";
        CurrentBlockSummaryString.Clear();

        CurrentBlockString = 
                "\nCorrect: " + trialLevel.NumCorrect_Block +
                "\nTbCompletions: " + trialLevel.NumTbCompletions_Block +
                "\nAvgTimeToChoice: " + trialLevel.AvgTimeToChoice_Block.ToString("0.00") + "s" +
                "\nTimeToCompletion: " + trialLevel.TimeToCompletion_Block.ToString("0.00") + "s" +
                "\nReward Pulses: " + NumRewardPulses_InBlock + 
                "\nNonStimTouches: " + trialLevel.NonStimTouches_Block;
        if (blocksAdded > 1)
            CurrentBlockString += "\n";

        //Add CurrentBlockString if block wasn't aborted:
        if (trialLevel.AbortCode == 0)
            CurrentBlockSummaryString.AppendLine(CurrentBlockString.ToString());
    }

}
