/*
MIT License

Copyright (c) 2023 Multitask - Unified - Suite -for-Expts

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files(the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/


using System.Collections.Generic;
using UnityEngine;
using USE_Def_Namespace;
using USE_StimulusManagement;


namespace EffortControl_Namespace
{
    public class EffortControl_TaskDef : TaskDef
    {
    }

    public class EffortControl_BlockDef : BlockDef
    {
        public int NumClicksLeft;
        public int NumClicksRight;
        public int NumCoinsLeft;
        public int NumCoinsRight;
        public int NumPulsesLeft;
        public int NumPulsesRight;
        public int PulseSizeLeft;
        public int PulseSizeRight;
        public int ClicksPerOutline;
        public bool TokensInMiddleOfOutlines;
        public int PosStep;
        public int NegStep;
        public string TrialDefSelectionStyle;
        public int MaxDiffLevel;
        public int AvgDiffLevel;
        public int DiffLevelJitter;
        public int NumReversalsUntilTerm = -1;
        public int MinTrialsBeforeTermProcedure = -1;
        public int TerminationWindowSize = -1;
        public float[] RandomMinTrialDuration;
        public float? MinTrialDuration;
        

        public override void GenerateTrialDefsFromBlockDef()
        {
            //pick # of trials from minmax
            if (RandomMinMaxTrials != null)
            {
                MaxTrials = RandomNumGenerator.Next(RandomMinMaxTrials[0], RandomMinMaxTrials[1]);
                MinTrials = RandomMinMaxTrials[0];
            }
            else if (MinMaxTrials != null)
            {
                MaxTrials = MinMaxTrials[1];
                MinTrials = MinMaxTrials[0];
            }

            if(RandomMinTrialDuration != null)
            {
                int lowestPossibleDuration = (int)(RandomMinTrialDuration[0] * 100);
                int highestPossibleDuration = (int)(RandomMinTrialDuration[1] * 100);
                MinTrialDuration = (RandomNumGenerator.Next(lowestPossibleDuration, highestPossibleDuration)) / 100;
            }
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
                td.NumReversalsUntilTerm = NumReversalsUntilTerm;
                td.MinTrialsBeforeTermProcedure = MinTrialsBeforeTermProcedure;
                td.TerminationWindowSize = TerminationWindowSize;
                td.MinTrialDuration = MinTrialDuration;
                TrialDefs.Add(td);
            }
        }
        
        public override void AddToTrialDefsFromBlockDef()
        {
            MaxTrials = TrialDefs.Count;

            for (int iTrial = 0; iTrial < TrialDefs.Count; iTrial++)
            {
                EffortControl_TrialDef td = (EffortControl_TrialDef)TrialDefs[iTrial];
                td.ContextName = ContextName;
                td.TrialDefSelectionStyle = TrialDefSelectionStyle;
                td.MaxDiffLevel = MaxDiffLevel;
                td.AvgDiffLevel = AvgDiffLevel;
                td.DiffLevelJitter = DiffLevelJitter;
                td.NumReversalsUntilTerm = NumReversalsUntilTerm;
                td.MinTrialsBeforeTermProcedure = MinTrialsBeforeTermProcedure;
                td.TerminationWindowSize = TerminationWindowSize;

                TrialDefs[iTrial] = td;
            }
        }
    }

    public class EffortControl_TrialDef : TrialDef
    {
        public int NumClicksLeft;
        public int NumClicksRight;
        public int NumCoinsLeft;
        public int NumCoinsRight;
        public int NumPulsesLeft;
        public int NumPulsesRight;
        public int PulseSizeLeft;
        public int PulseSizeRight;
        public int ClicksPerOutline;
        public bool TokensInMiddleOfOutlines;
        public int PosStep;
        public int NegStep;
        public string TrialDefSelectionStyle;
        public int MaxDiffLevel;
        public int AvgDiffLevel;
        public int DiffLevelJitter;
        public int NumReversalsUntilTerm;
        public int MinTrialsBeforeTermProcedure;
        public int TerminationWindowSize;
        public float[] RandomMinTrialDuration;
        public float? MinTrialDuration;
    }

    public class EffortControl_StimDef : StimDef
    {    
    }
}