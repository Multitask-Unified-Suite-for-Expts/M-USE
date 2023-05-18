using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tobii.Research;
using Tobii.Research.Unity;

public class TobiiEyeTrackerController : EyeTrackerController_Base
{
    public static TobiiEyeTrackerController Instance { get; private set; }
    private IEyeTracker cachedEyeTracker = null;

    public IEyeTracker iEyeTracker;
    // Start is called before the first frame update
    void Awake()
    {
        Instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override void FindEyeTracker()
    {
        if (iEyeTracker == null && EyeTrackingOperations.FindAllEyeTrackers().Count > 0)
        {
            // An eyetracker is connected and on
            iEyeTracker = EyeTrackingOperations.FindAllEyeTrackers()[0];
        }
    }
}
