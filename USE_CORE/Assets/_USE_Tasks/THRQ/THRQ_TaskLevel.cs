using USE_Settings;
using USE_StimulusManagement;
using USE_ExperimentTemplate_Task;
using THRQ_Namespace;
using System.Collections.Specialized;
using UnityEngine;


public class THRQ_TaskLevel : ControlLevel_Task_Template
{
    THRQ_BlockDef CurrentBlock => GetCurrentBlockDef<THRQ_BlockDef>();
    THRQ_TrialLevel trialLevel;

    [HideInInspector] public int TrialsCompleted_Task = 0;
    [HideInInspector] public int TrialsCorrect_Task = 0;
    [HideInInspector] public int SelectObjectTouches_Task = 0;
    [HideInInspector] public int BackdropTouches_Task = 0;
    [HideInInspector] public int ItiTouches_Task = 0;
    [HideInInspector] public int TouchRewards_Task = 0;
    [HideInInspector] public int ReleaseRewards_Task = 0;
    [HideInInspector] public int ReleasedEarly_Task = 0;
    [HideInInspector] public int ReleasedLate_Task = 0;
    [HideInInspector] public int TouchesMovedOutside_Task = 0;

    public override void DefineControlLevel()
    {
        trialLevel = (THRQ_TrialLevel)TrialLevel;
        DefineBlockData();

        RunBlock.AddSpecificInitializationMethod(() =>
        {
            SetSkyBox(CurrentBlock.ContextName);
            MinTrials_InBlock = CurrentBlock.MinTrials;
            MaxTrials_InBlock = CurrentBlock.MaxTrials;
            //trialLevel.ResetBlockVariables();
            CalculateBlockSummaryString();
        });

    }



    //public override OrderedDictionary GetBlockResultsData()
    //{
    //    OrderedDictionary data = new OrderedDictionary
    //    {
    //        ["Trials Completed"] = trialLevel.TrialCount_InBlock + 1,
    //        ["Trials Correct"] = trialLevel.TrialsCorrect_Block,
    //        ["Touches Released Early"] = trialLevel.NumReleasedEarly_Block,
    //        ["Touches Released Late"] = trialLevel.NumReleasedLate_Block,
    //        ["Touches Moved Outside"] = trialLevel.NumTouchesMovedOutside_Block
    //    };
    //    return data;
    //}

    //public override OrderedDictionary GetTaskSummaryData()
    //{
    //    OrderedDictionary data = base.GetTaskSummaryData();
    //    data["Trials Completed"] = TrialsCompleted_Task;
    //    data["Trials Correct"] = TrialsCorrect_Task;
    //    data["Touch Rewards"] = TouchRewards_Task;
    //    data["Release Rewards"] = ReleaseRewards_Task;
    //    data["Select Object Touches"] = SelectObjectTouches_Task;
    //    data["Avoid Object Touches"] = AvoidObjectTouches_Task;
    //    data["Backdrop Touches"] = BackdropTouches_Task;
    //    data["ITI Touches"] = ItiTouches_Task;
    //    data["Released Early"] = ReleasedEarly_Task;
    //    data["Released Late"] = ReleasedLate_Task;
    //    data["Touches Moved Outside"] = TouchesMovedOutside_Task;

    //    return data;
    //}

    public void CalculateBlockSummaryString()
    {
        //CurrentBlockSummaryString.Clear();

        //CurrentBlockSummaryString.AppendLine("<b>\nMin Trials in Block: </b>" + MinTrials_InBlock +
        //                     "<b>\nMax Trials in Block: </b>" + MaxTrials_InBlock +
        //                        "<b>\n\nBlock Name: " + CurrentBlock.BlockName + "</b>" +
        //                "\nTrials Correct: " + trialLevel.TrialsCorrect_Block +
        //                "\nReleased Early: " + trialLevel.NumReleasedEarly_Block +
        //                "\nReleased Late: " + trialLevel.NumReleasedLate_Block +
        //                "\nMoved Outside Object: " + trialLevel.NumTouchesMovedOutside_Block +
        //                "\n\nAvoid Object Touches: " + trialLevel.AvoidObjectTouches_Block +
        //                "\nSelect Object Touches: " + trialLevel.SelectObjectTouches_Block +
        //                "\nBackdrop Touches: " + trialLevel.BackdropTouches_Block +
        //                "\nNum Pulses: " + (trialLevel.NumTouchRewards_Block + trialLevel.NumReleaseRewards_Block)
        //                );
    }

    public override void SetTaskSummaryString()
    {
        base.SetTaskSummaryString();

        //if (trialLevel.TrialCount_InTask != 0)
        //{
        //    CurrentTaskSummaryString.Append($"\nAccuracy: {(Math.Round(decimal.Divide(TrialsCorrect_Task, (trialLevel.TrialCount_InTask)), 2)) * 100}%" +
        //                                            $"\n# Released Early: {ReleasedEarly_Task}" +
        //                                            $"\n# Released Late: {ReleasedLate_Task}" +
        //                                            $"\n# Backdrop Touches: {BackdropTouches_Task}");
        //}

    }

    private void DefineBlockData()
    {
        //BlockData.AddDatum("NumTrialsCompleted", () => trialLevel.TrialsCompleted_Block);
        //BlockData.AddDatum("NumTrialsCorrect", () => trialLevel.TrialsCorrect_Block);
        //BlockData.AddDatum("AvoidObjectTouches_Block", () => trialLevel.AvoidObjectTouches_Block);
        //BlockData.AddDatum("SelectObjectTouches_Block", () => trialLevel.SelectObjectTouches_Block);
        //BlockData.AddDatum("BackdropTouches_Block", () => trialLevel.BackdropTouches_Block);
        //BlockData.AddDatum("ItiTouches_Block", () => trialLevel.NumItiTouches_Block);
        //BlockData.AddDatum("NumTouchRewards", () => trialLevel.NumTouchRewards_Block);
        //BlockData.AddDatum("NumReleaseRewards", () => trialLevel.NumReleaseRewards_Block);
        //BlockData.AddDatum("DifficultyLevel", () => CurrentBlock.BlockName);
        //BlockData.AddDatum("NumReleasedEarly", () => trialLevel.NumReleasedEarly_Block);
        //BlockData.AddDatum("NumReleasedLate", () => trialLevel.NumReleasedLate_Block);
        //BlockData.AddDatum("NumTouchesMovedOutside", () => trialLevel.NumTouchesMovedOutside_Block);
    }


}