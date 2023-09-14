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
        public int PreCue_Size;
        public string SpatialCue_Icon;
        public string Mask_Icon;
        public bool UseSpinAnimation;
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

        public override void GenerateTrialDefsFromBlockDef()
        {
            TrialDefs = new List<AntiSaccade_TrialDef>().ConvertAll(x => (TrialDef)x);

            for(int i = 0; i < NumTrials; i++)
            {
                AntiSaccade_TrialDef td = new AntiSaccade_TrialDef();
                td.PreCue_Size = PreCue_Size;
                td.SpatialCue_Icon = SpatialCue_Icon;
                td.Mask_Icon = Mask_Icon;
                td.UseSpinAnimation = UseSpinAnimation;
                td.RandomSpatialCueColor = RandomSpatialCueColor;
                td.RandomMaskColor = RandomMaskColor;
                td.ContextName = ContextName;
                td.TargetStimIndex = TargetStimIndex;
                td.DistractorStimIndices = DistractorStimIndices;
                td.TargetStim_DisplayPos = TargetStim_DisplayPos;
                td.TargetStim_ChoosePos = TargetStim_ChoosePos;
                td.DistractorStims_ChoosePos = DistractorStims_ChoosePos;
                td.HaloFbDuration = HaloFbDuration;
                td.SpatialCue_Pos = SpatialCue_Pos;
                td.RewardMag = RewardMag;
                TrialDefs.Add(td);
            }
        }
    }

    public class AntiSaccade_TrialDef : TrialDef
    {
        public int PreCue_Size;
        public string SpatialCue_Icon;
        public string Mask_Icon;
        public bool UseSpinAnimation;
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
    }

    public class AntiSaccade_StimDef : StimDef
    {
        public bool IsTarget;
    }
}