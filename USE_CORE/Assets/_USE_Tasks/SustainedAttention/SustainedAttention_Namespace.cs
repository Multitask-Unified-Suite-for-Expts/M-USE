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
        public Vector2 ResponseWindow; //used to control how much time they have to make selection after object animates (closes mouth)

        public Vector3 AngleProbs;

        public float DisplayTargetDuration;
        public float DisplayDistractorsDuration;

        //Targets:
        public float TargetMinAnimGap;
        public float TargetCloseDuration;
        public int[] TargetSizes;
        public int[] TargetSpeeds;
        public float[] TargetNextDestDist;
        public Vector2[] TargetRatesAndDurations;
        public bool RotateTargets;

        //Distractors:
        public float DistractorMinAnimGap;
        public float DistractorCloseDuration;
        public int[] DistractorSizes;
        public int[] DistractorSpeeds;
        public float[] DistractorNextDestDist;
        public Vector2[] DistractorRatesAndDurations;
        public bool RotateDistractors;

    }

    public class SustainedAttention_StimDef : StimDef
    {
    
    }
}