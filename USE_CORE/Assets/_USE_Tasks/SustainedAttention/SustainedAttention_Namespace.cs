using USE_StimulusManagement;
using USE_Def_Namespace;
using System.Collections.Generic;
using UnityEngine;


namespace SustainedAttention_Namespace
{
    public class SustainedAttention_TaskDef : TaskDef
    {
    }

    public class SustainedAttention_BlockDef : BlockDef
    {
    }

    public class SustainedAttention_TrialDef : TrialDef
    {
        public int[] TrialObjectIndices; //Where you will specify the ObjectNames you wish to use in the trial
        public float DisplayTargetDuration;
        public float DisplayDistractorsDuration;
    }

    public class SustainedAttention_StimDef : StimDef
    {
    }
}