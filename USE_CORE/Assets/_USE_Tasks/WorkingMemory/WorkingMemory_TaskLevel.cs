using System;
using System.IO;
using UnityEngine;
using USE_ExperimentTemplate_Block;
using USE_ExperimentTemplate_Task;
using USE_Settings;
using WorkingMemory_Namespace;

public class WorkingMemory_TaskLevel : ControlLevel_Task_Template
{
    WorkingMemory_BlockDef wmBD => GetCurrentBlockDef<WorkingMemory_BlockDef>();
    WorkingMemory_TrialLevel wmTL;
    public override void DefineControlLevel()
    {
        wmTL = (WorkingMemory_TrialLevel)TrialLevel;

        SetSettings();

        AssignBlockData();

        SetupTask.AddInitializationMethod(() =>
        {
            //HARD CODED TO MINIMIZE EMPTY SKYBOX DURATION, CAN'T ACCESS TRIAL DEF YET & CONTEXT NOT IN BLOCK DEF
            RenderSettings.skybox = CreateSkybox(ContextExternalFilePath + Path.DirectorySeparatorChar + "Ice.png");
        });

        RunBlock.AddInitializationMethod(() =>
        {
            RenderSettings.skybox = CreateSkybox(ContextExternalFilePath + Path.DirectorySeparatorChar + "Ice.png");
            wmTL.ResetBlockVariables();
            wmTL.TokenFBController.SetTotalTokensNum(wmBD.NumTokenBar);
            wmTL.TokenFBController.SetTokenBarValue(wmBD.NumInitialTokens);
            SetBlockSummaryString();
        });
    }

    public void SetSettings()
    {
        string TaskName = "WorkingMemory";
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ContextExternalFilePath"))
            wmTL.ContextExternalFilePath = (String)SessionSettings.Get(TaskName + "_TaskSettings", "ContextExternalFilePath");
        else wmTL.ContextExternalFilePath = ContextExternalFilePath;
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonPosition"))
            wmTL.ButtonPosition = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonPosition");
        else Debug.LogError("Start Button Position settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonScale"))
            wmTL.ButtonScale = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonScale");
        else Debug.LogError("Start Button Scale settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "StimFacingCamera"))
            wmTL.StimFacingCamera = (bool)SessionSettings.Get(TaskName + "_TaskSettings", "StimFacingCamera");
        else Debug.LogError("Stim Facing Camera setting not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ShadowType"))
            wmTL.ShadowType = (string)SessionSettings.Get(TaskName + "_TaskSettings", "ShadowType");
        else Debug.LogError("Shadow Type setting not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "NeutralITI"))
            wmTL.NeutralITI = (bool)SessionSettings.Get(TaskName + "_TaskSettings", "NeutralITI");
        else Debug.LogError("Neutral ITI setting not defined in the TaskDef");
       

    }    

    public void SetBlockSummaryString()
    {
        BlockSummaryString.Clear();
        
        BlockSummaryString.AppendLine("\nBlock Num: " + (wmTL.BlockCount + 1) +
                                      "\nTrial Num: " + (wmTL.TrialCount_InBlock + 1) +
                                      "\n" + 
                                      "\n Accuracy: " + String.Format("{0:0.000}", wmTL.Accuracy_InBlock) +  
                                      "\n" + 
                                      "\nAvg Search Duration: " + String.Format("{0:0.000}", wmTL.AverageSearchDuration_InBlock) +
                                      "\n" + 
                                      "\nNum Touch Duration Error: " + wmTL.TouchDurationError_InBlock + 
                                      "\n" +
                                      "\nNum Reward Given: " + wmTL.NumRewardGiven_InBlock + 
                                      "\nNum Token Bar Filled: " + wmTL.NumTokenBarFull_InBlock +
                                      "\nTotal Tokens Collected: " + wmTL.TotalTokensCollected_InBlock);
    }
    public void AssignBlockData()
    {
        BlockData.AddDatum("Block Accuracy", ()=> wmTL.Accuracy_InBlock);
        BlockData.AddDatum("Avg Search Duration", ()=> wmTL.AverageSearchDuration_InBlock);
        BlockData.AddDatum("Num Touch Duration Error", ()=> wmTL.TouchDurationError_InBlock);
        BlockData.AddDatum("Num Reward Given", ()=> wmTL.NumRewardGiven_InBlock);
        BlockData.AddDatum("Num Token Bar Filled", ()=> wmTL.NumTokenBarFull_InBlock);
        BlockData.AddDatum("Total Tokens Collected", ()=> wmTL.TotalTokensCollected_InBlock);
    }
}