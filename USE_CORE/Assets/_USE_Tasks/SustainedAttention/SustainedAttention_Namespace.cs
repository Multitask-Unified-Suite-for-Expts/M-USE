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

        public float DisplayTargetDuration;
        public float DisplayDistractorsDuration;
        public float PlayDuration;

        //Targets:
        public bool RotateTargets;
        public float TargetCloseDuration;
        public int[] TargetSizes;
        public int[] TargetSpeeds;
        public float[] TargetNextDestDist;
        public Vector2[] TargetIntervalsAndDurations;

        //Distractors:
        public bool RotateDistractors;
        public float DistractorCloseDuration;
        public int[] DistractorSizes;
        public int[] DistractorSpeeds;
        public float[] DistractorNextDestDist;
        public Vector2[] DistractorIntervalsAndDurations;

    }

    public class SustainedAttention_StimDef : StimDef
    {
    
    }
}