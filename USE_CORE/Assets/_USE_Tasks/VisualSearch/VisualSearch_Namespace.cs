using System.Collections.Generic;
using UnityEngine;
using USE_ExperimentTemplate_Block;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Trial;
using USE_StimulusManagement;

namespace VisualSearch_Namespace
{
    public class VisualSearch_TaskDef : TaskDef
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

    public class VisualSearch_BlockDef : BlockDef
    {
        //Already-existing fields (inherited from BlockDef)
        //public int BlockCount;
        //public TrialDef[] TrialDefs;
        public string BlockName;
        public TokenReward[][]TrialStimTokenReward;
        public int[] nRepetitionsMinMax;
        public string ContextName;
        public int NumPulses;
        public int NumInitialTokens;
        public int NumTokenBar;
        public int PulseSize;
        public bool RandomizedLocations;
        public bool? TokensWithStimOn;

        public override void GenerateTrialDefsFromBlockDef()
        {
            //pick # of trials from minmax
            int num = RandomNumGenerator.Next(nRepetitionsMinMax[0], nRepetitionsMinMax[1]);
            TrialDefs = new List<VisualSearch_TrialDef>().ConvertAll(x => (TrialDef)x);
            for (int iTrial = 0; iTrial < num; iTrial++)
            {
                VisualSearch_TrialDef td = new VisualSearch_TrialDef();
                td.ContextName = ContextName;
                td.TrialStimTokenReward = TrialStimTokenReward;
                td.NumInitialTokens = NumInitialTokens;
                td.RandomizedLocations = RandomizedLocations;
                if (TokensWithStimOn != null)
                    td.TokensWithStimOn = TokensWithStimOn;
                else
                    td.TokensWithStimOn = false;
                TrialDefs[iTrial] = td;                td.BlockCount = BlockCount;
                TrialDefs.Add(td);
            }
        }
        public override void AddToTrialDefsFromBlockDef()
        {
            for (int iTrial = 0; iTrial < TrialDefs.Count; iTrial++)
            {
                VisualSearch_TrialDef td = (VisualSearch_TrialDef)TrialDefs[iTrial];
                td.BlockName = BlockName;
                td.NumInitialTokens = NumInitialTokens;
                td.RandomizedLocations = RandomizedLocations;
                td.NumPulses = NumPulses;
                td.NumTokenBar = NumTokenBar;
                td.PulseSize = PulseSize;
                if (TokensWithStimOn != null)
                    td.TokensWithStimOn = TokensWithStimOn;
                else
                    td.TokensWithStimOn = false;
                TrialDefs[iTrial] = td;
                TrialDefs[iTrial] = td;
            }
        }
    }

    public class VisualSearch_TrialDef : TrialDef
    {
        //Already-existing fields (inherited from TrialDef)
        //public int BlockCount, TrialCountInBlock, TrialCountInTask;
        //public TrialStims TrialStims;
        public int[] TrialStimIndices;
        public Vector3[] TrialStimLocations;
        public string TrialID;
        public TokenReward[][] TrialStimTokenReward;
        public bool? TokensWithStimOn;
        public int NumPulses;
        public int NumInitialTokens;
        public int NumTokenBar;
        public int PulseSize;
        public bool RandomizedLocations;
        public string ContextName;
        public string BlockName;
    }

    public class VisualSearch_StimDef : StimDef
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
        public int TokenUpdate;

    }
}