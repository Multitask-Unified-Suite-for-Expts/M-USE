using USE_StimulusManagement;
using USE_Def_Namespace;
using UnityEngine;


namespace KeepTrack_Namespace
{
    public class KeepTrack_TaskDef : TaskDef
    {
    }

    public class KeepTrack_BlockDef : BlockDef
    {
    }

    public class KeepTrack_TrialDef : TrialDef
    {
        public int[] TrialObjectIndices; //Where you will specify the ObjectNames you wish to use in the trial
        public float DisplayTargetDuration;
        public float DisplayDistractorsDuration;
    }

    public class KeepTrack_StimDef : StimDef
    {
    }
}