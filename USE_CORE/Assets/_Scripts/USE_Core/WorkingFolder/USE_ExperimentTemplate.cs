using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using USE_States;
using USE_Data;
using USE_Settings;
using USE_StimulusManagement;
using ConfigDynamicUI;
using USE_ExperimenterDisplay;
using USE_ExperimentTemplate_Classes;

// using USE_TasksCustomTypes;

namespace USE_ExperimentTemplate
{
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

		public void AddEventCodeColumns()
		{
			AddDatum("EventCodes", () => string.Join(",", taskLevel.EventCodeManager.GetBuffer("sent")));
			AddDatum("SplitEventCodes", () => string.Join(",", taskLevel.EventCodeManager.GetBuffer("split")));
			AddDatum("PreSplitEventCodes", () => string.Join(",", taskLevel.EventCodeManager.GetBuffer("presplit")));
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
		public bool SerialPortActive, SyncBoxActive, EventCodesActive, RewardPulsesActive, SonicationActive;
		public string EyetrackerType, SelectionType;
		
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
	public class BlockDef
	{
		public int BlockCount;
		public TrialDef[] TrialDefs;
		public int? TotalTokensNum;
		public int? MinTrials, MaxTrials;

		public virtual void GenerateTrialDefsFromBlockDef()
		{
		}

		public virtual void AddToTrialDefsFromBlockDef()
		{
		}

		public virtual void BlockInitializationMethod()
		{
		}
	}
	public abstract class TrialDef
	{
		public int BlockCount, TrialCountInBlock, TrialCountInTask;
		public TrialStims TrialStims;
	}

}
