using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using USE_Settings;
using TriLib;
using USE_States;
using Object = UnityEngine.Object;

namespace USE_UI{

	public class USE_Button: Button{
		
		public Vector3 ButtonPosition;
		public Vector3 ButtonScale;

		public State SetActiveOnInitialization;
		public State SetInactiveOnTermination;

		public USE_Button(){

			ButtonPosition = new Vector3(0f, 0f, 0f);
			ButtonScale= new Vector3(1f, 1f, 1f);

		}

		public USE_Button(Vector3 position, Vector3 scale){

			ButtonPosition = position;
			ButtonScale = scale;
		}

		public void defineButton(){

			this.transform.position = ButtonPosition;
			this.transform.localScale = ButtonScale;

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

		public void ToggleVisibility(bool visibility)
		{
			this.enabled = visibility;
		}

	}
}

/*namespace USE_StimulusManagement
{
	public class StimDefPointer: MonoBehaviour
	{
		public StimDef StimDef;

		public StimDefPointer()
		{
			StimDef = new StimDef();
		}

		public StimDefPointer(StimDef sd)
		{
			StimDef = sd;
		}

		public T GetStimDef<T>() where T : StimDef
		{
			return (T) StimDef;
		}
	}
	
	public class StimDef
	{

		public Dictionary<string, StimGroup> StimGroups; //stimulus type field (e.g. sample/target/irrelevant/etc)
		public string StimName;
		public string StimPath;
		public string PrefabPath;
		public string ExternalFilePath;
		public string StimFolderPath;
		public string StimExtension;
		public int StimCode; //optional, for analysis purposes
		public string StimID;
		public int[] StimDimVals; //only if this is parametrically-defined stim
		[System.NonSerialized] public GameObject StimGameObject; //not in config, generated at runtime
		public Vector3 StimLocation; //to be passed in explicitly if trial doesn't include location method
		public Vector3 StimRotation; //to be passed in explicitly if trial doesn't include location method
		public Vector2 StimScreenLocation; //screen position calculated during trial
		public float? StimScale;
		public bool StimLocationSet;
		public bool StimRotationSet;
		public float StimTrialPositiveFbProb; //set to -1 if stim is irrelevant
		public float StimTrialRewardMag; //set to -1 if stim is irrelevant
		public TokenReward[] TokenRewards;
		public int[] BaseTokenGain;
		public int[] BaseTokenLoss;
		public int TimesUsedInBlock;
		public bool isRelevant;
		public bool TriggersSonication;
		public State SetActiveOnInitialization;
		public State SetInactiveOnTermination;

		public StimDef()
		{
			StimGroups = new Dictionary<string, StimGroup>();
		}

		public StimDef(StimGroup sg, State setActiveOnInit = null, State setInactiveOnTerm = null)
		{
			if (!(string.IsNullOrEmpty(PrefabPath) | string.IsNullOrWhiteSpace(PrefabPath))  && !(string.IsNullOrEmpty(ExternalFilePath) | string.IsNullOrWhiteSpace(PrefabPath)))
				Debug.LogWarning("StimDef for stimulus " + StimName + " is being specified with both an external file path and a prefab path. Only the external filepath will be checked.");
			sg.stimDefs.Add(this);
			StimGroups = new Dictionary<string, StimGroup>();
			StimGroups.Add(sg.stimGroupName, sg);
			SetVisibilityOnOffStates(setActiveOnInit, setInactiveOnTerm);
		}

		public StimDef(StimGroup sg, int[] dimVals, State setActiveOnInit = null, State setInactiveOnTerm = null)
		{
			StimDimVals = dimVals;
			StimPath = "placeholder";
			sg.stimDefs.Add(this);
			StimGroups = new Dictionary<string, StimGroup>();
			StimGroups.Add(sg.stimGroupName, sg);
			SetVisibilityOnOffStates(setActiveOnInit, setInactiveOnTerm);
		}

		public StimDef(StimGroup sg, GameObject obj, State setActiveOnInit = null, State setInactiveOnTerm = null)
		{
			StimGameObject = obj;
			sg.stimDefs.Add(this);
			StimGroups = new Dictionary<string, StimGroup>();
			StimGroups.Add(sg.stimGroupName, sg);
			SetVisibilityOnOffStates(setActiveOnInit, setInactiveOnTerm);
		}

		public void SetVisibilityOnOffStates(State setActiveOnInit, State setInactiveOnTerm)
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

		public void ActivateOnStateInit(object sender, EventArgs e)
		{
			ToggleVisibility(true);
		}

		public void InactivateOnStateTerm(object sender, EventArgs e)
		{
			ToggleVisibility(false);
		}
		

		public StimDef CopyStimDef(StimGroup sg)
		{
			StimDef sd = new StimDef(sg);
			if (StimName != null)
				sd.StimName = StimName;
			if (BaseTokenGain != null)
				sd.BaseTokenGain = BaseTokenGain;
			if (BaseTokenLoss != null)
				sd.BaseTokenLoss = BaseTokenLoss;
			if (StimPath != null)
				sd.StimPath = StimPath;
			sd.StimCode = StimCode;
			if (StimID != null)
				sd.StimID = StimID;
			if (StimDimVals != null)
				sd.StimDimVals = StimDimVals;
			if (StimGameObject != null)
				sd.StimGameObject = StimGameObject;
			sd.StimLocation = StimLocation;
			sd.StimRotation = StimRotation;
			sd.StimScreenLocation = StimScreenLocation;
			sd.StimLocationSet = StimLocationSet;
			sd.StimRotationSet = StimRotationSet;
			sd.StimTrialPositiveFbProb = StimTrialPositiveFbProb;
			sd.StimTrialRewardMag = StimTrialRewardMag;
			if (TokenRewards != null)
				sd.TokenRewards = TokenRewards;
			sd.TimesUsedInBlock = TimesUsedInBlock;
			sd.isRelevant = isRelevant;
			return sd;
		}

		public bool ToggleVisibility(bool visibility)
		{
			bool toggled = false;
			if (StimGameObject.activeInHierarchy != visibility)
			{
				StimGameObject.SetActive(visibility);
				toggled = true;
			}

			return toggled;
		}

		public void AddToStimGroup(StimGroup sg)
		{
			if (!StimGroups.ContainsValue(sg))
			{
				sg.stimDefs.Add(this);
				StimGroups.Add(sg.stimGroupName, sg);
			}
			else
			{
				Debug.LogWarning("Attempted to add stim " + StimName + " to StimGroup " +
				                 sg.stimGroupName + " but this stimulus is already a member of this StimGroup.");
			}
		}

		public void AddToStimGroup(IEnumerable<StimGroup> stimGroups)
		{
			foreach (StimGroup sg in stimGroups)
			{
				AddToStimGroup(sg);
			}
		}

		public void RemoveFromStimGroup(StimGroup sg)
		{
			if (StimGroups.ContainsValue(sg))
			{
				// sg.stimDefs.Remove(this);
				StimGroups.Remove(sg.stimGroupName);
			}
			else
			{
				Debug.LogWarning("Attempted to remove stim " + StimName + " from StimGroup " +
				                 sg.stimGroupName + " but this stimulus is not a member of this StimGroup.");
			}
		}

		public void RemoveFromStimGroup(string stimGroupName)
		{
			if (StimGroups.ContainsKey(stimGroupName))
			{
				StimGroups[stimGroupName].stimDefs.Remove(this);
				StimGroups.Remove(stimGroupName);
			}
			else
			{
				Debug.LogWarning("Attempted to remove stim " + StimName + " from StimGroup " +
				                 stimGroupName + " but this stimulus is not a member of this StimGroup.");
			}
		}

		public void RemoveFromStimGroup(IEnumerable<StimGroup> sgs)
		{
			foreach (StimGroup sg in sgs)
				RemoveFromStimGroup(sg);
		}

		public void RemoveFromStimGroup(IEnumerable<string> sgnames)
		{
			foreach (string name in sgnames)
				RemoveFromStimGroup(name);
		}

		public GameObject Load()
		{
			if (StimGameObject != null)
			{
				Debug.LogWarning("Attempting to load stimulus " + StimName + ", but there is already a GameObject associated with this stimulus loaded.");
			}
			if (!string.IsNullOrEmpty(ExternalFilePath))
				StimGameObject = LoadExternalStimFromFile();
			else if (StimDimVals != null)
			{
				ExternalFilePath = FilePathFromDims("placeholder1", new List<string[]>(), "placeholder3");
				StimGameObject = LoadExternalStimFromFile();
			}
			else if (!string.IsNullOrEmpty(PrefabPath))
				StimGameObject = Resources.Load<GameObject>(PrefabPath);
			else
			{
				Debug.LogWarning("Attempting to load stimulus " + StimName + ", but no Unity Resources path, external file path, or dimensional values have been provided.");
				return null;
			}

			if (!string.IsNullOrEmpty(StimName))
				StimGameObject.name = StimName;

			StimGameObject.AddComponent<StimDefPointer>();
			StimGameObject.GetComponent<StimDefPointer>().StimDef = this;
			
			return StimGameObject;
		}

		public GameObject LoadPrefabFromResources(string prefabPath = "")
		{
			if (!string.IsNullOrEmpty(prefabPath))
				PrefabPath = prefabPath;
			StimGameObject = Resources.Load<GameObject>(PrefabPath);
			PositionRotationScale();
			if (!string.IsNullOrEmpty(StimName))
				StimGameObject.name = StimName;
			StimGameObject.AddComponent<StimDefPointer>();
			StimGameObject.GetComponent<StimDefPointer>().StimDef = this;
			return StimGameObject;
		}

		public GameObject LoadExternalStimFromFile(string stimFilePath = "")
		{
			if (!string.IsNullOrEmpty(stimFilePath))
			{
				ExternalFilePath = stimFilePath;
			}

			if (!string.IsNullOrEmpty(StimFolderPath) && !ExternalFilePath.StartsWith(StimFolderPath))
			{
				if (!ExternalFilePath.StartsWith(Path.DirectorySeparatorChar.ToString()) &&
				    !StimFolderPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
					ExternalFilePath = StimFolderPath + Path.DirectorySeparatorChar + ExternalFilePath;
				else if (ExternalFilePath.StartsWith(Path.DirectorySeparatorChar.ToString()) &&
				         StimFolderPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
					ExternalFilePath = StimFolderPath + ExternalFilePath.Substring(1);
				else
					ExternalFilePath = StimFolderPath + ExternalFilePath;
			}

			if (!string.IsNullOrEmpty(StimExtension) && !ExternalFilePath.EndsWith(StimExtension))
			{
				if (!StimExtension.StartsWith("."))
					ExternalFilePath = ExternalFilePath + "." + StimExtension;
				else
					ExternalFilePath = ExternalFilePath + StimExtension;
			}
			StimGameObject = LoadModel();
			PositionRotationScale();
			if (!string.IsNullOrEmpty(StimName))
				StimGameObject.name = StimName;
			StimGameObject.AddComponent<StimDefPointer>();
			StimGameObject.GetComponent<StimDefPointer>().StimDef = this;
			return StimGameObject;
		}

		public void Destroy()
		{
			StimGroup[] sgs = StimGroups.Values.ToArray();
			for (int iG = 0; iG < sgs.Length; iG++)
				RemoveFromStimGroup(sgs[iG]);

			Object.Destroy(StimGameObject);
			if (SetActiveOnInitialization != null)
			{
				SetActiveOnInitialization.StateInitializationFinished -= ActivateOnStateInit;
				SetActiveOnInitialization = null;
			}

			if (SetInactiveOnTermination != null)
			{
				SetInactiveOnTermination.StateTerminationFinished -= InactivateOnStateTerm;
				SetInactiveOnTermination = null;
			}
		}


		public void AddMesh()
		{
			foreach (var m in StimGameObject.transform.GetComponentsInChildren<MeshRenderer>())
			{
				m.gameObject.AddComponent(typeof(MeshCollider));
			}
		}

		public GameObject LoadModel(bool visibiility = false)
		{
			using (var assetLoader = new AssetLoader())
			{
				try
				{
					var assetLoaderOptions = AssetLoaderOptions.CreateInstance();
					assetLoaderOptions.AutoPlayAnimations = true;
					assetLoaderOptions.AddAssetUnloader = true;
					StimGameObject = assetLoader.LoadFromFile(ExternalFilePath, assetLoaderOptions);
				}
				catch (System.Exception e)
				{
					Debug.Log(ExternalFilePath);
					Debug.LogError(e.ToString());
					return null;
				}
			}

			PositionRotationScale();
			AddMesh();
			ToggleVisibility(visibiility);
			return StimGameObject;
		}

		private void PositionRotationScale()
		{
			StimGameObject.transform.position = StimLocation;
			StimGameObject.transform.rotation = Quaternion.Euler(StimRotation);

			if (StimScale == null)
				StimScale = 1;
			
			StimGameObject.transform.localScale = new Vector3(StimScale.Value, StimScale.Value, StimScale.Value);
		}


		public string FilePathFromDims(string folderPath, IEnumerable<string[]> featureNames,
			string neutralPatternedColorName)
		{
			//UnityEngine.Debug.Log(featureVals);
			string filename = "";
			for (int iDim = 0; iDim < featureNames.Count(); iDim++)
			{
				filename += featureNames.ElementAt(iDim)[StimDimVals[iDim]];
				if (iDim < 4)
					filename = filename + "_";
			}

			if (StimDimVals[1] != 0 && StimDimVals[2] == 0)
			{
				//special case for patterned Quaddle without color
				int colour = filename.IndexOf('C');
				string c1 = filename.Substring(colour, 16);
				filename = filename.Replace(c1, neutralPatternedColorName);
			}
			else if (StimDimVals[1] == 0)
			{
				//special case where colours are solid for neutral pattern
				int colour = filename.IndexOf('C');
				string c1 = filename.Substring(colour + 1, 7);
				string c2 = filename.Substring(colour + 9, 7);
				filename = filename.Replace(c2, c1);
			}

			return filename;

			//return CheckFileName(folderPath, filename);
		}
	}


	[System.Serializable]
	public class StimGroup
	{
		public List<StimDef> stimDefs;
		public string stimGroupName;
		public State SetActiveOnInitialization;
		public State SetInactiveOnTermination;

		public StimGroup(string groupName, State setActiveOnInit = null, State setInactiveOnTerm = null)
		{
			stimGroupName = groupName;
			stimDefs = new List<StimDef>();
			SetVisibilityOnOffStates(setActiveOnInit, setInactiveOnTerm);
		}
		
		public StimGroup(string groupName, IEnumerable<StimDef> stims, State setActiveOnInit = null, State setInactiveOnTerm = null)
		{
			stimGroupName = groupName;
			stimDefs = new List<StimDef>();
			AddStims(stims);
			SetVisibilityOnOffStates(setActiveOnInit, setInactiveOnTerm);
		}

		public StimGroup(string groupName, IEnumerable<GameObject> gos, State setActiveOnInit = null, State setInactiveOnTerm = null)
		{
			stimGroupName = groupName;
			stimDefs = new List<StimDef>();
			AddStims(gos);
			SetVisibilityOnOffStates(setActiveOnInit, setInactiveOnTerm);
		}

		public StimGroup(string groupName, IEnumerable<int[]> dimValGroup, string folderPath, IEnumerable<string[]> featureNames, string neutralPatternedColorName, Camera cam, float scale = 1, State setActiveOnInit = null, State setInactiveOnTerm = null) 
		{
			stimGroupName = groupName;
			stimDefs = new List<StimDef>();
			AddStims(dimValGroup);
			SetVisibilityOnOffStates(setActiveOnInit, setInactiveOnTerm);
		}

		public StimGroup(string groupName, string TaskName, string stimDefFilePath, State setActiveOnInit = null, State setInactiveOnTerm = null)
		{
			stimGroupName = groupName;
			stimDefs = new List<StimDef>();
			AddStims(TaskName, stimDefFilePath);
			SetVisibilityOnOffStates(setActiveOnInit, setInactiveOnTerm);
		}

		public StimGroup(string groupName, StimGroup sgOrig, IEnumerable<int> stimSubsetIndices, State setActiveOnInit = null, State setInactiveOnTerm = null)
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

		public void AddStims(int[] dimVals)
		{
			StimDef stim = new StimDef(this, dimVals);
			// stim.ToggleVisibility(false);
		}

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
			{
				sd.AddToStimGroup(this);
				// sd.ToggleVisibility(false);
			}
		}

		public void AddStims(StimGroup sgOrig, IEnumerable<int> stimSubsetIndices)
		{
			foreach (int index in stimSubsetIndices)
			{
				sgOrig.stimDefs[index].AddToStimGroup(this);
			}
		}

		public void RemoveStims(StimDef stim)
		{
			stim.RemoveFromStimGroup(this);
		}
		
		public void RemoveStims(IEnumerable<StimDef> stims)
		{
			foreach (StimDef stim in stims)
			{
				stim.RemoveFromStimGroup(this);
			}
		}
		
		
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
			Debug.LogWarning("Attempted to remove StimDef with dimensional values " + dimVals + " from StimGroup " + stimGroupName + 
			                 ", but this StimGroup does not include a StimDef with these dimensional values.");
		}

		public void RemoveStims(IEnumerable<int[]> dimValGroup)
		{
			foreach (int[] dimVals in dimValGroup)
			{
				RemoveStims(dimVals);
			}
		}

		public void RemoveStims(StimGroup sgOrig, IEnumerable<int> stimSubsetIndices)
		{
			foreach (int index in stimSubsetIndices)
			{
				sgOrig.stimDefs[index].RemoveFromStimGroup(this);
			}
		}

		public void LoadStims()
		{
			foreach(StimDef sd in stimDefs)
				sd.Load();
		}

		public void LoadPrefabStimFromResources()
		{
			foreach (StimDef sd in stimDefs)
				sd.LoadPrefabFromResources();
		}

		public void LoadExternalStims()
		{
			foreach (StimDef sd in stimDefs)
				sd.LoadExternalStimFromFile();
		}

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
		}

		public void SetLocations(IEnumerable<Vector3> locs)
		{
			Vector3[] LocArray = locs.ToArray();
			if (LocArray.Length == stimDefs.Count)
			{
				for (int iL = 0; iL < LocArray.Length; iL++)
					stimDefs[iL].StimLocation = LocArray[iL];
			}
			else
			{
				Debug.LogError("Attempted to set the locations of stims in StimGroup " + stimGroupName +
				               ", but there are " + stimDefs.Count + " stimuli in this group and " + LocArray.Length +
				               " locations were given.");
			}
		}
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
		}
		
	}
}

*/
