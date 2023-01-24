using System;
using UnityEngine;
using USE_ExperimentTemplate_Task;
using USE_Settings;
using WorkingMemory_Namespace;

public class WorkingMemory_TaskLevel : ControlLevel_Task_Template
{
    WorkingMemory_BlockDef flBD => GetCurrentBlockDef<WorkingMemory_BlockDef>();
    WorkingMemory_TrialLevel wmTL;
    public override void DefineControlLevel()
    {
        wmTL = (WorkingMemory_TrialLevel)TrialLevel;
        string TaskName = "WorkingMemory";
        if (SessionSettings.SettingClassExists(TaskName + "_TaskSettings"))
        {
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ContextExternalFilePath"))
                wmTL.ContextExternalFilePath = (String)SessionSettings.Get(TaskName + "_TaskSettings", "ContextExternalFilePath");
            else wmTL.ContextExternalFilePath = ContextExternalFilePath;
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonPosition"))
                wmTL.ButtonPosition = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonPosition");
            else Debug.LogError("Start Button Position settings not defined in the TaskDef");
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonScale"))
                wmTL.ButtonScale = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonScale");
            else Debug.LogError("Start Button Scale settings not defined in the TaskDef");
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "FBSquarePosition"))
                wmTL.FBSquarePosition = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "FBSquarePosition");
            else Debug.LogError("FB Square Position settings not defined in the TaskDef");
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "FBSquareScale"))
                wmTL.FBSquareScale = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "FBSquareScale");
            else Debug.LogError("FB Square Scale settings not defined in the TaskDef");
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "StimFacingCamera"))
                wmTL.StimFacingCamera = (bool)SessionSettings.Get(TaskName + "_TaskSettings", "StimFacingCamera");
            else Debug.LogError("Stim Facing Camera setting not defined in the TaskDef");
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ShadowType"))
                wmTL.ShadowType = (string)SessionSettings.Get(TaskName + "_TaskSettings", "ShadowType");
            else Debug.LogError("Shadow Type setting not defined in the TaskDef");
           
        }
        else
        {
            Debug.Log("[ERROR] TaskDef is not in config folder");
        }
    }
    public void SetBlockSummaryString()
    {
        BlockSummaryString.Clear();
        
        BlockSummaryString.AppendLine("\nBlock Num: " + (wmTL.BlockCount + 1) +
                                      "\nTrial Num: " + (wmTL.TrialCount_InBlock + 1) +
                                      "\n" /*+ 
                                      "\nAccuracy: " + String.Format("{0:0.000}", wmTL.Accuracy_InBlock) +  
                                      "\n" + 
                                      "\nAvg Search Duration: " + String.Format("{0:0.000}", wmTL.AverageSearchDuration_InBlock) +
                                      "\n" + 
                                      "\nNum Touch Duration Error: " + wmTL.TouchDurationError_InBlock + 
                                      "\nNum Reward Given: " + wmTL.NumRewardGiven_InBlock + 
                                      "\nNum Token Bar Filled: " + wmTL.NumTokenBarFull_InBlock +
                                      "\nTotal Tokens Collected: " + wmTL.TotalTokensCollected_InBlock*/);
    }

}