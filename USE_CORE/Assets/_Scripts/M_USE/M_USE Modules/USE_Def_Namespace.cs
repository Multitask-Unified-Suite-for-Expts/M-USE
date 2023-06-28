using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using USE_ExperimentTemplate_Trial;

namespace USE_Def_Namespace
{
    public class TaskDef
    {
        public string TaskName;
        public string ExternalStimFolderPath;
        public string PrefabStimFolderPath;
        public string ExternalStimExtension;
        public List<string[]> FeatureNames;
        public string neutralPatternedColorName;
        public float? ExternalStimScale;
        public List<string[]> FeedbackControllers;
        public int? TotalTokensNum;
        public bool SerialPortActive, SyncBoxActive, EventCodesActive, RewardPulsesActive, SonicationActive;
        public string SelectionType;
        public Dictionary<string, string> CustomSettings;

    }
    
    public class BlockDef
    {
        public int BlockCount;
        public List<TrialDef> TrialDefs;
        public int? TotalTokensNum;
        public int? MinTrials, MaxTrials;
        public System.Random RandomNumGenerator;

        public virtual void GenerateTrialDefsFromBlockDef()
        {
        }

        public virtual void AddToTrialDefsFromBlockDef()
        {
        }

        public virtual void BlockInitializationMethod()
        {
        }
    }
    
    public abstract class TrialDef
    {
        public int BlockCount, TrialCountInBlock, TrialCountInTask;
        public TrialStims TrialStims;
    }

    
}
