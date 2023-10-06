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
    public ControlLevel_Session_Template SessionLevel;
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
            if (SessionValues.UsingDefaultConfigs)
                WriteSessionConfigsToPersistantDataPath();

            importSettings_Level.SettingsDetails = new List<SettingsDetails>()
            {
                new SettingsDetails(SessionValues.ConfigFolderPath, "SingleType", "SessionConfig", typeof(SessionDef)),
                new SettingsDetails(SessionValues.ConfigFolderPath, "JSON", "SessionEventCode", typeof(Dictionary<string, EventCode>))
            };
        });
        ImportSessionSettings.AddUpdateMethod(() =>
        {
            if (importSettings_Level.fileParsed)
            {
                Debug.Log(importSettings_Level.SettingsDetails[0].SearchString + " PARSED!");

                if (importSettings_Level.SettingsDetails[0].SearchString == "SessionConfig")
                {
                    SessionValues.SessionDef = (SessionDef)importSettings_Level.parsedResult;  //set sessiondef to the parsed content
                }
                else if (importSettings_Level.SettingsDetails[0].SearchString == "SessionEventCode")
                {
                    SessionValues.EventCodeManager.SessionEventCodes = (Dictionary<string, EventCode>)importSettings_Level.parsedResult;  //set event codes to parsed content
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
                SetupInputManagement(SessionLevel.selectTask, SessionLevel.loadTask);
                SetupSessionDataControllers();
            });

        CreateDataFolder.AddDefaultInitializationMethod(() =>
        {
            dataFolderCreated = false;
            if (SessionValues.StoreData)
                StartCoroutine(CreateSessionDataFolder(result =>
                {
                    dataFolderCreated = true;
                }));
            else
                dataFolderCreated = true; //set to true if not storing data so the state ends?
            //NEED TO MOVE THIS TO WHEREVER NORMAL BUILD CREATES THE TASKSELECTIONDATA FOLDER:
        });

        CreateDataFolder.SpecifyTermination(() => dataFolderCreated && !setupPaused, LoadTaskScene);

        int iTask = 0;
        string taskName = "";
        AsyncOperation loadScene = null;

        LoadTaskScene.AddSpecificInitializationMethod(() =>
        {
            taskSceneLoaded = false;
            taskName = (string)SessionValues.SessionDef.TaskMappings[iTask];
            loadScene = SceneManager.LoadSceneAsync(taskName, LoadSceneMode.Additive);
            string configFolderName = SessionValues.SessionDef.TaskMappings.Cast<DictionaryEntry>().ElementAt(iTask).Key.ToString();
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

            string configFolderName = SessionValues.SessionDef.TaskMappings.Cast<DictionaryEntry>().ElementAt(iTask).Key.ToString();

            GetTaskLevelType.Invoke(this, new object[] { configFolderName, verifyTask_Level });

            verifyTask_Level.TaskLevel.TaskConfigPath = SessionValues.ConfigFolderPath + "/" + configFolderName;
        });

        VerifyTask.SpecifyTermination(() => verifyTask_Level.Terminated && !setupPaused && iTask < SessionValues.SessionDef.TaskMappings.Count - 1, LoadTaskScene,
            () =>
            {
                SessionLevel.ActiveTaskLevels.Add(taskLevel);
                SceneManager.UnloadSceneAsync(taskName);
                iTask++;
            });
        VerifyTask.SpecifyTermination(() => verifyTask_Level.Terminated && !setupPaused && iTask == SessionValues.SessionDef.TaskMappings.Count - 1, () => null, () =>
        {
            ParentState.ChildLevel = null;
            SceneManager.UnloadSceneAsync(taskName);
        });
    }


    public void GetTaskLevelType<T>(string configFolderName, VerifyTask_Level verifyTask_Level) where T : ControlLevel_Task_Template
    {
        //Gets the task level type using reflection which cannot be done outside an invoked method
        string taskName = (string)SessionValues.SessionDef.TaskMappings[configFolderName];
        verifyTask_Level.TaskLevel = GameObject.Find(taskName + "_Scripts").GetComponent<T>();
    }


    private void SetDataPaths()
    {
        SessionValues.FilePrefix = $"Session_{DateTime.Now.ToString("MM_dd_yy__HH_mm_ss")}_{SessionValues.SubjectID}";
        ServerManager.SetSessionDataFolder("DATA__" + SessionValues.FilePrefix);

        if (SessionValues.StoringDataOnServer)
            SessionValues.SessionDataPath = ServerManager.SessionDataFolderPath;
        else if (SessionValues.StoringDataLocally)
            SessionValues.SessionDataPath = SessionValues.LocateFile.GetPath("Data Folder") + Path.DirectorySeparatorChar + SessionValues.FilePrefix;

        SessionValues.TaskSelectionDataPath = SessionValues.UsingLocalConfigs ? SessionValues.SessionDataPath + Path.DirectorySeparatorChar + "TaskSelectionData" : $"{SessionValues.SessionDataPath}/TaskSelectionData";
    }


    private IEnumerator CreateSessionDataFolder(Action<bool> callbackBool)
    {
        yield return StartCoroutine(SessionLevel.SessionData.CreateFile());
        ServerManager.SessionDataFolderCreated = true;
        SessionLevel.LogWriter.StoreDataIsSet = true; //tell log writer when storeData boolean has been set (waiting until data folder is created)
        callbackBool?.Invoke(true);
    }

    private IEnumerator CreateTaskSelectionDataFolder(Action<bool> callbackBool)
    {
        if (SessionValues.WebBuild)
            yield return StartCoroutine(ServerManager.CreateFolder(SessionValues.TaskSelectionDataPath));
        else
        {
        }

        callbackBool?.Invoke(true);
    }

    private void SetupInputManagement(State inputActive, State inputInactive)
    {
        SessionValues.EventCodeManager.splitBytes = SessionValues.SessionDef.SplitBytes;

        SessionValues.InputManager = new GameObject("InputManager");
        SessionValues.InputManager.SetActive(true);

        SessionValues.InputTrackers = Instantiate(Resources.Load<GameObject>("InputTrackers"),
            SessionValues.InputManager.transform);
        SessionValues.MouseTracker = SessionValues.InputTrackers.GetComponent<MouseTracker>();
        SessionValues.GazeTracker = SessionValues.InputTrackers.GetComponent<GazeTracker>();

        SessionValues.SelectionTracker = new SelectionTracker();
        if (SessionValues.SessionDef.SelectionType.ToLower().Equals("gaze"))
        {
            SessionLevel.SelectionHandler = SessionValues.SelectionTracker.SetupSelectionHandler("session", "GazeSelection", SessionValues.GazeTracker, inputActive, inputInactive);
            SessionLevel.SelectionHandler.MinDuration = 0.7f;
        }
        else
        {
            SessionLevel.SelectionHandler = SessionValues.SelectionTracker.SetupSelectionHandler("session", "MouseButton0Click", SessionValues.MouseTracker, inputActive, inputInactive);
            SessionValues.MouseTracker.enabled = true;
            SessionLevel.SelectionHandler.MinDuration = 0.01f;
            SessionLevel.SelectionHandler.MaxDuration = 2f;
        }

        SessionValues.MouseTracker.ShotgunRaycast.SetShotgunVariables(SessionValues.SessionDef.ShotgunRayCastCircleSize_DVA, SessionValues.SessionDef.ParticipantDistance_CM, SessionValues.SessionDef.ShotgunRaycastSpacing_DVA);
        SessionValues.GazeTracker.ShotgunRaycast.SetShotgunVariables(SessionValues.SessionDef.ShotgunRayCastCircleSize_DVA, SessionValues.SessionDef.ParticipantDistance_CM, SessionValues.SessionDef.ShotgunRaycastSpacing_DVA);

        if (SessionValues.SessionDef.EyeTrackerActive)
        {
            if (GameObject.Find("TobiiEyeTrackerController") == null)
            {
                // gets called once when finding and creating the tobii eye tracker prefabs
                GameObject TobiiEyeTrackerControllerGO = new GameObject("TobiiEyeTrackerController");
                SessionValues.TobiiEyeTrackerController =
                    TobiiEyeTrackerControllerGO.AddComponent<TobiiEyeTrackerController>();
                GameObject TrackBoxGO = Instantiate(Resources.Load<GameObject>("TrackBoxGuide"),
                    TobiiEyeTrackerControllerGO.transform);
                GameObject EyeTrackerGO = Instantiate(Resources.Load<GameObject>("EyeTracker"),
                    TobiiEyeTrackerControllerGO.transform);
                GameObject CalibrationGO = Instantiate(Resources.Load<GameObject>("GazeCalibration"));
                SessionValues.GazeTracker.enabled = true;


                GameObject GazeTrail = Instantiate(Resources.Load<GameObject>("GazeTrail"),
                    TobiiEyeTrackerControllerGO.transform);
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.SetParent(TobiiEyeTrackerControllerGO.transform, true);
                // Position and scale the cube as desired
                cube.transform.position = new Vector3(0f, 1f, 60f);
                cube.transform.localScale = new Vector3(106f, 62f, 0.1f);
                cube.SetActive(false);

            }
        }

        if (SessionValues.SessionDef.MonitorDetails != null && SessionValues.SessionDef.ScreenDetails != null)
        {
            USE_CoordinateConverter.ScreenDetails = new ScreenDetails(
                SessionValues.SessionDef.ScreenDetails.LowerLeft_Cm,
                SessionValues.SessionDef.ScreenDetails.UpperRight_Cm,
                SessionValues.SessionDef.ScreenDetails.PixelResolution);
            USE_CoordinateConverter.MonitorDetails = new MonitorDetails(
                SessionValues.SessionDef.MonitorDetails.PixelResolution,
                SessionValues.SessionDef.MonitorDetails.CmSize);
            USE_CoordinateConverter.SetMonitorDetails(USE_CoordinateConverter.MonitorDetails);
            USE_CoordinateConverter.SetScreenDetails(USE_CoordinateConverter.ScreenDetails);
        }
    }


    private void WriteSessionConfigsToPersistantDataPath()
    {
        if (Directory.Exists(SessionValues.ConfigFolderPath))
            Directory.Delete(SessionValues.ConfigFolderPath, true);
            
        Directory.CreateDirectory(SessionValues.ConfigFolderPath);
        List<string> configsToWrite = new List<string>() { "SessionConfig_singleType", "SessionEventCodeConfig_json", "DisplayConfig_json" };
        foreach (string config in configsToWrite)
        {
            byte[] textFileBytes = Resources.Load<TextAsset>("DefaultSessionConfigs/" + config).bytes;
            string configName = config;
            if (configName.ToLower().Contains("sessionconfig"))
                configName += ".txt";
            else if (configName.ToLower().Contains("eventcode") || configName.ToLower().Contains("displayconfig"))
                configName += ".json";
            File.WriteAllBytes(SessionValues.ConfigFolderPath + Path.DirectorySeparatorChar + configName, textFileBytes);
        }
    }


    private void SetupSessionDataControllers()
    {
        SessionLevel.SessionData = (SessionData)SessionValues.SessionDataControllers.InstantiateDataController<SessionData>
            ("SessionData", SessionValues.StoreData, SessionValues.SessionDataPath); //SessionDataControllers.InstantiateSessionData(StoreData, SessionValues.SessionDataPath);
        SessionLevel.SessionData.fileName = SessionValues.FilePrefix + "__SessionData.txt";
        SessionLevel.SessionData.sessionLevel = SessionLevel;
        SessionLevel.SessionData.InitDataController();
        SessionLevel.SessionData.ManuallyDefine();

        if (SessionValues.SessionDef.SerialPortActive)
        {
            SessionValues.SerialSentData = (SerialSentData)SessionValues.SessionDataControllers.InstantiateDataController<SerialSentData>
                ("SerialSentData", SessionValues.StoreData, SessionValues.TaskSelectionDataPath 
                                                + Path.DirectorySeparatorChar + "Task0001");
            SessionValues.SerialSentData.fileName = SessionValues.FilePrefix + "__SerialSentData_0001_TaskSelection.txt";
            SessionValues.SerialSentData.sessionLevel = SessionLevel;
            SessionValues.SerialSentData.InitDataController();
            SessionValues.SerialSentData.ManuallyDefine();

            SessionValues.SerialRecvData = (SerialRecvData)SessionValues.SessionDataControllers.InstantiateDataController<SerialRecvData>
                ("SerialRecvData", SessionValues.StoreData, SessionValues.TaskSelectionDataPath
                                                                       + Path.DirectorySeparatorChar + "Task0001" );
            SessionValues.SerialRecvData.fileName = SessionValues.FilePrefix + "__SerialRecvData_0001_TaskSelection.txt";
            SessionValues.SerialRecvData.sessionLevel = SessionLevel;
            SessionValues.SerialRecvData.InitDataController();
            SessionValues.SerialRecvData.ManuallyDefine();
        }

        SessionLevel.FrameData = (FrameData)SessionValues.SessionDataControllers.InstantiateDataController<FrameData>("FrameData", "TaskSelection", SessionValues.StoreData, SessionValues.TaskSelectionDataPath + Path.DirectorySeparatorChar + "Task0001");
        SessionLevel.FrameData.fileName = SessionValues.FilePrefix + "__FrameData_0001_TaskSelection.txt";
        SessionLevel.FrameData.sessionLevel = SessionLevel;
        SessionLevel.FrameData.InitDataController();
        SessionLevel.FrameData.ManuallyDefine();

        if (SessionValues.SessionDef.EventCodesActive)
            SessionLevel.FrameData.AddEventCodeColumns();
        if (SessionValues.SessionDef.FlashPanelsActive)
            SessionLevel.FrameData.AddFlashPanelColumns();

        if (SessionValues.SessionDef.EyeTrackerActive)
        {
            SessionValues.GazeData = (GazeData)SessionValues.SessionDataControllers.InstantiateDataController<GazeData>("GazeData", "TaskSelection", SessionValues.StoreData, SessionValues.TaskSelectionDataPath + Path.DirectorySeparatorChar + "Task0001");

            SessionValues.GazeData.fileName = "TaskSelection__GazeData.txt";
            SessionValues.GazeData.sessionLevel = SessionLevel;
            SessionValues.GazeData.InitDataController();
            SessionValues.GazeData.ManuallyDefine();
          //  SessionValues.TobiiEyeTrackerController.GazeData = SessionValues.GazeData;
            SessionValues.GazeTracker.Init(SessionLevel.FrameData, 0);

        }
        SessionValues.MouseTracker.Init(SessionLevel.FrameData, 0);
    }
}
