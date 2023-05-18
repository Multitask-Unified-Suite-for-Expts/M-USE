using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using USE_ExperimentTemplate_Block;
using USE_Settings;
using USE_StimulusManagement;
using USE_ExperimentTemplate_Task;
using FeatureUncertaintyWM_Namespace;

public class FeatureUncertaintyWM_TaskLevel : ControlLevel_Task_Template
{
    FeatureUncertaintyWM_BlockDef fuWMBD => GetCurrentBlockDef<FeatureUncertaintyWM_BlockDef>();
    FeatureUncertaintyWM_TrialLevel fuWMTL;
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
        fuWMTL = (FeatureUncertaintyWM_TrialLevel)TrialLevel;

        SetSettings();
        AssignBlockData();

        RunBlock.AddInitializationMethod(() =>
        {
            fuWMTL.ContextName = fuWMBD.ContextName;
            Debug.Log(ContextExternalFilePath);
            Debug.Log(fuWMTL.ContextName);
            RenderSettings.skybox = CreateSkybox(fuWMTL.GetContextNestedFilePath(ContextExternalFilePath, fuWMTL.ContextName), UseDefaultConfigs);
            EventCodeManager.SendCodeNextFrame(SessionEventCodes["ContextOn"]); fuWMTL.ResetBlockVariables();
            fuWMTL.TokenFBController.SetTotalTokensNum(fuWMBD.NumTokenBar);
            fuWMTL.TokenFBController.SetTokenBarValue(fuWMBD.NumInitialTokens);
            SetBlockSummaryString();
        });
    }

    public void SetSettings()
    {
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ContextExternalFilePath"))
            fuWMTL.ContextExternalFilePath = (String)SessionSettings.Get(TaskName + "_TaskSettings", "ContextExternalFilePath");
        else fuWMTL.ContextExternalFilePath = ContextExternalFilePath;
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "StartButtonPosition"))
            fuWMTL.StartButtonPosition = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "StartButtonPosition");
        else Debug.LogError("Start Button Position settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "StartButtonScale"))
            fuWMTL.StartButtonScale = (float)SessionSettings.Get(TaskName + "_TaskSettings", "StartButtonScale");
        else Debug.LogError("Start Button Scale settings not defined in the TaskDef");

        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "StimFacingCamera"))
            fuWMTL.StimFacingCamera = (bool)SessionSettings.Get(TaskName + "_TaskSettings", "StimFacingCamera");
        else Debug.LogError("Stim Facing Camera setting not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ShadowType"))
            fuWMTL.ShadowType = (string)SessionSettings.Get(TaskName + "_TaskSettings", "ShadowType");
        else Debug.LogError("Shadow Type setting not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "NeutralITI"))
            fuWMTL.NeutralITI = (bool)SessionSettings.Get(TaskName + "_TaskSettings", "NeutralITI");
        else Debug.LogError("Neutral ITI setting not defined in the TaskDef");

        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "TouchFeedbackDuration"))
            fuWMTL.TouchFeedbackDuration = (float)SessionSettings.Get(TaskName + "_TaskSettings", "TouchFeedbackDuration");
        else
            fuWMTL.TouchFeedbackDuration = .3f;

        if (SessionSettings.SettingExists("Session", "MacMainDisplayBuild"))
            fuWMTL.MacMainDisplayBuild = (bool)SessionSettings.Get("Session", "MacMainDisplayBuild");
        else
            fuWMTL.MacMainDisplayBuild = false;
    }

    public void SetBlockSummaryString()
    {
        BlockSummaryString.Clear();
        float avgBlockSearchDuration = 0;
        if (fuWMTL.SearchDurations_InBlock.Count != 0)
            avgBlockSearchDuration = fuWMTL.SearchDurations_InBlock.Average();
        BlockSummaryString.AppendLine("Accuracy: " + String.Format("{0:0.000}", fuWMTL.Accuracy_InBlock) +
                                      "\n" +
                                      "\nAvg Search Duration: " + String.Format("{0:0.000}", avgBlockSearchDuration) +
                                      "\n" +
                                      "\nNum Reward Given: " + fuWMTL.NumRewardPulses_InBlock +
                                      "\nNum Token Bar Filled: " + fuWMTL.NumTokenBarFull_InBlock +
                                      "\nTotal Tokens Collected: " + fuWMTL.TotalTokensCollected_InBlock);
    }
    public override void SetTaskSummaryString()
    {
        float avgTaskSearchDuration = 0;
        if (SearchDurations_InTask.Count > 0)
            avgTaskSearchDuration = (float)Math.Round(SearchDurations_InTask.Average(), 2);
        if (fuWMTL.TrialCount_InTask != 0)
        {
            CurrentTaskSummaryString.Clear();
            CurrentTaskSummaryString.Append($"\n<b>{ConfigName}</b>" +
                                            $"\n<b># Trials:</b> {fuWMTL.TrialCount_InTask} ({(Math.Round(decimal.Divide(NumAborted_InTask, (fuWMTL.TrialCount_InTask)), 2)) * 100}% aborted)" +
                                            $"\t<b># Blocks:</b> {BlockCount}" +
                                            $"\t<b># Reward Pulses:</b> {NumRewardPulses_InTask}" +
                                            $"\nAccuracy: {(Math.Round(decimal.Divide(NumCorrect_InTask, (fuWMTL.TrialCount_InTask)), 2)) * 100}%" +
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
        BlockData.AddDatum("Block Accuracy", () => fuWMTL.Accuracy_InBlock);
        BlockData.AddDatum("Avg Search Duration", () => fuWMTL.AverageSearchDuration_InBlock);
        BlockData.AddDatum("Num Reward Given", () => fuWMTL.NumRewardPulses_InBlock);
        BlockData.AddDatum("Num Token Bar Filled", () => fuWMTL.NumTokenBarFull_InBlock);
        BlockData.AddDatum("Total Tokens Collected", () => fuWMTL.TotalTokensCollected_InBlock);
    }


}