using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using USE_States;
using USE_Settings;
using USE_ExperimentTemplate_Trial;
using USE_StimulusManagement;
using GazeCalibration_Namespace;

public class GazeCalibration_TrialLevel : ControlLevel_Trial_Template
{
    public GazeCalibration_TrialDef CurrentTrialDef => GetCurrentTrialDef<GazeCalibration_TrialDef>();

    public override void DefineControlLevel()
    {
        
    }

    protected override void DefineTrialStims()
    {
        //Define StimGroups consisting of StimDefs whose gameobjects will be loaded at TrialLevel_SetupTrial and 
        //destroyed at TrialLevel_Finish
    }
    
}
