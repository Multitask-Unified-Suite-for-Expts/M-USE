using System;
using System.Collections.Generic;
using UnityEngine;
using USE_StimulusManagement;
using USE_Def_Namespace;

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
        public float ItiDuration;

        //Target:
        public Vector2 TargetSize;
        public float TargetSpeed;
        public float TargetAnimationInterval;
        public int TargetReward;

        //Distractor:
        public Vector2 DistractorSize;
        public float DistractorSpeed;
        public float DistractorAnimationInterval;
        public int DistractorReward;

    }

    public class SustainedAttention_StimDef : StimDef
    {
    
    }
}