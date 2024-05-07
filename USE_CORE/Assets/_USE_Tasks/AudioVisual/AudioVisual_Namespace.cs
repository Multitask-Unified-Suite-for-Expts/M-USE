using System;
using System.Collections.Generic;
using UnityEngine;
using USE_StimulusManagement;
using USE_Def_Namespace;

namespace AudioVisual_Namespace
{
    public class AudioVisual_TaskDef : TaskDef
    {
    }

    public class AudioVisual_BlockDef : BlockDef
    {
    }

    public class AudioVisual_TrialDef : TrialDef
    {
        public string AudioClipName;
        public string CorrectObject;
        public float AudioClipPlayDuration;


        public float WaitCueSize;
        public float[] WaitCueColor;

        public float LeftObjectSize;
        public float LeftObjectPos;
        public float[] LeftObjectColor;

        public float RightObjectSize;
        public float RightObjectPos;
        public float[] RightObjectColor;


        public float PreparationDuration;
        public float DisplayOptionsDuration;
        public float WaitPeriodDuration;
        public float ChoiceDuration;
        public float FeedbackDuration;
        public float ItiDuration;

    }

    public class AudioVisual_StimDef : StimDef
    {
    }
}