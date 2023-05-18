using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

public class PlayerViewPanel //: MonoBehaviour
{
    private GameObject circle;
    // public Transform parent;
    private GameObject playerViewCanv;
    private Vector3 circleLocation;
    private float visualAngle;
    private float distanceToScreen;
    private Vector3 textLocation;

    // Start is called before the first frame update
    void Start()
    {
        // parent = transform;
    }
    public GameObject CreateLine(string name, Vector3 start, Vector3 end, Color color, Transform transform)
    {
        GameObject myLine = new GameObject(name, typeof(LineRenderer), typeof(RectTransform), typeof(CanvasRenderer));
        myLine.layer = LayerMask.NameToLayer("UI");
        myLine.transform.SetParent(transform);

        RectTransform rectTransform = myLine.GetComponent<RectTransform>();
        rectTransform.localPosition = Vector3.zero;
        rectTransform.anchoredPosition = Vector3.zero;
        rectTransform.sizeDelta = myLine.transform.parent.GetComponent<RectTransform>().sizeDelta;
        rectTransform.anchorMin = myLine.transform.parent.GetComponent<RectTransform>().anchorMin;
        rectTransform.anchorMax = myLine.transform.parent.GetComponent<RectTransform>().anchorMax;
        rectTransform.pivot = Vector3.zero;


        LineRenderer lineRenderer = myLine.GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = false;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.startWidth = 200f;
        lineRenderer.endWidth = 200f;
        Debug.Log($"START POS: {start} END POS: {end}");
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
        lineRenderer.sortingOrder = 1000;

        lineRenderer.alignment = LineAlignment.TransformZ; // use the view space for calculating length

        return myLine;
    }

    public GameObject DrawSampleLines(string lineName, Color col, Vector2 start, Vector2 end, Transform parent) // removed GameObject parent
    {
        GameObject sampleLines = new GameObject(lineName, typeof(RectTransform), typeof(UnityEngine.UI.Extensions.UILineRenderer));
        sampleLines.transform.SetParent(parent, false);
        sampleLines.GetComponent<RectTransform>().anchorMax = Vector2.zero;
        sampleLines.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        sampleLines.GetComponent<RectTransform>().sizeDelta = new Vector2(1,1);
        sampleLines.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

        UnityEngine.UI.Extensions.UILineRenderer lineRenderer = sampleLines.GetComponent<UnityEngine.UI.Extensions.UILineRenderer>();

        lineRenderer.Points = new Vector2[] { start, end };
        lineRenderer.color = col;
        lineRenderer.RelativeSize = false;
        lineRenderer.SetAllDirty();
        return sampleLines;
    }

    public GameObject CreateTextObject(string textName, string text, Color col, Vector2 textLocation, Vector2 size, Transform parent)
    {
        GameObject textObject = new GameObject(textName, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent);
        textObject.SetActive(false);

        textObject.GetComponent<RectTransform>().localPosition = Vector2.zero;
        textObject.GetComponent<RectTransform>().localScale = size;
        textObject.GetComponent<RectTransform>().anchoredPosition = textLocation;
        textObject.GetComponent<RectTransform>().anchorMax = Vector2.zero;
        textObject.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        
        Text txt = textObject.GetComponent<Text>();
        txt.text = text;
        txt.color = col;
        txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;

        return textObject;
    }

    /*
    public GameObject drawHalo(Vector2  haloLocation, Vector2 size)
    {
        GameObject haloOject = new GameObject("Halo", typeof(RectTransform));
        Behaviour halo = (Behaviour)GetComponent("Halo");
        halo.GetType().GetProperty("enabled").SetValue(halo, false, null);
        halo.AddComponent<"Halo">();
        halo.AddComponent<CanvasRenderer>();
        halo.GetComponent<Light>().flare = 
        halo.GetComponent<UnityEngine.UI.Extensions.UICircle>().fill = false;
        halo.GetComponent<UnityEngine.UI.Extensions.UICircle>().thickness = 2f;
        halo.GetComponent<RectTransform>().sizeDelta = size;
        halo.GetComponent<RectTransform>().anchoredPosition = haloLocation;// new Vector3(calibPointPixel.x, calibPointPixel.y, exptViewCam.nearClipPlane);
        return halo;
    }
    */
    Vector2 GetMidpoint(Vector2[] points)
    {
        float sumX = 0f;
        float sumY = 0f;
        int count = points.Length;

        foreach (Vector2 point in points)
        {
            sumX += point.x;
            sumY += point.y;
        }

        return new Vector2(sumX / count, sumY / count);
    }

}
