using UnityEngine;
using USE_ExperimentTemplate;
using USE_StimulusManagement;

namespace WWW_Namespace
{
    public class WWW_TaskDef : TaskDef
    {
    
    }

    public class WWW_BlockDef : BlockDef
    {
    }

    public class WWW_TrialDef : TrialDef
    {
    
        public string TrialName;
        public int TrialCode;
        public int Context;
        public string ContextName;
        //ObjectNums refers to items in a list of objects to be loaded from resources folder
        public int[] ObjectNums;
        //CorrectObjectOrder is an array of same length as ObjectNums, refers to elements in that array (e.g. {2 3 1 4} refers to 2nd object specified in ObjectNums
        public int[] CorrectObjectTouchOrder;
        //public Vector3[][] ObjectLocations;
        public int[] ObjectXLocations;
        public int[] ObjectYLocations;
        public Vector3[][] ObjectRotations;
        public int TokensAddedPerCorrectTouch;
        public int TokensSubtractedPerIncorrectTouch;
        public Color SphereColor; 
    }

    public class WWW_StimDef : StimDef
    {
    
    }
}