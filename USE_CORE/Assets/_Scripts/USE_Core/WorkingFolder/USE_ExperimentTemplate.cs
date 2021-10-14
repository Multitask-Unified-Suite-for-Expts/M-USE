using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using USE_States;
using USE_Data;
using USE_Settings;

namespace USE_ExperimentTemplate
{
	public class ControlLevel_Session_Template : ControlLevel
	{
		protected SessionData SessionData;
		private SessionDataControllers SessionDataControllers;
		private bool StoreData;
		[HideInInspector]
		public string SubjectID, SessionID, SessionDataPath, FilePrefix;

		public string TaskSelectionSceneName;

		protected Dictionary<string, ControlLevel_Task_Template> ActiveTaskLevels;
		private ControlLevel_Task_Template CurrentTask;
		[HideInInspector]
		public string CurrentTaskName;
		public List<ControlLevel_Task_Template> AvailableTaskLevels;
		public List<string> ActiveTaskNames;

		//For Loading config information
		public SessionDetails SessionDetails;
		public LocateFile LocateFile;

		private Camera SessionCam;

		//private void Awake()
		//{
		//	DontDestroyOnLoad(gameObject);
		//}

		public override void LoadSettings()
		{
			//load session config file
			SubjectID = SessionDetails.GetItemValue("SubjectID");
			SessionID = SessionDetails.GetItemValue("SessionID");
			FilePrefix = "Subject_" + SubjectID + "__Session_" + SessionID + "__" + DateTime.Today.ToString("dd_MM_yyyy") + "__" + DateTime.Now.ToString("HH_mm_ss");
			SessionSettings.ImportSettings_MultipleType("Session", LocateFile.FindFileInFolder(LocateFile.GetPath("Config File Folder"), "*Session*"));

			//if there is a single event code config file for all experiments, load it
			string eventCodeFileString = LocateFile.FindFileInFolder(LocateFile.GetPath("Config File Folder"), "*EventCode*");
			if (!string.IsNullOrEmpty(eventCodeFileString))
				SessionSettings.ImportSettings_SingleTypeJSON<EventCodeConfig>("EventCodeConfig", eventCodeFileString);

			if (SessionSettings.SettingExists("Session", "TaskNames"))
				ActiveTaskNames = (List<string>)SessionSettings.Get("Session", "TaskNames");
			else if (ActiveTaskNames.Count == 0)
				Debug.LogError("No task names specified in Session config file or by other means.");

			if (SessionSettings.SettingExists("Session", "StoreData"))
				StoreData = (bool)SessionSettings.Get("Session", "StoreData");

			SessionDataPath = LocateFile.GetPath("Data Folder") + Path.DirectorySeparatorChar + FilePrefix;
		}

		public override void DefineControlLevel()
		{
			//DontDestroyOnLoad(gameObject);
			State setupSession = new State("SetupSession");
			State selectTask = new State("SelectTask");
			State runTask = new State("RunTask");
			State finishSession = new State("FinishSession");
			AddActiveStates(new List<State> { setupSession, selectTask, runTask, finishSession });

			SessionDataControllers = new SessionDataControllers(GameObject.Find("DataControllers"));
			ActiveTaskLevels = new Dictionary<string, ControlLevel_Task_Template>();

			SessionCam = Camera.main;

			setupSession.AddDefaultInitializationMethod(() =>
			{
			SessionData.CreateFile();
			foreach (ControlLevel_Task_Template tl in AvailableTaskLevels)
			{
					if (ActiveTaskNames.Contains(tl.TaskName))
					{
						ActiveTaskLevels.Add(tl.TaskName, tl);
						tl.SessionDataControllers = SessionDataControllers;
						tl.LocateFile = LocateFile;
						tl.SessionDataPath = SessionDataPath;
						if (!SessionSettings.SettingExists("Session", "ConfigFolderNames"))
							tl.TaskConfigPath = LocateFile.GetPath("Config File Folder") + Path.DirectorySeparatorChar + tl.TaskName;
						else
						{
							List<string> configFolders = (List<string>)SessionSettings.Get("Session", "ConfigFolderNames");
							tl.TaskConfigPath = LocateFile.GetPath("Config File Folder") + Path.DirectorySeparatorChar + configFolders[ActiveTaskNames.IndexOf(tl.TaskName)];
						}
						tl.FilePrefix = FilePrefix;
						tl.StoreData = StoreData;
						tl.SubjectID = SubjectID;
						tl.SessionID = SessionID;
						tl.DefineTaskLevel();
						//LoadAsyncScene(SceneManager.GetSceneByName(tl.TaskSceneName));
						SceneManager.LoadScene(tl.TaskSceneName, LoadSceneMode.Additive);
					}
				}
			});
			setupSession.SpecifyTermination(() => true, selectTask);

			//tasksFinished is a placeholder, eventually there will be a proper task selection screen
			bool tasksFinished = false;
			selectTask.AddUniversalInitializationMethod(() =>
			{
				SceneManager.SetActiveScene(SceneManager.GetSceneByName(TaskSelectionSceneName));
				SessionCam.gameObject.SetActive(true);
				foreach (ControlLevel_Task_Template tl in ActiveTaskLevels.Values)
				{
					if (tl.TaskCam == null)
					{
						tl.TaskCam = GameObject.Find(tl.TaskName + "_Camera").GetComponent<Camera>();
						tl.TaskCam.gameObject.SetActive(false);
					}
				}
				tasksFinished = false;
				if (AvailableTaskLevels.Count > 0)
				{
					CurrentTask = ActiveTaskLevels[AvailableTaskLevels[0].TaskName];
					AvailableTaskLevels.RemoveAt(0);
				}
				else
					tasksFinished = true;
			});
			selectTask.SpecifyTermination(() => !tasksFinished, runTask, () => runTask.AddChildLevel(CurrentTask));
			selectTask.SpecifyTermination(() => tasksFinished, finishSession);

			//automatically finish tasks after running one - placeholder for proper selection
			//runTask.AddLateUpdateMethod
			runTask.AddUniversalInitializationMethod(() => {
				SessionCam.gameObject.SetActive(false);

			});
			runTask.SpecifyTermination(() => CurrentTask.Terminated, selectTask, () => { SessionData.AppendData(); SessionData.WriteData(); });

			finishSession.SpecifyTermination(() => true, null, ()=> SessionData.AppendData());

			SessionData = SessionDataControllers.InstantiateSessionData(StoreData, SessionDataPath);
			SessionData.sessionLevel = this;
			SessionData.InitDataController();
			SessionData.ManuallyDefine();
		}

		IEnumerator LoadAsyncScene(Scene scene)
		{

			AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(scene.name);

			// Wait until the asynchronous scene fully loads
			while (!asyncLoad.isDone)
			{
				yield return null;
			}
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
			Debug.Log(SessionData.folderPath);

			//	//Save EditorLog and Player Log files
			if (StoreData)
			{
				System.IO.Directory.CreateDirectory(SessionDataPath + Path.DirectorySeparatorChar + "LogFile");
				string logPath = "";
				if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX | SystemInfo.operatingSystemFamily == OperatingSystemFamily.Linux)
				{
					if (Application.isEditor)
						logPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "/Library/Logs/Unity/Editor.log";
					else
						logPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "/Library/Logs/Unity/Player.log";
				}
				else if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows)
				{
					if (Application.isEditor)
					{
						logPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Unity\\Editor\\Editor.log";
					}
					else
					{
						logPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "Low\\" + Application.companyName + "\\" + Application.productName + "\\Player.log";
					}
				}
				if (Application.isEditor)
					File.Copy(logPath, SessionDataPath + Path.DirectorySeparatorChar + "LogFile" + Path.DirectorySeparatorChar + "Editor.log");
				else
					File.Copy(logPath, SessionDataPath + Path.DirectorySeparatorChar + "LogFile" + Path.DirectorySeparatorChar + "Player.log");

				System.IO.Directory.CreateDirectory(SessionDataPath + Path.DirectorySeparatorChar + "SessionSettings");

				SessionSettings.StoreSettings(SessionDataPath + Path.DirectorySeparatorChar + "SessionSettings" + Path.DirectorySeparatorChar);
			}
		}
	}

	public abstract class ControlLevel_Task_Template : ControlLevel
	{
		public string TaskName;
		[HideInInspector]
		public int BlockCount;
		protected int NumBlocksInTask;
		public ControlLevel_Trial_Template TrialLevel;
		protected BlockData BlockData;
		protected FrameData FrameData;
		protected TrialData TrialData;

		[HideInInspector]
		public SessionDataControllers SessionDataControllers;

		[HideInInspector]
		public bool StoreData;
		[HideInInspector]
		public string SessionDataPath, TaskConfigPath, TaskDataPath, SubjectID, SessionID, FilePrefix;
		[HideInInspector]
		public LocateFile LocateFile;

		public string TaskSceneName;
		public Camera TaskCam;

		//protected TrialDef[] AllTrialDefs;
		//protected TrialDef[] CurrentBlockTrialDefs;
		private TaskDef TaskDef;
		private BlockDef[] BlockDefs;
		private BlockDef CurrentBlockDef;
		private TrialDef[] AllTrialDefs;

		protected Type TaskDefType, BlockDefType, TrialDefType;

		public virtual void SpecifyTypes() { }

		private void ReadSettingsFiles()
		{
			//user specifies what custom types they have that inherit from TaskDef, BlockDef, and TrialDef;
			SpecifyTypes();

			if (TaskDefType == null)
				TaskDefType = typeof(TaskDef);
			if (BlockDefType == null)
				BlockDefType = typeof(BlockDef);
			if (TrialDefType == null)
				TrialDefType = typeof(TrialDef);

			//read in the TaskDef, BlockDef and TrialDef files (any of these may not exist)
			MethodInfo readTaskDef = GetType().GetMethod(nameof(this.ReadTaskDef)).MakeGenericMethod(new Type[] { TaskDefType });
			readTaskDef.Invoke(this, new object[] { TaskConfigPath });
			MethodInfo readBlockDefs = GetType().GetMethod(nameof(this.ReadBlockDefs)).MakeGenericMethod(new Type[] { BlockDefType });
			readBlockDefs.Invoke(this, new object[] { TaskConfigPath });
			MethodInfo readTrialDefs = GetType().GetMethod(nameof(this.ReadTrialDefs)).MakeGenericMethod(new Type[] { TrialDefType });
			readTrialDefs.Invoke(this, new object[] { TaskConfigPath });

			//handling of block and trial defs so that each BlockDef contains a TrialDef[] array

			if (AllTrialDefs == null)
			{
				if (BlockDefs == null)
					Debug.LogError("Neither BlockDef nor TrialDef config files provided in " + TaskName + " folder, no trials generated as a result.");
				else
				{
					//Do something with blockdef by itself
					Debug.LogError("BlockDef config file provided without TrialDef config file in " + TaskName + " folder, no method currently exists to handle this case.");
				}

			}
			else
			{
				if (BlockDefs == null)
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
				}
				for (int iBlock = 0; iBlock < BlockDefs.Length; iBlock++)
				{
					if (BlockDefs[iBlock] == null)
						BlockDefs[iBlock] = new BlockDef();
					BlockDefs[iBlock].BlockCount = iBlock;
					BlockDefs[iBlock].TrialDefs = GetTrialDefsInBlock(iBlock, AllTrialDefs);
				}
			}

		}


		public void DefineTaskLevel()
		{
			ReadSettingsFiles();

			State setupTask = new State("SetupTask");
			State runBlock = new State("RunBlock");
			State blockFeedback = new State("BlockFeedback");
			State finishTask = new State("FinishTask");
			runBlock.AddChildLevel(TrialLevel);
			AddActiveStates(new List<State> { setupTask, runBlock, blockFeedback, finishTask });

			TrialLevel.TrialDefType = TrialDefType;


			AddInitializationMethod(() => {
				//cam.enabled = false;
				TaskCam.gameObject.SetActive(true);
				SceneManager.SetActiveScene(SceneManager.GetSceneByName(TaskSceneName));
				BlockCount = -1;
				//DetermineNumBlocksInTask();
				//prepare blockDef[]
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
				//SendTrialDefsToTrialLevel();
			});

			runBlock.AddLateUpdateMethod(() => FrameData.AppendData());

			runBlock.SpecifyTermination(() => TrialLevel.Terminated, blockFeedback);

			blockFeedback.AddLateUpdateMethod(() => FrameData.AppendData());
			blockFeedback.SpecifyTermination(() => BlockCount < BlockDefs.Length - 1, runBlock, () => { BlockData.AppendData(); BlockData.WriteData(); });
			blockFeedback.SpecifyTermination(() => BlockCount == BlockDefs.Length - 1, finishTask, () => { BlockData.AppendData(); BlockData.WriteData(); });

			finishTask.SpecifyTermination(() => true, null);

			AddDefaultTerminationMethod(() => {
				SessionDataControllers.RemoveDataController("BlockData");
				SessionDataControllers.RemoveDataController("TrialData");
				SessionDataControllers.RemoveDataController("FrameData");
				//cam.enabled = true;
				TaskCam.gameObject.SetActive(false);
			});



			DefineControlLevel();

			//Setup data management
			TaskDataPath = SessionDataPath + Path.DirectorySeparatorChar + TaskName;
			FilePrefix = FilePrefix + "_" + TaskName;
			BlockData = SessionDataControllers.InstantiateBlockData(StoreData, TaskDataPath + Path.DirectorySeparatorChar + "BlockData");
			BlockData.taskLevel = this;
			BlockData.fileName = FilePrefix + "__BlockData";
			BlockData.InitDataController();

			TrialData = SessionDataControllers.InstantiateTrialData(StoreData, TaskDataPath + Path.DirectorySeparatorChar + "TrialData");
			TrialData.taskLevel = this;
			TrialData.trialLevel = TrialLevel;
			TrialLevel.TrialData = TrialData;
			TrialData.fileName = FilePrefix + "__TrialData";
			TrialData.InitDataController();

			FrameData = SessionDataControllers.InstantiateFrameData(StoreData, TaskDataPath + Path.DirectorySeparatorChar + "FrameData");
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
			TrialLevel.DefineTrialLevel();
		}



		public void ReadTaskDef<T>(string taskConfigFolder) where T : TaskDef
		{
			string taskDefFile = LocateFile.FindFileInFolder(taskConfigFolder, "*" + TaskName + "*Task*");
			if (!string.IsNullOrEmpty(taskDefFile))
			{
				SessionSettings.ImportSettings_MultipleType(TaskName + "_TaskSettings", taskDefFile);
				TaskDef = (T)SessionSettings.Get(TaskName + "_TaskSettings");
			}
			else
				Debug.Log("No taskdef file in config folder (this may not be a problem).");
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
					SessionSettings.ImportSettings_SingleTypeArray<BlockDef>("blockDefs", blockDefFile);
				else
					SessionSettings.ImportSettings_SingleTypeJSON<BlockDef[]>("blockDefs", blockDefFile);
				BlockDefs = (T[])SessionSettings.Get("blockDefs");
			}
			else
				Debug.Log("No blockdef file in config folder (this may not be a problem).");
		}

		public void ReadTrialDefs<T>(string taskConfigFolder) where T : TrialDef
		{
			//string taskConfigFolder = LocateFile.GetPath("Config File Folder") + Path.DirectorySeparatorChar + TaskName;
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
			BlockData.AppendData();
			BlockData.WriteData();
			FrameData.AppendData();
			FrameData.WriteData();
		}
	}


	public abstract class ControlLevel_Trial_Template : ControlLevel
	{
		[HideInInspector]
		public TrialData TrialData;
		[HideInInspector]
		public FrameData FrameData;
		[HideInInspector]
		public int BlockCount, TrialCount_InTask, TrialCount_InBlock, AbortCode;
		protected int NumTrialsInBlock;
		[HideInInspector]
		public SessionDataControllers SessionDataControllers;

		[HideInInspector]
		public bool StoreData;
		[HideInInspector]
		public string TaskDataPath, FilePrefix;

		protected State SetupTrial, FinishTrial;

		public TrialDef[] TrialDefs;
		//protected TrialDef CurrentTrialDef;
		protected T GetCurrentTrialDef<T>() where T:TrialDef { return (T) TrialDefs[TrialCount_InBlock]; }
		public Type TrialDefType;

		public void DefineTrialLevel()
		{
			SetupTrial = new State("SetupTrial");
			FinishTrial = new State("FinishTrial");
			AddActiveStates(new List<State> { SetupTrial, FinishTrial });
			//DefineTrial();
			AddInitializationMethod(() =>
			{
				TrialCount_InBlock = -1;
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
				//MethodInfo getCTrialDef = GetType().GetMethod(nameof(this.GetCurrentTrialDef)).MakeGenericMethod(new Type[] { TrialDefType });
				//getCTrialDef.Invoke(this, new object[] {  });
				//PopulateCurrentTrialVariables();
			});

			FinishTrial.SpecifyTermination(() => TrialCount_InBlock < TrialDefs.Length - 1, SetupTrial);
			FinishTrial.SpecifyTermination(() => TrialCount_InBlock == TrialDefs.Length - 1, null);

			FinishTrial.AddUniversalTerminationMethod(() =>
			{
				TrialData.AppendData();
				TrialData.WriteData();
				FrameData.AppendData();
				FrameData.WriteData();
				//WriteDataFiles();
			});
			DefineControlLevel();
			TrialData.ManuallyDefine();
			TrialData.AddStateTimingData(this);
			TrialData.CreateFile();
		}

		public virtual void PopulateCurrentTrialVariables() { }

		private void OnApplicationQuit()
		{
			TrialData.AppendData();
			TrialData.WriteData();
		}

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

	public class SessionDataControllers:MonoBehaviour
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

		public BlockData InstantiateBlockData(bool storeData, string path)
		{
			BlockData dc = AddContainer("BlockData").AddComponent<BlockData>();
			SpecifyParameters(dc, storeData, path);
			return dc;
		}

		public TrialData InstantiateTrialData(bool storeData, string path)
		{
			TrialData dc = AddContainer("TrialData").AddComponent<TrialData>();
			SpecifyParameters(dc, storeData, path);
			return dc;
		}

		public FrameData InstantiateFrameData(bool storeData, string path)
		{
			FrameData dc = AddContainer("FrameData").AddComponent<FrameData>();
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
			Destroy(DataControllerContainers[name]);
			DataControllerContainers.Remove(name);
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
		public DateTime TaskStart_DateTime;
		public int TaskStart_Frame;
		public float TaskStart_UnityTime;
		public string TaskName;
	}
	public class BlockDef
	{
		public int BlockCount;
		public TrialDef[] TrialDefs;
	}
	public abstract class TrialDef
	{
		public int BlockCount, TrialCountInBlock, TrialCountInTask;
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
