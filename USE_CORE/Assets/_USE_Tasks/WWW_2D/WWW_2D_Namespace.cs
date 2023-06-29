using System.Collections.Generic;
using UnityEngine;
using USE_Def_Namespace;
using USE_StimulusManagement;

namespace WWW_2D_Namespace
{
    public class WWW_2D_BlockDef : BlockDef
    {
        public string BlockName;
        public string ContextName;
        public int[] CorrectObjectTouchOrder;
        public int[] nRepetitionsMinMax;
        public int[] SearchStimsIndices;
        public int[] DistractorStimsIndices;
        public Vector3[] SearchStims_LocalPosition;
        public Vector3[] DistractorStims_LocalPosition;
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
        public int ErrorThreshold;


        public override void GenerateTrialDefsFromBlockDef()
        {
            //pick # of trials from minmax
            int num = RandomNumGenerator.Next(nRepetitionsMinMax[0], nRepetitionsMinMax[1]);
            TrialDefs = new List<WWW_2D_TrialDef>().ConvertAll(x => (TrialDef)x);
            for (int iTrial = 0; iTrial < num; iTrial++)
            {
                WWW_2D_TrialDef td = new WWW_2D_TrialDef();
                td.BlockName = BlockName;
                td.ContextName = ContextName;
                td.CorrectObjectTouchOrder = CorrectObjectTouchOrder;
                td.SearchStimsIndices = SearchStimsIndices;
                td.DistractorStimsIndices = DistractorStimsIndices;
                td.SearchStimsLocations = SearchStims_LocalPosition;
                td.DistractorStimsLocations = DistractorStims_LocalPosition;
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
                TrialDefs.Add(td);
            }
        }
    }

    public class WWW_2D_TrialDef : TrialDef
    {
        public string BlockName;
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
        public int ErrorThreshold;

        public int MaxTrials;
    }

    public class WWW_2D_StimDef : StimDef
    {
        //relates to variables to evaluate stimuli
        public bool IsCurrentTarget;
        public bool IsDistractor;
    }

    public class WWW_2D_TaskDef : TaskDef
    {
        // string ContextExternalFilePath;
        Vector3 ButtonPosition;
        Vector3 ButtonScale;
        Vector3 FBSquarePosition;
        Vector3 FBSquareScale;
        Vector3 ButtonColor;
        string ButtonText;
        string ContextExternalFilePath;
        bool StimFacingCamera;
        string ShadowType;
        bool NeutralITI;
    }
    //Any other custom classes useful for the functioning of the task could be included in this namespace.
}
