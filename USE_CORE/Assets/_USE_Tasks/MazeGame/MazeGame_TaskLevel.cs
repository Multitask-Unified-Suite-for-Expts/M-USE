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

public class MazeGame_TaskLevel : ControlLevel_Task_Template
{
    [HideInInspector] public int[] MazeNumSquares, MazeNumTurns;
    [HideInInspector] public Vector2[] MazeDims, MazeStart, MazeFinish;
    [HideInInspector] public string[] MazeName;

    public int totalErrors_InBlock,
        perseverativeErrors_InBlock,
        backtrackErrors_InBlock,
        ruleAbidingErrors_InBlock,
        ruleBreakingErrors_InBlock,
        retouchCorrect_InBlock,
        correctTouches_InBlock, 
        numRewardPulses_InBlock,
        nonStimTouches_InBlock;

    
    private List<int> totalErrors_InTask,
        perseverativeErrors_InTask,
        backtrackErrors_InTask,
        ruleAbidingErrors_InTask,
        ruleBreakingErrors_InTask,
        retouchCorrect_InTask,
        correctTouches_InTask,
        numRewardPulses_InTask;

    private float AvgTotalErrors,
        AvgPerseverativeErrors,
        AvgBacktrackErrors,
        AvgRuleAbidingErrors,
        AvgRuleBreakingErrors,
        AvgRetouchCorrect, 
        AvgCorrectTouches,
        AvgReward;

    [HideInInspector] public string BlockAveragesString;
    [HideInInspector] public string CurrentBlockString;
    [HideInInspector] public StringBuilder PreviousBlocksString;
    private int blocksAdded = 0;
    [HideInInspector] public MazeDef[] MazeDefs;
    private string mazeKeyFilePath;
    private MazeGame_TrialLevel mgTL;
    private int mIndex;
    public string TaskName = "MazeGame";
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
            
            //HARD CODED TO MINIMIZE EMPTY SKYBOX DURATION, CAN'T ACCESS TRIAL DEF YET & CONTEXT NOT IN BLOCK DEF
            RenderSettings.skybox = CreateSkybox(ContextExternalFilePath + Path.DirectorySeparatorChar + "Concrete3.png");
            mgTL.ContextActive = true;
         //   EventCodeManager.SendCodeNextFrame(TaskEventCodes["ContextOn"]);
            
            ResetBlockVariables();
            FindMaze();
            CalculateBlockSummaryString();
        });
        BlockFeedback.AddInitializationMethod(() =>
        {
            mgTL.DestroyChildren(GameObject.Find("MainCameraCopy"));
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
       // BlockData.AddDatum("NumNonStimSelections", () => mgTL.NonStimTouches_InBlock);
    }
    public void AddBlockValuesToTaskValues()
    {
        Debug.Log("TASK THING: " + (numRewardPulses_InTask == null ? "NULL" : "NOT NULL"));
        numRewardPulses_InTask.Add(numRewardPulses_InBlock);
        totalErrors_InTask.Add(totalErrors_InBlock);
        correctTouches_InTask.Add(correctTouches_InBlock);
        retouchCorrect_InTask.Add(retouchCorrect_InBlock);
        perseverativeErrors_InTask.Add(perseverativeErrors_InBlock);
        backtrackErrors_InTask.Add(backtrackErrors_InBlock);
        ruleAbidingErrors_InTask.Add(ruleAbidingErrors_InBlock);
        ruleBreakingErrors_InTask.Add(ruleBreakingErrors_InBlock);
        

        //TrialsCompleted_Task.Add(mgTL.NumTrials_Block);

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
    }
    public void CalculateBlockSummaryString()
    {
        ClearStrings();

        CurrentBlockString = "<b>Current Block:</b>" +
                             "\nTotal Errors: " + totalErrors_InBlock +
                             //"\nCorrect Touches: " + correctTouches_InBlock +
                             "\nRule-Abiding Errors: " + ruleAbidingErrors_InBlock +
                             "\nRule-Breaking Errors: " + ruleBreakingErrors_InBlock + 
                             "\nPerseverative Errors: " + perseverativeErrors_InBlock +
                             "\nBacktrack Errors: " + backtrackErrors_InBlock +
                             "\nRetouch Correct: " + retouchCorrect_InBlock +
                             "\nRewards: " + numRewardPulses_InBlock;
        // "\nAvgTimeToChoice: " + trialLevel.AvgTimeToChoice_Block.ToString("0.00") + "s" +
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
                              "\nAvg Reward: " + AvgReward.ToString("0.00");
            
            BlockSummaryString.AppendLine(BlockAveragesString.ToString());
        }

        //Add Previous blocks string:
        if(PreviousBlocksString.Length > 0)
        {
            BlockSummaryString.AppendLine("\n" + PreviousBlocksString.ToString());
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
            mgTL.ButtonScale = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonScale");
        else Debug.LogError("Start Button Scale settings not defined in the TaskDef");
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
    }
    private void LoadMazeDef()
    {
        SessionSettings.ImportSettings_SingleTypeArray<MazeDef>("MazeDefs", mazeKeyFilePath);
        MazeDefs = (MazeDef[])SessionSettings.Get("MazeDefs");
        MazeDims = new Vector2[MazeDefs.Length];
        MazeNumSquares = new int[MazeDefs.Length];
        MazeNumTurns = new int[MazeDefs.Length];
        MazeStart = new Vector2[MazeDefs.Length];
        MazeFinish = new Vector2[MazeDefs.Length];
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
        Debug.Log("MAZE DEF NAME: " + mgTL.mazeDefName);
    }
}