using UnityEngine;
using USE_ExperimentTemplate;
using USE_StimulusManagement;
//test

namespace EffortControl_Namespace
{
    public class EffortControl_TaskDef : TaskDef
    {
        //Already-existing fields (inherited from TaskDef)      
        //public DateTime TaskStart_DateTime;
        //public int TaskStart_Frame;
        //public float TaskStart_UnityTime;
        //public string TaskName;
        //public string ExternalStimFolderPath;
        //public string PrefabStimFolderPath;
        //public string ExternalStimExtension;
        //public List<string[]> FeatureNames;
        //public string neutralPatternedColorName;
        //public float? ExternalStimScale;
    }

    public class EffortControl_BlockDef : BlockDef
    {
        //Already-existing fields (inherited from BlockDef)
		//public int BlockCount;
		//public TrialDef[] TrialDefs;
    }

    public class EffortControl_TrialDef : TrialDef
    {
        //Already-existing fields (inherited from TrialDef)
        //public int BlockCount, TrialCountInBlock, TrialCountInTask;
        //public TrialStims TrialStims;
        public string TrialName;
        public int TrialCode;
        public int ContextNum;
        public string ConditionName;
        public string ContextName;
        public int NumOfClicksLeft;
        public int NumOfClicksRight;
        public int NumOfCoinsLeft;
        public int NumOfCoinsRight;
        public int NumOfPulsesLeft;
        public int NumOfPulsesRight;
        public int PulseSizeLeft;
        public int PulseSizeRight;
        public int ClicksPerOutline;
        public float[] TouchOnOffTimeRange;
        public float InitialChoiceMinDuration;
        public float StarttoTapDispDelay;
        public float FinalTouchToVisFeedbackDelay;
        public float FinalTouchToRewardDelay;
    }

    public class EffortControl_StimDef : StimDef
    {
        //Already-existing fields (inherited from TrialDef)
        //public Dictionary<string, StimGroup> StimGroups; //stimulus type field (e.g. sample/target/irrelevant/etc)
        //public string StimName;
        //public string StimPath;
        //public string PrefabPath;
        //public string ExternalFilePath;
        //public string StimFolderPath;
        //public string StimExtension;
        //public int StimCode; //optional, for analysis purposes
        //public string StimID;
        //public int[] StimDimVals; //only if this is parametrically-defined stim
     //   [System.NonSerialized] //public GameObject StimGameObject; //not in config, generated at runtime
        //public Vector3 StimLocation; //to be passed in explicitly if trial doesn't include location method
        //public Vector3 StimRotation; //to be passed in explicitly if trial doesn't include location method
        //public Vector2 StimScreenLocation; //screen position calculated during trial
        //public float? StimScale;
        //public bool StimLocationSet;
        //public bool StimRotationSet;
        //public float StimTrialPositiveFbProb; //set to -1 if stim is irrelevant
        //public float StimTrialRewardMag; //set to -1 if stim is irrelevant
        //public TokenReward[] TokenRewards;
        //public int[] BaseTokenGain;
        //public int[] BaseTokenLoss;
        //public int TimesUsedInBlock;
        //public bool isRelevant;
        //public bool TriggersSonication;
        //public State SetActiveOnInitialization;
        //public State SetInactiveOnTermination;
    
    }
}