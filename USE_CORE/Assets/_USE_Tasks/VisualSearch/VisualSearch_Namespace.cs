using System.Runtime.InteropServices;
using UnityEngine;
using USE_ExperimentTemplate;
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

        Vector3 ButtonPosition;
        Vector3 ButtonScale;
        Vector3 ButtonColor;
        string ButtonText;
        string ContextExternalFilePath;
    }

    public class VisualSearch_BlockDef : BlockDef
    {
        //Already-existing fields (inherited from BlockDef)
        //public int BlockCount;
        //public TrialDef[] TrialDefs;
        public int[] SearchStimsIndices;
        public int[] DistractorStimsIndices;
        public Vector3[] SearchStimsLocations;
        public Vector3[] DistractorStimsLocations;
        public int[] nRepetitionsMinMax;
        public string TrialID;
        public string ContextName;

        public override void GenerateTrialDefsFromBlockDef()
        {
            //pick # of trials from minmax
            System.Random rnd = new System.Random();
            int num = rnd.Next(nRepetitionsMinMax[0], nRepetitionsMinMax[1]);
            TrialDefs = new TrialDef[num];//actual correct # 

            for (int iTrial = 0; iTrial < TrialDefs.Length; iTrial++)
            {
                VisualSearch_TrialDef td = new VisualSearch_TrialDef();
                td.TrialID = TrialID;
                /*
                td.MinTouchDuration = MinTouchDuration;
                td.MaxTouchDuration = MaxTouchDuration;
                */
                td.SearchStimsIndices = SearchStimsIndices;
                td.DistractorStimsIndices = DistractorStimsIndices;
                td.SearchStimsLocations = SearchStimsLocations;
                td.DistractorStimsLocations = DistractorStimsLocations;
                td.ContextName = ContextName;
                //td.RandomizedLocations = RandomizedLocations;
                TrialDefs[iTrial] = td;
            }
        }

    }

    public class VisualSearch_TrialDef : TrialDef
    {
        //Already-existing fields (inherited from TrialDef)
        //public int BlockCount, TrialCountInBlock, TrialCountInTask;
        //public TrialStims TrialStims;
        public int[] SearchStimsIndices;
        public int[] DistractorStimsIndices;
        public Vector3[] SearchStimsLocations;
        public Vector3[] DistractorStimsLocations;
        public int[] nRepetitionsMinMax;
        public string TrialID;
        public string ContextName;
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

    }
}