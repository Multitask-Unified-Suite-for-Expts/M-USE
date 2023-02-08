using System;
using System.Collections.Generic;
using UnityEngine;
using USE_ExperimentTemplate_Block;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Trial;
using USE_StimulusManagement;


namespace EffortControl_Namespace
{
    public class EffortControl_TaskDef : TaskDef
    {
    }

    public class EffortControl_BlockDef : BlockDef
    {
    }

    public class EffortControl_TrialDef : TrialDef
    {
        public string TrialId;
        public int NumClicksLeft;
        public int NumClicksRight;
        public int NumCoinsLeft;
        public int NumCoinsRight;
        public int NumPulsesLeft;
        public int NumPulsesRight;
        public int PulseSizeLeft;
        public int PulseSizeRight;
        public int ClicksPerOutline;
        public int Touches;
    }

    public class EffortControl_StimDef : StimDef
    {    
    }
}