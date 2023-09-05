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

        // Slider Variables
        public int[] SliderGain;
        public int[] SliderLoss;

        public bool GuidedSequenceLearning;
        public int MaxCorrectTrials;


        public override void GenerateTrialDefsFromBlockDef()
        {
            //pick # of trials from minmax
            MaxTrials = RandomNumGenerator.Next(RandomMinMaxTrials[0], RandomMinMaxTrials[1]);
            TrialDefs = new List<WhatWhenWhere_TrialDef>().ConvertAll(x => (TrialDef)x);
            for (int iTrial = 0; iTrial< MaxTrials; iTrial++)
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
                td.SliderInitial = SliderInitialValue;
                td.BlockEndType = BlockEndType;
                td.BlockEndThreshold = BlockEndThreshold;
                td.BlockEndWindow = BlockEndWindow;
                td.NumPulses = NumPulses;
                td.PulseSize = PulseSize;
                td.LeaveFeedbackOn = LeaveFeedbackOn;
                td.ErrorThreshold = ErrorThreshold;
                td.MaxTrials = MaxTrials;
                td.MaxCorrectTrials = MaxCorrectTrials;
                td.GuidedSequenceLearning = GuidedSequenceLearning;
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

        // Slider Variables
        public int[] SliderGain;
        public int[] SliderLoss;
        public int SliderInitial;

        public bool GuidedSequenceLearning;
        public int MaxCorrectTrials;

    }

    public class WhatWhenWhere_StimDef : StimDef
    {
        public bool IsCurrentTarget;
        public bool IsDistractor;
    }

    public class WhatWhenWhere_TaskDef : TaskDef
    {
    }
    
}
