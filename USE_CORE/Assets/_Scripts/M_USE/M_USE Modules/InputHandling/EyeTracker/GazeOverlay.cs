using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GazeOverlay : MonoBehaviour
{
    public int maxTrailLength = 10; // Maximum number of gaze points to display.
    public float trailLifetime = 1f; // How long each gaze point lasts (in seconds).
    public float pointSize = 5f; // Size of each gaze point.

    private RectTransform canvasTransform; // Canvas transform as RectTransform
    private Queue<GameObject> gazePoints;

    private float monitorWidth; // Actual width of the monitor.
    private float monitorHeight; // Actual height of the monitor.

    void Start()
    {
        monitorWidth = Session.SessionDef.MonitorDetails.PixelResolution.x;
        monitorHeight = Session.SessionDef.MonitorDetails.PixelResolution.y;

        // Set canvasTransform as RectTransform of the Canvas component
        canvasTransform = GetComponent<RectTransform>();

        gazePoints = new Queue<GameObject>();

        if (Display.displays.Length > 1)
        {
            Display.displays[1].Activate();
        }

        GetComponent<Canvas>().targetDisplay = 1;
    }

    void Update()
    {
        Vector2 gazePosition = GetGazePosition();

        // Scale the gaze position to the canvas resolution
        gazePosition.x = (gazePosition.x / monitorWidth) * canvasTransform.rect.width;
        gazePosition.y = (gazePosition.y / monitorHeight) * canvasTransform.rect.height;

        // Clamp the gaze position within the canvas boundaries
        gazePosition.x = Mathf.Clamp(gazePosition.x, 0, canvasTransform.rect.width - pointSize);
        gazePosition.y = Mathf.Clamp(gazePosition.y, 0, canvasTransform.rect.height - pointSize);

        GameObject gazePoint = new GameObject("GazePoint");
        gazePoint.transform.SetParent(canvasTransform, false);

        Image image = gazePoint.AddComponent<Image>();
        image.color = Color.red;

        // Set the sprite to a circle
        image.sprite = Resources.Load<Sprite>("UI/Skin/Knob"); // Default Unity circle sprite

        RectTransform pointRectTransform = gazePoint.GetComponent<RectTransform>();
        pointRectTransform.pivot = new Vector2(0, 0); // Set pivot to bottom-left corner
        pointRectTransform.anchorMin = new Vector2(0, 0); // Set anchor to bottom-left corner
        pointRectTransform.anchorMax = new Vector2(0, 0); // Set anchor to bottom-left corner
        pointRectTransform.anchoredPosition = gazePosition;
        pointRectTransform.sizeDelta = new Vector2(pointSize, pointSize);

        gazePoints.Enqueue(gazePoint);

        if (gazePoints.Count > maxTrailLength)
        {
            Destroy(gazePoints.Dequeue());
        }
    }

    Vector2 GetGazePosition()
    {
        return InputBroker.gazePosition;
    }
}
