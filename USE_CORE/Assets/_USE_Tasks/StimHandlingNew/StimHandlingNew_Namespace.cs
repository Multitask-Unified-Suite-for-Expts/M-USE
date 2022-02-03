using UnityEngine;
using USE_ExperimentTemplate;
using USE_StimulusManagement;

namespace StimHandlingNew_Namespace
{
    public class StimHandlingNew_TaskDef : TaskDef
    {
    
    }

    public class StimHandlingNew_BlockDef : BlockDef
    {
    
    }

    public class StimHandlingNew_TrialDef : TrialDef
    {
        public string TrialName;
        public int TrialCode;
        public int Context;
        public string ContextName;
        //ObjectNums refers to items in a list of objects to be loaded from resources folder
        public int[] GroupAIndices;
        public int[] GroupBIndices;
        public int[] GroupCIndices;
        public Vector3[] GroupALocations;
        public Vector3[] GroupBLocations;
        public Vector3[] GroupCLocations;
    }

    public class StimHandlingNew_StimDef : StimDef
    {
    
    }
}