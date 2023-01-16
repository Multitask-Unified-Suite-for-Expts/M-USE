using System;
using System.Collections.Generic;
using StimHandling_Namespace;
using WhatWhenWhere_Namespace;
using ContinuousRecognition_Namespace;
using WorkingMemory_Namespace;
using VisualSearch_Namespace;
using EffortControl_Namespace;
using MazeGame_Namespace;
using FlexLearning_Namespace;
using THR_Namespace;
//using FinderGame_Namespace;
using Kia_Task_Namespace;
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
            "WhatWhenWhere", new TaskTypes
            {
                TaskLevelType = typeof(WhatWhenWhere_TaskLevel),
                TrialLevelType = typeof(WhatWhenWhere_TrialLevel),
                TaskDefType = typeof(WhatWhenWhere_TaskDef),
                BlockDefType = typeof(WhatWhenWhere_BlockDef),
                TrialDefType = typeof(WhatWhenWhere_TrialDef),
                StimDefType = typeof(WhatWhenWhere_StimDef)
            }
        }
        ,{
            "EffortControl", new TaskTypes
            {
                TaskLevelType = typeof(EffortControl_TaskLevel),
                TrialLevelType = typeof(EffortControl_TrialLevel),
                TaskDefType = typeof(EffortControl_TaskDef),
                BlockDefType = typeof(EffortControl_BlockDef),
                TrialDefType = typeof(EffortControl_TrialDef),
                StimDefType = typeof(EffortControl_StimDef)
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

        
        ,{
            "MazeGame", new TaskTypes
            {
                TaskLevelType = typeof(MazeGame_TaskLevel),
                TrialLevelType = typeof(MazeGame_TrialLevel),
                TaskDefType = typeof(MazeGame_TaskDef),
                BlockDefType = typeof(MazeGame_BlockDef),
                TrialDefType = typeof(MazeGame_TrialDef),
                StimDefType = typeof(MazeGame_StimDef)
            }
        },
        {
            "FlexLearning", new TaskTypes
            {
                TaskLevelType = typeof(FlexLearning_TaskLevel),
                TrialLevelType = typeof(FlexLearning_TrialLevel),
                TaskDefType = typeof(FlexLearning_TaskDef),
                BlockDefType = typeof(FlexLearning_BlockDef),
                TrialDefType = typeof(FlexLearning_TrialDef),
                StimDefType = typeof(FlexLearning_StimDef)
            }
        }
        ,{
            "THR", new TaskTypes
            {
                TaskLevelType = typeof(THR_TaskLevel),
                TrialLevelType = typeof(THR_TrialLevel),
                TaskDefType = typeof(THR_TaskDef),
                BlockDefType = typeof(THR_BlockDef),
                TrialDefType = typeof(THR_TrialDef),
                StimDefType = typeof(THR_StimDef)
            }
        }
        //,{
        //    "FinderGame", new TaskTypes
        //    {
        //        TaskLevelType = typeof(FinderGame_TaskLevel),
        //        TrialLevelType = typeof(FinderGame_TrialLevel),
        //        TaskDefType = typeof(FinderGame_TaskDef),
        //        BlockDefType = typeof(FinderGame_BlockDef),
        //        TrialDefType = typeof(FinderGame_TrialDef),
        //        StimDefType = typeof(FinderGame_StimDef)
        //    }
        //}
        ,{
            "Kia_Task", new TaskTypes
            {
                TaskLevelType = typeof(Kia_Task_TaskLevel),
                TrialLevelType = typeof(Kia_Task_TrialLevel),
                TaskDefType = typeof(Kia_Task_TaskDef),
                BlockDefType = typeof(Kia_Task_BlockDef),
                TrialDefType = typeof(Kia_Task_TrialDef),
                StimDefType = typeof(Kia_Task_StimDef)
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
