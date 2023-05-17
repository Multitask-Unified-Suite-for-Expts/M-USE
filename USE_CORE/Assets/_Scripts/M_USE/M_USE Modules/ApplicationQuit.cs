using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ApplicationQuit : MonoBehaviour {

	public void Quit()
	{
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#else
	Application.Quit();
#endif
	}

	void Update () {
		if(Input.GetKeyDown(KeyCode.Escape))
			Quit();
	}
}
