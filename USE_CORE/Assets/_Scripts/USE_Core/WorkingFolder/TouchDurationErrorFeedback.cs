using UnityEngine;
using USE_StimulusManagement;

public class TouchDurationErrorFeedback:MonoBehaviour
{
    private bool Grating = false;

    private float TimeRemaining = 0;
    /*
    private void GratingSquareFeedback(SelectionHandler<StimDef> MouseHandler, GameObject go, AudioFBController audioFBController)
    {
        audioFBController.Play("Negative");
        if (MouseHandler.GetHeldTooShort())
            GratedSquareFlash(HeldTooShortTexture, go, gratingSquareDuration.value);
        else if (MouseHandler.GetHeldTooLong())
            GratedSquareFlash(HeldTooLongTexture, go, gratingSquareDuration.value);
        MouseHandler.SetHeldTooLong(false);
        MouseHandler.SetHeldTooShort(false);
        touchDurationError = false;
    }
    private void GratedSquareFlash(Texture2D newTexture, GameObject square, float gratingSquareDuration)
    {
        Color32 originalColor = square.GetComponent<Renderer>().material.color;
        Texture originalTexture = square.GetComponent<Renderer>().material.mainTexture;
        if (TimeRemaining > 0)
        {
            Grating = true;
            
            square.GetComponent<Renderer>().material.color = new Color32(224, 78, 92, 255);
            square.GetComponent<Renderer>().material.mainTexture = newTexture;
        }
        
        yield return new WaitForSeconds(gratingSquareDuration);
        square.GetComponent<Renderer>().material.mainTexture = originalTexture;
        square.GetComponent<Renderer>().material.color = originalColor;
        Grating = false;
        if (square.name == "FBSquare") square.SetActive(false);
    }*/
}
