using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using USE_ExperimentTemplate_Task;
using USE_Settings;
using MazeGame_Namespace;
using HiddenMaze;
using Newtonsoft.Json;
using USE_ExperimentTemplate_Block;
using USE_Utilities;
using Random = UnityEngine.Random;

public class MazeGame_TaskLevel : ControlLevel_Task_Template
{
    [HideInInspector] public MazeDef[] MazeDefs;
    [HideInInspector] public int[] MazeNumSquares, MazeNumTurns;
    [HideInInspector]public Vector2[] MazeDims, MazeStart, MazeFinish;
    [HideInInspector] public string[] MazeName;
    private int mIndex;
    MazeGame_BlockDef mgBD => GetCurrentBlockDef<MazeGame_BlockDef>();
    MazeGame_TrialLevel mgTL;
    private string mazeKeyFilePath;
    public int totalErrors_InBlock = 0,
    perseverativeErrors_InBlock = 0,
    backtrackErrors_InBlock = 0,
    ruleAbidingErrors_InBlock = 0,
    ruleBreakingErrors_InBlock = 0,
    retouchCorrect_InBlock = 0,
    correctTouches_InBlock = 0;

    public override void DefineControlLevel()
    {
        mgTL = (MazeGame_TrialLevel)TrialLevel;
        SetSettings();
        AssignBlockData();
        
        SetupTask.AddInitializationMethod(() =>
        { 
            //HARD CODED TO MINIMIZE EMPTY SKYBOX DURATION, CAN'T ACCESS TRIAL DEF YET & CONTEXT NOT IN BLOCK DEF
            RenderSettings.skybox = CreateSkybox(ContextExternalFilePath + Path.DirectorySeparatorChar +  "Concrete3.png");
            LoadMazeDef();
        });

        RunBlock.AddInitializationMethod(() =>
        {
            ResetBlockVariables();
            FindMaze();
        });
        
        
    }

    public void AssignBlockData()
    {
        BlockData.AddDatum("TotalErrors", ()=> totalErrors_InBlock);
        BlockData.AddDatum("CorrectTouches", ()=>correctTouches_InBlock);
        BlockData.AddDatum("RetouchCorrect", ()=>retouchCorrect_InBlock);
        BlockData.AddDatum("PerseverativeErrors", ()=> perseverativeErrors_InBlock);
        BlockData.AddDatum("BacktrackErrors", ()=>backtrackErrors_InBlock);
        BlockData.AddDatum("RuleAbidingErrors", ()=>ruleAbidingErrors_InBlock);
        BlockData.AddDatum("RuleBreakingErrors", ()=>ruleBreakingErrors_InBlock);
        BlockData.AddDatum("NumRewardPulses", ()=>mgTL.NumRewardPulses_InBlock);
        BlockData.AddDatum("NumNonStimSelections", ()=>mgTL.NumNonStimSelections_InBlock);
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
        mgTL.NumRewardPulses_InBlock = 0;
        mgTL.NumNonStimSelections_InBlock = 0;
    }

    private void SetSettings()
    {
        string TaskName = "MazeGame";
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ContextExternalFilePath"))
            mgTL.ContextExternalFilePath = (String)SessionSettings.Get(TaskName + "_TaskSettings", "ContextExternalFilePath");
        else mgTL.ContextExternalFilePath = ContextExternalFilePath;
        
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "MazeKeyFilePath"))
            mazeKeyFilePath = (string)SessionSettings.Get(TaskName + "_TaskSettings", "MazeKeyFilePath");
        else Debug.LogError("Maze key file path settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "MazeFilePath"))
            mgTL.MazeFilePath = (String)SessionSettings.Get(TaskName + "_TaskSettings", "MazeFilePath");
        else Debug.LogError("Maze File Path not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonPosition"))
            mgTL.ButtonPosition = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonPosition");
        else Debug.LogError("Start Button Position settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonScale"))
            mgTL.ButtonScale = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonScale");
        else Debug.LogError("Start Button Scale settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "TileSize"))
            mgTL.TileSize = (float)SessionSettings.Get(TaskName + "_TaskSettings", "TileSize");
        else
        {
            mgTL.TileSize = 0.5f; // default value in the case it isn't specified
            Debug.Log("Tile Size settings not defined in the TaskDef. Default setting of " + mgTL.TileSize + " is used instead.");
        }
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "TileTexture"))
            mgTL.TileTexture = (string)SessionSettings.Get(TaskName + "_TaskSettings", "TileTexture");
        else
        {
            mgTL.TileTexture = "Tile"; // default value in the case it isn't specified
            Debug.Log("Tile Texture settings not defined in the TaskDef. Default setting of " + mgTL.TileTexture + " is used instead.");
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
            mgTL.incorrectRuleAbidingColor = (float[])SessionSettings.Get(TaskName + "_TaskSettings", "IncorrectRuleAbidingColor");
        else Debug.LogError("Incorrect Rule Abiding Color settings not defined in the TaskDef");
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "IncorrectRuleBreakingColor"))
            mgTL.incorrectRuleBreakingColor = (float[])SessionSettings.Get(TaskName + "_TaskSettings", "IncorrectRuleBreakingColor");
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
        for (int iMaze = 0; iMaze < MazeDefs.Length; iMaze++)
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
            
        if (mgBD.MazeName != null) mIndex = MazeName.FindAllIndexof(mgBD.MazeName)[0];
        else
        {
            int[] mdIndices = MazeDims.FindAllIndexof(mgBD.MazeDims);
            int[] mnsIndices = MazeNumSquares.FindAllIndexof(mgBD.MazeNumSquares);
            int[] mntIndices = MazeNumTurns.FindAllIndexof(mgBD.MazeNumTurns);
            int[] msIndices = MazeStart.FindAllIndexof(mgBD.MazeStart);
            int[] mfIndices = MazeFinish.FindAllIndexof(mgBD.MazeFinish);
            int[] possibleMazeDefIndices = mfIndices
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
