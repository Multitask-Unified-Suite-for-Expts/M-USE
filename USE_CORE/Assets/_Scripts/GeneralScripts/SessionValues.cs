using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using SelectionTracking;
using UnityEngine;
using UnityEngine.InputSystem;
using USE_Def_Namespace;
using USE_DisplayManagement;
using USE_ExperimenterDisplay;
using USE_ExperimentTemplate_Classes;
using USE_ExperimentTemplate_Data;
using USE_ExperimentTemplate_Session;
using USE_Settings;
using USE_UI;
using static UnityEditor.ShaderData;


public static class SessionValues
{
    public static bool WebBuild = false;
    public static bool UseDefaultConfigs = false;
    public static bool Using2DStim = false;
    
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
        if (ConfigAccessType == "Default")
        {
            fileContent = File.ReadAllText(Application.persistentDataPath + Path.DirectorySeparatorChar + "M_USE_DefaultConfigs" + Path.DirectorySeparatorChar + fileName);
            callback(fileContent);
        }
        else if (ConfigAccessType == "Server")
        {
            ServerManager.GetFileStringAsync(ServerManager.SessionConfigFolderPath, "SessionConfig", result =>
            {
                callback(result);
            });
        }
        else if (ConfigAccessType == "Local")
        {
            fileContent = File.ReadAllText(LocateFile.FindFilePathInExternalFolder(ConfigFolderPath, $"*{fileName}*"));
            callback(fileContent);
        }
        else
        {
            callback(null);
        }

        yield break;
    }

    public static IEnumerator BetterReadSettingsFile<T>(string fileName, string fileType, Action<T[]> callback)
    {
        yield return GetFileContentString(fileName, result =>
        {
            if (result != null)
            {
                T[] settingsArray = null;
                if (fileType == "SingleTypeArray")
                    settingsArray = ImportSettings_SingleTypeArray<T>(fileType, result);
                else if (fileType == "SingleTypeJSON")
                    settingsArray = ImportSettings_SingleTypeJSON<T>(fileType, result);
                else if (fileType == "SingleTypeDelimited")
                    settingsArray = ImportSettings_SingleTypeDelimited<T>(fileType, result);
                else
                {
                    Debug.Log("Failed to read Settings File. This is a problem.");
                    callback(null);
                    return;
                }

                callback(settingsArray);
            }
            else
            {
                Debug.LogError("Error retrieving file content.");
                callback(null);
            }
        });
    }

    
    public static T[] ImportSettings_SingleTypeArray<T>(string settingsCategory, string fileContentString, char delimiter = '\t')
    {
        //Settings settings = new Settings(settingsCategory, settingsPath);

        Debug.Log("Attempting to load settings file " + settingsCategory);
        //
        // if (serverFileString == null)
        // {
        //     if (!File.Exists(settingsPath))
        //         return null;
        // }

    string[] lines;

        if (ConfigAccessType == "Server")
        {
            string[] splitLines = fileContentString.Split('\n');
            List<string> stringList = new List<string>();
            foreach (var line in splitLines)
            {
                if (string.IsNullOrEmpty(line.Trim()) || line.Trim().StartsWith("//", StringComparison.Ordinal)) // Skip empty and/or commented out lines
                    continue;
                stringList.Add(line);
            }
            lines = stringList.ToArray();
        }
        else
        {
            lines = new[] { "idk" };
        }
        // if (serverFileString != null)
        // {
        //     string[] splitLines = serverFileString.Split('\n');
        //     List<string> stringList = new List<string>();
        //     foreach (var line in splitLines)
        //     {
        //         if (string.IsNullOrEmpty(line.Trim()) || line.Trim().StartsWith("//", StringComparison.Ordinal)) // Skip empty and/or commented out lines
        //             continue;
        //         stringList.Add(line);
        //     }
        //     lines = stringList.ToArray();
        // } 
        // else
        // {
        //     lines = ReadSettingsFile(settingsPath, "//", "...");
        // }

    T[] settingsArray = new T[lines.Length - 1];

    string[] fieldNames = lines[0].Split(delimiter);

        foreach (string fieldName in fieldNames)
        {
            string tempFieldName = fieldName.Trim();
            if (typeof(T).GetProperty(tempFieldName) == null && typeof(T).GetField(tempFieldName) == null)
            {
                throw new Exception("Settings file \"" + settingsCategory + "\" contains the header \""
                                    + tempFieldName + "\" but this is not a public property or field of the provided type "
                                    + typeof(T) + ".");
            }
        }

    FieldInfo[] fieldInfos = typeof(T).GetFields();

    for (int iLine = 1; iLine < lines.Length; iLine++)
    {
        // Creates an instance for the entire line (ie. BlockDef)
        settingsArray[iLine - 1] = Activator.CreateInstance<T>();
        
        // Splits the separate fields for the single instance (ie. fields of BlockDef)
        string[] values = lines[iLine].Split(delimiter);
        for (int iVal = 0; iVal < fieldNames.Length; iVal++)
        {
            string fieldName = fieldNames[iVal].Trim();
            try
            {
                PropertyInfo propertyInfo = typeof(T).GetProperty(fieldName);
                FieldInfo fieldInfo = typeof(T).GetField(fieldName);
                
                // Checks if the value is a Field or Property of the type T, and sets the value 
                if (propertyInfo != null)
                {
                    Type propertyType = propertyInfo.PropertyType;
                    propertyInfo.SetValue(settingsArray[iLine - 1], Convert.ChangeType(values[iVal], propertyType));
                }
                else if (fieldInfo != null)
                {
                    Type fieldType = fieldInfo.FieldType;

                        fieldInfo.SetValue(settingsArray[iLine - 1], Convert.ChangeType(values[iVal], fieldType));
                    }
                }
                catch (Exception e)
                {
                    Debug.Log(fieldNames[iVal] + ": " + values[iVal]);
                    Debug.Log("Error adding TDF file \"" + settingsCategory + "\" to Settings \"" + settingsCategory + "\".");
                    throw new Exception(e.Message + "\t" + e.StackTrace);
                }
            }
        }

        return settingsArray;

    }
   // public static T[] ImportSettings_SingleTypeJSON<T>(string settingsCategory, string settingsPath, string serverFileString = null, string dictName = "")
    public static T[] ImportSettings_SingleTypeJSON<T>(string settingsCategory, string fileContentString, string dictName = "")
    {
        Debug.Log("Attempting to load settings file " + settingsCategory + ".");
        // if (dictName == "")
        //     dictName = settingsCategory;
        //
        // string dataAsJsonString = serverFileString == null ? File.ReadAllText(settingsPath) : serverFileString;
        T settingsInstance = Activator.CreateInstance<T>();
        try
        {
            settingsInstance = JsonConvert.DeserializeObject<T>(fileContentString);
        }
        catch (Exception e)
        {
            Debug.Log("Error adding JSON file \"" + settingsCategory + "\" to Settings \"" + settingsCategory + "\".");
            throw new Exception(e.Message + "\t" + e.StackTrace);
         }

        T[] settingsArray = new T[] { settingsInstance };
        return settingsArray;
    }
  //  public static T ImportSettings_SingleTypeDelimited<T>(string settingsCategory, string settingsPath, string serverFileString = null, char delimiter = '\t')
    public static T[] ImportSettings_SingleTypeDelimited<T>(string settingsCategory, string fileContentString, char delimiter = '\t')
    {
        Debug.Log("Attempting to load settings file " + settingsCategory);
        
        string[] lines;
        lines = fileContentString.Split('\n');
        
        T settingsInstance = Activator.CreateInstance<T>();

        foreach (string line in lines)
        {
            if (string.IsNullOrEmpty(line.Trim()) || line.Trim().StartsWith("//", StringComparison.Ordinal)) //Skip empty and/or commented out lines
                continue;
            string[] splitString = line.Split(delimiter);
            try
            {
                string fieldName = splitString[1].Trim();
                string fieldValue = splitString[2].Trim();
                
                if(SurroundedByQuotes(fieldValue))
                    fieldValue = fieldValue = fieldValue.Substring(1, fieldValue.Length - 2);

                AssignFieldValue(fieldName, fieldValue, settingsInstance);
            
                // FieldInfo fieldInfo = typeof(T).GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                // PropertyInfo propertyInfo = typeof(T).GetProperty(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                //
                // if (propertyInfo != null)
                // {
                //     Type propertyType = propertyInfo.PropertyType;
                //     propertyInfo.SetValue(settingsInstance, Convert.ChangeType(stringValue, propertyType));
                // }
                // else if (fieldInfo != null)
                // {
                //     Type fieldType = fieldInfo.FieldType;
                //     fieldInfo.SetValue(settingsInstance, Convert.ChangeType(stringValue, fieldType));
                // }
            }
            catch (Exception e)
            {
                Debug.Log("Attempted to import Settings file \"" + settingsCategory + "\" but line \"" + line + "\" has " + line.Length + " entries, 3 expected.");
                throw new Exception(e.Message + "\t" + e.StackTrace);
            }
        }

        // Need to be returning an array for the callback method to be consistent across all three
        T[] settingsArray = new T[] { settingsInstance };
        // FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        //
        // foreach (FieldInfo field in fields)
        // {
        //     object value = field.GetValue(settingsInstance);
        //     Debug.Log($"Field: {field.Name}, Value: {value}");
        // }
        //
        return settingsArray;
    }


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

    private static void AssignFieldValue<T>(string fieldName, string fieldValue, T settingsInstance)
    {
        FieldInfo fieldInfo = typeof(T).GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        PropertyInfo propertyInfo = typeof(T).GetProperty(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (propertyInfo != null)
        {
            Type propertyType = propertyInfo.PropertyType;
            propertyInfo.SetValue(settingsInstance, Convert.ChangeType(fieldValue, propertyType));
        }
        else if (fieldInfo != null)
        {
            Type fieldType = fieldInfo.FieldType;
            if (fieldType.Equals(typeof(OrderedDictionary)))
            {
                if (StartsOrEndsWithBrackets(fieldValue.Trim()))
                {
                    fieldValue = fieldValue.Substring(1, fieldValue.Length - 2);
                }
                string[] sArray = fieldValue.Split(',');
                OrderedDictionary pairs = new OrderedDictionary();
                for (int sCount = 0; sCount < sArray.Length; sCount++)
                {
                    sArray[sCount] = sArray[sCount].Replace("\"", "");
                    sArray[sCount] = sArray[sCount].Trim();
                    string[] sArray2 = sArray[sCount].Split(':');
                    pairs.Add(sArray2[0].Trim(), sArray2[1].Trim());
                }
                fieldInfo.SetValue(settingsInstance, pairs);
            }
            else if (fieldType.Equals(typeof(Dictionary<string, string>)))
            {
                if (StartsOrEndsWithBrackets(fieldValue))
                    fieldValue = fieldValue.Substring(1, fieldValue.Length - 2);
                string[] sArray = fieldValue.Split(',');
                Dictionary<string, string> pairs = new Dictionary<string, string>();
                for (int sCount = 0; sCount < sArray.Length; sCount++)
                {
                    sArray[sCount] = sArray[sCount].Replace("\"", "");
                    sArray[sCount] = sArray[sCount].Trim();
                    string[] sArray2 = sArray[sCount].Split(':');
                    pairs.Add(sArray2[0].Trim(), sArray2[1].Trim());
                }
                fieldInfo.SetValue(settingsInstance, pairs);
            }
            else if (fieldType.Equals(typeof(Vector3[])))
            {
                string[][] sArray = (string[][])JsonConvert.DeserializeObject(fieldValue, typeof(string[][]));
                Vector3[] finalArray = new Vector3[sArray.Length];
                for (int iVal = 0; iVal < sArray.Length; iVal++)
                {
                    finalArray[iVal] = new Vector3(float.Parse(sArray[iVal][0]), float.Parse(sArray[iVal][1]), float.Parse(sArray[iVal][2]));
                }
                fieldInfo.SetValue(settingsInstance, finalArray);
            }

            else if (fieldType.Equals(typeof(List<string>)))
            {
                if (StartsOrEndsWithBrackets(fieldValue))
                    fieldValue = fieldValue.Substring(1, fieldValue.Length - 2);
                string[] sArray = fieldValue.Split(',');
                List<string> valuesList = new List<string>();
                for (int sCount = 0; sCount < sArray.Length; sCount++)
                {
                    sArray[sCount] = sArray[sCount].Replace("\"", "");
                    sArray[sCount] = sArray[sCount].Trim();
                    valuesList.Add(sArray[sCount]);
                }
                fieldInfo.SetValue(settingsInstance, valuesList);
            }
            else if (fieldType.Equals(typeof(MonitorDetails)))
            {
                var deserializedValue = JsonConvert.DeserializeObject<MonitorDetails>(fieldValue);
                fieldInfo.SetValue(settingsInstance, deserializedValue);
            }
            else if (fieldType.Equals(typeof(ScreenDetails)))
            {
                var deserializedValue = JsonConvert.DeserializeObject<ScreenDetails>(fieldValue);
                fieldInfo.SetValue(settingsInstance, deserializedValue);
            }
            else
            {
                fieldInfo.SetValue(settingsInstance, Convert.ChangeType(fieldValue, fieldType));
            }
        }
    }

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
