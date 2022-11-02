using System;
using UnityEngine;
using USE_ExperimentTemplate_Task;
using USE_Settings;

public class WorkingMemory_TaskLevel : ControlLevel_Task_Template
{

    public override void DefineControlLevel()
    {
        WorkingMemory_TrialLevel wmTL = (WorkingMemory_TrialLevel)TrialLevel;
        string TaskName = "WorkingMemory";
        if (SessionSettings.SettingClassExists(TaskName + "_TaskSettings"))
        {
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ContextExternalFilePath"))
            wmTL.MaterialFilePath = (String)SessionSettings.Get(TaskName + "_TaskSettings", "ContextExternalFilePath");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonPosition"))
            wmTL.buttonPosition = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonPosition");
        else Debug.LogError("[ERROR] Start Button Position settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonScale"))
            wmTL.buttonScale = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonScale");
        else Debug.LogError("[ERROR] Start Button Scale settings not defined in the TaskDef");
        }
        else
        {
            Debug.Log("[ERROR] TaskDef is not in config folder");
        }
    }


}