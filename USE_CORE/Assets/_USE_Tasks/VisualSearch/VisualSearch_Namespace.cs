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


using System.Collections.Generic;
using UnityEngine;
using USE_Def_Namespace;
using USE_StimulusManagement;
using USE_ExperimentTemplate_Classes;

namespace VisualSearch_Namespace
{
    public class VisualSearch_TaskDef : TaskDef
    {
    }

    public class VisualSearch_BlockDef : BlockDef
    {
        public Reward[][] ProbabilisticTrialStimTokenReward;
        public Reward[] ProbabilisticNumPulses;
        public bool RandomizedLocations;
        public bool TokensWithStimOn;
        public float? FeatureSimilarity;

        public override void GenerateTrialDefsFromBlockDef()
        {
            //pick # of trials from minmax
            if (RandomMinMaxTrials != null)
            {
                MaxTrials = RandomNumGenerator.Next(RandomMinMaxTrials[0], RandomMinMaxTrials[1]);
                MinTrials = RandomMinMaxTrials[0];
            }
            else
            {
                MaxTrials = MinMaxTrials[1];
                MinTrials = MinMaxTrials[0];
            }

            TrialDefs = new List<VisualSearch_TrialDef>().ConvertAll(x => (TrialDef)x);
            for (int iTrial = 0; iTrial < MaxTrials; iTrial++)
            {
                VisualSearch_TrialDef td = new VisualSearch_TrialDef();
                td.ContextName = ContextName;
                td.BlockName = BlockName;
                td.ProbablisticNumPulses = ProbabilisticNumPulses;
                td.ProbabilisticTrialStimTokenReward = ProbabilisticTrialStimTokenReward;
                td.NumPulses = NumPulses;
                td.NumInitialTokens = NumInitialTokens;
                td.TokenBarCapacity = TokenBarCapacity;
                td.PulseSize = PulseSize;
                td.TokensWithStimOn = TokensWithStimOn;
                td.MaxTrials = MaxTrials;
                td.BlockCount = BlockCount;
                td.ParticleHaloActive = ParticleHaloActive;
                td.CircleHaloActive = CircleHaloActive;
                TrialDefs[iTrial] = td;             
                TrialDefs.Add(td);
            }
        }

        public override void AddToTrialDefsFromBlockDef()
        {
            MaxTrials = TrialDefs.Count;
            MinTrials = TrialDefs.Count;

            for (int iTrial = 0; iTrial < TrialDefs.Count; iTrial++)
            {
                VisualSearch_TrialDef td = (VisualSearch_TrialDef)TrialDefs[iTrial];
                td.ContextName = ContextName;
                td.BlockName = BlockName;
                td.ProbablisticNumPulses = ProbabilisticNumPulses;
                td.NumInitialTokens = NumInitialTokens;
                td.TokenBarCapacity = TokenBarCapacity;
                td.NumPulses = NumPulses;
                td.PulseSize = PulseSize;
                td.TokensWithStimOn = TokensWithStimOn;
                td.ParticleHaloActive = ParticleHaloActive;
                td.CircleHaloActive = CircleHaloActive;
                TrialDefs[iTrial] = td;
            }
        }
    }

    public class VisualSearch_TrialDef : TrialDef
    {
        public Vector3[] TrialStimLocations;
        public int[] TrialStimIndices;
        public int[] TrialStimTokenReward;
        public Reward[][] ProbabilisticTrialStimTokenReward;
        public Reward[] ProbablisticNumPulses;
        public bool RandomizedLocations;
        public bool TokensWithStimOn;
        public float? FeatureSimilarity;
    }

    public class VisualSearch_StimDef : StimDef
    {
        public bool IsTarget;
    }

    public class VisualSearch_TrialDataSummary
    {
        public float? FeatureSimilarity;
        public float? ReactionTime;
        public int NumDistractors;
        public float? SelectionPrecision;
        public int? CorrectSelection;

    }
    public class VisualSearch_TaskDataSummary
    {
        public float? AvgReactionTime;
        public double? HighFeatureSimilarityAccuracy;
        public double? LowFeatureSimilarityAccuracy;
        public double? DistractorInterferenceAccuracy;
        public double? DistractorInterferenceReactionTime;
        public float? AvgSelectionPrecision;
        public float? TotalAccuracy;
        public float? MedianFeatureSimilarity;

    }
}