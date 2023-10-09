/*
MIT License

Copyright (c) 2023 Multitask - Unified - Suite -for-Expts

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files(the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/



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
using GazeCalibration_Namespace;
using FeatureUncertaintyWM_Namespace;
using AntiSaccade_Namespace;
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
        ,{
            "GazeCalibration", new TaskTypes
            {
                TaskLevelType = typeof(GazeCalibration_TaskLevel),
                TrialLevelType = typeof(GazeCalibration_TrialLevel),
                TaskDefType = typeof(GazeCalibration_TaskDef),
                BlockDefType = typeof(GazeCalibration_BlockDef),
                TrialDefType = typeof(GazeCalibration_TrialDef),
                StimDefType = typeof(GazeCalibration_StimDef)
            }
        }

        ,{
            "FeatureUncertaintyWM", new TaskTypes
            {
                TaskLevelType = typeof(FeatureUncertaintyWM_TaskLevel),
                TrialLevelType = typeof(FeatureUncertaintyWM_TrialLevel),
                TaskDefType = typeof(FeatureUncertaintyWM_TaskDef),
                BlockDefType = typeof(FeatureUncertaintyWM_BlockDef),
                TrialDefType = typeof(FeatureUncertaintyWM_TrialDef),
                StimDefType = typeof(FeatureUncertaintyWM_StimDef)
            }
        }

        ,{
            "AntiSaccade", new TaskTypes
            {
                TaskLevelType = typeof(AntiSaccade_TaskLevel),
                TrialLevelType = typeof(AntiSaccade_TrialLevel),
                TaskDefType = typeof(AntiSaccade_TaskDef),
                BlockDefType = typeof(AntiSaccade_BlockDef),
                TrialDefType = typeof(AntiSaccade_TrialDef),
                StimDefType = typeof(AntiSaccade_StimDef)
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
