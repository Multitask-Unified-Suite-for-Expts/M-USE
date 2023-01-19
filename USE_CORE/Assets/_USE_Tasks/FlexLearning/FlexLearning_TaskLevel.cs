using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Block;
using FlexLearning_Namespace;
using System;
using UnityEngine;
using USE_Settings;

public class FlexLearning_TaskLevel : ControlLevel_Task_Template
{
 
    FlexLearning_BlockDef flBD => GetCurrentBlockDef<FlexLearning_BlockDef>();
    public override void DefineControlLevel()
    {
        FlexLearning_TrialLevel flTL = (FlexLearning_TrialLevel)TrialLevel;
        string TaskName = "FlexLearning";
        if (SessionSettings.SettingClassExists(TaskName + "_TaskSettings"))
        {
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ContextExternalFilePath"))
                flTL.MaterialFilePath =
                    (String)SessionSettings.Get(TaskName + "_TaskSettings", "ContextExternalFilePath");
            else Debug.LogError("Context External File Path not defined in the TaskDef");
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonPosition"))
                flTL.buttonPosition = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonPosition");
            else Debug.LogError("Start Button Position settings not defined in the TaskDef");
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonScale"))
                flTL.buttonScale = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonScale");
            else Debug.LogError("Start Button Scale settings not defined in the TaskDef");
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "StimFacingCamera"))
                flTL.stimFacingCamera = (bool)SessionSettings.Get(TaskName + "_TaskSettings", "StimFacingCamera");
            else Debug.LogError("Stim Facing Camera setting not defined in the TaskDef");
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ShadowType"))
                flTL.shadowType = (string)SessionSettings.Get(TaskName + "_TaskSettings", "ShadowType");
            else Debug.LogError("Shadow Type setting not defined in the TaskDef");
            /*
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "UsingRewardPump"))
                flTL.usingRewardPump = (bool)SessionSettings.Get(TaskName + "_TaskSettings", "UsingRewardPump");
            else Debug.LogError("Using Reward Pump setting not defined in the TaskDef");
        */}
        else
        {
            Debug.Log("[ERROR] TaskDef is not in config folder");
        }

        
        RunBlock.AddInitializationMethod(() =>
        {
            /*
            flTL.totalErrors_InBlock = 0;
            flTL.errorType_InBlockString = "";
            flTL.errorType_InBlock.Clear();
            Array.Clear(flTL.numTotal_InBlock, 0, flTL.numTotal_InBlock.Length);
            Array.Clear(flTL.numCorrect_InBlock, 0, flTL.numCorrect_InBlock.Length);
            Array.Clear(flTL.numErrors_InBlock, 0, flTL.numErrors_InBlock.Length);
            flTL.accuracyLog_InBlock = "";
            */
            flTL.runningAcc.Clear();
            flTL.MinTrials = flBD.MinMaxTrials[0];
            flTL.MaxTrials = flBD.MinMaxTrials[1];
            flTL.NumTokenBar = flBD.NumTokenBar;
            flTL.numTokenBarFull = 0;
            TrialLevel.TokenFBController.SetTokenBarValue(flBD.NumInitialTokens); 
            
        });

        RunBlock.AddUpdateMethod(() =>
        {
            BlockSummaryString.Clear();
            BlockSummaryString.AppendLine("Block Num: " + (flTL.BlockCount + 1) + "\nTrial Count: " + (flTL.TrialCount_InBlock + 1) + 
                                          "\nNum Reward Given: " + flTL.numReward + "\nNum Token Bar Filled: " + 
                                          flTL.numTokenBarFull + "\nTotalTokensCollected: " + flTL.totalTokensCollected);
        });
        
    }
    public T GetCurrentBlockDef<T>() where T : BlockDef
    {
        return (T)CurrentBlockDef;
    }

}