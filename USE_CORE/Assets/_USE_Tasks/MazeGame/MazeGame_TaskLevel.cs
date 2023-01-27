using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using USE_ExperimentTemplate_Task;
using USE_Settings;
using MazeGame_Namespace;
using HiddenMaze;
using USE_ExperimentTemplate_Block;
using USE_Utilities;
using Random = UnityEngine.Random;

public class MazeGame_TaskLevel : ControlLevel_Task_Template
{
    [HideInInspector] public MazeDef[] MazeDefs;
    [HideInInspector] public int[] MazeNumSquares, MazeNumTurns;
    public Vector2[] MazeDims;
    [HideInInspector] public string[] MazeName;
    MazeGame_BlockDef mgBD => GetCurrentBlockDef<MazeGame_BlockDef>();


    public override void DefineControlLevel()
    {
        MazeGame_TrialLevel mgTL = (MazeGame_TrialLevel)TrialLevel;
        string TaskName = "MazeGame";
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ContextExternalFilePath"))
            mgTL.ContextExternalFilePath = (String)SessionSettings.Get(TaskName + "_TaskSettings", "ContextExternalFilePath");
        else Debug.LogError("Context External File Path not defined in the TaskDef");

        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "MazeFilePath"))
            mgTL.MazeFilePath = (String)SessionSettings.Get(TaskName + "_TaskSettings", "MazeFilePath");
        else Debug.LogError("Maze File Path not defined in the TaskDef");

        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonPosition"))
            mgTL.ButtonPosition = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonPosition");
        else Debug.LogError("Start Button Position settings not defined in the TaskDef");

        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonScale"))
            mgTL.ButtonScale = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonScale");
        else Debug.LogError("Start Button Scale settings not defined in the TaskDef");
        /*
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "TileColor"))
            mgTL.TileColor = (Color)SessionSettings.Get(TaskName + "_TaskSettings", "TileColor");
        else Debug.LogError("Tile Color settings not defined in the TaskDef");
*/
        string mazeKeyFilePath = "";
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "MazeKeyFilePath"))
            mazeKeyFilePath = (string)SessionSettings.Get(TaskName + "_TaskSettings", "MazeKeyFilePath");
        else Debug.LogError("Maze key file path settings not defined in the TaskDef");
        
        SetupTask.AddInitializationMethod(() =>
        {
            SessionSettings.ImportSettings_SingleTypeArray<MazeDef>("MazeDefs", mazeKeyFilePath);
            MazeDefs = (MazeDef[])SessionSettings.Get("MazeDefs");
            MazeDims = new Vector2[MazeDefs.Length];
            MazeNumSquares = new int[MazeDefs.Length];
            MazeNumTurns = new int[MazeDefs.Length];
            MazeName = new string[MazeDefs.Length];
            for (int iMaze = 0; iMaze < MazeDefs.Length; iMaze++)
            {
                MazeDims[iMaze] = MazeDefs[iMaze].mDims;
                MazeNumSquares[iMaze] = MazeDefs[iMaze].mNumSquares;
                MazeNumTurns[iMaze] = MazeDefs[iMaze].mNumTurns;
                MazeName[iMaze] = MazeDefs[iMaze].mName;
            }
        });

        RunBlock.AddInitializationMethod(() =>
        {
            //for given block MazeDims, MazeNumSquares, MazeNumTurns, get all indices of that value, find intersect
            //then choose random member of intersect and assign to this block's trials
            
            int[] mdIndices = MazeDims.FindAllIndexof(mgBD.MazeDims);
            Debug.Log("INDEX OF mgBD.MazeDims: " + mdIndices.ToString());
            int[] mnsIndices = MazeNumSquares.FindAllIndexof(mgBD.MazeNumSquares);
            int[] mntIndices = MazeNumTurns.FindAllIndexof(mgBD.MazeNumTurns);
            int[] possibleMazeDefIndices = mntIndices.Intersect(mdIndices.Intersect(mnsIndices)).ToArray();
            
            int chosenIndex = possibleMazeDefIndices[Random.Range(0, possibleMazeDefIndices.Length)];
            mgTL.mazeDefName = MazeName[chosenIndex];
            Debug.Log("MAZE DEF NAME: " + mgTL.mazeDefName);
            //remove the maze specifications from all of the arrays
            MazeDefs = MazeDefs.Where((source, index) =>index != chosenIndex).ToArray();
            MazeDims = MazeDims.Where((source, index) =>index != chosenIndex).ToArray();
            MazeNumSquares = MazeNumSquares.Where((source, index) =>index != chosenIndex).ToArray();
            MazeNumTurns = MazeNumTurns.Where((source, index) =>index != chosenIndex).ToArray();
            MazeName = MazeName.Where((source, index) =>index != chosenIndex).ToArray();
        });
    }
    public T GetCurrentBlockDef<T>() where T : BlockDef
    {
        return (T)CurrentBlockDef;
    }
    


}
