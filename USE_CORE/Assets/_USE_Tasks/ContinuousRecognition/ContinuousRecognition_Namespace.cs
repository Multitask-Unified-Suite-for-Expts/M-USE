using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using USE_StimulusManagement;
using Random = UnityEngine.Random;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Block;
using USE_ExperimentTemplate_Trial;

namespace ContinuousRecognition_Namespace
{
    public class ContinuousRecognition_TaskDef : TaskDef
    {

    }

    public class ContinuousRecognition_BlockDef : BlockDef
    {
        // public int[] TrialDefs,
        public int[] BlockStimIndices;
        public int[] NumObjectsMinMax;
        public int[] InitialStimRatio;

        public List<int> PC_Stim;
        public List<int> PNC_Stim;
        public List<int> New_Stim;
        public List<int> Unseen_Stim;
        public List<int> TrialStimIndices;

        public int NumRows;
        public int NumColumns;
        public float X_Start;
        public float Y_Start;
        public float X_Gap;
        public float Y_Gap;
        public float X_Gap_FB;
        public float Y_Gap_FB;

        public Vector3[] BlockStimLocations; //from Config if user specifies!!!
        public Vector3[] StimLocations; //calculated below in case they don't specify locations!
        public Vector3[] BlockFeedbackLocations;
        public float[] X_Locations;
        public float[] Y_Locations;
        public float[] X_FbLocations;
        public float[] Y_FbLocations;

        public int NumTokens; //BUT HOW DO WE LINK IT TO THE TOKENBAR?

        public int TrialCount, NumRewardPulses;

        public float DisplayStimsDuration, ChooseStimDuration, TouchFeedbackDuration, TrialEndDuration,
            DisplayResultDuration, TokenRevealDuration, TokenUpdateDuration;

        public string BlockName;
        public string ContextName;

        public int ManuallySpecifyLocation;

        public override void GenerateTrialDefsFromBlockDef()
        {
            PC_Stim = new List<int>();
            PNC_Stim = new List<int>();
            New_Stim = new List<int>();
            Unseen_Stim = new List<int>();
            TrialStimIndices = new List<int>();


            //Calculate BlockStimLocations:
            StimLocations = new Vector3[X_Locations.Length * Y_Locations.Length];

            int index = 0;
            for (int i = 0; i < Y_Locations.Length; i++)
            {
                float y = Y_Locations[i];
                for (int j = 0; j < X_Locations.Length; j++)
                {
                    float x = X_Locations[j];
                    StimLocations[index] = new Vector3(x, y, 0);
                    Debug.Log(StimLocations[index]);
                    index++;
                }
            }
            if(ManuallySpecifyLocation == 0)    BlockStimLocations = StimLocations;

            //Calculate FeedbackLocations;
            BlockFeedbackLocations = new Vector3[X_FbLocations.Length * Y_FbLocations.Length];
            index = 0;
            for (int i = 0; i < Y_FbLocations.Length; i++)
            {
                float y = Y_FbLocations[i];
                for (int j = 0; j < X_FbLocations.Length; j++)
                {
                    float x = X_FbLocations[j];
                    BlockFeedbackLocations[index] = new Vector3(x, y, 0);
                    index++;
                }
            }


            int maxNumTrials = NumObjectsMinMax[1] - NumObjectsMinMax[0] + 1;
            TrialDefs = new ContinuousRecognition_TrialDef[maxNumTrials];
            int numTrialStims = NumObjectsMinMax[0]; //incremented at end
            bool theEnd = false;

            for (int trialIndex = 0; trialIndex < maxNumTrials && !theEnd; trialIndex++)
            {   
                ContinuousRecognition_TrialDef trial = new ContinuousRecognition_TrialDef();
                trial.BlockStimIndices = BlockStimIndices;

                Vector3[] trialStimLocations = new Vector3[NumObjectsMinMax[0] + trialIndex];
                for(int i = 0; i < numTrialStims; i++)
                {
                    int randomIndex = Random.Range(0, BlockStimLocations.Length);
                    while(Array.IndexOf(trialStimLocations, BlockStimLocations[randomIndex]) != -1)
                    {
                        randomIndex = Random.Range(0, BlockStimLocations.Length);    
                    }
                    trialStimLocations[i] = BlockStimLocations[randomIndex];
                }
                trial.TrialFeedbackLocations = BlockFeedbackLocations;
                trial.TrialStimLocations = trialStimLocations;
                trial.TrialStimIndices = TrialStimIndices;
                trial.PC_Stim = PC_Stim;
                trial.PNC_Stim = PNC_Stim;
                trial.Unseen_Stim = Unseen_Stim;
                trial.New_Stim = New_Stim;
                trial.NumObjectsMinMax = NumObjectsMinMax;
                trial.InitialStimRatio = InitialStimRatio;
                trial.NumTrialStims = numTrialStims;
                trial.MaxNumTrials = maxNumTrials;
                trial.DisplayStimsDuration = DisplayStimsDuration;
                trial.ChooseStimDuration = ChooseStimDuration;
                trial.DisplayResultDuration = DisplayResultDuration;
                trial.TrialEndDuration = TrialEndDuration;
                trial.TouchFeedbackDuration = TouchFeedbackDuration;
                trial.ContextName = ContextName;
                trial.TokenRevealDuration = TokenRevealDuration;
                trial.TokenUpdateDuration = TokenUpdateDuration;
                trial.TotalTokensNum = TotalTokensNum;
                trial.NumRewardPulses = NumRewardPulses;

                TrialDefs[trialIndex] = trial;
                numTrialStims++;
            }
        }

    }

    public class ContinuousRecognition_TrialDef : TrialDef
    {
        public Vector3[] TrialStimLocations;
        public Vector3[] TrialFeedbackLocations;

        public int[] BlockStimIndices;
        public int[] NumObjectsMinMax;
        public int[] InitialStimRatio;

        public List<int> PC_Stim;
        public List<int> PNC_Stim;
        public List<int> Unseen_Stim;
        public List<int> New_Stim;
        public List<int> TrialStimIndices;

        public int WrongStimIndex;
        public int NumTrialStims;
        public int MaxNumTrials;

        public int? TotalTokensNum;
        public int NumRewardPulses;

        public float DisplayStimsDuration, ChooseStimDuration, TrialEndDuration, TouchFeedbackDuration, 
            DisplayResultDuration, TokenRevealDuration, TokenUpdateDuration;

        public bool IsNewStim;
        public string ContextName;

        //Data:
        public float TimeChosen;
        public float TimeToChoice;
    }

    public class ContinuousRecognition_StimDef : StimDef
    {
        public bool PreviouslyChosen;
    }

}
