using FlexLearning_Namespace;
using System;
using System.Collections.Generic;
using UnityEngine;
using USE_ExperimentTemplate_Block;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Trial;
using USE_StimulusManagement;
using USE_Def_Namespace;

namespace GazeCalibration_Namespace
{
    public class GazeCalibration_TaskDef : TaskDef
    {
    }

    public class GazeCalibration_BlockDef : BlockDef
    {
        //Already-existing fields (inherited from BlockDef)
        //public int BlockCount;
        //public TrialDef[] TrialDefs;
        public int NumTrials = 5;
        public int NumPulses = 2;
        public int PulseSize = 250;

        public override void GenerateTrialDefsFromBlockDef()
        {
            TrialDefs = new List<GazeCalibration_TrialDef>().ConvertAll(x => (TrialDef)x);

            for (int iTrial = 0; iTrial < NumTrials; iTrial++)
            {
                GazeCalibration_TrialDef td = new GazeCalibration_TrialDef();
                td.NumTrials = NumTrials;
                td.NumPulses = NumPulses;
                td.PulseSize = PulseSize;
                TrialDefs.Add(td);
            }
        }
    }

    public class GazeCalibration_TrialDef : TrialDef
    {
        //Already-existing fields (inherited from TrialDef)
        //public int BlockCount, TrialCountInBlock, TrialCountInTask;
        //public TrialStims TrialStims;
       // public int BlockID;
        public string ContextName;
        public int NumTrials;
        public int NumPulses;
        public int PulseSize;
    }

    public class GazeCalibration_StimDef : StimDef
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
        //public bool IsRelevant;
        //public bool TriggersSonication;
        //public State SetActiveOnInitialization;
        //public State SetInactiveOnTermination;
    
    }
}