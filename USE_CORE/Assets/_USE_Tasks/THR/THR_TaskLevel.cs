using System.Text;
using THR_Namespace;
using USE_ExperimentTemplate_Task;
using System.Collections.Specialized;


public class THR_TaskLevel : ControlLevel_Task_Template
{
    public string CurrentBlockString;
    public StringBuilder PreviousBlocksString;

    public int BlockStringsAdded = 0;

    THR_BlockDef CurrentBlock => GetCurrentBlockDef<THR_BlockDef>();
    THR_TrialLevel trialLevel;

    public int TrialsCompleted_Task = 0;
    public int TrialsCorrect_Task = 0;
    public int SelectObjectTouches_Task = 0;
    public int AvoidObjectTouches_Task = 0;
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

        CurrentBlockString = "";
        PreviousBlocksString = new StringBuilder();

        DefineBlockData();

        RunBlock.AddSpecificInitializationMethod(() =>
        {
            trialLevel.ResetBlockVariables();
            CalculateBlockSummaryString();
        });

        BlockFeedback.AddSpecificInitializationMethod(() =>
        {
            if(!SessionValues.WebBuild)
            {
                if (BlockStringsAdded > 0)
                    CurrentBlockString += "\n";
                BlockStringsAdded++;
                PreviousBlocksString.Insert(0, CurrentBlockString);
            }
        });
    }

/*    public override void SetTaskSummaryString() // MOVED TO TASK TEMPLATE
    {
        if (trialLevel.TrialCount_InTask != 0)
        {
            CurrentTaskSummaryString.Clear();
            CurrentTaskSummaryString.Append($"\n<b>{ConfigFolderName}</b>" +
                                            $"\n<b># Trials:</b> {trialLevel.TrialCount_InTask} | " +
                                            $"\t<b># Blocks:</b> {BlockCount} | " +
                                            $"\t<b># Rewards:</b> {TouchRewards_Task + ReleaseRewards_Task}");
        }
        else
            CurrentTaskSummaryString.Append($"\n<b>{ConfigFolderName}</b>");
    }
*/

    public override OrderedDictionary GetBlockResultsData()
    {
        OrderedDictionary data = new OrderedDictionary
        {
            ["Trials Completed"] = trialLevel.TrialCount_InBlock + 1,
            ["Trials Correct"] = trialLevel.TrialsCorrect_Block,
            ["Touches Released Early"] = trialLevel.NumReleasedEarly_Block,
            ["Touches Released Late"] = trialLevel.NumReleasedLate_Block,
            ["Touches Moved Outside"] = trialLevel.NumTouchesMovedOutside_Block
        };
        return data;
    }

    public override OrderedDictionary GetTaskSummaryData()
    {
        OrderedDictionary data = base.GetTaskSummaryData();
        data["Trials Completed"] = TrialsCompleted_Task;
        data["Trials Correct"] = TrialsCorrect_Task;
        data["Touch Rewards"] = TouchRewards_Task;
        data["Release Rewards"] = ReleaseRewards_Task;
        data["Select Object Touches"] = SelectObjectTouches_Task;
        data["Avoid Object Touches"] = AvoidObjectTouches_Task;
        data["Backdrop Touches"] = BackdropTouches_Task;
        data["ITI Touches"] = ItiTouches_Task;
        data["Released Early"] = ReleasedEarly_Task;
        data["Released Late"] = ReleasedLate_Task;
        data["Touches Moved Outside"] = TouchesMovedOutside_Task;
       
        return data;
    }

    public void CalculateBlockSummaryString()
    {
        ClearStrings();

        CurrentBlockString = ("<b>Block Name: " + "(" + CurrentBlock.BlockName + "):" + "</b>" +
                        "\nTrialsCorrect: " + trialLevel.TrialsCorrect_Block + " (out of " + trialLevel.TrialsCompleted_Block + ")" +
                        "\nReleasedEarly: " + trialLevel.NumReleasedEarly_Block +
                        "\nReleasedLate: " + trialLevel.NumReleasedLate_Block +
                        "\nMovedOutsideObject: " + trialLevel.NumTouchesMovedOutside_Block +
                        "\nAvoidObjectTouches: " + trialLevel.AvoidObjectTouches_Block +
                        "\nSelectObjectTouches: " + trialLevel.SelectObjectTouches_Block +
                        "\nBackdropTouches: " + trialLevel.BackdropTouches_Block +
                        "\nRewards: " + (trialLevel.NumTouchRewards_Block + trialLevel.NumReleaseRewards_Block) +
                        "\n");

        CurrentBlockSummaryString.AppendLine(CurrentBlockString).ToString();
        if (PreviousBlocksString.Length > 0)
            CurrentBlockSummaryString.AppendLine(PreviousBlocksString.ToString());
    }

    private void DefineBlockData()
    {
        BlockData.AddDatum("NumTrialsCompleted", () => trialLevel.TrialsCompleted_Block);
        BlockData.AddDatum("NumTrialsCorrect", () => trialLevel.TrialsCorrect_Block);
        BlockData.AddDatum("AvoidObjectTouches_Block", () => trialLevel.AvoidObjectTouches_Block);
        BlockData.AddDatum("SelectObjectTouches_Block", () => trialLevel.SelectObjectTouches_Block);
        BlockData.AddDatum("BackdropTouches_Block", () => trialLevel.BackdropTouches_Block);
        BlockData.AddDatum("ItiTouches_Block", () => trialLevel.NumItiTouches_Block);
        BlockData.AddDatum("NumTouchRewards", () => trialLevel.NumTouchRewards_Block);
        BlockData.AddDatum("NumReleaseRewards", () => trialLevel.NumReleaseRewards_Block);
        BlockData.AddDatum("DifficultyLevel", () => CurrentBlock.BlockName);
        BlockData.AddDatum("NumReleasedEarly", () => trialLevel.NumReleasedEarly_Block);
        BlockData.AddDatum("NumReleasedLate", () => trialLevel.NumReleasedLate_Block);
        BlockData.AddDatum("NumTouchesMovedOutside", () => trialLevel.NumTouchesMovedOutside_Block);
    }

    void ClearStrings()
    {
        CurrentBlockString = "";
        CurrentBlockSummaryString.Clear();
    }

}