using CognitiveMaze_Namespace;
using USE_ExperimentTemplate_Task;

public class CognitiveMaze_TaskLevel : ControlLevel_Task_Template
{
	public override void SpecifyTypes()
	{
		//note that since EffortControl_TaskDef and EffortControl_BlockDef do not add any fields or methods to their parent types, 
		//they do not actually need to be specified here, but they are included to make this script more useful for later copying.
		TaskLevelType = typeof(CognitiveMaze_TaskLevel);
		TrialLevelType = typeof(CognitiveMaze_TrialLevel);
		TaskDefType = typeof(CognitiveMaze_TaskDef);
		BlockDefType = typeof(CognitiveMaze_BlockDef);
		TrialDefType = typeof(CognitiveMaze_TrialDef);
	}

	public override void DefineControlLevel()
	{
	}

}
