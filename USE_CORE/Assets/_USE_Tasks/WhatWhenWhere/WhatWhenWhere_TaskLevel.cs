using WhatWhenWhere_Namespace;
using System;
using UnityEngine;
using USE_Settings;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Block;

public class WhatWhenWhere_TaskLevel : ControlLevel_Task_Template
{
    WhatWhenWhere_BlockDef bd => GetCurrentBlockDef<WhatWhenWhere_BlockDef>();
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
            wwwTL.MaterialFilePath = (String) SessionSettings.Get(TaskName + "_TaskSettings", "ContextExternalFilePath");
        else Debug.LogError("Context External File Path setting not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "UsingRewardPump"))
            wwwTL.usingRewardPump = (bool)SessionSettings.Get(TaskName + "_TaskSettings", "UsingRewardPump");
        else Debug.LogError("Using Reward Pump setting not defined in the TaskDef");
        RunBlock.AddInitializationMethod(() =>
        {
           wwwTL.totalErrors_InBlock = 0 ;
           wwwTL.errorType_InBlockString = "";
           wwwTL.errorType_InBlock.Clear();
           wwwTL.slotErrorCount = 0;
           wwwTL.distractorSlotErrorCount = 0;
           wwwTL.repetitionErrorCount = 0;
           wwwTL.touchDurationErrorCount = 0;
           wwwTL.irrelevantSelectionErrorCount = 0;
           wwwTL.noScreenTouchErrorCount = 0;
            Array.Clear(wwwTL.numTotal_InBlock, 0, wwwTL.numTotal_InBlock.Length);
           Array.Clear(wwwTL.numCorrect_InBlock, 0, wwwTL.numCorrect_InBlock.Length);
           Array.Clear(wwwTL.numErrors_InBlock, 0, wwwTL.numErrors_InBlock.Length);
           wwwTL.accuracyLog_InBlock = "";
           wwwTL.runningAcc.Clear();
           wwwTL.MinTrials = bd.nRepetitionsMinMax[0];
        });

        RunBlock.AddUpdateMethod(() =>
        {
            BlockSummaryString.Clear();
            BlockSummaryString.AppendLine("Block Num: " + (wwwTL.BlockCount) + "\nTrial Count: " + (wwwTL.TrialCount_InBlock) +
            "\nTotal Errors: " + wwwTL.totalErrors_InBlock + "\nError Type: " + wwwTL.errorType_InBlockString + "\nPerformance: " + wwwTL.accuracyLog_InBlock + "\n# Slider Complete: " + wwwTL.sliderCompleteQuantity);

        });



    }
    public T GetCurrentBlockDef<T>() where T : BlockDef
    {
        return (T)CurrentBlockDef;
    }

}
