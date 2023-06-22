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

        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ContextExternalFilePath"))
            gcTL.ContextExternalFilePath = (String)SessionSettings.Get(TaskName + "_TaskSettings", "ContextExternalFilePath");
        else gcTL.ContextExternalFilePath = ContextExternalFilePath;

        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "SpoofGazeWithMouse"))
            gcTL.SpoofGazeWithMouse = (bool)SessionSettings.Get(TaskName + "_TaskSettings", "SpoofGazeWithMouse");
        else
            Debug.LogError("Spoof Gaze With Mouse setting not defined in the TaskDef. Default set to TRUE.");
       
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "CalibPointsInset"))
            gcTL.CalibPointsInset = (float[])SessionSettings.Get(TaskName + "_TaskSettings", "CalibPointsInset");
        else
            Debug.LogError("Calib Points Inset setting not defined in the TaskDef. Default set to [0.1, 0.1]");
        
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "MaxCircleScale"))
            gcTL.MaxCircleScale = (float)SessionSettings.Get(TaskName + "_TaskSettings", "MaxCircleScale");
        else Debug.LogError("Max Circle Scale setting not defined in the TaskDef");

        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "MinCircleScale"))
            gcTL.MinCircleScale = (float)SessionSettings.Get(TaskName + "_TaskSettings", "MinCircleScale");
        else Debug.LogError("Min Circle Scale setting not defined in the TaskDef");

        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ShrinkDuration"))
            gcTL.ShrinkDuration = (float)SessionSettings.Get(TaskName + "_TaskSettings", "ShrinkDuration");
        else Debug.LogError("Shrink Duration setting not defined in the TaskDef");
    }

}