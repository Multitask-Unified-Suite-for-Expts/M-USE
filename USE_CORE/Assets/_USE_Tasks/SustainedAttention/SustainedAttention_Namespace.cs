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
        public bool RotateTowardsDest;

        public float DisplayTargetDuration;
        public float DisplayDistractorsDuration;
        public float PlayDuration;

        //Targets:
        public int[] TargetSizes;
        public int[] TargetSpeeds;
        public float[] TargetNextDestDist;
        public float[] TargetAnimationIntervals;

        //Distractors:
        public int[] DistractorSizes;
        public int[] DistractorSpeeds;
        public float[] DistractorNextDestDist;
        public float[] DistractorAnimationIntervals;
    }

    public class SustainedAttention_StimDef : StimDef
    {
    
    }
}