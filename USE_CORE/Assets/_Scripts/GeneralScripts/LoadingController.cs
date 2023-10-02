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


//NT: USE THIS CLASS BY SETTING SessionValues.LoadingCanvas_GO Active or Inactive!

public class LoadingController : MonoBehaviour
{
    //For loading circle animation:
    public GameObject FillCircle_GO;
    [HideInInspector] public Image FillCircle_Image;
    private float Progress;

    //For star animation:
    public GameObject Star_GO;
    private float starRotationSpeed = 125f;


    void Start()
    {
        FillCircle_Image = FillCircle_GO.GetComponent<Image>();
        Progress = 0f;
        FillCircle_Image.fillAmount = Progress;
    }

    private void Update()
    {
        RotateStar();
        //LoadingCircleAnimation();
    }

    private void RotateStar()
    {
        Star_GO.transform.Rotate(Vector3.forward, starRotationSpeed * Time.deltaTime);
    }

    private void LoadingCircleAnimation()
    {
        if (FillCircle_GO.activeInHierarchy)
        {
            Progress += 1f * Time.deltaTime;
            if (Progress >= 1f)
                Progress = 0f;
            FillCircle_Image.fillAmount = Progress;
        }
    }


}
