/*
MIT License

Copyright (c) 2023 Multitask - Unified - Suite -for-Expts

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files(the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/



using System;
using System.Collections.Generic;
using UnityEngine;
using USE_StimulusManagement;
using Random = UnityEngine.Random;
using USE_Def_Namespace;


namespace ContinuousRecognition_Namespace
{
    public class ContinuousRecognition_TaskDef : TaskDef
    {
        public bool MakeStimPopOut;
    }

    public class ContinuousRecognition_BlockDef : BlockDef
    {
        //FROM BLOCK CONFIG:
        public int[] BlockStimIndices;
        public int[] NumObjectsMinMax;
        public int[] InitialStimRatio;
        public float[] X_Locations;
        public float[] Y_Locations;
        public bool ShakeStim, FindAllStim, UseStarfield, ManuallySpecifyLocation;
        public Vector3[] BlockStimLocations; //Empty unless they specify locations in block config (and set ManuallySpecifyLocation to true)

        //Calculated below (DONT SET IN CONFIG!!!):
        public int MaxNumTrials;


        public override void GenerateTrialDefsFromBlockDef()
        {
            int maxNumStim = NumObjectsMinMax[1];
            if (FindAllStim)
                MaxNumTrials = CalculateMaxNumTrials(maxNumStim);
            else
                MaxNumTrials = NumObjectsMinMax[1] - NumObjectsMinMax[0] + 1;


            //Calculate STIM Locations:
            if(!ManuallySpecifyLocation)
            {
                BlockStimLocations = new Vector3[X_Locations.Length * Y_Locations.Length];
                int stimIndex = 0;
                for (int i = 0; i < Y_Locations.Length; i++)
                {
                    float y = Y_Locations[i];
                    for (int j = 0; j < X_Locations.Length; j++)
                    {
                        float x = X_Locations[j];
                        BlockStimLocations[stimIndex] = new Vector3(x, y, 0);
                        stimIndex++;
                    }
                }
            }

            TrialDefs = new List<ContinuousRecognition_TrialDef>().ConvertAll(x=>(TrialDef)x);

            int numTrialStims = NumObjectsMinMax[0]; //Starts as first num in array and increments by at end of loop for each trial

            for (int trialIndex = 0; trialIndex < MaxNumTrials; trialIndex++)
            {   
                ContinuousRecognition_TrialDef trial = new ContinuousRecognition_TrialDef();

                Vector3[] trialStimLocations;
                if (FindAllStim && trialIndex > maxNumStim - 2)
                {
                    trialStimLocations = new Vector3[maxNumStim];
                    numTrialStims = maxNumStim;
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
                trial.BlockStimIndices = BlockStimIndices;
                trial.TrialStimLocations = trialStimLocations;
                trial.NumObjectsMinMax = NumObjectsMinMax;
                trial.InitialStimRatio = InitialStimRatio;
                trial.NumTrialStims = numTrialStims;
                trial.MaxNumTrials = MaxNumTrials;
                trial.ContextName = ContextName;
                trial.NumPulses = NumPulses;
                trial.TokenGain = TokenGain;
                trial.TokenLoss = TokenLoss;

                trial.PulseSize = PulseSize;
                trial.TokenBarCapacity = TokenBarCapacity;
                trial.FindAllStim = FindAllStim;
                trial.UseStarfield = UseStarfield;
                trial.ShakeStim = ShakeStim;

                trial.ParticleHaloActive = ParticleHaloActive;
                trial.CircleHaloActive = CircleHaloActive;

                TrialDefs.Add(trial);
                numTrialStims++;
            }
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
            while ((Num_PC + Num_New + Num_PNC) < totalTrialStim)
            {
                float PC_AddPerc = (Num_PC + 1) / (Num_PC + 1 + Num_New + Num_PNC);
                float PC_AddDiff = PC_AddPerc - PC_TargetPerc;

                float NonPC_AddPerc = Num_PC / (Num_PC + 1 + Num_New + Num_PNC);
                float NonPC_AddDiff = NonPC_AddPerc - PC_TargetPerc;

                if (PC_AddDiff < NonPC_AddDiff)
                    Num_PC++;
                else
                {
                    if (Num_PNC < Num_New)
                        Num_PNC++;
                    else
                        Num_New++;
                }
            }

            while(Num_PC + Num_New + Num_PNC > totalTrialStim)
            {
                if (Num_New > 1)
                    Num_New--;
                else
                {
                    if (Num_PC > Num_PNC || Num_PC == Num_PNC)
                        Num_PC--;
                    else
                        Num_PNC--;
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
        //FROM BLOCK CONFIG & PASSED:
        public int[] BlockStimIndices;
        public int[] NumObjectsMinMax;
        public int[] InitialStimRatio;
        public float[] X_FbLocations = new float[] {-3f, -1.8f, -.6f, .6f, 1.8f, 3f};
        public float[] Y_FbLocations = new float[] {2.1f, 1.05f, 0f, -1.05f, -2.1f};
        public bool ShakeStim, FindAllStim, UseStarfield;

        //Not in block config BUT STILL PASSED DOWN:
        public Vector3[] TrialStimLocations;
        public int NumTrialStims;
        public int MaxNumTrials;
    }

    public class ContinuousRecognition_StimDef : StimDef
    {
        public int TrialNumFirstShownOn = -1;
        public bool PreviouslyChosen;
    }

}
