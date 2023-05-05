using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
    public GameObject DrawSampleLines(string lineName, Color col, List<Vector2> pointList) // removed GameObject parent
    {
        float radPix = 100; // dummy value 1920 used, ((MonitorDetails)SessionSettings.Get("sessionConfig", "monitorDetails")).CmSize[0]

        GameObject sampleLines = new GameObject(lineName, typeof(RectTransform), typeof(UnityEngine.UI.Extensions.UILineRenderer));
        //sampleLines.transform.SetParent(parent);
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
}
