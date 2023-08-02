using System;
using SelectionTracking;
using UnityEngine;
using USE_Def_Namespace;
using USE_ExperimenterDisplay;
using USE_ExperimentTemplate_Data;
using USE_ExperimentTemplate_Session;
using USE_UI;


public static class SessionValues
{
    public static bool WebBuild;
    public static bool Using2DStim;

    //Info Collected from Init Screen Panels:
    public static string SubjectID;
    public static string SubjectAge;
    public static bool UsingDefaultConfigs;
    public static bool UsingLocalConfigs;
    public static bool UsingServerConfigs;
    public static bool StoringDataLocally;
    public static bool StoringDataOnServer;

    //Prefabs passed from SessionLevel;
    public static GameObject BlockResultsPrefab;
    public static GameObject BlockResults_GridElementPrefab;

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
    public static string TaskSelectionDataPath;
    public static string FilePrefix;


    public static SerialRecvData SerialRecvData;
    public static SerialSentData SerialSentData;
    public static GazeData GazeData;
    public static GameObject InputTrackers;
    public static MouseTracker MouseTracker;
    public static GazeTracker GazeTracker;
    public static TobiiEyeTrackerController TobiiEyeTrackerController;
    public static GameObject InputManager;
    public static FlashPanelController FlashPanelController;

    public static EventCodeManager EventCodeManager;
    
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
    //public int TaskButtonSize = 240; //not currently used
    //public int TaskButtonSpacing = 45; //not currently used
    //public int TaskButtonGridMaxPerRow = 4; //not currently used
    //public bool UseTaskButtonsGrid;

    static SessionValues()
    {
        Debug.Log("SESSION VALUES CONSTRUCTOR!");

        LoadPrefabs();

    }

    private static void LoadPrefabs()
    {
        BlockResults_GridElementPrefab = Resources.Load<GameObject>("BlockResults_GridElement");
        BlockResultsPrefab = Resources.Load<GameObject>("BlockResults");
    }

    //public static IEnumerator GetFileContentString(string fileName, Action<string> callback)
    //{
    //    string fileContent;
    //    if (ConfigAccessType == "Local" || ConfigAccessType == "Default")
    //    {
    //        fileContent = File.ReadAllText(LocateFile.FindFilePathInExternalFolder(ConfigFolderPath, $"*{fileName}*")); //Will need to check that this works during Web Build
    //        callback(fileContent);
    //    }
    //    else if (ConfigAccessType == "Server")
    //    {
    //        yield return CoroutineHelper.StartCoroutine(ServerManager.GetFileStringAsync(ConfigFolderPath, "SessionConfig", result =>
    //        {
    //            callback(result);
    //        }));
    //    }
    //    else
    //        callback(null);

    //}

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


    /*private static string[] ReadSettingsFile(string settingsPath, string commentPrefix = "", string continueSuffix = "")
    {
        List<string> outputList = new List<string>();
        //read the file
        StreamReader textFile;
        try
        {
            //read in all data and parse it
            textFile = new StreamReader(settingsPath);
        }
        catch (Exception e)
        {
            Debug.Log("The settings file could not be read:" + settingsPath);
            throw new Exception(e.Message + "\t" + e.StackTrace);
        }

        if (!(textFile == null || textFile == default(StreamReader)))
        {
            string line;
            while ((line = textFile.ReadLine()) != null)
            {
                if (string.IsNullOrEmpty(line) || (!string.IsNullOrEmpty(commentPrefix) && line.StartsWith(commentPrefix, StringComparison.Ordinal)))
                    continue;
                while (!string.IsNullOrEmpty(continueSuffix) && line.EndsWith(continueSuffix, StringComparison.Ordinal))
                {
                    line = line.Remove(line.Length - continueSuffix.Length);
                    string newLine = textFile.ReadLine().Trim();
                    while (string.IsNullOrEmpty(newLine) || (!string.IsNullOrEmpty(commentPrefix) && newLine.StartsWith(commentPrefix, StringComparison.Ordinal)))
                    {
                        newLine = textFile.ReadLine().Trim();
                    }
                    line += newLine;
                }
                outputList.Add(line);
            }
        }
        return outputList.ToArray();
    }*/

    // private static void AssignFieldValue<T>(string fieldName, string fieldValue, T settingsInstance)
    // {
    //     FieldInfo fieldInfo = typeof(T).GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    //     PropertyInfo propertyInfo = typeof(T).GetProperty(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    //
    //     if (propertyInfo != null)
    //     {
    //         Type propertyType = propertyInfo.PropertyType;
    //         propertyInfo.SetValue(settingsInstance, Convert.ChangeType(fieldValue, propertyType));
    //     }
    //     else if (fieldInfo != null)
    //     {
    //         Type fieldType = fieldInfo.FieldType;
    //         if (fieldType.Equals(typeof(OrderedDictionary)))
    //         {
    //             if (StartsOrEndsWithBrackets(fieldValue.Trim()))
    //             {
    //                 fieldValue = fieldValue.Substring(1, fieldValue.Length - 2);
    //             }
    //             string[] sArray = fieldValue.Split(',');
    //             OrderedDictionary pairs = new OrderedDictionary();
    //             for (int sCount = 0; sCount < sArray.Length; sCount++)
    //             {
    //                 sArray[sCount] = sArray[sCount].Replace("\"", "");
    //                 sArray[sCount] = sArray[sCount].Trim();
    //                 string[] sArray2 = sArray[sCount].Split(':');
    //                 pairs.Add(sArray2[0].Trim(), sArray2[1].Trim());
    //             }
    //             fieldInfo.SetValue(settingsInstance, pairs);
    //         }
    //         else if (fieldType.Equals(typeof(Dictionary<string, string>)))
    //         {
    //             if (StartsOrEndsWithBrackets(fieldValue))
    //                 fieldValue = fieldValue.Substring(1, fieldValue.Length - 2);
    //             string[] sArray = fieldValue.Split(',');
    //             Dictionary<string, string> pairs = new Dictionary<string, string>();
    //             for (int sCount = 0; sCount < sArray.Length; sCount++)
    //             {
    //                 sArray[sCount] = sArray[sCount].Replace("\"", "");
    //                 sArray[sCount] = sArray[sCount].Trim();
    //                 string[] sArray2 = sArray[sCount].Split(':');
    //                 pairs.Add(sArray2[0].Trim(), sArray2[1].Trim());
    //             }
    //             fieldInfo.SetValue(settingsInstance, pairs);
    //         }
    //         else if (fieldType.Equals(typeof(Vector3[])))
    //         {
    //             string[][] sArray = (string[][])JsonConvert.DeserializeObject(fieldValue, typeof(string[][]));
    //             Vector3[] finalArray = new Vector3[sArray.Length];
    //             for (int iVal = 0; iVal < sArray.Length; iVal++)
    //             {
    //                 finalArray[iVal] = new Vector3(float.Parse(sArray[iVal][0]), float.Parse(sArray[iVal][1]), float.Parse(sArray[iVal][2]));
    //             }
    //             fieldInfo.SetValue(settingsInstance, finalArray);
    //         }
    //
    //         else if (fieldType.Equals(typeof(List<int>)))
    //         {
    //             if (StartsOrEndsWithBrackets(fieldValue))
    //                 fieldValue = fieldValue.Substring(1, fieldValue.Length - 2);
    //
    //             string[] sArray = fieldValue.Split(',');
    //             List<int> valuesList = new List<int>();
    //
    //             for (int sCount = 0; sCount < sArray.Length; sCount++)
    //             {
    //                 if (int.TryParse(sArray[sCount], out int parsedValue))
    //                     valuesList.Add(parsedValue);
    //             }
    //             fieldInfo.SetValue(settingsInstance, valuesList);
    //         }
    //
    //         else if (fieldType.Equals(typeof(List<string>)))
    //         {
    //             if (StartsOrEndsWithBrackets(fieldValue))
    //                 fieldValue = fieldValue.Substring(1, fieldValue.Length - 2);
    //             string[] sArray = fieldValue.Split(',');
    //             List<string> valuesList = new List<string>();
    //             for (int sCount = 0; sCount < sArray.Length; sCount++)
    //             {
    //                 sArray[sCount] = sArray[sCount].Replace("\"", "");
    //                 sArray[sCount] = sArray[sCount].Trim();
    //                 valuesList.Add(sArray[sCount]);
    //             }
    //             fieldInfo.SetValue(settingsInstance, valuesList);
    //         }
    //
    //         else if (fieldType.Equals(typeof(MonitorDetails)))
    //         {
    //             var deserializedValue = JsonConvert.DeserializeObject<MonitorDetails>(fieldValue);
    //             fieldInfo.SetValue(settingsInstance, deserializedValue);
    //         }
    //         else if (fieldType.Equals(typeof(ScreenDetails)))
    //         {
    //             var deserializedValue = JsonConvert.DeserializeObject<ScreenDetails>(fieldValue);
    //             fieldInfo.SetValue(settingsInstance, deserializedValue);
    //         }
    //         else
    //         {
    //             fieldInfo.SetValue(settingsInstance, Convert.ChangeType(fieldValue, fieldType));
    //         }
    //     }
    // }

    public static bool StartsOrEndsWithBrackets(string s)
    {
        if (s.StartsWith("(", StringComparison.Ordinal) &&
            s.EndsWith(")", StringComparison.Ordinal) ||
            s.StartsWith("{", StringComparison.Ordinal) &&
            s.EndsWith("}", StringComparison.Ordinal) ||
            s.StartsWith("[", StringComparison.Ordinal) &&
            s.EndsWith("]", StringComparison.Ordinal))
        {
            return true;
        }
        else
            return false;
    }

    public static bool SurroundedByQuotes(string s)
    {
        if(s.StartsWith("\"", StringComparison.Ordinal) &&
            s.EndsWith("\"", StringComparison.Ordinal))
            return true;
        else return false;
    }
}
