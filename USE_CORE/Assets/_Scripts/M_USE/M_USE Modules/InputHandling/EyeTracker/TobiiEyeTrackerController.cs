using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tobii.Research;
using Tobii.Research.Unity;
using USE_DisplayManagement;
using EyeTrackerData_Namespace;
using Tobii.Research.Unity.CodeExamples;

public class TobiiEyeTrackerController : EyeTrackerController_Base
{
    public static TobiiEyeTrackerController Instance { get; private set; } 
    public IEyeTracker iEyeTracker;
    public EyeTracker EyeTracker;
    public ScreenBasedCalibration ScreenBasedCalibration;
    public DisplayArea DisplayArea;
    public GameObject TrackBoxGuideGO;
    public Camera Camera;
    public bool isCalibrating;

    public TobiiGazeSample mostRecentGazeSample;
    public TobiiGazeDataSubscription GazeDataSubscription;
    public USE_ExperimentTemplate_Data.GazeData GazeData;
   
    // Start is called before the first frame update
    private void Awake()
    {
        Instance = this;
        base.Awake();
        mostRecentGazeSample = new TobiiGazeSample();

    }

    // Update is called once per frame
    void Update()
    {
        Camera = Camera.main;

        while (iEyeTracker == null  || EyeTracker == null || TrackBoxGuideGO == null)
            FindEyeTrackerComponents();

    }

    public override void FindEyeTrackerComponents()
    {
        // An eyetracker is connected and on

        if (EyeTrackingOperations.FindAllEyeTrackers().Count == 0)
            return;

        if (iEyeTracker == null)
        {
            iEyeTracker = EyeTrackingOperations.FindAllEyeTrackers()[0];
            ScreenBasedCalibration = new ScreenBasedCalibration(iEyeTracker);

            DisplayArea = iEyeTracker.GetDisplayArea();

        }

        else if (EyeTracker == null && GameObject.Find("EyeTracker(Clone)") != null)
        {
            EyeTracker = GameObject.Find("EyeTracker(Clone)").GetComponent<EyeTracker>();
            GazeDataSubscription = GameObject.Find("EyeTracker(Clone)").GetComponent<TobiiGazeDataSubscription>();
            GazeDataSubscription._eyeTracker = iEyeTracker;

            Debug.Log("is iEYE TRACKER NULL: " + (iEyeTracker == null ? "Yes" : "No"));
            iEyeTracker.GazeDataReceived += GazeDataSubscription.EnqueueEyeData;
            GazeDataSubscription.TobiiEyeTrackerController = this;
            //    EyeTracker.SubscribeToGazeData = true;

        }

        else if (TrackBoxGuideGO == null && GameObject.Find("TrackBoxGuide(Clone)") != null)
            TrackBoxGuideGO = GameObject.Find("TrackBoxGuide(Clone)");

    }
/*
    public void OnGazeDataReceived(GazeDataEventArgs e)
    {
        // Left Eye Data
        mostRecentGazeSample.leftPupilValidity = e.LeftEye.Pupil.Validity.ToString();
        mostRecentGazeSample.leftGazeOriginValidity = e.LeftEye.GazeOrigin.Validity.ToString();
        mostRecentGazeSample.leftGazePointValidity = e.LeftEye.GazePoint.Validity.ToString();
        mostRecentGazeSample.leftGazePointOnDisplayArea = e.LeftEye.GazePoint.PositionOnDisplayArea.ToVector2();
        mostRecentGazeSample.leftGazeOriginInUserCoordinateSystem = e.LeftEye.GazeOrigin.PositionInUserCoordinates.ToVector3();
        mostRecentGazeSample.leftGazePointInUserCoordinateSystem = e.LeftEye.GazePoint.PositionInUserCoordinates.ToVector3();
        mostRecentGazeSample.leftGazeOriginInTrackboxCoordinateSystem = e.LeftEye.GazeOrigin.PositionInTrackBoxCoordinates.ToVector3();
        mostRecentGazeSample.leftPupilDiameter = e.LeftEye.Pupil.PupilDiameter;

        // Right Eye Data
        mostRecentGazeSample.rightPupilValidity = e.RightEye.Pupil.Validity.ToString();
        mostRecentGazeSample.rightGazeOriginValidity = e.RightEye.GazeOrigin.Validity.ToString();
        mostRecentGazeSample.rightGazePointValidity = e.RightEye.GazePoint.Validity.ToString();
        mostRecentGazeSample.rightGazePointOnDisplayArea = e.RightEye.GazePoint.PositionOnDisplayArea.ToVector2();
        mostRecentGazeSample.rightGazeOriginInUserCoordinateSystem = e.RightEye.GazeOrigin.PositionInUserCoordinates.ToVector3();
        mostRecentGazeSample.rightGazePointInUserCoordinateSystem = e.RightEye.GazePoint.PositionInUserCoordinates.ToVector3();
        mostRecentGazeSample.rightGazeOriginInTrackboxCoordinateSystem = e.RightEye.GazeOrigin.PositionInTrackBoxCoordinates.ToVector3();
        mostRecentGazeSample.rightPupilDiameter = e.RightEye.Pupil.PupilDiameter;

        mostRecentGazeSample.systemTimeStamp = e.SystemTimeStamp;

    }
*/
}