using System.Collections.Generic;
using UnityEngine;
using USE_Def_Namespace;
using USE_StimulusManagement;
using USE_ExperimentTemplate_Classes;

namespace FlexLearning_Namespace
{
    public class FlexLearning_TaskDef : TaskDef
    {
    }

    public class FlexLearning_BlockDef : BlockDef
    {
        public Vector3[] TrialStimLocations;
        public int[] TrialStimIndices;
        public int[] TrialStimTokenReward;
        public Reward[][] ProbablisticTrialStimTokenReward;
        public Reward[] ProbabilisticNumPulses;
        public bool RandomizedLocations;
        public bool TokensWithStimOn = false;

        public override void GenerateTrialDefsFromBlockDef()
        {
            //pick # of trials from minmax
            MaxTrials = RandomNumGenerator.Next(RandomMinMaxTrials[0], RandomMinMaxTrials[1]);
            TrialDefs = new List<FlexLearning_TrialDef>().ConvertAll(x => (TrialDef)x);
            for (int iTrial = 0; iTrial < MaxTrials; iTrial++)
            {
                FlexLearning_TrialDef td = new FlexLearning_TrialDef();
                td.BlockName = BlockName;
                td.TrialStimIndices = TrialStimIndices;
                td.TrialStimLocations = TrialStimLocations;
                td.ContextName = ContextName;
                td.ProbabilisticTrialStimTokenReward = ProbablisticTrialStimTokenReward;
                td.NumInitialTokens = NumInitialTokens;
                td.RandomizedLocations = RandomizedLocations;
                td.BlockEndType = BlockEndType;
                td.BlockEndThreshold = BlockEndThreshold;
                td.BlockEndWindow = BlockEndWindow;
                td.ProbablisticNumPulses = ProbabilisticNumPulses;
                td.TokenBarCapacity = TokenBarCapacity;
                td.PulseSize = PulseSize;
                td.MaxTrials = MaxTrials;
                td.TokensWithStimOn = TokensWithStimOn;
                TrialDefs.Add(td);
            }
        }
        public override void AddToTrialDefsFromBlockDef()
        {
            // Sets maxNum to the number of TrialDefs present, and generate a random max if a range is provided
            MaxTrials = TrialDefs.Count;
            if (RandomMinMaxTrials != null)
            {
                if (RandomNumGenerator == null)
                    Debug.Log("RANDOM NUM GENERATOR NULL!");
                else
                    MaxTrials = RandomNumGenerator.Next(RandomMinMaxTrials[0], RandomMinMaxTrials[1]);
            }
            for (int iTrial = 0; iTrial < TrialDefs.Count; iTrial++)
            {
                FlexLearning_TrialDef td = (FlexLearning_TrialDef)TrialDefs[iTrial];
                td.BlockName = BlockName;
                td.NumInitialTokens = NumInitialTokens;
                td.BlockEndType = BlockEndType;
                td.BlockEndThreshold = BlockEndThreshold;
                td.BlockEndWindow = BlockEndWindow;
                td.ProbablisticNumPulses = ProbabilisticNumPulses;
                td.NumPulses = NumPulses;
                td.TokenBarCapacity = TokenBarCapacity;
                td.PulseSize = PulseSize;
                td.ContextName = ContextName;
                td.RandomMinMaxTrials = RandomMinMaxTrials;
                td.MaxTrials = MaxTrials;
                td.TokensWithStimOn = TokensWithStimOn;
                TrialDefs[iTrial] = td;
            }
        }
    }

    public class FlexLearning_TrialDef : TrialDef
    {
        public int[] TrialStimIndices;
        public Vector3[] TrialStimLocations;
        public int[] TrialStimTokenReward;
        public Reward[][] ProbabilisticTrialStimTokenReward;
        public Reward[] ProbablisticNumPulses;
        public bool RandomizedLocations;
        public bool TokensWithStimOn;
    }

    public class FlexLearning_StimDef : StimDef
    {
        public bool IsTarget;
    }
}