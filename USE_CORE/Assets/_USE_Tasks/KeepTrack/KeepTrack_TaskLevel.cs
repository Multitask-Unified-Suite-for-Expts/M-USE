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
    [HideInInspector] public int SectionChanges_Task = 0;
    [HideInInspector] public int TargetAnimations_Task = 0;
    [HideInInspector] public int DistractorAnimations_Task = 0;
    [HideInInspector] public int TargetSel_BeforeFirstAnim_Task = 0;
    [HideInInspector] public int TargetSel_BeforeResponseWindow_Task = 0;
    [HideInInspector] public int TargetSel_WithinResponseWindow_Task = 0;
    [HideInInspector] public int TargetSel_AfterResponseWindow_Task = 0;
    [HideInInspector] public int AdditionalTargetSel_Task = 0;

    [HideInInspector] public int TargetIntervalsMissed_Task = 0;
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
            KT_Objects_ConfigValues = customSettings.FirstOrDefault(setting => setting.SearchString == ConfigFolderName + "_ObjectsDef").AssignCustomSetting<KT_Object_ConfigValues[]>();

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

        CurrentBlockString = "\nTargetSel_WithinWindow: " + trialLevel.TargetSel_WithinResponseWindow_Block +
                             "\nTargetSel_AfterWindow: " + trialLevel.TargetSel_AfterResponseWindow_Block +
                             "\nTargetSel_BeforeWindow: " + trialLevel.TargetSel_BeforeResponseWindow_Block +
                             "\nAdditional_TargetSel: " + trialLevel.AdditionalTargetSel_Block +

                             "\nIntervals w/o Sel: " + trialLevel.TargetIntervalsMissed_Block +
                             "\nDistractor Sel: " + trialLevel.DistractorSelections_Block +
                             "\nDistractor Rej: " + trialLevel.DistractorRejections_Block +
                             "\nReward Pulses: " + NumRewardPulses_InBlock;

        CurrentBlockSummaryString.AppendLine(CurrentBlockString).ToString();
    }

    public override void SetTaskSummaryString()
    {
        CurrentTaskSummaryString.Clear();
        base.SetTaskSummaryString();
        CurrentTaskSummaryString.Append($"\t# Target Selections Within Response Window: {TargetSel_WithinResponseWindow_Task}");
    }

    public override OrderedDictionary GetTaskSummaryData()
    {
        OrderedDictionary data = base.GetTaskSummaryData();

        data["Trials Completed"] = TrialsCompleted_Task;
        data["Section Changes"] = SectionChanges_Task;
        data["Target Animations"] = TargetAnimations_Task;
        data["Distractor Animations"] = DistractorAnimations_Task;
        data["TargetSel_BeforeWindow"] = TargetSel_BeforeResponseWindow_Task;
        data["TargetSel_WithinWindow"] = TargetSel_WithinResponseWindow_Task;
        data["TargetSel_AfterWindow"] = TargetSel_AfterResponseWindow_Task;
        data["Additional Target Sel"] = AdditionalTargetSel_Task;

        data["Distractor Selections"] = DistractorSelections_Task;
        data["Distractor Rejections"] = DistractorRejections_Task;
        data["TargetSel_BeforeFirstAnim"] = TargetSel_BeforeFirstAnim_Task;
        data["Intervals Without Selection"] = TargetIntervalsMissed_Task;
        data["SliderBar Completions"] = SliderBarCompletions_Task;

        return data;
    }

    public override OrderedDictionary GetTaskResultsData()
    {
        OrderedDictionary data = base.GetTaskResultsData();

        data["Trials Completed"] = TrialsCompleted_Task;
        data["TargetSel_BeforeWindow"] = TargetSel_BeforeResponseWindow_Task;
        data["TargetSel_WithinWindow"] = TargetSel_WithinResponseWindow_Task;
        data["TargetSel_AfterWindow"] = TargetSel_AfterResponseWindow_Task;
        data["Additional Target Sel"] = AdditionalTargetSel_Task;
        data["Distractor Selections"] = DistractorSelections_Task;
        data["Distractor Rejections"] = DistractorRejections_Task;
        data["TargetSel_BeforeFirstAnim"] = TargetSel_BeforeFirstAnim_Task;
        data["Intervals Without Selection"] = TargetIntervalsMissed_Task;

        return data;
    }


    private void DefineBlockData()
    {
        BlockData.AddDatum("BlockName", () => CurrentBlock.BlockName);
        BlockData.AddDatum("ContextName", () => CurrentBlock.ContextName);
        BlockData.AddDatum("TrialsCompleted", () => trialLevel.TrialCompletions_Block);
        BlockData.AddDatum("CalculatedThreshold", () => trialLevel.calculatedThreshold_timing);
        BlockData.AddDatum("DiffLevelsSummary", () => trialLevel.DiffLevelsSummary);
        BlockData.AddDatum("SliderBarCompletions", () => trialLevel.SliderBarCompletions_Block);

        BlockData.AddDatum("TargetSel_BeforeFirstAnim", () => trialLevel.TargetSel_BeforeFirstAnim_Block);
        BlockData.AddDatum("TargetSel_BeforeReponseWindow", () => trialLevel.TargetSel_BeforeResponseWindow_Block);
        BlockData.AddDatum("TargetSel_WithinResponseWindow", () => trialLevel.TargetSel_WithinResponseWindow_Block);
        BlockData.AddDatum("TargetSel_AfterResponseWindow", () => trialLevel.TargetSel_AfterResponseWindow_Block);
        BlockData.AddDatum("AdditionalTargetSel", () => trialLevel.AdditionalTargetSel_Block);

        BlockData.AddDatum("SectionChanges", () => trialLevel.SectionChanges_Block);
        BlockData.AddDatum("TargetAnimations", () => trialLevel.TargetAnimations_Block);
        BlockData.AddDatum("DistractorAnimations", () => trialLevel.DistractorAnimations_Block);
        BlockData.AddDatum("TargetIntervalsWithoutASelection", () => trialLevel.TargetIntervalsMissed_Block);
        BlockData.AddDatum("DistractorSelections", () => trialLevel.DistractorSelections_Block);
        BlockData.AddDatum("DistractorRejections", () => trialLevel.DistractorRejections_Block);
    }


    public void ClearStrings()
    {
        CurrentBlockString = "";
        CurrentBlockSummaryString.Clear();
    }


}