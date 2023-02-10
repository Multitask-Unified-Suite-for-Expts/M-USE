using VisualSearch_Namespace;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using UnityEngine;
using USE_Settings;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Block;

public class VisualSearch_TaskLevel : ControlLevel_Task_Template
{
    [HideInInspector] public int TouchDurationError_InTask = 0;
    [HideInInspector] public int NumRewardPulses_InTask = 0;
    [HideInInspector] public int NumTokenBarFull_InTask = 0;
    [HideInInspector] public int TotalTokensCollected_InTask = 0;

    [HideInInspector] public string CurrentBlockString;
    [HideInInspector] public StringBuilder PreviousBlocksString;
    [HideInInspector] public int BlockStringsAdded = 0;
    VisualSearch_BlockDef vsBD => GetCurrentBlockDef<VisualSearch_BlockDef>();
    VisualSearch_TrialDef vsTD;
    //private VisualSearch_TrialDef vsTD => GetCurrentTrialDef<VisualSearch_TrialDef>();
    VisualSearch_TrialLevel vsTL;
    public override void DefineControlLevel()
    {
        vsTL = (VisualSearch_TrialLevel)TrialLevel;
        //vsTD = (VisualSearch_TrialDef)vsTL.GetCurrentTrialDef<VisualSearch_TrialDef>();
        SetSettings();
        CurrentBlockString = "";
        PreviousBlocksString = new StringBuilder();
        
        RunBlock.AddInitializationMethod(() =>
        {
            //Hard coded because trial level variable isn't available yet
            RenderSettings.skybox = CreateSkybox(ContextExternalFilePath + Path.DirectorySeparatorChar +  "Grass");
            ResetBlockVariables();
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["ContextOn"]);
            
            //Set the Initial Token Values for the Block
            vsTL.TokenFBController.SetTotalTokensNum(vsBD.NumTokenBar);
            vsTL.TokenFBController.SetTokenBarValue(vsBD.NumInitialTokens);
            SetBlockSummaryString();
        });
        BlockFeedback.AddInitializationMethod(() =>
        {
            if (BlockStringsAdded > 0)
                CurrentBlockString += "\n";
            BlockStringsAdded++;
            PreviousBlocksString.Insert(0, CurrentBlockString);

            TouchDurationError_InTask += vsTL.TouchDurationError_InBlock;
            NumRewardPulses_InTask += vsTL.NumRewardPulses_InBlock;
            NumTokenBarFull_InTask += vsTL.NumTokenBarFull_InBlock;
            TotalTokensCollected_InTask += vsTL.TotalTokensCollected_InBlock;
        });
        AssignBlockData();
    }
    private void ResetBlockVariables()
    {
        vsTL.SearchDurationsList.Clear();
        vsTL.AverageSearchDuration_InBlock = 0;
        vsTL.NumErrors_InBlock = 0;
        vsTL.NumCorrect_InBlock = 0;
        vsTL.NumRewardPulses_InBlock = 0;
        vsTL.NumTokenBarFull_InBlock = 0;
        vsTL.TouchDurationError_InBlock = 0;
        vsTL.TotalTokensCollected_InBlock = 0;
        vsTL.Accuracy_InBlock = 0;
    }
    public override OrderedDictionary GetSummaryData()
    {
        OrderedDictionary data = new OrderedDictionary();

        data["Touch Duration Error"] = TouchDurationError_InTask;
        data["Reward Pulses"] = NumRewardPulses_InTask;
        data["Token Bar Full"] = NumTokenBarFull_InTask;
        data["Total Tokens Collected"] = TotalTokensCollected_InTask;
        return data;
    }
    public void SetBlockSummaryString()
    {
        ClearStrings();
        BlockSummaryString.AppendLine("<b>Block Num: " + (vsTL.BlockCount + 1) + "</b>" +
                                      "\nTrial Num: " + (vsTL.TrialCount_InBlock + 1) +
                                      "\n" + 
                                      "\nAccuracy: " + String.Format("{0:0.00}", vsTL.Accuracy_InBlock) +  
                                      "\n" + 
                                      "\nAvg Search Duration: " + String.Format("{0:0.00}", vsTL.AverageSearchDuration_InBlock) +
                                      "\n" + 
                                      "\nNum Touch Duration Error: " + vsTL.TouchDurationError_InBlock + 
                                      "\n" +
                                      "\nNum Reward Given: " + vsTL.NumRewardPulses_InBlock + 
                                      "\nNum Token Bar Filled: " + vsTL.NumTokenBarFull_InBlock +
                                      "\nTotal Tokens Collected: " + vsTL.TotalTokensCollected_InBlock);
        BlockSummaryString.AppendLine(CurrentBlockString).ToString();
        if (PreviousBlocksString.Length > 0)
            BlockSummaryString.AppendLine(PreviousBlocksString.ToString());
    }


    private void SetSettings()
    {
        string TaskName = "VisualSearch";
        
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ContextExternalFilePath"))
            vsTL.ContextExternalFilePath = (String)SessionSettings.Get(TaskName + "_TaskSettings", "ContextExternalFilePath");
        else vsTL.ContextExternalFilePath = ContextExternalFilePath;
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonPosition"))
            vsTL.ButtonPosition = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonPosition");
        else Debug.LogError("Start Button Position settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonScale"))
            vsTL.ButtonScale = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonScale");
        else Debug.LogError("Start Button Scale settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "FBSquarePosition"))
            vsTL.FBSquarePosition = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "FBSquarePosition");
        else Debug.LogError("FB Square Position settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "FBSquareScale"))
            vsTL.FBSquareScale = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "FBSquareScale");
        else Debug.LogError("FB Square Scale settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "StimFacingCamera"))
            vsTL.StimFacingCamera = (bool)SessionSettings.Get(TaskName + "_TaskSettings", "StimFacingCamera");
        else Debug.LogError("Stim Facing Camera setting not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ShadowType"))
            vsTL.ShadowType = (string)SessionSettings.Get(TaskName + "_TaskSettings", "ShadowType");
        else Debug.LogError("Shadow Type setting not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "NeutralITI"))
            vsTL.NeutralITI = (bool)SessionSettings.Get(TaskName + "_TaskSettings", "NeutralITI");
        else Debug.LogError("Neutral ITI setting not defined in the TaskDef");

    }

    public void AssignBlockData()
    {
        BlockData.AddDatum("Block Accuracy", ()=> vsTL.Accuracy_InBlock);
        BlockData.AddDatum("Avg Search Duration", ()=> vsTL.AverageSearchDuration_InBlock);
        BlockData.AddDatum("Num Touch Duration Error", ()=> vsTL.TouchDurationError_InBlock);
        BlockData.AddDatum("Num Reward Given", ()=> vsTL.NumRewardPulses_InBlock);
        BlockData.AddDatum("Num Token Bar Filled", ()=> vsTL.NumTokenBarFull_InBlock);
        BlockData.AddDatum("Total Tokens Collected", ()=> vsTL.TotalTokensCollected_InBlock);
    }
    public void ClearStrings()
    {
        CurrentBlockString = "";
        BlockSummaryString.Clear();
    }
}