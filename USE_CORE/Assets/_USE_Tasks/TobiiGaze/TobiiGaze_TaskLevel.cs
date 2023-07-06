using System;
using USE_Settings;
using USE_StimulusManagement;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Block;
using TobiiGaze_Namespace;

public class TobiiGaze_TaskLevel : ControlLevel_Task_Template
{
    TobiiGaze_BlockDef tgBD => GetCurrentBlockDef<TobiiGaze_BlockDef>();
    TobiiGaze_TrialLevel tgTL;
    public override void DefineControlLevel()
    {
        tgTL = (TobiiGaze_TrialLevel)TrialLevel;

        
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ContextExternalFilePath"))
            tgTL.ContextExternalFilePath = (String)SessionSettings.Get(TaskName + "_TaskSettings", "ContextExternalFilePath");
        else tgTL.ContextExternalFilePath = SessionValues.SessionDef.ContextExternalFilePath;

    }


}