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
           // SetSettings();
        });
    }
    private void SetSettings()
    {
        #if (!UNITY_WEBGL)
            gcTL.MonitorDetails = MonitorDetails;
        #endif

        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ContextExternalFilePath"))
            gcTL.ContextExternalFilePath = (String)SessionSettings.Get(TaskName + "_TaskSettings", "ContextExternalFilePath");
        else gcTL.ContextExternalFilePath = ContextExternalFilePath;

        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "SpoofGazeWithMouse"))
            gcTL.SpoofGazeWithMouse = (bool)SessionSettings.Get(TaskName + "_TaskSettings", "SpoofGazeWithMouse");
        else Debug.LogError("Spoof Gaze With Mouse setting not defined in the TaskDef");
        
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "SmallCirclePosition"))
            gcTL.SmallCirclePosition = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "SmallCirclePosition");
        else Debug.LogError("SmallCirclePosition setting not defined in the TaskDef");
        
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "BigCirclePosition"))
            gcTL.BigCirclePosition = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "BigCirclePosition");
        else Debug.LogError("BigCirclePosition setting not defined in the TaskDef");
        
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "SmallCircleSize"))
            gcTL.SmallCircleSize = (float)SessionSettings.Get(TaskName + "_TaskSettings", "SmallCircleSize");
        else Debug.LogError("SmallCircleSize setting not defined in the TaskDef");
        
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "BigCircleSize"))
            gcTL.BigCircleSize = (float)SessionSettings.Get(TaskName + "_TaskSettings", "BigCircleSize");
        else Debug.LogError("BigCircleSize setting not defined in the TaskDef");


    }

}