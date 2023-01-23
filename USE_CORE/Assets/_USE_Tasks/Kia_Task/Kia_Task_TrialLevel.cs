using UnityEngine;
using USE_ExperimentTemplate_Block;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Trial;
using USE_States;
using USE_StimulusManagement;
using Kia_Task_Namespace;

public class Kia_Task_TrialLevel : ControlLevel_Trial_Template
{
    public Kia_Task_TrialDef CurrentTrialDef => GetCurrentTrialDef<Kia_Task_TrialDef>();

    public override void DefineControlLevel()
    {
        

    }

    protected override void DefineTrialStims()
    {
        //Define StimGroups consisting of StimDefs whose gameobjects will be loaded at TrialLevel_SetupTrial and 
        //destroyed at TrialLevel_Finish
    }
    
}
