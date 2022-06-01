using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerViewPanel : MonoBehaviour
{
    private GameObject circle;
    public Transform parent;
    private GameObject playerViewCanv;
    private Vector3 circleLocation;
    private float visualAngle;
    private float distanceToScreen;
    private Vector3 textLocation;

    // Start is called before the first frame update
    void Start()
    {
        
        parent = transform;
        /*
        visualAngle = 2;
        circleLocation = new Vector3(-134f, 0.5f, 0f);
        distanceToScreen = 50; // dummy value
        drawCircle(circleLocation, distanceToScreen, visualAngle, parent);

        List<Vector2> pointList = new List<Vector2>();
        pointList.Add(new Vector2(200, 200));
        pointList.Add(new Vector2(300, 200));
        drawSampleLines("Line", Color.green, parent, pointList);

        textLocation = new Vector3(200f, 200f, 0f);
        writeText("Hi", Color.red, parent, textLocation);
        */
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public GameObject drawCircle(Vector3 circleLocation, float distanceToScreen, float visualAngle, Transform parent)
    {
        float radCm = 2 * distanceToScreen * (Mathf.Tan((Mathf.PI * visualAngle / 180f) / 2));
        float radPix = 100; // dummy value 1920 used, ((MonitorDetails)SessionSettings.Get("sessionConfig", "monitorDetails")).CmSize[0]

        GameObject degreeCircle = new GameObject("DegreeCircle", typeof(RectTransform), typeof(UnityEngine.UI.Extensions.UICircle));

        degreeCircle.AddComponent<CanvasRenderer>();
        degreeCircle.transform.SetParent(parent);
        degreeCircle.GetComponent<UnityEngine.UI.Extensions.UICircle>().fill = false;
        degreeCircle.GetComponent<UnityEngine.UI.Extensions.UICircle>().thickness = 2f;
        degreeCircle.GetComponent<RectTransform>().sizeDelta = new Vector2(radPix * 2, radPix * 2);
        degreeCircle.GetComponent<RectTransform>().anchoredPosition = circleLocation;// new Vector3(calibPointPixel.x, calibPointPixel.y, exptViewCam.nearClipPlane);
        return degreeCircle;

    }
    public GameObject drawSampleLines(string lineName, Color col, Transform parent, List<Vector2> pointList) // removed GameObject parent
    {
        float radPix = 100; // dummy value 1920 used, ((MonitorDetails)SessionSettings.Get("sessionConfig", "monitorDetails")).CmSize[0]

        GameObject sampleLines = new GameObject(lineName, typeof(RectTransform), typeof(UnityEngine.UI.Extensions.UILineRenderer));
        sampleLines.transform.SetParent(parent);
        sampleLines.AddComponent<CanvasRenderer>();
        sampleLines.GetComponent<RectTransform>().anchorMax = Vector2.zero;
        sampleLines.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        sampleLines.GetComponent<RectTransform>().sizeDelta = new Vector2(radPix * 2, radPix * 2);
        sampleLines.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

        UnityEngine.UI.Extensions.UILineRenderer lineComp = sampleLines.GetComponent<UnityEngine.UI.Extensions.UILineRenderer>();

        lineComp.Points = pointList.ToArray();
        lineComp.color = col;
        lineComp.relativeSize = false;
        return sampleLines;
    }
    public GameObject writeText(string text, Color col, Transform parent, Vector2 textLocation)
    {
        float radPix = 100; // dummy value 1920 used, ((MonitorDetails)SessionSettings.Get("sessionConfig", "monitorDetails")).CmSize[0]
        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent);
        textObject.GetComponent<RectTransform>().localPosition = Vector2.zero;
        textObject.GetComponent<RectTransform>().sizeDelta = new Vector2(radPix * 2, radPix * 2);
        textObject.GetComponent<RectTransform>().anchoredPosition = textLocation;
        textObject.GetComponent<RectTransform>().anchorMax = Vector2.zero;
        textObject.GetComponent<RectTransform>().anchorMin = Vector2.zero;

        /*
        Vector2 calibPointPixel = ProportionToPixel(calibPoint.testPoint, playerViewRes) + playerViewCanv.GetComponent<RectTransform>().anchoredPosition;
        rt.anchoredPosition = calibPointPixel + offset +
            new Vector2(playerViewCanv.GetComponent<RectTransform>().sizeDelta.x / 2, playerViewCanv.GetComponent<RectTransform>().sizeDelta.y / 2);
        //still don't know why I have to add half the resolution to this but it works..
        */
        Text txt = textObject.GetComponent<Text>();
        txt.text = text;
        txt.color = col;
        txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.alignment = TextAnchor.MiddleCenter;

        return textObject;
    }
}
