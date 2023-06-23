using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
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
using Random = UnityEngine.Random;
using UnityEngine.InputSystem;
using TMPro;
using Tobii.Research.Unity;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Button = UnityEngine.UI.Button;
using USE_DisplayManagement;
using Tobii.Research;
using UnityEngine.Serialization;
using static UnityEngine.UI.CanvasScaler;
using static SelectionTracking.SelectionTracker;
using UnityEngine.UIElements;
using USE_ExperimentTemplate_Trial;
using Image = UnityEngine.UI.Image;
using Tobii.Research.Unity.CodeExamples;

using UnityEditor;
using System.Threading.Tasks;
using ConfigDynamicUI;
//using UnityEngine.Windows.WebCam;


namespace USE_ExperimentTemplate_Session
{
    public class ControlLevel_Session_Template : ControlLevel
    {
        [HideInInspector] public int SessionId_SQL;

        private bool IsHuman;

        [HideInInspector] public float ParticipantDistance_CM;
        [HideInInspector] public float ShotgunRaycastSpacing_DVA;
        [HideInInspector] public float ShotgunRaycastCircleSize_DVA;

        [HideInInspector] public bool TasksFinished;

        protected SummaryData SummaryData;
        protected SessionData SessionData;
        protected SerialSentData SerialSentData;
        protected SerialRecvData SerialRecvData;
        private SessionDataControllers SessionDataControllers;
        private bool StoreData;
        private bool MacMainDisplayBuild;
        [HideInInspector] public string SubjectID, SessionID, SessionDataPath, FilePrefix;
        public string SessionLevelDataPath;
        public string TaskSelectionSceneName;

        protected List<ControlLevel_Task_Template> ActiveTaskLevels;
        public ControlLevel_Task_Template CurrentTask;
        public ControlLevel_Task_Template GazeCalibrationTaskLevel;
        private OrderedDictionary TaskMappings;
        private string ContextExternalFilePath;
        private string TaskIconsFolderPath;
        [HideInInspector]public Vector3[] TaskIconLocations;
        private Dictionary<string, string> TaskIcons;
        protected int taskCount;
        private float TaskSelectionTimeout;

        [HideInInspector] public int LongRewardHotKeyPulseSize;
        [HideInInspector] public int LongRewardHotKeyNumPulses;
        [HideInInspector] public int RewardHotKeyPulseSize;
        [HideInInspector] public int RewardHotKeyNumPulses;

        //For Loading config information
        public SessionDetails SessionDetails;
        public LocateFile LocateFile;

        private SerialPortThreaded SerialPortController;
        private SyncBoxController SyncBoxController;
        private EventCodeManager EventCodeManager;
        [HideInInspector] public SelectionTracker SelectionTracker;
        private SelectionTracker.SelectionHandler SelectionHandler;
        private GameObject InputManager;
        private MouseTracker MouseTracker;
        private GazeTracker GazeTracker;
        private GameObject InputTrackers;
        protected FrameData FrameData;
        protected USE_ExperimentTemplate_Data.GazeData GazeData;
        
        // EyeTracker Variables
        public TobiiEyeTrackerController TobiiEyeTrackerController;
        private MonitorDetails MonitorDetails;
        private ScreenDetails ScreenDetails;
    //    public EyeTrackerData_Namespace.TobiiGazeSample TobiiGazeSample;


        private Camera SessionCam;
        private Camera MirrorCam;
        private ExperimenterDisplayController ExperimenterDisplayController;
        [HideInInspector] public RenderTexture CameraMirrorTexture;

        private string configFileFolder;
        private bool TaskSceneLoaded, SceneLoading, GuidedTaskSelection, EyeTrackerActive;

        private bool SerialPortActive, SyncBoxActive, EventCodesActive, RewardPulsesActive, SonicationActive;
        private string EyetrackerType, SelectionType;
        private Dictionary<string, EventCode> SessionEventCodes;
        private List<string> selectedConfigsList = new List<string>();
        private SessionInfoPanel SessionInfoPanel;
        public StringBuilder PreviousTaskSummaryString = new StringBuilder();

        public DisplayController DisplayController;

        [HideInInspector] public GameObject TaskButtonsContainer;

        //Set in inspector
        public GameObject TaskSelection_Starfield;
        public GameObject HumanVersionToggleButton;
        public GameObject HumanStartPanelPrefab;
        public GameObject TaskSelectionCanvasGO;
        public GameObject ToggleAudioButton;
        public GameObject StartButtonPrefabGO;
        public AudioClip TaskSelection_HumanAudio;

        [HideInInspector] public float audioPlaybackSpot;

        [HideInInspector] public AudioSource TaskSelection_AudioSource;

        [HideInInspector] public HumanStartPanel HumanStartPanel;
        [HideInInspector] public USE_StartButton USE_StartButton;



        public override void LoadSettings()
        {
            HumanStartPanel = gameObject.AddComponent<HumanStartPanel>();
            HumanStartPanel.SetSessionLevel(this);
            HumanStartPanel.HumanStartPanelPrefab = HumanStartPanelPrefab;

            USE_StartButton = gameObject.AddComponent<USE_StartButton>();
            USE_StartButton.StartButtonPrefab = StartButtonPrefabGO;


            // Set the name of the data file given input into init screen
            SubjectID = SessionDetails.GetItemValue("SubjectID");
            SessionID = SessionDetails.GetItemValue("SessionID");

            string sessionDataFolder = ServerManager.GetSessionDataFolder();
            if(!string.IsNullOrEmpty(sessionDataFolder))
                FilePrefix = sessionDataFolder.Split(new string[] { "__" }, 2, StringSplitOptions.None)[1];
            else
                FilePrefix = "Session_" + SessionID + "__Subject_" + SubjectID + "__" + DateTime.Now.ToString("MM_dd_yy__HH_mm_ss");


            if (SessionValues.WebBuild)
            {
                SessionDataPath = ServerManager.SessionDataFolderPath;
                TaskIconsFolderPath = "DefaultResources/TaskIcons"; //Currently having web build use in house task icons instead of loading from server. 
                ContextExternalFilePath = "DefaultResources/Contexts"; //TEMPORARILY HAVING WEB BUILD USE DEFAUULT CONTEXTS

                if (SessionValues.UseDefaultConfigs)
                {
                    //ContextExternalFilePath = "Assets/_USE_Session/Resources/DefaultResources/Contexts";
                    configFileFolder = Application.persistentDataPath + Path.DirectorySeparatorChar + "M_USE_DefaultConfigs";
                    WriteSessionConfigsToPersistantDataPath();
                    SessionSettings.ImportSettings_MultipleType("Session", LocateFile.FindFilePathInExternalFolder(configFileFolder, "*SessionConfig*"));
                    LoadSessionConfigSettings();
                }
                else //Using Server Configs:
                {
                    //ContextExternalFilePath = "Resources/Contexts"; //path from root server folder
                    //TaskIconsFolderPath = "Resources/TaskIcons"; //un comment if end up wanting to load from server instead (and also remove the one above)
                    configFileFolder = ServerManager.SessionConfigFolderPath;
                    StartCoroutine(ServerManager.GetFileStringAsync(ServerManager.SessionConfigFolderPath, "SessionConfig", result =>
                    {
                        if (!string.IsNullOrEmpty(result))
                        {
                            SessionSettings.ImportSettings_MultipleType("Session", configFileFolder, result);
                            LoadSessionConfigSettings();
                        }
                        else
                            Debug.Log("SESSION CONFIG COROUTINE RESULT IS EMPTY!!!");
                    }));
                }
            }
            else //Normal Build:
            {
                configFileFolder = LocateFile.GetPath("Config Folder");
                SessionDataPath = LocateFile.GetPath("Data Folder") + Path.DirectorySeparatorChar + FilePrefix;
                SessionSettings.ImportSettings_MultipleType("Session", LocateFile.FindFilePathInExternalFolder(configFileFolder, "*SessionConfig*"));
                LoadSessionConfigSettings();
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

            SessionDataControllers = new SessionDataControllers(GameObject.Find("DataControllers"));
            ActiveTaskLevels = new List<ControlLevel_Task_Template>();//new Dictionary<string, ControlLevel_Task_Template>();

            SessionCam = Camera.main;


#if (UNITY_WEBGL)
                //If WebGL Build, immedietely load taskselection screen and set initCam inactive. Otherwise create ExperimenterDisplay
                GameObject initCamGO = GameObject.Find("InitCamera");
                initCamGO.SetActive(false);
                TaskSelection_Starfield.SetActive(true);
#else

            TaskSelection_Starfield.SetActive(false);
            GameObject experimenterDisplay = Instantiate(Resources.Load<GameObject>("Default_ExperimenterDisplay"));
            experimenterDisplay.name = "ExperimenterDisplay";
            ExperimenterDisplayController = experimenterDisplay.AddComponent<ExperimenterDisplayController>();
            experimenterDisplay.AddComponent<PreserveObject>();
            ExperimenterDisplayController.InitializeExperimenterDisplay(this, experimenterDisplay);

            GameObject mirrorCamGO = new GameObject("MirrorCamera");
            Camera MirrorCam = mirrorCamGO.AddComponent<Camera>();
            MirrorCam.CopyFrom(Camera.main);
            MirrorCam.cullingMask = 0;

            RawImage mainCameraCopy_Image = GameObject.Find("MainCameraCopy").GetComponent<RawImage>();

#endif

            // Create the input tracker object
            InputManager = new GameObject("InputManager");
            InputManager.SetActive(true);

            InputTrackers = Instantiate(Resources.Load<GameObject>("InputTrackers"), InputManager.transform);
            MouseTracker = InputTrackers.GetComponent<MouseTracker>();
            GazeTracker = InputTrackers.GetComponent<GazeTracker>();




            SelectionTracker = new SelectionTracker();
            if (SelectionType.ToLower().Equals("gaze"))
            {
                SelectionHandler = SelectionTracker.SetupSelectionHandler("session", "GazeSelection", GazeTracker, selectTask, loadTask);
                SelectionHandler.MinDuration = 0.7f;
            }
            else
            {
                SelectionHandler = SelectionTracker.SetupSelectionHandler("session", "MouseButton0Click", MouseTracker, selectTask, loadTask);
                MouseTracker.enabled = true;
                SelectionHandler.MinDuration = 0.01f;
                SelectionHandler.MaxDuration = 2f;
            }

            if (EyeTrackerActive)
            {
                if (GameObject.Find("TobiiEyeTrackerController") == null)
                {
                    // gets called once when finding and creating the tobii eye tracker prefabs
                    GameObject TobiiEyeTrackerControllerGO = new GameObject("TobiiEyeTrackerController");
                    TobiiEyeTrackerController = TobiiEyeTrackerControllerGO.AddComponent<TobiiEyeTrackerController>();
                    GameObject TrackBoxGO = Instantiate(Resources.Load<GameObject>("TrackBoxGuide"), TobiiEyeTrackerControllerGO.transform);
                    GameObject EyeTrackerGO = Instantiate(Resources.Load<GameObject>("EyeTracker"), TobiiEyeTrackerControllerGO.transform);
                    GameObject CalibrationGO = Instantiate(Resources.Load<GameObject>("GazeCalibration"));
                    GazeTracker.enabled = true;


                    /*  //  GameObject GazeTrail = Instantiate(Resources.Load<GameObject>("GazeTrail"), TobiiEyeTrackerControllerGO.transform); 
                    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.transform.SetParent(TobiiEyeTrackerControllerGO.transform, true);
                    // Position and scale the cube as desired
                    cube.transform.position = new Vector3(0f, 1f, 60f);
                    cube.transform.localScale = new Vector3(106f, 62f, 0.1f);
                    cube.SetActive(false);*/

                }
            }
            if (MonitorDetails != null && ScreenDetails != null)
            {
                USE_CoordinateConverter.ScreenDetails = new ScreenDetails(ScreenDetails.LowerLeft_Cm, ScreenDetails.UpperRight_Cm, ScreenDetails.PixelResolution);
                USE_CoordinateConverter.MonitorDetails = new MonitorDetails(MonitorDetails.PixelResolution, MonitorDetails.CmSize);
                USE_CoordinateConverter.SetMonitorDetails(USE_CoordinateConverter.MonitorDetails);
                USE_CoordinateConverter.SetScreenDetails(USE_CoordinateConverter.ScreenDetails);
            }

            // Instantiating Task Selection Frame Data
            // Instantiate normal session data controller for all tasks


            bool waitForSerialPort = false;
            bool taskAutomaticallySelected = false;
            setupSession.AddDefaultInitializationMethod(() =>
            {
                SessionData.CreateFile();

                //Create Session Settings folder inside Data Folder: ----------------------------------------------------------------------------------------
                if (SessionValues.WebBuild)
                {
                    if (!Application.isEditor) //DOESNT CURRENTLY WORK FOR DEFAULT CONFIGS CUZ THATS NOT A CONFIG ON THE SERVER, so it cant find it to copy from
                    {
                        StartCoroutine(CreateFolderOnServer(SessionDataPath + Path.DirectorySeparatorChar + "SessionSettings", () =>
                        {
                            StartCoroutine(CopySessionConfigFolderToDataFolder()); //Copy Session Config folder to Data folder so that the settings are stored:
                        }));
                    }
                }
                else
                {
                    string sessionSettingsFolderPath = SessionDataPath + Path.DirectorySeparatorChar + "SessionSettings";
                    System.IO.Directory.CreateDirectory(sessionSettingsFolderPath);
                    SessionSettings.StoreSettings(sessionSettingsFolderPath + Path.DirectorySeparatorChar);
                }

                EventCodeManager = GameObject.Find("MiscScripts").GetComponent<EventCodeManager>(); //new EventCodeManager();
                if (SerialPortActive)
                {

                    SerialPortController = new SerialPortThreaded();
                    if (SyncBoxActive)
                    {
                        SyncBoxController = new SyncBoxController();
                        SyncBoxController.serialPortController = SerialPortController;
                        SerialSentData.sc = SerialPortController;
                        SerialRecvData.sc = SerialPortController;
                    }

                    if (EventCodesActive)
                    {
                        EventCodeManager.SyncBoxController = SyncBoxController;
                        EventCodeManager.codesActive = true;
                    }
                    waitForSerialPort = true;
                    if (SessionSettings.SettingExists("Session", "SerialPortAddress"))
                        SerialPortController.SerialPortAddress =
                            (string)SessionSettings.Get("Session", "SerialPortAddress");
                    else if (SessionSettings.SettingClassExists("SyncBoxConfig"))
                    {
                        if (SessionSettings.SettingExists("SyncBoxConfig", "SerialPortAddress"))
                            SerialPortController.SerialPortAddress =
                                (string)SessionSettings.Get("SyncBoxConfig", "SerialPortAddress");
                    }

                    if (SessionSettings.SettingExists("Session", "SerialPortSpeed"))
                        SerialPortController.SerialPortSpeed =
                            (int)SessionSettings.Get("Session", "SerialPortSpeed");
                    else if (SessionSettings.SettingClassExists("SyncBoxConfig"))
                    {
                        if (SessionSettings.SettingExists("SyncBoxConfig", "SerialPortSpeed"))
                            SerialPortController.SerialPortSpeed =
                                (int)SessionSettings.Get("SyncBoxConfig", "SerialPortSpeed");
                    }

                    SerialPortController.Initialize();

                }
            });

            int iTask = 0;
            SceneLoading = false;
            string taskName = "";
            AsyncOperation loadScene = null;
            setupSession.AddUpdateMethod(() =>
            {
                if (waitForSerialPort && Time.time - StartTimeAbsolute > SerialPortController.initTimeout / 1000 + 0.5f)
                {
                    if (SyncBoxActive)
                        if (SessionSettings.SettingExists("Session", "SyncBoxInitCommands"))
                        {
                            SyncBoxController.SendCommand(
                                (List<string>)SessionSettings.Get("Session", "SyncBoxInitCommands"));
                        }

                    waitForSerialPort = false;
                }

                if (EyeTrackerActive && GazeCalibrationTaskLevel == null)
                {
                    //Have to add calibration task level as child of calibration state here, because it isn't available prior

                    GazeCalibrationTaskLevel = GameObject.Find("GazeCalibration_Scripts").GetComponent<GazeCalibration_TaskLevel>();
                    PopulateTaskLevel(GazeCalibrationTaskLevel, false);
                    gazeCalibration.AddChildLevel(GazeCalibrationTaskLevel);
                    GazeCalibrationTaskLevel.TrialLevel.TaskLevel = GazeCalibrationTaskLevel;
                    GazeCalibrationTaskLevel.gameObject.SetActive(false);
                }


                if (iTask < TaskMappings.Count)
                {
                    if (!SceneLoading)
                    {
                        //AsyncOperation loadScene;
                        SceneLoading = true;
                        taskName = (string)TaskMappings[iTask];
                        loadScene = SceneManager.LoadSceneAsync(taskName, LoadSceneMode.Additive);
                        string configName = TaskMappings.Cast<DictionaryEntry>().ElementAt(iTask).Key.ToString();
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
            setupSession.AddLateUpdateMethod(() =>
            {
                //AppendSerialData();
            });

            setupSession.SpecifyTermination(() => iTask >= TaskMappings.Count && !waitForSerialPort && EyeTrackerActive, gazeCalibration);
            setupSession.SpecifyTermination(() => iTask >= TaskMappings.Count && !waitForSerialPort && !EyeTrackerActive, selectTask);

            setupSession.AddDefaultTerminationMethod(() =>
            {
                SessionSettings.Save();
                if (!SessionValues.WebBuild)
                {
                    GameObject initCamGO = GameObject.Find("InitCamera");
                    initCamGO.SetActive(false);
                    SessionInfoPanel = GameObject.Find("SessionInfoPanel").GetComponent<SessionInfoPanel>();
                }
                EventCodeManager.SendCodeImmediate(SessionEventCodes["SetupSessionEnds"]);
            });

            // Canvas[] TaskSelectionCanvasses = null;
            gazeCalibration.AddInitializationMethod(() =>
            {
                FrameData.gameObject.SetActive(false);

                GazeCalibrationTaskLevel.TaskCam = Camera.main;
                // GazeCalibrationTaskLevel.ConfigName = "GazeCalibration";
                GazeCalibrationTaskLevel.TrialLevel.runCalibration = true;
                ExperimenterDisplayController.ResetTask(GazeCalibrationTaskLevel, GazeCalibrationTaskLevel.TrialLevel);

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
                GameObject.Find("GazeCalibration(Clone)").transform.Find("GazeCalibration_Canvas").gameObject.SetActive(false);
                GameObject.Find("GazeCalibration(Clone)").transform.Find("GazeCalibration_Scripts").gameObject.SetActive(false);
                if (GazeCalibrationTaskLevel.EyeTrackerActive && TobiiEyeTrackerController.Instance.isCalibrating)
                {
                    TobiiEyeTrackerController.Instance.isCalibrating = false;
                    TobiiEyeTrackerController.Instance.ScreenBasedCalibration.LeaveCalibrationMode();
                }

                GazeData.folderPath = SessionLevelDataPath + Path.DirectorySeparatorChar + "GazeData";
                FrameData.gameObject.SetActive(true);
            });

            TaskButtonsContainer = null;
            Dictionary<string, USE_TaskButton> taskButtonsDict = new Dictionary<string, USE_TaskButton>();
            string selectedConfigName = null;
            selectTask.AddUniversalInitializationMethod(() =>
            {
                if (SelectionHandler.AllSelections.Count > 0)
                    SelectionHandler.ClearSelections();
                TaskSelectionCanvasGO.SetActive(true);

                TaskSelection_Starfield.SetActive(IsHuman ? true : false);

#if (!UNITY_WEBGL)
                if (DisplayController.SwitchDisplays) //SwitchDisplay stuff doesnt full work yet!
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

                EventCodeManager.SendCodeImmediate(SessionEventCodes["SelectTaskStarts"]);

                if (SerialPortActive) {
                    SerialSentData.CreateFile();
                    SerialRecvData.CreateFile();
                }

                if (EyeTrackerActive)
                {
                    GazeData.CreateFile();
                }

                FrameData.CreateFile();

                SessionSettings.Restore();
                selectedConfigName = null;
                taskAutomaticallySelected = false; // gives another chance to select even if previous task loading was due to timeout

                SessionCam.gameObject.SetActive(true);


                // Don't show the task buttons if we encountered an error during setup
                if (LogPanel.HasError())
                    return;

                SceneLoading = true;
                if (taskCount >= TaskMappings.Count)
                {
                    TasksFinished = true;
                    return;
                }

                if (TaskButtonsContainer != null)
                {
                    TaskButtonsContainer.SetActive(true);
                    if (GuidedTaskSelection)
                    {
                        // if guided selection, we need to adjust the shading of the icons and buttons after the task buttons object is already created                        
                        string key = TaskMappings.Keys.Cast<string>().ElementAt(taskCount);
                        foreach (KeyValuePair<string, USE_TaskButton> taskButton in taskButtonsDict)
                        {
                            if (taskButton.Key == key)
                            {
                                taskButton.Value.TaskButtonGO.GetComponent<RawImage>().color = new Color(1f, 1f, 1f, 1f);
                                taskButton.Value.TaskButtonGO.GetComponent<RawImage>().raycastTarget = true;
                                taskButton.Value.TaskButtonGO.AddComponent<HoverEffect>(); //Adding HoverEffect to make button bigger when hovered over. 
                            }
                            else
                            {
                                taskButton.Value.TaskButtonGO.GetComponent<RawImage>().color = new Color(.5f, .5f, .5f, .35f);
                                taskButton.Value.TaskButtonGO.GetComponent<RawImage>().raycastTarget = false;
                                HoverEffect hoverEffect = taskButton.Value.TaskButtonGO.GetComponent<HoverEffect>();
                                if (hoverEffect != null)
                                    Destroy(hoverEffect);
                            }
                        }
                    }
                    return;
                }
                // Container for all the task buttons
                TaskButtonsContainer = new GameObject("TaskButtons");
                TaskButtonsContainer.transform.parent = GameObject.Find("TaskSelectionCanvas").transform;
                TaskButtonsContainer.transform.localPosition = Vector3.zero;
                TaskButtonsContainer.transform.localScale = Vector3.one * 1.06f;

                // We'll use height for the calculations because it is generally smaller than the width
                int numTasks = TaskMappings.Count;
                float buttonSize;
                float buttonSpacing;
                if (MacMainDisplayBuild && !Application.isEditor)
                {
                    buttonSize = 249f;
                    buttonSpacing = 28.5f;
                }
                else
                {
                    buttonSize = 188f;
                    buttonSpacing = 18f;
                }

                float buttonsWidth = numTasks * buttonSize + (numTasks - 1) * buttonSpacing;
                float buttonStartX = (buttonSize - buttonsWidth) / 2;

                float buttonY = 0f;
                //if(IsHuman)
                //    buttonY = -125f;

                if (TaskIconLocations.Count() != numTasks) //If user didn't specify in config, Generate default locations:
                {
                    TaskIconLocations = new Vector3[numTasks];
                    for (int i = 0; i < numTasks; i++)
                    {
                        TaskIconLocations[i] = new Vector3(buttonStartX, buttonY, 0);
                        buttonStartX += buttonSize + buttonSpacing;
                    }
                }

                int count = 0;

                //Create each individual task icon
                foreach (DictionaryEntry task in TaskMappings)
                {
                    // Assigns configName and taskName according to Session Config Task Mappings
                    string configName = (string)task.Key;
                    string taskName = (string)task.Value;

                    USE_TaskButton taskButton = new USE_TaskButton(TaskButtonsContainer.transform.parent.GetComponent<Canvas>(), TaskIconLocations[count], buttonSize, configName);
                    taskButton.TaskButtonGO.transform.SetParent(TaskButtonsContainer.transform, false);
                    taskButtonsDict.Add(configName, taskButton);

                    string taskFolderPath = GetConfigFolderPath(configName);

                    if (!SessionValues.WebBuild)
                    {
                        if (!Directory.Exists(taskFolderPath))
                        {
                            Destroy(taskButton);
                            throw new DirectoryNotFoundException($"Task folder for '{configName}' at '{taskFolderPath}' does not exist.");
                        }
                    }

                    RawImage image = taskButtonsDict[configName].TaskButtonGO.GetComponent<RawImage>();

                    if (SessionValues.WebBuild)
                        image.texture = Resources.Load<Texture2D>(TaskIconsFolderPath + "/" + taskName);
                    else
                        image.texture = LoadPNG(TaskIconsFolderPath + Path.DirectorySeparatorChar + taskName + ".png");

                    if (GuidedTaskSelection)
                    {
                        // If guided task selection, only make the next icon interactable
                        string key = TaskMappings.Keys.Cast<string>().ElementAt(taskCount);
                        

                        if (configName == key)
                        {
                            image.color = new Color(1f, 1f, 1f, 1f);
                            taskButtonsDict[configName].TaskButtonGO.GetComponent<RawImage>().raycastTarget = true;
                            taskButton.TaskButtonGO.AddComponent<HoverEffect>(); //Adding HoverEffect to make button bigger when hovered over. 
                        }
                        else
                        {
                            image.color = new Color(.5f, .5f, .5f, .35f);
                            taskButtonsDict[configName].TaskButtonGO.GetComponent<RawImage>().raycastTarget = false;
                        }
                    }
                    else
                    {
                        // If not guided task selection, make all icons interactable
                        taskButtonsDict[configName].TaskButtonGO.GetComponent<RawImage>().raycastTarget = true;
                        taskButton.TaskButtonGO.AddComponent<HoverEffect>();
                        
                    }
                    count++;
                }

                if (IsHuman)
                {
                    HumanVersionToggleButton.SetActive(true);
                }
            });

            selectTask.AddFixedUpdateMethod(() =>
            {
                SelectionTracker.UpdateActiveSelections();

                if (SelectionHandler.SuccessfulSelections.Count > 0)
                {
                    selectedConfigName = SelectionHandler.LastSuccessfulSelection.SelectedGameObject?.GetComponent<USE_TaskButton>()?.configName;
                    Debug.Log("SELECTED CONFIGNAME: " + selectedConfigName);
                    if (selectedConfigName != null)
                        taskAutomaticallySelected = false;
                }
            });

            selectTask.AddLateUpdateMethod(() =>
            {
                AppendSerialData();
                FrameData.AppendDataToBuffer();
            });

            selectTask.SpecifyTermination(() => selectedConfigName != null, loadTask);

            // Don't have automatic task selection if we encountered an error during setup
            if (TaskSelectionTimeout >= 0 && !LogPanel.HasError())
            {
                selectTask.AddTimer(TaskSelectionTimeout, loadTask, () =>
                {
                    foreach (DictionaryEntry task in TaskMappings)
                    {
                        //Find the next task in the list that is still interactable
                        string configName = (string)task.Key;
                        string taskName = (string)task.Value;

                        // If the next task button in the task mappings is not interactable, skip until the next available config is found
                        if (!(taskButtonsDict[configName].TaskButtonGO.GetComponent<RawImage>().raycastTarget)) continue;
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
                USE_TaskButton taskButton = taskButtonsDict[selectedConfigName];
                RawImage image = taskButton.TaskButtonGO.GetComponent<RawImage>();
                

                if (!SessionValues.WebBuild) //Let patients play same task as many times as they want
                {
                    taskButton.TaskButtonGO.GetComponent<RawImage>().color = new Color(.5f, .5f, .5f, .35f);
                    taskButton.TaskButtonGO.GetComponent<RawImage>().raycastTarget = false;
                    HoverEffect hoverEffect = taskButton.TaskButtonGO.GetComponent<HoverEffect>();
                    if (hoverEffect != null)
                        Destroy(hoverEffect);
                }

                string taskName = (string)TaskMappings[selectedConfigName];
                loadScene = SceneManager.LoadSceneAsync(taskName, LoadSceneMode.Additive);
                loadScene.completed += (_) =>
                {
                    OnSceneLoaded(selectedConfigName, false);
                    CurrentTask = ActiveTaskLevels.Find((task) => task.ConfigName == selectedConfigName);
                    //selectedConfigsList.Add(CurrentTask.ConfigName);  
                };
            });

            loadTask.AddFixedUpdateMethod(() =>
            {
                SelectionTracker.UpdateActiveSelections();
            });

            loadTask.AddLateUpdateMethod(() =>
            {
                AppendSerialData();
                FrameData.AppendDataToBuffer();
            });

            loadTask.SpecifyTermination(() => CurrentTask != null && CurrentTask.TaskLevelDefined, runTask, () =>
            {
                //if(IsHuman)
                //{
                //    TaskSelection_AudioSource.Stop();
                //    Destroy(GetComponent<AudioListener>());
                //}

                TaskSelection_Starfield.SetActive(false);

                runTask.AddChildLevel(CurrentTask);
                if (CameraMirrorTexture != null)
                    CameraMirrorTexture.Release();
                SceneManager.SetActiveScene(SceneManager.GetSceneByName(CurrentTask.TaskName));
                CurrentTask.TrialLevel.TaskLevel = CurrentTask;
                if (ExperimenterDisplayController != null)
                    ExperimenterDisplayController.ResetTask(CurrentTask, CurrentTask.TrialLevel);

                if (SerialPortActive)
                {
                    AppendSerialData();
                    SerialRecvData.AppendDataToFile();
                    SerialSentData.AppendDataToFile();
                    SerialRecvData.CreateNewTaskIndexedFolder((taskCount + 1) * 2, SessionDataPath, "SerialRecvData", CurrentTask.TaskName);
                    SerialSentData.CreateNewTaskIndexedFolder((taskCount + 1) * 2, SessionDataPath, "SerialSentData", CurrentTask.TaskName);
                }
            });

            //automatically finish tasks after running one - placeholder for proper selection
            //runTask.AddLateUpdateMethod
            runTask.AddUniversalInitializationMethod(() =>
            {
                SessionCam.gameObject.SetActive(false);

                EventCodeManager.SendCodeImmediate(SessionEventCodes["RunTaskStarts"]);

#if (!UNITY_WEBGL)

                if (DisplayController.SwitchDisplays)
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

            if (EventCodesActive)
            {
                runTask.AddFixedUpdateMethod(() => EventCodeManager.EventCodeFixedUpdate());
                // runTask.AddLateUpdateMethod(() => EventCodeManager.EventCodeLateUpdate());
            }


            runTask.AddLateUpdateMethod(() =>
            {
                SelectionTracker.UpdateActiveSelections();
                AppendSerialData();
            });

            runTask.SpecifyTermination(() => CurrentTask.Terminated, selectTask, () =>
            {
                if (PreviousTaskSummaryString != null && CurrentTask.CurrentTaskSummaryString != null)
                    PreviousTaskSummaryString.Insert(0, CurrentTask.CurrentTaskSummaryString);

                SummaryData.AddTaskRunData(CurrentTask.ConfigName, CurrentTask, CurrentTask.GetSummaryData());

                SessionData.AppendDataToBuffer();
                SessionData.AppendDataToFile();

                SceneManager.UnloadSceneAsync(CurrentTask.TaskName);
                SceneManager.SetActiveScene(SceneManager.GetSceneByName(TaskSelectionSceneName));

                ActiveTaskLevels.Remove(CurrentTask);

                if (CameraMirrorTexture != null)
                    CameraMirrorTexture.Release();

                if (ExperimenterDisplayController != null)
                    ExperimenterDisplayController.ResetTask(null, null);

                if (EyeTrackerActive && TobiiEyeTrackerController.Instance.isCalibrating)
                {
                    TobiiEyeTrackerController.Instance.isCalibrating = false;
                    TobiiEyeTrackerController.Instance.ScreenBasedCalibration.LeaveCalibrationMode();
                }

                taskCount++;

                if (SerialPortActive)
                {
                    SerialRecvData.CreateNewTaskIndexedFolder((taskCount + 1) * 2 - 1, SessionDataPath, "SerialRecvData", "SessionLevel");
                    SerialSentData.CreateNewTaskIndexedFolder((taskCount + 1) * 2 - 1, SessionDataPath, "SerialSentData", "SessionLevel");


                }
                //     SessionDataPath + Path.DirectorySeparatorChar +
                //                             SerialRecvData.GetNiceIntegers(4, taskCount + 1 * 2 - 1) + "_TaskSelection";
                // SerialSentData.folderPath = SessionDataPath + Path.DirectorySeparatorChar +
                //                             SerialSentData.GetNiceIntegers(4, taskCount + 1 * 2 - 1) + "_TaskSelection";


                if (EyeTrackerActive)
                {
                    GazeData.CreateNewTaskIndexedFolder((taskCount + 1) * 2 - 1, SessionLevelDataPath, "GazeData", "SessionLevel");
                    GazeData.fileName = FilePrefix + "__GazeData" + GazeData.GetNiceIntegers(4, (taskCount + 1) * 2 - 1) + "SessionLevel.txt";
                }

                FrameData.CreateNewTaskIndexedFolder((taskCount + 1) * 2 - 1, SessionLevelDataPath, "FrameData", "SessionLevel");
                FrameData.fileName = FilePrefix + "__FrameData" + FrameData.GetNiceIntegers(4, (taskCount + 1) * 2 - 1) + "SessionLevel.txt";

                FrameData.gameObject.SetActive(true);
            });

            finishSession.AddInitializationMethod(() =>
            {
                EventCodeManager.SendCodeImmediate(SessionEventCodes["FinishSessionStarts"]);
            });

            finishSession.SpecifyTermination(() => true, () => null, () =>
            {
                SessionData.AppendDataToBuffer();
                SessionData.AppendDataToFile();

                AppendSerialData();
                if (SerialPortActive)
                {
                    SerialSentData.AppendDataToFile();
                    SerialRecvData.AppendDataToFile();
                }

                if (EyeTrackerActive)
                    GazeData.AppendDataToFile();

                FrameData.AppendDataToFile();
            });

            SessionData = (SessionData)SessionDataControllers.InstantiateDataController<SessionData>
                ("SessionData", StoreData, SessionDataPath); //SessionDataControllers.InstantiateSessionData(StoreData, SessionDataPath);
            SessionData.fileName = FilePrefix + "__SessionData.txt";
            SessionData.sessionLevel = this;
            SessionData.InitDataController();
            SessionData.ManuallyDefine();

            SessionData.AddDatum("SelectedTaskConfigName", () => selectedConfigName);
            SessionData.AddDatum("TaskAutomaticallySelected", () => taskAutomaticallySelected);

            if (SerialPortActive)
            {
                SerialSentData = (SerialSentData)SessionDataControllers.InstantiateDataController<SerialSentData>
                    ("SerialSentData", StoreData, SessionDataPath + Path.DirectorySeparatorChar + "SerialSentData"
                                                  + Path.DirectorySeparatorChar + "0001_TaskSelection");
                SerialSentData.fileName = FilePrefix + "__SerialSentData_0001_TaskSelection.txt";
                SerialSentData.sessionLevel = this;
                SerialSentData.InitDataController();
                SerialSentData.ManuallyDefine();

                SerialRecvData = (SerialRecvData)SessionDataControllers.InstantiateDataController<SerialRecvData>
                    ("SerialRecvData", StoreData, SessionDataPath + Path.DirectorySeparatorChar + "SerialRecvData"
                                                  + Path.DirectorySeparatorChar + "0001_TaskSelection");
                SerialRecvData.fileName = FilePrefix + "__SerialRecvData_0001_TaskSelection.txt";
                SerialRecvData.sessionLevel = this;
                SerialRecvData.InitDataController();
                SerialRecvData.ManuallyDefine();
            }

            SummaryData.Init(StoreData, SessionDataPath);

            SessionLevelDataPath = SessionDataPath + Path.DirectorySeparatorChar + "SessionLevel";
            FrameData = (FrameData)SessionDataControllers.InstantiateDataController<FrameData>("FrameData", "SessionLevel", StoreData, SessionLevelDataPath + Path.DirectorySeparatorChar + "FrameData");
            FrameData.fileName = "SessionLevel__FrameData.txt";
            FrameData.sessionLevel = this;
            FrameData.InitDataController();
            FrameData.ManuallyDefine();

            if (EventCodesActive)
                FrameData.AddEventCodeColumns();

            if (EyeTrackerActive)
            {
                GazeData = (USE_ExperimentTemplate_Data.GazeData)SessionDataControllers.InstantiateDataController<USE_ExperimentTemplate_Data.GazeData>("GazeData", "SessionLevel", StoreData, SessionLevelDataPath + Path.DirectorySeparatorChar + "GazeData");

                GazeData.fileName = "SessionLevel__GazeData.txt";
                GazeData.sessionLevel = this;
                GazeData.InitDataController();
                GazeData.ManuallyDefine();
                TobiiEyeTrackerController.GazeData = GazeData;
                GazeTracker.Init(FrameData, 0);

            }
            MouseTracker.Init(FrameData, 0);
        }

        private void LoadSessionConfigSettings()
        {
            if (SessionSettings.SettingExists("Session", "SyncBoxActive"))
                SyncBoxActive = (bool)SessionSettings.Get("Session", "SyncBoxActive");
            else
                SyncBoxActive = false;

            if (SessionSettings.SettingExists("Session", "EventCodesActive"))
                EventCodesActive = (bool)SessionSettings.Get("Session", "EventCodesActive");
            else
                EventCodesActive = false;

            if (SessionSettings.SettingExists("Session", "RewardPulsesActive"))
                RewardPulsesActive = (bool)SessionSettings.Get("Session", "RewardPulsesActive");
            else
                RewardPulsesActive = false;

            if (SessionSettings.SettingExists("Session", "SonicationActive"))
                SonicationActive = (bool)SessionSettings.Get("Session", "SonicationActive");
            else
                SonicationActive = false;

            if (SessionSettings.SettingExists("Session", "LongRewardHotKeyPulseSize"))
                LongRewardHotKeyPulseSize = (int)SessionSettings.Get("Session", "LongRewardHotKeyPulseSize");
            else
                LongRewardHotKeyPulseSize = 500;

            if (SessionSettings.SettingExists("Session", "LongRewardHotKeyNumPulses"))
                LongRewardHotKeyNumPulses = (int)SessionSettings.Get("Session", "LongRewardHotKeyNumPulses");
            else
                LongRewardHotKeyNumPulses = 1;

            if (SessionSettings.SettingExists("Session", "RewardHotKeyPulseSize"))
                RewardHotKeyPulseSize = (int)SessionSettings.Get("Session", "RewardHotKeyPulseSize");
            else
                RewardHotKeyPulseSize = 250;

            if (SessionSettings.SettingExists("Session", "RewardHotKeyNumPulses"))
                RewardHotKeyNumPulses = (int)SessionSettings.Get("Session", "RewardHotKeyNumPulses");
            else
                RewardHotKeyNumPulses = 1;
            if (SessionSettings.SettingExists("Session", "EyeTrackerActive"))
                EyeTrackerActive = (bool)SessionSettings.Get("Session", "EyeTrackerActive");
            else
                EyeTrackerActive = false;
            if (SessionSettings.SettingExists("Session", "SelectionType"))
                SelectionType = (string)SessionSettings.Get("Session", "SelectionType");
            else
                SelectionType = "mouse";

            //MAKE SURE SYNCBOX INACTIVE FOR WEB BUILD (Can eventually remove this once thilo provides web build session configs with it marked false)
            if (SessionValues.WebBuild)
                SyncBoxActive = false;

            if (SyncBoxActive)
                SerialPortActive = true;


            //Load the Session Event Code Config file --------------------------------------------------------------------------------------------------
            string eventCodeFileString = "";

            if(SessionValues.WebBuild && !SessionValues.UseDefaultConfigs)
            {
                StartCoroutine(ServerManager.GetFileStringAsync(ServerManager.SessionConfigFolderPath, "EventCode", result =>
                {
                    SessionSettings.ImportSettings_SingleTypeJSON<Dictionary<string, EventCode>>("EventCodeConfig", configFileFolder, result);
                    SessionEventCodes = (Dictionary<string, EventCode>)SessionSettings.Get("EventCodeConfig");
                }));
            }
            else
            {
                string path = SessionValues.UseDefaultConfigs ? (Application.persistentDataPath + Path.DirectorySeparatorChar + "M_USE_DefaultConfigs") : configFileFolder;
                eventCodeFileString = LocateFile.FindFilePathInExternalFolder(configFileFolder, "*EventCode*");
                if (!string.IsNullOrEmpty(eventCodeFileString))
                {
                    SessionSettings.ImportSettings_SingleTypeJSON<Dictionary<string, EventCode>>("EventCodeConfig", eventCodeFileString);
                    SessionEventCodes = (Dictionary<string, EventCode>)SessionSettings.Get("EventCodeConfig");
                    //EventCodesActive = true;
                }
                else if (EventCodesActive)
                    Debug.LogWarning("EventCodesActive variable set to true in Session Config file but no session level event codes file is given.");
            }


           

            List<string> taskNames;
            if (SessionSettings.SettingExists("Session", "TaskNames"))
            {
                taskNames = (List<string>)SessionSettings.Get("Session", "TaskNames");
                TaskMappings = new OrderedDictionary();
                taskNames.ForEach((taskName) => TaskMappings.Add(taskName, taskName));
            }
            else if (SessionSettings.SettingExists("Session", "TaskMappings"))
                TaskMappings = (OrderedDictionary)SessionSettings.Get("Session", "TaskMappings");
            else if (TaskMappings.Count == 0)
                Debug.LogError("No task names or task mappings specified in Session config file or by other means.");


            if (SessionSettings.SettingExists("Session", "ShotgunRaycastCircleSize_DVA"))
                ShotgunRaycastCircleSize_DVA = (float)SessionSettings.Get("Session", "ShotgunRaycastCircleSize_DVA");
            else
                ShotgunRaycastCircleSize_DVA = 1.25f;

            if (SessionSettings.SettingExists("Session", "ParticipantDistance_CM"))
                ParticipantDistance_CM = (float)SessionSettings.Get("Session", "ParticipantDistance_CM");
            else
                ParticipantDistance_CM = 60f;

            if (SessionSettings.SettingExists("Session", "ShotgunRaycastSpacing_DVA"))
                ShotgunRaycastSpacing_DVA = (float)SessionSettings.Get("Session", "ShotgunRaycastSpacing_DVA");
            else
                ShotgunRaycastSpacing_DVA = .3f;


            if (SessionSettings.SettingExists("Session", "IsHuman"))
                IsHuman = (bool)SessionSettings.Get("Session", "IsHuman");

            if (SessionSettings.SettingExists("Session", "TaskIconLocations"))
                TaskIconLocations = (Vector3[])SessionSettings.Get("Session", "TaskIconLocations");

            if (SessionSettings.SettingExists("Session", "GuidedTaskSelection"))
                GuidedTaskSelection = (bool)SessionSettings.Get("Session", "GuidedTaskSelection");

            if (SessionSettings.SettingExists("Session", "EyeTrackerActive"))
                EyeTrackerActive = (bool)SessionSettings.Get("Session", "EyeTrackerActive");

            if (SessionSettings.SettingExists("Session", "ContextExternalFilePath"))
                ContextExternalFilePath = (string)SessionSettings.Get("Session", "ContextExternalFilePath");

            if (SessionSettings.SettingExists("Session", "TaskIconsFolderPath"))
                TaskIconsFolderPath = (string)SessionSettings.Get("Session", "TaskIconsFolderPath");

            if (SessionSettings.SettingExists("Session", "TaskIcons"))
                TaskIcons = (Dictionary<string, string>)SessionSettings.Get("Session", "TaskIcons");

            if (SessionSettings.SettingExists("Session", "StoreData"))
                StoreData = (bool)SessionSettings.Get("Session", "StoreData");

            if (SessionSettings.SettingExists("Session", "MacMainDisplayBuild"))
                MacMainDisplayBuild = (bool)SessionSettings.Get("Session", "MacMainDisplayBuild");

            if (SessionSettings.SettingExists("Session", "TaskSelectionTimeout"))
                TaskSelectionTimeout = (float)SessionSettings.Get("Session", "TaskSelectionTimeout");


            if (SessionSettings.SettingExists("Session", "SerialPortActive"))
                SerialPortActive = (bool)SessionSettings.Get("Session", "SerialPortActive");

        }

        private void WriteSessionConfigsToPersistantDataPath()
        {
            if (Directory.Exists(configFileFolder))
                Directory.Delete(configFileFolder, true);

            if (!Directory.Exists(configFileFolder))
            {
                Directory.CreateDirectory(configFileFolder);
                List<string> configsToWrite = new List<string>() { "SessionConfig", "EventCodeConfig", "DisplayConfig" };
                foreach (string config in configsToWrite)
                {
                    byte[] textFileBytes = Resources.Load<TextAsset>("DefaultSessionConfigs/" + config).bytes;
                    File.WriteAllBytes(configFileFolder + Path.DirectorySeparatorChar + config + ".txt", textFileBytes);
                }
            }
        }

        public void HandleToggleAudioButtonClick()
        {
            if (TaskSelection_AudioSource.isPlaying)
            {
                audioPlaybackSpot = TaskSelection_AudioSource.time;
                TaskSelection_AudioSource.Stop();
                ToggleAudioButton.transform.Find("Cross").gameObject.SetActive(true);
            }
            else
            {
                TaskSelection_AudioSource.time = audioPlaybackSpot;
                TaskSelection_AudioSource.Play();
                ToggleAudioButton.transform.Find("Cross").gameObject.SetActive(false);

            }
        }
       /* void GetTaskLevelFromString<T>() where T : ControlLevel_Task_Template
            {
                foreach (ControlLevel_Task_Template taskLevel in ActiveTaskLevels)
                    if (taskLevel.GetType() == typeof(T))
                        CurrentTask = taskLevel;
                CurrentTask = null;
            }*/
        public void HandleHumanVersionToggleButtonClick()
        {
            IsHuman = !IsHuman;

            //if(IsHuman)
            //{
            //    gameObject.AddComponent<AudioListener>();
            //    TaskSelection_AudioSource = gameObject.AddComponent<AudioSource>();
            //    TaskSelection_AudioSource.clip = TaskSelection_HumanAudio;
            //    TaskSelection_AudioSource.loop = true;
            //    TaskSelection_AudioSource.time = audioPlaybackSpot;
            //    TaskSelection_AudioSource.Play();
            //}
            //else
            //{
            //    audioPlaybackSpot = TaskSelection_AudioSource.time;
            //    TaskSelection_AudioSource.Stop();
            //    Destroy(GetComponent<AudioListener>());
            //}

            //Change text on button:
            HumanVersionToggleButton.GetComponentInChildren<TextMeshProUGUI>().text = IsHuman ? "Human Version" : "Primate Version";
            //Toggle Starfield:
            TaskSelection_Starfield.SetActive(TaskSelection_Starfield.activeInHierarchy ? false : true);
            //push task buttons up to 0 Y for humans, or back to -100 Y for monkeys
            TaskButtonsContainer.transform.localPosition = new Vector3(TaskButtonsContainer.transform.localPosition.x, TaskButtonsContainer.transform.localPosition.y + (IsHuman ? -125f : 125f), TaskButtonsContainer.transform.localPosition.z);

        }

        private void AppendSerialData()
        {
            if (SerialPortActive)
            {
                if (SerialPortController.BufferCount("sent") > 0)
                {
                    try
                    {
                        // Debug.Log("sentdata: " + SerialSentData);
                        // Debug.Log("sentdata.sc: " + SerialSentData.sc);
                        // Debug.Log("sentdata.sc: " + SerialSentData.sc.BufferCount("sent"));
                        // Debug.Log("sentdata.sc: " + SerialSentData.sc.BufferToString("sent"));
                        SerialSentData.AppendDataToBuffer();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }

                if (SerialPortController.BufferCount("received") > 0)
                {
                    try
                    {
                        // Debug.Log("recvdata: " + SerialRecvData);
                        // Debug.Log("recvdata.sc: " + SerialRecvData.sc);
                        // Debug.Log("recvdata.sc: " + SerialRecvData.sc.BufferCount("received"));
                        // Debug.Log("recvdata.sc: " + SerialRecvData.sc.BufferToString("received"));
                        SerialRecvData.AppendDataToBuffer();
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
                if (SessionValues.UseDefaultConfigs)
                    path = Application.persistentDataPath + Path.DirectorySeparatorChar + "M_USE_DefaultConfigs";
                else
                    path = $"{ServerManager.SessionConfigFolderPath}/{configName}";
            }
            else
            {
                if (!SessionSettings.SettingExists("Session", "ConfigFolderNames"))
                    return configFileFolder + Path.DirectorySeparatorChar + configName;
                else
                {
                    List<string> configFolders =
                        (List<string>)SessionSettings.Get("Session", "ConfigFolderNames");
                    int index = 0;
                    foreach (string k in TaskMappings.Keys)
                    {
                        if (k.Equals(configName)) break;
                        ++index;
                    }
                    path = configFileFolder + Path.DirectorySeparatorChar + configFolders[index];
                }
            }
            return path;
        }

        public ControlLevel_Task_Template PopulateTaskLevel(ControlLevel_Task_Template tl, bool verifyOnly)
        {
            tl.USE_StartButton = USE_StartButton;
            tl.TaskSelectionCanvasGO = TaskSelectionCanvasGO;
            tl.HumanStartPanel = HumanStartPanel;
            tl.IsHuman = IsHuman;
            tl.DisplayController = DisplayController;
            tl.SessionDataControllers = SessionDataControllers;
            tl.LocateFile = LocateFile;
            tl.SessionDataPath = SessionDataPath;
            tl.SessionLevelDataPath = SessionLevelDataPath;


            if (SessionValues.UseDefaultConfigs)
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


            tl.FilePrefix = FilePrefix;
            tl.StoreData = StoreData;
            tl.SubjectID = SubjectID;
            tl.SessionID = SessionID;
            tl.SerialRecvData = SerialRecvData;
            tl.SerialSentData = SerialSentData;
            tl.GazeData = GazeData;

            tl.SelectionTracker = SelectionTracker;
            
            tl.EyeTrackerActive = EyeTrackerActive;

            if (EyeTrackerActive)
            {
                tl.GazeTracker = GazeTracker;
                tl.TobiiEyeTrackerController = TobiiEyeTrackerController;
            }
            tl.MouseTracker = MouseTracker;

            tl.InputManager = InputManager;
            tl.SelectionType = SelectionType;

            tl.ContextExternalFilePath = ContextExternalFilePath;
            tl.SerialPortActive = SerialPortActive;
            tl.SyncBoxActive = SyncBoxActive;
            tl.EventCodeManager = EventCodeManager;
            tl.EventCodesActive = EventCodesActive;
            tl.SessionEventCodes = SessionEventCodes;
            if (SerialPortActive)
                tl.SerialPortController = SerialPortController;
            if (SyncBoxActive)
            {
                SyncBoxController.SessionEventCodes = SessionEventCodes;
                tl.SyncBoxController = SyncBoxController;
            }
            tl.ShotgunRaycastCircleSize_DVA = ShotgunRaycastCircleSize_DVA;
            tl.ShotgunRaycastSpacing_DVA = ShotgunRaycastSpacing_DVA;
            tl.ParticipantDistance_CM = ParticipantDistance_CM;


            if (SessionSettings.SettingExists("Session", "RewardPulsesActive"))
                tl.RewardPulsesActive = (bool)SessionSettings.Get("Session", "RewardPulsesActive");
            else
                tl.RewardPulsesActive = false;

            if (SessionSettings.SettingExists("Session", "SonicationActive"))
                tl.SonicationActive = (bool)SessionSettings.Get("Session", "SonicationActive");
            else;
                tl.SonicationActive = false;


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
            string taskName = (string)TaskMappings[configName];
            var methodInfo = GetType().GetMethod(nameof(this.PrepareTaskLevel));
            
            Type taskType = USE_Tasks_CustomTypes.CustomTaskDictionary[taskName].TaskLevelType;
            MethodInfo prepareTaskLevel = methodInfo.MakeGenericMethod(new Type[] { taskType });
            prepareTaskLevel.Invoke(this, new object[] { configName, verifyOnly });
            // TaskSceneLoaded = true;
            SceneLoading = false;
        }

        public void PrepareTaskLevel<T>(string configName, bool verifyOnly) where T : ControlLevel_Task_Template
        {
            string taskName = (string)TaskMappings[configName];
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

        void OnApplicationQuit()
        {

            if (StoreData)
            {
                string symlinkLocation = LocateFile.GetPath("Data Folder") + Path.DirectorySeparatorChar + "LatestSession";
#if UNITY_STANDALONE_WIN
                uint GENERIC_READ = 0x80000000;
                uint GENERIC_WRITE = 0x40000000;
                uint FILE_SHARE_READ = 0x00000001;
                uint OPEN_EXISTING = 3;
                uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
                uint FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000;
                uint FSCTL_SET_REPARSE_POINT = 0x900A4;
                uint IO_REPARSE_TAG_MOUNT_POINT = 0xA0000003;
                Directory.CreateDirectory(symlinkLocation);

                // Open the file with the correct perms
                IntPtr dirHandle = CreateFile(
                    symlinkLocation,
                    GENERIC_READ | GENERIC_WRITE,
                    FILE_SHARE_READ,
                    IntPtr.Zero,
                    OPEN_EXISTING,
                    FILE_FLAG_BACKUP_SEMANTICS | FILE_FLAG_OPEN_REPARSE_POINT,
                    IntPtr.Zero
                );

                // \??\ indicates that the path should be non-interpreted
                string prefix = @"\??\";
                string substituteName = prefix + SessionDataPath;
                // char is 2 bytes because strings are UTF-16
                int substituteByteLen = substituteName.Length * sizeof(char);
                ReparseDataBuffer rdb = new ReparseDataBuffer
                {
                    ReparseTag = IO_REPARSE_TAG_MOUNT_POINT,
                    // 12 bytes is the byte length from SubstituteNameOffset to
                    // before PathBuffer
                    ReparseDataLength = (ushort)(substituteByteLen + 12),
                    SubstituteNameOffset = 0,
                    SubstituteNameLength = (ushort)substituteByteLen,
                    // Needs to be at least 2 ahead (accounting for nonexistent null-terminator)
                    PrintNameOffset = (ushort)(substituteByteLen + 2),
                    PrintNameLength = 0,
                    PathBuffer = substituteName
                };

                var result = DeviceIoControl(
                    dirHandle,
                    FSCTL_SET_REPARSE_POINT,
                    ref rdb,
                    // 20 bytes is the byte length for everything but the PathBuffer
                    (uint)(substituteName.Length * sizeof(char) + 20),
                    IntPtr.Zero,
                    0,
                    IntPtr.Zero,
                    IntPtr.Zero
                );

                CloseHandle(dirHandle);
#endif
                //Create Log Folder & Files for Normal Build: -----------------------------------------------------------------------------------------------
                if (!SessionValues.WebBuild) //Web Build log folder & file creation already handled in the WebBuildLogWriter.cs class
                {
                    System.IO.Directory.CreateDirectory(SessionDataPath + Path.DirectorySeparatorChar + "LogFile");

                    string logPath = "";
                    if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX | SystemInfo.operatingSystemFamily == OperatingSystemFamily.Linux)
                    {
                        string pathName = Application.isEditor ? "/Library/Logs/Unity/Editor.log" : "/Library/Logs/Unity/Player.log";
                        logPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + pathName;
                    }
                    else if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows)
                    {
                        string pathName = Application.isEditor ? "\\Unity\\Editor\\Editor.log" : ("Low\\" + Application.companyName + "\\" + Application.productName + "\\Player.log");
                        logPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + pathName;
                    }

                    string logFileName = Application.isEditor ? "Editor.log" : "Player.log";
                    File.Copy(logPath, SessionDataPath + Path.DirectorySeparatorChar + "LogFile" + Path.DirectorySeparatorChar + logFileName);
                }


            }
        }



        private IEnumerator CreateFolderOnServer(string folderPath, Action callback)
        {
            yield return ServerManager.CreateFolder(folderPath);
            callback?.Invoke();
        }

        private IEnumerator CopySessionConfigFolderToDataFolder()
        {
            string sourcePath = ServerManager.SessionConfigFolderPath; //UN COMMENT THIS LATER!
            //string sourcePath = "CONFIGS/SessionConfig_021_VS_FL_VS_v01_Set05_STIM"; // Just used to test!
            string destinationPath = $"{ServerManager.SessionDataFolderPath}/SessionSettings";
            yield return ServerManager.CopyFolder(sourcePath, destinationPath);
        }
        public void OnGUI()
        {
            if (CameraMirrorTexture == null) return;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), CameraMirrorTexture);
        }
    }

    public class SessionDef
    {
        public string Subject;
        public DateTime SessionStart_DateTime;
        public int SessionStart_Frame;
        public float SessionStart_UnityTime;
        public string SessionID;
        public bool SerialPortActive, SyncBoxActive, EventCodesActive, RewardPulsesActive, SonicationActive;
        public string EyetrackerType, SelectionType;
    }
}
