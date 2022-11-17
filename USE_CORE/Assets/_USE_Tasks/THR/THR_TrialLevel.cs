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

    //Set in Editor
    public Material SquareMaterial;
        
    //misc
    public Ray mouseRay;
    public Camera THR_Cam;
    public bool ClickReleased;
    public bool ColorsSet;
    public string RedWhiteStripeFilePath;

    public Color32 DarkBlueBackgroundColor;
    public Color32 LightRedColor;
    public Color32 LightBlueColor;

    //public Material GratingLeft;
    //public Material GratingRight;

    [HideInInspector]
    public ConfigNumber MinTouchDuration, MaxTouchDuration, SquareSize, PositionX, PositionY;


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
            DebugTrialInfo();

            if(!ConfigVariablesLoaded)
            {
                LoadConfigVariables();
                ConfigVariablesLoaded = true;
            }

            //if(GratingLeft == null || GratingRight == null)
            //    LoadGratingMaterials();

            if(TrialCount_InBlock == 0)
                ResetBlockGlobalVariables();        

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
                        AudioFBController.Play("Negative");
                }
            }
        });
        WhiteSquare.AddTimer(() => CurrentTrial.WhiteSquareDuration, BlueSquare, () => StartCoroutine(WhiteToBlueStatePause()));

        //BLUE SQUARE state -------------------------------------------------------------------------------------------------------------------------
        BlueSquare.AddInitializationMethod(() =>
        {
            SquareMaterial.color = LightBlueColor;
            TrialStartTime = Time.time;
        });
        BlueSquare.AddUpdateMethod(() =>
        {
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
                        if (CurrentTrial.RewardTouch)
                            GiveTouchReward = true;
                    }
                }
                else
                {
                    StartCoroutine(ChangeBackgroundColor(THR_Cam.backgroundColor, LightRedColor));                    
                }
            }

            if (InputBroker.GetMouseButtonUp(0))
            {
                if(ClickedSquare)
                {
                    SquareMaterial.color = Color.gray;
                    TouchReleaseTime = Time.time;
                    HeldDuration = TouchReleaseTime - TouchStartTime;

                    if (HeldDuration > MinTouchDuration.value && HeldDuration < MaxTouchDuration.value) //(HeldDuration > CurrentTrial.MinTouchDuration && HeldDuration < CurrentTrial.MaxTouchDuration)
                    {
                        if (CurrentTrial.RewardRelease)
                            GiveHoldReward = true;
                    }
                    else if (HeldDuration < MinTouchDuration.value) //(HeldDuration < CurrentTrial.MinTouchDuration);
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
            if (Time.time - TrialStartTime > CurrentTrial.TimeToAutoEndTrialSec)
                AutoEndTrial = true;
            
        });
        BlueSquare.AddTimer(() => CurrentTrial.BlueSquareDuration, Feedback); //remove *20 eventually
        BlueSquare.SpecifyTermination(() => ClickReleased, Feedback);
        BlueSquare.SpecifyTermination(() => AutoEndTrial, ITI); //go to feedback if time elapsed. 

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

            if (GiveHoldReward)
                NumTrialsCorrectBlock++;

            CheckIfBlockShouldEnd(); //NOT DONE YET
        });
        ITI.AddTimer(() => CurrentTrial.ItiDuration, FinishTrial, () => NumTrialsCompletedBlock++);
    }


    void LoadConfigVariables()
    {
        //Variables from the Config UI:
        MinTouchDuration = ConfigUiVariables.get<ConfigNumber>("MinTouchDuration");
        MaxTouchDuration = ConfigUiVariables.get<ConfigNumber>("MaxTouchDuration");
        SquareSize = ConfigUiVariables.get<ConfigNumber>("SquareSize");
        PositionX = ConfigUiVariables.get<ConfigNumber>("PositionX");
        PositionY = ConfigUiVariables.get<ConfigNumber>("PositionY");

        //Start them out as the current trial amounts (specified in block config):
        MinTouchDuration.value = CurrentTrial.MinTouchDuration;
        MaxTouchDuration.value = CurrentTrial.MaxTouchDuration;
        SquareSize.value = CurrentTrial.SquareSize;
        PositionX.value = CurrentTrial.PositionX;
        PositionY.value = CurrentTrial.PositionY;
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
    }

    private void SetSquareSizeAndPosition()
    {
        if (CurrentTrial.RandomSquareSize)
        {
            float randomSize = Random.Range(SquareSize.min, SquareSize.max); //(CurrentTrial.SquareSize, CurrentTrial.SquareSize)
            SquareGO.transform.localScale = new Vector3(randomSize, randomSize, 1);
        }
        else
            SquareGO.transform.localScale = new Vector3(CurrentTrial.SquareSize, CurrentTrial.SquareSize, 1);

        if (CurrentTrial.RandomSquarePosition)
        {
            float x = Random.Range(PositionX.min, PositionX.max); //(CurrentTrial.PositionX_Min, CurrentTrial.PositionX_Max);
            float y = Random.Range(PositionY.min, PositionY.max); //CurrentTrial.PositionY_Min, CurrentTrial.PositionY_Max); 
            SquareGO.transform.localPosition = new Vector3(x, y, 0);
        }
        else
            SquareGO.transform.localPosition = new Vector3(CurrentTrial.PositionX, CurrentTrial.PositionY, 0);
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

    private void DebugTrialInfo()
    {
        Debug.Log("TRIAL COUNT IN BLOCK = " + TrialCount_InBlock);
        Debug.Log("REWARD TOUCH? " + CurrentTrial.RewardTouch);
        Debug.Log("REWARD RELEASE? " + CurrentTrial.RewardRelease);
        Debug.Log("RANDOM SQUARE SIZE? " + CurrentTrial.RandomSquareSize);
        Debug.Log("RANDOM SQUARE POSITION? " + CurrentTrial.RandomSquarePosition);
    }

    private void CreateColors()
    {
        LightBlueColor = new Color32(12, 176, 255, 255);
        DarkBlueBackgroundColor = new Color32(2, 3, 39, 255);
        LightRedColor = new Color32(142, 6, 20, 255);

    }

    private IEnumerator ChangeBackgroundColor(Color32 initialColor, Color32 newColor)
    {
        Cursor.visible = false;
        THR_Cam.backgroundColor = newColor;
        yield return new WaitForSeconds(1f);
        THR_Cam.backgroundColor = initialColor;
        Cursor.visible = true;
    }

    private void ResetBlockGlobalVariables()
    {
        NumTrialsCompletedBlock = 0;
        NumTrialsCorrectBlock = 0;
    }

    private void ResetGlobalTrialVariables()
    {
        ClickedSquare = false;
        ClickReleased = false;
        GiveHoldReward = false;
        GiveTouchReward = false;
    }

    private string GetContextNestedFilePath(string contextName)
    {
        //Recursive search the sub folders of the MaterialFilePath to get Context File Path
        string backupContextName = "LinearDark";
        string contextPath = "";

        string[] filePaths = Directory.GetFiles(MaterialFilePath, $"{contextName}*", SearchOption.AllDirectories);

        if (filePaths.Length == 1)
            contextPath = filePaths[0];
        else
        {
            Debug.Log($"Context File Path Not Found. Defaulting to {backupContextName}.");
            contextPath = Directory.GetFiles(MaterialFilePath, backupContextName, SearchOption.AllDirectories)[0]; //Use Default LinearDark if can't find file.
        }

        return contextPath;
    }

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