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
        });

        BlockFeedback.AddSpecificInitializationMethod(() => HandleBlockStrings());
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

    public override OrderedDictionary GetBlockResultsData()
    {
        OrderedDictionary data = new OrderedDictionary
        {
            ["Trials Completed"] = trialLevel.TrialCompletions_Block,
        };
        return data;
    }

    private void DefineBlockData()
    {
        BlockData.AddDatum("BlockName", () => CurrentBlock.BlockName);
        BlockData.AddDatum("TrialsCompleted", () => trialLevel.TrialCompletions_Block);
        BlockData.AddDatum("ContextName", () => CurrentBlock.ContextName);
        BlockData.AddDatum("CalculatedThreshold", () => trialLevel.calculatedThreshold);
        BlockData.AddDatum("DiffLevelsSummary", () => trialLevel.DiffLevelsSummary);
        //MORE TO ADD:
    }

    public void CalculateBlockSummaryString()
    {
        ClearStrings();

        CurrentBlockString = "\nTrials Completed: " + trialLevel.TrialCompletions_Block +
                        "\nReward Pulses: " + NumRewardPulses_InBlock;

        CurrentBlockSummaryString.AppendLine(CurrentBlockString).ToString();
    }

    public void ClearStrings()
    {
        CurrentBlockString = "";
        CurrentBlockSummaryString.Clear();
    }


}