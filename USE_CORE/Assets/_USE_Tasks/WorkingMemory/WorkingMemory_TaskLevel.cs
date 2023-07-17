using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using UnityEngine;
using USE_ExperimentTemplate_Task;
using USE_Settings;
using WorkingMemory_Namespace;

public class WorkingMemory_TaskLevel : ControlLevel_Task_Template
{
    WorkingMemory_BlockDef wmBD => GetCurrentBlockDef<WorkingMemory_BlockDef>();
    WorkingMemory_TrialLevel wmTL;
    public int NumCorrect_InTask = 0;
    public List<float> SearchDurations_InTask = new List<float>();
    public int NumErrors_InTask = 0;
    public int NumRewardPulses_InTask = 0;
    public int NumTokenBarFull_InTask = 0;
    public int TotalTokensCollected_InTask = 0;
    public float Accuracy_InTask = 0;
    public float AverageSearchDuration_InTask = 0;
    public int NumAborted_InTask = 0;
    public override void DefineControlLevel()
    {
        wmTL = (WorkingMemory_TrialLevel)TrialLevel;

        SetSettings();
        AssignBlockData();

        RunBlock.AddInitializationMethod(() =>
        {
            wmTL.ContextName = wmBD.ContextName;

            string contextFilePath;
            if (SessionValues.WebBuild)
                contextFilePath = $"{SessionValues.SessionDef.ContextExternalFilePath}/{wmBD.ContextName}";
            else
                contextFilePath = wmTL.GetContextNestedFilePath(SessionValues.SessionDef.ContextExternalFilePath, wmBD.ContextName, "LinearDark");

            RenderSettings.skybox = CreateSkybox(contextFilePath);

            if (SessionValues.SessionDef.EventCodesActive)
                SessionValues.EventCodeManager.SendCodeNextFrame(SessionValues.SessionEventCodes["ContextOn"]);

            wmTL.ResetBlockVariables();
            wmTL.TokenFBController.SetTotalTokensNum(wmBD.NumTokenBar);
            wmTL.TokenFBController.SetTokenBarValue(wmBD.NumInitialTokens);
            SetBlockSummaryString();
        });
    }

    public void SetSettings()
    {
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ContextExternalFilePath"))
            wmTL.ContextExternalFilePath = (String)SessionSettings.Get(TaskName + "_TaskSettings", "ContextExternalFilePath");
        else wmTL.ContextExternalFilePath = SessionValues.SessionDef.ContextExternalFilePath;
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "StartButtonPosition"))
            wmTL.StartButtonPosition = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "StartButtonPosition");
        else Debug.LogError("Start Button Position settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "StartButtonScale"))
            wmTL.StartButtonScale = (float)SessionSettings.Get(TaskName + "_TaskSettings", "StartButtonScale");
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

        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "TouchFeedbackDuration"))
            wmTL.TouchFeedbackDuration = (float)SessionSettings.Get(TaskName + "_TaskSettings", "TouchFeedbackDuration");
        else
            wmTL.TouchFeedbackDuration = .3f;

        if (SessionSettings.SettingExists("Session", "MacMainDisplayBuild"))
            wmTL.MacMainDisplayBuild = (bool)SessionSettings.Get("Session", "MacMainDisplayBuild");
        else
            wmTL.MacMainDisplayBuild = false;
    }


    public override OrderedDictionary GetBlockResultsData()
    {
        OrderedDictionary data = new OrderedDictionary
        {
            ["Trials Completed"] = wmTL.TrialCount_InBlock + 1,
            ["Trials Correct"] = wmTL.NumCorrect_InBlock,
            ["Errors"] = wmTL.NumErrors_InBlock,
            ["Avg Search Duration"] = wmTL.AverageSearchDuration_InBlock.ToString("0.0") + "s",
        };
        return data;
    }


    public void SetBlockSummaryString()
    {
        BlockSummaryString.Clear();
        float avgBlockSearchDuration = 0;
        if (wmTL.SearchDurations_InBlock.Count != 0)
            avgBlockSearchDuration = wmTL.SearchDurations_InBlock.Average();
        BlockSummaryString.AppendLine("Accuracy: " + String.Format("{0:0.000}", wmTL.Accuracy_InBlock) +  
                                      "\n" + 
                                      "\nAvg Search Duration: " + String.Format("{0:0.000}", avgBlockSearchDuration) +
                                      "\n" +
                                      "\nNum Reward Given: " + wmTL.NumRewardPulses_InBlock + 
                                      "\nNum Token Bar Filled: " + wmTL.NumTokenBarFull_InBlock +
                                      "\nTotal Tokens Collected: " + wmTL.TotalTokensCollected_InBlock);
    }
    public override void SetTaskSummaryString()
    {
        float avgTaskSearchDuration = 0;
        if (SearchDurations_InTask.Count > 0)
            avgTaskSearchDuration = (float)Math.Round(SearchDurations_InTask.Average(), 2);
        if (wmTL.TrialCount_InTask != 0)
        {
            CurrentTaskSummaryString.Clear();
            CurrentTaskSummaryString.Append($"\n<b>{ConfigName}</b>" + 
                                            $"\n<b># Trials:</b> {wmTL.TrialCount_InTask} ({(Math.Round(decimal.Divide(NumAborted_InTask,(wmTL.TrialCount_InTask)),2))*100}% aborted)" + 
                                            $"\t<b># Blocks:</b> {BlockCount}" + 
                                            $"\t<b># Reward Pulses:</b> {NumRewardPulses_InTask}" +
                                            $"\nAccuracy: {(Math.Round(decimal.Divide(NumCorrect_InTask,(wmTL.TrialCount_InTask)),2))*100}%" + 
                                            $"\tAvg Search Duration: {avgTaskSearchDuration}" +
                                            $"\n# Token Bar Filled: {NumTokenBarFull_InTask}" +
                                            $"\n# Tokens Collected: {TotalTokensCollected_InTask}");
        }
        else
        {
            CurrentTaskSummaryString.Append($"\n<b>{ConfigName}</b>");
        }
            
    }
    public void AssignBlockData()
    {
        BlockData.AddDatum("Block Accuracy", ()=> wmTL.Accuracy_InBlock);
        BlockData.AddDatum("Avg Search Duration", ()=> wmTL.AverageSearchDuration_InBlock);
        BlockData.AddDatum("Num Reward Given", ()=> wmTL.NumRewardPulses_InBlock);
        BlockData.AddDatum("Num Token Bar Filled", ()=> wmTL.NumTokenBarFull_InBlock);
        BlockData.AddDatum("Total Tokens Collected", ()=> wmTL.TotalTokensCollected_InBlock);
    }
}