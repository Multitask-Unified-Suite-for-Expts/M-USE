using System;
using System.Collections.Generic;
using UnityEngine;
using USE_Data;
using USE_StimulusManagement;

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
            {
                ButtonCompletedClickCount[iButton]++;

                if (SimpleRaycastTarget != null)
                    SessionValues.EventCodeManager.CheckForAndSendEventCode(SimpleRaycastTarget, $"Button{iButton}ReleasedFrom", null);
            }

            if (InputBroker.GetMouseButton(iButton))
            {
                ButtonStatus[iButton] = 1;
                if (InputBroker.GetMouseButtonDown(iButton))
                {
                    ButtonPressDuration[iButton] = 0;

                    if (SimpleRaycastTarget != null)
                       SessionValues.EventCodeManager.CheckForAndSendEventCode(SimpleRaycastTarget, $"Button{iButton}PressedOn", null);
                }
                else
                    ButtonPressDuration[iButton] += Time.deltaTime;
            }
            else
                ButtonStatus[iButton] = 0;
        }
    }

    public override void AddFieldsToFrameData(DataController frameData)
    {
        frameData.AddDatum("MousePosition", () => InputBroker.mousePosition != null ? InputBroker.mousePosition : new Vector3(float.NaN, float.NaN, float.NaN));
        frameData.AddDatum("MouseButtonStatus", () => "[" + string.Join(",",ButtonStatus) + "]");
        frameData.AddDatum("SimpleRaycastTarget", ()=> SimpleRaycastTarget != null ? SimpleRaycastTarget.name : null);
        frameData.AddDatum("ShotgunModalTarget", ()=> ShotgunModalTarget != null ? ShotgunModalTarget.name : null);
    }

    //returns GO that is the current target
    public override void FindCurrentTarget()
    {
        CurrentInputScreenPosition = InputBroker.mousePosition;

        if (CurrentInputScreenPosition.Value.x < 0 || CurrentInputScreenPosition.Value.y < 0 || CurrentInputScreenPosition.Value.x > Screen.width || CurrentInputScreenPosition.Value.y > Screen.height
            || float.IsNaN(CurrentInputScreenPosition.Value.x) || float.IsNaN(CurrentInputScreenPosition.Value.y) || float.IsNaN(CurrentInputScreenPosition.Value.z))
        {
            CurrentInputScreenPosition = null;
        }

        if (CurrentInputScreenPosition != null && Camera.main != null)
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

            //Find Current Target and return it if found:
            SimpleRaycastTarget = InputBroker.RaycastBoth(CurrentInputScreenPosition.Value);
        }
    }


}
