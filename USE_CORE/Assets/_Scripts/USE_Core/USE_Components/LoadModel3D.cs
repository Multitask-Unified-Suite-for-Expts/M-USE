using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TriLib;

public class LoadModel3D : MonoBehaviour {
	public string filepath = null;
	public bool loadOnStart = false;

	public GameObject containerForLoadedObjects;
	
	void Start () {
		if(loadOnStart){
			Load();
		}		
	}
	
	public GameObject Load(string filepath = null)
	{
		if (filepath == null)
			filepath = this.filepath;
		
		using (var assetLoader = new AssetLoader())
		{
			try
			{
				var assetLoaderOptions = AssetLoaderOptions.CreateInstance();
				//assetLoaderOptions.RotationAngles = new Vector3(90f, 180f, 0f);
				assetLoaderOptions.AutoPlayAnimations = true;
				assetLoaderOptions.AddAssetUnloader = true;
				var loadedGameObject = assetLoader.LoadFromFile(filepath, assetLoaderOptions);
				loadedGameObject.transform.SetParent(containerForLoadedObjects.transform, false);
				//loadedGameObject.transform.position = new Vector3(128f, 0f, 0f);
				//loadedGameObject.transform.localScale = new Vector3(0.05, 0.05, 0.05);
				return loadedGameObject;
			}
			catch (System.Exception e)
			{
				Debug.Log(filepath);
				Debug.LogError(e.ToString());
			}
			return null;
		}
	}
}
