﻿using USE_ExperimentTemplate;
using UnityEngine;

namespace StimHandling_Namespace
{
    //There is no need for the EffortControl_TaskDef or EffortControl_BlockDef 
    //classes to be defined here, as they add nothing to their parent classes.
    //However they are left here to make this script an easier template to copy.
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

    //Any other custom classes useful for the functioning of the task could be included in this namespace.
}