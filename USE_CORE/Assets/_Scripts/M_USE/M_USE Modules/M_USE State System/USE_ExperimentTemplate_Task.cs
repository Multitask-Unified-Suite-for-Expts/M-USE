using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using USE_UI;
using USE_States;
using USE_Settings;
using USE_StimulusManagement;
using ConfigDynamicUI;
using USE_ExperimenterDisplay;
using USE_ExperimentTemplate_Classes;
using USE_ExperimentTemplate_Data;
using USE_ExperimentTemplate_Trial;
using USE_ExperimentTemplate_Block;
using SelectionTracking;
using UnityEngine.InputSystem;
using USE_DisplayManagement;
using Tobii.Research.Unity;
using Tobii.Research;
using static UnityEngine.EventSystems.EventTrigger;
using MazeGame_Namespace;
using UnityEngine.UIElements;

namespace USE_ExperimentTemplate_Task
{

    public abstract class ControlLevel_Task_Template : ControlLevel
    {
        public float ShotgunRaycastCircleSize_DVA;
        public float ShotgunRaycastSpacing_DVA;
        public float ParticipantDistance_CM;

        public string PrefabPath;

        public string ConfigName;
        public string TaskName;
        public string TaskProjectFolder;
        [HideInInspector] public int BlockCount;
        protected int NumBlocksInTask;
        public ControlLevel_Trial_Template TrialLevel;
        protected BlockData BlockData;
        protected FrameData FrameData;
        protected TrialData TrialData;
        [HideInInspector] public SerialSentData SerialSentData;
        [HideInInspector] public SerialRecvData SerialRecvData;

        [HideInInspector] public SessionDataControllers SessionDataControllers;
        [HideInInspector] public SelectionTracker SelectionTracker;
        [HideInInspector] public bool EyeTrackerActive;
        [HideInInspector] public EyeTracker EyeTracker;
        [HideInInspector] public IEyeTracker IEyeTracker;
        [HideInInspector] public ScreenBasedCalibration ScreenBasedCalibration;
        [HideInInspector] public DisplayArea DisplayArea;

        [HideInInspector] public bool StoreData, SerialPortActive, SyncBoxActive, EventCodesActive, RewardPulsesActive, SonicationActive, UseDefaultConfigs;
        [HideInInspector] public string ContextExternalFilePath, SessionDataPath, TaskConfigPath, TaskDataPath, SubjectID, SessionID, FilePrefix, EyetrackerType, SelectionType;
        [HideInInspector] public MonitorDetails MonitorDetails;
        [HideInInspector] public LocateFile LocateFile;
        [HideInInspector] public StringBuilder BlockSummaryString, CurrentTaskSummaryString, PreviousBlockSummaryString;
        private int TaskStringsAdded = 0;

        // public string TaskSceneName;
        public Camera TaskCam;
        public GameObject[] TaskCanvasses;

        //protected TrialDef[] AllTrialDefs;
        //protected TrialDef[] CurrentBlockTrialDefs;
        protected TaskDef TaskDef;
        protected BlockDef[] BlockDefs;
        protected BlockDef CurrentBlockDef;
        protected TrialDef[] AllTrialDefs;

        public BlockDef currentBlockDef
        {
            get
            {
                return CurrentBlockDef;
            }
        }


        public TaskStims TaskStims;
        [HideInInspector] public StimGroup PreloadedStims, PrefabStims, ExternalStims, RuntimeStims;
        public List<GameObject> PreloadedStimGameObjects;
        public List<string> PrefabStimPaths;
        protected ConfigUI configUI;
        protected ConfigVarStore ConfigUiVariables;
        [HideInInspector] public ExperimenterDisplayController ExperimenterDisplayController;
        [HideInInspector] public SessionInfoPanel SessionInfoPanel;

        [HideInInspector] public DisplayController DisplayController;

        private GameObject Controllers;

        [HideInInspector] public SerialPortThreaded SerialPortController;
        [HideInInspector] public SyncBoxController SyncBoxController;
        [HideInInspector] public EventCodeManager EventCodeManager;
        protected Dictionary<string, EventCode> CustomTaskEventCodes;
        public Dictionary<string, EventCode> SessionEventCodes;

        public Type TaskLevelType;
        protected Type TrialLevelType, TaskDefType, BlockDefType, TrialDefType, StimDefType;
        protected State SetupTask, RunBlock, BlockFeedback, FinishTask;
        protected bool BlockFbFinished;
        protected float BlockFbSimpleDuration;
        protected TaskLevelTemplate_Methods TaskLevel_Methods;

        protected int? MinTrials, MaxTrials;

        [HideInInspector] public RenderTexture DrawRenderTexture;

        [HideInInspector] public GameObject TaskSelectionCanvasGO;

        [HideInInspector] public bool IsHuman;

        [HideInInspector] public HumanStartPanel HumanStartPanel;

        [HideInInspector] public event EventHandler TaskSkyboxSet_Event;



        public virtual void SpecifyTypes()
        {
            TaskLevelType = USE_Tasks_CustomTypes.CustomTaskDictionary[TaskName].TaskLevelType;
            TrialLevelType = USE_Tasks_CustomTypes.CustomTaskDictionary[TaskName].TrialLevelType;
            TaskDefType = USE_Tasks_CustomTypes.CustomTaskDictionary[TaskName].TaskDefType;
            BlockDefType = USE_Tasks_CustomTypes.CustomTaskDictionary[TaskName].BlockDefType;
            TrialDefType = USE_Tasks_CustomTypes.CustomTaskDictionary[TaskName].TrialDefType;
            StimDefType = USE_Tasks_CustomTypes.CustomTaskDictionary[TaskName].StimDefType;
        }

        public void DefineTaskLevel(bool verifyOnly)
        {
            if (UseDefaultConfigs)
                PrefabPath = "/DefaultResources/Stimuli";

            TaskLevel_Methods = new TaskLevelTemplate_Methods();
            ReadSettingsFiles(verifyOnly);
            ProcessCustomSettingsFiles();
            FindStims();
            if (verifyOnly) return;

            SetupTask = new State("SetupTask");
            RunBlock = new State("RunBlock");
            BlockFeedback = new State("BlockFeedback");
            FinishTask = new State("FinishTask");
            RunBlock.AddChildLevel(TrialLevel);
            AddActiveStates(new List<State> { SetupTask, RunBlock, BlockFeedback, FinishTask });

            if (EyeTrackerActive)
            {
               // InitializeEyeTrackerSettings();
                TrialLevel.EyeTracker = EyeTracker;
                TrialLevel.IEyeTracker = IEyeTracker;
                TrialLevel.ScreenBasedCalibration = ScreenBasedCalibration;
                TrialLevel.DisplayArea = DisplayArea;
               // GameObject.Find("[TrackBoxGuide]").GetComponent<TrackBoxGuide>().SetCanvasTrackBox(GameObject.Find($"{TaskName}_Canvas"));
            }
                TrialLevel.TrialDefType = TrialDefType;
            TrialLevel.StimDefType = StimDefType;

            Add_ControlLevel_InitializationMethod(() =>
            {
                BlockCount = -1;
                BlockSummaryString = new StringBuilder();
                PreviousBlockSummaryString = new StringBuilder();
                CurrentTaskSummaryString = new StringBuilder();

                #if (!UNITY_WEBGL)
                    SessionInfoPanel = GameObject.Find("SessionInfoPanel").GetComponent<SessionInfoPanel>();

                    if (configUI == null)
                        configUI = FindObjectOfType<ConfigUI>();
                    configUI.clear();
                    if (ConfigUiVariables != null)
                        configUI.store = ConfigUiVariables;
                    else
                        configUI.store = new ConfigVarStore();
                    configUI.GenerateUI();

                #endif

                TaskCam.gameObject.SetActive(true);
                if (TaskCanvasses != null)
                    foreach (GameObject go in TaskCanvasses)
                        go.SetActive(true);

                Controllers.SetActive(true);
            });
            
            SetupTask.AddInitializationMethod(() =>
            {
                SetTaskSummaryString();
                EventCodeManager.SendCodeImmediate(SessionEventCodes["SetupTaskStarts"]);

                //Create HumanStartPanel

                if (IsHuman)
                {
                    HumanStartPanel.SetupDataAndCodes(FrameData, EventCodeManager, SessionEventCodes);
                    HumanStartPanel.SetTaskLevel(this);
                    Canvas taskCanvas = GameObject.Find(TaskName + "_Canvas").GetComponent<Canvas>();
                    HumanStartPanel.CreateHumanStartPanel(taskCanvas, TaskName);
                }
            });

            SetupTask.SpecifyTermination(() => true, RunBlock);


            RunBlock.AddUniversalInitializationMethod(() =>
            {
                EventCodeManager.SendCodeImmediate(SessionEventCodes["RunBlockStarts"]);

                BlockCount++;
                CurrentBlockDef = BlockDefs[BlockCount];
                TrialLevel.BlockCount = BlockCount;
                if (BlockCount == 0)
                    TrialLevel.TrialCount_InTask = -1;
                TrialLevel.TrialDefs = CurrentBlockDef.TrialDefs;
            });

        //Hotkey for WebGL build so we can end task and go to next block
        #if(UNITY_WEBGL)
            RunBlock.AddUpdateMethod(() =>
            {
                if(TrialLevel != null)
                {
                    if(InputBroker.GetKeyUp(KeyCode.P)) //Pause Game:
                        Time.timeScale = Time.timeScale == 1 ? 0 : 1;

                    if (InputBroker.GetKeyUp(KeyCode.E)) //End Task
                    {
                        if (Time.timeScale == 0) //if paused, unpause before ending task
                            Time.timeScale = 1;

                        TrialLevel.AbortCode = 5;
                        TrialLevel.ForceBlockEnd = true;
                        TrialLevel.FinishTrialCleanup();
                        TrialLevel.ClearActiveTrialHandlers();
                        SpecifyCurrentState(FinishTask);
                    }

                    if (InputBroker.GetKeyUp(KeyCode.N)) //Next Block
                    {
                        if (Time.timeScale == 0) //if paused, unpause before ending block
                            Time.timeScale = 1;

                        if (TrialLevel.AudioFBController.IsPlaying())
                            TrialLevel.AudioFBController.audioSource.Stop();
                        TrialLevel.AbortCode = 3;
                        TrialLevel.ForceBlockEnd = true;
                        TrialLevel.SpecifyCurrentState(TrialLevel.GetStateFromName("FinishTrial"));
                    }
                }
            });
        #endif

            RunBlock.AddLateUpdateMethod(() =>
            {
                FrameData.AppendData();
                EventCodeManager.EventCodeLateUpdate();
            });
            RunBlock.SpecifyTermination(() => TrialLevel.Terminated, BlockFeedback);
            


            BlockFeedback.AddUniversalInitializationMethod(() =>
            {
                if(BlockSummaryString != null)
                {
                    int trialsCompleted = (TrialLevel.AbortCode == 0 || TrialLevel.AbortCode == 6) ? TrialLevel.TrialCount_InBlock + 1 : TrialLevel.TrialCount_InBlock;
                    string blockTitle = $"<b>\n- - - - - - - - - - - - - - - - - - - - - - - - - - - - - -" +
                                        $"\n\nBlock {BlockCount + 1}" +
                                        $"\nTrials Completed: {trialsCompleted}\n</b>";

                    PreviousBlockSummaryString.Insert(0, BlockSummaryString); //Add current block string to full list of previous blocks. 
                    PreviousBlockSummaryString.Insert(0, blockTitle);
                }
                EventCodeManager.SendCodeImmediate(SessionEventCodes["BlockFeedbackStarts"]);
            });
            BlockFeedback.AddUpdateMethod(() =>
            {
                if (Time.time - BlockFeedback.TimingInfo.StartTimeAbsolute >= BlockFbSimpleDuration)
                    BlockFbFinished = true;
                else
                    BlockFbFinished = false;
            });
            BlockFeedback.AddLateUpdateMethod(() =>
            {
                if (StoreData)
                    FrameData.AppendData();

                EventCodeManager.EventCodeLateUpdate();
            });
            BlockFeedback.SpecifyTermination(() => BlockFbFinished && BlockCount < BlockDefs.Length - 1, RunBlock, () =>
            {
                if (StoreData)
                {
                    BlockData.AppendData();
                    BlockData.WriteData();
                }
            });
            BlockFeedback.SpecifyTermination(() => BlockFbFinished && BlockCount == BlockDefs.Length - 1, FinishTask, () =>
            {
                if (StoreData)
                {
                    BlockData.AppendData();
                    BlockData.WriteData();
                }
            });

            FinishTask.AddDefaultInitializationMethod(() =>
            {
                EventCodeManager.SendCodeImmediate(SessionEventCodes["FinishTaskStarts"]);

                //Clear trialsummarystring and Blocksummarystring at end of task:
                if(TrialLevel.TrialSummaryString != null && BlockSummaryString != null)
                {
                    TrialLevel.TrialSummaryString = "";
                    BlockSummaryString.Clear();
                    BlockSummaryString.AppendLine("");
                }

                ClearActiveTaskHandlers();
            });

            FinishTask.SpecifyTermination(() => true, () => null);

            AddDefaultTerminationMethod(() =>
            {
                if(SessionDataControllers != null)
                {
                    SessionDataControllers.RemoveDataController("BlockData_" + TaskName);
                    SessionDataControllers.RemoveDataController("TrialData_" + TaskName);
                    SessionDataControllers.RemoveDataController("FrameData_" + TaskName);
                }


                int sgNum = TaskStims.AllTaskStimGroups.Count;
                for (int iSg = 0; iSg < sgNum; iSg++)
                {
                    StimGroup[] taskSgs = new StimGroup[TaskStims.AllTaskStimGroups.Count];
                    TaskStims.AllTaskStimGroups.Values.CopyTo(taskSgs, 0);
                    StimGroup sg = taskSgs[0];
                    while (sg.stimDefs.Count > 0)
                    {
                        sg.stimDefs[0].Destroy();
                        sg.stimDefs.RemoveAt(0);
                    }

                    sg.DestroyStimGroup();
                }

                TaskStims.AllTaskStims.DestroyStimGroup();
                TaskCam.gameObject.SetActive(false);

                if (TaskCanvasses != null)
                    foreach (GameObject go in TaskCanvasses)
                        go.SetActive(false);

                Destroy(Controllers);

#if (!UNITY_WEBGL)
                    //Destroy Text on Experimenter Display:
                    foreach (Transform child in GameObject.Find("MainCameraCopy").transform)
                    {
                        Destroy(child.gameObject);
                    }
#endif
            });




            //Setup data management
            TaskDataPath = SessionDataPath + Path.DirectorySeparatorChar + ConfigName;
            FilePrefix = FilePrefix + "_" + ConfigName;
            BlockData = (BlockData) SessionDataControllers.InstantiateDataController<BlockData>("BlockData", ConfigName,
                StoreData, TaskDataPath + Path.DirectorySeparatorChar + "BlockData");
                //InstantiateBlockData(StoreData, ConfigName,
                //TaskDataPath + Path.DirectorySeparatorChar + "BlockData");
            BlockData.taskLevel = this;
            BlockData.fileName = FilePrefix + "__BlockData.txt";

            TrialData = (TrialData) SessionDataControllers.InstantiateDataController<TrialData>("TrialData", ConfigName,
                StoreData, TaskDataPath + Path.DirectorySeparatorChar + "TrialData");
                //SessionDataControllers.InstantiateTrialData(StoreData, ConfigName,
                //TaskDataPath + Path.DirectorySeparatorChar + "TrialData");
            TrialData.taskLevel = this;
            TrialData.trialLevel = TrialLevel;
            TrialLevel.TrialData = TrialData;
            TrialData.fileName = FilePrefix + "__TrialData.txt";


            FrameData = (FrameData) SessionDataControllers.InstantiateDataController<FrameData>("FrameData", ConfigName,
                StoreData, TaskDataPath + Path.DirectorySeparatorChar + "FrameData");

            //SessionDataControllers.InstantiateFrameData(StoreData, ConfigName,
              //  TaskDataPath + Path.DirectorySeparatorChar + "FrameData");
            FrameData.taskLevel = this;
            FrameData.trialLevel = TrialLevel;
            TrialLevel.FrameData = FrameData;
            FrameData.fileName = FilePrefix + "__FrameData_PreTrial.txt";


            BlockData.InitDataController();
            TrialData.InitDataController();
            FrameData.InitDataController();

            BlockData.ManuallyDefine();
            FrameData.ManuallyDefine();

            if (EventCodesActive)
                FrameData.AddEventCodeColumns();

            //user-defined task control level 
            DefineControlLevel();

            BlockData.AddStateTimingData(this);
            BlockData.CreateFile();
            //BlockData.LogDataController(); //USING TO SEE FORMAT OF DATA CONTROLLER
            FrameData.CreateFile();
            //FrameData.LogDataController(); //USING TO SEE FORMAT OF DATA CONTROLLER

            //AddDataController(BlockData, StoreData, TaskDataPath + Path.DirectorySeparatorChar + "BlockData", FilePrefix + "_BlockData.txt");
            Controllers = new GameObject("Controllers");
            GameObject fbControllers = Instantiate(Resources.Load<GameObject>("FeedbackControllers"), Controllers.transform);
            GameObject inputTrackers = Instantiate(Resources.Load<GameObject>("InputTrackers"), Controllers.transform);
            
            // fbControllers.transform.SetParent(Controllers.transform);
            // inputTrackers.transform.SetParent(Controllers.transform);
            if (!EyeTrackerActive)
                inputTrackers.GetComponent<GazeTracker>().enabled = false;


            // GameObject fbControllers = Instantiate(fbControllersPrefab, Controllers.transform);
            // GameObject inputTrackers = Instantiate(inputTrackersPrefab, Controllers.transform);


            List<string> fbControllersList = new List<string>();
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "FeedbackControllers"))
                fbControllersList = (List<string>)SessionSettings.Get(TaskName + "_TaskSettings", "FeedbackControllers");
            int totalTokensNum = 5;
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "TotalTokensNum"))
                totalTokensNum = (int)SessionSettings.Get(TaskName + "_TaskSettings", "TotalTokensNum");

            fbControllers.GetComponent<AudioFBController>().SessionEventCodes = SessionEventCodes;
            fbControllers.GetComponent<HaloFBController>().SessionEventCodes = SessionEventCodes;
            fbControllers.GetComponent<TokenFBController>().SessionEventCodes = SessionEventCodes;
            fbControllers.GetComponent<SliderFBController>().SessionEventCodes = SessionEventCodes;
            fbControllers.GetComponent<TouchFBController>().SessionEventCodes = SessionEventCodes;

            TrialLevel.SelectionTracker = SelectionTracker;
                
            TrialLevel.AudioFBController = fbControllers.GetComponent<AudioFBController>();
            TrialLevel.HaloFBController = fbControllers.GetComponent<HaloFBController>();
            TrialLevel.TokenFBController = fbControllers.GetComponent<TokenFBController>();
            TrialLevel.SliderFBController = fbControllers.GetComponent<SliderFBController>();
            TrialLevel.TouchFBController = fbControllers.GetComponent<TouchFBController>();
            TrialLevel.TouchFBController.audioFBController = TrialLevel.AudioFBController;

            TrialLevel.SerialPortController = SerialPortController;
            TrialLevel.SerialPortActive = SerialPortActive;
            TrialLevel.SerialRecvData = SerialRecvData;
            TrialLevel.SerialSentData = SerialSentData;
            TrialLevel.SyncBoxController = SyncBoxController;

            TrialLevel.DisplayController = DisplayController; 

            if(SyncBoxController != null)
                TrialLevel.SyncBoxController.EventCodeManager = EventCodeManager;

            TrialLevel.EventCodeManager = EventCodeManager;
            TrialLevel.TouchFBController.EventCodeManager = EventCodeManager;

            if (CustomTaskEventCodes != null)
                TrialLevel.TaskEventCodes = CustomTaskEventCodes;
            if (SessionEventCodes != null)
                TrialLevel.SessionEventCodes = SessionEventCodes;

            TrialLevel.UseDefaultConfigs = UseDefaultConfigs;


            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ShotgunRaycastCircleSize_DVA"))
                TrialLevel.ShotgunRaycastCircleSize_DVA = (float)SessionSettings.Get(TaskName + "_TaskSettings", "ShotgunRaycastCircleSize_DVA");
            else
                TrialLevel.ShotgunRaycastCircleSize_DVA = ShotgunRaycastCircleSize_DVA;

            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ParticipantDistance_CM"))
                TrialLevel.ParticipantDistance_CM = (float)SessionSettings.Get(TaskName + "_TaskSettings", "ParticipantDistance_CM");
            else
                TrialLevel.ParticipantDistance_CM = ParticipantDistance_CM;

            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ShotgunRaycastSpacing_DVA"))
                TrialLevel.ShotgunRaycastSpacing_DVA = (float)SessionSettings.Get(TaskName + "_TaskSettings", "ShotgunRaycastSpacing_DVA");
            else
                TrialLevel.ShotgunRaycastSpacing_DVA = ShotgunRaycastSpacing_DVA;


            if (UseDefaultConfigs)
                TrialLevel.LoadTexturesFromResources();
            else
                TrialLevel.LoadTextures(ContextExternalFilePath); //loading the textures before Init'ing the TouchFbController. 

            //Automatically giving TouchFbController;
            TrialLevel.TouchFBController.Init(TrialData, FrameData);

            bool audioInited = false;
            foreach (string fbController in fbControllersList)
            {
                switch (fbController)
                {
                    case "Audio":
                        if (!audioInited)
                        {
                            TrialLevel.AudioFBController.Init(FrameData, EventCodeManager);
                            audioInited = true;
                        }
                        break; 
                    
                    case "Halo":
                        TrialLevel.HaloFBController.Init(FrameData, EventCodeManager);
                        break;
                    
                    case "Token":
                        if (!audioInited)
                        {
                            TrialLevel.AudioFBController.Init(FrameData, EventCodeManager);
                            audioInited = true;
                        }
                        TrialLevel.TokenFBController.Init(TrialData, FrameData, TrialLevel.AudioFBController, EventCodeManager);
                        break;
                    
                    case "Slider":
                        if (!audioInited)
                        {
                            TrialLevel.AudioFBController.Init(FrameData, EventCodeManager);
                            audioInited = true;
                        }
                        TrialLevel.SliderFBController.Init(TrialData, FrameData, TrialLevel.AudioFBController);
                        break;
                    
                    default:
                        Debug.LogWarning(fbController + " is not a valid feedback controller.");
                        break;
                }
            }

            TrialLevel.MouseTracker = inputTrackers.GetComponent<MouseTracker>();
            TrialLevel.MouseTracker.Init(FrameData, 0);
            TrialLevel.MouseTracker.ShotgunRaycast.SetShotgunVariables(ShotgunRaycastCircleSize_DVA, ParticipantDistance_CM, ShotgunRaycastSpacing_DVA);
            if (EyeTrackerActive)
            {
                //MAYBE DON'T NEED TO GATE, CHECK - SD
                TrialLevel.GazeTracker = inputTrackers.GetComponent<GazeTracker>();
                TrialLevel.GazeTracker.Init(FrameData, 0);
            }
            TrialLevel.SelectionType = SelectionType;

            Controllers.SetActive(false);

            TrialLevel.SessionDataControllers = SessionDataControllers;
            TrialLevel.FilePrefix = FilePrefix;
            TrialLevel.TaskStims = TaskStims;
            TrialLevel.PreloadedStims = PreloadedStims;
            TrialLevel.PrefabStims = PrefabStims;
            TrialLevel.ExternalStims = ExternalStims;
            TrialLevel.RuntimeStims = RuntimeStims;
            TrialLevel.ConfigUiVariables = ConfigUiVariables;
            TrialLevel.IsHuman = IsHuman;
            TrialLevel.HumanStartPanel = HumanStartPanel;
            TrialLevel.TaskSelectionCanvasGO = TaskSelectionCanvasGO;

            TrialLevel.DefineTrialLevel();
        }

        
        public void ClearActiveTaskHandlers()
        {
            if (SelectionTracker.TaskHandlerNames.Count > 0)
            {
                List<string> toRemove = new List<string>();

                foreach (string handlerName in SelectionTracker.TaskHandlerNames)
                {
                    if (SelectionTracker.ActiveSelectionHandlers.ContainsKey(handlerName))
                    {
                        SelectionTracker.ActiveSelectionHandlers.Remove(handlerName);
                        toRemove.Add(handlerName);
                    }
                }

                foreach (string handlerName in toRemove)
                    SelectionTracker.TaskHandlerNames.Remove(handlerName);
            }
        }

        private void ReadSettingsFiles(bool verifyOnly)
        {
            //user specifies what custom types they have that inherit from TaskDef, BlockDef, and TrialDef;
            SpecifyTypes();
            TaskStims = new TaskStims();

            if (TaskDefType == null)
                TaskDefType = typeof(TaskDef);
            if (BlockDefType == null)
                BlockDefType = typeof(BlockDef);
            if (TrialDefType == null)
                TrialDefType = typeof(TrialDef);
            if (StimDefType == null)
                StimDefType = typeof(StimDef);


            //read in the TaskDef, BlockDef, TrialDef, and StimDef files (any of these may not exist)
            MethodInfo readTaskDef = GetType().GetMethod(nameof(this.ReadTaskDef))
                .MakeGenericMethod(new Type[] { TaskDefType });
            readTaskDef.Invoke(this, new object[] { TaskConfigPath });
            MethodInfo readBlockDefs = GetType().GetMethod(nameof(this.ReadBlockDefs))
                .MakeGenericMethod(new Type[] { BlockDefType });
            readBlockDefs.Invoke(this, new object[] { TaskConfigPath });
            MethodInfo readTrialDefs = GetType().GetMethod(nameof(this.ReadTrialDefs))
                .MakeGenericMethod(new Type[] { TrialDefType });
            readTrialDefs.Invoke(this, new object[] { TaskConfigPath });
            MethodInfo readStimDefs = GetType().GetMethod(nameof(this.ReadStimDefs))
                .MakeGenericMethod(new Type[] { StimDefType });
            readStimDefs.Invoke(this, new object[] { TaskConfigPath });



            string configUIVariableFile = LocateFile.FindFileInExternalFolder(TaskConfigPath, "*" + TaskName + "*ConfigUiDetails*");
            if (!string.IsNullOrEmpty(configUIVariableFile))
            {
                SessionSettings.ImportSettings_SingleTypeJSON<ConfigVarStore>(TaskName + "_ConfigUiDetails", configUIVariableFile);
                ConfigUiVariables = (ConfigVarStore)SessionSettings.Get(TaskName + "_ConfigUiDetails");
            }

            string eventCodeFile = LocateFile.FindFileInExternalFolder(TaskConfigPath, "*" + TaskName + "*EventCodeConfig*");
            if (!string.IsNullOrEmpty(eventCodeFile))
            {
                SessionSettings.ImportSettings_SingleTypeJSON<Dictionary<string, EventCode>>(TaskName + "_EventCodeConfig", eventCodeFile);
                CustomTaskEventCodes = (Dictionary<string, EventCode>)SessionSettings.Get(TaskName + "_EventCodeConfig");
                EventCodesActive = true;
            }



            //---------------------------------------------------------------------------------------------------------------------------------------
            Dictionary<string, string> customSettings = new Dictionary<string, string>();
            string settingsFilePath;

            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "CustomSettings"))
                customSettings = (Dictionary<string, string>)SessionSettings.Get(TaskName + "_TaskSettings", "CustomSettings");

            if (customSettings != null)
            {
                foreach (string key in customSettings.Keys)
                {
                    string filePath = TaskConfigPath + Path.DirectorySeparatorChar + key; //initially set for not default configs, then changed below for UseDefaultConfigs
                    string settingsFileName = key.Split('.')[0];

                    if (UseDefaultConfigs)
                    {
                        string folderPath = Application.persistentDataPath + Path.DirectorySeparatorChar + "M_USE_DefaultConfigs" + Path.DirectorySeparatorChar + TaskName + "_DefaultConfigs";
                        filePath = folderPath + Path.DirectorySeparatorChar + settingsFileName;

                        if(!Directory.Exists(folderPath))
                            Directory.CreateDirectory(folderPath);

                        if (!File.Exists(filePath))
                        {
                            var db = Resources.Load<TextAsset>(TaskName + "_DefaultConfigs/" + settingsFileName);
                            byte[] data = db.bytes;
                            System.IO.File.WriteAllBytes(filePath, data);
                        }
                        else
                            settingsFilePath = LocateFile.FindFileInExternalFolder(TaskConfigPath, "*" + TaskName + "*" + settingsFileName + "*");
                        
                    }

                    Type settingsType = GetTaskCustomSettingsType(settingsFileName);
               
                    switch (customSettings[key].ToLower())
                    {
                        case ("singletypearray"):
                            MethodInfo readCustomSingleTypeArray = GetType().GetMethod(nameof(this.ReadCustomSingleTypeArray)).MakeGenericMethod(new Type[] { settingsType });
                            readCustomSingleTypeArray.Invoke(this, new object[] { filePath, settingsFileName});
                            break;
                        case ("singletypejson"):
                            MethodInfo readCustomSingleTypeJson = GetType().GetMethod(nameof(this.ReadCustomSingleTypeJson)).MakeGenericMethod(new Type[] { settingsType });
                            readCustomSingleTypeJson.Invoke(this, new object[] { filePath, settingsFileName });
                            break;
                        case ("multipletype"):
                            MethodInfo readCustomMultipleTypes = GetType().GetMethod(nameof(this.ReadCustomMultipleTypes)).MakeGenericMethod(new Type[] { settingsType });
                            readCustomMultipleTypes.Invoke(this, new object[] { filePath, settingsFileName });
                            break;
                        default:
                            Debug.LogError("DEFAULT CUSTOM SETTINGS SWITCH STATEMENT");
                            break;
                    }
                }
                //--------------------------------------------------------------------------------------------------------------------------------------


                //handling of block and trial defs so that each BlockDef contains a TrialDef[] array

                if (AllTrialDefs == null) //no trialDefs have been imported from settings files
                {
                    if (BlockDefs == null)
                        Debug.LogError("Neither BlockDef nor TrialDef config files provided in " + TaskName +
                                       " folder, no trials generated as a result.");
                    else
                    {
                        if (!verifyOnly)
                        {
                            for (int iBlock = 0; iBlock < BlockDefs.Length; iBlock++)
                            {
                                BlockDefs[iBlock].RandomNumGenerator = new System.Random((int)DateTime.Now.Ticks + iBlock);
                                BlockDefs[iBlock].GenerateTrialDefsFromBlockDef();
                            }
                        }
                    }

                }
                else //trialDefs imported from settings files
                {
                    if (BlockDefs == null) //no blockDef file, trialdefs should be complete
                    {
                        Debug.Log("TrialDef config file provided without BlockDef config file in " + TaskName +
                                  " folder, BlockDefs will be generated with default values for all fields from TrialDefs.");
                        if (AllTrialDefs[AllTrialDefs.Length - 1].BlockCount != 0)
                        {
                            if (AllTrialDefs[0].BlockCount == 0)
                                BlockDefs = new BlockDef[AllTrialDefs[AllTrialDefs.Length - 1].BlockCount];
                            else if (AllTrialDefs[0].BlockCount == 1)
                                BlockDefs = new BlockDef[AllTrialDefs[AllTrialDefs.Length - 1].BlockCount - 1];
                            else
                                Debug.LogError("TrialDef config file in " + TaskName +
                                               " folder includes BlockCounts that are neither 0- nor 1-indexed.");
                        }
                        else
                        {
                            Debug.Log("TrialDef config file in " + TaskName +
                                      " folder only generates a single block (this is not a problem if you do not intend to use a block structure in your experiment).");
                            BlockDefs = new BlockDef[1];
                        }

                        //add trialDef[] for each block;
                        if (!verifyOnly)
                        {
                            for (int iBlock = 0; iBlock < BlockDefs.Length; iBlock++)
                            {
                                if (BlockDefs[iBlock] == null)
                                    BlockDefs[iBlock] = new BlockDef();
                                BlockDefs[iBlock].BlockCount = iBlock;
                                BlockDefs[iBlock].TrialDefs = GetTrialDefsInBlock(iBlock, AllTrialDefs);
                            }
                        }
                    }
                    else //there is a blockDef file, its information may need to be added to TrialDefs
                    {

                        //add trialDef[] for each block;
                        if (!verifyOnly)
                        {
                            for (int iBlock = 0; iBlock < BlockDefs.Length; iBlock++)
                            {
                                BlockDefs[iBlock].TrialDefs = GetTrialDefsInBlock(iBlock + 1, AllTrialDefs);
                                BlockDefs[iBlock].RandomNumGenerator = new System.Random((int)DateTime.Now.Ticks + iBlock);
                                BlockDefs[iBlock].AddToTrialDefsFromBlockDef();
                            }
                        }

                    }
                }
            }
        }

        public virtual Type GetTaskCustomSettingsType(string typeName)
        {
            return null;
        }

        public virtual void ProcessCustomSettingsFiles()
        {
            
        }

        public virtual Dictionary<string, object> SummarizeTask()
        {
            return new Dictionary<string, object>();
        }

        public void FindStims()
        {
            MethodInfo addTaskStimDefsToTaskStimGroup = GetType().GetMethod(nameof(this.AddTaskStimDefsToTaskStimGroup))
                .MakeGenericMethod(new Type[] { StimDefType });

            //PreloadedStims = GameObjects in scene prior to build
            PreloadedStims = new StimGroup("PreloadedStims");
            TaskStims.AllTaskStimGroups.Add("PreloadedStims", PreloadedStims);
            //Prefab stims are already created in ReadStimDefs
            TaskStims.AllTaskStimGroups.Add("PrefabStims", PrefabStims);
            //ExternalStims is already created in ReadStimDefs (not ideal as hard to follow)
            TaskStims.AllTaskStimGroups.Add("ExternalStims", ExternalStims);
            RuntimeStims = new StimGroup("RuntimeStims");
            TaskStims.AllTaskStimGroups.Add("RuntimeStims", RuntimeStims);

            DefinePreloadedStims();
            if(PrefabStims.stimDefs.Count > 0)
                DefinePrefabStims();
            if(ExternalStims.stimDefs.Count > 0)
                DefineExternalStims();
        }

        protected virtual void DefinePreloadedStims()
        {
            MethodInfo taskStimDefFromGameObject = GetType().GetMethod(nameof(TaskStimDefFromGameObject))
                .MakeGenericMethod((new Type[] { StimDefType }));
            if (PreloadedStimGameObjects != null && PreloadedStimGameObjects.Count > 0)
            {
                foreach (GameObject go in PreloadedStimGameObjects)
                {
                    taskStimDefFromGameObject.Invoke(this, new object[] { go, PreloadedStims });
                    // addTaskStimDefsToTaskStimGroup.Invoke(this, new object[] {TaskConfigPath});
                }
                PreloadedStims.AddStims(PreloadedStimGameObjects);
            }
        }

        protected virtual void DefinePrefabStims()
        {
            MethodInfo taskStimDefFromPrefabPath = GetType().GetMethod(nameof(TaskStimDefFromPrefabPath))
                .MakeGenericMethod((new Type[] { StimDefType }));

            float stimScale = 1;

            if (SessionSettings.SettingClassExists(TaskName + "_TaskSettings"))
            {
                if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ExternalStimScale"))
                    stimScale = (float)SessionSettings.Get(TaskName + "_TaskSettings", "ExternalStimScale");
            }

            foreach (StimDef sd in PrefabStims.stimDefs)
                sd.StimScale = stimScale;

            if (PrefabStimPaths != null && PrefabStimPaths.Count > 0)
            {
                foreach (string path in PrefabStimPaths)
                    taskStimDefFromPrefabPath.Invoke(this, new object[] { path, PrefabStims });
            }
        }

        protected virtual void DefineInternalStims()
        {

        }

        protected virtual void DefineExternalStims()
        {
            // need to add check for files in stimfolderpath if there is no stimdef file (take all files)
            string stimFolderPath = "";
            string stimExtension = "";
            float stimScale = 1;

            if (SessionSettings.SettingClassExists(TaskName + "_TaskSettings"))
            {
                if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ExternalStimFolderPath"))
                    stimFolderPath = (string)SessionSettings.Get(TaskName + "_TaskSettings", "ExternalStimFolderPath");
                if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ExternalStimExtension"))
                    stimExtension = (string)SessionSettings.Get(TaskName + "_TaskSettings", "ExternalStimExtension");
                if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ExternalStimScale"))
                    stimScale = (float)SessionSettings.Get(TaskName + "_TaskSettings", "ExternalStimScale");
            }

            foreach (StimDef sd in ExternalStims.stimDefs)
            {
                sd.StimFolderPath = stimFolderPath;
                sd.StimExtension = stimExtension;
                sd.StimScale = stimScale;

                //add StimExtesion to file path if it doesn't already contain it
                if (!string.IsNullOrEmpty(sd.StimExtension) && !sd.FileName.EndsWith(sd.StimExtension))
                {
                    if (!sd.StimExtension.StartsWith("."))
                        sd.FileName = sd.FileName + "." + sd.StimExtension;
                    else
                        sd.FileName = sd.FileName + sd.StimExtension;
                }

                //we will only use StimFolderPath if ExternalFilePath doesn't already contain it
                if (!string.IsNullOrEmpty(sd.StimFolderPath) && !sd.FileName.StartsWith(sd.StimFolderPath))
                {
                    List<string> filenames = RecursiveFileFinder.FindFile(sd.StimFolderPath, sd.FileName, sd.StimExtension);
                    if (filenames.Count > 1)
                    {
                        string firstFilename = Path.GetFileName(filenames[0]);
                        for (int iFile = filenames.Count - 1; iFile > 0; iFile--)
                        {
                            if (Path.GetFileName(filenames[iFile]) == firstFilename)
                            {
                                Debug.LogWarning("During task setup for " + TaskName + " attempted to find stimulus " +
                                                    sd.FileName + " in folder " + sd.StimFolderPath +
                                                    ", but files with this name are found at both " + firstFilename +
                                                    " and "
                                                    + filenames[iFile] + ".  Only the first will be used.");
                                filenames.RemoveAt(iFile);
                            }
                        }
                    }

                    if (filenames.Count == 1)
                        sd.FileName = filenames[0];
                    else if (filenames.Count == 0)
                        Debug.LogError("During task setup for " + TaskName + " attempted to find stimulus " +
                                        sd.FileName + " in folder " +
                                        sd.StimFolderPath +
                                        " but no file matching this pattern was found in this folder or subdirectories.");
                    else
                    {
                        Debug.LogError("During task setup for " + TaskName + " attempted to find stimulus " +
                                        sd.FileName + " in folder " +
                                        sd.StimFolderPath +
                                        " but multiple non-identical files matching this pattern were found in this folder or subdirectories.");
                    }
                }
                else
                {
                    //if ExternalFilePath already contains the StimFolerPath string, do not change it,
                    //but should also have method to check this file exists
                }

            }
        }



        public void ReadCustomSingleTypeArray<T>(string filePath, string settingsName) where T : CustomSettingsType
        {
            SessionSettings.ImportSettings_SingleTypeArray<T>(settingsName, filePath);
        }

        public void ReadCustomMultipleTypes<T>(string filePath, string settingsName) where T : CustomSettingsType
        {
            SessionSettings.ImportSettings_MultipleType(settingsName, filePath);
        }

        public void ReadCustomSingleTypeJson<T>(string filePath, string settingsName) where T : CustomSettingsType
        {
            SessionSettings.ImportSettings_SingleTypeJSON<T>(settingsName, filePath);
        }


        public void ReadTaskDef<T>(string taskConfigFolder) where T : TaskDef
        {
            string taskDefFile = LocateFile.FindFileInExternalFolder(taskConfigFolder, "*" + TaskName + "*Task*");
            if (!string.IsNullOrEmpty(taskDefFile))
                SessionSettings.ImportSettings_MultipleType(TaskName + "_TaskSettings", taskDefFile);
            else
                Debug.Log("No taskdef file in config folder (this may not be a problem).");
        }

        public void ReadBlockDefs<T>(string taskConfigFolder) where T : BlockDef
        {

            string blockDefFile = LocateFile.FindFileInExternalFolder(taskConfigFolder, "*" + TaskName + "*BlockDef*");
            if (!string.IsNullOrEmpty(blockDefFile))
            {
                string blockDefText = File.ReadAllText(blockDefFile).Trim();
                if (blockDefText.Substring(0, 10) == "BlockDef[]") // stupid legacy case, shouldn't be included
                    SessionSettings.ImportSettings_MultipleType("blockDefs", blockDefFile);
                else if (blockDefFile.ToLower().Contains("tdf"))
                        SessionSettings.ImportSettings_SingleTypeArray<T>("blockDefs", blockDefFile);
                else
                    SessionSettings.ImportSettings_SingleTypeJSON<T[]>("blockDefs", blockDefFile);
                BlockDefs = (T[]) SessionSettings.Get("blockDefs");
            }
            else
                Debug.Log("No blockdef file in config folder (this may not be a problem).");
        }

        public void ReadTrialDefs<T>(string taskConfigFolder) where T : TrialDef
        {
            string trialDefFile = LocateFile.FindFileInExternalFolder(taskConfigFolder, "*" + TaskName + "*TrialDef*");
            if (!string.IsNullOrEmpty(trialDefFile))
            {
                if (trialDefFile.ToLower().Contains("tdf"))
                    SessionSettings.ImportSettings_SingleTypeArray<T>(TaskName + "_TrialDefs", trialDefFile);
                else
                    SessionSettings.ImportSettings_SingleTypeJSON<T[]>(TaskName + "_TrialDefs", trialDefFile);
                AllTrialDefs = (T[])SessionSettings.Get(TaskName + "_TrialDefs");
            }
            else
                Debug.Log("No trialdef file in config folder (this may not be a problem).");
        }

        public void ReadStimDefs<T>(string taskConfigFolder) where T : StimDef
        {
            string stimDefFile;
            string key = UseDefaultConfigs ? (TaskName + "_PrefabStims") : (TaskName + "_ExternalStimDefs");
            string defaultStimDefFile = taskConfigFolder + "/" + TaskName + (UseDefaultConfigs ? "_StimDeftdf" : "_StimDeftdf.txt");

            PrefabStims = new StimGroup("PrefabStims");
            ExternalStims = new StimGroup("ExternalStims");

            if (UseDefaultConfigs)
                stimDefFile = defaultStimDefFile; //prob works for editor, but not build
            else
                stimDefFile = LocateFile.FindFileInExternalFolder(taskConfigFolder, "*" + TaskName + "*StimDef*");

            if (!string.IsNullOrEmpty(stimDefFile))
            {
                if (stimDefFile.ToLower().Contains("tdf"))
                    SessionSettings.ImportSettings_SingleTypeArray<T>(key, stimDefFile);
                else
                    SessionSettings.ImportSettings_SingleTypeJSON<T[]>(key, stimDefFile);

                IEnumerable<StimDef> potentials = (T[])SessionSettings.Get(key);
                if (potentials == null || potentials.Count() < 1)
                    return;
                else
                {
                    if(UseDefaultConfigs)
                    {
                        PrefabStims = new StimGroup("PrefabStims", (T[])SessionSettings.Get(key));
                        foreach (var stim in PrefabStims.stimDefs)
                            PrefabStimPaths.Add(stim.PrefabPath + "/" + stim.FileName);
                    }
                    else
                        ExternalStims = new StimGroup("ExternalStims", (T[])SessionSettings.Get(key));                    
                }
            }
            else
                Debug.Log("No stimdef file in config folder (this may not be a problem).");
        }

        public void AddTaskStimDefsToTaskStimGroup<T>(StimGroup sg, IEnumerable<T> stimDefs) where T : StimDef
        {
            sg.AddStims(stimDefs);
        }

        public T TaskStimDefFromGameObject<T>(GameObject go, StimGroup sg = null) where T : StimDef, new()
        {
            StimDef sd = new T();
            sd.StimGameObject = go;
            sd.StimName = go.name;
            if (sg != null)
                sd.AddToStimGroup(sg);
            return (T)sd;
        }

        public T TaskStimDefFromPrefabPath<T>(string prefabPath, StimGroup sg = null) where T : StimDef, new()
        {
            StimDef sd = new T();
            sd.PrefabPath = PrefabPath;
            
            sd.StimName = Path.GetFileName(prefabPath);
            if (sg != null)
                sd.AddToStimGroup(sg);
            return (T)sd;
        } 

        public List<TrialDef> GetTrialDefsInBlock(int BlockNum, TrialDef[] trialDefs)
        {
            List<TrialDef> trialList = new List<TrialDef>();
            int currentBlockCount = -1;
            for (int iTrial = 0; (currentBlockCount <= BlockNum) & (iTrial < trialDefs.Length); iTrial++)
            {
                currentBlockCount = trialDefs[iTrial].BlockCount;
                if (currentBlockCount == BlockNum)
                    trialList.Add(trialDefs[iTrial]);
            }

            return trialList;
        }

        private void OnApplicationQuit()
        {
            if (BlockData != null)
            {
                BlockData.AppendData();
                BlockData.WriteData();
            }

            if (FrameData != null)
            {
                FrameData.AppendData();
                FrameData.WriteData();
            }
        }
        
        
        public T GetCurrentBlockDef<T>() where T : BlockDef
        {
            return (T)CurrentBlockDef;
        }

        public virtual void SetTaskSummaryString()
        {
            CurrentTaskSummaryString.Append($"\n<b>{ConfigName}</b>");
        }


    }


    public class TaskLevelTemplate_Methods
    {
        public bool CheckBlockEnd(string blockEndType, IEnumerable<float> runningTrialPerformance, float performanceThreshold = 1,
            int? minTrials = null, int? maxTrials = null)
        {
            // Takes in accuracy info from the current trial to determine whether to end the block
            List<float> rTrialPerformance = (List<float>)runningTrialPerformance;

            if (CheckTrialRange(rTrialPerformance.Count, minTrials, maxTrials) != null)
                return CheckTrialRange(rTrialPerformance.Count, minTrials, maxTrials).Value;

            switch (blockEndType)
            {
                case "CurrentTrialPerformance":
                    if (rTrialPerformance[rTrialPerformance.Count-1] <= performanceThreshold)
                    {
                        Debug.Log("Block ending due to trial performance below threshold.");
                        return true;
                    }
                    else
                        return false;
                default:
                    return false;
            }
        }
        public bool CheckBlockEnd(string blockEndType, IEnumerable<int> runningAcc, float accThreshold = 1, int windowSize = 1, int? minTrials = null, int? maxTrials = null)
        {
            //takes in accuracy information from the current block and determines if the block should end

            List<int> rAcc = (List<int>)runningAcc;
            float? immediateAvg; //this is the running average over the past n trials, where n = windowSize
            float? prevAvg; //this is the running average over the n trials PRIOR to the trials used for immediateAvg
                            //(allows easy comparison of changes between performance across two windows
            int? sumdif; //the simple sum of the number of different trial outcomes in the windows used to compute 
                         //immediateAvg and prevAvg
                         
            if (rAcc.Count >= windowSize)
            {
                immediateAvg = (float)rAcc.GetRange(rAcc.Count - windowSize, windowSize).Average();
            }
            else
                immediateAvg = null;

            if (rAcc.Count >= windowSize * 2)
            {
                prevAvg = (float)rAcc.GetRange(rAcc.Count - windowSize * 2, windowSize).Average();
                sumdif = rAcc.GetRange(rAcc.Count - windowSize * 2, windowSize).Sum() -
                         rAcc.GetRange(rAcc.Count - windowSize, windowSize).Sum();
            }
            else
            {
                prevAvg = null;
                sumdif = null;
            }

            if (CheckTrialRange(rAcc.Count, minTrials, maxTrials) != null)
                return CheckTrialRange(rAcc.Count, minTrials, maxTrials).Value;

            switch (blockEndType)
            {
                case "SimpleThreshold":
                    Debug.Log("checkingthreshold #################################################");
                    Debug.Log("Immediate: " + immediateAvg + ", threshold: " + accThreshold);
                    if (immediateAvg >= accThreshold)
                    {
                        Debug.Log("Block ending due to performance above threshold.");
                        return true;
                    }
                    else
                        return false;
                case "ThresholdAndPeak":
                    if (immediateAvg >= accThreshold && immediateAvg <= prevAvg)
                    {
                        Debug.Log("Block ending due to performance above threshold and no continued improvement.");
                        return true;
                    }
                    else
                        return false;
                case "ThresholdOrAsymptote":
                    if (sumdif != null && sumdif.Value <= 1)
                    {
                        Debug.Log("Block ending due to asymptotic performance.");
                        return true;
                    }
                    else if (immediateAvg >= accThreshold)
                    {
                        Debug.Log("Block ending due to performance above threshold.");
                        return true;
                    }
                    else
                        return false;
                default:
                    return false;
            }
        }

        private bool? CheckTrialRange(int nTrials, int? minTrials = null, int? maxTrials = null)
        {
            if (nTrials < minTrials)
                return false;
            if (nTrials >= maxTrials)
                return true;
            return null;
        }
    }

    public class TaskStims
    {
        public StimGroup AllTaskStims;
        public Dictionary<string, StimGroup> AllTaskStimGroups;
        public string TaskStimFolderPath;
        public string TaskStimExtension;

        public TaskStims()
        {
            AllTaskStims = new StimGroup("AllTaskStims");
            AllTaskStimGroups = new Dictionary<string, StimGroup>();
        }

        public void CreateStimDef(StimGroup sg)
        {
            StimDef sd = new StimDef(sg);
            CheckPathAndDuplicate(sd);
        }

        public void CreateStimDef(StimGroup sg, int[] dimVals)
        {
            StimDef sd = new StimDef(sg, dimVals);
            CheckPathAndDuplicate(sd);
        }

        public void CreateStimDef(StimGroup sg, GameObject obj)
        {
            StimDef sd = new StimDef(sg, obj);
            CheckPathAndDuplicate(sd);
        }

        public StimGroup CreateStimGroup(string groupName, State setActiveOnInit = null, State setInactiveOnTerm = null)
        {
            StimGroup sg = new StimGroup(groupName, setActiveOnInit, setInactiveOnTerm);
            AllTaskStimGroups.Add(groupName, sg);
            return sg;
        }

        public StimGroup CreateStimGroup(string groupName, IEnumerable<StimDef> stims, State setActiveOnInit = null, State setInactiveOnTerm = null)
        {
            StimGroup sg = new StimGroup(groupName, stims, setActiveOnInit, setInactiveOnTerm);
            AllTaskStimGroups.Add(groupName, sg);
            AddNewStims(sg.stimDefs);
            return sg;
        }

        public StimGroup CreateStimGroup(string groupName, IEnumerable<int[]> dimValGroup, string folderPath,
            IEnumerable<string[]> featureNames, string neutralPatternedColorName, Camera cam, float scale = 1, State setActiveOnInit = null, State setInactiveOnTerm = null)
        {
            StimGroup sg = new StimGroup(groupName, dimValGroup, folderPath, featureNames, neutralPatternedColorName, cam, scale, setActiveOnInit, setInactiveOnTerm);
            AllTaskStimGroups.Add(groupName, sg);
            AddNewStims(sg.stimDefs);
            return sg;
        }

        public StimGroup CreateStimGroup(string groupName, string TaskName, string stimDefFilePath, State setActiveOnInit = null, State setInactiveOnTerm = null)
        {
            StimGroup sg = new StimGroup(groupName, TaskName, stimDefFilePath, setActiveOnInit, setInactiveOnTerm);
            AllTaskStimGroups.Add(groupName, sg);
            AddNewStims(sg.stimDefs);
            return sg;
        }

        public StimGroup CreateStimGroup(string groupName, StimGroup sgOrig, IEnumerable<int> stimSubsetIndices, State setActiveOnInit = null, State setInactiveOnTerm = null)
        {
            StimGroup sg = new StimGroup(groupName, sgOrig, stimSubsetIndices, setActiveOnInit, setInactiveOnTerm);
            if (!AllTaskStimGroups.ContainsKey(groupName))
                AllTaskStimGroups.Add(groupName, sg);
            else
            {
                Debug.LogWarning("");
                AllTaskStimGroups[groupName] = sg;
            }
            AddNewStims(sg.stimDefs);
            return sg;
        }

        private StimDef CheckPathAndDuplicate(StimDef sd)
        {
            if (!string.IsNullOrEmpty(TaskStimFolderPath) && string.IsNullOrEmpty(sd.StimFolderPath))
                sd.StimFolderPath = TaskStimFolderPath;
            if (!string.IsNullOrEmpty(TaskStimExtension) && string.IsNullOrEmpty(sd.StimExtension))
                sd.StimExtension = TaskStimExtension;

            if (!AllTaskStims.stimDefs.Contains(sd))
                AllTaskStims.AddStims(sd);
            else
                Debug.LogWarning("Attempted to add duplicate StimDef " + sd.StimName + " to AllTaskStims, " +
                                 "duplication of object has been avoided.");

            return sd;
        }

        private void AddNewStims(List<StimDef> sds)
        {
            foreach (StimDef sd in sds)
            {
                if (!AllTaskStims.stimDefs.Contains(sd))
                {
                    CheckPathAndDuplicate(sd);
                }
            }
        }
    }

    public class CustomSettingsType
    {

    }


    public class TaskDef
    {
        public string TaskName;
        public string ExternalStimFolderPath;
        public string PrefabStimFolderPath;
        public string ExternalStimExtension;
        public List<string[]> FeatureNames;
        public string neutralPatternedColorName;
        public float? ExternalStimScale;
        public List<string[]> FeedbackControllers;
        public int? TotalTokensNum;
        public bool SerialPortActive, SyncBoxActive, EventCodesActive, RewardPulsesActive, SonicationActive;
        public string SelectionType;
        public Dictionary<string, string> CustomSettings;

    }

}
