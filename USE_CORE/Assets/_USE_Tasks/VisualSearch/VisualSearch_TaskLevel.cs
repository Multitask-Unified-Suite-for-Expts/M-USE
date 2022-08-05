using USE_ExperimentTemplate;
using VisualSearch_Namespace;
using System;
using UnityEngine;
using USE_Settings;

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
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "NumTokens"))
                vsTL.TaskTokenNum = (int)SessionSettings.Get(TaskName + "_TaskSettings", "NumTokens");
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
            //vsTL.MaterialFilePath = bd.ContextExternalFilePath;
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