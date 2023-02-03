using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using USE_ExperimentTemplate_Trial;
using USE_StimulusManagement;

public class TaskHelperFunctions
{

    private bool Grating = false; 
    //Error Tracking Variables
    private bool TouchDurationError;
    private int NumTouchDurationError;
    private StimDef StimDef;


    public IEnumerator GratedSquareFlash(Texture2D newTexture, GameObject square, float gratingSquareDuration)
    {
        Grating = true;
        Color32 originalColor = square.GetComponent<Renderer>().material.color;
        Texture originalTexture = square.GetComponent<Renderer>().material.mainTexture;
        square.GetComponent<Renderer>().material.color = new Color32(224, 78, 92, 255);
        square.GetComponent<Renderer>().material.mainTexture = newTexture;
        yield return new WaitForSeconds(gratingSquareDuration);
        square.GetComponent<Renderer>().material.mainTexture = originalTexture;
        square.GetComponent<Renderer>().material.color = originalColor;
        Grating = false;
        if (square.name == "FBSquare") square.SetActive(false);
    }
}
