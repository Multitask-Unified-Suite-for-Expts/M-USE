using System;
using System.Collections.Generic;
using StimHandlingNew_Namespace;
//#END OF USING DIRECTIVES - DO NOT DELETE THIS LINE#


static public class USE_Tasks_CustomTypes
{
    static public Dictionary<string, TaskTypes> CustomTaskDictionary = new Dictionary<string, TaskTypes>()
    {
        {
            "StimHandlingNew", new TaskTypes
            {
                TaskLevelType = typeof(StimHandlingNew_TaskLevel),
                TrialLevelType = typeof(StimHandlingNew_TrialLevel),
                TaskDefType = typeof(StimHandlingNew_TaskDef),
                BlockDefType = typeof(StimHandlingNew_BlockDef),
                TrialDefType = typeof(StimHandlingNew_TrialDef),
                StimDefType = typeof(StimHandlingNew_StimDef)
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