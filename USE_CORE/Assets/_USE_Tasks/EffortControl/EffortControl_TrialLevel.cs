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

    //Prefabs to Instantiate:
    public GameObject StimLeftPrefab;
    public GameObject StimRightPrefab;
    public GameObject RewardPrefab;
    public GameObject OutlinePrefab;

    //Game Objects:
    private GameObject StartButton, StimLeft, StimRight, TrialStim, BalloonContainerLeft,
    BalloonContainerRight, BalloonOutline, RewardContainerLeft, RewardContainerRight, Reward, MiddleBarrier;

    // trial config variables
    //public float InitialChoiceDuration;
    // public float RewardDelay;
    // public float StartDelay;
    // public float StepsToProgressUpdate;

    // timing variable 
    private float? avgClickTime;

    // effort Reward variables
    private int NumClicks, clickCount;

    // trial count variables
    private int numChosenLeft, numChosenRight; //he's not doing anything with these currently
    [HideInInspector]
    public String Choice;
    [System.NonSerialized] public int response = -1;

    // vector3 variables
    private Vector3 TrialStimInitLocalScale;
    private Vector3 fbInitLocalScale;
    private Vector3 scaleUpAmountLeft;
    private Vector3 scaleUpAmountRight;
    private Vector3 scaleUpAmount;
    public Vector3 maxScale;

    // misc variables
    private Ray mouseRay;
    private Color red;
    private Color gray = new Color(0.5f, 0.5f, 0.5f);

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

    public Vector3 CenteredPos;
    public GameObject Wrapper;
    public bool Centered;
    public int CenteringSpeed = 1;

    public List<GameObject> RemoveParentList;

    //Data variables:
    public int RewardPulses_Block;
    public int Touches_Block;
    public int Completions_Block;

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
        SetTokenVariables();

        if(AudioFBController != null)
            AddAudioClips();

        //SETUP TRIAL state -----------------------------------------------------------------------------------------------------
        SetupTrial.AddInitializationMethod(() =>
        {
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
            ChangeColor(StimRight, red);
            ChangeColor(StimLeft, red);
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
            Input.ResetInputAxes(); //reset input in case they holding down
            ActivateStimAndRewards();
            TrialStim = null;
            MouseTracker.ResetClickCount();

            maxScale = new Vector3(60, 0, 60);
            scaleUpAmountLeft = maxScale / CurrentTrial.NumClicksLeft;
            scaleUpAmountRight = maxScale / CurrentTrial.NumClicksRight;

            CreateBalloonOutlines(CurrentTrial.NumClicksLeft, scaleUpAmountLeft, CurrentTrial.ClicksPerOutline, StimLeft.transform.position, BalloonContainerLeft);
            CreateBalloonOutlines(CurrentTrial.NumClicksRight, scaleUpAmountRight, CurrentTrial.ClicksPerOutline, StimRight.transform.position, BalloonContainerRight);
            CreateRewards(CurrentTrial.NumCoinsLeft, RewardContainerLeft.transform.position, RewardContainerLeft);
            CreateRewards(CurrentTrial.NumCoinsRight, RewardContainerRight.transform.position, RewardContainerRight);
        });

        MouseTracker.AddSelectionHandler(mouseHandler, ChooseBalloon);
        ChooseBalloon.AddUpdateMethod(() =>
        {
            GameObject hit = mouseHandler.SelectedGameObject;
            if (hit == null)
                return;

            if (hit.transform.name == "StimLeft")
            {
                numChosenLeft++;
                Choice = "left";

                ChangeColor(StimRight, gray);
                ChangeContainerColor(BalloonContainerRight, gray);
                DestroyChildren(RewardContainerRight);

                TrialStim = hit.transform.gameObject;
                NumClicks = CurrentTrial.NumClicksLeft;
                scaleUpAmount = scaleUpAmountLeft;
            }
            else if (hit.transform.name == "StimRight")
            {
                numChosenRight++;
                Choice = "right";

                ChangeColor(StimLeft, gray);
                ChangeContainerColor(BalloonContainerLeft, gray);
                DestroyChildren(RewardContainerLeft);

                TrialStim = hit.transform.gameObject;
                NumClicks = CurrentTrial.NumClicksRight;
                scaleUpAmount = scaleUpAmountRight;
            }
        });
        ChooseBalloon.SpecifyTermination(() => TrialStim != null, CenterSelection, () =>
        {
            AudioFBController.Play("SelectionMade");
            ChooseDuration = ChooseBalloon.TimingInfo.Duration;
        });

        //Center Selection state -------------------------------------------------------------------------------------------------------
        CenterSelection.AddInitializationMethod(() =>
        {
            Wrapper = new GameObject();
            Wrapper.name = "Wrapper";
            Centered = false;
            CenteredPos = new Vector3((Choice == "left" ? 1f : -1f), 0, 0);

            MiddleBarrier.SetActive(false);

            if (Choice == "left")
            {
                SetParents(Wrapper, new List<GameObject>() {BalloonContainerLeft, StimLeft, RewardContainerLeft});
                DestroyChildren(BalloonContainerRight);
                StimRight.SetActive(false);
            }
            else
            {
                SetParents(Wrapper, new List<GameObject>() {BalloonContainerRight, StimRight, RewardContainerRight});
                DestroyChildren(BalloonContainerLeft);
                StimLeft.SetActive(false);
            }
        });
        CenterSelection.AddUpdateMethod(() =>
        {
            if(Wrapper.transform.position != CenteredPos)
            {
                Wrapper.transform.position = Vector3.MoveTowards(Wrapper.transform.position, CenteredPos, CenteringSpeed * Time.deltaTime);
            }
            if (Wrapper.transform.position == CenteredPos)
                Centered = true;
        });
        CenterSelection.SpecifyTermination(() => Centered, InflateBalloon, () => RemoveParents(Wrapper, RemoveParentList));

        //Inflate Balloon state -------------------------------------------------------------------------------------------------------
        List<float> clickTimings = new List<float>();
        float timeTracker = 0;

        InflateBalloon.AddInitializationMethod(() =>
        {
            if (Choice == "left") //set Reward tokens to inactive since they are replaced by tokenbar tokens. 
                RewardContainerLeft.SetActive(false);
            else
                RewardContainerRight.SetActive(false);

            TokenFBController.SetTotalTokensNum(Choice == "left" ? CurrentTrial.NumCoinsLeft : CurrentTrial.NumCoinsRight);
            TokenFBController.enabled = true;
            timeTracker = Time.time;
        });
        MouseTracker.AddSelectionHandler(mouseHandler, InflateBalloon);
        InflateBalloon.AddUpdateMethod(() =>
        {
            if (mouseHandler.SelectionMatches(TrialStim))
            {
                if (clickCount < NumClicks)
                    AudioFBController.Play("Inflate");

                clickTimings.Add(Time.time - timeTracker);
                timeTracker = Time.time;

                if (clickCount == 0)
                {
                    GameObject container = (Choice == "left") ? BalloonContainerLeft : BalloonContainerRight;
                    Vector3 scale = container.transform.GetChild(0).transform.localScale; //get scale of first outlline
                    scale.y = TrialStim.transform.localScale.y; //set scale to new Y
                    TrialStim.transform.localScale = scale; //increase balloon size to first outline
                }
                else
                    TrialStim.transform.localScale += scaleUpAmount; //increase balloon size
                
                clickCount++;
                mouseHandler.Stop(); //Stop detecting presses until mouse is released. 
            }

            if (clickCount >= NumClicks)
            {
                AudioFBController.Play("InflateThenPop");
                response = 1;
                avgClickTime = clickTimings.Average(); //Calculate Avg Time
            }

            if (InputBroker.GetMouseButtonUp(0))
                mouseHandler.Start(); //Start detecting presses again
            
        });

        InflateBalloon.AddTimer(45f, Feedback); //FIX HERE! CURRENTTRIAL.InflateDuration DOESN'T WORK. 
        InflateBalloon.SpecifyTermination(() => clickCount >= NumClicks, FeedbackDelay);
        InflateBalloon.AddDefaultTerminationMethod(() =>
        {
            if (response == 1)
            {
                if (Choice == "left")
                    DestroyChildren(BalloonContainerLeft);
                else
                    DestroyChildren(BalloonContainerRight);
            }
        });

        //Feedback Delay state -------------------------------------------------------------------------------------------------------
        FeedbackDelay.AddTimer(() => CurrentTrial.FeedbackDelayDuration, Feedback);
        FeedbackDelay.AddDefaultTerminationMethod(() =>
        {
            if (response == 1)
            {
                TrialStim.SetActive(false);
                TrialStim.transform.localScale = TrialStimInitLocalScale;
            }
        });

        //Feedback state -------------------------------------------------------------------------------------------------------
        Feedback.AddInitializationMethod(() =>
        {
            if (response == 1)
            {
                Completions_Block++;
                GameObject centeredGO = new GameObject();
                centeredGO.transform.position = Vector3.zero;
                TokenFBController.AddTokens(centeredGO, Choice == "left" ? CurrentTrial.NumCoinsLeft : CurrentTrial.NumCoinsRight);
                Destroy(centeredGO);

                if (SyncBoxController != null)
                    GiveReward();
                
                AudioPlayed = true;
            }
            Touches_Block += MouseTracker.GetClickCount();
        });
        Feedback.SpecifyTermination(() => AudioPlayed && !AudioFBController.IsPlaying() && !TokenFBController.IsAnimating(), ITI, () =>
        {
            TokenFBController.enabled = false;
            AudioPlayed = false;
        });

        //ITI state -------------------------------------------------------------------------------------------------------
        ITI.AddInitializationMethod(() =>
        {
            TrialStim.SetActive(false);
            DestroyChildren(BalloonContainerLeft);
            DestroyChildren(BalloonContainerRight);
        });
        ITI.AddTimer(.5f, FinishTrial, () =>   // HE HARD CODED THIS VALUE
        { 
            DestroyChildren(BalloonContainerLeft);
            DestroyChildren(BalloonContainerRight);
            DestroyChildren(RewardContainerLeft);
            DestroyChildren(RewardContainerRight);
            TrialStim.transform.localScale = TrialStimInitLocalScale;

            ResetToOriginalPositions();
        });

        LogTrialData();
        LogFrameData();
    }

    //HELPER FUNCTIONS -------------------------------------------------------------------------------------------------------
    void SetTokenVariables()
    {
        TokenFBController.SetFlashingTime(1.5f);
        TokenFBController.tokenBoxYOffset = 20;
        TokenFBController.tokenSize = 105;
        TokenFBController.tokenSpacing = -18;
    }

    void SetParents(GameObject wrapper, List<GameObject> objects) // 1) Setting the parent of each GO, and 2) Adding to RemovalList (so can remove easily later)
    {
        RemoveParentList = new List<GameObject>();
        foreach(GameObject go in objects)
        {
            go.transform.parent = wrapper.transform; //set parent
            RemoveParentList.Add(go); //add to remove parent list for later
        }
    }

    void RemoveParents(GameObject wrapper, List<GameObject> objects)
    {
        if (objects.Count < 1 || objects == null)
            Debug.Log("There are no objects in the List!");
        else
        {
            foreach (GameObject go in objects)
                go.transform.parent = null;
        }
        Destroy(wrapper);
    }

    void ResetToOriginalPositions()
    {
        if(Choice == "left")
        {
            BalloonContainerLeft.transform.position = LeftContainerOriginalPosition;
            RewardContainerLeft.transform.position = LeftRewardContainerOriginalPosition;
            StimLeft.transform.position = LeftStimOriginalPosition;
        }
        else
        {
            BalloonContainerRight.transform.position = RightContainerOriginalPosition;
            RewardContainerRight.transform.position = RightRewardContainerOriginalPosition;
            StimRight.transform.position = RightStimOriginalPosition;
        }
    }

    void GiveReward()
    {
        if (Choice == "left")
        {
            SyncBoxController.SendRewardPulses(CurrentTrial.NumPulsesLeft, CurrentTrial.PulseSizeLeft);
            RewardPulses_Block += CurrentTrial.NumPulsesLeft;
        }
        else
        {
            SyncBoxController.SendRewardPulses(CurrentTrial.NumPulsesRight, CurrentTrial.PulseSizeRight);
            RewardPulses_Block += CurrentTrial.NumPulsesRight;
        }
    }

    void ActivateStimAndRewards()
    {
        MiddleBarrier.SetActive(true);
        StimRight.SetActive(true);
        StimLeft.SetActive(true);
        RewardContainerLeft.SetActive(true);
        RewardContainerRight.SetActive(true);
    }

    void DisableAllGameobjects()
    {
        StartButton.SetActive(false);
        StimLeft.SetActive(false);
        StimRight.SetActive(false);
        BalloonOutline.SetActive(false);
    }

    void LoadVariables()
    {
        if (StartButton == null)
            CreateStartButton();

        StimLeft = Instantiate(StimLeftPrefab, StimLeftPrefab.transform.position, StimLeftPrefab.transform.rotation);
        StimLeft.name = "StimLeft";

        StimRight = Instantiate(StimRightPrefab, StimRightPrefab.transform.position, StimRightPrefab.transform.rotation);
        StimRight.name = "StimRight";

        Reward = Instantiate(RewardPrefab, RewardPrefab.transform.position, RewardPrefab.transform.rotation);
        Reward.name = "Reward";
        //Reward.GetComponent<Renderer>().material.color = gray; //turn token color to grey so they dont look collected yet. 

        BalloonOutline = Instantiate(OutlinePrefab, OutlinePrefab.transform.position, OutlinePrefab.transform.rotation);
        BalloonOutline.name = "Outline";
        BalloonOutline.transform.localScale = new Vector3(10, 0, 10);

        MiddleBarrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
        MiddleBarrier.name = "MiddleBarrier";
        MiddleBarrier.transform.position = Vector3.zero;
        MiddleBarrier.transform.localScale = new Vector3(.0125f, 4, 0);

        BalloonContainerLeft = new GameObject("BalloonContainerLeft");
        BalloonContainerLeft.transform.position = new Vector3(-1, .15f, .5f);
        BalloonContainerLeft.transform.localScale = new Vector3(1, 1, 1);

        BalloonContainerRight = new GameObject("BalloonContainerRight");
        BalloonContainerRight.transform.position = new Vector3(1, .15f, .5f);
        BalloonContainerRight.transform.localScale = new Vector3(1, 1, 1);

        RewardContainerLeft = new GameObject("RewardContainerLeft");
        RewardContainerLeft.transform.position = new Vector3(-.85f, 1.6f, 0);
        RewardContainerLeft.transform.localScale = new Vector3(1, 1, 1);

        RewardContainerRight = new GameObject("RewardContainerRight");
        RewardContainerRight.transform.position = new Vector3(.85f, 1.6f, 0);
        RewardContainerRight.transform.localScale = new Vector3(1, 1, 1);
        
        red = StimLeft.GetComponent<Renderer>().material.color;
        TrialStimInitLocalScale = StimLeft.transform.localScale;

        //Wrap Left side objects in container and Center to left side:
        GameObject leftWrapper = new GameObject();
        leftWrapper.name = "LeftWrapper";
        List<GameObject> leftList = new List<GameObject>() { BalloonContainerLeft, RewardContainerLeft, StimLeft };
        SetParents(leftWrapper, leftList);
        leftWrapper.transform.position = new Vector3(-.16f, 0, 0); //Centering on left half of screen. 

        //Wrap Right side objects in container and Center to right side:
        GameObject rightWrapper = new GameObject();
        rightWrapper.name = "RightWrapper";
        List<GameObject> rightList = new List<GameObject>() { BalloonContainerRight, RewardContainerRight, StimRight };
        SetParents(rightWrapper, rightList);
        rightWrapper.transform.position = new Vector3(.16f, 0, 0); //Centering on right half of screen. 

        LeftContainerOriginalPosition = BalloonContainerLeft.transform.position;
        RightContainerOriginalPosition = BalloonContainerRight.transform.position;
        LeftRewardContainerOriginalPosition = RewardContainerLeft.transform.position;
        RightRewardContainerOriginalPosition = RewardContainerRight.transform.position;
        LeftStimOriginalPosition = StimLeft.transform.position;
        RightStimOriginalPosition = StimRight.transform.position;

        //now that positions are set, remove parents so the balloon is clickable. 
        RemoveParents(leftWrapper, leftList);
        RemoveParents(rightWrapper, rightList);

        MiddleBarrier.SetActive(false);
        StartButton.SetActive(false);
        BalloonOutline.SetActive(false);
        Reward.SetActive(false);

        variablesLoaded = true;
    }

    void CreateBalloonOutlines(int numBalloons, Vector3 scaleUpAmount, int clickPerOutline, Vector3 pos, GameObject container)
    {
        for (int i = clickPerOutline; i <= numBalloons; i += clickPerOutline)
        {
            GameObject outline = Instantiate(BalloonOutline, pos, BalloonOutline.transform.rotation);
            outline.transform.parent = container.transform;
            outline.name = "Outline" + (i + 1);
            outline.transform.localScale += (i) * scaleUpAmount;
            outline.SetActive(true);
        }
    }

    void CreateRewards(int NumRewards, Vector3 pos, GameObject container)
    {
        float width = Reward.GetComponent<Renderer>().bounds.size.x - .035f; //Get Reward width! (-.35f cuz need to be closer together)
        pos -= new Vector3(((NumRewards - 1) * (width / 2)), 0, 0);
        for (int i = 0; i < NumRewards; i++)
        {
            GameObject RewardClone = Instantiate(Reward, pos, Reward.transform.rotation, container.transform);
            RewardClone.transform.Translate(new Vector3(i * width, 0, 0), Space.World);
            RewardClone.name = "Reward" + Choice + (i + 1);
            RewardClone.SetActive(true);
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
        FrameData.AddDatum("StartButton", () => StartButton.activeSelf);
        FrameData.AddDatum("StimLeft", () => StimLeft.activeSelf);
        FrameData.AddDatum("StimRight", () => StimRight.activeSelf);
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
                buttonPosition = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonPosition");
            else Debug.Log("[ERROR] Start Button Position settings not defined in the TaskDef");

            if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonScale"))
                buttonScale = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonScale");
            else Debug.Log("[ERROR] Start Button Position settings not defined in the TaskDef");
        }
        else Debug.Log("[ERROR] TaskDef is not in config folder");

        GameObject startButton = new GameObject("StartButton");
        SpriteRenderer spriteRend = startButton.AddComponent<SpriteRenderer>();
        spriteRend.sprite = Sprite.Create(tex, new Rect(rect.x, rect.y, tex.width, tex.height), new Vector2(.5f, .5f), 100f);
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
