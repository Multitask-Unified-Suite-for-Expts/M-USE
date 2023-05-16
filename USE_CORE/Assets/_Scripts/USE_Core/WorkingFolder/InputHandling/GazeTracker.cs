using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using USE_Data;
using Tobii.Research;
using Tobii.Research.Unity;
using System.Linq;

public class GazeTracker : InputTracker
{
    // ================== DELETE THIS CLASS, EVERYTHING IS IMPLEMENTED IN EITHER THE INPUT BROKER OR SELECTION TRACKING ===============


    // private Vector3 CurrentSelectionLocation = new Vector3 (-1f, -1f, -1f);
   // public bool SpoofGazeWithMouse = true;
    private GameObject HoverObject;

    public override void AddFieldsToFrameData(DataController frameData)
    {
        frameData.AddDatum("GazePosition", () => CurrentInputScreenPosition);
     //   frameData.AddDatum("HoverObject", () => HoverObject != null ? HoverObject.name : null);
    }

    public override GameObject FindCurrentTarget()
    {
        //OUT OF DATE WITH RECENT SELECTION HANDLING CHANGES

        /*if (CurrentInputScreenPosition != null)
        {
            if (Physics.Raycast(Camera.main.ScreenPointToRay(CurrentInputScreenPosition.Value), out RaycastHit hit, Mathf.Infinity))
            {
                HoverObject = hit.transform.root.gameObject;
                return HoverObject;
            }
        }*/
        return null;
    }
    public override void CustomUpdate()
    {
        /*// Get the connected eye tracker
        IEyeTracker IEyeTracker = EyeTrackingOperations.FindAllEyeTrackers()[0];
        if (IEyeTracker == null)
        {
            Debug.LogError("Could not find the eye tracker.");
        }
        else
        {
            DisplayArea displayArea = IEyeTracker.GetDisplayArea();
            EyeTracker eyeTracker = GameObject.Find("[EyeTracker]").GetComponent<EyeTracker>();
            // Get the most recent gaze data point
            var gazeData = eyeTracker?.LatestGazeData;
            Vector3? screenPoint = null;
            if (gazeData != null)
            {
                // Get the gaze points for each eye
                var leftGazePoint = gazeData.Left.GazePointOnDisplayArea;
                var rightGazePoint = gazeData.Right.GazePointOnDisplayArea;

                // Check if both eyes are valid
                if (gazeData.Left.GazePointValid && gazeData.Right.GazePointValid)
                {
                    // Average the gaze points from both eyes
                    var combinedGazePoint = new Vector2(
                        (leftGazePoint.x + rightGazePoint.x) / 2f,
                        (leftGazePoint.y + rightGazePoint.y) / 2f);

                    // Convert the combined gaze point to screen coordinates
                    screenPoint = new Vector2(
                        displayArea.TopLeft.X + combinedGazePoint.x * displayArea.Width,
                        displayArea.TopLeft.Y + combinedGazePoint.y * displayArea.Height);
                }
                else if (gazeData.Left.GazePointValid)
                {
                    // Use the gaze point from the left eye
                    screenPoint = new Vector2(
                        displayArea.TopLeft.X + leftGazePoint.x * displayArea.Width,
                        displayArea.TopLeft.Y + leftGazePoint.y * displayArea.Height);
                }
                else if (gazeData.Right.GazePointValid)
                {
                    // Use the gaze point from the right eye
                    screenPoint = new Vector2(
                        displayArea.TopLeft.X + rightGazePoint.x * displayArea.Width,
                        displayArea.TopLeft.Y + rightGazePoint.y * displayArea.Height);
                }

                CurrentInputScreenPosition = screenPoint;
            }
            else
            {
                CurrentInputScreenPosition = null;
            }
        }*/
        

    }


}
