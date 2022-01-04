using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace USE_StimulusManagement
{
public class StimDef{

		public string StimType; //stimulus type field (e.g. sample/target/irrelevant/etc)
		public string StimName;
        public string StimPath;
        public int StimCode; //optional, for analysis purposes
        public string StimID;
        public int[] StimDimVals; //only if this is parametrically-defined stim
        [System.NonSerialized]
        public GameObject StimGameObject; //not in config, generated at runtime
        public Vector3 StimLocation; //to be passed in explicitly if trial doesn't include location method
        public Vector3 StimRotation; //to be passed in explicitly if trial doesn't include location method
        public Vector2 StimScreenLocation;//screen position calculated during trial
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

		public StimDef CopyStimDef()
		{
			StimDef sd = new StimDef();
			if(StimName != null)
				sd.StimName = StimName;
			if (StimType != null)
				sd.StimType = StimType;
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
			if (StimLocation != null)
				sd.StimLocation = StimLocation;
			if (StimRotation != null)
				sd.StimRotation = StimRotation;
			if (StimScreenLocation != null)
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

		public void setStimType(string type)
		{
			StimType = type;
		}


		public void AddMesh()
		{
			foreach (var m in StimGameObject.transform.GetComponentsInChildren<MeshRenderer>())
			{
				m.gameObject.AddComponent(typeof(MeshCollider));
			}
		}

		public void loadModel(Camera cam, float scale, bool visibiility = false) {
			LoadModel3D lm = GameObject.Find("USE_Components").GetComponent<LoadModel3D>();
			StimGameObject = lm.Load(StimPath);
			AddMesh();
			StimGameObject.transform.position = StimLocation;
			StimGameObject.transform.rotation = Quaternion.Euler(StimRotation);
			StimGameObject.transform.localScale = new Vector3(scale, scale, scale);
			StimScreenLocation = cam.WorldToScreenPoint(StimLocation);
			ToggleVisibility(visibiility);
		}

		public void FilePathFromDims(string folderPath, IEnumerable<string[]> featureNames, string neutralPatternedColorName)
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
			{  //special case for patterned Quaddle without color
				int colour = filename.IndexOf('C');
				string c1 = filename.Substring(colour, 16);
				filename = filename.Replace(c1, neutralPatternedColorName);
			}
			else if (StimDimVals[1] == 0)
			{  //special case where colours are solid for neutral pattern
				int colour = filename.IndexOf('C');
				string c1 = filename.Substring(colour + 1, 7);
				string c2 = filename.Substring(colour + 9, 7);
				filename = filename.Replace(c2, c1);
			}

			//return CheckFileName(folderPath, filename);
		} 
	}


	[System.Serializable]
	public class StimGroup:MonoBehaviour
	{
		public List<StimDef> StimDefs;
		public string StimType;

		//constructor that accepts list of stimulus
		public StimGroup(string type, List<StimDef> stims)
		{
			StimDefs = stims;
			StimType = type;
			ToggleVisibility(false);
		}

		//constructor that accepts array of stimulus
		public StimGroup(string type, StimDef[] stims)
		{
			StimDefs = stims.OfType<StimDef>().ToList();
			StimType = type;
			ToggleVisibility(false);
		}

		public StimGroup(string type, IEnumerable<int[]> stimDimVals, string folderPath, IEnumerable<string[]> featureNames, string neutralPatternedColorName, Camera cam, float scale = 1) 
		{
			StimType = type;
			foreach(int[] dimVals in stimDimVals)
			{
				StimDef sd = new StimDef();
				sd.StimDimVals = dimVals;
				sd.FilePathFromDims(folderPath, featureNames, neutralPatternedColorName);
				sd.loadModel(cam, scale);
			}
		}

		public void AddStimulus(StimDef stim)
		{
			// when stimulus is added to StimGroup, its type is automatically updated to be the same as of StimGroup
			stim.setStimType(StimType);
			StimDefs.Add(stim);
			stim.ToggleVisibility(false);
		}

		public void DestroyStimGroup(float t = 0.0f)
		{
			foreach (StimDef stim in StimDefs)
				Destroy(stim.StimGameObject, t);
			Destroy(this);
		}

		public void ToggleVisibility(bool visibility)
		{
			foreach (StimDef stim in StimDefs)
			{
				stim.ToggleVisibility(visibility);
			}
		}

		public void AddMesh()
		{
			foreach (StimDef stim in StimDefs)
			{
				stim.AddMesh();
			}
			
		}

	}

}
