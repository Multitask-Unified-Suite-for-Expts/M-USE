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
    public EyeTracker EyeTracker;
    // Start is called before the first frame update
    private void Awake()
    {
        Instance = this;
        base.Awake();

    }

    // Update is called once per frame
    void Update()
    {
        while (iEyeTracker == null  && EyeTracker == null)
            FindEyeTracker();
    }

    public override void FindEyeTracker()
    {
        if (iEyeTracker == null && EyeTrackingOperations.FindAllEyeTrackers().Count > 0)
        {
            // An eyetracker is connected and on
            iEyeTracker = EyeTrackingOperations.FindAllEyeTrackers()[0];
        }
        if (EyeTracker == null && GameObject.Find("EyeTracker(Clone)") != null)
        {
            EyeTracker = GameObject.Find("EyeTracker(Clone)").GetComponent<EyeTracker>();
        }
    }
}
