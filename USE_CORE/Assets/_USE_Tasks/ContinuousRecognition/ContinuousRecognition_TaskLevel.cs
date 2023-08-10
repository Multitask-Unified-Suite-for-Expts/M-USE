using System;
using System.Text;
using System.Collections.Generic;
using System.Collections.Specialized;
using ContinuousRecognition_Namespace;
using UnityEngine;
using USE_Settings;
using USE_ExperimentTemplate_Task;
using System.Linq;
using System.Collections;

public class ContinuousRecognition_TaskLevel : ControlLevel_Task_Template
{
    [HideInInspector] public List<int> TrialsCompleted_Task;

    [HideInInspector] public List<int> TrialsCorrect_Task;
    [HideInInspector] public float AvgNumCorrect = 0;

    [HideInInspector] public List<int> TokenBarCompletions_Task;
    [HideInInspector] float AvgNumTbCompletions = 0;

    [HideInInspector] public List<int> TotalRewards_Task;
    [HideInInspector] public float AvgNumRewards = 0;

    [HideInInspector] public List<float> TimeToChoice_Task;
    [HideInInspector] public float AvgTimeToChoice = 0;

    [HideInInspector] public List<float> TimeToCompletion_Task;
    [HideInInspector] public float AvgTimeToCompletion = 0;

    [HideInInspector] public List<float> NonStimTouches_Task;
    [HideInInspector] public float AvgNonStimTouches_Task = 0;

    [HideInInspector] public double StanDev;
    [HideInInspector] public string BlockAveragesString;
    [HideInInspector] public string CurrentBlockString;
    [HideInInspector] public StringBuilder PreviousBlocksString;

    ContinuousRecognition_BlockDef CurrentBlock => GetCurrentBlockDef<ContinuousRecognition_BlockDef>();
    ContinuousRecognition_TrialLevel trialLevel;

    public int blocksAdded;

    public override void SpecifyTypes()
    {
        TaskLevelType = typeof(ContinuousRecognition_TaskLevel);
        TrialLevelType = typeof(ContinuousRecognition_TrialLevel);
        TaskDefType = typeof(ContinuousRecognition_TaskDef);
        BlockDefType = typeof(ContinuousRecognition_BlockDef);
        TrialDefType = typeof(ContinuousRecognition_TrialDef);
        StimDefType = typeof(ContinuousRecognition_StimDef);
    }

    public override void DefineControlLevel()
    {
        trialLevel = (ContinuousRecognition_TrialLevel) TrialLevel;

        BlockAveragesString = "";
        CurrentBlockString = "";
        PreviousBlocksString = new StringBuilder();

        SetupBlockData();

        blocksAdded = 0;

        RunBlock.AddInitializationMethod(() =>
        {
            SetSkyBox(CurrentBlock.ContextName, TaskCam.gameObject.GetComponent<Skybox>());

            trialLevel.ContextActive = true;

            trialLevel.TokenFBController.SetTotalTokensNum(CurrentBlock.NumTokenBar);
            trialLevel.TokenFBController.SetTokenBarValue(CurrentBlock.InitialTokenAmount);
            trialLevel.ResetBlockVariables();

            CalculateBlockSummaryString();
        });
        RunBlock.AddDefaultTerminationMethod(() => AddBlockValuesToTaskValues());

        BlockFeedback.AddInitializationMethod(() =>
        {
            if(!SessionValues.WebBuild && trialLevel.AbortCode == 0)
            {
                CalculateBlockAverages();
                CalculateStanDev();

                if(trialLevel.AbortCode == 0)
                {
                    CurrentBlockString += "\n" + "\n";
                    CurrentBlockString = CurrentBlockString.Replace("Current Block", $"Block {blocksAdded + 1}");
                    PreviousBlocksString.Insert(0, CurrentBlockString); //Add current block string to full list of previous blocks. 
                    blocksAdded++;  
                }
            }
        });        
    }

    public override void SetTaskSummaryString()
    {
        if(trialLevel.TrialCount_InTask != 0)
        {
            CurrentTaskSummaryString.Clear();
            CurrentTaskSummaryString.Append($"\n<b>{ConfigFolderName}</b>" +
                                            $"\n<b># Trials:</b> {trialLevel.TrialCount_InTask} | " +
                                            $"\t<b># Blocks:</b> {BlockCount} | " +
                                            $"\t<b># Rewards:</b> {TotalRewards_Task.Count} | " +
                                            $"\t<b># TbFilled:</b> {TokenBarCompletions_Task.Count}");
        }
        else
            CurrentTaskSummaryString.Append($"\n<b>{ConfigFolderName}</b>");
        
    }

    public void AddBlockValuesToTaskValues()
    {
        NonStimTouches_Task.Add(trialLevel.NonStimTouches_Block);
        TrialsCompleted_Task.Add(trialLevel.NumTrials_Block);
        TrialsCorrect_Task.Add(trialLevel.NumCorrect_Block);
        TokenBarCompletions_Task.Add(trialLevel.NumTbCompletions_Block);
        TimeToChoice_Task.Add(trialLevel.AvgTimeToChoice_Block);
        TimeToCompletion_Task.Add(trialLevel.TimeToCompletion_Block);
        TotalRewards_Task.Add(trialLevel.NumRewards_Block);
    }



    public override OrderedDictionary GetBlockResultsData()
    {
        OrderedDictionary data = new OrderedDictionary
        {
            ["Score"] = trialLevel.Score + "XP",
            ["Trials Correct"] = trialLevel.NumCorrect_Block,
            ["Trials Completed"] = trialLevel.TrialCount_InBlock + 1,
            ["Time"] = trialLevel.TimeToCompletion_Block.ToString("0") + "s",
            ["TokenBar Completions"] = trialLevel.NumTbCompletions_Block
        };
        return data;
    }

    public override OrderedDictionary GetTaskSummaryData()
    {
        OrderedDictionary data = new OrderedDictionary
        {
            ["Trials Completed"] = TrialsCompleted_Task.Sum(),
            ["Trials Correct"] = TrialsCorrect_Task.Sum(),
            ["TokenBar Completions"] = TokenBarCompletions_Task.Sum(),
            ["Total Rewards"] = TotalRewards_Task.Sum(),
        };
        return data;
    }

    void SetupBlockData()
    {
        BlockData.AddDatum("BlockName", () => CurrentBlock.BlockName);
        BlockData.AddDatum("NonStimTouches", () => trialLevel.NonStimTouches_Block);
        BlockData.AddDatum("NumTrials", () => trialLevel.NumTrials_Block);
        BlockData.AddDatum("NumCorrect", () => trialLevel.NumCorrect_Block);
        BlockData.AddDatum("TokenBarCompletions", () => trialLevel.NumTbCompletions_Block);
        BlockData.AddDatum("TimeToChoice", () => trialLevel.AvgTimeToChoice_Block);
        BlockData.AddDatum("TimeToCompletion", () => trialLevel.TimeToCompletion_Block);
        BlockData.AddDatum("NumRewards", () => trialLevel.NumRewards_Block);
        BlockData.AddDatum("MaxNumTrials", () => CurrentBlock.MaxNumTrials);
    }

    public void CalculateBlockSummaryString()
    {
        ClearStrings();
        
        CurrentBlockString = "<b>Current Block:</b>" +
                "\nCorrect: " + trialLevel.NumCorrect_Block +
                "\nTbCompletions: " + trialLevel.NumTbCompletions_Block +
                "\nAvgTimeToChoice: " + trialLevel.AvgTimeToChoice_Block.ToString("0.00") + "s" +
                "\nTimeToCompletion: " + trialLevel.TimeToCompletion_Block.ToString("0.00") + "s" +
                "\nRewards: " + trialLevel.NumRewards_Block +
                "\nNonStimTouches: " + trialLevel.NonStimTouches_Block;
        if (blocksAdded > 1)
            CurrentBlockString += "\n";

        //Add CurrentBlockString if block wasn't aborted:
        if (trialLevel.AbortCode == 0)
            BlockSummaryString.AppendLine(CurrentBlockString.ToString());

        if (blocksAdded > 1) //If atleast 2 blocks to average, set Averages string and add to BlockSummaryString:
        {
            BlockAveragesString = "-------------------------------------------------" +
                                "\n" +
                                "\n<b>Block Averages (" + blocksAdded + " blocks):" + "</b>" +
                                "\nAvg Correct: " + AvgNumCorrect.ToString("0.00") +
                                "\nAvg TbCompletions: " + AvgNumTbCompletions.ToString("0.00") +
                                "\nAvg TimeToChoice: " + AvgTimeToChoice.ToString("0.00") + "s" +
                                "\nAvg TimeToCompletion: " + AvgTimeToCompletion.ToString("0.00") + "s" +
                                "\nAvg Rewards: " + AvgNumRewards.ToString("0.00") +
                                "\nAvg NonStimTouches: " + AvgNonStimTouches_Task.ToString("0.00") +
                                "\nStandard Deviation: " + StanDev.ToString("0.00");

            BlockSummaryString.AppendLine(BlockAveragesString.ToString());
        }

        //Add Previous blocks string:
        if(PreviousBlocksString.Length > 0)
            BlockSummaryString.AppendLine("\n" + PreviousBlocksString.ToString());
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
        if (TrialsCorrect_Task.Count >= 1)
            AvgNumCorrect = (float)TrialsCorrect_Task.AsQueryable().Average();

        if (TokenBarCompletions_Task.Count >= 1)
            AvgNumTbCompletions = (float)TokenBarCompletions_Task.AsQueryable().Average();
        
        if (TimeToChoice_Task.Count >= 1)
            AvgTimeToChoice = (float)TimeToChoice_Task.AsQueryable().Average();

        if (TimeToCompletion_Task.Count >= 1)
            AvgTimeToCompletion = (float)TimeToCompletion_Task.AsQueryable().Average();

        if (TotalRewards_Task.Count >= 1)
            AvgNumRewards = (float)TotalRewards_Task.AsQueryable().Average();

        if (NonStimTouches_Task.Count >= 1)
            AvgNonStimTouches_Task = (float)NonStimTouches_Task.AsQueryable().Average();
    }

}
