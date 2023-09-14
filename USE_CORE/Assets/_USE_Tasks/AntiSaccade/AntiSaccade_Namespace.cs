using System;
using System.Collections.Generic;
using UnityEngine;
using USE_StimulusManagement;
using USE_Def_Namespace;


namespace AntiSaccade_Namespace
{
    public class AntiSaccade_TaskDef : TaskDef
    {
    }

    public class AntiSaccade_BlockDef : BlockDef
    {
        //Inherited and Used:
        //public string ContextName;
        //public int BlockCount;
    }

    public class AntiSaccade_TrialDef : TrialDef
    {
        public int PreCue_Size;
        public string SpatialCue_Icon;
        public string Mask_Icon;
        public bool UseSpinAnimation;
        public bool DeactivateNonSelectedStimOnSel;
        public bool RandomSpatialCueColor;
        public bool RandomMaskColor;
        public int TargetStimIndex;
        public int[] DistractorStimIndices;
        public Vector3 SpatialCue_Pos;
        public Vector3 TargetStim_DisplayPos;
        public Vector3 TargetStim_ChoosePos;
        public Vector3[] DistractorStims_ChoosePos;
        public float HaloFbDuration;
        public int RewardMag;
        public float PreCueDuration;
        public float AlertCueDuration;
        public float SpatialCueDuration;
        public float DisplayTargetDuration;
        public float MaskDuration;
        public float PostMaskDelayDuration;
        public float ChooseStimDuration;
        public float FeedbackDuration;
        public float ItiDuration;
    }

    public class AntiSaccade_StimDef : StimDef
    {
        public bool IsTarget;
    }
}