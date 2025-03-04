/*
MIT License

Copyright (c) 2023 Multitask - Unified - Suite -for-Expts

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files(the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/



using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using USE_States;
using System.IO;
using System.Linq;
using SelectionTracking;
using UnityEngine.SceneManagement;
using USE_Def_Namespace;
using USE_DisplayManagement;
using USE_ExperimentTemplate_Classes;
using USE_ExperimentTemplate_Data;
using USE_ExperimentTemplate_Session;
using USE_ExperimentTemplate_Task;


public class SetupSession_Level : ControlLevel
{
    private State ImportSessionSettings, CreateDataFolder, LoadTaskScene, VerifyTask;
    public bool settingsImported, dataFolderCreated, taskSceneLoaded, taskVerified;
    public bool setupPaused;
    private List<string> unverifiedTasks;
    public ImportSettings_Level importSettings_Level;
    public SessionData SessionData;
    public FrameData FrameData;
    private ControlLevel_Task_Template taskLevel;


    public override void DefineControlLevel()
    {
        ImportSessionSettings = new State("ImportSessionSettings");
        CreateDataFolder = new State("CreateDataFolder");
        LoadTaskScene = new State("LoadTaskScene");
        VerifyTask = new State("VerifyTask");
        AddActiveStates(new List<State> { ImportSessionSettings, CreateDataFolder, LoadTaskScene, VerifyTask });

        importSettings_Level = GameObject.Find("ControlLevels").GetComponent<ImportSettings_Level>();
        ImportSessionSettings.AddChildLevel(importSettings_Level);
        ImportSessionSettings.AddDefaultInitializationMethod(() =>
        {
            SetDataPaths();
            if (Session.UsingDefaultConfigs)
                WriteSessionConfigsToPersistantDataPath();

            importSettings_Level.SettingsDetails = new List<SettingsDetails>()
            {
                new SettingsDetails(Session.ConfigFolderPath, "SingleType", "SessionConfig", typeof(SessionDef)),
                new SettingsDetails(Session.ConfigFolderPath, "JSON", "SessionEventCode", typeof(Dictionary<string, EventCode>))
            };
        });
        ImportSessionSettings.AddUpdateMethod(() =>
        {
            if (importSettings_Level.fileParsed)
            {
                Debug.Log(importSettings_Level.SettingsDetails[0].SearchString + " PARSED!");

                if (importSettings_Level.SettingsDetails[0].SearchString == "SessionConfig")
                {
                    Session.SessionDef = (SessionDef)importSettings_Level.parsedResult;  //set sessiondef to the parsed content
                }
                else if (importSettings_Level.SettingsDetails[0].SearchString == "SessionEventCode")
                {
                    Session.EventCodeManager.SessionEventCodes = (Dictionary<string, EventCode>)importSettings_Level.parsedResult;  //set event codes to parsed content
                }
                else
                    Debug.LogError($"The {importSettings_Level.SettingsDetails[0].SearchString} has been parsed, but is unable to be set as it is not a SessionConfig or EventCode file.");

                importSettings_Level.importPaused = false;
                settingsImported = true;
                setupPaused = false;
            }
        });
        ImportSessionSettings.SpecifyTermination(() => ImportSessionSettings.ChildLevel.Terminated && !setupPaused, CreateDataFolder,
            () =>
            {
                settingsImported = false;
                SetupInputManagement(Session.SessionLevel.selectTask, Session.SessionLevel.loadTask);
                SetupSessionDataControllers();
            });

        CreateDataFolder.AddDefaultInitializationMethod(() =>
        {
            dataFolderCreated = false;
            if (Session.StoreData)
                StartCoroutine(CreateSessionDataFolder(result =>
                {
                    dataFolderCreated = true;
                }));
            else
                dataFolderCreated = true;
        });

        CreateDataFolder.SpecifyTermination(() => dataFolderCreated && !setupPaused, LoadTaskScene);

        int iTask = 0;
        string taskName = "";
        AsyncOperation loadScene = null;

        LoadTaskScene.AddSpecificInitializationMethod(() =>
        {
            taskSceneLoaded = false;
            taskName = (string)Session.SessionDef.TaskMappings[iTask];
            loadScene = SceneManager.LoadSceneAsync(taskName, LoadSceneMode.Additive);
            string configFolderName = Session.SessionDef.TaskMappings.Cast<DictionaryEntry>().ElementAt(iTask).Key.ToString();
            loadScene.completed += (_) =>
            {
                taskSceneLoaded = true;
                GameObject.Find(taskName + "_Camera").SetActive(false);
            };
        });

        LoadTaskScene.SpecifyTermination(() => taskSceneLoaded && !setupPaused, VerifyTask);


        VerifyTask_Level verifyTask_Level = GameObject.Find("ControlLevels").GetComponent<VerifyTask_Level>();

        VerifyTask.AddChildLevel(verifyTask_Level);
        VerifyTask.AddSpecificInitializationMethod(() =>
        {
            //loads 
            var methodInfo = GetType().GetMethod(nameof(this.GetTaskLevelType));
            Type taskType = USE_Tasks_CustomTypes.CustomTaskDictionary[taskName].TaskLevelType;
            MethodInfo GetTaskLevelType = methodInfo.MakeGenericMethod(new Type[] { taskType });

            string configFolderName = Session.SessionDef.TaskMappings.Cast<DictionaryEntry>().ElementAt(iTask).Key.ToString();

            GetTaskLevelType.Invoke(this, new object[] { configFolderName, verifyTask_Level });
            verifyTask_Level.TaskLevel.TaskConfigPath = Session.ConfigFolderPath + "/" + configFolderName;
        });

        VerifyTask.SpecifyTermination(() => verifyTask_Level.Terminated && !setupPaused && iTask < Session.SessionDef.TaskMappings.Count - 1, LoadTaskScene,
        () =>
        {
            Session.SessionLevel.ActiveTaskLevels.Add(taskLevel);
            SceneManager.UnloadSceneAsync(taskName);
            iTask++;
        });
        VerifyTask.SpecifyTermination(() => verifyTask_Level.Terminated && !setupPaused && iTask == Session.SessionDef.TaskMappings.Count - 1, () => null, () =>
        {
            ParentState.ChildLevel = null;
            SceneManager.UnloadSceneAsync(taskName);
        });
    }


    public void GetTaskLevelType<T>(string configFolderName, VerifyTask_Level verifyTask_Level) where T : ControlLevel_Task_Template
    {
        //Gets the task level type using reflection which cannot be done outside an invoked method
        string taskName = (string)Session.SessionDef.TaskMappings[configFolderName];
        verifyTask_Level.TaskLevel = GameObject.Find(taskName + "_Scripts").GetComponent<T>();
    }


    private void SetDataPaths()
    {
        Session.FilePrefix = $"Session_{DateTime.Now.ToString("MM_dd_yy__HH_mm_ss")}_{Session.SubjectID}";
        ServerManager.SetSessionDataFolder("DATA__" + Session.FilePrefix);

        if (Session.StoringDataOnServer)
            Session.SessionDataPath = ServerManager.SessionDataFolderPath;
        else if (Session.StoringDataLocally)
            Session.SessionDataPath = Session.LocateFile.GetPath("Data Folder") + Path.DirectorySeparatorChar + Session.FilePrefix;

        Session.TaskSelectionDataPath = Session.UsingLocalConfigs ? Session.SessionDataPath + Path.DirectorySeparatorChar + "TaskSelectionData" : $"{Session.SessionDataPath}/TaskSelectionData";
    }


    private IEnumerator CreateSessionDataFolder(Action<bool> callbackBool)
    {
        yield return StartCoroutine(Session.SessionLevel.SessionData.CreateFile());
        ServerManager.SessionDataFolderCreated = true;
        Session.LogWriter.StoreDataIsSet = true; //tell log writer when storeData boolean has been set (waiting until data folder is created)
        callbackBool?.Invoke(true);
    }


    private void SetupInputManagement(State inputActive, State inputInactive)
    {
        Session.EventCodeManager.splitBytes = Session.SessionDef.SplitBytes;

        Session.InputManager = new GameObject("InputManager");
        Session.InputManager.SetActive(true);

        Session.InputTrackers = Instantiate(Resources.Load<GameObject>("InputTrackers"),
            Session.InputManager.transform);
        Session.MouseTracker = Session.InputTrackers.GetComponent<MouseTracker>();
        Session.GazeTracker = Session.InputTrackers.GetComponent<GazeTracker>();

        Session.SelectionTracker = new SelectionTracker();
        if (Session.SessionDef.SelectionType.ToLower().Equals("gaze"))
        {
            Session.SessionLevel.SelectionHandler = Session.SelectionTracker.SetupSelectionHandler("session", "GazeShotgun", Session.GazeTracker, inputActive, inputInactive);
            Session.GazeTracker.enabled = true;
            Session.SessionLevel.SelectionHandler.MinDuration = 0.7f;
        }
        else if(Session.SessionDef.SelectionType.ToLower().Equals("mouseHover"))
        {
            Session.SessionLevel.SelectionHandler = Session.SelectionTracker.SetupSelectionHandler("session", "MouseHover", Session.MouseTracker, inputActive, inputInactive);
            Session.MouseTracker.enabled = true;
            Session.SessionLevel.SelectionHandler.MinDuration = 0.7f;
        }
        else
        {
            Session.SessionLevel.SelectionHandler = Session.SelectionTracker.SetupSelectionHandler("session", "MouseButton0Click", Session.MouseTracker, inputActive, inputInactive);
            Session.MouseTracker.enabled = true;
            Session.SessionLevel.SelectionHandler.MinDuration = 0.01f;
            Session.SessionLevel.SelectionHandler.MaxDuration = 2f;
        }

        Session.MouseTracker.ShotgunRaycast.SetShotgunVariables(Session.SessionDef.ShotgunRaycastCircleSize_DVA, Session.SessionDef.ParticipantDistance_CM, Session.SessionDef.ShotgunRaycastSpacing_DVA);
        Session.GazeTracker.ShotgunRaycast.SetShotgunVariables(Session.SessionDef.ShotgunRaycastCircleSize_DVA, Session.SessionDef.ParticipantDistance_CM, Session.SessionDef.ShotgunRaycastSpacing_DVA);

        

        if (Session.SessionDef.MonitorDetails != null && Session.SessionDef.ScreenDetails != null)
        {
            USE_CoordinateConverter.ScreenDetails = new ScreenDetails(
                Session.SessionDef.ScreenDetails.LowerLeft_Cm,
                Session.SessionDef.ScreenDetails.UpperRight_Cm,
                Session.SessionDef.ScreenDetails.PixelResolution);
            USE_CoordinateConverter.MonitorDetails = new MonitorDetails(
                Session.SessionDef.MonitorDetails.PixelResolution,
                Session.SessionDef.MonitorDetails.CmSize);
            USE_CoordinateConverter.SetMonitorDetails(USE_CoordinateConverter.MonitorDetails);
            USE_CoordinateConverter.SetScreenDetails(USE_CoordinateConverter.ScreenDetails);
        }
    }


    private void WriteSessionConfigsToPersistantDataPath()
    {
        if (Directory.Exists(Session.ConfigFolderPath))
            Directory.Delete(Session.ConfigFolderPath, true);
            
        Directory.CreateDirectory(Session.ConfigFolderPath);
        List<string> configsToWrite = new List<string>() { "SessionConfig_singleType", "SessionEventCodeConfig_json" };
        foreach (string config in configsToWrite)
        {
            byte[] textFileBytes = Resources.Load<TextAsset>("DefaultSessionConfigs/" + config).bytes;
            string configName = config;
            if (configName.ToLower().Contains("sessionconfig"))
                configName += ".txt";
            else if (configName.ToLower().Contains("eventcode"))
                configName += ".json";
            File.WriteAllBytes(Session.ConfigFolderPath + Path.DirectorySeparatorChar + configName, textFileBytes);
        }
    }


    private void SetupSessionDataControllers()
    {
        Session.SessionLevel.SessionData = (SessionData)Session.SessionDataControllers.InstantiateDataController<SessionData>
            ("SessionData", Session.SessionDataPath); //SessionDataControllers.InstantiateSessionData(StoreData, SessionValues.SessionDataPath);
        Session.SessionLevel.SessionData.fileName = Session.FilePrefix + "__SessionData.txt";
        Session.SessionLevel.SessionData.InitDataController();
        Session.SessionLevel.SessionData.ManuallyDefine();

        Session.TaskSelectionDataPath = Session.TaskSelectionDataPath + Path.DirectorySeparatorChar + "Task0001";

        if (Session.SessionDef.SerialPortActive)
        {
            Session.SerialSentData = (SerialSentData)Session.SessionDataControllers.InstantiateDataController<SerialSentData>
                ("SerialSentData", Session.TaskSelectionDataPath);
            Session.SerialSentData.fileName = Session.FilePrefix + "__SerialSentData_0001_TaskSelection.txt";
            Session.SerialSentData.InitDataController();
            Session.SerialSentData.ManuallyDefine();

            Session.SerialRecvData = (SerialRecvData)Session.SessionDataControllers.InstantiateDataController<SerialRecvData>
                ("SerialRecvData", Session.TaskSelectionDataPath);
            Session.SerialRecvData.fileName = Session.FilePrefix + "__SerialRecvData_0001_TaskSelection.txt";
            Session.SerialRecvData.InitDataController();
            Session.SerialRecvData.ManuallyDefine();
        }

        Session.SessionLevel.FrameData = (FrameData)Session.SessionDataControllers.InstantiateDataController<FrameData>("FrameData", "TaskSelection", Session.TaskSelectionDataPath);
        Session.SessionLevel.FrameData.fileName = Session.FilePrefix + "__FrameData_0001_TaskSelection.txt";
        Session.SessionLevel.FrameData.InitDataController();
        Session.SessionLevel.FrameData.ManuallyDefine();

        if (Session.SessionDef.EventCodesActive)
            Session.SessionLevel.FrameData.AddEventCodeColumns();
        if (Session.SessionDef.FlashPanelsActive)
            Session.SessionLevel.FrameData.AddFlashPanelColumns();

        if (Session.SessionDef.EyeTrackerActive)
        {
            Session.GazeData = (GazeData)Session.SessionDataControllers.InstantiateDataController<GazeData>
                            ("GazeData", Session.TaskSelectionDataPath);
            Session.GazeData.fileName = Session.FilePrefix + "__GazeData_0001_TaskSelection.txt";
            Session.GazeData.InitDataController();
            Session.GazeData.ManuallyDefine();
            Session.GazeTracker.Init(Session.SessionLevel.FrameData, 0);

        }

        Session.MouseTracker.Init(Session.SessionLevel.FrameData, 0);
    }
}
