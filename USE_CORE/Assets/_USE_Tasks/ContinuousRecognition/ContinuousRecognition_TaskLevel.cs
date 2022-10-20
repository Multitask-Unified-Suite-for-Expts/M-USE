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
    public float AvgNumCorrect;

    public List<int> NumTrials_Task;
    public float AvgNumTrials;

    public List<int> NumTbCompletions_Task;
    public float AvgNumTbCompletions;

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

            trialLevel.NumTbCompletions_Block = 0;


        });
        RunBlock.AddUpdateMethod(() =>
        {

            BlockSummaryString ="\n" +
                                "BLOCK AVERAGES: " +
                                "\nAvg NumTrials: " + AvgNumTrials +
                                "\nAvg Correct: " + AvgNumCorrect +
                                "\nAvg TbCompletions: " + AvgNumTbCompletions +
                                "\n" +
                                "\nCURRENT BLOCK:" +
                                "\nBlock Name: " + currentBlock.BlockName +
                                "\nTrials: " + trialLevel.NumTrials_Block +
                                "\nCorrect: " + trialLevel.NumCorrect_Block +
                                "\nTokenBarCompletions: " + trialLevel.NumTbCompletions_Block;
        });



        BlockFeedback.AddInitializationMethod(() =>
        {
            NumTrials_Task.Add(trialLevel.NumTrials_Block); // at end of each block, add block's NumTrials to task List;
            NumCorrect_Task.Add(trialLevel.NumCorrect_Block); //at end of each block, add block's NumCorrect to task List;
            NumTbCompletions_Task.Add(trialLevel.NumTbCompletions_Block);

            CalculateBlockAverages();


            LogBlockData(trialLevel);
        });
    }


    private void CalculateBlockAverages()
    {
        float avg;
        float truncated;

        //Avg Num Trials
        if (NumTrials_Task.Count == 0) AvgNumTrials = 0;
        else
        {
            int numTrials = 0;
            foreach (int num in NumTrials_Task) numTrials += num;
            avg = (float) numTrials / NumTrials_Task.Count;
            truncated = (float)(Math.Truncate((double)avg * 100.0) / 100.0);
            AvgNumTrials = (float)(Math.Round((double)avg, 1));
        }

        //Avg Num Correct
        if (NumCorrect_Task.Count == 0) AvgNumCorrect = 0;
        else
        {
            int numCorrect = 0;
            foreach (int num in NumCorrect_Task) numCorrect += num;
            avg = (float) numCorrect / NumCorrect_Task.Count;
            truncated = (float)(Math.Truncate((double)avg * 100.0) / 100.0);
            AvgNumCorrect = (float)(Math.Round((double)avg, 1));
        }

        //Avg Num TokenBar Completions
        if (NumTbCompletions_Task.Count == 0) AvgNumTbCompletions = 0;
        else
        {
            int numCompletions = 0;
            foreach (int num in NumTbCompletions_Task) numCompletions += num;
            avg = (float) numCompletions / NumTbCompletions_Task.Count;
            truncated = (float)(Math.Truncate((double)avg * 100.0) / 100.0);
            AvgNumTbCompletions = (float)(Math.Round((double)avg, 1));
        }




    }


    private void LogBlockData(ContinuousRecognition_TrialLevel trialLevel)
    { 
        BlockData.AddDatum("BlockName", () => currentBlock.BlockName);
        BlockData.AddDatum("NumTrials", () => trialLevel.NumTrials_Block);
        BlockData.AddDatum("NumCorrect", () => trialLevel.NumCorrect_Block);
        BlockData.AddDatum("TokenBarCompletions", () => trialLevel.NumTbCompletions_Block);
    }

    public T GetCurrentBlockDef<T>() where T : BlockDef
    {
        return (T)CurrentBlockDef;
    }

}
