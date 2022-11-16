using System;
using UnityEngine;
using USE_ExperimentTemplate_Task;
using USE_Settings;
using MazeGame_Namespace;

public class MazeGame_TaskLevel : ControlLevel_Task_Template
{

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

        RunBlock.AddInitializationMethod(() =>
        {
            
         //   if (CurrentBlockDef.TileColor == null && TaskDef.TileColor != null)
           //     CurrentBlockDef.TileColor = TaskDef.TileColor;

         //   foreach (TrialDef td in CurrentBlockDef.TrialDefs)
          //      if (td.TileColor == null && CurrentBlockDef.TileColor != null)
             //       td.TileColor == CurrentBlockDef.TileColor
        });
        // if (CurrentBlockDef.TileColor == null && TaskDef.TileColor != null)
        //   CurrentBlockDef.TileColor = TaskDef.TileColor;

    }


}