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
using USE_ExperimentTemplate_Data;
using USE_ExperimentTemplate_Task;
using SelectionTracking;
using TMPro;
using Tobii.Research.Unity.CodeExamples;
using USE_Def_Namespace;
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

        [HideInInspector] public bool TasksFinished;
        
        public InitScreen SessionInitScreen;

        protected SummaryData SummaryData;
        public SessionData SessionData;

        public string TaskSelectionSceneName;

        public List<ControlLevel_Task_Template> ActiveTaskLevels;
        public ControlLevel_Task_Template CurrentTask;
        public ControlLevel_Task_Template GazeCalibrationTaskLevel;

        protected int taskCount;

        //For Loading config information
        public SessionDetails SessionDetails;
        public LocateFile LocateFile;

        public SelectionTracker.SelectionHandler SelectionHandler;
        private GameObject InputTrackers;
        public FrameData FrameData;
        private Camera SessionCam;
        private Camera MirrorCam;
        private GameObject mirrorCamGO;
        [HideInInspector] public RenderTexture CameraMirrorTexture;

        private GameObject experimenterDisplay;
        private RawImage mainCameraCopy_Image;

        private bool TaskSceneLoaded, SceneLoading;
        private List<string> selectedConfigsList = new List<string>();
        public StringBuilder PreviousTaskSummaryString = new StringBuilder();

        [HideInInspector] public DisplayController DisplayController;

        [HideInInspector] public GameObject TaskButtonsContainer;

        //Already in scene, so find them:
        [HideInInspector] public GameObject Starfield;
        [HideInInspector] public GameObject HumanVersionToggleButton;
        [HideInInspector] public GameObject ToggleAudioButton;

        //Load prefabs from resources:
        [HideInInspector] public GameObject BlockResults_GridElementPrefab;
        [HideInInspector] public GameObject BlockResultsPrefab;
        [HideInInspector] public GameObject HumanStartPanelPrefab;
        [HideInInspector] public GameObject StartButtonPrefabGO;
        [HideInInspector] public AudioClip BackgroundMusic_AudioClip;
        [HideInInspector] public AudioClip BlockResults_AudioClip;


        [HideInInspector] public float audioPlaybackSpot;
        [HideInInspector] public AudioSource BackgroundMusic_AudioSource;

        [HideInInspector] public GameObject InitCamGO;
        [HideInInspector] public LogWriter LogWriter;

        [HideInInspector] public State selectTask, loadTask;

        private ImportSettings_Level importSettings_Level;
        private InitScreen_Level initScreen_Level;

        private FlashPanelController FlashPanelController;



        public override void DefineControlLevel()
        {
            #if (UNITY_WEBGL)
                SessionValues.WebBuild = true;
            #endif

            SessionValues.SessionLevel = this;


            State initScreen = new State("InitScreen");
            State setupSession = new State("SetupSession");
            selectTask = new State("SelectTask");
            loadTask = new State("LoadTask");
            State setupTask = new State("VerifyTask");
            State runTask = new State("RunTask");
            State finishSession = new State("FinishSession");
            State gazeCalibration = new State("GazeCalibration");
            AddActiveStates(new List<State> { initScreen, setupSession, selectTask, loadTask, setupTask, runTask, finishSession, gazeCalibration });

            ActiveTaskLevels = new List<ControlLevel_Task_Template>();

            SessionCam = Camera.main;

            FindGameObjects();
            LoadPrefabs();

            SetDisplayController();

            SessionValues.LocateFile = gameObject.AddComponent<LocateFile>();

            SessionValues.FlashPanelController = GameObject.Find("UI_Canvas").GetComponent<FlashPanelController>();
            if(SessionValues.WebBuild)
                SessionValues.FlashPanelController.gameObject.SetActive(false);


            importSettings_Level = gameObject.GetComponent<ImportSettings_Level>();

            //InitScreen State---------------------------------------------------------------------------------------------------------------
            initScreen_Level = gameObject.GetComponent<InitScreen_Level>();
            initScreen.AddChildLevel(initScreen_Level);
            initScreen.SpecifyTermination(()=> initScreen.ChildLevel.Terminated, setupSession, () =>
            {
                if(SessionValues.WebBuild) //immedietely load taskselection screen and set initCam inactive
                    InitCamGO.SetActive(false); //Init canvas doesnt even use InitCam........ (we using this for something else??)
                else
                {
                    CreateExperimenterDisplay();
                    CreateMirrorCam();
                    Starfield.SetActive(false);
                }
            });

            string selectedConfigFolderName = null;
            bool taskAutomaticallySelected = false;
            

            bool waitForSerialPort = false;

            //SetupSession State---------------------------------------------------------------------------------------------------------------
            SetupSession_Level setupSessionLevel = GameObject.Find("ControlLevels").GetComponent<SetupSession_Level>();
            setupSession.AddChildLevel(setupSessionLevel);
            setupSessionLevel.SessionLevel = this;            

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

                if (SessionValues.SessionDef != null && SessionValues.SessionDef.EyeTrackerActive && GazeCalibrationTaskLevel == null)
                {
                    //Have to add calibration task level as child of calibration state here, because it isn't available prior
                    GazeCalibrationTaskLevel = GameObject.Find("GazeCalibration_Scripts").GetComponent<GazeCalibration_TaskLevel>();
                    GazeCalibrationTaskLevel.TaskName = "GazeCalibration";
                    GazeCalibrationTaskLevel.ConfigFolderName = "GazeCalibration";
                    // StartCoroutine(PopulateTaskLevel(GazeCalibrationTaskLevel, false, result =>
                    // {
                    //     GazeCalibrationTaskLevel = result;
                    //     gazeCalibration.AddChildLevel(GazeCalibrationTaskLevel);
                    //     GazeCalibrationTaskLevel.TrialLevel.TaskLevel = GazeCalibrationTaskLevel;
                    //     GazeCalibrationTaskLevel.gameObject.SetActive(false);
                    // }));
                }
            });

            setupSession.SpecifyTermination(() => setupSessionLevel.Terminated && !waitForSerialPort && SessionValues.SessionDef.EyeTrackerActive, gazeCalibration);
            setupSession.SpecifyTermination(() => setupSessionLevel.Terminated && !waitForSerialPort && !SessionValues.SessionDef.EyeTrackerActive, selectTask);
            setupSession.AddDefaultTerminationMethod(() =>
            {
                SessionSettings.Save();
                
                SetHumanPanelAndStartButton();
                SummaryData.Init();
                CreateSessionSettingsFolder();

                if (!SessionValues.SessionDef.FlashPanelsActive)
                    SessionValues.FlashPanelController.TurnOffFlashPanels();
                else
                    SessionValues.FlashPanelController.runPattern = true;
                
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
                   
                    SessionValues.SerialPortController.Initialize();
                }
                if (!SessionValues.WebBuild)
                {
                    InitCamGO.SetActive(false);
                    SessionValues.SessionInfoPanel = GameObject.Find("SessionInfoPanel").GetComponent<SessionInfoPanel>();
                }
                SessionValues.EventCodeManager.SendCodeImmediate("SetupSessionEnds");
            });

            //GazeCalibration State---------------------------------------------------------------------------------------------------------------
            gazeCalibration.AddInitializationMethod(() =>
            {
                FrameData.gameObject.SetActive(false);

                GazeCalibrationTaskLevel.TaskCam = Camera.main;

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
                GazeCalibrationTaskLevel.ConfigFolderName = "GazeCalibration";
                GameObject.Find("GazeCalibration(Clone)").transform.Find("GazeCalibration_Canvas").gameObject.SetActive(false);
                GameObject.Find("GazeCalibration(Clone)").transform.Find("GazeCalibration_Scripts").gameObject.SetActive(false);
                if (SessionValues.SessionDef.EyeTrackerActive && TobiiEyeTrackerController.Instance.isCalibrating)
                {
                    TobiiEyeTrackerController.Instance.isCalibrating = false;
                    TobiiEyeTrackerController.Instance.ScreenBasedCalibration.LeaveCalibrationMode();
                }

                SessionValues.GazeData.folderPath = SessionValues.TaskSelectionDataPath + Path.DirectorySeparatorChar + "GazeData";
                FrameData.gameObject.SetActive(true);

            });

            TaskButtonsContainer = null;
            Dictionary<string, GameObject> taskButtonGOs = new Dictionary<string, GameObject>();
            selectedConfigFolderName = null;

            //SelectTask State---------------------------------------------------------------------------------------------------------------
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

                if (!SessionValues.WebBuild)
                    HumanVersionToggleButton.SetActive(false);

                SessionValues.TaskSelectionCanvasGO.SetActive(true);

                Starfield.SetActive(SessionValues.SessionDef.IsHuman);

                if(!SessionValues.WebBuild)
                {
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
                }

                SessionValues.EventCodeManager.SendCodeImmediate("SelectTaskStarts");

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
                selectedConfigFolderName = null;
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

                if(SessionValues.SessionDef.TaskButtonGridSpots != null)
                {
                    for(int i = 0; i < SessionValues.SessionDef.NumGridSpots; i++)
                    {
                        GameObject gridItem = new GameObject("GridItem_" + (i+1));
                        gridItem.AddComponent<RawImage>();
                        gridItem.GetComponent<RawImage>().enabled = false;
                        gridItem.transform.SetParent(TaskButtonsContainer.transform);
                        gridItem.transform.localPosition = Vector3.zero;
                        gridItem.transform.localScale = Vector3.one;
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

                    if (SessionValues.SessionDef.TaskButtonGridSpots != null)
                    {
                        int gridNumber = SessionValues.SessionDef.TaskButtonGridSpots[count];

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
                        selectedConfigFolderName = chosenGO;
                        taskAutomaticallySelected = false;
                    }
                }
                AppendSerialData();
                StartCoroutine(FrameData.AppendDataToBuffer());
            });
            selectTask.SpecifyTermination(() => selectedConfigFolderName != null, loadTask, () => ResetSelectedTaskButtonSize());


            // Don't have automatic task selection if we encountered an error during setup
          /*  if (SessionValues.SessionDef.TaskSelectionTimeout >= 0 && !LogPanel.HasError())
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
/*            }
*/            
            selectTask.SpecifyTermination(() => TasksFinished, finishSession);

            //LoadTask State---------------------------------------------------------------------------------------------------------------
            loadTask.AddInitializationMethod(() =>
            {
                TaskButtonsContainer.SetActive(false);

                GameObject taskButton = taskButtonGOs[selectedConfigFolderName];
                RawImage image = taskButton.GetComponent<RawImage>();

                if (!SessionValues.WebBuild) //Let patients play same task as many times as they want
                {
                    image.color = new Color(.5f, .5f, .5f, .35f);
                    image.raycastTarget = false;
                    if (taskButton.TryGetComponent<HoverEffect>(out var hoverEffect))
                        Destroy(hoverEffect);
                }
                
                string taskName = (string)SessionValues.SessionDef.TaskMappings[selectedConfigFolderName];
                loadScene = SceneManager.LoadSceneAsync(taskName, LoadSceneMode.Additive);
                SceneLoading = true;
                loadScene.completed += (_) =>
                {
                    Type taskType = USE_Tasks_CustomTypes.CustomTaskDictionary[taskName].TaskLevelType;
                    
                    MethodInfo SetCurrentTaskMethod = GetType().GetMethod(nameof(this.SetCurrentTask)).MakeGenericMethod(new Type[] { taskType });
                    SetCurrentTaskMethod.Invoke(this, new object[] {taskName});
                    
                    SceneLoading = false;
                };
            });

            bool DefiningTask = false;
            loadTask.AddUpdateMethod(() =>
            {
                Debug.Log(("scene loading: " + SceneLoading));
                Debug.Log("Current Task: " + CurrentTask);
                if (CurrentTask != null)
                    Debug.Log("TaskLevel Defined: " + CurrentTask.TaskLevelDefined);
                if (!SceneLoading && CurrentTask != null && !DefiningTask)
                {
                    DefiningTask = true;
                    CurrentTask.DefineTaskLevel();
                }

            });
            
            loadTask.AddFixedUpdateMethod(() =>
            {
            });

            loadTask.AddLateUpdateMethod(() =>
            {
                SessionValues.SelectionTracker.UpdateActiveSelections();
                AppendSerialData();
                StartCoroutine(FrameData.AppendDataToBuffer());
            });

            loadTask.SpecifyTermination(() => CurrentTask!= null && CurrentTask.TaskLevelDefined, setupTask, () =>

            {
                DefiningTask = false;
                Starfield.SetActive(false);
                runTask.AddChildLevel(CurrentTask);
                SessionCam.gameObject.SetActive(false);
                CurrentTask.TaskCam = GameObject.Find(CurrentTask.TaskName + "_Camera").GetComponent<Camera>();
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

            SetupTask_Level setupTaskLevel = GameObject.Find("ControlLevels").GetComponent<SetupTask_Level>();
            setupTask.AddChildLevel(setupTaskLevel);
            setupTask.AddInitializationMethod(() =>
            {
                setupTaskLevel.TaskLevel = CurrentTask;
                SessionValues.EventCodeManager.SendCodeImmediate("SetupTaskStarts");

              
            });
            setupTask.SpecifyTermination(() => setupTaskLevel.Terminated, runTask);
            //RunTask State---------------------------------------------------------------------------------------------------------------
            runTask.AddUniversalInitializationMethod(() =>
            {

                SessionValues.EventCodeManager.SendCodeImmediate("RunTaskStarts");

                if(!SessionValues.WebBuild)
                {
                    if (SessionValues.DisplayController.SwitchDisplays)
                        CurrentTask.TaskCam.targetDisplay = 1;
                    else
                    {
                        CameraMirrorTexture = new RenderTexture(Screen.width, Screen.height, 24);
                        CameraMirrorTexture.Create();
                        CurrentTask.TaskCam.targetTexture = CameraMirrorTexture;
                        mainCameraCopy_Image.texture = CameraMirrorTexture;
                    }
                }
            });
            
            runTask.AddFixedUpdateMethod(() =>
            {
                SessionValues.EventCodeManager.EventCodeFixedUpdate();
            });
            
            runTask.AddLateUpdateMethod(() =>
            {
                SessionValues.SelectionTracker.UpdateActiveSelections();
                AppendSerialData();
            });

            runTask.SpecifyTermination(() => CurrentTask.Terminated, selectTask, () =>
            {
                if (PreviousTaskSummaryString != null && CurrentTask.CurrentTaskSummaryString != null)
                    PreviousTaskSummaryString.Insert(0, CurrentTask.CurrentTaskSummaryString);

                StartCoroutine(SummaryData.AddTaskRunData(CurrentTask.ConfigFolderName, CurrentTask, CurrentTask.GetTaskSummaryData()));

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
                    SessionValues.GazeData.CreateNewTaskIndexedFolder((taskCount + 1) * 2 - 1, SessionValues.TaskSelectionDataPath, "GazeData", "SessionLevel");
                    SessionValues.GazeData.fileName = SessionValues.FilePrefix + "__GazeData" + SessionValues.GazeData.GetNiceIntegers(4, (taskCount + 1) * 2 - 1) + "SessionLevel.txt";
                }

                FrameData.CreateNewTaskIndexedFolder((taskCount + 1) * 2 - 1, SessionValues.TaskSelectionDataPath, "FrameData", "SessionLevel");
                FrameData.fileName = SessionValues.FilePrefix + "__FrameData" + FrameData.GetNiceIntegers(4, (taskCount + 1) * 2 - 1) + "SessionLevel.txt";

                FrameData.gameObject.SetActive(true);
            });

            //FinishSession State---------------------------------------------------------------------------------------------------------------
            finishSession.AddInitializationMethod(() =>
            {
                SessionValues.EventCodeManager.SendCodeImmediate("FinishSessionStarts");
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
        }

        private void FindGameObjects()
        {
            try
            {
                InitCamGO = GameObject.Find("InitCamera");
                SessionValues.TaskSelectionCanvasGO = GameObject.Find("TaskSelectionCanvas");
                HumanVersionToggleButton = GameObject.Find("HumanVersionToggleButton");
                ToggleAudioButton = GameObject.Find("AudioButton");
                Starfield = GameObject.Find("Starfield");
                LogWriter = GameObject.Find("MiscScripts").GetComponent<LogWriter>();
                SessionValues.SessionDataControllers = new SessionDataControllers(GameObject.Find("DataControllers"));
                SessionValues.EventCodeManager = GameObject.Find("MiscScripts").GetComponent<EventCodeManager>();

                HumanVersionToggleButton.SetActive(false);
                ToggleAudioButton.SetActive(false);
                SessionValues.TaskSelectionCanvasGO.SetActive(false); //have to find HumanVersionToggleButton and ToggleAudioButton before setting TaskSelectionCanvas inactive.
                Starfield.SetActive(false);
            }
            catch (Exception e)
            {
                Debug.LogError("FAILED FINDING GAMEOBJECTS! Error Message: " + e.Message);
            }
        }

        private void LoadPrefabs()
        {
            try
            {
                BlockResults_GridElementPrefab = Resources.Load<GameObject>("BlockResults_GridElement");
                BlockResultsPrefab = Resources.Load<GameObject>("BlockResults");
                HumanStartPanelPrefab = Resources.Load<GameObject>("HumanStartPanel");
                StartButtonPrefabGO = Resources.Load<GameObject>("StartButton");
                BackgroundMusic_AudioClip = Resources.Load<AudioClip>("BackgroundMusic");
                BlockResults_AudioClip = Resources.Load<AudioClip>("BlockResults");
            }
            catch (Exception e)
            {
                Debug.LogError("FAILED TO LOAD PREFAB OR AUDIO CLIP FROM RESOURCES! Error Message: " + e.Message);
            }
        }

        private void SetHumanPanelAndStartButton()
        {
            SessionValues.HumanStartPanel = gameObject.AddComponent<HumanStartPanel>();
            SessionValues.HumanStartPanel.SetSessionLevel(this);
            SessionValues.HumanStartPanel.HumanStartPanelPrefab = HumanStartPanelPrefab;

            SessionValues.USE_StartButton = gameObject.AddComponent<USE_StartButton>();
            SessionValues.USE_StartButton.StartButtonPrefab = StartButtonPrefabGO;
        }

        private void SetDisplayController()
        {
            DisplayController = gameObject.AddComponent<DisplayController>();
            SessionInitScreen = GameObject.Find("InitScreen_GO").GetComponent<InitScreen>();
            DisplayController.HandleDisplays(SessionInitScreen);
            SessionValues.DisplayController = DisplayController;
        }

        private void CreateExperimenterDisplay()
        {
            experimenterDisplay = Instantiate(Resources.Load<GameObject>("Default_ExperimenterDisplay"));
            experimenterDisplay.name = "ExperimenterDisplay";
            SessionValues.ExperimenterDisplayController = experimenterDisplay.AddComponent<ExperimenterDisplayController>();
            experimenterDisplay.AddComponent<PreserveObject>();
            SessionValues.ExperimenterDisplayController.InitializeExperimenterDisplay(this, experimenterDisplay);
        }

        private void CreateMirrorCam()
        {
            mirrorCamGO = new GameObject("MirrorCamera");
            MirrorCam = mirrorCamGO.AddComponent<Camera>();
            MirrorCam.CopyFrom(Camera.main);
            MirrorCam.cullingMask = 0;
            mainCameraCopy_Image = GameObject.Find("MainCameraCopy").GetComponent<RawImage>();
        }

        private void CreateSessionSettingsFolder() //Create Session Settings Folder inside Data Folder and copy config folder into it
        {
            if(SessionValues.UsingServerConfigs)
            {
                if(!Application.isEditor)
                {
                    StartCoroutine(CreateFolderOnServer(SessionValues.SessionDataPath + Path.DirectorySeparatorChar + "SessionSettings", () =>
                    {
                        StartCoroutine(CopySessionConfigFolderToDataFolder()); //Copy Session Config folder to Data folder so that the settings are stored
                    }));
                }
            }
            else if(SessionValues.UsingLocalConfigs)
            {
                string sourceFolderPath = SessionValues.ConfigFolderPath;
                string destinationFolderPath = SessionValues.SessionDataPath + Path.DirectorySeparatorChar + "SessionSettings";
                CopyLocalFolder(sourceFolderPath, destinationFolderPath);
            }
        }

        public void CopyLocalFolder(string sourceFolderPath, string destinationFolderPath)
        {
            if (!Directory.Exists(sourceFolderPath))
            {
                Debug.Log("Source folder does not exist to copy from!");
                return;
            }

            if (!Directory.Exists(destinationFolderPath))
                Directory.CreateDirectory(destinationFolderPath);
            
            DirectoryInfo sourceDir = new DirectoryInfo(sourceFolderPath);
            DirectoryInfo destinationDir = new DirectoryInfo(destinationFolderPath);

            foreach (FileInfo file in sourceDir.GetFiles())
            {
                string destinationFilePath = Path.Combine(destinationDir.FullName, file.Name);
                file.CopyTo(destinationFilePath, true);
            }

            foreach (DirectoryInfo subDir in sourceDir.GetDirectories())
            {
                string subDestinationFolderPath = Path.Combine(destinationDir.FullName, subDir.Name);
                CopyLocalFolder(subDir.FullName, subDestinationFolderPath);
            }

            Debug.Log("Local Folder Folder copied successfully!");
        }
        //
        // private IEnumerator CreateSessionDataFolder(Action<bool> callbackBool)
        // {
        //     yield return StartCoroutine(SessionData.CreateFile());
        //     ServerManager.SessionDataFolderCreated = true;
        //     LogWriter.StoreDataIsSet = true; //tell log writer when storeData boolean has been set (waiting until data folder is created)
        //     callbackBool?.Invoke(true);
        // }
        //
        // private void SetupInputManagement(State inputActive, State inputInactive)
        // {
        //     SessionValues.InputManager = new GameObject("InputManager");
        //     SessionValues.InputManager.SetActive(true);
        //
        //     InputTrackers = Instantiate(Resources.Load<GameObject>("InputTrackers"), SessionValues.InputManager.transform);
        //     SessionValues.MouseTracker = InputTrackers.GetComponent<MouseTracker>();
        //     SessionValues.GazeTracker = InputTrackers.GetComponent<GazeTracker>();
        //
        //     SessionValues.SelectionTracker = new SelectionTracker();
        //     if (SessionValues.SessionDef.SelectionType.ToLower().Equals("gaze"))
        //     {
        //         SelectionHandler = SessionValues.SelectionTracker.SetupSelectionHandler("session", "GazeSelection", SessionValues.GazeTracker, inputActive, inputInactive);
        //         SelectionHandler.MinDuration = 0.7f;
        //     }
        //     else
        //     {
        //         SelectionHandler = SessionValues.SelectionTracker.SetupSelectionHandler("session", "MouseButton0Click", SessionValues.MouseTracker, inputActive, inputInactive);
        //         SessionValues.MouseTracker.enabled = true;
        //         SelectionHandler.MinDuration = 0.01f;
        //         SelectionHandler.MaxDuration = 2f;
        //     }
        //
        //     if (SessionValues.SessionDef.EyeTrackerActive)
        //     {
        //         if (GameObject.Find("TobiiEyeTrackerController") == null)
        //         {
        //             // gets called once when finding and creating the tobii eye tracker prefabs
        //             GameObject TobiiEyeTrackerControllerGO = new GameObject("TobiiEyeTrackerController");
        //             SessionValues.TobiiEyeTrackerController = TobiiEyeTrackerControllerGO.AddComponent<TobiiEyeTrackerController>();
        //             GameObject TrackBoxGO = Instantiate(Resources.Load<GameObject>("TrackBoxGuide"), TobiiEyeTrackerControllerGO.transform);
        //             GameObject EyeTrackerGO = Instantiate(Resources.Load<GameObject>("EyeTracker"), TobiiEyeTrackerControllerGO.transform);
        //             GameObject CalibrationGO = Instantiate(Resources.Load<GameObject>("GazeCalibration"));
        //             SessionValues.GazeTracker.enabled = true;
        //
        //
        //             GameObject GazeTrail = Instantiate(Resources.Load<GameObject>("GazeTrail"), TobiiEyeTrackerControllerGO.transform); 
        //             GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //             cube.transform.SetParent(TobiiEyeTrackerControllerGO.transform, true);
        //             // Position and scale the cube as desired
        //             cube.transform.position = new Vector3(0f, 1f, 60f);
        //             cube.transform.localScale = new Vector3(106f, 62f, 0.1f);
        //             cube.SetActive(false);
        //
        //         }
        //     }
        //     if (SessionValues.SessionDef.MonitorDetails != null && SessionValues.SessionDef.ScreenDetails != null)
        //     {
        //         USE_CoordinateConverter.ScreenDetails = new ScreenDetails(SessionValues.SessionDef.ScreenDetails.LowerLeft_Cm, SessionValues.SessionDef.ScreenDetails.UpperRight_Cm, SessionValues.SessionDef.ScreenDetails.PixelResolution);
        //         USE_CoordinateConverter.MonitorDetails = new MonitorDetails(SessionValues.SessionDef.MonitorDetails.PixelResolution, SessionValues.SessionDef.MonitorDetails.CmSize);
        //         USE_CoordinateConverter.SetMonitorDetails(USE_CoordinateConverter.MonitorDetails);
        //         USE_CoordinateConverter.SetScreenDetails(USE_CoordinateConverter.ScreenDetails);
        //     }}
        //
        // private void SetupSessionDataControllers()
        // {
        //     SessionData = (SessionData)SessionValues.SessionDataControllers.InstantiateDataController<SessionData>
        //         ("SessionData", SessionValues.SessionDef.StoreData, SessionValues.SessionDataPath); //SessionDataControllers.InstantiateSessionData(StoreData, SessionValues.SessionDataPath);
        //     SessionData.fileName = SessionValues.FilePrefix + "__SessionData.txt";
        //     SessionData.sessionLevel = this;
        //     SessionData.InitDataController();
        //     SessionData.ManuallyDefine();
        //
        //     if (SessionValues.SessionDef.SerialPortActive)
        //     {
        //         SessionValues.SerialSentData = (SerialSentData)SessionValues.SessionDataControllers.InstantiateDataController<SerialSentData>
        //             ("SerialSentData", SessionValues.SessionDef.StoreData, SessionValues.SessionDataPath + Path.DirectorySeparatorChar + "SerialSentData"
        //                                           + Path.DirectorySeparatorChar + "0001_TaskSelection");
        //         SessionValues.SerialSentData.fileName = SessionValues.FilePrefix + "__SerialSentData_0001_TaskSelection.txt";
        //         SessionValues.SerialSentData.sessionLevel = this;
        //         SessionValues.SerialSentData.InitDataController();
        //         SessionValues.SerialSentData.ManuallyDefine();
        //
        //         SessionValues.SerialRecvData = (SerialRecvData)SessionValues.SessionDataControllers.InstantiateDataController<SerialRecvData>
        //             ("SerialRecvData", SessionValues.SessionDef.StoreData, SessionValues.SessionDataPath + Path.DirectorySeparatorChar + "SerialRecvData"
        //                                                                    + Path.DirectorySeparatorChar + "0001_TaskSelection");
        //         SessionValues.SerialRecvData.fileName = SessionValues.FilePrefix + "__SerialRecvData_0001_TaskSelection.txt";
        //         SessionValues.SerialRecvData.sessionLevel = this;
        //         SessionValues.SerialRecvData.InitDataController();
        //         SessionValues.SerialRecvData.ManuallyDefine();
        //     }
        //
        //     FrameData = (FrameData)SessionValues.SessionDataControllers.InstantiateDataController<FrameData>("FrameData", "TaskSelection", SessionValues.SessionDef.StoreData, SessionValues.TaskSelectionDataPath + Path.DirectorySeparatorChar + "FrameData");
        //     FrameData.fileName = "TaskSelection__FrameData.txt";
        //     FrameData.sessionLevel = this;
        //     FrameData.InitDataController();
        //     FrameData.ManuallyDefine();
        //
        //     if (SessionValues.SessionDef.EventCodesActive)
        //         FrameData.AddEventCodeColumns();
        //     if (SessionValues.SessionDef.FlashPanelsActive)
        //         FrameData.AddFlashPanelColumns();
        //
        //     if (SessionValues.SessionDef.EyeTrackerActive)
        //     {
        //         SessionValues.GazeData = (GazeData)SessionValues.SessionDataControllers.InstantiateDataController<GazeData>("GazeData", "TaskSelection", SessionValues.SessionDef.StoreData, SessionValues.TaskSelectionDataPath + Path.DirectorySeparatorChar + "GazeData");
        //
        //         SessionValues.GazeData.fileName = "TaskSelection__GazeData.txt";
        //         SessionValues.GazeData.sessionLevel = this;
        //         SessionValues.GazeData.InitDataController();
        //         SessionValues.GazeData.ManuallyDefine();
        //         SessionValues.TobiiEyeTrackerController.GazeData = SessionValues.GazeData;
        //         SessionValues.GazeTracker.Init(FrameData, 0);
        //
        //     }
        //     SessionValues.MouseTracker.Init(FrameData, 0);
        // }
        //
        // private void WriteSessionConfigsToPersistantDataPath()
        // {
        //     if (Directory.Exists(SessionValues.ConfigFolderPath ))
        //         Directory.Delete(SessionValues.ConfigFolderPath , true);
        //
        //     if (!Directory.Exists(SessionValues.ConfigFolderPath ))
        //     {
        //         Directory.CreateDirectory(SessionValues.ConfigFolderPath );
        //         List<string> configsToWrite = new List<string>() { "SessionConfig_singleType", "EventCodeConfig_json", "DisplayConfig_json" };
        //         foreach (string config in configsToWrite)
        //         {
        //             byte[] textFileBytes = Resources.Load<TextAsset>("DefaultSessionConfigs/" + config).bytes;
        //             string configName = config;
        //             if (configName.ToLower().Contains("sessionconfig"))
        //                 configName += ".txt";
        //             else if (configName.ToLower().Contains("eventcode") || configName.ToLower().Contains("displayconfig"))
        //                 configName += ".json";
        //             File.WriteAllBytes(SessionValues.ConfigFolderPath  + Path.DirectorySeparatorChar + configName, textFileBytes);
        //
        //             Debug.Log("WROTE " + configName + " TO PERSISTANT PATH!");
        //         }
        //     }
        // }

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
            HumanVersionToggleButton.GetComponentInChildren<TextMeshProUGUI>().text = SessionValues.SessionDef.IsHuman ? "Human Version" : "Primate Version";
            Starfield.SetActive(!Starfield.activeInHierarchy);
        }

        private void AppendSerialData()
        {
            if (SessionValues.SessionDef.SerialPortActive)
            {
                if (SessionValues.SerialPortController.BufferCount("sent") > 0)
                {
                    try
                    {
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
                        SessionValues.SerialRecvData.AppendDataToBuffer();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }
        }

        public string GetConfigFolderPath(string configName)
        {
            string path;

            if (SessionValues.UsingDefaultConfigs)
                path = Application.persistentDataPath + Path.DirectorySeparatorChar + "M_USE_DefaultConfigs";
            else if (SessionValues.UsingServerConfigs)
                path = $"{ServerManager.SessionConfigFolderPath}/{configName}";
            else
            {
                if (!SessionSettings.SettingExists("Session", "ConfigFolderNames"))
                    return SessionValues.ConfigFolderPath + Path.DirectorySeparatorChar + configName;
                else
                {
                    List<string> configFolders = (List<string>)SessionSettings.Get("Session", "ConfigFolderNames");
                    int index = 0;
                    foreach (string k in SessionValues.SessionDef.TaskMappings.Keys)
                    {
                        if (k.Equals(configName)) break;
                        ++index;
                    }
                    path = SessionValues.ConfigFolderPath + Path.DirectorySeparatorChar + configFolders[index];
                }
            }
            return path;
        }

        // void OnTaskSceneLoaded(string configFolderName, bool verifyOnly)
        // {
        //     //We need to wait until the scene is loaded (async) to get
        //     //the task type in order to call PrepareTaskLevel
        //     string taskName = (string)SessionValues.SessionDef.TaskMappings[configFolderName];
        //     var methodInfo = GetType().GetMethod(nameof(this.GetTaskLevelType));
        //     
        //     Type taskType = USE_Tasks_CustomTypes.CustomTaskDictionary[taskName].TaskLevelType;
        //     MethodInfo GetTaskLevelType = methodInfo.MakeGenericMethod(new Type[] { taskType });
        //     GetTaskLevelType.Invoke(this, new object[] { configFolderName, verifyOnly });
        //     
        //     
        // }
        //
        // public void GetTaskLevelType<T>(string configFolderName, bool verifyOnly) where T : ControlLevel_Task_Template
        // {
        //     //Gets the task level type using reflection which cannot be done outside an invoked method
        //     string taskName = (string)SessionValues.SessionDef.TaskMappings[configFolderName];
        //     ControlLevel_Task_Template tl = GameObject.Find(taskName + "_Scripts").GetComponent<T>();
        //     // return tl as ControlLevel_Task_Template; 
        //     //it would be nice to return type T and 
        //     tl.ConfigFolderName = configFolderName;
        //
        //     // tl.importSettings_Level = importSettings_Level;
        //     // tl.verifyTask_Level = verifyTask_Level;
        //     
        //     StartCoroutine(PopulateTaskLevel(tl, verifyOnly, result =>
        //     {
        //         if (result != null)
        //         {
        //             tl = result;
        //             if (verifyOnly)
        //             {
        //                 SessionSettings.Restore();
        //                 SceneManager.UnloadSceneAsync(taskName);
        //             }
        //             else
        //                 CurrentTask = tl;
        //
        //             SceneLoading = false;
        //         }
        //         else
        //         {
        //             Debug.Log("Tasklevel result is null");
        //         }
        //     }));
        // }
        //
        // IEnumerator PopulateTaskLevel(ControlLevel_Task_Template tl, bool verifyOnly, Action<ControlLevel_Task_Template> callback)
        // {
        //     if (tl.TaskCam == null)
        //         tl.TaskCam = GameObject.Find(tl.TaskName + "_Camera").GetComponent<Camera>();
        //
        //     tl.TaskCam.gameObject.SetActive(false);
        //  tl.BlockResults_AudioClip = BlockResults_AudioClip;
        //
        //     tl.BlockResultsPrefab = BlockResultsPrefab;
        //     tl.BlockResults_GridElementPrefab = BlockResults_GridElementPrefab;
        //
        //
        //     if (SessionValues.UsingDefaultConfigs)
        //     {
        //         tl.TaskConfigPath = GetConfigFolderPath(tl.ConfigFolderName) + Path.DirectorySeparatorChar + tl.TaskName + "_DefaultConfigs";
        //
        //         //Write Task Config Folder and its files to Persistant data path for Webbuild using default configs---------------------
        //         if (!Directory.Exists(tl.TaskConfigPath))
        //         {
        //             Directory.CreateDirectory(tl.TaskConfigPath);
        //
        //             Dictionary<string, string> configDict = new Dictionary<string, string>
        //             {
        //                 {"_TaskDef_singleType", "_TaskDef_singleType.txt"},
        //                 {"_BlockDef_array", "_BlockDef_array.txt"},
        //                 {"_TrialDef_array", "_TrialDef_array.txt"},
        //                 {"_StimDef_array", "_StimDef_array.txt"},
        //                 {"_ConfigUiDetails_json", "_ConfigUiDetails_json.json"},
        //                 {"_EventCodeConfig_json", "_EventCodeConfig_json.json"},
        //                 {"MazeDef_array", "MazeDef_array.txt"}
        //             };
        //             TextAsset configTextAsset;
        //             foreach (var entry in configDict)
        //             {
        //                 configTextAsset = Resources.Load<TextAsset>("DefaultSessionConfigs/" + tl.TaskName + "_DefaultConfigs/" + tl.TaskName + entry.Key);
        //                 if (configTextAsset == null)//try it without task name (cuz MazeDef.txt doesnt have MazeGame in front of it)
        //                     configTextAsset = Resources.Load<TextAsset>("DefaultSessionConfigs/" + tl.TaskName + "_DefaultConfigs/" + entry.Key);
        //                 if (configTextAsset != null)
        //                     File.WriteAllBytes(tl.TaskConfigPath + Path.DirectorySeparatorChar + tl.TaskName + entry.Value, configTextAsset.bytes);
        //             }
        //         }
        //     }
        //     else
        //         tl.TaskConfigPath = GetConfigFolderPath(tl.ConfigFolderName);
        //     
        //     yield return StartCoroutine(tl.DefineTaskLevel(verifyOnly));
        //     if (verifyOnly)
        //     {
        //         callback?.Invoke(tl);
        //         yield break;
        //     }
        //
        //     ActiveTaskLevels.Add(tl);
        //     if (tl.TaskCanvasses != null)
        //         foreach (Canvas canvas in tl.TaskCanvasses)
        //             canvas.gameObject.SetActive(true);
        //     callback?.Invoke(tl);
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
        
        
        public void SetCurrentTask<T>(string taskName) where T : ControlLevel_Task_Template
        {
            CurrentTask = GameObject.Find(taskName + "_Scripts").GetComponent<T>();
        }
    }

    
    
}
