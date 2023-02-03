using WhatWhenWhere_Namespace;
using System;
using System.IO;
using System.Text;
using UnityEngine;
using USE_Settings;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Block;

public class WhatWhenWhere_TaskLevel : ControlLevel_Task_Template
{
    WhatWhenWhere_BlockDef wwwBD => GetCurrentBlockDef<WhatWhenWhere_BlockDef>();
    WhatWhenWhere_TrialLevel wwwTL;
    public override void DefineControlLevel()
    {
        wwwTL = (WhatWhenWhere_TrialLevel)TrialLevel;
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
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "NeutralITI"))
            wwwTL.NeutralITI = (bool)SessionSettings.Get(TaskName + "_TaskSettings", "NeutralITI");
        else Debug.LogError("Neutral ITI setting not defined in the TaskDef");
        
        SetupTask.AddInitializationMethod(() =>
        {
            //HARD CODED TO MINIMIZE EMPTY SKYBOX DURATION, CAN'T ACCESS TRIAL DEF YET & CONTEXT NOT IN BLOCK DEF
            RenderSettings.skybox = CreateSkybox(ContextExternalFilePath + Path.DirectorySeparatorChar +  "Desert.png");
        });
        RunBlock.AddInitializationMethod(() =>
        {
           wwwTL.errorType_InBlockString = "";
           wwwTL.errorType_InBlock.Clear();
           wwwTL.slotErrorCount_InBlock = 0;
           wwwTL.distractorSlotErrorCount_InBlock = 0;
           wwwTL.repetitionErrorCount_InBlock = 0;
           wwwTL.noSelectionErrorCount_InBlock = 0;
           wwwTL.touchDurationErrorCount_InBlock = 0;
           wwwTL.numNonStimSelections_InBlock = 0;
           wwwTL.numRewardGiven_InBlock = 0;
           //comment better here
           Array.Clear(wwwTL.numTotal_InBlock, 0, wwwTL.numTotal_InBlock.Length);
           Array.Clear(wwwTL.numCorrect_InBlock, 0, wwwTL.numCorrect_InBlock.Length);
           Array.Clear(wwwTL.numErrors_InBlock, 0, wwwTL.numErrors_InBlock.Length);
           wwwTL.accuracyLog_InBlock = "";
           wwwTL.averageSearchDuration_InBlock = 0;
           wwwTL.runningAcc.Clear();
           wwwTL.MinTrials = wwwBD.nRepetitionsMinMax[0];
           SetBlockSummaryString();
        });

        // RunBlock.AddUpdateMethod(() =>
        // {
        //     BlockSummaryString.Clear();
        //     BlockSummaryString.AppendLine("Block Num: " + (wwwTL.BlockCount) + "\nTrial Count: " + (wwwTL.TrialCount_InBlock) +
        //     "\nTotal Errors: " + wwwTL.totalErrors_InBlock + "\nError Type: " + wwwTL.errorType_InBlockString + "\nPerformance: " + wwwTL.accuracyLog_InBlock + "\n# Slider Complete: " + wwwTL.sliderCompleteQuantity);
        //
        // });
    }
    public void SetBlockSummaryString()
    {
        BlockSummaryString.Clear();
        BlockSummaryString.AppendLine("\nBlock Num: " + (wwwTL.BlockCount + 1) +
                                      "\nTrial Num: " + (wwwTL.TrialCount_InBlock + 1) +
                                      "\n" + 
                                      "\nAverage Search Duration: " + wwwTL.averageSearchDuration_InBlock+
                                      "\nAccuracy: " + wwwTL.accuracyLog_InBlock + 
                                      "\n" +
                                      "\nDistractor Slot Error Count: " + wwwTL.distractorSlotErrorCount_InBlock+
                                      "\nNon-Distractor Slot Error Count: " + wwwTL.slotErrorCount_InBlock + 
                                      "\nRepetition Error Count: "  + wwwTL.repetitionErrorCount_InBlock +
                                      "\nTouch Duration Error Count: " + wwwTL.touchDurationErrorCount_InBlock + 
                                      "\nNon-Stim Touch Error Count: " + wwwTL.numNonStimSelections_InBlock+
                                      "\nNo Selection Error Count: " + wwwTL.noSelectionErrorCount_InBlock);
    }

    private void AssignBlockDatum()
    {
        BlockData.AddDatum("Block Accuracy", ()=> wwwTL.accuracyLog_InBlock);
        BlockData.AddDatum("Avg Search Duration", ()=> wwwTL.averageSearchDuration_InBlock);
        BlockData.AddDatum("Num Touch Duration Error", ()=> wwwTL.distractorSlotErrorCount_InBlock);
        BlockData.AddDatum("Num Touch Duration Error", ()=> wwwTL.slotErrorCount_InBlock);
        BlockData.AddDatum("Num Touch Duration Error", ()=> wwwTL.repetitionErrorCount_InBlock);
        BlockData.AddDatum("Num Touch Duration Error", ()=> wwwTL.touchDurationErrorCount_InBlock);
        BlockData.AddDatum("Num Touch Duration Error", ()=> wwwTL.numNonStimSelections_InBlock);
        BlockData.AddDatum("Num Touch Duration Error", ()=> wwwTL.noSelectionErrorCount_InBlock);
        BlockData.AddDatum("Num Reward Given", ()=> wwwTL.numRewardGiven_InBlock);
    }
    // public T GetCurrentBlockDef<T>() where T : BlockDef
    // {
    //     return (T)CurrentBlockDef;
    // }

}
