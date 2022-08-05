using USE_ExperimentTemplate;
using WhatWhenWhere_Namespace;
using ExperimenterDisplayPanels;
using System;
using UnityEngine;
using USE_ExperimentTemplate_Classes;
using USE_Settings;

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

        RunBlock.AddInitializationMethod(() =>
        {
           wwwTL.totalErrors_InBlock = 0 ;
           wwwTL.errorType_InBlockString = "";
           wwwTL.errorType_InBlock.Clear();
           Array.Clear(wwwTL.numTotal_InBlock, 0, wwwTL.numTotal_InBlock.Length);
           Array.Clear(wwwTL.numCorrect_InBlock, 0, wwwTL.numCorrect_InBlock.Length);
           Array.Clear(wwwTL.numErrors_InBlock, 0, wwwTL.numErrors_InBlock.Length);
           wwwTL.accuracyLog_InBlock = "";
           wwwTL.runningAcc.Clear();
           Debug.Log("trial number maximum: " + bd.TrialDefs.Length);
           wwwTL.MinTrials = bd.nRepetitionsMinMax[0];
        });

        RunBlock.AddUpdateMethod(() =>
        {
            BlockSummaryString = "Block Num: " + (wwwTL.BlockCount) + "\nTrial Count: " + (wwwTL.TrialCount_InBlock) +
            "\nTotal Errors: " + wwwTL.totalErrors_InBlock + "\nError Type: " + wwwTL.errorType_InBlockString + "\nPerformance: " + wwwTL.accuracyLog_InBlock + "\n# Slider Complete: " + wwwTL.sliderCompleteQuantity;

        });



    }
    public T GetCurrentBlockDef<T>() where T : BlockDef
    {
        return (T)CurrentBlockDef;
    }

}
