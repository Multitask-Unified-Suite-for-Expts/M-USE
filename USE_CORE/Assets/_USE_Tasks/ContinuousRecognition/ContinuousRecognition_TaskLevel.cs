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

    //public List<int> NumTrials_Task;
    //public float AvgNumTrials;

    public List<int> NumTbCompletions_Task;
    public float AvgNumTbCompletions;

    public List<float> TimeToChoice_Task;
    public float AvgTimeToChoice;

    public List<float> TimeToCompletion_Task;
    public float AvgTimeToCompletion;

    public List<float> NumRewards_Task;
    public float AvgNumRewards;

    public List<double> StanDevs_Task;
    public double AvgStanDev;

    public string BlockAveragesString;
    public string CurrentBlockString;
    public string PreviousBlocksString;
    

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

            trialLevel.NumTrials_Block = 0;
            trialLevel.NumCorrect_Block = 0;

            trialLevel.NumTbCompletions_Block = 0;

            trialLevel.TimeToChoice_Block.Clear();
            trialLevel.AvgTimeToChoice_Block = 0;

            trialLevel.TimeToCompletion_Block = 0;

            trialLevel.NumRewards_Block = 0;
        

        });
        RunBlock.AddUpdateMethod(() =>
        {
            CalculateBlockAveragesString();
            CalculateCurrentBlockString(trialLevel);

            BlockSummaryString = BlockAveragesString + "\n" +
                                 CurrentBlockString + "\n" +
                                 PreviousBlocksString;

        });


        BlockFeedback.AddInitializationMethod(() =>
        {
            PreviousBlocksString += CurrentBlockString; //Add current block string to full list of previous blocks. 

            //NumTrials_Task.Add(trialLevel.NumTrials_Block); // at end of each block, add block's NumTrials to task List;
            NumCorrect_Task.Add(trialLevel.NumCorrect_Block); //at end of each block, add block's NumCorrect to task List;
            NumTbCompletions_Task.Add(trialLevel.NumTbCompletions_Block);
            TimeToChoice_Task.Add(trialLevel.AvgTimeToChoice_Block);
            TimeToCompletion_Task.Add(trialLevel.TimeToCompletion_Block);
            NumRewards_Task.Add(trialLevel.NumRewards_Block);

            CalculateBlockAverages();
            CalculateStanDev();

            LogBlockData(trialLevel);
        });
    }


    private void CalculateBlockAveragesString()
    {
        BlockAveragesString = 
        "\nAvg Correct: " + AvgNumCorrect.ToString("0.00") +
        "\nAvg StanDev: " + AvgStanDev.ToString("0.00") +
        "\nAvg TbCompletions: " + AvgNumTbCompletions.ToString("0.00") +
        "\nAvg TimeToPick: " + AvgTimeToChoice.ToString("0.00") +
        "\nAvg TimeToCompletion: " + AvgTimeToCompletion.ToString("0.00") +
        "\nAvg Rewards: " + AvgNumRewards.ToString("0.00");
    }

    private void CalculateCurrentBlockString(ContinuousRecognition_TrialLevel trialLevel)
    {
        CurrentBlockString =
        "\n" + "<size=26><b><color=#2d3436ff>Block </color></b></size>" + "(" + currentBlock.BlockName + "):" +
        //"\nTrials: " + trialLevel.NumTrials_Block +
        "\nCorrect: " + trialLevel.NumCorrect_Block +
        "\nTbCompletions: " + trialLevel.NumTbCompletions_Block +
        "\nAvgTimeToChoice: " + trialLevel.AvgTimeToChoice_Block.ToString("0.00") +
        "\nTimeToCompletion: " + trialLevel.TimeToCompletion_Block.ToString("0.00") +
        "\nRewards: " + trialLevel.NumRewards_Block;
    }


    //NOT RIGHT
    private void CalculateStanDev()
    {
        StanDevs_Task.Clear();

        double blockMean = (double)AvgNumCorrect;
        Debug.Log("BLOCKMEAN = " + blockMean);
        double blockSumOfSquares = 0;

        foreach (int num in NumCorrect_Task)
        {
            blockSumOfSquares += Math.Pow(num - blockMean, 2);
            double BlockStanDev = Math.Sqrt(blockSumOfSquares / NumCorrect_Task.Count);
            StanDevs_Task.Add(BlockStanDev);
        }

        if (NumCorrect_Task.Count == 0) AvgStanDev = 0;
        else
        {
            double sum = 0;
            foreach (double num in StanDevs_Task) sum += num;
            AvgStanDev = (double)sum / StanDevs_Task.Count;
        }

    }

    private void CalculateBlockAverages()
    {
        //Avg Num Trials
        //if (NumTrials_Task.Count == 0) AvgNumTrials = 0;
        //else
        //{
        //    float sum = 0;
        //    foreach (int num in NumTrials_Task) sum += num;
        //    AvgNumTrials = (float)sum / NumTrials_Task.Count;

        //    //float avg = (float) sum / NumTrials_Task.Count;
        //    //float truncated = (float)(Math.Truncate((double)avg * 100.0) / 100.0);
        //    //AvgNumTrials = (float)(Math.Round((double)avg, 1));
        //}

        //Avg Num Correct
        if (NumCorrect_Task.Count == 0) AvgNumCorrect = 0;
        else
        {
            float sum = 0;
            foreach (int num in NumCorrect_Task) sum += num;
            AvgNumCorrect = (float)sum / NumCorrect_Task.Count;
            //float avg = (float) sum / NumCorrect_Task.Count;
            //float truncated = (float)(Math.Truncate((double)avg * 100.0) / 100.0);
            //AvgNumCorrect = (float)(Math.Round((double)avg, 1));
        }

        //Avg Num TokenBar Completions
        if (NumTbCompletions_Task.Count == 0) AvgNumTbCompletions = 0;
        else
        {
            float sum = 0;
            foreach (int num in NumTbCompletions_Task) sum += num;
            AvgNumTbCompletions = (float)sum / NumTbCompletions_Task.Count;
            //float avg = (float) sum / NumTbCompletions_Task.Count;
            //float truncated = (float)(Math.Truncate((double)avg * 100.0) / 100.0);
            //AvgNumTbCompletions = (float)(Math.Round((double)avg, 1));
        }

        //Avg TimeToChoice
        if (TimeToChoice_Task.Count == 0) AvgTimeToChoice = 0;
        else
        {
            float sum = 0;
            foreach (float num in TimeToChoice_Task) sum += num;
            AvgTimeToChoice = (float)sum / TimeToChoice_Task.Count;
            //float avg = (float) sum / TimeToChoice_Task.Count;
            //float truncated = (float)(Math.Truncate((double)avg * 100.0) / 100.0);
            //AvgTimeToChoice = (float)(Math.Round((double)avg, 4));
        }

        //Avg TimeToCompletion
        if (TimeToCompletion_Task.Count == 0) AvgTimeToCompletion = 0;
        else
        {
            float sum = 0;
            foreach (float num in TimeToCompletion_Task) sum += num;
            AvgTimeToCompletion = (float)sum / TimeToCompletion_Task.Count;
            //float avg = (float)sum / TimeToCompletion_Task.Count;
            //float truncated = (float)(Math.Truncate((double)avg * 100.0) / 100.0);
            //AvgTimeToCompletion = (float)(Math.Round((double)avg, 2));
        }

        if (NumRewards_Task.Count == 0) AvgNumRewards = 0;
        else
        {
            float sum = 0;
            foreach (float num in NumRewards_Task) sum += num;
            AvgNumRewards = (float)sum / NumRewards_Task.Count;
        }


    }


    private void LogBlockData(ContinuousRecognition_TrialLevel trialLevel)
    { 
        BlockData.AddDatum("BlockName", () => currentBlock.BlockName);
        BlockData.AddDatum("NumTrials", () => trialLevel.NumTrials_Block);
        BlockData.AddDatum("NumCorrect", () => trialLevel.NumCorrect_Block);
        BlockData.AddDatum("TokenBarCompletions", () => trialLevel.NumTbCompletions_Block);
        BlockData.AddDatum("TimeToChoice", () => trialLevel.AvgTimeToChoice_Block);
        BlockData.AddDatum("TimeToCompletion", () => trialLevel.TimeToCompletion_Block);
        BlockData.AddDatum("NumRewards", () => trialLevel.NumRewards_Block);

    }

    public T GetCurrentBlockDef<T>() where T : BlockDef
    {
        return (T)CurrentBlockDef;
    }

}
