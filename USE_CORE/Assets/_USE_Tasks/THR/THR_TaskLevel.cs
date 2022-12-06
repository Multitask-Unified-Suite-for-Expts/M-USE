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
using System.Collections.Specialized;

public class THR_TaskLevel : ControlLevel_Task_Template
{
    public string CurrentBlockString;
    public StringBuilder PreviousBlocksString;

    public int BlockStringsAdded;

    THR_BlockDef currentBlock => GetCurrentBlockDef<THR_BlockDef>();

    public int TrialsCompleted_Task = 0;
    public int TrialsCorrect_Task = 0;
    public int BlueSquareTouches_Task = 0;
    public int WhiteSquareTouches_Task = 0;
    public int NonSquareTouches_Task = 0;
    public int TouchRewards_Task = 0;
    public int ReleaseRewards_Task = 0;


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

        SetupTask.AddInitializationMethod(() => {
            SetupBlockData(trialLevel);
            BlockStringsAdded = 0;
        });

        RunBlock.AddInitializationMethod(() =>
        {
            trialLevel.TrialsCompleted_Block = 0;
            trialLevel.TrialsCorrect_Block = 0;
            trialLevel.NonSquareTouches_Block = 0;
            trialLevel.BlueSquareTouches_Block = 0;
            trialLevel.WhiteSquareTouches_Block = 0;
            trialLevel.NumTouchRewards_Block = 0;
            trialLevel.NumReleaseRewards_Block = 0;
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
            if(BlockStringsAdded > 0)
                CurrentBlockString += "\n";
            BlockStringsAdded++;
            PreviousBlocksString.Insert(0, CurrentBlockString);

            TrialsCompleted_Task += trialLevel.TrialsCompleted_Block;
            TrialsCorrect_Task += trialLevel.TrialsCorrect_Block;
            BlueSquareTouches_Task += trialLevel.BlueSquareTouches_Block;
            WhiteSquareTouches_Task += trialLevel.WhiteSquareTouches_Block;
            NonSquareTouches_Task += trialLevel.NonSquareTouches_Block;
            TouchRewards_Task += trialLevel.NumTouchRewards_Block;
            ReleaseRewards_Task += trialLevel.NumReleaseRewards_Block;
        });
    }

    public override OrderedDictionary GetSummaryData()
    {
        OrderedDictionary data = new OrderedDictionary();

        data["Trials Completed"] = TrialsCompleted_Task;
        data["Trials Correct"] = TrialsCorrect_Task;
        data["Blue Square Touches"] = BlueSquareTouches_Task;
        data["White Square Touches"] = WhiteSquareTouches_Task;
        data["Non Square Touches"] = NonSquareTouches_Task;
        data["Touch Rewards"] = TouchRewards_Task;
        data["Release Rewards"] = ReleaseRewards_Task;

        return data;
    }

    void SetBlockSummaryString(THR_TrialLevel trialLevel)
    {
        ClearStrings();

        CurrentBlockString = ("<b>Block " + "(" + currentBlock.BlockName + "):" + "</b>" +
                        "\nNumTrialsCompleted: " + trialLevel.TrialsCompleted_Block +
                        "\nNumTrialsCorrect: " + trialLevel.TrialsCorrect_Block +
                        "\nPerformance: " + trialLevel.PerformancePercentage + "%" +
                        "\nNumTouchesWhiteSquare: " + trialLevel.WhiteSquareTouches_Block +
                        "\nNumTouchesBlueSquare: " + trialLevel.BlueSquareTouches_Block +
                        "\nNumTouchesOutsideSquare: " + trialLevel.NonSquareTouches_Block +
                        "\nNumRewards: " + (trialLevel.NumTouchRewards_Block + trialLevel.NumReleaseRewards_Block) +
                        "\nPerfThresholdMet: " + trialLevel.PerfThresholdMet +
                        "\n");

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
        BlockData.AddDatum("NumTrialsCompleted", () => trialLevel.TrialsCompleted_Block);
        BlockData.AddDatum("NumTrialsCorrect", () => trialLevel.TrialsCorrect_Block);
        BlockData.AddDatum("WhiteSquareTouches_Block", () => trialLevel.WhiteSquareTouches_Block);
        BlockData.AddDatum("BlueSquareTouches_Block", () => trialLevel.BlueSquareTouches_Block);
        BlockData.AddDatum("NonSquareTouches_Block", () => trialLevel.NonSquareTouches_Block);
        BlockData.AddDatum("NumTouchRewards", () => trialLevel.NumTouchRewards_Block);
        BlockData.AddDatum("NumReleaseRewards", () => trialLevel.NumReleaseRewards_Block);
        BlockData.AddDatum("DifficultyLevel", () => currentBlock.BlockName);
    }

    public T GetCurrentBlockDef<T>() where T : BlockDef
    {
        return (T)CurrentBlockDef;
    }

}