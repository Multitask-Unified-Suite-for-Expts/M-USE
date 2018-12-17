using UnityEngine;
using USE_States;

public class PauseResumeControlLevel : MonoBehaviour {
	public ControlLevel targetControlLevel; 

	void Update () {
		if(targetControlLevel != null)
			if(Input.GetKeyDown(KeyCode.P))
				PauseResume();
	}

	public void PauseResume(){
		targetControlLevel.Paused = !targetControlLevel.Paused;
	}
}
