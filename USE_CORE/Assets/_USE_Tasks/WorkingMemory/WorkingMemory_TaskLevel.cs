using System;
using USE_ExperimentTemplate;
using USE_Settings;
using WorkingMemory_Namespace;

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