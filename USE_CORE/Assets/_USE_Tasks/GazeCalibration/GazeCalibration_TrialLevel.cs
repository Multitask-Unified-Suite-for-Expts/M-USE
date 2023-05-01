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

public class GazeCalibration_TrialLevel : ControlLevel_Trial_Template
{
    public GazeCalibration_TrialDef CurrentTrialDef => GetCurrentTrialDef<GazeCalibration_TrialDef>();
    public GazeCalibration_TaskLevel CurrentTaskLevel => GetTaskLevel<GazeCalibration_TaskLevel>();

    public GameObject GC_CanvasGO;
    public USE_Circle USE_Circle;

    // Task Def Variables
    [HideInInspector] public String ContextExternalFilePath;
    [HideInInspector] public bool SpoofGazeWithMouse;
   
    [HideInInspector] public Vector3 SmallCirclePosition;
    [HideInInspector] public float SmallCircleSize;
    [HideInInspector] public Vector3 BigCirclePosition;
    [HideInInspector] public float BigCircleSize;

    //for calibration point definition
    [HideInInspector] public Vector2[] ninePoints, sixPoints;
    [HideInInspector] public int numCalibPoints;
    [HideInInspector] public float[] calibPointsInset = new float[2] { .1f, .15f };
    [HideInInspector] public Vector2[] calibPointsADCS;
    private ScreenTransformations screenTransformations;


    //Other Calibration Point Variables....?
    private float acceptableCalibrationDistance;
    [HideInInspector]
    public bool currentCalibrationPointFinished, calibrationUnfinished, calibrationFinished;
    private int recalibratePoint = 0;
    //private bool calibAssessment;
    private Vector2 currentScreenTarget;
    private Vector2 currentADCSTarget;
    private Vector3 moveVector;
    private Vector3 calibCircleStartPos;
    private int CalibNum;


    //Calibration Timing Variables
    private float epochStartTime;
    private float proportionOfMoveTime;
    private float proportionOfShrinkTime;
    private float calibCircleMoveTime = .75f;
    private float assessTime = 2.0f; //0.5f;
    private float calibCircleShrinkTime = 0.6f; //0.5f;//0.3f;
    private float calibTime = 0.3f;
    private float rewardTime = 0.5f;
    private float blinkOnDuration = 0.2f;
    private float blinkOffDuration = 0.1f;
    private float blinkStartTime = 0;

    //Calibration Sizing Variables
    private Vector3 bigCircleMaxScale = new Vector3(1.5f, 1.5f, 1f);
    private float bigCircleShrinkTargetSize = .1f;
    private float smallCircleSize = 0.15f;

    // Game Objects
    private USE_Circle CalibSmallCircle;
    private USE_Circle CalibBigCircle;

    // NEW Tobii SDK Variables
    private static DisplayArea DisplayArea;
    private EyeTracker EyeTracker;
    private IEyeTracker IEyeTracker;
    private NormalizedPoint2D currentNormPoint;
    private static ScreenBasedCalibration ScreenBasedCalibration;
    private CalibrationResult CalibrationResult;
    private Vector2? latestGazePosition;
    public MonitorDetails MonitorDetails;

    private float screenWidth, screenHeight;
    private GameObject ResultContainer;

    private bool recalibpoint = false;
    private bool resultAdded = false;
    private bool resultsDisplayed = false;
    private bool pointFinished = false;
    private bool keyboardOverride = false;



    // Selection Handling
    private SelectionTracking.SelectionTracker.SelectionHandler SelectionHandler;

    public override void DefineControlLevel()
    {
        //Define Calibration Points in ADCS (as proportion of the display)
        ninePoints = new Vector2[]
        {new Vector2(calibPointsInset[0], calibPointsInset[1]),
            new Vector2(0.5f, calibPointsInset[1]),
            new Vector2(1f - calibPointsInset[0], calibPointsInset[1]),
            new Vector2(calibPointsInset[0], 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(1f - calibPointsInset[0], 0.5f),
            new Vector2(calibPointsInset[0], 1f - calibPointsInset[1]),
            new Vector2(0.5f, 1f - calibPointsInset[1]),
            new Vector2(1f - calibPointsInset[0], 1f - calibPointsInset[1]),};
        
        
        sixPoints = new Vector2[6]
        {new Vector2(calibPointsInset[0], calibPointsInset[1]),
            new Vector2(0.5f, calibPointsInset[1]),
            new Vector2(1f - calibPointsInset[0], calibPointsInset[1]),
            new Vector2(calibPointsInset[0], 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(1f - calibPointsInset[0], 0.5f)};


        State Init = new State("Init");
        State Blink = new State("Blink");
        State Shrink = new State("Shrink");
        State Check = new State("Check");
        State Calibrate = new State("Calibrate");
        State Confirm = new State("Confirm");

        AddActiveStates(new List<State> { Init, Blink, Shrink, Check, Calibrate, Confirm });

        Add_ControlLevel_InitializationMethod(() =>
        {
            // screenTransformations = new ScreenTransformations();
            
            GazeTracker = new GazeTracker();
            Execute();

            screenWidth = MonitorDetails.PixelResolution.x;
            screenHeight = MonitorDetails.PixelResolution.y;
            

            RenderSettings.skybox = CreateSkybox(GetContextNestedFilePath(ContextExternalFilePath, "LinearDark", "LinearDark"), UseDefaultConfigs);
            
            // Assign UI Circles for the calib circles
            if (CalibSmallCircle == null)
                CalibSmallCircle = new USE_Circle(GC_CanvasGO.GetComponent<Canvas>(), SmallCirclePosition, SmallCircleSize, "CalibrationSmallCircle");
            if (CalibBigCircle == null)
                CalibBigCircle = new USE_Circle(GC_CanvasGO.GetComponent<Canvas>(), BigCirclePosition, BigCircleSize, "CalibrationBigCircle");
            
            GameObject ResultContainer = new GameObject("ResultContainer");
            ResultContainer.transform.parent = GC_CanvasGO.GetComponent<Canvas>().transform;
           
            SetCanvasOriginToBottomLeft(CalibSmallCircle.CircleGO);
            SetCanvasOriginToBottomLeft(CalibBigCircle.CircleGO);

        });
        SetupTrial.AddInitializationMethod(() =>
        {
            // Reset the number of points that have been calibrated at the start of the trial
            CalibNum = 0;

            calibrationUnfinished = true;
            calibrationFinished = false;

            ScreenBasedCalibration.EnterCalibrationMode(); // Tell eyetracker to begin calibration
            CalibBigCircle.CircleGO.GetComponent<UnityEngine.UI.Extensions.UICircle>().color = Color.black;
        });

        SetupTrial.SpecifyTermination(()=> true, Init);

        Init.AddInitializationMethod(() =>
        {
            CalibBigCircle.CircleGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(960, 540);
            NormalizedPoint2D preCalibPt = new NormalizedPoint2D(0.5f, 0.5f);
            CalibBigCircle.CircleGO.SetActive(true);
            // Collect data.
            CalibrationStatus status = ScreenBasedCalibration.CollectData(preCalibPt);
            if (status != CalibrationStatus.Success)
            {
                // Try again if it didn't go well the first time.
                // Not all eye tracker models will fail at this point, but instead fail on ComputeAndApply.
                ScreenBasedCalibration.CollectData(preCalibPt);
                ScreenBasedCalibration.DiscardData(preCalibPt);
            }
        });

        //------------------ DEFINE NUM CALIB POINTS GIVEN KEY CODES ----------------------------------
        Init.SpecifyTermination(() => InputBroker.GetKeyUp(KeyCode.Space), Blink, () => {
            numCalibPoints = 9;
            DefineCalibPoints(numCalibPoints);
        });
        Init.SpecifyTermination(() => InputBroker.GetKeyUp(KeyCode.Alpha9), Blink, () => {
            numCalibPoints = 9;
            DefineCalibPoints(numCalibPoints);
        });
        Init.SpecifyTermination(() => InputBroker.GetKeyUp(KeyCode.Alpha6), Blink, () => {
            numCalibPoints = 6;
            DefineCalibPoints(numCalibPoints);
        });
        Init.SpecifyTermination(() => InputBroker.GetKeyUp(KeyCode.Alpha5), Blink, () => {
            numCalibPoints = 5;
            DefineCalibPoints(numCalibPoints);
        });
        Init.SpecifyTermination(() => InputBroker.GetKeyUp(KeyCode.Alpha3), Blink, () => {
            numCalibPoints = 3;
            DefineCalibPoints(numCalibPoints);
        });
        Init.SpecifyTermination(() => InputBroker.GetKeyUp(KeyCode.Alpha1), Blink, () => {
            numCalibPoints = 1;
            DefineCalibPoints(numCalibPoints);
        });
        
        //------------------BLINK THE CALIBRATION POINT-------------------------------------
        Blink.AddInitializationMethod(() =>
        {
            //Set the calibration point to max size
            CalibBigCircle.CircleGO.transform.localScale = bigCircleMaxScale;
            
            currentADCSTarget = calibPointsADCS[CalibNum]; // get calib coordinates in ADCS space
            currentScreenTarget = ADCSToScreen(currentADCSTarget); // get calib coordinates in Screen space
            CalibBigCircle.CircleGO.GetComponent<RectTransform>().anchoredPosition = currentScreenTarget;

            
            currentCalibrationPointFinished = false;
            keyboardOverride = false;
        });

        Blink.AddUpdateMethod(() =>
        {
            // Blinks the current calibration point until the acceptable calibration is met or keyboard overrid triggered
            blinkStartTime = CheckBlink(blinkStartTime, CalibBigCircle.CircleGO);
            keyboardOverride |= InputBroker.GetKeyDown(KeyCode.Space);

            latestGazePosition = GetGazeLocation();
        });

        Blink.SpecifyTermination(() =>
            keyboardOverride || (latestGazePosition != null && Vector2.Distance((Vector2)latestGazePosition, currentScreenTarget) < acceptableCalibrationDistance),
            Shrink, () => CalibBigCircle.CircleGO.SetActive(true));

        //----------------- SHRINK THE CALIBRATION POINT ----------------------------
        Shrink.AddInitializationMethod(() =>
        {
            CalibSmallCircle.CircleGO.transform.localScale = new Vector3(smallCircleSize, smallCircleSize, smallCircleSize);
            CalibSmallCircle.CircleGO.GetComponent<RectTransform>().anchoredPosition = currentScreenTarget;
            CalibSmallCircle.CircleGO.GetComponent<UnityEngine.UI.Extensions.UICircle>().color = Color.red;
            CalibSmallCircle.CircleGO.SetActive(true);
            proportionOfShrinkTime = 0;
        });

        Shrink.AddUpdateMethod(() =>
        {
            ShrinkCalibCircle(Shrink.TimingInfo.StartTimeAbsolute);
            latestGazePosition = GetGazeLocation();
        });
        Shrink.SpecifyTermination(() => proportionOfShrinkTime == 1, Check);
        Shrink.SpecifyTermination(() => !keyboardOverride & (latestGazePosition != null && Vector2.Distance((Vector2)latestGazePosition, currentScreenTarget) > acceptableCalibrationDistance), Blink);
      
        //----------------------- CHECK THE READINESS TO CALIBRATE -------------------------
        Check.AddInitializationMethod(() => keyboardOverride = false);
        Check.AddUpdateMethod(() => keyboardOverride |= InputBroker.GetKeyDown(KeyCode.Space));
        Check.SpecifyTermination(() => keyboardOverride || (latestGazePosition != null && Vector2.Distance((Vector2)latestGazePosition, currentScreenTarget) < acceptableCalibrationDistance), Calibrate);

        //------------------------ CALIBRATE THE GAZE INPUT --------------------------------
        Calibrate.AddInitializationMethod(() =>
        {
            // Convert to NormalizedPoint2D for the Tobii Eyetracker to interpret (same ADCS space)
            currentNormPoint = new NormalizedPoint2D(calibPointsADCS[CalibNum].x, calibPointsADCS[CalibNum].y);
        });
        Calibrate.AddUpdateMethod(() =>
        {
            TobiiReadCalibrationMsg(currentNormPoint);
            keyboardOverride |= InputBroker.GetKeyDown(KeyCode.Space);
        });
        Calibrate.SpecifyTermination(() => currentCalibrationPointFinished | keyboardOverride, Confirm, () =>
        {
            // The ScreenBasedCalibration.ComputeAndApply() method collects eye tracking data at the current calibration point, computes the calibration settings, and applies them to the eye tracker.
            // The calibration point and its associated data are then added to the CalibrationResults.CalibrationPoints property.
            CalibrationResult = ScreenBasedCalibration.ComputeAndApply();
            Debug.Log(string.Format("##########Compute and apply returned {0} and collected at {1} points.",CalibrationResult.Status, CalibrationResult.CalibrationPoints.Count));

        });

        Confirm.AddInitializationMethod(() =>
        {
            recalibpoint = false;
            resultAdded = false;
            resultsDisplayed = false;
            pointFinished = false;
            CalibSmallCircle.CircleGO.GetComponent<UnityEngine.UI.Extensions.UICircle>().color = Color.blue;

          // resultsDisplayed = DisplayCalibrationResults(); // just added\
          if (CalibrationResult.CalibrationPoints.Count != 0)
            PlotSamplePoints();
            if (SyncBoxController != null)
            {
               // SyncBoxController.AddToSend("RWD " + rewardTime * 10000);
               // ARE WE GIVING REWARD AFTER CALIBRATION, I DON'T GET IT??? -SD
            }
        });
        Confirm.AddUpdateMethod(() =>
        {/*
            if (mainLevel.externalDataManager.calibMsgResult.Length > 0 && !resultAdded)
            {
                resultAdded = RecordCalibrationResult(mainLevel.externalDataManager.calibMsgResult);
            }*/
            if (CalibNum == calibPointsADCS.Length - 1 && !resultsDisplayed) //put this here instead of init in case calib message takes more than a frame to send
            {
                ClearCalibVisuals();
           //     resultsDisplayed = DisplayCalibrationResults();
            }
            if (InputBroker.anyKey)
            {
                //string commandString = Input.inputString;
                if (InputBroker.GetKeyDown(KeyCode.Space) && CalibNum == calibPointsADCS.Length - 1)
                {
                    calibrationFinished = true;
                }
                else if (InputBroker.GetKeyDown(KeyCode.Equals))
                {
                    pointFinished = true;
                }
                else if (InputBroker.GetKeyDown(KeyCode.Minus))
                {
                    DiscardCalibrationPoint(currentNormPoint);
                    recalibpoint = true;
                }
              /*  REIMPLEMENT ************* I DIDN'T KNOW HOW TO GET GENERIC INPUT KEY 
               *  
               *  else if (int.TryParse(InputBroker.GetKey(), out recalibratePoint))
                {
                    if (recalibratePoint > 0 & recalibratePoint < 10)
                    {
                        DiscardCalibrationPoint(recalibratePoint - 1);
                        calibCount = -1; //set to -1 because the termination includes calibCount++
                        ClearCalibResults();
                        DefineCalibPoints(1);
                        //calibSuccess = true;
                    }
                }*/
            }
            if (Time.time - Confirm.TimingInfo.StartTimeAbsolute > assessTime)
            {
                pointFinished = true;
                CalibBigCircle.CircleGO.SetActive(false);
                CalibSmallCircle.CircleGO.SetActive(false);
            }
        });
        Confirm.SpecifyTermination(() => CalibNum < calibPointsADCS.Length - 1 && pointFinished, Blink, () => {//!calibrationFinished, blink, ()=> {
            CalibNum++;
            CalibBigCircle.CircleGO.SetActive(false);
            CalibSmallCircle.CircleGO.SetActive(false);
        });
        Confirm.SpecifyTermination(() => recalibpoint, Blink, () => {
            CalibBigCircle.CircleGO.SetActive(false);
            CalibSmallCircle.CircleGO.SetActive(false);
        });
        Confirm.SpecifyTermination(() => calibrationFinished, FinishTrial, () =>
        {
            calibrationUnfinished = false;
            ScreenBasedCalibration.LeaveCalibrationMode();
            if (SyncBoxController != null)
            {
              //  EventCodeManager.SendCodeImmediate(103); **UPDATE AND ASK WHAT EVENT CODE WE WANT TO USE - SD **
            }

            // ** UPDATE AND ASK WHAT KIND OF DATA WE WANT TO BE STORING - SD **
    /*        if (CurrentTaskLevel.StoreData)
            {
                mainLevel.udpManager.SendString("ET###save_calibration_textfile");
                mainLevel.udpManager.SendString("ET###save_calibration_binfile");
            }
            mainLevel.WriteFrameByFrameData();
            mainLevel.TimesRunCalibration++;*/
        });
        Confirm.AddDefaultTerminationMethod(() =>
        {
            DestroySampleResults();
            Debug.Break();
        });

    }

    private void DiscardCalibrationPoint(NormalizedPoint2D normalizedPoint2D)
    {
        ScreenBasedCalibration.DiscardData(normalizedPoint2D);
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
                calibPointsADCS = sixPoints;
                acceptableCalibrationDistance = Vector2.Distance(ADCSToScreen(sixPoints[0]), ADCSToScreen(sixPoints[1])) / 2;
                break;
            case 5:
                calibPointsADCS = new Vector2[5] {
                ninePoints [0],
                ninePoints [2],
                ninePoints [4],
                ninePoints [6],
                ninePoints [8]};
                acceptableCalibrationDistance = Vector2.Distance(ADCSToScreen(ninePoints[0]), ADCSToScreen(ninePoints[4])) / 2;
                break;
            case 3:
                calibPointsADCS = new Vector2[3]{
                ninePoints [3],
                ninePoints [4],
                ninePoints [5] };
                acceptableCalibrationDistance = Vector2.Distance(ADCSToScreen(ninePoints[0]), ADCSToScreen(ninePoints[1])) / 2;
                break;
            case 1:
                Vector2[] originalPoints = new Vector2[numCalibPoints];
                switch (numCalibPoints)
                {
                    case 9:
                        originalPoints = ninePoints;
                        break;
                    case 5:
                        originalPoints = new Vector2[5] {
                    ninePoints [0],
                    ninePoints [2],
                    ninePoints [4],
                    ninePoints [6],
                    ninePoints [8]
                };
                        break;
                    case 3:
                        originalPoints = new Vector2[3] {
                    ninePoints [3],
                    ninePoints [4],
                    ninePoints [5]
                };
                        break;
                }
                calibPointsADCS = new Vector2[1] { originalPoints[recalibratePoint - 1] };
                break;
        }
    }
    private void ClearCalibVisuals()
    {/*
        for (int i = 0; i < calibResult.results.Count; i++)gla
        {
            Destroy(calibResult.results[i].resultDisplay);
        }*/
    }
    private void ClearCalibResults()
    {
        ClearCalibVisuals();
   //     calibResult = new EyeTrackerData_Namespace.CalibrationResult();
    }
    void ShrinkCalibCircle(float startTime)
    {
        proportionOfShrinkTime = (Time.time - startTime) / calibCircleShrinkTime;
        if (proportionOfShrinkTime > 1)
        {
            proportionOfShrinkTime = 1;
            CalibBigCircle.CircleGO.transform.localScale = new Vector3(bigCircleShrinkTargetSize, bigCircleShrinkTargetSize, bigCircleShrinkTargetSize);
        }
        else
        {
            float newScale = bigCircleMaxScale[0] * (1 - ((1 - bigCircleShrinkTargetSize) * proportionOfShrinkTime));
            CalibBigCircle.CircleGO.transform.localScale = new Vector3(newScale, newScale, newScale);
        }
    }
    private float CheckBlink(float blinkStartTime, GameObject circle)
    {
        if (circle.activeInHierarchy && Time.time - blinkStartTime > blinkOnDuration)
        {
            circle.SetActive(false);
            blinkStartTime = Time.time;
        }
        else if (!circle.activeInHierarchy && Time.time - blinkStartTime > blinkOffDuration)
        {
            circle.SetActive(true);
            blinkStartTime = Time.time;
        }
        return blinkStartTime;
    }
  /*  public bool DisplayCalibrationResults()
    {
        // Create empty lists to store left and right eye gaze positions
        List<Vector2> leftEyeSamples = new List<Vector2>();
        List<Vector2> rightEyeSamples = new List<Vector2>();

        foreach (Tobii.Research.CalibrationPoint point in CalibrationResult.CalibrationPoints)
        {
            Debug.Log("POINT SAMPLES COUNT: " + point.CalibrationSamples.Count);
            foreach (CalibrationSample sample in point.CalibrationSamples)
            {
                // Add the left and right eye gaze positions to their respective lists
                var adcsPointLeft = new Vector2(sample.LeftEye.PositionOnDisplayArea.X, sample.LeftEye.PositionOnDisplayArea.Y);
                leftEyeSamples.Add(ConvertADCSGazePointToVector2(adcsPointLeft));

                var adcsPointRight = new Vector2(sample.RightEye.PositionOnDisplayArea.X, sample.RightEye.PositionOnDisplayArea.Y);
                rightEyeSamples.Add(ConvertADCSGazePointToVector2(adcsPointRight));
            }
        }

        // Convert the lists to arrays if needed
        Vector2[] leftEyeSamplesArray = leftEyeSamples.ToArray();
        Vector2[] rightEyeSamplesArray = rightEyeSamples.ToArray();

        if (leftEyeSamplesArray.Length == calibPointsADCS.Length)
            return true;
        else
            return false;
    }*/


    public void TobiiReadCalibrationMsg(NormalizedPoint2D point)
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
    public void Execute()
    {
        IEyeTracker = EyeTrackingOperations.FindAllEyeTrackers()[0];
        if (IEyeTracker == null)
        {
            Debug.LogError("Could not find the eye tracker.");
        }
        else
        {
            ScreenBasedCalibration = new ScreenBasedCalibration(IEyeTracker);
            EyeTracker = GameObject.Find("[EyeTracker]").GetComponent<EyeTracker>();
            DisplayArea = IEyeTracker.GetDisplayArea();
        }

    }

    private Vector2? GetGazeLocation()
    {
        // Get the most recent gaze data point
        var gazeData = EyeTracker?.LatestGazeData;
        Vector2? screenPoint = null;
        if (gazeData != null)
        {

            // Get the gaze points for each eye
            var leftGazePoint = gazeData.Left.GazePointOnDisplayArea;
            var rightGazePoint = gazeData.Right.GazePointOnDisplayArea;

            // Check if both eyes are valid
            if (gazeData.Left.GazePointValid && gazeData.Right.GazePointValid)
            {
                // Average the gaze points from both eyes
                var combinedGazePoint = new Vector2(
                    (leftGazePoint.x + rightGazePoint.x) / 2f,
                    (leftGazePoint.y + rightGazePoint.y) / 2f);

                screenPoint = ADCSToScreen(combinedGazePoint);
            }
            else if (gazeData.Left.GazePointValid)
            {
                // Use the gaze point from the left eye
                screenPoint = ADCSToScreen(leftGazePoint);
            }
            else if (gazeData.Right.GazePointValid)
            {
                // Use the gaze point from the right eye
                screenPoint = ADCSToScreen(leftGazePoint);
            }

            return screenPoint;
        }
        return null;
    }

    public Vector2 ADCSToScreen(Vector2 adcsGazePoint)
    {
        float x = adcsGazePoint.x * screenWidth;
        float y = (1 - adcsGazePoint.y) * screenHeight;
        return new Vector2(x, y);
    }
    public Vector2 ScreenToADCS(Vector2 screenPoint)
    {
        float x = screenPoint.x / screenWidth;
        float y = 1 - (screenPoint.y / screenHeight);
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
        

        Tobii.Research.CalibrationPoint calibPoint = CalibrationResult.CalibrationPoints[CalibNum];
        for (int i = 0; i < calibPoint.CalibrationSamples.Count; i++)
        {
            CalibrationSample sample = calibPoint.CalibrationSamples[i];

            // Record the positions of the Left and Right eye for each sample of the calibration point 
            Vector2 leftSamplePos = ADCSToScreen(new Vector2(sample.LeftEye.PositionOnDisplayArea.X, sample.LeftEye.PositionOnDisplayArea.Y));
            Vector2 rightSamplePos = ADCSToScreen( new Vector2(sample.RightEye.PositionOnDisplayArea.X, sample.RightEye.PositionOnDisplayArea.Y));
            
            // Create objects to represent the sample on the experimenter display
            USE_Circle leftSampleCircle = new USE_Circle(GC_CanvasGO.GetComponent<Canvas>(), leftSamplePos, 15f, $"L {i}");
            USE_Circle rightSampleCircle = new USE_Circle(GC_CanvasGO.GetComponent<Canvas>(), rightSamplePos, 15f, $"R {i}");
            
            leftSampleCircle.CircleGO.GetComponent<UnityEngine.UI.Extensions.UICircle>().color = Color.blue;
            rightSampleCircle.CircleGO.GetComponent<UnityEngine.UI.Extensions.UICircle>().color = Color.red;

            leftSampleCircle.CircleGO.SetActive(true);
            rightSampleCircle.CircleGO.SetActive(true);

        }
    }

    private Vector2 GetAveragePosition(NormalizedPoint2D leftEye, NormalizedPoint2D rightEye)
    {
        var avgPosition = new Vector2((leftEye.X + rightEye.X) / 2f, (leftEye.Y + rightEye.Y) / 2f);
        var avgScreenPosition = ADCSToScreen(avgPosition);
        return avgScreenPosition;
    }

    private void DestroySampleResults()
    {
        foreach (Transform child in GC_CanvasGO.transform)
        {
            // Check if the child object's name starts with "L" or "R"
            if (child.name.StartsWith("L") || child.name.StartsWith("R"))
            {
                // Destroy the child object
                Destroy(child.gameObject);
            }
        }
    }

}
