using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using USE_Settings;


public class DisplayController : MonoBehaviour
{
    [HideInInspector] public InitScreen InitScreen;
    [HideInInspector] public Canvas InitScreenCanvas;
    [HideInInspector] public bool SingleDisplayBuild;
    [HideInInspector] public bool SwitchDisplays;
    [HideInInspector] public bool MacBuild;

    public Dictionary<string, bool> DisplayDict;


    public void HandleDisplays(InitScreen initScreen) //Called by Start method of InitScreen
    {
        InitScreen = initScreen;
        InitScreenCanvas = initScreen.GetComponentInParent<Canvas>();
        LoadDisplaySettings();
        SetDisplays();
    }

    public void LoadDisplaySettings()
    {
        string folderPath = "";

        if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX || SystemInfo.operatingSystemFamily == OperatingSystemFamily.Linux)
        {
            if (Application.isEditor)
                folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "/Desktop/MUSE/Configs";
            else
            {
                MacBuild = true;
                string[] stringParts = Application.dataPath.Split('/');
                string path = "/";
                for (int i = 0; i < stringParts.Length - 3; i++)
                    path += stringParts[i] + "/";
                path += "Configs";
                folderPath = path;
            }
        }
        else //Windows:
        {
            if (Application.isEditor)
                folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "/MUSE/Configs";
            else
            {
                string[] stringParts = Application.dataPath.Split('/');
                string path = "";
                for (int i = 0; i < stringParts.Length - 2; i++)
                    path += stringParts[i] + "/";
                path += "Configs";
                folderPath = path;
            }
        }

        if (folderPath.Length > 1)
        {
            string displayConfigLocation = FindFileInFolder(folderPath, "*DisplayConfig*");

            if (!string.IsNullOrEmpty(displayConfigLocation))
            {
                SessionSettings.ImportSettings_SingleTypeJSON<Dictionary<string, bool>>("DisplayConfig", displayConfigLocation);
                DisplayDict = (Dictionary<string, bool>)SessionSettings.Get("DisplayConfig");
                SingleDisplayBuild = DisplayDict["SingleDisplayBuild"];
                SwitchDisplays = DisplayDict["SwitchDisplays"];
            }
        }
    }


    public void SetDisplays()
    {
        //if ((SingleDisplayBuild && SwitchDisplays) || SwitchDisplays) //If both, just switch. Basically error handling
        //{
        //    InitScreenCanvas.targetDisplay = 0;
        //    GameObject.Find("InitCamera").GetComponent<Camera>().targetDisplay = 0;
        //    Debug.Log("MAIN CAM NAME: " + Camera.main.name);
        //    Camera.main.targetDisplay = 1; //Change main camera cuz that's where TaskSelectionCanvas rendering to
        //    //And then in session, when experimenter display is instantiated, change all its children with cameras to targetdisplay 0.
        //}
        if (SingleDisplayBuild && !SwitchDisplays) //Put InitScreen on Main Display
            InitScreenCanvas.targetDisplay = 0;
        

        if(MacBuild)
        {
            InitScreen.transform.localScale *= 1.75f;
            GameObject confirmButton = InitScreen.transform.Find("ButtonConfirm").gameObject;
            confirmButton.transform.position = new Vector3(confirmButton.transform.position.x, confirmButton.transform.position.y + 1000f, confirmButton.transform.position.z);
        }
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
