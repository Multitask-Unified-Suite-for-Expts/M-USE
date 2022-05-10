using UnityEngine;
using USE_ExperimentTemplate;
using USE_States;
using USE_StimulusManagement;
using test_Namespace;

public class test_TrialLevel : ControlLevel_Trial_Template
{
    public test_TrialDef CurrentTrialDef => GetCurrentTrialDef<test_TrialDef>();

    public override void DefineControlLevel()
    {
        

    }

    protected override void DefineTrialStims()
    {
        //Define StimGroups consisting of StimDefs whose gameobjects will be loaded at TrialLevel_SetupTrial and 
        //destroyed at TrialLevel_Finish
    }
    
}
