using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using USE_Data;
using Tobii.Research;
using Tobii.Research.Unity;
using System.Linq;

public class GazeTracker : InputTracker
{
    public override void AddFieldsToFrameData(DataController frameData)
    {
        frameData.AddDatum("GazePosition", () => CurrentInputScreenPosition);
        frameData.AddDatum("SimpleRaycastTarget", () => SimpleRaycastTarget != null ? SimpleRaycastTarget.name : null);
        frameData.AddDatum("ShotgunModalTarget", () => ShotgunModalTarget != null ? ShotgunModalTarget.name : null);
    }

    public override GameObject FindCurrentTarget()
    {
        CurrentInputScreenPosition = InputBroker.gazePosition;

        if (CurrentInputScreenPosition.Value.x < 0 || CurrentInputScreenPosition.Value.y < 0 || CurrentInputScreenPosition.Value.x > Screen.width || CurrentInputScreenPosition.Value.y > Screen.height ||
                    float.IsNaN(CurrentInputScreenPosition.Value.x) || float.IsNaN(CurrentInputScreenPosition.Value.y) || float.IsNaN(CurrentInputScreenPosition.Value.z))
            CurrentInputScreenPosition = null;

        if (CurrentInputScreenPosition != null)
        {
            //Find Current Shotgun Target:
            Dictionary<GameObject, float> proportions = ShotgunRaycast.RaycastShotgunProportions(CurrentInputScreenPosition.Value, Camera.main);
            ShotgunGoAboveThreshold.Clear();

            foreach (var pair in proportions)
            {
                if (pair.Value > ShotgunThreshold)
                    ShotgunGoAboveThreshold.Add(pair.Key);
            }

            ShotgunModalTarget = ShotgunRaycast.ModalShotgunTarget(proportions);
            
            if (ShotgunModalTarget != null)
                return ShotgunModalTarget;

            //Find Current Target and return it if found:
            SimpleRaycastTarget = InputBroker.RaycastBoth(CurrentInputScreenPosition.Value);
            if (SimpleRaycastTarget != null)
                return SimpleRaycastTarget;

        }
        return null;
    }
    public override void CustomUpdate()
    {

    }


}
