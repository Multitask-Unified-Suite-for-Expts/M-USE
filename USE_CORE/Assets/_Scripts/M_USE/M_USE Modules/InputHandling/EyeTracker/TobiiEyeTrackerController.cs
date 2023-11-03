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
    public IEyeTracker iEyeTracker;
    public EyeTracker EyeTracker;
    
    public GameObject EyeTracker_GO;
    public GameObject TrackBoxGuide_GO;
    public GameObject GazeTrail_GO;

    public ScreenBasedCalibration ScreenBasedCalibration;
    public DisplayArea DisplayArea;
    public Camera Camera;
    public bool isCalibrating;

    public TobiiGazeSample mostRecentGazeSample;
    public TobiiGazeDataSubscription GazeDataSubscription;
    //public USE_ExperimentTemplate_Data.GazeData GazeData;
   
    // Start is called before the first frame update
    private void Awake()
    {
        base.Awake();
        mostRecentGazeSample = new TobiiGazeSample();
    }

    // Update is called once per frame
    void Update()
    {
        Camera = Camera.main;

        while (iEyeTracker == null  || EyeTracker == null)
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

        else if (EyeTracker == null && EyeTracker_GO != null)
        {
            EyeTracker = EyeTracker_GO.GetComponent<EyeTracker>();
            GazeDataSubscription = EyeTracker_GO.GetComponent<TobiiGazeDataSubscription>();
            GazeDataSubscription._eyeTracker = iEyeTracker;
            iEyeTracker.GazeDataReceived += GazeDataSubscription.EnqueueEyeData;
        }
    }
}
