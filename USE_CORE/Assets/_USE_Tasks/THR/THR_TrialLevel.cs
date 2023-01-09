using UnityEngine;
using System.Collections.Generic;
using System;
using Random = UnityEngine.Random;
using ConfigDynamicUI;
using System.Collections;
using System.Linq;
using UnityEngine.UI;
using System.IO;
using UnityEngine.Profiling;
using TMPro;
using USE_ExperimentTemplate_Block;
using USE_ExperimentTemplate_Trial;
using USE_Settings;
using USE_States;
using USE_StimulusManagement;
using THR_Namespace;
using UnityEditor.UIElements;
using UnityEngine.EventSystems;

public class THR_TrialLevel : ControlLevel_Trial_Template
{
    public THR_TrialDef CurrentTrial => GetCurrentTrialDef<THR_TrialDef>();

    public string MaterialFilePath;
    public string ContextPath;

    public Texture2D BackdropStripeTexture;
    public Texture2D HeldTooShortTexture;
    public Texture2D HeldTooLongTexture;

    public Renderer BackdropRenderer;
    public Texture BackdropTexture;
    public GameObject BackdropPrefab;
    public GameObject BackdropGO;
    public GameObject SquarePrefab;
    public GameObject SquareGO;
    public Renderer SquareRenderer;
    public Texture SquareTexture;
    public Canvas THR_Canvas;
    public GameObject THR_CanvasGO;

    public float TrialStartTime;
    public float TouchStartTime;
    public float TouchReleaseTime;
    public float HeldDuration;
    public float BackdropTouchTime;
    public float BackdropTouches;

    public bool TrialComplete;

    public bool GiveTouchReward;
    public bool GiveReleaseReward;
    public bool ClickedSquare;
    public bool TimeRanOut;
    public bool ConfigVariablesLoaded;

    public List<int> TrialCompletionList;
    public int TrialsCompleted_Block;
    public int TrialsCorrect_Block;

    //Data variables:
    public int BackdropTouches_Trial;
    public int BlueSquareTouches_Trial;
    public int WhiteSquareTouches_Trial;
    public int ItiTouches_Trial;
    public int TouchRewards_Trial;
    public int ReleaseRewards_Trial;
    public int NumTouchesMovedOutside_Trial;

    public int BackdropTouches_Block; 
    public int BlueSquareTouches_Block; 
    public int WhiteSquareTouches_Block;
    public int NumTouchRewards_Block;
    public int NumReleaseRewards_Block;
    public int NumItiTouches_Block;
    public int NumReleasedEarly_Block;
    public int NumReleasedLate_Block;
    public int NumTouchesMovedOutside_Block;

    //Set in Editor
    public Material SquareMaterial;
    public Material BackdropMaterial;

    //misc
    public bool WhiteSquareTouched;
    public bool BlueSquareTouched;
    public bool BlueSquareReleased;
    public bool ClickedOutsideSquare;
    public bool ClickedWhiteSquare;
    public bool IsLevelOne;
    public bool PerfThresholdMet;
    public bool ColorsSet;

    public Color32 LightBlueColor;
    public Color32 LightRedColor;
    public Color32 DarkBlueBackgroundColor;
    public Color32 InitialSquareColor;
    public Color32 InitialBackdropColor;

    public float BlockDefaultSquareSize;
    public float BlockDefaultPositionX;
    public float BlockDefaultPositionY;
    public float BlockDefaultMinTouchDuration;
    public float BlockDefaultMaxTouchDuration;
    public float BlockDefaultWhiteSquareDuration;
    public float BlockDefaultBlueSquareDuration;

    public bool AudioPlayed;
    public bool Grating;
    public bool HeldTooShort;
    public bool HeldTooLong;

    public float WhiteTimeoutTime;
    public float WhiteStartTime;
    public float BlueStartTime;
    public float ReactionTime
    {
        get
        {
            return TouchStartTime - TrialStartTime;
        }
    }

    public float WhiteTimeoutDuration;

    public float GraySquareTimer;
    public float RewardEarnedTime;
    public float RewardGivenTime;
    public float RewardTimer;
    public bool RewardGiven;

    public float HoldSquareTime;
    public bool MovedOutside;

    public override void DefineControlLevel()
    {
        State InitTrial = new State("InitTrial");
        State WhiteSquare = new State("WhiteSquare");
        State BlueSquare = new State("BlueSquare");
        State Feedback = new State("Feedback");
        State ITI = new State("ITI");
        AddActiveStates(new List<State> { InitTrial, WhiteSquare, BlueSquare, Feedback, ITI});


        //SETUP TRIAL state -------------------------------------------------------------------------------------------------------------------------
        SetupTrial.AddInitializationMethod(() =>
        {
            Cursor.visible = false; //Set cursor to invisible and then User can click C hotkey to turn on if needed. 

            if (TrialCount_InBlock == 0)
                TrialCompletionList = new List<int>();

            if (TrialCount_InBlock == 0)
                SetBlockDefaultValues();
            
            if(!ColorsSet)
                CreateColors();

            if (BackdropGO == null)
                CreateBackdrop();

            if (SquareGO == null)
                CreateSquare();

            SetTrialSummaryString();
        });
        SetupTrial.SpecifyTermination(() => true, InitTrial);

        //INIT TRIAL state --------------------------------------------------------------------------------------------------------------------------
        InitTrial.AddInitializationMethod(() =>
        {
            if(HeldTooLongTexture == null) //don't have to check all 3 since they're both activated at same time. 
                LoadGratingMaterials();

            ResetGlobalTrialVariables();

            if(TrialCount_InBlock == 0)
            {
                SetSquareSizeAndPosition();
                SetConfigValuesToBlockDefaults();
            }
            else
            {
                if(ConfigValuesChanged())
                {
                    SetTrialValuesToConfigValues();
                    UpdateSquare();
                }
                else
                    SetSquareSizeAndPosition();
            }
        });
        InitTrial.SpecifyTermination(() => true, WhiteSquare, () => TrialStartTime = Time.time);

        //WHITE SQUARE state ------------------------------------------------------------------------------------------------------------------------
        SelectionHandler<THR_StimDef> mouseHandler = new SelectionHandler<THR_StimDef>();
        MouseTracker.AddSelectionHandler(mouseHandler, WhiteSquare);

        WhiteSquare.AddInitializationMethod(() =>
        {
            Input.ResetInputAxes(); //reset input in case they still touching!
            SquareMaterial.color = Color.white;
            if (!SquareGO.activeSelf) //don't have to check both square and backdrop since they're both activated at same time. 
                ActivateSquareAndBackdrop();
            WhiteStartTime = Time.time;
            WhiteTimeoutTime = 0;
            WhiteSquareTouched = false;
        });
        WhiteSquare.AddUpdateMethod(() =>
        {
            if(WhiteTimeoutTime != 0 && (Time.time - WhiteTimeoutTime) > CurrentTrial.TimeoutDuration)
                WhiteTimeoutTime = 0;
            
            if(MouseTracker.CurrentTargetGameObject == SquareGO)
            {
                ClickedWhiteSquare = true;
                Input.ResetInputAxes(); //to stop them from holding down white square. 
                if(WhiteTimeoutTime == 0)
                {
                    WhiteTimeoutTime = Time.time;
                    WhiteStartTime = Time.time; //reset original WhiteStartTime so that normal duration resets.
                    if(!AudioFBController.IsPlaying()) //will keep playing every timeout duration period if they still holding. Which could be good to teach them not to!
                        AudioFBController.Play("Negative");
                }
            }
           
            if (InputBroker.GetMouseButtonUp(0) && ClickedWhiteSquare)
            {
                WhiteSquareTouches_Trial++;
                ClickedWhiteSquare = false;
            }
        });
        WhiteSquare.SpecifyTermination(() => ((Time.time - WhiteStartTime) > CurrentTrial.WhiteSquareDuration) && WhiteTimeoutTime == 0 && !InputBroker.GetMouseButton(0), BlueSquare);

        //BLUE SQUARE state -------------------------------------------------------------------------------------------------------------------------
        MouseTracker.AddSelectionHandler(mouseHandler, BlueSquare);

        BlueSquare.AddInitializationMethod(() =>
        {
            Input.ResetInputAxes(); //reset input in case they still touching!
            SquareMaterial.color = LightBlueColor;
            if (!SquareGO.activeSelf)
                ActivateSquareAndBackdrop();
            BlueStartTime = Time.time;
            BlueSquareTouched = false;
            BlueSquareReleased = false;
            BackdropTouchTime = 0;
            BackdropTouches = 0;
            MovedOutside = false;
        });
        BlueSquare.AddUpdateMethod(() =>
        {
            //If they clicked the Square (and its not still grating from a previous error)
            if (MouseTracker.CurrentTargetGameObject == SquareGO && !Grating)
            {
                if (!BlueSquareTouched)
                {
                    TouchStartTime = Time.time;
                    BlueSquareTouched = true;
                }

                HeldDuration = mouseHandler.currentTargetDuration;
                //Fix short touches from turning blue for split sec before changing. 
                if (HeldDuration >= .045f) 
                    SquareMaterial.color = Color.blue;

                if (CurrentTrial.RewardTouch)
                {
                    BlueSquareTouches_Trial++;
                    GiveTouchReward = true;
                    RewardEarnedTime = Time.time;
                }
                //Auto stop them if holding the square for max duration
                if (mouseHandler.currentTargetDuration > CurrentTrial.MaxTouchDuration)
                {
                    NumReleasedLate_Block++;
                    HeldTooLong = true;
                    BlueSquareReleased = true; //force bluesquarereleased if they holding for max duration, so that it can go to neg FB. 
                }
            }
            //If they clicked the Backdrop (and not after clicking and holding the square (handled down below))
            if (MouseTracker.CurrentTargetGameObject == BackdropGO && !BlueSquareTouched && !Grating)
            {
                ClickedOutsideSquare = true;
                if (BackdropTouches == 0)
                {
                    BackdropTouchTime = Time.time;
                    BlueStartTime += CurrentTrial.TimeoutDuration; //add extra second so it doesn't go straight to white after grating
                    Input.ResetInputAxes(); //Reset input axis so they can't keep holding down on the backdrop.
                    StartCoroutine(GratedBackdropFlash(BackdropStripeTexture)); //Turn the backdrop to grated texture
                    BackdropTouches++;
                }
            }
            //If they already clicked the backdrop once, and the timeoutduration has lapsed, reset the variables
            if (BackdropTouchTime != 0 && (Time.time - BackdropTouchTime) > CurrentTrial.TimeoutDuration)
            {
                BackdropTouches = 0;
                BackdropTouchTime = 0;
            }

            //When they first touch blue square, and then move it over to backdrop.
            if (MouseTracker.CurrentTargetGameObject == BackdropGO && BlueSquareTouched)
            {
                MovedOutside = true;
                BlueSquareReleased = true; // force the state to end. 
                //incrementing MovedOutsideTouches down in feedback instead of here in update.
            }

            if (InputBroker.GetMouseButtonUp(0) && !Grating)
            {
                if(BlueSquareTouched && !BlueSquareReleased)
                {
                    TouchReleaseTime = Time.time;
                    BlueSquareTouches_Trial++;
                    HeldDuration = mouseHandler.currentTargetDuration;
                    if(CurrentTrial.RewardRelease)
                    {
                        if(HeldDuration >= CurrentTrial.MinTouchDuration && HeldDuration <= CurrentTrial.MaxTouchDuration)
                        {
                            GiveReleaseReward = true;
                            RewardEarnedTime = Time.time;
                        }
                        else if(HeldDuration < CurrentTrial.MinTouchDuration)
                        {
                            NumReleasedEarly_Block++;
                            HeldTooShort = true;
                        }
                        //The Else (Greater than MaxDuration) is handled above where I auto stop them for holding for max dur. 
                    }
                    else
                        SquareMaterial.color = Color.gray;

                    BlueSquareReleased = true;
                }
                if(ClickedOutsideSquare)
                {
                    BackdropTouches_Trial++;
                    ClickedOutsideSquare = false;
                }
            }
            if(Time.time - TrialStartTime > CurrentTrial.TimeToAutoEndTrialSec)
                TimeRanOut = true;
        });
        //Go back to white square if bluesquare time lapses (and they aren't already holding down)
        BlueSquare.SpecifyTermination(() => (Time.time - BlueStartTime > CurrentTrial.BlueSquareDuration) && !InputBroker.GetMouseButton(0) && !BlueSquareReleased && !Grating, WhiteSquare);
        BlueSquare.SpecifyTermination(() => (BlueSquareReleased && !Grating) || TimeRanOut || GiveTouchReward, Feedback); //If rewarding touch and they touched, or click the square and release, or run out of time. 

        //FEEDBACK state ----------------------------------------------------------------------------------------------------------------------------
        Feedback.AddInitializationMethod(() =>
        {
            RewardTimer = Time.time - RewardEarnedTime; //start the timer at the difference between rewardtimeEarned and right now.
            GraySquareTimer = 0;
            AudioPlayed = false;

            if(GiveTouchReward || GiveReleaseReward)
            {
                AudioFBController.Play("Positive");
                if(GiveReleaseReward)
                    SquareMaterial.color = Color.gray;
            }
            else //held too long, held too short, moved outside, or timeRanOut
            {
                AudioFBController.Play("Negative");
                if (HeldTooShort)
                    StartCoroutine(GratedSquareFlash(HeldTooShortTexture));
                else if (HeldTooLong)
                    StartCoroutine(GratedSquareFlash(HeldTooLongTexture));
                else if (MovedOutside)
                {
                    StartCoroutine(GratedSquareFlash(BackdropStripeTexture));
                    NumTouchesMovedOutside_Trial++;
                }
            }
            AudioPlayed = true;
        });
        Feedback.AddUpdateMethod(() =>
        {
            if(GiveReleaseReward)
            {
                if (GraySquareTimer < CurrentTrial.GreyOnReleaseDuration)
                    GraySquareTimer += Time.deltaTime;
                else
                    GraySquareTimer = -1;

                if(SyncBoxController != null)
                {
                    if (RewardTimer < CurrentTrial.ReleaseToRewardDelay)
                        RewardTimer += Time.deltaTime;
                    else
                    {
                        SyncBoxController.SendRewardPulses(CurrentTrial.NumReleasePulses, CurrentTrial.PulseSize);
                        RewardGiven = true;
                        ReleaseRewards_Trial += CurrentTrial.NumReleasePulses;
                    }
                }
            }
            if(GiveTouchReward && SyncBoxController != null)
            {
                if(RewardTimer < CurrentTrial.TouchToRewardDelay)
                    RewardTimer += Time.deltaTime;
                else
                {
                    SyncBoxController.SendRewardPulses(CurrentTrial.NumTouchPulses, CurrentTrial.PulseSize);
                    RewardGiven = true;
                    TouchRewards_Trial += CurrentTrial.NumTouchPulses;
                }
            }
        });
        Feedback.SpecifyTermination(() => GiveReleaseReward && SyncBoxController == null && GraySquareTimer == -1, ITI); //to handle when syncbox is null and releaseReward so gotta wait for graytimer!
        Feedback.SpecifyTermination(() => GiveReleaseReward && RewardGiven && GraySquareTimer == -1, ITI); //when they receive release reward and graytimer done!
        Feedback.SpecifyTermination(() => GiveTouchReward && SyncBoxController == null, ITI); //earned touch reward but no syncbox. don't need to wait for gray since touch is immediate.
        Feedback.SpecifyTermination(() => GiveTouchReward && RewardGiven, ITI); //when they receive touch reward!
        Feedback.SpecifyTermination(() => (HeldTooShort || HeldTooLong || MovedOutside) && AudioPlayed && !Grating, ITI); //If they got wrong
        Feedback.SpecifyTermination(() => (TimeRanOut) && AudioPlayed, ITI); //state ends after receiving neg FB (if didn't get correct).
        Feedback.AddTimer(() => CurrentTrial.FbDuration, ITI);

        //ITI state ---------------------------------------------------------------------------------------------------------------------------------
        ITI.AddInitializationMethod(() =>
        {
            SquareGO.SetActive(false);

            ConfigVariablesLoaded = false;

            if (GiveReleaseReward || GiveTouchReward)
                TrialsCorrect_Block++;

            if ((CurrentTrial.RewardTouch && GiveTouchReward) || (CurrentTrial.RewardRelease && GiveReleaseReward))
                TrialCompletionList.Insert(0, 1);
            else
                TrialCompletionList.Insert(0, 0);
        });
        ITI.AddUpdateMethod(() =>
        {
            if(InputBroker.GetMouseButtonUp(0))
                ItiTouches_Trial++;
        });
        ITI.AddTimer(() => CurrentTrial.ItiDuration, FinishTrial, () =>
        {
            AddTrialTouchNumsToBlock();
            TrialsCompleted_Block++;
            TrialComplete = true;

            CheckIfBlockShouldEnd();
        });

        LogTrialData();
        LogFrameData();
    }


    //HELPER FUNCTIONS ------------------------------------------------------------------------------------------

    void AddTrialTouchNumsToBlock()
    {
        BlueSquareTouches_Block += BlueSquareTouches_Trial;
        WhiteSquareTouches_Block += WhiteSquareTouches_Trial;
        BackdropTouches_Block += BackdropTouches_Trial;
        NumItiTouches_Block += ItiTouches_Trial;
        NumTouchRewards_Block += TouchRewards_Trial;
        NumReleaseRewards_Block += ReleaseRewards_Trial;
        NumTouchesMovedOutside_Block += NumTouchesMovedOutside_Trial;
    }

    void SetTrialSummaryString()
    {
        TrialSummaryString = "Trial #" + (TrialCount_InBlock + 1) +
                              "\nRewarding: " + (CurrentTrial.RewardTouch ? "Touch" : "Release") +
                              "\nRandomPosition: " + ((CurrentTrial.RandomSquarePosition ? "True" : "False")) +
                              "\nRandomSize: " + ((CurrentTrial.RandomSquareSize ? "True" : "False"));
    }

    void LoadGratingMaterials()
    {
        HeldTooShortTexture = Resources.Load<Texture2D>("VerticalStripes");
        HeldTooLongTexture = Resources.Load<Texture2D>("HorizontalStripes");
        BackdropStripeTexture = Resources.Load<Texture2D>("BackgroundStripes");
    }

    protected override bool CheckBlockEnd()
    {
        return PerfThresholdMet;
    }

    void CheckIfBlockShouldEnd()
    {
        if(TrialsCompleted_Block >= CurrentTrial.PerfWindowEndTrials)
        {
            int sum = 0;
            for(int i = 0; i < CurrentTrial.PerfWindowEndTrials; i++)
            {
                sum += TrialCompletionList[i];
            }
            float performancePerc = sum / CurrentTrial.PerfWindowEndTrials;
            if(performancePerc >= CurrentTrial.PerfThresholdEndTrials)
                PerfThresholdMet = true; //Will trigger CheckBlockEnd function to terminate block
        }
    }

    void SetBlockDefaultValues()
    {
        BlockDefaultSquareSize = CurrentTrial.SquareSize;
        BlockDefaultPositionX = CurrentTrial.PositionX;
        BlockDefaultPositionY = CurrentTrial.PositionY;
        BlockDefaultMinTouchDuration = CurrentTrial.MinTouchDuration;
        BlockDefaultMaxTouchDuration = CurrentTrial.MaxTouchDuration;
        BlockDefaultWhiteSquareDuration = CurrentTrial.WhiteSquareDuration;
        BlockDefaultBlueSquareDuration = CurrentTrial.BlueSquareDuration;
    }

    bool ConfigValuesChanged()
    {
        if (BlockDefaultSquareSize != ConfigUiVariables.get<ConfigNumber>("squareSize").value
            || BlockDefaultPositionX != ConfigUiVariables.get<ConfigNumber>("positionX").value
            || BlockDefaultPositionY != ConfigUiVariables.get<ConfigNumber>("positionY").value
            || BlockDefaultMinTouchDuration != ConfigUiVariables.get<ConfigNumber>("minTouchDuration").value
            || BlockDefaultMaxTouchDuration != ConfigUiVariables.get<ConfigNumber>("maxTouchDuration").value
            || BlockDefaultWhiteSquareDuration != ConfigUiVariables.get<ConfigNumber>("whiteSquareDuration").value
            || BlockDefaultBlueSquareDuration != ConfigUiVariables.get<ConfigNumber>("blueSquareDuration").value)
        {
            return true;
        }
        else
            return false;
    }

    void UpdateSquare()
    {
        SquareGO.transform.localScale = new Vector3(CurrentTrial.SquareSize, CurrentTrial.SquareSize, .001f);
        SquareGO.transform.localPosition = new Vector3(CurrentTrial.PositionX, CurrentTrial.PositionY, 90);
    }

    void SetTrialValuesToConfigValues()
    {
        CurrentTrial.MinTouchDuration = ConfigUiVariables.get<ConfigNumber>("minTouchDuration").value;
        CurrentTrial.MaxTouchDuration = ConfigUiVariables.get<ConfigNumber>("maxTouchDuration").value;
        CurrentTrial.SquareSize = ConfigUiVariables.get<ConfigNumber>("squareSize").value;
        CurrentTrial.PositionX = ConfigUiVariables.get<ConfigNumber>("positionX").value;
        CurrentTrial.PositionY = ConfigUiVariables.get<ConfigNumber>("positionY").value;
        CurrentTrial.WhiteSquareDuration = ConfigUiVariables.get<ConfigNumber>("whiteSquareDuration").value;
        CurrentTrial.BlueSquareDuration = ConfigUiVariables.get<ConfigNumber>("blueSquareDuration").value;
    }

    void ActivateSquareAndBackdrop()
    {
        BackdropGO.SetActive(true);
        SquareGO.SetActive(true);
    }

    void CreateBackdrop()
    {
        BackdropGO = Instantiate(BackdropPrefab, new Vector3(0, 0, 95), Quaternion.identity);
        BackdropGO.transform.localScale = new Vector3(275, 150, .5f);
        BackdropGO.AddComponent<BoxCollider>();
        BackdropGO.name = "BackdropGO";

        BackdropRenderer = BackdropGO.GetComponent<Renderer>();
        BackdropMaterial = BackdropRenderer.material;
        BackdropTexture = BackdropRenderer.material.mainTexture;
        InitialBackdropColor = BackdropMaterial.color;
    }

    void CreateSquare()
    {
        SquareGO = Instantiate(SquarePrefab, new Vector3(0, 1, 90), Quaternion.identity);
        SquareGO.AddComponent<BoxCollider>();
        SquareGO.name = "SquareGO";

        SquareRenderer = SquareGO.GetComponent<Renderer>();
        SquareMaterial = SquareRenderer.material;
        SquareTexture = SquareRenderer.material.mainTexture;
        InitialSquareColor = SquareMaterial.color;
    }

    void SetSquareSizeAndPosition()
    {
        if(CurrentTrial.RandomSquareSize)
        {
            float randomSize = Random.Range(CurrentTrial.SquareSizeMin, CurrentTrial.SquareSizeMax);
            SquareGO.transform.localScale = new Vector3(randomSize, randomSize, .001f);
        }
        else
            SquareGO.transform.localScale = new Vector3(CurrentTrial.SquareSize, CurrentTrial.SquareSize, .001f);
        
        if(CurrentTrial.RandomSquarePosition)
        {
            float x = Random.Range(CurrentTrial.PositionX_Min, CurrentTrial.PositionX_Max);
            float y = Random.Range(CurrentTrial.PositionY_Min, CurrentTrial.PositionY_Max);
            SquareGO.transform.localPosition = new Vector3(x, y, 90);
        }
        else
            SquareGO.transform.localPosition = new Vector3(CurrentTrial.PositionX, CurrentTrial.PositionY, 90);
        
    }

    void SetConfigValuesToBlockDefaults()
    {
        ConfigUiVariables.get<ConfigNumber>("minTouchDuration").SetValue(BlockDefaultMinTouchDuration);
        ConfigUiVariables.get<ConfigNumber>("maxTouchDuration").SetValue(BlockDefaultMaxTouchDuration);
        ConfigUiVariables.get<ConfigNumber>("squareSize").SetValue(BlockDefaultSquareSize);
        ConfigUiVariables.get<ConfigNumber>("positionX").SetValue(BlockDefaultPositionX);
        ConfigUiVariables.get<ConfigNumber>("positionY").SetValue(BlockDefaultPositionY);
        ConfigUiVariables.get<ConfigNumber>("whiteSquareDuration").SetValue(BlockDefaultWhiteSquareDuration);
        ConfigUiVariables.get<ConfigNumber>("blueSquareDuration").SetValue(BlockDefaultBlueSquareDuration);
    }

    void CreateColors()
    {
        DarkBlueBackgroundColor = new Color32(2, 3, 39, 255);
        LightRedColor = new Color32(224, 78, 92, 255);
        LightBlueColor = new Color32(0, 150, 255, 255);
    }

    IEnumerator GratedSquareFlash(Texture2D newTexture)
    {
        Grating = true;
        SquareMaterial.color = LightRedColor;
        SquareRenderer.material.mainTexture = newTexture;
        yield return new WaitForSeconds(CurrentTrial.GratingSquareDuration);
        SquareRenderer.material.mainTexture = SquareTexture;
        Grating = false;
    }

    IEnumerator GratedBackdropFlash(Texture2D newTexture)
    {
        Grating = true;
        Color32 currentSquareColor = SquareMaterial.color; 
        SquareMaterial.color = new Color32(255, 153, 153, 255); 
        BackdropMaterial.color = LightRedColor;
        BackdropRenderer.material.mainTexture = newTexture;
        AudioFBController.Play("Negative");
        yield return new WaitForSeconds(1f);
        BackdropRenderer.material.mainTexture = BackdropTexture;
        BackdropMaterial.color = InitialBackdropColor;
        SquareMaterial.color = currentSquareColor;
        Grating = false;
    }

    void ResetGlobalTrialVariables()
    {
        HeldTooLong = false;
        HeldTooShort = false;
        RewardGiven = false;
        GiveReleaseReward = false;
        GiveTouchReward = false;
        TimeRanOut = false;
        BackdropTouches_Trial = 0;
        BlueSquareTouches_Trial = 0;
        WhiteSquareTouches_Trial = 0;
        NumTouchesMovedOutside_Trial = 0;
        ItiTouches_Trial = 0;
        TouchStartTime = 0;
        HeldDuration = 0;
    }

    void LogTrialData()
    {
        TrialData.AddDatum("SquareSize", () => CurrentTrial.SquareSize);
        TrialData.AddDatum("SquarePosX", () => CurrentTrial.PositionX);
        TrialData.AddDatum("SquarePosY", () => CurrentTrial.PositionY);
        TrialData.AddDatum("MinTouchDuration", () => CurrentTrial.MinTouchDuration);
        TrialData.AddDatum("MaxTouchDuration", () => CurrentTrial.MaxTouchDuration);
        TrialData.AddDatum("RewardTouch", () => CurrentTrial.RewardTouch);
        TrialData.AddDatum("RewardRelease", () => CurrentTrial.RewardRelease);
        TrialData.AddDatum("DifficultyLevel", () => CurrentTrial.BlockName);
        TrialData.AddDatum("BlueSquareTouches_Trial", () => BlueSquareTouches_Trial);
        TrialData.AddDatum("WhiteSquareTouches_Trial", () => WhiteSquareTouches_Trial);
        TrialData.AddDatum("BackdropTouches_Trial", () => BackdropTouches_Trial);
        TrialData.AddDatum("MovedOutsideSquare_Trial", () => NumTouchesMovedOutside_Trial);
        TrialData.AddDatum("ItiTouches_Trial", () => ItiTouches_Trial);
        TrialData.AddDatum("ReactionTime", () => ReactionTime);
        TrialData.AddDatum("TouchStartTime", () => TouchStartTime);
        TrialData.AddDatum("HeldDuration", () => HeldDuration);
    }

    void LogFrameData()
    {
        FrameData.AddDatum("TouchPosition", () => InputBroker.mousePosition);
        FrameData.AddDatum("SquareGO", () => SquareGO.activeSelf);
    }

}
