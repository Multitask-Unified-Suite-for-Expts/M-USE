using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestJsonSaveLoad : MonoBehaviour {

	public ExampleJsonData exampleData;
	public ExampleJsonData exampleDataToBeOverwritten;

	public JsonSaveLoad jsonSaveLoad;
	private void Awake()
	{
		if (jsonSaveLoad == null)
			jsonSaveLoad = FindObjectOfType<JsonSaveLoad>();
	}
	
	public void LoadData(){
		exampleData.ignoreMe = Random.Range(0, 100);
		var i = exampleData.ignoreMe;
		exampleData = jsonSaveLoad.LoadObject<ExampleJsonData>("example");
		Debug.Log("After loading, value of ignored variable:" + exampleData.ignoreMe + " changed from: " + i);
		Debug.Log("The value of the ignored variables should be same as the default values defined in class");
	}
	
	public void SaveData(){
		jsonSaveLoad.SaveObject("example", exampleData);
	}
	
	public void OverwriteData(){
		exampleDataToBeOverwritten.ignoreMe = Random.Range(0, 100);
		var i = exampleDataToBeOverwritten.ignoreMe;
		jsonSaveLoad.OverwriteObject("example", exampleDataToBeOverwritten);
		Debug.Log("After overwriting, value of ignored variable:" + exampleDataToBeOverwritten.ignoreMe + " changed from: " + i);
		Debug.Log("The value of the ignored variables should remain unchanged");
	}
	
	public void DeleteData()
	{
		jsonSaveLoad.DeleteObject("example");
	}
}
