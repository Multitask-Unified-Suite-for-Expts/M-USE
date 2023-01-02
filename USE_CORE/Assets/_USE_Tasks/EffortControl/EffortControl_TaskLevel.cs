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
using System.Collections.Specialized;
using System.IO;

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

    public int NumCompletions = 0;
    public int NumPulses = 0;
    public int TotalTouches = 0;

    public override void DefineControlLevel()
    {
        EffortControl_TrialLevel trialLevel = (EffortControl_TrialLevel)TrialLevel;
        string TaskName = "EffortControl";
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ContextExternalFilePath"))
            trialLevel.MaterialFilePath = (String)SessionSettings.Get(TaskName + "_TaskSettings", "ContextExternalFilePath");
        else if (SessionSettings.SettingExists("Session", "ContextExternalFilePath"))
            trialLevel.MaterialFilePath = (String)SessionSettings.Get("Session", "ContextExternalFilePath");
        else
            Debug.Log("ContextExternalFilePath NOT specified in the Session Config OR Task Config!");

    }

    public override OrderedDictionary GetSummaryData()
    {
        OrderedDictionary data = new OrderedDictionary();

        data["Num Completions"] = NumCompletions;
        data["Num Pulses"] = NumPulses;
        data["Total Touches"] = TotalTouches;

        return data;
    }

}