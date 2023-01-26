using FlexLearning_Namespace;
using System;
using UnityEngine;
using USE_Settings;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Block;

public class FlexLearning_TaskLevel : ControlLevel_Task_Template
{
    FlexLearning_BlockDef flBD => GetCurrentBlockDef<FlexLearning_BlockDef>();
    FlexLearning_TrialLevel flTL;
    public override void DefineControlLevel()
    {
        flTL = (FlexLearning_TrialLevel)TrialLevel;
        string TaskName = "FlexLearning";
        if (SessionSettings.SettingClassExists(TaskName + "_TaskSettings"))
        {
             if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ContextExternalFilePath"))
                 flTL.ContextExternalFilePath = (String)SessionSettings.Get(TaskName + "_TaskSettings", "ContextExternalFilePath");
            else flTL.ContextExternalFilePath = ContextExternalFilePath;
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonPosition"))
                flTL.ButtonPosition = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonPosition");
            else Debug.LogError("Start Button Position settings not defined in the TaskDef");
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonScale"))
                flTL.ButtonScale = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonScale");
            else Debug.LogError("Start Button Scale settings not defined in the TaskDef");
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "FBSquarePosition"))
                flTL.FBSquarePosition = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "FBSquarePosition");
            else Debug.LogError("FB Square Position settings not defined in the TaskDef");
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "FBSquareScale"))
                flTL.FBSquareScale = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "FBSquareScale");
            else Debug.LogError("FB Square Scale settings not defined in the TaskDef");
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "StimFacingCamera"))
                flTL.StimFacingCamera = (bool)SessionSettings.Get(TaskName + "_TaskSettings", "StimFacingCamera");
            else Debug.LogError("Stim Facing Camera setting not defined in the TaskDef");
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ShadowType"))
                flTL.ShadowType = (string)SessionSettings.Get(TaskName + "_TaskSettings", "ShadowType");
            else Debug.LogError("Shadow Type setting not defined in the TaskDef");
            
        }
        else
        {
            Debug.Log("[ERROR] TaskDef is not in config folder");
        }

        
        RunBlock.AddInitializationMethod(() =>
        {
            System.Random rnd = new System.Random();
            int RandomMaxTrials = rnd.Next(flBD.MinMaxTrials[0], flBD.MinMaxTrials[1]);
            flTL.MaxTrials = RandomMaxTrials;
            flTL.MinTrials = flBD.MinMaxTrials[0];
            ResetBlockVariables();
            flTL.TokenFBController.SetTotalTokensNum(flBD.NumTokenBar);
            flTL.TokenFBController.SetTokenBarValue(flBD.NumInitialTokens);
            SetBlockSummaryString();
            flTL.runningAcc.Clear();
            
        });
        AssignBlockData();
    }
    public T GetCurrentBlockDef<T>() where T : BlockDef
    {
        return (T)CurrentBlockDef;
    }

    private void ResetBlockVariables()
    {
        flTL.SearchDurationsList.Clear();
        flTL.AverageSearchDuration_InBlock = 0;
        flTL.NumErrors_InBlock = 0;
        flTL.NumCorrect_InBlock = 0;
        flTL.NumRewardGiven_InBlock = 0;
        flTL.NumTokenBarFull_InBlock = 0;
        flTL.TouchDurationError_InBlock = 0;
    }

    public void SetBlockSummaryString()
    {
        BlockSummaryString.Clear();
        
        BlockSummaryString.AppendLine("\nBlock Num: " + (flTL.BlockCount + 1) +
                                      "\nTrial Num: " + (flTL.TrialCount_InBlock + 1) +
                                      "\n" + 
                                      "\nAccuracy: " + String.Format("{0:0.000}", flTL.Accuracy_InBlock) +  
                                      "\n" + 
                                      "\nAvg Search Duration: " + String.Format("{0:0.000}", flTL.AverageSearchDuration_InBlock) +
                                      "\n" + 
                                      "\nNum Touch Duration Error: " + flTL.TouchDurationError_InBlock + 
                                      "\n" +
                                      "\nNum Reward Given: " + flTL.NumRewardGiven_InBlock + 
                                      "\nNum Token Bar Filled: " + flTL.NumTokenBarFull_InBlock +
                                      "\nTotal Tokens Collected: " + flTL.TotalTokensCollected_InBlock);
    }

    public void AssignBlockData()
    {
        BlockData.AddDatum("Block Accuracy", ()=> (float)flTL.Accuracy_InBlock);
        BlockData.AddDatum("Avg Search Duration", ()=> flTL.AverageSearchDuration_InBlock);
        BlockData.AddDatum("Num Touch Duration Error", ()=> flTL.TouchDurationError_InBlock);
        BlockData.AddDatum("Num Reward Given", ()=> flTL.NumRewardGiven_InBlock);
        BlockData.AddDatum("Num Token Bar Filled", ()=> flTL.NumTokenBarFull_InBlock);
        BlockData.AddDatum("Total Tokens Collected", ()=> flTL.TotalTokensCollected_InBlock);
    }
}