using System.Collections.Generic;
using USE_ExperimentTemplate;
using ContinuousRecognition_Namespace;
using UnityEngine;
using USE_StimulusManagement;

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
        StimGroup display;
        BlockFeedback.AddInitializationMethod(() =>
        {
            List<int> chosen = TrialLevel.GetCurrentTrialDef<ContinuousRecognition_TrialDef>().PreviouslyChosenStimuli;
            Vector3[] Grid = TrialLevel.GetCurrentTrialDef<ContinuousRecognition_TrialDef>().Grid;
            Vector3[] locs = new Vector3[chosen.Count];
            for (int i = 0; i < chosen.Count; i++)
            {
                locs[i] = Grid[i];
            }
            display = new StimGroup("display", ExternalStims, chosen);
            display.SetLocations(locs);
            display.ToggleVisibility(true);
        });
    
    }


}