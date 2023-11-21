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



using UnityEngine;
using USE_StimulusManagement;
using USE_Def_Namespace;
using System.Collections.Generic;



namespace AntiSaccade_Namespace
{
    public class AntiSaccade_TaskDef : TaskDef
    {
    }

    public class AntiSaccade_BlockDef : BlockDef
    {
        public int PosStep;
        public int NegStep;
        public string TrialDefSelectionStyle;
        public int MaxDiffLevel;
        public int AvgDiffLevel;
        public int DiffLevelJitter;
        public int NumReversalsUntilTerm = -1;
        public int MinTrialsBeforeTermProcedure = -1;
        public int TerminationWindowSize = -1;
        
        public override void GenerateTrialDefsFromBlockDef()
        {
            TrialDefs = new List<AntiSaccade_TrialDef>().ConvertAll(x => (TrialDef)x);
            for (int iTrial = 0; iTrial < NumTrials; iTrial++) 
            {
                AntiSaccade_TrialDef td = new AntiSaccade_TrialDef();
                td.BlockName = BlockName;
                td.ContextName = ContextName;
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
                
                TrialDefs.Add(td);
            }
        }
        
        public override void AddToTrialDefsFromBlockDef()
        {
            for (int iTrial = 0; iTrial < TrialDefs.Count; iTrial++)
            {
                AntiSaccade_TrialDef td = (AntiSaccade_TrialDef)TrialDefs[iTrial];
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

    public class AntiSaccade_TrialDef : TrialDef
    {
        public string SaccadeType;
        public int PreCue_Size;
        public string SpatialCue_Icon;
        public string Mask_Icon;
        public bool SpatialCueActiveThroughDisplayTarget;
        public bool UseSpinAnimation;
        public bool DeactivateNonSelectedStimOnSel;
        public bool RandomSpatialCueColor;
        public bool RandomMaskColor;
        public int TargetStimIndex;
        public int[] DistractorStimIndices;
        public Vector3 Mask_Pos;
        public Vector3 SpatialCue_Pos;
        public Vector3 TargetStim_DisplayPos;
        public Vector3 TargetStim_ChoosePos;
        public Vector3[] DistractorStims_ChoosePos;
        public float HaloFbDuration;
        public int RewardMag;
        public float PreCueDuration;
        public float AlertCueDuration;
        public float AlertCueDelayDuration;
        public float SpatialCueDuration;
        public float SpatialCueDelayDuration;
        public float DisplayTargetDuration;
        public float MaskDuration;
        public float PostMaskDelayDuration;
        public float ChooseStimDuration;
        public float FeedbackDuration;
        public float ItiDuration;
        public int PosStep;
        public int NegStep;
        public string TrialDefSelectionStyle;
        public int MaxDiffLevel;
        public int AvgDiffLevel;
        public int DiffLevelJitter;
        public int NumReversalsUntilTerm;
        public int MinTrialsBeforeTermProcedure;
        public int TerminationWindowSize;
    }

    public class AntiSaccade_StimDef : StimDef
    {
        public bool IsTarget;
    }
}