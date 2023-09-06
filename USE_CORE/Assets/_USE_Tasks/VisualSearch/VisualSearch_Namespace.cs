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
        public bool TokensWithStimOn = false;

        public override void GenerateTrialDefsFromBlockDef()
        {
            //pick # of trials from minmax
            MaxTrials = RandomNumGenerator.Next(RandomMinMaxTrials[0], RandomMinMaxTrials[1]);
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
                TrialDefs[iTrial] = td;             
                TrialDefs.Add(td);
            }
        }
        public override void AddToTrialDefsFromBlockDef()
        {
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

                TrialDefs[iTrial] = td;
            }
        }
    }

    public class VisualSearch_TrialDef : TrialDef
    {
        public int[] TrialStimIndices;
        public Vector3[] TrialStimLocations;
        public int[] TrialStimTokenReward;
        public Reward[][] ProbabilisticTrialStimTokenReward;
        public Reward[] ProbablisticNumPulses;
        public bool TokensWithStimOn;
        public bool RandomizedLocations;
    }

    public class VisualSearch_StimDef : StimDef
    {
        public bool IsTarget;
    }
}