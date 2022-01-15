using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using USE_Settings;
using TriLib;

namespace USE_StimulusManagement
{
	public class StimDef
	{

		public Dictionary<string, StimGroup> StimGroups; //stimulus type field (e.g. sample/target/irrelevant/etc)
		public string StimName;
		public string StimPath;
		public string PrefabPath;
		public string ExternalFilePath;
		public int StimCode; //optional, for analysis purposes
		public string StimID;
		public int[] StimDimVals; //only if this is parametrically-defined stim
		[System.NonSerialized] public GameObject StimGameObject; //not in config, generated at runtime
		public Vector3 StimLocation; //to be passed in explicitly if trial doesn't include location method
		public Vector3 StimRotation; //to be passed in explicitly if trial doesn't include location method
		public Vector2 StimScreenLocation; //screen position calculated during trial
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

		public StimDef()
		{
			StimGroups = new Dictionary<string, StimGroup>();
		}

		public StimDef(StimGroup sg)
		{
			if (!(string.IsNullOrEmpty(PrefabPath) | string.IsNullOrWhiteSpace(PrefabPath))  && !(string.IsNullOrEmpty(ExternalFilePath) | string.IsNullOrWhiteSpace(PrefabPath)))
				Debug.LogWarning("StimDef for stimulus " + StimName + " is being specified with both an external file path and a prefab path. Only the external filepath will be checked.");
			sg.stimDefs.Add(this);
			StimGroups = new Dictionary<string, StimGroup>();
			StimGroups.Add(sg.stimGroupName, sg);
		}

		public StimDef(StimGroup sg, int[] dimVals)
		{
			StimDimVals = dimVals;
			StimPath = "placeholder";
			sg.stimDefs.Add(this);
			StimGroups = new Dictionary<string, StimGroup>();
			StimGroups.Add(sg.stimGroupName, sg);
		}

		public StimDef(StimGroup sg, GameObject obj)
		{
			StimGameObject = obj;
			sg.stimDefs.Add(this);
			StimGroups = new Dictionary<string, StimGroup>();
			StimGroups.Add(sg.stimGroupName, sg);
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
				sg.stimDefs.Remove(this);
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
				return StimGameObject;
			}
			if (!string.IsNullOrEmpty(ExternalFilePath))
				StimGameObject = LoadModel();
			else if (!string.IsNullOrEmpty(PrefabPath))
				StimGameObject = Resources.Load<GameObject>(StimPath);
			else if (StimDimVals != null)
			{
				StimPath = FilePathFromDims("placeholder1", new List<string[]>(), "placeholder3");
				StimGameObject = LoadModel();
			}
			else
			{
				
				Debug.LogWarning("Attempting to load stimulus " + StimName + ", but no Unity Resources path, external file path, or dimensional values have been provided.");
				return null;
			}
			return StimGameObject;
		}

		public void Destroy()
		{
			foreach (StimGroup sg in StimGroups.Values)
				RemoveFromStimGroup(sg);
			Object.Destroy(StimGameObject);
		}


		public void AddMesh()
		{
			foreach (var m in StimGameObject.transform.GetComponentsInChildren<MeshRenderer>())
			{
				m.gameObject.AddComponent(typeof(MeshCollider));
			}
		}

		public GameObject LoadModel(float scale = 1, bool visibiility = false)
		{
			using (var assetLoader = new AssetLoader())
			{
				try
				{
					var assetLoaderOptions = AssetLoaderOptions.CreateInstance();
					assetLoaderOptions.AutoPlayAnimations = true;
					assetLoaderOptions.AddAssetUnloader = true;
					StimGameObject = assetLoader.LoadFromFile(StimPath, assetLoaderOptions);
				}
				catch (System.Exception e)
				{
					Debug.Log(StimPath);
					Debug.LogError(e.ToString());
					return null;
				}
			}

			AddMesh();
			StimGameObject.transform.position = StimLocation;
			StimGameObject.transform.rotation = Quaternion.Euler(StimRotation);
			StimGameObject.transform.localScale = new Vector3(scale, scale, scale);
			ToggleVisibility(visibiility);
			return StimGameObject;
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

		public StimGroup(string groupName)
		{
			stimGroupName = groupName;
			stimDefs = new List<StimDef>();
		}
		
		public StimGroup(string groupName, IEnumerable<StimDef> stims)
		{
			stimGroupName = groupName;
			stimDefs = new List<StimDef>();
			AddStims(stims);
		}

		public StimGroup(string groupName, IEnumerable<int[]> dimValGroup, string folderPath, IEnumerable<string[]> featureNames, string neutralPatternedColorName, Camera cam, float scale = 1) 
		{
			stimGroupName = groupName;
			stimDefs = new List<StimDef>();
			AddStims(dimValGroup);
		}

		public StimGroup(string groupName, string TaskName, string stimDefFilePath)
		{
			stimGroupName = groupName;
			stimDefs = new List<StimDef>();
			AddStims(TaskName, stimDefFilePath);
		}

		public StimGroup(string groupName, StimGroup sgOrig, IEnumerable<int> stimSubsetIndices)
		{
			stimGroupName = groupName;
			stimDefs = new List<StimDef>();
			AddStims(sgOrig, stimSubsetIndices);
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
				// sgOrig.stimDefs[index].ToggleVisibility(false);
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

		public void DestroyStimGroup()
		{
			foreach (StimDef stim in stimDefs)
				stim.Destroy();
		}

		public void ToggleVisibility(bool visibility)
		{
			foreach (StimDef stim in stimDefs)
			{
				stim.ToggleVisibility(visibility);
			}
		}
		
		
	}
}
