using System;
using System.Collections.Generic;
using ContinuousRecognition_Namespace;
using UnityEngine;
using UnityEngine.UI;
using USE_Settings;
using USE_StimulusManagement;
using USE_ExperimentTemplate_Task;
public class ContinuousRecognition_TaskLevel : ControlLevel_Task_Template
{
    public override void SpecifyTypes()
    {
        TaskLevelType = typeof(ContinuousRecognition_TaskLevel);
        TrialLevelType = typeof(ContinuousRecognition_TrialLevel);
        TaskDefType = typeof(ContinuousRecognition_TaskDef);
        BlockDefType = typeof(ContinuousRecognition_BlockDef);
        TrialDefType = typeof(ContinuousRecognition_TrialDef);
        StimDefType = typeof(ContinuousRecognition_StimDef);
    } 
    public override void DefineControlLevel() //RUNS WHEN THE TASK IS DEFINED!
    {           
        ContinuousRecognition_TrialLevel trialLevel = (ContinuousRecognition_TrialLevel)TrialLevel;
        string TaskName = "ContinuousRecognition";
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ContextExternalFilePath"))
            trialLevel.MaterialFilePath = (String)SessionSettings.Get(TaskName + "_TaskSettings", "ContextExternalFilePath");

        //Clearing the list of picked stim indices at beginning of each block. 
        RunBlock.AddInitializationMethod(() =>
        {
            trialLevel.ChosenStimIndices.Clear();
        });
    }

}
