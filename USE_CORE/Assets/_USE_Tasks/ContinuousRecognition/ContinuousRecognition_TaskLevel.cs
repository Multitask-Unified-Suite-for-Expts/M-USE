using System;
using System.Text;
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

    public List<int> NumTbCompletions_Task;
    public float AvgNumTbCompletions;

    public List<float> TimeToChoice_Task;
    public float AvgTimeToChoice;

    public List<float> TimeToCompletion_Task;
    public float AvgTimeToCompletion;

    public List<float> NumRewards_Task;
    public float AvgNumRewards;

    public double StanDev;

    public string BlockAveragesString;
    public string CurrentBlockString;
    public StringBuilder PreviousBlocksString;

    public int TrialCount;

    public GameObject Starfield;

   

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

        BlockSummaryString = new StringBuilder();
        BlockAveragesString = "";
        CurrentBlockString = "";
        PreviousBlocksString = new StringBuilder();


        //Clearing the list of picked stim indices at beginning of each block. 
        RunBlock.AddInitializationMethod(() =>
        {
            if (!Starfield.activeSelf) Starfield.SetActive(true);

            trialLevel.ChosenStimIndices.Clear();

            trialLevel.NumTrials_Block = 0;
            trialLevel.NumCorrect_Block = 0;
            trialLevel.NumTbCompletions_Block = 0;
            trialLevel.TimeToChoice_Block.Clear();
            trialLevel.AvgTimeToChoice_Block = 0;
            trialLevel.TimeToCompletion_Block = 0;
            trialLevel.NumRewards_Block = 0;

            CalculateBlockSummaryString(trialLevel);
        });



        RunBlock.AddUpdateMethod(() =>
        {
            if (trialLevel.TrialComplete)
            {
                CalculateBlockSummaryString(trialLevel); //Update string if they finish a trial 
                trialLevel.TrialComplete = false;
            }
        });


        BlockFeedback.AddInitializationMethod(() =>
        {
            if(BlockCount > 0) CurrentBlockString += "\n";
            PreviousBlocksString.Insert(0,CurrentBlockString); //Add current block string to full list of previous blocks. 

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


    private void CalculateBlockSummaryString(ContinuousRecognition_TrialLevel trialLevel)
    {
        ClearStrings();

        BlockAveragesString = "<size=18><b>Block Averages " + $"({BlockCount});" + "</b></size>" +
                          "\nAvg Correct: " + AvgNumCorrect.ToString("0.00") +
                          "\nAvg TbCompletions: " + AvgNumTbCompletions.ToString("0.00") +
                          "\nAvg TimeToPick: " + AvgTimeToChoice.ToString("0.00") + "s" +
                          "\nAvg TimeToCompletion: " + AvgTimeToCompletion.ToString("0.00") + "s" +
                          "\nAvg Rewards: " + AvgNumRewards.ToString("0.00") +
                          "\nStandard Deviation: " + StanDev.ToString("0.00") +
                          "\n";

        CurrentBlockString = "<b>Block" + "(" + currentBlock.BlockName + "):" + "</b>" +
                        "\nCorrect: " + trialLevel.NumCorrect_Block +
                        "\nTbCompletions: " + trialLevel.NumTbCompletions_Block +
                        "\nAvgTimeToChoice: " + trialLevel.AvgTimeToChoice_Block.ToString("0.00") + "s" +
                        "\nTimeToCompletion: " + trialLevel.TimeToCompletion_Block.ToString("0.00") + "s" +
                        "\nRewards: " + trialLevel.NumRewards_Block;

        if (BlockCount > 0) CurrentBlockString += "\n";

        BlockSummaryString.AppendLine(BlockAveragesString.ToString());
        BlockSummaryString.AppendLine(CurrentBlockString.ToString());
        if(PreviousBlocksString.Length > 0) BlockSummaryString.AppendLine(PreviousBlocksString.ToString());
    }

    private void ClearStrings()
    {
        BlockAveragesString = "";
        CurrentBlockString = "";
        BlockSummaryString.Clear();
    }

    private void CalculateStanDev()
    {
        if (NumCorrect_Task.Count == 0) StanDev = 0;
        else
        {
            double Mean = (double)AvgNumCorrect;
            List<double> squaredDeviations = new List<double>();
            foreach (var num in NumCorrect_Task) squaredDeviations.Add(Math.Pow(num - Mean, 2));
            double SumOfSquares = 0;
            foreach (var num in squaredDeviations) SumOfSquares += num;
            var variance = SumOfSquares / NumCorrect_Task.Count;
            StanDev = Math.Sqrt(variance);
        }
    }

    private void CalculateBlockAverages()
    {
        //Avg Num Correct
        if (NumCorrect_Task.Count == 0) AvgNumCorrect = 0;
        else
        {
            float sum = 0;
            foreach (int num in NumCorrect_Task) sum += num;
            AvgNumCorrect = (float)sum / NumCorrect_Task.Count;
        }

        //Avg Num TokenBar Completions
        if (NumTbCompletions_Task.Count == 0) AvgNumTbCompletions = 0;
        else
        {
            float sum = 0;
            foreach (int num in NumTbCompletions_Task) sum += num;
            AvgNumTbCompletions = (float)sum / NumTbCompletions_Task.Count;
        }

        //Avg TimeToChoice
        if (TimeToChoice_Task.Count == 0) AvgTimeToChoice = 0;
        else
        {
            float sum = 0;
            foreach (float num in TimeToChoice_Task) sum += num;
            AvgTimeToChoice = (float)sum / TimeToChoice_Task.Count;
        }

        //Avg TimeToCompletion
        if (TimeToCompletion_Task.Count == 0) AvgTimeToCompletion = 0;
        else
        {
            float sum = 0;
            foreach (float num in TimeToCompletion_Task) sum += num;
            AvgTimeToCompletion = (float)sum / TimeToCompletion_Task.Count;
        }

        //Avg NumRewards
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
        BlockData.AddDatum("MaxNumTrials", () => currentBlock.MaxNumTrials);

    }

    public T GetCurrentBlockDef<T>() where T : BlockDef
    {
        return (T)CurrentBlockDef;
    }

}
