using System;
using System.Collections.Generic;
//using System.IO;
using System.Linq;
//using System.Reflection;
//using System.Security.Policy;
using JetBrains.Annotations;
using UnityEngine;
using USE_Settings;
//using TriLib;
//using UnityEngine.UI;
using USE_States;
//using Object = UnityEngine.Object;
//using USE_ExperimentTemplate_Classes;
using System.Collections;
//using USE_ExperimentTemplate_Session;
//using System.Threading.Tasks;
//using UnityEngine.UI;

namespace USE_StimulusManagement
{
	[System.Serializable]
	public class StimGroup
	{
		public List<StimDef> stimDefs;
		public string stimGroupName;
		public State SetActiveOnInitialization;
		public State SetInactiveOnTermination;
		public bool IsActive;

		public StimGroup(string groupName, State setActiveOnInit = null, State setInactiveOnTerm = null)
		{
			stimGroupName = groupName;
			stimDefs = new List<StimDef>();
			SetVisibilityOnOffStates(setActiveOnInit, setInactiveOnTerm);
		}

		public StimGroup(string groupName, IEnumerable<StimDef> stims, State setActiveOnInit = null,
			State setInactiveOnTerm = null)
		{
			stimGroupName = groupName;
			stimDefs = new List<StimDef>();
			AddStims(stims);
			SetVisibilityOnOffStates(setActiveOnInit, setInactiveOnTerm);
		}

		/* UNUSED
		public StimGroup(string groupName, IEnumerable<GameObject> gos, State setActiveOnInit = null,
			State setInactiveOnTerm = null)
		{
			stimGroupName = groupName;
			stimDefs = new List<StimDef>();
			AddStims(gos);
			SetVisibilityOnOffStates(setActiveOnInit, setInactiveOnTerm);
		}*/

		public StimGroup(string groupName, IEnumerable<int[]> dimValGroup, string folderPath,
			IEnumerable<string[]> featureNames, string neutralPatternedColorName, Camera cam, float scale = 1,
			State setActiveOnInit = null, State setInactiveOnTerm = null)
		{
			stimGroupName = groupName;
			stimDefs = new List<StimDef>();
			AddStims(dimValGroup);
			SetVisibilityOnOffStates(setActiveOnInit, setInactiveOnTerm);
		}

		public StimGroup(string groupName, string TaskName, string stimDefFilePath, State setActiveOnInit = null,
			State setInactiveOnTerm = null)
		{
			stimGroupName = groupName;
			stimDefs = new List<StimDef>();
			AddStims(TaskName, stimDefFilePath);
			SetVisibilityOnOffStates(setActiveOnInit, setInactiveOnTerm);
		}

		public StimGroup(string groupName, StimGroup sgOrig, IEnumerable<int> stimSubsetIndices,
			State setActiveOnInit = null, State setInactiveOnTerm = null)
		{
			stimGroupName = groupName;
			stimDefs = new List<StimDef>();
			AddStims(sgOrig, stimSubsetIndices);
			SetVisibilityOnOffStates(setActiveOnInit, setInactiveOnTerm);
		}


		public void SetVisibilityOnOffStates(State setActiveOnInit = null, State setInactiveOnTerm = null)
		{
			if (setActiveOnInit != null)
			{
				SetActiveOnInitialization = setActiveOnInit;
				SetActiveOnInitialization.StateInitializationFinished += ActivateOnStateInit;
			}

			if (setInactiveOnTerm != null)
			{
				SetInactiveOnTermination = setInactiveOnTerm;
				SetInactiveOnTermination.StateTerminationFinished += InactivateOnStateTerm;
			}
		}

		private void ActivateOnStateInit(object sender, EventArgs e)
		{
			ToggleVisibility(true);
		}

		private void InactivateOnStateTerm(object sender, EventArgs e)
		{
			ToggleVisibility(false);
		}

		public void AddStims(StimDef stim)
		{
			stim.AddToStimGroup(this);
			// stim.ToggleVisibility(false);
		}

		public void AddStims(IEnumerable<StimDef> stims)
		{
			foreach (StimDef stim in stims)
			{
				stim.AddToStimGroup(this);
				// stim.ToggleVisibility(false);
			}
		}

		public void AddStims(GameObject go)
		{
			StimDef stim = new StimDef(this, go);
		}

		public void AddStims(IEnumerable<GameObject> gos)
		{
			foreach (GameObject go in gos)
			{
				StimDef stim = new StimDef(this, go);
			}
		}

		/* UNUSED
		public void AddStims(int[] dimVals)
		{
			StimDef stim = new StimDef(this, dimVals);
			// stim.ToggleVisibility(false);
		}
		*/

		public void AddStims(IEnumerable<int[]> dimValGroup)
		{
			foreach (int[] dimVals in dimValGroup)
			{
				StimDef stim = new StimDef(this, dimVals);
				// stim.ToggleVisibility(false);
			}
		}

		public void AddStims(string TaskName, string stimDefFilePath)
		{
			SessionSettings.ImportSettings_SingleTypeArray<StimDef>(TaskName + "_StimDefs", stimDefFilePath);
			List<StimDef> sds = (List<StimDef>)SessionSettings.Get(TaskName + "_StimDefs");
			foreach (StimDef sd in sds)
				sd.AddToStimGroup(this);
		}

		public void AddStims(StimGroup sgOrig, IEnumerable<int> stimSubsetIndices)
		{
			foreach (int index in stimSubsetIndices)
			{
				sgOrig.stimDefs[index].AddToStimGroup(this);
				if (sgOrig.stimDefs[index].StimIndex != index)
					Debug.LogError("Stim at StimDef index " + index + " does not correspond to the listed StimIndex: " +
					               sgOrig.stimDefs[index].StimIndex);
			}
		}

/* UNUSED
public void RemoveStims(StimDef stim)
{
	stim.RemoveFromStimGroup(this);
}*/

/* UNUSED
public void RemoveStims(IEnumerable<StimDef> stims)
{
	foreach (StimDef stim in stims)
	{
		stim.RemoveFromStimGroup(this);
	}
}*/

		/* UNUSED
		public void RemoveStims(int[] dimVals)
		{
			foreach (StimDef sd in stimDefs)
			{
				if (sd.StimDimVals == dimVals)
				{
					sd.RemoveFromStimGroup(this);
					return;
				}
			}

			Debug.LogWarning("Attempted to remove StimDef with dimensional values " + dimVals + " from StimGroup " +
			                 stimGroupName +
			                 ", but this StimGroup does not include a StimDef with these dimensional values.");
		}
		*/

/* UNUSED
public void RemoveStims(IEnumerable<int[]> dimValGroup)
{
	foreach (int[] dimVals in dimValGroup)
	{
		RemoveStims(dimVals);
	}
}*/

/* UNUSED
public void RemoveStims(StimGroup sgOrig, IEnumerable<int> stimSubsetIndices)
{
	foreach (int index in stimSubsetIndices)
	{
		sgOrig.stimDefs[index].RemoveFromStimGroup(this);
	}
}*/

		public IEnumerator LoadStims()
		{
			foreach (StimDef sd in stimDefs)
			{
				if (sd.StimGameObject == null){
					yield return CoroutineHelper.StartCoroutine(sd.Load(stimResultGO =>
					{
						if (stimResultGO != null)
							sd.StimGameObject = stimResultGO;
						else
							Debug.Log("LOAD COROUTINE - STIM RESULT GAMEOBJECT IS NULL!!!!!!!!!!!!");
					}));
				}
			}
		}

/* UNUSED
public void LoadPrefabStimFromResources()
{
	foreach (StimDef sd in stimDefs)
		sd.LoadPrefabFromResources();
}*/

/* UNUSED
public void LoadExternalStims()
{
	foreach (StimDef sd in stimDefs)
		sd.LoadExternalStimFromFile();
}*/


		public void DestroyStimGroup()
		{
			int nStims = stimDefs.Count;
			for (int iS = 0; iS < nStims; iS++)
			{
				StimDef sd = stimDefs[0];
				sd.RemoveFromStimGroup(this);
				if (sd.SetActiveOnInitialization != null)
				{
					sd.SetActiveOnInitialization.StateInitializationFinished -= sd.ActivateOnStateInit;
					sd.SetActiveOnInitialization = null;
				}

				if (sd.SetInactiveOnTermination != null)
				{
					sd.SetInactiveOnTermination.StateTerminationFinished -= sd.InactivateOnStateTerm;
					sd.SetInactiveOnTermination = null;
				}

				stimDefs.RemoveAt(0);
				GameObject.Destroy(sd.StimGameObject);
			}
		}

		public void ToggleVisibility(bool visibility)
		{
			foreach (StimDef stim in stimDefs)
			{
				stim.ToggleVisibility(visibility);
			}

			IsActive = visibility;
		}

		public void SetLocations(IEnumerable<Vector3> locs)
		{
			Vector3[] LocArray = locs.ToArray();
			if (LocArray.Length == stimDefs.Count)
			{
				for (int iL = 0; iL < LocArray.Length; iL++)
				{
					stimDefs[iL].StimLocation = LocArray[iL];
				}
			}
			else
			{
				Debug.LogError("Attempted to set the locations of stims in StimGroup " + stimGroupName +
				               ", but there are " + stimDefs.Count + " stimuli in this group and " + LocArray.Length +
				               " locations were given.");
			}
		}

/* UNUSED
public void SetRotations(IEnumerable<Vector3> rots)
{
	Vector3[] rotArray = rots.ToArray();
	if (rotArray.Length == stimDefs.Count)
	{
		for (int iL = 0; iL < rotArray.Length; iL++)
			stimDefs[iL].StimLocation = rotArray[iL];
	}
	else
	{
		Debug.LogError("Attempted to set the rotations of stims in StimGroup " + stimGroupName +
		               ", but there are " + stimDefs.Count + " stimuli in this group and " + rotArray.Length +
		               " rotations were given.");
	}
}*/
	}
}