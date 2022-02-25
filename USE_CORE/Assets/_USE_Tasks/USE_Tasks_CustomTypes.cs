using System;
using System.Collections.Generic;
using StimHandling_Namespace;
using WWW_Namespace;
using ContinuousRecognition_Namespace;
using test_Namespace;
using WorkingMemory_Namespace;
using VisualSearch_Namespace;
// using EC_Namespace;
//#END OF USING DIRECTIVES - DO NOT DELETE THIS LINE#


static public class USE_Tasks_CustomTypes
{
    static public Dictionary<string, TaskTypes> CustomTaskDictionary = new Dictionary<string, TaskTypes>()
    {
        {
            "StimHandling", new TaskTypes
            {
                TaskLevelType = typeof(StimHandling_TaskLevel),
                TrialLevelType = typeof(StimHandling_TrialLevel),
                TaskDefType = typeof(StimHandling_TaskDef),
                BlockDefType = typeof(StimHandling_BlockDef),
                TrialDefType = typeof(StimHandling_TrialDef),
                StimDefType = typeof(StimHandling_StimDef)
            }
        }
        ,{
            "WWW", new TaskTypes
            {
                TaskLevelType = typeof(WWW_TaskLevel),
                TrialLevelType = typeof(WWW_TrialLevel),
                TaskDefType = typeof(WWW_TaskDef),
                BlockDefType = typeof(WWW_BlockDef),
                TrialDefType = typeof(WWW_TrialDef),
                StimDefType = typeof(WWW_StimDef)
            }
        }
        ,{
            "ContinuousRecognition", new TaskTypes
            {
                TaskLevelType = typeof(ContinuousRecognition_TaskLevel),
                TrialLevelType = typeof(ContinuousRecognition_TrialLevel),
                TaskDefType = typeof(ContinuousRecognition_TaskDef),
                BlockDefType = typeof(ContinuousRecognition_BlockDef),
                TrialDefType = typeof(ContinuousRecognition_TrialDef),
                StimDefType = typeof(ContinuousRecognition_StimDef)
            }
        }
        ,{
            "test", new TaskTypes
            {
                TaskLevelType = typeof(test_TaskLevel),
                TrialLevelType = typeof(test_TrialLevel),
                TaskDefType = typeof(test_TaskDef),
                BlockDefType = typeof(test_BlockDef),
                TrialDefType = typeof(test_TrialDef),
                StimDefType = typeof(test_StimDef)
            }
        }
        ,{
            "WorkingMemory", new TaskTypes
            {
                TaskLevelType = typeof(WorkingMemory_TaskLevel),
                TrialLevelType = typeof(WorkingMemory_TrialLevel),
                TaskDefType = typeof(WorkingMemory_TaskDef),
                BlockDefType = typeof(WorkingMemory_BlockDef),
                TrialDefType = typeof(WorkingMemory_TrialDef),
                StimDefType = typeof(WorkingMemory_StimDef)
            }
        }
        ,{
            "VisualSearch", new TaskTypes
            {
                TaskLevelType = typeof(VisualSearch_TaskLevel),
                TrialLevelType = typeof(VisualSearch_TrialLevel),
                TaskDefType = typeof(VisualSearch_TaskDef),
                BlockDefType = typeof(VisualSearch_BlockDef),
                TrialDefType = typeof(VisualSearch_TrialDef),
                StimDefType = typeof(VisualSearch_StimDef)
            }
        }

        
//#END OF CUSTOMTASKTYPES DICTIONARY - DO NOT DELETE THIS LINE#
    };

    public class TaskTypes
    {
        public Type TaskLevelType;
        public Type TrialLevelType;
        public Type TaskDefType;
        public Type BlockDefType;
        public Type TrialDefType;
        public Type StimDefType;
    }
}
