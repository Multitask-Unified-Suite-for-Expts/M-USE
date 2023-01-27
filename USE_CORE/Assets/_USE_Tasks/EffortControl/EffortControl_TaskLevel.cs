using System;
using System.Text;
using System.Collections.Generic;
using EffortControl_Namespace;
using UnityEngine;
using UnityEngine.UI;
using USE_Settings;
using USE_StimulusManagement;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Block;
using System.Collections.Specialized;
using System.IO;
using FLU_Common_Namespace;
using WhatWhenWhere_Namespace;
using THR_Namespace;

public class EffortControl_TaskLevel : ControlLevel_Task_Template
{
    [HideInInspector] public string ContextName;

    [HideInInspector] public int Completions_Task = 0;
    [HideInInspector] public int RewardPulses_Task = 0;
    [HideInInspector] public int Touches_Task = 0;
    [HideInInspector] public int NumChosenLeft_Task = 0;
    [HideInInspector] public int NumChosenRight_Task = 0;
    [HideInInspector] public int NumChosenHigherReward_Task = 0;
    [HideInInspector] public int NumChosenLowerReward_Task = 0;
    [HideInInspector] public int NumChosenHigherEffort_Task = 0;
    [HideInInspector] public int NumChosenLowerEffort_Task = 0;

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

        string TaskName = "EffortControl";
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
            trialLevel.ButtonScale = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonScale");
        else Debug.Log("[ERROR] Start Button Position settings not defined in the TaskDef");

        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "IsHuman"))
            trialLevel.IsHuman = (bool)SessionSettings.Get(TaskName + "_TaskSettings", "IsHuman");
        else
            trialLevel.IsHuman = false;


        CurrentBlockString = "";
        PreviousBlocksString = new StringBuilder();

        SetupBlockData();

        SetupTask.AddInitializationMethod(() =>
        {
            RenderSettings.skybox = CreateSkybox(trialLevel.GetContextNestedFilePath(ContextName));
        });

        RunBlock.AddInitializationMethod(() =>
        {
            trialLevel.RewardPulses_Block = 0;
            trialLevel.Completions_Block = 0;
            trialLevel.Touches_Block = 0;
            trialLevel.NumChosenLeft_Block = 0;
            trialLevel.NumChosenRight_Block = 0;
            trialLevel.NumHigherEffortChosen_Block = 0;
            trialLevel.NumLowerEffortChosen_Block = 0;
            trialLevel.NumHigherRewardChosen_Block = 0;
            trialLevel.NumLowerRewardChosen_Block = 0;

            CalculateBlockSummaryString();
        });

        BlockFeedback.AddInitializationMethod(() =>
        {
            if (BlockStringsAdded > 0)
                CurrentBlockString += "\n";
            BlockStringsAdded++;
            PreviousBlocksString.Insert(0, CurrentBlockString);

            RewardPulses_Task += trialLevel.RewardPulses_Block;
            Completions_Task += trialLevel.Completions_Block;
            Touches_Task += trialLevel.Touches_Block;
            NumChosenLeft_Task += trialLevel.NumChosenLeft_Block;
            NumChosenRight_Task += trialLevel.NumChosenRight_Block;
            NumChosenHigherEffort_Task += trialLevel.NumHigherEffortChosen_Block;
            NumChosenLowerEffort_Task += trialLevel.NumLowerEffortChosen_Block;
            NumChosenHigherReward_Task += trialLevel.NumHigherRewardChosen_Block;
            NumChosenLowerReward_Task += trialLevel.NumLowerRewardChosen_Block;
        });
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
        data["Chose Higher Effort"] = NumChosenHigherEffort_Task;
        data["Chose Lower Effort"] = NumChosenLowerEffort_Task;

        return data;
    }

    public void CalculateBlockSummaryString()
    {
        ClearStrings();

        CurrentBlockString = ("<b>Block " + "(" + currentBlock.BlockName + "):" + "</b>" +
                        "\nTrialsCompleted: " + trialLevel.Completions_Block +
                        "\nTouches: " + trialLevel.Touches_Block +
                        "\nRewardPulses: " + trialLevel.RewardPulses_Block +
                        "\nChoseLeft: " + trialLevel.NumChosenLeft_Block +
                        "\nChoseRight: " + trialLevel.NumChosenRight_Block +
                        "\nChoseHigherReward: " + trialLevel.NumHigherRewardChosen_Block +
                        "\nChoseLowerReward: " + trialLevel.NumLowerRewardChosen_Block +
                        "\nChoseHigherEffort: " + trialLevel.NumHigherEffortChosen_Block +
                        "\nChoseLowerEffort: " + trialLevel.NumLowerEffortChosen_Block +
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
        BlockData.AddDatum("Touches", () => trialLevel.Touches_Block);
        BlockData.AddDatum("RewardPulses", () => trialLevel.RewardPulses_Block);
    }

    void ClearStrings()
    {
        CurrentBlockString = "";
        BlockSummaryString.Clear();
    }

}