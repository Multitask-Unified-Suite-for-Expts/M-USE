using UnityEngine;
using USE_Def_Namespace;
using USE_StimulusManagement;

namespace StimHandling_Namespace
{
    public class StimHandling_TaskDef : TaskDef
    {
    
    }

    public class StimHandling_BlockDef : BlockDef
    {
    
    }

    public class StimHandling_TrialDef : TrialDef
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

    public class StimHandling_StimDef : StimDef
    {
    
    }
}