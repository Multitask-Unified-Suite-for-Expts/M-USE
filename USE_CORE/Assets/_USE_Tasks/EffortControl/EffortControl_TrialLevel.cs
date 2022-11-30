using UnityEngine;
using System.Collections.Generic;
using USE_States;
using USE_StimulusManagement;
using EffortControl_Namespace;
using UnityEngine.UI;
using System;
using System.Linq;
using System.IO;
using USE_Settings;
using USE_ExperimentTemplate_Trial;
using USE_ExperimentTemplate_Block;

//testing

public class EffortControl_TrialLevel : ControlLevel_Trial_Template
{
    public EffortControl_TrialDef CurrentTrialDef => GetCurrentTrialDef<EffortControl_TrialDef>();
    //This variable is required for most tasks, and is defined as the output of the GetCurrentTrialDef function 

    // game object variables
    private GameObject initButton, fb, goCue, stimLeft, stimRight, trialStim, clickMarker, balloonContainerLeft,
    balloonContainerRight, balloonOutline, prize, rewardContainerLeft, rewardContainerRight, reward;
    //private Camera cam;

    // trial config variables
    //public float InitialChoiceDuration;
    // public float RewardDelay;
    // public float StartDelay;
    // public float StepsToProgressUpdate;

    // timing variable 
    private float? avgClickTime;

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
    private Vector3 scaleUpAmountLeft;
    private Vector3 scaleUpAmountRight;
    private Vector3 scaleUpAmount;
    public Vector3 maxScale;

    // misc variables
    private Ray mouseRay;
    private Color red;
    private Color gray;

    private bool variablesLoaded;
    public string MaterialFilePath;
    public string ContextPath;

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
        State InitTrial = new State("InitTrial");
        State InitDelay = new State("InitDelay");
        State ChooseBalloon = new State("ChooseBalloon");
        State InflateBalloon = new State("InflateBalloon");
        State Feedback = new State("Feedback");
        State FeedbackDelay = new State("FeedbackDelay");
        State ITI = new State("ITI");
        AddActiveStates(new List<State> { InitTrial, InitDelay, ChooseBalloon, InflateBalloon, Feedback, FeedbackDelay, ITI });

        SelectionHandler<EffortControl_StimDef> mouseHandler = new SelectionHandler<EffortControl_StimDef>();

        //AddInitializationMethod(() => { trialData.DefineDataController(); trialData.CreateFile(); });

        AddInitializationMethod(() => {
            if (!variablesLoaded)
            {
                variablesLoaded = true;
                loadVariables();
            }
        });

        SetupTrial.SpecifyTermination(() => true, InitTrial);

        // define initScreen state
        MouseTracker.AddSelectionHandler(mouseHandler, InitTrial);
        InitTrial.AddInitializationMethod(() => {

            trialCount++;

            avgClickTime = null;

            ResetRelativeStartTime();
            disableAllGameobjects();
            initButton.SetActive(true);
            ChangeColor(stimRight, red);
            ChangeColor(stimLeft, red);

            clickCount = 0;
            response = -1;

            maxScale = new Vector3(50, 0, 50);
            scaleUpAmountLeft = maxScale / CurrentTrialDef.NumOfClicksLeft;
            scaleUpAmountRight = maxScale / CurrentTrialDef.NumOfClicksRight;

            // ContextPath = GetContextNestedFilePath("LinearDark"); //Using this for now until MFP is stated in config. 
            ContextPath = GetContextNestedFilePath(CurrentTrialDef.ContextName);
            RenderSettings.skybox = CreateSkybox(ContextPath);
        });

        InitTrial.SpecifyTermination(() => mouseHandler.SelectionMatches(initButton), InitDelay);
        InitTrial.AddDefaultTerminationMethod(() => initButton.SetActive(false));

        InitDelay.AddTimer(() => CurrentTrialDef.InitToBalloonDelay, ChooseBalloon);

        // Define stimOn state
        ChooseBalloon.AddInitializationMethod(() => {
            stimRight.SetActive(true);
            stimLeft.SetActive(true);
            trialStim = null;
            createBalloons(CurrentTrialDef.NumOfClicksLeft, scaleUpAmountLeft, CurrentTrialDef.ClicksPerOutline, stimLeft.transform.position, balloonContainerLeft);
            createBalloons(CurrentTrialDef.NumOfClicksRight, scaleUpAmountRight, CurrentTrialDef.ClicksPerOutline, stimRight.transform.position, balloonContainerRight);
            createRewards(CurrentTrialDef.NumOfCoinsLeft, rewardContainerLeft.transform.position, rewardContainerLeft);
            createRewards(CurrentTrialDef.NumOfCoinsRight, rewardContainerRight.transform.position, rewardContainerRight);
        });

        MouseTracker.AddSelectionHandler(mouseHandler, ChooseBalloon);
        ChooseBalloon.AddUpdateMethod(() => {
            // check if user clicks on left or right
            GameObject hit = mouseHandler.SelectedGameObject;
            if (hit == null) return;
            if (hit.transform.name == "StimLeft")
            {
                numChosenLeft++;
                leftRightChoice = "left";

                ChangeColor(stimRight, gray);
                ChangeContainerColor(balloonContainerRight, gray);
                DestroyContainerChild(rewardContainerRight);

                trialStim = hit.transform.gameObject;
                numOfClicks = CurrentTrialDef.NumOfClicksLeft;
                scaleUpAmount = scaleUpAmountLeft;
            }
            else if (hit.transform.name == "StimRight")
            {
                numChosenRight++;
                leftRightChoice = "right";

                ChangeColor(stimLeft, gray);
                ChangeContainerColor(balloonContainerLeft, gray);
                DestroyContainerChild(rewardContainerLeft);

                trialStim = hit.transform.gameObject;
                numOfClicks = CurrentTrialDef.NumOfClicksRight;
                scaleUpAmount = scaleUpAmountRight;
            }
        });
        ChooseBalloon.SpecifyTermination(() => trialStim != null, InflateBalloon, () => {
            if (leftRightChoice == "left") {
                DestroyContainerChild(balloonContainerRight);
                stimRight.SetActive(false);
            } else {
                DestroyContainerChild(balloonContainerLeft);
                stimLeft.SetActive(false);
            }
        });

        // define collectResponse state
        List<float> clickTimings = new List<float>();
        float timeTracker = 0;

        InflateBalloon.AddInitializationMethod(() => {
            goCue.SetActive(true);
            clickTimings = new List<float>();
            timeTracker = Time.time;
        });
        MouseTracker.AddSelectionHandler(mouseHandler, InflateBalloon);
        InflateBalloon.AddUpdateMethod(() => {
            if (InputBroker.GetMouseButtonDown(0))
            {
                if (mouseHandler.SelectionMatches(trialStim))
                {
                    //add to clicktimings
                    clickTimings.Add(Time.time - timeTracker);
                    timeTracker = Time.time;

                    clickMarker.transform.position = mouseHandler.SelectedGameObject.transform.position;
                    clickMarker.SetActive(true);

                    if (clickCount == 0) {
                        GameObject container = (leftRightChoice == "left") ? balloonContainerLeft : balloonContainerRight;
                        Vector3 scale = container.transform.GetChild(0).transform.localScale;
                        scale.y = trialStim.transform.localScale.y;
                        trialStim.transform.localScale = scale;
                    } else {
                        trialStim.transform.localScale += scaleUpAmount;
                    }
                    clickCount++;
                    Debug.Log("Clicked balloon " + clickCount + " times.");
                }
                else
                {
                    Debug.Log("Clicked on something else");
                    // cam.backgroundColor = Color.red;
                }

                // disable gameObject if the user clicks enough time
                if (clickCount >= numOfClicks)
                {
                    Debug.Log("User clicked enough times, popping balloon");
                    clickMarker.SetActive(false);
                    response = 1;

                    //calculate average time
                    avgClickTime = clickTimings.Average();
                }
            }

            if (InputBroker.GetMouseButtonUp(0))
            {
                clickMarker.SetActive(false);
            }
        });

        InflateBalloon.AddTimer(45f, Feedback);
        InflateBalloon.SpecifyTermination(() => clickCount >= numOfClicks, FeedbackDelay);
        InflateBalloon.AddDefaultTerminationMethod(() => {
            goCue.SetActive(false);
            if (response == 1) {
                if (leftRightChoice == "left") DestroyContainerChild(balloonContainerLeft);
                else DestroyContainerChild(balloonContainerRight);
            }
        });

        FeedbackDelay.AddTimer(() => CurrentTrialDef.CompleteToFeedbackDelay, Feedback);
        FeedbackDelay.AddDefaultTerminationMethod(() => {
            if (response == 1) {
                trialStim.SetActive(false);
                trialStim.transform.localScale = trialStimInitLocalScale;
            }
        });

        Feedback.AddInitializationMethod(() => {
            if (response == 1)
            {
                // set prize's position to the position of the balloon
                prize.transform.position = trialStim.transform.position + new Vector3(0f, .5f, 0f);
                prize.SetActive(true);
                fb.GetComponent<RawImage>().color = Color.green;
                if (SyncBoxController != null) {
                    if (leftRightChoice == "left")
                        SyncBoxController.SendRewardPulses(CurrentTrialDef.NumOfPulsesLeft, CurrentTrialDef.PulseSizeLeft);
                    else
                        SyncBoxController.SendRewardPulses(CurrentTrialDef.NumOfPulsesRight, CurrentTrialDef.PulseSizeRight);
                }
            }
            else
            {
                fb.GetComponent<RawImage>().color = Color.red;
            }
            fb.SetActive(true);
        });

        Feedback.AddTimer(1f, ITI, () => fb.SetActive(false));

        //Define iti state
        ITI.AddInitializationMethod(() => {
            trialStim.SetActive(false);
            DestroyContainerChild(balloonContainerLeft);
            DestroyContainerChild(balloonContainerRight);
        });
        ITI.AddTimer(2f, FinishTrial, () => {
            Debug.Log("Trial" + trialCount + " completed");
            DestroyContainerChild(balloonContainerLeft);
            DestroyContainerChild(balloonContainerRight);
            DestroyContainerChild(rewardContainerLeft);
            DestroyContainerChild(rewardContainerRight);
            trialStim.transform.localScale = trialStimInitLocalScale;
            //trialData.AppendData(); 
            //trialData.WriteData();
        });
        TrialData.AddDatum("NumOfClicks", () => numOfClicks);
        //AddTerminationSpecification(() => trialCount > numTrials);


        // Additional recorded data 
        // TrialData.AddDatum("TrialName", () => CurrentTrialDef.TrialName);
        // TrialData.AddDatum("TrialCode", () => CurrentTrialDef.TrialCode);
        // TrialData.AddDatum("ContextNum", () => CurrentTrialDef.ContextNum);
        // TrialData.AddDatum("ConditionName", () => CurrentTrialDef.ConditionName);
        // TrialData.AddDatum("ContextName", () => CurrentTrialDef.ContextName);
        TrialData.AddDatum("ClicksPerOutline", () => CurrentTrialDef.ClicksPerOutline);
        // TrialData.AddDatum("InitialChoiceMinDuration", () => CurrentTrialDef.InitialChoiceMinDuration);
        // TrialData.AddDatum("StarttoTapDispDelay", () => CurrentTrialDef.StarttoTapDispDelay);
        // TrialData.AddDatum("FinalTouchToVisFeedbackDelay", () => CurrentTrialDef.FinalTouchToVisFeedbackDelay);
        // TrialData.AddDatum("FinalTouchToRewardDelay", () => CurrentTrialDef.FinalTouchToRewardDelay);

        TrialData.AddDatum("NumOfClicksLeft", () => CurrentTrialDef.NumOfClicksLeft);
        TrialData.AddDatum("NumOfClicksRight", () => CurrentTrialDef.NumOfClicksRight);
        TrialData.AddDatum("NumOfCoinsLeft", () => CurrentTrialDef.NumOfCoinsLeft);
        TrialData.AddDatum("NumOfCoinsRight", () => CurrentTrialDef.NumOfCoinsRight);
        TrialData.AddDatum("ChosenSide", () => leftRightChoice.ToUpper());
        TrialData.AddDatum("TimeTakenToChoose", () => ChooseBalloon.TimingInfo.Duration);
        TrialData.AddDatum("AverageClickTimes", () => avgClickTime);



    }


    private string GetContextNestedFilePath(string contextName)
    {
        string backupContextName = "LinearDark";
        string contextPath = "";

        string[] filePaths = Directory.GetFiles(MaterialFilePath, $"{contextName}*", SearchOption.AllDirectories);

        if (filePaths.Length >= 1)
            contextPath = filePaths[0];
        else
        {
            contextPath = Directory.GetFiles(MaterialFilePath, backupContextName, SearchOption.AllDirectories)[0]; //Use Default LinearDark if can't find file.
            Debug.Log($"Context File Path Not Found. Defaulting to {backupContextName}.");
        }

        return contextPath;
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
        red = stimLeft.GetComponent<Renderer>().material.color;
        gray = new Color(0.5f, 0.5f, 0.5f);
        reward = GameObject.Find("Reward");

        fbInitLocalScale = fb.transform.localScale;
        trialStimInitLocalScale = stimLeft.transform.localScale;

        initButton.SetActive(false);
        fb.SetActive(false);
        goCue.SetActive(false);
        clickMarker.SetActive(false);
        balloonOutline.SetActive(false);
        prize.SetActive(false);
        reward.SetActive(false);

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
 //   protected override void DefineTrialStims()
   // {
        //Define StimGroups consisting of StimDefs whose gameobjects will be loaded at TrialLevel_SetupTrial and 
        //destroyed at TrialLevel_Finish
    //}
    
}
