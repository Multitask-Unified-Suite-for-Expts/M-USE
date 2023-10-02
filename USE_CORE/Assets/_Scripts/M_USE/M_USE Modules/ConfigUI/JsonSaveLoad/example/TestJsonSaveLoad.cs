/*
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
