using System.Collections;
using System.Collections.Generic;
using Tobii.Research;
using Tobii.Research.Unity;
using UnityEngine;

public abstract class EyeTrackerController_Base : MonoBehaviour
{
    public void Awake()
    {
        CustomUpdate();
        CalibrateEyeTracker();
    }

    public abstract void FindEyeTrackerComponents();

    public virtual void CustomUpdate()  
    {
    }
    public virtual void CalibrateEyeTracker()
    {
    }

}
