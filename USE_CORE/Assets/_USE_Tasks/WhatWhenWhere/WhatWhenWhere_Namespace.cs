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
        public int PlayFeedbackSound_Correct;
        public int PlayFeedbackSound_Error;
        public float MinTouchDuration;
        public float MaxTouchDuration;
        public int[] SearchStimsIndices;
        public int[] DistractorStimsIndices;
        public Vector3[] SearchStimsLocations;
        public Vector3[] DistractorStimsLocations;
        public string ContextExternalFilePath;
        public int NumSteps;
        public bool RandomizedLocations;


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
                td.PlayFeedbackSound_Correct = PlayFeedbackSound_Correct;
                td.PlayFeedbackSound_Error = PlayFeedbackSound_Error;
                td.MinTouchDuration = MinTouchDuration;
                td.MaxTouchDuration = MaxTouchDuration;
                td.SearchStimsIndices = SearchStimsIndices;
                td.DistractorStimsIndices = DistractorStimsIndices;
                td.SearchStimsLocations = SearchStimsLocations;
                td.DistractorStimsLocations = DistractorStimsLocations;
                td.ContextExternalFilePath = ContextExternalFilePath;
                td.NumSteps = NumSteps;
                td.RandomizedLocations = RandomizedLocations;
                TrialDefs[iTrial] = td;
            }
        }
    }

    public class WhatWhenWhere_TrialDef : TrialDef
    {
        public string TrialID;
        public int TrialNum;
        public string ContextName;
        //ObjectNums refers to items in a list of objects to be loaded from resources folder
        
        //CorrectObjectOrder is an array of same length as ObjectNums, refers to elements in that array (e.g. {2 3 1 4} refers to 2nd object specified in ObjectNums
        public int[] CorrectObjectTouchOrder;
        public int[] nRepetitionsMinMax;
        public int PlayFeedbackSound_Correct;
        public int PlayFeedbackSound_Error;
        public Vector3[][] ObjectRotations;
        public float MinTouchDuration;
        public float MaxTouchDuration;
        public int[] SearchStimsIndices;
        public int[] DistractorStimsIndices;
        public Vector3[] SearchStimsLocations;
        public Vector3[] DistractorStimsLocations;
        public string ContextExternalFilePath;
        public int NumSteps;
        public bool RandomizedLocations;
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
    }
    //Any other custom classes useful for the functioning of the task could be included in this namespace.
}
