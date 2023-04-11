using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using USE_ExperimentTemplate_Classes;
using USE_Settings;


public class DisplayController : MonoBehaviour
{
    [HideInInspector] public Canvas InitScreenCanvas;
    [HideInInspector] public bool SingleDisplayBuild;

    public Dictionary<string, bool> DisplayDict;


    public void HandleDisplays(InitScreen initScreen) //Called by Start method of InitScreen
    {
        InitScreenCanvas = initScreen.GetComponentInParent<Canvas>();
        LoadDisplaySettings();
        SetDisplays();
    }

    public void LoadDisplaySettings()
    {
        string folderPath;

        if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX || SystemInfo.operatingSystemFamily == OperatingSystemFamily.Linux)
        {
            if (Application.isEditor)
                folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "/Desktop/MUSE_Master/DemoConfigs"; //
            else
                folderPath = Application.dataPath;
        }
        else //Windows:
        {
            if (Application.isEditor)
                folderPath = "";
            else
                folderPath = Application.dataPath;
        }

        //Debug.LogError("FOLDER PATH: " + folderPath);

        string displayConfigLocation = FindFileInFolder(folderPath, "*DisplayConfig*");

        if (!string.IsNullOrEmpty(displayConfigLocation))
        {
            Debug.Log("String not null! | " + "string: " + displayConfigLocation);
            SessionSettings.ImportSettings_SingleTypeJSON<Dictionary<string, bool>>("DisplayConfig", displayConfigLocation);
            DisplayDict = (Dictionary<string, bool>)SessionSettings.Get("DisplayConfig");
            Debug.Log("DISPLAY DICT COUNT: " + DisplayDict.Count);
            SingleDisplayBuild = DisplayDict["SingleDisplayBuild"];
            Debug.Log("SINGLE DISPLAY BUILD? " + SingleDisplayBuild);
        }
    }

    public void SetDisplays()
    {
        if (SingleDisplayBuild)
            InitScreenCanvas.targetDisplay--;
    }

    public string FindFileInFolder(string keyToFolder, string stringPattern)
    {
        string[] possibleFiles = Directory.GetFiles(keyToFolder, stringPattern);
        if (possibleFiles.Length == 1)
            return possibleFiles[0];
        else if (possibleFiles.Length == 0)
            Debug.Log("No file following pattern " + stringPattern + " is found at path " + keyToFolder + ".");
        else
            Debug.Log("More than one file following pattern " + stringPattern + " is found at path " + keyToFolder + ".");
        return "";
    }



}
