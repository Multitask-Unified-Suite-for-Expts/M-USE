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
    //MOST RECENT GAZE DATA FIELD, OVERWRITTEN 

    //IENUMERAT
   
    // Start is called before the first frame update
    private void Awake()
    {
        Instance = this;
        base.Awake();
        GazeDataSubscription = new TobiiGazeDataSubscription();
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
        if (iEyeTracker == null && EyeTrackingOperations.FindAllEyeTrackers().Count > 0)
        {
            iEyeTracker = EyeTrackingOperations.FindAllEyeTrackers()[0];
            ScreenBasedCalibration = new ScreenBasedCalibration(iEyeTracker);
            GazeDataSubscription._eyeTracker = iEyeTracker;
            DisplayArea = iEyeTracker.GetDisplayArea();

        }

        if (EyeTracker == null && GameObject.Find("EyeTracker(Clone)") != null)
        {
            EyeTracker = GameObject.Find("EyeTracker(Clone)").GetComponent<EyeTracker>();
        //    EyeTracker.SubscribeToGazeData = true;

        }

        if (TrackBoxGuideGO == null && GameObject.Find("TrackBoxGuide(Clone)") != null)
            TrackBoxGuideGO = GameObject.Find("TrackBoxGuide(Clone)");

    }

    // public void OnGazeDataReceived(GazeDataEventArgs e)
    // {
    //     // Left Eye Data
    //     mostRecentGazeSample.leftPupilValidity = e.LeftEye.Pupil.Validity.ToString();
    //     mostRecentGazeSample.leftGazeOriginValidity = e.LeftEye.GazeOrigin.Validity.ToString();
    //     mostRecentGazeSample.leftGazePointValidity = e.LeftEye.GazePoint.Validity.ToString();
    //     mostRecentGazeSample.leftGazePointOnDisplayArea = e.LeftEye.GazePoint.PositionOnDisplayArea.ToVector2();
    //     mostRecentGazeSample.leftGazeOriginInUserCoordinateSystem = e.LeftEye.GazeOrigin.PositionInUserCoordinates.ToVector3();
    //     mostRecentGazeSample.leftGazePointInUserCoordinateSystem = e.LeftEye.GazePoint.PositionInUserCoordinates.ToVector3();
    //     mostRecentGazeSample.leftGazeOriginInTrackboxCoordinateSystem = e.LeftEye.GazeOrigin.PositionInTrackBoxCoordinates.ToVector3();
    //     mostRecentGazeSample.leftPupilDiameter = e.LeftEye.Pupil.PupilDiameter;
    //
    //     // Right Eye Data
    //     mostRecentGazeSample.rightPupilValidity = e.RightEye.Pupil.Validity.ToString();
    //     mostRecentGazeSample.rightGazeOriginValidity = e.RightEye.GazeOrigin.Validity.ToString();
    //     mostRecentGazeSample.rightGazePointValidity = e.RightEye.GazePoint.Validity.ToString();
    //     mostRecentGazeSample.rightGazePointOnDisplayArea = e.RightEye.GazePoint.PositionOnDisplayArea.ToVector2();
    //     mostRecentGazeSample.rightGazeOriginInUserCoordinateSystem = e.RightEye.GazeOrigin.PositionInUserCoordinates.ToVector3();
    //     mostRecentGazeSample.rightGazePointInUserCoordinateSystem = e.RightEye.GazePoint.PositionInUserCoordinates.ToVector3();
    //     mostRecentGazeSample.rightGazeOriginInTrackboxCoordinateSystem = e.RightEye.GazeOrigin.PositionInTrackBoxCoordinates.ToVector3();
    //     mostRecentGazeSample.rightPupilDiameter = e.RightEye.Pupil.PupilDiameter;
    //
    //     mostRecentGazeSample.systemTimeStamp = e.SystemTimeStamp;
    //
    //     Debug.Log("GAZE DATA NAME??? " + GazeData.fileName);
    //    
    //     GazeData.AppendData();
    //     //GAZEDATA.APPENDDATA()
    // }

    public void AppendGazeData()
    {
       GazeDataSubscription.PumpGazeData();
    }
    
    // the events in the SDK are called on a thread internal to the SDK. That thread can not safely set values
    // that are to be read on the main thread. The simplest way to make it safe is to enqueue the date, and dequeue it
    // on the main thread, e.g. via Update() in a MonoBehaviour.
    public class TobiiGazeDataSubscription : MonoBehaviour
    {
        private TobiiEyeTrackerController TobiiEyeTrackerController;
        public IEyeTracker _eyeTracker;
        private Queue<GazeDataEventArgs> _queue = new Queue<GazeDataEventArgs>();
        public TobiiGazeSample mostRecentGazeSample;
        public USE_ExperimentTemplate_Data.GazeData GazeData;

        void Awake()
        {
        }

        void Update()
        {
            //PumpGazeData();
        }

        void OnEnable()
        {
            if (_eyeTracker != null)
            {
                Debug.Log("Calling OnEnable with eyetracker: " + _eyeTracker.DeviceName);
                _eyeTracker.GazeDataReceived += EnqueueEyeData;
            }
        }

        void OnDisable()
        {
            if (_eyeTracker != null)
            {
                _eyeTracker.GazeDataReceived -= EnqueueEyeData;
            }
        }

        void OnDestroy()
        {
            EyeTrackingOperations.Terminate();
        }

        // This method will be called on a thread belonging to the SDK, and can not safely change values
        // that will be read from the main thread.
        private void EnqueueEyeData(object sender, GazeDataEventArgs e)
        {
            lock (_queue)
            {
                _queue.Enqueue(e);
            }
        }

        private GazeDataEventArgs GetNextGazeData()
        {
            lock (_queue)
            {
                return _queue.Count > 0 ? _queue.Dequeue() : null;
            }
        }

        public void PumpGazeData()
        {
            var next = GetNextGazeData();
            while (next != null)
            {
                HandleGazeData(next);
                next = GetNextGazeData();
            }
        }

        // This method will be called on the main Unity thread
        private void HandleGazeData(GazeDataEventArgs e)
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
            GazeData.AppendData();
        }
    }
}
