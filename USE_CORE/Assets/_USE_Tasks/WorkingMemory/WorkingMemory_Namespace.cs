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
using USE_Def_Namespace;
using USE_StimulusManagement;
using USE_ExperimentTemplate_Classes;

namespace WorkingMemory_Namespace
{
    public class WorkingMemory_TaskDef : TaskDef
    {
    }

    public class WorkingMemory_BlockDef : BlockDef
    {
        public Vector3 SampleStimLocation;
        public Vector3[] SearchStimLocations;
        public int[] SearchStimIndices;
        public Vector3[] PostSampleDistractorLocations;
        public int[] PostSampleDistractorStimIndices;
        public int[] SearchStimTokenReward;
        public Reward[][] ProbabilisticSearchStimTokenReward;


        public float DisplaySampleDuration;
        public float PostSampleDelayDuration;
        public float DisplayPostSampleDistractorsDuration;
        public float PreTargetDelayDuration;

        public override void AddToTrialDefsFromBlockDef()
        {
            for (int iTrial = 0; iTrial < TrialDefs.Count; iTrial++)
            {
                WorkingMemory_TrialDef td = (WorkingMemory_TrialDef)TrialDefs[iTrial];
                td.BlockName = BlockName;
                td.NumInitialTokens = NumInitialTokens;
                td.NumPulses = NumPulses;
                td.TokenBarCapacity = TokenBarCapacity;
                td.PulseSize = PulseSize;
                td.BlockEndType = BlockEndType;
                td.BlockEndThreshold = BlockEndThreshold;
                td.BlockEndWindow = BlockEndWindow;
                td.ContextName = ContextName;
                TrialDefs[iTrial] = td;
            }
        }
    }

    public class WorkingMemory_TrialDef : TrialDef
    {
        public Vector3 SampleStimLocation;
        public Vector3[] SearchStimLocations;
        public int[] SearchStimIndices;
        public Vector3[] PostSampleDistractorStimLocations;
        public int[] PostSampleDistractorStimIndices;
        public int[] SearchStimTokenReward;
        public Reward[][] ProbabilisticSearchStimTokenReward;

        public float DisplaySampleDuration;
        public float PostSampleDelayDuration;
        public float DisplayPostSampleDistractorsDuration;
        public float PreTargetDelayDuration;
    }

    public class WorkingMemory_StimDef : StimDef
    {
        public bool IsTarget;
    }
}