﻿using USE_ExperimentTemplate;
using EffortControl_Namespace;

public class EffortControl_TaskLevel : ControlLevel_Task_Template
{
	public override void SpecifyTypes()
	{
		//note that since EffortControl_TaskDef and EffortControl_BlockDef do not add any fields or methods to their parent types, 
		//they do not actually need to be specified here, but they are included to make this script more useful for later copying.
		TaskDefType = typeof(EffortControl_TaskDef); 
		BlockDefType = typeof(EffortControl_BlockDef);
		TrialDefType = typeof(EffortControl_TrialDef);
	}

	public override void DefineControlLevel()
	{
	}

}
