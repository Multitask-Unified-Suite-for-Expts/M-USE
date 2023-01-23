using WhatWhenWhere_Namespace;
using System;
using UnityEngine;
using USE_Settings;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Block;

public class WhatWhenWhere_TaskLevel : ControlLevel_Task_Template
{
    WhatWhenWhere_BlockDef bd => GetCurrentBlockDef<WhatWhenWhere_BlockDef>();
    private WhatWhenWhere_TrialLevel wwwTL;
    public override void SpecifyTypes()
    {
        //note that since EffortControl_TaskDef and EffortControl_BlockDef do not add any fields or methods to their parent types, 
        //they do not actually need to be specified here, but they are included to make this script more useful for later copying.
        TaskLevelType = typeof(WhatWhenWhere_TaskLevel);
        TrialLevelType = typeof(WhatWhenWhere_TrialLevel);
        TaskDefType = typeof(WhatWhenWhere_TaskDef);
        BlockDefType = typeof(WhatWhenWhere_BlockDef);
        TrialDefType = typeof(WhatWhenWhere_TrialDef);
        StimDefType = typeof(WhatWhenWhere_StimDef);
    }

    public override void DefineControlLevel()
    {
        WhatWhenWhere_TrialLevel wwwTL = (WhatWhenWhere_TrialLevel)TrialLevel;
        string TaskName = "WhatWhenWhere";
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ContextExternalFilePath"))
            wwwTL.ContextExternalFilePath = (String)SessionSettings.Get(TaskName + "_TaskSettings", "ContextExternalFilePath");
        else wwwTL.ContextExternalFilePath = ContextExternalFilePath;
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonPosition"))
            wwwTL.ButtonPosition = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonPosition");
        else Debug.LogError("Start Button Position settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonScale"))
            wwwTL.ButtonScale = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonScale");
        else Debug.LogError("Start Button Scale settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "FBSquarePosition"))
            wwwTL.FBSquarePosition = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "FBSquarePosition");
        else Debug.LogError("FB Square Position settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "FBSquareScale"))
            wwwTL.FBSquareScale = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "FBSquareScale");
        else Debug.LogError("FB Square Scale settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "StimFacingCamera"))
            wwwTL.StimFacingCamera = (bool)SessionSettings.Get(TaskName + "_TaskSettings", "StimFacingCamera");
        RunBlock.AddInitializationMethod(() =>
        {
            //comment each error type
           wwwTL.totalErrors_InBlock = 0 ;
           wwwTL.errorType_InBlockString = "";
           wwwTL.errorType_InBlock.Clear();
           wwwTL.slotErrorCount = 0;
           wwwTL.distractorSlotErrorCount = 0;
           wwwTL.repetitionErrorCount = 0;
           wwwTL.touchDurationErrorCount = 0;
           wwwTL.irrelevantSelectionErrorCount = 0;
           wwwTL.noScreenTouchErrorCount = 0;
           //comment better here
           Array.Clear(wwwTL.numTotal_InBlock, 0, wwwTL.numTotal_InBlock.Length);
           Array.Clear(wwwTL.numCorrect_InBlock, 0, wwwTL.numCorrect_InBlock.Length);
           Array.Clear(wwwTL.numErrors_InBlock, 0, wwwTL.numErrors_InBlock.Length);
           wwwTL.accuracyLog_InBlock = "";
           wwwTL.runningAcc.Clear();
           wwwTL.MinTrials = bd.nRepetitionsMinMax[0];
        });

        // RunBlock.AddUpdateMethod(() =>
        // {
        //     BlockSummaryString.Clear();
        //     BlockSummaryString.AppendLine("Block Num: " + (wwwTL.BlockCount) + "\nTrial Count: " + (wwwTL.TrialCount_InBlock) +
        //     "\nTotal Errors: " + wwwTL.totalErrors_InBlock + "\nError Type: " + wwwTL.errorType_InBlockString + "\nPerformance: " + wwwTL.accuracyLog_InBlock + "\n# Slider Complete: " + wwwTL.sliderCompleteQuantity);
        //
        // });
    }

    public void UpdateBlockSummary()
    {
        BlockSummaryString.Clear();
        BlockSummaryString.AppendLine("Block Num: " + (wwwTL.BlockCount) + 
                                      "\nTrial Count: " + (wwwTL.TrialCount_InBlock) +
                                      "\nTotal Errors: " + wwwTL.totalErrors_InBlock + 
                                      "\nPerformance: " + wwwTL.accuracyLog_InBlock + 
                                      "\n# Slider Complete: " + wwwTL.sliderCompleteQuantity);
    }
    // public T GetCurrentBlockDef<T>() where T : BlockDef
    // {
    //     return (T)CurrentBlockDef;
    // }

}
