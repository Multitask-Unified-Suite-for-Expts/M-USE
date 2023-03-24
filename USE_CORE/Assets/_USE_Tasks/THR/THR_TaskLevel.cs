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
using System.IO;

public class THR_TaskLevel : ControlLevel_Task_Template
{
    public string CurrentBlockString;
    public StringBuilder PreviousBlocksString;

    public int BlockStringsAdded = 0;

    THR_BlockDef currentBlock => GetCurrentBlockDef<THR_BlockDef>();
    THR_TrialLevel trialLevel;

    public int TrialsCompleted_Task = 0;
    public int TrialsCorrect_Task = 0;
    public int BlueSquareTouches_Task = 0;
    public int WhiteSquareTouches_Task = 0;
    public int BackdropTouches_Task = 0;
    public int ItiTouches_Task = 0;
    public int TouchRewards_Task = 0;
    public int ReleaseRewards_Task = 0;
    public int ReleasedEarly_Task = 0;
    public int ReleasedLate_Task = 0;
    public int TouchesMovedOutside_Task = 0;


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
        trialLevel = (THR_TrialLevel)TrialLevel;

        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ContextExternalFilePath"))
            trialLevel.MaterialFilePath = (String)SessionSettings.Get(TaskName + "_TaskSettings", "ContextExternalFilePath");

        CurrentBlockString = "";
        PreviousBlocksString = new StringBuilder();

        SetupBlockData();

        RunBlock.AddInitializationMethod(() =>
        {
            trialLevel.TrialsCompleted_Block = 0;
            trialLevel.TrialsCorrect_Block = 0;
            trialLevel.BackdropTouches_Block = 0;
            trialLevel.BlueSquareTouches_Block = 0;
            trialLevel.WhiteSquareTouches_Block = 0;
            trialLevel.NumItiTouches_Block = 0;
            trialLevel.NumTouchRewards_Block = 0;
            trialLevel.NumReleaseRewards_Block = 0;
            trialLevel.NumReleasedEarly_Block = 0;
            trialLevel.NumReleasedLate_Block = 0;
            trialLevel.NumTouchesMovedOutside_Block = 0;
            trialLevel.PerfThresholdMet = false;

            CalculateBlockSummaryString();
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
            BackdropTouches_Task += trialLevel.BackdropTouches_Block;
            ItiTouches_Task += trialLevel.NumItiTouches_Block;
            TouchRewards_Task += trialLevel.NumTouchRewards_Block;
            ReleaseRewards_Task += trialLevel.NumReleaseRewards_Block;
            ReleasedEarly_Task += trialLevel.NumReleasedEarly_Block;
            ReleasedLate_Task += trialLevel.NumReleasedLate_Block;
            TouchesMovedOutside_Task += trialLevel.NumTouchesMovedOutside_Block;
        });
    }

    public override void SetTaskSummaryString()
    {
        if (trialLevel.TrialCount_InTask != 0)
        {
            CurrentTaskSummaryString.Clear();
            CurrentTaskSummaryString.Append($"\n<b>{ConfigName}</b>" +
                                            $"\n<b># Trials:</b> {trialLevel.TrialCount_InTask} | " +
                                            $"\t<b># Blocks:</b> {BlockCount} | " +
                                            $"\t<b># Rewards:</b> {TouchRewards_Task + ReleaseRewards_Task}");
        }
        else
            CurrentTaskSummaryString.Append($"\n<b>{ConfigName}</b>");
    }

    public override OrderedDictionary GetSummaryData()
    {
        OrderedDictionary data = new OrderedDictionary();

        data["Trials Completed"] = TrialsCompleted_Task;
        data["Trials Correct"] = TrialsCorrect_Task;
        data["Blue Square Touches"] = BlueSquareTouches_Task;
        data["White Square Touches"] = WhiteSquareTouches_Task;
        data["Non Square Touches"] = BackdropTouches_Task;
        data["ITI Touches"] = ItiTouches_Task;
        data["Touch Rewards"] = TouchRewards_Task;
        data["Release Rewards"] = ReleaseRewards_Task;
        data["Released Early"] = ReleasedEarly_Task;
        data["Released Late"] = ReleasedLate_Task;
        data["Touches Moved Outside"] = TouchesMovedOutside_Task;
        return data;
    }

    public void CalculateBlockSummaryString()
    {
        ClearStrings();

        CurrentBlockString = ("<b>Block " + "(" + currentBlock.BlockName + "):" + "</b>" +
                        "\nTrialsCorrect: " + trialLevel.TrialsCorrect_Block + " (out of " + trialLevel.TrialsCompleted_Block + ")" +
                        "\nReleasedEarly: " + trialLevel.NumReleasedEarly_Block +
                        "\nReleasedLate: " + trialLevel.NumReleasedLate_Block +
                        "\nMovedOutsideSquare: " + trialLevel.NumTouchesMovedOutside_Block +
                        "\nWhiteSquareTouches: " + trialLevel.WhiteSquareTouches_Block +
                        "\nBlueSquareTouches: " + trialLevel.BlueSquareTouches_Block +
                        "\nBackdropTouches: " + trialLevel.BackdropTouches_Block +
                        "\nRewards: " + (trialLevel.NumTouchRewards_Block + trialLevel.NumReleaseRewards_Block) +
                        "\n");

        BlockSummaryString.AppendLine(CurrentBlockString).ToString();
        if (PreviousBlocksString.Length > 0)
            BlockSummaryString.AppendLine(PreviousBlocksString.ToString());
    }


    void SetupBlockData()
    {
        BlockData.AddDatum("NumTrialsCompleted", () => trialLevel.TrialsCompleted_Block);
        BlockData.AddDatum("NumTrialsCorrect", () => trialLevel.TrialsCorrect_Block);
        BlockData.AddDatum("WhiteSquareTouches_Block", () => trialLevel.WhiteSquareTouches_Block);
        BlockData.AddDatum("BlueSquareTouches_Block", () => trialLevel.BlueSquareTouches_Block);
        BlockData.AddDatum("BackdropTouches_Block", () => trialLevel.BackdropTouches_Block);
        BlockData.AddDatum("ItiTouches_Block", () => trialLevel.NumItiTouches_Block);
        BlockData.AddDatum("NumTouchRewards", () => trialLevel.NumTouchRewards_Block);
        BlockData.AddDatum("NumReleaseRewards", () => trialLevel.NumReleaseRewards_Block);
        BlockData.AddDatum("DifficultyLevel", () => currentBlock.BlockName);
        BlockData.AddDatum("NumReleasedEarly", () => trialLevel.NumReleasedEarly_Block);
        BlockData.AddDatum("NumReleasedLate", () => trialLevel.NumReleasedLate_Block);
        BlockData.AddDatum("NumTouchesMovedOutside", () => trialLevel.NumTouchesMovedOutside_Block);
    }

    void ClearStrings()
    {
        CurrentBlockString = "";
        BlockSummaryString.Clear();
    }

}