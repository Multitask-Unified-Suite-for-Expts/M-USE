using System;
using System.Collections.Generic;
using UnityEngine;
using USE_StimulusManagement;
using USE_Def_Namespace;


namespace THRQ_Namespace
{
    public class THRQ_TaskDef : TaskDef
    {
    }

    public class THRQ_BlockDef : BlockDef
    {
        public int PerfWindowEndTrials;
        public float PerfThresholdEndTrials;
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
            TrialDefs = new List<THRQ_TrialDef>().ConvertAll(x => (TrialDef)x);

            // assign min and max for the block
            MinTrials = MinMaxTrials[0];
            MaxTrials = MinMaxTrials[1];

            for (int i = 0; i < MinMaxTrials[1]; i++)
            {
                THRQ_TrialDef trial = new THRQ_TrialDef();

                trial.TrialName = BlockName;
                trial.MinNumTrials = MinMaxTrials[0];
                trial.MaxNumTrials = MinMaxTrials[1];
                trial.PulseSize = PulseSize;
                trial.PerfWindowEndTrials = PerfWindowEndTrials;
                trial.PerfThresholdEndTrials = PerfThresholdEndTrials;
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

    public class THRQ_TrialDef : TrialDef
    {
        public string TrialName;
        public int MinNumTrials;
        public int MaxNumTrials;
        public int PerfWindowEndTrials;
        public float PerfThresholdEndTrials;
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

    public class THRQ_StimDef : StimDef
    {
    }
}