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

public class THR_TrialLevel : ControlLevel_Trial_Template
{
    public THR_TrialDef CurrentTrial => GetCurrentTrialDef<THR_TrialDef>();

    public string MaterialFilePath;
    public string ContextPath;

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

    public bool GiveTouchReward;
    public bool GiveHoldReward;
    public bool ClickedSquare;
    public bool AutoEndTrial;
    public bool ConfigVariablesLoaded;

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

    public float CurrentSquareSize;
    public float CurrentPositionX;
    public float CurrentPositionY;
        
    //misc
    public Ray mouseRay;
    public Camera THR_Cam;
    public bool ClickReleased;
    public bool ColorsSet;
    public string RedWhiteStripeFilePath;

    public Color32 DarkBlueBackgroundColor;
    public Color32 LightRedColor;
    public Color32 LightBlueColor;
    public Color32 LightGreyColor;

    //public Material GratingLeft;
    //public Material GratingRight;

    [HideInInspector]
    public ConfigNumber minTouchDuration, maxTouchDuration, squareSize, positionX, positionY, whiteSquareDuration, blueSquareDuration;


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
            //if (GratingLeft == null || GratingRight == null)
            //    LoadGratingMaterials();

            if (TrialCount_InBlock == 0)
                ResetGlobalBlockVariables();        

            if(!ColorsSet)
                CreateColors();

            if(THR_Cam == null)
                THR_Cam = GameObject.Find("THR_Camera").GetComponent<Camera>();

            THR_Cam.clearFlags = CameraClearFlags.SolidColor;
            THR_Cam.backgroundColor = DarkBlueBackgroundColor;
        });
        SetupTrial.SpecifyTermination(() => true, InitTrial);

        //INIT TRIAL state --------------------------------------------------------------------------------------------------------------------------
        InitTrial.AddInitializationMethod(() =>
        {
            if (THR_CanvasGO == null)
                CreateCanvas();

            if (SquareGO == null)
                CreateSquare();

            SetSquareSizeAndPosition();

            if (!ConfigVariablesLoaded)
            {
                LoadConfigVariables();
                ConfigVariablesLoaded = true;
            }

            ResetGlobalTrialVariables();
        });
        InitTrial.SpecifyTermination(() => true, WhiteSquare);

        //WHITE SQUARE state ------------------------------------------------------------------------------------------------------------------------
        WhiteSquare.AddInitializationMethod(() =>
        {
            THR_CanvasGO.SetActive(true);
            SquareGO.SetActive(true);
        });

        WhiteSquare.AddUpdateMethod(() =>
        {
            if(Input.GetMouseButtonDown(0))
            {
                mouseRay = THR_Cam.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if(Physics.Raycast(mouseRay, out hit))
                {
                    if(hit.transform.name == "SquareGO")
                    {
                        AudioFBController.Play("Negative");
                        NumTouchesWhiteSquare++;
                    }
                }
            }
        });
        WhiteSquare.AddTimer(() => whiteSquareDuration.value, BlueSquare, () => StartCoroutine(WhiteToBlueStatePause()));

        //BLUE SQUARE state -------------------------------------------------------------------------------------------------------------------------
        BlueSquare.AddInitializationMethod(() =>
        {
            SquareMaterial.color = LightBlueColor;
            TrialStartTime = Time.time;
        });
        BlueSquare.AddUpdateMethod(() =>
        {
            //LoadConfigVariables();

            if (InputBroker.GetMouseButtonDown(0))
            {
                mouseRay = THR_Cam.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(mouseRay, out hit))
                {
                    if(hit.transform.name == "SquareGO")
                    {
                        TouchStartTime = Time.time;
                        SquareMaterial.color = Color.blue;
                        ClickedSquare = true;
                        NumTouchesBlueSquare++;
                        if (CurrentTrial.RewardTouch)
                            GiveTouchReward = true;
                    }
                }
                else
                {
                    AudioFBController.Play("Negative");
                    StartCoroutine(BackgroundColorFlash(THR_Cam.backgroundColor, LightRedColor));
                    NumNonSquareTouches++;
                }
            }

            if (InputBroker.GetMouseButtonUp(0))
            {
                if(ClickedSquare)
                {
                    SquareMaterial.color = LightGreyColor;
                    TouchReleaseTime = Time.time;
                    HeldDuration = TouchReleaseTime - TouchStartTime;

                    //if(HeldDuration > CurrentTrial.MinTouchDuration && HeldDuration < CurrentTrial.MaxTouchDuration)
                    if (HeldDuration > minTouchDuration.value && HeldDuration < maxTouchDuration.value)
                    {
                        if (CurrentTrial.RewardRelease)
                            GiveHoldReward = true;
                    }
                    //else if(HeldDuration < CurrentTrial.MinTouchDuration)
                    else if (HeldDuration < minTouchDuration.value)
                    {
                        Debug.Log("Didn't hold long enough!");
                        //SquareRenderer.material = GratingLeft; //this doesn't work. turns it pink. 
                        //SET SOURCE IMAGE OF THE SQUARE GAME OBJECT TO THE GRATED PATTERN.
                    }
                    else
                    {
                        Debug.Log("Held too long!");
                        //SquareRenderer.material = GratingRight; //this doesn't work. turns it pink. 
                        //SET SOURCE IMAGE OF THE SQUARE GAME OBJECT TO THE GRATED PATTERN.
                    }
                    ClickReleased = true;
                }
            }
            //if (Time.time - TrialStartTime > CurrentTrial.TimeToAutoEndTrialSec)
            //    AutoEndTrial = true;
            
        });
        BlueSquare.AddTimer(() => blueSquareDuration.value *10, Feedback); //remove *20 eventually
        BlueSquare.SpecifyTermination(() => ClickReleased, Feedback);
        //BlueSquare.SpecifyTermination(() => AutoEndTrial, ITI); //go to feedback if time elapsed. 

        //FEEDBACK state ------------------------------------------------------------------------------------------------------------------------
        Feedback.AddInitializationMethod(() =>
        {
            FeedbackStartTime = Time.time;

            if((GiveTouchReward) || (GiveHoldReward))
            {
                AudioFBController.Play("Positive");
                if (SyncBoxController != null)
                {
                    if(GiveTouchReward)
                        SyncBoxController.SendRewardPulses(CurrentTrial.NumTouchPulses, CurrentTrial.PulseSize); //Touch reward first
                    if(GiveHoldReward)
                        SyncBoxController.SendRewardPulses(CurrentTrial.NumReleasePulses, CurrentTrial.PulseSize); //Then hold reward if earned
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

            //ConfigVariablesLoaded = false;

            if (GiveHoldReward)
                NumTrialsCorrectBlock++;

            CheckIfBlockShouldEnd(); //NOT DONE YET
        });
        ITI.AddTimer(() => CurrentTrial.ItiDuration, FinishTrial, () => NumTrialsCompletedBlock++);
    }


    void LoadConfigVariables()
    {
        //Variables from the Config UI:
        minTouchDuration = ConfigUiVariables.get<ConfigNumber>("minTouchDuration");
        maxTouchDuration = ConfigUiVariables.get<ConfigNumber>("maxTouchDuration");
        squareSize = ConfigUiVariables.get<ConfigNumber>("squareSize");
        positionX = ConfigUiVariables.get<ConfigNumber>("positionX");
        positionY = ConfigUiVariables.get<ConfigNumber>("positionY");
        whiteSquareDuration = ConfigUiVariables.get<ConfigNumber>("whiteSquareDuration");
        blueSquareDuration = ConfigUiVariables.get<ConfigNumber>("blueSquareDuration");

        if (squareSize.value != CurrentSquareSize)
            ChangeSquareSize();
        if (positionX.value != CurrentPositionX || positionY.value != CurrentPositionY)
            ChangeSquarePosition();
    }

    private void ChangeSquarePosition()
    {
        SquareGO.transform.localPosition = new Vector3(positionX.value, positionY.value, 0);
        //set new current
        CurrentPositionX = positionX.value;
        CurrentPositionY = positionY.value;
    }

    private void ChangeSquareSize()
    {
        SquareGO.transform.localScale = new Vector3(squareSize.value, squareSize.value);
        //set new current
        CurrentSquareSize = squareSize.value;
    }

    private void CheckIfBlockShouldEnd()
    {
        //ADD THIS FUNC
    }

    private void CreateSquare()
    {
        SquareGO = Instantiate(SquarePrefab, THR_CanvasGO.transform);
        SquareGO.name = "SquareGO";
        SquareRenderer = SquareGO.GetComponent<Renderer>();
        SquareMaterial = SquareGO.GetComponent<Renderer>().material;
        SquareMaterial.color = Color.white;
    }

    private void SetSquareSizeAndPosition()
    {
        if (CurrentTrial.RandomSquareSize)
        {
            //float randomSize = Random.Range(CurrentTrial.SquareSize, CurrentTrial.SquareSize);
            float randomSize = Random.Range(squareSize.min, squareSize.max);
            SquareGO.transform.localScale = new Vector3(randomSize, randomSize, 1);
            CurrentSquareSize = randomSize;
        }
        else
        {
            //SquareGO.transform.localScale = new Vector3(CurrentTrial.SquareSize, CurrentTrial.SquareSize, 1);
            //CurrentSquareSize = CurrentTrial.SquareSize;
            SquareGO.transform.localScale = new Vector3(squareSize.value, squareSize.value, 1);
            CurrentSquareSize = squareSize.value;
        }

        if (CurrentTrial.RandomSquarePosition)
        {
            //float x = Random.Range(CurrentTrial.PositionX_Min, CurrentTrial.PositionX_Max);
            //float y = Random.Range(CurrentTrial.PositionY_Min, CurrentTrial.PositionY_Max);
            float x = Random.Range(positionX.min, positionX.max);
            float y = Random.Range(positionY.min, positionY.max);
            SquareGO.transform.localPosition = new Vector3(x, y, 0);
            CurrentPositionX = x;
            CurrentPositionY = y;
        }
        else
        {
            //SquareGO.transform.localPosition = new Vector3(CurrentTrial.PositionX, CurrentTrial.PositionY, 0);
            //CurrentPositionX = CurrentTrial.PositionX;
            //CurrentPositionY = CurrentTrial.PositionY;
            SquareGO.transform.localPosition = new Vector3(positionX.value, positionY.value, 0);
            CurrentPositionX = positionX.value;
            CurrentPositionY = positionY.value;
        }
    }

    private void CreateCanvas()
    {
        THR_CanvasGO = new GameObject("THR_CanvasGO");
        THR_CanvasGO.AddComponent<Canvas>();
        THR_Canvas = THR_CanvasGO.GetComponent<Canvas>();
        THR_Canvas.renderMode = RenderMode.ScreenSpaceCamera;
        THR_Canvas.worldCamera = GameObject.Find("THR_Camera").GetComponent<Camera>();
        THR_CanvasGO.AddComponent<CanvasScaler>();
        THR_CanvasGO.AddComponent<GraphicRaycaster>();
    }

    private IEnumerator WhiteToBlueStatePause()
    {
        //Using this func to handle them clicking while its changing from WhiteSquare state to BlueSquare state
        yield return new WaitForSeconds(.5f);
    }

    //private void LoadGratingMaterials()
    //{
    //    GratingLeft = Resources.Load<Material>("DiagLeftGrating");
    //    GratingRight = Resources.Load<Material>("DiagRightGrating");
    //}

    private void CreateColors()
    {
        LightBlueColor = new Color32(12, 176, 255, 255);
        DarkBlueBackgroundColor = new Color32(2, 3, 39, 255);
        LightRedColor = new Color32(142, 6, 20, 255);
        LightGreyColor = new Color32(211, 211, 211, 255);
    }

    private IEnumerator BackgroundColorFlash(Color32 initialColor, Color32 newColor)
    {
        Cursor.visible = false;
        THR_Cam.backgroundColor = newColor;
        yield return new WaitForSeconds(1f);
        THR_Cam.backgroundColor = initialColor;
        Cursor.visible = true;
    }

    private void ResetGlobalBlockVariables()
    {
        NumTrialsCompletedBlock = 0;
        NumTrialsCorrectBlock = 0;
        NumNonSquareTouches = 0;
        NumTouchesBlueSquare = 0;
        NumTouchesWhiteSquare = 0;
    }

    private void ResetGlobalTrialVariables()
    {
        ClickedSquare = false;
        ClickReleased = false;
        GiveHoldReward = false;
        GiveTouchReward = false;
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



//had this in the blue square ELSE:
//RenderSettings.skybox = CreateSkybox(RedWhiteStripeFilePath);


//had this in setupTrial:
//ContextPath = GetContextNestedFilePath(CurrentTrial.ContextName);
//RenderSettings.skybox = CreateSkybox(ContextPath);
//RedWhiteStripeFilePath = GetContextNestedFilePath("RedStripe");