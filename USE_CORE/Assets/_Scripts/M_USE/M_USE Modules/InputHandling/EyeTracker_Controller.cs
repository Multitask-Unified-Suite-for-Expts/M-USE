using System.Collections;
using System.Collections.Generic;
using Tobii.Research;
using Tobii.Research.Unity;
using UnityEngine;

public class EyeTracker_Controller : MonoBehaviour
{
    public static EyeTracker_Controller Instance { get; private set; }
    private IEyeTracker cachedEyeTracker = null;
    public IEyeTracker iEyeTracker
    {
        get
        {
            if (cachedEyeTracker == null && EyeTrackingOperations.FindAllEyeTrackers().Count > 0)
            {
                // An eyetracker is connected and on
                cachedEyeTracker = EyeTrackingOperations.FindAllEyeTrackers()[0];
            }

            return cachedEyeTracker;
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        Instance = this;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
