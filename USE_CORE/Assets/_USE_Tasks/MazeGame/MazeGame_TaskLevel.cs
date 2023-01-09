using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using USE_ExperimentTemplate_Task;
using USE_Settings;
using MazeGame_Namespace;
using HiddenMaze;
using USE_ExperimentTemplate_Block;

public class MazeGame_TaskLevel : ControlLevel_Task_Template
{
    [HideInInspector] public MazeDef[] MazeDefs;
    [HideInInspector] public int[] MazeDims, MazeNumSquares, MazeNumTurns;
    MazeGame_BlockDef mgBD => GetCurrentBlockDef<MazeGame_BlockDef>();


    public override void DefineControlLevel()
    {
        MazeGame_TrialLevel mgTL = (MazeGame_TrialLevel)TrialLevel;
        string TaskName = "MazeGame";
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ContextExternalFilePath"))
            mgTL.MaterialFilePath = (String)SessionSettings.Get(TaskName + "_TaskSettings", "ContextExternalFilePath");
        else Debug.LogError("Context External File Path not defined in the TaskDef");

        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "MazeExternalFilePath"))
            mgTL.MazeFilePath = (String)SessionSettings.Get(TaskName + "_TaskSettings", "MazeExternalFilePath");
        else Debug.LogError("Maze External File Path not defined in the TaskDef");

        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonPosition"))
            mgTL.ButtonPosition = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonPosition");
        else Debug.LogError("Start Button Position settings not defined in the TaskDef");

        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonScale"))
            mgTL.ButtonScale = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonScale");
        else Debug.LogError("Start Button Scale settings not defined in the TaskDef");

        string mazeKeyFilePath = "";
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "MazeKeyFilePath"))
            mazeKeyFilePath = (string)SessionSettings.Get(TaskName + "_TaskSettings", "MazeKeyFilePath");
        else Debug.LogError("Maze key file path settings not defined in the TaskDef");
        
        SetupTask.AddInitializationMethod(() =>
        {
            SessionSettings.ImportSettings_SingleTypeArray<Maze>("MazeDefs", mazeKeyFilePath);
            MazeDefs = (MazeDef[])SessionSettings.Get("MazeDefs");
            Debug.Log("MAZE DEF LENGTH " + MazeDefs.Length);
            
            MazeDims = new int[MazeDefs.Length];
            MazeNumSquares = new int[MazeDefs.Length];
            MazeNumTurns = new int[MazeDefs.Length];
            Debug.Log("MAZE DEF LENGTH " + MazeDefs.Length);
            for (int iMaze = 0; iMaze < MazeDefs.Length; iMaze++)
            {
                MazeDims[iMaze] = MazeDefs[iMaze].mTotalSquares;
                MazeNumSquares[iMaze] = MazeDefs[iMaze].mNumSquares;
                MazeNumTurns[iMaze] = MazeDefs[iMaze].mNumTurns;
            }
        });

        RunBlock.AddInitializationMethod(() =>
        {
            //for given block MazeDims, MazeNumSquares, MazeNumTurns, get all indices of that value, find intersect
            //then choose random member of intersect and assign to this block's trials
            int[] mazeDimsIndices = MazeDefs.Select((b, i) => b.mTotalSquares == mgBD.MazeDims ? i : -1).Where(i => i != -1).ToArray();
            Debug.Log("MAZE DIM INDICES: " + mazeDimsIndices);
         //   if (CurrentBlockDef.TileColor == null && TaskDef.TileColor != null)
           //     CurrentBlockDef.TileColor = TaskDef.TileColor;

         //   foreach (TrialDef td in CurrentBlockDef.TrialDefs)
          //      if (td.TileColor == null && CurrentBlockDef.TileColor != null)
             //       td.TileColor == CurrentBlockDef.TileColor
        });
        // if (CurrentBlockDef.TileColor == null && TaskDef.TileColor != null)
        //   CurrentBlockDef.TileColor = TaskDef.TileColor;

    }
    public override void ReadCustomSettingsFiles()
    {
        //string mazeDefFile = LocateFile.FindFileInFolder(TaskConfigPath, "*" + TaskName + "*MazeDef*");
        //SessionSettings.ImportSettings_SingleTypeArray<Maze>(TaskName + "_MazeDefs", mazeDefFile);
        //MazeDefs = (Maze[])SessionSettings.Get(TaskName + "_MazeDefs");
    }
    public T GetCurrentBlockDef<T>() where T : BlockDef
    {
        return (T)CurrentBlockDef;
    }
    public static class EM
    {
        public static int[] FindAllIndexof<T>(IEnumerable<T> values, T val)
        {
            return values.Select((b,i) => object.Equals(b, val) ? i : -1).Where(i => i != -1).ToArray();
        }
    }


}