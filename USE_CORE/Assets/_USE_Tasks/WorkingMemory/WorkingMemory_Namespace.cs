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
        public int NumInitialTokens;
        public int NumTokenBar;
        public bool StimFacingCamera;

        public override void AddToTrialDefsFromBlockDef()
        {
            for (int iTrial = 0; iTrial < TrialDefs.Count; iTrial++)
            {
                WorkingMemory_TrialDef td = (WorkingMemory_TrialDef)TrialDefs[iTrial];
                td.BlockName = BlockName;
                td.NumInitialTokens = NumInitialTokens;
                td.NumPulses = NumPulses;
                td.NumTokenBar = NumTokenBar;
                td.PulseSize = PulseSize;
                td.BlockEndType = BlockEndType;
                td.BlockEndThreshold = BlockEndThreshold;
                td.BlockEndWindow = BlockEndWindow;
                td.StimFacingCamera = StimFacingCamera;
                td.ContextName = ContextName;
                TrialDefs[iTrial] = td;
            }
        }
    }

    public class WorkingMemory_TrialDef : TrialDef
    {
        public int[] SearchStimIndices, PostSampleDistractorIndices;

        public Reward[][] SearchStimTokenReward;
        public Vector3[] SearchStimLocations, PostSampleDistractorLocations, TargetSampleLocation;

        public int NumInitialTokens;
        public int NumTokenBar;
        public bool StimFacingCamera;
    }

    public class WorkingMemory_StimDef : StimDef
    {
        public bool IsTarget;
    }
}