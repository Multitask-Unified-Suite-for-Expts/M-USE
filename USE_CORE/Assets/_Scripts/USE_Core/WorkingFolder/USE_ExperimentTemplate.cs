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
