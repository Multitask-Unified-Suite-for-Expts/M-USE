using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using SelectionTracking;
using UnityEngine;
using USE_Def_Namespace;
using USE_DisplayManagement;
using USE_ExperimenterDisplay;
using USE_ExperimentTemplate_Classes;
using USE_ExperimentTemplate_Data;
using USE_ExperimentTemplate_Session;
using USE_UI;


public static class SessionValues
{
    public static bool WebBuild;
    public static bool UsingDefaultConfigs;
    public static bool Using2DStim;
    
    public static ControlLevel_Session_Template SessionLevel;
    public static SessionInfoPanel SessionInfoPanel;
    public static USE_StartButton USE_StartButton;
    public static GameObject TaskSelectionCanvasGO;
    public static HumanStartPanel HumanStartPanel;
    public static DisplayController DisplayController;
    public static ExperimenterDisplayController ExperimenterDisplayController;
    public static SessionDataControllers SessionDataControllers;
    public static LocateFile LocateFile;
    public static string SessionDataPath;
    public static string SessionLevelDataPath;
    public static string FilePrefix;
    public static string SubjectID;
    public static string SessionID;
    public static SerialRecvData SerialRecvData;
    public static SerialSentData SerialSentData;
    public static GazeData GazeData;
    public static MouseTracker MouseTracker;
    public static GazeTracker GazeTracker;
    public static TobiiEyeTrackerController TobiiEyeTrackerController;
    public static GameObject InputManager;

    public static EventCodeManager EventCodeManager;
    public static Dictionary<string, EventCode> SessionEventCodes;
    
    public static string ConfigAccessType;
    public static string ConfigFolderPath;

    public static SyncBoxController SyncBoxController;
    public static SerialPortThreaded SerialPortController;


    public static SelectionTracker SelectionTracker;
    public static SelectionTracker.SelectionHandler SelectionHandler;

    public static SessionDef SessionDef;
    // ===== FIELDS OF SESSIONDEF =====
    // public OrderedDictionary TaskMappings;
    // public List<string> TaskNames;
    // public Dictionary<string, string> TaskIcons;
    // public string ContextExternalFilePath;
    // public string TaskIconsFolderPath;
    // public Vector3[] TaskIconLocations;
    // public float TaskSelectionTimeout;
    // public bool MacMainDisplayBuild;
    // public bool IsHuman;
    // public bool StoreData;
    // public bool EventCodesActive;
    // public bool SyncBoxActive;
    // public bool SerialPortActive;
    // public string SerialPortAddress;
    // public int SerialPortSpeed;
    // public List<string> SyncBoxInitCommands;
    // public int SplitBytes;
    // public string EyetrackerType;
    // public bool EyeTrackerActive;
    // public string SelectionType = "mouse";
    // public MonitorDetails MonitorDetails;
    // public ScreenDetails ScreenDetails;
    // public bool SonicationActive;
    // public float ShotgunRayCastCircleSize_DVA = 1.25f;
    // public float ShotgunRaycastSpacing_DVA = 0.3f;
    // public float ParticipantDistance_CM = 60f;
    // public int RewardHotKeyNumPulses = 1;
    // public int RewardHotKeyPulseSize = 250;
    // public bool GuidedTaskSelection;
    // public float BlockResultsDuration;



    static SessionValues() // idk about this???
    {
        // Perform actions when certain values are true

        // if (SessionDef.SyncBoxActive)
        // {
        //     SyncBoxController = new SyncBoxController();
        //     SyncBoxController.serialPortController = SerialPortController;
        //     SerialSentData.sc = SerialPortController;
        //     SerialRecvData.sc = SerialPortController;
        //     SyncBoxController.SessionEventCodes = SessionEventCodes;
        //     // SyncBoxController.SessionEventCodes = SessionEventCodes;
        //     // tl.SyncBoxController = SyncBoxController;
        // }
    }
    //
    // public static float ShotgunRayCastCircleSize_DVA;
    // public static float ShotgunRaycastSpacing_DVA;
    // public static float ParticipantDistance_CM;

    public static IEnumerator GetFileContentString(string fileName, Action<string> callback)
    {
        string fileContent;
        if (ConfigAccessType == "Local" || ConfigAccessType == "Default")
        {
            fileContent = File.ReadAllText(LocateFile.FindFilePathInExternalFolder(ConfigFolderPath, $"*{fileName}*")); //Will need to check that this works during Web Build
            callback(fileContent);
        }
        else if (ConfigAccessType == "Server")
        {
            yield return CoroutineHelper.StartCoroutine(ServerManager.GetFileStringAsync(ConfigFolderPath, "SessionConfig", result =>
            {
                callback(result);
            }));
        }
        else
            callback(null);

    }

    // public static IEnumerator BetterReadSettingsFile<T>(string fileName, string fileType, Action<T[]> callback)
    // {
    //     yield return CoroutineHelper.StartCoroutine(GetFileContentString(fileName, result =>
    //     {
    //         if (result != null)
    //         {
    //             T[] settingsArray = null;
    //             if (fileType == "SingleTypeArray")
    //                 settingsArray = ImportSettings_SingleTypeArray<T>(fileType, result);
    //             else if (fileType == "SingleTypeJSON")
    //                 settingsArray = ImportSettings_SingleTypeJSON<T>(fileType, result);
    //             else if (fileType == "SingleTypeDelimited")
    //                 settingsArray = ImportSettings_SingleTypeDelimited<T>(fileType, result);
    //             else
    //             {
    //                 Debug.Log("Failed to read Settings File. This is a problem.");
    //                 callback(null);
    //                 return;
    //             }
    //             callback(settingsArray);
    //         }
    //         else
    //         {
    //             Debug.LogError("Error retrieving file content.");
    //             callback(null);
    //         }
    //     }));
    // }

    
}
