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
        [HideInInspector] public bool TasksFinished;
        
        protected SummaryData SummaryData;
        public SessionData SessionData;

        public string TaskSelectionSceneName;

        public List<ControlLevel_Task_Template> ActiveTaskLevels;
        public ControlLevel_Task_Template CurrentTask;
        public ControlLevel_Task_Template GazeCalibrationTaskLevel;

        public int taskCount;

        //For Loading config information
        public SessionDetails SessionDetails;
        public LocateFile LocateFile;

        public SelectionTracker.SelectionHandler SelectionHandler;
        private GameObject InputTrackers;
        public FrameData FrameData;
        [HideInInspector] public RenderTexture CameraRenderTexture;

        private GameObject ExperimenterDisplay;
        private RawImage ExpDisplayRenderImage;

        private Camera SessionCam;

        private Camera MirrorCam;
        private GameObject MirrorCamGO;

        private bool TaskSceneLoaded, SceneLoading;
        private List<string> selectedConfigsList = new List<string>();
        public StringBuilder PreviousTaskSummaryString = new StringBuilder();

        [HideInInspector] public GameObject TaskButtonsContainer;

        //Already in scene, so find them:
        [HideInInspector] public GameObject Starfield;
        [HideInInspector] public GameObject HumanVersionToggleButton;
        [HideInInspector] public GameObject ToggleAudioButton;

        //Load prefabs from resources:
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
        public bool runSessionLevelCalibration;

        public bool waitForSerialPort;


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
            State setupTask = new State("SetupTask");
            State runTask = new State("RunTask");
            State finishSession = new State("FinishSession");
            State gazeCalibration = new State("GazeCalibration");
            AddActiveStates(new List<State> { initScreen, setupSession, selectTask, loadTask, setupTask, runTask, finishSession, gazeCalibration });

            ActiveTaskLevels = new List<ControlLevel_Task_Template>();

            SessionCam = Camera.main;

            FindGameObjects();
            LoadPrefabs();

            SessionValues.LocateFile = gameObject.AddComponent<LocateFile>();

            SessionValues.FlashPanelController = GameObject.Find("UI_Canvas").GetComponent<FlashPanelController>();
            SessionValues.FlashPanelController.FindPanels();
            if(SessionValues.WebBuild)
                SessionValues.FlashPanelController.gameObject.SetActive(false);


            importSettings_Level = gameObject.GetComponent<ImportSettings_Level>();

            //InitScreen State---------------------------------------------------------------------------------------------------------------
            initScreen_Level = gameObject.GetComponent<InitScreen_Level>();
            initScreen.AddChildLevel(initScreen_Level);
            initScreen.SpecifyTermination(()=> initScreen.ChildLevel.Terminated, setupSession, () =>
            {
                if (SessionValues.WebBuild)
                    InitCamGO.SetActive(false);
                else
                {
                    CreateExperimenterDisplay();
                    CreateMirrorCam();
                    Starfield.SetActive(false);
                }
            });

            string selectedConfigFolderName = null;
            bool taskAutomaticallySelected = false;
            

            //bool waitForSerialPort = false;

            //SetupSession State---------------------------------------------------------------------------------------------------------------
            SetupSession_Level setupSessionLevel = GameObject.Find("ControlLevels").GetComponent<SetupSession_Level>();
            setupSession.AddChildLevel(setupSessionLevel);
            setupSessionLevel.SessionLevel = this;            

            SceneLoading = false;
            AsyncOperation loadScene = null;
            setupSession.AddUpdateMethod(() =>
            {
                if (SessionValues.SessionDef == null)
                    return;

                if(SessionValues.SessionDef.SerialPortActive && !waitForSerialPort && (SessionValues.SerialPortController == null))
                {
                    SessionValues.SerialPortController = GameObject.Find("MiscScripts").GetComponent<SerialPortThreaded>();
                    
                    if (SessionValues.SessionDef.SyncBoxActive)
                    {
                        SessionValues.SyncBoxController = new SyncBoxController();
                        SessionValues.SyncBoxController.serialPortController = SessionValues.SerialPortController;
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

                    if (waitForSerialPort && Time.time - StartTimeAbsolute > SessionValues.SerialPortController.initTimeout / 1000f + 0.5f)
                    {
                        if (SessionValues.SessionDef.SyncBoxActive && SessionValues.SessionDef.SyncBoxInitCommands != null)
                            SessionValues.SyncBoxController.SendCommand((List<string>)SessionValues.SessionDef.SyncBoxInitCommands);
                    }
                }
            });

            setupSession.SpecifyTermination(() => setupSessionLevel.Terminated && !waitForSerialPort && runSessionLevelCalibration, gazeCalibration);
            setupSession.SpecifyTermination(() => setupSessionLevel.Terminated && !waitForSerialPort && !runSessionLevelCalibration, selectTask);
            setupSession.AddDefaultTerminationMethod(() =>
            {
                SessionSettings.Save();
                
                SetHumanPanelAndStartButton();
                SummaryData.Init();

                if(SessionValues.StoreData)
                    CreateSessionSettingsFolder();

                if (SessionValues.SessionDef.SerialPortActive)
                {
                    SessionValues.SerialSentData.sc = SessionValues.SerialPortController;
                    SessionValues.SerialRecvData.sc = SessionValues.SerialPortController;
                }
               

                if (!SessionValues.SessionDef.FlashPanelsActive)
                    SessionValues.FlashPanelController.TurnOffFlashPanels();
                else
                    SessionValues.FlashPanelController.runPattern = true;
                
                if (!SessionValues.WebBuild)
                {
                    InitCamGO.SetActive(false);
                    SessionValues.SessionInfoPanel = GameObject.Find("SessionInfoPanel").GetComponent<SessionInfoPanel>();
                }
                SessionValues.EventCodeManager.SendCodeImmediate("SetupSessionEnds");

                if(SessionValues.SessionDef != null && SessionValues.SessionDef.EyeTrackerActive && GazeCalibrationTaskLevel == null)
                {
                    //Have to add calibration task level as child of calibration state here, because it isn't available prior
                    GazeCalibrationTaskLevel = GameObject.Find("GazeCalibration_Scripts").GetComponent<GazeCalibration_TaskLevel>();
                    gazeCalibration.AddChildLevel(GazeCalibrationTaskLevel);
                    GazeCalibrationTaskLevel.TaskName = "GazeCalibration";
                    GazeCalibrationTaskLevel.ConfigFolderName = "GazeCalibration";
                    runSessionLevelCalibration = true;
                }

                
            });

            //GazeCalibration State---------------------------------------------------------------------------------------------------------------
            gazeCalibration.AddSpecificInitializationMethod(() =>
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
                // GazeCalibrationTaskLevel.TaskName = "GazeCalibration";
                // GazeCalibrationTaskLevel.ConfigFolderName = "GazeCalibration";
                runSessionLevelCalibration = false;
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

                HumanVersionToggleButton.SetActive(SessionValues.SessionDef.IsHuman);

                SessionValues.TaskSelectionCanvasGO.SetActive(true);
                SessionValues.TaskSelectionCameraGO.SetActive(true);

                Starfield.SetActive(SessionValues.SessionDef.IsHuman);

                if(!SessionValues.WebBuild)
                {
                    CameraRenderTexture = new RenderTexture(Screen.width, Screen.height, 24);
                    CameraRenderTexture.Create();
                    SessionValues.TaskSelectionCameraGO.GetComponent<Camera>().targetTexture = CameraRenderTexture;
                    SessionValues.MirrorCanvasGO.GetComponent<RawImage>().texture = CameraRenderTexture;
                    ExpDisplayRenderImage.texture = CameraRenderTexture;
                    SessionValues.MirrorCanvasGO.SetActive(true);
                    SessionValues.MirrorCanvasGO.GetComponent<RawImage>().enabled = true;
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

                SessionValues.LoadingController.DeactivateLoadingCanvas(); //Turn off loading circle now that about to set taskselection canvas active!

                SessionCam.gameObject.SetActive(true);


                // Don't show the task buttons if we encountered an error during setup
                if (LogPanel.HasError())
                    return;

                SceneLoading = true;
                if(!SessionValues.WebBuild)
                {
                    if (taskCount >= SessionValues.SessionDef.TaskMappings.Count)
                    {
                        TasksFinished = true;
                        return;
                    }
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
                gridLayout.constraintCount = SessionValues.WebBuild ? 5 : SessionValues.SessionDef.TaskButtonGridMaxPerRow; //using 5 for WebBuild since there are 9 tasks
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

                    if (!SessionValues.UsingServerConfigs)
                    {
                        if (!Directory.Exists(taskFolderPath))
                        {
                            Destroy(taskButtonGO);
                            throw new DirectoryNotFoundException($"Task folder for '{configName}' at '{taskFolderPath}' does not exist.");
                        }
                    }

                    if (SessionValues.UsingServerConfigs)
                    {
                        StartCoroutine(ServerManager.LoadTextureFromServer($"{SessionValues.SessionDef.TaskIconsFolderPath}/{taskName}.png", imageResult =>
                        {
                            if (imageResult != null)
                                image.texture = imageResult;
                            else
                                Debug.Log("NULL GETTING TASK ICON TEXTURE FROM SERVER!");
                        }));
                    }
                    else if(SessionValues.UsingDefaultConfigs)
                    {
                        image.texture = Resources.Load<Texture2D>($"{SessionValues.SessionDef.TaskIconsFolderPath}/{taskName}");
                    }
                    else if(SessionValues.UsingLocalConfigs)
                        image.texture = LoadExternalPNG(SessionValues.SessionDef.TaskIconsFolderPath + Path.DirectorySeparatorChar + taskName + ".png");


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
                    if(!SessionValues.SessionDef.PlayBackgroundMusic)
                        ToggleAudioButton.transform.Find("Cross").gameObject.SetActive(true);
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
                
                DataLateUpdate();
            });
            selectTask.SpecifyTermination(() => TasksFinished, finishSession);
            selectTask.SpecifyTermination(() => selectedConfigFolderName != null, loadTask, () => ResetSelectedTaskButtonSize());


            selectTask.AddTimer(() => SessionValues.SessionDef != null ? SessionValues.SessionDef.TaskSelectionTimeout : 0f, loadTask, () =>
            {
                foreach (DictionaryEntry task in SessionValues.SessionDef.TaskMappings)
                {
                    //Find the next task in the list that is still interactable
                    string configName = (string)task.Key;

                    // If the next task button in the task mappings is not interactable, skip until the next available config is found
                    if (!taskButtonGOs[configName].GetComponent<RawImage>().raycastTarget)
                        continue;

                    taskAutomaticallySelected = true;
                    selectedConfigFolderName = configName;
                    break;
                }
            });
                  //LoadTask State---------------------------------------------------------------------------------------------------------------
            loadTask.AddSpecificInitializationMethod(() =>
            {
                SessionValues.LoadingController.ActivateLoadingCanvas();

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
                if (!SceneLoading && CurrentTask != null && !DefiningTask)
                {
                    DefiningTask = true;
                    CurrentTask.ConfigFolderName = selectedConfigFolderName;
                    CurrentTask.DefineTaskLevel();
                }

            });

            loadTask.AddLateUpdateMethod(() =>
            {
                SessionValues.SelectionTracker.UpdateActiveSelections();
                DataLateUpdate();
            });

            loadTask.SpecifyTermination(() => CurrentTask!= null && CurrentTask.TaskLevelDefined, setupTask, () =>
            {
                DefiningTask = false;
                Starfield.SetActive(false);
                runTask.AddChildLevel(CurrentTask);
                //SessionCam.gameObject.SetActive(false);
                CurrentTask.TaskCam = GameObject.Find(CurrentTask.TaskName + "_Camera").GetComponent<Camera>();
                CurrentTask.TaskCam.targetDisplay = SessionValues.TaskSelectionCameraGO.GetComponent<Camera>().targetDisplay;

                SetTaskMainBackground(); 

                if (CameraRenderTexture != null)
                    CameraRenderTexture.Release();

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
                    SessionValues.SerialRecvData.folderPath = SessionValues.SessionDataPath + Path.DirectorySeparatorChar + "Task" +SessionValues.GetNiceIntegers(4, taskCount + 1) + "_" + CurrentTask.ConfigFolderName + Path.DirectorySeparatorChar + "SerialRecvData";
                    SessionValues.SerialSentData.folderPath = SessionValues.SessionDataPath + Path.DirectorySeparatorChar + "Task" +SessionValues.GetNiceIntegers(4, taskCount + 1) + "_" + CurrentTask.ConfigFolderName + Path.DirectorySeparatorChar + "SerialSentData";

                }

                StartCoroutine(FrameData.AppendDataToFile());
            });

            //automatically finish tasks after running one - placeholder for proper selection
            //runTask.AddLateUpdateMethod

            SetupTask_Level setupTaskLevel = GameObject.Find("ControlLevels").GetComponent<SetupTask_Level>();
            setupTask.AddChildLevel(setupTaskLevel);
            setupTask.AddSpecificInitializationMethod(() =>
            {
                setupTaskLevel.TaskLevel = CurrentTask;
                SessionValues.EventCodeManager.SendRangeCode("SetupTaskStarts", taskCount);

                CurrentTask.TaskConfigPath = SessionValues.ConfigFolderPath + "/" + CurrentTask.ConfigFolderName;
            });
            setupTask.SpecifyTermination(() => setupTaskLevel.Terminated, runTask);
            //RunTask State---------------------------------------------------------------------------------------------------------------
            runTask.AddUniversalInitializationMethod(() =>
            {
                SessionValues.TaskSelectionCameraGO.SetActive(false);
                SessionValues.TaskSelectionCanvasGO.SetActive(false);
                SessionCam.gameObject.SetActive(false);

                SessionValues.EventCodeManager.SendCodeImmediate("RunTaskStarts");

                if(!SessionValues.WebBuild)
                {
                    CameraRenderTexture = new RenderTexture(Screen.width, Screen.height, 24);
                    CameraRenderTexture.Create();
                    MirrorCamGO.GetComponent<Camera>().CopyFrom(CurrentTask.TaskCam);
                    CurrentTask.TaskCam.targetTexture = CameraRenderTexture;
                    SessionValues.MirrorCanvasGO.GetComponent<RawImage>().texture = CameraRenderTexture;
                    ExpDisplayRenderImage.texture = CameraRenderTexture;
                }
            });
            
            runTask.AddUpdateMethod(() => { SessionValues.EventCodeManager.SendBufferedEventCodes(); });
            
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

                if (SessionValues.SessionDef.SerialPortActive)
                {
                    StartCoroutine(SessionValues.SerialRecvData.AppendDataToFile());
                    StartCoroutine(SessionValues.SerialSentData.AppendDataToFile());
                }

                if (CurrentTask.TaskName != "GazeCalibration")
                    SceneManager.UnloadSceneAsync(CurrentTask.TaskName);
                SceneManager.SetActiveScene(SceneManager.GetSceneByName(TaskSelectionSceneName));

                ActiveTaskLevels.Remove(CurrentTask);

                if (CameraRenderTexture != null)
                    CameraRenderTexture.Release();

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

                    SessionValues.SerialRecvData.CreateNewTaskIndexedFolder(taskCount + 1, SessionValues.SessionDataPath, "TaskSelectionData", "Task");
                    SessionValues.SerialRecvData.fileName = SessionValues.FilePrefix + "__SerialRecvData_" + SessionValues.GetNiceIntegers(4, taskCount + 1) + "_TaskSelection.txt";
                    SessionValues.SerialSentData.CreateNewTaskIndexedFolder(taskCount + 1, SessionValues.SessionDataPath, "TaskSelectionData", "Task");
                    SessionValues.SerialSentData.fileName = SessionValues.FilePrefix + "__SerialSentData_" + SessionValues.GetNiceIntegers(4, taskCount + 1) + "_TaskSelection.txt";
                }

                if (SessionValues.SessionDef.EyeTrackerActive)
                {
                    SessionValues.GazeData.CreateNewTaskIndexedFolder(taskCount + 1, SessionValues.SessionDataPath, "TaskSelectionData", "Task");
                    SessionValues.GazeData.fileName = SessionValues.FilePrefix + "__GazeData_" + SessionValues.GetNiceIntegers(4, taskCount + 1) + "_TaskSelection.txt";
                }

                FrameData.CreateNewTaskIndexedFolder(taskCount + 1, SessionValues.SessionDataPath, "TaskSelectionData", "Task");
                FrameData.fileName = SessionValues.FilePrefix + "__FrameData_" + SessionValues.GetNiceIntegers(4, taskCount + 1) + "_TaskSelection.txt";

                FrameData.gameObject.SetActive(true);

                CurrentTask = null;
                selectedConfigFolderName = null;
            });

            //FinishSession State---------------------------------------------------------------------------------------------------------------
            finishSession.AddSpecificInitializationMethod(() =>
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


        private void OnApplicationQuit()
        {
            if (CurrentTask == null)
                Debug.Log("CURRENT TASK IS NULL BEFORE TRYING TO WRITE TASK SUMMARY DATA!");
            else
                StartCoroutine(SummaryData.AddTaskRunData(CurrentTask.ConfigFolderName, CurrentTask, CurrentTask.GetTaskSummaryData()));
        }

        //Method is used to have every task set their main background as the MUSE blue background
        private void SetTaskMainBackground()
        {
            if (CurrentTask.TaskCam != null)
            {
                if (!CurrentTask.TaskCam.gameObject.TryGetComponent<Skybox>(out var skyboxComponent))
                    skyboxComponent = CurrentTask.TaskCam.gameObject.AddComponent<Skybox>();
                skyboxComponent.material = Resources.Load<Material>("MUSE_MainBackground");
            }
            else
                Debug.LogWarning("TASK CAM IS NULL!");
        }

        private void FindGameObjects()
        {
            try
            {
                SessionValues.LoadingController = GameObject.Find("LoadingCanvas").GetComponent<LoadingController>();
                InitCamGO = GameObject.Find("InitCamera");
                SessionValues.MirrorCanvasGO = GameObject.Find("MirrorCanvas");
                SessionValues.MirrorCanvasGO.SetActive(false);
                SessionValues.TaskSelectionCameraGO = GameObject.Find("TaskSelectionCamera");
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

        private void CreateExperimenterDisplay()
        {
            ExperimenterDisplay = Instantiate(Resources.Load<GameObject>("Default_ExperimenterDisplay"));
            ExperimenterDisplay.name = "ExperimenterDisplay";
            SessionValues.ExperimenterDisplayController = ExperimenterDisplay.AddComponent<ExperimenterDisplayController>();
            ExperimenterDisplay.AddComponent<PreserveObject>();
            SessionValues.ExperimenterDisplayController.InitializeExperimenterDisplay(this, ExperimenterDisplay);
        }

        private void CreateMirrorCam()
        {
            MirrorCamGO = new GameObject("MirrorCamera");
            MirrorCam = MirrorCamGO.AddComponent<Camera>();
            Skybox skybox = MirrorCamGO.AddComponent<Skybox>();
            skybox.material = Resources.Load<Material>("MUSE_MainBackground");
            MirrorCam.CopyFrom(SessionValues.TaskSelectionCameraGO.GetComponent<Camera>());
            MirrorCam.cullingMask = 0;
            ExpDisplayRenderImage = GameObject.Find("MainCameraCopy").GetComponent<RawImage>();
        }

        private void CreateSessionSettingsFolder() //Create Session Settings Folder inside Data Folder and copy config folder into it
        {
            if (SessionValues.UsingServerConfigs)
            {
                if (!Application.isEditor)
                {
                    StartCoroutine(CreateFolderOnServer(SessionValues.SessionDataPath + Path.DirectorySeparatorChar + "SessionConfigs", () =>
                    {
                        StartCoroutine(CopySessionConfigFolderToDataFolder());
                    }));
                }
            }
            else if (SessionValues.UsingLocalConfigs)
            {
                string sourceFolderPath = SessionValues.ConfigFolderPath;
                string destinationFolderPath = SessionValues.SessionDataPath + Path.DirectorySeparatorChar + "SessionConfigs";
                CopyLocalFolder(sourceFolderPath, destinationFolderPath);
            }
            else if (SessionValues.UsingDefaultConfigs)
                Debug.Log("Using Default Configs, so not copying the session config folder to the data folder.");
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
        }
        

        private void ResetSelectedTaskButtonSize()
        {
            if (SelectionHandler.SuccessfulSelections.Count > 0)
            {
                if (SelectionHandler.LastSuccessfulSelection.SelectedGameObject.TryGetComponent(out HoverEffect hoverComponent))
                    hoverComponent.SetToInitialSize();
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
                        StartCoroutine(SessionValues.SerialSentData.AppendDataToBuffer());
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
                        StartCoroutine(SessionValues.SerialRecvData.AppendDataToBuffer());
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
            //if (CameraMirrorTexture == null) return;
            //GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), CameraMirrorTexture);
        }
        
        
        public void SetCurrentTask<T>(string taskName) where T : ControlLevel_Task_Template
        {
            CurrentTask = GameObject.Find(taskName + "_Scripts").GetComponent<T>();
        }

        public void DataLateUpdate()
        {
            AppendSerialData();
            StartCoroutine(FrameData.AppendDataToBuffer());
            SessionValues.EventCodeManager.EventCodeLateUpdate();
        }
    }
}
