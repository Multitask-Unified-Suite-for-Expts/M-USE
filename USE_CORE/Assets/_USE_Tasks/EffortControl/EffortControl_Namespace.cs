using System.Collections.Generic;
using USE_Def_Namespace;
using USE_StimulusManagement;


namespace EffortControl_Namespace
{
    public class EffortControl_TaskDef : TaskDef
    {
    }

    public class EffortControl_BlockDef : BlockDef
    {
        public string BlockName;
        public string ContextName;
        public string TrialId;
        public int NumTrials;
        public int NumClicksLeft;
        public int NumClicksRight;
        public int NumCoinsLeft;
        public int NumCoinsRight;
        public int NumPulsesLeft;
        public int NumPulsesRight;
        public int PulseSizeLeft;
        public int PulseSizeRight;
        public int ClicksPerOutline;
        public override void GenerateTrialDefsFromBlockDef()
        {
            TrialDefs = new List<EffortControl_TrialDef>().ConvertAll(x => (TrialDef)x);

            for (int iTrial = 0; iTrial < NumTrials; iTrial++) // Set to NumTrials in the BlockDef
            {
                EffortControl_TrialDef td = new EffortControl_TrialDef();
                td.BlockName = BlockName;
                td.ContextName = ContextName;
                td.TrialId = TrialId;
                td.NumClicksLeft = NumClicksLeft;
                td.NumClicksRight = NumClicksRight;
                td.NumCoinsLeft = NumCoinsLeft;
                td.NumCoinsRight = NumCoinsRight;
                td.NumPulsesLeft = NumPulsesLeft;
                td.NumPulsesRight = NumPulsesRight;
                td.PulseSizeLeft = PulseSizeLeft;
                td.PulseSizeRight = PulseSizeRight;
                td.ClicksPerOutline = ClicksPerOutline;
                TrialDefs.Add(td);

            }
        }
    }

    public class EffortControl_TrialDef : TrialDef
    {
        public string TrialId;
        public string BlockName;
        public string ContextName;
        public int NumClicksLeft;
        public int NumClicksRight;
        public int NumCoinsLeft;
        public int NumCoinsRight;
        public int NumPulsesLeft;
        public int NumPulsesRight;
        public int PulseSizeLeft;
        public int PulseSizeRight;
        public int ClicksPerOutline;
    }

    public class EffortControl_StimDef : StimDef
    {    
    }
}