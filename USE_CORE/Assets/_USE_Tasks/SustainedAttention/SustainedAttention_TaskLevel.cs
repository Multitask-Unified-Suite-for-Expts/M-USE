using USE_Settings;
using USE_StimulusManagement;
using USE_ExperimentTemplate_Task;
using SustainedAttention_Namespace;
using UnityEngine;

public class SustainedAttention_TaskLevel : ControlLevel_Task_Template
{
    SustainedAttention_BlockDef CurrentBlock => GetCurrentBlockDef<SustainedAttention_BlockDef>();
    SustainedAttention_TrialLevel trialLevel;



    public override void DefineControlLevel()
    {
        trialLevel = (SustainedAttention_TrialLevel)TrialLevel;

        Session.HumanStartPanel.AddTaskDisplayName(TaskName, "Sustained Attention");
        Session.HumanStartPanel.AddTaskInstructions(TaskName, "Select the target stimuli when it closes its mouth!");

        RunBlock.AddSpecificInitializationMethod(() =>
        {
            SetSkyBox(CurrentBlock.ContextName);
        });

    }


}