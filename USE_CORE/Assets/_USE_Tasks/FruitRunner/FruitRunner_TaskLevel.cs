using USE_ExperimentTemplate_Task;
using FruitRunner_Namespace;
using UnityEngine;


public class FruitRunner_TaskLevel : ControlLevel_Task_Template
{
    FruitRunner_BlockDef CurrentBlock => GetCurrentBlockDef<FruitRunner_BlockDef>();
    FruitRunner_TrialLevel trialLevel;

    [HideInInspector] public string CurrentBlockString;
    [HideInInspector] public int BlockStringsAdded = 0;


    public override void DefineControlLevel()
    {
        trialLevel = (FruitRunner_TrialLevel)TrialLevel;
        CurrentBlockString = "";
        DefineBlockData();

        Session.HumanStartPanel.AddTaskDisplayName(TaskName, "Fruit Runner");
        Session.HumanStartPanel.AddTaskInstructions(TaskName, "Collect the target objects to earn your reward!");

        RunBlock.AddSpecificInitializationMethod(() =>
        {
            trialLevel.ResetBlockVariables();
            SetSkyBox(CurrentBlock.ContextName);
            CalculateBlockSummaryString();
            TaskCam.fieldOfView = 50;

        });

        BlockFeedback.AddSpecificInitializationMethod(() => HandleBlockStrings());

    }



    public void CalculateBlockSummaryString()
    {
        ClearStrings();

        CurrentBlockString = "\nReward Pulses: " + NumRewardPulses_InBlock;

        CurrentBlockSummaryString.AppendLine(CurrentBlockString).ToString();
    }

    private void DefineBlockData()
    {
        //BlockData.AddDatum("BlockName", () => CurrentBlock.BlockName);
        //BlockData.AddDatum("ContextName", () => CurrentBlock.ContextName);
        //Add rest of block data
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