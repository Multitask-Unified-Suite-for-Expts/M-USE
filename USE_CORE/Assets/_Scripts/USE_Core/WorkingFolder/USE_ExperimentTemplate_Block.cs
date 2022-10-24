using System.Collections.Generic;
using USE_ExperimentTemplate_Trial;

namespace USE_ExperimentTemplate_Block
{
	public class BlockDef
	{
		public int BlockCount;
		public List<TrialDef> TrialDefs;
		public int? TotalTokensNum;
		public int? MinTrials, MaxTrials;

		public virtual void GenerateTrialDefsFromBlockDef()
		{
		}

		public virtual void AddToTrialDefsFromBlockDef()
		{
		}

		public virtual void BlockInitializationMethod()
		{
		}
	}
}
