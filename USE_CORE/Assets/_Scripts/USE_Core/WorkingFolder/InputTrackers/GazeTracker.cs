using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using USE_Data;

public class GazeTracker : InputTracker
{
    // private Vector3 CurrentSelectionLocation = new Vector3 (-1f, -1f, -1f);
    public bool SpoofGazeWithMouse = true;
    private GameObject HoverObject;

    public override void AddFieldsToFrameData(DataController frameData)
    {
        frameData.AddDatum("GazePosition", () => CurrentSelectionLocation);
        frameData.AddDatum("HoverObject", () => HoverObject != null ? HoverObject.name : null);
    }

    public override GameObject FindCurrentTarget()
    {
        if (CurrentSelectionLocation != null)
        {
            if (Physics.Raycast(Camera.main.ScreenPointToRay(CurrentSelectionLocation.Value), out RaycastHit hit, Mathf.Infinity))
            {
                HoverObject = hit.transform.root.gameObject;
                return HoverObject;
            }
        }
        return null;
    }
    public override void CustomUpdate()
    {
        if (SpoofGazeWithMouse)
        {
            CurrentSelectionLocation = InputBroker.mousePosition;
        }
    }
}
