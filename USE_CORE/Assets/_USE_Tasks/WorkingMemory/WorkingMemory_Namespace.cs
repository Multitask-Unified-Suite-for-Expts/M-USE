using UnityEngine;
using USE_Def_Namespace;
using USE_StimulusManagement;
using USE_ExperimentTemplate_Classes;

namespace WorkingMemory_Namespace
{
    public class WorkingMemory_TaskDef : TaskDef
    {
    }

    public class WorkingMemory_BlockDef : BlockDef
    {
        public Vector3 SampleStimLocation;
        public Vector3[] SearchStimLocations;
        public int[] SearchStimIndices;
        public Vector3[] PostSampleDistractorLocations;
        public int[] PostSampleDistractorStimIndices;
        public int[] SearchStimTokenReward;
        public Reward[][] ProbabilisticSearchStimTokenReward;


        public float DisplaySampleDuration;
        public float PostSampleDelayDuration;
        public float DisplayPostSampleDistractorsDuration;
        public float PreTargetDelayDuration;

        public override void AddToTrialDefsFromBlockDef()
        {
            for (int iTrial = 0; iTrial < TrialDefs.Count; iTrial++)
            {
                WorkingMemory_TrialDef td = (WorkingMemory_TrialDef)TrialDefs[iTrial];
                td.BlockName = BlockName;
                td.NumInitialTokens = NumInitialTokens;
                td.NumPulses = NumPulses;
                td.TokenBarCapacity = TokenBarCapacity;
                td.PulseSize = PulseSize;
                td.BlockEndType = BlockEndType;
                td.BlockEndThreshold = BlockEndThreshold;
                td.BlockEndWindow = BlockEndWindow;
                td.ContextName = ContextName;
                TrialDefs[iTrial] = td;
            }
        }
    }

    public class WorkingMemory_TrialDef : TrialDef
    {
        public Vector3 SampleStimLocation;
        public Vector3[] SearchStimLocations;
        public int[] SearchStimIndices;
        public Vector3[] PostSampleDistractorStimLocations;
        public int[] PostSampleDistractorStimIndices;
        public int[] SearchStimTokenReward;
        public Reward[][] ProbabilisticSearchStimTokenReward;

        public float DisplaySampleDuration;
        public float PostSampleDelayDuration;
        public float DisplayPostSampleDistractorsDuration;
        public float PreTargetDelayDuration;
    }

    public class WorkingMemory_StimDef : StimDef
    {
        public bool IsTarget;
    }
}