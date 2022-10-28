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
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonPosition"))
                flTL.buttonPosition = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonPosition");
            else Debug.LogError("[ERROR] Start Button Position settings not defined in the TaskDef");
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonScale"))
                flTL.buttonScale = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonScale");
            else Debug.LogError("[ERROR] Start Button Scale settings not defined in the TaskDef");
        }
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
            System.Random rnd = new System.Random();
             
            flTL.MaxTrials = rnd.Next(flBD.MinMaxTrials[0], flBD.MinMaxTrials[1]);
            flTL.NumTokenBar = flBD.NumTokenBar;
            TrialLevel.TokenFBController.SetTokenBarValue(flBD.NumInitialTokens); 
        });

        RunBlock.AddUpdateMethod(() =>
        {
            BlockSummaryString = "Block Num: " + (flTL.BlockCount) + "\nTrial Count: " + (flTL.TrialCount_InBlock);
            //  "\nTotal Errors: " + vsTL.totalErrors_InBlock + "\nError Type: " + vsTL.errorType_InBlockString + "\nPerformance: " + vsTL.accuracyLog_InBlock;
        });
        
    }
    public T GetCurrentBlockDef<T>() where T : BlockDef
    {
        return (T)CurrentBlockDef;
    }

}