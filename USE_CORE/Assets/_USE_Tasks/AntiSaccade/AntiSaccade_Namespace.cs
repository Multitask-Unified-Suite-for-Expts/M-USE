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
        public bool RandomSpatialCueColor;
        public int TargetStimIndex;
        public int[] DistractorStimIndices;
        public Vector3 SpacialCue_Pos;
        public Vector3 TargetStim_DisplayPos;
        public Vector3 TargetStim_ChoosePos;
        public Vector3[] DistractorStims_ChoosePos;
        public int RewardMag;

        public override void GenerateTrialDefsFromBlockDef()
        {
            TrialDefs = new List<AntiSaccade_TrialDef>().ConvertAll(x => (TrialDef)x);

            for(int i = 0; i < NumTrials; i++)
            {
                AntiSaccade_TrialDef td = new AntiSaccade_TrialDef();
                td.RandomSpatialCueColor = RandomSpatialCueColor;
                td.ContextName = ContextName;
                td.TargetStimIndex = TargetStimIndex;
                td.DistractorStimIndices = DistractorStimIndices;
                td.TargetStim_DisplayPos = TargetStim_DisplayPos;
                td.TargetStim_ChoosePos = TargetStim_ChoosePos;
                td.DistractorStims_ChoosePos = DistractorStims_ChoosePos;
                td.SpacialCue_Pos = SpacialCue_Pos;
                td.RewardMag = RewardMag;
                TrialDefs.Add(td);
            }
        }
    }

    public class AntiSaccade_TrialDef : TrialDef
    {
        public bool RandomSpatialCueColor;
        public int TargetStimIndex;
        public int[] DistractorStimIndices;
        public Vector3 SpacialCue_Pos;
        public Vector3 TargetStim_DisplayPos;
        public Vector3 TargetStim_ChoosePos;
        public Vector3[] DistractorStims_ChoosePos;
        public int RewardMag;
    }

    public class AntiSaccade_StimDef : StimDef
    {
        public bool IsTarget;
    }
}