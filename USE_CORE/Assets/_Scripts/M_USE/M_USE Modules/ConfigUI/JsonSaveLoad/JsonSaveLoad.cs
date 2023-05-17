using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using Newtonsoft.Json;
public class JsonSaveLoad : MonoBehaviour
{
	public string folderPath = "";
	public bool isRelative = true;
	private string GetFilePath(string filename, bool create_directories = false){
		string directory = folderPath;
		if(isRelative)
			directory = Application.persistentDataPath + Path.DirectorySeparatorChar + folderPath;
		if(create_directories)
			Directory.CreateDirectory(directory);
		return directory + Path.DirectorySeparatorChar + filename + ".json";
	}
	
	public T LoadObject<T>(string filename, bool create_instance_if_not_found = true, bool isAbsolutePath=false)
	{
		string filePath = filename;
		if(!isAbsolutePath)
			filePath = GetFilePath(filename);
		
		try{
			if (File.Exists(filePath))
			{
				string dataAsJson = File.ReadAllText(filePath);
				return JsonConvert.DeserializeObject<T>(dataAsJson);
				//return  JsonMapper.ToObject<T>(dataAsJson);
			}
			Debug.Log("[JsonSaveLoad] file does not exist, path: " + filePath);
		}catch(System.Exception e){
			Debug.LogException(e);
		}
		
		try{
			if (create_instance_if_not_found)
				return (T)System.Activator.CreateInstance(typeof(T));
		}catch (System.Exception e)
		{
			Debug.LogException(e);
		}
		
		return default(T);
	}
	
	
	// deprecated
	public void OverwriteObject(string sourceFileName, System.Object targetObject)
	{
		string filePath = GetFilePath(sourceFileName);

		try
		{
			if (File.Exists(filePath))
			{
				string dataAsJson = File.ReadAllText(filePath);
				JsonUtility.FromJsonOverwrite(dataAsJson, targetObject);
			}
			else 
				Debug.Log("[JsonSaveLoad] file does not exist, path: " + filePath);
		}
		catch (System.Exception e)
		{
			Debug.LogException(e);
		}
	}

	public void SaveObject(string filename, System.Object obj)
	{
		try
		{
			//string dataAsJson = JsonUtility.ToJson(obj);
			//string dataAsJson = JsonMapper.ToJson(obj);
			string dataAsJson = JsonConvert.SerializeObject(obj);
			string filePath = GetFilePath(filename, create_directories:true);
			Debug.Log("[JsonSaveLoad] saving to filepath:" + filePath);
			File.WriteAllText(filePath, dataAsJson);
		}
		catch (System.Exception e)
		{
			Debug.LogException(e);
		}
	}
	
	public void DeleteObject(string filename){
		string filePath = GetFilePath(filename);
		try
		{
			if (File.Exists(filePath))
			{
				File.Delete(filePath);
				Debug.Log("[JsonSaveLoad] removing object at filepath:" + filePath);
			}				
			else
				Debug.Log("[JsonSaveLoad] file does not exist, path: " + filePath);
		}
		catch (System.Exception e)
		{
			Debug.LogException(e);
		}
	}
}