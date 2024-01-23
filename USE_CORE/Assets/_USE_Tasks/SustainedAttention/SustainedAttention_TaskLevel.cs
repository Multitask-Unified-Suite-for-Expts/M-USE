using USE_ExperimentTemplate_Task;
using SustainedAttention_Namespace;
using UnityEngine;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Linq;


public class SustainedAttention_TaskLevel : ControlLevel_Task_Template
{
    SustainedAttention_BlockDef CurrentBlock => GetCurrentBlockDef<SustainedAttention_BlockDef>();
    SustainedAttention_TrialLevel trialLevel;

    [HideInInspector] public string CurrentBlockString;
    [HideInInspector] public int BlockStringsAdded = 0;

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
    public SA_Object_ConfigValues[] SA_Objects_ConfigValues;


    public override void DefineControlLevel()
    {
        trialLevel = (SustainedAttention_TrialLevel)TrialLevel;
        CurrentBlockString = "";
        DefineBlockData();
        Session.HumanStartPanel.AddTaskDisplayName(TaskName, "Sustained Attention");
        Session.HumanStartPanel.AddTaskInstructions(TaskName, "Keep your eye on the Target object. When it animates (closes its mouth), select it as quickly as you can!");

        RunBlock.AddSpecificInitializationMethod(() =>
        {
            //Grab custom settings from Object Config that are read in:
            SA_Objects_ConfigValues = customSettings.FirstOrDefault(setting => setting.SearchString == "SustainedAttention_ObjectsDef").AssignCustomSetting<SA_Object_ConfigValues[]>();

            CurrentBlock.ContextName = CurrentBlock.ContextName.Trim();
            SetSkyBox(CurrentBlock.ContextName);
            trialLevel.ResetBlockVariables();
            CalculateBlockSummaryString();
        });

        BlockFeedback.AddSpecificInitializationMethod(() => HandleBlockStrings());
    }


    public override List<CustomSettings> DefineCustomSettings()
    {
        customSettings.Add(new CustomSettings("SustainedAttention_ObjectsDef", typeof(SA_Object_ConfigValues), "array", SA_Objects_ConfigValues));
        return customSettings;
    }

    public void CalculateBlockSummaryString()
    {
        ClearStrings();

        CurrentBlockString = "\nSuccessful Target Selections: " + trialLevel.SuccessfulTargetSelections_Block +
                             "\nUnsuccessful Target Selections: " + trialLevel.UnsuccessfulTargetSelections_Block +
                             "\nDistractor Selections: " + trialLevel.DistractorSelections_Block +
                             "\nDistractor Rejections: " + trialLevel.DistractorRejections_Block +
                             "\nAdditional Target Selections: " + trialLevel.AdditionalTargetSelections_Block +
                             "\nIntervals Without A Selection: " + trialLevel.TargetAnimsWithoutSelection_Block +
                             "\nTarget Selections Before First Anim: " + trialLevel.TargetSelectionsBeforeFirstAnim_Block +
                             "\nReward Pulses: " + NumRewardPulses_InBlock;

        CurrentBlockSummaryString.AppendLine(CurrentBlockString).ToString();
    }

    public override void SetTaskSummaryString()
    {
        CurrentTaskSummaryString.Clear();
        base.SetTaskSummaryString();
        CurrentTaskSummaryString.Append($"\t<b># Successful Target Selections:</b> {SuccessfulTargetSelections_Task}");
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

    public override OrderedDictionary GetBlockResultsData()
    {
        OrderedDictionary data = new OrderedDictionary
        {
            ["Trials Completed"] = trialLevel.TrialCompletions_Block,
            ["Successful Target Selections"] = trialLevel.SuccessfulTargetSelections_Block,
            ["Unsuccessful Target Selections"] = trialLevel.UnsuccessfulTargetSelections_Block,
            ["Distractor Selections"] = trialLevel.DistractorSelections_Block,
            ["Distractor Rejections"] = trialLevel.DistractorRejections_Block,
            ["Premature Target Selections"] = trialLevel.TargetSelectionsBeforeFirstAnim_Block,
            ["Intervals Without A Selection"] = trialLevel.TargetAnimsWithoutSelection_Block,
            ["Additional Target Selections"] = trialLevel.AdditionalTargetSelections_Block,
            ["SliderBar Completions"] = trialLevel.SliderBarCompletions_Block,

        };
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

    private void HandleBlockStrings()
    {
        if (!Session.WebBuild)
        {
            if (BlockStringsAdded > 0)
                CurrentBlockString += "\n";
            BlockStringsAdded++;
        }
    }

    public void ClearStrings()
    {
        CurrentBlockString = "";
        CurrentBlockSummaryString.Clear();
    }


}