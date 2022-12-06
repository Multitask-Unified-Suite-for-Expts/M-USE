using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using USE_StimulusManagement;
using Random = UnityEngine.Random;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Block;
using USE_ExperimentTemplate_Trial;


namespace THR_Namespace
{
    public class THR_TaskDef : TaskDef
    {
        //Already-existing fields (inherited from TaskDef)      
        //public DateTime TaskStart_DateTime;
        //public int TaskStart_Frame;
        //public float TaskStart_UnityTime;
        //public string TaskName;
        //public string ExternalStimFolderPath;
        //public string PrefabStimFolderPath;
        //public string ExternalStimExtension;
        //public List<string[]> FeatureNames;
        //public string neutralPatternedColorName;
        //public float? ExternalStimScale;
    }

    public class THR_BlockDef : BlockDef
    {
        //Already-existing fields (inherited from BlockDef)
        //public int BlockCount;
        //public TrialDef[] TrialDefs;

        public string BlockName;
        //public string ContextName;
        public int[] MinMaxNumTrials;
        public int PulseSize;
        public int PerfWindowEndTrials;
        public float PerfThresholdEndTrials;
        public float WhiteSquareDuration;
        public float BlueSquareDuration;
        public float FbDuration;
        public float ItiDuration;
        public float MinTouchDuration;
        public float MaxTouchDuration;
        public float TouchToRewardDelay;
        public float ReleaseToRewardDelay;
        public float SquareSize;
        public float SquareSizeMin;
        public float SquareSizeMax;
        public float PositionX;
        public float PositionX_Min;
        public float PositionX_Max;
        public float PositionY;
        public float PositionY_Min;
        public float PositionY_Max;
        public bool RewardTouch;
        public int NumTouchPulses;
        public bool RewardRelease;
        public int NumReleasePulses;
        public bool RandomReward;
        public bool RandomSquareSize;
        public bool RandomSquarePosition;
        public int TimeToAutoEndTrialSec;


        public override void GenerateTrialDefsFromBlockDef()
        {
            TrialDefs = new List<THR_TrialDef>().ConvertAll(x => (TrialDef)x);

            for (int i = 0; i < MinMaxNumTrials[1]; i++)
            {
                THR_TrialDef trial = new THR_TrialDef();

                //trial.ContextName = ContextName;
                trial.BlockName = BlockName;
                trial.MinNumTrials = MinMaxNumTrials[0];
                trial.MaxNumTrials = MinMaxNumTrials[1];
                trial.PulseSize = PulseSize;
                trial.PerfWindowEndTrials = PerfWindowEndTrials;
                trial.PerfThresholdEndTrials = PerfThresholdEndTrials;
                trial.WhiteSquareDuration = WhiteSquareDuration;
                trial.BlueSquareDuration = BlueSquareDuration;
                trial.FbDuration = FbDuration;
                trial.ItiDuration = ItiDuration;
                trial.MinTouchDuration = MinTouchDuration;
                trial.MaxTouchDuration = MaxTouchDuration;
                trial.TouchToRewardDelay = TouchToRewardDelay;
                trial.ReleaseToRewardDelay = ReleaseToRewardDelay;
                trial.SquareSize = SquareSize;
                trial.SquareSizeMin = SquareSizeMin;
                trial.SquareSizeMax = SquareSizeMax;
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
                trial.RandomReward = RandomReward;
                trial.RandomSquareSize = RandomSquareSize;
                trial.RandomSquarePosition = RandomSquarePosition;
                trial.TimeToAutoEndTrialSec = TimeToAutoEndTrialSec;

                TrialDefs.Add(trial);
            }
        }
    }


    public class THR_TrialDef : TrialDef
    {
        //Already-existing fields (inherited from TrialDef)
        //public int BlockCount, TrialCountInBlock, TrialCountInTask;
        //public TrialStims TrialStims;

        //public string ContextName;
        public string BlockName;
        public int MinNumTrials;
        public int MaxNumTrials;
        public int PulseSize;
        public int PerfWindowEndTrials;
        public float PerfThresholdEndTrials;
        public float WhiteSquareDuration;
        public float BlueSquareDuration;
        public float FbDuration;
        public float ItiDuration;
        public float MinTouchDuration;
        public float MaxTouchDuration;
        public float TouchToRewardDelay;
        public float ReleaseToRewardDelay;
        public float SquareSize;
        public float SquareSizeMin;
        public float SquareSizeMax;
        public float PositionX;
        public float PositionX_Min;
        public float PositionX_Max;
        public float PositionY;
        public float PositionY_Min;
        public float PositionY_Max;
        public bool RewardTouch;
        public int NumTouchPulses;
        public bool RewardRelease;
        public int NumReleasePulses;
        public bool RandomReward;
        public bool RandomSquareSize;
        public bool RandomSquarePosition;
        public int TimeToAutoEndTrialSec;
    }


    public class THR_StimDef : StimDef
    {
        //Already-existing fields (inherited from Stim  Def)
        //public Dictionary<string, StimGroup> StimGroups; //stimulus type field (e.g. sample/target/irrelevant/etc)
        //public string StimName;
        //public string StimPath;
        //public string PrefabPath;
        //public string ExternalFilePath;
        //public string StimFolderPath;
        //public string StimExtension;
        //public int StimCode; //optional, for analysis purposes
        //public string StimID;
        //public int[] StimDimVals; //only if this is parametrically-defined stim
        //[System.NonSerialized] //public GameObject StimGameObject; //not in config, generated at runtime
        //public Vector3 StimLocation; //to be passed in explicitly if trial doesn't include location method
        //public Vector3 StimRotation; //to be passed in explicitly if trial doesn't include location method
        //public Vector2 StimScreenLocation; //screen position calculated during trial
        //public float? StimScale;
        //public bool StimLocationSet;
        //public bool StimRotationSet;
        //public float StimTrialPositiveFbProb; //set to -1 if stim is irrelevant
        //public float StimTrialRewardMag; //set to -1 if stim is irrelevant
        //public TokenReward[] TokenRewards;
        //public int[] BaseTokenGain;
        //public int[] BaseTokenLoss;
        //public int TimesUsedInBlock;
        //public bool isRelevant;
        //public bool TriggersSonication;
        //public State SetActiveOnInitialization;
        //public State SetInactiveOnTermination;
    
    }
}






//EXTRA CONFIG VARIABLES FROM PREV VERSION
//public string ContextName;
//public string BlockName;
//public int MaxNumTrials;
//public int PerfWindowEndTrials;
//public float PerfThresholdEndTrials;
//public float ItiDuration;
//public float ItiDurationSecStartMax;
//public float ItiDurationSecAbsMin;
//public float ItiDurationSecAbsMax;
//public float BlinkOnDurationStartMin;
//public float BlinkOnDurationStartMax;
//public float BlinkOnDurationAbsMin;
//public float BlinkOnDurationAbsMax;
//public float BlinkOffDurationStartMin;
//public float BlinkOffDurationStartMax;
//public float BlinkOffDurationAbsMin;
//public float BlinkOffDurationAbsMax;
//public float MinTouchDuration;
//public float MinTouchDurationMin;
//public float MinTouchDurationMax;
//public float MaxTouchDuration;
//public float MaxTouchDurationMin;
//public float MaxTouchDurationMax;
//public int TouchToRewardDelayMS;
//public int TouchToRewardDelayMSMin;
//public int TouchToRewardDelayMSMax;
//public int ReleaseToRewardDelayMS;
//public int ReleaseToRewardDelayMSMin;
//public int ReleaseToRewardDelayMSMax;
//public int SquareSizeStartMin;
//public int SquareSizeStartMax;
//public int SquareSizeAbsMin;
//public int SquareSizeAbsMax;
//public int SquareSizeWhite;
//public int SquareSizeWhiteMin;
//public int SquareSizeWhiteMax;
//public int SquareLocationX;
//public int SquareLocationXMin;
//public int SquareLocationY;
//public int SquareLocationYMin;
//public int SquareLocationXforWhite;
//public int SquareLocationYforWhite;
//public int NumLocationX;
//public int MinNumLocationX;
//public int MaxNumLocationX;
//public int NumLocationY;
//public int MinNumLocationY;
//public int MaxNumLocationY;
//public float PadX;
//public float PadXMin;
//public float PadXMax;
//public float PadY;
//public float PadYMin;
//public float PadYMax;
//public bool RandomSquarePosition;
//public int RewardCountStartMin;
//public int RewardCountStartMax;
//public int RewardCountAbsMin;
//public int RewardCountAbsMax;
//public bool RewardOnTouchDown;
//public bool RewardOnEveryTouchRelease;
//public int TimeToAutoEndTrialSec;
//public float MinRT;