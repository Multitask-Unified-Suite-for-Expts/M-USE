using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using USE_States;
using EffortControl_Namespace;
using System;
using System.Linq;
using System.IO;
using USE_Settings;
using USE_ExperimentTemplate_Trial;
using ConfigDynamicUI;


public class EffortControl_TrialLevel : ControlLevel_Trial_Template
{
    public EffortControl_TrialDef currentTrial => GetCurrentTrialDef<EffortControl_TrialDef>();
    public EffortControl_TaskLevel currentTask => GetTaskLevel<EffortControl_TaskLevel>();

    //Prefabs to Instantiate:
    public GameObject StimNoMaterialPrefab;
    public GameObject StimLeftPrefab;
    public GameObject StimRightPrefab;
    public GameObject RewardPrefab;
    public GameObject OutlinePrefab;

    public Vector3 ButtonPosition;
    public Vector3 ButtonScale;
    public Vector3 OriginalStartButtonPosition;

    //Game Objects:
    GameObject StartButton, StimLeft, StimRight, TrialStim, BalloonContainerLeft, BalloonContainerRight,
               BalloonOutline, RewardContainerLeft, RewardContainerRight, Reward, MiddleBarrier, Borders;

    //Colors:
    [HideInInspector] Color Red;
    [HideInInspector] Color32 OffWhiteOutlineColor = new Color32(250, 249, 246, 0);

    [HideInInspector] Vector3 LeftScaleUpAmount;
    [HideInInspector] Vector3 RightScaleUpAmount;
    [HideInInspector] Vector3 MaxScale;
    [HideInInspector] Vector3 TrialStimInitLocalScale;
    [HideInInspector] Vector3 LeftContainerOriginalPosition;
    [HideInInspector] Vector3 RightContainerOriginalPosition;
    [HideInInspector] Vector3 LeftRewardContainerOriginalPosition;
    [HideInInspector] Vector3 RightRewardContainerOriginalPosition;
    [HideInInspector] Vector3 LeftStimOriginalPosition;
    [HideInInspector] Vector3 RightStimOriginalPosition;

    [HideInInspector] public string MaterialFilePath; //set in task level

    //Misc Variables:
    [System.NonSerialized] public int Response = -1;
    [HideInInspector] int ClicksNeeded; //becomes left/right num clicks once they make selection. 
    [HideInInspector] int ClickCount;
    [HideInInspector] bool AddTokenInflateAudioPlayed;
    [HideInInspector] bool ObjectsCreated;
    [HideInInspector] List<GameObject> RemoveParentList;
    [HideInInspector] GameObject Wrapper;

    string SideChoice; //left or right
    string RewardChoice; //higher or lower
    string EffortChoice; //higher or lower

    //To center the balloon they selected:
    public float CenteringSpeed = 1.5f;
    [HideInInspector] bool Centered;
    [HideInInspector] Vector3 CenteredPos;
    [HideInInspector] public bool Flashing;

    //Variables to Inflate balloon at interval rate
    [HideInInspector] float InflateClipDuration;
    [HideInInspector] bool Inflate;
    [HideInInspector] private readonly float MaxInflation_Y = 35f;
    [HideInInspector] float ScalePerInflation_Y;
    [HideInInspector] public float ScaleTimer;
    [HideInInspector] Vector3 IncrementAmounts;
    Vector3 NextScale;

    //Data variables:
    [HideInInspector] public float? AvgClickTime;
    [HideInInspector] public float ChooseDuration;
    [HideInInspector] public int RewardPulses_Block;
    [HideInInspector] public int Touches_Block;
    [HideInInspector] public int Completions_Block;
    [HideInInspector] public int NumChosenLeft_Block;
    [HideInInspector] public int NumChosenRight_Block;
    [HideInInspector] public int NumHigherEffortChosen_Block;
    [HideInInspector] public int NumLowerEffortChosen_Block;
    [HideInInspector] public int NumHigherRewardChosen_Block;
    [HideInInspector] public int NumLowerRewardChosen_Block;

    [HideInInspector] public ConfigNumber scalingInterval, inflateDuration, itiDuration, popToFeedbackDelay; //ScalingInterval is used for balloonInflation!

    [HideInInspector] public GameObject MaxOutline_Left;
    [HideInInspector] public GameObject MaxOutline_Right;

    [HideInInspector] public bool InflateAudioPlayed;
    [HideInInspector] public bool IsHuman;


    public override void DefineControlLevel()
    {
        State InitTrial = new State("InitTrial");
        State ChooseBalloon = new State("ChooseBalloon");
        State CenterSelection = new State("CenterSelection");
        State InflateBalloon = new State("InflateBalloon");
        State PopBalloon = new State("PopBalloon");
        State Feedback = new State("Feedback");
        State ITI = new State("ITI");
        AddActiveStates(new List<State> { InitTrial, ChooseBalloon, CenterSelection, InflateBalloon, PopBalloon, Feedback, ITI });

        SelectionHandler<EffortControl_StimDef> mouseHandler = new SelectionHandler<EffortControl_StimDef>();

        LoadTextures(MaterialFilePath);
  
        if(TokenFBController != null)
            SetTokenVariables();

        if(AudioFBController != null)
            InflateClipDuration = AudioFBController.GetClip("EC_Inflate").length;

        //SETUP TRIAL state -----------------------------------------------------------------------------------------------------
        SetupTrial.AddInitializationMethod(() =>
        {
            MaxOutline_Left = new GameObject();
            MaxOutline_Right = new GameObject();

            if (!ObjectsCreated)
                CreateObjects();

            LoadConfigUIVariables();
            SetTrialSummaryString();
        });
        SetupTrial.SpecifyTermination(() => true, InitTrial);

        MouseTracker.AddSelectionHandler(mouseHandler, InitTrial);
        //INIT Trial state -------------------------------------------------------------------------------------------------------
        InitTrial.AddInitializationMethod(() =>
        {
            TokenFBController.enabled = false;
            AvgClickTime = null;
            ResetRelativeStartTime(); 
            DisableAllGameobjects();
            StartButton.SetActive(true);
            ClickCount = 0;
            Response = -1;
            ChooseDuration = 0; //reset how long it took them to choose each trial. 
        });
        InitTrial.SpecifyTermination(() => mouseHandler.SelectionMatches(StartButton), ChooseBalloon, () => StartButton.SetActive(false));

        //Choose Balloon state -------------------------------------------------------------------------------------------------------
        MouseTracker.AddSelectionHandler(mouseHandler, ChooseBalloon);
        ChooseBalloon.AddInitializationMethod(() =>
        {
            Input.ResetInputAxes(); //reset input in case they holding down
            ActivateStimAndRewards();
            TrialStim = null;
            MouseTracker.ResetClickCount();

            MaxScale = new Vector3(60, 0, 60);
            LeftScaleUpAmount = MaxScale / currentTrial.NumClicksLeft;
            RightScaleUpAmount = MaxScale / currentTrial.NumClicksRight;

            CreateBalloonOutlines(currentTrial.NumClicksLeft, LeftScaleUpAmount, currentTrial.ClicksPerOutline, StimLeft.transform.position, BalloonContainerLeft);
            CreateBalloonOutlines(currentTrial.NumClicksRight, RightScaleUpAmount, currentTrial.ClicksPerOutline, StimRight.transform.position, BalloonContainerRight);
            CreateRewards(currentTrial.NumCoinsLeft, RewardContainerLeft.transform.position, RewardContainerLeft);
            CreateRewards(currentTrial.NumCoinsRight, RewardContainerRight.transform.position, RewardContainerRight);

            CreateTransparentBalloons();
        });

        ChooseBalloon.AddUpdateMethod(() =>
        {
            GameObject hit = mouseHandler.SelectedGameObject;
            if (hit == null)
                return;
            else
            {
                mouseHandler.Stop();
                if (hit.transform.name.Contains("Left"))
                {
                    SideChoice = "left";
                    TrialStim = StimLeft;
                }
                else if(hit.transform.name.Contains("Right"))
                {
                    SideChoice = "right";
                    TrialStim = StimRight;
                }
            }
        });
        ChooseBalloon.SpecifyTermination(() => TrialStim != null, CenterSelection, () =>
        {
            DestroyChildren(SideChoice == "left" ? RewardContainerRight : RewardContainerLeft);
            ClicksNeeded = (SideChoice == "left" ? currentTrial.NumClicksLeft : currentTrial.NumClicksRight);
            AudioFBController.Play("EC_BalloonChosen");
            SetChoices();
        });

        //Center Selection state -------------------------------------------------------------------------------------------------------
        CenterSelection.AddInitializationMethod(() =>
        {
            ChooseDuration = ChooseBalloon.TimingInfo.Duration;

            Wrapper = new GameObject();
            Wrapper.name = "Wrapper";
            Centered = false;
            CenteredPos = new Vector3((SideChoice == "left" ? 1f : -1f), 0, 0);

            MiddleBarrier.SetActive(false);

            if (SideChoice == "left")
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
                Wrapper.transform.position = Vector3.MoveTowards(Wrapper.transform.position, CenteredPos, CenteringSpeed * Time.deltaTime);
            
            if (Wrapper.transform.position == CenteredPos)
                Centered = true;
        });
        CenterSelection.SpecifyTermination(() => Centered, InflateBalloon);
        CenterSelection.AddDefaultTerminationMethod(() =>
        {
            RemoveParents(Wrapper, RemoveParentList);

            if (SideChoice == "left")
            {
                RewardContainerLeft.SetActive(false); //set Reward tokens to inactive since they are replaced by tokenbar tokens.
                MaxOutline_Left.transform.parent = null; //remove parent of transparent balloon outline. 
            }
            else
            {
                RewardContainerRight.SetActive(false);
                MaxOutline_Right.transform.parent = null;
            }
            TokenFBController.SetTotalTokensNum(SideChoice == "left" ? currentTrial.NumCoinsLeft : currentTrial.NumCoinsRight);
            TokenFBController.enabled = true;
        });

        //Inflate Balloon state -------------------------------------------------------------------------------------------------------
        List<float> clickTimings = new List<float>();
        float timeTracker = 0;
        MouseTracker.AddSelectionHandler(mouseHandler, InflateBalloon);

        InflateBalloon.AddInitializationMethod(() =>
        {
            ScalePerInflation_Y = (MaxInflation_Y - TrialStim.transform.localScale.y) / (SideChoice == "left" ? currentTrial.NumClicksLeft : currentTrial.NumClicksRight);
            timeTracker = Time.time;
            IncrementAmounts = new Vector3();
            Flashing = false;
            InflateAudioPlayed = false;
            ScaleTimer = 0;
        });

        InflateBalloon.AddUpdateMethod(() =>
        {
            if (Inflate)
            {
                if (!InflateAudioPlayed)
                {
                    AudioFBController.Play("EC_Inflate");
                    InflateAudioPlayed = true;
                }

                ScaleTimer += Time.deltaTime;

                if(ScaleTimer >= (InflateClipDuration / scalingInterval.value)) //When timer hits for next inflation
                {
                    if (TrialStim.transform.localScale != NextScale)
                        ScaleToNextInterval();
                    else
                    {
                        Inflate = false;
                        mouseHandler.Start();
                        if (ClickCount >= ClicksNeeded)
                        {
                            Response = 1;
                            AvgClickTime = clickTimings.Average();
                        }
                    }
                    ScaleTimer = 0; //Reset Timer for next inflation increment;
                }
            }

            if (mouseHandler.SelectionMatches(TrialStim) || mouseHandler.SelectionMatches(MaxOutline_Left) || mouseHandler.SelectionMatches(MaxOutline_Right))
            {
                Input.ResetInputAxes();
                ClickCount++;
                clickTimings.Add(Time.time - timeTracker);
                timeTracker = Time.time;
                mouseHandler.Stop();

                CalculateInflation(); //Sets Inflate to TRUE at end of func
                InflateAudioPlayed = false;
            }
        });
        InflateBalloon.AddTimer(() => inflateDuration.value, PopBalloon);
        InflateBalloon.SpecifyTermination(() => Response == 1, PopBalloon);
        InflateBalloon.AddDefaultTerminationMethod(() =>
        {
            if (SideChoice == "left")
                MaxOutline_Left.transform.parent = BalloonContainerLeft.transform;
            else
                MaxOutline_Right.transform.parent = BalloonContainerRight.transform;

            DestroyChildren(SideChoice == "left" ? BalloonContainerLeft : BalloonContainerRight);
            InflateAudioPlayed = false;
        });

        //PopBalloon state -------------------------------------------------------------------------------------------------------
        float delayStartTime = 0;

        PopBalloon.AddDefaultInitializationMethod(() =>
        {
            if (Response == 1)
            {
                if(IsHuman)
                    AudioFBController.Play("EC_HarshPop"); //better for humans
                else
                    AudioFBController.Play("EC_NicePop"); //better for monkeys
            }
            else
            {
                AudioFBController.Play("TimeRanOut");
                TokenFBController.enabled = false;
            }
            TrialStim.SetActive(false);
        });
        PopBalloon.AddUpdateMethod(() =>
        {
            if (!TrialStim.activeSelf)
                delayStartTime += Time.deltaTime;
        });
        PopBalloon.SpecifyTermination(() => !TrialStim.activeSelf && delayStartTime > popToFeedbackDelay.value, Feedback, () => delayStartTime = 0);

        //Feedback state -------------------------------------------------------------------------------------------------------
        Feedback.AddInitializationMethod(() =>
        {
            if (Response == 1)
            {
                GameObject CenteredGO = new GameObject();
                CenteredGO.transform.position = new Vector3(0, .5f, 0);
                TokenFBController.AddTokens(CenteredGO, SideChoice == "left" ? currentTrial.NumCoinsLeft : currentTrial.NumCoinsRight);
                Destroy(CenteredGO);

                if (SyncBoxController != null)
                    GiveReward();
                
                Completions_Block++;
                AddTokenInflateAudioPlayed = true;
            }
            Touches_Block += MouseTracker.GetClickCount();
        });
        Feedback.SpecifyTermination(() => AddTokenInflateAudioPlayed && !AudioFBController.IsPlaying() && !TokenFBController.IsAnimating(), ITI, () =>
        {
            TokenFBController.enabled = false;
            AddTokenInflateAudioPlayed = false;
        });
        Feedback.SpecifyTermination(() => true && Response != 1, ITI);

        //ITI state -------------------------------------------------------------------------------------------------------
        ITI.AddInitializationMethod(() =>
        {
            DestroyChildren(BalloonContainerLeft);
            DestroyChildren(BalloonContainerRight);
            DestroyChildren(RewardContainerLeft);
            DestroyChildren(RewardContainerRight);
            ResetToOriginalPositions();
            currentTask.CalculateBlockSummaryString();
        });
        ITI.AddTimer(itiDuration.value, FinishTrial);

        LogTrialData();
        LogFrameData();
    }

    //HELPER FUNCTIONS -------------------------------------------------------------------------------------------------------
    void ScaleToNextInterval()
    {
        //If close and next increment would go over target scale, recalculate the exact amount:
        if (TrialStim.transform.localScale.x + IncrementAmounts.x > NextScale.x) 
            IncrementAmounts = new Vector3((NextScale.x - TrialStim.transform.localScale.x), (NextScale.y - TrialStim.transform.localScale.y), (NextScale.z - TrialStim.transform.localScale.z));

        //Scale:
        TrialStim.transform.localScale = new Vector3(TrialStim.transform.localScale.x + IncrementAmounts.x, TrialStim.transform.localScale.y + IncrementAmounts.y, TrialStim.transform.localScale.z + IncrementAmounts.z);
    }

    void CalculateInflation()
    {      
        GameObject container = (SideChoice == "left") ? BalloonContainerLeft : BalloonContainerRight;
        NextScale = container.transform.GetChild(ClickCount-1).transform.localScale;
        NextScale.y = ScalePerInflation_Y + TrialStim.transform.localScale.y;
        Vector3 difference = NextScale - TrialStim.transform.localScale;
        IncrementAmounts = new Vector3((difference.x / scalingInterval.value), (difference.y / scalingInterval.value), (difference.z / scalingInterval.value));

        Inflate = true;
    }

    IEnumerator FlashOutline()
    {
        Flashing = true;
        GameObject container = (SideChoice == "left") ? BalloonContainerLeft : BalloonContainerRight;

        GameObject child = container.transform.GetChild(ClickCount).transform.gameObject;
        if (child != null && Response != 1 && !Inflate)
        {
            child.transform.GetComponent<Renderer>().material.color = Color.red;
            yield return new WaitForSeconds(.5f);
            if(child != null)
            {
                child.transform.GetComponent<Renderer>().material.color = OffWhiteOutlineColor;
                yield return new WaitForSeconds(.5f);
            }
        }
        Flashing = false;
    }

    void SetChoices()
    {
        if(SideChoice == "left")
        {
            NumChosenLeft_Block++;
            EffortChoice = (currentTrial.NumClicksLeft > currentTrial.NumClicksRight ? "higher" : "lower");
            RewardChoice = (currentTrial.NumCoinsLeft > currentTrial.NumCoinsRight ? "higher" : "lower");
        }
        else
        {
            NumChosenRight_Block++;
            EffortChoice = (currentTrial.NumClicksLeft > currentTrial.NumClicksRight ? "lower" : "higher");
            RewardChoice = (currentTrial.NumCoinsLeft > currentTrial.NumCoinsRight ? "lower" : "higher");
        }

        if (EffortChoice == "higher")
            NumHigherEffortChosen_Block++;
        else
            NumLowerEffortChosen_Block++;

        if (RewardChoice == "higher")
            NumHigherRewardChosen_Block++;
        else
            NumLowerRewardChosen_Block++;
    }

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
        TrialStim.transform.localScale = TrialStimInitLocalScale;

        if (SideChoice == "left")
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
        if (SideChoice == "left")
        {
            SyncBoxController.SendRewardPulses(currentTrial.NumPulsesLeft, currentTrial.PulseSizeLeft);
            RewardPulses_Block += currentTrial.NumPulsesLeft;
        }
        else
        {
            SyncBoxController.SendRewardPulses(currentTrial.NumPulsesRight, currentTrial.PulseSizeRight);
            RewardPulses_Block += currentTrial.NumPulsesRight;
        }
    }

    void ActivateStimAndRewards()
    {
        Borders.SetActive(true);
        MiddleBarrier.SetActive(true);
        StimRight.SetActive(true);
        StimLeft.SetActive(true);
        RewardContainerLeft.SetActive(true);
        RewardContainerRight.SetActive(true);
    }

    void DisableAllGameobjects()
    {
        StimLeft.SetActive(false);
        StimRight.SetActive(false);
        BalloonOutline.SetActive(false);
    }

    void LoadConfigUIVariables()
    {
        scalingInterval = ConfigUiVariables.get<ConfigNumber>("scalingInterval");
        inflateDuration = ConfigUiVariables.get<ConfigNumber>("inflateDuration");
        itiDuration = ConfigUiVariables.get<ConfigNumber>("itiDuration");
        popToFeedbackDelay = ConfigUiVariables.get<ConfigNumber>("popToFeedbackDelay");
    }

    void CreateObjects()
    {
        StartButton = CreateSquare("Start Button", StartButtonTexture, ButtonPosition, ButtonScale);

        StimLeft = Instantiate(StimLeftPrefab, StimLeftPrefab.transform.position, StimLeftPrefab.transform.rotation);
        StimLeft.name = "StimLeft";
        Red = StimLeft.GetComponent<Renderer>().material.color; //set red color. 
        StimLeft.GetComponent<Renderer>().material.color = Red;
        TrialStimInitLocalScale = StimLeft.transform.localScale;

        StimRight = Instantiate(StimRightPrefab, StimRightPrefab.transform.position, StimRightPrefab.transform.rotation);
        StimRight.name = "StimRight";
        StimRight.GetComponent<Renderer>().material.color = Red;

        Reward = Instantiate(RewardPrefab, RewardPrefab.transform.position, RewardPrefab.transform.rotation);
        Reward.name = "Reward";
        Reward.GetComponent<Renderer>().material.color = Color.gray; //turn token color to grey so they dont look collected yet. 

        BalloonOutline = Instantiate(OutlinePrefab, OutlinePrefab.transform.position, OutlinePrefab.transform.rotation);
        BalloonOutline.name = "Outline";
        BalloonOutline.transform.localScale = new Vector3(10, 0.01f, 10);
        BalloonOutline.GetComponent<Renderer>().material.color = OffWhiteOutlineColor;

        List<GameObject> borderList = new List<GameObject>();

        Borders = new GameObject("Borders");

        GameObject topBorder = GameObject.CreatePrimitive(PrimitiveType.Cube);
        topBorder.name = "TopBorder";
        topBorder.transform.parent = Borders.transform;
        topBorder.transform.position = new Vector3(0, -.005f, 0);
        topBorder.transform.eulerAngles = new Vector3(0, 0, 90f);
        topBorder.transform.localScale = new Vector3(.05f, 4.018f, .001f);
        borderList.Add(topBorder);

        GameObject rightBorder = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightBorder.name = "RightBorder";
        rightBorder.transform.parent = Borders.transform;
        rightBorder.transform.position = new Vector3(2.035f, -1.157f, 0);
        rightBorder.transform.eulerAngles = Vector3.zero;
        rightBorder.transform.localScale = new Vector3(.05f, 2.35f, .001f);
        borderList.Add(rightBorder);

        GameObject leftBorder = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftBorder.name = "LeftBorder";
        leftBorder.transform.parent = Borders.transform;
        leftBorder.transform.position = new Vector3(-2.035f, -1.159f, 0);
        leftBorder.transform.eulerAngles = Vector3.zero;
        leftBorder.transform.localScale = new Vector3(.05f, 2.35f, .001f);
        borderList.Add(leftBorder);

        GameObject bottomBorder = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bottomBorder.name = "BottomBorder";
        bottomBorder.transform.parent = Borders.transform;
        bottomBorder.transform.position = new Vector3(0, -2.3f, 0);
        bottomBorder.transform.eulerAngles = new Vector3(0, 0, 90f);
        bottomBorder.transform.localScale = new Vector3(.05f, 4.018f, .001f);
        borderList.Add(bottomBorder);

        Borders.transform.position = new Vector3(0, 1.755f, 0);

        MiddleBarrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
        MiddleBarrier.name = "MiddleBarrier";
        MiddleBarrier.transform.position = new Vector3(0, .602f, 0);
        MiddleBarrier.transform.localScale = new Vector3(.0125f, 2.245f, .001f);
        borderList.Add(MiddleBarrier);

        foreach (GameObject border in borderList)
        {
            Material mat = border.GetComponent<Renderer>().material;
            mat.color = OffWhiteOutlineColor;
            mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            mat.SetFloat("_SpecularHighlights", 0f);
        }

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
        Borders.SetActive(false);

        ObjectsCreated = true;
    }

    void CreateTransparentBalloons()
    {
        MaxOutline_Left = Instantiate(StimNoMaterialPrefab, StimLeft.transform.position, StimLeftPrefab.transform.rotation);
        MaxOutline_Left.name = "MaxOutline_Left";
        MaxOutline_Left.transform.localScale = new Vector3(70f, .1f, 70f);
        MaxOutline_Left.transform.SetParent(BalloonContainerLeft.transform);

        MaxOutline_Right = Instantiate(StimNoMaterialPrefab, StimRight.transform.position, StimRightPrefab.transform.rotation);
        MaxOutline_Right.name = "MaxOutline_Right";
        MaxOutline_Right.transform.localScale = new Vector3(70f, .1f, 70f);
        MaxOutline_Right.transform.SetParent(BalloonContainerRight.transform);
        
        MaxOutline_Left.SetActive(true);
        MaxOutline_Right.SetActive(true);
    }

    void CreateBalloonOutlines(int numBalloons, Vector3 ScaleUpAmount, int clickPerOutline, Vector3 pos, GameObject container)
    {
        string containerName = (container.name == "BalloonContainerLeft" ? "Left" : "Right");
        for (int i = clickPerOutline; i <= numBalloons; i += clickPerOutline)
        {
            GameObject outline = Instantiate(BalloonOutline, pos, BalloonOutline.transform.rotation);
            outline.transform.parent = container.transform;
            outline.name = "Outline" + containerName + (i);
            outline.transform.localScale += (i) * ScaleUpAmount;
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
            RewardClone.name = "Reward" + SideChoice + (i + 1);
            RewardClone.SetActive(true);
        }
    }

    void SetTrialSummaryString()
    {
        TrialSummaryString = "Trial #" + (TrialCount_InBlock + 1) + " In Block" +
                             "\n" +
                             "\nLeft Balloon: " + currentTrial.NumClicksLeft + " Clicks, " + currentTrial.NumCoinsLeft + " Tokens" +
                             "\n" +
                             "\nRight Balloon: " + currentTrial.NumClicksRight + " Clicks, " + currentTrial.NumCoinsRight + " Tokens";
    }

    void LogTrialData()
    {
        TrialData.AddDatum("ClicksNeeded", () => ClicksNeeded);
        TrialData.AddDatum("ClicksNeededLeft", () => currentTrial.NumClicksLeft);
        TrialData.AddDatum("ClicksNeededRight", () => currentTrial.NumClicksRight);
        TrialData.AddDatum("NumCoinsLeft", () => currentTrial.NumCoinsLeft);
        TrialData.AddDatum("NumCoinsRight", () => currentTrial.NumCoinsRight);
        TrialData.AddDatum("ChosenSide", () => SideChoice.ToUpper());
        TrialData.AddDatum("ChosenEffort", () => EffortChoice.ToUpper());
        TrialData.AddDatum("ChosenReward", () => RewardChoice.ToUpper());
        TrialData.AddDatum("TimeTakenToChoose", () => ChooseDuration);
        TrialData.AddDatum("AverageClickTimes", () => AvgClickTime);
        TrialData.AddDatum("ClicksPerOutline", () => currentTrial.ClicksPerOutline);
    }

    void LogFrameData()
    {
        FrameData.AddDatum("TouchPosition", () => InputBroker.mousePosition);
        FrameData.AddDatum("StartButton", () => StartButton.activeSelf);
        FrameData.AddDatum("StimLeft", () => StimLeft.activeSelf);
        FrameData.AddDatum("StimRight", () => StimRight.activeSelf);
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
