using FlexLearning_Namespace;
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
using USE_ExperimentTemplate_Trial;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class FlexLearning_TaskLevel : ControlLevel_Task_Template
{
    [HideInInspector] public int TouchDurationError_InTask = 0;
    [HideInInspector] public int NumRewardPulses_InTask = 0;
    [HideInInspector] public int NumTokenBarFull_InTask = 0;
    [HideInInspector] public int TotalTokensCollected_InTask = 0;
    [HideInInspector] public int AbortedTrials_InTask = 0;
    [HideInInspector] public int NumCorrect_InTask = 0;
    [HideInInspector] public int NumErrors_InTask = 0;
    [HideInInspector] public List<float> SearchDurationsList_InTask;
    
    [HideInInspector] public string CurrentBlockString;
    [HideInInspector] public StringBuilder PreviousBlocksString;
    [HideInInspector] public int BlockStringsAdded = 0;
    FlexLearning_BlockDef flBD => GetCurrentBlockDef<FlexLearning_BlockDef>();
    FlexLearning_TrialLevel flTL;
    public override void DefineControlLevel()
    {   
        flTL = (FlexLearning_TrialLevel)TrialLevel;
        SetSettings();
        
        CurrentBlockString = "";
        PreviousBlocksString = new StringBuilder();
        
        Add_ControlLevel_InitializationMethod(() =>
        {
            ResetTaskVariables();
        });

        RunBlock.AddInitializationMethod(() =>
        {
            // Sets Min/Max for the CheckBlockEnd at the TrialLevel
            System.Random rnd = new System.Random();
            int RandomMaxTrials = rnd.Next(flBD.MinMaxTrials[0], flBD.MinMaxTrials[1]);
            flTL.MaxTrials = RandomMaxTrials;
            flTL.MinTrials = flBD.MinMaxTrials[0];
            flTL.TokensWithStimOn = flBD.TokensWithStimOn;
            
            ResetBlockVariables();
            RenderSettings.skybox = CreateSkybox(flTL.GetContextNestedFilePath(flTL.ContextExternalFilePath, flBD.ContextName));
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["ContextOn"]);
            
            //Set the Initial Token Values for the Block
            flTL.TokenFBController.SetTotalTokensNum(flBD.NumTokenBar);
            flTL.TokenFBController.SetTokenBarValue(flBD.NumInitialTokens);
            SetBlockSummaryString();
            
        });
        BlockFeedback.AddInitializationMethod(() =>
        {
            if (BlockStringsAdded > 0)
                CurrentBlockString += "\n";
            BlockStringsAdded++;
            PreviousBlocksString.Insert(0, CurrentBlockString);

            TouchDurationError_InTask += flTL.TouchDurationError_InBlock;
        });
        AssignBlockData();
    }

    private void SetSettings()
    {
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ContextExternalFilePath"))
            flTL.ContextExternalFilePath = (String)SessionSettings.Get(TaskName + "_TaskSettings", "ContextExternalFilePath");
        else flTL.ContextExternalFilePath = ContextExternalFilePath;
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonPosition"))
            flTL.ButtonPosition = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonPosition");
        else
            flTL.ButtonPosition = new Vector3(0, 0, 0);
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonScale"))
           flTL.ButtonScale = (float)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonScale");
        else
            flTL.ButtonScale = 120f;
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "StimFacingCamera"))
            flTL.StimFacingCamera = (bool)SessionSettings.Get(TaskName + "_TaskSettings", "StimFacingCamera");
        else Debug.LogError("Stim Facing Camera setting not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ShadowType"))
            flTL.ShadowType = (string)SessionSettings.Get(TaskName + "_TaskSettings", "ShadowType");
        else Debug.LogError("Shadow Type setting not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "NeutralITI"))
            flTL.NeutralITI = (bool)SessionSettings.Get(TaskName + "_TaskSettings", "NeutralITI");
        else Debug.LogError("Neutral ITI setting not defined in the TaskDef");
    }
    private void ResetBlockVariables()
    {
        flTL.SearchDurationsList.Clear();
        flTL.runningAcc.Clear();
        flTL.Accuracy_InBlock = 0;
        flTL.AverageSearchDuration_InBlock = 0;
        flTL.NumErrors_InBlock = 0;
        flTL.NumCorrect_InBlock = 0;
        flTL.NumRewardPulses_InBlock = 0;
        flTL.NumTokenBarFull_InBlock = 0;
        flTL.TouchDurationError_InBlock = 0;
        flTL.TotalTokensCollected_InBlock = 0;
    }
    public override OrderedDictionary GetSummaryData()
    {
        OrderedDictionary data = new OrderedDictionary();

        data["Touch Duration Error"] = TouchDurationError_InTask;
        data["Reward Pulses"] = NumRewardPulses_InTask;
        data["Token Bar Full"] = NumTokenBarFull_InTask;
        data["Total Tokens Collected"] = TotalTokensCollected_InTask;
        data["Average Search Duration"] = SearchDurationsList_InTask.Average();
        data["Accuracy"] = decimal.Divide(NumCorrect_InTask, (flTL.TrialCount_InTask));
        
        return data;
    }

    public void SetBlockSummaryString()
    {
        ClearStrings();
        
        BlockSummaryString.AppendLine("<b>Block Num: " + (flTL.BlockCount + 1) + "</b>" +
                                      "\n" + 
                                      "<b>\nTrial Num: </b>" + (flTL.TrialCount_InBlock + 1) +
                                      "\n" + 
                                      "\nAccuracy: " + String.Format("{0:0.000}", (float)flTL.Accuracy_InBlock) +  
                                      "\n" + 
                                      "\nAvg Search Duration: " + String.Format("{0:0.000}", flTL.AverageSearchDuration_InBlock) +
                                      "\n" + 
                                      "\nNum Touch Duration Error: " + flTL.TouchDurationError_InBlock + 
                                      "\n" +
                                      "\nNum Reward Given: " + flTL.NumRewardPulses_InBlock + 
                                      "\nNum Token Bar Filled: " + flTL.NumTokenBarFull_InBlock +
                                      "\nTotal Tokens Collected: " + flTL.TotalTokensCollected_InBlock);
        BlockSummaryString.AppendLine(CurrentBlockString).ToString();
        if (PreviousBlocksString.Length > 0)
            BlockSummaryString.AppendLine(PreviousBlocksString.ToString());
    }
    public override void SetTaskSummaryString()
    {
        CurrentTaskSummaryString.Clear();
        if (flTL.TrialCount_InTask != 0)
            CurrentTaskSummaryString.Append($"\n<b>{ConfigName}</b>" + 
                                            $"\n# Trials: {flTL.TrialCount_InTask + 1} ({(Math.Round(decimal.Divide(AbortedTrials_InTask,(flTL.TrialCount_InTask)),2))*100}% aborted)" + 
                                            $"\n#Blocks Completed: {BlockCount}" + 
                                            $"\nAccuracy: {(Math.Round(decimal.Divide(NumCorrect_InTask,(flTL.TrialCount_InTask)),2))*100}%" + 
                                            $"\nAvg Search Duration: {Math.Round(SearchDurationsList_InTask.Average(),2)}" +
                                            $"\n# Reward Pulses: {NumRewardPulses_InTask}" +
                                            $"\n# Token Bar Filled: {NumTokenBarFull_InTask}" +
                                            $"\n# Tokens Collected: {TotalTokensCollected_InTask}");
        else
            CurrentTaskSummaryString.Append($"\n<b>{ConfigName}</b>");
    }

    public void AssignBlockData()
    {
        BlockData.AddDatum("BlockAccuracy", ()=> (float)flTL.Accuracy_InBlock);
        BlockData.AddDatum("AvgSearchDuration", ()=> flTL.AverageSearchDuration_InBlock);
        BlockData.AddDatum("NumTouchDurationError", ()=> flTL.TouchDurationError_InBlock);
        BlockData.AddDatum("NumRewardGiven", ()=> flTL.NumRewardPulses_InBlock);
        BlockData.AddDatum("NumTokenBarFilled", ()=> flTL.NumTokenBarFull_InBlock);
        BlockData.AddDatum("TotalTokensCollected", ()=> flTL.TotalTokensCollected_InBlock);
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