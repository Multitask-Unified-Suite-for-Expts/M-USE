using System;
using System.Collections.Generic;
using UnityEngine;
using USE_ExperimentTemplate_Task;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using EffortControl_Namespace;


public class EffortControl_TaskLevel : ControlLevel_Task_Template
{
    //[HideInInspector] public string ContextName;

    //Task Values used for SummaryData file
    [HideInInspector] public int Completions_Task = 0;
    [HideInInspector] public int Touches_Task = 0;
    [HideInInspector] public int NumChosenLeft_Task = 0;
    [HideInInspector] public int NumChosenRight_Task = 0;
    [HideInInspector] public int NumHigherRewardChosen_Task = 0;
    [HideInInspector] public int NumLowerRewardChosen_Task = 0;
    [HideInInspector] public int NumSameRewardChosen_Task = 0;
    [HideInInspector] public int NumHigherEffortChosen_Task = 0;
    [HideInInspector] public int NumLowerEffortChosen_Task = 0;
    [HideInInspector] public int NumSameEffortChosen_Task = 0;
    [HideInInspector] public List<float?> InflationDurations_Task = new List<float?>();

    [HideInInspector] public string CurrentBlockString;
    [HideInInspector] public StringBuilder PreviousBlocksString;
    [HideInInspector] public int BlockStringsAdded = 0;
    EffortControl_BlockDef CurrentBlock => GetCurrentBlockDef<EffortControl_BlockDef>();
    private EffortControl_TaskDef currentTaskDef => GetTaskDef<EffortControl_TaskDef>();
    EffortControl_TrialLevel trialLevel;


    public override void DefineControlLevel()
    {
        trialLevel = (EffortControl_TrialLevel)TrialLevel;

        CurrentBlockString = "";
        PreviousBlocksString = new StringBuilder();
        
        DefineBlockData();

        RunBlock.AddSpecificInitializationMethod(() =>
        {
            CurrentBlock.ContextName = CurrentBlock.ContextName.Trim();
            SetSkyBox(CurrentBlock.ContextName);
            trialLevel.ResetBlockVariables();
        });

        BlockFeedback.AddSpecificInitializationMethod(() => HandleBlockStrings());
    }

    private void HandleBlockStrings()
    {
        if (!SessionValues.WebBuild)
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
            ["Total Touches"] = trialLevel.TotalTouches_Block,
            ["Chose Higher Effort"] = trialLevel.NumHigherEffortChosen_Block,
            ["Chose Lower Effort"] = trialLevel.NumLowerEffortChosen_Block,
            ["Chose Higher Reward"] = trialLevel.NumHigherRewardChosen_Block,
            ["Chose Lower Reward"] = trialLevel.NumLowerRewardChosen_Block
        };
        return data;
    }

    public override OrderedDictionary GetTaskSummaryData()
    {

        OrderedDictionary data = base.GetTaskSummaryData();

        data["Completions"] = Completions_Task;
        data["Touches"] = Touches_Task;
        data["Chose Left"] = NumChosenLeft_Task;
        data["Chose Right"] = NumChosenRight_Task;
        data["Chose Higher Reward"] = NumHigherRewardChosen_Task;
        data["Chose Lower Reward"] = NumLowerRewardChosen_Task;
        data["Chose Same Reward"] = NumSameRewardChosen_Task;
        data["Chose Higher Effort"] = NumHigherEffortChosen_Task;
        data["Chose Lower Effort"] = NumLowerEffortChosen_Task;
        data["Chose Same Effort"] = NumSameEffortChosen_Task;
        data["Avg Inflation Duration"] = CalculateAverageDuration(InflationDurations_Task);

        return data;
    }

    public void CalculateBlockSummaryString()
    {
        ClearStrings();
        CurrentBlockString = ("\nTouches: " + trialLevel.TotalTouches_Block +
                        "\nReward Pulses: " + NumRewardPulses_InBlock +
                        "\n\nChose Left: " + trialLevel.NumChosenLeft_Block +
                        "\nChose Right: " + trialLevel.NumChosenRight_Block +
                        "\n\nChose Higher Reward: " + trialLevel.NumHigherRewardChosen_Block +
                        "\nChose Lower Reward: " + trialLevel.NumLowerRewardChosen_Block +
                        "\nChose Same Reward: " + trialLevel.NumSameRewardChosen_Block +
                        "\n\nChose Higher Effort: " + trialLevel.NumHigherEffortChosen_Block +
                        "\nChose Lower Effort: " + trialLevel.NumLowerEffortChosen_Block +
                        "\nChose Same Effort: " + trialLevel.NumSameEffortChosen_Block);
        CurrentBlockSummaryString.AppendLine(CurrentBlockString).ToString();
        /*if (PreviousBlocksString.Length > 0)
            CurrentBlockSummaryString.AppendLine(PreviousBlocksString.ToString());*/
    }

    void DefineBlockData()
    {
        BlockData.AddDatum("TrialsCompleted", () => trialLevel.Completions_Block);
        BlockData.AddDatum("ChoseLeft", () => trialLevel.NumChosenLeft_Block);
        BlockData.AddDatum("ChoseRight", () => trialLevel.NumChosenRight_Block);
        BlockData.AddDatum("ChoseHigherReward", () => trialLevel.NumHigherRewardChosen_Block);
        BlockData.AddDatum("ChoseLowerReward", () => trialLevel.NumLowerRewardChosen_Block);
        BlockData.AddDatum("ChoseHigherEffort", () => trialLevel.NumHigherEffortChosen_Block);
        BlockData.AddDatum("ChoseLowerEffort", () => trialLevel.NumLowerEffortChosen_Block);
        BlockData.AddDatum("TotalTouches", () => trialLevel.TotalTouches_Block);
        BlockData.AddDatum("AvgInflationDuration", () => CalculateAverageDuration(trialLevel.InflationDurations_Block));
        
    }

    public void ClearStrings()
    {
        CurrentBlockString = "";
        CurrentBlockSummaryString.Clear();
    }
    public override void SetTaskSummaryString()
    {
        CurrentTaskSummaryString.Clear();
        base.SetTaskSummaryString();

        if (trialLevel.TrialCount_InTask != 0)
        {
            decimal percentChoseLeft = Math.Round(decimal.Divide(NumChosenLeft_Task, (trialLevel.TrialCount_InTask)), 2) * 100;
            decimal percentChoseHigherReward = Math.Round(decimal.Divide(NumHigherRewardChosen_Task, (trialLevel.TrialCount_InTask)), 2) * 100;
            decimal percentChoseHigherEffort = Math.Round(decimal.Divide(NumHigherEffortChosen_Task, (trialLevel.TrialCount_InTask)), 2) * 100;
            decimal percentChoseSameReward = Math.Round(decimal.Divide(NumSameRewardChosen_Task, (trialLevel.TrialCount_InTask)), 2) * 100;
            decimal percentChoseSameEffort = Math.Round(decimal.Divide(NumSameEffortChosen_Task, (trialLevel.TrialCount_InTask)), 2) * 100;

            CurrentTaskSummaryString.Append($"\n# Token Bar Completions: {Completions_Task}" +
                                            $"\n% Chose Left: {percentChoseLeft}%" +
                                            $"\n% Chose Higher Reward: {percentChoseHigherReward}% (Same Reward: {percentChoseSameReward}%)" +
                                            $"\n% Chose Higher Effort: {percentChoseHigherEffort}% (Same Effort: {percentChoseSameEffort}%)");
        }
            
    }

}