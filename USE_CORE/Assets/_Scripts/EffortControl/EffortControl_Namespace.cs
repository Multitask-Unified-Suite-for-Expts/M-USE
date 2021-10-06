using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using USE_ExperimentTemplate;
using USE_Settings;

namespace EffortControl_Namespace
{
	public class EffortControl_TaskDef : TaskDef
	{

	}

	public class EffortControl_BlockDef : BlockDef
	{

	}

	public class EffortControl_TrialDef : TrialDef
	{
		public int NumOfClicksLeft;
		public int NumOfClicksRight;
		public int NumOfCoinsLeft;
		public int NumOfCoinsRight;
		public int ClicksPerOutline;

	}
}
