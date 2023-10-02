/*
MIT License

Copyright (c) 2023 Multitask - Unified - Suite -for-Expts

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files(the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/



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
