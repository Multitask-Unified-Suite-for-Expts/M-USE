using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using USE_States;
using USE_Settings;
using USE_StimulusManagement;
using ConfigDynamicUI;
using USE_ExperimenterDisplay;
using USE_ExperimentTemplate_Classes;
using USE_ExperimentTemplate_Data;
using USE_ExperimentTemplate_Trial;
using USE_ExperimentTemplate_Block;

namespace USE_ExperimentTemplate_Task
{

    public abstract class ControlLevel_Task_Template : ControlLevel
    {
        public string ConfigName;
        public string TaskName;
        public string TaskProjectFolder;
        [HideInInspector] public int BlockCount;
        protected int NumBlocksInTask;
        public ControlLevel_Trial_Template TrialLevel;
        protected BlockData BlockData;
        protected FrameData FrameData;
        protected TrialData TrialData;

        [HideInInspector] public SessionDataControllers SessionDataControllers;

        [HideInInspector] public bool StoreData, SyncBoxActive, EventCodesActive, RewardPulsesActive, SonicationActive;
        [HideInInspector] public string SessionDataPath, TaskConfigPath, TaskDataPath, SubjectID, SessionID, FilePrefix, EyetrackerType, SelectionType;
        [HideInInspector] public LocateFile LocateFile;
        [HideInInspector] public StringBuilder BlockSummaryString;

        // public string TaskSceneName;
        public Camera TaskCam;
        public GameObject[] TaskCanvasses;

        //protected TrialDef[] AllTrialDefs;
        //protected TrialDef[] CurrentBlockTrialDefs;
        protected TaskDef TaskDef;
        protected BlockDef[] BlockDefs;
        protected BlockDef CurrentBlockDef;
        protected TrialDef[] AllTrialDefs;

        //
        // private StimGroup AllTaskStims;
        // public Dictionary<string, StimGroup> AllTaskStimGroups;
        public TaskStims TaskStims;
        [HideInInspector] public StimGroup PreloadedStims, PrefabStims, ExternalStims, RuntimeStims;
        public List<GameObject> PreloadedStimGameObjects;
        public List<string> PrefabStimPaths;
        protected ConfigUI configUI;
        protected ConfigVarStore ConfigUiVariables;
        [HideInInspector] public ExperimenterDisplayController ExperimenterDisplayController;

        private GameObject Controllers;

        [HideInInspector] public SerialPortThreaded SerialPortController;
        [HideInInspector] public SyncBoxController SyncBoxController;
        [HideInInspector] public EventCodeManager EventCodeManager;
        protected Dictionary<string, EventCode> TaskEventCodes;

        public Type TaskLevelType;
        protected Type TrialLevelType, TaskDefType, BlockDefType, TrialDefType, StimDefType;
        protected State SetupTask, RunBlock, BlockFeedback, FinishTask;
        protected bool BlockFbFinished;
        protected float BlockFbSimpleDuration;
        protected TaskLevelTemplate_Methods TaskLevel_Methods;

        protected int? MinTrials, MaxTrials;

        [HideInInspector] public RenderTexture DrawRenderTexture;

        public void OnGUI()
        {
            // GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), DrawRenderTexture);
        }

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
            TaskLevel_Methods = new TaskLevelTemplate_Methods();
            ReadSettingsFiles();
            ReadCustomSettingsFiles();
            FindStims();
            if (verifyOnly) return;

            SetupTask = new State("SetupTask");
            RunBlock = new State("RunBlock");
            BlockFeedback = new State("BlockFeedback");
            FinishTask = new State("FinishTask");
            RunBlock.AddChildLevel(TrialLevel);
            AddActiveStates(new List<State> { SetupTask, RunBlock, BlockFeedback, FinishTask });

            TrialLevel.TrialDefType = TrialDefType;
            TrialLevel.StimDefType = StimDefType;

            AddInitializationMethod(() =>
            {
                BlockCount = -1;
                BlockSummaryString = new StringBuilder();
                TaskCam.gameObject.SetActive(true);
                if (TaskCanvasses != null)
                    foreach (GameObject go in TaskCanvasses)
                        go.SetActive(true);
                //
                // GameObject experimenterInfoPrefab = Resources.Load<GameObject>("ExperimenterInfo");
                // GameObject experimenterInfo = Instantiate(experimenterInfoPrefab);
                // experimenterInfo.name = "ExperimenterInfo";
                //
                // GameObject cameraObj = new GameObject("DrawCamera");
                // cameraObj.transform.SetParent(experimenterInfo.transform);
                // Camera newCamera = cameraObj.AddComponent<Camera>();
                // newCamera.CopyFrom(Camera.main);
                // newCamera.cullingMask = 0;
                //
                // DrawRenderTexture = new RenderTexture(Screen.width, Screen.height, 24);
                // DrawRenderTexture.Create();
                // Camera.main.targetTexture = DrawRenderTexture;
                //
                // RawImage mainCameraCopy = GameObject.Find("MainCameraCopy").GetComponent<RawImage>();
                // mainCameraCopy.texture = DrawRenderTexture;
                //mainCameraCopy.rectTransform.sizeDelta = new Vector2(Screen.width / 2, Screen.height / 2);

                if (configUI == null)
                    configUI = FindObjectOfType<ConfigUI>();
                configUI.clear();
                if (ConfigUiVariables != null)
                    configUI.store = ConfigUiVariables;
                else
                    configUI.store = new ConfigVarStore();
                configUI.GenerateUI();

                Controllers.SetActive(true);
            });

            SetupTask.SpecifyTermination(() => true, RunBlock);


            RunBlock.AddUniversalInitializationMethod(() =>
            {
                BlockCount++;
                CurrentBlockDef = BlockDefs[BlockCount];
                TrialLevel.BlockCount = BlockCount;
                if (BlockCount == 0)
                    TrialLevel.TrialCount_InTask = -1;
                TrialLevel.TrialDefs = CurrentBlockDef.TrialDefs;
            });

            RunBlock.AddLateUpdateMethod(() =>
            {
                FrameData.AppendData();
                EventCodeManager.EventCodeLateUpdate();
            });
            RunBlock.SpecifyTermination(() => TrialLevel.Terminated, BlockFeedback);


            BlockFeedback.AddUpdateMethod(() =>
            {
                // BlockFbFinished = true;
                if (Time.time - BlockFeedback.TimingInfo.StartTimeAbsolute >= BlockFbSimpleDuration)
                    BlockFbFinished = true;
                else
                    BlockFbFinished = false;
            });
            BlockFeedback.AddLateUpdateMethod(() =>
            {
                FrameData.AppendData();
                EventCodeManager.EventCodeLateUpdate();
            });
            BlockFeedback.SpecifyTermination(() => BlockFbFinished && BlockCount < BlockDefs.Length - 1, RunBlock, () =>
            {
                BlockData.AppendData();
                BlockData.WriteData();
            });
            BlockFeedback.SpecifyTermination(() => BlockFbFinished && BlockCount == BlockDefs.Length - 1, FinishTask, () =>
            {
                BlockData.AppendData();
                BlockData.WriteData();
            });

            FinishTask.SpecifyTermination(() => true, () => null);

            AddDefaultTerminationMethod(() =>
            {
                SessionDataControllers.RemoveDataController("BlockData_" + TaskName);
                SessionDataControllers.RemoveDataController("TrialData_" + TaskName);
                SessionDataControllers.RemoveDataController("FrameData_" + TaskName);
                int sgNum = TaskStims.AllTaskStimGroups.Count;
                for (int iSg = 0; iSg < sgNum; iSg++)
                {
                    StimGroup[] taskSgs = new StimGroup[TaskStims.AllTaskStimGroups.Count];
                    TaskStims.AllTaskStimGroups.Values.CopyTo(taskSgs, 0);
                    StimGroup sg = taskSgs[0];
                    //WHY DOESN'T THIS WORK - it doesn't seem to matter that it doesn't
                    // string[] keys = new string[TaskStims.AllTaskStimGroups.Count];
                    // TaskStims.AllTaskStimGroups.Keys.CopyTo(keys, 0);
                    // TaskStims.AllTaskStimGroups.Remove(keys[0]);
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
                Controllers.SetActive(false);

            });

            //user-defined task control level 
            DefineControlLevel();



            //Setup data management
            TaskDataPath = SessionDataPath + Path.DirectorySeparatorChar + ConfigName;
            FilePrefix = FilePrefix + "_" + ConfigName;
            BlockData = SessionDataControllers.InstantiateBlockData(StoreData, ConfigName,
                TaskDataPath + Path.DirectorySeparatorChar + "BlockData");
            BlockData.taskLevel = this;
            BlockData.fileName = FilePrefix + "__BlockData";
            BlockData.InitDataController();

            TrialData = SessionDataControllers.InstantiateTrialData(StoreData, ConfigName,
                TaskDataPath + Path.DirectorySeparatorChar + "TrialData");
            TrialData.taskLevel = this;
            TrialData.trialLevel = TrialLevel;
            TrialLevel.TrialData = TrialData;
            TrialData.fileName = FilePrefix + "__TrialData";
            TrialData.InitDataController();

            FrameData = SessionDataControllers.InstantiateFrameData(StoreData, ConfigName,
                TaskDataPath + Path.DirectorySeparatorChar + "FrameData");
            FrameData.taskLevel = this;
            FrameData.trialLevel = TrialLevel;
            TrialLevel.FrameData = FrameData;
            FrameData.fileName = FilePrefix + "__FrameData_PreTrial";
            FrameData.InitDataController();

            BlockData.ManuallyDefine();
            FrameData.ManuallyDefine();

            if (EventCodesActive)
                FrameData.AddEventCodeColumns();

            BlockData.AddStateTimingData(this);
            BlockData.CreateFile();
            FrameData.CreateFile();

            //AddDataController(BlockData, StoreData, TaskDataPath + Path.DirectorySeparatorChar + "BlockData", FilePrefix + "_BlockData.txt");
            GameObject fbControllersPrefab = Resources.Load<GameObject>("FeedbackControllers");
            GameObject inputTrackersPrefab = Resources.Load<GameObject>("InputTrackers");
            Controllers = new GameObject("Controllers");
            GameObject fbControllers = Instantiate(fbControllersPrefab, Controllers.transform);
            GameObject inputTrackers = Instantiate(inputTrackersPrefab, Controllers.transform);

            List<string> fbControllersList = new List<string>();
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "FeedbackControllers"))
                fbControllersList = (List<string>)SessionSettings.Get(TaskName + "_TaskSettings", "FeedbackControllers");
            int totalTokensNum = 5;
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "TotalTokensNum"))
                totalTokensNum = (int)SessionSettings.Get(TaskName + "_TaskSettings", "TotalTokensNum");

            TrialLevel.AudioFBController = fbControllers.GetComponent<AudioFBController>();
            TrialLevel.HaloFBController = fbControllers.GetComponent<HaloFBController>();
            TrialLevel.TokenFBController = fbControllers.GetComponent<TokenFBController>();


            TrialLevel.SerialPortController = SerialPortController;
            TrialLevel.SyncBoxController = SyncBoxController;
            TrialLevel.EventCodeManager = EventCodeManager;
            if (TaskEventCodes != null)
                TrialLevel.TaskEventCodes = TaskEventCodes;

            bool audioInited = false;
            foreach (string fbController in fbControllersList)
            {
                switch (fbController)
                {
                    case "Audio":
                        if (!audioInited) TrialLevel.AudioFBController.Init(FrameData);
                        break;
                    case "Halo":
                        TrialLevel.HaloFBController.Init(FrameData);
                        break;
                    case "Token":
                        if (!audioInited) TrialLevel.AudioFBController.Init(FrameData);
                        TrialLevel.TokenFBController.Init(TrialData, FrameData, TrialLevel.AudioFBController);
                        TrialLevel.TokenFBController.SetTotalTokensNum(totalTokensNum);
                        break;
                    default:
                        Debug.LogWarning(fbController + " is not a valid feedback controller.");
                        break;
                }
            }

            TrialLevel.MouseTracker = inputTrackers.GetComponent<MouseTracker>();
            TrialLevel.MouseTracker.Init(FrameData, 0);
            TrialLevel.GazeTracker = inputTrackers.GetComponent<GazeTracker>();
            if (!string.IsNullOrEmpty(EyetrackerType) & EyetrackerType.ToLower() != "none" &
                EyetrackerType.ToLower() != "null")
            {
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

            TrialLevel.DefineTrialLevel();
        }


        private void ReadSettingsFiles()
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



            string configUIVariableFile = LocateFile.FindFileInFolder(TaskConfigPath, "*" + TaskName + "*ConfigUiDetails*");
            if (!string.IsNullOrEmpty(configUIVariableFile))
            {
                SessionSettings.ImportSettings_SingleTypeJSON<ConfigVarStore>(TaskName + "_ConfigUiDetails", configUIVariableFile);
                ConfigUiVariables = (ConfigVarStore)SessionSettings.Get(TaskName + "_ConfigUiDetails");
            }

            string eventCodeFile = LocateFile.FindFileInFolder(TaskConfigPath, "*" + TaskName + "*EventCodeConfig*");
            if (!string.IsNullOrEmpty(eventCodeFile))
            {
                SessionSettings.ImportSettings_SingleTypeJSON<Dictionary<string, EventCode>>(TaskName + "_EventCodeConfig", eventCodeFile);
                TaskEventCodes = (Dictionary<string, EventCode>)SessionSettings.Get(TaskName + "_EventCodeConfig");
                EventCodesActive = true;
            }

            //handling of block and trial defs so that each BlockDef contains a TrialDef[] array

            if (AllTrialDefs == null) //no trialDefs have been imported from settings files
            {
                if (BlockDefs == null)
                    Debug.LogError("Neither BlockDef nor TrialDef config files provided in " + TaskName +
                                   " folder, no trials generated as a result.");
                else
                {
                    for (int iBlock = 0; iBlock < BlockDefs.Length; iBlock++)
                    {
                        BlockDefs[iBlock].GenerateTrialDefsFromBlockDef();
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
                    for (int iBlock = 0; iBlock < BlockDefs.Length; iBlock++)
                    {
                        if (BlockDefs[iBlock] == null)
                            BlockDefs[iBlock] = new BlockDef();
                        BlockDefs[iBlock].BlockCount = iBlock;
                        BlockDefs[iBlock].TrialDefs = GetTrialDefsInBlock(iBlock, AllTrialDefs);
                    }
                }
                else //there is a blockDef file, its information may need to be added to TrialDefs
                {

                    //add trialDef[] for each block;
                    for (int iBlock = 0; iBlock < BlockDefs.Length; iBlock++)
                    {
                        BlockDefs[iBlock].TrialDefs = GetTrialDefsInBlock(iBlock + 1, AllTrialDefs);
                        BlockDefs[iBlock].AddToTrialDefsFromBlockDef();
                    }
                }
            }
        }

        public virtual void ReadCustomSettingsFiles()
        {
            
        }

        public virtual Dictionary<string, object> SummarizeTask() {
            return new Dictionary<string, object>();
        }

        public void FindStims()
        {
            MethodInfo addTaskStimDefsToTaskStimGroup = GetType().GetMethod(nameof(this.AddTaskStimDefsToTaskStimGroup))
                .MakeGenericMethod(new Type[] { StimDefType });

            //PreloadedStims = GameObjects in scene prior to build
            PreloadedStims = new StimGroup("PreloadedStims");
            TaskStims.AllTaskStimGroups.Add("PreloadedStims", PreloadedStims);
            PrefabStims = new StimGroup("PrefabStims");
            TaskStims.AllTaskStimGroups.Add("PrefabStims", PrefabStims);
            //ExternalStims is already created in ReadStimDefs (not ideal as hard to follow)
            TaskStims.AllTaskStimGroups.Add("ExternalStims", ExternalStims);
            RuntimeStims = new StimGroup("RuntimeStims");
            TaskStims.AllTaskStimGroups.Add("RuntimeStims", RuntimeStims);

            DefinePreloadedStims();
            DefinePrefabStims();
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

            if (PrefabStimPaths != null && PrefabStimPaths.Count > 0)
            {
                //Prefabs with explicit path given
                foreach (string path in PrefabStimPaths)
                {
                    taskStimDefFromPrefabPath.Invoke(this, new object[] { path, PreloadedStims });
                }

            }
            else
            {
                //Prefabs in Prefabs/TaskFolder or TaskFolder/Prefabs
                List<string> prefabPaths = new List<string>();
                string[] prefabFolders =
                    {"Assets/Resources/Prefabs/" + TaskName, "Assets/_USE_Tasks/" + TaskName + "/Prefabs"};
                foreach (string path in prefabFolders)
                {
                    if (Directory.Exists(path))
                        prefabPaths.AddRange(Directory.GetFiles(path).ToList());
                }
                // string[] guids = AssetDatabase.FindAssets("t: GameObject", prefabFolders);
                foreach (string pp in prefabPaths)
                {
                    taskStimDefFromPrefabPath.Invoke(this, new object[] { pp, PreloadedStims });
                }
            }

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
                if (!string.IsNullOrEmpty(sd.StimExtension) && !sd.ExternalFilePath.EndsWith(sd.StimExtension))
                {
                    if (!sd.StimExtension.StartsWith("."))
                        sd.ExternalFilePath = sd.ExternalFilePath + "." + sd.StimExtension;
                    else
                        sd.ExternalFilePath = sd.ExternalFilePath + sd.StimExtension;
                }


                //we will only use StimFolderPath if ExternalFilePath doesn't already contain it
                if (!string.IsNullOrEmpty(sd.StimFolderPath) && !sd.ExternalFilePath.StartsWith(sd.StimFolderPath))
                {

                    //this checking needs to be done during task setup - check each stim exists at start of session instead
                    //of at start of each trial
                    List<string> filenames = RecursiveFileFinder.FindFile(sd.StimFolderPath, sd.ExternalFilePath, sd.StimExtension);

                    if (filenames.Count > 1)
                    {
                        string firstFilename = Path.GetFileName(filenames[0]);
                        for (int iFile = filenames.Count - 1; iFile > 0; iFile--)
                        {
                            if (Path.GetFileName(filenames[iFile]) == firstFilename)
                            {
                                Debug.LogWarning("During task setup for " + TaskName + " attempted to find stimulus " +
                                                 sd.ExternalFilePath + " in folder " + sd.StimFolderPath +
                                                 ", but files with this name are found at both " + firstFilename +
                                                 " and "
                                                 + filenames[iFile] + ".  Only the first will be used.");
                                filenames.RemoveAt(iFile);
                            }
                        }
                    }
                    

                    if (filenames.Count == 1)
                        sd.ExternalFilePath = filenames[0];
                    else if (filenames.Count == 0)
                        Debug.LogError("During task setup for " + TaskName + " attempted to find stimulus " +
                                       sd.ExternalFilePath + " in folder " +
                                       sd.StimFolderPath +
                                       " but no file matching this pattern was found in this folder or subdirectories.");
                    else
                    {
                        Debug.LogError("During task setup for " + TaskName + " attempted to find stimulus " +
                                       sd.ExternalFilePath + " in folder " +
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

        public void ReadTaskDef<T>(string taskConfigFolder) where T : TaskDef
        {
            string taskDefFile = LocateFile.FindFileInFolder(taskConfigFolder, "*" + TaskName + "*Task*");
            if (!string.IsNullOrEmpty(taskDefFile))
            {
                SessionSettings.ImportSettings_MultipleType(TaskName + "_TaskSettings", taskDefFile);
                // TaskDef = (T) SessionSettings.Get(TaskName + "_TaskSettings");
            }
            else
            {
                Debug.Log("No taskdef file in config folder (this may not be a problem).");
            }
        }

        public void ReadBlockDefs<T>(string taskConfigFolder) where T : BlockDef
        {

            string blockDefFile = LocateFile.FindFileInFolder(taskConfigFolder, "*" + TaskName + "*BlockDef*");
            if (!string.IsNullOrEmpty(blockDefFile))
            {
                string blockDefText = File.ReadAllText(blockDefFile).Trim();
                if (blockDefText.Substring(0, 10) == "BlockDef[]") // stupid legacy case, shouldn't be included
                    SessionSettings.ImportSettings_MultipleType("blockDefs", blockDefFile);
                else if (blockDefFile.ToLower().Contains("tdf"))
                    SessionSettings.ImportSettings_SingleTypeArray<T>("blockDefs", blockDefFile);
                else
                    SessionSettings.ImportSettings_SingleTypeJSON<T[]>("blockDefs", blockDefFile);
                BlockDefs = (T[])SessionSettings.Get("blockDefs");
            }
            else
                Debug.Log("No blockdef file in config folder (this may not be a problem).");
        }

        public void ReadTrialDefs<T>(string taskConfigFolder) where T : TrialDef
        {
            //string taskConfigFolder = configFileFolder + Path.DirectorySeparatorChar + TaskName;
            string trialDefFile = LocateFile.FindFileInFolder(taskConfigFolder, "*" + TaskName + "*TrialDef*");
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
            //string taskConfigFolder = configFileFolder + Path.DirectorySeparatorChar + TaskName;
            string stimDefFile = LocateFile.FindFileInFolder(taskConfigFolder, "*" + TaskName + "*StimDef*");
            if (!string.IsNullOrEmpty(stimDefFile))
            {
                if (stimDefFile.ToLower().Contains("tdf"))
                    SessionSettings.ImportSettings_SingleTypeArray<T>(TaskName + "_ExternalStimDefs", stimDefFile);
                else
                    SessionSettings.ImportSettings_SingleTypeJSON<T[]>(TaskName + "_ExternalStimDefs", stimDefFile);

                ExternalStims = new StimGroup("ExternalStims", (T[])SessionSettings.Get(TaskName + "_ExternalStimDefs"));
                // TaskStims.CreateStimGroup("ExternalStims", (T[]) SessionSettings.Get(TaskName + "_Stims"));
            }
            else
            {
                ExternalStims = new StimGroup("ExternalStims");
                Debug.Log("No stimdef file in config folder (this may not be a problem).");
            }
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
            sd.PrefabPath = prefabPath;
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

    }


    public class TaskLevelTemplate_Methods
    {
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
                immediateAvg = (float)rAcc.GetRange(rAcc.Count - windowSize, windowSize).Average();
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
    }
}