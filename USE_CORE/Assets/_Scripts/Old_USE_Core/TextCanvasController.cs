using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using USE_Utilities;
using System.Diagnostics;


public class TextCanvasController : MonoBehaviour {

	private Text textbox;
	private GameObject background;
	private string text;

	private int keycounter;

	// Use this for initialization
	void Start () {
		textbox = UnityExtension.GetChildByName (gameObject, "Textbox").GetComponent<Text> ();
		background = UnityExtension.GetChildByName (gameObject, "Background");

		EnableTextCanvas (false);

		ResetKeycounter ();
	}

	public void ContinueText(){
		text = "Press any key to continue...";

		textbox.text = text;
	}

	public void InstructionText(){

		string text;
		switch (keycounter) {
		case 1:
			text = "Thank you for partcipating in our study!\n\nPress any key to continue...";
			textbox.text = text;
			UpdateKeycounter ();

			break;
		case 2:
			text = "You will be playing a simple learning game." +
				"\n\nPress any key to continue...";
			textbox.text = text;
			UpdateKeycounter ();

			break;		
		case 3:
			text = "At the start of each trial, you will be in front of a door with a black circle on it." +
			"\n\nPress any key to contine...";

			textbox.text = text;
			UpdateKeycounter ();

			break;		
		case 4:
			text = "Move towards the door with the joystick until the circle changes colour to blue." + 
				"\n\nPress any key to continue...";

			textbox.text = text;
			UpdateKeycounter ();

			break;
		case 5:
			text = "Stop moving and stare directly at the center of the circle. Once you have done this for long enough the door will open." +
			"\n\nPress any key to continue...";
			
			textbox.text = text;
			UpdateKeycounter ();

			break;
		case 6:
			text = "You will enter into an arena containing two objects. Choose one of these objects by walking over it." +
				"\n\nPress any key to continue...";

			textbox.text = text;
			UpdateKeycounter ();

			break;
		case 7:
			text = "You can switch to the other object by walking over it." +
				"\n\nPress any key to continue...";

			textbox.text = text;
			UpdateKeycounter ();

			break;
		case 8:
			text = "Bring whichever object you choose to the black door and you will be told if your choice was correct or incorrect." +
				"\n\nPress any key to continue...";

			textbox.text = text;
			UpdateKeycounter ();

			break;
		case 9:
			text = "One object is correct, one is incorrect. You will have to learn which is correct by trial and error." +
				"\n\nPress any key to continue...";

			textbox.text = text;
			UpdateKeycounter ();

			break;
		case 10:
			text = "It is possible to get 100% accuracy once you learn the rules. The rules are based on a combination of two things." +
				"\n\nPress any key to continue...";

			textbox.text = text;
			UpdateKeycounter ();

			break;
		case 11:
			text = "First, the features of each object: their shape, colour, pattern, or arm type." +
				"\n\nPress any key to continue...";

			textbox.text = text;
			UpdateKeycounter ();

			break;
		case 12:
			text = "Second, the type of environment you are in, shown by the ground you are walking over." +
				"\n\nPress any key to continue...";

			textbox.text = text;
			UpdateKeycounter ();

			break;
		case 13:
			text = "If you pay attention to both the object features and the ground, you will be able to master the rules." +
				"\n\nPress any key to continue...";

			textbox.text = text;
			UpdateKeycounter ();

			break;
		case 14:
			text = "Once you have demonstrated sufficient mastery of the rules, the rules will change and you will have to learn the new rules." +
				"\n\nPress any key to continue...";

			textbox.text = text;
			UpdateKeycounter ();

			break;
		case 15:
			text = "A rule change will be shown by the ground changing to something new." +
				"\n\nPress any key to continue...";

			textbox.text = text;
			UpdateKeycounter ();

			break;
		case 16: 
			text = "While you do the experiment, we will be tracking your eye movements." +
				"\n\nWe will take a moment now to calibrate the eyetracker to your eyes." +
				"\n\nPress any key to start calibrating...";
			textbox.text = text;
			UpdateKeycounter ();

			break;
		default:
			break;
		}
	
	}

	public void SetText(string text){
		if (!textbox.isActiveAndEnabled) {
			EnableTextCanvas (true);
		}
		textbox.text = text;
	}

	public bool CheckTextEnd(string epoch){

		int iend = -1;
		if (epoch.Equals ("instruction")) {
			iend = 16;
		}

		if (keycounter == iend) {
			ResetKeycounter ();

			return true;
		} else {
			return false;
		}
	}

	private void ResetKeycounter(){
		keycounter = 1;
	}

	private void UpdateKeycounter(){
		if (Input.anyKeyDown) {
			keycounter++;
		}
	} 

	public void EnableTextCanvas(bool flag){
		if (!gameObject.activeSelf) {
			gameObject.SetActive (true);		
		}
		textbox.gameObject.SetActive (flag);
		background.SetActive (flag);


	}
}
