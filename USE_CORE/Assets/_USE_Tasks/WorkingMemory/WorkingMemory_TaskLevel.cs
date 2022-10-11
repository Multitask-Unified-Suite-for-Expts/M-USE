using System;
using USE_ExperimentTemplate_Task;
using USE_Settings;

public class WorkingMemory_TaskLevel : ControlLevel_Task_Template
{

    public override void DefineControlLevel()
    {
        WorkingMemory_TrialLevel wmTL = (WorkingMemory_TrialLevel)TrialLevel;
        string TaskName = "WorkingMemory";
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ContextExternalFilePath"))
            wmTL.MaterialFilePath = (String)SessionSettings.Get(TaskName + "_TaskSettings", "ContextExternalFilePath");

    }


}