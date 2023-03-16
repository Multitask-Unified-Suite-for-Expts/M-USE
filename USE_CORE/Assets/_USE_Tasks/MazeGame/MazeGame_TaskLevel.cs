using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using MazeGame_Namespace;
using UnityEngine;
using USE_ExperimentTemplate_Task;
using USE_Settings;
using USE_Utilities;
using Random = UnityEngine.Random;

public class MazeGame_TaskLevel : ControlLevel_Task_Template
{
    [HideInInspector] public int[] MazeNumSquares;
    [HideInInspector] public int[] MazeNumTurns;
    [HideInInspector] public Vector2[] MazeDims;
    [HideInInspector] public string[] MazeStart;
    [HideInInspector] public string[] MazeFinish;
    [HideInInspector] public string[] MazeName;
    
    [HideInInspector]
    public int totalErrors_InBlock;
    public int perseverativeErrors_InBlock;
    public int backtrackErrors_InBlock;
    public int ruleAbidingErrors_InBlock;
    public int ruleBreakingErrors_InBlock;
    public int retouchCorrect_InBlock;
    public int correctTouches_InBlock; 
    public int numRewardPulses_InBlock;
    public int numAbortedTrials_InBlock;
    public int nonStimTouches_InBlock;
    public int numSliderBarFull_InBlock;
    public List<float?> mazeDurationsList_InBlock = new List<float?>();

    private List<int> totalErrors_InTask;
    private List<int> perseverativeErrors_InTask;
    private List<int> backtrackErrors_InTask;
    private List<int> ruleAbidingErrors_InTask;
    private List<int> ruleBreakingErrors_InTask;
    private List<int> retouchCorrect_InTask;
    private List<int> correctTouches_InTask;
    private List<int> numRewardPulses_InTask;
    private List<int> numAbortedTrials_InTask;
    private List<int> numSliderBarFull_InTask;
    private List<List<float?>> mazeDurationsList_InTask;

    private float AvgTotalErrors;
    private float AvgPerseverativeErrors;
    private float AvgBacktrackErrors;
    private float AvgRuleAbidingErrors;
    private float AvgRuleBreakingErrors;
    private float AvgRetouchCorrect; 
    private float AvgCorrectTouches;
    private float AvgMazeDuration;
    private float AvgReward;

    [HideInInspector] public string BlockAveragesString;
    [HideInInspector] public string CurrentBlockString;
    [HideInInspector] public StringBuilder PreviousBlocksString;

    private int blocksAdded = 0;
    private MazeDef[] MazeDefs;
    private string mazeKeyFilePath;
    private MazeGame_TrialLevel mgTL;
    private int mIndex;
    private MazeGame_BlockDef mgBD => GetCurrentBlockDef<MazeGame_BlockDef>();


    public override void DefineControlLevel()
    {
        totalErrors_InTask = new List<int>();
        perseverativeErrors_InTask = new List<int>();
        backtrackErrors_InTask = new List<int>();
        ruleAbidingErrors_InTask = new List<int>();
        ruleBreakingErrors_InTask = new List<int>();
        retouchCorrect_InTask = new List<int>();
        correctTouches_InTask = new List<int>();
        numRewardPulses_InTask = new List<int>();
        numSliderBarFull_InTask = new List<int>();
        numAbortedTrials_InTask = new List<int>();
        mazeDurationsList_InTask = new List<List<float?>>();
        
        mgTL = (MazeGame_TrialLevel)TrialLevel;
        SetSettings();
        AssignBlockData();
        
        BlockAveragesString = "";
        CurrentBlockString = "";
        PreviousBlocksString = new StringBuilder();
        
        blocksAdded = 0;
        LoadMazeDef();
        RunBlock.AddInitializationMethod(() =>
        {
            if (mgTL.playerViewLoaded)
            {
                mgTL.DestroyChildren(GameObject.Find("MainCameraCopy"));
                mgTL.playerViewTextList.Clear();
                mgTL.playerViewLoaded = false;
            }
                
            RenderSettings.skybox = CreateSkybox(mgTL.GetContextNestedFilePath(ContextExternalFilePath, mgBD.ContextName, "LinearDark"));
            mgTL.contextName = mgBD.ContextName;
            EventCodeManager.SendCodeNextFrame(CustomTaskEventCodes["ContextOn"]);
            
            ResetBlockVariables();
            FindMaze();
            CalculateBlockSummaryString();
        });
        BlockFeedback.AddInitializationMethod(() =>
        {
            if (mgTL.AbortCode == 0)
            {
                CurrentBlockString += "\n" + "\n";
                CurrentBlockString = CurrentBlockString.Replace("Current Block", $"Block {blocksAdded + 1}");
                PreviousBlocksString.Insert(0,CurrentBlockString); //Add current block string to full list of previous blocks. 
                AddBlockValuesToTaskValues();
                blocksAdded++;
            }
            CalculateBlockAverages();
        });
    }
    public void AssignBlockData()
    {
        BlockData.AddDatum("TotalErrors", () => totalErrors_InBlock);
        BlockData.AddDatum("CorrectTouches", () => correctTouches_InBlock);
        BlockData.AddDatum("RetouchCorrect", () => retouchCorrect_InBlock);
        BlockData.AddDatum("PerseverativeErrors", () => perseverativeErrors_InBlock);
        BlockData.AddDatum("BacktrackErrors", () => backtrackErrors_InBlock);
        BlockData.AddDatum("RuleAbidingErrors", () => ruleAbidingErrors_InBlock);
        BlockData.AddDatum("RuleBreakingErrors", () => ruleBreakingErrors_InBlock);
        BlockData.AddDatum("NumRewardPulses", () => numRewardPulses_InBlock);
        BlockData.AddDatum("NumSliderBarFull", ()=>numSliderBarFull_InBlock);
        BlockData.AddDatum("NumAbortedTrials", ()=> numAbortedTrials_InBlock);
        BlockData.AddDatum("MazeDurations", () => String.Join(",",mazeDurationsList_InBlock));
       // BlockData.AddDatum("NumNonStimSelections", () => mgTL.NonStimTouches_InBlock);
    }
    public void AddBlockValuesToTaskValues()
    {
        numRewardPulses_InTask.Add(numRewardPulses_InBlock);
        totalErrors_InTask.Add(totalErrors_InBlock);
        correctTouches_InTask.Add(correctTouches_InBlock);
        retouchCorrect_InTask.Add(retouchCorrect_InBlock);
        perseverativeErrors_InTask.Add(perseverativeErrors_InBlock);
        backtrackErrors_InTask.Add(backtrackErrors_InBlock);
        ruleAbidingErrors_InTask.Add(ruleAbidingErrors_InBlock);
        ruleBreakingErrors_InTask.Add(ruleBreakingErrors_InBlock);
        numAbortedTrials_InTask.Add(numAbortedTrials_InBlock);
        numSliderBarFull_InTask.Add(numSliderBarFull_InBlock);
        mazeDurationsList_InTask.Add(mazeDurationsList_InBlock);
        Debug.Log("MAZE DURATIONS IN TASK?? " + String.Join(",",String.Join(",",mazeDurationsList_InTask.SelectMany(list => list))));
    }
    public override OrderedDictionary GetSummaryData()
    {
        OrderedDictionary data = new OrderedDictionary();

        data["Num Reward Pulses"] = numRewardPulses_InTask.AsQueryable().Sum();
        data["Total Errors"] = totalErrors_InTask.AsQueryable().Sum();
        data["Correct Touches"] = correctTouches_InTask.AsQueryable().Sum();
        data["Retouch Correct"] = retouchCorrect_InTask.AsQueryable().Sum();
        data["Perseverative Errors"] = perseverativeErrors_InTask.AsQueryable().Sum();
        data["Backtrack Errors"] = backtrackErrors_InTask.AsQueryable().Sum();
        data["Rule-Abiding Errors"] = ruleAbidingErrors_InTask.AsQueryable().Sum();
        data["Rule-Breaking Errors"] = ruleBreakingErrors_InTask.AsQueryable().Sum();
        data["Num Aborted Trials"] = numAbortedTrials_InTask.AsQueryable().Sum();
        data["Num Slider Bar Full"] = numSliderBarFull_InTask.AsQueryable().Sum();
        data["Maze Durations"] = mazeDurationsList_InTask.AsQueryable();
        return data;
    }
    private void ResetBlockVariables()
    {
        totalErrors_InBlock = 0;
        correctTouches_InBlock = 0;
        retouchCorrect_InBlock = 0;
        perseverativeErrors_InBlock = 0;
        backtrackErrors_InBlock = 0;
        ruleAbidingErrors_InBlock = 0;
        ruleBreakingErrors_InBlock = 0;
        numRewardPulses_InBlock = 0;
        nonStimTouches_InBlock = 0;
        numAbortedTrials_InBlock = 0;
        numSliderBarFull_InBlock = 0;
        mazeDurationsList_InBlock.Clear();
    }
    public void CalculateBlockSummaryString()
    {
        ClearStrings();

        CurrentBlockString = "<b>Block Num:</b>" + (BlockCount + 1) + 
                             "\nMaze Durations: " + String.Join(",",mazeDurationsList_InBlock) +
                             "\n\nTotal Errors: " + totalErrors_InBlock +
                             "\nRule-Abiding Errors: " + ruleAbidingErrors_InBlock +
                             "\nRule-Breaking Errors: " + ruleBreakingErrors_InBlock + 
                             "\nPerseverative Errors: " + perseverativeErrors_InBlock +
                             "\nBacktrack Errors: " + backtrackErrors_InBlock +
                             "\nRetouch Correct: " + retouchCorrect_InBlock +
                             "\n\nRewards: " + numRewardPulses_InBlock;
        
        if (blocksAdded > 1)
            CurrentBlockString += "\n";

        //Add CurrentBlockString if block wasn't aborted:
        if (mgTL.AbortCode == 0)
            BlockSummaryString.AppendLine(CurrentBlockString.ToString());


        if (blocksAdded > 1) //If atleast 2 blocks to average, set Averages string and add to BlockSummaryString:
        {
            BlockAveragesString = "-------------------------------------------------" +
                              "\n" +
                              "\n<b>Block Averages (" + blocksAdded + " blocks):" + "</b>" +
                              "\nAvg Total Errors: " + AvgTotalErrors.ToString("0.00") +
                              "\nAvg Correct Touches: " + AvgCorrectTouches.ToString("0.00") +
                              "\nAvg Rule-Abiding Errors: " + AvgRuleAbidingErrors.ToString("0.00") + "s" +
                              "\nAvg Rule-Breaking Errors: " + AvgRuleBreakingErrors.ToString("0.00") +
                              "\nAvg Preservative Errors: " + AvgPerseverativeErrors.ToString("0.00") +
                              "\nAvg Backtrack Errors: " + AvgBacktrackErrors.ToString("0.00") + "s" +
                              "\nAvg Retouch Correct: " + AvgRetouchCorrect.ToString("0.00") +
                              "\nAvg Reward: " + AvgReward.ToString("0.00") +
                              "\nAvg Maze Duration: " + AvgMazeDuration.ToString("0.00");;
            
            BlockSummaryString.AppendLine(BlockAveragesString.ToString());
        }

        //Add Previous blocks string:
        if(PreviousBlocksString.Length > 0)
        {
            BlockSummaryString.AppendLine("\n" + PreviousBlocksString.ToString());
        }
    }
    public override void SetTaskSummaryString()
    {
        if (mgTL.TrialCount_InTask != 0)
        {
            CurrentTaskSummaryString.Clear();
            CurrentTaskSummaryString.Append($"\n<b>{ConfigName}</b>" +
                                            $"\n<b># Trials:</b> {mgTL.TrialCount_InTask} ({(Math.Round(decimal.Divide(numAbortedTrials_InTask.AsQueryable().Sum(), (mgTL.TrialCount_InTask)), 2)) * 100}% aborted)" +
                                            $"\t<b># Blocks:</b> {BlockCount}" +
                                            $"\t<b># Reward Pulses:</b> {numRewardPulses_InTask.AsQueryable().Sum()}" +
                                            $"\n<b># Rule-Break Errors:</b> {ruleBreakingErrors_InTask.AsQueryable().Sum()}" +
                                            $"\t<b># Rule-AbidingErrors:</b> {ruleAbidingErrors_InTask.AsQueryable().Sum()}" +
                                         //   $"\nAccuracy: {(Math.Round(decimal.Divide(NumCorrect_InTask, (flTL.TrialCount_InTask)), 2)) * 100}%" +
                                            $"\nAvg Maze Duration: {AvgMazeDuration}" +
                                            $"\n# Slider Bar Filled: {numSliderBarFull_InTask.AsQueryable().Sum()}");
        }
        else
        {
            CurrentTaskSummaryString.Append($"\n<b>{ConfigName}</b>");
        }
    }
    public void ClearStrings()
    {
        BlockAveragesString = "";
        CurrentBlockString = "";
        BlockSummaryString.Clear();
    }
    private void CalculateBlockAverages()
    {
        if (totalErrors_InTask.Count >= 1)
            AvgTotalErrors = (float)totalErrors_InTask.AsQueryable().Average();
        
        if (correctTouches_InTask.Count >= 1)
            AvgCorrectTouches = (float)correctTouches_InTask.AsQueryable().Average();

        if (retouchCorrect_InTask.Count >= 1)
            AvgRetouchCorrect = (float)retouchCorrect_InTask.AsQueryable().Average();

        if (perseverativeErrors_InTask.Count >= 1)
            AvgPerseverativeErrors = (float)perseverativeErrors_InTask.AsQueryable().Average();

        if (backtrackErrors_InTask.Count >= 1)
            AvgBacktrackErrors = (float)backtrackErrors_InTask.AsQueryable().Average();
        
        if (ruleAbidingErrors_InTask.Count >= 1)
            AvgRuleAbidingErrors = (float)ruleAbidingErrors_InTask.AsQueryable().Average();
        
        if (ruleBreakingErrors_InTask.Count >= 1)
            AvgRuleBreakingErrors = (float)ruleBreakingErrors_InTask.AsQueryable().Average();

        if (numRewardPulses_InTask.Count >= 1)
            AvgReward = (float)numRewardPulses_InTask.AsQueryable().Average();

        if (mazeDurationsList_InTask.Count >= 1)
            AvgMazeDuration = mazeDurationsList_InTask.SelectMany(list => list)
                .Average((float? duration) => duration ?? 0);
    }
    private void SetSettings()
    {
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ContextExternalFilePath"))
            mgTL.ContextExternalFilePath =
                (string)SessionSettings.Get(TaskName + "_TaskSettings", "ContextExternalFilePath");
        else mgTL.ContextExternalFilePath = ContextExternalFilePath;

        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "MazeKeyFilePath"))
            mazeKeyFilePath = (string)SessionSettings.Get(TaskName + "_TaskSettings", "MazeKeyFilePath");
        else Debug.LogError("Maze key file path settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "MazeFilePath"))
            mgTL.MazeFilePath = (string)SessionSettings.Get(TaskName + "_TaskSettings", "MazeFilePath");
        else Debug.LogError("Maze File Path not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonPosition"))
            mgTL.ButtonPosition = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonPosition");
        else Debug.LogError("Start Button Position settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonScale"))
            mgTL.ButtonScale = (float)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonScale");
        else Debug.LogError("Start Button Scale settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "NeutralITI"))
            mgTL.NeutralITI = (bool)SessionSettings.Get(TaskName + "_TaskSettings", "NeutralITI");
        else
        {
            mgTL.NeutralITI = false;
            Debug.Log("Neutral ITI settings not defined in the TaskDef. Default Setting of false is used instead");
        }
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "TileSize"))
        {
            mgTL.TileSize = (float)SessionSettings.Get(TaskName + "_TaskSettings", "TileSize");
        }
        else
        {
            mgTL.TileSize = 0.5f; // default value in the case it isn't specified
            Debug.Log("Tile Size settings not defined in the TaskDef. Default setting of " + mgTL.TileSize +
                      " is used instead.");
        }

        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "TileTexture"))
        {
            mgTL.TileTexture = (string)SessionSettings.Get(TaskName + "_TaskSettings", "TileTexture");
        }
        else
        {
            mgTL.TileTexture = "Tile"; // default value in the case it isn't specified
            Debug.Log("Tile Texture settings not defined in the TaskDef. Default setting of " + mgTL.TileTexture +
                      " is used instead.");
        }

        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "NumBlinks"))
            mgTL.NumBlinks = (int)SessionSettings.Get(TaskName + "_TaskSettings", "NumBlinks");
        else Debug.LogError("Num Blinks settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "StartColor"))
            mgTL.startColor = (float[])SessionSettings.Get(TaskName + "_TaskSettings", "StartColor");
        else Debug.LogError("Start Color settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "FinishColor"))
            mgTL.finishColor = (float[])SessionSettings.Get(TaskName + "_TaskSettings", "FinishColor");
        else Debug.LogError("Finish Color settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "CorrectColor"))
            mgTL.correctColor = (float[])SessionSettings.Get(TaskName + "_TaskSettings", "CorrectColor");
        else Debug.LogError("Correct Color settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "LastCorrectColor"))
            mgTL.lastCorrectColor = (float[])SessionSettings.Get(TaskName + "_TaskSettings", "LastCorrectColor");
        else Debug.LogError("Last Correct Color settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "IncorrectRuleAbidingColor"))
            mgTL.incorrectRuleAbidingColor =
                (float[])SessionSettings.Get(TaskName + "_TaskSettings", "IncorrectRuleAbidingColor");
        else Debug.LogError("Incorrect Rule Abiding Color settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "IncorrectRuleBreakingColor"))
            mgTL.incorrectRuleBreakingColor =
                (float[])SessionSettings.Get(TaskName + "_TaskSettings", "IncorrectRuleBreakingColor");
        else Debug.LogError("Incorrect Rule Breaking Color settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "DefaultTileColor"))
            mgTL.defaultTileColor = (float[])SessionSettings.Get(TaskName + "_TaskSettings", "DefaultTileColor");
        else Debug.LogError("Default Tile Color settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "FixedRatioReward"))
            mgTL.UsingFixedRatioReward = (bool)SessionSettings.Get(TaskName + "_TaskSettings", "FixedRatioReward");
        else
        {
            mgTL.UsingFixedRatioReward = false;
            Debug.Log("Fixed Ratio Reward settings not defined in the TaskDef, set as default of false");
        }
    }
    private void LoadMazeDef()
    {
        SessionSettings.ImportSettings_SingleTypeArray<MazeDef>("MazeDefs", mazeKeyFilePath);
        MazeDefs = (MazeDef[])SessionSettings.Get("MazeDefs");
        MazeDims = new Vector2[MazeDefs.Length];
        MazeNumSquares = new int[MazeDefs.Length];
        MazeNumTurns = new int[MazeDefs.Length];
        MazeStart = new string[MazeDefs.Length];
        MazeFinish = new string[MazeDefs.Length];
        MazeName = new string[MazeDefs.Length];
        for (var iMaze = 0; iMaze < MazeDefs.Length; iMaze++)
        {
            MazeDims[iMaze] = MazeDefs[iMaze].mDims;
            MazeNumSquares[iMaze] = MazeDefs[iMaze].mNumSquares;
            MazeNumTurns[iMaze] = MazeDefs[iMaze].mNumTurns;
            MazeStart[iMaze] = MazeDefs[iMaze].mStart;
            MazeFinish[iMaze] = MazeDefs[iMaze].mFinish;
            MazeName[iMaze] = MazeDefs[iMaze].mName;
        }
    }
    private void FindMaze()
    {
        //for given block MazeDims, MazeNumSquares, MazeNumTurns, get all indices of that value, find intersect
        //then choose random member of intersect and assign to this block's trials

        if (mgBD.MazeName != null)
        {
            mIndex = MazeName.FindAllIndexof(mgBD.MazeName)[0];
        }
        else
        {
            var mdIndices = MazeDims.FindAllIndexof(mgBD.MazeDims);
            var mnsIndices = MazeNumSquares.FindAllIndexof(mgBD.MazeNumSquares);
            var mntIndices = MazeNumTurns.FindAllIndexof(mgBD.MazeNumTurns);
            var msIndices = MazeStart.FindAllIndexof(mgBD.MazeStart);
            var mfIndices = MazeFinish.FindAllIndexof(mgBD.MazeFinish);
            var possibleMazeDefIndices = mfIndices
                .Intersect(msIndices.Intersect(mntIndices.Intersect(mdIndices.Intersect(mnsIndices)))).ToArray();

            mIndex = possibleMazeDefIndices[Random.Range(0, possibleMazeDefIndices.Length)];

            //remove the maze specifications from all of the arrays so the maze won't repeat on subsequent blocks of the same conditions
            MazeDefs = MazeDefs.Where((source, index) => index != mIndex).ToArray();
            MazeDims = MazeDims.Where((source, index) => index != mIndex).ToArray();
            MazeNumSquares = MazeNumSquares.Where((source, index) => index != mIndex).ToArray();
            MazeNumTurns = MazeNumTurns.Where((source, index) => index != mIndex).ToArray();
            MazeStart = MazeStart.Where((source, index) => index != mIndex).ToArray();
            MazeFinish = MazeFinish.Where((source, index) => index != mIndex).ToArray();
            MazeName = MazeName.Where((source, index) => index != mIndex).ToArray();
        }

        mgTL.mazeDefName = MazeName[mIndex];
    }
}