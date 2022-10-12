using System;
using UnityEngine;
using USE_StimulusManagement;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Block;
using USE_ExperimentTemplate_Trial;

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
        public List<int> chosenStims;
    }

    public class ContinuousRecognition_BlockDef : BlockDef
    { 
        public int[] BlockStimIndices, nObjectsMinMax, Ratio;

        public float
            DisplayStimsDuration,
            ChooseStimDuration,
            TrialEndDuration,
            TouchFeedbackDuration,
            DisplayResultDuration,TokenRevealDuration, TokenUpdateDuration;
        
        public List<int> PreviouslyChosenStimuli, PreviouslyNotChosenStim, TrialStimIndices, UnseenStims, NewStim;
        public Vector3[] BlockStimLocations, StimLocation;
        public int trialCount, ManuallySpecifyLocation, row, col, PC_count, PNC_count, new_count;
        public Vector3 ContextColor;
        public string ContextName;

        public override void GenerateTrialDefsFromBlockDef()
        {
            int numGrid = row * col;
            Debug.Log("num grid is " + numGrid + "row is " + row + "; col is " + col);
            Vector3[] Locations = new Vector3[numGrid];
            
            // calculate horizontal and vertical offset
            float horizontal = 12f/col;
            float vertical = 7.7f/row;
            int gridIndex = 0;
            // edges
            float x = -6;
            float y = 4;
            float z = 0;
            
            // create grid by filling in location array
            for (int i = 0; i < row; i++)
            {
                x = -6;
                for (int j = 0; j < col; j++)
                {
                    Locations[gridIndex] = new Vector3(x, y, z);
                    x += horizontal;
                    gridIndex++;
                }
                y -= vertical;
            }

            // if user want to specify their own stim location, use user location, other wise, use grid
            if (ManuallySpecifyLocation == 1)
            {
                if (StimLocation.Length < nObjectsMinMax[1])
                {
                    Debug.Log("Did not specify enough locations!");
                    Debug.Break();
                }
                else
                {
                    BlockStimLocations = StimLocation;
                    Debug.Log("BlockStimLocations lengths is " + BlockStimLocations.Length);
                }
            }
            else
            {
                BlockStimLocations = Locations;
            }

            // init some lists
            PreviouslyChosenStimuli = new List<int>();
            PreviouslyNotChosenStim = new List<int>();
            UnseenStims = new List<int>();
            TrialStimIndices = new List<int>();
            NewStim = new List<int>();
            
            // calculate total number of trials
            int numTrials = nObjectsMinMax[1] - nObjectsMinMax[0] + 1;
            TrialDefs = new ContinuousRecognition_TrialDef[numTrials]; //actual correct # 
            
            // number of stims on first trial
            int numTrialStims = nObjectsMinMax[0];
            int tmp = 0;
            bool end = false;

            // trial loop 
            for (int iTrial = 0; iTrial < numTrials && !end; iTrial++)
            {
                Debug.Log("aaaaaaaaaaa iTrial num is " + iTrial);
                Debug.Log("aaaaaaaaaaa numTiral is " + numTrials);
                ContinuousRecognition_TrialDef td = new ContinuousRecognition_TrialDef();
                td.BlockStimIndices = BlockStimIndices;
                td.trialCount = trialCount;

                // set up stim location by randomly choosing positions from grid
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
                
                // set context color according to blockConfig
                td.TrialStimLocations = arr;
                td.Grid = Locations;
                td.TrialStimIndices = TrialStimIndices;
                td.PreviouslyChosenStimuli = PreviouslyChosenStimuli;
                td.PreviouslyNotChosenStimuli = PreviouslyNotChosenStim;
                td.nObjectsMinMax = nObjectsMinMax;
                td.Ratio = Ratio;
                td.UnseenStims = UnseenStims;
                td.numTrialStims = numTrialStims;
                td.maxNumTrials = numTrials;
                td.DisplayStimsDuration = DisplayStimsDuration;
                td.ChooseStimDuration = ChooseStimDuration;
                td.TrialEndDuration = TrialEndDuration;
                td.TouchFeedbackDuration = TouchFeedbackDuration;
                td.DisplayResultDuration = DisplayResultDuration;
                td.ManuallySpecifyLocation = ManuallySpecifyLocation;
                td.row = row;
                td.col = col;
                td.PNC_count = PNC_count;
                td.PC_count = PC_count;
                td.new_Count = new_count;
                td.ContextColor = ContextColor;
                td.ContextName = ContextName;
                td.TokenRevealDuration = TokenRevealDuration;
                td.TokenUpdateDuration = TokenUpdateDuration;
                TrialDefs[iTrial] = td;
                numTrialStims++;
                trialCount++;
            }
        }
    }

    public class ContinuousRecognition_TrialDef : TrialDef
    {
        public int[] BlockStimIndices, nObjectsMinMax, Ratio, metrics;
        public Vector3[] TrialStimLocations;
        public int trialCount, numTrialStims, maxNumTrials;
        public bool isNewStim;
        public Vector3[] Grid;
        public Vector3 ContextColor;
        public string ContextName;

        public List<int> PreviouslyChosenStimuli, PreviouslyNotChosenStimuli, TrialStimIndices, UnseenStims;
        public int row, col, Context, PC_count, PNC_count, new_Count;

        public float
            DisplayStimsDuration,
            ChooseStimDuration,
            TrialEndDuration,
            TouchFeedbackDuration,
            DisplayResultDuration, TokenRevealDuration, TokenUpdateDuration;

        public int ManuallySpecifyLocation;
        
        //ObjectNums refers to items in a list of objects to be loaded from resources folder
        public int[] ObjectNums;
        
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