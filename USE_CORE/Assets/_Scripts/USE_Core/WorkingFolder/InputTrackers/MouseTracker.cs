using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using USE_Data;
using USE_StimulusManagement;
using UnityEngine.EventSystems;

public class MouseTracker : InputTracker
{
    [CanBeNull] private GameObject HoverObject;
    private bool UsingSecondMonitor = false;
    private int ClickCount = 0;
    private string hoverObjectName = "";

    public void ResetClickCount() {
        ClickCount = 0;
    }

    public int GetClickCount() {
        return ClickCount;
    }

    public override void AddFieldsToFrameData(DataController frameData)
    {
        frameData.AddDatum("MousePosition", () => InputBroker.mousePosition);
        frameData.AddDatum("MouseButton0", () => InputBroker.GetMouseButton(0));
        frameData.AddDatum("MouseButton1", () => InputBroker.GetMouseButton(1));
        frameData.AddDatum("MouseButton2", () => InputBroker.GetMouseButton(2));
        frameData.AddDatum("HoverObject", ()=> HoverObject != null ? HoverObject.name : null);
    }

    public override GameObject FindCurrentTarget()
    {
        Vector3 touchPos = InputBroker.mousePosition;
#if !UNITY_EDITOR
        Vector3 screenCoords = Display.RelativeMouseAt(touchPos);
        if (AllowedDisplay >= 0 && touchPos.z != AllowedDisplay) {
            return null;
        }
#endif

        // If the mouse button is up, that means they clicked and released. This is a good way to only count clicks and not holds
        if (InputBroker.GetMouseButtonUp(0))
            ClickCount++;

        Vector3 direction = touchPos - Camera.main.transform.position;
        GameObject hitObject = RaycastBoth(touchPos, direction);

        if (hitObject != null)
        {
            HoverObject = hitObject;

            if (InputBroker.GetMouseButton(0))
                return hitObject;
        }
        return null;
    }

    public GameObject RaycastBoth(Vector3 touchPos, Vector3 direction)
    {
        GameObject target = null;
        float distance2D = 0;
        float distance3D = 0;

        //3D:
        RaycastHit hit;
        if(Physics.Raycast(Camera.main.ScreenPointToRay(touchPos), out hit, Mathf.Infinity))
        {
            target = hit.transform.gameObject;
            distance3D = (hit.point - touchPos).magnitude;
        }

        //2D:
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = touchPos;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        foreach(RaycastResult result in results)
        {
            if (result.gameObject != null)
            {
                distance2D = (result.gameObject.transform.position - touchPos).magnitude;
                if(target == null || (distance3D != 0 && (distance2D < distance3D)))
                {
                    target = result.gameObject;
                    break;
                }
            }
        }
        return target;
    }

}
