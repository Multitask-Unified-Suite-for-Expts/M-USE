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
        public float ResponseWindow; //used to control how much time they have to make selection after object animates (closes mouth)
        public bool RotateTowardsDest;

        public float DisplayTargetDuration;
        public float DisplayDistractorsDuration;
        public float PlayDuration;

        //Targets:
        public float TargetCloseDuration;
        public int[] TargetSizes;
        public int[] TargetSpeeds;
        public float[] TargetNextDestDist;
        public float[] TargetAnimIntervals;
        public float[] TargetAnimDurations;
        public Vector2[] TargetIntervalsAndDurations;

        //Distractors:
        public float DistractorCloseDuration;
        public int[] DistractorSizes;
        public int[] DistractorSpeeds;
        public float[] DistractorNextDestDist;
        public float[] DistractorAnimIntervals;
        public float[] DistractorAnimDurations;
        public Vector2[] DistractorIntervalsAndDurations;

    }

    public class SustainedAttention_StimDef : StimDef
    {
    
    }
}