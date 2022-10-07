using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using USE_ExperimentTemplate;
using USE_StimulusManagement;
using Random = UnityEngine.Random;

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

        public Vector3[] BlockStimLocations;

        // public int BlockCount, TotalTokenNums, MaxTrials
        public int TrialCount, NumRewardPulses,
            PC_Count, New_Count, PNC_Count; //I removed the PNC_Count, New_Count, PC_Count;

        public float DisplayStimsDuration, ChooseStimDuration, TouchFeedbackDuration, TrialEndDuration,
            DisplayResultDuration, TokenRevealDuration, TokenUpdateDuration;

        public string BlockName;
        public string ContextName;

        public override void GenerateTrialDefsFromBlockDef()
        {
            PC_Stim = new List<int>();
            PNC_Stim = new List<int>();
            New_Stim = new List<int>();
            Unseen_Stim = new List<int>();
            TrialStimIndices = new List<int>();

            int maxNumTrials = NumObjectsMinMax[1] - NumObjectsMinMax[0] + 1;
            TrialDefs = new ContinuousRecognition_TrialDef[maxNumTrials];
            int numTrialStims = NumObjectsMinMax[0]; //incremented at end
            bool theEnd = false;

            for(int trialIndex = 0; trialIndex < maxNumTrials && !theEnd; trialIndex++)
            {
                ContinuousRecognition_TrialDef trial = new ContinuousRecognition_TrialDef();
                trial.BlockStimIndices = BlockStimIndices;
                trial.TrialCount = TrialCount;

                Vector3[] trialStimLocations = new Vector3[NumObjectsMinMax[0] + trialIndex];

                for(int i = 0; i < numTrialStims; i++)
                {
                    int randomIndex = Random.Range(0, BlockStimLocations.Length);
                    while(Array.IndexOf(trialStimLocations, BlockStimLocations[randomIndex]) != -1)
                    {
                        randomIndex = Random.Range(0, BlockStimIndices.Length);    
                    }
                    trialStimLocations[i] = BlockStimLocations[randomIndex];
                }
                trial.TrialStimLocations = trialStimLocations;
                trial.TrialStimIndices = TrialStimIndices;
                trial.PC_Stim = PC_Stim;
                trial.PNC_Stim = PNC_Stim;
                trial.Unseen_Stim = Unseen_Stim;
                trial.New_Stim = New_Stim;
                trial.PNC_Count = PNC_Count;
                trial.PC_Count = PC_Count;
                trial.New_Count = New_Count;
                trial.NumObjectsMinMax = NumObjectsMinMax;
                trial.InitialStimRatio = InitialStimRatio;
                trial.NumTrialStims = numTrialStims;
                trial.MaxNumTrials = maxNumTrials;
                trial.DisplayStimsDuration = DisplayStimsDuration;
                trial.ChooseStimDuration = ChooseStimDuration;
                trial.TrialEndDuration = TrialEndDuration;
                trial.TouchFeedbackDuration = TouchFeedbackDuration;
                trial.ContextName = ContextName;
                trial.TokenRevealDuration = TokenRevealDuration;
                trial.TokenUpdateDuration = TokenUpdateDuration;

                TrialDefs[trialIndex] = trial;
                numTrialStims++;
                TrialCount++;
            }
        }

    }

    public class ContinuousRecognition_TrialDef : TrialDef
    {
        public Vector3[] TrialStimLocations;

        public int[] BlockStimIndices;
        public int[] NumObjectsMinMax;
        public int[] InitialStimRatio;

        public List<int> PC_Stim;
        public List<int> PNC_Stim;
        public List<int> Unseen_Stim;
        public List<int> New_Stim;
        public List<int> TrialStimIndices;

        public int WrongStimIndex;

        public int TrialCount;
        public int NumTrialStims;
        public int MaxNumTrials;
        public int PC_Count;
        public int PNC_Count;
        public int New_Count;

        public float DisplayStimsDuration, ChooseStimDuration, TrialEndDuration, TouchFeedbackDuration, 
            DisplayResultDuration, TokenRevealDuration, TokenUpdateDuration;

        public bool IsNewStim;
        public string ContextName;
    }

    public class ContinuousRecognition_StimDef : StimDef
    {
        public bool PreviouslyChosen;
    }

}
