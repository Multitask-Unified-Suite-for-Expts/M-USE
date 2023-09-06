using System;
using System.Collections.Generic;
using UnityEngine;
using USE_StimulusManagement;
using USE_Def_Namespace;
using EffortControl_Namespace;

namespace AntiSaccade_Namespace
{
    public class AntiSaccade_TaskDef : TaskDef
    {

    }

    public class AntiSaccade_BlockDef : BlockDef
    {
        public int TargetStimIndex;
        public int[] DistractorStimIndices;
        public Vector3 TargetStimPosition;
        public Vector3[] DistractorStimPositions;
        public Vector3 SpacialCuePosition;

        public override void GenerateTrialDefsFromBlockDef()
        {
            TrialDefs = new List<AntiSaccade_TrialDef>().ConvertAll(x => (TrialDef)x);

            for(int i = 0; i < NumTrials; i++)
            {
                AntiSaccade_TrialDef td = new AntiSaccade_TrialDef();
                td.ContextName = ContextName;
                td.TargetStimIndex = TargetStimIndex;
                td.DistractorStimIndices = DistractorStimIndices;
                td.TargetStimPosition = TargetStimPosition;
                td.DistractorStimPositions = DistractorStimPositions;
                td.SpacialCuePosition = SpacialCuePosition;

                TrialDefs.Add(td);
            }
        }
    }

    public class AntiSaccade_TrialDef : TrialDef
    {
        public int TargetStimIndex;
        public int[] DistractorStimIndices;
        public Vector3 TargetStimPosition;
        public Vector3[] DistractorStimPositions;
        public Vector3 SpacialCuePosition;

    }

    public class AntiSaccade_StimDef : StimDef
    {

    }
}