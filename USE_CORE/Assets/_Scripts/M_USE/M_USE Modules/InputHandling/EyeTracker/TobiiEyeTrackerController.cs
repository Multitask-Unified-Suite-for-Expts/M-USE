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
            EyeTracker = GameObject.Find("EyeTracker(Clone)").GetComponent<EyeTracker>();

        if (TrackBoxGuideGO == null && GameObject.Find("TrackBoxGuide(Clone)") != null)
            TrackBoxGuideGO = GameObject.Find("TrackBoxGuide(Clone)");

    }

    public void OnGazeDataReceived(object sender, GazeDataEventArgs e)
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

        gazeSample.systemTimeStamp = e.SystemTimeStamp;

        GazeData.AppendData();
        //GAZEDATA.APPENDDATA()
    }
}
