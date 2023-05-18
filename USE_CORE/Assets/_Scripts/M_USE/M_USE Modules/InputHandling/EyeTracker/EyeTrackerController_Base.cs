using System.Collections;
using System.Collections.Generic;
using Tobii.Research;
using Tobii.Research.Unity;
using UnityEngine;

public abstract class EyeTrackerController_Base : MonoBehaviour
{
    private void Awake()
    {
        FindEyeTracker();
        CustomUpdate();
        CalibrateEyeTracker();
    }
    

    public abstract void FindEyeTracker();

    public virtual void CustomUpdate()  
    {
    }
    public virtual void CalibrateEyeTracker()
    {
    }
}
