using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using ConfigDynamicUI;
using System.Collections;
using USE_ExperimentTemplate_Trial;
using USE_States;
using THR_Namespace;
using UnityEngine.EventSystems;

public class THR_TrialLevel : ControlLevel_Trial_Template
{
    public THR_TrialDef currentTrial => GetCurrentTrialDef<THR_TrialDef>();
    public THR_TaskLevel currentTask => GetTaskLevel<THR_TaskLevel>();

    [HideInInspector] public string MaterialFilePath;

    private Renderer BackdropRenderer;
    private GameObject BackdropGO;
    private GameObject SquareGO;
    private Renderer SquareRenderer;
    private Texture SquareTexture;
    private Material SquareMaterial;

    private float TrialStartTime;
    private float TouchStartTime;
    private float TouchReleaseTime;
    private float? HeldDuration;
    private float BackdropTouchTime;
    private float BackdropTouches;

    private bool GiveTouchReward;
    private bool GiveReleaseReward;
    private bool GiveReward;
    private bool TimeRanOut;

    [HideInInspector] public List<int> TrialCompletionList;
    [HideInInspector] public int TrialsCompleted_Block;
    [HideInInspector] public int TrialsCorrect_Block;

    //Data variables:
    [HideInInspector] public int BackdropTouches_Trial;
    [HideInInspector] public int BlueSquareTouches_Trial;
    [HideInInspector] public int WhiteSquareTouches_Trial;
    [HideInInspector] public int ItiTouches_Trial;
    [HideInInspector] public int TouchRewards_Trial;
    [HideInInspector] public int ReleaseRewards_Trial;
    [HideInInspector] public int NumTouchesMovedOutside_Trial;

    [HideInInspector] public int BackdropTouches_Block;
    [HideInInspector] public int BlueSquareTouches_Block;
    [HideInInspector] public int WhiteSquareTouches_Block;
    [HideInInspector] public int NumTouchRewards_Block;
    [HideInInspector] public int NumReleaseRewards_Block;
    [HideInInspector] public int NumItiTouches_Block;
    [HideInInspector] public int NumReleasedEarly_Block;
    [HideInInspector] public int NumReleasedLate_Block;
    [HideInInspector] public int NumTouchesMovedOutside_Block;


    private bool BlueSquareTouched;
    private bool BlueSquareReleased;
    private bool AudioPlayed;
    private bool Grating;
    private bool HeldTooShort;
    private bool HeldTooLong;
    public bool PerfThresholdMet;
    private bool MovedOutside;
    private bool ConfigValuesChangedInPrevTrial;

    private Color32 LightBlueColor;
    private Color32 LightRedColor;
    private Color32 InitialBackdropColor;

    private float WhiteTimeoutTime;
    private float WhiteStartTime;
    private float BlueStartTime;
    private float ReactionTime
    {
        get
        {
            return TouchStartTime - TrialStartTime;
        }
    }

    private float RewardEarnedTime;
    private float RewardTimer;


    public override void DefineControlLevel()
    {
        State InitTrial = new State("InitTrial");
        State WhiteSquare = new State("WhiteSquare");
        State BlueSquare = new State("BlueSquare");
        State Feedback = new State("Feedback");
        State Reward = new State("Reward");
        State ITI = new State("ITI");
        AddActiveStates(new List<State> { InitTrial, WhiteSquare, BlueSquare, Feedback, Reward, ITI});

        LoadTextures(MaterialFilePath);

        Add_ControlLevel_InitializationMethod(() =>
        {
            if (BackdropGO == null)
                CreateBackdrop();
            if (SquareGO == null)
                CreateSquare();
            CreateColors();
        });

        //SETUP TRIAL state -------------------------------------------------------------------------------------------------------------------------
        SetupTrial.SpecifyTermination(() => true, InitTrial);

        //INIT TRIAL state --------------------------------------------------------------------------------------------------------------------------
        InitTrial.AddInitializationMethod(() =>
        {
            ResetGlobalTrialVariables();
            SetTrialSummaryString();

            if(TrialCount_InBlock == 0)
                SetConfigValuesToTrialValues();

            LoadConfigUIVariables();
            SetSquareSizeAndPosition();

            currentTask.CalculateBlockSummaryString();

            if (TrialCount_InTask != 0)
                currentTask.SetTaskSummaryString();
        });
        InitTrial.SpecifyTermination(() => true, WhiteSquare, () => TrialStartTime = Time.time);

        //WHITE SQUARE state ------------------------------------------------------------------------------------------------------------------------
        WhiteSquare.AddInitializationMethod(() =>
        {
            Input.ResetInputAxes();
            SquareMaterial.color = Color.white;
            if (!SquareGO.activeInHierarchy)
                ActivateSquareAndBackdrop();
            WhiteStartTime = Time.time;
            WhiteTimeoutTime = 0;
        });
        WhiteSquare.AddUpdateMethod(() =>
        {
            if(WhiteTimeoutTime != 0 && (Time.time - WhiteTimeoutTime) > currentTrial.TimeoutDuration)
                WhiteTimeoutTime = 0;

            if (InputBroker.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject()) //makes sure mouse isn't over a UI element. 
            {
                RaycastHit hit;
                if (Physics.Raycast(Camera.main.ScreenPointToRay(InputBroker.mousePosition), out hit, Mathf.Infinity))
                {
                    if (hit.transform.gameObject.name == "SquareGO")
                    {
                        WhiteSquareTouches_Trial++;
                        if (WhiteTimeoutTime == 0)
                        {
                            WhiteTimeoutTime = Time.time;
                            WhiteStartTime = Time.time; //reset original WhiteStartTime so that normal duration resets.
                            AudioFBController.Play("Negative");
                        }
                    }
                    if (hit.transform.gameObject.name == "BackdropGO")
                    {
                        BackdropTouches_Trial++;
                        StartCoroutine(GratedBackdropFlash(BackdropStripesTexture));
                    }
                }

            }
        });
        WhiteSquare.SpecifyTermination(() => ((Time.time - WhiteStartTime) > currentTrial.WhiteSquareDuration) && WhiteTimeoutTime == 0, BlueSquare);

        //BLUE SQUARE state -------------------------------------------------------------------------------------------------------------------------
        BlueSquare.AddInitializationMethod(() =>
        {
            Input.ResetInputAxes();
            SquareMaterial.color = LightBlueColor;
            if (!SquareGO.activeInHierarchy)
                ActivateSquareAndBackdrop();
            BlueStartTime = Time.time;
            BlueSquareTouched = false;
            BlueSquareReleased = false;
            MovedOutside = false;
            BackdropTouchTime = 0;
            BackdropTouches = 0;
            HeldDuration = 0;
        });
        BlueSquare.AddUpdateMethod(() =>
        {
            if (InputBroker.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                RaycastHit hit;
                if (Physics.Raycast(Camera.main.ScreenPointToRay(InputBroker.mousePosition), out hit, Mathf.Infinity))
                {
                    if (hit.transform.gameObject.name == "SquareGO" && !Grating)
                    {
                        if (!BlueSquareTouched)
                        {
                            TouchStartTime = Time.time;
                            BlueSquareTouched = true;
                        }

                        if (currentTrial.RewardTouch)
                        {
                            SquareMaterial.color = Color.gray;
                            BlueSquareTouches_Trial++;
                            GiveTouchReward = true;
                            RewardEarnedTime = Time.time;
                        }
                        else
                            SquareMaterial.color = Color.blue;
                    }
                    if (hit.transform.gameObject.name == "BackdropGO" && !BlueSquareTouched && !Grating)
                    {
                        if (BackdropTouches == 0)
                        {
                            BackdropTouchTime = Time.time;
                            BlueStartTime += currentTrial.TimeoutDuration; //add extra second so it doesn't go straight to white after grating
                            Input.ResetInputAxes();
                            StartCoroutine(GratedBackdropFlash(BackdropStripesTexture));
                            BackdropTouches++;
                            BackdropTouches_Trial++;
                        }
                    }
                }
            }

            if (InputBroker.GetMouseButton(0) && BlueSquareTouched)
            {
                HeldDuration += Time.deltaTime;

                RaycastHit hitt;
                if (Physics.Raycast(Camera.main.ScreenPointToRay(InputBroker.mousePosition), out hitt, Mathf.Infinity))
                {
                    if (hitt.transform.gameObject.name == "BackdropGO")
                    {
                        Input.ResetInputAxes();
                        NumTouchesMovedOutside_Trial++;
                        MovedOutside = true;
                    }
                }
            }

            if (InputBroker.GetMouseButtonUp(0))
            {
                if (BlueSquareTouched && !BlueSquareReleased)
                {
                    TouchReleaseTime = Time.time;
                    if (currentTrial.RewardRelease)
                    {
                        if (HeldDuration >= currentTrial.MinTouchDuration && HeldDuration <= currentTrial.MaxTouchDuration)
                        {
                            BlueSquareTouches_Trial++;
                            GiveReleaseReward = true;
                            RewardEarnedTime = Time.time;
                        }
                        else if (HeldDuration < currentTrial.MinTouchDuration)
                        {
                            NumReleasedEarly_Block++;
                            HeldTooShort = true;
                        }
                        //The Else (Greater than MaxDuration) is handled below where I auto stop them for holding for max dur. 
                    }
                    else
                        SquareMaterial.color = Color.gray;

                    BlueSquareReleased = true;
                }
            }

            if (HeldDuration >= currentTrial.MaxTouchDuration && BlueSquareTouched)
            {
                NumReleasedLate_Block++;
                HeldTooLong = true;
            }

            if (Time.time - TrialStartTime > currentTrial.TimeToAutoEndTrialSec)
                TimeRanOut = true;


            if (BackdropTouchTime != 0 && (Time.time - BackdropTouchTime) > currentTrial.TimeoutDuration)
            {
                BackdropTouches = 0;
                BackdropTouchTime = 0;
            }
        });
        BlueSquare.SpecifyTermination(() => (Time.time - BlueStartTime > currentTrial.BlueSquareDuration) && !InputBroker.GetMouseButton(0) && !BlueSquareReleased && !Grating, WhiteSquare); //Go back to white square if bluesquare time lapses (and they aren't already holding down)
        BlueSquare.SpecifyTermination(() => (BlueSquareReleased && !Grating) || MovedOutside || HeldTooLong || HeldTooShort || TimeRanOut || GiveTouchReward, Feedback); //If rewarding touch and they touched, or click the square and release, or run out of time. 

        //FEEDBACK state ----------------------------------------------------------------------------------------------------------------------------
        Feedback.AddInitializationMethod(() =>
        {
            RewardTimer = Time.time - RewardEarnedTime; //start the timer at the difference between rewardtimeEarned and right now.
            AudioPlayed = false;
            GiveReward = false;

            if(GiveTouchReward || GiveReleaseReward)
            {
                AudioFBController.Play("Positive");
                if (GiveReleaseReward)
                    SquareMaterial.color = Color.gray;
            }
            else //held too long, held too short, moved outside, or timeRanOut
            {
                AudioFBController.Play("Negative");
                if (currentTrial.ShowNegFb)
                {
                    if (HeldTooShort)
                        StartCoroutine(GratedSquareFlash(HeldTooShortTexture));
                    else if (HeldTooLong)
                        StartCoroutine(GratedSquareFlash(HeldTooLongTexture));
                    else if (MovedOutside)
                        StartCoroutine(GratedSquareFlash(BackdropStripesTexture));
                }
            }
            AudioPlayed = true;
        });
        Feedback.AddUpdateMethod(() =>
        {
            if((GiveTouchReward || GiveReleaseReward) && SyncBoxController != null)
            {
                if (RewardTimer < (GiveTouchReward ? currentTrial.TouchToRewardDelay : currentTrial.ReleaseToRewardDelay))
                    RewardTimer += Time.deltaTime;
                else
                    GiveReward = true;
            }
        });
        Feedback.SpecifyTermination(() => GiveReward, Reward); //If they got right, syncbox isn't null, and timer is met.
        Feedback.SpecifyTermination(() => (GiveTouchReward || GiveReleaseReward) && SyncBoxController == null, ITI); //If they got right, syncbox IS null, don't make them wait.  
        Feedback.SpecifyTermination(() => !GiveTouchReward && !GiveReleaseReward && AudioPlayed && !Grating, ITI); //if didn't get right, so no pulses. 

        Reward.AddInitializationMethod(() =>
        {
            if (GiveReleaseReward && SyncBoxController != null)
            {
                SyncBoxController.SendRewardPulses(currentTrial.NumReleasePulses, currentTrial.PulseSize);
                ReleaseRewards_Trial += currentTrial.NumReleasePulses;
                SessionInfoPanel.UpdateSessionSummaryValues(("totalRewardPulses",currentTrial.NumReleasePulses));
            }
            if (GiveTouchReward && SyncBoxController != null)
            {
                SyncBoxController.SendRewardPulses(currentTrial.NumTouchPulses, currentTrial.PulseSize);
                TouchRewards_Trial += currentTrial.NumTouchPulses;
                SessionInfoPanel.UpdateSessionSummaryValues(("totalRewardPulses",currentTrial.NumReleasePulses));
            }
        });
        Reward.SpecifyTermination(() => true, ITI);

        //ITI state ---------------------------------------------------------------------------------------------------------------------------------
        ITI.AddUpdateMethod(() =>
        {
            if(InputBroker.GetMouseButtonUp(0))
                ItiTouches_Trial++;
        });
        ITI.AddTimer(() => currentTrial.ItiDuration, FinishTrial);
        ITI.AddDefaultTerminationMethod(() =>
        {
            SquareGO.SetActive(false);

            if (GiveReleaseReward || GiveTouchReward)
                TrialsCorrect_Block++;

            if (GiveTouchReward || GiveReleaseReward)
                TrialCompletionList.Insert(0, 1);
            else
                TrialCompletionList.Insert(0, 0);
            
            AddTrialTouchNumsToBlock();
            TrialsCompleted_Block++;
            currentTask.CalculateBlockSummaryString();

            CheckIfBlockShouldEnd();

            ConfigValuesChangedInPrevTrial = ConfigValuesChanged();
        });

        LogTrialData();
        LogFrameData();
    }


    //HELPER FUNCTIONS ------------------------------------------------------------------------------------------
    private void AddTrialTouchNumsToBlock()
    {
        BlueSquareTouches_Block += BlueSquareTouches_Trial;
        WhiteSquareTouches_Block += WhiteSquareTouches_Trial;
        BackdropTouches_Block += BackdropTouches_Trial;
        NumItiTouches_Block += ItiTouches_Trial;
        NumTouchRewards_Block += TouchRewards_Trial;
        NumReleaseRewards_Block += ReleaseRewards_Trial;
        NumTouchesMovedOutside_Block += NumTouchesMovedOutside_Trial;
    }

    private void SetTrialSummaryString()
    {
        TrialSummaryString = "Rewarding: " + (currentTrial.RewardTouch ? "Touch" : "Release") +
                              "\nRandomPosition: " + ((currentTrial.RandomSquarePosition ? "True" : "False")) +
                              "\nRandomSize: " + ((currentTrial.RandomSquareSize ? "True" : "False"));
    }

    protected override bool CheckBlockEnd()
    {
        return PerfThresholdMet;
    }

    private void CheckIfBlockShouldEnd()
    {
        if(TrialsCompleted_Block >= currentTrial.PerfWindowEndTrials)
        {
            int sum = 0;
            for(int i = 0; i < currentTrial.PerfWindowEndTrials; i++)
            {
                sum += TrialCompletionList[i];
            }
            float performancePerc = sum / currentTrial.PerfWindowEndTrials;
            if(performancePerc >= currentTrial.PerfThresholdEndTrials)
                PerfThresholdMet = true; //Will trigger CheckBlockEnd function to terminate block
        }
    }

    private void LoadConfigUIVariables()
    {
        currentTrial.MinTouchDuration = ConfigUiVariables.get<ConfigNumber>("minTouchDuration").value;
        currentTrial.MaxTouchDuration = ConfigUiVariables.get<ConfigNumber>("maxTouchDuration").value;
        currentTrial.SquareSize = (int)ConfigUiVariables.get<ConfigNumber>("squareSize").value;
        currentTrial.PositionX = (int)ConfigUiVariables.get<ConfigNumber>("positionX").value;
        currentTrial.PositionY = (int)ConfigUiVariables.get<ConfigNumber>("positionY").value;
        currentTrial.WhiteSquareDuration = ConfigUiVariables.get<ConfigNumber>("whiteSquareDuration").value;
        currentTrial.BlueSquareDuration = ConfigUiVariables.get<ConfigNumber>("blueSquareDuration").value;
    }

    private bool ConfigValuesChanged()
    {
        if (currentTrial.SquareSize != ConfigUiVariables.get<ConfigNumber>("squareSize").value
            || currentTrial.PositionX != ConfigUiVariables.get<ConfigNumber>("positionX").value
            || currentTrial.PositionY != ConfigUiVariables.get<ConfigNumber>("positionY").value)
        {
            return true;
        }
        else
            return false;
    }

    private void SetConfigValuesToTrialValues()
    {
        ConfigUiVariables.get<ConfigNumber>("squareSize").SetValue(currentTrial.SquareSize);
        ConfigUiVariables.get<ConfigNumber>("positionX").SetValue(currentTrial.PositionX);
        ConfigUiVariables.get<ConfigNumber>("positionY").SetValue(currentTrial.PositionY);
        ConfigUiVariables.get<ConfigNumber>("minTouchDuration").SetValue(currentTrial.MinTouchDuration);
        ConfigUiVariables.get<ConfigNumber>("maxTouchDuration").SetValue(currentTrial.MaxTouchDuration);
        ConfigUiVariables.get<ConfigNumber>("whiteSquareDuration").SetValue(currentTrial.WhiteSquareDuration);
        ConfigUiVariables.get<ConfigNumber>("blueSquareDuration").SetValue(currentTrial.BlueSquareDuration);
    }

    private void SetSquareSizeAndPosition()
    {
        if (currentTrial.RandomSquareSize && !ConfigValuesChangedInPrevTrial)
        {
            int randomSize = Random.Range(currentTrial.SquareSizeMin, currentTrial.SquareSizeMax);
            SquareGO.transform.localScale = new Vector3(randomSize, randomSize, .001f);
            ConfigUiVariables.get<ConfigNumber>("squareSize").SetValue(randomSize);
            currentTrial.SquareSize = randomSize;
        }
        else
            SquareGO.transform.localScale = new Vector3(currentTrial.SquareSize, currentTrial.SquareSize, .001f);

        if (currentTrial.RandomSquarePosition && !ConfigValuesChangedInPrevTrial)
        {
            int x = Random.Range(currentTrial.PositionX_Min, currentTrial.PositionX_Max);
            int y = Random.Range(currentTrial.PositionY_Min, currentTrial.PositionY_Max);
            SquareGO.transform.localPosition = new Vector3(x, y, 90);
            ConfigUiVariables.get<ConfigNumber>("positionX").SetValue(x);
            ConfigUiVariables.get<ConfigNumber>("positionY").SetValue(y);
            currentTrial.PositionX = x;
            currentTrial.PositionY = y;
        }
        else
            SquareGO.transform.localPosition = new Vector3(currentTrial.PositionX, currentTrial.PositionY, 90);
    }


    private void ActivateSquareAndBackdrop()
    {
        BackdropGO.SetActive(true);
        SquareGO.SetActive(true);
    }

    private void CreateBackdrop()
    {
        BackdropGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        BackdropGO.name = "BackdropGO";
        BackdropGO.transform.position = new Vector3(0, 0, 95);
        BackdropGO.transform.localScale = new Vector3(275, 150, .5f);
        BackdropGO.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        BackdropRenderer = BackdropGO.GetComponent<Renderer>();
        BackdropRenderer.material.mainTexture = THR_BackdropTexture;
        InitialBackdropColor = BackdropRenderer.material.color;

        BackdropGO.GetComponent<Renderer>().material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        BackdropGO.GetComponent<Renderer>().material.SetFloat("_SpecularHighlights", 0f);
    }

    private void CreateSquare()
    {
        SquareGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        SquareGO.name = "SquareGO";
        SquareGO.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        SquareRenderer = SquareGO.GetComponent<Renderer>();
        SquareMaterial = SquareRenderer.material;
        SquareTexture = SquareRenderer.material.mainTexture;
        SquareGO.GetComponent<Renderer>().material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        SquareGO.GetComponent<Renderer>().material.SetFloat("_SpecularHighlights", 0f);
    }

    private void CreateColors()
    {
        LightRedColor = new Color32(224, 78, 92, 255);
        LightBlueColor = new Color32(0, 150, 255, 255);
    }

    private IEnumerator GratedSquareFlash(Texture2D newTexture)
    {
        Grating = true;
        SquareMaterial.color = LightRedColor;
        SquareRenderer.material.mainTexture = newTexture;
        yield return new WaitForSeconds(currentTrial.GratingSquareDuration);
        SquareMaterial.color = Color.gray;
        SquareRenderer.material.mainTexture = SquareTexture;
        Grating = false;
    }

    private IEnumerator GratedBackdropFlash(Texture2D newTexture)
    {
        Grating = true;
        Color32 currentSquareColor = SquareMaterial.color;
        SquareMaterial.color = new Color32(255, 153, 153, 255);
        BackdropRenderer.material.color = LightRedColor;
        BackdropRenderer.material.mainTexture = newTexture;
        AudioFBController.Play("Negative");
        yield return new WaitForSeconds(1f);
        BackdropRenderer.material.mainTexture = THR_BackdropTexture;
        BackdropRenderer.material.color = InitialBackdropColor;
        SquareMaterial.color = currentSquareColor;
        Grating = false;
    }

    private void ResetGlobalTrialVariables()
    {
        HeldTooLong = false;
        HeldTooShort = false;
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
        TouchRewards_Trial = 0;
        ReleaseRewards_Trial = 0;
    }

    private void LogTrialData()
    {
        TrialData.AddDatum("SquareSize", () => currentTrial.SquareSize);
        TrialData.AddDatum("SquarePosX", () => currentTrial.PositionX);
        TrialData.AddDatum("SquarePosY", () => currentTrial.PositionY);
        TrialData.AddDatum("MinTouchDuration", () => currentTrial.MinTouchDuration);
        TrialData.AddDatum("MaxTouchDuration", () => currentTrial.MaxTouchDuration);
        TrialData.AddDatum("RewardTouch", () => currentTrial.RewardTouch);
        TrialData.AddDatum("RewardRelease", () => currentTrial.RewardRelease);
        TrialData.AddDatum("DifficultyLevel", () => currentTrial.BlockName);
        TrialData.AddDatum("BlueSquareTouches_Trial", () => BlueSquareTouches_Trial);
        TrialData.AddDatum("WhiteSquareTouches_Trial", () => WhiteSquareTouches_Trial);
        TrialData.AddDatum("BackdropTouches_Trial", () => BackdropTouches_Trial);
        TrialData.AddDatum("MovedOutsideSquare_Trial", () => NumTouchesMovedOutside_Trial);
        TrialData.AddDatum("ItiTouches_Trial", () => ItiTouches_Trial);
        TrialData.AddDatum("ReactionTime", () => ReactionTime);
        TrialData.AddDatum("TouchStartTime", () => TouchStartTime);
        TrialData.AddDatum("HeldDuration", () => HeldDuration);
    }

    private void LogFrameData()
    {
        FrameData.AddDatum("TouchPosition", () => InputBroker.mousePosition);
        FrameData.AddDatum("SquareGO", () => SquareGO.activeInHierarchy);
    }

}
