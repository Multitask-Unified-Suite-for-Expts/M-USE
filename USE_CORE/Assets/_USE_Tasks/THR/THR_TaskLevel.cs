using System;
using System.Text;
using System.Collections.Generic;
using THR_Namespace;
using UnityEngine;
using UnityEngine.UI;
using USE_Settings;
using USE_StimulusManagement;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Block;

public class THR_TaskLevel : ControlLevel_Task_Template
{


    THR_BlockDef currentBlock => GetCurrentBlockDef<THR_BlockDef>();

    public override void SpecifyTypes()
    {
        TaskLevelType = typeof(THR_TaskLevel);
        TrialLevelType = typeof(THR_TrialLevel);
        TaskDefType = typeof(THR_TaskDef);
        BlockDefType = typeof(THR_BlockDef);
        TrialDefType = typeof(THR_TrialDef);
        //StimDefType = typeof(ContinuousRecognition_StimDef);
    }


    public override void DefineControlLevel()
    {
        THR_TrialLevel trialLevel = (THR_TrialLevel)TrialLevel;

        string TaskName = "THR";
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ContextExternalFilePath"))
            trialLevel.MaterialFilePath = (String)SessionSettings.Get(TaskName + "_TaskSettings", "ContextExternalFilePath");

        RunBlock.AddInitializationMethod(() =>
        {
            trialLevel.NumTrialsCompletedBlock = 0;
            trialLevel.NumTrialsCorrectBlock = 0;
            trialLevel.NumNonSquareTouches = 0;
            trialLevel.NumTouchesBlueSquare = 0;
            trialLevel.NumTouchesWhiteSquare = 0;
        });

  
    }


    public T GetCurrentBlockDef<T>() where T : BlockDef
    {
        return (T)CurrentBlockDef;
    }


}