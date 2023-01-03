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
        public string BlockName;
        public int NumTrials;
        public string ContextName;
        public int NumClicksLeft;
        public int NumClicksRight;
        public int NumCoinsLeft;
        public int NumCoinsRight;
        public int NumPulsesLeft;
        public int NumPulsesRight;
        public int PulseSizeLeft;
        public int PulseSizeRight;
        public int ClicksPerOutline;
        public float CompleteToFeedbackDelay;
        public float InitToBalloonDelay;
        public float InflateDuration;

        public override void GenerateTrialDefsFromBlockDef()
        {
            TrialDefs = new List<EffortControl_TrialDef>().ConvertAll(x => (TrialDef)x);
            for(int i = 0; i < NumTrials; i++)
            {
                EffortControl_TrialDef trial = new EffortControl_TrialDef();
                trial.ContextName = ContextName;
                trial.NumClicksLeft = NumClicksLeft;
                trial.NumClicksRight = NumClicksRight;
                trial.NumCoinsLeft = NumCoinsLeft;
                trial.NumCoinsRight = NumCoinsRight;
                trial.ClicksPerOutline = ClicksPerOutline;
                trial.NumPulsesLeft = NumPulsesLeft;
                trial.NumPulsesRight = NumPulsesRight;
                trial.PulseSizeLeft = PulseSizeLeft;
                trial.PulseSizeRight = PulseSizeRight;
                trial.CompleteToFeedbackDelay = CompleteToFeedbackDelay;
                trial.InitToBalloonDelay = InitToBalloonDelay;
                trial.InflateDuration = InflateDuration;
                TrialDefs.Add(trial);
            }

        }
    }

    public class EffortControl_TrialDef : TrialDef
    {
        public string ContextName;
        public int NumClicksLeft;
        public int NumClicksRight;
        public int NumCoinsLeft;
        public int NumCoinsRight;
        public int NumPulsesLeft;
        public int NumPulsesRight;
        public int PulseSizeLeft;
        public int PulseSizeRight;
        public int ClicksPerOutline;
        public float CompleteToFeedbackDelay;
        public float InitToBalloonDelay;
        public float InflateDuration;
        // public float[] TouchOnOffTimeRange;
        // public float InitialChoiceMinDuration;
        // public float StarttoTapDispDelay;
        // public float FinalTouchToVisFeedbackDelay;
        // public float FinalTouchToRewardDelay;
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