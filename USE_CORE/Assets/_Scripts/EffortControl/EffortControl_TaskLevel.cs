using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using USE_ExperimentTemplate;
using USE_Settings;
using USE_States;
using EffortControl_Namespace;

public class EffortControl_TaskLevel : ControlLevel_Task_Template
{
	public override void SpecifyTypes()
	{
		TaskDefType = typeof(EffortControl_TaskDef);
		BlockDefType = typeof(EffortControl_BlockDef);
		TrialDefType = typeof(EffortControl_TrialDef);
	}

	public override void DefineControlLevel()
	{
	}

}

