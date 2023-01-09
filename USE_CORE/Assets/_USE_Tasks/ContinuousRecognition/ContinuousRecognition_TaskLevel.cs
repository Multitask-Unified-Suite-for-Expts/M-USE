using System;
using System.Text;
using System.Collections.Generic;
using System.Collections.Specialized;
using ContinuousRecognition_Namespace;
using UnityEngine;
using UnityEngine.UI;
using USE_Settings;
using USE_StimulusManagement;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Block;


public class ContinuousRecognition_TaskLevel : ControlLevel_Task_Template
{
    [HideInInspector] public List<int> TrialsCompleted_Task;

    [HideInInspector] public List<int> TrialsCorrect_Task;
    [HideInInspector] public float AvgNumCorrect;

    [HideInInspector] public List<int> TokenBarCompletions_Task;
    [HideInInspector] float AvgNumTbCompletions;

    [HideInInspector] public List<int> TotalRewards_Task;
    [HideInInspector] public float AvgNumRewards;

    [HideInInspector] public List<float> TimeToChoice_Task;
    [HideInInspector] public float AvgTimeToChoice;

    [HideInInspector] public List<float> TimeToCompletion_Task;
    [HideInInspector] public float AvgTimeToCompletion;

    [HideInInspector] public double StanDev;
    [HideInInspector] public string BlockAveragesString;
    [HideInInspector] public string CurrentBlockString;
    [HideInInspector] public StringBuilder PreviousBlocksString;

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

        if(SessionSettings.SettingExists(TaskName + "_TaskSettings", "ContextExternalFilePath"))
            trialLevel.MaterialFilePath = (String)SessionSettings.Get(TaskName + "_TaskSettings", "ContextExternalFilePath");
        else if(SessionSettings.SettingExists("Session", "ContextExternalFilePath"))
            trialLevel.MaterialFilePath = (String)SessionSettings.Get("Session", "ContextExternalFilePath");
        else
            Debug.Log("ContextExternalFilePath NOT specified in the Session Config OR Task Config!");

        if (SessionSettings.SettingExists("Session", "MacMainDisplayBuild"))
            trialLevel.MacMainDisplayBuild = (bool)SessionSettings.Get("Session", "MacMainDisplayBuild");
        else
            trialLevel.MacMainDisplayBuild = false;

        BlockAveragesString = "";
        CurrentBlockString = "";
        PreviousBlocksString = new StringBuilder();

        RunBlock.AddInitializationMethod(() =>
        {
            RenderSettings.skybox = CreateSkybox(trialLevel.GetContextNestedFilePath(currentBlock.ContextName));
            trialLevel.ContextActive = true;
            EventCodeManager.SendCodeNextFrame(TaskEventCodes["ContextOn"]);

            SetupBlockData(trialLevel); //PROBLEM: missing headers and duplicating columns in subsequent blocks 

            trialLevel.AdjustedPositionsForMac = false;

            trialLevel.ChosenStimIndices.Clear();
            trialLevel.NumTrials_Block = 0;
            trialLevel.NumCorrect_Block = 0;
            trialLevel.NumTbCompletions_Block = 0;
            trialLevel.TimeToChoice_Block.Clear();
            trialLevel.AvgTimeToChoice_Block = 0;
            trialLevel.TimeToCompletion_Block = 0;
            trialLevel.NumRewards_Block = 0;
            trialLevel.Score = 0;

            trialLevel.TokenFBController.SetTokenBarValue(0);

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

            TrialsCompleted_Task.Add(trialLevel.NumTrials_Block);
            TrialsCorrect_Task.Add(trialLevel.NumCorrect_Block); //at end of each block, add block's NumCorrect to task List;
            TokenBarCompletions_Task.Add(trialLevel.NumTbCompletions_Block);
            TimeToChoice_Task.Add(trialLevel.AvgTimeToChoice_Block);
            TimeToCompletion_Task.Add(trialLevel.TimeToCompletion_Block);
            TotalRewards_Task.Add(trialLevel.NumRewards_Block);

            CalculateBlockAverages();
            CalculateStanDev();
        });
        
    }

    int GetTotal(List<int> total)
    {
        int count = 0;
        foreach(int num in total)
            count += num;
        return count;
    }

    public override OrderedDictionary GetSummaryData()
    {
        OrderedDictionary data = new OrderedDictionary();

        data["Trials Completed"] = GetTotal(TrialsCompleted_Task);
        data["Trials Correct"] = GetTotal(TrialsCorrect_Task);
        data["TokenBar Completions"] = GetTotal(TokenBarCompletions_Task);
        data["Total Rewards"] = GetTotal(TotalRewards_Task);
        return data;
    }

    private void CalculateBlockSummaryString(ContinuousRecognition_TrialLevel trialLevel)
    {
        ClearStrings();

        if (BlockCount > 0)
        {
            BlockAveragesString = "<size=18><b>Block Averages " + $"({BlockCount});" + "</b></size>" +
                              "\nAvg Correct: " + AvgNumCorrect.ToString("0.00") +
                              "\nAvg TbCompletions: " + AvgNumTbCompletions.ToString("0.00") +
                              "\nAvg TimeToChoice: " + AvgTimeToChoice.ToString("0.00") + "s" +
                              "\nAvg TimeToCompletion: " + AvgTimeToCompletion.ToString("0.00") + "s" +
                              "\nAvg Rewards: " + AvgNumRewards.ToString("0.00") +
                              "\nStandard Deviation: " + StanDev.ToString("0.00") +
                              "\n";
        }

        CurrentBlockString = "<b>Block " + "(" + currentBlock.BlockName + "):" + "</b>" +
                        "\nCorrect: " + trialLevel.NumCorrect_Block +
                        "\nTbCompletions: " + trialLevel.NumTbCompletions_Block +
                        "\nAvgTimeToChoice: " + trialLevel.AvgTimeToChoice_Block.ToString("0.00") + "s" +
                        "\nTimeToCompletion: " + trialLevel.TimeToCompletion_Block.ToString("0.00") + "s" +
                        "\nRewards: " + trialLevel.NumRewards_Block;

        if (BlockCount > 0) CurrentBlockString += "\n";

        BlockSummaryString.AppendLine(BlockAveragesString.ToString());
        BlockSummaryString.AppendLine(CurrentBlockString.ToString());
        if(PreviousBlocksString.Length > 0)
            BlockSummaryString.AppendLine(PreviousBlocksString.ToString());
    }

    void ClearStrings()
    {
        BlockAveragesString = "";
        CurrentBlockString = "";
        BlockSummaryString.Clear();
    }

    void CalculateStanDev()
    {
        if (TrialsCorrect_Task.Count == 0)
            StanDev = 0;
        else
        {
            double mean = (double)AvgNumCorrect;
            List<double> squaredDeviations = new List<double>();
            foreach (var num in TrialsCorrect_Task)
                squaredDeviations.Add(Math.Pow(num - mean, 2));
            double SumOfSquares = 0;
            foreach (var num in squaredDeviations)
                SumOfSquares += num;
            var variance = SumOfSquares / TrialsCorrect_Task.Count;
            StanDev = Math.Sqrt(variance);
        }
    }

    void CalculateBlockAverages()
    {
        //Avg Num Correct
        if (TrialsCorrect_Task.Count == 0)
            AvgNumCorrect = 0;
        else
            AvgNumCorrect = GetIntListAverage(TrialsCorrect_Task);

        //Avg Num TokenBar Completions
        if (TokenBarCompletions_Task.Count == 0)
            AvgNumTbCompletions = 0;
        else
            AvgNumTbCompletions = GetIntListAverage(TokenBarCompletions_Task);
        
        //Avg TimeToChoice
        if (TimeToChoice_Task.Count == 0)
            AvgTimeToChoice = 0;
        else
            AvgTimeToChoice = GetFloatListAverage(TimeToChoice_Task);

        //Avg TimeToCompletion
        if (TimeToCompletion_Task.Count == 0)
            AvgTimeToCompletion = 0;
        else
            AvgTimeToCompletion = GetFloatListAverage(TimeToCompletion_Task);

        //Avg NumRewards
        if (TotalRewards_Task.Count == 0)
            AvgNumRewards = 0;
        else
            AvgNumRewards = GetIntListAverage(TotalRewards_Task);
    }

    float GetFloatListAverage(List<float> numList)
    {
        if (numList.Count == 0)
            return 0;
        else
        {
            float sum = 0;
            foreach (float num in numList)
                sum += num;
            return sum / numList.Count;
        }
    }

    float GetIntListAverage(List<int> numList)
    {
        if (numList.Count == 0)
            return 0;
        else
        {
            float sum = 0;
            foreach (var num in numList)
                sum += num;
            return sum / numList.Count;
        }
    }

    void SetupBlockData(ContinuousRecognition_TrialLevel trialLevel)
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

}
