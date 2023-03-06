using VisualSearch_Namespace;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
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
    [HideInInspector] public int AbortedTrials_InTask = 0;
    [HideInInspector] public int NumCorrect_InTask = 0;
    [HideInInspector] public int NumErrors_InTask = 0;
    [HideInInspector] public List<float> SearchDurationsList_InTask;
    private double avgSearchDuration = 0;
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
        
        Add_ControlLevel_InitializationMethod(() =>
        {
            ResetTaskVariables();
        });
        
        RunBlock.AddInitializationMethod(() =>
        {
            //Hard coded because trial level variable isn't available yet
            RenderSettings.skybox = CreateSkybox(ContextExternalFilePath + Path.DirectorySeparatorChar +  "Grass");
            
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["ContextOn"]);

            vsTL.TokensWithStimOn = vsBD.TokensWithStimOn;
            vsTL.ResetBlockVariables();
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
            
            TouchDurationError_InTask += vsTL.TouchDurationError_InBlock; //Not actively updating on session panel, 
                                                                            //ok to calculate after block end
        });
        AssignBlockData();
    }

    public override OrderedDictionary GetSummaryData()
    {
        OrderedDictionary data = new OrderedDictionary();

        data["Touch Duration Error"] = TouchDurationError_InTask;
        data["Reward Pulses"] = NumRewardPulses_InTask;
        data["Token Bar Full"] = NumTokenBarFull_InTask;
        data["Total Tokens Collected"] = TotalTokensCollected_InTask;
        if(SearchDurationsList_InTask.Count > 0)
            data["Average Search Duration"] = SearchDurationsList_InTask.Average();
        if(vsTL.TrialCount_InTask != 0)
            data["Accuracy"] = decimal.Divide(NumCorrect_InTask, (vsTL.TrialCount_InTask));
        
        return data;
    }
    public void SetBlockSummaryString()
    {
        ClearStrings();
        BlockSummaryString.AppendLine("<b>Block Num: " + (vsTL.BlockCount + 1) + "</b>" +
                                      "<b>\nTrial Num: </b>" + (vsTL.TrialCount_InBlock + 1) +
                                      "\n" + 
                                      "\nAccuracy: " + String.Format("{0:0.00}", (float)vsTL.Accuracy_InBlock) +  
                                      "\n" + 
                                      "\nAvg Search Duration: " + String.Format("{0:0.00}", vsTL.AverageSearchDuration_InBlock) +
                                      "\n" + 
                                      "\nNum Touch Duration Error: " + vsTL.TouchDurationError_InBlock + 
                                      "\nNum Aborted Trials" + + vsTL.AbortedTrials_InBlock + 
                                      "\n"+
                                      "\nNum Reward Given: " + vsTL.NumRewardPulses_InBlock + 
                                      "\nNum Token Bar Filled: " + vsTL.NumTokenBarFull_InBlock +
                                      "\nTotal Tokens Collected: " + vsTL.TotalTokensCollected_InBlock);
        BlockSummaryString.AppendLine(CurrentBlockString);
        if (PreviousBlocksString.Length > 0)
            BlockSummaryString.AppendLine(PreviousBlocksString.ToString());
    }

    public override void SetTaskSummaryString()
    {
        if (SearchDurationsList_InTask.Count > 0)
            avgSearchDuration = Math.Round(SearchDurationsList_InTask.Average(), 2);
        if (vsTL.TrialCount_InTask != 0)
        {
            CurrentTaskSummaryString.Clear();
            CurrentTaskSummaryString.Append($"\n<b>{ConfigName}</b>" + 
                                                    $"\n# Trials: {vsTL.TrialCount_InTask} ({(Math.Round(decimal.Divide(AbortedTrials_InTask,(vsTL.TrialCount_InTask)),2))*100}% aborted)" + 
                                                    $"\n# Blocks: {BlockCount}" + 
                                                    $"\nAccuracy: {(Math.Round(decimal.Divide(NumCorrect_InTask,(vsTL.TrialCount_InTask)),2))*100}%" + 
                                                    $"\nAvg Search Duration: {avgSearchDuration}" +
                                                    $"\n# Reward Pulses: {NumRewardPulses_InTask}" +
                                                    $"\n# Token Bar Filled: {NumTokenBarFull_InTask}" +
                                                    $"\n# Tokens Collected: {TotalTokensCollected_InTask}");
        }
        else
        {
            CurrentTaskSummaryString.Append($"\n<b>{ConfigName}</b>");
        }
            
    }
    
    private void SetSettings()
    {
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ContextExternalFilePath"))
            vsTL.ContextExternalFilePath = (String)SessionSettings.Get(TaskName + "_TaskSettings", "ContextExternalFilePath");
        else vsTL.ContextExternalFilePath = ContextExternalFilePath;

        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonPosition"))
            vsTL.ButtonPosition = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonPosition");
        else
            vsTL.ButtonPosition = new Vector3(0, 0, 0);
        if (SessionSettings.SettingExists(TaskName +"_TaskSettings", "ButtonScale"))
            vsTL.ButtonScale = (float)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonScale");
        else
            vsTL.ButtonScale = 120f;
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
        BlockData.AddDatum("Block Accuracy", ()=> (float)vsTL.Accuracy_InBlock);
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
    public void ResetTaskVariables()
    {
        NumCorrect_InTask = 0;
        NumErrors_InTask = 0;
        TouchDurationError_InTask = 0;
        NumRewardPulses_InTask = 0;
        NumTokenBarFull_InTask = 0;
        TotalTokensCollected_InTask = 0;
        AbortedTrials_InTask = 0;
        SearchDurationsList_InTask.Clear();
    }
}