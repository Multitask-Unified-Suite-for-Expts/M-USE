using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using USE_States;
using USE_Settings;
using USE_ExperimentTemplate_Trial;
using USE_StimulusManagement;
using GazeCalibration_Namespace;
using EyeTrackerData_Namespace;
using System;
using USE_UI;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;
using System.Globalization;
using Tobii.Research;
using Tobii.Research.Unity;
using System.Windows.Forms;
using USE_Common_Namespace;
using System.Linq;
using USE_DisplayManagement;
using static Tobii.Research.Unity.CalibrationThread;
using static System.Net.Mime.MediaTypeNames;
using UnityEngine.UI;
using System.Text;

public class GazeCalibration_TrialLevel : ControlLevel_Trial_Template
{
    public GazeCalibration_TrialDef CurrentTrialDef => GetCurrentTrialDef<GazeCalibration_TrialDef>();
    public GazeCalibration_TaskLevel CurrentTaskLevel => GetTaskLevel<GazeCalibration_TaskLevel>();

    public GameObject GC_CanvasGO;

    // Task Def Variables
    [HideInInspector] public String ContextExternalFilePath;
    [HideInInspector] public bool SpoofGazeWithMouse;
    [HideInInspector] public float[] CalibPointsInset;
    [HideInInspector] public float MaxCircleScale;
    [HideInInspector] public float MinCircleScale;
    [HideInInspector] public float ShrinkDuration;

    //for calibration point definition
    [HideInInspector] public NormalizedPoint2D[] ninePoints;
    [HideInInspector] public int numCalibPoints;
 //   [HideInInspector] public float[] calibPointsInset = new float[2] { .1f, .15f };

    [HideInInspector] public NormalizedPoint2D[] calibPointsADCS;
    private ScreenTransformations screenTransformations;


    //Other Calibration Point Variables....?
    private float acceptableCalibrationDistance;
    [HideInInspector]
    public bool currentCalibrationPointFinished, calibrationUnfinished, calibrationFinished;
    private int recalibratePoint = 0;
    //private bool calibAssessment;
    private Vector2 currentScreenTarget;
    private NormalizedPoint2D currentADCSTarget;
    private Vector3 moveVector;
    private Vector3 calibCircleStartPos;
    private int CalibNum;
    private bool pointFinished, recalibPoint;
    private bool OnFinalPoint;

    private float elapsedShrinkDuration; 
    private float blinkOnDuration = 0.2f;
    private float blinkOffDuration = 0.1f;
    private float blinkTimer = 0;
    private float assessTime = 5f;


    // Game Objects
    private USE_Circle CalibBigCircle;

    // NEW Tobii SDK Variables
    //private static DisplayArea DisplayArea;/*
    /*private EyeTracker EyeTracker;
    private IEyeTracker IEyeTracker;*/
    private NormalizedPoint2D currentNormPoint;
    //private static ScreenBasedCalibration ScreenBasedCalibration;
    private CalibrationResult CalibrationResult;
    private Vector2? latestGazePosition;
    public MonitorDetails MonitorDetails;

    public GameObject EyeTrackerPrefab;
    public GameObject TrackBoxPrefab;

    private float screenWidth, screenHeight;
    private GameObject PlayerViewPanelGO;
    private UnityEngine.UI.Text txt;
/*
    private bool recalibpoint = false;
    private bool resultAdded = false;
    private bool resultsDisplayed = false;
    private bool pointFinished = false;*/
    private bool keyboardOverride = false;

    private PlayerViewPanel PlayerViewPanel;
    private GameObject InstructionsGO;
    private GameObject PlayerTextGO;
    private GameObject ResultContainer;
    private SelectionTracking.SelectionTracker.SelectionHandler SelectionHandler;


    public override void DefineControlLevel()
    {
        State Init = new State("Init");
        State Blink = new State("Blink");
        State Shrink = new State("Shrink");
        State Check = new State("Check");
        State Calibrate = new State("Calibrate");
        State Confirm = new State("Confirm");
        State ITI = new State("ITI");

        AddActiveStates(new List<State> { Init, Blink, Shrink, Check, Calibrate, Confirm, ITI });

        Add_ControlLevel_InitializationMethod(() =>
        {
            DefineCalibPoints();
            InitializeEyeTrackerSettings();

            // Create necessary variables to display text onto the Experimenter Display
            PlayerViewPanel = new PlayerViewPanel();
            PlayerViewPanelGO = GameObject.Find("MainCameraCopy");

            // Assign UI Circles for the calib circles if not yet created
            if (CalibBigCircle == null)
                CalibBigCircle = new USE_Circle(GC_CanvasGO.GetComponent<Canvas>(), Vector3.zero, MaxCircleScale, "CalibrationBigCircle");
            
            // Create a container for the calibration results
            if (ResultContainer == null)
            {
                ResultContainer = new GameObject("ResultContainer", typeof(Canvas));
                ResultContainer.transform.parent = GC_CanvasGO.GetComponent<Canvas>().transform;
                ResultContainer.GetComponent<RectTransform>().anchorMin = Vector3.zero;
                ResultContainer.GetComponent<RectTransform>().anchorMax = Vector3.zero;
                ResultContainer.GetComponent<RectTransform>().localPosition = Vector3.zero;
                ResultContainer.GetComponent<RectTransform>().anchoredPosition = Vector2.zero; // Have to adjust local position in addition to anchored, becauses anchored Position is only Vector2, the z is off -SD
                ResultContainer.GetComponent<RectTransform>().localScale = new Vector3(1f,1f,1f);
                ResultContainer.GetComponent<RectTransform>().pivot = Vector3.zero;
            }

            // Create an object to store any information for the Experimenter
            if (InstructionsGO == null)
                InstructionsGO = PlayerViewPanel.CreateTextObject("Calibration Instructions", "", Color.black, Vector3.zero, new Vector2(2, 2), PlayerViewPanelGO.transform);
            
            // **USED FOR DEBUGGING, DELETE ONCE DONE
            PlayerTextGO = PlayerViewPanel.CreateTextObject("PlayerText", "Gaze Location", Color.black, new Vector2(960, 540), new Vector2(2, 2), GC_CanvasGO.transform);

            /* DISCUSS IF WE WANT TO INPUT OR USE THE SYSTEM GENERATED DISPLAY - SD
            screenWidth = (MonitorDetails.CmSize.x)*10;
            screenHeight = (MonitorDetails.CmSize.y)*10;*/
            MonitorDetails = new MonitorDetails(new Vector2(1920, 1080), new Vector2(43.5f, 24.0f));
        });

        SetupTrial.AddInitializationMethod(() =>
        {
            InitializeExperimenterDisplayInstructions();
        });

        SetupTrial.SpecifyTermination(()=> true, Init);

        if (SpoofGazeWithMouse)
        {
            SelectionHandler = SelectionTracker.SetupSelectionHandler("trial", "MouseHover", Init, ITI);
        }
        else
        {
            SelectionHandler = SelectionTracker.SetupSelectionHandler("trial", "GazeSelection", Init, ITI);
        }

        Init.AddUpdateMethod(() =>
        {
            // Define the number of calibration points given the following key codes (Space, 6, 5, 3, 1)
            if (InputBroker.GetKeyUp(KeyCode.Space))
                numCalibPoints = 9;
            
            else if (InputBroker.GetKeyUp(KeyCode.Alpha6))
                numCalibPoints = 6;
            
            else if (InputBroker.GetKeyUp(KeyCode.Alpha5))
                numCalibPoints = 5;
            
            else if (InputBroker.GetKeyUp(KeyCode.Alpha3))
                numCalibPoints = 3;
            
            else if (InputBroker.GetKeyUp(KeyCode.Alpha1))
                numCalibPoints = 1;
            
            // **USED FOR DEBUGGING, DELETE ONCE DONE
            PlayerTextGO.GetComponent<UnityEngine.UI.Text>().text = SelectionHandler.CurrentInputLocation().ToString();
            PlayerTextGO.SetActive(true);
            // **
        });
        
        Init.SpecifyTermination(() => numCalibPoints != 0, Blink);
        
        Init.AddDefaultTerminationMethod(() =>
        {
            // Turn off the Experimenter Display Instructions after a selection has been made
            if (InstructionsGO != null)
                InstructionsGO.SetActive(false);
            
            // Only enter Calibration if an eyetracker is being used
            if (!SpoofGazeWithMouse)
                ScreenBasedCalibration.EnterCalibrationMode();

            // Assign the correct calibration points given the User's selection
            DefineCalibPoints(numCalibPoints);
        });

        //----------------------------------------------------- BLINK THE CALIBRATION POINT -----------------------------------------------------
        
        Blink.AddInitializationMethod(() =>
        {
            // Initialize the Calibration Point at Max Scale
            InitializeCalibPoint();
            blinkTimer = 0;
            // Reset variables relating to calibration completion
            currentCalibrationPointFinished = false;
            keyboardOverride = false;
        });

        Blink.AddUpdateMethod(() =>
        {
            // Blinks the current calibration point until the acceptable calibration is met or keyboard override is triggered
            BlinkCalibrationPoint(CalibBigCircle.CircleGO);
            keyboardOverride |= InputBroker.GetKeyDown(KeyCode.Space);

            // **USED FOR DEBUGGING, DELETE ONCE DONE
            PlayerTextGO.GetComponent<UnityEngine.UI.Text>().text = SelectionHandler.CurrentInputLocation().ToString();
            // **

        });

        Blink.SpecifyTermination(() => keyboardOverride || InCalibrationRange(), Shrink);

        //----------------------------------------------------- SHRINK THE CALIBRATION POINT -----------------------------------------------------
        
        Shrink.AddInitializationMethod(() =>
        {
            elapsedShrinkDuration = 0;
        });

        Shrink.AddUpdateMethod(() =>
        {
            ShrinkGameObject(CalibBigCircle.CircleGO, MinCircleScale, ShrinkDuration);
        });

        Shrink.SpecifyTermination(() => elapsedShrinkDuration > ShrinkDuration, Check, () =>
        {
            // Make sure that the Scale is set to the min scale
            CalibBigCircle.SetCircleScale(MinCircleScale);
        });

        Shrink.SpecifyTermination(() => !InCalibrationRange() && elapsedShrinkDuration != 0, Blink);

        //----------------------------------------------------- CHECK CALIBRATION READINESS -----------------------------------------------------
        
        Check.AddInitializationMethod(() =>
        {
            keyboardOverride = false;
            TrialSummaryString = "Press Space to Override and Calibrate even if Gaze is not in an Acceptable Range";
        });
        
        Check.AddUpdateMethod(() => keyboardOverride |= InputBroker.GetKeyDown(KeyCode.Space));
        
        Check.SpecifyTermination(() => keyboardOverride || InCalibrationRange(), Calibrate, () =>
        {
            currentNormPoint = calibPointsADCS[CalibNum];
            TrialSummaryString =  $"Calibration Beginning at {calibPointsADCS[CalibNum].ToString()}";
        });

        //-------------------------------------------------------- CALIBRATE GAZE POINT --------------------------------------------------------
       
        Calibrate.AddInitializationMethod(() =>
        {
            keyboardOverride = false;
            CalibBigCircle.CircleGO.GetComponent<UnityEngine.UI.Extensions.UICircle>().color = Color.green;

        });
       
        Calibrate.AddUpdateMethod(() =>
        {
            // Determines if the collected point contains valid gaze Data
            if(!SpoofGazeWithMouse)
                DetermineCollectDataStatus(currentNormPoint);
            keyboardOverride |= InputBroker.GetKeyDown(KeyCode.Space);
        });
     
        Calibrate.SpecifyTermination(() => currentCalibrationPointFinished | keyboardOverride, Delay, () =>
        {
            // Collects eye tracking data at the current calibration point, computes the calibration settings, and applies them to the eye tracker.
            if (!SpoofGazeWithMouse)
            {
                CalibrationResult = ScreenBasedCalibration.ComputeAndApply();
                TrialSummaryString = string.Format("Compute and Apply Returned <b>{0}</b> and collected at <b>{1}</b> points.", CalibrationResult.Status, CalibrationResult.CalibrationPoints.Count);
            }

            StateAfterDelay = Confirm;

            // Assign a 3 Second delay following calibration to allow the sample to be properly recorded
            if (!SpoofGazeWithMouse)
                DelayDuration = 3f;
            else
                DelayDuration = 0;
        });

        //---------------------------------------------------- CONFIRM CALIBRATION RESULTS ----------------------------------------------------

        Confirm.AddInitializationMethod(() =>
        {
            pointFinished = false;
            recalibPoint = false;
            assessTime = 5f;

            if (CalibNum == calibPointsADCS.Length - 1)
                OnFinalPoint = true;
            else
                OnFinalPoint = false;

            if (!SpoofGazeWithMouse)
            {
                // Plots sample points to the Result Container, if they exist for the current calibration point
                PlotSamplePoints();

                if (ResultContainer.transform.GetChildCount() > 0)
                {
                    TrialSummaryString = $"Calibration Results Displayed at {calibPointsADCS[CalibNum].ToVector2().ToString()}";
                }
                else
                {
                    TrialSummaryString = $"No Samples Collected at this Calibration Point: {calibPointsADCS[CalibNum].ToVector2().ToString()}";
                }
            }
            

            if (SyncBoxController != null)
            {
                // Provide reward during the Confirm state based off values in the BlockDef
                SyncBoxController.SendRewardPulses(CurrentTrialDef.NumPulses, CurrentTrialDef.PulseSize);
            }
        });

        Confirm.AddUpdateMethod(() =>
        {
            if (InputBroker.GetKeyDown(KeyCode.Equals))
            {
                //  All calibration points have been displayed and the final one has been validated
                if (CalibNum == calibPointsADCS.Length - 1)
                    calibrationFinished = true;
                else
                    pointFinished = true;
            }
            else if (InputBroker.GetKeyDown(KeyCode.Minus))
            {
                // User selected to recalibrate current point, sample data is discarded and return to Blink
                if(!SpoofGazeWithMouse)
                    ScreenBasedCalibration.DiscardData(currentNormPoint);
                recalibPoint = true;
            }
        });

        // Dictates the subsequent state given the outcome of the User validation
        Confirm.SpecifyTermination(() => recalibPoint, Blink);
        
        Confirm.SpecifyTermination(() => pointFinished, Blink, ()=> { CalibNum++; });

        Confirm.SpecifyTermination(()=> calibrationFinished, ITI);
        
        Confirm.AddTimer(() => assessTime, Delay, ()=>
        {
            DelayDuration = 0;

            if (!OnFinalPoint)
            {
                StateAfterDelay = Blink;
                CalibNum++;
                TrialSummaryString = $"Timed out of assessment, calibration point is considered valid and continuing on to point {CalibNum}.";
            }
            else
            {
                StateAfterDelay = ITI;
                TrialSummaryString = "Timed out of assessment, calibration point is considered valid and calibration is complete.";
            }
        });

        Confirm.AddUniversalTerminationMethod(() =>
        {
            // Set the calibration point to inactive at the end of confirming
            CalibBigCircle.CircleGO.SetActive(false);
            DestroyChildren(ResultContainer);
        });

        ITI.AddInitializationMethod(() =>
        {
            // Leave calibration mode once the user has confirmed all points
            if(!SpoofGazeWithMouse)
                ScreenBasedCalibration.LeaveCalibrationMode();
            DestroyChildren(ResultContainer);
        });

        ITI.SpecifyTermination(() => true, FinishTrial);

    }

    // ---------------------------------------------------------- METHODS ----------------------------------------------------------
    private void ShrinkGameObject(GameObject gameObject, float targetSize, float shrinkDuration)
    {
        Vector3 startingScale = gameObject.transform.localScale;
        Vector3 finalScale = new Vector3(targetSize, targetSize, targetSize);
        gameObject.GetComponent<UnityEngine.UI.Extensions.UICircle>().color = Color.red;
        gameObject.SetActive(true);
        gameObject.transform.localScale = Vector3.Lerp(startingScale, finalScale, elapsedShrinkDuration / shrinkDuration);
        elapsedShrinkDuration += Time.deltaTime;
    }
    void DefineCalibPoints(int nPoints)
    {
        switch (nPoints)
        {
            case 9:
                calibPointsADCS = ninePoints;
                acceptableCalibrationDistance = Vector2.Distance(ADCSToScreen(ninePoints[0]), ADCSToScreen(ninePoints[1])) / 2;

                break;
            case 6:
                calibPointsADCS = new NormalizedPoint2D[6] {
                ninePoints [0],
                ninePoints [1],
                ninePoints [2],
                ninePoints [3],
                ninePoints [4],
                ninePoints[5]};
                acceptableCalibrationDistance = Vector2.Distance(ADCSToScreen(ninePoints[0]), ADCSToScreen(ninePoints[1])) / 2;
                break;
            case 5:
                calibPointsADCS = new NormalizedPoint2D[5] {
                ninePoints [0],
                ninePoints [2],
                ninePoints [4],
                ninePoints [6],
                ninePoints [8]};
                acceptableCalibrationDistance = Vector2.Distance(ADCSToScreen(ninePoints[0]), ADCSToScreen(ninePoints[4])) / 2;
                break;
            case 3:
                calibPointsADCS = new NormalizedPoint2D[3]{
                ninePoints [3],
                ninePoints [4],
                ninePoints [5] };
                acceptableCalibrationDistance = Vector2.Distance(ADCSToScreen(ninePoints[0]), ADCSToScreen(ninePoints[1])) / 2;
                break;
            case 1:
                NormalizedPoint2D[] originalPoints = new NormalizedPoint2D[numCalibPoints];
                switch (numCalibPoints)
                {
                    case 9:
                        originalPoints = ninePoints;
                        break;
                    case 5:
                        originalPoints = new NormalizedPoint2D[5] {
                    ninePoints [0],
                    ninePoints [2],
                    ninePoints [4],
                    ninePoints [6],
                    ninePoints [8]
                };
                        break;
                    case 3:
                        originalPoints = new NormalizedPoint2D[3] {
                    ninePoints [3],
                    ninePoints [4],
                    ninePoints [5]
                };
                        break;
                }
                calibPointsADCS = new NormalizedPoint2D[1] { originalPoints[recalibratePoint - 1] };
                break;
        }
    }
    private void BlinkCalibrationPoint(GameObject go)
    {
        blinkTimer += Time.deltaTime;

        if (go.activeSelf && (blinkTimer > blinkOnDuration))
        {
            blinkTimer = 0;
            go.SetActive(false);
        }
        else if (!go.activeSelf && (blinkTimer > blinkOffDuration))
        {
            blinkTimer = 0;
            go.SetActive(true);
        }
    }
    public void DetermineCollectDataStatus(NormalizedPoint2D point)
    {
        CalibrationStatus status = ScreenBasedCalibration.CollectData(point);
        
        if (status.Equals(CalibrationStatus.Success))
        {
            // Done calibrating the point if successful
            currentCalibrationPointFinished = true;
        }
        else if (status.Equals(CalibrationStatus.Failure))
        {
            // Continue calibrating the point if failure
            currentCalibrationPointFinished = false;
        }
        else //unkown message type
        {
            currentCalibrationPointFinished = false;
        }
    }
    
    public Vector2 ADCSToScreen(NormalizedPoint2D normADCSGazePoint)
    {
        Vector2 adcsGazePoint = normADCSGazePoint.ToVector2();
        Debug.Log("THESE ARE THE MONITOR DETAILS X PIXELS: " + MonitorDetails.PixelResolution.x);
        float x = adcsGazePoint.x * MonitorDetails.PixelResolution.x;
        float y = MonitorDetails.PixelResolution.y - (adcsGazePoint.y * MonitorDetails.PixelResolution.y);
        return new Vector2(x, y);
    }
    public Vector2 ScreenToADCS(Vector2 screenPoint)
    {
        float x = screenPoint.x / MonitorDetails.PixelResolution.x;
        float y = 1 - (screenPoint.y / MonitorDetails.PixelResolution.y);
        return new Vector2(x, y);
    }
    
    private void SetCanvasOriginToBottomLeft(GameObject GO)
    {
        RectTransform rectTransform = GO.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.zero;
    }

    private void PlotSamplePoints()
    {
        Debug.Log("CALIBRATION RESULTS POINTS: " + CalibrationResult.CalibrationPoints.Count);
        Tobii.Research.CalibrationPoint calibPoint = null;
        
        Debug.Log($"CURRENT NORM POINT: ({currentNormPoint.X}, {currentNormPoint.Y})");
        float tolerance = 0.01f; // set tolerance to a small value

        foreach (var Calib in CalibrationResult.CalibrationPoints)
        {
            if (Mathf.Abs(Calib.PositionOnDisplayArea.X - currentNormPoint.X) < tolerance &&
                Mathf.Abs(Calib.PositionOnDisplayArea.Y - currentNormPoint.Y) < tolerance)
            {
                calibPoint = Calib;
                break;
            }
        }
        if (calibPoint == null)
        {
            Debug.Log("DOESN'T EXIST $$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$");
            return;
        }
        else
        {
            Debug.Log($"CHOSEN NORM POINT: ({calibPoint.PositionOnDisplayArea.X}, {calibPoint.PositionOnDisplayArea.Y})");

            for (int i = 0; i <= (calibPoint.CalibrationSamples.Count >= 10 ? 10 : calibPoint.CalibrationSamples.Count); i++)
            {
                CalibrationSample sample = calibPoint.CalibrationSamples[i];
                // Record the positions of the Left and Right eye for each sample of the calibration point 
                Vector2 leftSamplePos = ADCSToScreen(sample.LeftEye.PositionOnDisplayArea);
                Vector2 rightSamplePos = ADCSToScreen(sample.RightEye.PositionOnDisplayArea);

                Debug.Log($"LEFT SAMPLE {i}: ({sample.LeftEye.PositionOnDisplayArea.X},{sample.LeftEye.PositionOnDisplayArea.Y})");
                Debug.Log($"LEFT SAMPLE CONVERTED TO SCREEN {i}: {leftSamplePos.ToString()}");
                Debug.Log($"RIGHT SAMPLE {i}: ({sample.RightEye.PositionOnDisplayArea.X},{sample.RightEye.PositionOnDisplayArea.Y})");
                Debug.Log($"RIGHT SAMPLE CONVERTED TO SCREEN {i}: {rightSamplePos.ToString()}");

                // Create objects to represent the sample on the experimenter display

                USE_Circle leftSampleCircle = new USE_Circle(ResultContainer.GetComponent<Canvas>(), leftSamplePos, 0.15f, $"L {i}");
                USE_Circle rightSampleCircle = new USE_Circle(ResultContainer.GetComponent<Canvas>(), rightSamplePos, 0.15f, $"R {i}");

                leftSampleCircle.CircleGO.GetComponent<UnityEngine.UI.Extensions.UICircle>().color = Color.blue;
                rightSampleCircle.CircleGO.GetComponent<UnityEngine.UI.Extensions.UICircle>().color = Color.red;

                leftSampleCircle.CircleGO.SetActive(true);
                rightSampleCircle.CircleGO.SetActive(true);

            }
        }
    }

    private void DefineCalibPoints()
    {
        //Define Calibration Points in ADCS (as proportion of the display)
        ninePoints = new NormalizedPoint2D[]
        {new NormalizedPoint2D(CalibPointsInset[0], CalibPointsInset[1]),
            new NormalizedPoint2D(0.5f, CalibPointsInset[1]),
            new NormalizedPoint2D(1f - CalibPointsInset[0], CalibPointsInset[1]),
            new NormalizedPoint2D(CalibPointsInset[0], 0.5f),
            new NormalizedPoint2D(0.5f, 0.5f),
            new NormalizedPoint2D(1f - CalibPointsInset[0], 0.5f),
            new NormalizedPoint2D(CalibPointsInset[0], 1f - CalibPointsInset[1]),
            new NormalizedPoint2D(0.5f, 1f - CalibPointsInset[1]),
            new NormalizedPoint2D(1f - CalibPointsInset[0], 1f - CalibPointsInset[1]),};
    }

    private void InitializeExperimenterDisplayInstructions() 
    {
        string calibrationInstructions = "<b>HOW TO CALIBRATE</b>" +
                                         "\n\nPress the Space Bar to Begin a 9 - Point Calibration" +
                                         "\n\n OR" +
                                         "\n\nPress 1, 3, 5, or 6 to Begin the Respective Point Calibration";
        Vector2 textLocation = playerViewPosition(new Vector2(960, 540), PlayerViewPanelGO.transform);
        
        InstructionsGO.GetComponent<RectTransform>().anchoredPosition = textLocation;
        InstructionsGO.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 200); //Adjusts the size of box that contains the text
        InstructionsGO.GetComponent<UnityEngine.UI.Text>().text = calibrationInstructions;
    }



    private void InitializeCalibPoint()
    {
        CalibBigCircle.CircleGO.GetComponent<UnityEngine.UI.Extensions.UICircle>().color = Color.black;
        CalibBigCircle.SetCircleScale(MaxCircleScale);
        currentADCSTarget = calibPointsADCS[CalibNum]; // get calib coordinates in ADCS space
        currentScreenTarget = ADCSToScreen(currentADCSTarget); // get calib coordinates in Screen space
        CalibBigCircle.CircleGO.GetComponent<RectTransform>().anchoredPosition = currentScreenTarget;
        CalibBigCircle.CircleGO.SetActive(true);
    }

    private bool InCalibrationRange()
    {
        return (Vector2.Distance((Vector2)SelectionHandler.CurrentInputLocation(), currentScreenTarget) < acceptableCalibrationDistance);
    }

    public override void ResetTrialVariables()
    {
        CalibNum = 0;
        numCalibPoints = 0;
        OnFinalPoint = false;

        recalibPoint = false;
        pointFinished = false;
        calibrationUnfinished = true;
        calibrationFinished = false;
    }

    public override void FinishTrialCleanup()
    {
        if(InstructionsGO != null)
            InstructionsGO.SetActive(false);
        
        if(CalibBigCircle != null)
            CalibBigCircle.CircleGO.SetActive(false);

        DestroyChildren(ResultContainer);
    }
    public void InitializeEyeTrackerSettings()
    {
        Debug.Log("EYETRACKER AVAILABLE? " + EyeTrackingOperations.FindAllEyeTrackers().Count);
        IEyeTracker = EyeTrackingOperations.FindAllEyeTrackers()[0];
        if (IEyeTracker == null)
        {
            Debug.LogError("Could not find the eye tracker.");
        }
        else
        {
            ScreenBasedCalibration = new ScreenBasedCalibration(IEyeTracker);
            EyeTracker = GameObject.Find("[EyeTracker]").GetComponent<EyeTracker>();
            // Sets the Display area to the info entered into the Tobii Pro Eye Tracker Manager Display Setup,
            // but updates with the Unity Editor Display sizing as well
            DisplayArea = IEyeTracker.GetDisplayArea();

            // INCLUDE INFO BELOW IF WE WANT TO ENTER AND SET THE DISPLAY AREA - SD

            /*Point3D topLeft = new Point3D(0, 0, 0);
            Point3D bottomLeft = new Point3D(0, -screenHeight, 0);
            Point3D topRight = new Point3D(screenWidth, 0, 0);

            DisplayArea = new DisplayArea(topLeft, bottomLeft, topRight);*/
            //IEyeTracker.SetDisplayArea(DisplayArea);
        }

    }
}
