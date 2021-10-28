using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using USE_States;
using USE_ExperimentTemplate;
using WWW_Namespace;

public class WWW_TrialLevel : ControlLevel_Trial_Template
{
	//This variable is required for most tasks, and is defined as the output of the GetCurrentTrialDef function 
	public WWW_TrialDef CurrentTrialDef => GetCurrentTrialDef<WWW_TrialDef>();

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
	private int numOfClicks, clickCount;

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
		reward.SetActive(false);
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
			GameObject rewardClone = Instantiate(reward, pos, reward.transform.rotation, container.transform);
			rewardClone.transform.Translate(new Vector3(i * width, 0, 0), Space.World);
			rewardClone.name = "Reward" + leftRightChoice + (i + 1);
			//rewardClone.transform.parent = container.transform;
			rewardClone.SetActive(true);
		}
	}


	//public override void PopulateCurrentTrialVariables()
	//{
	//	//CurrentTrialDef = (EffortControl_TrialDef)TrialDefs[TrialCount_InBlock];
	//	//CurrentTrialDef.NumOfClicksLeft = CurrentTrialDef.NumOfClicksLeft;
	//	//CurrentTrialDef.NumOfClicksRight = CurrentTrialDef.NumOfClicksRight;
	//	//CurrentTrialDef.NumOfCoinsLeft = CurrentTrialDef.NumOfCoinsLeft;
	//	//CurrentTrialDef.NumOfCoinsRight = CurrentTrialDef.NumOfCoinsRight;
	//	//CurrentTrialDef.ClicksPerOutline = CurrentTrialDef.ClicksPerOutline;
	//}
}






