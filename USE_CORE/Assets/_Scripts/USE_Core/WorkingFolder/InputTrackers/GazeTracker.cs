using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using USE_Data;

public class GazeTracker : InputTracker
{
    private Vector3 GazePos = new Vector3 (-1f, -1f, -1f);
    private bool SpoofGazeWithMouse = false;
    private GameObject HoverObject;

    public override void AddFieldsToFrameData(DataController frameData)
    {
        frameData.AddDatum("GazePosition", () => GazePos);
        frameData.AddDatum("HoverObject", () => HoverObject != null ? HoverObject.name : null);
    }

    public override GameObject FindCurrentTarget()
    {
        if (Physics.Raycast(Camera.main.ScreenPointToRay(GazePos), out RaycastHit hit, Mathf.Infinity))
        {
            HoverObject = hit.transform.root.gameObject;
            return HoverObject;
        }
        return null;
    }
    public override void CustomUpdate()
    {
        if (SpoofGazeWithMouse)
        {
            GazePos = InputBroker.mousePosition;
        }
    }
}
