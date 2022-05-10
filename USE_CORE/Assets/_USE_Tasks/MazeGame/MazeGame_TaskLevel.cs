using USE_ExperimentTemplate;
using MazeGame_Namespace;

public class MazeGame_TaskLevel : ControlLevel_Task_Template
{

    public override void DefineControlLevel()
    {
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