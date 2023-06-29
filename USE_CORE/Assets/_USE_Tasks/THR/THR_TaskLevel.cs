using System;
using System.Text;
using THR_Namespace;
using USE_Settings;
using USE_ExperimentTemplate_Task;
using System.Collections.Specialized;
using UnityEngine;

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

        trialLevel.MaterialFilePath = ContextExternalFilePath;

        SetSettings();

        CurrentBlockString = "";
        PreviousBlocksString = new StringBuilder();

        SetupBlockData();

        RunBlock.AddInitializationMethod(() =>
        {
            trialLevel.ResetTrialVariables();

            CalculateBlockSummaryString();
        });

        BlockFeedback.AddInitializationMethod(() =>
        {
            if(!SessionValues.WebBuild)
            {
                if (BlockStringsAdded > 0)
                    CurrentBlockString += "\n";
                BlockStringsAdded++;
                PreviousBlocksString.Insert(0, CurrentBlockString);
            }

            AddBlockValuesToTaskValues();
        });
    }

    public void SetSettings()
    {
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ContextExternalFilePath"))
            trialLevel.MaterialFilePath = (String)SessionSettings.Get(TaskName + "_TaskSettings", "ContextExternalFilePath");

        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "StartWithBlueSquare"))
            trialLevel.StartWithBlueSquare = (bool)SessionSettings.Get(TaskName + "_TaskSettings", "StartWithBlueSquare");

        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonPosition"))
            trialLevel.ButtonPosition = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonPosition");
        else
            trialLevel.ButtonPosition = new Vector3(0, 0, 0);

        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonScale"))
            trialLevel.ButtonScale = (float)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonScale");
        else
            trialLevel.ButtonScale = 120f;

        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "TouchFeedbackDuration"))
            trialLevel.TouchFeedbackDuration = (float)SessionSettings.Get(TaskName + "_TaskSettings", "TouchFeedbackDuration");
        else
            trialLevel.TouchFeedbackDuration = .3f;
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

    public void AddBlockValuesToTaskValues()
    {
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
    }

    public override OrderedDictionary GetSummaryData()
    {
        OrderedDictionary data = new OrderedDictionary
        {
            ["Trials Completed"] = TrialsCompleted_Task,
            ["Trials Correct"] = TrialsCorrect_Task,
            ["Blue Square Touches"] = BlueSquareTouches_Task,
            ["White Square Touches"] = WhiteSquareTouches_Task,
            ["Non Square Touches"] = BackdropTouches_Task,
            ["ITI Touches"] = ItiTouches_Task,
            ["Touch Rewards"] = TouchRewards_Task,
            ["Release Rewards"] = ReleaseRewards_Task,
            ["Released Early"] = ReleasedEarly_Task,
            ["Released Late"] = ReleasedLate_Task,
            ["Touches Moved Outside"] = TouchesMovedOutside_Task
        };
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