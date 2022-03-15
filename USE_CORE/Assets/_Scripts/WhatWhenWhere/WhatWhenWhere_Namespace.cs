using USE_ExperimentTemplate;
using UnityEngine;
using USE_StimulusManagement;

namespace WhatWhenWhere_Namespace
{
    //There is no need for the EffortControl_TaskDef or EffortControl_BlockDef 
    //classes to be defined here, as they add nothing to their parent classes.
    //However they are left here to make this script an easier template to copy.
    public class WhatWhenWhere_TaskDef : TaskDef
    {

    }

    public class WhatWhenWhere_BlockDef : BlockDef
    {
        // public int BlockCount;
        // public TrialDef[] TrialDefs;
        public int Context;
        public int[] CorrectObjectTouchOrder;
        public int[] nRepetitionsMinMax;
        public int PlayFeedbackSound_Correct;
        public int PlayFeedbackSound_Error;
        public float MinTouchDuration;
        public float MaxTouchDuration;
        public int[] SearchStimsIndices;
        public Vector3[] SearchStimsLocations;


        public override void GenerateTrialDefsFromBlockDef()
        {
            //pick # of trials from minmax
            System.Random rnd = new System.Random();
            int num = rnd.Next(nRepetitionsMinMax[0], nRepetitionsMinMax[1]);
            TrialDefs = new TrialDef[num];//actual correct # 
            
            for (int iTrial = 0; iTrial< TrialDefs.Length; iTrial++)
            {
                WhatWhenWhere_TrialDef td = (WhatWhenWhere_TrialDef)TrialDefs[iTrial];
                td.Context = Context;
                td.CorrectObjectTouchOrder = CorrectObjectTouchOrder;
                td.PlayFeedbackSound_Correct = PlayFeedbackSound_Correct;
                td.PlayFeedbackSound_Error = PlayFeedbackSound_Error;
                td.MinTouchDuration = MinTouchDuration;
                td.MaxTouchDuration = MaxTouchDuration;
                td.SearchStimsIndices = SearchStimsIndices;
                td.SearchStimsLocations = SearchStimsLocations;
            }
        }
    }

    public class WhatWhenWhere_TrialDef : TrialDef
    {
        public string TrialID;
        public int TrialNum;
        public int Context;
        public string ContextName;
        //ObjectNums refers to items in a list of objects to be loaded from resources folder
        public int[] CorrectO;
        //CorrectObjectOrder is an array of same length as ObjectNums, refers to elements in that array (e.g. {2 3 1 4} refers to 2nd object specified in ObjectNums
        public int[] CorrectObjectTouchOrder;
        //public Vector3[][] ObjectLocations;
        public float[] ObjectXLocations;
        public float[] ObjectYLocations;
        public int[] nRepetitionsMinMax;
        public int PlayFeedbackSound_Correct;
        public int PlayFeedbackSound_Error;
        public Vector3[][] ObjectRotations;
        public int TokensAddedPerCorrectTouch;
        public int TokensSubtractedPerIncorrectTouch;
        public Color SphereColor;
        public float MinTouchDuration;
        public float MaxTouchDuration;

        public int[] SearchStimsIndices;
        public Vector3[] SearchStimsLocations;
    }

    public class WhatWhenWhere_StimDef : StimDef
    {
        public bool IsCurrentTarget;
    }
    //Any other custom classes useful for the functioning of the task could be included in this namespace.
}
