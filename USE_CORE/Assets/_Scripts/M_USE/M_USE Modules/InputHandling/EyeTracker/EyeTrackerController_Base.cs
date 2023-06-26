using System.Collections;
using System.Collections.Generic;
using Tobii.Research;
using Tobii.Research.Unity;
using UnityEngine;
using USE_DisplayManagement;

public abstract class EyeTrackerController_Base : MonoBehaviour
{
    public MonitorDetails MonitorDetails;
    public ScreenDetails ScreenDetails;
    public float EyeDistance;

    public void Awake()
    {
        CalibrateEyeTracker();
    }

    public void Update()
    {
        CustomUpdate();
    }
    public abstract void FindEyeTrackerComponents();

    public virtual void CustomUpdate()  
    {
    }
    public virtual void CalibrateEyeTracker()
    {
    }

}
