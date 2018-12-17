using UnityEngine;
using USE_States;
using UnityEngine.UI;

public class PauseResumeControlLevel : MonoBehaviour {
	public ControlLevel targetControlLevel; 
	public Text text;
	public string textToPause = "Pause";
	public string textToResume = "Resume";

	void Update () {
		if(targetControlLevel != null)
			if(Input.GetKeyDown(KeyCode.P))
				PauseResume();
	}

	public void PauseResume(){
		targetControlLevel.Paused = !targetControlLevel.Paused;
		if(text != null){
			if(targetControlLevel.Paused)
				text.text = this.textToResume;
			else
				text.text = this.textToPause;
		}
	}
}
