using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SelectionTracking;
using UnityEngine;
using USE_ExperimentTemplate_Classes;
using USE_ExperimentTemplate_Data;
using USE_ExperimentTemplate_Session;
using USE_UI;


public static class SessionValues
{
    public static bool WebBuild;
    public static bool UseDefaultConfigs;
    public static bool Using2DStim;
    public static bool IsHuman;
    public static bool StoreData; //Set by sessionLevel after reading in config. 
    public static bool EyeTrackerActive;
    public static bool SerialPortActive;
    public static bool SyncBoxActive;
    public static bool EventCodesActive;

    public static ControlLevel_Session_Template SessionLevel;
    public static USE_StartButton USE_StartButton;
    //public static GameObject TaskSelectionCanvasGO;
    public static HumanStartPanel HumanStartPanel;
    public static DisplayController DisplayController;
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
    public static SelectionTracker SelectionTracker;
    public static MouseTracker MouseTracker;
    public static GameObject InputManager;
    public static string SelectionType;
    public static string ContextExternalFilePath;
    public static EventCodeManager EventCodeManager;
    public static Dictionary<string, EventCode> SessionEventCodes;

    public static float ShotgunRayCastCircleSize_DVA;
    public static float ShotgunRaycastSpacing_DVA;
    public static float ParticipantDistance_CM;



    static SessionValues()
    {
        // Perform actions when certain values are true
        if (EyeTrackerActive)
        {
            // tl.GazeTracker = GazeTracker;
            // tl.TobiiEyeTrackerController = TobiiEyeTrackerController;
        }

        if (SerialPortActive)
        {
            // tl.SerialPortController = SerialPortController;
        }

        if (SyncBoxActive)
        {
            // SyncBoxController.SessionEventCodes = SessionEventCodes;
            // tl.SyncBoxController = SyncBoxController;
        }
    }
 public static void ImportSettings_SingleTypeArray<T>(string settingsCategory, string settingsPath, string serverFileString = null, char delimiter = '\t')
{
    //Settings settings = new Settings(settingsCategory, settingsPath);

    Debug.Log("Attempting to load settings file " + settingsPath);

    if (serverFileString == null)
    {
        if (!File.Exists(settingsPath))
            return;
    }

    string[] lines;

    if (serverFileString != null)
    {
        string[] splitLines = serverFileString.Split('\n');
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
        lines = ReadSettingsFile(settingsPath, "//", "...");
    }

    T[] settingsArray = new T[lines.Length - 1];

    string[] fieldNames = lines[0].Split(delimiter);

    foreach (string fieldName in fieldNames)
    {
        string tempFieldName = fieldName.Trim();
        Debug.Log("field name: " + tempFieldName);
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
                Debug.Log("Error adding TDF file \"" + settingsPath + "\" to Settings \"" + settingsCategory + "\".");
                throw new Exception(e.Message + "\t" + e.StackTrace);
            }
        }

        // Checking that the values in each instance and related fields are sensible
        var type = settingsArray[iLine - 1].GetType();
        var fields = type.GetFields();

        foreach (var field in fields)
        {
            var fieldName = field.Name;
            var fieldValue = field.GetValue(settingsArray[iLine - 1]);

            Debug.Log($"BlockNum {iLine}: {fieldName} = {fieldValue}");
        }
    }

    // settings.AddSetting(settingsCategory, settingsArray);
   // allSettings.Add(settingsCategory, settings);
}
 private static string[] ReadSettingsFile(string settingsPath, string commentPrefix = "", string continueSuffix = "")
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
                 line = line + newLine;
             }
             outputList.Add(line);
         }
     }
     return outputList.ToArray();
 }

    
    
}

