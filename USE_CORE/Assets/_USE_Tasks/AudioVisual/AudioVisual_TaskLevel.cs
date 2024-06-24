using USE_ExperimentTemplate_Task;
using AudioVisual_Namespace;
using UnityEngine;
using System.Collections.Specialized;


public class AudioVisual_TaskLevel : ControlLevel_Task_Template
{
    AudioVisual_BlockDef CurrentBlock => GetCurrentBlockDef<AudioVisual_BlockDef>();
    AudioVisual_TrialLevel trialLevel;

    [HideInInspector] public int TrialsCompleted_Task;
    [HideInInspector] public int TrialsCorrect_Task;
    [HideInInspector] public int TokenBarCompletions_Task;

    [HideInInspector] public string CurrentBlockString;
    public int blocksAdded;



    public override void DefineControlLevel()
    {
        trialLevel = (AudioVisual_TrialLevel) TrialLevel;

        CurrentBlockString = "";
        DefineBlockData();
        blocksAdded = 0;

        Session.HumanStartPanel.AddTaskDisplayName(TaskName, "Audio Visual");
        Session.HumanStartPanel.AddTaskInstructions(TaskName, "Select the correct object based on the sound frequency you hear!");

        RunBlock.AddSpecificInitializationMethod(() =>
        {
            SetSkyBox(CurrentBlock.ContextName.Trim());
            CalculateBlockSummaryString();
            trialLevel.ResetBlockVariables();
        });

        BlockFeedback.AddSpecificInitializationMethod(() =>
        {
            if (!Session.WebBuild && trialLevel.AbortCode == 0)
            {
                CurrentBlockString += "\n" + "\n";
                CurrentBlockString = CurrentBlockString.Replace("Current Block", $"Block {blocksAdded + 1}");
                blocksAdded++;
            }
        });

    }


    public override void SetTaskSummaryString()
    {
        CurrentTaskSummaryString.Clear();
        base.SetTaskSummaryString();
        CurrentTaskSummaryString.Append($"\t<b># TbFilled:</b> {TokenBarCompletions_Task}");
    }


    public override OrderedDictionary GetTaskSummaryData()
    {
        OrderedDictionary data = base.GetTaskSummaryData();
        data["Trials Correct"] = TrialsCorrect_Task;
        data["TokenBar Completions"] = TokenBarCompletions_Task;

        return data;
    }

    public override OrderedDictionary GetTaskResultsData()
    {
        OrderedDictionary data = base.GetTaskResultsData();
        data["Trials Completed"] = TrialsCompleted_Task;
        data["Trials Correct"] = TrialsCorrect_Task;
        data["TokenBar Completions"] = TokenBarCompletions_Task;

        return data;
    }

    private void DefineBlockData()
    {
        BlockData.AddDatum("BlockName", () => CurrentBlock.BlockName);
        BlockData.AddDatum("BlockCount", () => CurrentBlock.BlockCount);
        BlockData.AddDatum("ContextName", () => CurrentBlock.ContextName);

        BlockData.AddDatum("NumCorrect", () => trialLevel.NumCorrect_Block);

        BlockData.AddDatum("TokenBarCompletions", () => trialLevel.NumTbCompletions_Block);
        BlockData.AddDatum("TimeToChoice", () => trialLevel.AvgTimeToChoice_Block);
        BlockData.AddDatum("TimeToCompletion", () => trialLevel.TimeToCompletion_Block);
    }

    public void CalculateBlockSummaryString()
    {
        CurrentBlockString = "";
        CurrentBlockSummaryString.Clear();

        CurrentBlockString =
                "\nCorrect: " + trialLevel.NumCorrect_Block +
                "\nTbCompletions: " + trialLevel.NumTbCompletions_Block +
                "\nAvgTimeToChoice: " + trialLevel.AvgTimeToChoice_Block.ToString("0.00") + "s" +
                "\nTimeToCompletion: " + trialLevel.TimeToCompletion_Block.ToString("0.00") + "s" +
                "\nReward Pulses: " + NumRewardPulses_InBlock;

        if (blocksAdded > 1)
            CurrentBlockString += "\n";

        ////Add CurrentBlockString if block wasn't aborted:
        if (trialLevel.AbortCode == 0)
            CurrentBlockSummaryString.AppendLine(CurrentBlockString.ToString());
    }


}