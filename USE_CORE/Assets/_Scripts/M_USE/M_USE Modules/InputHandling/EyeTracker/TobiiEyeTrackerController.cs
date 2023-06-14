using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tobii.Research;
using Tobii.Research.Unity;
using USE_DisplayManagement;
using EyeTrackerData_Namespace;

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
    public USE_ExperimentTemplate_Data.GazeData GazeData;

    //MOST RECENT GAZE DATA FIELD, OVERWRITTEN 

    //IENUMERAT
   
    // Start is called before the first frame update
    private void Awake()
    {
        Instance = this;
        base.Awake();

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
        if (iEyeTracker == null && EyeTrackingOperations.FindAllEyeTrackers().Count > 0)
        {
            iEyeTracker = EyeTrackingOperations.FindAllEyeTrackers()[0];
            ScreenBasedCalibration = new ScreenBasedCalibration(iEyeTracker);

            iEyeTracker.GazeDataReceived += OnGazeDataReceived;
            DisplayArea = iEyeTracker.GetDisplayArea();
        }

        if (EyeTracker == null && GameObject.Find("EyeTracker(Clone)") != null)
        {
            EyeTracker = GameObject.Find("EyeTracker(Clone)").GetComponent<EyeTracker>();
            EyeTracker.SubscribeToGazeData = true;

        }

        if (TrackBoxGuideGO == null && GameObject.Find("TrackBoxGuide(Clone)") != null)
            TrackBoxGuideGO = GameObject.Find("TrackBoxGuide(Clone)");

    }

    public void OnGazeDataReceived(object sender, GazeDataEventArgs e)
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

        Debug.Log("like im in here!!!");
        Debug.Log("GAZE DATA NAME??? " + GazeData.fileName);
        GazeData.AppendData();
        //GAZEDATA.APPENDDATA()
    }
}
