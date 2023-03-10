using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using USE_Data;
using USE_StimulusManagement;
using UnityEngine.EventSystems;

public class MouseTracker : InputTracker
{
    private bool UsingSecondMonitor = false;
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
        frameData.AddDatum("CurrentTargetGO", ()=> TargetedGameObject != null ? TargetedGameObject.name : null);
    }

    //returns GO that is the current target
    public override GameObject FindCurrentTarget()
    {
        CurrentInputScreenPosition = InputBroker.mousePosition;
        if (CurrentInputScreenPosition.Value.x < 0 || CurrentInputScreenPosition.Value.y < 0) //should also be if x or y is greater than screen
            CurrentInputScreenPosition = null;

        //Trying to implement display control:
//#if !UNITY_EDITOR
//        Vector3 screenCoords = Display.RelativeMouseAt(touchPos);
//        if (AllowedDisplay >= 0 && touchPos.z != AllowedDisplay) {
//            return null;
//        }
//#endif

        if (CurrentInputScreenPosition != null)
        {
            Vector3 direction = CurrentInputScreenPosition.Value - Camera.main.transform.position;
            GameObject hitObject = InputBroker.RaycastBoth(CurrentInputScreenPosition.Value, direction);

            if (hitObject != null)
            {
                TargetedGameObject = hitObject;
                // if (InputBroker.GetMouseButton(0))
                return hitObject;
            }
        }
        return null;
    }

}
