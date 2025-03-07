using System.Collections;
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

    private void Awake()
    {
        base.Awake();
        mostRecentGazeSample = new TobiiGazeSample();
    }

    private void Start()
    {
        Camera = Camera.main;
        StartCoroutine(WaitForEyeTracker(10f)); // Wait for 10 seconds
    }

    IEnumerator WaitForEyeTracker(float timeout)
    {
        float elapsed = 0f;
        while ((iEyeTracker == null || EyeTracker == null) && elapsed < timeout)
        {
            FindEyeTrackerComponents();
            if (iEyeTracker != null && EyeTracker != null)
            {
                Debug.Log("Eye tracker successfully found!");
                yield break;
            }

            elapsed += 0.5f;  // Match WaitForSeconds delay
            yield return new WaitForSeconds(0.5f); // Retry every 0.5 seconds
        }

        Debug.LogError("Failed to find eye tracker within timeout.");
    }

    public override void FindEyeTrackerComponents()
    {
        var trackers = EyeTrackingOperations.FindAllEyeTrackers();
        if (trackers.Count == 0)
        {
            Debug.LogWarning("No eyetrackers detected.");
            return;
        }

        if (iEyeTracker == null)
        {
            iEyeTracker = trackers[0];
            ScreenBasedCalibration = new ScreenBasedCalibration(iEyeTracker);
            DisplayArea = iEyeTracker.GetDisplayArea();
            Debug.Log($"Found eye tracker: {iEyeTracker.Model}");
        }

        if (EyeTracker == null && EyeTracker_GO != null)
        {
            EyeTracker = EyeTracker_GO.GetComponent<EyeTracker>();
            GazeDataSubscription = EyeTracker_GO.GetComponent<TobiiGazeDataSubscription>();

            if (GazeDataSubscription != null)
            {
                if (GazeDataSubscription._eyeTracker == null)
                {
                    GazeDataSubscription._eyeTracker = iEyeTracker;
                    iEyeTracker.GazeDataReceived -= GazeDataSubscription.EnqueueEyeData;  // Prevent double subscription
                    iEyeTracker.GazeDataReceived += GazeDataSubscription.EnqueueEyeData;
                    Debug.Log("Gaze data subscription initialized.");
                }
            }
            else
            {
                Debug.LogError("Failed to find GazeDataSubscription component.");
            }
        }
        else
        {
            Debug.LogError("EyeTracker_GO is not assigned.");
        }
    }
}
