using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using USE_Data;

public class MouseTracker : InputTracker
{
    public override void AddFieldsToFrameData(DataController frameData)
    {
        frameData.AddDatum("MousePosition", ()=> InputBroker.mousePosition);
        frameData.AddDatum("MouseButton0", ()=> InputBroker.GetMouseButton(0));
        frameData.AddDatum("MouseButton1", ()=> InputBroker.GetMouseButton(1));
        frameData.AddDatum("MouseButton2", ()=> InputBroker.GetMouseButton(2));
    }

    public override GameObject FindCurrentTarget()
    {
        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(Camera.main.ScreenPointToRay(InputBroker.mousePosition), out hit, Mathf.Infinity))
        {
            return hit.transform.gameObject;
        }
        else
            return null;
    }
}
