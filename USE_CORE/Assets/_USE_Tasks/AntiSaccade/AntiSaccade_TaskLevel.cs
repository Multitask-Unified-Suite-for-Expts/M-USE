using USE_ExperimentTemplate_Task;
using AntiSaccade_Namespace;
using UnityEngine;
using System.Text;
using System.Collections.Specialized;

public class AntiSaccade_TaskLevel : ControlLevel_Task_Template
{
    AntiSaccade_BlockDef CurrentBlock => GetCurrentBlockDef<AntiSaccade_BlockDef>();
    AntiSaccade_TrialLevel trialLevel;

    [HideInInspector] public string CurrentBlockString;
    [HideInInspector] public StringBuilder PreviousBlocksString;
    [HideInInspector] public int BlockStringsAdded = 0;

    //Task Values used for SummaryData file
    [HideInInspector] public int TrialsCompleted_Task = 0;
    [HideInInspector] public int TrialsCorrect_Task = 0;
    [HideInInspector] public int TokenBarsCompleted_Task = 0;

    public override void SpecifyTypes()
    {
        TaskLevelType = typeof(AntiSaccade_TaskLevel);
        TrialLevelType = typeof(AntiSaccade_TrialLevel);
        TaskDefType = typeof(AntiSaccade_TaskDef);
        BlockDefType = typeof(AntiSaccade_BlockDef);
        TrialDefType = typeof(AntiSaccade_TrialDef);
        StimDefType = typeof(AntiSaccade_StimDef);
    }

    public override void DefineControlLevel()
    {
        trialLevel = (AntiSaccade_TrialLevel)TrialLevel;

        CurrentBlockString = "";
        PreviousBlocksString = new StringBuilder();

        DefineBlockData();

        RunBlock.AddSpecificInitializationMethod(() =>
        {
            trialLevel.ResetBlockVariables();
            SetSkyBox(CurrentBlock.ContextName);
            trialLevel.TokenFBController.SetTotalTokensNum(CurrentBlock.TokenBarCapacity);
            trialLevel.TokenFBController.SetTokenBarValue(CurrentBlock.NumInitialTokens);
        });

        BlockFeedback.AddSpecificInitializationMethod(() => HandleBlockStrings());

    }

    private void HandleBlockStrings()
    {
        if (!SessionValues.WebBuild)
        {
            if (BlockStringsAdded > 0)
                CurrentBlockString += "\n";
            PreviousBlocksString.Insert(0, CurrentBlockString);
            BlockStringsAdded++;
        }
    }

    public override OrderedDictionary GetBlockResultsData()
    {
        OrderedDictionary data = new OrderedDictionary
        {
            ["Trials Correct"] = trialLevel.TrialsCorrect_Block,
            ["Trials Completed"] = trialLevel.TrialCompletions_Block,
            ["TokenBar Completions"] = trialLevel.TokenBarCompletions_Block,
        };
        return data;
    }

    private void DefineBlockData()
    {
        BlockData.AddDatum("SaccadeType", () => trialLevel.SaccadeType);
        BlockData.AddDatum("TrialsCompleted", () => trialLevel.TrialCompletions_Block);
        BlockData.AddDatum("TrialsCorrect", () => trialLevel.TrialsCorrect_Block);
        BlockData.AddDatum("TokenBarCompletions", () => trialLevel.TokenBarCompletions_Block);

    }

    public void CalculateBlockSummaryString()
    {
        ClearStrings();

        CurrentBlockString = "\nSaccadeType: " + trialLevel.SaccadeType +
                        "\nTrials Completed: " + trialLevel.TrialCompletions_Block +
                        "\nTrials Correct: " + trialLevel.TrialsCorrect_Block +
                        "\nTokenBar Completions: " + trialLevel.TokenBarCompletions_Block +
                        "\nReward Pulses: " + NumRewardPulses_InBlock;

        CurrentBlockSummaryString.AppendLine(CurrentBlockString).ToString();
    }

    public void ClearStrings()
    {
        CurrentBlockString = "";
        CurrentBlockSummaryString.Clear();
    }


}