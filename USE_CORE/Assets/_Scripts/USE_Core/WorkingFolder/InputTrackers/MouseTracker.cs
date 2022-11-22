using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using USE_Data;

public class MouseTracker : InputTracker
{
    [CanBeNull] private GameObject HoverObject;
    private bool UsingSecondMonitor = false;

    public override void AddFieldsToFrameData(DataController frameData)
    {
        frameData.AddDatum("MousePosition", () => InputBroker.mousePosition);
        frameData.AddDatum("MouseButton0", () => InputBroker.GetMouseButton(0));
        frameData.AddDatum("MouseButton1", () => InputBroker.GetMouseButton(1));
        frameData.AddDatum("MouseButton2", () => InputBroker.GetMouseButton(2));
        frameData.AddDatum("HoverObject", () => HoverObject != null ? HoverObject.name : null);
    }

    public override GameObject FindCurrentTarget()
    {
        Vector3 touchPos = InputBroker.mousePosition;
        if (UsingSecondMonitor)
            touchPos.x = touchPos.x + 1920;

        if (Physics.Raycast(Camera.main.ScreenPointToRay(touchPos), out RaycastHit hit, Mathf.Infinity))
        {
            HoverObject = hit.transform.root.gameObject;
            if (InputBroker.GetMouseButton(0)) return HoverObject;
        }
        return null;
    }
}
