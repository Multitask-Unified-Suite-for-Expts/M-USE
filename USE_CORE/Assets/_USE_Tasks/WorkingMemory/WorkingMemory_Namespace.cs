using UnityEngine;
using USE_ExperimentTemplate;
using USE_StimulusManagement;

namespace WorkingMemory_Namespace
{
    public class WorkingMemory_TaskDef : TaskDef
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

    public class WorkingMemory_BlockDef : BlockDef
    {
        //Already-existing fields (inherited from BlockDef)
		//public int BlockCount;
		//public TrialDef[] TrialDefs;
    }

    public class WorkingMemory_TrialDef : TrialDef
    {
        //Already-existing fields (inherited from TrialDef)
		//public int BlockCount, TrialCountInBlock, TrialCountInTask;
		//public TrialStims TrialStims;
        public int[] TargetIndices, DistractorIndices1, DistractorIndices2;
        public Vector3[] TargetSampleLocations, DistractorLocations1, TargetSearchLocations, DistractorLocations2;

        public float initTrialDuration,
            baselineDuration,
            displaySampleDuration,
            delay1Duration,
            displayDistractors1Duration,
            delay2Duration,
            maxSearchduration,
            selectionFbDuration,
            tokenFbDuration,
            trialEndDuration;
    }

    public class WorkingMemory_StimDef : StimDef
    {
        //Already-existing fields (inherited from Stim  Def)
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
        //[System.NonSerialized] //public GameObject StimGameObject; //not in config, generated at runtime
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
        public bool IsTarget;
    }
}