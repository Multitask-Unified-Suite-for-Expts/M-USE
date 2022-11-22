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

public class THR_TrialLevel : ControlLevel_Trial_Template
{
    public THR_TrialDef CurrentTrial => GetCurrentTrialDef<THR_TrialDef>();

    public string MaterialFilePath;
    public string ContextPath;

    public GameObject BackdropPrefab;
    public GameObject BackdropGO;
    public Renderer BackdropRenderer;
    public GameObject SquarePrefab;
    public GameObject SquareGO;
    public Renderer SquareRenderer;
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
    public Ray mouseRay;
    public Camera THR_Cam;
    public bool ClickReleased;
    public bool ColorsSet;
    public string RedWhiteStripeFilePath;

    public Color32 DarkBlueBackgroundColor;
    public Color32 LightRedColor;
    public Color32 LightBlueColor;
    public Color32 InitialSquareColor;
    public Color32 InitialBackdropColor;

    public bool ConfigValuesChanged;
    public bool EndBlock;

    public float BlockDefaultSquareSize;
    public float BlockDefaultPositionX;
    public float BlockDefaultPositionY;
    public float BlockDefaultMinTouchDuration;
    public float BlockDefaultMaxTouchDuration;
    public float BlockDefaultWhiteSquareDuration;
    public float BlockDefaultBlueSquareDuration;

    public bool IsLevelOne;

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
        });
        SetupTrial.SpecifyTermination(() => true, InitTrial);

        //INIT TRIAL state --------------------------------------------------------------------------------------------------------------------------
        InitTrial.AddInitializationMethod(() =>
        {
            ResetGlobalTrialVariables();

            if (TrialCount_InBlock == 0)
                SetSquareSizeAndPosition();

            if (TrialCount_InBlock > 0)
            {
                ConfigValuesChanged = DidConfigValuesChange();
                if (ConfigValuesChanged)
                {
                    LoadConfigVariables();
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
            if (InputBroker.GetMouseButton(0))
            {
                if (MouseTracker.CurrentTargetGameObject == SquareGO)
                {
                    TouchStartTime = Time.time;
                    SquareMaterial.color = Color.green;
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
                    //REMOVED SO THAT EXPERIMENTER CLICKS DONT CHANGE GAME:
                    //if(!AudioFBController.IsPlaying())
                    //    AudioFBController.Play("Negative");
                    //StartCoroutine(BackgroundColorFlash(LightRedColor));
                    if (nonSquareTouches == 0)
                    {
                        NumNonSquareTouches++;
                        nonSquareTouches++;
                    }
                }
            }

            if(InputBroker.GetMouseButtonUp(0))
            {
                if(ClickedSquare)
                {
                    Cursor.visible = false;
                    SquareMaterial.color = Color.grey;
                    HeldDuration = mouseHandler.currentTargetDuration;
                    TouchReleaseTime = HeldDuration - TouchStartTime;

                    if (HeldDuration > CurrentTrial.MinTouchDuration && HeldDuration < CurrentTrial.MaxTouchDuration)
                    {
                        if (CurrentTrial.RewardRelease)
                            GiveHoldReward = true;
                    }
                    else
                        StartCoroutine(SquareColorFlash(LightRedColor));
                    
                    ClickReleased = true;
                }
            }
            if (Time.time - TrialStartTime > CurrentTrial.TimeToAutoEndTrialSec)
                AutoEndTrial = true;
        });
        BlueSquare.AddTimer(() => CurrentTrial.BlueSquareDuration, Feedback);
        BlueSquare.SpecifyTermination(() => ClickReleased, Feedback);
        BlueSquare.SpecifyTermination(() => AutoEndTrial, ITI); //go to feedback if time elapsed.

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
                        SyncBoxController.SendRewardPulses(CurrentTrial.NumTouchPulses, CurrentTrial.PulseSize);
                    if (GiveHoldReward)
                        SyncBoxController.SendRewardPulses(CurrentTrial.NumReleasePulses, CurrentTrial.PulseSize); 
                }
            }
            else
                AudioFBController.Play("Negative");          
        });
        Feedback.SpecifyTermination(() => !AudioFBController.IsPlaying() && Time.time - FeedbackStartTime > CurrentTrial.FbDuration, ITI);

        //ITI state -----------------------------------------------------------------------------------------------------------------------------
        ITI.AddInitializationMethod(() =>
        {
            SquareGO.SetActive(false);
            SquareMaterial.color = Color.white;

            ConfigVariablesLoaded = false;

            if (GiveHoldReward || GiveTouchReward)
                NumTrialsCorrectBlock++;

            if ((CurrentTrial.RewardTouch && GiveTouchReward) || (CurrentTrial.RewardRelease && GiveHoldReward))
                TrialCompletionList.Add(1);
            else
                TrialCompletionList.Add(0);

            //CheckIfBlockShouldEnd();
        });
        ITI.AddTimer(() => CurrentTrial.ItiDuration, FinishTrial, () =>
        {
            NumTrialsCompletedBlock++;
            TrialComplete = true;
        });
        LogTrialData();
        LogFrameData();
    }

    protected override bool CheckBlockEnd()
    {
        return EndBlock;
    }

    void CheckIfBlockShouldEnd()
    {
        if (NumTrialsCompletedBlock > CurrentTrial.PerfWindowEndTrials)
        {
            TrialCompletionList.Reverse();

            int sum = 0;
            for(int i = 0; i < CurrentTrial.PerfWindowEndTrials; i++)
            {
                sum += TrialCompletionList[i];
            }
            float performancePerc = sum / CurrentTrial.PerfWindowEndTrials;
            if (performancePerc >= CurrentTrial.PerfThresholdEndTrials)
                EndBlock = true;
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

    bool DidConfigValuesChange()
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
        SquareGO.transform.localScale = new Vector3(CurrentTrial.SquareSize, CurrentTrial.SquareSize, 1);
        SquareGO.transform.localPosition = new Vector3(CurrentTrial.PositionX, CurrentTrial.PositionY, 90);
    }

    void LoadConfigVariables()
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
        BackdropMaterial = BackdropGO.GetComponent<Renderer>().material;
        InitialBackdropColor = BackdropMaterial.color;
    }

    void CreateSquare()
    {
        SquareGO = Instantiate(SquarePrefab, new Vector3(0, 1, 90), Quaternion.identity);
        SquareGO.AddComponent<BoxCollider>();
        SquareGO.name = "SquareGO";
        SquareRenderer = SquareGO.GetComponent<Renderer>();
        SquareMaterial = SquareGO.GetComponent<Renderer>().material;
        InitialSquareColor = SquareMaterial.color;
        //SquareMaterial.color = Color.white;
    }

    void SetSquareSizeAndPosition()
    {
        if (CurrentTrial.RandomSquareSize)
        {
            float randomSize = Random.Range(CurrentTrial.SquareSizeMin, CurrentTrial.SquareSizeMax);
            SquareGO.transform.localScale = new Vector3(randomSize, randomSize, 1);
        }
        else
            SquareGO.transform.localScale = new Vector3(CurrentTrial.SquareSize, CurrentTrial.SquareSize, 1);
        

        if (CurrentTrial.RandomSquarePosition)
        {
            float x = Random.Range(CurrentTrial.PositionX_Min, CurrentTrial.PositionX_Max);
            float y = Random.Range(CurrentTrial.PositionY_Min, CurrentTrial.PositionY_Max);
            SquareGO.transform.localPosition = new Vector3(x, y, 90);
        }
        else
            SquareGO.transform.localPosition = new Vector3(CurrentTrial.PositionX, CurrentTrial.PositionY, 90);
        
    }

    IEnumerator WhiteToBlueStatePause()
    {
        //Using this func to handle them clicking while its changing from WhiteSquare state to BlueSquare state
        yield return new WaitForSeconds(1f);
    }

    void CreateColors()
    {
        LightBlueColor = new Color32(12, 176, 255, 255);
        DarkBlueBackgroundColor = new Color32(2, 3, 39, 255);
        LightRedColor = new Color32(142, 6, 20, 255);
    }

    IEnumerator SquareColorFlash(Color32 newColor)
    {
        Cursor.visible = false;
        SquareMaterial.color = newColor;
        yield return new WaitForSeconds(1f);
        SquareMaterial.color = InitialSquareColor;
    }

    IEnumerator BackgroundColorFlash(Color32 newColor)
    {
        Cursor.visible = false;
        BackdropMaterial.color = newColor;
        yield return new WaitForSeconds(1f);
        BackdropMaterial.color = InitialBackdropColor;
        Cursor.visible = true;
    }

    void ResetGlobalTrialVariables()
    {
        ClickedSquare = false;
        ClickReleased = false;
        GiveHoldReward = false;
        GiveTouchReward = false;
        ConfigValuesChanged = false;
        EndBlock = false;
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

    protected override void DefineTrialStims()
    {
        //Define StimGroups consisting of StimDefs whose gameobjects will be loaded at TrialLevel_SetupTrial and 
        //destroyed at TrialLevel_Finish

        //NO STIMS FOR THIS TASK. 
    }
    
}




//SETUP TRIAL INITILAITON:
//if (THR_CanvasGO == null)
//    CreateCanvas();

//if(THR_Cam == null)
//    THR_Cam = GameObject.Find("THR_Camera").GetComponent<Camera>();

//THR_Cam.clearFlags = CameraClearFlags.SolidColor;
//THR_Cam.backgroundColor = DarkBlueBackgroundColor;


//FEEDBACK INITILAIZTION METHOD:
//if ((GiveTouchReward) || (GiveHoldReward))
//{
//    AudioFBController.Play("Positive");
//    if (SyncBoxController != null)
//    {
//        if (GiveTouchReward)
//            SyncBoxController.SendRewardPulses(CurrentTrial.NumTouchPulses, CurrentTrial.PulseSize); //Touch reward first
//        if (GiveHoldReward)
//            SyncBoxController.SendRewardPulses(CurrentTrial.NumReleasePulses, CurrentTrial.PulseSize); //Then hold reward if earned
//    }
//}
//else
//    AudioFBController.Play("Negative");


//private void CreateCanvas()
//{
//    THR_CanvasGO = new GameObject("THR_CanvasGO");
//    THR_CanvasGO.AddComponent<Canvas>();
//    THR_Canvas = THR_CanvasGO.GetComponent<Canvas>();
//    THR_Canvas.renderMode = RenderMode.ScreenSpaceCamera;
//    THR_Canvas.worldCamera = GameObject.Find("THR_Camera").GetComponent<Camera>();
//    THR_CanvasGO.AddComponent<CanvasScaler>();
//    THR_CanvasGO.AddComponent<GraphicRaycaster>();
//}


//BLUE SQUARE UPDATE METHOD:
//if (InputBroker.GetMouseButtonDown(0))
//{
//    mouseRay = THR_Cam.ScreenPointToRay(Input.mousePosition);
//    RaycastHit hit;
//    if (Physics.Raycast(mouseRay, out hit))
//    {
//        if(hit.transform.name == "SquareGO")
//        {
//            TouchStartTime = Time.time;
//            SquareMaterial.color = Color.green;
//            ClickedSquare = true;
//            NumTouchesBlueSquare++;
//            if (CurrentTrial.RewardTouch)
//                GiveTouchReward = true;
//        }
//    }
//    else
//    {
//        AudioFBController.Play("Negative");
//        StartCoroutine(BackgroundColorFlash(THR_Cam.backgroundColor, LightRedColor));
//        NumNonSquareTouches++;
//    }
//}

//if (InputBroker.GetMouseButtonUp(0))
//{
//    if(ClickedSquare)
//    {
//        Cursor.visible = false;
//        SquareMaterial.color = Color.grey;
//        TouchReleaseTime = Time.time;
//        HeldDuration = TouchReleaseTime - TouchStartTime;

//        if (HeldDuration > CurrentTrial.MinTouchDuration && HeldDuration < CurrentTrial.MaxTouchDuration)
//        {
//            if (CurrentTrial.RewardRelease)
//                GiveHoldReward = true;
//        }
//        else if (HeldDuration < CurrentTrial.MinTouchDuration)
//        {
//            Debug.Log("Didn't hold long enough!");
//            SquareMaterial.color = LightRedColor;
//        }
//        else
//        {
//            Debug.Log("Held too long!");
//            SquareMaterial.color = LightRedColor;
//        }
//        ClickReleased = true;
//    }
//}



//WHITE SQUARE UPDATE METHOD:
//if(Input.GetMouseButtonDown(0))
//{
//    mouseRay = THR_Cam.ScreenPointToRay(Input.mousePosition);
//    RaycastHit hit;
//    if(Physics.Raycast(mouseRay, out hit))
//    {
//        if(hit.transform.name == "SquareGO")
//        {
//            AudioFBController.Play("Negative");
//            NumTouchesWhiteSquare++;
//        }
//    }
//}




//had this in the blue square ELSE:
//RenderSettings.skybox = CreateSkybox(RedWhiteStripeFilePath);


//had this in setupTrial:
//ContextPath = GetContextNestedFilePath(CurrentTrial.ContextName);
//RenderSettings.skybox = CreateSkybox(ContextPath);
//RedWhiteStripeFilePath = GetContextNestedFilePath("RedStripe");