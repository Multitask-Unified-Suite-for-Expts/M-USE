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
using System.Collections;
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


    // Inherited from the Session Level...
    public MonitorDetails MonitorDetails;

    // Calibration Point Definition
    [HideInInspector] public NormalizedPoint2D[] allCalibPoints;
    [HideInInspector] public NormalizedPoint2D[] calibPointsADCS;
    [HideInInspector] public int numCalibPoints;
    private NormalizedPoint2D currentADCSTarget;
    private Vector2 currentScreenPixelTarget = new Vector2(float.NaN, float.NaN);
    private int calibNum;

    // Shrink Calibration Point Variables
    private Vector3 originalScale;
    private float elapsedShrinkDuration;

    // Calibration Assessment Variables
    [HideInInspector] public bool currentCalibrationPointFinished;
    [HideInInspector] public bool CalibrationAccepted;

    private int RecalibCount;
    private bool RestartCalibration;

    private NormalizedPoint2D currentNormPoint;

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

    // Experimenter Display Text Variables
    private StringBuilder InfoString = new StringBuilder();
    private StringBuilder CurrentProgressString = new StringBuilder();
    private StringBuilder ResultsString = new StringBuilder();


    private float TimeInCalibrationRange = 0f;

    private float DistanceToCurrentPoint = 0f;

    private List<USE_Circle> Circles = new List<USE_Circle>();



    public override void DefineControlLevel()
    {
        State Init = new State("Init");
        State Fixate = new State("Blink");
        State Shrink = new State("Shrink");
        State CollectData = new State("CollectData");
        State Reward = new State("Reward");
        State ApplyCalibration = new State("ApplyCalibration");
        State ConfirmResults = new State("ConfirmResults");
        State ITI = new State("ITI");

        AddActiveStates(new List<State> { Init, Fixate, Shrink, CollectData, Reward, ApplyCalibration, ConfirmResults, ITI });

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
                CreateResultContainer();

            AssignGazeCalibrationCameraToTrackboxCanvas();
        });
        SetupTrial.SpecifyTermination(() => Session.TobiiEyeTrackerController.ScreenBasedCalibration != null, Init);


        //SELECTION HANDLER ------------------------------------------------------------------------------------------------------------------------------------------
        SelectionHandler = Session.SelectionTracker.SetupSelectionHandler("trial", "GazeShotgun", Session.GazeTracker, Init, ITI);


        //----------------------------------------------------- INIT -------------------------------------------------------------------------------------------------
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
            DefineCalibPoints(numCalibPoints);
            InfoString.Clear();
            DistanceToCurrentPoint = 0;
        });

        //----------------------------------------------------- CONFIRM GAZE IS IN RANGE OF THE CALIBRATION POINT -----------------------------------------------------
        Fixate.AddSpecificInitializationMethod(() =>
        {
            // Initialize the Calibration Point at Max Scale
            InitializeCalibPoint(calibNum);

            currentCalibrationPointFinished = false;

            CurrentProgressString.Clear();
            CurrentProgressString.Append($"Calib Point #:  {calibNum + 1} (of {numCalibPoints})"
                                   + $"\nCalib Position: ({String.Format("{0:0.00}", calibPointsADCS[calibNum].X)}, {String.Format("{0:0.00}", calibPointsADCS[calibNum].Y)})"
                                   + $"\nRecalib Count: {RecalibCount}");

            InfoString.Clear();
            InfoString.Append("Info"
                                + "\nThe calibration point is blinking."
                                + "\nInstruct the player to focus on the point until the circle shrinks.");

            SetTrialSummaryString();


            ResultsString.Clear();
            LeftSamples.Clear();
            LeftSampleDistances.Clear();
            RightSamples.Clear();
            RightSampleDistances.Clear();


            //Reset before fixation
            TimeInCalibrationRange = 0;

            CalibCircle.CircleGO.SetActive(true);

            RestartCalibration = false;


        });

        Fixate.AddUpdateMethod(() =>
        {
            SetTrialSummaryString();
        });

        Fixate.SpecifyTermination(() => InCalibrationRange(), Shrink, () => { InfoString.Clear(); });

        //----------------------------------------------------- SHRINK THE CALIBRATION POINT WHILE IN CALIBRATION RANGE -----------------------------------------------------
        Shrink.AddSpecificInitializationMethod(() =>
        {
            InfoString.Append("\n\nInfo"
                                + "\nThe calibration point is shrinking."
                                + "\nContinue to focus on the point to prepare for calibration.");
            SetTrialSummaryString();

            originalScale = CalibCircle.CircleGO.transform.localScale;

            elapsedShrinkDuration = 0;
        });

        Shrink.AddUpdateMethod(() =>
        {
            SetTrialSummaryString();

            elapsedShrinkDuration += Time.deltaTime;

            ShrinkGameObject(CalibCircle.CircleGO, CurrentTrialDef.MinCircleScale, CurrentTrialDef.ShrinkDuration);
        });

        Shrink.SpecifyTermination(() => InCalibrationRange() && elapsedShrinkDuration > (CurrentTrialDef.ShrinkDuration - 0.05f), CollectData, () =>
        {
            currentNormPoint = calibPointsADCS[calibNum];
        });
        Shrink.SpecifyTermination(() => !InCalibrationRange() && elapsedShrinkDuration != 0, Fixate);


        //-------------------------------------------------------- COLLECT DATA --------------------------------------------------------
        int unsuccessfulCount = 0;

        CollectData.AddSpecificInitializationMethod(() =>
        {
            InfoString.Clear();
            InfoString.Append("\nInfo"
                                + $"Collecting Data for point at: ({String.Format("{0:0.00}", calibPointsADCS[calibNum].X)}, {String.Format("{0:0.00}", calibPointsADCS[calibNum].Y)})");

            SetTrialSummaryString();

            unsuccessfulCount = 0;

            currentCalibrationPointFinished = false;
        });

        CollectData.AddUpdateMethod(() =>
        {
            SetTrialSummaryString();

            //Keep shrinking the circle during Check state (which should only be 1 frame anyway);
            ShrinkGameObject(CalibCircle.CircleGO, CurrentTrialDef.MinCircleScale, CurrentTrialDef.ShrinkDuration);

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
                    Debug.LogWarning("CollectData() CALIBRATION METHOD WAS UNSUCCESSFUL MULTIPLE TIMES!!!!");
            }
        });

        CollectData.SpecifyTermination(() => currentCalibrationPointFinished, Reward, () =>
        {
            // Make sure that the Scale is set to the min scale
            CalibCircle.SetCircleScale(CurrentTrialDef.MinCircleScale);
            CalibCircle.CircleGO.GetComponent<UnityEngine.UI.Extensions.UICircle>().color = Color.green;

            InfoString.Clear();
        });

        //---------------------------------------------------- Reward -------------------------------------------------------------------------
        Reward.AddSpecificInitializationMethod(() =>
        {
            if (ShouldGiveReward())
            {
                StartCoroutine(Session.SyncBoxController?.SendRewardPulses(CurrentTrialDef.NumPulses, CurrentTrialDef.PulseSize));
                CurrentTaskLevel.NumRewardPulses_InBlock += CurrentTrialDef.NumPulses;
                CurrentTaskLevel.NumRewardPulses_InTask += CurrentTrialDef.NumPulses;
            }
        });
        Reward.SpecifyTermination(() => true, Delay, () =>
        {
            if (calibNum == calibPointsADCS.Length - 1)
            {
                StateAfterDelay = ApplyCalibration;
            }
            else
            {
                StateAfterDelay = Fixate;
                calibNum++;
            }
        });

        //---------------------------------------------------- COMPUTE AND APPLY CALIBRATION RESULTS ----------------------------------------------------
        ApplyCalibration.AddSpecificInitializationMethod(() =>
        {
            //COMPUTE AND APPLY THE CALIBRATION RESULTS AFTER ALL POINTS ARE DONE:
            CalibrationResult = Session.TobiiEyeTrackerController.ScreenBasedCalibration.ComputeAndApply(); // Collects eye tracking data at the current calibration point, computes the calibration settings, and applies them to the eye tracker.

            if (CalibrationResult == null)
                Debug.LogError("CALIBRATION RESULT IS NULL AFTER COMPUTING AND APPLYING");
        });
        ApplyCalibration.SpecifyTermination(() => true, ConfirmResults);


        //---------------------------------------------------- CONFIRM CALIBRATION RESULTS ----------------------------------------------------
        ConfirmResults.AddSpecificInitializationMethod(() =>
        {
            ShowAllCalibPoints();

            ShowSamplePointsInRange();


            InfoString.Append("\nInfo"
                            + "\nPress = to accept the calibration"
                            + "\nPress - to start the calibration over");

            SetTrialSummaryString();
        });

        ConfirmResults.AddUpdateMethod(() =>
        {
            if (InputBroker.GetKeyDown(KeyCode.Equals))
            {
                CalibrationAccepted = true;
            }
            else if (InputBroker.GetKeyDown(KeyCode.Minus))
            {
                RestartCalibration = true;

                RecalibCount++;

                //DO WE ALSO WANT TO DISCARD DATA FOR ALL POINTS?
                //for(int i = 0; i < calibPointsADCS.Length; i++)
                //{
                //    Session.TobiiEyeTrackerController.ScreenBasedCalibration.DiscardData(calibPointsADCS[i]);
                //}
            }
        });

        ConfirmResults.SpecifyTermination(() => RestartCalibration, Fixate, () => { calibNum = 0; });
        ConfirmResults.SpecifyTermination(() => CalibrationAccepted, ITI);
        ConfirmResults.AddTimer(() => CurrentTrialDef.ConfirmDuration, ITI);

        //ITI---------------------------------------------------------------------------------------------------------------------
        ITI.AddSpecificInitializationMethod(() =>
        {
            CalibCircle.CircleGO.SetActive(false);
            DestroyChildren(ResultContainer);
        });
        ITI.SpecifyTermination(() => true, FinishTrial);


        DefineTrialData();
        DefineFrameData();

    }



    // ---------------------------------------------------------- METHODS ----------------------------------------------------------
    private bool ShouldGiveReward()
    {
        string rewardStructure = CurrentTaskDef.RewardStructure.ToLower();

        if (string.IsNullOrEmpty(rewardStructure))
        {
            Debug.LogError("REWARD STRUCTURE STRING IS NULL OR EMPTY! String = " + rewardStructure);
            return false;
        }

        if (rewardStructure != "perpoint" && rewardStructure != "oncompletion")
        {
            Debug.LogError("REWARD STRCTURE DOES NOT EQUAL PerPoint or OnCompletion");
            return false;
        }

        bool isPerPoint = rewardStructure == "perpoint";
        bool isOnCompletion = rewardStructure == "oncompletion" && calibNum == calibPointsADCS.Length - 1;

        if (isPerPoint || isOnCompletion)
            return true;

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

                RecalibCount = 9;
                break;
            case 6:
                calibPointsADCS = new NormalizedPoint2D[6] {
                allCalibPoints [4],
                allCalibPoints [0],
                allCalibPoints [1],
                allCalibPoints [2],
                allCalibPoints [3],
                allCalibPoints[5]};

                RecalibCount = 6;
                break;
            case 5:
                calibPointsADCS = new NormalizedPoint2D[5] {
                allCalibPoints [4],
                allCalibPoints [0],
                allCalibPoints [2],
                allCalibPoints [6],
                allCalibPoints [8]};

                RecalibCount = 5;
                break;
            case 3:
                calibPointsADCS = new NormalizedPoint2D[3]{
                allCalibPoints [4],
                allCalibPoints [3],
                allCalibPoints [5] };

                RecalibCount = 3;
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



    private void ShowSamplePointsInRange()
    {
        if (CalibrationResult == null)
        {
            Debug.LogWarning("No calibration result so not showing sample points");
            return;
        }

        LeftSamples.Clear();
        RightSamples.Clear();

        int threshold = 250;

        for(int i = 0; i < CalibrationResult.CalibrationPoints.Count; i++)
        {
            var calibResultPoint = CalibrationResult.CalibrationPoints[i];

            for (int j = 0; j < calibResultPoint.CalibrationSamples.Count; i++)
            {
                CalibrationSample sample = calibResultPoint.CalibrationSamples[j];

                Vector2 leftSamplePos = (Vector2)USE_CoordinateConverter.GetScreenPixel(sample.LeftEye.PositionOnDisplayArea.ToVector2(), "screenadcs", 60);
                Vector2 rightSamplePos = (Vector2)USE_CoordinateConverter.GetScreenPixel(sample.RightEye.PositionOnDisplayArea.ToVector2(), "screenadcs", 60);

                float leftSampleDistance = Vector2.Distance(leftSamplePos, (Vector2)USE_CoordinateConverter.GetScreenPixel(calibPointsADCS[i].ToVector2(), "screenadcs", 60f));
                float rightSampleDistance = Vector2.Distance(rightSamplePos, (Vector2)USE_CoordinateConverter.GetScreenPixel(calibPointsADCS[i].ToVector2(), "screenadcs", 60f));

                Debug.LogWarning("LEFT SAMPLE DISTANCE = " + leftSampleDistance + " | RIGHT SAMPLE DISTANCE: " + rightSampleDistance);

                if (leftSampleDistance <= threshold && rightSampleDistance <= threshold)
                {
                    LeftSamples.Add(leftSamplePos);
                    RightSamples.Add(rightSamplePos);

                    LeftSampleDistances.Add(leftSampleDistance);
                    RightSampleDistances.Add(rightSampleDistance);
                }
                else
                    Debug.LogWarning("Atleast one sample was outside threshold");
            }

            CreateSampleCircles();
        }
    }

    private void CreateSampleCircles()
    {
        if (LeftSamples != null && LeftSamples.Count > 0)
        {
            for (int i = 0; i < LeftSamples.Count; i++)
            {
                USE_Circle leftSampleCircle = new USE_Circle(ResultContainer.GetComponent<Canvas>(), LeftSamples[i], 5, "LeftSampleCircle_ " + i);
                leftSampleCircle.CircleGO.GetComponent<UnityEngine.UI.Extensions.UICircle>().color = Color.cyan;
                leftSampleCircle.CircleGO.SetActive(true);
                Circles.Add(leftSampleCircle);
            }
        }
        else
            Debug.LogWarning("NO LEFT SAMPLES");

        if (RightSamples != null && RightSamples.Count > 0)
        {
            for (int i = 0; i < RightSamples.Count; i++)
            {
                USE_Circle rightSampleCircle = new USE_Circle(ResultContainer.GetComponent<Canvas>(), RightSamples[i], 5, "RightSampleCircle_ " + i);
                rightSampleCircle.CircleGO.GetComponent<UnityEngine.UI.Extensions.UICircle>().color = Color.magenta;
                rightSampleCircle.CircleGO.SetActive(true);
                Circles.Add(rightSampleCircle);
            }
        }
        else
            Debug.LogWarning("NO RIGHT SAMPLES");
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

    private void InitializeCalibPoint(int index)
    {
        CalibCircle.CircleGO.GetComponent<UnityEngine.UI.Extensions.UICircle>().color = Color.black;
        CalibCircle.SetCircleScale(CurrentTrialDef.MaxCircleScale);
        currentADCSTarget = calibPointsADCS[index]; // get calib coordinates in screen ADCS space
        currentScreenPixelTarget = (Vector2)USE_CoordinateConverter.GetScreenPixel(currentADCSTarget.ToVector2(), "screenadcs", 60); // get calib coordinates in Screen space
        CalibCircle.CircleGO.GetComponent<RectTransform>().anchoredPosition = currentScreenPixelTarget;
        CalibCircle.CircleGO.SetActive(true);
    }

    private void ShowAllCalibPoints()
    {
        if(calibPointsADCS.Length < 1)
        {
            Debug.LogWarning("CANT INITIALIZE ALL CALIB POINTS BECAUSE ITS LENGTH IS LESS THAN 1");
            return;
        }

        for(int i = 0; i < calibPointsADCS.Length; i++)
        {
            var calibPoint = calibPointsADCS[i];

            var location = (Vector2)USE_CoordinateConverter.GetScreenPixel(calibPoint.ToVector2(), "screenadcs", 60);

            USE_Circle calibCircle = new USE_Circle(ResultContainer.GetComponent<Canvas>(), location, CurrentTrialDef.MaxCircleScale, "CalibCircle_Point" + i);
            calibCircle.CircleGO.GetComponent<UnityEngine.UI.Extensions.UICircle>().color = Color.green;
            calibCircle.CircleGO.SetActive(true);
            Circles.Add(calibCircle);
        }
    }

    private bool InCalibrationRange()
    {
        DistanceToCurrentPoint = Vector2.Distance((Vector2)SelectionHandler.CurrentInputLocation(), currentScreenPixelTarget);

        bool inRange = DistanceToCurrentPoint < CurrentTaskDef.AcceptableDistance_Pixels;

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

        RestartCalibration = false;
        CalibrationAccepted = false;

        currentScreenPixelTarget = new Vector2(float.NaN, float.NaN);

        if (LeftSamples.Count > 0)
            LeftSamples.Clear();
        
        if(RightSamples.Count > 0)
            RightSamples.Clear();

        CurrentProgressString.Clear();
        ResultsString.Clear();
        InfoString.Clear();

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
        TrialData.AddDatum("RecalibCount", () => RecalibCount); // Number of recalibrations
        TrialData.AddDatum("CalibrationCompleted", () => CalibrationAccepted ? "Completed" : "Incomplete");
    }
    private void DefineFrameData()
    {
        // Gaze Calibration specific frame data
        FrameData.AddDatum("CalibrationCircleVisible", () => CalibCircle?.CircleGO.activeSelf); // Whether the calibration circle is visible
        FrameData.AddDatum("CurrentCalibrationPointPosition", () => currentScreenPixelTarget);
        FrameData.AddDatum("InCalibrationRange", () => InCalibrationRange() ? 1 : 0); // If the gaze point is within the acceptable calibration range
    }

    private void SetTrialSummaryString()
    {
        TrialSummaryString = "TimeInCalibRange: " + TimeInCalibrationRange.ToString("F2") + " s"
                             + "\n"
                             + "\n"
                             + "Acceptable Distance: " + CurrentTaskDef.AcceptableDistance_Pixels.ToString()
                             + "\n"
                             + "Distance To Point: " + DistanceToCurrentPoint.ToString()
                             + "\n"
                             + "\n"
                             + CurrentProgressString.ToString()
                             + "\n"
                             + ResultsString.ToString()
                             + "\n"
                             + InfoString.ToString();


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
