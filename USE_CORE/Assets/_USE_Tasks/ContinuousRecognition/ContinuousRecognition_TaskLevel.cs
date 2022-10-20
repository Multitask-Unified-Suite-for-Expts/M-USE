using System;
using System.Collections.Generic;
using ContinuousRecognition_Namespace;
using UnityEngine;
using UnityEngine.UI;
using USE_Settings;
using USE_StimulusManagement;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Block;

public class ContinuousRecognition_TaskLevel : ControlLevel_Task_Template
{
    public List<int> NumCorrect_Task;
    public float AvgNumCorrect_Task;

    public List<int> NumTrials_Task;
    public float AvgNumTrials_Task;

    ContinuousRecognition_BlockDef currentBlock => GetCurrentBlockDef<ContinuousRecognition_BlockDef>();
    public override void SpecifyTypes()
    {
        TaskLevelType = typeof(ContinuousRecognition_TaskLevel);
        TrialLevelType = typeof(ContinuousRecognition_TrialLevel);
        TaskDefType = typeof(ContinuousRecognition_TaskDef);
        BlockDefType = typeof(ContinuousRecognition_BlockDef);
        TrialDefType = typeof(ContinuousRecognition_TrialDef);
        StimDefType = typeof(ContinuousRecognition_StimDef);
    } 
    public override void DefineControlLevel() //RUNS WHEN THE TASK IS DEFINED!
    {           
        ContinuousRecognition_TrialLevel trialLevel = (ContinuousRecognition_TrialLevel)TrialLevel;

        string TaskName = "ContinuousRecognition";
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ContextExternalFilePath"))
            trialLevel.MaterialFilePath = (String)SessionSettings.Get(TaskName + "_TaskSettings", "ContextExternalFilePath");

        //Clearing the list of picked stim indices at beginning of each block. 
        RunBlock.AddInitializationMethod(() =>
        {
            trialLevel.ChosenStimIndices.Clear();

            trialLevel.NumTrials_Block = 0; //reset num trials per block at beg of each block
            trialLevel.NumCorrect_Block = 0; //reset num correct per block at beg of each block

            trialLevel.TokenBarCompletions_Block = 0;


        });
        RunBlock.AddUpdateMethod(() =>
        {

            BlockSummaryString ="\n" +
                                "BLOCK AVERAGES: " +
                                "\nAvgNumTrials: " + AvgNumTrials_Task +
                                "\nAvgNumCorrect: " + AvgNumCorrect_Task + 
                                "\n" +
                                "\nCURRENT BLOCK:" +
                                "\nBlock Name: " + currentBlock.BlockName +
                                "\nTrials: " + trialLevel.NumTrials_Block +
                                "\nCorrect: " + trialLevel.NumCorrect_Block +
                                "\nTokenBarCompletions: " + trialLevel.TokenBarCompletions_Block;
        });



        BlockFeedback.AddInitializationMethod(() =>
        {
            NumTrials_Task.Add(trialLevel.NumTrials_Block); // at end of each block, add block's NumTrials to task List;
            NumCorrect_Task.Add(trialLevel.NumCorrect_Block); //at end of each block, add block's NumCorrect to task List;

            CalculateBlockAverages();


            LogBlockData(trialLevel);
        });
    }


    private void CalculateBlockAverages()
    {
        //Avg Num Trials
        if (NumTrials_Task.Count == 0) AvgNumTrials_Task = 0;
        else
        {
            int numTrials = 0;
            foreach (int num in NumTrials_Task) numTrials += num;
            AvgNumTrials_Task = (float) numTrials / NumTrials_Task.Count;
        }

        //Avg Num Correct
        if (NumCorrect_Task.Count == 0) AvgNumCorrect_Task = 0;
        else
        {
            int numCorrect = 0;
            foreach (int num in NumCorrect_Task) numCorrect += num;
            AvgNumCorrect_Task = (float) numCorrect / NumCorrect_Task.Count;
        }






    }


    private void LogBlockData(ContinuousRecognition_TrialLevel trialLevel)
    { 
        BlockData.AddDatum("BlockName", () => currentBlock.BlockName);
        BlockData.AddDatum("NumTrials", () => trialLevel.NumTrials_Block);
        BlockData.AddDatum("NumCorrect", () => trialLevel.NumCorrect_Block);
        BlockData.AddDatum("TokenBarCompletions", () => trialLevel.TokenBarCompletions_Block);
    }

    public T GetCurrentBlockDef<T>() where T : BlockDef
    {
        return (T)CurrentBlockDef;
    }

}
