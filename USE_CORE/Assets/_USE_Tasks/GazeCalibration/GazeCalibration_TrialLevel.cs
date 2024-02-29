/*
MIT License

Copyright (c) 2023 Multitask - Unified - Suite -for-Expts

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files(the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/


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


    // Inherited from the Session Level...
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
            // Create necessary variables to display text onto the Experimenter Display
            if (!Session.WebBuild)
            {
                PlayerViewPanel = gameObject.AddComponent<PlayerViewPanel>();
                PlayerViewPanelGO = GameObject.Find("MainCameraCopy");
            }

            GC_CanvasGO = GameObject.Find("GazeCalibration_Canvas");

            if (!Session.TobiiEyeTrackerController.isCalibrating)
            {
                Session.TobiiEyeTrackerController.ScreenBasedCalibration.EnterCalibrationMode();
                Session.TobiiEyeTrackerController.isCalibrating = true;
            }
        });

        SetupTrial.AddSpecificInitializationMethod(() =>
        {
            AssignCalibPositions();
            if (!CurrentTrialDef.SpoofGazeWithMouse)
                InitializeEyeTrackerSettings();

            InfoString.Append("<b>Info</b>"
                                + "\nPress <b>Space</b> to begin a <b>9</b> point calibration"
                                + "\nPress <b>6</b>, <b>5</b>, or <b>3</b> to begin the respective point calibration");
            SetTrialSummaryString();

            // Assign UI Circles for the calib circles if not yet created
            if (GC_CanvasGO.transform.Find("CalibrationCircle") == null)
            {
                CalibCircle = new USE_Circle(GC_CanvasGO.GetComponent<Canvas>(), Vector3.zero, CurrentTrialDef.MaxCircleScale, "CalibrationCircle");
                CalibCircle.CircleGO.SetActive(false);
            }

            // Create a container for the calibration results
            if (ResultContainer == null)
            {
                CreateResultContainer();
            }

            AssignGazeCalibrationCameraToTrackboxCanvas();


        });


        SetupTrial.SpecifyTermination(()=> (!CurrentTrialDef.SpoofGazeWithMouse ? (Session.TobiiEyeTrackerController.ScreenBasedCalibration != null):true), Init);

        /*if (CurrentTrialDef.SpoofGazeWithMouse)
            SelectionHandler = Session.SelectionTracker.SetupSelectionHandler("trial", "MouseHover", Session.MouseTracker, Init, ITI);
        else
            */
        SelectionHandler = Session.SelectionTracker.SetupSelectionHandler("trial", "GazeSelection", Session.GazeTracker, Init, ITI);
        

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
        });
        
        Init.SpecifyTermination(() => numCalibPoints != 0, Blink, () =>
        {
            // Only enter Calibration if an eyetracker is being used
            if (!Session.TobiiEyeTrackerController.isCalibrating)
            {
                Session.TobiiEyeTrackerController.ScreenBasedCalibration.EnterCalibrationMode();
                Session.TobiiEyeTrackerController.isCalibrating = true;
            }
            // Assign the correct calibration points given the User's selection

            DefineCalibPoints(numCalibPoints);
            InfoString.Clear();
        });

        //----------------------------------------------------- BLINK THE CALIBRATION POINT -----------------------------------------------------
        
        Blink.AddSpecificInitializationMethod(() =>
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
        
        Shrink.AddSpecificInitializationMethod(() =>
        {
            elapsedShrinkDuration = 0;
            InfoString.Append("<b>\n\nInfo</b>"
                                + "\nThe calibration point is <b>shrinking</b>."
                                + "\nContinue to focus on the point to prepare for calibration.");
            SetTrialSummaryString();
        });

        Shrink.AddUpdateMethod(() =>
        {
            ShrinkGameObject(CalibCircle.CircleGO, CurrentTrialDef.MinCircleScale, CurrentTrialDef.ShrinkDuration);
        });

        Shrink.SpecifyTermination(() => elapsedShrinkDuration > CurrentTrialDef.ShrinkDuration, Check, () =>
        {
            // Make sure that the Scale is set to the min scale
            CalibCircle.SetCircleScale(CurrentTrialDef.MinCircleScale);
        });

        Shrink.SpecifyTermination(() => !InCalibrationRange() && elapsedShrinkDuration != 0, Blink);
        
        Shrink.AddUniversalLateTerminationMethod(() => { InfoString.Clear(); });

        //----------------------------------------------------- CHECK CALIBRATION READINESS -----------------------------------------------------
        
        Check.AddSpecificInitializationMethod(() =>
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
       
        Calibrate.AddSpecificInitializationMethod(() =>
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
            if(!CurrentTrialDef.SpoofGazeWithMouse)
                DetermineCollectDataStatus(currentNormPoint);
            keyboardOverride |= InputBroker.GetKeyDown(KeyCode.Space);
        });
     
        Calibrate.SpecifyTermination(() => currentCalibrationPointFinished | keyboardOverride, Delay, () =>
        {
            // Collects eye tracking data at the current calibration point, computes the calibration settings, and applies them to the eye tracker.
            if (!CurrentTrialDef.SpoofGazeWithMouse)
            {
                CalibrationResult = Session.TobiiEyeTrackerController.ScreenBasedCalibration.ComputeAndApply();
            }

            currentCalibrationPointFinished = false;
            StateAfterDelay = Confirm;

            // Assign a 3 Second delay following calibration to allow the sample to be properly recorded
            if (!CurrentTrialDef.SpoofGazeWithMouse)
                DelayDuration = 3f;
            else
                DelayDuration = 0;

            InfoString.Clear();
        });

        //---------------------------------------------------- CONFIRM CALIBRATION RESULTS ----------------------------------------------------

        Confirm.AddSpecificInitializationMethod(() =>
        {
            CalibCircle.CircleGO.GetComponent<UnityEngine.UI.Extensions.UICircle>().color = Color.white;
            if (!CurrentTrialDef.SpoofGazeWithMouse)
            {
                InfoString.Append("<b>\n\nInfo</b>"
                                + string.Format("\nCompute and Apply Returned <b>{0}</b>", CalibrationResult.Status)
                                + "\nPress <b> = </b> to accept the point"
                                + "\nPress <b> - </b> to recalibrate the point");

                // Plots sample points to the Result Container, if they exist for the current calibration point
                CollectSamplePoints();
                CreateSampleLines(LeftSamples, RightSamples, (Vector2)USE_CoordinateConverter.GetScreenPixel(calibPointsADCS[calibNum].ToVector2(), "screenadcs", 60));

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

            if (Session.SyncBoxController != null)
            {
                // Provide reward during the Confirm state based off values in the BlockDef
                Session.SyncBoxController.SendRewardPulses(CurrentTrialDef.NumPulses, CurrentTrialDef.PulseSize);
                CurrentTaskLevel.NumRewardPulses_InBlock += CurrentTrialDef.NumPulses;
                CurrentTaskLevel.NumRewardPulses_InTask += CurrentTrialDef.NumPulses;
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
                if(!CurrentTrialDef.SpoofGazeWithMouse)
                    Session.TobiiEyeTrackerController.ScreenBasedCalibration.DiscardData(currentNormPoint);
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

        Confirm.AddUniversalLateTerminationMethod(() =>
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

        ITI.AddSpecificInitializationMethod(() =>
        {
            // Leave calibration mode once the user has confirmed all points
            // Collects eye tracking data at the current calibration point, computes the calibration settings, and applies them to the eye tracker.
            if (!CurrentTrialDef.SpoofGazeWithMouse)
            {
                CalibrationResult = Session.TobiiEyeTrackerController.ScreenBasedCalibration.ComputeAndApply();
                CollectSamplePoints();
            }
        });

        ITI.SpecifyTermination(() => true, FinishTrial, ()=>
        {
            
            // Destroy remaining results on the experimenter display at the end of the trial
            DestroyChildren(ResultContainer);
        });

    }

    private void OnApplicationQuit()
    {
        if (Session.TobiiEyeTrackerController != null && Session.TobiiEyeTrackerController.isCalibrating)
            Session.TobiiEyeTrackerController.ScreenBasedCalibration.LeaveCalibrationMode();
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
                acceptableCalibrationDistance = Vector2.Distance((Vector2)USE_CoordinateConverter.GetScreenPixel(allCalibPoints[0].ToVector2(), "screenadcs", 60), (Vector2)USE_CoordinateConverter.GetScreenPixel(allCalibPoints[1].ToVector2(), "screenadcs", 60)) / 2;

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
                acceptableCalibrationDistance = Vector2.Distance((Vector2)USE_CoordinateConverter.GetScreenPixel(allCalibPoints[0].ToVector2(), "screenadcs", 60), (Vector2)USE_CoordinateConverter.GetScreenPixel(allCalibPoints[1].ToVector2(), "screenadcs", 60)) / 2;

                RecalibCount = new int[6];
                break;
            case 5:
                calibPointsADCS = new NormalizedPoint2D[5] {
                allCalibPoints [4],
                allCalibPoints [0],
                allCalibPoints [2],
                allCalibPoints [6],
                allCalibPoints [8]};
                acceptableCalibrationDistance = Vector2.Distance((Vector2)USE_CoordinateConverter.GetScreenPixel(allCalibPoints[0].ToVector2(), "screenadcs", 60), (Vector2)USE_CoordinateConverter.GetScreenPixel(allCalibPoints[4].ToVector2(), "screenadcs", 60)) / 2;

                RecalibCount = new int[5];
                break;
            case 3:
                calibPointsADCS = new NormalizedPoint2D[3]{
                allCalibPoints [4],
                allCalibPoints [3],
                allCalibPoints [5] };
                acceptableCalibrationDistance = Vector2.Distance((Vector2)USE_CoordinateConverter.GetScreenPixel(allCalibPoints[0].ToVector2(), "screenadcs", 60), (Vector2)USE_CoordinateConverter.GetScreenPixel(allCalibPoints[1].ToVector2(), "screenadcs", 60)) / 2; 

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


    public void DetermineCollectDataStatus(NormalizedPoint2D point)
    {
        CalibrationStatus status = Session.TobiiEyeTrackerController.ScreenBasedCalibration.CollectData(point);
        Debug.Log("STATUS: " + status.ToString());
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
            if (Calib.PositionOnDisplayArea.ToVector2() == calibPointsADCS[calibNum].ToVector2())
                calibPoint = Calib;
        }
        if (calibPoint == null)
        {
            Debug.Log("There is no data associated with the current calibration point.");
            return;
        }
        else
        {
            //for (int i = 0; i < (calibPoint.CalibrationSamples.Count >= 10 ? 10 : calibPoint.CalibrationSamples.Count); i++)
            for (int i = 0; i < calibPoint.CalibrationSamples.Count; i++)
            {
                
                CalibrationSample sample = calibPoint.CalibrationSamples[i];
                // Record the positions of the Left and Right eye for each sample of the calibration point 
                Vector2 leftSamplePos = (Vector2)USE_CoordinateConverter.GetScreenPixel(sample.LeftEye.PositionOnDisplayArea.ToVector2(), "screenadcs", 60);
                Vector2 rightSamplePos = (Vector2)USE_CoordinateConverter.GetScreenPixel(sample.RightEye.PositionOnDisplayArea.ToVector2(), "screenadcs", 60);
                LeftSamples.Add(leftSamplePos);
                RightSamples.Add(rightSamplePos);

            }

            CreateSampleLines(LeftSamples, RightSamples, (Vector2)USE_CoordinateConverter.GetScreenPixel(calibPointsADCS[calibNum].ToVector2(), "screenadcs", 60f));
            LeftSamples.Clear();
            RightSamples.Clear();

        }
    }

    private void CreateSampleLines(List<Vector2> leftSamples, List<Vector2> rightSamples, Vector2 calibPoint)
    {
        for (int i = 0; i < leftSamples.Count; i++)
        {
            USE_Line leftSampleLine = new USE_Line(ResultContainer.GetComponent<Canvas>(), ScreenToPlayerViewPosition(leftSamples[i], ResultContainer.transform), ScreenToPlayerViewPosition(calibPoint, ResultContainer.transform), Color.blue, $"L{i + 1}", true);
            LeftSampleDistances.Add(leftSampleLine.LineLength);
            leftSampleLine.LineGO.SetActive(true);
        }

        for (int i = 0; i < rightSamples.Count; i++)
        {
            USE_Line rightSampleLine = new USE_Line(ResultContainer.GetComponent<Canvas>(), ScreenToPlayerViewPosition(rightSamples[i], ResultContainer.transform), ScreenToPlayerViewPosition(calibPoint, ResultContainer.transform), Color.red, $"R{i + 1}", true);
            RightSampleDistances.Add(rightSampleLine.LineLength);
            rightSampleLine.LineGO.SetActive(true);
        }
    }

    private void AssignCalibPositions()
    {
        allCalibPoints = new NormalizedPoint2D[]
        {
            new NormalizedPoint2D(CurrentTrialDef.CalibPointsInset[0], CurrentTrialDef.CalibPointsInset[1]),
            new NormalizedPoint2D(0.5f, CurrentTrialDef.CalibPointsInset[1]),
            new NormalizedPoint2D(1f - CurrentTrialDef.CalibPointsInset[0], CurrentTrialDef.CalibPointsInset[1]),
            new NormalizedPoint2D(CurrentTrialDef.CalibPointsInset[0], 0.5f),
            new NormalizedPoint2D(0.5f, 0.5f),
            new NormalizedPoint2D(1f - CurrentTrialDef.CalibPointsInset[0], 0.5f),
            new NormalizedPoint2D(CurrentTrialDef.CalibPointsInset[0], 1f - CurrentTrialDef.CalibPointsInset[1]),
            new NormalizedPoint2D(0.5f, 1f - CurrentTrialDef.CalibPointsInset[1]),
            new NormalizedPoint2D(1f - CurrentTrialDef.CalibPointsInset[0], 1f - CurrentTrialDef.CalibPointsInset[1])
        };
    }


    private void InitializeCalibPoint()
    {
        CalibCircle.CircleGO.GetComponent<UnityEngine.UI.Extensions.UICircle>().color = Color.black;
        CalibCircle.SetCircleScale(CurrentTrialDef.MaxCircleScale);
        currentADCSTarget = calibPointsADCS[calibNum]; // get calib coordinates in screen ADCS space
        currentScreenPixelTarget = (Vector2)USE_CoordinateConverter.GetScreenPixel(currentADCSTarget.ToVector2(), "screenadcs", 60); // get calib coordinates in Screen space
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
        if(GameObject.Find("CalibrationCircle") != null)
            CalibCircle.CircleGO.SetActive(false);
        if(GameObject.Find("ResultContainer") != null)
            DestroyChildren(ResultContainer);
    }
    public void InitializeEyeTrackerSettings()
    {
        if (IEyeTracker == null)
            IEyeTracker = Session.TobiiEyeTrackerController.iEyeTracker;
        if (EyeTracker == null)
            EyeTracker = Session.TobiiEyeTrackerController.EyeTracker;/*
        if (ScreenBasedCalibration == null)
            ScreenBasedCalibration = TobiiEyeTrackerController.Instance.ScreenBasedCalibration;
        if (DisplayArea == null)
            DisplayArea = TobiiEyeTrackerController.Instance.DisplayArea;*/

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

    private void AssignGazeCalibrationCameraToTrackboxCanvas()
    {
        // Find the GazeCalibration_Camera GameObject in the scene
        GameObject gazeCalibrationCameraGO = GameObject.Find("GazeCalibration_Camera");
        if (gazeCalibrationCameraGO != null)
        {
            // Get the Camera component attached to the GazeCalibration_Camera GameObject
            Camera gazeCalibrationCamera = gazeCalibrationCameraGO.GetComponent<Camera>();

            if (gazeCalibrationCamera != null)
            {
                // Find the CanvasTrackBox child of the TrackBoxGuide_GO GameObject
                Transform canvasTrackBoxTransform = Session.TobiiEyeTrackerController.TrackBoxGuide_GO.transform.Find("CanvasTrackBox");

                if (canvasTrackBoxTransform != null)
                {
                    Canvas canvas = canvasTrackBoxTransform.GetComponent<Canvas>();
                    if (canvas != null)
                    {
                        // Assign the camera to the worldCamera property of the Canvas component
                        canvas.worldCamera = gazeCalibrationCamera;
                    }
                    else
                    {
                        Debug.LogError("The CanvasTrackBox does not have a Canvas component attached.");
                    }
                }
                else
                {
                    Debug.LogError("CanvasTrackBox child GameObject not found.");
                }
            }
            else
            {
                Debug.LogError("GazeCalibration_Camera does not have a Camera component attached.");
            }
        }
        else
        {
            Debug.LogError("GazeCalibration_Camera GameObject not found in the scene.");
        }

    }

}
