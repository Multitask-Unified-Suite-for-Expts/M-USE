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
        public string BlockName;
        public int[] MinMaxNumTrials;
        public bool ShowNegFb;
        public int PulseSize;
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
        public int PositionX;
        public int PositionX_Min;
        public int PositionX_Max;
        public int PositionY;
        public int PositionY_Min;
        public int PositionY_Max;
        public bool RewardTouch;
        public int NumTouchPulses;
        public bool RewardRelease;
        public int NumReleasePulses;
        public bool RandomObjectSize;
        public bool RandomObjectPosition;
        public int TimeToAutoEndTrialSec;


        public override void GenerateTrialDefsFromBlockDef()
        {
            TrialDefs = new List<THR_TrialDef>().ConvertAll(x => (TrialDef)x);

            for (int i = 0; i < MinMaxNumTrials[1]; i++)
            {
                THR_TrialDef trial = new THR_TrialDef();

                trial.TrialName = BlockName;
                trial.MinNumTrials = MinMaxNumTrials[0];
                trial.MaxNumTrials = MinMaxNumTrials[1];
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
        public int PulseSize;
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
        public int PositionX;
        public int PositionX_Min;
        public int PositionX_Max;
        public int PositionY;
        public int PositionY_Min;
        public int PositionY_Max;
        public bool RewardTouch;
        public int NumTouchPulses;
        public bool RewardRelease;
        public int NumReleasePulses;
        public bool RandomObjectSize;
        public bool RandomObjectPosition;
        public int TimeToAutoEndTrialSec;
        public float TimeoutDuration;
    }


    public class THR_StimDef : StimDef
    {
        
    }
}

