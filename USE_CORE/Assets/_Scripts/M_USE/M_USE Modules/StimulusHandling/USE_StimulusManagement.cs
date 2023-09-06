using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using USE_Settings;
using TriLib;
using UnityEngine.UI;
using USE_States;
using Object = UnityEngine.Object;
using USE_ExperimentTemplate_Classes;
using System.Collections;


namespace USE_StimulusManagement
{
	public class StimDef
	{
		public Dictionary<string, StimGroup> StimGroups; //stimulus type field (e.g. sample/target/irrelevant/etc)
		public string StimName;
		public string StimPath;
		public string PrefabPath;
		public string FileName;
		public string StimFolderPath;
		public string StimExtension;
		public int StimCode; //optional, for analysis purposes
		public int StimIndex;
		public string StimID;
		public int[] StimDimVals; //only if this is parametrically-defined stim
		[System.NonSerialized] public GameObject StimGameObject; //not in config, generated at runtime
		public GameObject CanvasGameObject;
		public Vector3 StimLocation; //to be passed in explicitly if trial doesn't include location method
		public Vector3 StimRotation; //to be passed in explicitly if trial doesn't include location method
		public Vector2 StimScreenLocation; //screen position calculated during trial
		public float? StimScale;
		public bool StimLocationSet;
		public bool StimRotationSet;
		public float StimTrialPositiveFbProb; //set to -1 if stim is irrelevant
		public int StimTokenRewardMag; //set to -1 if stim is irrelevant
		public Reward[] TokenRewards;
		public Reward[] PulseRewards;
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
			if (!(string.IsNullOrEmpty(PrefabPath) | string.IsNullOrWhiteSpace(PrefabPath))  && !(string.IsNullOrEmpty(FileName) | string.IsNullOrWhiteSpace(PrefabPath)))
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

		public StimDef CopyStimDef()
		{
			StimDef sd = new StimDef();
			if (StimName != null)
				sd.StimName = StimName;
			if (StimPath != null)
				sd.StimPath = StimPath;
			if (PrefabPath != null)
				sd.PrefabPath = PrefabPath;
			if (FileName != null)
				sd.FileName = FileName;
			if (StimFolderPath != null)
				sd.StimFolderPath = StimFolderPath;
			if (StimExtension != null)
				sd.StimExtension = StimExtension;
			if (StimID != null)
				sd.StimID = StimID;
			if (StimDimVals != null)
				sd.StimDimVals = StimDimVals;

			if (CanvasGameObject != null)
				sd.CanvasGameObject = CanvasGameObject;

			sd.StimCode = StimCode;
			sd.StimLocation = StimLocation;
			sd.StimRotation = StimRotation;
			sd.StimScreenLocation = StimScreenLocation;
			sd.StimScale = StimScale;
			sd.StimLocationSet = StimLocationSet;
			sd.StimRotationSet = StimRotationSet;
			sd.StimTrialPositiveFbProb = StimTrialPositiveFbProb;
			sd.StimTokenRewardMag = StimTokenRewardMag;
			sd.StimIndex = StimIndex;
			if (TokenRewards != null)
				sd.TokenRewards = TokenRewards;
			if (BaseTokenGain != null)
				sd.BaseTokenGain = BaseTokenGain;
			if (BaseTokenLoss != null)
				sd.BaseTokenLoss = BaseTokenLoss;
			sd.TimesUsedInBlock = TimesUsedInBlock;
			sd.isRelevant = isRelevant;
			return sd;
		}
		
		public StimDef CopyStimDef(StimGroup sg)
		{
			StimDef sd = CopyStimDef();
			sd.AddToStimGroup(sg);
			return sd;
		}

		public StimDef CopyStimDef<T>() where T : StimDef, new()
		{
			T sd = new T();
			if (StimName != null)
				sd.StimName = StimName;
			if (StimPath != null)
				sd.StimPath = StimPath;
			if (PrefabPath != null)
				sd.PrefabPath = PrefabPath;
			if (FileName != null)
				sd.FileName = FileName;
			if (StimFolderPath != null)
				sd.StimFolderPath = StimFolderPath;
			if (StimExtension != null)
				sd.StimExtension = StimExtension;
			if (StimID != null)
				sd.StimID = StimID;
			if (StimDimVals != null)
				sd.StimDimVals = StimDimVals;

			if (CanvasGameObject != null)
				sd.CanvasGameObject = CanvasGameObject;

			sd.StimCode = StimCode;
			sd.StimLocation = StimLocation;
			sd.StimRotation = StimRotation;
			sd.StimScreenLocation = StimScreenLocation;
			sd.StimScale = StimScale;
			sd.StimLocationSet = StimLocationSet;
			sd.StimRotationSet = StimRotationSet;
			sd.StimTrialPositiveFbProb = StimTrialPositiveFbProb;
			sd.StimTokenRewardMag = StimTokenRewardMag;
			sd.StimIndex = StimIndex;
			if (TokenRewards != null)
				sd.TokenRewards = TokenRewards;
			if (BaseTokenGain != null)
				sd.BaseTokenGain = BaseTokenGain;
			if (BaseTokenLoss != null)
				sd.BaseTokenLoss = BaseTokenLoss;
			sd.TimesUsedInBlock = TimesUsedInBlock;
			sd.isRelevant = isRelevant;
			return sd;
		}

		public void ToggleVisibility(bool visibility)
		{
            StimGameObject.SetActive(visibility);
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



        public IEnumerator Load(Action<GameObject> callback)
        {
			SessionValues.Using2DStim = FileName.Contains("png");

			if (SessionValues.UsingDefaultConfigs)
			{
                StimGameObject = LoadPrefabFromResources();
				callback?.Invoke(StimGameObject);
			}
            else
            {
                if (!string.IsNullOrEmpty(FileName))
                {
					if (SessionValues.UsingServerConfigs)
					{
						yield return CoroutineHelper.StartCoroutine(LoadExternalStimFromServer(returnedStimGO =>
						{
							if (returnedStimGO != null)
								StimGameObject = returnedStimGO;
							else
								Debug.Log("RETURNED STIM GAMEOBJECT IS NULL!!!!!!");
						}));
					}
					else if(SessionValues.UsingLocalConfigs)
					{
						StimGameObject = LoadExternalStimFromFile();
					}
                }
                else if (StimDimVals != null)
                {
                    FileName = FilePathFromDims("placeholder1", new List<string[]>(), "placeholder3");
                    StimGameObject = LoadExternalStimFromFile();
                }
                else if (!string.IsNullOrEmpty(PrefabPath)) //this one neccessary?
					StimGameObject = Resources.Load<GameObject>(PrefabPath);
                else
                {
                    Debug.LogWarning("Attempting to load stimulus " + StimName + ", but no Unity Resources path, external file path, or dimensional values have been provided.");
					callback?.Invoke(null);
                }


                if (!string.IsNullOrEmpty(StimName))
                    StimGameObject.name = StimName;
                else
                {
                    string[] FileNameStrings;
                    if (FileName.Contains("\\"))
                        FileNameStrings = FileName.Split('\\');
                    else
                        FileNameStrings = FileName.Split('/');

                    string splitString = FileNameStrings[FileNameStrings.Length - 1];
                    StimGameObject.name = splitString.Split('.')[0];
                }

				callback?.Invoke(StimGameObject);
            }
        }


        private List<GameObject> GetAllObjectsInHierarchy(GameObject parentObject)
		{
			List<GameObject> objects = new List<GameObject>();
			objects.Add(parentObject);
			foreach (Transform child in parentObject.transform)
			{
				// objects.Add(child.gameObject);
				List<GameObject> childChildren = GetAllObjectsInHierarchy(child.gameObject);
				objects.AddRange(childChildren);
			}
			return objects;
		}

		public void AssignStimDefPointeToObjectHierarchy(GameObject parentObject, StimDef sd)
		{
			List<GameObject> objectsInHierarchy = GetAllObjectsInHierarchy(parentObject);
			foreach (GameObject obj in objectsInHierarchy)
			{
				obj.AddComponent<StimDefPointer>();
				obj.GetComponent<StimDefPointer>().StimDef = this;
			}
		}
		
		//MAY NEED TO CHANGE THE PATH FOR WHEN NOT IN EDITOR!
		public GameObject LoadPrefabFromResources()
		{
			string fullPath = "DefaultResources/Stimuli/" + FileName.Split('.')[0];
			StimGameObject = LoadModel(fullPath);

			PositionRotationScale();
			if (!string.IsNullOrEmpty(StimName))
				StimGameObject.name = StimName;
			AssignStimDefPointeToObjectHierarchy(StimGameObject, this);
			return StimGameObject;
		}


        private string WriteStimToPersistantDataPath(byte[] stimFileBytes)
        {
            string folderPath = Application.persistentDataPath + Path.DirectorySeparatorChar + "Stimuli";

            if (!Directory.Exists(folderPath))
				Directory.CreateDirectory(folderPath);
			
            string stimPath = folderPath + Path.DirectorySeparatorChar + FileName;
			if(!File.Exists(stimPath))
			{
                Debug.Log("WRITING STIM TO PERSISTANT DATA PATH!");
                File.WriteAllBytes(stimPath, stimFileBytes);
				Debug.Log("DONE WRITING BYTES TO PERSISTANT DATA PATH!");
            }

            return stimPath;
        }

        public IEnumerator LoadExternalStimFromServer(Action<GameObject> callback) //ONLY WORKS FOR 2D Stim
		{
			string filePath = $"Resources/Stimuli/{FileName}"; //may need to document this or make it configurable or something

			yield return CoroutineHelper.StartCoroutine(ServerManager.LoadTextureFromServer(filePath, textureResult =>
			{
				if (textureResult != null)
				{
					if (SessionValues.Using2DStim)
					{
						StimGameObject = new GameObject();
						StimGameObject.SetActive(false);
						RawImage image = StimGameObject.AddComponent<RawImage>();
						image.texture = textureResult;
						if (CanvasGameObject != null)
							StimGameObject.GetComponent<RectTransform>().SetParent(CanvasGameObject.GetComponent<RectTransform>());
					}

					PositionRotationScale();
					if (!string.IsNullOrEmpty(StimName))
						StimGameObject.name = StimName;
					AssignStimDefPointeToObjectHierarchy(StimGameObject, this);
					callback?.Invoke(StimGameObject);
				}
				else
					Debug.Log("LOAD TEXTURE RESULT IS NULL!");
			}));
		}

		public GameObject LoadExternalStimFromFile(string stimFilePath = "")
		{
			//add StimExtesion to file path if it doesn't already contain it
			if (!string.IsNullOrEmpty(StimExtension) && !FileName.EndsWith(StimExtension))
			{
				if (!StimExtension.StartsWith("."))
					FileName = FileName + "." + StimExtension;
				else
					FileName = FileName + StimExtension;
			}
			if(string.IsNullOrEmpty(StimExtension))

			{
				StimExtension = Path.GetExtension(FileName);
			}			//by default stimFilePath argument is empty, and files are found using StimFolderPath + ExternalFilePath
			//so usually this first if statement is never called - used for cases where we might want to find a file in an unusual location
			if (!string.IsNullOrEmpty(stimFilePath))
			{
				FileName = stimFilePath;
				//should add a method to check this file exists and return error if not
			}
			//we will only use StimFolderPath if ExternalFilePath doesn't already contain it
			else if (!string.IsNullOrEmpty(StimFolderPath) && !FileName.StartsWith(StimFolderPath))
			{				
				//this checking needs to be done during task setup - check each stim exists at start of session instead of at start of each trial
				List<string> filenames = RecursiveFileFinder.FindFile(StimFolderPath, FileName, StimExtension);
				if (filenames.Count == 1)
				{
					FileName = filenames[0];
				}
				else if (filenames.Count == 0)
					Debug.LogError("Attempted to load stimulus " + FileName + " in folder " + 
					               StimFolderPath + "but no file matching this pattern was found in this folder or subdirectories.");
				else
					Debug.LogError("Attempted to load stimulus " + FileName + " in folder " + 
					               StimFolderPath + "but multiple files matching this pattern were found in this folder or subdirectories.");
			}
			else
			{
				//if ExternalFilePath already contains the StimFolerPath string, do not change it,
				//but should also have method to check this file exists
			}
			
			//switch case based on StimDef filetype
			if (String.IsNullOrEmpty(StimExtension))
			{
				//parse filename for stimExtension and assign
			}
			
			switch (StimExtension.ToLower())
			{
				case ".fbx":
					StimGameObject = LoadModel(FileName);
					PositionRotationScale();
					break;
				case ".png":
					StimGameObject = new GameObject();//give it name
					RawImage stimGOImage = StimGameObject.AddComponent<RawImage>();
					stimGOImage.texture = LoadPNG(FileName);
					if (CanvasGameObject != null)
						StimGameObject.GetComponent<RectTransform>().SetParent(CanvasGameObject.GetComponent<RectTransform>());
					PositionRotationScale();
					break;
				default:
					break;
			}
			
			if (!string.IsNullOrEmpty(StimName))
				StimGameObject.name = StimName;
			AssignStimDefPointeToObjectHierarchy(StimGameObject, this);
			return StimGameObject;
		}

		public Texture2D LoadPNG(string filePath, bool visibility = false)
		{
			Texture2D tex = null;
			byte[] fileData;
			if (File.Exists(filePath))
			{
				fileData = File.ReadAllBytes(filePath);
				tex = new Texture2D(2, 2);
				tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
			}
			else
				Debug.LogError("FILE DOES NOT EXIST!");

			PositionRotationScale();
			ToggleVisibility(visibility);
			return tex;
		}
		public void DestroyStimGameObject()
		{
			StimGroup[] sgs = StimGroups.Values.ToArray();
			for (int iG = 0; iG < sgs.Length; iG++)
				RemoveFromStimGroup(sgs[iG]);
			
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

			if (StimGameObject != null)
			{
				//DestroyRecursive(StimGameObject);
				GameObject.Destroy(StimGameObject);
				Resources.UnloadUnusedAssets();
			}

			StimGameObject = null;
		}

        public void DestroyRecursive(GameObject go)
        {
            if (SessionValues.WebBuild) //need to eventually delete when get better solution
            {
                Object.Destroy(go);
                return;
            }

			if(go.GetComponent<Tile>()  != null)
			{
				return;
			}

            // Destroy MeshFilters and their associated Meshes
            MeshFilter[] meshFilters = go.GetComponentsInChildren<MeshFilter>();
            foreach (MeshFilter meshFilter in meshFilters)
            {
                if (meshFilter.sharedMesh != null)
                {
                    Debug.Log(go.name + " MeshFilter Mesh " + meshFilter.sharedMesh.name);
                    Object.DestroyImmediate(meshFilter.sharedMesh, false);
                }
                Object.DestroyImmediate(meshFilter, false);
            }

            // Destroy SkinnedMeshRenderers and their associated Meshes
            SkinnedMeshRenderer[] skinnedMeshRenderers = go.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (SkinnedMeshRenderer skinnedMeshRenderer in skinnedMeshRenderers)
            {
                if (skinnedMeshRenderer.sharedMesh != null)
                {
                    Debug.Log(go.name + " SkinnedMeshRenderer Mesh " + skinnedMeshRenderer.sharedMesh.name);
                    Object.DestroyImmediate(skinnedMeshRenderer.sharedMesh, false);
                }
                Object.DestroyImmediate(skinnedMeshRenderer, false);
            }

            // Destroy Textures and Materials
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;
                foreach (Material material in materials)
                {
                    if (material != null)
                    {
                        Debug.Log(go.name + " Material " + material.name);

                        // Destroy Textures
                        if (material.mainTexture != null)
                        {
                            if (material.mainTexture is Texture2D)
                            {
                                Debug.Log(go.name + " Texture2D " + material.mainTexture.name + " (Attached to: " + go.name + ")");
                                Object.DestroyImmediate(material.mainTexture, false);
                            }
                            else
                            {
                                Debug.Log(go.name + " Texture " + material.mainTexture.name + " (Attached to: " + go.name + ")");
                                Object.DestroyImmediate(material.mainTexture, false);
                            }
                        }

                        Object.DestroyImmediate(material, false);
                    }
                }
                renderer.sharedMaterials = new Material[materials.Length];
            }

            // Destroy GameObject
            Object.DestroyImmediate(go, false);
        }



		public GameObject LoadModel(string filePath, bool visibiility = false)
		{
			using (var assetLoader = new AssetLoader())
			{
				try
				{
					var assetLoaderOptions = AssetLoaderOptions.CreateInstance();
					assetLoaderOptions.AutoPlayAnimations = true;
					assetLoaderOptions.AddAssetUnloader = true;

					if(SessionValues.UsingDefaultConfigs)
					{
						StimGameObject = Object.Instantiate(Resources.Load(filePath) as GameObject);
						if (StimGameObject == null)
							Debug.Log("STIM GO IS NULL!!!!!!!!!");
					}
                    else
						StimGameObject = assetLoader.LoadFromFile(filePath);
				}
				catch (Exception e)
				{
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
            StimGameObject.transform.localPosition = StimLocation;

            if (StimRotation != null)
                StimGameObject.transform.rotation = Quaternion.Euler(StimRotation);

            if (StimScale == null)
                StimScale = 1;

            StimGameObject.transform.localScale = new Vector3(StimScale.Value, StimScale.Value, StimScale.Value);
		}



        public void AddMesh()
        {
            foreach (var m in StimGameObject.transform.GetComponentsInChildren<MeshRenderer>())
                m.gameObject.AddComponent(typeof(MeshCollider));
        }
        

        public string FilePathFromDims(string folderPath, IEnumerable<string[]> featureNames, string neutralPatternedColorName)
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
		public bool IsActive;

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
				sd.AddToStimGroup(this);			
		}

		public void AddStims(StimGroup sgOrig, IEnumerable<int> stimSubsetIndices)
		{
			foreach (int index in stimSubsetIndices)
			{
				sgOrig.stimDefs[index].AddToStimGroup(this);
				if (sgOrig.stimDefs[index].StimIndex != index)
					Debug.LogError("Stim at StimDef index " + index + " does not correspond to the listed StimIndex: " + sgOrig.stimDefs[index].StimIndex);
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
				sd.DestroyStimGameObject();
			}
		}

		public void ToggleVisibility(bool visibility)
		{
			foreach (StimDef stim in stimDefs)
			{
				stim.ToggleVisibility(visibility);
			}
            SessionValues.EventCodeManager.SendCodeImmediate(visibility ? "StimOn" : "StimOff");
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
