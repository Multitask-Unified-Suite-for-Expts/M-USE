using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using USE_Common_Namespace;

//Each Sequence Manager in the experiment needs an associated set of sequence definitions. 
//Duplicate this script, attach it to the instantiated Sequence Manager object in the scene hierarchy (NOT the Sequence Manager prefab in the Project folders!).
//Edit it to attach the correct methods to the various epochs.

//A sequence is composed of a number of epochs. These are just stages that are meant to be run sequentially.

//Each epoch has 5 properties that can be defined (although any of them can be left blank). 
//1 - Initialization: A set of methods to be run once, immediately upon beginning the epoch.
//2 - FixedUpdate: A set of methods to be run once during every fixedUpdate during the epoch, repeating for as long as the epoch lasts.
//3 - Update: A set of methods to be run once every frame during the epoch, repeating for as long as the epoch lasts.
//4 - Termination: A set of methods to be run once, immediately at the end of the epoch.
//5 - TerminationCriterion: A method that returns a boolean, which returns false if, on the subsequent frame, 
//	we still want to run the same set of Update methods. It returns true if, on the subsequent frame, we want to change to the next epoch in our sequence.

//In many cases, there will be no special methods to run during initialization, fixedUpdate, update, or termination, in which case they should be set to null.
//There must be a termination criterion set, otherwise the epoch will run forever (unless it is terminated by another sequence).
//Note that epochTerminationCriterion returns a boolean (true if the epoch is finished, false otherwise). All other methods should return void.
//No method takes any arguments.

public class SequenceDefinition_Calibration : MG_SequenceController {

	//for calibration point definition
	public int numCalibPoints = 9;
	public float[] calibPointsInset = new float[2] {.1f, .15f};
	public Vector2[] calibPointsADCS;
	private float acceptableCalibrationDistance;
	public bool currentCalibrationPointFinished;
	public bool calibrationFinished;
	private int recalibratePoint = 0;
	private bool calibAssessment;

	public bool usingNeurarduino;

	//for calibration circle movement/shrink definition
	private float epochStartTime;


	private Vector3 currentCalibTargetScreen;
	private Vector2 currentCalibTargetADCS;
	private Vector3 moveVector;
	private Vector3 calibCircleStartPos;

	private int calibCount;

	private float proportionOfMoveTime;
	private float proportionOfShrinkTime;

	private float calibCircleMoveTime = .75f;
	private float assessTime = 0.5f;
	private float calibCircleShrinkTime = 0.5f;//0.3f;
	private float calibTime = 0.3f;
	private float rewardTime = 0.5f;

	private Vector3 bigCircleMaxScale = new Vector3 (0.6f, 0.6f, 0.6f);
	private float bigCircleShrinkTargetSize = .1f;
	private float smallCircleSize = 0.065f;

	private float blinkOnDuration = 0.2f;
	private float blinkOffDuration = 0.1f;

	// Game Objects
	private GameObject calibSmallCircle;
	private GameObject calibBigCircle;
	private GameObject calibCanvas;
	private Sprite redCircle;
	private Sprite blackCircle;
	private Sprite blueCircle;
	private Camera cam;


	private ExperimentInfoController exptInfo;
	private CalibrationResult calibResult;

//	public MonitorDetails monitorDetails;

//	public Dictionary<string,int[]> monitorDims = new Dictionary<string, int[]> {
//		{ "Tobii", new int[]{ 1080, 1920 } },
//		{ "Acer", new int[]{ 1080, 1920 } },
//		{"Macbook",new int[] { 1800, 2880 }},
//		{"MacbookNoRetina",new int[] { 900, 1440 }},
//		{"Thunderbolt",new int[] { 1440, 2560}}
//	};
	public float widthCm;

	private ExternalDataManager externalDataManager;
	private UDPManager udpManager;

	public Vector2[] ninePoints;
	//	private MG_SequenceController sequenceController;
	//	private SequenceDefinition_Main mainSequence;

	public override void DefineSequence () {

//		if (SceneManager.GetActiveScene ().name == "Dynamic") {
//			widthCm = ConfigReader.exptSettings.MonitorDetails ["monitorDetails"].cmSize [0];
//		} else if (SceneManager.GetActiveScene ().name == "Static") {
//			widthCm = ConfigReader_static.exptSettings.MonitorDetails ["monitorDetails"].cmSize [0];
//		}

		ninePoints = new Vector2[9]
		{new Vector2(calibPointsInset[0], calibPointsInset[1]),
			new Vector2(0.5f, calibPointsInset[1]),
			new Vector2(1f - calibPointsInset[0], calibPointsInset[1]),
			new Vector2(calibPointsInset[0], 0.5f),
			new Vector2(0.5f, 0.5f),
			new Vector2(1f - calibPointsInset[0], 0.5f),
			new Vector2(calibPointsInset[0], 1f - calibPointsInset[1]),
			new Vector2(0.5f, 1f - calibPointsInset[1]),
			new Vector2(1f - calibPointsInset[0], 1f - calibPointsInset[1])};

		try{
			exptInfo = GameObject.Find ("ExperimenterInfo").GetComponent<ExperimentInfoController>();
		} catch{
		}
		

		SerialPortController serialPortController = GameObject.Find ("ScriptManager").GetComponent<SerialPortController> ();

		cam = Camera.main;
		externalDataManager = GameObject.Find ("ScriptManager").GetComponent<ExternalDataManager> ();
		udpManager = GameObject.Find ("ScriptManager").GetComponent<UDPManager> ();



		// Assign Game Objects
		calibSmallCircle = GameObject.Find("CalibrationSmallCircle");
		calibBigCircle = GameObject.Find("CalibrationBigCircle");
		calibCanvas = GameObject.Find ("CalibrationCanvas");
		redCircle = Resources.Load<Sprite>("calibration-red-circle") as Sprite;
		blackCircle = Resources.Load <Sprite> ("calibration-black-circle") as Sprite;
		blueCircle = Resources.Load <Sprite> ("calibration-blue-circle") as Sprite;

		calibBigCircle.GetComponent<Image> ().sprite = blackCircle;

		AddSequenceInitializationMethod (
			() => {
				print("Calib sequence init");
				udpManager.SendString("ET\tenter_calibration");
				DefineCalibPoints (numCalibPoints);
//				currentCalibTargetADCS = calibPointsADCS.Dequeue ();
				calibCanvas.SetActive (true);
				calibrationFinished = false;
				calibCount = 0;
				calibResult = new CalibrationResult();
			});


		//EPOCH 0 = blink calibration circle
		int eBlink = 0;
		float blinkStartTime = 0;
		bool keyboardOverride = false;
		AddEpochInitializationMethod (eBlink,
			() => {
				calibAssessment = false;
				calibBigCircle.transform.localScale = bigCircleMaxScale;
//				if(currentCalibrationPointFinished){
				currentCalibTargetADCS = calibPointsADCS[calibCount];
//				}
				currentCalibrationPointFinished = false;
				currentCalibTargetScreen = ScreenTransformations.AdcsToScreenPoint(currentCalibTargetADCS);
				calibBigCircle.transform.position = currentCalibTargetScreen;
				calibBigCircle.SetActive(true);
				calibSmallCircle.SetActive(false);
				epochStartTime = Time.time;
				keyboardOverride = false;
			});
		AddEpochUpdateMethod (eBlink,
			() => {
				blinkStartTime = CheckBlink (blinkStartTime, calibBigCircle);
				if (Input.GetKeyDown (KeyCode.Space)){
					keyboardOverride = true;
				}
			});
		AddEpochTerminationCriterionMethod (eBlink,
			() =>  keyboardOverride || Vector3.Distance (externalDataManager.eyePosition, currentCalibTargetScreen) < acceptableCalibrationDistance);
		AddEpochTerminationMethod (eBlink,
			() => calibBigCircle.SetActive (true));

		//EPOCH 1 - Shrink calibration circle
		int eShrink = 1;
		AddEpochInitializationMethod (eShrink,
			() => {
				calibSmallCircle.transform.localScale = new Vector3(smallCircleSize, smallCircleSize, smallCircleSize);
				calibSmallCircle.transform.position = currentCalibTargetScreen;
				calibSmallCircle.GetComponent<Image> ().sprite = redCircle;
				calibSmallCircle.SetActive(true);
				proportionOfShrinkTime = 0;
				epochStartTime = Time.time;
			});
		AddEpochUpdateMethod (eShrink,
			() => {
				ShrinkCalibCircle ();
				if(!keyboardOverride){
					if(Vector3.Distance (externalDataManager.eyePosition, currentCalibTargetScreen) > acceptableCalibrationDistance){
						SwitchEpoch(eBlink);
					}
				}
			});
		AddEpochTerminationCriterionMethod (eShrink,
			() => proportionOfShrinkTime == 1);
		//

		//EPOCH 3 - Check readiness to calibrate
		int eCheck = 2;
		AddEpochInitializationMethod (eCheck,
			() => keyboardOverride = false);
		AddEpochUpdateMethod (eCheck,
			() => {
				if (Input.GetKeyDown (KeyCode.Space)) {
					keyboardOverride = true;
				}
			});
		AddEpochTerminationCriterionMethod (eCheck,
			() => keyboardOverride || Input.GetKeyDown (KeyCode.Space) ||
			Vector3.Distance (externalDataManager.eyePosition, currentCalibTargetScreen) < acceptableCalibrationDistance);


		//EPOCH 4 - Calibrate!
		int eCalibrate = 3;
		AddEpochInitializationMethod(eCalibrate,
			()=>{
				Debug.Log("calibrate");
				udpManager.SendString("ET\tcollect_calibration_at_point\tfloat " + currentCalibTargetADCS.x.ToString () + "\tfloat " + currentCalibTargetADCS.y.ToString ());
			});
		AddEpochTerminationCriterionMethod (eCalibrate,
			() => currentCalibrationPointFinished); //set by ExternalDataManager, which is probably bad practice
		AddEpochTerminationMethod (eCalibrate,
			() => {
				if (calibCount == calibPointsADCS.Length - 1 & !calibAssessment) {
					Debug.Log("calc and apply calib");
					calibAssessment = true;
					udpManager.SendString("ET\tcompute_and_apply_calibration");
				}
			});

		//EPOCH 5
		int eConfirm = 4;

		AddEpochInitializationMethod (eConfirm,
			() => {
				epochStartTime = Time.time;
				calibSmallCircle.GetComponent<Image> ().sprite = blueCircle;
				if (usingNeurarduino){
					serialPortController.SendString ("RWD " + rewardTime * 10000);
				}
			});
		AddEpochTerminationCriterionMethod (eConfirm,
			() => AssessCalibration ());


		AddSequenceTerminationCriterionMethod (
			() => calibrationFinished);
		AddSequenceTerminationMethod (
			() => {
				print("Calib Terminated");
				udpManager.SendString("ET\tleave_calibration");
				ClearCalibResults();
				GameObject.Find ("CalibrationCanvas").SetActive (false);
			});
	}

	private bool AssessCalibration(){
		bool finishEpoch = false;
		if (Time.time - epochStartTime > assessTime) {
			calibBigCircle.SetActive (false);
			calibSmallCircle.SetActive (false);
			if (calibCount < calibPointsADCS.Length - 1) {
				calibCount++;
				finishEpoch = true;
			} else {
				//get calibration results
				if (Input.anyKey) {
					string commandString = Input.inputString;
					if (commandString == " ") {
						finishEpoch = true;
						calibrationFinished = true;
						ClearCalibVisuals ();
					}else if(commandString == "r"){
						finishEpoch = true;
						calibCount = 0;
						ClearCalibResults ();
						for (int i = 0; i < numCalibPoints; i++) {
							DiscardCalibrationPoint (i);
						}
						DefineCalibPoints (numCalibPoints);
					}else if(int.TryParse(commandString, out recalibratePoint)){
						if (recalibratePoint > 0 & recalibratePoint < 10) {
							DiscardCalibrationPoint (recalibratePoint - 1);
							calibCount = 0;
							ClearCalibResults ();
							DefineCalibPoints (1);
							finishEpoch = true;
						}
					}
				}
			}
		}
		return finishEpoch;
	}


	private void checkEye() {
		if (Vector3.Distance (externalDataManager.eyePosition, currentCalibTargetScreen) > acceptableCalibrationDistance) {
			SwitchEpoch (2);
		}
	}


	void DefineCalibPoints(int nPoints){
		switch (nPoints) {
		case 9:
			calibPointsADCS = ninePoints;
			acceptableCalibrationDistance = Vector2.Distance (ScreenTransformations.AdcsToScreenPoint(ninePoints [0]), ScreenTransformations.AdcsToScreenPoint(ninePoints [1])) / 2;
			break;
		case 5:
			calibPointsADCS = new Vector2[5] {
				ninePoints [0],
				ninePoints [2],
				ninePoints [4],
				ninePoints [6],
				ninePoints [8]};
			acceptableCalibrationDistance = Vector2.Distance (ScreenTransformations.AdcsToScreenPoint(ninePoints [0]), ScreenTransformations.AdcsToScreenPoint(ninePoints [4])) / 2;
			break;
		case 3:
			calibPointsADCS = new Vector2[3]{ 
				ninePoints [3], 
				ninePoints [4], 
				ninePoints [5] };
			acceptableCalibrationDistance = Vector2.Distance (ScreenTransformations.AdcsToScreenPoint(ninePoints [0]), ScreenTransformations.AdcsToScreenPoint(ninePoints [1])) / 2;
			break;
		case 1:
			Vector2[] originalPoints = new Vector2[numCalibPoints];
			switch (numCalibPoints) {
			case 9:
				originalPoints = ninePoints;
				break;
			case 5:
				originalPoints = new Vector2[5] {
					ninePoints [0],
					ninePoints [2],
					ninePoints [4],
					ninePoints [6],
					ninePoints [8]
				};
				break;
			case 3:
				originalPoints = new Vector2[3] { 
					ninePoints [3], 
					ninePoints [4], 
					ninePoints [5]
				};
				break;
			}
			calibPointsADCS = new Vector2[1]{ originalPoints [recalibratePoint - 1] };
			break;
		}
	}

	private float CheckBlink(float blinkStartTime, GameObject circle){
		if (circle.activeInHierarchy && Time.time - blinkStartTime > blinkOnDuration) {
			circle.SetActive (false);
			blinkStartTime = Time.time;
		} else if (!circle.activeInHierarchy && Time.time - blinkStartTime > blinkOffDuration) {
			circle.SetActive (true);
			blinkStartTime = Time.time;
		}
		return blinkStartTime;
	}


	void ShrinkCalibCircle(){
		proportionOfShrinkTime = (Time.time - epochStartTime) / calibCircleShrinkTime;
		if (proportionOfShrinkTime > 1) {
			proportionOfShrinkTime = 1;
			calibBigCircle.transform.localScale = new Vector3 (bigCircleShrinkTargetSize, bigCircleShrinkTargetSize, bigCircleShrinkTargetSize);
		} else {
			float newScale = bigCircleMaxScale[0] * ( 1 - ((1 - bigCircleShrinkTargetSize) * proportionOfShrinkTime));
			calibBigCircle.transform.localScale = new Vector3 (newScale, newScale, newScale);
		}
	}
//
//	public Vector3 ScreenTransformations.AdcsToScreenPoint(Vector2 adcsPoint){
//		Vector3 screenPoint = Vector3.zero;
//		if (Application.isEditor) {
//			screenPoint = new Vector3 (adcsPoint.x * Screen.width, (monitorDetails.pixelRes[1] - editorDistanceFromOrigin) - (adcsPoint.y * monitorDetails.pixelRes[1]), cam.nearClipPlane);
//		} else {
//			screenPoint = new Vector3 (adcsPoint.x * Screen.width, monitorDetails.pixelRes[1] - (adcsPoint.y * monitorDetails.pixelRes[1]), cam.nearClipPlane);
//		}
//		return screenPoint;
//	}

	public void RecordCalibrationResult(string calibString){
		try{
			Debug.Log(calibString);
			string[] calibLines = calibString.Split('\n');
			for (int i = 0; i < calibLines.Length; i++) {
				string[] splitLine = calibLines [i].Split ('\t');
				//should have something to deal with CALIB line, but too lazy right now
				if (splitLine [0].Equals ("Status")) {
					if (splitLine [1].Equals ("calibration_status_success")) {
						calibResult.calibrationStatus = true;
					} //else {
					//uhoh
					//}
				} else if (splitLine [1].Equals ("calibration_result")) {
					int iPoint = int.Parse (splitLine [2]);// - 1;
					if (iPoint > calibResult.results.Count){
						iPoint = iPoint - 1; //for some reason Tobii has some eyetrackers use 0 as the first point and others use 1
					}
					int iSample = int.Parse (splitLine [3]);
					string[] pointPosStr = splitLine [4].Substring (1, splitLine [4].Length - 2).Split (',');
					Vector2 pointPos = new Vector2 (float.Parse (pointPosStr [0].Trim ()), float.Parse (pointPosStr [1].Trim ()));
					if (pointPos == Vector2.zero) {
						continue; //some eyetrackers (X=120) add a bunch of zero-coordinates to the calibration
					}
					string eye = splitLine [5];
					bool sampleValidity;
					if (splitLine [6].Equals ("validity_valid_and_used")) {
						sampleValidity = true;
					} else {
						sampleValidity = false;
					}
					string[] samplePosStr = splitLine [7].Substring (1, splitLine [7].Length - 2).Split (',');
					//valid samples are 16 decimal places, which is way more than needed, so we trim them, but invalid ones have position (0,0), so only 1 long, hence the need for a min.
					float sampleX = float.Parse (samplePosStr [0].Trim ().Substring (0, Mathf.Min (5, samplePosStr [0].Trim ().Length - 1)));
					float sampleY = float.Parse (samplePosStr [1].Trim ().Substring (0, Mathf.Min (5, samplePosStr [1].Trim ().Length - 1)));
					Vector2 samplePos = new Vector2 (sampleX, sampleY);

					if (iPoint > calibResult.results.Count - 1) {
						CalibrationPointResult pointResult = new CalibrationPointResult ();
						pointResult.testPoint = pointPos;
						calibResult.results.Add (pointResult);
					}
					calibResult.results [iPoint].AddCalibSample (eye, sampleValidity, samplePos);
				}
			}
		}catch{
			print (calibString);
		}

	}

	public void DisplayCalibrationResults(){
		for (int i = 0; i < calibResult.results.Count; i++) {
//			for (int j = 0; j < calibResult.results [i].leftSamples.Count; j++) {
//				Vector2 sampleL = calibResult.results [i].leftSamples [j].sample;
//			}
//			for (int j = 0; j < calibResult.results [i].rightSamples.Count; j++) {
//				Vector2 sampleL = calibResult.results [i].rightSamples [j].sample;
//			}
			calibResult.results [i].resultDisplay = exptInfo.DrawCalibResult ("CalibResult " + (i+1), widthCm, 1, calibResult.results [i]);
		}
	}


	private void DiscardCalibrationPoint(int point){
		udpManager.SendString ("ET\tdiscard_calibration_at_point\tfloat " + ninePoints [point].x.ToString () +
			"\tfloat " + ninePoints [point].y.ToString ());
	}

	private void ClearCalibResults(){
		ClearCalibVisuals ();
		calibResult = new CalibrationResult();
	}

	private void ClearCalibVisuals(){
		for (int i = 0; i < calibResult.results.Count; i++) {
			Destroy (calibResult.results [i].resultDisplay);
		}
	}

}
