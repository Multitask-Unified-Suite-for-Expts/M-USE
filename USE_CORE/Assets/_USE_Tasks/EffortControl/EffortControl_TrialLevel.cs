/*
MIT License

Copyright (c) 2023 Multitask - Unified - Suite -for-Expts

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files(the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/


using UnityEngine;
using System.Collections.Generic;
using USE_States;
using EffortControl_Namespace;
using System.Linq;
using USE_ExperimentTemplate_Trial;
using ConfigDynamicUI;
using UnityEngine.UI;
using TMPro;
using USE_ExperimentTemplate_Task;
using USE_UI;


public class EffortControl_TrialLevel : ControlLevel_Trial_Template
{
    public EffortControl_TrialDef CurrentTrial => GetCurrentTrialDef<EffortControl_TrialDef>();
    public EffortControl_TaskLevel CurrentTaskLevel => GetTaskLevel<EffortControl_TaskLevel>();
    public EffortControl_TaskDef CurrentTask => GetTaskDef<EffortControl_TaskDef>();

    //Prefabs to Instantiate:
    public GameObject StimNoMaterialPrefab;
    public GameObject StimLeftPrefab;
    public GameObject StimRightPrefab;
    public GameObject RewardPrefab;
    public GameObject OutlinePrefab;

    [HideInInspector] public Vector3 OriginalStartButtonPosition;

    public GameObject EC_CanvasGO;

    private GameObject StartButton, StimLeft, StimRight, TrialStim, OutlineContainerLeft, OutlineContainerRight,
                        BalloonOutline, RewardContainerLeft, RewardContainerRight, Reward, MiddleBarrier;

    private Color Red;
    private Color32 OffWhiteOutlineColor = new Color32(250, 249, 246, 0);

    private Vector3 MaxScale = new Vector3(66f, 0, 65.5f); //CONTROLS THE SIZE OF THE OUTLINES!

    private Vector3 LeftScaleUpAmount;
    private Vector3 RightScaleUpAmount;
    private Vector3 TrialStimInitLocalScale;
    private Vector3 LeftContainerOriginalPosition;
    private Vector3 RightContainerOriginalPosition;
    private Vector3 LeftRewardContainerOriginalPosition;
    private Vector3 RightRewardContainerOriginalPosition;
    private Vector3 LeftStimOriginalPosition;
    private Vector3 RightStimOriginalPosition;


    [System.NonSerialized] public int Response = -1;
    private int InflationsNeeded; //becomes left/right num clicks once they make selection. 
    private int NumInflations;
    private bool AddTokenInflateAudioPlayed;
    private bool ObjectsCreated;
    [HideInInspector] private List<GameObject> RemoveParentList;
    [HideInInspector] private GameObject Wrapper;

    private string SideChoice; //left or right
    private string RewardChoice; //higher or lower
    private string EffortChoice; //higher or lower

    //To center the balloon they selected:
    protected float CenteringSpeed = 1.5f;
    private Vector3 CenteredPos;
    [HideInInspector] public bool Flashing;

    //Variables to Inflate balloon at interval rate
    private float InflateClipDuration;
    private bool Inflate;
    private readonly float MaxInflation_Y = 35f; //used for how thick we want the balloon to be. 
    private float ScalePerInflation_Y;
    [HideInInspector] public float ScaleTimer;
    private Vector3 IncrementAmounts;
    private Vector3 NextScale;

    //Trial specific Data variables:
    [HideInInspector] public float AvgClickTime;
    [HideInInspector] public float ChooseDuration;
    [HideInInspector] public float InflationDuration;
    //Block specific Data variables:
    [HideInInspector] public int TotalTouches_Block;
    [HideInInspector] public int Completions_Block;
    [HideInInspector] public int NumChosenLeft_Block;
    [HideInInspector] public int NumChosenRight_Block;
    [HideInInspector] public int NumHigherEffortChosen_Block;
    [HideInInspector] public int NumLowerEffortChosen_Block;
    [HideInInspector] public int NumSameEffortChosen_Block;
    [HideInInspector] public int NumHigherRewardChosen_Block;
    [HideInInspector] public int NumLowerRewardChosen_Block;
    [HideInInspector] public int NumSameRewardChosen_Block;
    [HideInInspector] public List<float?> InflationDurations_Block = new List<float?>();

    [HideInInspector] public ConfigNumber minObjectTouchDuration, maxObjectTouchDuration, scalingInterval, inflateDuration, itiDuration, popToFeedbackDelay, choiceToTouchDelay, sbToBalloonDelay; //ScalingInterval is used for balloonInflation!

    [HideInInspector] public GameObject MaxOutline_Left;
    [HideInInspector] public GameObject MaxOutline_Right;

    [HideInInspector] public bool InflateAudioPlayed;

    [HideInInspector] public List<float> clickTimings;
    [HideInInspector] public float timeTracker;

    [HideInInspector] public float BalloonSelectedTime;
    [HideInInspector] public float StartButtonSelectedTime;

    [HideInInspector] public int TrialTouches;

    [HideInInspector] public List<GameObject> ObjectList;



    public override void DefineControlLevel()
    {
        State InitTrial = new State("InitTrial");
        State ChooseBalloon = new State("ChooseBalloon");
        State CenterSelection = new State("CenterSelection");
        State InflateBalloon = new State("InflateBalloon");
        State Feedback = new State("Feedback");
        State ITI = new State("ITI");
        AddActiveStates(new List<State> { InitTrial, ChooseBalloon, CenterSelection, InflateBalloon, Feedback, ITI });

        Add_ControlLevel_InitializationMethod(() =>
        {
            if (AudioFBController != null)
                InflateClipDuration = AudioFBController.GetClip("EC_Inflate").length;

            if (StartButton == null)
            {
                if (Session.SessionDef.IsHuman)
                {
                    StartButton = Session.HumanStartPanel.StartButtonGO;
                    Session.HumanStartPanel.SetVisibilityOnOffStates(InitTrial, InitTrial);
                }
                else
                {
                    StartButton = Session.USE_StartButton.CreateStartButton(EC_CanvasGO.GetComponent<Canvas>(), CurrentTask.StartButtonPosition, CurrentTask.StartButtonScale);
                    Session.USE_StartButton.SetVisibilityOnOffStates(InitTrial, InitTrial);
                }
            }

            if (!ObjectsCreated)
                CreateObjects();
        });

        //SETUP TRIAL state ---------------------------------------------------------------------------------------------------------------------------------------------
        SetupTrial.AddSpecificInitializationMethod(() =>
        {
            LoadConfigUIVariables();

            if (TrialCount_InTask != 0)
                CurrentTaskLevel.SetTaskSummaryString();

            SetTokenVariables();
        });
        SetupTrial.SpecifyTermination(() => true, InitTrial);

        //Setup Handler:
        var Handler = Session.SelectionTracker.SetupSelectionHandler("trial", "MouseButton0Click", Session.MouseTracker, InitTrial, InflateBalloon);
        //Enable Touch Feedback:
        TouchFBController.EnableTouchFeedback(Handler, CurrentTask.TouchFeedbackDuration, CurrentTask.StartButtonScale * 10, EC_CanvasGO, true);

        //INIT Trial state ----------------------------------------------------------------------------------------------------------------------------------------------
        InitTrial.AddSpecificInitializationMethod(() =>
        {
            //Set handler active in case they ran out of time mid inflation and it was never set back to active
            if (Handler != null)
                Handler.HandlerActive = true;

            TokenFBController.enabled = false;
            ResetRelativeStartTime(); 
            DisableAllGameobjects();

            ResetToOriginalPositions();

            if(TrialStim != null)
            {
                TrialStim.transform.localScale = TrialStimInitLocalScale;
                TrialStim = null;
            }

            CurrentTaskLevel.CalculateBlockSummaryString();

            if(Handler.AllSelections.Count > 0)
                Handler.ClearSelections();
            Handler.MinDuration = minObjectTouchDuration.value;
            Handler.MaxDuration = maxObjectTouchDuration.value;

        });
        InitTrial.SpecifyTermination(() => Handler.LastSuccessfulSelectionMatchesStartButton(), Delay, () =>
        {
            DelayDuration = sbToBalloonDelay.value;
            StateAfterDelay = ChooseBalloon;
        });

        //Choose Balloon state -------------------------------------------------------------------------------------------------------------------------------------------
        ChooseBalloon.AddSpecificInitializationMethod(() =>
        {
            Input.ResetInputAxes(); //reset input in case they holding down

            MiddleBarrier.SetActive(true);

            OutlineContainerLeft.transform.localPosition = new Vector3(-1.14f, -1.45f, .5f);
            OutlineContainerRight.transform.localPosition = new Vector3(1.14f, -1.45f, .5f);

            float tokensYPos = CurrentTrial.TokensInMiddleOfOutlines ? .54f : 1.5825f;
            RewardContainerLeft.transform.localPosition = new Vector3(StimLeft.transform.position.x, tokensYPos, 0);
            RewardContainerRight.transform.localPosition = new Vector3(StimRight.transform.position.x, tokensYPos, 0);
            
            LeftScaleUpAmount = MaxScale / CurrentTrial.NumClicksLeft;
            RightScaleUpAmount = MaxScale / CurrentTrial.NumClicksRight;

            CreateBalloonOutlines(CurrentTrial.NumClicksLeft, LeftScaleUpAmount, StimLeft.transform.position, OutlineContainerLeft);
            CreateBalloonOutlines(CurrentTrial.NumClicksRight, RightScaleUpAmount, StimRight.transform.position, OutlineContainerRight);
            CreateRewards(CurrentTrial.NumCoinsLeft, RewardContainerLeft.transform.position, RewardContainerLeft);
            CreateRewards(CurrentTrial.NumCoinsRight, RewardContainerRight.transform.position, RewardContainerRight);
            CreateTransparentBalloons();

            ActivateObjects();

            Session.TargetObjects.Add(StimLeft);
            Session.TargetObjects.Add(StimRight);

            SideChoice = null;

            if(Handler.AllSelections.Count > 0)
                Handler.ClearSelections();
        });
        ChooseBalloon.AddUpdateMethod(() =>
        {
            if (Handler.SuccessfulSelections.Count > 0)
            {
                BalloonSelectedTime = Time.time;
                if (Handler.LastSuccessfulSelection.SelectedGameObject.name.Contains("Left"))
                {
                    SideChoice = "Left";
                    TrialStim = StimLeft;
                }
                else if (Handler.LastSuccessfulSelection.SelectedGameObject.name.Contains("Right"))
                {
                    SideChoice = "Right";
                    TrialStim = StimRight;
                }
            }

            //Neg FB if touch outside balloon. Adding "sideChoice == null" so that they cant click outside balloon at the end and mess up pop audio.
            if (InputBroker.GetMouseButtonDown(0) && SideChoice == null)
            {
                GameObject hitGO = InputBroker.RaycastBoth(InputBroker.mousePosition);
                if (hitGO == null)
                    AudioFBController.Play("Negative");
            }
        });
        ChooseBalloon.SpecifyTermination(() => SideChoice != null, CenterSelection, () =>
        {
            Session.EventCodeManager.AddToFrameEventCodeBuffer("Button0PressedOnTargetObject");//SELECTION STUFF (code may not be exact and/or could be moved to Selection handler)
            Session.EventCodeManager.AddToFrameEventCodeBuffer(TaskEventCodes["BalloonChosen"]);

            DestroyChildren(SideChoice == "Left" ? RewardContainerRight : RewardContainerLeft);
            InflationsNeeded = SideChoice == "Left" ? CurrentTrial.NumClicksLeft : CurrentTrial.NumClicksRight;
            AudioFBController.Play("EC_BalloonChosen");
            RecordChoices();
        });

        //Center Selection state -----------------------------------------------------------------------------------------------------------------------------------------
        CenterSelection.AddSpecificInitializationMethod(() =>
        {
            ChooseDuration = ChooseBalloon.TimingInfo.Duration;
            Wrapper = new GameObject("Wrapper");
            MiddleBarrier.SetActive(false);

            if (SideChoice == "Left")
            {
                SetParents(Wrapper, new List<GameObject>() {OutlineContainerLeft, StimLeft, RewardContainerLeft});
                DestroyChildren(OutlineContainerRight);
                StimRight.SetActive(false);
            }
            else
            {
                SetParents(Wrapper, new List<GameObject>() {OutlineContainerRight, StimRight, RewardContainerRight});
                DestroyChildren(OutlineContainerLeft);
                StimLeft.SetActive(false);
            }

            CenteredPos = new Vector3(SideChoice == "Left" ? 1.01f : -1.01f, 0, 0);
        });
        CenterSelection.AddUpdateMethod(() =>
        {
            if (Wrapper.transform.position != CenteredPos)
                Wrapper.transform.position = Vector3.MoveTowards(Wrapper.transform.position, CenteredPos, CenteringSpeed * Time.deltaTime);
        });
        CenterSelection.SpecifyTermination(() => Wrapper.transform.position == CenteredPos, Delay);
        CenterSelection.AddDefaultTerminationMethod(() =>
        {
            StateAfterDelay = InflateBalloon;
            DelayDuration = choiceToTouchDelay.value;

            RemoveParents(Wrapper, RemoveParentList);

            if (SideChoice == "Left")
            {
                RewardContainerLeft.SetActive(false); //set Reward tokens to inactive since they are replaced by tokenbar tokens.
                MaxOutline_Left.transform.parent = null; //remove parent of transparent balloon outline. 
            }
            else
            {
                RewardContainerRight.SetActive(false);
                MaxOutline_Right.transform.parent = null;
            }
            TokenFBController.SetTotalTokensNum(SideChoice == "Left" ? CurrentTrial.NumCoinsLeft : CurrentTrial.NumCoinsRight);
            TokenFBController.enabled = true;
            Session.EventCodeManager.AddToFrameEventCodeBuffer("TokenBarVisible");
        });

        //Inflate Balloon state -----------------------------------------------------------------------------------------------------------------------------------------
        int outlineClicksRemaining = 1;
        int successfulSelections = 0;
        float startTime = 0;
        float holdTime = 0;
        List<GameObject> correctObjects = new List<GameObject>();

        InflateBalloon.AddSpecificInitializationMethod(() =>
        {
            ScalePerInflation_Y = (MaxInflation_Y - TrialStim.transform.localScale.y) / (SideChoice == "Left" ? CurrentTrial.NumClicksLeft : CurrentTrial.NumClicksRight);
            IncrementAmounts = new Vector3();
            Flashing = false;
            InflateAudioPlayed = false;
            InflationDuration = 0;
            ScaleTimer = 0;
            Session.MouseTracker.ResetClicks();
            clickTimings = new List<float>();
            timeTracker = 0;

            TrialTouches = 0;
            NumInflations = 0;

            if (Handler.AllSelections.Count > 0)
                Handler.ClearSelections();

            SetTrialSummaryString();

            outlineClicksRemaining = CurrentTrial.ClicksPerOutline;
            startTime = 0;
            holdTime = 0;
            successfulSelections = 0;

            correctObjects = new List<GameObject>() {TrialStim, MaxOutline_Left, MaxOutline_Right };

            Inflate = false;

            if (Handler != null)
                Handler.HandlerActive = true;
        });
        InflateBalloon.AddUpdateMethod(() =>
        {
            InflationDuration += Time.deltaTime;

            if (Inflate)
            {
                if (!InflateAudioPlayed)
                {
                    AudioFBController.Play("EC_Inflate");
                    InflateAudioPlayed = true;
                }

                ScaleTimer += Time.deltaTime;
                if (ScaleTimer >= (InflateClipDuration / scalingInterval.value)) //When timer hits for next inflation
                {
                    if (TrialStim.transform.localScale != NextScale)
                    {
                        ScaleToNextInterval();
                    }
                    else //Reached the scale 
                    {
                        Inflate = false;
                        Handler.HandlerActive = true;
                        if (NumInflations >= InflationsNeeded) //Done enough inflations
                        {
                            Response = 1;
                            AvgClickTime = clickTimings.Average();
                        }
                    }
                    ScaleTimer = 0; //Reset Timer for next inflation increment;
                }
            }

            if (InputBroker.GetMouseButtonDown(0))
            {
                startTime = Time.time;

                //Neg FB if touch outside balloon. Adding response != 1 so that they cant click outside balloon at the end and mess up pop audio.
                if (Response != 1)
                {
                    GameObject hitGO = InputBroker.RaycastBoth(InputBroker.mousePosition);
                    if (hitGO == null)
                        AudioFBController.Play("Negative");
                }
            }

            if(InputBroker.GetMouseButtonUp(0))
            {
                TrialTouches++;
                TotalTouches_Block++;
                CurrentTaskLevel.Touches_Task++;
                SetTrialSummaryString();
                CurrentTaskLevel.CalculateBlockSummaryString();
                CurrentTaskLevel.SetTaskSummaryString();

                holdTime = Time.time - startTime;

                if(holdTime >= Handler.MinDuration && holdTime < Handler.MaxDuration)
                {
                    GameObject clickedGO = InputBroker.RaycastBoth(InputBroker.mousePosition);
                    if(clickedGO != null && correctObjects.Contains(clickedGO)) //If they correctly clicked inside the balloon
                    {
                        successfulSelections++;

                        if (outlineClicksRemaining > 1 && !Inflate)
                        {
                            if (outlineClicksRemaining > 1) //Dont play on the last one because full inflate will play 
                                AudioFBController.Play("SHORT_INFLATION");
                            outlineClicksRemaining--;
                        }
                        else if (outlineClicksRemaining == 1 && !Inflate)
                        {
                            if (NumInflations < InflationsNeeded)
                            {
                                Input.ResetInputAxes();
                                clickTimings.Add(Time.time - timeTracker);
                                timeTracker = Time.time;

                                Handler.HandlerActive = false;
                                NumInflations++;

                                Session.EventCodeManager.AddToFrameEventCodeBuffer("CorrectResponse");

                                CalculateInflation(); //Sets Inflate to TRUE at end of func
                                InflateAudioPlayed = false;

                                outlineClicksRemaining = CurrentTrial.ClicksPerOutline;
                            }
                        }
                    }
                }
            }
        });
        InflateBalloon.AddTimer(() => inflateDuration.value, Delay);
        InflateBalloon.SpecifyTermination(() => Response == 1, Delay);
        InflateBalloon.AddDefaultTerminationMethod(() =>
        {
            StateAfterDelay = Feedback;
            DelayDuration = popToFeedbackDelay.value;
            
            if (SideChoice == "Left")
                MaxOutline_Left.transform.parent = OutlineContainerLeft.transform;
            else
                MaxOutline_Right.transform.parent = OutlineContainerRight.transform;

            DestroyChildren(SideChoice == "Left" ? OutlineContainerLeft : OutlineContainerRight);
            InflateAudioPlayed = false;
            
            if (Response == 1)
            {
                InflationDurations_Block.Add(InflationDuration);
                CurrentTaskLevel.InflationDurations_Task.Add(InflationDuration);
                AudioFBController.Play(Session.SessionDef.IsHuman ? "EC_HarshPop" : "EC_NicePop");
            }
            else
            {
                Session.EventCodeManager.AddToFrameEventCodeBuffer("NoChoice");
                Session.EventCodeManager.SendRangeCode("CustomAbortTrial", AbortCodeDict["NoSelectionMade"]);
                AbortCode = 6;

                InflationDurations_Block.Add(null);
                CurrentTaskLevel.InflationDurations_Task.Add(null);

                AudioFBController.Play("TimeRanOut");
                TokenFBController.enabled = false;
            }
            TrialStim.SetActive(false);
        });

        //Feedback state ------------------------------------------------------------------------------------------------------------------------------------------------
        Feedback.AddSpecificInitializationMethod(() =>
        {
            AddTokenInflateAudioPlayed = false;

            if (Response == 1)
            {
                GameObject CenteredGO = new GameObject();
                CenteredGO.transform.position = new Vector3(0, .5f, 0);
                TokenFBController.AddTokens(CenteredGO, SideChoice == "Left" ? CurrentTrial.NumCoinsLeft : CurrentTrial.NumCoinsRight);
                Destroy(CenteredGO);

                Completions_Block++;
                CurrentTaskLevel.Completions_Task++;

                AddTokenInflateAudioPlayed = true;
            }
        });
        Feedback.SpecifyTermination(() => AddTokenInflateAudioPlayed && !TokenFBController.IsAnimating(), ITI);
        Feedback.SpecifyTermination(() => true && Response != 1, ITI);
        Feedback.AddUniversalTerminationMethod(() =>
        {
            if(TokenFBController.IsTokenBarFull())
                GiveReward();
            TokenFBController.enabled = false;
            AddTokenInflateAudioPlayed = false;
        });

        //ITI state ------------------------------------------------------------------------------------------------------------------------------------------------------
        ITI.AddTimer(itiDuration.value, FinishTrial);
        
        DefineTrialData();
        DefineFrameData();
    }

    //HELPER FUNCTIONS ---------------------------------------------------------------------------------------------------------------------------------------------------
    public override void ResetTrialVariables()
    {
        NumInflations = 0;
        Response = -1;
        ChooseDuration = 0;
        InflationDuration = 0;
        InflationsNeeded = 0;
        AvgClickTime = 0;
        SideChoice = "";
        EffortChoice = "";
        RewardChoice = "";
        TrialTouches = 0;
    }

    private void DeactivateGameObjects()
    {
        if (TrialStim != null && TrialStim.activeInHierarchy)
            TrialStim.SetActive(false);

        if (MiddleBarrier.activeInHierarchy)
            MiddleBarrier.SetActive(false);

        if (OutlineContainerLeft != null)
            DestroyChildren(OutlineContainerLeft);
        if (OutlineContainerRight != null)
            DestroyChildren(OutlineContainerRight);
        if (RewardContainerLeft != null)
            DestroyChildren(RewardContainerLeft);
        if (RewardContainerRight != null)
            DestroyChildren(RewardContainerRight);

        Destroy(MaxOutline_Right);
        Destroy(MaxOutline_Left);
    }

    public override void FinishTrialCleanup() //called automatically at start of FinishTrial state
    {
        DeactivateGameObjects();

        if (AbortCode == 0) //Normal
            CurrentTaskLevel.CalculateBlockSummaryString();
        else
        {
            CurrentTaskLevel.NumAbortedTrials_InBlock++;
            CurrentTaskLevel.NumAbortedTrials_InTask++;
            CurrentTaskLevel.ClearStrings();
            CurrentTaskLevel.CurrentBlockSummaryString.AppendLine("");
        }

        ClearTrialSummaryString();
        TokenFBController.ResetTokenBarFull();
    }

    public override void DefineCustomTrialDefSelection()
    {
        TrialDefSelectionStyle = CurrentTrial.TrialDefSelectionStyle;
        posStep = CurrentTrial.PosStep;
        negStep = CurrentTrial.NegStep;
        maxDiffLevel = CurrentTrial.MaxDiffLevel;
        avgDiffLevel = CurrentTrial.AvgDiffLevel;
        diffLevelJitter = CurrentTrial.DiffLevelJitter;
        NumReversalsUntilTerm = CurrentTrial.NumReversalsUntilTerm;
        MinTrialsBeforeTerm = CurrentTrial.MinTrialsBeforeTerm;
        TerminationWindowSize = CurrentTrial.TerminationWindowSize;
        //BlockCount = CurrentTaskLevel.currentBlockDef.BlockCount;
        
        int randomDouble = avgDiffLevel + Random.Range(-diffLevelJitter, diffLevelJitter);
        difficultyLevel = randomDouble;
    }
    
    public void ActivateObjects()
    {
        foreach (GameObject go in ObjectList)
        {
            if (go != null)
                go.SetActive(true);
        }
    }

    public void ResetBlockVariables()
    {
        Completions_Block = 0;
        NumChosenLeft_Block = 0;
        NumChosenRight_Block = 0;
        NumHigherRewardChosen_Block = 0;
        NumLowerRewardChosen_Block = 0;
        NumHigherEffortChosen_Block = 0;
        NumLowerEffortChosen_Block = 0;
        NumSameEffortChosen_Block = 0;
        NumSameRewardChosen_Block = 0;
        TotalTouches_Block = 0;
        calculatedThreshold = 0;
        reversalsCount = 0;
        DiffLevelsAtReversals.Clear();
        InflationDurations_Block.Clear();
        runningPerformance.Clear();
    }

    private void ScaleToNextInterval()
    {
        //If close and next increment would go over target scale, recalculate the exact amount:
        if (TrialStim.transform.localScale.x + IncrementAmounts.x > NextScale.x) 
            IncrementAmounts = new Vector3((NextScale.x - TrialStim.transform.localScale.x), (NextScale.y - TrialStim.transform.localScale.y), (NextScale.z - TrialStim.transform.localScale.z));

        //Scale:
        TrialStim.transform.localScale += new Vector3(IncrementAmounts.x, IncrementAmounts.y, IncrementAmounts.z);
    }

    private void CalculateInflation()
    {      
        GameObject container = (SideChoice == "Left") ? OutlineContainerLeft : OutlineContainerRight;
        NextScale = container.transform.GetChild(NumInflations-1).transform.localScale;
        NextScale.y = ScalePerInflation_Y + TrialStim.transform.localScale.y;
        Vector3 difference = NextScale - TrialStim.transform.localScale;
        IncrementAmounts = new Vector3((difference.x / scalingInterval.value), (difference.y / scalingInterval.value), (difference.z / scalingInterval.value));

        Inflate = true;
    }

    private void RecordChoices()
    {
        if(SideChoice == "Left")
        {
            NumChosenLeft_Block++;
            CurrentTaskLevel.NumChosenLeft_Task++;
            EffortChoice = CompareValues(CurrentTrial.NumClicksLeft, CurrentTrial.NumClicksRight);
            RewardChoice = CompareValues(CurrentTrial.NumCoinsLeft, CurrentTrial.NumCoinsRight);
        }
        else
        {
            NumChosenRight_Block++;
            CurrentTaskLevel.NumChosenRight_Task++;
            EffortChoice = CompareValues(CurrentTrial.NumClicksRight, CurrentTrial.NumClicksLeft);
            RewardChoice = CompareValues(CurrentTrial.NumCoinsRight, CurrentTrial.NumCoinsLeft);
        }

        if (EffortChoice == "Higher")
        {
            NumHigherEffortChosen_Block++;
            CurrentTaskLevel.NumHigherEffortChosen_Task++;
            runningPerformance.Add(1);

        }
        else if (EffortChoice == "Lower")
        {
            NumLowerEffortChosen_Block++;
            CurrentTaskLevel.NumLowerEffortChosen_Task++;
            runningPerformance.Add(0);

        }
        else
        {
            NumSameEffortChosen_Block++;
            CurrentTaskLevel.NumSameEffortChosen_Task++;
        }

        if (RewardChoice == "Higher")
        {
            NumHigherRewardChosen_Block++;
            CurrentTaskLevel.NumHigherRewardChosen_Task++;
        }
        else if (RewardChoice == "Lower")
        {
            NumLowerRewardChosen_Block++;
            CurrentTaskLevel.NumLowerRewardChosen_Task++;
        }
        else
        {
            NumSameRewardChosen_Block++;
            CurrentTaskLevel.NumSameRewardChosen_Task++;
        }
    }

    public string CompareValues(int chosenValue, int otherValue)
    {
        if (chosenValue == otherValue)
            return "Same";
        else if (chosenValue > otherValue)
            return "Higher";
        else
            return "Lower";
    }

    private void SetTokenVariables()
    {
        if (TokenFBController == null)
            return;

        TokenFBController.tokenSize = 106f;
        TokenFBController.tokenBoxYOffset = 5f;

        TokenFBController.SetFlashingTime(1.5f);
        TokenFBController.tokenSpacing = -(Screen.width * .009375f);
    }

    private void SetParents(GameObject wrapper, List<GameObject> objects) // 1) Setting the parent of each GO, and 2) Adding to RemovalList (so can remove easily later)
    {
        RemoveParentList = new List<GameObject>();
        foreach(GameObject go in objects)
        {
            go.transform.parent = wrapper.transform; //set parent
            RemoveParentList.Add(go); //add to remove parent list for later
        }
    }

    private void RemoveParents(GameObject wrapper, List<GameObject> objects)
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

    private void ResetToOriginalPositions()
    {
        OutlineContainerLeft.transform.position = LeftContainerOriginalPosition;
        RewardContainerLeft.transform.position = LeftRewardContainerOriginalPosition;
        StimLeft.transform.position = LeftStimOriginalPosition;

        OutlineContainerRight.transform.position = RightContainerOriginalPosition;
        RewardContainerRight.transform.position = RightRewardContainerOriginalPosition;
        StimRight.transform.position = RightStimOriginalPosition;
    }

    private void GiveReward()
    {
        if (Session.SyncBoxController == null)
            return;

        if (SideChoice == "Left")
        {
            Session.SyncBoxController.SendRewardPulses(CurrentTrial.NumPulsesLeft, CurrentTrial.PulseSizeLeft);
            CurrentTaskLevel.NumRewardPulses_InBlock += CurrentTrial.NumPulsesLeft;
            CurrentTaskLevel.NumRewardPulses_InTask += CurrentTrial.NumPulsesLeft;

        }
        else
        {
            Session.SyncBoxController.SendRewardPulses(CurrentTrial.NumPulsesRight, CurrentTrial.PulseSizeRight);
            CurrentTaskLevel.NumRewardPulses_InBlock += CurrentTrial.NumPulsesRight;
            CurrentTaskLevel.NumRewardPulses_InTask += CurrentTrial.NumPulsesRight;
        }
    }

    private void DisableAllGameobjects()
    {
        if(StimLeft != null)
            StimLeft.SetActive(false);
        if(StimRight != null)
            StimRight.SetActive(false);
        if(BalloonOutline != null)
            BalloonOutline.SetActive(false);
    }

    private void LoadConfigUIVariables()
    {
        minObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("minObjectTouchDuration");
        maxObjectTouchDuration = ConfigUiVariables.get<ConfigNumber>("maxObjectTouchDuration");
        scalingInterval = ConfigUiVariables.get<ConfigNumber>("scalingInterval");
        inflateDuration = ConfigUiVariables.get<ConfigNumber>("inflateDuration");
        itiDuration = ConfigUiVariables.get<ConfigNumber>("itiDuration");
        popToFeedbackDelay = ConfigUiVariables.get<ConfigNumber>("popToFeedbackDelay");
        choiceToTouchDelay = ConfigUiVariables.get<ConfigNumber>("choiceToTouchDelay");
        sbToBalloonDelay = ConfigUiVariables.get<ConfigNumber>("sbToBalloonDelay");
    }


    private GameObject CreateBalloon(GameObject prefab, Vector3 viewPos, string name)
    {
        Vector3 worldPos = Camera.main.ViewportToWorldPoint(viewPos);
        GameObject instantiated = Instantiate(prefab, worldPos, prefab.transform.rotation);
        instantiated.name = name;
        return instantiated;
    }

    private void CreateObjects()
    {
        Vector3 stimLeftViewPos = new Vector3(.25f, .1925f, 2.225f);
        StimLeft = CreateBalloon(StimLeftPrefab, stimLeftViewPos, "StimLeft");
        StimLeft.transform.localScale = new Vector3(9f, 1, 9f);
        Red = StimLeft.GetComponent<Renderer>().material.color;
        TrialStimInitLocalScale = StimLeft.transform.localScale;
        AddRigidBody(StimLeft);
        ObjectList.Add(StimLeft);

        Vector3 stimRightViewPos = new Vector3(.75f, .1925f, 2.225f);
        StimRight = CreateBalloon(StimRightPrefab, stimRightViewPos, "StimRight");
        StimRight.transform.localScale = new Vector3(9f, 1, 9f);
        AddRigidBody(StimRight);
        ObjectList.Add(StimRight);


        Reward = Instantiate(RewardPrefab, RewardPrefab.transform.position, RewardPrefab.transform.rotation);
        Reward.name = "Reward";
        Reward.GetComponent<Renderer>().material.color = Color.gray; //turn token color to grey so they dont look collected yet.

        BalloonOutline = Instantiate(OutlinePrefab, OutlinePrefab.transform.position, OutlinePrefab.transform.rotation);
        BalloonOutline.name = "Outline";
        BalloonOutline.transform.localScale = new Vector3(10, 0.01f, 10);
        BalloonOutline.GetComponent<Renderer>().material.color = OffWhiteOutlineColor;

        OutlineContainerLeft = new GameObject("OutlineContainerLeft");
        ObjectList.Add(OutlineContainerLeft);
        OutlineContainerRight = new GameObject("OutlineContainerRight");
        ObjectList.Add(OutlineContainerRight);

        RewardContainerLeft = new GameObject("RewardContainerLeft");
        ObjectList.Add(RewardContainerLeft);
        RewardContainerRight = new GameObject("RewardContainerRight");
        ObjectList.Add(RewardContainerRight);

        CreateMiddleBarrier();

        LeftContainerOriginalPosition = OutlineContainerLeft.transform.position;
        RightContainerOriginalPosition = OutlineContainerRight.transform.position;
        LeftRewardContainerOriginalPosition = RewardContainerLeft.transform.position;
        RightRewardContainerOriginalPosition = RewardContainerRight.transform.position;
        LeftStimOriginalPosition = StimLeft.transform.position;
        RightStimOriginalPosition = StimRight.transform.position;

        StartButton.SetActive(false);
        BalloonOutline.SetActive(false);
        Reward.SetActive(false);

        ObjectsCreated = true;
    }

    private void CreateMiddleBarrier()
    {
        MiddleBarrier = new GameObject("MiddleBarrier");
        MiddleBarrier.transform.SetParent(EC_CanvasGO.transform, false);
        Image image = MiddleBarrier.AddComponent<Image>();
        image.rectTransform.anchoredPosition = Vector2.zero;
        image.transform.localScale = new Vector3(.06f, 20f, .001f);
        MiddleBarrier.SetActive(false);
    }

    private void CreateTransparentBalloons()
    {
        //transparent balloon with size of biggest outline, allows easy detection of clicks within the entire balloon
        MaxOutline_Left = Instantiate(StimNoMaterialPrefab, StimLeft.transform.position, StimLeftPrefab.transform.rotation);
        MaxOutline_Left.name = "MaxOutline_Left";
        MaxOutline_Left.transform.localScale = new Vector3(77f, .1f, 77f);
        MaxOutline_Left.transform.SetParent(OutlineContainerLeft.transform);

        MaxOutline_Right = Instantiate(StimNoMaterialPrefab, StimRight.transform.position, StimRightPrefab.transform.rotation);
        MaxOutline_Right.name = "MaxOutline_Right";
        MaxOutline_Right.transform.localScale = new Vector3(77f, .1f, 77f);
        MaxOutline_Right.transform.SetParent(OutlineContainerRight.transform);

        ObjectList.Add(MaxOutline_Left);
        ObjectList.Add(MaxOutline_Right);

        Session.TargetObjects.Add(MaxOutline_Left);
        Session.TargetObjects.Add(MaxOutline_Right);
    }

    private void CreateBalloonOutlines(int numBalloons, Vector3 ScaleUpAmount, Vector3 pos, GameObject container)
    {
        for (int i = 1; i <= numBalloons; i ++)
        {
            GameObject outline = Instantiate(BalloonOutline, pos, BalloonOutline.transform.rotation);
            outline.transform.parent = container.transform;
            outline.name = "Outline_" + (container.name.ToLower().Contains("left") ? "Left_" : "Right_") + i;
            outline.transform.localScale += i * ScaleUpAmount;
            AddRigidBody(outline);
            ObjectList.Add(outline);
            Session.TargetObjects.Add(outline);
        }
    }

    private void CreateRewards(int NumRewards, Vector3 pos, GameObject container)
    {
        int numCoins = Mathf.Max(CurrentTrial.NumCoinsLeft, CurrentTrial.NumCoinsRight);
        float scaler = 1f; //100% for 9 or less
        if(numCoins > 9)
        {
            int num = numCoins;
            while (num > 9)
            {
                scaler -= .0555f;
                num--;
            }
        }

        //3D REWARDS:
        float width = (Reward.GetComponent<Renderer>().bounds.size.x - .035f) * scaler; //Get Reward width! (-.35f cuz need to be closer together)
        pos -= new Vector3((NumRewards - 1) * (width / 2), 0, 0);
        for (int i = 0; i < NumRewards; i++)
        {
            GameObject RewardClone = Instantiate(Reward, pos, Reward.transform.rotation, container.transform);
            RewardClone.transform.localScale *= scaler;
            RewardClone.transform.Translate(new Vector3(i * width, 0.028f, -.0001f), Space.World);
            RewardClone.name = "Reward" + SideChoice + (i + 1);
            AddRigidBody(RewardClone);
            ObjectList.Add(RewardClone);
        }
    }

    private void SetTrialSummaryString()
    {
        TrialSummaryString = "Touches: " + TrialTouches +
                            "\nSide Chosen: " + SideChoice +
                            "\nReward Chosen: " + RewardChoice +
                            "\nEffort Chosen: " + EffortChoice;
    }

    private void ClearTrialSummaryString()
    {
        TrialSummaryString = "";
    }

    private void DefineTrialData()
    {
        TrialData.AddDatum("TrialID", () => CurrentTrial.TrialID);
        TrialData.AddDatum("InflationsNeeded", () => InflationsNeeded);
        TrialData.AddDatum("ClicksNeededLeft", () => CurrentTrial.NumClicksLeft);
        TrialData.AddDatum("ClicksNeededRight", () => CurrentTrial.NumClicksRight);
        TrialData.AddDatum("NumCoinsLeft", () => CurrentTrial.NumCoinsLeft);
        TrialData.AddDatum("NumCoinsRight", () => CurrentTrial.NumCoinsRight);
        TrialData.AddDatum("ChosenSide", () => SideChoice);
        TrialData.AddDatum("ChosenEffort", () => EffortChoice);
        TrialData.AddDatum("ChosenReward", () => RewardChoice);
        TrialData.AddDatum("TimeTakenToChoose", () => ChooseDuration);
        TrialData.AddDatum("TimeTakenToInflateBaloon", () => InflationDuration);
        TrialData.AddDatum("AverageClickTimes", () => AvgClickTime);
        TrialData.AddDatum("ClicksPerOutline", () => CurrentTrial.ClicksPerOutline);
        TrialData.AddDatum("TrialTouches", () => TrialTouches);
    }

    private void DefineFrameData()
    {
        FrameData.AddDatum("TouchPosition", () => InputBroker.mousePosition);
        FrameData.AddDatum("StartButton", () => StartButton?.activeInHierarchy);
        FrameData.AddDatum("StimLeft", () => StimLeft?.activeInHierarchy);
        FrameData.AddDatum("StimRight", () => StimRight?.activeInHierarchy);
    }

    protected override bool CheckBlockEnd()
    {
        int prevResult = -1;

        Debug.Log("runningPerformance.Count: " + runningPerformance.Count + "/ mintrialsbeforeterm: " + MinTrialsBeforeTerm);
        if (MinTrialsBeforeTerm < 0 || runningPerformance.Count < MinTrialsBeforeTerm + 1)
            return false;

        if (runningPerformance.Count > 1)
        {
            prevResult = runningPerformance[^2];
        }

        if (runningPerformance.Last() == 1)
        {
            if (prevResult == 0)
            {
                DiffLevelsAtReversals.Add(CurrentTrial.DifficultyLevel);
                reversalsCount++;
            }
        }
        else if (runningPerformance.Last() == 0)
        {
            if (prevResult == 1)
            {
                DiffLevelsAtReversals.Add(CurrentTrial.DifficultyLevel);
                reversalsCount++;
            }
        }

        //TaskLevelTemplate_Methods TaskLevel_Methods = new TaskLevelTemplate_Methods();
        Debug.Log("reversalsCount: " + reversalsCount + " / NumReversalsUntilTerm: " + NumReversalsUntilTerm);
        if (NumReversalsUntilTerm != -1 && reversalsCount >= NumReversalsUntilTerm)
        {
            List<int> lastElements = DiffLevelsAtReversals.Skip(DiffLevelsAtReversals.Count - NumReversalsUntilTerm).ToList();
            calculatedThreshold = (int)lastElements.Average();
            Debug.Log("The average DL at the last " + NumReversalsUntilTerm + " reversals is " + calculatedThreshold);
            return true;
        }
        return false;
    }

}
