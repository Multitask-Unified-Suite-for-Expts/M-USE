using USE_ExperimentTemplate_Task;
using FruitRunner_Namespace;
using UnityEngine;
using System.Collections.Specialized;

public class FruitRunner_TaskLevel : ControlLevel_Task_Template
{
    FruitRunner_BlockDef CurrentBlock => GetCurrentBlockDef<FruitRunner_BlockDef>();
    FruitRunner_TrialLevel trialLevel;

    [HideInInspector] public int TargetsHit_Task;
    [HideInInspector] public int TargetsMissed_Task;
    [HideInInspector] public int DistractorsHit_Task;
    [HideInInspector] public int DistractorsAvoided_Task;
    [HideInInspector] public int BlockadesHit_Task;
    [HideInInspector] public int BlockadesAvoided_Task;


    private Quaternion CameraOriginalRotation;




    public override void DefineControlLevel()
    {
        trialLevel = (FruitRunner_TrialLevel)TrialLevel;
        CurrentBlockString = "";
        DefineBlockData();

        CameraOriginalRotation = Camera.main.transform.rotation;

        Session.HumanStartPanel.AddTaskDisplayName(TaskName, "Fruit Runner");
        Session.HumanStartPanel.AddTaskInstructions(TaskName, "Collect the target objects to earn your reward!");

        RunBlock.AddSpecificInitializationMethod(() =>
        {
            Camera.main.transform.rotation = CameraOriginalRotation;

            SetTrialFogStrength();
            trialLevel.ResetBlockVariables();
            SetSkyBox(CurrentBlock.ContextName);
            SetBlockSummaryString();
        });
        BlockFeedback.AddSpecificInitializationMethod(() =>
        {
            Camera.main.transform.rotation = new Quaternion(0f, 0f, 0f, 0f);
        });
    }

    void SetTrialFogStrength()
    {
        RenderSettings.fog = true;
        RenderSettings.fogDensity = CurrentBlock.FogStrength;
        
    }

    public override OrderedDictionary GetTaskSummaryData()
    {
        OrderedDictionary data = base.GetTaskSummaryData();

        //data["TokenBar Completions"] = TokenBarCompletions_Task;
        data["Targets Hit"] = TargetsHit_Task;
        data["Targets Missed"] = TargetsMissed_Task;
        data["Distractors Hit"] = DistractorsHit_Task;
        data["Distractors Avoided"] = DistractorsAvoided_Task;
        data["Blockades Hit"] = BlockadesHit_Task;
        data["Blockades Avoided"] = BlockadesAvoided_Task;

        return data;
    }

    public override OrderedDictionary GetTaskResultsData()
    {
        OrderedDictionary data = base.GetTaskResultsData();

        data["Targets Hit"] = $"{TargetsHit_Task}/{TargetsHit_Task + TargetsMissed_Task}";
        data["Distractors Avoided"] = $"{DistractorsAvoided_Task}/{DistractorsAvoided_Task + DistractorsHit_Task}";
        data["Blockades Avoided"] = $"{BlockadesAvoided_Task}/{BlockadesAvoided_Task + BlockadesHit_Task}";

        return data;
    }


    public override void SetBlockSummaryString()
    {
        ClearStrings();

        CurrentBlockString = "\nReward Pulses: " + NumRewardPulses_InBlock;

        CurrentBlockSummaryString.AppendLine(CurrentBlockString).ToString();
    }

    private void DefineBlockData()
    {
        BlockData.AddDatum("BlockName", () => CurrentBlock.BlockName);
        BlockData.AddDatum("FogStrength", () => CurrentBlock.FogStrength);

        BlockData.AddDatum("TargetsHit", () => trialLevel.TargetsHit_Block);
        BlockData.AddDatum("TargetsMissed", () => trialLevel.TargetsMissed_Block);
        BlockData.AddDatum("DistractorsHit", () => trialLevel.DistractorsHit_Block);
        BlockData.AddDatum("DistractorsAvoided", () => trialLevel.DistractorsAvoided_Block);
        BlockData.AddDatum("BlockadesHit", () => trialLevel.BlockadesHit_Block);
        BlockData.AddDatum("BlockadesAvoided", () => trialLevel.BlockadesAvoided_Block);
    }


    public void ClearStrings()
    {
        CurrentBlockString = "";
        CurrentBlockSummaryString.Clear();
    }


}