using USE_ExperimentTemplate_Task;
using FruitRunner_Namespace;
using UnityEngine;
using System.Collections.Specialized;

public class FruitRunner_TaskLevel : ControlLevel_Task_Template
{
    FruitRunner_BlockDef CurrentBlock => GetCurrentBlockDef<FruitRunner_BlockDef>();
    FruitRunner_TrialLevel trialLevel;

    [HideInInspector] public string CurrentBlockString;
    [HideInInspector] public int BlockStringsAdded = 0;

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
            CalculateBlockSummaryString();
            //TaskCam.fieldOfView = 50;

        });
        BlockFeedback.AddSpecificInitializationMethod(() =>
        {
            Camera.main.transform.rotation = new Quaternion(0f, 0f, 0f, 0f);
            HandleBlockStrings();
        });
    }

    void SetTrialFogStrength()
    {
        RenderSettings.fog = true;
        RenderSettings.fogDensity = CurrentBlock.FogStrength;
        
    }

    public override OrderedDictionary GetBlockResultsData()
    {
        OrderedDictionary data = new OrderedDictionary
        {
            ["Score"] = trialLevel.Score_Block,
            ["Targets Hit"] = $"{trialLevel.TargetsHit_Block}/{trialLevel.TargetsHit_Block + trialLevel.TargetsMissed_Block}",
            ["Distractors Hit"] = $"{trialLevel.DistractorsAvoided_Block}/{trialLevel.DistractorsHit_Block + trialLevel.DistractorsAvoided_Block}",
            ["Blockades Hit"] = $"{trialLevel.BlockadesHit_Block}/{trialLevel.BlockadesHit_Block + trialLevel.BlockadesAvoided_Block}",
        };
        return data;
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



    public void CalculateBlockSummaryString()
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