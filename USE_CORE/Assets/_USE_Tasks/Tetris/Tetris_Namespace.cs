using System.Collections.Generic;
using UnityEngine;
using USE_StimulusManagement;
using USE_Def_Namespace;

namespace Tetris_Namespace
{
    public class Tetris_TaskDef : TaskDef
    {
        public Vector3 ButtonPosition;
        public float ButtonScale;
        float TouchFbDuration;
    }

    public class Tetris_BlockDef : BlockDef
    {
        public int NumTrials;
        public int NumRewardPulses;
        public int PulseSize;
        public int RewardMag;
        public string BlockName;

        public override void GenerateTrialDefsFromBlockDef()
        {
            TrialDefs = new List<Tetris_TrialDef>().ConvertAll(x=>(TrialDef)x);

            for(int trialIndex = 0; trialIndex < NumTrials; trialIndex++)
            {
                Tetris_TrialDef trial = new Tetris_TrialDef();
                TrialDefs.Add(trial);
            }
        }

    }

    public class Tetris_TrialDef : TrialDef
    {
  
    }

    public class Tetris_StimDef : StimDef
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
}