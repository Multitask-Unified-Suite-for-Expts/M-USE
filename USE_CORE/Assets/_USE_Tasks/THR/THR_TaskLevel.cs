using System;
using System.Text;
using System.Collections.Generic;
using THR_Namespace;
using UnityEngine;
using UnityEngine.UI;
using USE_Settings;
using USE_StimulusManagement;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Block;

public class THR_TaskLevel : ControlLevel_Task_Template
{
    public string CurrentBlockString;
    public StringBuilder PreviousBlocksString;

    THR_BlockDef currentBlock => GetCurrentBlockDef<THR_BlockDef>();


    public override void SpecifyTypes()
    {
        TaskLevelType = typeof(THR_TaskLevel);
        TrialLevelType = typeof(THR_TrialLevel);
        TaskDefType = typeof(THR_TaskDef);
        BlockDefType = typeof(THR_BlockDef);
        TrialDefType = typeof(THR_TrialDef);
        StimDefType = typeof(THR_StimDef);
    }

    public override void DefineControlLevel()
    {
        THR_TrialLevel trialLevel = (THR_TrialLevel)TrialLevel;

        string TaskName = "THR";
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ContextExternalFilePath"))
            trialLevel.MaterialFilePath = (String)SessionSettings.Get(TaskName + "_TaskSettings", "ContextExternalFilePath");

        CurrentBlockString = "";
        PreviousBlocksString = new StringBuilder();

        SetupTask.AddInitializationMethod(() => SetupBlockData(trialLevel));

        RunBlock.AddInitializationMethod(() =>
        {
            trialLevel.NumTrialsCompletedBlock = 0;
            trialLevel.NumTrialsCorrectBlock = 0;
            trialLevel.NumNonSquareTouches = 0;
            trialLevel.NumTouchesBlueSquare = 0;
            trialLevel.NumTouchesWhiteSquare = 0;
            trialLevel.NumTouchRewards = 0;
            trialLevel.NumReleaseRewards = 0;
            trialLevel.PerfThresholdMet = false;

            SetBlockSummaryString(trialLevel);
        });
        RunBlock.AddUpdateMethod(() =>
        {
            if(trialLevel.TrialComplete)
            {
                SetBlockSummaryString(trialLevel);
                trialLevel.TrialComplete = false;
            }
        });

        BlockFeedback.AddInitializationMethod(() =>
        {
            if (BlockCount > 0)
                CurrentBlockString += "\n";
            PreviousBlocksString.Insert(0, CurrentBlockString);
        });

    }

    void SetBlockSummaryString(THR_TrialLevel trialLevel)
    {
        ClearStrings();

        CurrentBlockString = ("<b>Block " + "(" + currentBlock.BlockName + "):" + "</b>" +
                        "\nNumTrialsCompleted: " + trialLevel.NumTrialsCompletedBlock +
                        "\nNumTrialsCorrect: " + trialLevel.NumTrialsCorrectBlock +
                        "\nNumTouchesWhiteSquare: " + trialLevel.NumTouchesWhiteSquare +
                        "\nNumTouchesBlueSquare: " + trialLevel.NumTouchesBlueSquare +
                        "\nNumTouchesOutsideSquare: " + trialLevel.NumNonSquareTouches +
                        "\nNumRewards: " + (trialLevel.NumTouchRewards + trialLevel.NumReleaseRewards) +
                        "\nPerfThresholdMet? " + trialLevel.PerfThresholdMet);

        if (BlockCount > 0)
            CurrentBlockString += "\n";

        BlockSummaryString.AppendLine(CurrentBlockString).ToString();
        if (PreviousBlocksString.Length > 0)
            BlockSummaryString.AppendLine(PreviousBlocksString.ToString());
    }

    void ClearStrings()
    {
        CurrentBlockString = "";
        BlockSummaryString.Clear();
    }

    void SetupBlockData(THR_TrialLevel trialLevel)
    {
        BlockData.AddDatum("NumTrialsCompleted", () => trialLevel.NumTrialsCompletedBlock);
        BlockData.AddDatum("NumTrialsCorrect", () => trialLevel.NumTrialsCorrectBlock);
        BlockData.AddDatum("NumTouchesWhiteSquare", () => trialLevel.NumTouchesWhiteSquare);
        BlockData.AddDatum("NumTouchesBlueSquare", () => trialLevel.NumTouchesBlueSquare);
        BlockData.AddDatum("NumTouchesOutsideSquare", () => trialLevel.NumNonSquareTouches);
        BlockData.AddDatum("NumTouchRewards", () => trialLevel.NumTouchRewards);
        BlockData.AddDatum("NumReleaseRewards", () => trialLevel.NumReleaseRewards);
        BlockData.AddDatum("DifficultyLevel", () => currentBlock.BlockName);
    }

    public T GetCurrentBlockDef<T>() where T : BlockDef
    {
        return (T)CurrentBlockDef;
    }


}