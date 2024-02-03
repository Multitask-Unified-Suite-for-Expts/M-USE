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

namespace WhatWhenWhere_Namespace
{
    public class WhatWhenWhere_BlockDef : BlockDef
    {
        // Stimuli Selection Variables
        public int[] CorrectObjectTouchOrder;
        public int ErrorThreshold;
        public int[] SearchStimIndices;
        public Vector3[] SearchStimLocations;
        public int[] DistractorStimIndices;
        public Vector3[] DistractorStimLocations;
        
        // Configuration of Stimuli
        public bool RandomizedLocations;
        public bool LeaveFeedbackOn;
        public bool ParticleHaloActive;

        // Slider Variables
        public int[] SliderGain;
        public int[] SliderLoss;

        public bool GuidedSequenceLearning;
        public int MaxCorrectTrials;
        public int? MaxTrialErrors;

        public float MaxSimilarity;
        public float MinSimilarity;
        public float MeanSimilarity;

        
        public override void GenerateTrialDefsFromBlockDef()
        {
            //pick # of trials from minmax
            if (RandomMinMaxTrials != null)
            {
                MaxTrials = RandomNumGenerator.Next(RandomMinMaxTrials[0], RandomMinMaxTrials[1]);
                MinTrials = RandomMinMaxTrials[0];
            }
            else
            {
                MaxTrials = MinMaxTrials[1];
                MinTrials = MinMaxTrials[0];
            }

            TrialDefs = new List<WhatWhenWhere_TrialDef>().ConvertAll(x => (TrialDef)x);
            for (int iTrial = 0; iTrial < MaxTrials; iTrial++)
            {
                WhatWhenWhere_TrialDef td = new WhatWhenWhere_TrialDef();
                td.BlockName = BlockName;
                td.ContextName = ContextName;
                td.CorrectObjectTouchOrder = CorrectObjectTouchOrder;
                td.SearchStimIndices = SearchStimIndices;
                td.DistractorStimIndices = DistractorStimIndices;
                td.SearchStimLocations = SearchStimLocations;
                td.DistractorStimLocations = DistractorStimLocations;
                td.RandomizedLocations = RandomizedLocations;
                td.SliderGain = SliderGain;
                td.SliderLoss = SliderLoss;
                td.SliderInitialValue = SliderInitialValue;
                td.BlockEndType = BlockEndType;
                td.BlockEndThreshold = BlockEndThreshold;
                td.BlockEndWindow = BlockEndWindow;
                td.NumPulses = NumPulses;
                td.PulseSize = PulseSize;
                td.LeaveFeedbackOn = LeaveFeedbackOn;
                td.ErrorThreshold = ErrorThreshold;
                td.MaxTrials = MaxTrials;
                td.MaxCorrectTrials = MaxCorrectTrials;
                td.MaxTrialErrors = MaxTrialErrors;
                td.GuidedSequenceLearning = GuidedSequenceLearning;
                td.ParticleHaloActive = ParticleHaloActive;

                TrialDefs.Add(td);
            }
        }
    }

    public class WhatWhenWhere_TrialDef : TrialDef
    {
        // Stimuli Selection Variables
        public int[] CorrectObjectTouchOrder;
        public int ErrorThreshold;
        public int[] SearchStimIndices;
        public Vector3[] SearchStimLocations;
        public int[] DistractorStimIndices;
        public Vector3[] DistractorStimLocations;
        
        // Configuration of Stimuli
        public bool RandomizedLocations;
        public bool LeaveFeedbackOn;

        public bool GuidedSequenceLearning;
        public int MaxCorrectTrials;
        public int? MaxTrialErrors;

        public bool ParticleHaloActive;
    }

    public class WhatWhenWhere_StimDef : StimDef
    {
        public bool IsCurrentTarget;
        public bool IsDistractor;
    }

    public class WhatWhenWhere_TaskDef : TaskDef
    {
    }

    public class WhatWhenWhere_BlockDataSummary
    {
        public int BlockNum;
        public int TotalTouches;
        public int CorrectTouches;
        public int IncompleteTouches;
        public float MinSimilarity;
        public float MaxSimilarity;
        public float MeanSimilarity;
        public float BlockDuration;
        public float NumRewardedTrials;
        public int TrialsToCriterion;
    }

}
