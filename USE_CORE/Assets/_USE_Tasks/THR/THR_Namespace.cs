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


using System.Collections.Generic;
using USE_StimulusManagement;
using USE_Def_Namespace;



namespace THR_Namespace
{
    public class THR_TaskDef : TaskDef
    {
        public bool StartWithSelectObjectState;
    }

    public class THR_BlockDef : BlockDef
    {
        public bool ShowNegFb;
        public int PerfWindowEndTrials;
        public float PerfThresholdEndTrials;
        public float AvoidObjectDuration;
        public float SelectObjectDuration;
        public float TimeoutDuration;
        public float ItiDuration;
        public float MinTouchDuration;
        public float MaxTouchDuration;
        public float TouchToRewardDelay;
        public float ReleaseToRewardDelay;
        public float ObjectSize;
        public float ObjectSizeMin;
        public float ObjectSizeMax;
        public float PositionX;
        public int PositionX_Min;
        public int PositionX_Max;
        public float PositionY;
        public int PositionY_Min;
        public int PositionY_Max;
        public bool RewardTouch;
        public int NumTouchPulses;
        public bool RewardRelease;
        public int NumReleasePulses;
        public bool RandomObjectSize;
        public bool RandomObjectPosition;
        public float TimeToAutoEndTrialSec;


        public override void GenerateTrialDefsFromBlockDef()
        {
            TrialDefs = new List<THR_TrialDef>().ConvertAll(x => (TrialDef)x);

            // assign min and max for the block
            MinTrials = MinMaxTrials[0];
            MaxTrials = MinMaxTrials[1];

            for (int i = 0; i < MinMaxTrials[1]; i++)
            {
                THR_TrialDef trial = new THR_TrialDef();

                trial.TrialName = BlockName;
                trial.MinNumTrials = MinMaxTrials[0];
                trial.MaxNumTrials = MinMaxTrials[1];
                trial.ShowNegFb = ShowNegFb;
                trial.PulseSize = PulseSize;
                trial.PerfWindowEndTrials = PerfWindowEndTrials;
                trial.PerfThresholdEndTrials = PerfThresholdEndTrials;
                trial.AvoidObjectDuration = AvoidObjectDuration;
                trial.SelectObjectDuration = SelectObjectDuration;
                trial.ItiDuration = ItiDuration;
                trial.MinTouchDuration = MinTouchDuration;
                trial.MaxTouchDuration = MaxTouchDuration;
                trial.TouchToRewardDelay = TouchToRewardDelay;
                trial.ReleaseToRewardDelay = ReleaseToRewardDelay;
                trial.ObjectSize = ObjectSize;
                trial.ObjectSizeMin = ObjectSizeMin;
                trial.ObjectSizeMax = ObjectSizeMax;
                trial.PositionX = PositionX;
                trial.PositionX_Min = PositionX_Min;
                trial.PositionX_Max = PositionX_Max;
                trial.PositionY = PositionY;
                trial.PositionY_Min = PositionY_Min;
                trial.PositionY_Max = PositionY_Max;
                trial.RewardTouch = RewardTouch;
                trial.NumTouchPulses = NumTouchPulses;
                trial.RewardRelease = RewardRelease;
                trial.NumReleasePulses = NumReleasePulses;
                trial.RandomObjectSize = RandomObjectSize;
                trial.RandomObjectPosition = RandomObjectPosition;
                trial.TimeToAutoEndTrialSec = TimeToAutoEndTrialSec;
                trial.TimeoutDuration = TimeoutDuration;

                TrialDefs.Add(trial);
            }
        }
    }


    public class THR_TrialDef : TrialDef
    {
        public string TrialName;
        public int MinNumTrials;
        public int MaxNumTrials;
        public bool ShowNegFb;
        public int PerfWindowEndTrials;
        public float PerfThresholdEndTrials;
        public float AvoidObjectDuration;
        public float SelectObjectDuration;
        public float ItiDuration;
        public float MinTouchDuration;
        public float MaxTouchDuration;
        public float TouchToRewardDelay;
        public float ReleaseToRewardDelay;
        public float ObjectSize;
        public float ObjectSizeMin;
        public float ObjectSizeMax;
        public float PositionX;
        public int PositionX_Min;
        public int PositionX_Max;
        public float PositionY;
        public int PositionY_Min;
        public int PositionY_Max;
        public bool RewardTouch;
        public int NumTouchPulses;
        public bool RewardRelease;
        public int NumReleasePulses;
        public bool RandomObjectSize;
        public bool RandomObjectPosition;
        public float TimeToAutoEndTrialSec;
        public float TimeoutDuration;
    }


    public class THR_StimDef : StimDef
    {
        
    }
}

