﻿/*
MIT License

Copyright (c) 2023 Multitask - Unified - Suite -for-Expts

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files(the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/




using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using USE_Settings;
using UnityEngine.UI;
using USE_States;
using Object = UnityEngine.Object;
using USE_ExperimentTemplate_Classes;
using System.Collections;
using GLTFast;
using System.Threading.Tasks;

namespace USE_StimulusManagement
{
	public class StimDef
	{
		public Dictionary<string, StimGroup> StimGroups; //stimulus type field (e.g. sample/target/irrelevant/etc)
		public string StimName;
		public string FileName; //name of file
		public string StimFolderPath;

		public string StimFilePath
		{
			get
			{
				string path = "";

				if (Session.UsingDefaultConfigs)
				{
					path = $"{Session.DefaultStimFolderPath}/{FileName}";
                }
				else if (Session.UsingLocalConfigs)
				{
                    List<string> filenames = RecursiveFileFinder.FindFile(StimFolderPath, FileName, StimExtension);
					if (filenames.Count < 1)
						Debug.LogError("COULDNT FIND THE STIM!");
					else
						path = filenames[0];
				}
				else if (Session.UsingServerConfigs)
				{
					path = $"{ServerManager.ServerURL}/{ServerManager.ServerStimFolderPath}/{FileName}";
                }
				else
				{
					Debug.LogError("NOT USING DEFAULT, LOCAL, OR SERVER CONFIGS WHILE TRYING TO GET STIMFILEPATH!");
				}

				return path;
			}
		}

		public string StimFilePathIfInTaskResourcesFolder
		{
			get
			{
                string path = "";

				if (Session.UsingLocalConfigs)
				{
					path = $"{Session.TaskLevel.TaskResourcesPath}/{FileName}";
				}
				else if (Session.UsingServerConfigs)
				{
					path = $"{ServerManager.ServerURL}/{Session.TaskLevel.TaskResourcesPath}/{FileName}"; //trying
				}
				else
					Debug.LogError("TRIED TO GET STIM PATH IN TASK RESOURCES FOLDER BUT NOT USING LOCAL OR SERVER CONFIGS!");

				return path;
            }
		}

		public string StimExtension;
		public int StimCode; //optional, for analysis purposes
		public int StimIndex;
		public string StimID;
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

		private bool LoadingAsync;

        public GameObject MaskGameObject; //Mask Gameobject for the stim, if instantiated by the MaskController class. 



        public StimDef()
		{
			StimGroups = new Dictionary<string, StimGroup>();
		}

		public StimDef(StimGroup sg, State setActiveOnInit = null, State setInactiveOnTerm = null)
		{
			sg.stimDefs.Add(this);
            StimGroups = new Dictionary<string, StimGroup>
            {
                { sg.stimGroupName, sg }
            };
            SetVisibilityOnOffStates(setActiveOnInit, setInactiveOnTerm);
		}

		public StimDef(StimGroup sg, GameObject obj, State setActiveOnInit = null, State setInactiveOnTerm = null)
		{
			StimGameObject = obj;
			sg.stimDefs.Add(this);
            StimGroups = new Dictionary<string, StimGroup>
            {
                { sg.stimGroupName, sg }
            };
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
			if (FileName != null)
				sd.FileName = FileName;
			if (StimFolderPath != null)
				sd.StimFolderPath = StimFolderPath;
			if (StimID != null)
				sd.StimID = StimID;

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
			if (FileName != null)
				sd.FileName = FileName;
			if (StimFolderPath != null)
				sd.StimFolderPath = StimFolderPath;
			if (StimID != null)
				sd.StimID = StimID;

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
            StimGameObject?.SetActive(visibility);
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
				Debug.LogWarning("Attempted to add stim " + StimName + " to StimGroup " + sg.stimGroupName + " but this stimulus is already a member of this StimGroup.");
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
            FileName = FileName.Trim(); //trim file name because sometimes the stimdefs have extra space.

			Session.Using2DStim = FileName.ToLower().Contains(".png") || FileName.ToLower().Contains("_png");

            if (Session.UsingDefaultConfigs)
				LoadPrefabFromResources();
            else
            {
                if (Session.UsingServerConfigs)
                {
                    if (Session.Using2DStim)
                        yield return CoroutineHelper.StartCoroutine(Load2DStimFromServer());
                    else
                        LoadExternalStimFromFile();
                }
                else if (Session.UsingLocalConfigs)
                    LoadExternalStimFromFile(); //Call should be awaited, but we cant cuz this is coroutine not async. Resolved with "yield return waitUntil" line below
            }

			//HAVE TO WAIT UNTIL LOADFROMEXTERNALFILE() FINISHES LOADING THE STIMGAMEOBJECT!
			float startTime = Time.time;
			yield return new WaitUntil(() => (StimGameObject != null && !LoadingAsync) || Time.time - startTime >= Session.SessionDef.MaxStimLoadingDuration);

			if (StimGameObject == null)
				Debug.LogError("STIM GO STILL NULL AFTER YIELDING! MAX STIM LOADING DURATION HAS BEEN SURPASSED!");

			//For 2D stim, set as child of Canvas:
            if (Session.Using2DStim && CanvasGameObject != null)
                StimGameObject.GetComponent<RectTransform>().SetParent(CanvasGameObject.GetComponent<RectTransform>());

			SetStimName();
            PositionRotationScale();
            AddMesh();
            ToggleVisibility(false);
            AssignStimDefPointerToObjectHierarchy(StimGameObject, this);

            callback?.Invoke(StimGameObject);
        }
		
		public void LoadPrefabFromResources()
		{
			try
			{
				StimGameObject = Object.Instantiate(Resources.Load(StimFilePath) as GameObject);
				StimGameObject.SetActive(false);
			}
			catch(Exception e)
			{
                Debug.LogError($"ERROR LOADING STIM FROM RESOURCES PATH: " + StimFilePath + " | Error: "+ e.Message);
            }
        }

        public IEnumerator Load2DStimFromServer()
		{
            //string filePath = $"{ServerManager.ServerStimFolderPath}/{FileName}";

            yield return CoroutineHelper.StartCoroutine(ServerManager.LoadTextureFromServer(StimFilePath, textureResult =>
			{
				if (textureResult != null)
				{
					StimGameObject = new GameObject();
					StimGameObject.SetActive(false);
					RawImage image = StimGameObject.AddComponent<RawImage>();
					image.texture = textureResult;
				}
				else
					Debug.LogError("TRIED TO LOAD 2D STIM FROM SERVER BUT THE RESULTING TEXTURE IS NULL!");
			}));
		}

        public async void LoadExternalStimFromFile()
		{
			StimExtension = "." + FileName.Split('.')[1];

			switch (StimExtension.ToLower())
			{
				case ".png":
					LoadExternalPNG();
					break;
                case ".gltf":
                    await LoadExternalGLTF();
                    break;
                default:
					break;
			}
		}

		public async Task LoadExternalGLTF()
		{
            try
            {
				var gltf = new GltfImport();

				//First try the Task/TaskResources folder:
				var success = await gltf.Load(StimFilePathIfInTaskResourcesFolder);

				//If didnt find it in taskResources folder, try normal stim file path:
				if(!success)			
					success = await gltf.Load(StimFilePath);
				
				if (success)
				{
					LoadingAsync = true;
					StimGameObject = new GameObject();
					StimGameObject.SetActive(false);
					await gltf.InstantiateMainSceneAsync(StimGameObject.transform);
					LoadingAsync = false;
				}
				else
				{
                    Debug.LogError("UNSUCCESFUL LOADING GLTF FROM BOTH TASK RESOURCES FOLDER AND EXTERNAL FILE PATH");
				}
			}
			catch(Exception e)
			{
				Debug.LogError($"FAILED TO LOAD GLTF: {FileName} | Error: " + e.Message.ToString());
			}
        }

		public void LoadExternalPNG()
        {
			StimGameObject = new GameObject();
            RawImage stimGOImage = StimGameObject.AddComponent<RawImage>();

			string path = "";

			if (File.Exists(StimFilePathIfInTaskResourcesFolder))
				path = StimFilePathIfInTaskResourcesFolder;
			else
			{
				if (File.Exists(StimFilePath))
					path = StimFilePath;
			}

			if (path.Length > 0)
			{
				byte[] fileData = File.ReadAllBytes(path);
				Texture2D tex = new Texture2D(2, 2);
				tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
				stimGOImage.texture = tex;
			}
			else
				Debug.LogError("COULDNT FIND THE FILE AT TASK RESOURCES FOLDER OR NORMAL STIM FILE PATH!");
        }


		private void PositionRotationScale()
        {
            StimGameObject.transform.localPosition = StimLocation;

            if (StimRotation != null)
                StimGameObject.transform.rotation = Quaternion.Euler(StimRotation);

			StimScale ??= 1;

            StimGameObject.transform.localScale = new Vector3(StimScale.Value, StimScale.Value, StimScale.Value);
        }

        private void SetStimName()
        {
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
        }


        private List<GameObject> GetAllObjectsInHierarchy(GameObject parentObject)
        {
            List<GameObject> objects = new List<GameObject>() { parentObject };
            foreach (Transform child in parentObject.transform)
            {
                List<GameObject> childChildren = GetAllObjectsInHierarchy(child.gameObject);
                objects.AddRange(childChildren);
            }
            return objects;
        }

        public void AssignStimDefPointerToObjectHierarchy(GameObject parentObject, StimDef sd)
        {
            List<GameObject> objectsInHierarchy = GetAllObjectsInHierarchy(parentObject);
            foreach (GameObject obj in objectsInHierarchy)
            {
                obj.AddComponent<StimDefPointer>();
                obj.GetComponent<StimDefPointer>().StimDef = this;
            }
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

				//Resources.UnloadUnusedAssets();
			}

			StimGameObject = null;
		}

        public void DestroyRecursive(GameObject go)
        {
            if (Session.WebBuild) //need to eventually delete when get better solution
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


        public void AddMesh()
        {
            foreach (var m in StimGameObject.transform.GetComponentsInChildren<MeshRenderer>())
                m.gameObject.AddComponent(typeof(MeshCollider));
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
	        IsActive = true;
			ToggleVisibility(true);
		}

		private void InactivateOnStateTerm(object sender, EventArgs e)
		{
			IsActive = false;
			ToggleVisibility(false);
			SetActiveOnInitialization.StateInitializationFinished -= ActivateOnStateInit;
			SetInactiveOnTermination.StateTerminationFinished -= InactivateOnStateTerm;
		}


        public void ToggleVisibility(bool visibility)
        {
            foreach (StimDef stim in stimDefs)
            {
                stim.ToggleVisibility(visibility);
            }
            Session.EventCodeManager.AddToFrameEventCodeBuffer(visibility ? "StimOn" : "StimOff");
            IsActive = visibility;
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
		

		public void RemoveStims(StimGroup sgOrig, IEnumerable<int> stimSubsetIndices)
		{
			foreach (int index in stimSubsetIndices)
			{
				sgOrig.stimDefs[index].RemoveFromStimGroup(this);
			}
		}


        public IEnumerator LoadStims()
        {
            if (stimDefs == null)
            {
                Debug.LogError("STIMDEFS IS NULL!");
                yield break;
            }

            var stimDefsCopy = new List<StimDef>(stimDefs);

            foreach (StimDef sd in stimDefsCopy)
            {
                if (sd.StimGameObject == null)
                {
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
