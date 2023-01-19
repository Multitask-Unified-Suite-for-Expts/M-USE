using VisualSearch_Namespace;
using System;
using System.Linq;
using UnityEngine;
using USE_Settings;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Block;

public class VisualSearch_TaskLevel : ControlLevel_Task_Template
{
    VisualSearch_BlockDef vsBD => GetCurrentBlockDef<VisualSearch_BlockDef>();
    
    VisualSearch_TrialLevel vsTL;
    public override void DefineControlLevel()
    {
        vsTL = (VisualSearch_TrialLevel)TrialLevel;
        string TaskName = "VisualSearch";
        if (SessionSettings.SettingClassExists(TaskName + "_TaskSettings"))
        {
            if(SessionSettings.SettingExists(TaskName + "_TaskSettings", "ContextExternalFilePath"))
                         vsTL.MaterialFilePath = (String)SessionSettings.Get(TaskName + "_TaskSettings", "ContextExternalFilePath");
            else if(SessionSettings.SettingExists("Session", "ContextExternalFilePath"))
                         vsTL.MaterialFilePath = (String)SessionSettings.Get("Session", "ContextExternalFilePath");
            else Debug.Log("ContextExternalFilePath NOT specified in the Session Config OR Task Config!");
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonPosition"))
                vsTL.ButtonPosition = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonPosition");
            else Debug.LogError("Start Button Position settings not defined in the TaskDef");
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonScale"))
                vsTL.ButtonScale = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonScale");
            else Debug.LogError("Start Button Scale settings not defined in the TaskDef");
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "StimFacingCamera"))
                vsTL.StimFacingCamera = (bool)SessionSettings.Get(TaskName + "_TaskSettings", "StimFacingCamera");
            else Debug.LogError("Stim Facing Camera setting not defined in the TaskDef");
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ShadowType"))
                vsTL.ShadowType = (string)SessionSettings.Get(TaskName + "_TaskSettings", "ShadowType");
            else Debug.LogError("Shadow Type setting not defined in the TaskDef");
        }
        else
        {
            Debug.LogError("TaskDef is not in config folder");
        }
        

        RunBlock.AddInitializationMethod(() =>
        {
            ResetBlockVariables();
            vsTL.TokenFBController.SetTotalTokensNum(vsBD.NumTokenBar);
            vsTL.TokenFBController.SetTokenBarValue(vsBD.NumInitialTokens);
            SetBlockSummaryString();
        });
    }
    public T GetCurrentBlockDef<T>() where T : BlockDef
    {
        return (T)CurrentBlockDef;
    }

    private void ResetBlockVariables()
    {
        vsTL.SearchDurationsList.Clear();
        vsTL.NumErrors_InBlock = 0;
        vsTL.NumCorrect_InBlock = 0;
        vsTL.NumRewardGiven_InBlock = 0;
        vsTL.NumTokenBarFull_InBlock = 0;
        vsTL.TouchDurationError_InBlock = 0;
    }

    public void SetBlockSummaryString()
    {
        BlockSummaryString.Clear();
        
        BlockSummaryString.AppendLine("\nBlock Num: " + (vsTL.BlockCount + 1) +
                                      "\nTrial Num: " + (vsTL.TrialCount_InBlock + 1) +
                                      "\n" + 
                                      "\nAccuracy: " + vsTL.Accuracy_InBlock +  
                                      "\n" + 
                                      "\nAvg Search Duration: " + vsTL.AverageSearchDuration_InBlock +
                                      "\n" + 
                                      "\nNum Touch Duration Error: " + vsTL.TouchDurationError_InBlock + 
                                      "\nNum Reward Given: " + vsTL.NumRewardGiven_InBlock + 
                                      "\nNum Token Bar Filled: " + vsTL.NumTokenBarFull_InBlock +
                                      "\nTotal Tokens Collected: " + vsTL.TotalTokensCollected_InBlock);
    }
}