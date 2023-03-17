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
    [HideInInspector] public int NumChosenHigherReward_Task = 0;
    [HideInInspector] public int NumChosenLowerReward_Task = 0;
    [HideInInspector] public int NumChosenHigherEffort_Task = 0;
    [HideInInspector] public int NumChosenLowerEffort_Task = 0;
    [HideInInspector] public int NumChosenSameReward_Task = 0;
    [HideInInspector] public int NumChosenSameEffort_Task = 0;
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
            RenderSettings.skybox = CreateSkybox(trialLevel.GetContextNestedFilePath(trialLevel.MaterialFilePath, ContextName, "LinearDark"));
            EventCodeManager.SendCodeImmediate(CustomTaskEventCodes["ContextOn"]);
        });

        BlockFeedback.AddInitializationMethod(() =>
        {
            if (BlockStringsAdded > 0)
                CurrentBlockString += "\n";
            BlockStringsAdded++;
            PreviousBlocksString.Insert(0, CurrentBlockString);
            AddBlockValuesToTaskValues();
        });
    }

    public void SetSettings()
    {
        if (SessionSettings.SettingExists("Session", "IsHuman"))
            trialLevel.IsHuman = (bool)SessionSettings.Get("Session", "IsHuman");
        else
            trialLevel.IsHuman = false;

        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ContextExternalFilePath"))
            trialLevel.MaterialFilePath = (String)SessionSettings.Get(TaskName + "_TaskSettings", "ContextExternalFilePath");
        else if (SessionSettings.SettingExists("Session", "ContextExternalFilePath"))
            trialLevel.MaterialFilePath = (String)SessionSettings.Get("Session", "ContextExternalFilePath");
        else
            Debug.Log("ContextExternalFilePath NOT specified in the Session Config OR Task Config!");

        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ContextName"))
            ContextName = (String)SessionSettings.Get(TaskName + "_TaskSettings", "ContextName");
        else
        {
            ContextName = "Dark";
            Debug.Log($"No ContextName specified in the {TaskName} Task Config. Defaulting to {ContextName}");
        }

        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonPosition"))
        {
            trialLevel.ButtonPosition = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonPosition");
            trialLevel.OriginalStartButtonPosition = trialLevel.ButtonPosition;
        }
        else Debug.Log("[ERROR] Start Button Position settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonScale"))
            trialLevel.ButtonScale = (float)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonScale");
        else Debug.Log("[ERROR] Start Button Position settings not defined in the TaskDef");
    }

    public void AddBlockValuesToTaskValues()
    {
        RewardPulses_Task += trialLevel.RewardPulses_Block;
        Completions_Task += trialLevel.Completions_Block;
        Touches_Task += trialLevel.TotalTouches_Block;
        NumChosenLeft_Task += trialLevel.NumChosenLeft_Block;
        NumChosenRight_Task += trialLevel.NumChosenRight_Block;
        NumChosenHigherEffort_Task += trialLevel.NumHigherEffortChosen_Block;
        NumChosenLowerEffort_Task += trialLevel.NumLowerEffortChosen_Block;
        NumChosenSameEffort_Task += trialLevel.NumSameEffortChosen_Block;
        NumChosenHigherReward_Task += trialLevel.NumHigherRewardChosen_Block;
        NumChosenLowerReward_Task += trialLevel.NumLowerRewardChosen_Block;
        NumChosenSameReward_Task += trialLevel.NumSameRewardChosen_Block;
        NumAborted_Task += trialLevel.NumAborted_Block;
    }

    public override OrderedDictionary GetSummaryData()
    {
        OrderedDictionary data = new OrderedDictionary();

        data["Completions"] = Completions_Task;
        data["Reward Pulses"] = RewardPulses_Task;
        data["Touches"] = Touches_Task;
        data["Chose Left"] = NumChosenLeft_Task;
        data["Chose Right"] = NumChosenRight_Task;
        data["Chose Higher Reward"] = NumChosenHigherReward_Task;
        data["Chose Lower Reward"] = NumChosenLowerReward_Task;
        data["Chose Same Reward"] = NumChosenSameReward_Task;
        data["Chose Higher Effort"] = NumChosenHigherEffort_Task;
        data["Chose Lower Effort"] = NumChosenLowerEffort_Task;
        data["Chose Same Effort"] = NumChosenSameEffort_Task;

        return data;
    }

    public void CalculateBlockSummaryString()
    {
        ClearStrings();

        CurrentBlockString = ("<b>Block Num: </b>" + (trialLevel.BlockCount + 1) + 
                        "\nTrials Completed: " + trialLevel.Completions_Block +
                        "\n\nTouches: " + trialLevel.TotalTouches_Block +
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
        if (PreviousBlocksString.Length > 0)
            BlockSummaryString.AppendLine(PreviousBlocksString.ToString());
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
            decimal percentChoseHigherReward = Math.Round(decimal.Divide(NumChosenHigherReward_Task, (trialLevel.TrialCount_InTask)), 2) * 100;
            decimal percentChoseHigherEffort = Math.Round(decimal.Divide(NumChosenHigherEffort_Task, (trialLevel.TrialCount_InTask)), 2) * 100;
            decimal percentChoseSameReward = Math.Round(decimal.Divide(NumChosenSameReward_Task, (trialLevel.TrialCount_InTask)), 2) * 100;
            decimal percentChoseSameEffort = Math.Round(decimal.Divide(NumChosenSameEffort_Task, (trialLevel.TrialCount_InTask)), 2) * 100;
            
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