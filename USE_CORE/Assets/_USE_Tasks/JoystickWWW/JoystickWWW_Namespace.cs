using System.Collections.Generic;
using UnityEngine;
using USE_Def_Namespace;
using USE_StimulusManagement;

namespace JoystickWWW_Namespace
{
    public class JoystickWWW_BlockDef : BlockDef
    {
        
        // Stimuli Selection Variables
        public int[] CorrectObjectTouchOrder;
        public int[] nRepetitionsMinMax;
        public int[] SearchStimsIndices;
        public int[] DistractorStimsIndices;
        public Vector3[] SearchStimsLocations;
        public Vector3[] DistractorStimsLocations;
        
        // Configuration of Stimuli
        public bool LeaveFeedbackOn;
        public int ErrorThreshold;
        
        public int[] SliderGain;
        public int[] SliderLoss;
        
        // Slider Variables
        public int SliderInitial;
        public bool RandomizedLocations;
        
        public bool GuidedSequenceLearning;
        public int MaxCorrectTrials;
        
        /*public string BlockEndType;
        public float BlockEndThreshold;
        public int BlockEndWindow;
        public int NumPulses;
        public int PulseSize;*/
        


        public override void GenerateTrialDefsFromBlockDef()
        {
            //pick # of trials from minmax
            int num = RandomNumGenerator.Next(nRepetitionsMinMax[0], nRepetitionsMinMax[1]);
            TrialDefs = new List<JoystickWWW_TrialDef>().ConvertAll(x => (TrialDef)x);
            for (int iTrial = 0; iTrial< num; iTrial++)
            {
                JoystickWWW_TrialDef td = new JoystickWWW_TrialDef();
                td.BlockName = BlockName;
                td.ContextName = ContextName;
                td.CorrectObjectTouchOrder = CorrectObjectTouchOrder;
                td.SearchStimIndices = SearchStimsIndices;
                td.DistractorStimIndices = DistractorStimsIndices;
                td.SearchStimLocations = SearchStimsLocations;
                td.DistractorStimLocations = DistractorStimsLocations;
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
                td.MaxTrials = num;
                td.MaxCorrectTrials = MaxCorrectTrials;
                td.GuidedSequenceLearning = GuidedSequenceLearning;
                TrialDefs.Add(td);
            }
        }
    }

    public class JoystickWWW_TrialDef : TrialDef
    {
        // Stimuli Selection Variables
        public int[] CorrectObjectTouchOrder;
        public int ErrorThreshold;
        public int[] SearchStimIndices;
        public int[] DistractorStimIndices;
        public Vector3[] SearchStimLocations;
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

    public class JoystickWWW_StimDef : StimDef
    {
        //relates to variables to evaluate stimuli
        public bool IsCurrentTarget;
        public bool IsDistractor;
    }

    public class JoystickWWW_TaskDef : TaskDef
    {
    }
    //Any other custom classes useful for the functioning of the task could be included in this namespace.
}
