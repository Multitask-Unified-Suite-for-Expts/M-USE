using System;
using System.Collections.Generic;
using StimHandling_Namespace;
using WWW_Namespace;
using ContinuousRecognition_Namespace;
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