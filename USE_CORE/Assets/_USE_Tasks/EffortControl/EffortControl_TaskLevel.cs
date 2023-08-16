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
    [HideInInspector] public string ContextName;

    //Task Values used for SummaryData file
    [HideInInspector] public int Completions_Task = 0;
    [HideInInspector] public int RewardPulses_Task = 0;
    [HideInInspector] public int Touches_Task = 0;
    [HideInInspector] public int NumChosenLeft_Task = 0;
    [HideInInspector] public int NumChosenRight_Task = 0;
    [HideInInspector] public int NumHigherRewardChosen_Task = 0;
    [HideInInspector] public int NumLowerRewardChosen_Task = 0;
    [HideInInspector] public int NumSameRewardChosen_Task = 0;
    [HideInInspector] public int NumHigherEffortChosen_Task = 0;
    [HideInInspector] public int NumLowerEffortChosen_Task = 0;
    [HideInInspector] public int NumSameEffortChosen_Task = 0;
    [HideInInspector] public int NumAborted_Task = 0;
    [HideInInspector] public List<float?> InflationDurations_Task = new List<float?>();

    [HideInInspector] public string CurrentBlockString;
    [HideInInspector] public StringBuilder PreviousBlocksString;
    [HideInInspector] public int BlockStringsAdded = 0;
    EffortControl_BlockDef CurrentBlock => GetCurrentBlockDef<EffortControl_BlockDef>();
    EffortControl_TrialLevel trialLevel;

    public override void SpecifyTypes()
    {
        TaskLevelType = typeof(EffortControl_TaskLevel);
        TrialLevelType = typeof(EffortControl_TrialLevel);
        TaskDefType = typeof(EffortControl_TaskDef);
        BlockDefType = typeof(EffortControl_BlockDef);
        TrialDefType = typeof(EffortControl_TrialDef);
        StimDefType = typeof(EffortControl_StimDef);
    }

    public override void DefineControlLevel()
    {
        trialLevel = (EffortControl_TrialLevel)TrialLevel;

        CurrentBlockString = "";
        PreviousBlocksString = new StringBuilder();
        
        SetupBlockData();

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
        float avgInflationDuration_Task;
        
        if (InflationDurations_Task.Any(item => item.HasValue))
        {
            avgInflationDuration_Task = (float)InflationDurations_Task
                .Where(item => item.HasValue)
                .Average(item => item.Value);
        }
        else
        {
            avgInflationDuration_Task = 0f;
        }

        OrderedDictionary data = new OrderedDictionary
        {
            ["Trial Count In Task"] = trialLevel.TrialCount_InTask + 1,
            
            ["Completions"] = Completions_Task,
            ["Reward Pulses"] = RewardPulses_Task,
            ["Touches"] = Touches_Task,
            ["Chose Left"] = NumChosenLeft_Task,
            ["Chose Right"] = NumChosenRight_Task,
            ["Chose Higher Reward"] = NumHigherRewardChosen_Task,
            ["Chose Lower Reward"] = NumLowerRewardChosen_Task,
            ["Chose Same Reward"] = NumSameRewardChosen_Task,
            ["Chose Higher Effort"] = NumHigherEffortChosen_Task,
            ["Chose Lower Effort"] = NumLowerEffortChosen_Task,
            ["Chose Same Effort"] = NumSameEffortChosen_Task,
            ["Avg Inflation Duration"] = avgInflationDuration_Task
        };

        return data;
    }

    public void CalculateBlockSummaryString()
    {
        ClearStrings();
        CurrentBlockString = ("\nTouches: " + trialLevel.TotalTouches_Block +
                        "\nReward Pulses: " + trialLevel.RewardPulses_Block +
                        "\n\nChose Left: " + trialLevel.NumChosenLeft_Block +
                        "\nChose Right: " + trialLevel.NumChosenRight_Block +
                        "\n\nChose Higher Reward: " + trialLevel.NumHigherRewardChosen_Block +
                        "\nChose Lower Reward: " + trialLevel.NumLowerRewardChosen_Block +
                        "\nChose Same Reward: " + trialLevel.NumSameRewardChosen_Block +
                        "\n\nChose Higher Effort: " + trialLevel.NumHigherEffortChosen_Block +
                        "\nChose Lower Effort: " + trialLevel.NumLowerEffortChosen_Block +
                        "\nChoseSameEffort: " + trialLevel.NumSameEffortChosen_Block);
        CurrentBlockSummaryString.AppendLine(CurrentBlockString).ToString();
        /*if (PreviousBlocksString.Length > 0)
            CurrentBlockSummaryString.AppendLine(PreviousBlocksString.ToString());*/
    }

    void SetupBlockData()
    {
        float avgInflationDuration_Block;
        if (trialLevel.InflationDurations_Block.Any(item => item.HasValue))
        {
            avgInflationDuration_Block = (float)trialLevel.InflationDurations_Block
                .Where(item => item.HasValue)
                .Average(item => item.Value);
        }
        else
        {
            avgInflationDuration_Block = 0f;
        }

        
        BlockData.AddDatum("TrialsCompleted", () => trialLevel.Completions_Block);
        BlockData.AddDatum("ChoseLeft", () => trialLevel.NumChosenLeft_Block);
        BlockData.AddDatum("ChoseRight", () => trialLevel.NumChosenRight_Block);
        BlockData.AddDatum("ChoseHigherReward", () => trialLevel.NumHigherRewardChosen_Block);
        BlockData.AddDatum("ChoseLowerReward", () => trialLevel.NumLowerRewardChosen_Block);
        BlockData.AddDatum("ChoseHigherEffort", () => trialLevel.NumHigherEffortChosen_Block);
        BlockData.AddDatum("ChoseLowerEffort", () => trialLevel.NumLowerEffortChosen_Block);
        BlockData.AddDatum("TotalTouches", () => trialLevel.TotalTouches_Block);
        BlockData.AddDatum("RewardPulses", () => trialLevel.RewardPulses_Block);
        BlockData.AddDatum("AvgInflationDuration", () =>avgInflationDuration_Block);
        
    }

    public void ClearStrings()
    {
        CurrentBlockString = "";
        CurrentBlockSummaryString.Clear();
    }
    public override void SetTaskSummaryString()
    {
        CurrentTaskSummaryString.Clear();

        if (trialLevel.TrialCount_InTask != 0)
        {
            decimal percentAbortedTrials = (Math.Round(decimal.Divide(NumAborted_Task, (trialLevel.TrialCount_InTask)), 2)) * 100;
            decimal percentChoseLeft = Math.Round(decimal.Divide(NumChosenLeft_Task, (trialLevel.TrialCount_InTask)), 2) * 100;
            decimal percentChoseHigherReward = Math.Round(decimal.Divide(NumHigherRewardChosen_Task, (trialLevel.TrialCount_InTask)), 2) * 100;
            decimal percentChoseHigherEffort = Math.Round(decimal.Divide(NumHigherEffortChosen_Task, (trialLevel.TrialCount_InTask)), 2) * 100;
            decimal percentChoseSameReward = Math.Round(decimal.Divide(NumSameRewardChosen_Task, (trialLevel.TrialCount_InTask)), 2) * 100;
            decimal percentChoseSameEffort = Math.Round(decimal.Divide(NumSameEffortChosen_Task, (trialLevel.TrialCount_InTask)), 2) * 100;
            
            CurrentTaskSummaryString.Append($"\n<b>{ConfigFolderName}</b>" + 
                                            $"\n<b># Trials:</b> {trialLevel.TrialCount_InTask} ({percentAbortedTrials}% aborted)" + 
                                            $"\t<b># Blocks:</b> {BlockCount}" + 
                                            $"\t<b># Reward Pulses:</b> {RewardPulses_Task}" +
                                            $"\n# Token Bar Completions: {Completions_Task}" +
                                            $"\n% Chose Left: {percentChoseLeft}%" +
                                            $"\n% Chose Higher Reward: {percentChoseHigherReward}% (Same Reward: {percentChoseSameReward}%)" + 
                                            $"\n% Chose Higher Effort: {percentChoseHigherEffort}% (Same Effort: {percentChoseSameEffort}%)");
        }
        else
        {
            CurrentTaskSummaryString.Append($"\n<b>{ConfigFolderName}</b>");
        }
            
    }

}