using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivateMultiDisplays : MonoBehaviour
{
    // Use this for initialization
    void Start()
    {

        if (!Application.isEditor)
        {
            Application.targetFrameRate = 60;
            for (int i = 1; i < Display.displays.Length; i++)
            {
                Display.displays[i].Activate();
                // Display.displays[i].SetRenderingResolution(Screen.width, Screen.height);
            }
        }
    }
}