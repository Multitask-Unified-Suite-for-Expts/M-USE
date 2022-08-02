using System;
using USE_ExperimentTemplate;
using MazeGame_Namespace;
using USE_Settings;

public class MazeGame_TaskLevel : ControlLevel_Task_Template
{

    public override void DefineControlLevel()
    {
        MazeGame_TrialLevel mgTL = (MazeGame_TrialLevel)TrialLevel;
        string TaskName = "MazeGame";
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ContextExternalFilePath"))
            mgTL.MaterialFilePath = (String)SessionSettings.Get(TaskName + "_TaskSettings", "ContextExternalFilePath");
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