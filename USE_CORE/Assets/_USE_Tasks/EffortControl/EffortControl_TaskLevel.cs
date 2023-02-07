using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using USE_Settings;
using USE_StimulusManagement;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Block;
using System.Collections.Specialized;
using System.IO;
using EffortControl_Namespace;

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
    //[HideInInspector] public StringBuilder PreviousBlocksString;
    //[HideInInspector] public int BlockStringsAdded = 0;

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
        SetSettings();
        SetupBlockData();

        SetupTask.AddInitializationMethod(() =>
        {
            RenderSettings.skybox = CreateSkybox(trialLevel.GetContextNestedFilePath(trialLevel.MaterialFilePath, ContextName, "LinearDark"));
        });

        BlockFeedback.AddInitializationMethod(() =>
        {
            AddTrialValuesToTaskValues();
        });
    }

    public void SetSettings()
    {
        if (SessionSettings.SettingExists("Session", "IsHuman"))
            trialLevel.IsHuman = (bool)SessionSettings.Get("Session", "IsHuman");
        else
            trialLevel.IsHuman = false;

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
    }

    public void AddTrialValuesToTaskValues()
    {
        RewardPulses_Task += trialLevel.RewardPulses;
        Completions_Task += trialLevel.Completions;
        Touches_Task += trialLevel.TotalTouches;
        NumChosenLeft_Task += trialLevel.NumChosenLeft;
        NumChosenRight_Task += trialLevel.NumChosenRight;
        NumChosenHigherEffort_Task += trialLevel.NumHigherEffortChosen;
        NumChosenLowerEffort_Task += trialLevel.NumLowerEffortChosen;
        NumChosenHigherReward_Task += trialLevel.NumHigherRewardChosen;
        NumChosenLowerReward_Task += trialLevel.NumLowerRewardChosen;
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

        CurrentBlockString = ("\nTrialsCompleted: " + trialLevel.Completions +
                        "\nTouches: " + trialLevel.TotalTouches +
                        "\nRewardPulses: " + trialLevel.RewardPulses +
                        "\nChoseLeft: " + trialLevel.NumChosenLeft +
                        "\nChoseRight: " + trialLevel.NumChosenRight +
                        "\nChoseHigherReward: " + trialLevel.NumHigherRewardChosen +
                        "\nChoseLowerReward: " + trialLevel.NumLowerRewardChosen +
                        "\nChoseHigherEffort: " + trialLevel.NumHigherEffortChosen +
                        "\nChoseLowerEffort: " + trialLevel.NumLowerEffortChosen +
                        "\n");
        BlockSummaryString.AppendLine(CurrentBlockString).ToString();
    }

    void SetupBlockData()
    {
        BlockData.AddDatum("TrialsCompleted", () => trialLevel.Completions);
        BlockData.AddDatum("ChoseLeft", () => trialLevel.NumChosenLeft);
        BlockData.AddDatum("ChoseRight", () => trialLevel.NumChosenRight);
        BlockData.AddDatum("ChoseHigherReward", () => trialLevel.NumHigherRewardChosen);
        BlockData.AddDatum("ChoseLowerReward", () => trialLevel.NumLowerRewardChosen);
        BlockData.AddDatum("ChoseHigherEffort", () => trialLevel.NumHigherEffortChosen);
        BlockData.AddDatum("ChoseLowerEffort", () => trialLevel.NumLowerEffortChosen);
        BlockData.AddDatum("TotalTouches", () => trialLevel.TotalTouches);
        BlockData.AddDatum("RewardPulses", () => trialLevel.RewardPulses);
    }

    void ClearStrings()
    {
        CurrentBlockString = "";
        BlockSummaryString.Clear();
    }

}