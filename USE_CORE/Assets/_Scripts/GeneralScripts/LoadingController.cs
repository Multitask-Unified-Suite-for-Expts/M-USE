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
using TMPro;

[RequireComponent(typeof(Canvas))] //Class is attached to LoadingCanvas gameobject in scene
public class LoadingController : MonoBehaviour
{
    //Set in inspector:
    public GameObject Star_GO;
    public TextMeshProUGUI DotsText;

    private readonly float StarRotationSpeed = 125f;
    private float dotStartTime;
    private float dotInterval = .4f;

    void Start()
    {
        if (Star_GO == null || DotsText == null)
            Debug.LogError("STARGO OR DOTSTEXT IS NULL!");

        DeactivateLoadingCanvas();

        DotsText.text = ".";
        dotStartTime = Time.time;
    }

    private void Update()
    {
        RotateStar();

        if(Time.time - dotStartTime >= dotInterval)
            UpdateDotText();
    }

    private void UpdateDotText()
    {
        if (DotsText.text.Length > 2)
            DotsText.text = ".";
        else
            DotsText.text += ".";

        dotStartTime = Time.time;
    }

    private void RotateStar()
    {
        Star_GO.transform.Rotate(Vector3.back, StarRotationSpeed * Time.deltaTime);
    }

    //CALL THESE METHODS TO USE THIS CLASS--------------------------------------
    public void ActivateLoadingCanvas()
    {
        gameObject.SetActive(true);

        if (Session.WebBuild)
        {
            GetComponent<Canvas>().targetDisplay = 0;
        }
    }

    public void DeactivateLoadingCanvas()
    {
        gameObject.SetActive(false);
    }
    //--------------------------------------------------------------------------



}
