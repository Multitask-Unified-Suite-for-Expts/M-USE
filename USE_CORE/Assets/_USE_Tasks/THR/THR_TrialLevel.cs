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
    public int NonSquareTouches_Trial;
    public int BlueSquareTouches_Trial;
    public int WhiteSquareTouches_Trial;
    public int ItiTouches_Trial;
    public int TouchRewards_Trial;
    public int ReleaseRewards_Trial;

    public int NonSquareTouches_Block; 
    public int BlueSquareTouches_Block; 
    public int WhiteSquareTouches_Block;
    public int NumTouchRewards_Block;
    public int NumReleaseRewards_Block;
    public int NumItiTouches_Block;
    public int NumReleasedEarly_Block;
    public int NumReleasedLate_Block;

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

    public float TouchRewardEarnedTime;
    public float TouchRewardGivenTime;
    public float ReleaseRewardEarnedTime;
    public float ReleaseRewardGivenTime;

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
            if (TrialCount_InBlock == 0)
                TrialCompletionList = new List<int>();

            Cursor.visible = false;

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
            Cursor.visible = true;
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
            {
                WhiteTimeoutTime = 0;
                Cursor.visible = true;
            }
            
            if(MouseTracker.CurrentTargetGameObject == SquareGO)
            {
                ClickedWhiteSquare = true;
                if(WhiteTimeoutTime == 0)
                {
                    WhiteTimeoutTime = Time.time;
                    WhiteStartTime = Time.time; //reset original WhiteStartTime so that normal duration resets.
                    if(!AudioFBController.IsPlaying()) //will keep playing every timeout duration period if they still holding. Which is good to teach them not to!
                        AudioFBController.Play("Negative");
                    Cursor.visible = false;
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
            SquareMaterial.color = LightBlueColor;
            if (!SquareGO.activeSelf)
                ActivateSquareAndBackdrop();
            BlueStartTime = Time.time;
            BlueSquareTouched = false;
            BlueSquareReleased = false;
            BackdropTouchTime = 0;
            BackdropTouches = 0;
            Cursor.visible = true;
        });
        BlueSquare.AddUpdateMethod(() =>
        {
            //if they already clicked the backdrop once, and the timeoutduration has lapsed, reset the variables
            if(BackdropTouchTime != 0 && (Time.time - BackdropTouchTime) > CurrentTrial.TimeoutDuration)
            {
                BackdropTouches = 0;
                BackdropTouchTime = 0;
            }

            if(MouseTracker.CurrentTargetGameObject == SquareGO && !Grating)
            {
                if(!BlueSquareTouched)
                {
                    TouchStartTime = Time.time;
                    BlueSquareTouched = true;
                }
                HeldDuration = mouseHandler.currentTargetDuration;
                if(HeldDuration >= .045f)
                    SquareMaterial.color = Color.blue;
                if(CurrentTrial.RewardTouch)
                {
                    Cursor.visible = false;
                    BlueSquareTouches_Trial++;
                    GiveTouchReward = true;
                    TouchRewardEarnedTime = Time.time;
                }
            }
            if(MouseTracker.CurrentTargetGameObject == BackdropGO && !Grating)
            {
                ClickedOutsideSquare = true;
                if(BackdropTouches == 0)
                {
                    BackdropTouchTime = Time.time;
                    BlueStartTime += CurrentTrial.TimeoutDuration; //add extra second so it doesn't go straight to white after grating
                    StartCoroutine(GratedBackdropFlash(BackdropStripeTexture));
                    BackdropTouches++;
                }
            }

            if(InputBroker.GetMouseButtonUp(0) && !Grating)
            {
                if(BlueSquareTouched && !BlueSquareReleased)
                {
                    TouchReleaseTime = Time.time;
                    Cursor.visible = false;
                    BlueSquareTouches_Trial++;
                    HeldDuration = mouseHandler.currentTargetDuration;
                    if(CurrentTrial.RewardRelease)
                    {
                        if(HeldDuration >= CurrentTrial.MinTouchDuration && HeldDuration <= CurrentTrial.MaxTouchDuration)
                        {
                            SquareMaterial.color = Color.gray;
                            GiveReleaseReward = true;
                            ReleaseRewardEarnedTime = Time.time;
                        }
                        else if(HeldDuration < CurrentTrial.MinTouchDuration)
                        {
                            StartCoroutine(GratedSquareFlash(HeldTooShortTexture));
                            NumReleasedEarly_Block++;
                        }
                        else
                        {
                            StartCoroutine(GratedSquareFlash(HeldTooLongTexture));
                            NumReleasedLate_Block++;
                        }
                    }
                    else
                        SquareMaterial.color = Color.gray;

                    BlueSquareReleased = true;
                }
                if(ClickedOutsideSquare)
                {
                    NonSquareTouches_Trial++;
                    ClickedOutsideSquare = false;
                }
            }
            if (Time.time - TrialStartTime > CurrentTrial.TimeToAutoEndTrialSec)
                TimeRanOut = true;
        });
        //Go back to white square if bluesquare time lapses (and they aren't already holding down)
        BlueSquare.SpecifyTermination(() => Time.time - BlueStartTime > CurrentTrial.BlueSquareDuration && !InputBroker.GetMouseButton(0) && !BlueSquareReleased && !Grating, WhiteSquare);
        BlueSquare.SpecifyTermination(() => BlueSquareReleased || TimeRanOut, Feedback); //If they click the square and release, OR run out of time, go to feedback state. 
        BlueSquare.SpecifyTermination(() => CurrentTrial.RewardTouch && GiveTouchReward, Feedback); //If rewarding touch, Go to feedback as soon as they click!!!

        //FEEDBACK state ----------------------------------------------------------------------------------------------------------------------------
        Feedback.AddInitializationMethod(() =>
        {
            AudioPlayed = false;

            if((GiveTouchReward) || (GiveReleaseReward))
            {
                AudioFBController.Play("Positive");
                AudioPlayed = true;
                float waitTime = 0;

                if (GiveTouchReward && SyncBoxController != null)
                {
                    waitTime = CurrentTrial.TouchToRewardDelay - (Time.time - TouchRewardEarnedTime);
                    StartCoroutine(GiveReward(Time.time, waitTime, CurrentTrial.NumTouchPulses));
                    TouchRewards_Trial += CurrentTrial.NumTouchPulses;
                }
                if (GiveReleaseReward && SyncBoxController != null)
                {
                    waitTime = CurrentTrial.ReleaseToRewardDelay - (Time.time - ReleaseRewardEarnedTime);
                    StartCoroutine(GiveReward(Time.time, waitTime, CurrentTrial.NumReleasePulses));
                    ReleaseRewards_Trial += CurrentTrial.NumReleasePulses;
                }
            }
            if(TimeRanOut)
            {
                AudioFBController.Play("Negative");
                AudioPlayed = true;
            }
        });
        Feedback.SpecifyTermination(() => CurrentTrial.RewardTouch && AudioPlayed && !AudioFBController.IsPlaying(), ITI);
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

            AddTrialTouchNumsToBlock();
            TrialsCompleted_Block++;
            TrialComplete = true;

            CheckIfBlockShouldEnd();
        });
        ITI.AddTimer(() => CurrentTrial.ItiDuration, FinishTrial, () => Cursor.visible = true);

        LogTrialData();
        LogFrameData();
    }


    //HELPER FUNCTIONS ------------------------------------------------------------------------------------------

    IEnumerator GiveReward(float codeStartTime, float waitTime, int numPulses)
    {
        if (waitTime < 0)
            waitTime = 0;
        else
            waitTime -= Time.time - codeStartTime; //fine tuning by removing amout of time its taken for code to execute

        yield return new WaitForSeconds(waitTime);
        SyncBoxController.SendRewardPulses(numPulses, CurrentTrial.PulseSize);
    }

    void AddTrialTouchNumsToBlock()
    {
        BlueSquareTouches_Block += BlueSquareTouches_Trial;
        WhiteSquareTouches_Block += WhiteSquareTouches_Trial;
        NonSquareTouches_Block += NonSquareTouches_Trial;
        NumItiTouches_Block += ItiTouches_Trial;
        NumTouchRewards_Block += TouchRewards_Trial;
        NumReleaseRewards_Block += ReleaseRewards_Trial;
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
        Cursor.visible = false;
        SquareMaterial.color = LightRedColor;
        SquareRenderer.material.mainTexture = newTexture;
        AudioFBController.Play("Negative");
        yield return new WaitForSeconds(1f);
        SquareRenderer.material.mainTexture = SquareTexture;
        Cursor.visible = true;
    }

    IEnumerator GratedBackdropFlash(Texture2D newTexture)
    {
        Grating = true;
        Cursor.visible = false;
        Color32 currentSquareColor = SquareMaterial.color; //color before change
        SquareMaterial.color = new Color32(255, 153, 153, 255); //color to change to 
        BackdropMaterial.color = LightRedColor;
        BackdropRenderer.material.mainTexture = newTexture;
        AudioFBController.Play("Negative");
        yield return new WaitForSeconds(1f);
        BackdropRenderer.material.mainTexture = BackdropTexture;
        BackdropMaterial.color = InitialBackdropColor;
        SquareMaterial.color = currentSquareColor;
        Cursor.visible = true;
        Grating = false;
    }

    void ResetGlobalTrialVariables()
    {
        GiveReleaseReward = false;
        GiveTouchReward = false;
        TimeRanOut = false;
        NonSquareTouches_Trial = 0;
        BlueSquareTouches_Trial = 0;
        WhiteSquareTouches_Trial = 0;
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
        TrialData.AddDatum("NonSquareTouches_Trial", () => NonSquareTouches_Trial);
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
