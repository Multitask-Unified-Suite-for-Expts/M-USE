using System;
using System.Text;
using System.Collections.Generic;
using ContinuousRecognition_Namespace;
using UnityEngine;
using UnityEngine.UI;
using USE_Settings;
using USE_StimulusManagement;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Block;


public class EffortControl_TaskLevel : ControlLevel_Task_Template
{   
    //public override void SpecifyTypes()
    //{
    //    //note that since EffortControl_TaskDef and EffortControl_BlockDef do not add any fields or methods to their parent types, 
    //    //they do not actually need to be specified here, but they are included to make this script more useful for later copying.
    //    TaskLevelType = typeof(EffortControl_TaskLevel);
    //    TrialLevelType = typeof(EffortControl_TrialLevel);
    //    TaskDefType = typeof(EffortControl_TaskDef);
    //    BlockDefType = typeof(EffortControl_BlockDef);
    //    TrialDefType = typeof(EffortControl_TrialDef);
    //    StimDefType = typeof(EffortControl_StimDef);
    //}
    
    public override void DefineControlLevel()
    {
        EffortControl_TrialLevel wmTL = (EffortControl_TrialLevel)TrialLevel;
        string TaskName = "EffortControl";
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ContextExternalFilePath"))
            wmTL.MaterialFilePath = (String)SessionSettings.Get(TaskName + "_TaskSettings", "ContextExternalFilePath");

    }


}