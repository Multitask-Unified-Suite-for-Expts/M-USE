using USE_ExperimentTemplate_Task;
using KeepTrack_Namespace;
using UnityEngine;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Linq;


public class KeepTrack_TaskLevel : ControlLevel_Task_Template
{
    KeepTrack_BlockDef CurrentBlock => GetCurrentBlockDef<KeepTrack_BlockDef>();
    KeepTrack_TrialLevel trialLevel;

    //DATA
    [HideInInspector] public int TrialsCompleted_Task = 0;
    [HideInInspector] public int SuccessfulTargetSelections_Task = 0;
    [HideInInspector] public int UnsuccessfulTargetSelections_Task = 0;
    [HideInInspector] public int TargetSelectionsBeforeFirstAnim_Task = 0;
    [HideInInspector] public int TargetAnimsWithoutSelection_Task = 0;
    [HideInInspector] public int AdditionalTargetSelections_Task = 0;
    [HideInInspector] public int DistractorSelections_Task = 0;
    [HideInInspector] public int DistractorRejections_Task = 0;
    [HideInInspector] public int SliderBarCompletions_Task = 0;


    //OBJECTS LOADED FROM OBJECT CONFIG:
    public KT_Object_ConfigValues[] KT_Objects_ConfigValues;


    public override void DefineControlLevel()
    {
        trialLevel = (KeepTrack_TrialLevel)TrialLevel;
        CurrentBlockString = "";
        DefineBlockData();
        Session.HumanStartPanel.AddTaskDisplayName(TaskName, "Keep Track");
        Session.HumanStartPanel.AddTaskInstructions(TaskName, "Keep your eye on the Target object. Select the object as quickly as you can once it animates (closes its mouth)!");


        RunBlock.AddSpecificInitializationMethod(() =>
        {
            //Grab custom settings from Object Config that are read in:
            KT_Objects_ConfigValues = customSettings.FirstOrDefault(setting => setting.SearchString == "KeepTrack_ObjectsDef").AssignCustomSetting<KT_Object_ConfigValues[]>();

            CurrentBlock.ContextName = CurrentBlock.ContextName.Trim();
            SetSkyBox(CurrentBlock.ContextName);
            trialLevel.ResetBlockVariables();
            SetBlockSummaryString();
        });
    }


    public override List<CustomSettings> DefineCustomSettings()
    {
        customSettings.Add(new CustomSettings(ConfigFolderName + "_ObjectsDef", typeof(KT_Object_ConfigValues), "array", KT_Objects_ConfigValues));
        return customSettings;
    }

    public override void SetBlockSummaryString()
    {
        ClearStrings();

        CurrentBlockString = "\nSuccessful Target Sel: " + trialLevel.SuccessfulTargetSelections_Block +
                             "\nUnsuccessful Target Sel: " + trialLevel.UnsuccessfulTargetSelections_Block +
                             "\nDistractor Sel: " + trialLevel.DistractorSelections_Block +
                             "\nDistractor Rej: " + trialLevel.DistractorRejections_Block +
                             "\nAdditional Target Sel: " + trialLevel.AdditionalTargetSelections_Block +
                             "\nIntervals w/o Sel: " + trialLevel.TargetAnimsWithoutSelection_Block +
                             "\nReward Pulses: " + NumRewardPulses_InBlock;

        CurrentBlockSummaryString.AppendLine(CurrentBlockString).ToString();
    }

    public override void SetTaskSummaryString()
    {
        CurrentTaskSummaryString.Clear();
        base.SetTaskSummaryString();
        CurrentTaskSummaryString.Append($"\t# Successful Target Selections: {SuccessfulTargetSelections_Task}");
    }

    public override OrderedDictionary GetTaskSummaryData()
    {
        OrderedDictionary data = base.GetTaskSummaryData();

        data["Trials Completed"] = TrialsCompleted_Task;
        data["Successful Target Selections"] = SuccessfulTargetSelections_Task;
        data["Unsuccessful Target Selections"] = UnsuccessfulTargetSelections_Task;
        data["Distractor Selections"] = DistractorSelections_Task;
        data["Distractor Rejections"] = DistractorRejections_Task;
        data["Additional Target Selections"] = AdditionalTargetSelections_Task;
        data["Target Selections Before First Anim"] = TargetSelectionsBeforeFirstAnim_Task;
        data["Intervals Without Selections"] = TargetAnimsWithoutSelection_Task;
        data["SliderBar Completions"] = SliderBarCompletions_Task;
        return data;
    }

    public override OrderedDictionary GetTaskResultsData()
    {
        OrderedDictionary data = base.GetTaskResultsData();
        //data["Longest Streak"] = LongestStreak;
        //data["Average Streak"] = GetAvgStreak();
        //data["Trials Correct"] = TrialsCorrect_Task;
        //data["TokenBar Completions"] = TokenBarCompletions_Task;

        return data;
    }


    private void DefineBlockData()
    {
        BlockData.AddDatum("BlockName", () => CurrentBlock.BlockName);
        BlockData.AddDatum("ContextName", () => CurrentBlock.ContextName);

        BlockData.AddDatum("TrialsCompleted", () => trialLevel.TrialCompletions_Block);

        BlockData.AddDatum("SuccessfulTargetSelections", () => trialLevel.SuccessfulTargetSelections_Block);
        BlockData.AddDatum("UnsuccessfulTargetSelections", () => trialLevel.UnsuccessfulTargetSelections_Block);
        BlockData.AddDatum("TargetIntervalsWithoutASelection", () => trialLevel.TargetAnimsWithoutSelection_Block);

        BlockData.AddDatum("AdditionalTargetSelections", () => trialLevel.AdditionalTargetSelections_Block);
        BlockData.AddDatum("TargetSelectionsBeforeFirstAnim", () => trialLevel.TargetSelectionsBeforeFirstAnim_Block);

        BlockData.AddDatum("DistractorSelections", () => trialLevel.DistractorSelections_Block);
        BlockData.AddDatum("DistractorRejections", () => trialLevel.DistractorRejections_Block);

        BlockData.AddDatum("CalculatedThreshold", () => trialLevel.calculatedThreshold_timing);
        BlockData.AddDatum("DiffLevelsSummary", () => trialLevel.DiffLevelsSummary);

        BlockData.AddDatum("SliderBarCompletions", () => trialLevel.SliderBarCompletions_Block);
    }


    public void ClearStrings()
    {
        CurrentBlockString = "";
        CurrentBlockSummaryString.Clear();
    }


}