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
        public int[] SearchStimsIndices;
        public Vector3[] SearchStimsLocations;
        public int[] DistractorStimsIndices;
        public Vector3[] DistractorStimsLocations;
        
        // Configuration of Stimuli
        public bool RandomizedLocations;
        public bool LeaveFeedbackOn;

        // Slider Variables
        public int[] SliderGain;
        public int[] SliderLoss;
        public int SliderInitial;

        public override void GenerateTrialDefsFromBlockDef()
        {
            //pick # of trials from minmax
            MaxTrials = RandomNumGenerator.Next(MinMaxTrials[0], MinMaxTrials[1]);
            TrialDefs = new List<WhatWhenWhere_TrialDef>().ConvertAll(x => (TrialDef)x);
            for (int iTrial = 0; iTrial< MaxTrials; iTrial++)
            {
                WhatWhenWhere_TrialDef td = new WhatWhenWhere_TrialDef();
                td.BlockName = BlockName;
                td.ContextName = ContextName;
                td.CorrectObjectTouchOrder = CorrectObjectTouchOrder;
                td.SearchStimsIndices = SearchStimsIndices;
                td.DistractorStimsIndices = DistractorStimsIndices;
                td.SearchStimsLocations = SearchStimsLocations;
                td.DistractorStimsLocations = DistractorStimsLocations;
                td.RandomizedLocations = RandomizedLocations;
                td.SliderGain = SliderGain;
                td.SliderLoss = SliderLoss;
                td.SliderInitial = SliderInitial;
                td.BlockEndType = BlockEndType;
                td.BlockEndThreshold = BlockEndThreshold;
                td.BlockEndWindow = BlockEndWindow;
                td.NumPulses = NumPulses;
                td.PulseSize = PulseSize;
                td.LeaveFeedbackOn = LeaveFeedbackOn;
                td.ErrorThreshold = ErrorThreshold;
                td.MaxTrials = MaxTrials;
                TrialDefs.Add(td);
            }
        }
    }

    public class WhatWhenWhere_TrialDef : TrialDef
    {
        // Stimuli Selection Variables
        public int[] CorrectObjectTouchOrder;
        public int ErrorThreshold;
        public int[] SearchStimsIndices;
        public Vector3[] SearchStimsLocations;
        public int[] DistractorStimsIndices;
        public Vector3[] DistractorStimsLocations;
        
        // Configuration of Stimuli
        public bool RandomizedLocations;
        public bool LeaveFeedbackOn;

        // Slider Variables
        public int[] SliderGain;
        public int[] SliderLoss;
        public int SliderInitial;
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
