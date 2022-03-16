using USE_ExperimentTemplate;

namespace CognitiveMaze_Namespace
{
	//There is no need for the EffortControl_TaskDef or EffortControl_BlockDef 
	//classes to be defined here, as they add nothing to their parent classes.
	//However they are left here to make this script an easier template to copy.
	public class CognitiveMaze_TaskDef : TaskDef
	{

	}

	public class CognitiveMaze_BlockDef : BlockDef
	{

	}

	public class CognitiveMaze_TrialDef : TrialDef
	{
        public int Trial;

    }

    //Any other custom classes useful for the functioning of the task could be included in this namespace.
}
