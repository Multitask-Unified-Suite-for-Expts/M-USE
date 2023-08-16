using UnityEngine;
using USE_Def_Namespace;
using USE_StimulusManagement;
using USE_ExperimentTemplate_Classes;

namespace FeatureUncertaintyWM_Namespace
{
    public class FeatureUncertaintyWM_TaskDef : TaskDef
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

    public class FeatureUncertaintyWM_BlockDef : BlockDef
    {
        //Already-existing fields (inherited from BlockDef)
		//public int BlockCount;
		//public TrialDef[] TrialDefs;
        public int[] nRepetitionsMinMax, blockMcCompStimIndices; // blockMcCompStimIndices = all indices to all component stims this block
        //blockMcCompStimIndices= {10, 23, 48, 57}
        public int numMcStim, maxComp;
        public string BlockName;
        public int NumInitialTokens;
        public int NumTokenBar;
        public int NumPulses;
        public int PulseSize;
        public string BlockEndType;
        public float BlockEndThreshold;
        public int BlockEndWindow;
        public bool StimFacingCamera;
        public string ContextName;

        //public override void GenerateTrialDefsFromBlockDef()
        //{
        //    //pick # of trials from minmax
        //    System.Random rnd = new System.Random();
        //    int num = rnd.Next(MinMaxTrials[0], MinMaxTrials[1]);

        //    TrialDefs = new List<FeatureUncertaintyWM_TrialDef>().ConvertAll(x => (TrialDef)x);
        //    for (int iTrial = 0; iTrial < num; iTrial++)
        //    {
        //        FeatureUncertaintyWM_TrialDef td = new FeatureUncertaintyWM_TrialDef();
        //        td.numMcStim = numMcStim;
        //        TrialDefs.Add(td);
        //    }

        //}

        public override void AddToTrialDefsFromBlockDef()
        {
            for (int iTrial = 0; iTrial < TrialDefs.Count; iTrial++)
            {
                FeatureUncertaintyWM_TrialDef td = (FeatureUncertaintyWM_TrialDef)TrialDefs[iTrial];
                td.BlockName = BlockName;
                td.NumInitialTokens = NumInitialTokens;
                td.NumPulses = NumPulses;
                td.NumTokenBar = NumTokenBar;
                td.PulseSize = PulseSize;
                td.BlockEndType = BlockEndType;
                td.BlockEndThreshold = BlockEndThreshold;
                td.BlockEndWindow = BlockEndWindow;
                td.StimFacingCamera = StimFacingCamera;
                td.ContextName = ContextName;
                TrialDefs[iTrial] = td;
            }
        }
    }

    public class FeatureUncertaintyWM_TrialDef : TrialDef
    {
        //Already-existing fields (inherited from TrialDef)
		//public int BlockCount, TrialCountInBlock, TrialCountInTask;
		//public TrialStims TrialStims;
        public int numMcStim;
        public Vector3[] mcStimLocations, sampleCompLocations;
        public int[] mcNumCircles, mcTotalObjectCount, sampleCompIndices;
        public Reward[][] mcStimTokenReward;
        public int[][]  mcCompObjNumber, mcCompObjIndices, mcAngleOffset;
        //mcCompObjIndices= {{57, 10, 23}, {48}}
        public float[][] mcRadius;
        public string ContextName;
        public string BlockName;
        public int NumInitialTokens;
        public int NumTokenBar;
        public int NumPulses;
        public int PulseSize;
        public string BlockEndType;
        public float BlockEndThreshold;
        public int BlockEndWindow;
        public bool StimFacingCamera;
    }

    public class FeatureUncertaintyWM_StimDef : StimDef
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
    
    }

    public class FeatureUncertaintyWM_MultiCompStimDef : StimDef
    {
        public int numCircles, totalObjectCount;
        public int[]  compObjNumber, compObjIndices, angleOffset;
        public int mcStimInd;
        //componentObjIndices= {57, 10, 23}
        public float[] radius;
        public bool IsTarget;
        public int StimTrialRewardMag;
    }
}