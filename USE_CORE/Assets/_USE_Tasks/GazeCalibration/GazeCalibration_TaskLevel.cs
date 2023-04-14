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
            SetSettings();
        });
    }
    private void SetSettings()
    {
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ContextExternalFilePath"))
            gcTL.ContextExternalFilePath = (String)SessionSettings.Get(TaskName + "_TaskSettings", "ContextExternalFilePath");
        else gcTL.ContextExternalFilePath = ContextExternalFilePath;
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "StimFacingCamera"))
            gcTL.SpoofGazeWithMouse = (bool)SessionSettings.Get(TaskName + "_TaskSettings", "SpoofGazeWithMouse");
        else Debug.LogError("Spoof Gaze With Mouse setting not defined in the TaskDef");


    }

}