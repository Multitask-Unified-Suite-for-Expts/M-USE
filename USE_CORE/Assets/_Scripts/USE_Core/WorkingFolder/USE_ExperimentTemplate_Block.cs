namespace USE_ExperimentTemplate
{
	public class BlockDef
	{
		public int BlockCount;
		public TrialDef[] TrialDefs;
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
