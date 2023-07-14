using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using USE_UI;
using USE_States;
using USE_Settings;
using USE_ExperimenterDisplay;
using USE_ExperimentTemplate_Classes;
using USE_ExperimentTemplate_Data;
using USE_ExperimentTemplate_Task;
using SelectionTracking;
using TMPro;
using USE_Def_Namespace;
using System.Runtime.InteropServices;
#if (!UNITY_WEBGL)
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
#endif
using USE_DisplayManagement;
//using UnityEngine.Windows.WebCam;


namespace USE_ExperimentTemplate_Session
{
    public class ControlLevel_Session_Template : ControlLevel
    {
        [HideInInspector] public int SessionId_SQL;

        //private bool IsHuman;
        //private SessionDef SessionDef;
        // [HideInInspector] public float ParticipantDistance_CM;
        // [HideInInspector] public float ShotgunRaycastSpacing_DVA;
        // [HideInInspector] public float ShotgunRaycastCircleSize_DVA;

        [HideInInspector] public bool TasksFinished;

        protected SummaryData SummaryData;
        protected SessionData SessionData;
      //  private SessionDataControllers SessionDataControllers;

       // protected SerialSentData SerialSentData;
        //protected SerialRecvData SerialRecvData;
        //private bool StoreData;
       // private bool MacMainDisplayBuild;
      //  [HideInInspector] public string SubjectID, SessionID, FilePrefix;
       // public string SessionLevelDataPath;
        public string TaskSelectionSceneName;

        protected List<ControlLevel_Task_Template> ActiveTaskLevels;
        public ControlLevel_Task_Template CurrentTask;
        public ControlLevel_Task_Template GazeCalibrationTaskLevel;
        //private OrderedDictionary TaskMappings;
       // private string ContextExternalFilePath;
       // private string TaskIconsFolderPath;
      //  [HideInInspector]public Vector3[] TaskIconLocations;
       // private Dictionary<string, string> TaskIcons;
        protected int taskCount;
       // private float TaskSelectionTimeout;

      //  [HideInInspector] public int RewardHotKeyPulseSize;
      //  [HideInInspector] public int RewardHotKeyNumPulses;

        //For Loading config information
        public SessionDetails SessionDetails;
        public LocateFile LocateFile;

    //    private SerialPortThreaded SerialPortController;
       // private SyncBoxController SyncBoxController;
     //   private EventCodeManager EventCodeManager;
     //   [HideInInspector] public SelectionTracker SelectionTracker;
        private SelectionTracker.SelectionHandler SelectionHandler;
       // private GameObject InputManager;
       // private MouseTracker MouseTracker;
       // private GazeTracker GazeTracker;
        private GameObject InputTrackers;
        protected FrameData FrameData;
        //protected GazeData GazeData;
        
        // EyeTracker Variables
       // public TobiiEyeTrackerController TobiiEyeTrackerController;
       // private MonitorDetails MonitorDetails;
       // private ScreenDetails ScreenDetails;
    //    public EyeTrackerData_Namespace.TobiiGazeSample TobiiGazeSample;


        private Camera SessionCam;
        private Camera MirrorCam;
       // private ExperimenterDisplayController ExperimenterDisplayController;
        [HideInInspector] public RenderTexture CameraMirrorTexture;

       // private string configFileFolder;
       private bool TaskSceneLoaded, SceneLoading;
       //private bool GuidedTaskSelection, EyeTrackerActive;

        //private bool SerialPortActive, EventCodesActive, RewardPulsesActive, SonicationActive;
       // private bool SerialPortActive;
       // private string EyetrackerType, SelectionType;
       // private Dictionary<string, EventCode> SessionEventCodes;
        private List<string> selectedConfigsList = new List<string>();
       // private SessionInfoPanel SessionInfoPanel;
        public StringBuilder PreviousTaskSummaryString = new StringBuilder();

        //  [HideInInspector] public DisplayController DisplayController;

        [HideInInspector] public GameObject TaskButtonsContainer;

        //Set in inspector
        public GameObject BlockResults_GridElementPrefab;
        public GameObject BlockResultsPrefab;
        public GameObject TaskSelection_Starfield;
        public GameObject HumanVersionToggleButton;
        public GameObject HumanStartPanelPrefab;
        public GameObject TaskSelectionCanvasGO;
        public GameObject ToggleAudioButton;
        public GameObject StartButtonPrefabGO;
        public AudioClip BackgroundMusic_AudioClip;
        public AudioClip BlockResults_AudioClip;

        [HideInInspector] public float audioPlaybackSpot;
        [HideInInspector] public AudioSource BackgroundMusic_AudioSource;


        public override void LoadSettings()
        {
            SessionValues.HumanStartPanel = gameObject.AddComponent<HumanStartPanel>();
            SessionValues.HumanStartPanel.SetSessionLevel(this);
            SessionValues.HumanStartPanel.HumanStartPanelPrefab = HumanStartPanelPrefab;

            SessionValues.USE_StartButton = gameObject.AddComponent<USE_StartButton>();
            SessionValues.USE_StartButton.StartButtonPrefab = StartButtonPrefabGO;

            SessionValues.TaskSelectionCanvasGO = TaskSelectionCanvasGO;
            if (!SessionValues.WebBuild)
                HumanVersionToggleButton.SetActive(false);


            // Set the name of the data file given input into init screen
            
            SessionValues.SubjectID = SessionDetails.GetItemValue("SubjectID");
            SessionValues.SessionID = SessionDetails.GetItemValue("SessionID");

            
            string sessionDataFolder = ServerManager.GetSessionDataFolder();
            if(!string.IsNullOrEmpty(sessionDataFolder))
                SessionValues.FilePrefix = sessionDataFolder.Split(new string[] { "__" }, 2, StringSplitOptions.None)[1];
            else
                SessionValues.FilePrefix = "Session_" + SessionValues.SessionID + "__Subject_" + SessionValues.SubjectID + "__" + DateTime.Now.ToString("MM_dd_yy__HH_mm_ss");


            if (SessionValues.WebBuild)
            {
                SessionValues.SessionDataPath = ServerManager.SessionDataFolderPath;

                if (SessionValues.UsingDefaultConfigs)
                {
                    SessionValues.ConfigAccessType = "Default";
                    SessionValues.ConfigFolderPath = Application.persistentDataPath + Path.DirectorySeparatorChar + "M_USE_DefaultConfigs";

                    WriteSessionConfigsToPersistantDataPath();

                    StartCoroutine(SessionValues.BetterReadSettingsFile<SessionDef>("SessionConfig", "SingleTypeDelimited", settingsArray =>
                    {
                        if (settingsArray != null)
                        {
                            SessionValues.SessionDef = settingsArray[0];

                            SessionValues.SessionDef.ContextExternalFilePath = "DefaultResources/Contexts"; //TEMPORARILY HAVING WEB BUILD USE DEFAUULT CONTEXTS
                            SessionValues.SessionDef.TaskIconsFolderPath = "DefaultResources/TaskIcons";
                            LoadSessionConfigSettings();
                            DefineControlLevel();
                        }
                        else
                            Debug.Log("TRIED TO READ SESSION CONFIG BUT THE COROUTINE RESULT IS NULL!");
                    }));

                }
                else //Using Server Configs:
                {
                    SessionValues.ConfigAccessType = "Server";
                    SessionValues.ConfigFolderPath = ServerManager.SessionConfigFolderPath;

                    StartCoroutine(ServerManager.GetFileStringAsync(SessionValues.ConfigFolderPath, "SessionConfig", result =>
                    {
                        if (!string.IsNullOrEmpty(result))
                        {
                            StartCoroutine(SessionValues.BetterReadSettingsFile<SessionDef>("SessionConfig", "SingleTypeDelimited", settingsArray =>
                            {
                                if (settingsArray != null)
                                {
                                    SessionValues.SessionDef = settingsArray[0];
                                    SessionValues.SessionDef.ContextExternalFilePath = "DefaultResources/Contexts"; //TEMPORARILY HAVING WEB BUILD USE DEFAUULT CONTEXTS
                                    //SessionValues.SessionDef.ContextExternalFilePath = "Resources/Contexts"; //path from root server folder
                                    SessionValues.SessionDef.TaskIconsFolderPath = "Resources/TaskIcons";
                                    LoadSessionConfigSettings();
                                    DefineControlLevel();
                                }
                                else
                                    Debug.Log("SETTINGS ARRAY IS NULL!");
                            }));
                        }
                        else
                            Debug.Log("SESSION CONFIG COROUTINE RESULT IS EMPTY!!!");
                    }));
                }
            }
            else //Normal Build:
            {
                SessionValues.ConfigAccessType = "Local";
                SessionValues.ConfigFolderPath = SessionValues.LocateFile.GetPath("Config Folder");
                SessionValues.SessionDataPath = SessionValues.LocateFile.GetPath("Data Folder") + Path.DirectorySeparatorChar + SessionValues.FilePrefix;
                StartCoroutine(SessionValues.BetterReadSettingsFile<SessionDef>("SessionConfig", "SingleTypeDelimited", settingsArray =>
                {
                    SessionValues.SessionDef = settingsArray[0];
                    LoadSessionConfigSettings();
                    DefineControlLevel();
                }));
            }

        }


        public override void DefineControlLevel()
        {
            //DontDestroyOnLoad(gameObject);
            State setupSession = new State("SetupSession");
            State selectTask = new State("SelectTask");
            State loadTask = new State("LoadTask");
            State runTask = new State("RunTask");
            State finishSession = new State("FinishSession");
            State gazeCalibration = new State("GazeCalibration");
            AddActiveStates(new List<State> { setupSession, selectTask, loadTask, runTask, finishSession, gazeCalibration });

            SessionValues.SessionDataControllers = new SessionDataControllers(GameObject.Find("DataControllers"));
            ActiveTaskLevels = new List<ControlLevel_Task_Template>();//new Dictionary<string, ControlLevel_Task_Template>();

            SessionCam = Camera.main;


#if (UNITY_WEBGL)            
            //If WebGL Build, immedietely load taskselection screen and set initCam inactive. Otherwise create ExperimenterDisplay
            GameObject initCamGO = GameObject.Find("InitCamera");
            initCamGO.SetActive(false);
#else

            TaskSelection_Starfield.SetActive(false);
            GameObject experimenterDisplay = Instantiate(Resources.Load<GameObject>("Default_ExperimenterDisplay"));
            experimenterDisplay.name = "ExperimenterDisplay";
            SessionValues.ExperimenterDisplayController = experimenterDisplay.AddComponent<ExperimenterDisplayController>();
            experimenterDisplay.AddComponent<PreserveObject>();
            SessionValues.ExperimenterDisplayController.InitializeExperimenterDisplay(this, experimenterDisplay);

            GameObject mirrorCamGO = new GameObject("MirrorCamera");
            Camera MirrorCam = mirrorCamGO.AddComponent<Camera>();
            MirrorCam.CopyFrom(Camera.main);
            MirrorCam.cullingMask = 0;

            RawImage mainCameraCopy_Image = GameObject.Find("MainCameraCopy").GetComponent<RawImage>();

#endif

            // Create the input tracker object
            SessionValues.InputManager = new GameObject("InputManager");
            SessionValues.InputManager.SetActive(true);

            InputTrackers = Instantiate(Resources.Load<GameObject>("InputTrackers"), SessionValues.InputManager.transform);
            SessionValues.MouseTracker = InputTrackers.GetComponent<MouseTracker>();
            SessionValues.GazeTracker = InputTrackers.GetComponent<GazeTracker>();

            SessionValues.SelectionTracker = new SelectionTracker();
            if (SessionValues.SessionDef.SelectionType.ToLower().Equals("gaze"))
            {
                SelectionHandler = SessionValues.SelectionTracker.SetupSelectionHandler("session", "GazeSelection", SessionValues.GazeTracker, selectTask, loadTask);
                SelectionHandler.MinDuration = 0.7f;
            }
            else
            {
                SelectionHandler = SessionValues.SelectionTracker.SetupSelectionHandler("session", "MouseButton0Click", SessionValues.MouseTracker, selectTask, loadTask);
                SessionValues.MouseTracker.enabled = true;
                SelectionHandler.MinDuration = 0.01f;
                SelectionHandler.MaxDuration = 2f;
            }

            if (SessionValues.SessionDef.EyeTrackerActive)
            {
                if (GameObject.Find("TobiiEyeTrackerController") == null)
                {
                    // gets called once when finding and creating the tobii eye tracker prefabs
                    GameObject TobiiEyeTrackerControllerGO = new GameObject("TobiiEyeTrackerController");
                    SessionValues.TobiiEyeTrackerController = TobiiEyeTrackerControllerGO.AddComponent<TobiiEyeTrackerController>();
                    GameObject TrackBoxGO = Instantiate(Resources.Load<GameObject>("TrackBoxGuide"), TobiiEyeTrackerControllerGO.transform);
                    GameObject EyeTrackerGO = Instantiate(Resources.Load<GameObject>("EyeTracker"), TobiiEyeTrackerControllerGO.transform);
                    GameObject GazeTrail = Instantiate(Resources.Load<GameObject>("GazeTrail"), TobiiEyeTrackerControllerGO.transform);

                    GameObject CalibrationGO = Instantiate(Resources.Load<GameObject>("GazeCalibration"));
                    SessionValues.GazeTracker.enabled = true;


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
            }



            bool waitForSerialPort = false;
            bool taskAutomaticallySelected = false;
            setupSession.AddDefaultInitializationMethod(() =>
            {
                StartCoroutine(SessionData.CreateFile());

                //Create Session Settings folder inside Data Folder: ----------------------------------------------------------------------------------------
                if (SessionValues.WebBuild)
                {
                    if (!Application.isEditor) //DOESNT CURRENTLY WORK FOR DEFAULT CONFIGS CUZ THATS NOT A CONFIG ON THE SERVER, so it cant find it to copy from
                    {
                        StartCoroutine(CreateFolderOnServer(SessionValues.SessionDataPath + Path.DirectorySeparatorChar + "SessionSettings", () =>
                        {
                            StartCoroutine(CopySessionConfigFolderToDataFolder()); //Copy Session Config folder to Data folder so that the settings are stored:
                        }));
                    }
                }
                else
                {
                    string sessionSettingsFolderPath = SessionValues.SessionDataPath + Path.DirectorySeparatorChar + "SessionSettings";
                    Directory.CreateDirectory(sessionSettingsFolderPath);
                    SessionSettings.StoreSettings(sessionSettingsFolderPath + Path.DirectorySeparatorChar);
                }

                SessionValues.EventCodeManager = GameObject.Find("MiscScripts").GetComponent<EventCodeManager>(); //new EventCodeManager();
                if (SessionValues.SessionDef.SerialPortActive)
                {

                    SessionValues.SerialPortController = new SerialPortThreaded();
                    if (SessionValues.SessionDef.SyncBoxActive)
                    {
                        SessionValues.SyncBoxController = new SyncBoxController();
                        SessionValues.SyncBoxController.serialPortController = SessionValues.SerialPortController;
                        SessionValues.SerialSentData.sc = SessionValues.SerialPortController;
                        SessionValues.SerialRecvData.sc = SessionValues.SerialPortController;
                    }

                    if (SessionValues.SessionDef.EventCodesActive)
                    {
                        SessionValues.EventCodeManager.SyncBoxController = SessionValues.SyncBoxController;
                        SessionValues.EventCodeManager.codesActive = true;
                    }
                    waitForSerialPort = true;

                    SessionValues.SerialPortController.SerialPortAddress = SessionValues.SessionDef.SerialPortAddress;
                    SessionValues.SerialPortController.SerialPortSpeed = SessionValues.SessionDef.SerialPortSpeed;
                    
                    // if (SessionValues.SerialPortController.SerialPortAddress == null)
                    //     SessionValues.SerialPortController.SerialPortAddress = SessionValues.SessionDef.
                    
                    // if (SessionSettings.SettingExists("Session", "SerialPortAddress"))
                    //     SerialPortController.SerialPortAddress =
                    //         (string)SessionSettings.Get("Session", "SerialPortAddress");
                    // else if (SessionSettings.SettingClassExists("SyncBoxConfig"))
                    // {
                    //     if (SessionSettings.SettingExists("SyncBoxConfig", "SerialPortAddress"))
                    //         SerialPortController.SerialPortAddress =
                    //             (string)SessionSettings.Get("SyncBoxConfig", "SerialPortAddress");
                    // }

                    // if (SessionSettings.SettingExists("Session", "SerialPortSpeed"))
                    //     SerialPortController.SerialPortSpeed =
                    //         (int)SessionSettings.Get("Session", "SerialPortSpeed");
                    // else if (SessionSettings.SettingClassExists("SyncBoxConfig"))
                    // {
                    //     if (SessionSettings.SettingExists("SyncBoxConfig", "SerialPortSpeed"))
                    //         SerialPortController.SerialPortSpeed =
                    //             (int)SessionSettings.Get("SyncBoxConfig", "SerialPortSpeed");
                    // }

                    SessionValues.SerialPortController.Initialize();

                }
            });

            int iTask = 0;
            SceneLoading = false;
            string taskName = "";
            AsyncOperation loadScene = null;
            setupSession.AddUpdateMethod(() =>
            {
                if (waitForSerialPort && Time.time - StartTimeAbsolute > SessionValues.SerialPortController.initTimeout / 1000f + 0.5f)
                {
                    if (SessionValues.SessionDef.SyncBoxActive && SessionValues.SessionDef.SyncBoxInitCommands != null)
                        SessionValues.SyncBoxController.SendCommand((List<string>)SessionValues.SessionDef.SyncBoxInitCommands);
                    waitForSerialPort = false;
                }

                if (SessionValues.SessionDef.EyeTrackerActive && GazeCalibrationTaskLevel == null)
                {
                    //Have to add calibration task level as child of calibration state here, because it isn't available prior
                    GazeCalibrationTaskLevel = GameObject.Find("GazeCalibration_Scripts").GetComponent<GazeCalibration_TaskLevel>();
                    GazeCalibrationTaskLevel.TaskName = "GazeCalibration";
                    GazeCalibrationTaskLevel.ConfigName = "GazeCalibration";
                    PopulateTaskLevel(GazeCalibrationTaskLevel, false);
                    gazeCalibration.AddChildLevel(GazeCalibrationTaskLevel);
                    GazeCalibrationTaskLevel.TrialLevel.TaskLevel = GazeCalibrationTaskLevel;
                    GazeCalibrationTaskLevel.gameObject.SetActive(false);
                }


                if (iTask < SessionValues.SessionDef.TaskMappings.Count)
                {
                    if (!SceneLoading)
                    {
                        SceneLoading = true;
                        taskName = (string)SessionValues.SessionDef.TaskMappings[iTask];
                        loadScene = SceneManager.LoadSceneAsync(taskName, LoadSceneMode.Additive);
                        string configName = SessionValues.SessionDef.TaskMappings.Cast<DictionaryEntry>().ElementAt(iTask).Key.ToString();
                        // Unload it after memory because this loads the assets into memory but destroys the objects
                        loadScene.completed += (_) =>
                        {
                            SessionSettings.Save();
                            OnSceneLoaded(configName, true);
                            SessionSettings.Restore();
                            SceneManager.UnloadSceneAsync(taskName);
                            SceneLoading = false;
                            iTask++;
                        };
                    }
                }
            });
            //setupSession.AddLateUpdateMethod(() => AppendSerialData());

            setupSession.SpecifyTermination(() => iTask >= SessionValues.SessionDef.TaskMappings.Count && !waitForSerialPort && SessionValues.SessionDef.EyeTrackerActive, gazeCalibration);
            setupSession.SpecifyTermination(() => iTask >= SessionValues.SessionDef.TaskMappings.Count && !waitForSerialPort && !SessionValues.SessionDef.EyeTrackerActive, selectTask);

            setupSession.AddDefaultTerminationMethod(() =>
            {
                SessionSettings.Save();
                if (!SessionValues.WebBuild)
                {
                    GameObject initCamGO = GameObject.Find("InitCamera");
                    initCamGO.SetActive(false);
                    SessionValues.SessionInfoPanel = GameObject.Find("SessionInfoPanel").GetComponent<SessionInfoPanel>();
                }
                SessionValues.EventCodeManager.SendCodeImmediate(SessionValues.SessionEventCodes["SetupSessionEnds"]);
            });

            // Canvas[] TaskSelectionCanvasses = null;
            gazeCalibration.AddInitializationMethod(() =>
            {
                FrameData.gameObject.SetActive(false);

                GazeCalibrationTaskLevel.TaskCam = Camera.main;
                // GazeCalibrationTaskLevel.ConfigName = "GazeCalibration";
                GazeCalibrationTaskLevel.TrialLevel.runCalibration = true;
                SessionValues.ExperimenterDisplayController.ResetTask(GazeCalibrationTaskLevel, GazeCalibrationTaskLevel.TrialLevel);

                var GazeCalibrationCanvas = GameObject.Find("GazeCalibration(Clone)").transform.Find("GazeCalibration_Canvas");
                var GazeCalibrationScripts = GameObject.Find("GazeCalibration(Clone)").transform.Find("GazeCalibration_Scripts");
                GazeCalibrationCanvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                //  CalibrationCanvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceCamera;
                //CalibrationCanvas.GetComponent<Canvas>().worldCamera = Camera.main;

                GazeCalibrationCanvas.gameObject.SetActive(true);
                GazeCalibrationScripts.gameObject.SetActive(true);
            });

            gazeCalibration.SpecifyTermination(() => !GazeCalibrationTaskLevel.TrialLevel.runCalibration, () => selectTask, () =>
            {
                GazeCalibrationTaskLevel.TaskName = "GazeCalibration";
                GazeCalibrationTaskLevel.ConfigName = "GazeCalibration";
                GameObject.Find("GazeCalibration(Clone)").transform.Find("GazeCalibration_Canvas").gameObject.SetActive(false);
                GameObject.Find("GazeCalibration(Clone)").transform.Find("GazeCalibration_Scripts").gameObject.SetActive(false);
                if (SessionValues.SessionDef.EyeTrackerActive && TobiiEyeTrackerController.Instance.isCalibrating)
                {
                    TobiiEyeTrackerController.Instance.isCalibrating = false;
                    TobiiEyeTrackerController.Instance.ScreenBasedCalibration.LeaveCalibrationMode();
                }

                SessionValues.GazeData.folderPath = SessionValues.SessionLevelDataPath + Path.DirectorySeparatorChar + "GazeData";
                FrameData.gameObject.SetActive(true);

            });

            TaskButtonsContainer = null;
            Dictionary<string, GameObject> taskButtonGOs = new Dictionary<string, GameObject>();
            string selectedConfigName = null;
            selectTask.AddUniversalInitializationMethod(() =>
            {
                if (SessionValues.SessionDef.PlayBackgroundMusic)
                {
                    if (BackgroundMusic_AudioSource == null)
                        SetupBackgroundMusic();
                }
                else
                {
                    if(ToggleAudioButton.activeInHierarchy)
                        ToggleAudioButton.transform.Find("Cross").gameObject.SetActive(true);
                }

                if (SelectionHandler.AllSelections.Count > 0)
                    SelectionHandler.ClearSelections();

                SessionValues.TaskSelectionCanvasGO.SetActive(true);

                TaskSelection_Starfield.SetActive(SessionValues.SessionDef.IsHuman);


#if (!UNITY_WEBGL)
                if (SessionValues.DisplayController.SwitchDisplays) //SwitchDisplay stuff doesnt full work yet!
                {
                    SessionCam.targetDisplay = 1;

                    Canvas experimenterCanvas = GameObject.Find("ExperimenterCanvas").GetComponent<Canvas>();
                    experimenterCanvas.targetDisplay = 0;
                    foreach (Transform child in experimenterDisplay.transform)
                    {
                        Camera cam = child.GetComponent<Camera>();
                        if (cam != null)
                            cam.targetDisplay = 1 - cam.targetDisplay;
                    }
                }
                else
                {
                    CameraMirrorTexture = new RenderTexture(Screen.width, Screen.height, 24);
                    CameraMirrorTexture.Create();
                    SessionCam.targetTexture = CameraMirrorTexture;
                    mainCameraCopy_Image.texture = CameraMirrorTexture;
                }
#endif

                SessionValues.EventCodeManager.SendCodeImmediate(SessionValues.SessionEventCodes["SelectTaskStarts"]);

                if (SessionValues.SessionDef.SerialPortActive)
                {
                    StartCoroutine(SessionValues.SerialSentData.CreateFile());
                    StartCoroutine(SessionValues.SerialRecvData.CreateFile());
                }

                if (SessionValues.SessionDef.EyeTrackerActive)
                {
                    StartCoroutine(SessionValues.GazeData.CreateFile());
                }

                StartCoroutine(FrameData.CreateFile());

                SessionSettings.Restore();
                selectedConfigName = null;
                taskAutomaticallySelected = false; // gives another chance to select even if previous task loading was due to timeout

                SessionCam.gameObject.SetActive(true);


                // Don't show the task buttons if we encountered an error during setup
                if (LogPanel.HasError())
                    return;

                SceneLoading = true;
                if (taskCount >= SessionValues.SessionDef.TaskMappings.Count)
                {
                    TasksFinished = true;
                    return;
                }

                if (TaskButtonsContainer != null)
                {
                    TaskButtonsContainer.SetActive(true);
                    if (SessionValues.SessionDef.GuidedTaskSelection)
                    {
                        // if guided selection, we need to adjust the shading of the icons and buttons after the task buttons object is already created                        
                        string key = SessionValues.SessionDef.TaskMappings.Keys.Cast<string>().ElementAt(taskCount);

                        foreach (KeyValuePair<string, GameObject> taskButton in taskButtonGOs)
                        {
                            if (taskButton.Key == key)
                            {
                                taskButton.Value.GetComponent<RawImage>().color = new Color(1f, 1f, 1f, 1f);
                                taskButton.Value.GetComponent<RawImage>().raycastTarget = true; 
                                if (SessionValues.SessionDef.IsHuman && SessionValues.UsingDefaultConfigs)
                                    taskButton.Value.AddComponent<HoverEffect>();
                            }
                            else
                            {
                                taskButton.Value.GetComponent<RawImage>().color = new Color(.5f, .5f, .5f, .35f);
                                taskButton.Value.GetComponent<RawImage>().raycastTarget = false;
                                if (SessionValues.SessionDef.IsHuman && SessionValues.UsingDefaultConfigs)
                                {
                                    if (taskButton.Value.TryGetComponent<HoverEffect>(out var hoverEffect))
                                        Destroy(hoverEffect);
                                }
                            }
                        }
                    }
                    return;
                }


                TaskButtonsContainer = GameObject.Find("TaskButtonsGrid");

                GridLayoutGroup gridLayout = TaskButtonsContainer.GetComponent<GridLayoutGroup>();
                int size = SessionValues.WebBuild ? 250 : SessionValues.SessionDef.TaskButtonSize; //using 250 for web build
                gridLayout.cellSize = new Vector2(size, size);
                gridLayout.constraintCount = SessionValues.WebBuild ? 4 : SessionValues.SessionDef.TaskButtonGridMaxPerRow; //using 4 for WebBuild
                int spacing = SessionValues.WebBuild ? 45 : SessionValues.SessionDef.TaskButtonSpacing; //using 45 for web build
                gridLayout.spacing = new Vector2(spacing, spacing);

                List<GameObject> gridList = new List<GameObject>();

                if(SessionValues.SessionDef.TaskButtonGridPositions != null)
                {
                    for(int i = 0; i < SessionValues.SessionDef.NumGridCells; i++)
                    {
                        GameObject gridItem = new GameObject("GridItem_" + (i+1));
                        gridItem.AddComponent<RawImage>();
                        gridItem.GetComponent<RawImage>().enabled = false;
                        gridItem.transform.SetParent(TaskButtonsContainer.transform);
                        gridItem.transform.localPosition = Vector3.zero;
                        gridItem.transform.localScale = Vector3.one;
                        gridItem.AddComponent<Text>().text = i.ToString();
                        gridList.Add(gridItem);
                    }
                }

                int count = 0;
                foreach (DictionaryEntry task in SessionValues.SessionDef.TaskMappings)
                {
                    string configName = (string)task.Key;
                    string taskName = (string)task.Value;

                    GameObject taskButtonGO;
                    RawImage image;

                    if (SessionValues.SessionDef.TaskButtonGridPositions != null)
                    {
                        int gridNumber = SessionValues.SessionDef.TaskButtonGridPositions[count];

                        taskButtonGO = gridList[gridNumber];
                        taskButtonGO.name = configName;

                        image = taskButtonGO.GetComponent<RawImage>();
                        image.enabled = true;
                    }
                    else
                    {
                        taskButtonGO = new GameObject(configName);
                        image = taskButtonGO.AddComponent<RawImage>();
                        taskButtonGO.transform.SetParent(TaskButtonsContainer.transform);
                        taskButtonGO.transform.localPosition = Vector3.zero;
                        taskButtonGO.transform.localScale = Vector3.one;
                    }


                    string taskFolderPath = GetConfigFolderPath(configName);

                    if (!SessionValues.WebBuild)
                    {
                        if (!Directory.Exists(taskFolderPath))
                        {
                            Destroy(taskButtonGO);
                            throw new DirectoryNotFoundException($"Task folder for '{configName}' at '{taskFolderPath}' does not exist.");
                        }
                    }

                    if (SessionValues.WebBuild)
                    {
                        if (SessionValues.UsingDefaultConfigs)
                            image.texture = Resources.Load<Texture2D>($"{SessionValues.SessionDef.TaskIconsFolderPath}/{taskName}");
                        else
                        {
                            StartCoroutine(ServerManager.LoadTextureFromServer($"{SessionValues.SessionDef.TaskIconsFolderPath}/{taskName}.png", imageResult =>
                            {
                                if (imageResult != null)
                                    image.texture = imageResult;
                                else
                                    Debug.Log("NULL GETTING TASK ICON TEXTURE FROM SERVER!");
                            }));
                        }
                    }
                    else
                        image.texture = LoadPNG(SessionValues.SessionDef.TaskIconsFolderPath + Path.DirectorySeparatorChar + taskName + ".png");


                    if (SessionValues.SessionDef.GuidedTaskSelection)
                    {
                        // If guided task selection, only make the next icon interactable
                        string key = SessionValues.SessionDef.TaskMappings.Keys.Cast<string>().ElementAt(taskCount);

                        if (configName == key)
                        {
                            image.color = new Color(1f, 1f, 1f, 1f);
                            image.raycastTarget = true;
                            if (SessionValues.SessionDef.IsHuman && SessionValues.UsingDefaultConfigs)
                                taskButtonGO.AddComponent<HoverEffect>();
                        }
                        else
                        {
                            image.color = new Color(.5f, .5f, .5f, .35f);
                            image.raycastTarget = false;
                        }
                    }
                    else
                    {
                        // If not guided task selection, make all icons interactable
                        image.raycastTarget = true;
                        if (SessionValues.SessionDef.IsHuman && SessionValues.UsingDefaultConfigs)
                            taskButtonGO.AddComponent<HoverEffect>();
                    }


                    taskButtonGOs.Add(configName, taskButtonGO);

                    count++;
                }


                if (SessionValues.SessionDef.IsHuman)
                {
                    HumanVersionToggleButton.SetActive(true);
                    ToggleAudioButton.SetActive(true);
                }
            });

            selectTask.AddLateUpdateMethod(() =>
            {
                SessionValues.SelectionTracker.UpdateActiveSelections();

                if (SelectionHandler.SuccessfulSelections.Count > 0)
                {
                    string chosenGO = SelectionHandler.LastSuccessfulSelection.SelectedGameObject?.name;
                    if (chosenGO != null && taskButtonGOs.ContainsKey(chosenGO))
                    {
                        selectedConfigName = chosenGO;
                        taskAutomaticallySelected = false;
                    }
                }
                AppendSerialData();
                StartCoroutine(FrameData.AppendDataToBuffer());
            });
            selectTask.SpecifyTermination(() => selectedConfigName != null, loadTask, () => ResetSelectedTaskButtonSize());


            // Don't have automatic task selection if we encountered an error during setup
            if (SessionValues.SessionDef.TaskSelectionTimeout >= 0 && !LogPanel.HasError())
            {
                selectTask.AddTimer(SessionValues.SessionDef.TaskSelectionTimeout, loadTask, () =>
                {
                    foreach (DictionaryEntry task in SessionValues.SessionDef.TaskMappings)
                    {
                        //Find the next task in the list that is still interactable
                        string configName = (string)task.Key;

                        // If the next task button in the task mappings is not interactable, skip until the next available config is found
                        if (!taskButtonGOs[configName].GetComponent<RawImage>().raycastTarget)
                            continue;

                        taskAutomaticallySelected = true;
                        selectedConfigName = configName;
                        break;
                    }
                });
            }
            selectTask.SpecifyTermination(() => TasksFinished, finishSession);

            loadTask.AddInitializationMethod(() =>
            {
                // Make the selected task icon no longer interactable
                TaskButtonsContainer.SetActive(false);

                GameObject taskButton = taskButtonGOs[selectedConfigName];
                RawImage image = taskButton.GetComponent<RawImage>();

                if (!SessionValues.WebBuild) //Let patients play same task as many times as they want
                {
                    image.color = new Color(.5f, .5f, .5f, .35f);
                    image.raycastTarget = false;
                    if (taskButton.TryGetComponent<HoverEffect>(out var hoverEffect))
                        Destroy(hoverEffect);
                }

                string taskName = (string)SessionValues.SessionDef.TaskMappings[selectedConfigName];
                loadScene = SceneManager.LoadSceneAsync(taskName, LoadSceneMode.Additive);
                loadScene.completed += (_) =>
                {
                    OnSceneLoaded(selectedConfigName, false);
                    CurrentTask = ActiveTaskLevels.Find((task) => task.ConfigName == selectedConfigName);
                };
            });

            loadTask.AddFixedUpdateMethod(() =>
            {
                SessionValues.SelectionTracker.UpdateActiveSelections();
            });

            loadTask.AddLateUpdateMethod(() =>
            {
                AppendSerialData();
                FrameData.AppendDataToBuffer();
            });

            loadTask.SpecifyTermination(() => CurrentTask != null && CurrentTask.TaskLevelDefined, runTask, () =>
            {
                TaskSelection_Starfield.SetActive(false);
                runTask.AddChildLevel(CurrentTask);
                if (CameraMirrorTexture != null)
                    CameraMirrorTexture.Release();

                if (CurrentTask.TaskName != "GazeCalibration")
                    SceneManager.SetActiveScene(SceneManager.GetSceneByName(CurrentTask.TaskName));
                CurrentTask.TrialLevel.TaskLevel = CurrentTask;
                if (SessionValues.ExperimenterDisplayController != null)
                    SessionValues.ExperimenterDisplayController.ResetTask(CurrentTask, CurrentTask.TrialLevel);

                if (SessionValues.SessionDef.SerialPortActive)
                {
                    AppendSerialData();
                    StartCoroutine(SessionValues.SerialRecvData.AppendDataToFile());
                    StartCoroutine(SessionValues.SerialSentData.AppendDataToFile());
                    SessionValues.SerialRecvData.CreateNewTaskIndexedFolder((taskCount + 1) * 2, SessionValues.SessionDataPath, "SerialRecvData", CurrentTask.TaskName);
                    SessionValues.SerialSentData.CreateNewTaskIndexedFolder((taskCount + 1) * 2, SessionValues.SessionDataPath, "SerialSentData", CurrentTask.TaskName);
                }
            });

            //automatically finish tasks after running one - placeholder for proper selection
            //runTask.AddLateUpdateMethod
            runTask.AddUniversalInitializationMethod(() =>
            {
                SessionCam.gameObject.SetActive(false);

                SessionValues.EventCodeManager.SendCodeImmediate(SessionValues.SessionEventCodes["RunTaskStarts"]);

#if (!UNITY_WEBGL)

                if (SessionValues.DisplayController.SwitchDisplays)
                    CurrentTask.TaskCam.targetDisplay = 1;
                else
                {
                    CameraMirrorTexture = new RenderTexture(Screen.width, Screen.height, 24);
                    CameraMirrorTexture.Create();
                    CurrentTask.TaskCam.targetTexture = CameraMirrorTexture;
                    mainCameraCopy_Image.texture = CameraMirrorTexture;
                }

#endif

            });

            if (SessionValues.SessionDef.EventCodesActive)
            {
                runTask.AddFixedUpdateMethod(() => SessionValues.EventCodeManager.EventCodeFixedUpdate());
                // runTask.AddLateUpdateMethod(() => EventCodeManager.EventCodeLateUpdate());
            }


            runTask.AddLateUpdateMethod(() =>
            {
                SessionValues.SelectionTracker.UpdateActiveSelections();
                AppendSerialData();
            });

            runTask.SpecifyTermination(() => CurrentTask.Terminated, selectTask, () =>
            {
                if (PreviousTaskSummaryString != null && CurrentTask.CurrentTaskSummaryString != null)
                    PreviousTaskSummaryString.Insert(0, CurrentTask.CurrentTaskSummaryString);

                StartCoroutine(SummaryData.AddTaskRunData(CurrentTask.ConfigName, CurrentTask, CurrentTask.GetTaskSummaryData()));

                StartCoroutine(SessionData.AppendDataToBuffer());
                StartCoroutine(SessionData.AppendDataToFile());

                if(CurrentTask.TaskName != "GazeCalibration")
                    SceneManager.UnloadSceneAsync(CurrentTask.TaskName);
                SceneManager.SetActiveScene(SceneManager.GetSceneByName(TaskSelectionSceneName));

                ActiveTaskLevels.Remove(CurrentTask);

                if (CameraMirrorTexture != null)
                    CameraMirrorTexture.Release();

                if (SessionValues.ExperimenterDisplayController != null)
                    SessionValues.ExperimenterDisplayController.ResetTask(null, null);

                if (SessionValues.SessionDef.EyeTrackerActive && TobiiEyeTrackerController.Instance.isCalibrating)
                {
                    TobiiEyeTrackerController.Instance.isCalibrating = false;
                    TobiiEyeTrackerController.Instance.ScreenBasedCalibration.LeaveCalibrationMode();
                }

                taskCount++;

                if (SessionValues.SessionDef.SerialPortActive)
                {
                    SessionValues.SerialRecvData.CreateNewTaskIndexedFolder((taskCount + 1) * 2 - 1, SessionValues.SessionDataPath, "SerialRecvData", "SessionLevel");
                    SessionValues.SerialSentData.CreateNewTaskIndexedFolder((taskCount + 1) * 2 - 1, SessionValues.SessionDataPath, "SerialSentData", "SessionLevel");


                }
                //     SessionValues.SessionDataPath + Path.DirectorySeparatorChar +
                //                             SerialRecvData.GetNiceIntegers(4, taskCount + 1 * 2 - 1) + "_TaskSelection";
                // SerialSentData.folderPath = SessionValues.SessionDataPath + Path.DirectorySeparatorChar +
                //                             SerialSentData.GetNiceIntegers(4, taskCount + 1 * 2 - 1) + "_TaskSelection";


                if (SessionValues.SessionDef.EyeTrackerActive)
                {
                    SessionValues.GazeData.CreateNewTaskIndexedFolder((taskCount + 1) * 2 - 1, SessionValues.SessionLevelDataPath, "GazeData", "SessionLevel");
                    SessionValues.GazeData.fileName = SessionValues.FilePrefix + "__GazeData" + SessionValues.GazeData.GetNiceIntegers(4, (taskCount + 1) * 2 - 1) + "SessionLevel.txt";
                }

                FrameData.CreateNewTaskIndexedFolder((taskCount + 1) * 2 - 1, SessionValues.SessionLevelDataPath, "FrameData", "SessionLevel");
                FrameData.fileName = SessionValues.FilePrefix + "__FrameData" + FrameData.GetNiceIntegers(4, (taskCount + 1) * 2 - 1) + "SessionLevel.txt";

                FrameData.gameObject.SetActive(true);
            });

            finishSession.AddInitializationMethod(() =>
            {
                SessionValues.EventCodeManager.SendCodeImmediate(SessionValues.SessionEventCodes["FinishSessionStarts"]);
            });

            finishSession.SpecifyTermination(() => true, () => null, () =>
            {
                StartCoroutine(SessionData.AppendDataToBuffer());
                StartCoroutine(SessionData.AppendDataToFile());

                AppendSerialData();
                if (SessionValues.SessionDef.SerialPortActive)
                {
                    StartCoroutine(SessionValues.SerialSentData.AppendDataToFile());
                    StartCoroutine(SessionValues.SerialRecvData.AppendDataToFile());
                }

                if (SessionValues.SessionDef.EyeTrackerActive)
                    StartCoroutine(SessionValues.GazeData.AppendDataToFile());

                StartCoroutine(FrameData.AppendDataToFile());
            });

            SessionData = (SessionData)SessionValues.SessionDataControllers.InstantiateDataController<SessionData>
                ("SessionData", SessionValues.SessionDef.StoreData, SessionValues.SessionDataPath); //SessionDataControllers.InstantiateSessionData(StoreData, SessionValues.SessionDataPath);
            SessionData.fileName = SessionValues.FilePrefix + "__SessionData.txt";
            SessionData.sessionLevel = this;
            SessionData.InitDataController();
            SessionData.ManuallyDefine();

            SessionData.AddDatum("SelectedTaskConfigName", () => selectedConfigName);
            SessionData.AddDatum("TaskAutomaticallySelected", () => taskAutomaticallySelected);

            if (SessionValues.SessionDef.SerialPortActive)
            {
                SessionValues.SerialSentData = (SerialSentData)SessionValues.SessionDataControllers.InstantiateDataController<SerialSentData>
                    ("SerialSentData", SessionValues.SessionDef.StoreData, SessionValues.SessionDataPath + Path.DirectorySeparatorChar + "SerialSentData"
                                                  + Path.DirectorySeparatorChar + "0001_TaskSelection");
                SessionValues.SerialSentData.fileName = SessionValues.FilePrefix + "__SerialSentData_0001_TaskSelection.txt";
                SessionValues.SerialSentData.sessionLevel = this;
                SessionValues.SerialSentData.InitDataController();
                SessionValues.SerialSentData.ManuallyDefine();

                SessionValues.SerialRecvData = (SerialRecvData)SessionValues.SessionDataControllers.InstantiateDataController<SerialRecvData>
                    ("SerialRecvData", SessionValues.SessionDef.StoreData, SessionValues.SessionDataPath + Path.DirectorySeparatorChar + "SerialRecvData"
                                                                           + Path.DirectorySeparatorChar + "0001_TaskSelection");
                SessionValues.SerialRecvData.fileName = SessionValues.FilePrefix + "__SerialRecvData_0001_TaskSelection.txt";
                SessionValues.SerialRecvData.sessionLevel = this;
                SessionValues.SerialRecvData.InitDataController();
                SessionValues.SerialRecvData.ManuallyDefine();
            }

            SummaryData.Init();

            SessionValues.SessionLevelDataPath = SessionValues.SessionDataPath + Path.DirectorySeparatorChar + "SessionLevel";

            //if web build, create the SessionLevelDataFolder:
            if(SessionValues.WebBuild)
            {
                StartCoroutine(CreateFolderOnServer(SessionValues.SessionLevelDataPath, () =>
                {
                    Debug.Log("Done creating SessionLevel sub-folder at: " + SessionValues.SessionLevelDataPath);
                }));
            }

            FrameData = (FrameData)SessionValues.SessionDataControllers.InstantiateDataController<FrameData>("FrameData", "SessionLevel", SessionValues.SessionDef.StoreData, SessionValues.SessionLevelDataPath + Path.DirectorySeparatorChar + "FrameData");
            FrameData.fileName = "SessionLevel__FrameData.txt";
            FrameData.sessionLevel = this;
            FrameData.InitDataController();
            FrameData.ManuallyDefine();

            if (SessionValues.SessionDef.EventCodesActive)
                FrameData.AddEventCodeColumns();

            if (SessionValues.SessionDef.EyeTrackerActive)
            {
                SessionValues.GazeData = (GazeData)SessionValues.SessionDataControllers.InstantiateDataController<USE_ExperimentTemplate_Data.GazeData>("GazeData", "SessionLevel", SessionValues.SessionDef.StoreData, SessionValues.SessionLevelDataPath + Path.DirectorySeparatorChar + "GazeData");

                SessionValues.GazeData.fileName = "SessionLevel__GazeData.txt";
                SessionValues.GazeData.sessionLevel = this;
                SessionValues.GazeData.InitDataController();
                SessionValues.GazeData.ManuallyDefine();
                SessionValues.TobiiEyeTrackerController.GazeData = SessionValues.GazeData;
                SessionValues.GazeTracker.Init(FrameData, 0);

            }
            SessionValues.MouseTracker.Init(FrameData, 0);
        }

        private void LoadSessionConfigSettings()
        {
            GameObject.Find("MiscScripts").GetComponent<LogWriter>().StoreDataIsSet = true;

            //MAKE SURE SYNCBOX INACTIVE FOR WEB BUILD (Can eventually remove this once thilo provides web build session configs with it marked false)
            if (SessionValues.WebBuild)
                SessionValues.SessionDef.SyncBoxActive = false;

            if (SessionValues.SessionDef.SyncBoxActive)
                SessionValues.SessionDef.SerialPortActive = true;


            //Load the Session Event Code Config file --------------------------------------------------------------------------------------------------
            string eventCodeFileString = "";

            if(SessionValues.WebBuild && !SessionValues.UsingDefaultConfigs)
            {
                StartCoroutine(ServerManager.GetFileStringAsync(ServerManager.SessionConfigFolderPath, "EventCode", result =>
                {
                    SessionSettings.ImportSettings_SingleTypeJSON<Dictionary<string, EventCode>>("EventCodeConfig", SessionValues.ConfigFolderPath , result);
                    SessionValues.SessionEventCodes = (Dictionary<string, EventCode>)SessionSettings.Get("EventCodeConfig");
                }));
            }
            else
            {
                string path = SessionValues.UsingDefaultConfigs ? (Application.persistentDataPath + Path.DirectorySeparatorChar + "M_USE_DefaultConfigs") : SessionValues.ConfigFolderPath ;
                eventCodeFileString = SessionValues.LocateFile.FindFilePathInExternalFolder(SessionValues.ConfigFolderPath, "*EventCode*");
                if (!string.IsNullOrEmpty(eventCodeFileString))
                {
                   StartCoroutine(SessionValues.BetterReadSettingsFile<Dictionary<string, EventCode>>("EventCodeConfig", "SingleTypeJSON", settingsArray =>
                   {
                       SessionValues.SessionEventCodes = settingsArray[0];
                   }));            
                }
                else if (SessionValues.SessionDef.EventCodesActive)
                    Debug.LogWarning("EventCodesActive variable set to true in Session Config file but no session level event codes file is given.");
            }

            // List<string> taskNames;
            if (SessionValues.SessionDef.TaskNames != null)
            {
                SessionValues.SessionDef.TaskMappings = new OrderedDictionary();
                SessionValues.SessionDef.TaskNames.ForEach((taskName) => SessionValues.SessionDef.TaskMappings.Add(taskName, taskName));
            }
            else if (SessionValues.SessionDef.TaskMappings.Count == 0)
                Debug.LogError("No task names or task mappings specified in Session config file or by other means.");

        }

        private void WriteSessionConfigsToPersistantDataPath()
        {
            if (Directory.Exists(SessionValues.ConfigFolderPath ))
                Directory.Delete(SessionValues.ConfigFolderPath , true);

            if (!Directory.Exists(SessionValues.ConfigFolderPath ))
            {
                Directory.CreateDirectory(SessionValues.ConfigFolderPath );
                List<string> configsToWrite = new List<string>() { "SessionConfig", "EventCodeConfig", "DisplayConfig" };
                foreach (string config in configsToWrite)
                {
                    byte[] textFileBytes = Resources.Load<TextAsset>("DefaultSessionConfigs/" + config).bytes;
                    File.WriteAllBytes(SessionValues.ConfigFolderPath  + Path.DirectorySeparatorChar + config + ".txt", textFileBytes);
                }
            }
        }

        private void ResetSelectedTaskButtonSize()
        {
            if (SelectionHandler.SuccessfulSelections.Count > 0)
            {
                if (SelectionHandler.LastSuccessfulSelection.SelectedGameObject.TryGetComponent(out HoverEffect hoverComponent))
                    hoverComponent.SetToInitialSize();
                else
                    Debug.Log("HoverEffect component not found on selected TaskButton, so not resetting its size.");
            }
            else
                Debug.Log("No successfulSelection from which to get the taskButton GameObject from (so we can reset its size)");
        }

        private void SetupBackgroundMusic(float audioSpot = 0)
        {
            BackgroundMusic_AudioSource = gameObject.AddComponent<AudioSource>();
            BackgroundMusic_AudioSource.clip = BackgroundMusic_AudioClip;
            BackgroundMusic_AudioSource.loop = true;
            BackgroundMusic_AudioSource.volume = .55f;
            if (audioSpot != 0)
                BackgroundMusic_AudioSource.time = audioSpot;
            BackgroundMusic_AudioSource.Play();
            ToggleAudioButton.transform.Find("Cross").gameObject.SetActive(false);
        }

        public void HandleToggleAudioButtonClick()
        {
            if(BackgroundMusic_AudioSource != null)
            {
                if (BackgroundMusic_AudioSource.isPlaying)
                {
                    audioPlaybackSpot = BackgroundMusic_AudioSource.time;
                    BackgroundMusic_AudioSource.Stop();
                    ToggleAudioButton.transform.Find("Cross").gameObject.SetActive(true);
                }
                else
                {
                    BackgroundMusic_AudioSource.time = audioPlaybackSpot;
                    BackgroundMusic_AudioSource.Play();
                    ToggleAudioButton.transform.Find("Cross").gameObject.SetActive(false);
                }
            }
            else
            {
                SetupBackgroundMusic();
            }
        }

        public void HandleHumanVersionToggleButtonClick()
        {
            SessionValues.SessionDef.IsHuman = !SessionValues.SessionDef.IsHuman;

            if(SessionValues.SessionDef.IsHuman)
            {
                ToggleAudioButton.SetActive(true);

                if(!BackgroundMusic_AudioSource.isPlaying)
                    ToggleAudioButton.transform.Find("Cross").gameObject.SetActive(true);

                if (SessionValues.SessionDef.PlayBackgroundMusic)
                    SetupBackgroundMusic(audioPlaybackSpot);
            }
            else
            {
                if(BackgroundMusic_AudioSource != null)
                {
                    audioPlaybackSpot = BackgroundMusic_AudioSource.time;
                    BackgroundMusic_AudioSource.Stop();
                }
                ToggleAudioButton.SetActive(false);   
            }

            //Change text on button:
            HumanVersionToggleButton.GetComponentInChildren<TextMeshProUGUI>().text = SessionValues.SessionDef.IsHuman ? "Human Version" : "Primate Version";
            //Toggle Starfield:
            TaskSelection_Starfield.SetActive(!TaskSelection_Starfield.activeInHierarchy);
        }

        private void AppendSerialData()
        {
            if (SessionValues.SessionDef.SerialPortActive)
            {
                if (SessionValues.SerialPortController.BufferCount("sent") > 0)
                {
                    try
                    {
                        // Debug.Log("sentdata: " + SerialSentData);
                        // Debug.Log("sentdata.sc: " + SerialSentData.sc);
                        // Debug.Log("sentdata.sc: " + SerialSentData.sc.BufferCount("sent"));
                        // Debug.Log("sentdata.sc: " + SerialSentData.sc.BufferToString("sent"));
                        SessionValues.SerialSentData.AppendDataToBuffer();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }

                if (SessionValues.SerialPortController.BufferCount("received") > 0)
                {
                    try
                    {
                        // Debug.Log("recvdata: " + SerialRecvData);
                        // Debug.Log("recvdata.sc: " + SerialRecvData.sc);
                        // Debug.Log("recvdata.sc: " + SerialRecvData.sc.BufferCount("received"));
                        // Debug.Log("recvdata.sc: " + SerialRecvData.sc.BufferToString("received"));
                        SessionValues.SerialRecvData.AppendDataToBuffer();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }
        }
        
        string GetConfigFolderPath(string configName)
        {
            string path;

            if(SessionValues.WebBuild)
            {
                if (SessionValues.UsingDefaultConfigs)
                    path = Application.persistentDataPath + Path.DirectorySeparatorChar + "M_USE_DefaultConfigs";
                else
                    path = $"{ServerManager.SessionConfigFolderPath}/{configName}";
            }
            else
            {
                if (!SessionSettings.SettingExists("Session", "ConfigFolderNames"))
                    return SessionValues.ConfigFolderPath  + Path.DirectorySeparatorChar + configName;
                else
                {
                    List<string> configFolders =
                        (List<string>)SessionSettings.Get("Session", "ConfigFolderNames");
                    int index = 0;
                    foreach (string k in SessionValues.SessionDef.TaskMappings.Keys)
                    {
                        if (k.Equals(configName)) break;
                        ++index;
                    }
                    path = SessionValues.ConfigFolderPath  + Path.DirectorySeparatorChar + configFolders[index];
                }
            }
            return path;
        }

        public ControlLevel_Task_Template PopulateTaskLevel(ControlLevel_Task_Template tl, bool verifyOnly)
        {
	        tl.BlockResults_AudioClip = BlockResults_AudioClip;
            SessionValues.SessionLevel = this;
            //tl.USE_StartButton = USE_StartButton;
            //tl.TaskSelectionCanvasGO = TaskSelectionCanvasGO;
            //tl.HumanStartPanel = HumanStartPanel;
           // tl.IsHuman = IsHuman;
           // tl.DisplayController = DisplayController;
           // tl.SessionDataControllers = SessionDataControllers;
           // tl.LocateFile = LocateFile;
           // tl.SessionLevelDataPath = SessionLevelDataPath;

            tl.BlockResultsPrefab = BlockResultsPrefab;
            tl.BlockResults_GridElementPrefab = BlockResults_GridElementPrefab;


            if (SessionValues.UsingDefaultConfigs)
            {
                tl.TaskConfigPath = GetConfigFolderPath(tl.ConfigName) + Path.DirectorySeparatorChar + tl.TaskName + "_DefaultConfigs";

                //Write Task Config Folder and its files to Persistant data path for Webbuild using default configs---------------------
                if (!Directory.Exists(tl.TaskConfigPath))
                {
                    Directory.CreateDirectory(tl.TaskConfigPath);

                    Dictionary<string, string> configDict = new Dictionary<string, string>
                    {
                        {"_TaskDef", "_TaskDef.txt"},
                        {"_TaskDeftdf", "_TaskDef.txt"},
                        {"_BlockDeftdf", "_BlockDeftdf.txt"},
                        {"_TrialDeftdf", "_TrialDeftdf.txt"},
                        {"_StimDeftdf", "_StimDeftdf.txt"},
                        {"_ConfigUiDetails", "_ConfigUiDetails.json"},
                        {"_EventCodeConfig", "_EventCodeConfig.json"},
                        {"MazeDef", "MazeDef.txt"}
                    };
                    TextAsset configTextAsset;
                    foreach (var entry in configDict)
                    {
                        configTextAsset = Resources.Load<TextAsset>("DefaultSessionConfigs/" + tl.TaskName + "_DefaultConfigs/" + tl.TaskName + entry.Key);
                        if (configTextAsset == null)//try it without task name (cuz MazeDef.txt doesnt have MazeGame in front of it)
                            configTextAsset = Resources.Load<TextAsset>("DefaultSessionConfigs/" + tl.TaskName + "_DefaultConfigs/" + entry.Key);
                        if (configTextAsset != null)
                            File.WriteAllBytes(tl.TaskConfigPath + Path.DirectorySeparatorChar + tl.TaskName + entry.Value, configTextAsset.bytes);
                    }
                }
            }
            else
                tl.TaskConfigPath = GetConfigFolderPath(tl.ConfigName);

          //   tl.FilePrefix = FilePrefix;
          //  tl.StoreData = StoreData;
          //   tl.SubjectID = SubjectID;
          //   tl.SessionID = SessionID;
            // tl.SerialRecvData = SerialRecvData;
            // tl.SerialSentData = SerialSentData;
           // tl.GazeData = GazeData;

        //    tl.SelectionTracker = SelectionTracker;
            
           // tl.EyeTrackerActive = EyeTrackerActive;

            // if (EyeTrackerActive)
            // {
            //     tl.GazeTracker = GazeTracker;
            //    // tl.TobiiEyeTrackerController = TobiiEyeTrackerController;
            // }
            //tl.MouseTracker = MouseTracker;

           // tl.InputManager = InputManager;
         //   tl.SelectionType = SelectionType;

          //  tl.ContextExternalFilePath = ContextExternalFilePath;
         //   tl.SerialPortActive = SerialPortActive;
           // tl.SyncBoxActive = SyncBoxActive;
         //   tl.EventCodeManager = EventCodeManager;
          //  tl.EventCodesActive = EventCodesActive;
           // tl.SessionEventCodes = SessionValues.SessionEventCodes;
            // if (SerialPortActive)
            //     tl.SerialPortController = SerialPortController;
            if (SessionValues.SessionDef.SyncBoxActive)
            {
                SessionValues.SyncBoxController.SessionEventCodes = SessionValues.SessionEventCodes;
             //   tl.SyncBoxController = SyncBoxController;
            }
            // tl.ShotgunRaycastCircleSize_DVA = ShotgunRaycastCircleSize_DVA;
            // tl.ShotgunRaycastSpacing_DVA = ShotgunRaycastSpacing_DVA;
            // tl.ParticipantDistance_CM = ParticipantDistance_CM;

            //
            // if (SessionSettings.SettingExists("Session", "RewardPulsesActive"))
            //     tl.RewardPulsesActive = (bool)SessionSettings.Get("Session", "RewardPulsesActive");
            // else
            //     tl.RewardPulsesActive = false;
            //
            // if (SessionSettings.SettingExists("Session", "SonicationActive"))
            //     tl.SonicationActive = (bool)SessionSettings.Get("Session", "SonicationActive");
            // else
            //     tl.SonicationActive = false;


            StartCoroutine(tl.DefineTaskLevel(verifyOnly));
            //ActiveTaskTypes.Add(tl.TaskName, tl.TaskLevelType);
            // Don't add task to ActiveTaskLevels if we're just verifying
            if (verifyOnly) return tl;

            ActiveTaskLevels.Add(tl);
            if (tl.TaskCanvasses != null)
                foreach (Canvas canvas in tl.TaskCanvasses)
                    canvas.gameObject.SetActive(true);
            return tl;
        }


        void OnSceneLoaded(string configName, bool verifyOnly)
        {
            string taskName = (string)SessionValues.SessionDef.TaskMappings[configName];
            var methodInfo = GetType().GetMethod(nameof(this.PrepareTaskLevel));
            
            Type taskType = USE_Tasks_CustomTypes.CustomTaskDictionary[taskName].TaskLevelType;
            MethodInfo prepareTaskLevel = methodInfo.MakeGenericMethod(new Type[] { taskType });
            prepareTaskLevel.Invoke(this, new object[] { configName, verifyOnly });
            // TaskSceneLoaded = true;
            SceneLoading = false;
        }

        public void PrepareTaskLevel<T>(string configName, bool verifyOnly) where T : ControlLevel_Task_Template
        {
            string taskName = (string)SessionValues.SessionDef.TaskMappings[configName];
            ControlLevel_Task_Template tl = GameObject.Find(taskName + "_Scripts").GetComponent<T>();
            tl.ConfigName = configName;
            tl = PopulateTaskLevel(tl, verifyOnly);
            if (tl.TaskCam == null)
                tl.TaskCam = GameObject.Find(taskName + "_Camera").GetComponent<Camera>();

            tl.TaskCam.gameObject.SetActive(false);
        }
        // public void FindTaskCam<T>(string taskName) where T : ControlLevel_Task_Template
        // {
        // 	ControlLevel_Task_Template tl = GameObject.Find("ControlLevels").GetComponent<T>();
        // 	tl.TaskCam = GameObject.Find(taskName + "_Camera").GetComponent<Camera>();
        // 	tl.TaskCam.gameObject.SetActive(false);
        // }

#if UNITY_STANDALONE_WIN
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct ReparseDataBuffer
        {
            public uint ReparseTag;
            public ushort ReparseDataLength;
            public ushort Reserved;
            public ushort SubstituteNameOffset;
            public ushort SubstituteNameLength;
            public ushort PrintNameOffset;
            public ushort PrintNameLength;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string PathBuffer;
        }

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode,
            IntPtr lpSecurityAttributes, uint dwCreationDispositionulong, uint dwFlagsAndAttributes, IntPtr hTemplateFile);
        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern bool DeviceIoControl(IntPtr hDevice, uint dwIoControlCode, ref ReparseDataBuffer lpInBuffer,
            uint nInBufferSize, IntPtr lpOutBuffer, uint nOutBufferSize, IntPtr lpBytesReturned, IntPtr lpOverlapped);
        [DllImport("Kernel32.dll")]
        static extern bool CloseHandle(IntPtr hObject);
#endif



        private IEnumerator CreateFolderOnServer(string folderPath, Action callback)
        {
            yield return ServerManager.CreateFolder(folderPath);
            callback?.Invoke();
        }

        private IEnumerator CopySessionConfigFolderToDataFolder()
        {
            string sourcePath = ServerManager.SessionConfigFolderPath;
            string destinationPath = $"{ServerManager.SessionDataFolderPath}/SessionSettings";
            yield return ServerManager.CopyFolder(sourcePath, destinationPath);
        }
        public void OnGUI()
        {
            if (CameraMirrorTexture == null) return;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), CameraMirrorTexture);
        }
    }

    
}
