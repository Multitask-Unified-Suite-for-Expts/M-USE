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



using UnityEngine;
using USE_StimulusManagement;
using USE_Def_Namespace;

namespace TemporalOrderJudgement_Namespace
{
    public class TemporalOrderJudgement_TaskDef : TaskDef
    {
    }

    public class TemporalOrderJudgement_BlockDef : BlockDef
    {
        //Inherited and Used:
        //public string ContextName;
        //public int BlockCount;
    }

    public class TemporalOrderJudgement_TrialDef : TrialDef
    {
        public string VisualStimIdentity;
        public string AudioStimIdentity;
        public string CrossIdentity;

        public float CrossDuration;
        public float PostDisplayDelayDuration; //basically ends up being amount of time after visual stim is displayed
        public float ResponseDuration;
        public float FeedbackDuration;

        public float VisualStimOnsetDelay;
        public float AudioStimOnsetDelay;

        public Vector3 VisualStimSize;
        public Vector3 CrossSize;

        public Vector3 VisualStimPosition;
        public Vector3 CrossPosition;

        public bool VisualStimRandomColor;
        public bool CrossRandomColor;
    }

    public class TemporalOrderJudgement_StimDef : StimDef
    {
    }
}