using System;
using System.Collections.Generic;
using UnityEngine;
using USE_Data;

public class MouseTracker : InputTracker
{
    public int[] ButtonCompletedClickCount = {0, 0 , 0};
    public int[] ButtonStatus = {0, 0, 0};
    public float[] ButtonPressDuration = {0, 0, 0};

    public void ResetClicks()
    {
        Array.Clear(ButtonCompletedClickCount, 0, ButtonCompletedClickCount.Length);
        Array.Clear(ButtonPressDuration, 0, ButtonPressDuration.Length);
    }

    public int[] GetClickCount()
    {
        return ButtonCompletedClickCount;
    }

    //this should include every button
    public override void CustomUpdate()
    {
        for (int iButton = 0; iButton < 3; iButton++)
        {
            if (InputBroker.GetMouseButtonUp(iButton))
                ButtonCompletedClickCount[iButton]++;
            
            if (InputBroker.GetMouseButton(iButton))
            {
                ButtonStatus[iButton] = 1;
                if (InputBroker.GetMouseButtonDown(iButton))
                    ButtonPressDuration[iButton] = 0;
                else
                    ButtonPressDuration[iButton] += Time.deltaTime;
            }
            else
                ButtonStatus[iButton] = 0;
        }
    }

    public override void AddFieldsToFrameData(DataController frameData)
    {
        frameData.AddDatum("MousePosition", () => InputBroker.mousePosition);
        frameData.AddDatum("MouseButtonStatus", () => ButtonStatus);
        frameData.AddDatum("SimpleRaycastTarget", ()=> SimpleRaycastTarget != null ? SimpleRaycastTarget.name : null);
        frameData.AddDatum("ShotgunModalTarget", ()=> ShotgunModalTarget != null ? ShotgunModalTarget.name : null);
    }

    //returns GO that is the current target
    public override GameObject FindCurrentTarget()
    {
        CurrentInputScreenPosition = InputBroker.mousePosition;

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

}
