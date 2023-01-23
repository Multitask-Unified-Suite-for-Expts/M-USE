using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using USE_ExperimentTemplate_Trial;
using USE_StimulusManagement;

public class TaskHelperFunctions
{
    private Texture2D HeldTooShortTexture;
    private Texture2D HeldTooLongTexture;
    private Texture2D StartButtonTexture;
    private GameObject StartButton;
    private GameObject FBSquare;

    private bool Grating = false; 
    //Error Tracking Variables
    private bool TouchDurationError;
    private int NumTouchDurationError;
    private StimDef StimDef;
    private GameObject SquareGO;

    public int GetNumTouchDurationError()
    {
        return NumTouchDurationError;
    }

    public void SetNumTouchDurationError(int val)
    {
        NumTouchDurationError = val;
    }
    public Vector2 playerViewPosition(Vector3 position, Transform playerViewParent)
    {
        Vector2 pvPosition = new Vector2((position[0] / Screen.width) * playerViewParent.GetComponent<RectTransform>().sizeDelta.x, (position[1] / Screen.height) * playerViewParent.GetComponent<RectTransform>().sizeDelta.y);
        return pvPosition;
    }
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
    public GameObject CreateSquare(string name)
    {
        SquareGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        SquareGO.name = name;
       // SquareGO.AddComponent<MeshRenderer>();
        //SquareGO.AddComponent<Renderer>();
        SquareGO.GetComponent<Renderer>().material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        SquareGO.GetComponent<Renderer>().material.SetFloat("_SpecularHighlights",0f);
        return SquareGO;
    }
    
    public GameObject CreateStartButton(Texture2D StartButtonTexture, Vector3 StartButtonPosition, Vector3 StartButtonScale)
    {
        StartButton = CreateSquare("StartButton");
        StartButton.GetComponent<Renderer>().material.mainTexture = StartButtonTexture;
        StartButton.transform.position = StartButtonPosition;
        StartButton.transform.localScale = StartButtonScale;
        StartButton.SetActive(false);
        return StartButton;
    }
    public GameObject CreateFBSquare(Texture2D FBSquareTexture, Vector3 FBSquarePosition, Vector3 FBSquareScale)
    {
        FBSquare = CreateSquare("FBSquare");
        FBSquare.GetComponent<Renderer>().material.mainTexture = FBSquareTexture;
        FBSquare.transform.localScale = FBSquareScale;
        FBSquare.transform.position = FBSquarePosition;
        FBSquare.SetActive(false);
        return FBSquare;
    }
    public int ChooseTokenReward(TokenReward[] tokenRewards)
    {
        float totalProbability = 0;
        for (int i = 0; i < tokenRewards.Length; i++)
        {
            totalProbability += tokenRewards[i].Probability;
        }

        if (Math.Abs(totalProbability - 1) > 0.001)
            Debug.LogError("Sum of token reward probabilities on this trial is " + totalProbability + ", probabilities will be scaled to sum to 1.");

        float randomNumber = UnityEngine.Random.Range(0, totalProbability);

        TokenReward selectedReward = tokenRewards[0];
        float curProbSum = 0;
        foreach (TokenReward tr in tokenRewards)
        {
            curProbSum += tr.Probability;
            if (curProbSum >= randomNumber)
            {
                selectedReward = tr;
                break;
            }
        }
        return selectedReward.NumTokens;
    }
    public void SetShadowType(String ShadowType, String LightName)
    {
        //User options are None, Soft, Hard
        switch (ShadowType)
        {
            case "None":
                GameObject.Find("Directional Light").GetComponent<Light>().shadows = LightShadows.None;
                GameObject.Find(LightName).GetComponent<Light>().shadows = LightShadows.None;
                break;
            case "Soft":
                GameObject.Find("Directional Light").GetComponent<Light>().shadows = LightShadows.Soft;
                GameObject.Find(LightName).GetComponent<Light>().shadows = LightShadows.Soft;
                break;
            case "Hard":
                GameObject.Find("Directional Light").GetComponent<Light>().shadows = LightShadows.Hard;
                GameObject.Find(LightName).GetComponent<Light>().shadows = LightShadows.Hard;
                break;
            default:
                Debug.Log("User did not Input None, Soft, or Hard for the Shadow Type");
                break;
        }
    }
    /*
    private void TouchDurationErrorFeedback(SelectionHandler<StimDef> MouseHandler, GameObject go, 
           Texture2D HeldTooShortTexture, Texture2D HeldTooLongTexture, float gratingSquareDuration)
    {
        AudioFBController.Play("Negative");
        if (MouseHandler.GetHeldTooShort())
            StartCoroutine(GratedSquareFlash(HeldTooShortTexture, go, gratingSquareDuration));
        else if (MouseHandler.GetHeldTooLong())
            StartCoroutine(GratedSquareFlash(HeldTooLongTexture, go, gratingSquareDuration));
        
        MouseHandler.SetHeldTooLong(false);
        MouseHandler.SetHeldTooShort(false);
        TouchDurationError = false;
        //TouchDurationError_InBlock++; MOVE INTO INDIVIDUAL TASK?? 
    }*/
}
