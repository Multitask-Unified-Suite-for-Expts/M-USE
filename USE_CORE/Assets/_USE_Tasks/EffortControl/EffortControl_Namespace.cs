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
        public bool TokensInMiddleOfOutlines;
        public int NumClicksLeft;
        public int NumClicksRight;
        public int NumCoinsLeft;
        public int NumCoinsRight;
        public int NumPulsesLeft;
        public int NumPulsesRight;
        public int PulseSizeLeft;
        public int PulseSizeRight;
        public int ClicksPerOutline;
        public int PosStep;
        public int NegStep;
        public string TrialDefSelectionStyle;
        public int MaxDiffLevel;
        public int AvgDiffLevel;
        public int DiffLevelJitter;
        public override void GenerateTrialDefsFromBlockDef()
        {
            TrialDefs = new List<EffortControl_TrialDef>().ConvertAll(x => (TrialDef)x);

            for (int iTrial = 0; iTrial < NumTrials; iTrial++) 
            {
                EffortControl_TrialDef td = new EffortControl_TrialDef();
                td.BlockName = BlockName;
                td.ContextName = ContextName;
                td.TokensInMiddleOfOutlines = TokensInMiddleOfOutlines;
                td.NumClicksLeft = NumClicksLeft;
                td.NumClicksRight = NumClicksRight;
                td.NumCoinsLeft = NumCoinsLeft;
                td.NumCoinsRight = NumCoinsRight;
                td.NumPulsesLeft = NumPulsesLeft;
                td.NumPulsesRight = NumPulsesRight;
                td.PulseSizeLeft = PulseSizeLeft;
                td.PulseSizeRight = PulseSizeRight;
                td.ClicksPerOutline = ClicksPerOutline;
                td.DifficultyLevel = DifficultyLevel;
                td.PosStep = PosStep;
                td.NegStep = NegStep;
                td.TrialDefSelectionStyle = TrialDefSelectionStyle;
                td.MaxDiffLevel = MaxDiffLevel;
                td.AvgDiffLevel = AvgDiffLevel;
                td.DiffLevelJitter = DiffLevelJitter;
                TrialDefs.Add(td);

            }
        }
        
        public override void AddToTrialDefsFromBlockDef()
        {
            // Sets maxNum to the number of TrialDefs present, and generate a random max if a range is provided
            MaxTrials = TrialDefs.Count;
            for (int iTrial = 0; iTrial < TrialDefs.Count; iTrial++)
            {
                EffortControl_TrialDef td = (EffortControl_TrialDef)TrialDefs[iTrial];
                td.ContextName = ContextName;
                td.TrialDefSelectionStyle = TrialDefSelectionStyle;
                td.MaxDiffLevel = MaxDiffLevel;
                td.AvgDiffLevel = AvgDiffLevel;
                td.DiffLevelJitter = DiffLevelJitter;

                TrialDefs[iTrial] = td;
            }
        }
    }

    public class EffortControl_TrialDef : TrialDef
    {
        public bool TokensInMiddleOfOutlines;
        public int NumClicksLeft;
        public int NumClicksRight;
        public int NumCoinsLeft;
        public int NumCoinsRight;
        public int NumPulsesLeft;
        public int NumPulsesRight;
        public int PulseSizeLeft;
        public int PulseSizeRight;
        public int ClicksPerOutline;
        public int PosStep;
        public int NegStep;
        public string TrialDefSelectionStyle;
        public int MaxDiffLevel;
        public int AvgDiffLevel;
        public int DiffLevelJitter;
    }

    public class EffortControl_StimDef : StimDef
    {    
    }
}