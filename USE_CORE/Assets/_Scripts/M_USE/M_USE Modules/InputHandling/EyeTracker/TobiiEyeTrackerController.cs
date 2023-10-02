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



using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tobii.Research;
using Tobii.Research.Unity;
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
    //public USE_ExperimentTemplate_Data.GazeData GazeData;
   
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
            //SessionValues.TobiiEyeTrackerController = this;
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
