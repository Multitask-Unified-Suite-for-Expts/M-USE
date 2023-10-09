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
using System;


public class PlayerViewPanel:MonoBehaviour
{
    private GameObject circle;
    // public Transform parent;
    private GameObject playerViewCanv;
    private Vector3 circleLocation;
    private float visualAngle;
    private float distanceToScreen;
    private Vector3 textLocation;
    private Font textFont;

    void Start()
    {
        textFont = Resources.Load<Font>("Poppins/Poppins-Medium");
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
        txt.font = textFont;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;

        return textObject;
    }


    public void GenerateParticle(Vector3 position, Canvas RenderCanvas)
    {
        // Create a new GameObject
        GameObject particle = new GameObject("Particle");
        // Add a ParticleSystem component
        particle.AddComponent<ParticleSystem>();

        // Make the particle a child of the RenderCanvas
        particle.transform.SetParent(RenderCanvas.transform);
        particle.transform.position = position;

        // Get the ParticleSystem component
        ParticleSystem particleSystem = particle.GetComponent<ParticleSystem>();

        // Configure the particle system here if needed
        var main = particleSystem.main;
        main.startLifetime = 5f; // This particle will live for 5 seconds before being destroyed.

        var trails = particleSystem.trails;
        trails.enabled = true; // Enable trails for this particle.
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
