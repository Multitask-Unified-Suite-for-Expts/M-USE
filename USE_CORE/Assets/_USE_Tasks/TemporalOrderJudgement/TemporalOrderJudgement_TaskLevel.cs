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



using USE_Settings;
using USE_StimulusManagement;
using USE_ExperimentTemplate_Task;
using TemporalOrderJudgement_Namespace;
using UnityEngine;


public class TemporalOrderJudgement_TaskLevel : ControlLevel_Task_Template
{
    TemporalOrderJudgement_BlockDef CurrentBlock => GetCurrentBlockDef<TemporalOrderJudgement_BlockDef>();
    TemporalOrderJudgement_TrialLevel trialLevel;


    public override void DefineControlLevel()
    {
        trialLevel = (TemporalOrderJudgement_TrialLevel)TrialLevel;

        if (CurrentBlock == null)
            Debug.LogError("BLOCK IS NULL!");

        SetSkyBox(CurrentBlock.ContextName);
    }


}