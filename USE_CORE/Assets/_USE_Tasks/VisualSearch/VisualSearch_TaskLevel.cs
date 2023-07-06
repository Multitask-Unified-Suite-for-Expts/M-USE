using VisualSearch_Namespace;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using UnityEngine;
using USE_Settings;
using USE_ExperimentTemplate_Task;


public class VisualSearch_TaskLevel : ControlLevel_Task_Template
{
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
            vsTL.ContextName = vsBD.ContextName;

            string contextFilePath;
            if (SessionValues.WebBuild)
                contextFilePath = $"{ContextExternalFilePath}/{vsBD.ContextName}";
            else
                contextFilePath = vsTL.GetContextNestedFilePath(ContextExternalFilePath, vsBD.ContextName, "LinearDark");

            RenderSettings.skybox = CreateSkybox(contextFilePath);

            EventCodeManager.SendCodeNextFrame(SessionEventCodes["ContextOn"]);

            vsTL.TokensWithStimOn = vsBD.TokensWithStimOn;
            vsTL.ResetBlockVariables();
            //Set the Initial Token Values for the Block
            vsTL.TokenFBController.SetTotalTokensNum(vsBD.NumTokenBar);
            vsTL.TokenFBController.SetTokenBarValue(vsBD.NumInitialTokens);
            SetBlockSummaryString();
        });
        BlockFeedback.AddInitializationMethod(() =>
        {
            if(!SessionValues.WebBuild)
            {
                if (BlockStringsAdded > 0)
                    CurrentBlockString += "\n";
                BlockStringsAdded++;
                PreviousBlocksString.Insert(0, CurrentBlockString);
            }
            vsTL.SearchDurationsList.Clear();
        });
        AssignBlockData();
    }

    public override OrderedDictionary GetTaskSummaryData()
    {
        OrderedDictionary data = new OrderedDictionary
        {
            ["Reward Pulses"] = NumRewardPulses_InTask,
            ["Token Bar Full"] = NumTokenBarFull_InTask,
            ["Total Tokens Collected"] = TotalTokensCollected_InTask
        };
        if (SearchDurationsList_InTask.Count > 0)
            data["Average Search Duration"] = SearchDurationsList_InTask.Average();
        if(vsTL.TrialCount_InTask != 0)
            data["Accuracy"] = decimal.Divide(NumCorrect_InTask, (vsTL.TrialCount_InTask));
        
        return data;
    }
    public void SetBlockSummaryString()
    {
        ClearStrings();
        BlockSummaryString.AppendLine("Accuracy: " + String.Format("{0:0.00}", (float)vsTL.Accuracy_InBlock) +  
                                      "\n" + 
                                      "\nAvg Search Duration: " + String.Format("{0:0.00}", vsTL.AverageSearchDuration_InBlock) +
                                      "\n" + 
                                      "\nNum Aborted Trials: " + + vsTL.AbortedTrials_InBlock + 
                                      "\n"+
                                      "\nNum Reward Given: " + vsTL.NumRewardPulses_InBlock + 
                                      "\nNum Token Bar Filled: " + vsTL.NumTokenBarFull_InBlock +
                                      "\nTotal Tokens Collected: " + vsTL.TotalTokensCollected_InBlock);
        BlockSummaryString.AppendLine(CurrentBlockString);
        /*if (PreviousBlocksString.Length > 0)
            BlockSummaryString.AppendLine(PreviousBlocksString.ToString());*/
    }

    public override void SetTaskSummaryString()
    {
        if (SearchDurationsList_InTask.Count > 0)
            avgSearchDuration = Math.Round(SearchDurationsList_InTask.Average(), 2);
        if (vsTL.TrialCount_InTask != 0)
        {
            CurrentTaskSummaryString.Clear();
            CurrentTaskSummaryString.Append($"\n<b>{ConfigName}</b>" + 
                                                    $"\n<b># Trials:</b> {vsTL.TrialCount_InTask} ({(Math.Round(decimal.Divide(AbortedTrials_InTask,(vsTL.TrialCount_InTask)),2))*100}% aborted)" + 
                                                    $"\t<b># Blocks:</b> {BlockCount}" + 
                                                    $"\t<b># Reward Pulses:</b> {NumRewardPulses_InTask}" +
                                                    $"\nAccuracy: {(Math.Round(decimal.Divide(NumCorrect_InTask,(vsTL.TrialCount_InTask)),2))*100}%" + 
                                                    $"\tAvg Search Duration: {avgSearchDuration}" +
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

        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "StartButtonPosition"))
            vsTL.StartButtonPosition = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "StartButtonPosition");
        else
            vsTL.StartButtonPosition = new Vector3(0, 0, 0);
        if (SessionSettings.SettingExists(TaskName +"_TaskSettings", "StartButtonScale"))
            vsTL.StartButtonScale = (float)SessionSettings.Get(TaskName + "_TaskSettings", "StartButtonScale");
        else
            vsTL.StartButtonScale = 120f;
        
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "StimFacingCamera"))
            vsTL.StimFacingCamera = (bool)SessionSettings.Get(TaskName + "_TaskSettings", "StimFacingCamera");
        else Debug.LogError("Stim Facing Camera setting not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ShadowType"))
            vsTL.ShadowType = (string)SessionSettings.Get(TaskName + "_TaskSettings", "ShadowType");
        else Debug.LogError("Shadow Type setting not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "NeutralITI"))
            vsTL.NeutralITI = (bool)SessionSettings.Get(TaskName + "_TaskSettings", "NeutralITI");
        else Debug.LogError("Neutral ITI setting not defined in the TaskDef");

        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "TouchFeedbackDuration"))
            vsTL.TouchFeedbackDuration = (float)SessionSettings.Get(TaskName + "_TaskSettings", "TouchFeedbackDuration");
        else
            vsTL.TouchFeedbackDuration = .3f;

        if (SessionSettings.SettingExists("Session", "MacMainDisplayBuild"))
            vsTL.MacMainDisplayBuild = (bool)SessionSettings.Get("Session", "MacMainDisplayBuild");
        else
            vsTL.MacMainDisplayBuild = false;
    }

    public void AssignBlockData()
    {
        BlockData.AddDatum("Block Accuracy", ()=> (float)vsTL.Accuracy_InBlock);
        BlockData.AddDatum("Avg Search Duration", ()=> vsTL.AverageSearchDuration_InBlock);
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
        NumRewardPulses_InTask = 0;
        NumTokenBarFull_InTask = 0;
        TotalTokensCollected_InTask = 0;
        AbortedTrials_InTask = 0;
        SearchDurationsList_InTask.Clear();
    }
}