using USE_ExperimentTemplate;
using UnityEngine;
using USE_StimulusManagement;

namespace WhatWhenWhere_Namespace
{
    public class WhatWhenWhere_BlockDef : BlockDef
    {
        public string TrialID;
        public string ContextName;
        public int[] CorrectObjectTouchOrder;
        public int[] nRepetitionsMinMax;
        public int[] SearchStimsIndices;
        public int[] DistractorStimsIndices;
        public Vector3[] SearchStimsLocations;
        public Vector3[] DistractorStimsLocations;
        public int[] SliderGain;
        public int[] SliderLoss;
        public int SliderInitial;
        public bool RandomizedLocations;
        public string BlockEndType;
        public float BlockEndThreshold;
        public int BlockEndWindow;
        public int NumPulses;
        public int PulseSize;
        public bool LeaveFeedbackOn;


        public override void GenerateTrialDefsFromBlockDef()
        {
            //pick # of trials from minmax
            System.Random rnd = new System.Random();
            int num = rnd.Next(nRepetitionsMinMax[0], nRepetitionsMinMax[1]);
            
            TrialDefs = new TrialDef[num];//actual correct # 
            for (int iTrial = 0; iTrial< TrialDefs.Length; iTrial++)
            {
                WhatWhenWhere_TrialDef td = new WhatWhenWhere_TrialDef();
                td.TrialID = TrialID;
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
                TrialDefs[iTrial] = td;
            }
        }
    }

    public class WhatWhenWhere_TrialDef : TrialDef
    {
        public string TrialID;
        public string ContextName;
        //ObjectNums refers to items in a list of objects to be loaded from resources folder
        
        //CorrectObjectOrder is an array of same length as ObjectNums, refers to elements in that array (e.g. {2 3 1 4} refers to 2nd object specified in ObjectNums
        public int[] CorrectObjectTouchOrder;
        public int[] nRepetitionsMinMax;
        public int[] SearchStimsIndices;
        public int[] DistractorStimsIndices;
        public Vector3[] SearchStimsLocations;
        public Vector3[] DistractorStimsLocations;
        public bool RandomizedLocations;
        public int[] SliderGain;
        public int[] SliderLoss;
        public int SliderInitial;
        public string BlockEndType;
        public float BlockEndThreshold;
        public int BlockEndWindow;
        public int NumPulses;
        public int PulseSize;
        public bool LeaveFeedbackOn;
    }

    public class WhatWhenWhere_StimDef : StimDef
    {
        //relates to varaibles to evaluate stimuli
        public bool IsCurrentTarget;
        public bool IsDistractor;
    }

    public class WhatWhenWhere_TaskDef : TaskDef
    {
        string ContextExternalFilePath;
        Vector3 ButtonPosition;
        Vector3 ButtonScale;
    }
    //Any other custom classes useful for the functioning of the task could be included in this namespace.
}
