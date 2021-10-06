using System;
using USE_States;
using USE_Data;
using USE_ExperimentTemplate;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using EffortControl_Namespace;

public class EffortControl_TrialLevel_2 : ControlLevel_Trial_Template
{
	public EffortControl_TrialDef CurrentTrialDef;

	// game object variables
	private GameObject initButton, fb, goCue, stimLeft, stimRight, trialStim, clickMarker, balloonContainerLeft,
	balloonContainerRight, balloonOutline, prize, rewardContainerLeft, rewardContainerRight, reward;
	//private Camera cam;

	// trial config variables
	//public float InitialChoiceDuration;
	// public float RewardDelay;
	// public float StartDelay;
	// public float StepsToProgressUpdate;

	// effort reward variables
	private int numOfClicks, clickCount, numOfClicksLeft, numOfClicksRight, numOfCoinsLeft, numOfCoinsRight, clicksPerOutline;

	//public EffortControl_TrialDef[] trialDefs;
	//public EffortControl_TrialDef currentTrialDef;

	// trial count variables
	private int numChosenLeft, numChosenRight;
	[HideInInspector]
	public String leftRightChoice;
	[System.NonSerialized] public int response = -1, trialCount = -1;

	// vector3 variables
	private Vector3 trialStimInitLocalScale;
	private Vector3 fbInitLocalScale;
	private Vector3 sliderInitPosition;
	private Vector3 scaleUpAmountLeft;
	private Vector3 scaleUpAmountRight;
	private Vector3 scaleUpAmount;
	public Vector3 maxScale;

	// misc variables
	private Ray mouseRay;
	private Color red;
	private Color gray;
	private Slider slider;
	private float sliderValueIncreaseAmount;

	private bool variablesLoaded;

	//data control variables
	//public bool storeData;
	//public string dataPath;
	//public string dataFileName;

	public override void DefineControlLevel()
	{

		//EffortControl_TrialDataController trialData = GameObject.Find("DataControllers").GetComponent<EffortControl_TrialDataController>();
		//trialData.storeData = storeData;
		//trialData.folderPath = dataPath;
		//trialData.fileName = dataFileName;

		//define States within this Control Level
		State startButton = new State("StartButton");
		State stimOn = new State("StimOn");
		State collectResponse = new State("Response");
		State feedback = new State("Feedback");
		State iti = new State("ITI");
		AddActiveStates(new List<State> { startButton, stimOn, collectResponse, feedback, iti });

		//AddInitializationMethod(() => { trialData.DefineDataController(); trialData.CreateFile(); });

		AddInitializationMethod(() => {
			if (!variablesLoaded)
			{
				variablesLoaded = true;
				loadVariables();
			}
		});

		SetupTrial.SpecifyTermination(() => true, startButton);

		// define initScreen state
		startButton.AddInitializationMethod(() => {
			trialCount++;


			ResetRelativeStartTime();
			disableAllGameobjects();
			initButton.SetActive(true);
			ChangeColor(stimRight, red);
			ChangeColor(stimLeft, red);

			clickCount = 0;
			response = -1;

			slider.gameObject.transform.position = sliderInitPosition;
			slider.value = 0;

			scaleUpAmountLeft = maxScale / numOfClicksLeft;
			scaleUpAmountRight = maxScale / numOfClicksRight;
			//Debug.Log("ScaleUpAmountLeft" + scaleUpAmountLeft);
			//Debug.Log("ScaleUpAmountRight" + scaleUpAmountRight);
		});

		startButton.AddUpdateMethod(() => {
			if (InputBroker.GetMouseButtonDown(0))
			{
				mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
				RaycastHit hit;
				if (Physics.Raycast(mouseRay, out hit))
				{
					if (hit.transform.name == "StartButton")
					{
						response = 0;
					}
				}
			}
		});

		startButton.SpecifyTermination(() => response == 0, stimOn);
		startButton.AddDefaultTerminationMethod(() => initButton.SetActive(false));

		// Define stimOn state
		stimOn.AddInitializationMethod(() => {
			stimRight.SetActive(true);
			stimLeft.SetActive(true);
			trialStim = null;
			createBalloons(numOfClicksLeft, scaleUpAmountLeft, clicksPerOutline, stimLeft.transform.position, balloonContainerLeft);
			createBalloons(numOfClicksRight, scaleUpAmountRight, clicksPerOutline, stimRight.transform.position, balloonContainerRight);
			createRewards(numOfCoinsLeft, rewardContainerLeft.transform.position, rewardContainerLeft);
			createRewards(numOfCoinsRight, rewardContainerRight.transform.position, rewardContainerRight);
		});

		stimOn.AddUpdateMethod(() => {
			// check if user clicks on left or right
			if (InputBroker.GetMouseButtonDown(0))
			{
				mouseRay = Camera.main.ScreenPointToRay(InputBroker.mousePosition);
				RaycastHit hit;
				if (Physics.Raycast(mouseRay, out hit))
				{
					if (hit.transform.name == "StimLeft")
					{
						numChosenLeft++;
						Debug.Log("Chose left");
						leftRightChoice = "left";

						ChangeColor(stimRight, gray);
						ChangeContainerColor(balloonContainerRight, gray);
						DestroyContainerChild(rewardContainerRight);
						slider.transform.Translate(-400f, 0f, 0f);

						trialStim = hit.transform.gameObject;
						numOfClicks = numOfClicksLeft;
						scaleUpAmount = scaleUpAmountLeft;
					}
					else if (hit.transform.name == "StimRight")
					{
						numChosenRight++;
						Debug.Log("Chose right");
						leftRightChoice = "right";

						ChangeColor(stimLeft, gray);
						ChangeContainerColor(balloonContainerLeft, gray);
						DestroyContainerChild(rewardContainerLeft);
						slider.transform.Translate(400f, 0f, 0f);

						trialStim = hit.transform.gameObject;
						numOfClicks = numOfClicksRight;
						scaleUpAmount = scaleUpAmountRight;
					}
					else
					{
						Debug.Log("Didn't click on any balloon");
					}
				}
			}
		});
		stimOn.SpecifyTermination(() => trialStim != null, collectResponse);
		stimOn.AddDefaultTerminationMethod(() => {
			sliderValueIncreaseAmount = (100f / numOfClicks) / 100f;
			slider.gameObject.SetActive(true);
		});

		// define collectResponse state
		collectResponse.AddInitializationMethod(() => goCue.SetActive(true));
		collectResponse.AddUpdateMethod(() => {
			if (InputBroker.GetMouseButtonDown(0))
			{
				// store the point of click
				mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
				RaycastHit hit;

				if (Physics.Raycast(mouseRay, out hit))
				{
					// if the raycast hits the trialStim gameobject
					if (hit.transform.gameObject == trialStim)
					{
						slider.value += sliderValueIncreaseAmount;
						clickMarker.transform.position = hit.point;
						clickMarker.SetActive(true);

						trialStim.transform.localScale += scaleUpAmount;
						clickCount++;
						Debug.Log("Clicked balloon " + clickCount + " times.");
					}
					else
					{
						Debug.Log("Clicked on something else");
						// cam.backgroundColor = Color.red;
					}
				}

				// disable gameObject if the user clicks enough time
				if (clickCount >= numOfClicks)
				{
					Debug.Log("User clicked enough times, popping balloon");
					trialStim.SetActive(false);
					trialStim.transform.localScale = trialStimInitLocalScale;
					clickMarker.SetActive(false);
					response = 1;
					// set prize's position to the position of the balloon
					prize.transform.position = trialStim.transform.position + new Vector3(0f, .5f, 0f);
					prize.SetActive(true);
				}
			}

			if (InputBroker.GetMouseButtonUp(0))
			{
				clickMarker.SetActive(false);
			}
		});

		collectResponse.AddTimer(15f, feedback);
		collectResponse.SpecifyTermination(() => clickCount >= numOfClicks, feedback);
		collectResponse.AddDefaultTerminationMethod(() => {
			goCue.SetActive(false);
			if (leftRightChoice == "left")
			{
				DestroyContainerChild(balloonContainerLeft);
			}
			else
			{
				DestroyContainerChild(balloonContainerRight);
			}
		});

		feedback.AddInitializationMethod(() => {
			if (response == 1)
			{
				fb.GetComponent<RawImage>().color = Color.green;
			}
			else
			{
				fb.GetComponent<RawImage>().color = Color.red;
			}
			fb.SetActive(true);
			slider.gameObject.SetActive(false);
		});

		feedback.AddTimer(1f, iti, () => fb.SetActive(false));

		//Define iti state
		iti.AddInitializationMethod(() => trialStim.SetActive(false));
		iti.AddTimer(2f, FinishTrial);
		iti.AddDefaultTerminationMethod(() => {
			Debug.Log("Trial" + trialCount + " completed");
			DestroyContainerChild(balloonContainerLeft);
			DestroyContainerChild(balloonContainerRight);
			DestroyContainerChild(rewardContainerLeft);
			DestroyContainerChild(rewardContainerRight);
			trialStim.transform.localScale = trialStimInitLocalScale;
			//trialData.AppendData(); 
			//trialData.WriteData();
		});


		//AddTerminationSpecification(() => trialCount > numTrials);
	}

	// set all gameobjects to setActive false
	void disableAllGameobjects()
	{
		initButton.SetActive(false);
		fb.SetActive(false);
		// goCue.SetActive(false);
		stimLeft.SetActive(false);
		stimRight.SetActive(false);
		clickMarker.SetActive(false);
		prize.SetActive(false);
		slider.gameObject.SetActive(false);
		balloonOutline.SetActive(false);
	}

	// method for presetting variables
	void loadVariables()
	{
		initButton = GameObject.Find("StartButton");
		fb = GameObject.Find("FB");
		goCue = GameObject.Find("ResponseCue");
		stimLeft = GameObject.Find("StimLeft");
		stimRight = GameObject.Find("StimRight");
		clickMarker = GameObject.Find("ClickMarker");
		balloonContainerLeft = GameObject.Find("BalloonContainerLeft");
		balloonContainerRight = GameObject.Find("BalloonContainerRight");
		balloonOutline = GameObject.Find("OutlineBest");
		prize = GameObject.Find("Prize2");
		rewardContainerLeft = GameObject.Find("RewardContainerLeft");
		rewardContainerRight = GameObject.Find("RewardContainerRight");
		slider = GameObject.Find("Slider").GetComponent<Slider>();
		red = stimLeft.GetComponent<Renderer>().material.color;
		gray = new Color(0.5f, 0.5f, 0.5f);
		reward = GameObject.Find("Reward");

		fbInitLocalScale = fb.transform.localScale;
		trialStimInitLocalScale = stimLeft.transform.localScale;
		sliderInitPosition = slider.gameObject.transform.position;

		initButton.SetActive(false);
		fb.SetActive(false);
		goCue.SetActive(false);
		clickMarker.SetActive(false);
		balloonOutline.SetActive(false);
		prize.SetActive(false);
		GameObject.Find("Slider").SetActive(false);

		//cam = Camera.main.GetComponent<Camera>();
	}

	// method to place balloon 
	void placeBalloon(GameObject balloon)
	{
		// set the position of the balloon 1z in front of the camera
		balloon.transform.position = new Vector3(balloon.transform.position.x, balloon.transform.position.y, 1f);

	}

	void createBalloons(int numBalloons, Vector3 scaleUpAmount, int clickPerOutline, Vector3 pos, GameObject container)
	{
		for (int i = clickPerOutline; i <= numBalloons; i += clickPerOutline)
		{
			// get vector from camera to pos 
			Vector3 vectorToPos = pos - Camera.main.transform.position;
			// get position in distnce 10 from pos along vectorToPos
			Vector3 posInDist = vectorToPos.normalized;

			GameObject balloonClone = Instantiate(balloonOutline, pos, balloonOutline.transform.rotation);
			balloonClone.transform.parent = container.transform;
			balloonClone.name = "Clone" + (i + 1);
			balloonClone.transform.localScale += (i) * scaleUpAmount;
			balloonClone.GetComponent<Renderer>().material.color = red;

			balloonClone.SetActive(true);
		}
	}

	void DestroyContainerChild(GameObject container)
	{
		var children = new List<GameObject>();
		foreach (Transform child in container.transform) children.Add(child.gameObject);
		children.ForEach(child => Destroy(child));
	}

	void ChangeColor(GameObject obj, Color color)
	{
		var material = obj.GetComponent<Renderer>().material;
		material.color = color;
	}

	void ChangeContainerColor(GameObject container, Color color)
	{
		var balloons = new List<GameObject>();
		foreach (Transform child in container.transform) balloons.Add(child.gameObject);
		balloons.ForEach(child => {
			var material = child.GetComponent<Renderer>().material;
			material.color = color;
		});
	}

	// // method to create numOfRewards rewards
	void createRewards(int numOfRewards, Vector3 pos, GameObject container)
	{
		// get width of reward object
		float width = reward.GetComponent<Renderer>().bounds.size.x;
		pos -= new Vector3(((numOfRewards - 1) * (width / 2)), 0, 0);
		for (int i = 0; i < numOfRewards; i++)
		{
			GameObject rewardClone = Instantiate(reward, pos, reward.transform.rotation);
			rewardClone.transform.Translate(i * width, 0, 0);
			rewardClone.name = "Reward" + leftRightChoice + (i + 1);
			rewardClone.transform.parent = container.transform;
			rewardClone.SetActive(true);
		}
	}


	public override void PopulateCurrentTrialVariables()
	{
		CurrentTrialDef = (EffortControl_TrialDef)TrialDefs[TrialCount_InBlock];
		numOfClicksLeft = CurrentTrialDef.NumOfClicksLeft;
		numOfClicksRight = CurrentTrialDef.NumOfClicksRight;
		numOfCoinsLeft = CurrentTrialDef.NumOfCoinsLeft;
		numOfCoinsRight = CurrentTrialDef.NumOfCoinsRight;
		clicksPerOutline = CurrentTrialDef.ClicksPerOutline;
	}
}






