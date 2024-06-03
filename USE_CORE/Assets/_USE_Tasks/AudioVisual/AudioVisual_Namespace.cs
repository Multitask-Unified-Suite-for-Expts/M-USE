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
        public float AudioClipLength;
        public bool ShowTextFeedback;

        public string WaitCueIcon;
        public float WaitCueSize;
        public float[] WaitCueColor;

        public string LeftObjectIcon;
        public float LeftObjectSize;
        public Vector3 LeftObjectPos;
        public float[] LeftObjectColor;

        public string RightObjectIcon;
        public float RightObjectSize;
        public Vector3 RightObjectPos;
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