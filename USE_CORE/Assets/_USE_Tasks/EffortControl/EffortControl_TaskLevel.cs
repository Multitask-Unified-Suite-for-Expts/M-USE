using System;
using UnityEngine;
using USE_Settings;
using USE_ExperimentTemplate_Task;
using System.Collections.Specialized;
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

    [HideInInspector] public string CurrentBlockString;
    [HideInInspector] public StringBuilder PreviousBlocksString;
    [HideInInspector] public int BlockStringsAdded = 0;
    EffortControl_BlockDef currentBlock => GetCurrentBlockDef<EffortControl_BlockDef>();
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
        
        SetSettings();
        SetupBlockData();

        RunBlock.AddInitializationMethod(() =>
        {
            trialLevel.ResetBlockVariables();

            string contextFilePath;
            if (SessionValues.WebBuild)
                contextFilePath = $"{SessionValues.SessionDef.ContextExternalFilePath}/{currentBlock.ContextName}";
            else
                contextFilePath = trialLevel.GetContextNestedFilePath(SessionValues.SessionDef.ContextExternalFilePath, currentBlock.ContextName, "LinearDark");

            RenderSettings.skybox = CreateSkybox(contextFilePath);

            SessionValues.EventCodeManager.SendCodeImmediate("ContextOn");
        });

        BlockFeedback.AddInitializationMethod(() =>
        {
            AddBlockValuesToTaskValues();

            if(!SessionValues.WebBuild)
            {
                if (BlockStringsAdded > 0)
                    CurrentBlockString += "\n";
                PreviousBlocksString.Insert(0, CurrentBlockString);
                BlockStringsAdded++;
            }
        });
    }

    public void SetSettings()
    {
        trialLevel.ContextExternalFilePath = SessionValues.SessionDef.ContextExternalFilePath;

        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonPosition"))
        {
            trialLevel.ButtonPosition = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonPosition");
            trialLevel.OriginalStartButtonPosition = trialLevel.ButtonPosition;
        }
        else Debug.Log("[ERROR] Start Button Position settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonScale"))
            trialLevel.ButtonScale = (float)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonScale");
        else Debug.Log("[ERROR] Start Button Position settings not defined in the TaskDef");

        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "TouchFeedbackDuration"))
            trialLevel.TouchFeedbackDuration = (float)SessionSettings.Get(TaskName + "_TaskSettings", "TouchFeedbackDuration");
        else
            trialLevel.TouchFeedbackDuration = .3f;

        if (SessionSettings.SettingExists("Session", "MacMainDisplayBuild"))
            trialLevel.MacMainDisplayBuild = (bool)SessionSettings.Get("Session", "MacMainDisplayBuild");
        else
            trialLevel.MacMainDisplayBuild = false;
    }


    public void AddBlockValuesToTaskValues()
    {
        RewardPulses_Task += trialLevel.RewardPulses_Block;
        Completions_Task += trialLevel.Completions_Block;
        Touches_Task += trialLevel.TotalTouches_Block;
        NumChosenLeft_Task += trialLevel.NumChosenLeft_Block;
        NumChosenRight_Task += trialLevel.NumChosenRight_Block;
        NumHigherEffortChosen_Task += trialLevel.NumHigherEffortChosen_Block;
        NumLowerEffortChosen_Task += trialLevel.NumLowerEffortChosen_Block;
        NumSameEffortChosen_Task += trialLevel.NumSameEffortChosen_Block;
        NumHigherRewardChosen_Task += trialLevel.NumHigherRewardChosen_Block;
        NumLowerRewardChosen_Task += trialLevel.NumLowerRewardChosen_Block;
        NumSameRewardChosen_Task += trialLevel.NumSameRewardChosen_Block;
        NumAborted_Task += trialLevel.NumAborted_Block;

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
        OrderedDictionary data = new OrderedDictionary
        {
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
            ["Chose Same Effort"] = NumSameEffortChosen_Task
        };

        return data;
    }

    public void CalculateBlockSummaryString()
    {
        ClearStrings();
        CurrentBlockString = ("Touches: " + trialLevel.TotalTouches_Block +
                        "\nReward Pulses: " + trialLevel.RewardPulses_Block +
                        "\n\nChose Left: " + trialLevel.NumChosenLeft_Block +
                        "\nChose Right: " + trialLevel.NumChosenRight_Block +
                        "\n\nChose Higher Reward: " + trialLevel.NumHigherRewardChosen_Block +
                        "\nChose Lower Reward: " + trialLevel.NumLowerRewardChosen_Block +
                        "\nChose Same Reward: " + trialLevel.NumSameRewardChosen_Block +
                        "\n\nChose Higher Effort: " + trialLevel.NumHigherEffortChosen_Block +
                        "\nChose Lower Effort: " + trialLevel.NumLowerEffortChosen_Block +
                        "\nChoseSameEffort: " + trialLevel.NumSameEffortChosen_Block +
                        "\n");
        BlockSummaryString.AppendLine(CurrentBlockString).ToString();
        /*if (PreviousBlocksString.Length > 0)
            BlockSummaryString.AppendLine(PreviousBlocksString.ToString());*/
    }

    void SetupBlockData()
    {
        BlockData.AddDatum("TrialsCompleted", () => trialLevel.Completions_Block);
        BlockData.AddDatum("ChoseLeft", () => trialLevel.NumChosenLeft_Block);
        BlockData.AddDatum("ChoseRight", () => trialLevel.NumChosenRight_Block);
        BlockData.AddDatum("ChoseHigherReward", () => trialLevel.NumHigherRewardChosen_Block);
        BlockData.AddDatum("ChoseLowerReward", () => trialLevel.NumLowerRewardChosen_Block);
        BlockData.AddDatum("ChoseHigherEffort", () => trialLevel.NumHigherEffortChosen_Block);
        BlockData.AddDatum("ChoseLowerEffort", () => trialLevel.NumLowerEffortChosen_Block);
        BlockData.AddDatum("TotalTouches", () => trialLevel.TotalTouches_Block);
        BlockData.AddDatum("RewardPulses", () => trialLevel.RewardPulses_Block);
    }

    public void ClearStrings()
    {
        CurrentBlockString = "";
        BlockSummaryString.Clear();
    }
    public override void SetTaskSummaryString()
    {
        if (trialLevel.TrialCount_InTask != 0)
        {
            CurrentTaskSummaryString.Clear();

            decimal percentAbortedTrials = (Math.Round(decimal.Divide(NumAborted_Task, (trialLevel.TrialCount_InTask)), 2)) * 100;
            decimal percentChoseLeft = Math.Round(decimal.Divide(NumChosenLeft_Task, (trialLevel.TrialCount_InTask)), 2) * 100;
            decimal percentChoseHigherReward = Math.Round(decimal.Divide(NumHigherRewardChosen_Task, (trialLevel.TrialCount_InTask)), 2) * 100;
            decimal percentChoseHigherEffort = Math.Round(decimal.Divide(NumHigherEffortChosen_Task, (trialLevel.TrialCount_InTask)), 2) * 100;
            decimal percentChoseSameReward = Math.Round(decimal.Divide(NumSameRewardChosen_Task, (trialLevel.TrialCount_InTask)), 2) * 100;
            decimal percentChoseSameEffort = Math.Round(decimal.Divide(NumSameEffortChosen_Task, (trialLevel.TrialCount_InTask)), 2) * 100;
            
            CurrentTaskSummaryString.Append($"\n<b>{ConfigName}</b>" + 
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
            CurrentTaskSummaryString.Append($"\n<b>{ConfigName}</b>");
        }
            
    }

}