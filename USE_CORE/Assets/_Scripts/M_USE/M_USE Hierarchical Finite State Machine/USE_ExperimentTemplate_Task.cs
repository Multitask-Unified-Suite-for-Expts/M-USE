using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using USE_States;
using USE_Settings;
using USE_StimulusManagement;
using ConfigDynamicUI;
using USE_ExperimentTemplate_Classes;
using USE_ExperimentTemplate_Data;
using USE_ExperimentTemplate_Trial;
using System.Collections;
using USE_Def_Namespace;
using System.Collections.Specialized;
using TMPro;


namespace USE_ExperimentTemplate_Task
{
    public abstract class ControlLevel_Task_Template : ControlLevel
    {
        public string PrefabPath;

        public string ConfigFolderName;
        public string TaskName;
        public string TaskProjectFolder;
        [HideInInspector] public int BlockCount;
        protected int NumBlocksInTask;
        public ControlLevel_Trial_Template TrialLevel;
        public BlockData BlockData;
        public FrameData FrameData;
        public TrialData TrialData;
        [HideInInspector] public string TaskConfigPath, TaskDataPath;
        [HideInInspector] public StringBuilder BlockSummaryString, CurrentTaskSummaryString, PreviousBlockSummaryString;
        private int TaskStringsAdded = 0;
        public Camera TaskCam;
        public Canvas[] TaskCanvasses;
        public GameObject StimCanvas_2D;
        public TrialDef[] AllTrialDefs;
        protected TrialDef[] CurrentBlockTrialDefs;
        public TaskDef TaskDef;
        public BlockDef[] BlockDefs;
        private BlockDef CurrentBlockDef;
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
        public ConfigVarStore ConfigUiVariables;
        public Dictionary<string, EventCode> CustomTaskEventCodes;

        public Type TaskLevelType;
        public Type TrialLevelType, TaskDefType, BlockDefType, TrialDefType, StimDefType;
        protected State VerifyTask, SetupTask, RunBlock, BlockFeedback, FinishTask;
        protected bool BlockFbFinished;
        protected float BlockFbSimpleDuration;
        protected TaskLevelTemplate_Methods TaskLevel_Methods;

        protected int? MinTrials, MaxTrials;
        [HideInInspector] public RenderTexture DrawRenderTexture;
        [HideInInspector] public event EventHandler TaskSkyboxSet_Event;
        [HideInInspector] public bool TaskLevelDefined;

        [HideInInspector] public List<CustomSettings> customSettings;

        public bool TrialAndBlockDefsHandled;
        public bool StimsHandled;

        //Passed by sessionLevel
        [HideInInspector] public GameObject BlockResultsPrefab;
        [HideInInspector] public GameObject BlockResults_GridElementPrefab;
        [HideInInspector] public AudioClip BlockResults_AudioClip;
        [HideInInspector] public GameObject BlockResultsGO;

        private bool ContinueButtonClicked;

        //Passed by session level
        public ImportSettings_Level importSettings_Level;
        public VerifyTask_Level verifyTask_Level;


        public virtual void SpecifyTypes()
        {
            TaskLevelType = USE_Tasks_CustomTypes.CustomTaskDictionary[TaskName].TaskLevelType;
            TrialLevelType = USE_Tasks_CustomTypes.CustomTaskDictionary[TaskName].TrialLevelType;
            TaskDefType = USE_Tasks_CustomTypes.CustomTaskDictionary[TaskName].TaskDefType;
            BlockDefType = USE_Tasks_CustomTypes.CustomTaskDictionary[TaskName].BlockDefType;
            TrialDefType = USE_Tasks_CustomTypes.CustomTaskDictionary[TaskName].TrialDefType;
            StimDefType = USE_Tasks_CustomTypes.CustomTaskDictionary[TaskName].StimDefType;
        }

        public T GetTaskDef<T>() where T: TaskDef
        {
            return (T)TaskDef;
        }

        public void DefineTaskLevel()
        {
            TaskLevelDefined = false;


            TaskLevel_Methods = new TaskLevelTemplate_Methods();
            
            RunBlock = new State("RunBlock");
            BlockFeedback = new State("BlockFeedback");
            FinishTask = new State("FinishTask");
            RunBlock.AddChildLevel(TrialLevel);
            AddActiveStates(new List<State> { RunBlock, BlockFeedback, FinishTask });

            TrialLevel.TrialDefType = TrialDefType;
            TrialLevel.StimDefType = StimDefType;
            TrialLevel.TaskLevel = this;
        
            Add_ControlLevel_InitializationMethod(() =>
            {
                TaskCam.gameObject.SetActive(true);

                if (TaskCanvasses != null)
                    foreach (Canvas canvas in TaskCanvasses)
                        canvas.gameObject.SetActive(true);

                BlockCount = -1;
                BlockSummaryString = new StringBuilder();
                PreviousBlockSummaryString = new StringBuilder();
                CurrentTaskSummaryString = new StringBuilder();

                if (!SessionValues.WebBuild)
                {
                    if (configUI == null)
                        configUI = FindObjectOfType<ConfigUI>();
                    configUI.clear();
                    if (ConfigUiVariables != null)
                        configUI.store = ConfigUiVariables;
                    else
                        configUI.store = new ConfigVarStore();
                    configUI.GenerateUI();

                    if (TaskName == "GazeCalibration")
                    {
                        BlockDef bd = new BlockDef();
                        BlockDefs = new BlockDef[] { bd };
                        bd.GenerateTrialDefsFromBlockDef();
                    }
                }

                SessionValues.InputManager.SetActive(true);

                if (SessionValues.SessionDef.IsHuman)
                {
                    Canvas taskCanvas = GameObject.Find(TaskName + "_Canvas").GetComponent<Canvas>();
                    SessionValues.HumanStartPanel.SetupDataAndCodes(FrameData, SessionValues.EventCodeManager, SessionValues.EventCodeManager.SessionEventCodes);
                    SessionValues.HumanStartPanel.SetTaskLevel(this);
                    SessionValues.HumanStartPanel.CreateHumanStartPanel(taskCanvas, TaskName);
                }
            });

            //RunBlock State-----------------------------------------------------------------------------------------------------
            RunBlock.AddUniversalInitializationMethod(() =>
            {
                SessionValues.EventCodeManager.SendCodeImmediate("RunBlockStarts");

                BlockCount++;
                CurrentBlockDef = BlockDefs[BlockCount];
                TrialLevel.BlockCount = BlockCount;
                if (BlockCount == 0)
                    TrialLevel.TrialCount_InTask = -1;
                TrialLevel.TrialDefs = CurrentBlockDef.TrialDefs;
                
                TrialLevel.TaskStims = TaskStims;
                TrialLevel.PreloadedStims = PreloadedStims;
                TrialLevel.PrefabStims = PrefabStims;
                TrialLevel.ExternalStims = ExternalStims;
                TrialLevel.RuntimeStims = RuntimeStims;
                TrialLevel.ConfigUiVariables = ConfigUiVariables;
            });

            //Hotkeys for WebGL build so we can end task and go to next block
            if (SessionValues.WebBuild)
            {
                RunBlock.AddUpdateMethod(() =>
                {
                    if (TrialLevel != null)
                    {
                        if (InputBroker.GetKeyUp(KeyCode.P)) //Pause Game:
                        {
                            Time.timeScale = Time.timeScale == 1 ? 0 : 1;
                        }

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
                            TrialLevel.TokenFBController.animationPhase = TokenFBController.AnimationPhase.None;

                            Time.timeScale = 1;//if paused, unpause before ending block

                            if (SessionValues.HumanStartPanel.HumanStartPanelGO != null)
                                SessionValues.HumanStartPanel.HumanStartPanelGO.SetActive(false);

                            if (TrialLevel.AudioFBController.IsPlaying())
                                TrialLevel.AudioFBController.audioSource.Stop();
                            TrialLevel.AbortCode = 3;
                            TrialLevel.ForceBlockEnd = true;
                            TrialLevel.SpecifyCurrentState(TrialLevel.GetStateFromName("FinishTrial"));
                        }
                    }
                });
            }

            RunBlock.AddLateUpdateMethod(() =>
            {
                StartCoroutine(FrameData.AppendDataToBuffer());
                SessionValues.EventCodeManager.EventCodeLateUpdate();
            });
            RunBlock.SpecifyTermination(() => TrialLevel.Terminated, BlockFeedback);


            //BlockFeedback State-----------------------------------------------------------------------------------------------------
            float blockFeedbackDuration = 0; //Using this variable to control the fact that on web build they may use default configs which have value of 8s, but then they may switch to NPH verrsion, which would just show them blank blockresults screen for 8s. 
            BlockFeedback.AddUniversalInitializationMethod(() =>
            {
                blockFeedbackDuration = SessionValues.SessionDef.BlockResultsDuration;
                if (SessionValues.SessionDef.IsHuman)
                {
                    OrderedDictionary taskBlockResults = GetBlockResultsData();
                    if (taskBlockResults != null && taskBlockResults.Count > 0)
                        DisplayBlockResults(taskBlockResults);
                }
                else
                    blockFeedbackDuration = 0;

                SessionValues.EventCodeManager.SendCodeImmediate("BlockFeedbackStarts");
            });
            BlockFeedback.AddUpdateMethod(() =>
            {
                if (ContinueButtonClicked || (Time.time - BlockFeedback.TimingInfo.StartTimeAbsolute >= blockFeedbackDuration))
                    BlockFbFinished = true;
                else
                    BlockFbFinished = false;
            });
            BlockFeedback.AddLateUpdateMethod(() =>
            {
                if (SessionValues.StoreData)
                    StartCoroutine(FrameData.AppendDataToBuffer());

                if (SessionValues.SessionDef.EventCodesActive)
                    SessionValues.EventCodeManager.EventCodeLateUpdate();
            });
            BlockFeedback.SpecifyTermination(() => BlockFbFinished && BlockCount < BlockDefs.Length - 1, RunBlock);
            BlockFeedback.SpecifyTermination(() => BlockFbFinished && BlockCount == BlockDefs.Length - 1, FinishTask);
            BlockFeedback.AddDefaultTerminationMethod(() =>
            {
                if (ContinueButtonClicked)
                    ContinueButtonClicked = false;

                if (SessionValues.SessionDef.IsHuman && BlockResultsGO != null)
                    BlockResultsGO.SetActive(false);

                if (SessionValues.StoreData)
                {
                    StartCoroutine(BlockData.AppendDataToBuffer());
                    StartCoroutine(BlockData.AppendDataToFile());
                }
            });

            //FinishTask State-----------------------------------------------------------------------------------------------------
            FinishTask.AddDefaultInitializationMethod(() =>
            {
                if (TrialLevel.TokenFBController.enabled)
                    TrialLevel.TokenFBController.enabled = false;

                if (TrialLevel.ForceBlockEnd && SessionValues.StoreData) //If they used end task hotkey, still write the block data!
                {
                    StartCoroutine(BlockData.AppendDataToBuffer());
                    StartCoroutine(BlockData.AppendDataToFile());
                }

                if (SessionValues.SessionDef.EventCodesActive)
                    SessionValues.EventCodeManager.SendCodeImmediate("FinishTaskStarts");

                //Clear trialsummarystring and Blocksummarystring at end of task:
                if (TrialLevel.TrialSummaryString != null && BlockSummaryString != null)
                {
                    TrialLevel.TrialSummaryString = "";
                    BlockSummaryString.Clear();
                    BlockSummaryString.AppendLine("");
                }

                ClearActiveTaskHandlers();
            });

            FinishTask.SpecifyTermination(() => true, () => null);

            AddDefaultControlLevelTerminationMethod(() =>
            {
                if (SessionValues.SessionDataControllers != null)
                {
                    SessionValues.SessionDataControllers.RemoveDataController("BlockData_" + TaskName);
                    SessionValues.SessionDataControllers.RemoveDataController("TrialData_" + TaskName);
                    SessionValues.SessionDataControllers.RemoveDataController("FrameData_" + TaskName);
                    if (SessionValues.SessionDef.EyeTrackerActive)
                    {
                        SessionValues.SessionDataControllers.RemoveDataController("BlockData_GazeCalibration");
                        SessionValues.SessionDataControllers.RemoveDataController("FrameData_GazeCalibration");
                        SessionValues.SessionDataControllers.RemoveDataController("TrialData_GazeCalibration");
                    }
                }

                if (TaskStims != null)
                {
                    int sgNum = TaskStims.AllTaskStimGroups.Count;
                    for (int iSg = 0; iSg < sgNum; iSg++)
                    {
                        StimGroup[] taskSgs = new StimGroup[TaskStims.AllTaskStimGroups.Count];
                        TaskStims.AllTaskStimGroups.Values.CopyTo(taskSgs, 0);
                        StimGroup sg = taskSgs[0];

                        if(sg.stimDefs != null)
                        {
                            while (sg.stimDefs.Count > 0)
                            {
                                sg.stimDefs[0].DestroyStimGameObject();
                                sg.stimDefs.RemoveAt(0);
                            }
                            sg.DestroyStimGroup();
                        }

                    }
                    TaskStims.AllTaskStims.DestroyStimGroup();

                }

                TaskCam.gameObject.SetActive(false);

                if (TaskCanvasses != null)
                    foreach (Canvas canvas in TaskCanvasses)
                        canvas.gameObject.SetActive(false);

                Destroy(GameObject.Find("FeedbackControllers"));

                if (!SessionValues.WebBuild)
                {
                    foreach (Transform child in GameObject.Find("MainCameraCopy").transform)
                        Destroy(child.gameObject);
                }
            });
            
            TaskLevelDefined = true;
        }


        public void SetSkyBox(string contextName)
        {
            string contextFilePath = "";
            if (SessionValues.UsingDefaultConfigs)
                contextFilePath = $"{SessionValues.SessionDef.ContextExternalFilePath}/{contextName}";
            else if (SessionValues.UsingServerConfigs)
                contextFilePath = $"{SessionValues.SessionDef.ContextExternalFilePath}/{contextName}.png";
            else if (SessionValues.UsingLocalConfigs)
                contextFilePath = TrialLevel.GetContextNestedFilePath(SessionValues.SessionDef.ContextExternalFilePath, contextName, "LinearDark");

            StartCoroutine(HandleSkybox(contextFilePath));
        }


        private void HandleContinueButtonClick()
        {
            ContinueButtonClicked = true;
        }

        private void DisplayBlockResults(OrderedDictionary taskBlockResults)
        {
            GameObject taskCanvas = GameObject.Find(TaskName + "_Canvas");
            if (taskCanvas != null)
            {
                BlockResultsGO = Instantiate(SessionValues.BlockResultsPrefab);
                BlockResultsGO.name = "BlockResults";
                BlockResultsGO.transform.SetParent(taskCanvas.transform);
                BlockResultsGO.transform.localScale = Vector3.one;
                BlockResultsGO.transform.localPosition = Vector3.zero;

                GameObject continueButtonGO = BlockResultsGO.transform.Find("ContinueButton").gameObject;
                if (continueButtonGO != null)
                    continueButtonGO.AddComponent<Button>().onClick.AddListener(HandleContinueButtonClick);
                    
                Transform gridParent = BlockResultsGO.transform.Find("Grid");

                AudioSource blockResults_AudioSource = gameObject.AddComponent<AudioSource>();
                blockResults_AudioSource.clip = BlockResults_AudioClip;
                blockResults_AudioSource.volume = .9f;
                blockResults_AudioSource.Play();

                int count = 0;
                foreach (DictionaryEntry entry in taskBlockResults)
                {                        
                    blockResults_AudioSource.Play();

                    GameObject gridItem = Instantiate(SessionValues.BlockResults_GridElementPrefab, gridParent);
                    gridItem.name = "GridElement" + count;
                    TextMeshProUGUI itemText = gridItem.GetComponentInChildren<TextMeshProUGUI>();
                    itemText.text = $"{entry.Key}: <b>{entry.Value}</b>";
                    count++;
                }
            }
            else
                Debug.Log("Didn't find a Task Canvas named: " + TaskName + "_Canvas");
        }

        public void ClearActiveTaskHandlers()
        {
            if (SessionValues.SelectionTracker.TaskHandlerNames.Count > 0)
            {
                List<string> toRemove = new List<string>();

                foreach (string handlerName in SessionValues.SelectionTracker.TaskHandlerNames)
                {
                    if (SessionValues.SelectionTracker.ActiveSelectionHandlers.ContainsKey(handlerName))
                    {
                        SessionValues.SelectionTracker.ActiveSelectionHandlers.Remove(handlerName);
                        toRemove.Add(handlerName);
                    }
                }

                foreach (string handlerName in toRemove)
                    SessionValues.SelectionTracker.TaskHandlerNames.Remove(handlerName);
            }
        }

        
        public virtual OrderedDictionary GetTaskSummaryData()
        {
            return new OrderedDictionary();
        }

        public virtual OrderedDictionary GetBlockResultsData()
        {
            return new OrderedDictionary();
        }

        public virtual List<CustomSettings> DefineCustomSettings()
        {
            return null;
        }
        
        
        //handling of block and trial defs so that each BlockDef contains a TrialDef[] array
        public void HandleTrialAndBlockDefs(bool verifyOnly)
        {   
            if (AllTrialDefs == null || AllTrialDefs.Count() == 0) //no trialDefs have been imported from settings files
            {
                if (BlockDefs == null)
                    Debug.LogError("Neither BlockDef nor TrialDef config files provided in " + TaskName + " folder, no trials generated as a result.");
                else
                {
                    // if (!verifyOnly)
                    // {
                        for (int iBlock = 0; iBlock < BlockDefs.Length; iBlock++)
                        {
                            BlockDefs[iBlock].RandomNumGenerator = new System.Random((int)DateTime.Now.Ticks + iBlock);
                            BlockDefs[iBlock].GenerateTrialDefsFromBlockDef();
                        }
                    // }
                }
            }
            else //trialDefs imported from settings files
            {
                if (BlockDefs == null) //no blockDef file, trialdefs should be complete
                {
                    Debug.Log("TrialDef config file provided without BlockDef config file in " + TaskName + " folder, BlockDefs will be generated with default values for all fields from TrialDefs.");
                    if (AllTrialDefs[AllTrialDefs.Length - 1].BlockCount != 0)
                    {
                        if (AllTrialDefs[0].BlockCount == 0)
                            BlockDefs = new BlockDef[AllTrialDefs[AllTrialDefs.Length - 1].BlockCount];
                        else if (AllTrialDefs[0].BlockCount == 1)
                            BlockDefs = new BlockDef[AllTrialDefs[AllTrialDefs.Length - 1].BlockCount - 1];
                        else
                            Debug.LogError("TrialDef config file in " + TaskName + " folder includes BlockCounts that are neither 0- nor 1-indexed.");
                    }
                    else
                    {
                        Debug.Log("TrialDef config file in " + TaskName + " folder only generates a single block (this is not a problem if you do not intend to use a block structure in your experiment).");
                        BlockDefs = new BlockDef[1];
                    }

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
                    for (int iBlock = 0; iBlock < BlockDefs.Length; iBlock++)
                    {
                        BlockDefs[iBlock].TrialDefs = GetTrialDefsInBlock(iBlock + 1, AllTrialDefs);
                        BlockDefs[iBlock].RandomNumGenerator = new System.Random((int) DateTime.Now.Ticks + iBlock);
                        BlockDefs[iBlock].AddToTrialDefsFromBlockDef();
                    }
                }
            }
            TrialAndBlockDefsHandled = true;
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

            TaskStims.AllTaskStimGroups.Add("PreloadedStims", PreloadedStims);
            TaskStims.AllTaskStimGroups.Add("PrefabStims", PrefabStims);
            TaskStims.AllTaskStimGroups.Add("ExternalStims", ExternalStims);
            TaskStims.AllTaskStimGroups.Add("RuntimeStims", RuntimeStims);

            DefinePreloadedStims();

            if (PrefabStims.stimDefs != null && PrefabStims.stimDefs.Count > 0)
                DefinePrefabStims();

            if (ExternalStims.stimDefs != null && ExternalStims.stimDefs.Count > 0)
                DefineExternalStims();

            StimsHandled = true;
        }

        protected virtual void DefinePreloadedStims()
        {
            MethodInfo taskStimDefFromGameObject = GetType().GetMethod(nameof(TaskStimDefFromGameObject))
                .MakeGenericMethod((new Type[] { StimDefType }));
            if (PreloadedStimGameObjects != null && PreloadedStimGameObjects.Count > 0)
            {
                foreach (GameObject go in PreloadedStimGameObjects)
                    taskStimDefFromGameObject.Invoke(this, new object[] { go, PreloadedStims });
                
                PreloadedStims.AddStims(PreloadedStimGameObjects);
            }
        }

        protected virtual void DefinePrefabStims()
        {
            MethodInfo taskStimDefFromPrefabPath = GetType().GetMethod(nameof(TaskStimDefFromPrefabPath))
                .MakeGenericMethod((new Type[] { StimDefType }));

            GameObject taskCanvasGO = GameObject.Find(TaskName + "_Canvas");
            foreach (StimDef sd in PrefabStims.stimDefs)
            {
                sd.StimScale = TaskDef.ExternalStimScale;
                sd.CanvasGameObject = taskCanvasGO; //adding task canvas in case default stim end up being 2D
            }

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
            GameObject taskCanvasGO = GameObject.Find(TaskName + "_Canvas");

            foreach (StimDef sd in ExternalStims.stimDefs)
            {
                sd.StimFolderPath = TaskDef.ExternalStimFolderPath;
                sd.StimExtension = TaskDef.ExternalStimExtension;
                sd.StimScale = TaskDef.ExternalStimScale;
                sd.CanvasGameObject = taskCanvasGO;

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



        public void ReadCustomSingleTypeArray<T>(string filePath, string settingsName, string serverFileString = null) where T : CustomSettings
        {
            SessionSettings.ImportSettings_SingleTypeArray<T>(settingsName, filePath, serverFileString);
        }

        public void ReadCustomMultipleTypes<T>(string filePath, string settingsName, string serverFileString = null) where T : CustomSettings
        {
            SessionSettings.ImportSettings_MultipleType(settingsName, filePath, serverFileString);
        }

        public void ReadCustomSingleTypeJson<T>(string filePath, string settingsName, string serverFileString = null) where T : CustomSettings
        {
            SessionSettings.ImportSettings_SingleTypeJSON<T>(settingsName, filePath, serverFileString);
        }




        //DELETE DUPLICATE LATER
        public bool FileStringContainsTabs(string fileContent)
        {
            string[] lines = fileContent.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                if (line.Contains('\t'))
                {
                    int tabCount = line.Split('\t').Length; //check if all lines have same number of tabs 
                    for (int j = i + 1; j < lines.Length; j++)
                    {
                        string nextLine = lines[j].Trim();
                        if (!string.IsNullOrEmpty(nextLine) && nextLine.Split('\t').Length != tabCount)
                            return false; //Inconsistent number of tab-separated values
                    }
                    return true;
                }
            }
            return false;
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
                StartCoroutine(BlockData.AppendDataToBuffer());
                StartCoroutine(BlockData.AppendDataToFile());
            }

            if (FrameData != null)
            {
                StartCoroutine(FrameData.AppendDataToBuffer());
                StartCoroutine(FrameData.AppendDataToFile());
            }

            if (SessionValues.GazeData != null)
            {
                StartCoroutine(SessionValues.GazeData.AppendDataToFile());
            }
        }
        
        
        public T GetCurrentBlockDef<T>() where T : BlockDef
        {
            return (T)CurrentBlockDef;
        }

        public virtual void SetTaskSummaryString()
        {
            CurrentTaskSummaryString.Append($"\n<b>{ConfigFolderName}</b>");
        }


    }


    public class TaskLevelTemplate_Methods
    {
        public bool CheckBlockEnd(string blockEndType, IEnumerable<float?> runningTrialPerformance, float performanceThreshold = 1,
            int? minTrials = null, int? maxTrials = null)
        {
            // Takes in accuracy info from the current trial to determine whether to end the block
            List<float?> rTrialPerformance = (List<float?>)runningTrialPerformance;
            if (CheckTrialRange(rTrialPerformance.Count, minTrials, maxTrials) != null)
                return CheckTrialRange(rTrialPerformance.Count, minTrials, maxTrials).Value;

            // Add -1 to the running trial performance to indicate an aborted/incomplete trial

            switch (blockEndType)
            {
                case "CurrentTrialPerformance":
                    Debug.Log("####CHECKING BLOCK END, rTrialPerformance.Count: " + rTrialPerformance.Count + ", (rTrialPerformance[rTrialPerformance.Count - 1]: " + (rTrialPerformance[rTrialPerformance.Count - 1]));

                    if (rTrialPerformance[rTrialPerformance.Count - 1] != null && rTrialPerformance[rTrialPerformance.Count-1] <= performanceThreshold)
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

    public class CustomSettings
    {
        public string SearchString;
        public Type SettingsType;
        public string SettingsParsingStyle;
        public object ParsedResult;
        public Action<object> UpdateAction;
        
        //public CustomSettings(string searchString, Type settingsType, string settingsParsingStyle, Action<object> updateAction)
        public CustomSettings(string searchString, Type settingsType, string settingsParsingStyle, object parsedResult)
        {
            SearchString = searchString;
            SettingsType = settingsType;
            SettingsParsingStyle = settingsParsingStyle;
            ParsedResult = parsedResult;
        }

        public T AssignCustomSetting<T>()
        {
            return (T)ParsedResult;
        }
        
    }
}
