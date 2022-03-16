using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using USE_States;
using USE_Data;
using USE_Settings;
using USE_StimulusManagement;
// using USE_TasksCustomTypes;

namespace USE_ExperimentTemplate
{
	public class ControlLevel_Session_Template : ControlLevel
	{
		protected SessionData SessionData;
		private SessionDataControllers SessionDataControllers;
		private bool StoreData;
		[HideInInspector] public string SubjectID, SessionID, SessionDataPath, FilePrefix;

		public string TaskSelectionSceneName;

		// protected Dictionary<string, ControlLevel_Task_Template> ActiveTaskLevels;
		private Dictionary<string, Type> ActiveTaskTypes = new Dictionary<string, Type>();
		protected List<ControlLevel_Task_Template> ActiveTaskLevels;
		private ControlLevel_Task_Template CurrentTask;
		public List<ControlLevel_Task_Template> AvailableTaskLevels;
		public List<string> ActiveTaskNames;
		protected int taskCount;

		//For Loading config information
		public SessionDetails SessionDetails;
		public LocateFile LocateFile;

		private Camera SessionCam;

		private string configFileFolder;
		private bool TaskSceneLoaded, SceneLoading;
		
		public override void LoadSettings()
		{
			//load session config file
			configFileFolder = LocateFile.GetPath("Config File Folder");
			SubjectID = SessionDetails.GetItemValue("SubjectID");
			SessionID = SessionDetails.GetItemValue("SessionID");
			FilePrefix = "Subject_" + SubjectID + "__Session_" + SessionID + "__" +
			             DateTime.Today.ToString("dd_MM_yyyy") + "__" + DateTime.Now.ToString("HH_mm_ss");
			SessionSettings.ImportSettings_MultipleType("Session",
				LocateFile.FindFileInFolder(configFileFolder, "*Session*"));

			//if there is a single event code config file for all experiments, load it
			string eventCodeFileString =
				LocateFile.FindFileInFolder(configFileFolder, "*EventCode*");
			if (!string.IsNullOrEmpty(eventCodeFileString))
				SessionSettings.ImportSettings_SingleTypeJSON<EventCodeConfig>("EventCodeConfig", eventCodeFileString);

			if (SessionSettings.SettingExists("Session", "TaskNames"))
				ActiveTaskNames = (List<string>) SessionSettings.Get("Session", "TaskNames");
			else if (ActiveTaskNames.Count == 0)
				Debug.LogError("No task names specified in Session config file or by other means.");

			if (SessionSettings.SettingExists("Session", "StoreData"))
				StoreData = (bool) SessionSettings.Get("Session", "StoreData");

			SessionDataPath = LocateFile.GetPath("Data Folder") + Path.DirectorySeparatorChar + FilePrefix;
		}

		public override void DefineControlLevel()
		{
			//DontDestroyOnLoad(gameObject);
			State setupSession = new State("SetupSession");
			State selectTask = new State("SelectTask");
			State runTask = new State("RunTask");
			State finishSession = new State("FinishSession");
			AddActiveStates(new List<State> {setupSession, selectTask, runTask, finishSession});

			SessionDataControllers = new SessionDataControllers(GameObject.Find("DataControllers"));
			ActiveTaskLevels = new List<ControlLevel_Task_Template>();//new Dictionary<string, ControlLevel_Task_Template>();

			SessionCam = Camera.main;
			setupSession.AddDefaultInitializationMethod(() =>
			{
				SessionData.CreateFile();
			});
			
			int iTask = 0;
			bool oldStyleTaskLoading = false;
			bool newStyleTaskLoading = false;
			SceneLoading = false;
			string taskName;
			setupSession.AddUpdateMethod(() =>
			{
				if (iTask < ActiveTaskNames.Count)
				{
					Debug.Log("11111");
					if (!SceneLoading)
					{
						Debug.Log("22222");
						oldStyleTaskLoading = false;
						newStyleTaskLoading = false;
						int iAvail = 0;
						while (iAvail < AvailableTaskLevels.Count)
						{
							if (AvailableTaskLevels[iAvail].TaskName == ActiveTaskNames[iTask])
							{
								oldStyleTaskLoading = true;
								break;
							}

							iAvail++;
						}

						ControlLevel_Task_Template tl;
						AsyncOperation loadScene;
						if (oldStyleTaskLoading)
						{
							SceneLoading = true;
							tl = PopulateTaskLevel(AvailableTaskLevels[iAvail]);
							taskName = tl.TaskName;
							loadScene = SceneManager.LoadSceneAsync(taskName, LoadSceneMode.Additive);
							loadScene.completed += (_) => SceneLoaded(taskName);
						}
						else
						{
							if (!newStyleTaskLoading)
							{
								SceneLoading = true;
								newStyleTaskLoading = true;
								taskName = ActiveTaskNames[iTask];
								Debug.Log(taskName);
								loadScene = SceneManager.LoadSceneAsync(taskName, LoadSceneMode.Additive);
								loadScene.completed += (_) => SceneLoadedNew(taskName);
							}
							else
							{

							}
						}

						iTask++;
						// TaskSceneLoaded = false;
					}
				}
			});
			setupSession.SpecifyTermination(() => iTask >= ActiveTaskNames.Count && !SceneLoading, selectTask);

			//tasksFinished is a placeholder, eventually there will be a proper task selection screen
			bool tasksFinished = false;
			selectTask.AddUniversalInitializationMethod(() =>
			{
				SessionCam.gameObject.SetActive(true);
				tasksFinished = false;
				if (taskCount < ActiveTaskLevels.Count)
					CurrentTask = ActiveTaskLevels[taskCount]; //ActiveTaskLevels[AvailableTaskLevels[taskCount].TaskName];
				else
					tasksFinished = true;
			});
			selectTask.SpecifyTermination(() => !tasksFinished, runTask, () =>
			{
				runTask.AddChildLevel(CurrentTask);
				SessionCam.gameObject.SetActive(false);
				SceneManager.SetActiveScene(SceneManager.GetSceneByName(CurrentTask.TaskName));
			});
			selectTask.SpecifyTermination(() => tasksFinished, finishSession);

			//automatically finish tasks after running one - placeholder for proper selection
			//runTask.AddLateUpdateMethod
			runTask.AddUniversalInitializationMethod(() =>
			{
			});
			runTask.SpecifyTermination(() => CurrentTask.Terminated, selectTask, () =>
			{
				SceneManager.SetActiveScene(SceneManager.GetSceneByName(TaskSelectionSceneName));
				SessionData.AppendData();
				SessionData.WriteData();
				taskCount++;
			});

			finishSession.SpecifyTermination(() => true, ()=> null, () =>
			{
				SessionData.AppendData();
			});

			SessionData = SessionDataControllers.InstantiateSessionData(StoreData, SessionDataPath);
			SessionData.sessionLevel = this;
			SessionData.InitDataController();
			SessionData.ManuallyDefine();
		}

		ControlLevel_Task_Template PopulateTaskLevel(ControlLevel_Task_Template tl)
		{
			tl.SessionDataControllers = SessionDataControllers;
			tl.LocateFile = LocateFile;
			tl.SessionDataPath = SessionDataPath;
			if (!SessionSettings.SettingExists("Session", "ConfigFolderNames"))
				tl.TaskConfigPath =
					configFileFolder + Path.DirectorySeparatorChar +
					tl.TaskName;
			else
			{
				List<string> configFolders =
					(List<string>) SessionSettings.Get("Session", "ConfigFolderNames");
				tl.TaskConfigPath =
					configFileFolder + Path.DirectorySeparatorChar +
					configFolders[ActiveTaskNames.IndexOf(tl.TaskName)];
			}

			tl.FilePrefix = FilePrefix;
			tl.StoreData = StoreData;
			tl.SubjectID = SubjectID;
			tl.SessionID = SessionID;
			tl.DefineTaskLevel();
			ActiveTaskTypes.Add(tl.TaskName, tl.TaskLevelType);
			ActiveTaskLevels.Add(tl);
			return tl;
		}

		void SceneLoaded(string sceneName)
		{
			var methodInfo = GetType().GetMethod(nameof(this.FindTaskCam));
			MethodInfo findTaskCam = methodInfo.MakeGenericMethod(new Type[] {ActiveTaskTypes[sceneName]});
			findTaskCam.Invoke(this, new object[] {sceneName});
			// TaskSceneLoaded = true;
			SceneLoading = false;
		}

		void SceneLoadedNew(string taskName)
		{
			var methodInfo = GetType().GetMethod(nameof(this.PrepareTaskLevel));
			Type taskType = USE_Tasks_CustomTypes.CustomTaskDictionary[taskName].TaskLevelType;
			MethodInfo prepareTaskLevel = methodInfo.MakeGenericMethod(new Type[] {taskType});
			prepareTaskLevel.Invoke(this, new object[] {taskName});
			// TaskSceneLoaded = true;
			SceneLoading = false;
		}
		
		public void PrepareTaskLevel<T>(string taskName) where T : ControlLevel_Task_Template
		{
			Debug.Log(taskName);
			ControlLevel_Task_Template tl = GameObject.Find(taskName + "_Scripts").GetComponent<T>();
			tl = PopulateTaskLevel(tl);
			if(tl.TaskCam == null)
					tl.TaskCam = GameObject.Find(taskName + "_Camera").GetComponent<Camera>();
			tl.TaskCam.gameObject.SetActive(false);
		}
		public void FindTaskCam<T>(string taskName) where T : ControlLevel_Task_Template
		{
			ControlLevel_Task_Template tl = GameObject.Find("ControlLevels").GetComponent<T>();
			tl.TaskCam = GameObject.Find(taskName + "_Camera").GetComponent<Camera>();
			tl.TaskCam.gameObject.SetActive(false);
		}

		void OnApplicationQuit()
		{
			//	performancetext.AppendData();
			//	performancetext.WriteData();

			//	if (exptParameters.ContextMaterials != null)
			//	{
			//		foreach (var o in exptParameters.ContextMaterials)
			//		{
			//			Resources.UnloadAsset(o);
			//		}
			//	}

			//	if (eyeTrackType == 2)
			//	{
			//		if (calibLevel.calibrationUnfinished == true)
			//			udpManager.SendString("ET###leave_calibration");
			//		udpManager.SendString("ET###unsubscribe_eyetracker");
			//	}
			//	if (eventCodeManager.codesActive)
			//	{
			//		serialPortController.ClosePort();
			//	}
			//	trialLevel.WriteTrialData();
			//	blockData.AppendData();
			//	blockData.WriteData();
			//	//WriteFrameByFrameData();
			//	if (eyeTrackType == 2)
			//	{
			//		udpManager.SendString("DATA###clear_data");
			//		udpManager.CloseUDP();
			//	}
			//	//Save EditorLog and Player Log files
			if (StoreData)
			{
				System.IO.Directory.CreateDirectory(SessionDataPath + Path.DirectorySeparatorChar + "LogFile");
				string logPath = "";
				if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX |
				    SystemInfo.operatingSystemFamily == OperatingSystemFamily.Linux)
				{
					if (Application.isEditor)
						logPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal) +
						          "/Library/Logs/Unity/Editor.log";
					else
						logPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal) +
						          "/Library/Logs/Unity/Player.log";
				}
				else if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows)
				{
					if (Application.isEditor)
					{
						logPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) +
						          "\\Unity\\Editor\\Editor.log";
					}
					else
					{
						logPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "Low\\" +
						          Application.companyName + "\\" + Application.productName + "\\Player.log";
					}
				}

				if (Application.isEditor)
					File.Copy(logPath,
						SessionDataPath + Path.DirectorySeparatorChar + "LogFile" + Path.DirectorySeparatorChar +
						"Editor.log");
				else
					File.Copy(logPath,
						SessionDataPath + Path.DirectorySeparatorChar + "LogFile" + Path.DirectorySeparatorChar +
						"Player.log");

				System.IO.Directory.CreateDirectory(SessionDataPath + Path.DirectorySeparatorChar + "SessionSettings");

				SessionSettings.StoreSettings(SessionDataPath + Path.DirectorySeparatorChar + "SessionSettings" +
				                              Path.DirectorySeparatorChar);
			}
		}
	}

	public abstract class ControlLevel_Task_Template : ControlLevel
	{
		public string TaskName;
		public string TaskProjectFolder;
		[HideInInspector] public int BlockCount;
		protected int NumBlocksInTask;
		public ControlLevel_Trial_Template TrialLevel;
		protected BlockData BlockData;
		protected FrameData FrameData;
		protected TrialData TrialData;

		[HideInInspector] public SessionDataControllers SessionDataControllers;

		[HideInInspector] public bool StoreData;
		[HideInInspector] public string SessionDataPath, TaskConfigPath, TaskDataPath, SubjectID, SessionID, FilePrefix;
		[HideInInspector] public LocateFile LocateFile;

		// public string TaskSceneName;
		public Camera TaskCam;

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

		public Type TaskLevelType;
		protected Type TrialLevelType, TaskDefType, BlockDefType, TrialDefType, StimDefType;

		public virtual void SpecifyTypes()
		{
			Debug.Log(TaskName);
			TaskLevelType = USE_Tasks_CustomTypes.CustomTaskDictionary[TaskName].TaskLevelType;
			TrialLevelType = USE_Tasks_CustomTypes.CustomTaskDictionary[TaskName].TrialLevelType;
			TaskDefType = USE_Tasks_CustomTypes.CustomTaskDictionary[TaskName].TaskDefType;
			BlockDefType = USE_Tasks_CustomTypes.CustomTaskDictionary[TaskName].BlockDefType;
			TrialDefType = USE_Tasks_CustomTypes.CustomTaskDictionary[TaskName].TrialDefType;
			StimDefType = USE_Tasks_CustomTypes.CustomTaskDictionary[TaskName].StimDefType;
		}
		
		public void DefineTaskLevel()
		{
			ReadSettingsFiles();
			FindStims();

			State setupTask = new State("SetupTask");
			State runBlock = new State("RunBlock");
			State blockFeedback = new State("BlockFeedback");
			State finishTask = new State("FinishTask");
			runBlock.AddChildLevel(TrialLevel);
			AddActiveStates(new List<State> {setupTask, runBlock, blockFeedback, finishTask});

			TrialLevel.TrialDefType = TrialDefType;
			TrialLevel.StimDefType = StimDefType;

			AddInitializationMethod(() =>
			{
				BlockCount = -1;
				TaskCam.gameObject.SetActive(true);
			});

			setupTask.SpecifyTermination(() => true, runBlock);

			runBlock.AddUniversalInitializationMethod(() =>
			{
				BlockCount++;
				CurrentBlockDef = BlockDefs[BlockCount];
				TrialLevel.BlockCount = BlockCount;
				if (BlockCount == 0)
					TrialLevel.TrialCount_InTask = -1;
				TrialLevel.TrialDefs = CurrentBlockDef.TrialDefs;
			});

			runBlock.AddLateUpdateMethod(() => FrameData.AppendData());

			runBlock.SpecifyTermination(() => TrialLevel.Terminated, blockFeedback);

			blockFeedback.AddLateUpdateMethod(() => FrameData.AppendData());
			blockFeedback.SpecifyTermination(() => BlockCount < BlockDefs.Length - 1, runBlock, () =>
			{
				BlockData.AppendData();
				BlockData.WriteData();
			});
			blockFeedback.SpecifyTermination(() => BlockCount == BlockDefs.Length - 1, finishTask, () =>
			{
				BlockData.AppendData();
				BlockData.WriteData();
			});

			finishTask.SpecifyTermination(() => true, ()=> null);

			AddDefaultTerminationMethod(() =>
			{
				SessionDataControllers.RemoveDataController("BlockData_" + TaskName);
				SessionDataControllers.RemoveDataController("TrialData_" + TaskName);
				SessionDataControllers.RemoveDataController("FrameData_" + TaskName);
				int sgNum = TaskStims.AllTaskStimGroups.Count;
				for(int iSg = 0; iSg < sgNum; iSg++)
				{
					StimGroup[] taskSgs = new StimGroup[TaskStims.AllTaskStimGroups.Count];
					TaskStims.AllTaskStimGroups.Values.CopyTo(taskSgs, 0);
					StimGroup sg = taskSgs[0];
					//WHY DOESN'T THIS WORK - it doesn't seem to matter that it doesn't
					// string[] keys = new string[TaskStims.AllTaskStimGroups.Count];
					// TaskStims.AllTaskStimGroups.Keys.CopyTo(keys, 0);
					// TaskStims.AllTaskStimGroups.Remove(keys[0]);
					while(sg.stimDefs.Count>0)
						sg.stimDefs[0].Destroy();
					sg.DestroyStimGroup();
				}
				TaskStims.AllTaskStims.DestroyStimGroup();
				TaskCam.gameObject.SetActive(false);
			});
			
			//user-defined task control level 
			DefineControlLevel();

			//Setup data management
			TaskDataPath = SessionDataPath + Path.DirectorySeparatorChar + TaskName;
			FilePrefix = FilePrefix + "_" + TaskName;
			BlockData = SessionDataControllers.InstantiateBlockData(StoreData, TaskName,
				TaskDataPath + Path.DirectorySeparatorChar + "BlockData");
			BlockData.taskLevel = this;
			BlockData.fileName = FilePrefix + "__BlockData";
			BlockData.InitDataController();

			TrialData = SessionDataControllers.InstantiateTrialData(StoreData, TaskName,
				TaskDataPath + Path.DirectorySeparatorChar + "TrialData");
			TrialData.taskLevel = this;
			TrialData.trialLevel = TrialLevel;
			TrialLevel.TrialData = TrialData;
			TrialData.fileName = FilePrefix + "__TrialData";
			TrialData.InitDataController();

			FrameData = SessionDataControllers.InstantiateFrameData(StoreData, TaskName,
				TaskDataPath + Path.DirectorySeparatorChar + "FrameData");
			FrameData.taskLevel = this;
			FrameData.trialLevel = TrialLevel;
			TrialLevel.FrameData = FrameData;
			FrameData.fileName = FilePrefix + "__FrameData_PreTrial";
			FrameData.InitDataController();

			BlockData.ManuallyDefine();
			FrameData.ManuallyDefine();
			BlockData.AddStateTimingData(this);
			BlockData.CreateFile();
			FrameData.CreateFile();

			//AddDataController(BlockData, StoreData, TaskDataPath + Path.DirectorySeparatorChar + "BlockData", FilePrefix + "_BlockData.txt");

			TrialLevel.SessionDataControllers = SessionDataControllers;
			TrialLevel.FilePrefix = FilePrefix;
			TrialLevel.TaskStims = TaskStims;
			TrialLevel.PreloadedStims = PreloadedStims;
			TrialLevel.PrefabStims = PrefabStims;
			TrialLevel.ExternalStims = ExternalStims;
			TrialLevel.RuntimeStims = RuntimeStims;
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
				.MakeGenericMethod(new Type[] {TaskDefType});
			readTaskDef.Invoke(this, new object[] {TaskConfigPath});
			MethodInfo readBlockDefs = GetType().GetMethod(nameof(this.ReadBlockDefs))
				.MakeGenericMethod(new Type[] {BlockDefType});
			readBlockDefs.Invoke(this, new object[] {TaskConfigPath});
			MethodInfo readTrialDefs = GetType().GetMethod(nameof(this.ReadTrialDefs))
				.MakeGenericMethod(new Type[] {TrialDefType});
			readTrialDefs.Invoke(this, new object[] {TaskConfigPath});
			MethodInfo readStimDefs = GetType().GetMethod(nameof(this.ReadStimDefs))
				.MakeGenericMethod(new Type[] {StimDefType});
			readStimDefs.Invoke(this, new object[] {TaskConfigPath});

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
						BlockDefs[iBlock].TrialDefs = GetTrialDefsInBlock(iBlock, AllTrialDefs);
						BlockDefs[iBlock].AddToTrialDefsFromBlockDef();
					}
				}
			}
		}
		
		public void FindStims()
		{
			MethodInfo addTaskStimDefsToTaskStimGroup = GetType().GetMethod(nameof(this.AddTaskStimDefsToTaskStimGroup))
				.MakeGenericMethod(new Type[] {StimDefType});
			
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
				.MakeGenericMethod((new Type[] {StimDefType}));
			if (PreloadedStimGameObjects != null && PreloadedStimGameObjects.Count > 0)
			{
				foreach (GameObject go in PreloadedStimGameObjects)
				{
					taskStimDefFromGameObject.Invoke(this, new object[] {go, PreloadedStims});
					// addTaskStimDefsToTaskStimGroup.Invoke(this, new object[] {TaskConfigPath});
				}
				PreloadedStims.AddStims(PreloadedStimGameObjects);
			}
		}

		protected virtual void DefinePrefabStims()
		{
			MethodInfo taskStimDefFromPrefabPath = GetType().GetMethod(nameof(TaskStimDefFromPrefabPath))
				.MakeGenericMethod((new Type[] {StimDefType}));
			
			if (PrefabStimPaths != null && PrefabStimPaths.Count > 0)
			{
				//Prefabs with explicit path given
				foreach (string path in PrefabStimPaths)
				{
					taskStimDefFromPrefabPath.Invoke(this, new object[] {path, PreloadedStims});
				}

			}
			else
			{
				//Prefabs in Prefabs/TaskFolder or TaskFolder/Prefabs
				string[] prefabFolders =
					{"Assets/Resources/Prefabs/" + TaskName, "Assets/Resources/USE_Tasks/" + TaskName + "/Prefabs"};
				string[] guids = AssetDatabase.FindAssets("t: GameObject", prefabFolders);
				foreach (string guid in guids)
				{
					taskStimDefFromPrefabPath.Invoke(this, new object[] {AssetDatabase.GUIDToAssetPath(guid), PreloadedStims});
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
					stimFolderPath = (string) SessionSettings.Get(TaskName + "_TaskSettings", "ExternalStimFolderPath");
				if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ExternalStimExtension"))
					stimExtension = (string) SessionSettings.Get(TaskName + "_TaskSettings", "ExternalStimExtension");
				if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ExternalStimScale"))
					stimScale = (float) SessionSettings.Get(TaskName + "_TaskSettings", "ExternalStimScale");
			}

			foreach (StimDef sd in ExternalStims.stimDefs)
			{
				sd.StimFolderPath = stimFolderPath;
				sd.StimExtension = stimExtension;
				sd.StimScale = stimScale;
			}
		}

		public void ReadTaskDef<T>(string taskConfigFolder) where T : TaskDef
		{
			string taskDefFile = LocateFile.FindFileInFolder(taskConfigFolder, "*" + TaskName + "*Task*");
			if (!string.IsNullOrEmpty(taskDefFile))
			{
				SessionSettings.ImportSettings_MultipleType(TaskName + "_TaskSettings", taskDefFile);
				//TaskDef = (T) SessionSettings.Get(TaskName + "_TaskSettings");
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
				BlockDefs = (T[]) SessionSettings.Get("blockDefs");
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
				AllTrialDefs = (T[]) SessionSettings.Get(TaskName + "_TrialDefs");
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
				
				ExternalStims = new StimGroup("ExternalStims", (T[]) SessionSettings.Get(TaskName + "_ExternalStimDefs"));
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
			if (sg != null)
				sd.AddToStimGroup(sg);
			return (T) sd;
		}
		
		public T TaskStimDefFromPrefabPath<T>(string prefabPath, StimGroup sg = null) where T : StimDef, new()
		{
			StimDef sd = new T();
			sd.PrefabPath = prefabPath;
			if (sg != null)
				sd.AddToStimGroup(sg);
			return (T) sd;
		}

		public TrialDef[] GetTrialDefsInBlock(int BlockNum, TrialDef[] trialDefs)
		{
			List<TrialDef> trialList = new List<TrialDef>();
			int currentBlockCount = -1;
			for (int iTrial = 0; (currentBlockCount <= BlockNum) & (iTrial < trialDefs.Length); iTrial++)
			{
				currentBlockCount = trialDefs[iTrial].BlockCount;
				if (currentBlockCount == BlockNum)
					trialList.Add(trialDefs[iTrial]);
			}

			return trialList.ToArray();
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


	public abstract class ControlLevel_Trial_Template : ControlLevel
	{
		[HideInInspector] public TrialData TrialData;
		[HideInInspector] public FrameData FrameData;
		[HideInInspector] public int BlockCount, TrialCount_InTask, TrialCount_InBlock, AbortCode;
		protected int NumTrialsInBlock;
		[HideInInspector] public SessionDataControllers SessionDataControllers;

		[HideInInspector] public bool StoreData;
		[HideInInspector] public string TaskDataPath, FilePrefix;

		protected State SetupTrial, FinishTrial;

		public TrialDef[] TrialDefs;

		[HideInInspector] public TaskStims TaskStims;
		[HideInInspector] public StimGroup PreloadedStims, PrefabStims, ExternalStims, RuntimeStims;
		[HideInInspector] public List<StimGroup> TrialStims;

		//protected TrialDef CurrentTrialDef;
		protected T GetCurrentTrialDef<T>() where T : TrialDef
		{
			return (T) TrialDefs[TrialCount_InBlock];
		}

		public Type TrialDefType, StimDefType;

		public void DefineTrialLevel()
		{
			SetupTrial = new State("SetupTrial");
			FinishTrial = new State("FinishTrial");
			AddActiveStates(new List<State> {SetupTrial, FinishTrial});
			//DefineTrial();
			AddInitializationMethod(() =>
			{
				TrialCount_InBlock = -1;
				TrialStims = new List<StimGroup>();
				//DetermineNumTrialsInBlock();
			});

			SetupTrial.AddUniversalInitializationMethod(() =>
			{
				AbortCode = 0;
				TrialCount_InTask++;
				TrialCount_InBlock++;
				if (TrialCount_InTask >= 999)
					FrameData.fileName = FilePrefix + "__FrameData_Trial_" + (TrialCount_InTask + 1) + ".txt";
				else if (TrialCount_InTask >= 99)
					FrameData.fileName = FilePrefix + "__FrameData_Trial_0" + (TrialCount_InTask + 1) + ".txt";
				else if (TrialCount_InTask >= 9)
					FrameData.fileName = FilePrefix + "__FrameData_Trial_00" + (TrialCount_InTask + 1) + ".txt";
				else
					FrameData.fileName = FilePrefix + "__FrameData_Trial_000" + (TrialCount_InTask + 1) + ".txt";
				FrameData.CreateFile();
				DefineTrialStims();
				ResetRelativeStartTime();
				foreach (StimGroup sg in TrialStims)
				{
					sg.LoadStims();
				}
			});

			FinishTrial.SpecifyTermination(() => TrialCount_InBlock < TrialDefs.Length - 1, SetupTrial);
			FinishTrial.SpecifyTermination(() => TrialCount_InBlock == TrialDefs.Length - 1, ()=> null);

			FinishTrial.AddUniversalTerminationMethod(() =>
			{
				TrialData.AppendData();
				TrialData.WriteData();
				FrameData.AppendData();
				FrameData.WriteData();
				int nStimGroups = TrialStims.Count;
				for (int iG = 0; iG < nStimGroups; iG++)
				{
					TrialStims[0].DestroyStimGroup();
					TrialStims.RemoveAt(0);
				}
				//WriteDataFiles();
			});
			DefineControlLevel();
			TrialData.ManuallyDefine();
			TrialData.AddStateTimingData(this);
			TrialData.CreateFile();


		}


		protected virtual void DefineTrialStims()
		{

		}

		private void OnApplicationQuit()
		{
			if (TrialData != null)
			{
				TrialData.AppendData();
				TrialData.WriteData();
			}
		}
		
		
		public StimGroup CreateStimGroup(string groupName, State setActiveOnInit = null, State setInactiveOnTerm = null)
		{
			TaskStims.CreateStimGroup(groupName, setActiveOnInit, setInactiveOnTerm);
			return TaskStims.AllTaskStimGroups[groupName];
		}

		public StimGroup CreateStimGroup(string groupName, IEnumerable<StimDef> stims, State setActiveOnInit = null, State setInactiveOnTerm = null)
		{
			TaskStims.CreateStimGroup(groupName, stims, setActiveOnInit, setInactiveOnTerm);
			return TaskStims.AllTaskStimGroups[groupName];
		}

		public StimGroup CreateStimGroup(string groupName, IEnumerable<int[]> dimValGroup, string folderPath,
			IEnumerable<string[]> featureNames, string neutralPatternedColorName, Camera cam, float scale = 1, State setActiveOnInit = null, State setInactiveOnTerm = null)
		{
			TaskStims.CreateStimGroup(groupName, dimValGroup, folderPath, featureNames, neutralPatternedColorName, cam,
				scale, setActiveOnInit, setInactiveOnTerm);
			return TaskStims.AllTaskStimGroups[groupName];
		}

		public StimGroup CreateStimGroup(string groupName, string TaskName, string stimDefFilePath, State setActiveOnInit = null, State setInactiveOnTerm = null)
		{
			TaskStims.CreateStimGroup(groupName, TaskName, stimDefFilePath, setActiveOnInit, setInactiveOnTerm);
			return TaskStims.AllTaskStimGroups[groupName];
		}

		public StimGroup CreateStimGroup(string groupName, StimGroup sgOrig, IEnumerable<int> stimSubsetIndices, State setActiveOnInit = null, State setInactiveOnTerm = null)
		{
			TaskStims.CreateStimGroup(groupName, sgOrig, stimSubsetIndices, setActiveOnInit, setInactiveOnTerm);
			return TaskStims.AllTaskStimGroups[groupName];
		}

		public void DestroyStimGroup(StimGroup sg)
		{
			sg.DestroyStimGroup();
			TaskStims.AllTaskStimGroups.Remove(sg.stimGroupName);
		}

		public void DestroyStimGroup(string sgName)
		{
			TaskStims.AllTaskStimGroups[sgName].DestroyStimGroup();
			TaskStims.AllTaskStimGroups.Remove(sgName);
		}

		// MethodInfo taskStimDefFromPrefabPath = GetType().GetMethod(nameof(TaskStimDefFromPrefabPath))
		// 		.MakeGenericMethod((new Type[] {StimDefType}));
		// 		taskStimDefFromPrefabPath.Invoke(this, new object[] {path, PreloadedStims});
		
		
		protected T GetGameObjectStimDefComponent<T>(GameObject go) where T : StimDef
		{
			// return (T) go.GetComponent<StimDef>();
			MethodInfo getStimDef = GetType().GetMethod(nameof(StimDefPointer.GetStimDef)).MakeGenericMethod((new Type[] {StimDefType}));
			return (T)getStimDef.Invoke(this, new object[] {go});

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
			if(! AllTaskStimGroups.ContainsKey(groupName))
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

	public class TrialStims : TaskStims
	{
		
	}

	public class SessionData : DataController
	{
		public ControlLevel_Session_Template sessionLevel;

		public override void DefineDataController()
		{
			AddDatum("SubjectID", () => sessionLevel.SubjectID);
			AddDatum("SessionID", () => sessionLevel.SessionID);
			AddStateTimingData(sessionLevel);
		}
	}

	public class BlockData : DataController
	{
		public ControlLevel_Task_Template taskLevel;

		public override void DefineDataController()
		{
			AddDatum("SubjectID", () => taskLevel.SubjectID);
			AddDatum("SessionID", () => taskLevel.SessionID);
			AddDatum("TaskName", () => taskLevel.TaskName);
			AddDatum("BlockCount", () => taskLevel.BlockCount + 1);
		}
	}

	public class TrialData : DataController
	{
		public ControlLevel_Task_Template taskLevel;
		public ControlLevel_Trial_Template trialLevel;

		public override void DefineDataController()
		{
			AddDatum("SubjectID", () => taskLevel.SubjectID);
			AddDatum("SessionID", () => taskLevel.SessionID);
			AddDatum("TaskName", () => taskLevel.TaskName);
			AddDatum("BlockCount", () => taskLevel.BlockCount + 1);
			AddDatum("TrialCount_InTask", () => trialLevel.TrialCount_InTask + 1);
			AddDatum("TrialCount_InBlock", () => trialLevel.TrialCount_InBlock + 1);
			AddDatum("AbortCode", () => trialLevel.AbortCode);
		}
	}

	public class FrameData : DataController
	{
		public ControlLevel_Task_Template taskLevel;
		public ControlLevel_Trial_Template trialLevel;

		public override void DefineDataController()
		{
			AddDatum("SubjectID", () => taskLevel.SubjectID);
			AddDatum("SessionID", () => taskLevel.SessionID);
			AddDatum("TaskName", () => taskLevel.TaskName);
			AddDatum("BlockCount", () => taskLevel.BlockCount + 1);
			AddDatum("TrialCount_InTask", () => trialLevel.TrialCount_InTask + 1);
			AddDatum("TrialCount_InBlock", () => trialLevel.TrialCount_InBlock + 1);
			AddDatum("Frame", () => Time.frameCount);
			AddDatum("FrameStartUnity", () => Time.time);
		}
	}

	public class SessionDataControllers//:MonoBehaviour
	{
		private Dictionary<string, GameObject> DataControllerContainers;
		private GameObject DataContainer;

		public SessionDataControllers(GameObject cont)
		{
			DataControllerContainers = new Dictionary<string, GameObject>();
			DataContainer = cont;
		}

		public DataController InstantiateDataController(string str, bool storeData, string path)
		{
			DataController dc = AddContainer(str).AddComponent<DataController>();
			SpecifyParameters(dc, storeData, path);
			return dc;
		}

		public SessionData InstantiateSessionData(bool storeData, string path)
		{
			SessionData dc = AddContainer("SessionData").AddComponent<SessionData>();
			SpecifyParameters(dc, storeData, path);
			return dc;
		}

		public BlockData InstantiateBlockData(bool storeData, string taskName, string path)
		{
			BlockData dc = AddContainer("BlockData_" + taskName).AddComponent<BlockData>();
			SpecifyParameters(dc, storeData, path);
			return dc;
		}

		public TrialData InstantiateTrialData(bool storeData, string taskName, string path)
		{
			TrialData dc = AddContainer("TrialData_" + taskName).AddComponent<TrialData>();
			SpecifyParameters(dc, storeData, path);
			return dc;
		}

		public FrameData InstantiateFrameData(bool storeData, string taskName, string path)
		{
			FrameData dc = AddContainer("FrameData_" + taskName).AddComponent<FrameData>();
			SpecifyParameters(dc, storeData, path);
			return dc;
		}

		private GameObject AddContainer(string st)
		{
			if (DataContainer.transform.Find(st) == null)
			{
				GameObject go = new GameObject(st);
				go.transform.SetParent(DataContainer.transform);
				DataControllerContainers.Add(st, go);
				return go;
			}
			else
			{
				Debug.LogError("Attempted to add data controller container named " + st +
				" to DataControllers but a container with the same name has already been created.");
				return null;
			}
		}

		private void SpecifyParameters(DataController dc, bool storeData, string path, bool sm = true)
		{
			dc.storeData = storeData;
			dc.folderPath = path;
			dc.DefineManually = sm;
		}

		public void RemoveDataController(string name)
		{
			if (DataControllerContainers.ContainsKey(name))
			{
				GameObject.Destroy(DataControllerContainers[name]);
				DataControllerContainers.Remove(name);
			}else
				Debug.LogWarning("Attempted to destroy data controller " + name + ", but this does not exist.");
		}
	}


	public class SessionDef
	{
		public string Subject;
		public DateTime SessionStart_DateTime;
		public int SessionStart_Frame;
		public float SessionStart_UnityTime;
		public string SessionID;
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
	}
	public class BlockDef
	{
		public int BlockCount;
		public TrialDef[] TrialDefs;

		public virtual void GenerateTrialDefsFromBlockDef()
		{
		}

		public virtual void AddToTrialDefsFromBlockDef()
		{
		}
	}
	public abstract class TrialDef
	{
		public int BlockCount, TrialCountInBlock, TrialCountInTask;
		public TrialStims TrialStims;
	}

	public class EventCodeConfig
	{
		public EventCode MainInitEnd;
		public EventCode MainStartEnd;
		public EventCode MainInstruct1End;
		public EventCode MainInstruct2End;
		public EventCode MainCalibEnd;
		public EventCode MainTutorialEnd;
		public EventCode TrlStart;
		public EventCode TrlEnd;
		public EventCode FixCentralCueStart;
		public EventCode FixTargetStart;
		public EventCode FixDistractorStart;
		public EventCode FixIrrelevantStart;
		public EventCode FixObjectEnd;
		public EventCode TouchCentralCueStart;
		public EventCode TouchTargetStart;
		public EventCode TouchDistractorStart;
		public EventCode TouchIrrelevantStart;
		public EventCode TouchOtherStart;
		public EventCode TouchOff;
		public EventCode CorrectResponse;
		public EventCode IncorrectResponse;
		public EventCode Rewarded;
		public EventCode Unrewarded;
		public EventCode BreakFixation;
		public EventCode NoChoice;
		public EventCode NoFixationNoTrialStart;
		public EventCode Recalibration;
		public EventCode HoldKeyLift;
		public EventCode SlowReach;
		public EventCode FixPointOn;
		public EventCode FixPointOff;
		public EventCode ContextOn;
		public EventCode ContextOff;
		public EventCode StimOn;
		public EventCode StimOff;
		public EventCode GoCueOn;
		public EventCode GoCueOff;
		public EventCode SelectionVisualFbOn;
		public EventCode SelectionAuditoryFbOn;
		public EventCode TokensCompletFbOn;
		public EventCode TokensCompletFbOff;
		public EventCode Fluid1Onset;
		public EventCode Fluid2Onset;
		public EventCode TokensAddedMin;
		public EventCode TokensAddedMax;
		public EventCode RewardValidityMin;
		public EventCode RewardValidityMax;
		public EventCode DimensionalityMin;
		public EventCode DimensionalityMax;
		public EventCode TokenRewardPositive;
		public EventCode TokenRewardNegative;
		public EventCode TokenRewardNeutral;
		public EventCode BlockConditionMin;
		public EventCode BlockConditionMax;
		public EventCode ContextCodeMin;
		public EventCode ContextCodeMax;
		public EventCode StimCodeMin;
		public EventCode StimCodeMax;
		public EventCode TrialIndexMin;
		public EventCode TrialIndexMax;
		public EventCode TrialNumberMin;
		public EventCode TrialNumberMax;

	}

	public class EventCode
	{
		public int Value;
		public string Description;
	}

}
