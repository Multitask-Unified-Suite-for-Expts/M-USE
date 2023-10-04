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


using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


/*
 USE THIS CLASS BY SETTING SessionValues.LoadingCanvas_GO ACTIVE OR INACTIVE!
*/

public class LoadingController : MonoBehaviour
{
    //For loading circle animation:
    public GameObject FillCircle_GO;
    [HideInInspector] public Image FillCircle_Image;
    private float Progress;

    //For star animation:
    public GameObject Star_GO;
    private readonly float StarRotationSpeed = 125f;

    private TextMeshProUGUI LoadingText;


    void Start()
    {
        FillCircle_Image = FillCircle_GO.GetComponent<Image>();
        if (FillCircle_Image == null)
            Debug.LogWarning("FILL CIRCLE IMAGE IS NULL!");
        else
        {
            Progress = 0f;
            FillCircle_Image.fillAmount = Progress;
        }

        LoadingText = SessionValues.LoadingCanvas_GO.GetComponentInChildren<TextMeshProUGUI>();
        if (LoadingText == null)
            Debug.LogWarning("LOADING TEXT IS NULL!");
        else
            LoadingText.text = "Loading..";
    }

    private void Update()
    {
        RotateStar();
        //LoadingCircleAnimation();
    }

    private void RotateStar()
    {
        Star_GO.transform.Rotate(Vector3.forward, StarRotationSpeed * Time.deltaTime);
    }

    private void LoadingCircleAnimation()
    {
        if (FillCircle_GO.activeInHierarchy)
        {
            Progress += Time.deltaTime;
            if (Progress >= 1f)
                Progress -= 1f;
            FillCircle_Image.fillAmount = Progress;
        }
    }


}
