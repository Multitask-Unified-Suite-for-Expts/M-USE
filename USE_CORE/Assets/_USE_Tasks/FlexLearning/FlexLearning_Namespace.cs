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
        public int[] TrialStimIndices;
        public Vector3[] TrialStimLocations;
        public int NumInitialTokens;
        public Reward[][] TrialStimTokenReward;
        public Reward[] PulseReward;
        public int NumTokenBar;
        public bool RandomizedLocations;
        public bool? TokensWithStimOn = null;

        public override void GenerateTrialDefsFromBlockDef()
        {
            //pick # of trials from minmax
            MaxTrials = RandomNumGenerator.Next(MinMaxTrials[0], MinMaxTrials[1]);
            TrialDefs = new List<FlexLearning_TrialDef>().ConvertAll(x => (TrialDef)x);
            for (int iTrial = 0; iTrial < MaxTrials; iTrial++)
            {
                FlexLearning_TrialDef td = new FlexLearning_TrialDef();
                td.BlockName = BlockName;
                td.TrialStimIndices = TrialStimIndices;
                td.TrialStimLocations = TrialStimLocations;
                td.ContextName = ContextName;
                td.TrialStimTokenReward = TrialStimTokenReward;
                td.NumInitialTokens = NumInitialTokens;
                td.RandomizedLocations = RandomizedLocations;
                td.BlockEndType = BlockEndType;
                td.BlockEndThreshold = BlockEndThreshold;
                td.BlockEndWindow = BlockEndWindow;
                td.PulseReward = PulseReward;
                td.NumTokenBar = NumTokenBar;
                td.PulseSize = PulseSize;
                td.MaxTrials = MaxTrials;
                if (TokensWithStimOn != null)
                    td.TokensWithStimOn = TokensWithStimOn;
                else
                    td.TokensWithStimOn = false;
                TrialDefs.Add(td);
            }
        }
        public override void AddToTrialDefsFromBlockDef()
        {
            // Sets maxNum to the number of TrialDefs present, and generate a random max if a range is provided
            MaxTrials = TrialDefs.Count;
            if (MinMaxTrials != null)
            {
                if (RandomNumGenerator == null)
                    Debug.Log("RANDOM NUM GENERATOR NULL!");

                MaxTrials = RandomNumGenerator.Next(MinMaxTrials[0], MinMaxTrials[1]);
            }
            for (int iTrial = 0; iTrial < TrialDefs.Count; iTrial++)
            {
                FlexLearning_TrialDef td = (FlexLearning_TrialDef)TrialDefs[iTrial];
                td.BlockName = BlockName;
                td.NumInitialTokens = NumInitialTokens;
                td.BlockEndType = BlockEndType;
                td.BlockEndThreshold = BlockEndThreshold;
                td.BlockEndWindow = BlockEndWindow;
                td.PulseReward = PulseReward;
                td.NumTokenBar = NumTokenBar;
                td.PulseSize = PulseSize;
                td.ContextName = ContextName;
                td.MinMaxTrials = MinMaxTrials;
                td.MaxTrials = MaxTrials;
                if (TokensWithStimOn != null)
                    td.TokensWithStimOn = TokensWithStimOn;
                else
                    td.TokensWithStimOn = false;
                TrialDefs[iTrial] = td;
            }
        }
    }

    public class FlexLearning_TrialDef : TrialDef
    {
        public int[] TrialStimIndices;
        public Vector3[] TrialStimLocations;
        public Reward[][] TrialStimTokenReward;
        public Reward[] PulseReward;
        public bool RandomizedLocations;
        public bool StimFacingCamera;
        public int NumInitialTokens;
        public int NumTokenBar;
        public bool? TokensWithStimOn;
        public int[] MinMaxTrials;

    }

    public class FlexLearning_StimDef : StimDef
    {
        public bool IsTarget;

    }
}