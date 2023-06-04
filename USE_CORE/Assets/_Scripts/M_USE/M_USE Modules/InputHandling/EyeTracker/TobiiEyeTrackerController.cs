using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tobii.Research;
using Tobii.Research.Unity;
using USE_DisplayManagement;

public class TobiiEyeTrackerController : EyeTrackerController_Base
{
    public static TobiiEyeTrackerController Instance { get; private set; }
    private IEyeTracker cachedEyeTracker = null;

    public IEyeTracker iEyeTracker;
    public EyeTracker EyeTracker;
    public ScreenBasedCalibration ScreenBasedCalibration;
    public DisplayArea DisplayArea;
    public GameObject TrackBoxGuideGO;
    public Camera Camera;
    public bool isCalibrating;
   
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

        if (MonitorDetails != null && ScreenDetails != null && CoordinateConverter == null)
        {
            InitializeCoordinateConverter();
        }

    }

    public override void FindEyeTrackerComponents()
    {
        // An eyetracker is connected and on
        if (iEyeTracker == null && EyeTrackingOperations.FindAllEyeTrackers().Count > 0)
        {
            iEyeTracker = EyeTrackingOperations.FindAllEyeTrackers()[0];
            ScreenBasedCalibration = new ScreenBasedCalibration(iEyeTracker);

            DisplayArea = iEyeTracker.GetDisplayArea();
        }

        if (EyeTracker == null && GameObject.Find("EyeTracker(Clone)") != null)
            EyeTracker = GameObject.Find("EyeTracker(Clone)").GetComponent<EyeTracker>();

        if (TrackBoxGuideGO == null && GameObject.Find("TrackBoxGuide(Clone)") != null)
            TrackBoxGuideGO = GameObject.Find("TrackBoxGuide(Clone)");

    }

    private void InitializeCoordinateConverter()
    {
        CoordinateConverter = new USE_CoordinateConverter(MonitorDetails, ScreenDetails);
    }
}
