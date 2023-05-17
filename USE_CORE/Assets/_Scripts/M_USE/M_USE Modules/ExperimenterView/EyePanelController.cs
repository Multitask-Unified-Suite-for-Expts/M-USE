using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using USE_Utilities;

public class EyePanelController : MonoBehaviour {

	private GameObject eyeLeft;
	private GameObject eyeRight;
	//private GameObject panelLeft;
	//private GameObject panelRight;
	private Image leftPanelImg;
	private Image rightPanelImg;

	private Color valid;
	private Color notValid;
	// private ExternalDataManager externalDataManager;

	// Use this for initialization
	void Start () {
		eyeLeft = USE_Utilities.UnityExtension.GetChildByName (gameObject, "EyeballLeft");
		eyeRight = USE_Utilities.UnityExtension.GetChildByName (gameObject, "EyeballRight");

		leftPanelImg = USE_Utilities.UnityExtension.GetChildByName (gameObject, "LeftPanel").GetComponent<Image> ();
		rightPanelImg = USE_Utilities.UnityExtension.GetChildByName (gameObject, "RightPanel").GetComponent<Image> ();

		valid = Color.green;
		notValid = Color.red;

		// externalDataManager = GameObject.Find ("ScriptManager").GetComponent<ExternalDataManager> ();
	}	


//	void UpdateEyePosition(float xl, float yl, float xr, float yr, int vl, int vr){
//	
//	}

	public void UpdateEyePosition(float rx, float ry, bool rv, float lx, float ly,  bool lv){

		//eye position relative to screen


		//for now, offset the position to keep the eyes seperate. WONT be necessary when we get seeperate eye information
		rx = rx + 0.05f;
		lx = lx - 0.05f;

		//validity: is y position -1?
		// bool rightValid = externalDataManager.eyeValidity[1];
		// bool leftValid = externalDataManager.eyeValidity[0];

		bool rightValid = true;
		bool leftValid = true;

		//update the validity panels
		if (!leftValid) {
			leftPanelImg.color = notValid;
		} else {
			leftPanelImg.color = valid;
		}

		if (!rightValid) {
			rightPanelImg.color = notValid;
		} else {
			rightPanelImg.color = valid;
		}

		//update the eye position in the window


	}

	void ScaleEyePanel(){
		
	}
}
