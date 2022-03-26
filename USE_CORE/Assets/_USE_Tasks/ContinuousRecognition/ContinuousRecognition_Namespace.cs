using System;
using UnityEngine;
using USE_ExperimentTemplate;
using USE_StimulusManagement;
using System.Collections.Generic;
using Random = UnityEngine.Random;

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
        public int[] BlockStimIndices, nObjectsMinMax, Ratio;
        public List<int> PreviouslyChosenStimuli, PreviouslyNotChosenStim, TrialStimIndices, UnseenStims;
        public List<int> NewStim;
        public Vector3[] BlockStimLocations;
        public int trialCount;

        public override void GenerateTrialDefsFromBlockDef()
        {
            BlockStimLocations = new []
            {
                new Vector3(-5f,1f,0f), new Vector3(0f,1f,0f), new Vector3(5f, 1f, 0f),
                new Vector3(-5f,4f,0f), new Vector3(0f,4f,0f), new Vector3(5f, 4f, 0f),
                new Vector3(-5f,-3f,0f), new Vector3(0f,-3f,0f), new Vector3(5f, -3f, 0f)
            };
            
            PreviouslyChosenStimuli = new List<int>();
            PreviouslyNotChosenStim = new List<int>();
            UnseenStims = new List<int>();
            TrialStimIndices = new List<int>();
            NewStim = new List<int>();
            int numTrials = nObjectsMinMax[1] - nObjectsMinMax[0] + 1;
            TrialDefs = new ContinuousRecognition_TrialDef[numTrials]; //actual correct # 
            int numTrialStims = nObjectsMinMax[0];

            for (int iTrial = 0; iTrial< numTrials; iTrial++)
            {
                ContinuousRecognition_TrialDef td = new ContinuousRecognition_TrialDef();
                td.BlockStimIndices = BlockStimIndices;
                td.trialCount = trialCount;
                
                Vector3[] arr = new Vector3[nObjectsMinMax[0] + iTrial];
                for (int i = 0; i < numTrialStims; i++)
                {
                    int index = Random.Range(0, BlockStimLocations.Length);
                    while (Array.IndexOf(arr, BlockStimLocations[index]) != -1)
                    {
                        index = Random.Range(0, BlockStimLocations.Length);
                    }

                    arr[i] = BlockStimLocations[index];
                }

                td.TrialStimLocations = arr;
                td.TrialStimIndices = TrialStimIndices;
                td.PreviouslyChosenStimuli = PreviouslyChosenStimuli;
                td.PreviouslyNotChosenStimuli = PreviouslyNotChosenStim;
                td.nObjectsMinMax = nObjectsMinMax;
                td.Ratio = Ratio;
                td.UnseenStims = UnseenStims;
                td.numTrialStims = numTrialStims;
                td.maxNumTrials = numTrials;
                
                TrialDefs[iTrial] = td;
                numTrialStims++;
                trialCount++;
                
            }
        }
        
    }

    public class ContinuousRecognition_TrialDef : TrialDef
    {
        //TrialStimIndices provides indices to individual StimDefs in BlockStims (so a subset of BlockStims,
        //which is a subset of ExternalStims).
        public int[] BlockStimIndices, nObjectsMinMax, Ratio;
        public Vector3[] TrialStimLocations;
        public int trialCount, numTrialStims, maxNumTrials;

        public List<int> PreviouslyChosenStimuli;
        public List<int> PreviouslyNotChosenStimuli;
        public List<int> TrialStimIndices;
        public List<int> UnseenStims;
        
        
        //ObjectNums refers to items in a list of objects to be loaded from resources folder
        public int[] ObjectNums;
        public int Context;
        //public int[] BlockStimIndices;
        
        
        //----from stim handling for testing
        public int[] GroupAIndices;


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