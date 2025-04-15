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
using System.Runtime.InteropServices;
using System.Collections.Specialized;
#if (!UNITY_WEBGL)
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
#endif



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

        public int taskCount;

        //For Loading config information
        public SessionDetails SessionDetails;

        public SelectionTracker.SelectionHandler SelectionHandler;
        public FrameData FrameData;
        [HideInInspector] public RenderTexture CameraRenderTexture;

        //Experimenter Display variables:
        private GameObject ExperimenterDisplay_Parent;
        private GameObject ExperimenterDisplayGO;
        private Canvas ExperimenterDisplayCanvas;
        private GameObject SessionBuilderGO;
        private SessionBuilder SessionBuilder;
        private GameObject TaskOrder_GridParent;

        public RawImage ExpDisplayRenderImage;


        public Camera SessionCam;

        private Camera MirrorCam;
        private GameObject MirrorCamGO;

        private bool SceneLoading;
        public StringBuilder PreviousTaskSummaryString = new StringBuilder();

        [HideInInspector] public GameObject TaskButtonsContainer;

        //Already in scene, so find them:
        public GameObject HumanVersionToggleButton;
        public GameObject ToggleAudioButton;
        public GameObject RedAudioCross;
        public GameObject SavePanel;

        //Load prefabs from resources:
        [HideInInspector] public GameObject HumanStartPanelPrefab;
        [HideInInspector] public GameObject StartButtonPrefabGO;
        [HideInInspector] public AudioClip BlockResults_AudioClip;

        [HideInInspector] public State selectTask, loadTask;

        private ImportSettings_Level importSettings_Level;
        private InitScreen_Level initScreen_Level;

        public GameObject MainDirectionalLight;


        public bool waitForSerialPort;


        public GameObject SessionSummaryGO;
        public SessionSummaryController SessionSummaryController;


        public override void DefineControlLevel()
        {
            #if (UNITY_WEBGL)
                Session.WebBuild = true;
            #endif

            Session.SessionLevel = this;


            State initScreen = new State("InitScreen");
            State setupSession = new State("SetupSession");
            State sessionBuilder = new State("SessionBuilder");
            selectTask = new State("SelectTask");
            loadTask = new State("LoadTask");
            State setupTask = new State("SetupTask");
            State runTask = new State("RunTask");
            State finishSession = new State("FinishSession");
            State saveData = new State("SaveData");
            State loadGazeCalibration = new State("LoadGazeCalibration");
            State setupGazeCalibration = new State("SetupGazeCalibration");
            State gazeCalibration = new State("GazeCalibration");
            AddActiveStates(new List<State> { initScreen, setupSession, sessionBuilder, selectTask, loadTask, setupTask, runTask, finishSession, saveData, loadGazeCalibration, setupGazeCalibration, gazeCalibration });

            initScreen_Level = gameObject.GetComponent<InitScreen_Level>();
            SetupSession_Level setupSessionLevel = GameObject.Find("ControlLevels").GetComponent<SetupSession_Level>();
            SetupTask_Level setupTaskLevel = GameObject.Find("ControlLevels").GetComponent<SetupTask_Level>();

            ActiveTaskLevels = new List<ControlLevel_Task_Template>();

            SessionCam = Camera.main;

            FindGameObjects();
            LoadPrefabs();

            Session.CameraSyncController = gameObject.AddComponent<CameraSyncController>();

            Session.LocateFile = gameObject.AddComponent<LocateFile>();

            Session.FlashPanelController = GameObject.Find("UI_Canvas").GetComponent<FlashPanelController>();
            Session.FlashPanelController.FindPanels();
            if (Session.WebBuild)
                Session.FlashPanelController.gameObject.SetActive(false);


            importSettings_Level = gameObject.GetComponent<ImportSettings_Level>();

            if (Session.SessionAudioController == null)
                Session.SessionAudioController = GameObject.Find("MiscScripts").AddComponent<SessionAudioController>();

            //InitScreen State---------------------------------------------------------------------------------------------------------------
            initScreen.AddChildLevel(initScreen_Level);
            initScreen.SpecifyTermination(() => initScreen.ChildLevel.Terminated, setupSession, () =>
            {
                CreateExperimenterDisplay();
                
                if(Session.WebBuild)
                    Session.InitCamGO.SetActive(false);
                else
                {
                    CreateMirrorCam();
                }

            });

            string selectedConfigFolderName = null;

            //SetupSession State---------------------------------------------------------------------------------------------------------------
            setupSession.AddChildLevel(setupSessionLevel);

            SceneLoading = false;
            AsyncOperation loadScene = null;
            setupSession.AddUpdateMethod(() =>
            {
                if (Session.SessionDef == null)
                    return;

                if (Session.SessionDef.SerialPortActive && !waitForSerialPort && (Session.SerialPortController == null))
                {
                    Session.SerialPortController = GameObject.Find("MiscScripts").GetComponent<SerialPortThreaded>();

                    if (Session.SessionDef.SyncBoxActive)
                    {
                        Session.SyncBoxController = new SyncBoxController();
                        Session.SyncBoxController.serialPortController = Session.SerialPortController;
                    }

                    if (Session.SessionDef.EventCodesActive)
                    {
                        Session.EventCodeManager.SyncBoxController = Session.SyncBoxController;
                        Session.EventCodeManager.codesActive = true;
                    }
                    waitForSerialPort = true;

                    Session.SerialPortController.SerialPortAddress = Session.SessionDef.SerialPortAddress;
                    Session.SerialPortController.SerialPortSpeed = Session.SessionDef.SerialPortSpeed;

                    Session.SerialPortController.Initialize();

                    if (waitForSerialPort && Time.time - StartTimeAbsolute > Session.SerialPortController.initTimeout / 1000f + 0.5f)
                    {
                        if (Session.SessionDef.SyncBoxActive && Session.SessionDef.SyncBoxInitCommands != null)
                            Session.SyncBoxController.SendCommand((List<string>)Session.SessionDef.SyncBoxInitCommands);
                    }
                }

            });

            setupSession.SpecifyTermination(() => setupSessionLevel.Terminated && !waitForSerialPort, sessionBuilder);
            setupSession.AddDefaultTerminationMethod(() =>
            {
                SessionSettings.Save();

                SetHumanPanelAndStartButton();
                SummaryData.Init();

                if (Session.StoreData)
                    CreateSessionSettingsFolder();

                if (Session.SessionDef.SerialPortActive)
                {
                    Session.SerialSentData.sc = Session.SerialPortController;
                    Session.SerialRecvData.sc = Session.SerialPortController;
                }

                if (Session.SessionDef.SerialPortActive)
                {
                    StartCoroutine(Session.SerialSentData.CreateFile());
                    StartCoroutine(Session.SerialRecvData.CreateFile());

                }

                StartCoroutine(FrameData.CreateFile());

                if (Session.SessionDef.EyeTrackerActive && !Session.SessionDef.SpoofGazeWithMouse)
                    StartCoroutine(Session.GazeData.CreateFile());
                

                if (!Session.SessionDef.FlashPanelsActive)
                    Session.FlashPanelController.TurnOffFlashPanels();
                else
                    Session.FlashPanelController.runPattern = true;

                if (Session.SessionDef.EyeTrackerActive)
                    Session.GazeTracker.enabled = true;


                if (!Session.WebBuild)
                    Session.SessionInfoPanel = ExperimenterDisplayGO.transform.Find("SessionInfoPanel").GetComponent<SessionInfoPanel>();

                Session.EventCodeManager.SendCodeThisFrame("SetupSessionEnds");
            });
            
            //LoadGazeCalibration State---------------------------------------------------------------------------------------------------------------
            loadGazeCalibration.AddSpecificInitializationMethod(() =>
            {
                Type taskType = USE_Tasks_CustomTypes.CustomTaskDictionary["GazeCalibration"].TaskLevelType;

                MethodInfo SetCurrentTaskMethod = GetType().GetMethod(nameof(this.SetCurrentTask)).MakeGenericMethod(new Type[] { taskType });
                SetCurrentTaskMethod.Invoke(this, new object[] { "GazeCalibration" });

            });

            bool DefiningTask = false;
            loadGazeCalibration.AddUpdateMethod(() =>
            {
                //Session.EventCodeManager.CheckFrameEventCodeBuffer();

                if (!SceneLoading && CurrentTask != null && !DefiningTask)
                {
                    DefiningTask = true;
                    CurrentTask.DefineTaskLevel();
                }

            });

            loadGazeCalibration.SpecifyTermination(() => CurrentTask != null && CurrentTask.TaskLevelDefined, setupGazeCalibration, () =>
            {
                DefiningTask = false;
                CurrentTask.TaskCam = GameObject.Find(CurrentTask.TaskName + "_Camera").GetComponent<Camera>();
                CurrentTask.TrialLevel.TaskLevel = CurrentTask;

                StartCoroutine(FrameData.AppendDataToFile());
                AppendSerialData();
                Session.GazeCalibrationController.WriteSerialAndGazeDataThenReassignDataPath("SessionToGazeCalibration");
            });
            setupGazeCalibration.AddChildLevel(setupTaskLevel);
            setupGazeCalibration.AddSpecificInitializationMethod(() =>
            {
                CurrentTask = Session.GazeCalibrationController.GazeCalibrationTaskLevel;
                CurrentTask.TaskConfigPath = Session.ConfigFolderPath + "/" + CurrentTask.ConfigFolderName;

                setupTaskLevel.TaskLevel = CurrentTask;
                setupTaskLevel.ConfigFolderName = CurrentTask.ConfigFolderName;

            });
            setupGazeCalibration.SpecifyTermination(() => setupTaskLevel.Terminated, gazeCalibration);

            //GazeCalibration State---------------------------------------------------------------------------------------------------------------
            gazeCalibration.AddSpecificInitializationMethod(() =>
            {

                if (Session.WebBuild)
                {
                    Session.MainExperimenterCanvas_GO.SetActive(false);
                }

                Session.ParticipantCanvas_GO.SetActive(false);

                if (!Session.WebBuild)
                {
                    ExperimenterDisplayCanvas.gameObject.SetActive(true);
                    ExperimenterDisplayGO.SetActive(true);
                    ExperimenterDisplayCanvas.transform.Find("Background").gameObject.SetActive(false); //Turn off the spinning background on the Exp Display now that session builder is done:
                }
                else
                    ExperimenterDisplayCanvas.gameObject.SetActive(false);

                ExperimenterDisplayGO.transform.Find("TopRightSection_Background").transform.Find("MainCameraCopy").gameObject.GetComponent<GazeOverlay>().enabled = true;

                Session.ParticipantCanvas_GO.SetActive(false);
                //Session.LoadingTextGO_Display1.SetActive(false);
                Session.MainExperimenterCanvas_LoadingText_GO.SetActive(false);

                FrameData.gameObject.SetActive(false);
                SessionCam.gameObject.SetActive(false);
                Session.TaskSelectionCanvasGO.SetActive(false);

                // Activate gaze calibration components
                Session.GazeCalibrationController.ActivateGazeCalibrationComponents();
                Session.GazeCalibrationController.GazeCalibrationTaskLevel.ActivateTaskDataControllers();

                // Assign experimenter display render texture to the GazeCalibrationTaskLevel.TaskCam
                if (CameraRenderTexture != null)
                    CameraRenderTexture.Release();

                AssignExperimenterDisplayRenderTexture(Session.GazeCalibrationController.GazeCalibrationTaskLevel.TaskCam);

                // Set the current task and trial levels 
                Session.TaskLevel = Session.GazeCalibrationController.GazeCalibrationTaskLevel;
                Session.TrialLevel = Session.GazeCalibrationController.GazeCalibrationTrialLevel;
            });

            gazeCalibration.AddLateUpdateMethod(() =>
            {
                Session.EventCodeManager.CheckFrameEventCodeBuffer();

                Session.SelectionTracker.UpdateActiveSelections();
                AppendSerialData();
                if (Session.SessionDef.EyeTrackerActive && !Session.SessionDef.SpoofGazeWithMouse)
                    StartCoroutine(Session.GazeData.AppendDataToBuffer());

                //Session.EventCodeManager.EventCodeLateUpdate();
            });

            // Termination method for gaze calibration
            gazeCalibration.SpecifyTermination(() => Session.GazeCalibrationController.GazeCalibrationTaskLevel.Terminated, () => selectTask, () =>
            {
                Session.GazeCalibrationController.WriteSerialAndGazeDataThenReassignDataPath("GazeCalibrationToSession");

                //StartCoroutine(SessionData.AppendDataToBuffer());
                //StartCoroutine(SessionData.AppendDataToFile());
                

                // Check and exit calibration mode for Tobii eye tracker
                if(!Session.SessionDef.SpoofGazeWithMouse)
                {
                    if (Session.TobiiEyeTrackerController != null && Session.TobiiEyeTrackerController.isCalibrating)
                    {
                        Session.TobiiEyeTrackerController.isCalibrating = false;
                        Session.TobiiEyeTrackerController.ScreenBasedCalibration.LeaveCalibrationMode();
                    }
                }

                // Disable gaze calibration
                Session.GazeCalibrationController.RunCalibration = false;
                Session.GazeCalibrationController.DectivateGazeCalibrationComponents();

                // Activate TaskSelection scene elements
                FrameData.gameObject.SetActive(true);
                SessionCam.gameObject.SetActive(true);
                Session.TaskSelectionCanvasGO.SetActive(true);

                // Assign experimenter display render texture to the SessionCam
                if (CameraRenderTexture != null)
                    CameraRenderTexture.Release();

                AssignExperimenterDisplayRenderTexture(SessionCam);

                if (PreviousTaskSummaryString != null && Session.TaskLevel?.CurrentTaskSummaryString != null)
                    PreviousTaskSummaryString.Insert(0, Session.TaskLevel.CurrentTaskSummaryString);

                Session.TaskLevel = null;
                Session.TrialLevel = null;
                CurrentTask = null;

            });

            TaskButtonsContainer = null;
            Dictionary<string, GameObject> taskButtonGOs = new Dictionary<string, GameObject>();


            //SessionBuilder State---------------------------------------------------------------------------------------------------------------
            sessionBuilder.AddUniversalInitializationMethod(() =>
            {
                Session.MainExperimenterCanvas_LoadingText_GO.SetActive(false);

                //AssignExperimenterDisplayRenderTexture(SessionCam);

                ExperimenterDisplayGO.SetActive(false);

                //ACTIVATE SESSION BUILDER:
                SessionBuilder.ManualStart(TaskOrder_GridParent);
            });
            sessionBuilder.SpecifyTermination(() => SessionBuilder.RunButtonClicked && Session.SessionDef.EyeTrackerActive, loadGazeCalibration, () =>
            {
                LoadGazeGameObjects();
                gazeCalibration.AddChildLevel(Session.GazeCalibrationController.GazeCalibrationTaskLevel);
                Session.GazeCalibrationController.RunCalibration = true;
                SessionBuilderGO.SetActive(false);

            });
            sessionBuilder.SpecifyTermination(() => SessionBuilder.RunButtonClicked, selectTask);

            sessionBuilder.AddDefaultTerminationMethod(() =>
            {
                SessionBuilderGO.SetActive(false);
            });

            //SelectTask State---------------------------------------------------------------------------------------------------------------
            selectTask.AddUniversalInitializationMethod(() =>
            {
                //NEED TO RESET SHOTGUN VARIABLE IF USING GAZE BECAUSE GAZE TRIAL LEVEL CHANGES IT
                if (Session.SessionDef.SelectionType.ToLower().Contains("gaze"))
                {
                    Session.GazeTracker.UsingShotgunHandler = true;
                }


                Session.InitCamGO.SetActive(false);
                SessionCam.gameObject.SetActive(true);

                MainDirectionalLight.SetActive(true);

                // Activate TaskSelectionCanvas and assign it to the primary display
                Session.TaskSelectionCanvasGO.SetActive(true);
                Session.TaskSelectionCanvasGO.GetComponent<Canvas>().targetDisplay = 0;
                SessionCam.targetDisplay = 0;

                AssignExperimenterDisplayRenderTexture(SessionCam);

                if (Session.WebBuild)
                {
                    Session.MainExperimenterCanvas_GO.SetActive(false);
                }

                Session.ParticipantCanvas_GO.SetActive(false);

                if(!Session.WebBuild)
                {
                    ExperimenterDisplayCanvas.gameObject.SetActive(true);
                    ExperimenterDisplayGO.SetActive(true);
                    ExperimenterDisplayCanvas.transform.Find("Background").gameObject.SetActive(false); //Turn off the spinning background on the Exp Display now that session builder is done:
                }
                else
                    ExperimenterDisplayCanvas.gameObject.SetActive(false);
               


                if (Session.SessionDef.PlayBackgroundMusic)
                {
                    Session.SessionAudioController.PlayBackgroundMusic();
                    RedAudioCross.SetActive(false);
                }
                else
                {
                    Session.SessionAudioController.StopBackgroundMusic();
                    RedAudioCross.SetActive(true);
                }

                HumanVersionToggleButton.SetActive(Session.SessionDef.IsHuman);

                if (SelectionHandler.AllSelections.Count > 0)
                    SelectionHandler.ClearSelections();

                Session.EventCodeManager.SendCodeThisFrame("SelectTaskStarts");

                SessionSettings.Restore();
                selectedConfigFolderName = null;


                // Don't show the task buttons if we encountered an error during setup
                if (LogPanel.HasError())
                    return;

                SceneLoading = true;


                if (taskCount >= SessionBuilder.GetQueueLength())
                {
                    TasksFinished = true;
                    return;
                }


                if (TaskButtonsContainer != null)
                {
                    TaskButtonsContainer.SetActive(true);
                    if (Session.SessionDef.GuidedTaskSelection)
                    {
                        // if guided selection, we need to adjust the shading of the icons and buttons after the task buttons object is already created                        
                        string key = SessionBuilder.GetItemInQueue(taskCount).ConfigName;
                        foreach (KeyValuePair<string, GameObject> taskButton in taskButtonGOs)
                        {
                            if (taskButton.Key == key)
                            {
                                taskButton.Value.GetComponent<RawImage>().color = new Color(1f, 1f, 1f, 1f);
                                taskButton.Value.GetComponent<RawImage>().raycastTarget = true;
                                if (Session.SessionDef.IsHuman && Session.UsingDefaultConfigs)
                                    taskButton.Value.AddComponent<HoverEffect>();
                            }
                            else
                            {
                                taskButton.Value.GetComponent<RawImage>().color = new Color(.5f, .5f, .5f, .35f);
                                taskButton.Value.GetComponent<RawImage>().raycastTarget = false;
                                if (Session.SessionDef.IsHuman && Session.UsingDefaultConfigs)
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
                TaskButtonsContainer.SetActive(false);

                GridLayoutGroup gridLayout = TaskButtonsContainer.GetComponent<GridLayoutGroup>();
                int size = Session.SessionDef.TaskButtonSize;
                gridLayout.cellSize = new Vector2(size, size);
                gridLayout.constraintCount = Session.SessionDef.TaskButtonGridMaxPerRow;
                int spacing = Session.SessionDef.TaskButtonSpacing; //was 45 for web build
                gridLayout.spacing = new Vector2(spacing, spacing);

                List<GameObject> gridList = new List<GameObject>();

                if (Session.SessionDef.TaskButtonGridSpots != null)
                {
                    for (int i = 0; i < Session.SessionDef.NumGridSpots; i++)
                    {
                        GameObject gridItem = new GameObject("GridItem_" + (i + 1));
                        gridItem.AddComponent<RawImage>();
                        gridItem.GetComponent<RawImage>().enabled = false;
                        gridItem.transform.SetParent(TaskButtonsContainer.transform);
                        gridItem.transform.localPosition = Vector3.zero;
                        gridItem.transform.localScale = Vector3.one;
                        gridList.Add(gridItem);
                    }
                }

                List<QueueItem> tasksInQueue = SessionBuilder.GetQueueItems();

                int count = 0;

                foreach (QueueItem queueItem in tasksInQueue)
                {
                    string configName = queueItem.ConfigName;
                    string taskName = queueItem.Task.TaskName;

                    GameObject taskButtonGO;
                    RawImage image;

                    if (Session.SessionDef.TaskButtonGridSpots != null)
                    {
                        int gridNumber = Session.SessionDef.TaskButtonGridSpots[count];

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

                    if (!Session.UsingServerConfigs)
                    {
                        if (!Directory.Exists(taskFolderPath))
                        {
                            Destroy(taskButtonGO);
                            throw new DirectoryNotFoundException($"Task folder for '{configName}' at '{taskFolderPath}' does not exist.");
                        }
                    }

                    if (Session.UsingServerConfigs)
                    {
                        StartCoroutine(ServerManager.LoadTextureFromServer($"{ServerManager.ServerURL}/{Session.SessionDef.TaskIconsFolderPath}/{taskName}.png", imageResult =>
                        {
                            if (imageResult != null)
                                image.texture = imageResult;
                            else
                                Debug.LogError("NULL GETTING TASK ICON TEXTURE FROM SERVER!");
                        }));
                    }
                    else if(Session.UsingDefaultConfigs)
                    {
                        image.texture = Resources.Load<Texture2D>($"{Session.SessionDef.TaskIconsFolderPath}/{taskName}");
                    }
                    else if(Session.UsingLocalConfigs)
                        image.texture = LoadExternalPNG(Session.SessionDef.TaskIconsFolderPath + Path.DirectorySeparatorChar + taskName + ".png");


                    // If guided task selection, only make the next icon interactable
                    if (Session.SessionDef.GuidedTaskSelection)
                    {
                        string key = SessionBuilder.GetItemInQueue(taskCount).ConfigName;

                        if (configName == key)
                        {
                            image.color = new Color(1f, 1f, 1f, 1f);
                            image.raycastTarget = true;
                            if (Session.SessionDef.IsHuman && Session.UsingDefaultConfigs)
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
                        if (Session.SessionDef.IsHuman && Session.UsingDefaultConfigs)
                            taskButtonGO.AddComponent<HoverEffect>();
                    }

                    if(!taskButtonGOs.ContainsKey(configName))
                        taskButtonGOs.Add(configName, taskButtonGO);
                    count++;
                }

                TaskButtonsContainer.SetActive(true);

                if (Session.SessionDef.IsHuman)
                {
                    HumanVersionToggleButton.SetActive(true);
                    ToggleAudioButton.SetActive(true);
                    if (!Session.SessionDef.PlayBackgroundMusic)
                        RedAudioCross.SetActive(true);
                }

            });

            selectTask.AddLateUpdateMethod(() =>
            {
                Session.SelectionTracker.UpdateActiveSelections();


                if (SelectionHandler.SuccessfulSelections.Count > 0)
                {
                    string chosenGO = SelectionHandler.LastSuccessfulSelection.SelectedGameObject?.name;
                    if (chosenGO != null && taskButtonGOs.ContainsKey(chosenGO))
                    {
                        selectedConfigFolderName = chosenGO;
                    }
                }

                AppendSerialData();
                StartCoroutine(FrameData.AppendDataToBuffer());
                if(Session.SessionDef.EyeTrackerActive && !Session.SessionDef.SpoofGazeWithMouse)
                    StartCoroutine(Session.GazeData.AppendDataToBuffer());

            });
            selectTask.SpecifyTermination(() => TasksFinished, finishSession);
            selectTask.SpecifyTermination(() => selectedConfigFolderName != null, loadTask, () => ResetSelectedTaskButtonSize());
            selectTask.AddTimer(() => Session.SessionDef != null ? Session.SessionDef.TaskSelectionTimeout : 0f, loadTask, () =>
            {
                List<QueueItem> tasksInQueue = SessionBuilder.GetQueueItems();
                if(tasksInQueue != null && tasksInQueue.Count > 0)
                {
                    foreach(QueueItem task in tasksInQueue)
                    {
                        //Find the next task in the list that is still interactable
                        string configName = task.ConfigName;

                        // If the next task button in the task mappings is not interactable, skip until the next available config is found
                        if (!taskButtonGOs[configName].GetComponent<RawImage>().raycastTarget)
                            continue;

                        selectedConfigFolderName = configName;
                        break;
                    }
                }

            });
            selectTask.AddLateUpdateMethod(() => { Session.EventCodeManager.CheckFrameEventCodeBuffer(); });
            //LoadTask State---------------------------------------------------------------------------------------------------------------
            loadTask.AddSpecificInitializationMethod(() =>
            {
                MainDirectionalLight.SetActive(false);

                Session.ParticipantCanvas_GO.SetActive(true);
                Session.ParticipantCanvas_LoadingText_GO.SetActive(true);

                TaskButtonsContainer.SetActive(false);

                GameObject taskButton = taskButtonGOs[selectedConfigFolderName];
                RawImage image = taskButton.GetComponent<RawImage>();


                image.color = new Color(.5f, .5f, .5f, .35f);
                image.raycastTarget = false;
                if (taskButton.TryGetComponent<HoverEffect>(out var hoverEffect))
                    Destroy(hoverEffect);
                

                string taskName = (string)Session.SessionDef.TaskMappings[selectedConfigFolderName];

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
                Session.EventCodeManager.CheckFrameEventCodeBuffer();

                Session.SelectionTracker.UpdateActiveSelections();
                AppendSerialData();
                StartCoroutine(FrameData.AppendDataToBuffer());
                if(Session.SessionDef.EyeTrackerActive && !Session.SessionDef.SpoofGazeWithMouse)
                    StartCoroutine(Session.GazeData.AppendDataToBuffer());
            });

            loadTask.SpecifyTermination(() => CurrentTask!= null && CurrentTask.TaskLevelDefined, setupTask, () =>
            {
                Session.TaskSelectionCanvasGO.SetActive(false);
                DefiningTask = false;
                runTask.AddChildLevel(CurrentTask);
                SessionCam.gameObject.SetActive(false);
                CurrentTask.TaskCam = GameObject.Find(CurrentTask.TaskName + "_Camera").GetComponent<Camera>();

                if (CameraRenderTexture != null)
                    CameraRenderTexture.Release();

                SceneManager.SetActiveScene(SceneManager.GetSceneByName(CurrentTask.TaskName));
                CurrentTask.TrialLevel.TaskLevel = CurrentTask;

                if (Session.SessionDef.EyeTrackerActive)
                {
                    Session.GazeCalibrationController.OriginalTaskLevel = CurrentTask;
                    Session.GazeCalibrationController.OriginalTrialLevel = CurrentTask.TrialLevel;
                }

                StartCoroutine(WriteSerialAndGazeDataAndSwitchToTaskDataPaths());

            });
            setupTask.AddChildLevel(setupTaskLevel);
            setupTask.AddUpdateMethod(() => { Session.EventCodeManager.CheckFrameEventCodeBuffer(); });

            setupTask.AddLateUpdateMethod(() =>
            {
                Session.SelectionTracker.UpdateActiveSelections();
                AppendSerialData();
                
                if (Session.SessionDef.EyeTrackerActive && !Session.SessionDef.SpoofGazeWithMouse)
                    StartCoroutine(Session.GazeData.AppendDataToBuffer());

                if (CurrentTask.FrameData != null)
                    StartCoroutine(CurrentTask.FrameData.AppendDataToBuffer());
                else
                    StartCoroutine(FrameData.AppendDataToBuffer());
            });
            setupTask.AddSpecificInitializationMethod(() =>
            {
                setupTaskLevel.TaskLevel = CurrentTask;
                setupTaskLevel.ConfigFolderName = CurrentTask.ConfigFolderName;
                Session.EventCodeManager.SendRangeCodeThisFrame("SetupTaskStarts", taskCount);

                CurrentTask.TaskConfigPath = Session.ConfigFolderPath + "/" + CurrentTask.ConfigFolderName;
            });
            setupTask.SpecifyTermination(() => setupTaskLevel.Terminated, runTask, () =>
            {
                // Append to the file the remaining parts of the TaskSelection Frame Data
                StartCoroutine(FrameData.AppendDataToFile());
            });

            //RunTask State---------------------------------------------------------------------------------------------------------------
            runTask.AddUniversalInitializationMethod(() =>
            {
                Session.InitCamGO.SetActive(false);
                Session.SessionAudioController.StopBackgroundMusic();

                Session.EventCodeManager.SendCodeThisFrame("RunTaskStarts");
                Session.EventCodeManager.SendRangeCodeThisFrame("StimulationCondition", 0);


                if (!Session.WebBuild)
                    AssignExperimenterDisplayRenderTexture(CurrentTask.TaskCam);

            });
                       
            runTask.AddLateUpdateMethod(() =>
            {
                Session.EventCodeManager.CheckFrameEventCodeBuffer();

                Session.SelectionTracker.UpdateActiveSelections();
                AppendSerialData();
                if(Session.SessionDef.EyeTrackerActive && !Session.SessionDef.SpoofGazeWithMouse)
                    StartCoroutine(Session.GazeData.AppendDataToBuffer());
            });

            runTask.SpecifyTermination(() => CurrentTask.Terminated, selectTask, () =>
            {
                OrderedDictionary taskResultsData = CurrentTask.GetTaskResultsData();
                SessionBuilder.SetTaskData(CurrentTask.TaskName, CurrentTask.TrialLevel.TrialCount_InTask, CurrentTask.Duration, taskResultsData);
                SessionBuilder.SetExpDisplayIconAsInactive(taskCount);

                if (PreviousTaskSummaryString != null && CurrentTask.CurrentTaskSummaryString != null)
                    PreviousTaskSummaryString.Insert(0, CurrentTask.CurrentTaskSummaryString);
                
                StartCoroutine(SummaryData.AddTaskRunData(CurrentTask.ConfigFolderName, CurrentTask, CurrentTask.GetTaskSummaryData()));

                //StartCoroutine(SessionData.AppendDataToBuffer());
                //StartCoroutine(SessionData.AppendDataToFile());

                StartCoroutine(WriteSerialAndGazeDataAndSwitchToTaskSelectionDataPaths());

                SceneManager.UnloadSceneAsync(CurrentTask.TaskName);
                SceneManager.SetActiveScene(SceneManager.GetSceneByName(TaskSelectionSceneName));

                ActiveTaskLevels.Remove(CurrentTask);

                if (CameraRenderTexture != null)
                    CameraRenderTexture.Release();

                taskCount++;

                
                FrameData.fileName = Session.FilePrefix + "__FrameData_" + Session.GetNiceIntegers(taskCount + 1) + "_TaskSelection.txt";
                FrameData.CreateNewTaskIndexedFolder(taskCount + 1, Session.SessionDataPath, "TaskSelectionData", "Task");

                FrameData.gameObject.SetActive(true);
                Session.TaskSelectionDataPath = FrameData.folderPath;

                CurrentTask = null;
                selectedConfigFolderName = null;
            });

            //FinishSession State---------------------------------------------------------------------------------------------------------------
            bool skipSessionSummary = false;
            finishSession.AddSpecificInitializationMethod(() =>
            {
                Session.EventCodeManager.SendCodeThisFrame("FinishSessionStarts");

                ToggleAudioButton.SetActive(false);
                HumanVersionToggleButton.SetActive(false);

                skipSessionSummary = !Session.SessionDef.IsHuman;

                if (skipSessionSummary)
                    return;

                List<TaskObject> tasks = SessionBuilder.GetTasks();
                if (tasks != null)
                {
                    tasks = tasks.Where(task => task.TrialsCompleted > 0).ToList();
                    if (tasks.Count > 0)
                        CreateSessionSummaryPanel(tasks);
                    else
                    {
                        Debug.Log("No trials were completed for any tasks!");
                        skipSessionSummary = true; //didnt actually complete any trials, so also skip
                    }
                }
                else
                    Debug.LogWarning("TASKS ARE NULL!");
                
            });
            finishSession.AddLateUpdateMethod(() => { Session.EventCodeManager.CheckFrameEventCodeBuffer(); });
            finishSession.SpecifyTermination(() => skipSessionSummary, () => saveData);
            finishSession.SpecifyTermination(() => SessionSummaryController != null && SessionSummaryController.EndSessionButtonClicked, () => saveData);
            //finishSession.SpecifyTermination(() => SessionSummaryController != null && SessionSummaryController.EndSessionButtonClicked, () => null);
            finishSession.AddTimer(() => Session.SessionDef.SessionSummaryDuration, () => saveData);
            finishSession.AddDefaultTerminationMethod(() =>
            {
                SaveDataAtEndOfSession();
            });

            //SaveData State---------------------------------------------------------------------------------------------------------------
            saveData.AddSpecificInitializationMethod(() =>
            {
                SaveDataAtEndOfSession();

                SavePanel.SetActive(true);

                if(SessionSummaryGO != null)
                    SessionSummaryGO.SetActive(false);

                //start rotating background image
                ImageRotator rotator = Session.TaskSelectionCanvasGO.transform.Find("Background").gameObject.GetComponent<ImageRotator>();
                if (rotator != null)
                {
                    rotator.rotationSpeed = 10f;
                    rotator.enabled = true;
                }
                else
                    Debug.LogWarning("ROTATOR COMPONENT IS NULL ON THE BACKGROUND GAMEOBJECT");

                if (Session.SessionDef != null && Session.SessionDef.SerialPortActive && Session.SerialPortController != null)
                    Session.SerialPortController.ClosePort();

            });
            saveData.AddTimer(() => 2.5f, () => null);
            saveData.AddDefaultTerminationMethod(() =>
            {
                //Run the Quit method to the closing of: handle web build, normal build, or editor
                Session.ApplicationQuit.HandleClosingApplication();
            });
        }


        private IEnumerator WriteSerialAndGazeDataAndSwitchToTaskDataPaths()
        {
            if (Session.SessionDef.SerialPortActive)
            {
                AppendSerialData(); // Ensure data is prepared before writing

                // Wait for Serial Data to be written before changing paths
                yield return StartCoroutine(Session.SerialRecvData.AppendDataToFile());
                yield return StartCoroutine(Session.SerialSentData.AppendDataToFile());

                // Now update the paths after writing is done
                Session.SerialRecvData.folderPath = Session.SessionDataPath + Path.DirectorySeparatorChar +
                                                    "Task" + Session.GetNiceIntegers(taskCount + 1) + "_" +
                                                    CurrentTask.ConfigFolderName + Path.DirectorySeparatorChar +
                                                    "SerialRecvData";

                Session.SerialSentData.folderPath = Session.SessionDataPath + Path.DirectorySeparatorChar +
                                                    "Task" + Session.GetNiceIntegers(taskCount + 1) + "_" +
                                                    CurrentTask.ConfigFolderName + Path.DirectorySeparatorChar +
                                                    "SerialSentData";
            }

            // Wait for Frame Data to be written before updating path
            yield return StartCoroutine(FrameData.AppendDataToFile());

            if (Session.SessionDef.EyeTrackerActive)
            {
                // Wait for Gaze Data to be written before changing path
                yield return StartCoroutine(Session.GazeData.AppendDataToFile());

                // Now update the path
                Session.GazeData.folderPath = Session.SessionDataPath + Path.DirectorySeparatorChar +
                                              "Task" + Session.GetNiceIntegers(taskCount + 1) + "_" +
                                              CurrentTask.ConfigFolderName + Path.DirectorySeparatorChar +
                                              "GazeData";
            }
        }

        private IEnumerator WriteSerialAndGazeDataAndSwitchToTaskSelectionDataPaths()
        {
            if (Session.SessionDef.SerialPortActive)
            {
                yield return StartCoroutine(Session.SerialRecvData.AppendDataToFile());
                yield return StartCoroutine(Session.SerialSentData.AppendDataToFile());
            }
            if (Session.SessionDef.EyeTrackerActive)
            {
                yield return StartCoroutine(Session.GazeData.AppendDataToFile());
            }

            if (Session.SessionDef.SerialPortActive)
            {
                Session.SerialRecvData.fileName = Session.FilePrefix + "__SerialRecvData_" + Session.GetNiceIntegers(taskCount + 1) + "_TaskSelection.txt";
                Session.SerialRecvData.CreateNewTaskIndexedFolder(taskCount + 1, Session.SessionDataPath, "TaskSelectionData", "Task");
                Session.SerialSentData.fileName = Session.FilePrefix + "__SerialSentData_" + Session.GetNiceIntegers(taskCount + 1) + "_TaskSelection.txt";
                Session.SerialSentData.CreateNewTaskIndexedFolder(taskCount + 1, Session.SessionDataPath, "TaskSelectionData", "Task");

                Debug.Log("CREATING NEW SERIAL DATA FOLDER AND THIS IS PATH: " + Session.SerialSentData.folderPath);
            }

            if (Session.SessionDef.EyeTrackerActive)
            {
                Session.GazeData.fileName = Session.FilePrefix + "__GazeData_" + Session.GetNiceIntegers(taskCount + 1) + "_TaskSelection.txt";
                Session.GazeData.CreateNewTaskIndexedFolder(taskCount + 1, Session.SessionDataPath, "TaskSelectionData", "Task");
                // Session.GazeCalibrationController.ReassignGazeCalibrationDataFolderPath(Session.SessionDataPath + Path.DirectorySeparatorChar + "GazeCalibration" + Path.DirectorySeparatorChar + "TaskSelectionData");
            }

        }
        public void SaveDataAtEndOfSession()
        {
            if (Session.SessionDef == null)
                return;

            StartCoroutine(SessionData.AppendDataToBuffer());
            StartCoroutine(SessionData.AppendDataToFile());

            AppendSerialData();
            if (Session.SessionDef.SerialPortActive)
            {
                StartCoroutine(Session.SerialSentData.AppendDataToFile());
                StartCoroutine(Session.SerialRecvData.AppendDataToFile());
            }

            if (Session.SessionDef.EyeTrackerActive)
            {
                StartCoroutine(Session.GazeData.AppendDataToBuffer());
                StartCoroutine(Session.GazeData.AppendDataToFile());
            }

            StartCoroutine(FrameData.AppendDataToFile());
            if (CurrentTask == null)
                Debug.Log("Current Task is Null before trying to write summary data! (could be that no task was started yet)");
            else
                StartCoroutine(SummaryData.AddTaskRunData(CurrentTask.ConfigFolderName, CurrentTask, CurrentTask.GetTaskSummaryData()));
        }

        private void CreateSessionSummaryPanel(List<TaskObject> tasks)
        {
            SessionSummaryGO = Instantiate(Resources.Load<GameObject>("SessionSummary"));
            SessionSummaryGO.name = "SessionSummaryPanel";
            SessionSummaryController = SessionSummaryGO.GetComponent<SessionSummaryController>();
            SessionSummaryController.SessionBuilder = SessionBuilder;
            SessionSummaryGO.transform.SetParent(Session.TaskSelectionCanvasGO.transform);
            SessionSummaryGO.transform.localPosition = Vector3.zero;
            SessionSummaryGO.transform.localScale = Vector3.one;

            if (tasks != null)
                SessionSummaryController.CreateTaskSummaryGridItems(tasks);
            else
                Debug.LogError("TASKS ARE NULL");
        }


        private void FindGameObjects()
        {
            try
            {
                GameObject miscScripts = GameObject.Find("MiscScripts");
                Session.TimerController = miscScripts.GetComponent<TimerController>();
                Session.LogWriter = miscScripts.GetComponent<LogWriter>();
                Session.EventCodeManager = miscScripts.GetComponent<EventCodeManager>();
                Session.FullScreenController = miscScripts.GetComponent<FullScreenController>();
                Session.ApplicationQuit = miscScripts.GetComponent<ApplicationQuit>();
                Session.InitCamGO = GameObject.Find("InitCamera");
                Session.TaskSelectionCanvasGO = GameObject.Find("TaskSelectionCanvas");

                Session.ParticipantCanvas_GO = GameObject.Find("ParticipantCanvas");
                Session.ParticipantCanvas_LoadingText_GO = Session.ParticipantCanvas_GO.transform.Find("LoadingText").gameObject;

                Session.MainExperimenterCanvas_GO = GameObject.Find("ExperimenterMainCanvas");
                Session.MainExperimenterCanvas_LoadingText_GO = Session.MainExperimenterCanvas_GO.transform.Find("LoadingText").gameObject;

                Session.SessionDataControllers = new SessionDataControllers(GameObject.Find("DataControllers"));
                HumanVersionToggleButton = GameObject.Find("HumanVersionToggleButton");
                ToggleAudioButton = GameObject.Find("AudioButton");
                RedAudioCross = ToggleAudioButton.transform.Find("Cross").gameObject;
                SavePanel = GameObject.Find("SavePanel");
                SavePanel.SetActive(false);

                MainDirectionalLight = GameObject.Find("Directional Light");

                HumanVersionToggleButton.SetActive(false);
                ToggleAudioButton.SetActive(false);
                Session.TaskSelectionCanvasGO.SetActive(false); //have to find HumanVersionToggleButton and ToggleAudioButton before setting TaskSelectionCanvas inactive.
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
                BlockResults_AudioClip = Resources.Load<AudioClip>("BlockResults");
            }
            catch (Exception e)
            {
                Debug.LogError("FAILED TO LOAD PREFAB OR AUDIO CLIP FROM RESOURCES! Error Message: " + e.Message);
            }
        }

        private void SetHumanPanelAndStartButton()
        {
            Session.HumanStartPanel = gameObject.AddComponent<HumanStartPanel>();
            Session.HumanStartPanel.HumanStartPanelPrefab = HumanStartPanelPrefab;
            Session.USE_StartButton = gameObject.AddComponent<USE_StartButton>();
            Session.USE_StartButton.StartButtonPrefab = StartButtonPrefabGO;
        }

        private void CreateExperimenterDisplay()
        {
            ExperimenterDisplay_Parent = Instantiate(Resources.Load<GameObject>("Default_ExperimenterDisplay"));
            ExperimenterDisplay_Parent.name = "ExperimenterDisplay";
            Session.ExperimenterDisplayController = ExperimenterDisplay_Parent.AddComponent<ExperimenterDisplayController>();
            ExperimenterDisplay_Parent.AddComponent<PreserveObject>();
            Session.ExperimenterDisplayController.InitializeExperimenterDisplay(ExperimenterDisplay_Parent);

            SessionBuilderGO = ExperimenterDisplay_Parent.transform.Find("ExperimenterCanvas").transform.Find("SessionBuilder").gameObject;
            SessionBuilder = SessionBuilderGO.GetComponent<SessionBuilder>();
            SessionBuilderGO.SetActive(false);

            ExperimenterDisplayCanvas = ExperimenterDisplay_Parent.transform.Find("ExperimenterCanvas").GetComponent<Canvas>();

            if (Session.WebBuild)
                ExperimenterDisplayCanvas.targetDisplay = 0;

            ExperimenterDisplayGO = ExperimenterDisplayCanvas.transform.Find("ExpDisplay").gameObject;

            TaskOrder_GridParent = ExperimenterDisplayGO.transform.Find("HotKeyPanel").transform.Find("Image").transform.Find("TaskOrderSection").transform.Find("TaskOrder_GridParent").gameObject;

            ExperimenterDisplayGO.SetActive(false);
        }

        private void CreateMirrorCam()
        {
            MirrorCamGO = new GameObject("MirrorCamera");
            MirrorCam = MirrorCamGO.AddComponent<Camera>();
            Skybox skybox = MirrorCamGO.AddComponent<Skybox>();
            MirrorCam.CopyFrom(Camera.main);
            MirrorCam.cullingMask = 0;

            ExpDisplayRenderImage = ExperimenterDisplayGO.transform.Find("TopRightSection_Background").transform.Find("MainCameraCopy").gameObject.GetComponent<RawImage>();
            if (ExpDisplayRenderImage == null)
                Debug.LogError("EXP DISPLAY RENDER IMAGE IS NULL");
        }
        
        private void CreateSessionSettingsFolder()
        {
            string folderName = "SessionConfigs";

            if (Session.UsingServerConfigs)
            {
                StartCoroutine(CreateFolderOnServer(Session.SessionDataPath + Path.DirectorySeparatorChar + folderName, () =>
                {
                    StartCoroutine(CopySessionConfigFolderToDataFolder(folderName));
                }));
            }
            else if (Session.UsingLocalConfigs)
            {
                string sourceFolderPath = Session.ConfigFolderPath;
                string destinationFolderPath = Session.SessionDataPath + Path.DirectorySeparatorChar + folderName;
                CopyLocalFolder(sourceFolderPath, destinationFolderPath);
            }
            else if (Session.UsingDefaultConfigs)
            {
                Debug.Log("Using Default Configs, so not copying the session config folder to the data folder.");
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


        public void HandleToggleAudioButtonClick()
        {
            Session.SessionDef.PlayBackgroundMusic = !Session.SessionDef.PlayBackgroundMusic;

            if (Session.SessionAudioController.BackgroundMusic_AudioSource.isPlaying)
            {
                Session.SessionAudioController.StopBackgroundMusic();
                RedAudioCross.SetActive(true);
            }
            else
            {
                Session.SessionAudioController.PlayBackgroundMusic();
                RedAudioCross.SetActive(false);
            }
        }

        public void HandleHumanVersionToggleButtonClick()
        {
            Session.SessionDef.IsHuman = !Session.SessionDef.IsHuman;

            if(Session.SessionDef.IsHuman)
            {
                ToggleAudioButton.SetActive(true);
                RedAudioCross.SetActive(!Session.SessionAudioController.BackgroundMusic_AudioSource.isPlaying);

                if (Session.SessionDef.PlayBackgroundMusic)
                {
                    Session.SessionAudioController.PlayBackgroundMusic();
                    RedAudioCross.SetActive(false);
                }
            }
            else
            {
                if(Session.SessionAudioController.BackgroundMusic_AudioSource != null)
                {
                    Session.SessionAudioController.StopBackgroundMusic();
                    RedAudioCross.SetActive(true);
                }
                ToggleAudioButton.SetActive(false);   
            }
            HumanVersionToggleButton.GetComponentInChildren<TextMeshProUGUI>().text = Session.SessionDef.IsHuman ? "Human Version" : "Primate Version";
        }

        private void LoadGazeGameObjects()
        {
            if (GameObject.Find("TobiiEyeTrackerController") == null)
            {
                if(!Session.SessionDef.SpoofGazeWithMouse){
                // gets called once when finding and creating the tobii eye tracker prefabs
                GameObject TobiiEyeTrackerControllerGO = new GameObject("TobiiEyeTrackerController");
                Session.TobiiEyeTrackerController = TobiiEyeTrackerControllerGO.AddComponent<TobiiEyeTrackerController>();
                Session.TobiiEyeTrackerController.EyeTracker_GO = Instantiate(Resources.Load<GameObject>("EyeTracker"), TobiiEyeTrackerControllerGO.transform);
                Session.TobiiEyeTrackerController.TrackBoxGuide_GO = Instantiate(Resources.Load<GameObject>("TrackBoxGuide"), TobiiEyeTrackerControllerGO.transform);
                }

                Session.GazeCalibrationController = Instantiate(Resources.Load<GameObject>("GazeCalibration")).GetComponent<GazeCalibrationController>();
               // THIS LINE BELOW CONTROLS THE OVERLAYING GAZE ONTO THE PLAYER SCENE
               // Session.TobiiEyeTrackerController.GazeTrail_GO = Instantiate(Resources.Load<GameObject>("GazeTrail"), TobiiEyeTrackerControllerGO.transform);
            }
            
        }

        public void AssignExperimenterDisplayRenderTexture(Camera cam)
        {
            if (Session.WebBuild)
                return;

            CameraRenderTexture = new RenderTexture(Screen.width, Screen.height, 24);
            CameraRenderTexture.Create();
            cam.targetTexture = CameraRenderTexture;
            ExpDisplayRenderImage.texture = CameraRenderTexture;   
        }

        private void AppendSerialData()
        {
            if (Session.SessionDef.SerialPortActive)
            {
                if (Session.SerialPortController.BufferCount("sent") > 0)
                {
                    try
                    {
                        StartCoroutine(Session.SerialSentData.AppendDataToBuffer());
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }

                if (Session.SerialPortController.BufferCount("received") > 0)
                {
                    try
                    {
                        StartCoroutine(Session.SerialRecvData.AppendDataToBuffer());
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

            if (Session.UsingDefaultConfigs)
                path = Application.persistentDataPath + Path.DirectorySeparatorChar + "M_USE_DefaultConfigs";
            else if (Session.UsingServerConfigs)
                path = $"{ServerManager.SessionConfigFolderPath}/{configName}";
            else
            {
                if (!SessionSettings.SettingExists("Session", "ConfigFolderNames"))
                    return Session.ConfigFolderPath + Path.DirectorySeparatorChar + configName;
                else
                {
                    List<string> configFolders = (List<string>)SessionSettings.Get("Session", "ConfigFolderNames");
                    int index = 0;
                    foreach (string k in Session.SessionDef.TaskMappings.Keys)
                    {
                        if (k.Equals(configName)) break;
                        ++index;
                    }
                    path = Session.ConfigFolderPath + Path.DirectorySeparatorChar + configFolders[index];
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

        private IEnumerator CopySessionConfigFolderToDataFolder(string folderName)
        {
            string sourcePath = ServerManager.SessionConfigFolderPath;
            string destinationPath = $"{ServerManager.SessionDataFolderPath}/{folderName}";
            yield return ServerManager.CopyFolder(sourcePath, destinationPath);
        }


        public void OnGUI()
        {
            if (CameraRenderTexture == null) return;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), CameraRenderTexture);
        }
        
        
        public void SetCurrentTask<T>(string taskName) where T : ControlLevel_Task_Template
        {
            CurrentTask = GameObject.Find(taskName + "_Scripts").GetComponent<T>();
        }


    }
}
