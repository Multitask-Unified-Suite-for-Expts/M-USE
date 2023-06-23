using GazeCalibration_Namespace;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using USE_Settings;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Block;
using USE_ExperimentTemplate_Trial;

public class GazeCalibration_TaskLevel : ControlLevel_Task_Template
{
    GazeCalibration_BlockDef gcBD => GetCurrentBlockDef<GazeCalibration_BlockDef>();
    GazeCalibration_TrialLevel gcTL;
    public override void DefineControlLevel()
    {
        gcTL = (GazeCalibration_TrialLevel)TrialLevel;
        SetSettings();

        RunBlock.AddInitializationMethod (() =>
        {
            // Set the Background image as defined in the BlockDef
            RenderSettings.skybox = CreateSkybox(gcTL.GetContextNestedFilePath(ContextExternalFilePath, gcBD.ContextName, "LinearDark"));
        });
    }
    private void SetSettings()
    {   
        gcTL.ContextExternalFilePath = ContextExternalFilePath;
        gcTL.SpoofGazeWithMouse = false;
        gcTL.CalibPointsInset = new float[] {0.15f, 0.15f};
        gcTL.MaxCircleScale = 0.75f;
        gcTL.MinCircleScale = 0.15f;
        gcTL.ShrinkDuration = 1.5f;
    }

}
