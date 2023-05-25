using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using USE_States;
using USE_Settings;
using USE_ExperimentTemplate_Trial;
using USE_StimulusManagement;
using WWW_2D_Namespace;

public class WWW_2D_TrialLevel : ControlLevel_Trial_Template
{
    public WWW_2D_TrialDef CurrentTrialDef => GetCurrentTrialDef<WWW_2D_TrialDef>();

    public override void DefineControlLevel()
    {
        

    }

    protected override void DefineTrialStims()
    {
        //Define StimGroups consisting of StimDefs whose gameobjects will be loaded at TrialLevel_SetupTrial and 
        //destroyed at TrialLevel_Finish
    }
    
}
