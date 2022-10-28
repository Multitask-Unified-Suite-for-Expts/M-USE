using VisualSearch_Namespace;
using System;
using UnityEngine;
using USE_Settings;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Block;

public class VisualSearch_TaskLevel : ControlLevel_Task_Template
{
    VisualSearch_BlockDef bd => GetCurrentBlockDef<VisualSearch_BlockDef>();
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
            vsTL.NumTokenBar = bd.NumTokenBar;
            TrialLevel.TokenFBController.SetTokenBarValue(bd.NumInitialTokens); 
        });
        
        RunBlock.AddUpdateMethod(() =>
        {
            BlockSummaryString = "Block Num: " + (vsTL.BlockCount) + "\nTrial Count: " + (vsTL.TrialCount_InBlock);
          //  "\nTotal Errors: " + vsTL.totalErrors_InBlock + "\nError Type: " + vsTL.errorType_InBlockString + "\nPerformance: " + vsTL.accuracyLog_InBlock;
        });
    }
    public T GetCurrentBlockDef<T>() where T : BlockDef
    {
        return (T)CurrentBlockDef;
    }

}