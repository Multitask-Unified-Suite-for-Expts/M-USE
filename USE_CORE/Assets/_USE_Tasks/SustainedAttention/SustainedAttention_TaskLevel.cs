using USE_Settings;
using USE_StimulusManagement;
using USE_ExperimentTemplate_Task;
using SustainedAttention_Namespace;
using UnityEngine;
using System.Collections.Specialized;

public class SustainedAttention_TaskLevel : ControlLevel_Task_Template
{
    SustainedAttention_BlockDef CurrentBlock => GetCurrentBlockDef<SustainedAttention_BlockDef>();
    SustainedAttention_TrialLevel trialLevel;

    [HideInInspector] public string CurrentBlockString;
    [HideInInspector] public int BlockStringsAdded = 0;

    //DATA
    [HideInInspector] public int TrialsCompleted_Task = 0;

    [HideInInspector] public int SuccessfulTargetSelections_Task = 0;
    [HideInInspector] public int UnsuccessfulTargetSelections_Task = 0;
    [HideInInspector] public int DistractorSelections_Task = 0;
    [HideInInspector] public int IntervalsWithoutTargetSelection_Task = 0;


    public override void DefineControlLevel()
    {
        trialLevel = (SustainedAttention_TrialLevel)TrialLevel;
        CurrentBlockString = "";
        DefineBlockData();
        Session.HumanStartPanel.AddTaskDisplayName(TaskName, "Sustained Attention");
        Session.HumanStartPanel.AddTaskInstructions(TaskName, "Select the target stimuli when it closes its mouth!");

        RunBlock.AddSpecificInitializationMethod(() =>
        {
            CurrentBlock.ContextName = CurrentBlock.ContextName.Trim();
            SetSkyBox(CurrentBlock.ContextName);
            trialLevel.ResetBlockVariables();
        });

        BlockFeedback.AddSpecificInitializationMethod(() => HandleBlockStrings());
    }

    public override void SetTaskSummaryString()
    {
        CurrentTaskSummaryString.Clear();
        base.SetTaskSummaryString();
        CurrentTaskSummaryString.Append($"\t<b># Successful Target Selections:</b> {SuccessfulTargetSelections_Task}");
    }

    public override OrderedDictionary GetTaskSummaryData()
    {
        OrderedDictionary data = base.GetTaskSummaryData();

        data["Trials Completed"] = TrialsCompleted_Task;
        data["Successful Target Selections"] = SuccessfulTargetSelections_Task;
        data["Unsuccessful Target Selections"] = UnsuccessfulTargetSelections_Task;
        data["Distractor Selections"] = DistractorSelections_Task;
        data["Intervals Without Selections"] = IntervalsWithoutTargetSelection_Task;
        return data;
    }

    public override OrderedDictionary GetBlockResultsData()
    {
        OrderedDictionary data = new OrderedDictionary
        {
            ["Trials Completed"] = trialLevel.TrialCompletions_Block,
            ["Successful Target Selections"] = trialLevel.SuccessfulTargetSelections_Block,
            ["Unsuccessful Target Selections"] = trialLevel.UnsuccessfulTargetSelections_Block,
            ["Distractor Selections"] = trialLevel.DistractorSelections_Block,
            ["Intervals Without A Selection"] = trialLevel.IntervalsWithoutTargetSelection_Block,
        };
        return data;
    }

    private void DefineBlockData()
    {
        BlockData.AddDatum("BlockName", () => CurrentBlock.BlockName);
        BlockData.AddDatum("ContextName", () => CurrentBlock.ContextName);

        BlockData.AddDatum("TrialsCompleted", () => trialLevel.TrialCompletions_Block);
        BlockData.AddDatum("SuccessfulTargetSelections", () => trialLevel.SuccessfulTargetSelections_Block);
        BlockData.AddDatum("UnsuccessfulTargetSelections", () => trialLevel.UnsuccessfulTargetSelections_Block);
        BlockData.AddDatum("DistractorSelections", () => trialLevel.DistractorSelections_Block);
        BlockData.AddDatum("IntervalsWithoutASelection", () => trialLevel.IntervalsWithoutTargetSelection_Block);

        BlockData.AddDatum("CalculatedThreshold", () => trialLevel.calculatedThreshold);
        BlockData.AddDatum("DiffLevelsSummary", () => trialLevel.DiffLevelsSummary);
    }

    public void CalculateBlockSummaryString()
    {
        ClearStrings();

        CurrentBlockString = "\nTrials Completed: " + trialLevel.TrialCompletions_Block +
                             "\nSuccessful Target Selections: " + trialLevel.SuccessfulTargetSelections_Block +
                             "\nUnsuccessful Target Selections: " + trialLevel.UnsuccessfulTargetSelections_Block +
                             "\nDistractor Selections: " + trialLevel.DistractorSelections_Block +
                             "\nIntervals Without A Selection: " + trialLevel.DistractorSelections_Block +
                             "\nReward Pulses: " + NumRewardPulses_InBlock;

        CurrentBlockSummaryString.AppendLine(CurrentBlockString).ToString();
    }

    private void HandleBlockStrings()
    {
        if (!Session.WebBuild)
        {
            if (BlockStringsAdded > 0)
                CurrentBlockString += "\n";
            BlockStringsAdded++;
        }
    }

    public void ClearStrings()
    {
        CurrentBlockString = "";
        CurrentBlockSummaryString.Clear();
    }


}