using System.Security.Cryptography.X509Certificates;
using USE_ExperimentTemplate;
using ContinuousRecognition_Namespace;
using UnityEngine;
using UnityEngine.UI;
public class ContinuousRecognition_TaskLevel : ControlLevel_Task_Template
{
    public override void SpecifyTypes()
    {
        //note that since EffortControl_TaskDef and EffortControl_BlockDef do not add any fields or methods to their parent types, 
        //they do not actually need to be specified here, but they are included to make this script more useful for later copying.
        TaskLevelType = typeof(ContinuousRecognition_TaskLevel);
        TrialLevelType = typeof(ContinuousRecognition_TrialLevel);
        TaskDefType = typeof(ContinuousRecognition_TaskDef);
        BlockDefType = typeof(ContinuousRecognition_BlockDef);
        TrialDefType = typeof(ContinuousRecognition_TrialDef);
        StimDefType = typeof(ContinuousRecognition_StimDef);
    }
    public override void DefineControlLevel()
    {
        BlockFeedback.AddInitializationMethod(() =>
        {
            
        });
        

    }
    


}