using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using USE_States;
using System.IO;
using SelectionTracking;
using USE_Def_Namespace;
using USE_DisplayManagement;
using USE_ExperimentTemplate_Classes;
using USE_ExperimentTemplate_Data;
using USE_ExperimentTemplate_Session;


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
    public override void DefineControlLevel()
    {
        ImportSessionSettings = new State("ImportSessionSettings");
        CreateDataFolder = new State("CreateDataFolder");
        LoadTaskScene = new State("LoadTaskScene");
        VerifyTask = new State("VerifyTask");
        AddActiveStates(new List<State> {
            ImportSessionSettings, CreateDataFolder, LoadTaskScene, VerifyTask
        });
        
        
        ImportSessionSettings.AddChildLevel(importSettings_Level);
        ImportSessionSettings.AddDefaultInitializationMethod(() =>
        {
            SetDataPaths();
            SetConfigPathsAndTypes();
            importSettings_Level.SettingsDetails = new List<SettingsDetails>()
            {
                new SettingsDetails("SingleType", "SessionConfig", typeof(SessionDef)),
                new SettingsDetails("JSON", "EventCode", typeof(Dictionary<string, EventCode>))
            };
            if (SessionValues.WebBuild && SessionValues.UsingDefaultConfigs)
                importSettings_Level.SettingsDetails[0].FilePath = SessionValues.ConfigFolderPath;
            else
                importSettings_Level.SettingsDetails[0].FilePath = SessionValues.LocateFile.FindFilePathInExternalFolder(SessionValues.ConfigFolderPath, $"*{"SessionConfig"}*");

        });
        ImportSessionSettings.AddUpdateMethod(() =>
            {
            
                if (importSettings_Level.fileParsed)
                {
                    Debug.Log("FILE PARSED! SEARCH STRING: " + importSettings_Level.SettingsDetails[0].SearchString);
            
                    if (importSettings_Level.SettingsDetails[0].SearchString == "SessionConfig") //just parsed sessionconfig
                    {
                        //set sessiondef to the parsed content
                        SessionValues.SessionDef = (SessionDef)importSettings_Level.parsedResult;
                        
                        //determine file path of next config (event codes) based on content of sessiondef
                        if (SessionValues.WebBuild && !SessionValues.UsingDefaultConfigs) // Server
                            importSettings_Level.SettingsDetails[1].FilePath = SessionValues.ConfigFolderPath;
                        else  // Local or Default
                        {
                            string eventCodeFileString = SessionValues.LocateFile.FindFilePathInExternalFolder(SessionValues.ConfigFolderPath, "*EventCode*");
                            if (!String.IsNullOrEmpty(eventCodeFileString))
                                importSettings_Level.SettingsDetails[1].FilePath = eventCodeFileString;
                            else
                                Debug.Log(
                                    "Event Codes were not found in the config folder path. Not an issue if Event Codes are set INACTIVE.");
                        }
                    }
                    else if (importSettings_Level.SettingsDetails[0].SearchString == "EventCode") //just parsed eventcodeconfig
                    {
                        //set event codes to parsed content
                        SessionValues.EventCodeManager.SessionEventCodes = (Dictionary<string, EventCode>) importSettings_Level.parsedResult;
                    }
                    else
                        Debug.LogError($"The {importSettings_Level.SettingsDetails[0].SearchString} has been parsed, but is unable to be set as it is not a SessionConfig, EventCode, or DisplayConfig file.");
                    
                    importSettings_Level.importPaused = false;
                    settingsImported = true;
                    setupPaused = true;
                }
            });
        ImportSessionSettings.SpecifyTermination(() => ImportSessionSettings.ChildLevel.Terminated && !setupPaused, CreateDataFolder, 
            () =>
            {
                settingsImported = false;
                SetupInputManagement();
                SetupSessionDataControllers();
            });
        
        CreateDataFolder.AddDefaultInitializationMethod(() =>
        {
            dataFolderCreated = false;
            if (SessionValues.SessionDef.StoreData)
                StartCoroutine(CreateSessionDataFolder(result =>
                {
                    //StartCoroutine(CreateTaskSelectionDataFolder(result => dataFolderCreated = true));
                    dataFolderCreated = true;
                }));
            else
                dataFolderCreated = true; //set to true if not storing data so the state ends?
            //NEED TO MOVE THIS TO WHEREVER NORMAL BUILD CREATES THE TASKSELECTIONDATA FOLDER:
        });

        CreateDataFolder.SpecifyTermination(()=> dataFolderCreated && !setupPaused, LoadTaskScene);
        
        LoadTaskScene.SpecifyTermination(()=> taskSceneLoaded && !setupPaused, VerifyTask);
        
        VerifyTask.SpecifyTermination(()=> taskVerified && !setupPaused && unverifiedTasks.Count > 0, LoadTaskScene);
        VerifyTask.SpecifyTermination(()=> taskVerified && !setupPaused && unverifiedTasks.Count == 0, ()=> null);
    }
    

    private void SetDataPaths()
    {
        SessionValues.FilePrefix = "Session_" + SessionValues.SessionID + "__Subject_" + SessionValues.SubjectID + "__" + DateTime.Now.ToString("MM_dd_yy__HH_mm_ss");
        ServerManager.SetSessionDataFolder("DATA__" + SessionValues.FilePrefix);
        SessionValues.SessionDataPath = SessionValues.WebBuild ? ServerManager.SessionDataFolderPath : (SessionValues.LocateFile.GetPath("Data Folder") + Path.DirectorySeparatorChar + SessionValues.FilePrefix);
        SessionValues.TaskSelectionDataPath = SessionValues.WebBuild ? $"{SessionValues.SessionDataPath}/TaskSelectionData" : (SessionValues.SessionDataPath + Path.DirectorySeparatorChar + "TaskSelectionData");
    }
    
    
    private IEnumerator CreateSessionDataFolder(Action<bool> callbackBool)
    {
        yield return StartCoroutine(SessionData.CreateFile());
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
    
    private void SetConfigPathsAndTypes()
    {
        if (SessionValues.WebBuild)
        {
            if (SessionValues.UsingDefaultConfigs)
            {
                SessionValues.ConfigAccessType = "Default";
                SessionValues.ConfigFolderPath = Application.persistentDataPath + Path.DirectorySeparatorChar + "M_USE_DefaultConfigs";
                WriteSessionConfigsToPersistantDataPath();
            }
            else //Using Server Configs:
            {
                SessionValues.ConfigAccessType = "Server";
                SessionValues.ConfigFolderPath = ServerManager.SessionConfigFolderPath;
            }
        }
        else //Normal Build:
        {
            SessionValues.ConfigAccessType = "Local";
            SessionValues.ConfigFolderPath = SessionValues.LocateFile.GetPath("Config Folder");
        }
    }
    
    
        private void SetupInputManagement()
        {
            SessionValues.InputManager = new GameObject("InputManager");
            SessionValues.InputManager.SetActive(true);

            SessionValues.InputTrackers = Instantiate(Resources.Load<GameObject>("InputTrackers"), SessionValues.InputManager.transform);
            SessionValues.MouseTracker = SessionValues.InputTrackers.GetComponent<MouseTracker>();
            SessionValues.GazeTracker = SessionValues.InputTrackers.GetComponent<GazeTracker>();

            SessionValues.SelectionTracker = new SelectionTracker();
           

            if (SessionValues.SessionDef.EyeTrackerActive)
            {
                if (GameObject.Find("TobiiEyeTrackerController") == null)
                {
                    // gets called once when finding and creating the tobii eye tracker prefabs
                    GameObject TobiiEyeTrackerControllerGO = new GameObject("TobiiEyeTrackerController");
                    SessionValues.TobiiEyeTrackerController = TobiiEyeTrackerControllerGO.AddComponent<TobiiEyeTrackerController>();
                    GameObject TrackBoxGO = Instantiate(Resources.Load<GameObject>("TrackBoxGuide"), TobiiEyeTrackerControllerGO.transform);
                    GameObject EyeTrackerGO = Instantiate(Resources.Load<GameObject>("EyeTracker"), TobiiEyeTrackerControllerGO.transform);
                    GameObject CalibrationGO = Instantiate(Resources.Load<GameObject>("GazeCalibration"));
                    SessionValues.GazeTracker.enabled = true;


                    GameObject GazeTrail = Instantiate(Resources.Load<GameObject>("GazeTrail"), TobiiEyeTrackerControllerGO.transform); 
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
                USE_CoordinateConverter.ScreenDetails = new ScreenDetails(SessionValues.SessionDef.ScreenDetails.LowerLeft_Cm, SessionValues.SessionDef.ScreenDetails.UpperRight_Cm, SessionValues.SessionDef.ScreenDetails.PixelResolution);
                USE_CoordinateConverter.MonitorDetails = new MonitorDetails(SessionValues.SessionDef.MonitorDetails.PixelResolution, SessionValues.SessionDef.MonitorDetails.CmSize);
                USE_CoordinateConverter.SetMonitorDetails(USE_CoordinateConverter.MonitorDetails);
                USE_CoordinateConverter.SetScreenDetails(USE_CoordinateConverter.ScreenDetails);
            }}
    private void WriteSessionConfigsToPersistantDataPath()
    {
        if (Directory.Exists(SessionValues.ConfigFolderPath ))
            Directory.Delete(SessionValues.ConfigFolderPath , true);

        if (!Directory.Exists(SessionValues.ConfigFolderPath ))
        {
            Directory.CreateDirectory(SessionValues.ConfigFolderPath );
            List<string> configsToWrite = new List<string>() { "SessionConfig_singleType", "EventCodeConfig_json", "DisplayConfig_json" };
            foreach (string config in configsToWrite)
            {
                byte[] textFileBytes = Resources.Load<TextAsset>("DefaultSessionConfigs/" + config).bytes;
                string configName = config;
                if (configName.ToLower().Contains("sessionconfig"))
                    configName += ".txt";
                else if (configName.ToLower().Contains("eventcode") || configName.ToLower().Contains("displayconfig"))
                    configName += ".json";
                File.WriteAllBytes(SessionValues.ConfigFolderPath  + Path.DirectorySeparatorChar + configName, textFileBytes);

                Debug.Log("WROTE " + configName + " TO PERSISTANT PATH!");
            }
        }
    }
    
    
    
    // private void SetValuesForLoading_EventCodeConfig()
    // {
    //     // Add necessary fields to Load Session Event Codes from ImportSettings_Level
    //     importSettings_Level.SettingsDetails.SettingParsingStyle = "JSON";
    //     importSettings_Level.SettingsDetails.SettingType = typeof(Dictionary<string, EventCode>);
    //     importSettings_Level.SettingsDetails.SearchString = "EventCode";
    //
    //     if (SessionValues.WebBuild && !SessionValues.UsingDefaultConfigs) // Server
    //         importSettings_Level.SettingsDetails.FilePath = SessionValues.ConfigFolderPath;
    //     else  // Local or Default
    //     {
    //         string eventCodeFileString = SessionValues.LocateFile.FindFilePathInExternalFolder(SessionValues.ConfigFolderPath, "*EventCode*");
    //         if (!String.IsNullOrEmpty(eventCodeFileString))
    //             importSettings_Level.SettingsDetails.FilePath = eventCodeFileString;
    //         else
    //             Debug.Log(
    //                 "Event Codes were not found in the config folder path. Not an issue if Event Codes are set INACTIVE.");
    //     }
    // }
    
    
        private void SetupSessionDataControllers()
        {
            SessionData = (SessionData)SessionValues.SessionDataControllers.InstantiateDataController<SessionData>
                ("SessionData", SessionValues.SessionDef.StoreData, SessionValues.SessionDataPath); //SessionDataControllers.InstantiateSessionData(StoreData, SessionValues.SessionDataPath);
            SessionData.fileName = SessionValues.FilePrefix + "__SessionData.txt";
            SessionData.sessionLevel = SessionLevel;
            SessionData.InitDataController();
            SessionData.ManuallyDefine();

            if (SessionValues.SessionDef.SerialPortActive)
            {
                SessionValues.SerialSentData = (SerialSentData)SessionValues.SessionDataControllers.InstantiateDataController<SerialSentData>
                    ("SerialSentData", SessionValues.SessionDef.StoreData, SessionValues.SessionDataPath + Path.DirectorySeparatorChar + "SerialSentData"
                                                  + Path.DirectorySeparatorChar + "0001_TaskSelection");
                SessionValues.SerialSentData.fileName = SessionValues.FilePrefix + "__SerialSentData_0001_TaskSelection.txt";
                SessionValues.SerialSentData.sessionLevel = SessionLevel;
                SessionValues.SerialSentData.InitDataController();
                SessionValues.SerialSentData.ManuallyDefine();

                SessionValues.SerialRecvData = (SerialRecvData)SessionValues.SessionDataControllers.InstantiateDataController<SerialRecvData>
                    ("SerialRecvData", SessionValues.SessionDef.StoreData, SessionValues.SessionDataPath + Path.DirectorySeparatorChar + "SerialRecvData"
                                                                           + Path.DirectorySeparatorChar + "0001_TaskSelection");
                SessionValues.SerialRecvData.fileName = SessionValues.FilePrefix + "__SerialRecvData_0001_TaskSelection.txt";
                SessionValues.SerialRecvData.sessionLevel = SessionLevel;
                SessionValues.SerialRecvData.InitDataController();
                SessionValues.SerialRecvData.ManuallyDefine();
            }

            FrameData = (FrameData)SessionValues.SessionDataControllers.InstantiateDataController<FrameData>("FrameData", "TaskSelection", SessionValues.SessionDef.StoreData, SessionValues.TaskSelectionDataPath + Path.DirectorySeparatorChar + "FrameData");
            FrameData.fileName = "TaskSelection__FrameData.txt";
            FrameData.sessionLevel = SessionLevel;
            FrameData.InitDataController();
            FrameData.ManuallyDefine();

            if (SessionValues.SessionDef.EventCodesActive)
                FrameData.AddEventCodeColumns();
            if (SessionValues.SessionDef.FlashPanelsActive)
                FrameData.AddFlashPanelColumns();

            if (SessionValues.SessionDef.EyeTrackerActive)
            {
                SessionValues.GazeData = (GazeData)SessionValues.SessionDataControllers.InstantiateDataController<GazeData>("GazeData", "TaskSelection", SessionValues.SessionDef.StoreData, SessionValues.TaskSelectionDataPath + Path.DirectorySeparatorChar + "GazeData");

                SessionValues.GazeData.fileName = "TaskSelection__GazeData.txt";
                SessionValues.GazeData.sessionLevel = SessionLevel;
                SessionValues.GazeData.InitDataController();
                SessionValues.GazeData.ManuallyDefine();
                SessionValues.TobiiEyeTrackerController.GazeData = SessionValues.GazeData;
                SessionValues.GazeTracker.Init(FrameData, 0);

            }
            SessionValues.MouseTracker.Init(FrameData, 0);
        }
}

