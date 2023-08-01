using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using USE_DisplayManagement;
using USE_ExperimentTemplate_Trial;


namespace USE_Def_Namespace
{
    public class SessionDef
    {
        // public string Subject;
        // public DateTime SessionStart_DateTime;
        // public int SessionStart_Frame;
        // public float SessionStart_UnityTime;
        // public string SessionID;

        public OrderedDictionary TaskMappings;
        public List<string> TaskNames;
        public Dictionary<string, string> TaskIcons;

        public string ContextExternalFilePath;
        public string TaskIconsFolderPath;
        public Vector3[] TaskIconLocations;
        
        public float TaskSelectionTimeout;
        public bool MacMainDisplayBuild;
        public bool IsHuman;
        public bool StoreData;
        public bool EventCodesActive;
        public bool SyncBoxActive;
        public bool SerialPortActive;
        public string SerialPortAddress;
        public int SerialPortSpeed;
        public List<string> SyncBoxInitCommands;
        public int SplitBytes;
        
        public string EyetrackerType;
        public bool EyeTrackerActive;
        public string SelectionType = "mouse";
        public MonitorDetails MonitorDetails;
        public ScreenDetails ScreenDetails;
        
        public bool SonicationActive;

        public float ShotgunRayCastCircleSize_DVA = 1.25f;
        public float ShotgunRaycastSpacing_DVA = 0.3f;
        public float ParticipantDistance_CM = 60f;
        public int RewardHotKeyNumPulses = 1;
        public int RewardHotKeyPulseSize = 250;

        public float BlockResultsDuration; //specify for human session configs

        public bool PlayBackgroundMusic;

        public bool GuidedTaskSelection;
        

        public int TaskButtonSize = 225;
        public int TaskButtonSpacing = 25;
        public int TaskButtonGridMaxPerRow = 5;

        //If Want to specify positions:
        public List<int> TaskButtonGridSpots; //put into session config
        public int NumGridSpots = 20; //leave as 20, but can adjust if needed

    }

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
