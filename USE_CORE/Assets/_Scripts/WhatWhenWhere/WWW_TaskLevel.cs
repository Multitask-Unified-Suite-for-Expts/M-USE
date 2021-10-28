using USE_ExperimentTemplate;
using WWW_Namespace;

public class WWW_TaskLevel : ControlLevel_Task_Template
{
	public override void SpecifyTypes()
	{
		//note that since EffortControl_TaskDef and EffortControl_BlockDef do not add any fields or methods to their parent types, 
		//they do not actually need to be specified here, but they are included to make this script more useful for later copying.
		TaskDefType = typeof(WWW_TaskDef); 
		BlockDefType = typeof(WWW_BlockDef);
		TrialDefType = typeof(WWW_TrialDef);
	}

	public override void DefineControlLevel()
	{
	}

}
