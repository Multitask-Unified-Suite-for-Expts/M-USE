using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GazeOverlay : MonoBehaviour
{
    public int maxTrailLength = 10; // Maximum number of gaze points to display.
    public float trailLifetime = 1f; // How long each gaze point lasts (in seconds).
    public float pointSize = 5f; // Size of each gaze point.

    private Transform canvasTransform;
    private Queue<GameObject> gazePoints;

    private float originalWidth = 1920f; // Original width of the monitor.
    private float originalHeight = 1080f; // Original height of the monitor.
    private float targetWidth = 960f; // Target width of the overlay canvas.
    private float targetHeight = 540f; // Target height of the overlay canvas.

    void Start()
    {
        canvasTransform = GetComponent<Transform>();
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

        // Scale the gaze position to the target resolution.
        gazePosition.x = (gazePosition.x / originalWidth) * targetWidth;
        gazePosition.y = (gazePosition.y / originalHeight) * targetHeight;

        GameObject gazePoint = new GameObject("GazePoint");
        gazePoint.transform.SetParent(canvasTransform, false);

        Image image = gazePoint.AddComponent<Image>();
        image.color = Color.red;

        // Set the sprite to a circle.
        image.sprite = Resources.Load<Sprite>("UI/Skin/Knob"); // Default Unity circle sprite.

        RectTransform rectTransform = gazePoint.GetComponent<RectTransform>();
        rectTransform.pivot = new Vector2(0, 0); // Set pivot to bottom-left corner.
        rectTransform.anchorMin = new Vector2(0, 0); // Set anchor to bottom-left corner.
        rectTransform.anchorMax = new Vector2(0, 0); // Set anchor to bottom-left corner.
        rectTransform.anchoredPosition = gazePosition;
        rectTransform.sizeDelta = new Vector2(pointSize, pointSize);

        gazePoints.Enqueue(gazePoint);

        if (gazePoints.Count > maxTrailLength)
        {
            Destroy(gazePoints.Dequeue());
        }
    }


    Vector2 GetGazePosition()
    {
        // Replace this with your method for determining the gaze position.
        return InputBroker.gazePosition;
    }
}
