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

    public float FeedbackStartTime;
    public float TrialStartTime;
    public float TouchStartTime;
    public float TouchReleaseTime;
    public float HeldDuration;

    public bool TrialComplete;

    public bool GiveTouchReward;
    public bool GiveHoldReward;
    public bool ClickedSquare;
    public bool AutoEndTrial;
    public bool ConfigVariablesLoaded;

    public List<int> TrialCompletionList;
    public int NumTrialsCompletedBlock;
    public int NumTrialsCorrectBlock;
    public float PerformancePercentage
    {
        get
        {
            if (NumTrialsCompletedBlock > 0)
                return NumTrialsCorrectBlock / NumTrialsCompletedBlock;
            else
                return 0;
        }
    }

    //Data variables:
    public int NumNonSquareTouches;
    public int NumTouchesBlueSquare;
    public int NumTouchesWhiteSquare;

    //Set in Editor
    public Material SquareMaterial;
    public Material BackdropMaterial;

    //misc
    //public Camera THR_Cam;
    public bool ClickReleased;
    public bool ColorsSet;

    public Color32 LightRedColor;
    public Color32 GreyGreenColor;
    public Color32 GreenColor;
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

    public bool IsLevelOne;
    public bool PerfThresholdMet;

    public int NumTouchRewards;
    public int NumReleaseRewards;

    public float ReactionTime
    {
        get
        {
            return TouchStartTime - TrialStartTime;
        }
    }

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
            else
                SquareMaterial.color = Color.white;

            SetTrialSummaryString();
        });
        SetupTrial.SpecifyTermination(() => true, InitTrial);

        //INIT TRIAL state --------------------------------------------------------------------------------------------------------------------------
        InitTrial.AddInitializationMethod(() =>
        {
            if(HeldTooLongTexture == null || HeldTooShortTexture == null || BackdropTexture == null)
                LoadGratingMaterials();

            ResetGlobalTrialVariables();

            if (TrialCount_InBlock == 0)
            {
                SetSquareSizeAndPosition();
                SetConfigValuesToBlockDefaults();
            }
            else
            {
                if (ConfigValuesChanged())
                {
                    SetTrialValuesToConfigValues();
                    UpdateSquare();
                }
                else
                    SetSquareSizeAndPosition();
            }
        });
        InitTrial.SpecifyTermination(() => true, WhiteSquare);

        SelectionHandler<THR_StimDef> mouseHandler = new SelectionHandler<THR_StimDef>();
        MouseTracker.AddSelectionHandler(mouseHandler, WhiteSquare);

        int whiteTouches = 0;

        //WHITE SQUARE state ------------------------------------------------------------------------------------------------------------------------
        WhiteSquare.AddInitializationMethod(() =>
        {
            if(CurrentTrial.BlockName == "Level1")
            {
                Cursor.visible = false;
                ActivateSquareAndBackdrop();
            }
        });

        WhiteSquare.AddUpdateMethod(() =>
        {
            if(mouseHandler.SelectionMatches(SquareGO))
            {
                if (!AudioFBController.IsPlaying())
                    AudioFBController.Play("Negative");
                if (whiteTouches == 0)
                {
                    NumTouchesWhiteSquare++;
                    whiteTouches++;
                }
            }
        });
        WhiteSquare.SpecifyTermination(() => CurrentTrial.BlockName != "Level1", BlueSquare);
        WhiteSquare.AddTimer(() => CurrentTrial.WhiteSquareDuration, BlueSquare, () => StartCoroutine(WhiteToBlueStatePause()));

        NumTouchesWhiteSquare += whiteTouches;

        MouseTracker.AddSelectionHandler(mouseHandler, BlueSquare);

        int blueTouches = 0;
        int nonSquareTouches = 0;


        //BLUE SQUARE state -------------------------------------------------------------------------------------------------------------------------
        BlueSquare.AddInitializationMethod(() =>
        {
            SquareMaterial.color = Color.blue;
            if (!SquareGO.activeSelf)
                ActivateSquareAndBackdrop();
            Cursor.visible = true;
            TrialStartTime = Time.time;
        });
        BlueSquare.AddUpdateMethod(() =>
        {
            if(InputBroker.GetMouseButton(0))
            {
                //If pointer is over a UI Element (EXPERIMENTER DISPLAY)
                if (EventSystem.current.IsPointerOverGameObject())
                    return;

                if (MouseTracker.CurrentTargetGameObject == SquareGO)
                {
                    TouchStartTime = Time.time;
                    SquareMaterial.color = GreenColor;
                    ClickedSquare = true;
                    if (blueTouches == 0)
                    {
                        NumTouchesBlueSquare++;
                        blueTouches++;
                    }
                    if (CurrentTrial.RewardTouch)
                        GiveTouchReward = true;
                }
                else if (MouseTracker.CurrentTargetGameObject == BackdropGO)
                {
                    if (!AudioFBController.IsPlaying())
                        AudioFBController.Play("Negative");

                    StartCoroutine(StripeBackgroundFlash(BackdropStripeTexture));

                    if (nonSquareTouches == 0)
                    {
                        NumNonSquareTouches++;
                        nonSquareTouches++;
                    }
                }
            }

            if (InputBroker.GetMouseButtonUp(0))
            {
                if(ClickedSquare)
                {
                    Cursor.visible = false;
                    SquareMaterial.color = GreyGreenColor;
                    HeldDuration = mouseHandler.currentTargetDuration;
                    TouchReleaseTime = HeldDuration - TouchStartTime;

                    if(HeldDuration >= CurrentTrial.MinTouchDuration && HeldDuration <= CurrentTrial.MaxTouchDuration)
                    {
                        if(CurrentTrial.RewardRelease)
                            GiveHoldReward = true;
                    }
                    else if(HeldDuration < CurrentTrial.MinTouchDuration)
                        StartCoroutine(GratingSquareFlash(HeldTooShortTexture));
                    else
                        StartCoroutine(GratingSquareFlash(HeldTooLongTexture));

                    ClickReleased = true;
                }
            }
            if (Time.time - TrialStartTime > CurrentTrial.TimeToAutoEndTrialSec)
                AutoEndTrial = true;
        });
        BlueSquare.AddTimer(() => CurrentTrial.BlueSquareDuration, Feedback);
        BlueSquare.SpecifyTermination(() => ClickReleased, Feedback);
        BlueSquare.SpecifyTermination(() => AutoEndTrial, ITI);

        NumTouchesBlueSquare += blueTouches;
        NumNonSquareTouches += nonSquareTouches;

        //FEEDBACK state ------------------------------------------------------------------------------------------------------------------------
        Feedback.AddInitializationMethod(() =>
        {
            FeedbackStartTime = Time.time;

            if ((GiveTouchReward) || (GiveHoldReward))
            {
                AudioFBController.Play("Positive");
                if (SyncBoxController != null)
                {
                    if (GiveTouchReward)
                    {
                        SyncBoxController.SendRewardPulses(CurrentTrial.NumTouchPulses, CurrentTrial.PulseSize);
                        NumTouchRewards += CurrentTrial.NumTouchPulses;
                    }
                    if (GiveHoldReward)
                    {
                        SyncBoxController.SendRewardPulses(CurrentTrial.NumReleasePulses, CurrentTrial.PulseSize);
                        NumReleaseRewards += CurrentTrial.NumReleasePulses;
                    }
                }
            }
            else
            {
                if(!AudioFBController.IsPlaying())
                    AudioFBController.Play("Negative");
            }
        });
        Feedback.AddTimer(() => CurrentTrial.FbDuration, ITI);

        //ITI state -----------------------------------------------------------------------------------------------------------------------------
        ITI.AddInitializationMethod(() =>
        {
            SquareGO.SetActive(false);
            SquareMaterial.color = InitialSquareColor;

            ConfigVariablesLoaded = false;

            if (GiveHoldReward || GiveTouchReward)
                NumTrialsCorrectBlock++;

            if ((CurrentTrial.RewardTouch && GiveTouchReward) || (CurrentTrial.RewardRelease && GiveHoldReward))
                TrialCompletionList.Insert(0, 1);
            else
                TrialCompletionList.Insert(0, 0);

            NumTrialsCompletedBlock++;
            TrialComplete = true;

            CheckIfBlockShouldEnd();
        });
        ITI.AddTimer(() => CurrentTrial.ItiDuration, FinishTrial);

        LogTrialData();
        LogFrameData();
    }


    //HELPER FUNCTIONS ------------------------------------------------------------------------------------------

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
        if(NumTrialsCompletedBlock >= CurrentTrial.PerfWindowEndTrials)
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

    IEnumerator WhiteToBlueStatePause()
    {
        //Using this func to handle the player clicking while its changing from WhiteSquare state to BlueSquare state
        yield return new WaitForSeconds(1f);
    }

    void CreateColors()
    {
        DarkBlueBackgroundColor = new Color32(2, 3, 39, 255);
        LightRedColor = new Color32(224, 78, 92, 255);
        GreenColor = new Color32(45, 175, 34, 255);
        GreyGreenColor = new Color32(90, 140, 100, 255);
    }

    IEnumerator GratingSquareFlash(Texture2D newTexture)
    {
        Cursor.visible = false;
        SquareMaterial.color = LightRedColor;
        SquareRenderer.material.mainTexture = newTexture;
        yield return new WaitForSeconds(1f);
        SquareMaterial.mainTexture = SquareTexture;
        SquareMaterial.color = Color.blue;
        Cursor.visible = true;
    }

    IEnumerator StripeBackgroundFlash(Texture2D newTexture)
    {
        Cursor.visible = false;
        SquareMaterial.color = new Color32(255, 153, 153, 255);
        BackdropMaterial.color = LightRedColor;
        BackdropRenderer.material.mainTexture = newTexture;
        yield return new WaitForSeconds(1f);
        BackdropMaterial.mainTexture = BackdropTexture;
        BackdropMaterial.color = InitialBackdropColor;
        SquareMaterial.color = Color.blue;
        Cursor.visible = true;
    }

    void ResetGlobalTrialVariables()
    {
        ClickedSquare = false;
        ClickReleased = false;
        GiveHoldReward = false;
        GiveTouchReward = false;
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
        TrialData.AddDatum("ReactionTime", () => ReactionTime);
        TrialData.AddDatum("DifficultyLevel", () => CurrentTrial.BlockName);
    }

    void LogFrameData()
    {
        FrameData.AddDatum("TouchPosition", () => InputBroker.mousePosition);
        FrameData.AddDatum("SquareGO", () => SquareGO.activeSelf);
    }

    //private string GetContextNestedFilePath(string contextName)
    //{
    //    //Recursive search the sub folders of the MaterialFilePath to get Context File Path
    //    string backupContextName = "LinearDark";
    //    string contextPath = "";

    //    string[] filePaths = Directory.GetFiles(MaterialFilePath, $"{contextName}*", SearchOption.AllDirectories);

    //    if (filePaths.Length == 1)
    //        contextPath = filePaths[0];
    //    else
    //    {
    //        Debug.Log($"Context File Path Not Found. Defaulting to {backupContextName}.");
    //        contextPath = Directory.GetFiles(MaterialFilePath, backupContextName, SearchOption.AllDirectories)[0]; //Use Default LinearDark if can't find file.
    //    }

    //    return contextPath;
    //}
}
