using USE_ExperimentTemplate;
using UnityEngine;

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
        public string BlockName;
        public int ContextNum;
        public string ContextName;
    }

    public class WhatWhenWhere_TrialDef : TrialDef
    {
        public string TrialID;
        public int TrialNum;
        public int Context;
        public string ContextName;
        //ObjectNums refers to items in a list of objects to be loaded from resources folder
        public int[] ObjectNums;
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
    }

    //Any other custom classes useful for the functioning of the task could be included in this namespace.
}
