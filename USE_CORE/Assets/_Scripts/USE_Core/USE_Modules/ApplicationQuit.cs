using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ApplicationQuit : MonoBehaviour {

	public void Quit () {
		if (Application.isEditor) {
			UnityEditor.EditorApplication.isPlaying = false;
		}
		else
		{
			Application.Quit();
		}
	}
	
	void Update () {
		if(Input.GetKeyDown(KeyCode.Escape))
			Quit();
	}
}
