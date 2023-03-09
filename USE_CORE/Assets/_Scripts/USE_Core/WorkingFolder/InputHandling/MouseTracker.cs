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
    private int ButtonClickCount = 0; //change to array ultimatley so we can loop through all buttons. 
    public float ButtonPressDuration;

    public void ResetClickCount()
    {
        ButtonClickCount = 0;
    }

    public int GetClickCount()
    {
        return ButtonClickCount;
    }

    //this should include every button
    public override void CustomUpdate()
    {
        if (InputBroker.GetMouseButtonUp(0))
            ButtonClickCount++;
        
        if (InputBroker.GetMouseButtonDown(0))
        {
            ButtonPressDuration = 0;
        }

        if (InputBroker.GetMouseButton(0))
            ButtonPressDuration += Time.deltaTime;
        
    }

    public override void AddFieldsToFrameData(DataController frameData)
    {
        frameData.AddDatum("MousePosition", () => InputBroker.mousePosition);
        frameData.AddDatum("MouseButton0", () => InputBroker.GetMouseButton(0));
        frameData.AddDatum("MouseButton1", () => InputBroker.GetMouseButton(1));
        frameData.AddDatum("MouseButton2", () => InputBroker.GetMouseButton(2));
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
                if (InputBroker.GetMouseButton(0))
                    return hitObject;
            }
        }
        return null;
    }

}
