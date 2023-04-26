
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using USE_Settings;

namespace FLU_Common_Namespace{

    [System.Serializable]
	///<summary>Parameters that govern behaviour of the experiment as a whole.</summary>
	public class ExptParameters{
//        //Stim and Data Paths
//        string    relevantObjectPath    "/Users/marcus/Desktop/MG_Uncertainty/Stims/Quaddles/“
//        string    irrelevantObjectPath    “/Users/marcus/Desktop/MG_Uncertainty/Stims/Irrelevant/“
//        string    dataPath    “/Users/marcus/Desktop/MG_Uncertainty/Data/“
//
//        //Stim Naming Conventions
//        //S##_P##_C##&&&@@_##&&&@@_T##_A##_E##
//        string    stimFileType    “fbx”
//        //Each element in featureNames is a list of strings, each element in the list gives the exact string that refers to a particular value on a corresponding dimension. The first value is the “neutral” value on that dimension, the subsequent values are values 1, 2, 3, etc.
//        list<string>[] featureNames    {{“S00”, “S01”, “S02”, “S03”, “S04”}, {“P00”, “P01”, “P02”, “P03”, “P04”}, …
//            {“C6000000_6000000”, “C7523050_4000000”, “C7518099_4000000”, “C7502570_4000000”, “C7533870_4000000”}, {“T00”, “T01”, “T02”, “T03”, “T04”}, …
//            {“A00_E00”, “A01_E01”, “A02_E01”, “A02_E02”, “A02_E03”}};
//
//        //Hardware Connection Details
//        string    neuralAcquisitionDevice    "Neuralynx"
//        string    neurarduinoCommProtocol    "serial"
//        string    neurarduinoAddress    "/dev/tty.usbmodem1411"
//        int    serialPortSpeed    115200
//        int    eyetrackerType    1    //1 = mouse (faked), 2 = Tobii
//
//        //Sequence  Epochs Used
//        string[]    exptSequenceEpochs    [“Init”, “Calibration”, “Tutorial”, “Block”]
//        string[]    blockSequenceEpochs    [“LoadStims”, “Trial”, “Pause”]
//        string[]    blockSequenceTerminationCriterion    [“MinMaxTrials_AccuracyThreshold”] 
//        ***option to have block-specific version as opposed to default
//        string[]    trialSequenceEpochs    [“Wait”, “Baseline”, “CovertPrep”, “Sample”, “Choice”, “Choice2FB”, “VisualFB”, “Reward”, “ITI”]
//
//        ***way to specify standalone epochs
//        string[]    trialStandaloneEpochs    [“WaitError1” “baselineError1”]
//
//        //Epoch parameter specifications
//        //depends on specific epochs used
//        //e.g:
//        WAIT_blinkOnDuration = [1 0.5 1]
//            WAIT_blinkOffDuration = [1 0.5 1]
		/// <summary>
		/// Path of folder containing relevant objects (e.g. quaddles).
		/// </summary>
        public string RelevantObjectPath;
		/// <summary>
		/// Path of folder containing irrelevant objects that are not rewarded.
		/// </summary>
		public string IrrelevantObjectPath;
		/// <summary>
		/// The relevant object scale - size will be multiplied by this value in all dimensions.
		/// </summary>
        public float RelevantObjectScale;
		/// <summary>
		/// The irrelevant object scale - size will be multiplied by this value in all dimensions.
		/// </summary>
		public float IrrelevantObjectScale;
		/// <summary>
		/// The context path.
		/// </summary>
        public string ContextPath;
		/// <summary>
		/// The trial def tdf path.
		/// </summary>
		public string TrialDefTdfPath;
		public string RelevantStimDefTdfPath;
		public string IrrelevantStimDefTdfPath;
		public string[] ContextNames;
		public string[] ContextNums;
        public string DataPath;
        public string StimExtension;
        public string PatternlessNeutralColorName;
        public List<string>[] FeatureNames;
        public string NeuralAcquisitionDevice;
        public string NeurarduinoAddress;
        public int SerialPortSpeed;
        public string[] ExptSequenceEpochs;
        public string[] BlockSequenceEpochs;
        public string BlockSequenceTerminationCriterion;
        public string[] TrialSequenceEpochs;
        public string[] TrialStandaloneEpochs;
        [System.NonSerialized]
        public Material[] ContextMaterials;
    }

    //BlockDefinitions
    //each blockDef object defines a complete block, they are put into a list that defines an entire session
    //blockDef fields:
    //    BlockID:    string, (OPTIONAL) used for reference purposes
    //    TrialRange:    int[], length = 2, [min # of trials, max # of trials]
    //    BlockCode:    int, (OPTIONAL) code for data analysis purposes (e.g. block type reference)
    //    RuleInfo:    ruleInfo[], each element specifies one rule (can be any number of rules per block) - see description below
    //    LowRewardProb:    float, specifies probability of reward for any chosen objects that are not specified by a rule
    //    LowRewardMagnitude:    float, specifies magnitude of reward for any chosen objects that are not specified by a rule (how this is interpreted will depend on particular implementation, it could refer to # of pulses given to reward system, # of points shown on screen, volume of fluid, etc)
    //    ContextNums:    int[], (OPTIONAL) array of numbers referring to the contexts that are shown in the present block (how this is interpreted depends on particular implementation)
    //    ContextNames:    string[], (OPTIONAL) array of string names referring to the contexts that are shown in the present block (again, how this is interpreted depends on particular implementation)
    //    ActiveFeatureTemplate:    list<int>[], (OPTIONAL)length = 5 (number of possible dimensions), each element specifies which feature values are active in current search space, if contains only 0 that dimension is neutral. E.g. [{0}, {0}, {1, 3}, {0}, {2, 3}] specifies that dim 3 values 1 and 3 and dim 5 values 2 and 3, are active, all other dimensions are neutral.
    //    ActiveObjectArray:    string[], (OPTIONAL) a list of filenames to the relevant objects used in the current block, which should be found inside the relevantObjectPath folder
    //    ActiveObjectFolder:    string, (OPTIONAL) a folder name indicating a subfolder of relevantObjectPath, which contains all the relevant objects for this block
    //    NumIrrelevantObjects:    int[], 2 values, minimum and maximum number of irrelevant objects on a trial
    //    IrrelevantObjectArray:    array[], (OPTIONAL) a list of filenames to the irrelevant objects used in the current block, which should be found inside the irrelPath folder
    //    IrrelevantObjectFolder:    string, (OPTIONAL) a folder name indicating a subfolder of irrelPath, which contains all the irrelevant objects for this block
    //        if neither irrelevantObjectList nor irrelevantObjectFolder is provided, program will load NumIrrelevantObjects, randomly selected from the irrelPath
    //    TrialDefPath    string, (OPTIONAL) a pointer to a text file containing a list of trialDef objects that specify all the possible trials for this block, which will be presented in order given (default), or according to a order definition specified by an optional order field. Should be the same length as the second element of trialRange (max trial #). 
    //    TrialOrder (OPTIONAL)    string, flag to determine trial sequence order
    //        if trialListPath is not specified, a trial list will be generated automatically based on ruleInfo, ActiveFeatureTemplate, and objects specified by the object arguments described above

    [System.Serializable]
    public class BlockDef{
        public string BlockID;
        public int BlockCode;
        public int[] TrialRange;
        public RuleDef[] RuleArray;
        public float BasePositiveFbProb;
		public float BaseRewardProb;
        public float BaseRewardMag;
		public int? NumTotalTokens;
		public int? NumInitialTokens;
        public Reward[] BaseTokenRewardsPositive;
		public Reward[] BaseTokenRewardsNegative;
        public bool? PlayTokenOnPositiveSound;
        public bool? PlayTokenOnNegativeSound;
        public bool? PlayFbUpdatePositiveSound;
        public bool? PlayFbUpdateNegativeSound;
        public bool? PlayRewardSound;
		public int[] ContextNums;
		public int[] ItiContextNums;
        public string[] ContextNames;
        public string SonicationTiming; 
        //possible SonicationTiming values include: 
		//"StimOnset", "NegativeFeedbackOnset", "PositiveFeedbackOnset", "AllFeedbackOnset", "RewardOnset", 
		//"AnyStimSelection", "RewardedStimSelection", "UnrewardedStimSelection"
		public int? MaxConsecutiveSonicationTrials;
		public int? NumTrialsWithoutSonicationAfterMax;
        public List<int>[] ActiveFeatureTemplate;
        public string ActiveObjectFolder;
        public string[] ActiveObjectArray;
        public int[] NumRelevantObjectsPerTrial;
        public int[] NumIrrelevantObjectsPerTrial;
        public int NumIrrelevantObjectsPerBlock ;
        public string[] IrrelevantObjectArray;
        public string IrrelevantObjectFolder;
        public string TrialOrder;
        public string TrialDefPath;
		public string TrialDefTdfPath;
		public string RelevantStimDefTdfPath;
		public string IrrelevantStimDefTdfPath;
		public List<TrialDef> TrialDefs ;
        public List<StimDef> ActiveStimDefs ;
        public List<StimDef> IrrelevantStimDefs ;
        public bool CheckPerformance = true;
		public bool AbortTrialOnSampleOrDelayTouch;

		public BlockDef() { }
	}


    //RuleInfo objects define individual rules
    //ruleInfo fields:
    //    RuleName:    string
    //    RelevantFeatureTemplate:    list<int>[], length = 5 (number of possible dimensions. Each element specifies which feature values are part of the given rule. Should be a subset of the ActiveFeatureTemplate for the current block. E.g. [{0}, {1}, {0}, {0}, {0}] specifies that this list only applies to objects with a value of 1 on dimension 2
    //    RelevantObjectList:    string[], names objects that the rule applies to.
    //        A RuleInfo MUST have a RelevantFeatureTemplate, a RelevantObjectList, or both. If it has both, both will apply additively - e.g. you might want to specify that any objects with a value of 1 on Dimension 1 will be rewarded, but also a specific other object will be rewarded.
    //    ContextNums:    int[], the set of contexts in which this rule applies
    //    ContextNames:    string[], the set of contexts in which this rule applies
    //    RewardProb:    float, the probability of reward if an object that satisfies this rule is chosen
    //    RewardMag:    float, the magnitude of reward associated with this rule (how this is interpreted will depend on particular implementation, it could refer to # of pulses given to reward system, # of points shown on screen, volume of fluid, etc)
    [System.Serializable]
    public class RuleDef{
        public string RuleID;
        public int RuleCode;
        public List<int>[] RelevantFeatureTemplate;
        public int[] ContextNums;
        public string[] ContextNames;
        public float PositiveFbProb;
		public float RewardProb;
        public float RewardMag;
        public Reward[] TokenRewards;
    }


//    trialDef    {context, trialName (optional), trialcode (optional), {relevantObjectFiles}, 
//        {irrelevantObjectFiles}, {relevantObjLocations}, {irrelObjLocations}, {relObjrotations}, 
//        {irrelObjRotations}, {relObjectRewardProbs}, {relObjRewardMags}} *all relevant lists must be same length, all irrelevant lists must be same length

    
    [System.Serializable]
    public class TrialDef{
        public string TrialName;
        public int TrialCode;
        public int? ContextNum;
		public int? ItiContextNum;
		public int BlockNum;
		public int ConditionNum;
		public string ConditionName;
        public string ContextName;
        public string ContextPath;
        public StimDef[] RelevantStims;
        public StimDef[] IrrelevantStims;
		public StimDef[] SampleStims;
		public float SampleDuration;
		public float DelayDuration;
        [System.NonSerialized]
        public Material ContextMaterial;
		public Material ItiContextMaterial;
		public Reward[] TokenRewardsPositive;
		public Reward[] TokenRewardsNegative;
		public int? NumInitialTokens;
    }

	[System.Serializable]
	public class TrialDefTdf:TrialDef
	{
		public string RelevantStimDefTdfPath;
		public string IrrelevantStimDefTdfPath;
		public int[] RelevantStimCodes;
		public int[] IrrelevantStimCodes;
		public int[] SampleStimCodes;
		public Vector3[] RelevantStimLocations;
		public Vector3[] RelevantStimRotations;
		public Vector3[] IrrelevantStimLocations;
		public Vector3[] IrrelevantStimRotations;
		public Vector3[] SampleStimLocations;
		public Vector3[] SampleStimRotations;
		public Reward[][] RelevantTokenRewards;

		public TrialDef ConvertToTrialDef()
		{
			TrialDef td = new TrialDef
			{
				TrialName = TrialName,
				TrialCode = TrialCode,
				ContextNum = ContextNum,
				ItiContextNum = ItiContextNum,
				BlockNum = BlockNum,
				ConditionNum = ConditionNum,
				ContextName = ContextName,
				RelevantStims = RelevantStims,
				IrrelevantStims = IrrelevantStims,
				SampleStims = SampleStims,
				SampleDuration = SampleDuration,
				DelayDuration = DelayDuration
			};
			return td;
		}

		private int[] GetStimCodes(StimDef[] stimDefs)
		{
			int[] stimCodes = new int[stimDefs.Length];
			for (int iStim = 0; iStim < stimDefs.Length; iStim++)
				stimCodes[iStim] = stimDefs[iStim].StimCode;
			return stimCodes;
		}

		private StimDef[] PopulateStimDefs(int[] selectedStimCodes, StimDef[] allStimDefs, Vector3[] stimLocations, Vector3[] stimRotations, Reward[][] tokenRewards)
		{
			StimDef[] selectedStimDefs = new StimDef[selectedStimCodes.Length];
			int[] allStimCodes = GetStimCodes(allStimDefs);
			for (int iStim = 0; iStim < selectedStimCodes.Length; iStim++)
			{
				//Debug.Log("Stim " + iStim + " " + Time.frameCount);
				selectedStimDefs[iStim] = allStimDefs[Array.FindIndex<int>(allStimCodes, i => i == selectedStimCodes[iStim])].CopyStimDef();
				if (stimLocations != null && stimLocations.Length > 0)
				{
					selectedStimDefs[iStim].StimLocation = stimLocations[iStim];
					selectedStimDefs[iStim].StimLocationSet = true;
				}
				if (stimRotations != null && stimRotations.Length > 0)
				{
					selectedStimDefs[iStim].StimRotation = stimRotations[iStim];
					selectedStimDefs[iStim].StimRotationSet = true;
				}
				if (tokenRewards.Length > 0)
					selectedStimDefs[iStim].TokenRewards = tokenRewards[iStim];
			}
			return selectedStimDefs;
		}

		public StimDef[] PopulateStimDefs(StimDef[] stimDefs, string stimType)
		{
			switch (stimType)
			{
				case "relevant":
					RelevantStims = PopulateStimDefs(RelevantStimCodes, stimDefs, RelevantStimLocations, RelevantStimRotations, RelevantTokenRewards);
					return RelevantStims;
				case "irrelevant":
					IrrelevantStims = PopulateStimDefs(IrrelevantStimCodes, stimDefs, IrrelevantStimLocations, IrrelevantStimRotations, new Reward[0][]);
					return IrrelevantStims;
				case "sample":
					SampleStims = PopulateStimDefs(SampleStimCodes, stimDefs, SampleStimLocations, SampleStimRotations, new Reward[0][]);
					return SampleStims;
				default:
					return null;
			}
		}
		//public PopulateStimDefs(int[] stimCodes, )

		public TrialDef ConvertToTrialDef(StimDef[] relStims)
		{
			RelevantStims = PopulateStimDefs(RelevantStimCodes, relStims, RelevantStimLocations, RelevantStimRotations, RelevantTokenRewards);
			//IrrelevantStims = new StimDef[0];
			Debug.Log("relStims " + RelevantStims.Length);
			return ConvertToTrialDef();
		}

		public TrialDef ConvertToTrialDef(StimDef[] relStims, StimDef[] irrelStims = null, StimDef[] sampleStims = null)
		{
			//RelevantObjects = PopulateStimDefs(RelevantStimCodes, relStims, RelevantStimLocations, RelevantStimRotations, RelevantTokenRewards);
			if (irrelStims != null)
				IrrelevantStims = PopulateStimDefs(IrrelevantStimCodes, irrelStims, IrrelevantStimLocations, IrrelevantStimLocations, new Reward[0][]);
			else
				IrrelevantStims = new StimDef[0];
			if (sampleStims != null)
				SampleStims = PopulateStimDefs(SampleStimCodes, sampleStims, SampleStimLocations, SampleStimRotations, new Reward[0][]);
			else
				SampleStims = new StimDef[0];
			return ConvertToTrialDef(relStims);
		}
	}

	[System.Serializable]
    public class StimDef{
        public string StimName;
        public int[] BaseTokenGain;
        public int[] BaseTokenLoss;
        public string StimPath;
        public int StimCode; //optional, for analysis purposes
        public string StimID;
        public int[] StimDimVals; //only if this is parametrically-defined stim
        [System.NonSerialized]
        public GameObject StimGameObject; //not in config, generated at runtime
        public Vector3 StimLocation; //to be passed in explicitly if trial doesn't include location method
        public Vector3 StimRotation; //to be passed in explicitly if trial doesn't include location method
        public Vector2 StimScreenLocation;//screen position calculated during trial
        public bool StimLocationSet;
        public bool StimRotationSet;
        public float StimTrialPositiveFbProb; //set to -1 if stim is irrelevant
        public float StimTrialRewardMag; //set to -1 if stim is irrelevant
        public Reward[] TokenRewards;
        public int TimesUsedInBlock;
        public bool isRelevant;
		public bool TriggersSonication;

		public StimDef CopyStimDef()
		{
			StimDef sd = new StimDef();
			if(StimName != null)
				sd.StimName = StimName;
			if (BaseTokenGain != null)
				sd.BaseTokenGain = BaseTokenGain;
			if (BaseTokenLoss != null)
				sd.BaseTokenLoss = BaseTokenLoss;
			if (StimPath != null)
				sd.StimPath = StimPath;
			sd.StimCode = StimCode;
			if (StimID != null)
				sd.StimID = StimID;
			if (StimDimVals != null)
				sd.StimDimVals = StimDimVals;
			if (StimGameObject != null)
				sd.StimGameObject = StimGameObject;
			if (StimLocation != null)
				sd.StimLocation = StimLocation;
			if (StimRotation != null)
				sd.StimRotation = StimRotation;
			if (StimScreenLocation != null)
				sd.StimScreenLocation = StimScreenLocation;
			sd.StimLocationSet = StimLocationSet;
			sd.StimRotationSet = StimRotationSet;
			sd.StimTrialPositiveFbProb = StimTrialPositiveFbProb;
			sd.StimTrialRewardMag = StimTrialRewardMag;
			if (TokenRewards != null)
				sd.TokenRewards = TokenRewards;
			sd.TimesUsedInBlock = TimesUsedInBlock;
			sd.isRelevant = isRelevant;
			return sd;
		}
	}
	public class EventCodeConfig
	{
		public EventCode MainInitEnd;
		public EventCode MainStartEnd;
		public EventCode MainInstruct1End;
		public EventCode MainInstruct2End;
		public EventCode MainCalibEnd;
		public EventCode MainTutorialEnd;
		public EventCode TrlStart;
		public EventCode TrlEnd;
		public EventCode FixCentralCueStart;
		public EventCode FixTargetStart;
		public EventCode FixDistractorStart;
		public EventCode FixIrrelevantStart;
		public EventCode FixObjectEnd;
		public EventCode TouchCentralCueStart;
		public EventCode TouchTargetStart;
		public EventCode TouchDistractorStart;
		public EventCode TouchIrrelevantStart;
		public EventCode TouchOtherStart;
		public EventCode TouchOff;
		public EventCode CorrectResponse;
		public EventCode IncorrectResponse;
		public EventCode Rewarded;
		public EventCode Unrewarded;
		public EventCode BreakFixation;
		public EventCode NoChoice;
		public EventCode NoFixationNoTrialStart;
		public EventCode Recalibration;
		public EventCode HoldKeyLift;
		public EventCode SlowReach;
		public EventCode FixPointOn;
		public EventCode FixPointOff;
		public EventCode ContextOn;
		public EventCode ContextOff;
		public EventCode StimOn;
		public EventCode StimOff;
		public EventCode GoCueOn;
		public EventCode GoCueOff;
		public EventCode SelectionVisualFbOn;
		public EventCode SelectionAuditoryFbOn;
		public EventCode TokensCompletFbOn;
		public EventCode TokensCompletFbOff;
		public EventCode Fluid1Onset;
		public EventCode Fluid2Onset;
		public EventCode TokensAddedMin;
		public EventCode TokensAddedMax;
		public EventCode RewardValidityMin;
		public EventCode RewardValidityMax;
		public EventCode DimensionalityMin;
		public EventCode DimensionalityMax;
		public EventCode TokenRewardPositive;
		public EventCode TokenRewardNegative;
		public EventCode TokenRewardNeutral;
		public EventCode BlockConditionMin;
		public EventCode BlockConditionMax;
		public EventCode ContextCodeMin;
		public EventCode ContextCodeMax;
		public EventCode StimCodeMin;
		public EventCode StimCodeMax;
		public EventCode TrialIndexMin;
		public EventCode TrialIndexMax;
		public EventCode TrialNumberMin;
		public EventCode TrialNumberMax;

	}

	public class EventCode
	{
		public int Value;
		public string Description;
	}

	public class OverallPerformanceTracker
	{
		private int TrialsInExpt;
		private int TrialsInBlock;
		private int TrialsForRunningAvg;
		//public int TrialsForRunningAvg { get; set; }
		public Dictionary<string, PerformanceTracker> PerformanceTrackers;
		//PerformanceCounter BestChoiceCounter { get; }
		//PerformanceCounter PositiveFbCounter { get; }
		//PerformanceCounter RewardedCounter { get; }
		//PerformanceCounter TokenCounter { get; }
		//PerformanceCounter TotalTrialCounter { get; }
		//PerformanceCounter SuccessfulTrialCounter { get; }
		//PerformanceCounter AbortedTrialCounter { get; }

		public OverallPerformanceTracker(int trialsForRunningAvg)
		{
			TrialsInExpt = 0;
			TrialsInBlock = 0;
			TrialsForRunningAvg = trialsForRunningAvg;
			PerformanceTrackers = new Dictionary<string, PerformanceTracker>
			{
				{ "BestChoice", new PerformanceTracker(trialsForRunningAvg) },
				{ "PositiveFb", new PerformanceTracker(trialsForRunningAvg) },
				{ "Rewarded", new PerformanceTracker(trialsForRunningAvg) },
				{ "Tokens", new PerformanceTracker(trialsForRunningAvg) },
				{ "TotalTrials", new PerformanceTracker(trialsForRunningAvg) },
				{ "CompletedTrials", new PerformanceTracker(trialsForRunningAvg) },
				{ "AbortedTrials", new PerformanceTracker(trialsForRunningAvg) }
			};
			//BestChoiceCounter = new PerformanceCounter(trialsForRunningAvg);
			//PositiveFbCounter = new PerformanceCounter(trialsForRunningAvg);
			//RewardedCounter = new PerformanceCounter(trialsForRunningAvg);
			//TokenCounter = new PerformanceCounter(trialsForRunningAvg);
			//TotalTrialCounter = new PerformanceCounter(trialsForRunningAvg);
			//SuccessfulTrialCounter = new PerformanceCounter(trialsForRunningAvg);
			//AbortedTrialCounter = new PerformanceCounter(trialsForRunningAvg);
		}

		public void Reset()
		{
			foreach (PerformanceTracker pc in PerformanceTrackers.Values)
				pc.Reset();
		}

		public void UpdateCounts(string key, int value)
		{
			PerformanceTrackers[key].UpdatePerformance(value);
		}

		public void UpdateCounts(string[] keys, int value)
		{
			foreach (string key in keys)
				PerformanceTrackers[key].UpdatePerformance(value);
		}

		public void UpdateCounts(string[] keys, int[] values)
		{
			for (int i = 0; i < keys.Length; i++)
			{
				PerformanceTrackers[keys[i]].UpdatePerformance(values[i]);
			}
		}

		//public int GetCount(string key)
		//{
		//	//return PerformanceCounters[key]
		//}
	}

	public class PerformanceTracker
	{
		//public int ExptCount;
		//public int BlockCount;
		//public int RunningAvgCount;
		private int RunningAvgWindowSize;
		private Queue<int> ExptValues;
		private Queue<int> BlockValues;
		private Queue<int> RunningWindowValues;
		public int ExptCount;
		public int BlockCount;
		public int RunningWindowCount;
		public float ExptAvg;
		public float BlockAvg;
		public float RunningWindowAvg;
		//public ImmediatePerformanceSummary ExptSummary;
		//public ImmediatePerformanceSummary BlockSummary;
		//public ImmediatePerformanceSummary RunningWindowSummary;

		public PerformanceTracker(int windowSize)
		{
			//ExptCount = 0;
			//BlockCount = 0;
			//RunningAvgCount = 0;
			RunningAvgWindowSize = windowSize;
			ExptValues = new Queue<int>();
			BlockValues = new Queue<int>();
			RunningWindowValues = new Queue<int>();
		}

		public void Reset()
		{
			//BlockCount = 0;
			//RunningAvgCount = 0;
			BlockValues.Clear();
			RunningWindowValues.Clear();
		}

		public void UpdatePerformance(int newCount)
		{
			//ExptCount += newCount;
			//BlockCount += newCount;
			ExptValues.Enqueue(newCount);
			BlockValues.Enqueue(newCount);
			RunningWindowValues.Enqueue(newCount);
			if (RunningWindowValues.Count > RunningAvgWindowSize)
				RunningWindowValues.Dequeue();

			ExptCount = ExptValues.Sum();
			BlockCount = BlockValues.Sum();
			RunningWindowCount = RunningWindowValues.Sum();

			ExptAvg = (float)ExptValues.Average();
			BlockAvg = (float)BlockValues.Average();
			RunningWindowAvg = (float)RunningWindowValues.Average();
		}
	}

	//public class ImmediatePerformanceSummary
	//{
	//	public int ExptCount { get; }
	//	public int BlockCount { get; set; }
	//	public int RunningWindowCount { get; set; }
	//	public float ExptAvg { get; set; }
	//	public float BlockAvg { get; set; }
	//	public float RunningWindowAvg { get; set; }

	//	public void UpdateSummary(PerformanceTracker pt)
	//	{
	//		ExptCount = pt.ExptValues;
	//	}
	//}
}
