using USE_ExperimentTemplate;

namespace EffortControl_Namespace
{
	//There is no need for the EffortControl_TaskDef or EffortControl_BlockDef 
	//classes to be defined here, as they add nothing to their parent classes.
	//However they are left here to make this script an easier template to copy.
	public class EffortControl_TaskDef : TaskDef
	{
        //testing
	}

	public class EffortControl_BlockDef : BlockDef
	{

	}

	public class EffortControl_TrialDef : TrialDef
	{
		public string TrialName;
		public int TrialCode;
		public int ContextNum;
		public string ConditionName;
		public string ContextName;
		public int NumOfClicksLeft;
		public int NumOfClicksRight;
		public int NumOfCoinsLeft;
		public int NumOfCoinsRight;
		public int ClicksPerOutline;
		public float[] TouchOnOffTimeRange;
		public float InitialChoiceMinDuration;
		public float StarttoTapDispDelay;
		public float FinalTouchToVisFeedbackDelay;
		public float FinalTouchToRewardDelay;
	}

	//Any other custom classes useful for the functioning of the task could be included in this namespace.
}
