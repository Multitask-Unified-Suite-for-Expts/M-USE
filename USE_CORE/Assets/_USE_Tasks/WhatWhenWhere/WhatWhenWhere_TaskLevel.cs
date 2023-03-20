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

        SetSettings();
        DefineBlockData();
        
        RunBlock.AddInitializationMethod(() =>
        {
            wwwTL.ContextName = wwwBD.ContextName;
            RenderSettings.skybox = CreateSkybox(wwwTL.GetContextNestedFilePath(ContextExternalFilePath, wwwTL.ContextName));
            EventCodeManager.SendCodeNextFrame(SessionEventCodes["ContextOn"]);
            
            wwwTL.ResetBlockVariables();
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
        BlockSummaryString.AppendLine("Average Search Duration: " + wwwTL.averageSearchDuration_InBlock+
                                      "\nAccuracy: " + wwwTL.accuracyLog_InBlock + 
                                      "\n" +
                                      "\nDistractor Slot Error Count: " + wwwTL.distractorSlotErrorCount_InBlock+
                                      "\nNon-Distractor Slot Error Count: " + wwwTL.slotErrorCount_InBlock + 
                                      "\nRepetition Error Count: "  + wwwTL.repetitionErrorCount_InBlock +
                                      "\nTouch Duration Error Count: " + wwwTL.touchDurationErrorCount_InBlock + 
                                      "\nNon-Stim Touch Error Count: " + wwwTL.numNonStimSelections_InBlock+
                                      "\nNo Selection Error Count: " + wwwTL.noSelectionErrorCount_InBlock);
    }

    private void DefineBlockData()
    {
        BlockData.AddDatum("Block Accuracy", ()=> wwwTL.accuracyLog_InBlock);
        BlockData.AddDatum("Avg Search Duration", ()=> wwwTL.averageSearchDuration_InBlock);
        BlockData.AddDatum("Num Distractor Slot Error", ()=> wwwTL.distractorSlotErrorCount_InBlock);
        BlockData.AddDatum("Num Search Slot Error", ()=> wwwTL.slotErrorCount_InBlock);
        BlockData.AddDatum("Num Repetition Error", ()=> wwwTL.repetitionErrorCount_InBlock);
        //BlockData.AddDatum("Num Touch Duration Error", ()=> wwwTL.touchDurationErrorCount_InBlock);
        //BlockData.AddDatum("Num Non Stim Selections", ()=> wwwTL.numNonStimSelections_InBlock); USE MOUSE TRACKER AND VALIDATE
        BlockData.AddDatum("Num Aborted Trials", ()=> wwwTL.noSelectionErrorCount_InBlock);
        BlockData.AddDatum("Num Reward Given", ()=> wwwTL.numRewardGiven_InBlock);
    }

    public void SetSettings()
    {
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ContextExternalFilePath"))
            wwwTL.ContextExternalFilePath = (String)SessionSettings.Get(TaskName + "_TaskSettings", "ContextExternalFilePath");
        else wwwTL.ContextExternalFilePath = ContextExternalFilePath;
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "StartButtonPosition"))
            wwwTL.ButtonPosition = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "StartButtonPosition");
        else Debug.LogError("Start Button Position settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "StartButtonScale"))
            wwwTL.ButtonScale = (float)SessionSettings.Get(TaskName + "_TaskSettings", "StartButtonScale");
        else Debug.LogError("Start Button Scale settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "FBSquarePosition"))
            wwwTL.FBSquarePosition = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "FBSquarePosition");
        else Debug.LogError("FB Square Position settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "FBSquareScale"))
            wwwTL.FBSquareScale = (float)SessionSettings.Get(TaskName + "_TaskSettings", "FBSquareScale");
        else Debug.LogError("FBSquare Scale settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "StimFacingCamera"))
            wwwTL.StimFacingCamera = (bool)SessionSettings.Get(TaskName + "_TaskSettings", "StimFacingCamera");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "NeutralITI"))
            wwwTL.NeutralITI = (bool)SessionSettings.Get(TaskName + "_TaskSettings", "NeutralITI");
        else Debug.LogError("Neutral ITI setting not defined in the TaskDef");
    }

}