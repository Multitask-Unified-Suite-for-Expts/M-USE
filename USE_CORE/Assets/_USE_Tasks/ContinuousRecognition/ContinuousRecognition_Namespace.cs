using UnityEngine;
using USE_ExperimentTemplate;
using USE_StimulusManagement;
using System.Collections.Generic;

namespace ContinuousRecognition_Namespace
{
    public class ContinuousRecognition_TaskDef : TaskDef
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

    public class ContinuousRecognition_BlockDef : BlockDef
    {
        //BlockStims is a StimGroup consisting of the StimDefs specified by BlockStimIndices
        public StimGroup BlockStims;
        //BlockStimIndices provides indices to individual StimDefs in the ExternalStims StimGroup,
        //which is automatically created at the start of the task and includes every stimulus in the ContinousRecognition_StimDef_tdf file
        public int[] BlockStimIndices, nObjectsMinMax;
        public List<int> PreviouslyChosenStimuli;
        
        
        //-----------------------------------------------
        //Already-existing fields (inherited from BlockDef)
		//public int BlockCount;
		//public TrialDef[] TrialDefs;
        public override void GenerateTrialDefsFromBlockDef()
        {
            //pick # of trials from minmax
            PreviouslyChosenStimuli = new List<int>();
            int numTrials = nObjectsMinMax[1] - nObjectsMinMax[0] + 1;
            TrialDefs = new ContinuousRecognition_TrialDef[numTrials];//actual correct # 

            int numTrialStims = nObjectsMinMax[0];
            for (int iTrial = 0; iTrial< TrialDefs.Length; iTrial++)
            {
                ContinuousRecognition_TrialDef td = new ContinuousRecognition_TrialDef();
                //td.TrialStimLocations = something
                td.BlockStimIndices = BlockStimIndices;
                td.PreviouslyChosenStimuli = PreviouslyChosenStimuli;
                TrialDefs[iTrial] = td;
                numTrialStims++;
            }
        }
        
    }

    public class ContinuousRecognition_TrialDef : TrialDef
    {
        //TrialStimIndices provides indices to individual StimDefs in BlockStims (so a subset of BlockStims,
        //which is a subset of ExternalStims).
        public List<int> TrialStimIndices;
        public Vector3[] TrialStimLocations;
        //ObjectNums refers to items in a list of objects to be loaded from resources folder
        public int[] ObjectNums;
        public int Context;
        public int[] BlockStimIndices;
        public List<int> PreviouslyChosenStimuli;
        
        //----from stim handling for testing
        public int[] GroupAIndices;
        public int[] GroupBIndices;
        public int[] GroupCIndices;
        public Vector3[] GroupALocations;
        public Vector3[] GroupBLocations;
        public Vector3[] GroupCLocations;
        
        
        //-------------------------------------------
        //Already-existing fields (inherited from TrialDef)
        //public int BlockCount, TrialCountInBlock, TrialCountInTask;
    }

    public class ContinuousRecognition_StimDef : StimDef
    {
        //This bool indicates if a stimulus was previously selected or not
        public bool PreviouslyChosen;
        
        //------------------------------------------------
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