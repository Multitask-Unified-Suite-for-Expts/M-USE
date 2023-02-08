using System;
using System.Collections.Generic;
using UnityEngine;
using USE_ExperimentTemplate_Block;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Trial;
using USE_StimulusManagement;


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
        public string TrialId;
        public int NumClicksLeft;
        public int NumClicksRight;
        public int NumCoinsLeft;
        public int NumCoinsRight;
        public int NumPulsesLeft;
        public int NumPulsesRight;
        public int PulseSizeLeft;
        public int PulseSizeRight;
        public int ClicksPerOutline;
        public int Touches;
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