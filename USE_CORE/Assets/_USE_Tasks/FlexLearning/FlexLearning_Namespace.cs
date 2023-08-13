using System.Collections.Generic;
using UnityEngine;
using USE_Def_Namespace;
using USE_StimulusManagement;
using USE_ExperimentTemplate_Classes;

namespace FlexLearning_Namespace
{
    public class FlexLearning_TaskDef : TaskDef
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

        public bool StimFacingCamera;
        public string ShadowType;
        public bool NeutralITI;
    }

    public class FlexLearning_BlockDef : BlockDef
    {
        //Already-existing fields (inherited from BlockDef)
        //public int BlockCount;
        //public TrialDef[] TrialDefs;
        public int[] TrialStimIndices;
        public Vector3[] TrialStimLocations;
        public int[] MinMaxTrials;
        public string TrialID;
        public string BlockName;
        public string ContextName;
        public int NumInitialTokens;
        public Reward[][] TrialStimTokenReward;
        public Reward[] PulseReward;
        public string BlockEndType;
        public float BlockEndThreshold;
        public int BlockEndWindow;
        public int NumTokenBar;
        //public int NumPulses;
        public int PulseSize;
        public bool RandomizedLocations;
        public bool? TokensWithStimOn = null;

        public override void GenerateTrialDefsFromBlockDef()
        {
            //pick # of trials from minmax
            int maxNum = RandomNumGenerator.Next(MinMaxTrials[0], MinMaxTrials[1]);
            TrialDefs = new List<FlexLearning_TrialDef>().ConvertAll(x => (TrialDef)x);
            for (int iTrial = 0; iTrial < maxNum; iTrial++)
            {
                FlexLearning_TrialDef td = new FlexLearning_TrialDef();
                td.TrialID = TrialID;
                td.BlockName = BlockName;
                td.TrialStimIndices = TrialStimIndices;
                td.TrialStimLocations = TrialStimLocations;
                td.ContextName = ContextName;
                td.TrialStimTokenReward = TrialStimTokenReward;
                td.NumInitialTokens = NumInitialTokens;
                td.RandomizedLocations = RandomizedLocations;
                td.BlockEndType = BlockEndType;
                td.BlockEndThreshold = BlockEndThreshold;
                td.BlockEndWindow = BlockEndWindow;
                td.PulseReward = PulseReward;
                td.NumTokenBar = NumTokenBar;
                td.PulseSize = PulseSize;
                td.MaxTrials = maxNum;
                if (TokensWithStimOn != null)
                    td.TokensWithStimOn = TokensWithStimOn;
                else
                    td.TokensWithStimOn = false;
                TrialDefs.Add(td);
            }
        }
        public override void AddToTrialDefsFromBlockDef()
        {
            // Sets maxNum to the number of TrialDefs present, and generate a random max if a range is provided
            int maxNum = TrialDefs.Count;
            if (MinMaxTrials != null)
            {
                if (RandomNumGenerator == null)
                    Debug.Log("RANDOM NUM GENERATOR NULL!");

                maxNum = RandomNumGenerator.Next(MinMaxTrials[0], MinMaxTrials[1]);
            }
            for (int iTrial = 0; iTrial < TrialDefs.Count; iTrial++)
            {
                FlexLearning_TrialDef td = (FlexLearning_TrialDef)TrialDefs[iTrial];
                td.BlockName = BlockName;
                td.NumInitialTokens = NumInitialTokens;
                td.BlockEndType = BlockEndType;
                td.BlockEndThreshold = BlockEndThreshold;
                td.BlockEndWindow = BlockEndWindow;
                td.PulseReward = PulseReward;
                td.NumTokenBar = NumTokenBar;
                td.PulseSize = PulseSize;
                td.ContextName = ContextName;
                td.MinMaxTrials = MinMaxTrials;
                td.MaxTrials = maxNum;
                if (TokensWithStimOn != null)
                    td.TokensWithStimOn = TokensWithStimOn;
                else
                    td.TokensWithStimOn = false;
                TrialDefs[iTrial] = td;
            }
        }
    }

    public class FlexLearning_TrialDef : TrialDef
    {
        //Already-existing fields (inherited from TrialDef)
        //public int BlockCount, TrialCountInBlock, TrialCountInTask;
        //public TrialStims TrialStims;
        public int[] TrialStimIndices;
        public Vector3[] TrialStimLocations;
        public string BlockName;
        public string TrialID;
        public Reward[][] TrialStimTokenReward;
        public Reward[] PulseReward;
        public bool RandomizedLocations;
        public bool StimFacingCamera;
        public string ContextName;
        public string BlockEndType;
        public float BlockEndThreshold;
        public int BlockEndWindow;
        public int NumPulses;
        public int NumInitialTokens;
        public int NumTokenBar;
        public int PulseSize;
        public bool? TokensWithStimOn;
        public int MaxTrials;
        public int[] MinMaxTrials;

    }

    public class FlexLearning_StimDef : StimDef
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