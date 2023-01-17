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
            //if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "UsingRewardPump"))
               // vsTL.usingRewardPump = (bool)SessionSettings.Get(TaskName + "_TaskSettings", "UsingRewardPump");
            //else Debug.LogError("Using Reward Pump setting not defined in the TaskDef");
        }
        else
        {
            Debug.LogError("TaskDef is not in config folder");
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
            //vsTL.NumTokenBar = vsBD.NumTokenBar;
            vsTL.TokenFBController.SetTotalTokensNum(vsBD.NumTokenBar);
            vsTL.TokenFBController.SetTokenBarValue(vsBD.NumInitialTokens);
//            vsTL.TokenFBController.enabled = false;
        });
        
        RunBlock.AddUpdateMethod(() =>
        {
            BlockSummaryString.Clear();
            BlockSummaryString.AppendLine("Block Num: " + (vsTL.BlockCount + 1) + "\nTrial Count: " + (vsTL.TrialCount_InBlock + 1) + 
                                          "\nNum Reward Given: " + vsTL.numReward + "\nNum Token Bar Filled: " + vsTL.NumTokenBarFull + 
                                          "\nTotalTokensCollected: " + vsTL.TotalTokensCollected);
          //  "\nTotal Errors: " + vsTL.totalErrors_InBlock + "\nError Type: " + vsTL.errorType_InBlockString + "\nPerformance: " + vsTL.accuracyLog_InBlock;
        });
    }
    public T GetCurrentBlockDef<T>() where T : BlockDef
    {
        return (T)CurrentBlockDef;
    }
    /*public void CalculateBlockSummaryString()
    {
        ClearStrings();

        if (BlockCount > 0)
        {
            BlockAveragesString = "<b>Block Averages " + $"({BlockCount} block);" + "</b>" +
                                  "\nAvg Correct: " + AvgNumCorrect.ToString("0.00") +
                                  "\nAvg TbCompletions: " + AvgNumTbCompletions.ToString("0.00") +
                                  "\nAvg TimeToChoice: " + AvgTimeToChoice.ToString("0.00") + "s" +
                                  "\nAvg TimeToCompletion: " + AvgTimeToCompletion.ToString("0.00") + "s" +
                                  "\nAvg Rewards: " + AvgNumRewards.ToString("0.00") +
                                  "\nStandard Deviation: " + StanDev.ToString("0.00") +
                                  "\n";
        }

        CurrentBlockString = "<b>Block " + "(" + currentBlock.BlockName + "):" + "</b>" +
                             "\nCorrect: " + trialLevel.NumCorrect_Block +
                             "\nTbCompletions: " + trialLevel.NumTbCompletions_Block +
                             "\nAvgTimeToChoice: " + trialLevel.AvgTimeToChoice_Block.ToString("0.00") + "s" +
                             "\nTimeToCompletion: " + trialLevel.TimeToCompletion_Block.ToString("0.00") + "s" +
                             "\nRewards: " + trialLevel.NumRewards_Block;

        if (BlockCount > 0)
        {
            CurrentBlockString += "\n";
            BlockSummaryString.AppendLine(BlockAveragesString.ToString());
        }

        BlockSummaryString.AppendLine(CurrentBlockString.ToString());

        if(PreviousBlocksString.Length > 0)
            BlockSummaryString.AppendLine(PreviousBlocksString.ToString());
    }
    void ClearStrings()
    {
        BlockAveragesString = "";
        CurrentBlockString = "";
        BlockSummaryString.Clear();
    }
*/
}