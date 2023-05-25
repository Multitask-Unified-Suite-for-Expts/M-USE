using EyeTrackerData_Namespace;
using GazeCalibration_Namespace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Tobii.Research;
using Tobii.Research.Unity;
using UnityEditor;
using UnityEngine;
using USE_DisplayManagement;
using USE_ExperimentTemplate_Trial;
using USE_States;
using USE_UI;
using static Tobii.Research.Unity.CalibrationThread;

public class GazeCalibration_TrialLevel : ControlLevel_Trial_Template
{
    // MUSE Common Variables
    public GazeCalibration_TrialDef CurrentTrialDef => GetCurrentTrialDef<GazeCalibration_TrialDef>();
    public GazeCalibration_TaskLevel CurrentTaskLevel => GetTaskLevel<GazeCalibration_TaskLevel>();


    public GameObject GC_CanvasGO;
    private SelectionTracking.SelectionTracker.SelectionHandler SelectionHandler;

    // Task Def - Defined Variables
    [HideInInspector] public bool SpoofGazeWithMouse;
    [HideInInspector] public float[] CalibPointsInset;
    [HideInInspector] public float MaxCircleScale;
    [HideInInspector] public float MinCircleScale;
    [HideInInspector] public float ShrinkDuration;

    // Inherited from the Session Level...
    [HideInInspector] public String ContextExternalFilePath;
    public MonitorDetails MonitorDetails;

    // Calibration Point Definition
    [HideInInspector] public NormalizedPoint2D[] allCalibPoints;
    [HideInInspector] public NormalizedPoint2D[] calibPointsADCS;
    [HideInInspector] public int numCalibPoints;
    private float acceptableCalibrationDistance;
    private NormalizedPoint2D currentADCSTarget;
    private Vector2 currentScreenPixelTarget;
    private int calibNum;

    // Blink Calibration Point Variables
    private float elapsedShrinkDuration;
    private float blinkOnDuration = 0.2f;
    private float blinkOffDuration = 0.1f;
    private float blinkTimer = 0;


    // Calibration Assessment Variables
    [HideInInspector] public bool currentCalibrationPointFinished;
    [HideInInspector] public bool calibrationFinished;
    private bool pointFinished;
    private bool recalibPoint;
    private NormalizedPoint2D currentNormPoint;
    private float assessTime = 5f;
    private Vector2? latestGazePosition;
    private bool keyboardOverride = false;

    // Game Objects
    private USE_Circle CalibCircle;
    private GameObject PlayerViewPanelGO;
    private PlayerViewPanel PlayerViewPanel;
    private GameObject PlayerTextGO;
    private GameObject ResultContainer;
    
    // Tobii SDK Variables 
    private CalibrationResult CalibrationResult;
    public GameObject EyeTrackerPrefab;
    public GameObject TrackBoxPrefab;
    private IEyeTracker IEyeTracker;
    private EyeTracker EyeTracker;

    // USE_DisplayManagement Variables
    private DisplayCoordinate DisplayCoordinate;
    private ScreenDetails ScreenDetails;
    private USE_CoordinateConverter CoordinateConverter;


    // Gaze Data Samples
    private List<Vector2> LeftSamples = new List<Vector2>();
    private List<Vector2> RightSamples = new List<Vector2>();
    private List<float> LeftSampleDistances = new List<float>();
    private List<float> RightSampleDistances = new List<float>();
    private int[] RecalibCount;

    // Experimenter Display Text Variables
    private StringBuilder InfoString = new StringBuilder();
    private StringBuilder CurrentProgressString = new StringBuilder();
    private StringBuilder ResultsString = new StringBuilder();

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
            AssignCalibPositions();

            // Create necessary variables to display text onto the Experimenter Display
            PlayerViewPanel = new PlayerViewPanel();
            PlayerViewPanelGO = GameObject.Find("MainCameraCopy");

            // Assign UI Circles for the calib circles if not yet created
            if (CalibCircle == null)
                CalibCircle = new USE_Circle(GC_CanvasGO.GetComponent<Canvas>(), Vector3.zero, MaxCircleScale, "CalibrationBigCircle");
            
            // Create a container for the calibration results
            if (ResultContainer == null)
            {
                CreateResultContainer();
            }

            CoordinateConverter = TobiiEyeTrackerController.CoordinateConverter;

        });

        SetupTrial.AddInitializationMethod(() =>
        {
            if (!SpoofGazeWithMouse)
                InitializeEyeTrackerSettings();

            InfoString.Append("<b>Info</b>" 
                                + "\nPress <b>Space</b> to begin a <b>9</b> point calibration"
                                + "\nPress <b>6</b>, <b>5</b>, or <b>3</b> to begin the respective point calibration");
            SetTrialSummaryString();
            
            CalibCircle.CircleGO.GetComponent<RectTransform>().anchoredPosition = new Vector3 (0,0,0);
            CalibCircle.CircleGO.SetActive(true);


        });

        SetupTrial.SpecifyTermination(()=> (!SpoofGazeWithMouse? (ScreenBasedCalibration != null):true), Init, () =>
        {
            // Only enter Calibration if an eyetracker is being used
            if (!SpoofGazeWithMouse)
                ScreenBasedCalibration.EnterCalibrationMode();
        });
        var MouseHandler = SelectionTracker.SetupSelectionHandler("trial", "MouseHover", MouseTracker, Init, ITI);
        if (SpoofGazeWithMouse)
        {
            SelectionHandler = SelectionTracker.SetupSelectionHandler("trial", "MouseHover", MouseTracker, Init, ITI);
        }
        else
        {
            SelectionHandler = SelectionTracker.SetupSelectionHandler("trial", "GazeSelection", GazeTracker, Init, ITI);
        }

        Init.AddInitializationMethod(() =>
        {
            CalibCircle.CircleGO.SetActive(false);
        });

        Init.AddUpdateMethod(() =>
        {
            Debug.Log($"ACTIVE DISPLAY AREA TOP LEFT: {TobiiEyeTrackerController.DisplayArea.TopLeft.ToVector3().ToString("F3")}" +
                $" BOTTOM LEFT: {TobiiEyeTrackerController.DisplayArea.BottomLeft.ToVector3().ToString("F3")}" +
                $" BOTTOM TOP RIGHT: {TobiiEyeTrackerController.DisplayArea.TopRight.ToVector3().ToString("F3")}" +
                $" BOTTOM WIDTH: {TobiiEyeTrackerController.DisplayArea.Width}");

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
           // PlayerTextGO.GetComponent<UnityEngine.UI.Text>().text = SelectionHandler.CurrentInputLocation().ToString();
            //PlayerTextGO.SetActive(true);
            // **
        });
        
        Init.SpecifyTermination(() => numCalibPoints != 0, Blink);
        
        Init.AddDefaultTerminationMethod(() =>
        {
            // Assign the correct calibration points given the User's selection
            DefineCalibPoints(numCalibPoints);
            InfoString.Clear();
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
           
            CurrentProgressString.Clear();
            CurrentProgressString.Append($"<b>Calib Point #:  {calibNum + 1}</b> (of {numCalibPoints})"
                                   + $"\n<b>Calib Position:</b> ({String.Format("{0:0.00}", calibPointsADCS[calibNum].X)}, {String.Format("{0:0.00}", calibPointsADCS[calibNum].Y)})"
                                   + $"\n<b>Recalib Count:</b> {RecalibCount[calibNum]}");
            InfoString.Append("<b>\n\nInfo</b>" 
                                + "\nThe calibration point is <b>blinking</b>."
                                + "\nInstruct the player to focus on the point until the circle shrinks.");
            SetTrialSummaryString();
        });

        Blink.AddUpdateMethod(() =>
        {
            // Blinks the current calibration point until the acceptable calibration is met or keyboard override is triggered
            BlinkCalibrationPoint(CalibCircle.CircleGO);
            keyboardOverride |= InputBroker.GetKeyDown(KeyCode.Space);
        });

        Blink.SpecifyTermination(() => keyboardOverride || InCalibrationRange(), Shrink, () => { InfoString.Clear(); });

        //----------------------------------------------------- SHRINK THE CALIBRATION POINT -----------------------------------------------------
        
        Shrink.AddInitializationMethod(() =>
        {
            elapsedShrinkDuration = 0;
            InfoString.Append("<b>\n\nInfo</b>"
                                + "\nThe calibration point is <b>shrinking</b>."
                                + "\nContinue to focus on the point to prepare for calibration.");
            SetTrialSummaryString();
        });

        Shrink.AddUpdateMethod(() =>
        {
            ShrinkGameObject(CalibCircle.CircleGO, MinCircleScale, ShrinkDuration);
        });

        Shrink.SpecifyTermination(() => elapsedShrinkDuration > ShrinkDuration, Check, () =>
        {
            // Make sure that the Scale is set to the min scale
            CalibCircle.SetCircleScale(MinCircleScale);
        });

        Shrink.SpecifyTermination(() => !InCalibrationRange() && elapsedShrinkDuration != 0, Blink);
        
        Shrink.AddUniversalTerminationMethod(() => { InfoString.Clear(); });

        //----------------------------------------------------- CHECK CALIBRATION READINESS -----------------------------------------------------
        
        Check.AddInitializationMethod(() =>
        {
            keyboardOverride = false;
            InfoString.Append("<b>\n\nInfo</b>"
                                + "\nChecking that input is within range for calibration"
                                + "\n\nPress <b>Space</b> to override and calibrate regardless of gaze input location");
            SetTrialSummaryString();
        });
        
        Check.AddUpdateMethod(() => keyboardOverride |= InputBroker.GetKeyDown(KeyCode.Space));
        
        Check.SpecifyTermination(() => keyboardOverride || InCalibrationRange(), Calibrate, () =>
        {
            currentNormPoint = calibPointsADCS[calibNum];
            InfoString.Clear();
        });

        //-------------------------------------------------------- CALIBRATE GAZE POINT --------------------------------------------------------
       
        Calibrate.AddInitializationMethod(() =>
        {
            keyboardOverride = false;
            CalibCircle.CircleGO.GetComponent<UnityEngine.UI.Extensions.UICircle>().color = Color.green;
            InfoString.Append("<b>\n\nInfo</b>"
                                + $"\nCalibration Beginning at <b>({String.Format("{0:0.00}", calibPointsADCS[calibNum].X)}, {String.Format("{0:0.00}", calibPointsADCS[calibNum].Y)})</b>");
            SetTrialSummaryString();

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
            }

            currentCalibrationPointFinished = false;
            StateAfterDelay = Confirm;

            // Assign a 3 Second delay following calibration to allow the sample to be properly recorded
            if (!SpoofGazeWithMouse)
                DelayDuration = 3f;
            else
                DelayDuration = 0;

            InfoString.Clear();
        });

        //---------------------------------------------------- CONFIRM CALIBRATION RESULTS ----------------------------------------------------

        Confirm.AddInitializationMethod(() =>
        {
            CalibCircle.CircleGO.GetComponent<UnityEngine.UI.Extensions.UICircle>().color = Color.white;
            if (!SpoofGazeWithMouse)
            {
                InfoString.Append("<b>\n\nInfo</b>"
                                + string.Format("\nCompute and Apply Returned <b>{0}</b>", CalibrationResult.Status)
                                + "\nPress <b> = </b> to accept the point"
                                + "\nPress <b> - </b> to recalibrate the point");

                // Plots sample points to the Result Container, if they exist for the current calibration point
                CollectSamplePoints();
                CreateSampleLines(LeftSamples, RightSamples, (Vector2)CoordinateConverter.GetScreenPixel(calibPointsADCS[calibNum].ToVector2(), "screenadcs", 60));

                if (ResultContainer.transform.childCount > 0)
                {
                    ResultsString.Append($"\n\n<b>Calibration Results</b>"
                                         + $"\n<b>Left Eye</b>"
                                         + $"\n{CalculateSampleStatistics(LeftSampleDistances)}"
                                         + $"\n\n<b>Right Eye</b> "
                                         + $"\n{CalculateSampleStatistics(RightSampleDistances)}");
                }
                else
                {
                    ResultsString.Append($"No Samples Collected at this Calibration Point: <b>({String.Format("{0:0.00}", calibPointsADCS[calibNum].X)}, {String.Format("{0:0.00}", calibPointsADCS[calibNum].Y)})</b>");
                }
            }

            SetTrialSummaryString();

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
                if (calibNum == calibPointsADCS.Length - 1)
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
                RecalibCount[calibNum] += 1;
            }
        });

        // Dictates the subsequent state given the outcome of the User validation
        Confirm.SpecifyTermination(() => recalibPoint, Blink);
        
        Confirm.SpecifyTermination(() => pointFinished, Blink, ()=> { calibNum++; });

        Confirm.SpecifyTermination(()=> calibrationFinished, ITI);
        
        Confirm.AddTimer(() => assessTime, Delay, ()=>
        {
            DelayDuration = 0;

            if (!(calibNum == calibPointsADCS.Length - 1))
            {
                // Return to the Blinking state to calibrate the next point, if all points haven't been calibrated yet
                StateAfterDelay = Blink;
                calibNum++;
            }
            else
            {
                // Continues to ITI state since all points have been calibrated already
                StateAfterDelay = ITI;
            }
        });

        Confirm.AddUniversalTerminationMethod(() =>
        {
            // Set the calibration point to inactive at the end of confirming
            CalibCircle.CircleGO.SetActive(false);
            DestroyChildren(ResultContainer);

            // Reset variables once they have been evaluated
            pointFinished = false;
            recalibPoint = false;

            LeftSamples.Clear();
            LeftSampleDistances.Clear();
            RightSamples.Clear();
            RightSampleDistances.Clear();

            ResultsString.Clear();
            InfoString.Clear();

            SetTrialSummaryString();
        });

        ITI.AddInitializationMethod(() =>
        {
            // Leave calibration mode once the user has confirmed all points
            if(!SpoofGazeWithMouse)
                ScreenBasedCalibration.LeaveCalibrationMode();
            
            // Destroy remaining results on the experimenter display at the end of the trial
            DestroyChildren(ResultContainer);
        });

        ITI.SpecifyTermination(() => true, FinishTrial);

    }

    private void OnApplicationQuit()
    {
        if (!SpoofGazeWithMouse)
            ScreenBasedCalibration.LeaveCalibrationMode();
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
                calibPointsADCS = new NormalizedPoint2D[9] {
                allCalibPoints [4],
                allCalibPoints [0],
                allCalibPoints [1],
                allCalibPoints [2],
                allCalibPoints [3],
                allCalibPoints[5],
                allCalibPoints[6],
                allCalibPoints[7],
                allCalibPoints[8]};
                acceptableCalibrationDistance = Vector2.Distance((Vector2)CoordinateConverter.GetScreenPixel(allCalibPoints[0].ToVector2(), "monitoradcs",60), (Vector2)CoordinateConverter.GetScreenPixel(allCalibPoints[1].ToVector2(), "monitoradcs", 60)) / 2;

                RecalibCount = new int[9];
                break;
            case 6:
                calibPointsADCS = new NormalizedPoint2D[6] {
                allCalibPoints [4],
                allCalibPoints [0],
                allCalibPoints [1],
                allCalibPoints [2],
                allCalibPoints [3],
                allCalibPoints[5]};
                acceptableCalibrationDistance = Vector2.Distance((Vector2)CoordinateConverter.GetScreenPixel(allCalibPoints[0].ToVector2(), "monitoradcs", 60), (Vector2)CoordinateConverter.GetScreenPixel(allCalibPoints[1].ToVector2(), "monitoradcs", 60)) / 2;

                RecalibCount = new int[6];
                break;
            case 5:
                calibPointsADCS = new NormalizedPoint2D[5] {
                allCalibPoints [4],
                allCalibPoints [0],
                allCalibPoints [2],
                allCalibPoints [6],
                allCalibPoints [8]};
                acceptableCalibrationDistance = Vector2.Distance((Vector2)CoordinateConverter.GetScreenPixel(allCalibPoints[0].ToVector2(), "monitoradcs", 60), (Vector2)CoordinateConverter.GetScreenPixel(allCalibPoints[4].ToVector2(), "monitoradcs", 60)) / 2;

                RecalibCount = new int[5];
                break;
            case 3:
                calibPointsADCS = new NormalizedPoint2D[3]{
                allCalibPoints [4],
                allCalibPoints [3],
                allCalibPoints [5] };
                acceptableCalibrationDistance = Vector2.Distance((Vector2)CoordinateConverter.GetScreenPixel(allCalibPoints[0].ToVector2(), "monitoradcs", 60), (Vector2)CoordinateConverter.GetScreenPixel(allCalibPoints[1].ToVector2(), "monitoradcs", 60)) / 2; 

                RecalibCount = new int[3];
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

    private void OnGazeDataReceived(object sender, GazeDataEventArgs e)
    {
        // Process Left Eye gaze data each frame
        TobiiGazeSample gazeSample = new TobiiGazeSample();

        // Left Eye Data
        gazeSample.leftPupilValidity = e.LeftEye.Pupil.Validity.ToString();
        gazeSample.leftGazeOriginValidity = e.LeftEye.GazeOrigin.Validity.ToString();
        gazeSample.leftGazePointValidity = e.LeftEye.GazePoint.Validity.ToString();
        gazeSample.leftGazePointOnDisplayArea = e.LeftEye.GazePoint.PositionOnDisplayArea.ToVector2();
        gazeSample.leftGazeOriginInUserCoordinateSystem = e.LeftEye.GazeOrigin.PositionInUserCoordinates.ToVector3();
        gazeSample.leftGazePointInUserCoordinateSystem = e.LeftEye.GazePoint.PositionInUserCoordinates.ToVector3();
        gazeSample.leftGazeOriginInTrackboxCoordinateSystem = e.LeftEye.GazeOrigin.PositionInTrackBoxCoordinates.ToVector3();
        gazeSample.leftPupilDiameter = e.LeftEye.Pupil.PupilDiameter;
        
        // Right Eye Data
        gazeSample.rightPupilValidity = e.RightEye.Pupil.Validity.ToString();
        gazeSample.rightGazeOriginValidity = e.RightEye.GazeOrigin.Validity.ToString();
        gazeSample.rightGazePointValidity = e.RightEye.GazePoint.Validity.ToString();
        gazeSample.rightGazePointOnDisplayArea = e.RightEye.GazePoint.PositionOnDisplayArea.ToVector2();
        gazeSample.rightGazeOriginInUserCoordinateSystem = e.RightEye.GazeOrigin.PositionInUserCoordinates.ToVector3();
        gazeSample.rightGazePointInUserCoordinateSystem = e.RightEye.GazePoint.PositionInUserCoordinates.ToVector3();
        gazeSample.rightGazeOriginInTrackboxCoordinateSystem = e.RightEye.GazeOrigin.PositionInTrackBoxCoordinates.ToVector3();
        gazeSample.rightPupilDiameter = e.RightEye.Pupil.PupilDiameter;

    }

    public void DetermineCollectDataStatus(NormalizedPoint2D point)
    {
        CalibrationStatus status = ScreenBasedCalibration.CollectData(point);
        
        if (status.Equals(CalibrationStatus.Success))
        {
            // Done calibrating the point if successful
            currentCalibrationPointFinished = true;
        }
    }
    
    public Vector2 ADCSToScreen(NormalizedPoint2D normADCSGazePoint)
    {
        Vector2 adcsGazePoint = normADCSGazePoint.ToVector2();
        float x = adcsGazePoint.x * MonitorDetails.PixelResolution.x;
        float y = ((1- adcsGazePoint.y) * MonitorDetails.PixelResolution.y);
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

    private void CollectSamplePoints()
    {
        Tobii.Research.CalibrationPoint calibPoint = null;
        
        foreach (var Calib in CalibrationResult.CalibrationPoints)
        {
            if (Calib.PositionOnDisplayArea.ToVector2() == currentNormPoint.ToVector2())
                calibPoint = Calib;
        }
        if (calibPoint == null)
        {
            Debug.Log("There is no data associated with the current calibration point.");
            return;
        }
        else
        {
            for (int i = 0; i < (calibPoint.CalibrationSamples.Count >= 10 ? 10 : calibPoint.CalibrationSamples.Count); i++)
            {
                CalibrationSample sample = calibPoint.CalibrationSamples[i];
                // Record the positions of the Left and Right eye for each sample of the calibration point 
                Vector2 leftSamplePos = (Vector2)CoordinateConverter.GetScreenPixel(sample.LeftEye.PositionOnDisplayArea.ToVector2(), "screenadcs", 60);
                Vector2 rightSamplePos = (Vector2)CoordinateConverter.GetScreenPixel(sample.RightEye.PositionOnDisplayArea.ToVector2(), "screenadcs", 60);

                LeftSamples.Add(leftSamplePos);
                RightSamples.Add(rightSamplePos);

            }
        }
    }

    private void CreateSampleLines(List<Vector2> leftSamples, List<Vector2> rightSamples, Vector2 calibPoint)
    {
        for (int i = 0; i < leftSamples.Count; i++)
        {
            USE_Line leftSampleLine = new USE_Line(ResultContainer.GetComponent<Canvas>(), ScreenToPlayerViewPosition(leftSamples[i], ResultContainer.transform), ScreenToPlayerViewPosition(calibPoint, ResultContainer.transform), Color.blue, $"L{i + 1}");
            LeftSampleDistances.Add(leftSampleLine.LineLength);
            leftSampleLine.LineGO.SetActive(true);
        }

        for (int i = 0; i < rightSamples.Count; i++)
        {
            USE_Line rightSampleLine = new USE_Line(ResultContainer.GetComponent<Canvas>(), ScreenToPlayerViewPosition(rightSamples[i], ResultContainer.transform), ScreenToPlayerViewPosition(calibPoint, ResultContainer.transform), Color.red, $"R{i + 1}");
            RightSampleDistances.Add(rightSampleLine.LineLength);
            rightSampleLine.LineGO.SetActive(true);
        }
    }

    private void AssignCalibPositions()
    {
        allCalibPoints = new NormalizedPoint2D[]
        {
            new NormalizedPoint2D(CalibPointsInset[0], CalibPointsInset[1]),
            new NormalizedPoint2D(0.5f, CalibPointsInset[1]),
            new NormalizedPoint2D(1f - CalibPointsInset[0], CalibPointsInset[1]),
            new NormalizedPoint2D(CalibPointsInset[0], 0.5f),
            new NormalizedPoint2D(0.5f, 0.5f),
            new NormalizedPoint2D(1f - CalibPointsInset[0], 0.5f),
            new NormalizedPoint2D(CalibPointsInset[0], 1f - CalibPointsInset[1]),
            new NormalizedPoint2D(0.5f, 1f - CalibPointsInset[1]),
            new NormalizedPoint2D(1f - CalibPointsInset[0], 1f - CalibPointsInset[1])
        };
    }


    private void InitializeCalibPoint()
    {
        CalibCircle.CircleGO.GetComponent<UnityEngine.UI.Extensions.UICircle>().color = Color.black;
        CalibCircle.SetCircleScale(MaxCircleScale);
        currentADCSTarget = calibPointsADCS[calibNum]; // get calib coordinates in screen ADCS space

        currentScreenPixelTarget = (Vector2)CoordinateConverter.GetScreenPixel(currentADCSTarget.ToVector2(), "screenadcs", 60); // get calib coordinates in Screen space
        Debug.Log("#CURRENT screen Pixel TARGET: " + currentScreenPixelTarget.ToString());

        CalibCircle.CircleGO.GetComponent<RectTransform>().anchoredPosition = currentScreenPixelTarget;
        CalibCircle.CircleGO.SetActive(true);
    }

    private bool InCalibrationRange()
    {
        return (Vector2.Distance((Vector2)SelectionHandler.CurrentInputLocation(), currentScreenPixelTarget) < acceptableCalibrationDistance);
    }
    private void CreateResultContainer()
    {
        ResultContainer = new GameObject("ResultContainer", typeof(Canvas), typeof(CanvasRenderer));
        ResultContainer.transform.parent = PlayerViewPanelGO.transform;
        ResultContainer.GetComponent<RectTransform>().sizeDelta = ResultContainer.transform.parent.GetComponent<RectTransform>().sizeDelta;
        ResultContainer.GetComponent<RectTransform>().anchorMin = Vector3.zero;
        ResultContainer.GetComponent<RectTransform>().anchorMax = Vector3.zero;
        ResultContainer.GetComponent<RectTransform>().pivot = Vector3.zero;
        ResultContainer.GetComponent<RectTransform>().anchoredPosition = Vector3.zero;
    }
    public override void ResetTrialVariables()
    {
        calibNum = 0;
        numCalibPoints = 0;

        recalibPoint = false;
        pointFinished = false;
        calibrationFinished = false;

        if(LeftSamples.Count > 0)
            LeftSamples.Clear();
        
        if(RightSamples.Count > 0)
            RightSamples.Clear();

        CurrentProgressString.Clear();
        ResultsString.Clear();
        InfoString.Clear();
    }

    public override void FinishTrialCleanup()
    {
        if(CalibCircle != null)
            CalibCircle.CircleGO.SetActive(false);

        DestroyChildren(ResultContainer);
    }
    public void InitializeEyeTrackerSettings()
    {
        if (IEyeTracker == null)
            IEyeTracker = TobiiEyeTrackerController.Instance.iEyeTracker;
        if (EyeTracker == null)
            EyeTracker = TobiiEyeTrackerController.Instance.EyeTracker;
        if (ScreenBasedCalibration == null)
            ScreenBasedCalibration = new ScreenBasedCalibration(IEyeTracker);
        if (DisplayArea == null)
            DisplayArea = TobiiEyeTrackerController.Instance.DisplayArea;
    }

    private string CalculateSampleStatistics(List<float> samples)
    {
        int n = samples.Count;
        if (n <= 1)
        {
            return "Insufficient data to calculate standard deviation.";
        }

        // Calculate average
        float average = samples.Average();

        // Calculate standard deviation
        float sumOfSquaredDeviations = samples.Sum(value => Mathf.Pow(value - average, 2));
        float standardDeviation = Mathf.Sqrt(sumOfSquaredDeviations / samples.Count);

        string result = $"<b>Mean:</b> {String.Format("{0:0.000}", average)}" +
            $"\n<b>Standard Deviation:</b> {String.Format("{0:0.000}", standardDeviation)}";

        return result;
    }

    private void SetTrialSummaryString()
    {
        TrialSummaryString = CurrentProgressString.ToString() + ResultsString.ToString() + InfoString.ToString();
    }
    
}
