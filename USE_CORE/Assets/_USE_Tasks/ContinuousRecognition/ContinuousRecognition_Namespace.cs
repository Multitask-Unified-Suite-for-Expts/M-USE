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

        public Vector3[] BlockFeedbackLocations; //from config.

        // public int BlockCount, TotalTokenNums, MaxTrials
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
            StimLocations = new Vector3[NumRows * NumColumns];
            float x = X_Start;
            float y = Y_Start;
            int index = 0;

            for(int i = 0; i < NumColumns; i++) //Y loop
            {
                x = X_Start;
                for(int j = 0; j < NumRows; j++) //X Loop
                {
                    StimLocations[index] = new Vector3(x, y, 0);
                    x += X_Gap;
                    index++;
                }
                y -= Y_Gap;
            }

            if(ManuallySpecifyLocation == 0)    BlockStimLocations = StimLocations;


            //Calculate FeedbackLocations;
            BlockFeedbackLocations = new Vector3[NumRows * NumColumns];
            x = X_Start;
            y = Y_Start;
            index = 0;

            for (int i = 0; i < NumColumns; i++)
            {
                x = X_Start;
                for (int j = 0; j < NumRows; j++)
                {
                    BlockFeedbackLocations[index] = new Vector3(x, y, 0);
                    x += X_Gap_FB;
                    index++;
                }
                y -= Y_Gap_FB;
            }


            var s = "";
            foreach (var location in BlockFeedbackLocations) s += location;
            Debug.Log(s);


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
