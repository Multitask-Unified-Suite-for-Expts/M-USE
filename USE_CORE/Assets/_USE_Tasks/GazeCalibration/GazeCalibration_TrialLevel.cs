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


using GazeCalibration_Namespace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tobii.Research;
using Tobii.Research.Unity;
using UnityEngine;
using USE_DisplayManagement;
using USE_ExperimentTemplate_Trial;
using USE_States;
using USE_UI;


public class GazeCalibration_TrialLevel : ControlLevel_Trial_Template
{
    // MUSE Common Variables
    public GazeCalibration_TrialDef CurrentTrialDef => GetCurrentTrialDef<GazeCalibration_TrialDef>();
    public GazeCalibration_TaskLevel CurrentTaskLevel => GetTaskLevel<GazeCalibration_TaskLevel>();
    public GazeCalibration_TaskDef CurrentTaskDef => GetTaskDef<GazeCalibration_TaskDef>();


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
    private Vector2 currentScreenPixelTarget = new Vector2(float.NaN, float.NaN);
    private int calibNum;

    // Shrink Calibration Point Variables
    private Vector3 originalScale;
    private float elapsedShrinkDuration;

    // Calibration Assessment Variables
    [HideInInspector] public bool currentCalibrationPointFinished;
    [HideInInspector] public bool calibrationFinished;
    private bool pointFinished;
    private bool recalibPoint;
    private NormalizedPoint2D currentNormPoint;
    private bool keyboardOverride = false;

    // Game Objects
    private USE_Circle CalibCircle;
    private GameObject PlayerViewPanelGO;
    private PlayerViewPanel PlayerViewPanel;
    private GameObject ResultContainer;

    // Tobii SDK Variables 
    private CalibrationResult CalibrationResult;
    public GameObject EyeTrackerPrefab;
    public GameObject TrackBoxPrefab;
    private IEyeTracker IEyeTracker;
    private EyeTracker EyeTracker;

    // Gaze Data Samples
    private List<Vector2> LeftSamples = new List<Vector2>();
    private List<Vector2> RightSamples = new List<Vector2>();
    private List<float> LeftSampleDistances = new List<float>();
    private List<float> RightSampleDistances = new List<float>();
    private int[] RecalibCount;
    private float AverageCalibrationErrorL;
    private float AverageCalibrationErrorR;

    // Experimenter Display Text Variables
    private StringBuilder InfoString = new StringBuilder();
    private StringBuilder CurrentProgressString = new StringBuilder();
    private StringBuilder ResultsString = new StringBuilder();


    private float TimeInCalibrationRange = 0f;

    private float DistanceToCurrentPoint = 0f;


    public override void DefineControlLevel()
    {
        State Init = new State("Init");
        State Fixate = new State("Blink");
        State Shrink = new State("Shrink");
        State Check = new State("Check");
        State Calibrate = new State("Calibrate");
        State Confirm = new State("Confirm");
        State ITI = new State("ITI");

        AddActiveStates(new List<State> { Init, Fixate, Shrink, Check, Calibrate, Confirm, ITI });

        Add_ControlLevel_InitializationMethod(() =>
        {
            // Create necessary variables to display text onto the Experimenter Display
            if (!Session.WebBuild)
            {
                PlayerViewPanel = gameObject.AddComponent<PlayerViewPanel>();
                PlayerViewPanelGO = GameObject.Find("MainCameraCopy");
            }

            GC_CanvasGO = GameObject.Find("GazeCalibration_Canvas");


            if (Session.GazeCalibrationController.InTaskGazeCalibration)
                TrialCount_InTask = Session.GazeCalibrationController.InTaskGazeCalibration_TrialCount_InTask;
        });

        SetupTrial.AddSpecificInitializationMethod(() =>
        {
            AssignCalibPositions();

            if (!Session.SessionDef.SpoofGazeWithMouse)
                InitializeEyeTrackerSettings();

            InfoString.Append("Info"
                                + "\nPress Space to begin a 9 point calibration"
                                + "\nPress 6, 5, or 3 to begin the respective point calibration");
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

            if (!Session.SessionDef.SpoofGazeWithMouse)
                AssignGazeCalibrationCameraToTrackboxCanvas();
        });


        SetupTrial.SpecifyTermination(() => (!Session.SessionDef.SpoofGazeWithMouse ? (Session.TobiiEyeTrackerController.ScreenBasedCalibration != null) : true), Init);

        if (Session.SessionDef.SpoofGazeWithMouse)
            SelectionHandler = Session.SelectionTracker.SetupSelectionHandler("trial", "MouseHover", Session.MouseTracker, Init, ITI);
        else
            SelectionHandler = Session.SelectionTracker.SetupSelectionHandler("trial", "GazeShotgun", Session.GazeTracker, Init, ITI);


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

        Init.SpecifyTermination(() => numCalibPoints != 0, Fixate, () =>
        {
            TurnOnCalibration();

            // Assign the correct calibration points given the User's selection
            DefineCalibPoints(numCalibPoints);

            InfoString.Clear();

            DistanceToCurrentPoint = 0;

            //DELETE LATER IF GO BACK TO OTHER CALCULATION:
            acceptableCalibrationDistance = CurrentTaskDef.ShotgunRadius_Pixels;

            Debug.LogWarning("ACCEPTABLE DISTANCE = " + acceptableCalibrationDistance);
        });

        //----------------------------------------------------- CONFIRM GAZE IS IN RANGE OF THE CALIBRATION POINT -----------------------------------------------------
        Fixate.AddSpecificInitializationMethod(() =>
        {
            // Initialize the Calibration Point at Max Scale
            InitializeCalibPoint();

            // Reset variables relating to calibration completion
            currentCalibrationPointFinished = false;
            keyboardOverride = false;

            CurrentProgressString.Clear();
            CurrentProgressString.Append($"Calib Point #:  {calibNum + 1} (of {numCalibPoints})"
                                   + $"\nCalib Position: ({String.Format("{0:0.00}", calibPointsADCS[calibNum].X)}, {String.Format("{0:0.00}", calibPointsADCS[calibNum].Y)})"
                                   + $"\nRecalib Count: {RecalibCount[calibNum]}");
            InfoString.Append("\n\nInfo"
                                + "\nThe calibration point is blinking."
                                + "\nInstruct the player to focus on the point until the circle shrinks.");

            SetTrialSummaryString();

            //Reset before fixation
            TimeInCalibrationRange = 0;

        });

        Fixate.AddUpdateMethod(() =>
        {
            SetTrialSummaryString();

            keyboardOverride |= InputBroker.GetKeyDown(KeyCode.Space);
        });

        Fixate.SpecifyTermination(() => keyboardOverride || InCalibrationRange(), Shrink, () => { InfoString.Clear(); });

        //----------------------------------------------------- SHRINK THE CALIBRATION POINT -----------------------------------------------------
        //in the shrink state as long as InCalibrationRange, otherwise it goes back to blink

        Shrink.AddSpecificInitializationMethod(() =>
        {
            originalScale = CalibCircle.CircleGO.transform.localScale;
            elapsedShrinkDuration = 0;
            InfoString.Append("<b>\n\nInfo</b>"
                                + "\nThe calibration point is <b>shrinking</b>."
                                + "\nContinue to focus on the point to prepare for calibration.");
            SetTrialSummaryString();
        });

        Shrink.AddUpdateMethod(() =>
        {
            SetTrialSummaryString();

            elapsedShrinkDuration += Time.deltaTime;

            ShrinkGameObject(CalibCircle.CircleGO, CurrentTrialDef.MinCircleScale, CurrentTrialDef.ShrinkDuration);
        });

        // Terminate the shrink state 2 frames early so that the object continues to shrink for Check and Calibrate
        Shrink.SpecifyTermination(() => elapsedShrinkDuration > (CurrentTrialDef.ShrinkDuration - 0.05f), Check);

        Shrink.SpecifyTermination(() => !InCalibrationRange() && elapsedShrinkDuration != 0, Fixate);

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

        Check.AddUpdateMethod(() =>
        {
            SetTrialSummaryString();

            //Keep shrinking the circle during Check state (which should only be 1 frame anyway);
            ShrinkGameObject(CalibCircle.CircleGO, CurrentTrialDef.MinCircleScale, CurrentTrialDef.ShrinkDuration);

            //Set keyboardOverride to true if they press space
            keyboardOverride |= InputBroker.GetKeyDown(KeyCode.Space);
        });
        Check.SpecifyTermination(() => keyboardOverride || InCalibrationRange(), Calibrate, () =>
        {
            currentNormPoint = calibPointsADCS[calibNum];
            InfoString.Clear();
        });

        //-------------------------------------------------------- CALIBRATE GAZE POINT --------------------------------------------------------
        int unsuccessfulCount = 0;

        Calibrate.AddSpecificInitializationMethod(() =>
        {
            keyboardOverride = false;

            InfoString.Append("<b>\n\nInfo</b>"
                                + $"\nCalibration Beginning at <b>({String.Format("{0:0.00}", calibPointsADCS[calibNum].X)}, {String.Format("{0:0.00}", calibPointsADCS[calibNum].Y)})</b>");

            SetTrialSummaryString();

            unsuccessfulCount = 0;
        });

        Calibrate.AddUpdateMethod(() =>
        {
            SetTrialSummaryString();

            //Keep shrinking the circle during Check state (which should only be 1 frame anyway);
            ShrinkGameObject(CalibCircle.CircleGO, CurrentTrialDef.MinCircleScale, CurrentTrialDef.ShrinkDuration);

            if (!Session.SessionDef.SpoofGazeWithMouse)
            {
                CalibrationStatus status = Session.TobiiEyeTrackerController.ScreenBasedCalibration.CollectData(currentNormPoint);
                if (status.Equals(CalibrationStatus.Success))
                {
                    currentCalibrationPointFinished = true;
                }
                else
                {
                    unsuccessfulCount++;

                    if (unsuccessfulCount == 1)
                        Debug.LogWarning("The CollectData() calibration method was unsuccessful. This can happen the first time. Will try again next frame.");
                    else if (unsuccessfulCount > 1)
                        Debug.LogError("CollectData() CALIBRATION METHOD WAS UNSUCCESSFUL MULTIPLE TIMES!!!!");
                }
            }
            keyboardOverride |= InputBroker.GetKeyDown(KeyCode.Space);
        });

        Calibrate.SpecifyTermination(() => currentCalibrationPointFinished || keyboardOverride, Confirm, () =>
        {
            // Make sure that the Scale is set to the min scale
            CalibCircle.SetCircleScale(CurrentTrialDef.MinCircleScale);
            CalibCircle.CircleGO.GetComponent<UnityEngine.UI.Extensions.UICircle>().color = Color.green;

            currentCalibrationPointFinished = false;

            InfoString.Clear();
        });

        //---------------------------------------------------- CONFIRM CALIBRATION RESULTS ----------------------------------------------------

        Confirm.AddSpecificInitializationMethod(() =>
        {
            if (!Session.SessionDef.SpoofGazeWithMouse)
            {
                InfoString.Append("\n\nInfo"
                                + "\nPress = to accept the point"
                                + "\nPress - to recalibrate the point");


                // Plots sample points to the Result Container, if they exist for the current calibration point
                //CollectSamplePoints();
                //CreateSampleLines(LeftSamples, RightSamples, (Vector2)USE_CoordinateConverter.GetScreenPixel(calibPointsADCS[calibNum].ToVector2(), "screenadcs", 60));


                if (ResultContainer.transform.childCount > 0)
                {
                    ResultsString.Append($"\n\nCalibration Results"
                                         + $"\nLeft Eye"
                                         + $"\n{CalculateSampleStatistics(LeftSampleDistances)}"
                                         + $"\n\nRight Eye "
                                         + $"\n{CalculateSampleStatistics(RightSampleDistances)}");
                }
                else
                {
                    ResultsString.Append($"No Samples Collected at this Calibration Point: <b>({String.Format("{0:0.00}", calibPointsADCS[calibNum].X)}, {String.Format("{0:0.00}", calibPointsADCS[calibNum].Y)})</b>");
                }
            }

            SetTrialSummaryString();


            if (ShouldGiveReward())
            {
                StartCoroutine(Session.SyncBoxController?.SendRewardPulses(CurrentTrialDef.NumPulses, CurrentTrialDef.PulseSize));

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
                if (!Session.SessionDef.SpoofGazeWithMouse)
                    Session.TobiiEyeTrackerController.ScreenBasedCalibration.DiscardData(currentNormPoint);
                recalibPoint = true;
                RecalibCount[calibNum] += 1;
            }
        });

        // Dictates the subsequent state given the outcome of the User validation
        Confirm.SpecifyTermination(() => recalibPoint, Fixate);

        Confirm.SpecifyTermination(() => pointFinished, Fixate, () => { calibNum++; });

        Confirm.SpecifyTermination(() => calibrationFinished, ITI, () =>
        {
            //COMPUTE THE RESULTS AFTER ALL POINTS ARE DONE:
            CalibrationResult = Session.TobiiEyeTrackerController.ScreenBasedCalibration.ComputeAndApply(); // Collects eye tracking data at the current calibration point, computes the calibration settings, and applies them to the eye tracker.
        });

        Confirm.AddTimer(() => CurrentTrialDef.ConfirmDuration, Delay, () =>
        {
            DelayDuration = 0;

            if (!(calibNum == calibPointsADCS.Length - 1))
            {
                // Return to the Blinking state to calibrate the next point, if all points haven't been calibrated yet
                StateAfterDelay = Fixate;
                calibNum++;
            }
            else
            {
                // Continues to ITI state since all points have been calibrated already
                StateAfterDelay = ITI;

                //COMPUTE THE RESULTS AFTER ALL POINTS ARE DONE:
                CalibrationResult = Session.TobiiEyeTrackerController.ScreenBasedCalibration.ComputeAndApply(); // Collects eye tracking data at the current calibration point, computes the calibration settings, and applies them to the eye tracker.

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


        ITI.SpecifyTermination(() => true, FinishTrial, () =>
        {
            // Destroy remaining results on the experimenter display at the end of the trial
            DestroyChildren(ResultContainer);
        });

        DefineTrialData();
        DefineFrameData();

    }



    // ---------------------------------------------------------- METHODS ----------------------------------------------------------
    private bool ShouldGiveReward()
    {
        string rewardStructure = CurrentTaskDef.RewardStructure?.ToLower();

        if (string.IsNullOrEmpty(rewardStructure))
        {
            Debug.LogError("REWARD STRUCTURE STRING IS NULL OR EMPTY! String = " + rewardStructure);
            return false;
        }

        bool isPerPoint = rewardStructure == "perpoint";
        bool isOnCompletion = rewardStructure == "oncompletion" && calibNum == calibPointsADCS.Length - 1;

        if (isPerPoint || isOnCompletion)
        {
            Debug.LogWarning($"REWARDSTRUCTURE STRING IS CORRECTLY EITHER PerPoint or OnCompletion!");
            return true;
        }

        Debug.LogError($"STRING DOES NOT EQUAL PERPOINT OR ONCOMPLETION! String = {rewardStructure}");
        return false;
    }


    private void OnApplicationQuit()
    {
        TurnOffCalibration();
    }

    private void TurnOnCalibration()
    {
        if(Session.TobiiEyeTrackerController == null)
        {
            Debug.LogError("SESSION OR TOBII EYE TRACKER IS NULL");
            return;
        }

        if (Session.SessionDef.SpoofGazeWithMouse)
            return;
        
        if (Session.TobiiEyeTrackerController.ScreenBasedCalibration != null)
            Session.TobiiEyeTrackerController.ScreenBasedCalibration.EnterCalibrationMode();
        else
            Debug.LogError("SCREEN BASED CALIBRATION IS NULL!");
    }
    
    private void TurnOffCalibration()
    {
       if (Session.TobiiEyeTrackerController != null && Session.TobiiEyeTrackerController.isCalibrating)
        {
            Session.TobiiEyeTrackerController.isCalibrating = false;
            Session.TobiiEyeTrackerController.ScreenBasedCalibration.LeaveCalibrationMode();

        }
    }
    

    public override void DefineCustomTrialDefSelection()
    {
        TrialDefSelectionStyle = "gazeCalibration";
    }

    private void ShrinkGameObject(GameObject gameObject, float targetSize, float shrinkDuration)
    {
        Vector3 finalScale = new Vector3(targetSize, targetSize, targetSize);
        gameObject.SetActive(true);

        float progress = elapsedShrinkDuration / shrinkDuration;
        gameObject.transform.localScale = Vector3.Lerp(originalScale, finalScale, progress);
    }

    private void DefineCalibPoints(int nPoints)
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
                //acceptableCalibrationDistance = Vector2.Distance((Vector2)USE_CoordinateConverter.GetScreenPixel(allCalibPoints[0].ToVector2(), "screenadcs", 60), (Vector2)USE_CoordinateConverter.GetScreenPixel(allCalibPoints[1].ToVector2(), "screenadcs", 60)) / 2;

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
                //acceptableCalibrationDistance = Vector2.Distance((Vector2)USE_CoordinateConverter.GetScreenPixel(allCalibPoints[0].ToVector2(), "screenadcs", 60), (Vector2)USE_CoordinateConverter.GetScreenPixel(allCalibPoints[1].ToVector2(), "screenadcs", 60)) / 2;

                RecalibCount = new int[6];
                break;
            case 5:
                calibPointsADCS = new NormalizedPoint2D[5] {
                allCalibPoints [4],
                allCalibPoints [0],
                allCalibPoints [2],
                allCalibPoints [6],
                allCalibPoints [8]};
                //acceptableCalibrationDistance = Vector2.Distance((Vector2)USE_CoordinateConverter.GetScreenPixel(allCalibPoints[0].ToVector2(), "screenadcs", 60), (Vector2)USE_CoordinateConverter.GetScreenPixel(allCalibPoints[4].ToVector2(), "screenadcs", 60)) / 2;

                RecalibCount = new int[5];
                break;
            case 3:
                calibPointsADCS = new NormalizedPoint2D[3]{
                allCalibPoints [4],
                allCalibPoints [3],
                allCalibPoints [5] };
                //acceptableCalibrationDistance = Vector2.Distance((Vector2)USE_CoordinateConverter.GetScreenPixel(allCalibPoints[0].ToVector2(), "screenadcs", 60), (Vector2)USE_CoordinateConverter.GetScreenPixel(allCalibPoints[1].ToVector2(), "screenadcs", 60)) / 2; 

                RecalibCount = new int[3];
                break;
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
            for (int i = 0; i < calibPoint.CalibrationSamples.Count; i++)
            {
                CalibrationSample sample = calibPoint.CalibrationSamples[i];
                Vector2 leftSamplePos = (Vector2)USE_CoordinateConverter.GetScreenPixel(sample.LeftEye.PositionOnDisplayArea.ToVector2(), "screenadcs", 60);
                Vector2 rightSamplePos = (Vector2)USE_CoordinateConverter.GetScreenPixel(sample.RightEye.PositionOnDisplayArea.ToVector2(), "screenadcs", 60);
                LeftSamples.Add(leftSamplePos);
                RightSamples.Add(rightSamplePos);
            }

            // Log sample counts for debugging
            Debug.Log($"Collected {LeftSamples.Count} left samples and {RightSamples.Count} right samples.");

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

        // Check if we have elements in distances lists before calculating averages
        AverageCalibrationErrorL = LeftSampleDistances.Count > 0 ? LeftSampleDistances.Average() : 0f;
        AverageCalibrationErrorR = RightSampleDistances.Count > 0 ? RightSampleDistances.Average() : 0f;
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
        DistanceToCurrentPoint = Vector2.Distance((Vector2)SelectionHandler.CurrentInputLocation(), currentScreenPixelTarget);

        bool inRange = DistanceToCurrentPoint < acceptableCalibrationDistance;

        if(inRange)
        {
            TimeInCalibrationRange += Time.deltaTime;
            return true;
        }
        else
        {
            TimeInCalibrationRange = 0;
            return false;
        }
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

        currentScreenPixelTarget = new Vector2(float.NaN, float.NaN);

        if (LeftSamples.Count > 0)
            LeftSamples.Clear();
        
        if(RightSamples.Count > 0)
            RightSamples.Clear();

        CurrentProgressString.Clear();
        ResultsString.Clear();
        InfoString.Clear();

        AverageCalibrationErrorL = 0;
        AverageCalibrationErrorR = 0;
    }

    public override void FinishTrialCleanup()
    {
        if (GameObject.Find("CalibrationCircle") != null)
            CalibCircle.CircleGO.SetActive(false);
        if(GameObject.Find("ResultContainer") != null)
            DestroyChildren(ResultContainer);
    }

    public void InitializeEyeTrackerSettings()
    {
        if (IEyeTracker == null)
            IEyeTracker = Session.TobiiEyeTrackerController.iEyeTracker;
        if (EyeTracker == null)
            EyeTracker = Session.TobiiEyeTrackerController.EyeTracker;
    }

    private void DefineTrialData()
    {
        // Gaze Calibration specific trial data
        TrialData.AddDatum("NumCalibPoints", () => numCalibPoints); // Number of calibration points chosen
        TrialData.AddDatum("RecalibCount", () => RecalibCount?.Sum()); // Number of recalibrations for the current calibration point
        TrialData.AddDatum("CalibrationCompleted", () => calibrationFinished ? "Completed" : "Incomplete");
        TrialData.AddDatum("LeftAvgCalibrationError", () => AverageCalibrationErrorL); // average distance of all left eye results from the actually calib point
        TrialData.AddDatum("RightAvgCalibrationError", () => AverageCalibrationErrorR);// average distance of all left eye results from the actually calib point
    }
    private void DefineFrameData()
    {
        // Gaze Calibration specific frame data
        FrameData.AddDatum("CalibrationCircleVisible", () => CalibCircle?.CircleGO.activeSelf); // Whether the calibration circle is visible
        FrameData.AddDatum("CurrentCalibrationPointPosition", () => currentScreenPixelTarget);
        FrameData.AddDatum("InCalibrationRange", () => InCalibrationRange() ? 1 : 0); // If the gaze point is within the acceptable calibration range
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
        TrialSummaryString = "TimeInCalibRange: " + TimeInCalibrationRange.ToString("F2") + " s"
                             + "\n"
                             + CurrentProgressString.ToString()
                             + "\n"
                             + "\n"
                             + ResultsString.ToString()
                             + "\n"
                             + InfoString.ToString()
                             + "\n"
                             + "\n"
                             + DistanceToCurrentPoint.ToString();

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
