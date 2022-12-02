using VisualSearch_Namespace;
using System;
using UnityEngine;
using USE_Settings;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Block;

public class VisualSearch_TaskLevel : ControlLevel_Task_Template
{
    VisualSearch_BlockDef vsBD => GetCurrentBlockDef<VisualSearch_BlockDef>();
    public override void DefineControlLevel()
    {
        VisualSearch_TrialLevel vsTL = (VisualSearch_TrialLevel)TrialLevel;
        string TaskName = "VisualSearch";
        if (SessionSettings.SettingClassExists(TaskName + "_TaskSettings"))
        {
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ContextExternalFilePath"))
                vsTL.MaterialFilePath =
                    (String)SessionSettings.Get(TaskName + "_TaskSettings", "ContextExternalFilePath");
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonPosition"))
                vsTL.buttonPosition = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonPosition");
            else Debug.LogError("[ERROR] Start Button Position settings not defined in the TaskDef");
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonScale"))
                vsTL.buttonScale = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonScale");
            else Debug.LogError("[ERROR] Start Button Scale settings not defined in the TaskDef");
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "StimFacingCamera"))
                vsTL.stimFacingCamera = (bool)SessionSettings.Get(TaskName + "_TaskSettings", "StimFacingCamera");
            else Debug.LogError("Stim Facing Camera setting not defined in the TaskDef");
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ShadowType"))
                vsTL.shadowType = (string)SessionSettings.Get(TaskName + "_TaskSettings", "ShadowType");
            else Debug.LogError("Shadow Type setting not defined in the TaskDef");
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "UsingRewardPump"))
                vsTL.usingRewardPump = (bool)SessionSettings.Get(TaskName + "_TaskSettings", "UsingRewardPump");
            else Debug.LogError("Using Reward Pump setting not defined in the TaskDef");
        }
        else
        {
            Debug.Log("[ERROR] TaskDef is not in config folder");
        }
        

        RunBlock.AddInitializationMethod(() =>
        {
            /*
            vsTL.totalErrors_InBlock = 0;
            vsTL.errorType_InBlockString = "";
            vsTL.errorType_InBlock.Clear();
            Array.Clear(vsTL.numTotal_InBlock, 0, vsTL.numTotal_InBlock.Length);
            Array.Clear(vsTL.numCorrect_InBlock, 0, vsTL.numCorrect_InBlock.Length);
            Array.Clear(vsTL.numErrors_InBlock, 0, vsTL.numErrors_InBlock.Length);
            vsTL.accuracyLog_InBlock = "";*/
            vsTL.numReward = 0;
            vsTL.NumTokenBar = vsBD.NumTokenBar;
            TrialLevel.TokenFBController.SetTokenBarValue(vsBD.NumInitialTokens); 
        });
        
        RunBlock.AddUpdateMethod(() =>
        {
            BlockSummaryString.Clear();
            BlockSummaryString.AppendLine("Block Num: " + (vsTL.BlockCount + 1) + "\nTrial Count: " + (vsTL.TrialCount_InBlock + 1) + 
                                          "\nNum Reward Given: " + vsTL.numReward + "\nNum Token Bar Filled: " + vsTL.numTokenBarFull + 
                                          "\nTotalTokensCollected: " + vsTL.totalTokensCollected);
          //  "\nTotal Errors: " + vsTL.totalErrors_InBlock + "\nError Type: " + vsTL.errorType_InBlockString + "\nPerformance: " + vsTL.accuracyLog_InBlock;
        });
    }
    public T GetCurrentBlockDef<T>() where T : BlockDef
    {
        return (T)CurrentBlockDef;
    }

}