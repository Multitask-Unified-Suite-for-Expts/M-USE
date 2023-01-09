
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
        //--------------Inherited from TaskDef-----------------------------//
        //public string TaskName;
        //public string ExternalStimFolderPath;
        //public string PrefabStimFolderPath;
        //public string ExternalStimExtension;
        //public List<string[]> FeatureNames;
        //public string neutralPatternedColorName;
        //public float? ExternalStimScale;
        //public List<string[]> FeedbackControllers;
        //public int? TotalTokensNum;
        //public bool SerialPortActive, SyncBoxActive,
        //            EventCodesActive, RewardPulsesActive, SonicationActive;
        //public string SelectionType;
    }

    public class ContinuousRecognition_BlockDef : BlockDef
    {
        //INHERITED FROM BLOCKDEF:
        //public List<TrialDef> TrialDefs;
        //public int BlockCount;
        //public int? TotalTokensNum;
        //public int? MinTrials, MaxTrials;

        //FROM BLOCK CONFIG:
        public int[] BlockStimIndices;
        public int[] NumObjectsMinMax;
        public int[] InitialStimRatio;
        public float[] X_Locations;
        public float[] Y_Locations;
        public float[] X_FbLocations;
        public float[] Y_FbLocations;
        public int NumTokenBar, TrialCount, NumRewardPulses, PulseSize, RewardMag;
        public string BlockName, ContextName, ShadowType;
        public bool IsHuman, FindAllStim, StimFacingCamera, UseStarfield, ManuallySpecifyLocation;

        public List<int> PC_Stim;
        public List<int> PNC_Stim;
        public List<int> New_Stim;
        public List<int> Unseen_Stim;
        public List<int> TrialStimIndices;
        public int MaxNumTrials;
        public int MaxNumStim;
        public Vector3[] BlockStimLocations; //Value comes from Block Config if you specify. 
        public Vector3[] StimLocations; //Value calculated below. Used if you don't specify locations.
        public Vector3[] BlockFeedbackLocations;


        public override void GenerateTrialDefsFromBlockDef()
        {
            MaxNumStim = NumObjectsMinMax[1];
            if (FindAllStim)
                MaxNumTrials = CalculateMaxNumTrials(MaxNumStim);
            else
                MaxNumTrials = NumObjectsMinMax[1] - NumObjectsMinMax[0] + 1;

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
                    index++;
                }
            }
            if(!ManuallySpecifyLocation)
                BlockStimLocations = StimLocations;

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

            TrialDefs = new List<ContinuousRecognition_TrialDef>().ConvertAll(x=>(TrialDef)x);

            int numTrialStims = NumObjectsMinMax[0]; //Starts as first num in array and increments by at end of loop for each trial

            for (int trialIndex = 0; trialIndex < MaxNumTrials; trialIndex++)
            {   
                ContinuousRecognition_TrialDef trial = new ContinuousRecognition_TrialDef();
                trial.BlockStimIndices = BlockStimIndices;

                Vector3[] trialStimLocations;
                if (FindAllStim && trialIndex > MaxNumStim - 2)
                {
                    trialStimLocations = new Vector3[MaxNumStim];
                    numTrialStims = MaxNumStim;
                }
                else trialStimLocations = new Vector3[NumObjectsMinMax[0] + trialIndex];

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
                trial.MaxNumTrials = MaxNumTrials;
                trial.MaxNumStim = MaxNumStim;
                trial.ContextName = ContextName;
                trial.NumRewardPulses = NumRewardPulses;
                trial.RewardMag = RewardMag;
                trial.PulseSize = PulseSize;
                trial.NumTokenBar = NumTokenBar;
                trial.PC_Percentage_String = CalcPercentagePC();
                trial.FindAllStim = FindAllStim;
                trial.StimFacingCamera = StimFacingCamera;
                trial.ShadowType = ShadowType;
                trial.UseStarfield = UseStarfield;
                trial.IsHuman = IsHuman;

                TrialDefs.Add(trial);
                numTrialStims++;
            }
        }

        string CalcPercentagePC()
        {
            float[] all = GetStimPercentages();
            float multiplied = all[0] * 100;
            return multiplied.ToString() + "%";
        }

        int CalculateMaxNumTrials(int maxNumStim)
        {
            return maxNumStim + CalculateNumRemaining_EOT(maxNumStim);
        }

        int CalculateNumRemaining_EOT(int totalTrialStim)
        {
            int NumRemaining_BEG = 0;
            int NumRemaining_END = 0;
            for(int i = 0; i <= totalTrialStim-2; i++)
            {
                int num_New = GetNumNewStim_Trial(i+2);
               
                NumRemaining_END = NumRemaining_BEG + num_New - 1;
                NumRemaining_BEG = NumRemaining_END;
            }
            return NumRemaining_END;
        }

        int GetNumNewStim_Trial(int totalTrialStim)
        {
            float[] stimPercentages = GetStimPercentages();

            int Num_PC = (int)Math.Floor((double)stimPercentages[0] * totalTrialStim);
            int Num_New = (int)Math.Floor((double)stimPercentages[1] * totalTrialStim);
            int Num_PNC = (int)Math.Floor((double)stimPercentages[2] * totalTrialStim);
            if (Num_PC == 0) Num_PC = 1;
            if (Num_New == 0) Num_New = 1;
            if (Num_PNC == 0) Num_PNC = 1;

            float PC_TargetPerc = stimPercentages[0];
            int temp = 2;
            while ((Num_PC + Num_New + Num_PNC) < totalTrialStim)
            {
                float currentPerc = Num_PC / (Num_PC + Num_New + Num_PNC);
                float percDiff = currentPerc - PC_TargetPerc;

                float PC_AddPerc = (Num_PC + 1) / (Num_PC + 1 + Num_New + Num_PNC);
                float PC_AddDiff = PC_AddPerc - PC_TargetPerc;

                float NonPC_AddPerc = Num_PC / (Num_PC + 1 + Num_New + Num_PNC);
                float NonPC_AddDiff = NonPC_AddPerc - PC_TargetPerc;

                if (PC_AddDiff < NonPC_AddDiff)
                    Num_PC++;
                else
                {
                    if (temp % 2 == 0)
                        Num_New++;
                    else
                        Num_PNC++;
                }
            }
            return Num_New;
        }

        float[] GetStimPercentages()
        {
            var ratio = InitialStimRatio;
            float sum = 0;
            float[] stimPercentages = new float[ratio.Length];

            foreach (var num in ratio)
                sum += num;
            for (int i = 0; i < ratio.Length; i++)
                stimPercentages[i] = ratio[i] / sum;
            
            return stimPercentages;
        }

    }

    public class ContinuousRecognition_TrialDef : TrialDef
    {
        //INHERITED FROM TRIALDEF:
        //public int BlockCount, TrialCountInBlock, TrialCountInTask;
        //public TrialStims TrialStims;

        public bool IsHuman;
        public bool UseStarfield;
        public bool FindAllStim;
        public bool StimFacingCamera;

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
        public int MaxNumStim;

        public int NumTokenBar;
        public int NumRewardPulses;
        public int RewardMag;
        public int PulseSize;

        public bool GotTrialCorrect;
        public string ShadowType;
        public string ContextName;

        //Data:
        public float TimeChosen;
        public float TimeToChoice;

        public string Locations_String;
        public string PC_String;
        public string New_String;
        public string PNC_String;

        public string PC_Percentage_String;
    }

    public class ContinuousRecognition_StimDef : StimDef
    {
        public bool PreviouslyChosen;
    }

}
