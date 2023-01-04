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
using Newtonsoft.Json.Linq;
using UnityEngine.XR;
//using static UnityEditor.PlayerSettings;

public class EffortControl_TrialLevel : ControlLevel_Trial_Template
{
    public EffortControl_TrialDef CurrentTrial => GetCurrentTrialDef<EffortControl_TrialDef>();
    public EffortControl_TaskLevel CurrentTaskLevel => GetTaskLevel<EffortControl_TaskLevel>(); //this needed?

    // game object variables
    private GameObject StartButton, fb, goCue, stimLeft, stimRight, trialStim, clickMarker, balloonContainerLeft,
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
    private int NumClicks, clickCount;

    // trial count variables
    private int numChosenLeft, numChosenRight;
    [HideInInspector]
    public String Choice;
    [System.NonSerialized] public int response = -1;

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

    public float ChooseDuration;

    public Vector3 LeftContainerOriginalPosition;
    public Vector3 RightContainerOriginalPosition;
    public Vector3 LeftRewardContainerOriginalPosition;
    public Vector3 RightRewardContainerOriginalPosition;
    public Vector3 LeftStimOriginalPosition;
    public Vector3 RightStimOriginalPosition;

    public AudioClip SelectionMade_Audio;
    public AudioClip InflateThenPop_Audio;
    public AudioClip InflateBalloon_Audio;

    public bool AudioPlayed;

    public override void DefineControlLevel()
    {
        //define States within this Control Level
        State InitTrial = new State("InitTrial");
        State InitDelay = new State("InitDelay");
        State ChooseBalloon = new State("ChooseBalloon");
        State CenterSelection = new State("CenterSelection");
        State InflateBalloon = new State("InflateBalloon");
        State Feedback = new State("Feedback");
        State FeedbackDelay = new State("FeedbackDelay");
        State ITI = new State("ITI");
        AddActiveStates(new List<State> { InitTrial, InitDelay, ChooseBalloon, CenterSelection, InflateBalloon, Feedback, FeedbackDelay, ITI });

        SelectionHandler<EffortControl_StimDef> mouseHandler = new SelectionHandler<EffortControl_StimDef>();

        TokenFBController.enabled = false;
        TokenFBController.SetFlashingTime(1.5f);
        TokenFBController.tokenBoxYOffset = 17;
        TokenFBController.tokenSize = 110;
        TokenFBController.tokenSpacing = -15;

        if(AudioFBController != null)
            AddAudioClips();

        //SETUP TRIAL state -----------------------------------------------------------------------------------------------------
        SetupTrial.AddInitializationMethod(() =>
        {
            RenderSettings.skybox = CreateSkybox(GetContextNestedFilePath(CurrentTrial.ContextName));

            if (!variablesLoaded)
                LoadVariables();

            SetTrialSummaryString();
        });
        SetupTrial.SpecifyTermination(() => true, InitTrial);

        MouseTracker.AddSelectionHandler(mouseHandler, InitTrial);
        //INIT Trial state -------------------------------------------------------------------------------------------------------
        InitTrial.AddInitializationMethod(() =>
        {
            TokenFBController.enabled = false;

            avgClickTime = null;

            ResetRelativeStartTime(); 
            DisableAllGameobjects();
            StartButton.SetActive(true);
            ChangeColor(stimRight, red);
            ChangeColor(stimLeft, red);

            clickCount = 0;
            response = -1;

            ChooseDuration = 0; //reset how long it took them to choose each trial. 
        });
        InitTrial.SpecifyTermination(() => mouseHandler.SelectionMatches(StartButton), InitDelay, () => StartButton.SetActive(false));

        //INIT DELAY state -------------------------------------------------------------------------------------------------------
        InitDelay.AddTimer(() => CurrentTrial.InitDelayDuration, ChooseBalloon);

        //Choose Balloon state -------------------------------------------------------------------------------------------------------
        ChooseBalloon.AddInitializationMethod(() =>
        {
            ActivateStimAndRewards();

            trialStim = null;

            maxScale = new Vector3(60, 0, 60);
            scaleUpAmountLeft = maxScale / CurrentTrial.NumClicksLeft;
            scaleUpAmountRight = maxScale / CurrentTrial.NumClicksRight;

            CreateBalloons(CurrentTrial.NumClicksLeft, scaleUpAmountLeft, CurrentTrial.ClicksPerOutline, stimLeft.transform.position, balloonContainerLeft);
            CreateBalloons(CurrentTrial.NumClicksRight, scaleUpAmountRight, CurrentTrial.ClicksPerOutline, stimRight.transform.position, balloonContainerRight);
            CreateRewards(CurrentTrial.NumCoinsLeft, rewardContainerLeft.transform.position, rewardContainerLeft);
            CreateRewards(CurrentTrial.NumCoinsRight, rewardContainerRight.transform.position, rewardContainerRight);
            MouseTracker.ResetClickCount(); //purpose?
        });

        MouseTracker.AddSelectionHandler(mouseHandler, ChooseBalloon);
        ChooseBalloon.AddUpdateMethod(() =>
        {
            // check if user clicks on left or right
            GameObject hit = mouseHandler.SelectedGameObject;
            if (hit == null)
                return;

            if (hit.transform.name == "StimLeft")
            {
                numChosenLeft++;
                Choice = "left";

                ChangeColor(stimRight, gray);
                ChangeContainerColor(balloonContainerRight, gray);
                DestroyChildren(rewardContainerRight);

                trialStim = hit.transform.gameObject;
                NumClicks = CurrentTrial.NumClicksLeft;
                scaleUpAmount = scaleUpAmountLeft;
            }
            else if (hit.transform.name == "StimRight")
            {
                numChosenRight++;
                Choice = "right";

                ChangeColor(stimLeft, gray);
                ChangeContainerColor(balloonContainerLeft, gray);
                DestroyChildren(rewardContainerLeft);

                trialStim = hit.transform.gameObject;
                NumClicks = CurrentTrial.NumClicksRight;
                scaleUpAmount = scaleUpAmountRight;
            }
        });
        ChooseBalloon.SpecifyTermination(() => trialStim != null, CenterSelection, () => ChooseDuration = ChooseBalloon.TimingInfo.Duration);

        //Center Selection state -------------------------------------------------------------------------------------------------------
        CenterSelection.AddInitializationMethod(() =>
        {
            if (Choice == "left")
            {
                DestroyChildren(balloonContainerRight);
                stimRight.SetActive(false);
                CenterContainer(balloonContainerLeft, rewardContainerLeft, stimLeft);
            }
            else
            {
                DestroyChildren(balloonContainerLeft);
                stimLeft.SetActive(false);
                CenterContainer(balloonContainerRight, rewardContainerRight, stimRight);
            }
        });
        CenterSelection.AddUpdateMethod(() =>
        {

        });
        CenterSelection.SpecifyTermination(() => true, InflateBalloon); //edit this


        //Inflate Balloon state -------------------------------------------------------------------------------------------------------
        List<float> clickTimings = new List<float>();
        float timeTracker = 0;

        InflateBalloon.AddInitializationMethod(() =>
        {
            if (Choice == "left")
                rewardContainerLeft.SetActive(false);
            else
                rewardContainerRight.SetActive(false);

            TokenFBController.SetTotalTokensNum(Choice == "left" ? CurrentTrial.NumCoinsLeft : CurrentTrial.NumCoinsRight);
            TokenFBController.enabled = true;
            timeTracker = Time.time;
            //goCue.SetActive(true); //Commented out for now!
            AudioFBController.Play("SelectionMade");
        });
        MouseTracker.AddSelectionHandler(mouseHandler, InflateBalloon);
        InflateBalloon.AddUpdateMethod(() =>
        {
            if (mouseHandler.SelectionMatches(trialStim))
            {
                if (clickCount < NumClicks)
                    AudioFBController.Play("Inflate");

                //add to clicktimings
                clickTimings.Add(Time.time - timeTracker);
                timeTracker = Time.time;
                //clickMarker.SetActive(true); //Commenting out for now!!

                //clickMarker.transform.position = mouseHandler.SelectedGameObject.transform.position;

                if (clickCount == 0)
                {
                    GameObject container = (Choice == "left") ? balloonContainerLeft : balloonContainerRight;
                    Vector3 scale = container.transform.GetChild(0).transform.localScale;
                    scale.y = trialStim.transform.localScale.y;
                    trialStim.transform.localScale = scale;
                }
                else
                    trialStim.transform.localScale += scaleUpAmount;
                
                clickCount++;
                mouseHandler.Stop(); //Stop detecting presses until mouse is released. 
            }

            // disable gameObject if the user clicks enough time
            if (clickCount >= NumClicks)
            {
                AudioFBController.Play("InflateThenPop");
                //clickMarker.SetActive(false);
                response = 1;

                avgClickTime = clickTimings.Average(); //Calculate Avg Time
            }

            if (InputBroker.GetMouseButtonUp(0))
            {
                //clickMarker.SetActive(false);
                mouseHandler.Start(); //Start detecting presses again
            }
        });

        InflateBalloon.AddTimer(45f, Feedback); //FIX HERE! CURRENTTRIAL.InflateDuration DOESN'T WORK. 
        InflateBalloon.SpecifyTermination(() => clickCount >= NumClicks, FeedbackDelay);
        InflateBalloon.AddDefaultTerminationMethod(() =>
        {
            //goCue.SetActive(false);
            if (response == 1)
            {
                if (Choice == "left")
                    DestroyChildren(balloonContainerLeft);
                else
                    DestroyChildren(balloonContainerRight);
            }
        });

        //Feedback Delay state -------------------------------------------------------------------------------------------------------
        FeedbackDelay.AddTimer(() => CurrentTrial.FeedbackDelayDuration, Feedback);
        FeedbackDelay.AddDefaultTerminationMethod(() =>
        {
            if (response == 1)
            {
                trialStim.SetActive(false);
                trialStim.transform.localScale = trialStimInitLocalScale;
            }
        });

        //Feedback state -------------------------------------------------------------------------------------------------------
        Feedback.AddInitializationMethod(() => {
            if (response == 1)
            {
                ++CurrentTaskLevel.NumCompletions;
                prize.transform.position = trialStim.transform.position + new Vector3(0f, .5f, 0f); //Set Prize pos to Balloon
                //prize.SetActive(true);
                TokenFBController.AddTokens(prize, Choice == "left" ? CurrentTrial.NumCoinsLeft : CurrentTrial.NumCoinsRight);

                //fb.GetComponent<RawImage>().color = Color.green;
                if (SyncBoxController != null)
                    GiveReward();
                
                AudioPlayed = true;
            }
            else
                //fb.GetComponent<RawImage>().color = Color.red;
            
            //fb.SetActive(true); //Commented this out for now. not sure its purpose on top of Trophy being displayed. 
            CurrentTaskLevel.TotalTouches += MouseTracker.GetClickCount();
        });
        Feedback.SpecifyTermination(() => AudioPlayed && !AudioFBController.IsPlaying() && !TokenFBController.IsAnimating(), ITI, () =>
        {
            TokenFBController.enabled = false;
            AudioPlayed = false;
        });

        //ITI state -------------------------------------------------------------------------------------------------------
        ITI.AddInitializationMethod(() =>
        {
            trialStim.SetActive(false);
            DestroyChildren(balloonContainerLeft);
            DestroyChildren(balloonContainerRight);
        });
        ITI.AddTimer(.5f, FinishTrial, () =>   // HE HARD CODED THIS VALUE
        { 
            DestroyChildren(balloonContainerLeft);
            DestroyChildren(balloonContainerRight);
            DestroyChildren(rewardContainerLeft);
            DestroyChildren(rewardContainerRight);
            trialStim.transform.localScale = trialStimInitLocalScale;

            ResetToOriginalPositions();
        });

        LogTrialData();
        LogFrameData();
    }

    //HELPER FUNCTIONS -------------------------------------------------------------------------------------------------------
    void MoveContainerAnimation()
    {
        GameObject container = new GameObject();
        //set each parent to container.
        //then depending on whether they chose left or right, shift the oppose way.
    }

    void ActivateStimAndRewards()
    {
        if(!stimRight.activeSelf)
            stimRight.SetActive(true);

        if(!stimLeft.activeSelf)
            stimLeft.SetActive(true);

        if (!rewardContainerLeft.activeSelf)
            rewardContainerLeft.SetActive(true);

        if (!rewardContainerRight.activeSelf)
            rewardContainerRight.SetActive(true);
    }

    void CenterContainer(GameObject balloonContainer, GameObject rewardContainer, GameObject balloon)
    {
        if(Choice == "left")
            balloonContainer.transform.position = new Vector3(-.15f, .15f, .5f);
        else
            balloonContainer.transform.position = new Vector3(.15f, .15f, .5f); //this one left

        rewardContainer.transform.position = new Vector3(0f, -.3f, 0);
        balloon.transform.position = new Vector3(0f, -.015f, 0);
    }

    void ResetToOriginalPositions()
    {
        if(Choice == "left")
        {
            balloonContainerLeft.transform.position = LeftContainerOriginalPosition;
            rewardContainerLeft.transform.position = LeftRewardContainerOriginalPosition;
            stimLeft.transform.position = LeftStimOriginalPosition;
        }
        else
        {
            balloonContainerRight.transform.position = RightContainerOriginalPosition;
            rewardContainerRight.transform.position = RightRewardContainerOriginalPosition;
            stimRight.transform.position = RightStimOriginalPosition;
        }
    }

    void GiveReward()
    {
        if (Choice == "left")
        {
            CurrentTaskLevel.NumPulses += CurrentTrial.NumPulsesLeft;
            SyncBoxController.SendRewardPulses(CurrentTrial.NumPulsesLeft, CurrentTrial.PulseSizeLeft);
        }
        else
        {
            CurrentTaskLevel.NumPulses += CurrentTrial.NumPulsesRight;
            SyncBoxController.SendRewardPulses(CurrentTrial.NumPulsesRight, CurrentTrial.PulseSizeRight);
        }
    }

    void DisableAllGameobjects()
    {
        StartButton.SetActive(false);
        stimLeft.SetActive(false);
        stimRight.SetActive(false);
        balloonOutline.SetActive(false);
        prize.SetActive(false);
        //clickMarker.SetActive(false);
        //fb.SetActive(false);
        //goCue.SetActive(false);
    }

    // method for presetting variables
    void LoadVariables()
    {
        if (StartButton == null)
            CreateStartButton();

        //fb = GameObject.Find("FB");
        //goCue = GameObject.Find("ResponseCue");
        //clickMarker = GameObject.Find("ClickMarker");
        prize = GameObject.Find("Prize");
        stimLeft = GameObject.Find("StimLeft");
        stimRight = GameObject.Find("StimRight");
        balloonContainerLeft = GameObject.Find("BalloonContainerLeft");
        balloonContainerRight = GameObject.Find("BalloonContainerRight");
        balloonOutline = GameObject.Find("OutlineBest");
        rewardContainerLeft = GameObject.Find("RewardContainerLeft");
        rewardContainerRight = GameObject.Find("RewardContainerRight");
        reward = GameObject.Find("Reward");

        red = stimLeft.GetComponent<Renderer>().material.color;
        gray = new Color(0.5f, 0.5f, 0.5f);

        Color currentColor = reward.GetComponent<Renderer>().material.color;
        reward.GetComponent<Renderer>().material.color = gray;

        //fbInitLocalScale = fb.transform.localScale;
        trialStimInitLocalScale = stimLeft.transform.localScale;

        LeftContainerOriginalPosition = balloonContainerLeft.transform.position;
        RightContainerOriginalPosition = balloonContainerRight.transform.position;
        LeftRewardContainerOriginalPosition = rewardContainerLeft.transform.position;
        RightRewardContainerOriginalPosition = rewardContainerRight.transform.position;
        LeftStimOriginalPosition = stimLeft.transform.position;
        RightStimOriginalPosition = stimRight.transform.position;

        StartButton.SetActive(false);
        balloonOutline.SetActive(false);
        reward.SetActive(false);
        prize.SetActive(false);
        //fb.SetActive(false);
        //goCue.SetActive(false);
        //clickMarker.SetActive(false);

        variablesLoaded = true;
    }

    void CreateBalloons(int numBalloons, Vector3 scaleUpAmount, int clickPerOutline, Vector3 pos, GameObject container)
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

    void DestroyChildren(GameObject container)
    {
        var children = new List<GameObject>();
        foreach (Transform child in container.transform)
            children.Add(child.gameObject);
        foreach (GameObject child in children)
            Destroy(child);
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

    // // method to create NumRewards rewards
    void CreateRewards(int NumRewards, Vector3 pos, GameObject container)
    {
        //change the X from -.6 to -.575

        // get width of reward object
        float width = reward.GetComponent<Renderer>().bounds.size.x - .035f;
        pos -= new Vector3(((NumRewards - 1) * (width / 2)), 0, 0);
        for (int i = 0; i < NumRewards; i++)
        {
            GameObject rewardClone = Instantiate(reward, pos, reward.transform.rotation, container.transform);
            rewardClone.transform.Translate(new Vector3(i * width, 0, 0), Space.World);
            rewardClone.name = "Reward" + Choice + (i + 1);
            //rewardClone.transform.parent = container.transform;
            rewardClone.SetActive(true);
        }
    }

    void AddAudioClips()
    {
        AudioFBController.AddClip("InflateThenPop", InflateThenPop_Audio);
        AudioFBController.AddClip("Inflate", InflateBalloon_Audio);
        AudioFBController.AddClip("SelectionMade", SelectionMade_Audio);
    }

    void SetTrialSummaryString()
    {
        TrialSummaryString = "\n" +
                               "Trial #" + (TrialCount_InBlock + 1) +
                               "\n" +
                               "\nNumClicksLeft: " + CurrentTrial.NumClicksLeft +
                               "\nNumClicksRight: " + CurrentTrial.NumClicksRight +
                               "\nNumCoinsLeft: " + CurrentTrial.NumCoinsLeft +
                               "\nNumCoinsRight: " + CurrentTrial.NumCoinsRight +
                               "\nClicksPerOutline: " + CurrentTrial.ClicksPerOutline;
    }

    void LogTrialData()
    {
        TrialData.AddDatum("NumClicks", () => NumClicks);
        TrialData.AddDatum("NumClicksLeft", () => CurrentTrial.NumClicksLeft);
        TrialData.AddDatum("NumClicksRight", () => CurrentTrial.NumClicksRight);
        TrialData.AddDatum("NumCoinsLeft", () => CurrentTrial.NumCoinsLeft);
        TrialData.AddDatum("NumCoinsRight", () => CurrentTrial.NumCoinsRight);
        TrialData.AddDatum("ChosenSide", () => Choice.ToUpper());
        TrialData.AddDatum("TimeTakenToChoose", () => ChooseDuration);
        TrialData.AddDatum("AverageClickTimes", () => avgClickTime);
        TrialData.AddDatum("ClicksPerOutline", () => CurrentTrial.ClicksPerOutline);

        // Additional recorded data 
        // TrialData.AddDatum("TrialName", () => CurrentTrial.TrialName);
        // TrialData.AddDatum("TrialCode", () => CurrentTrial.TrialCode);
        // TrialData.AddDatum("ContextNum", () => CurrentTrial.ContextNum);
        // TrialData.AddDatum("ConditionName", () => CurrentTrial.ConditionName);
        // TrialData.AddDatum("ContextName", () => CurrentTrial.ContextName);
        // TrialData.AddDatum("InitialChoiceMinDuration", () => CurrentTrial.InitialChoiceMinDuration);
        // TrialData.AddDatum("StarttoTapDispDelay", () => CurrentTrial.StarttoTapDispDelay);
        // TrialData.AddDatum("FinalTouchToVisFeedbackDelay", () => CurrentTrial.FinalTouchToVisFeedbackDelay);
        // TrialData.AddDatum("FinalTouchToRewardDelay", () => CurrentTrial.FinalTouchToRewardDelay);
    }

    void LogFrameData()
    {
        FrameData.AddDatum("TouchPosition", () => InputBroker.mousePosition);
        FrameData.AddDatum("Context", () => CurrentTrial.ContextName);
        FrameData.AddDatum("StartButton", () => StartButton.activeSelf);
        FrameData.AddDatum("stimLeft", () => stimLeft.activeSelf);
        FrameData.AddDatum("stimRight", () => stimRight.activeSelf);
        FrameData.AddDatum("balloonOutline", () => balloonOutline.activeSelf);
        //FrameData.AddDatum("clickMarker", () => clickMarker.activeSelf);
        //FrameData.AddDatum("goCue", () => goCue.activeSelf);
        //FrameData.AddDatum("fb", () => fb.activeSelf);
        //FrameData.AddDatum("prize", () => prize.activeSelf);
    }


    void CreateStartButton()
    {
        string contextPath = GetContextNestedFilePath("StartButtonImage.png");
        Texture2D tex = LoadPNG(contextPath);
        Rect rect = new Rect(new Vector2(0, 0), new Vector2(1, 1));

        Vector3 buttonPosition = Vector3.zero;
        Vector3 buttonScale = Vector3.zero;
        string TaskName = "EffortControl";
        if (SessionSettings.SettingClassExists(TaskName + "_TaskSettings"))
        {
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonPosition"))
            {
                buttonPosition = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonPosition");
            }
            else Debug.Log("[ERROR] Start Button Position settings not defined in the TaskDef");
            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonScale"))
                buttonScale = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonScale");
            else Debug.Log("[ERROR] Start Button Position settings not defined in the TaskDef");
        }
        else Debug.Log("[ERROR] TaskDef is not in config folder");

        GameObject startButton = new GameObject("StartButton");
        SpriteRenderer spriteRend = startButton.AddComponent<SpriteRenderer>();
        spriteRend.sprite = Sprite.Create(tex, new Rect(rect.x / 2, rect.y / 2, tex.width / 2, tex.height / 2), new Vector2(.5f, .5f), 100f);
        startButton.AddComponent<BoxCollider>();
        startButton.transform.localScale = buttonScale;
        startButton.transform.position = buttonPosition;
        StartButton = startButton;
    }

    public string GetContextNestedFilePath(string contextName)
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

}
