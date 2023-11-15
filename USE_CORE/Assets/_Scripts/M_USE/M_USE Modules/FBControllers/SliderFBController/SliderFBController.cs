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



using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using USE_Data;

public class SliderFBController : MonoBehaviour
{
    private Color PositiveHaloColor = new Color(1, 1, 0, 0.2f);
    private Color NegativeHaloColor = new Color(1, 1, 1, 0.2f);

    public GameObject SliderPrefab;
    public GameObject SliderHaloPrefab;

    [HideInInspector] public GameObject SliderGO;
    [HideInInspector] public GameObject SliderHaloGO;
    [HideInInspector] public Slider Slider;
    private Image SliderHaloImage;
    
    enum AnimationPhase { None, Update, Flashing };
    private AnimationPhase animationPhase = AnimationPhase.None;
    private bool sliderBarFull;
    
    private float animationStartTime;
    private float animationEndTime;
    private float flashingTime = 0.5f;
    private float updateTime = 0.5f;
    private int numSliderBarFull = 0;

    private float targetValue;
    private float sliderValueChange;

    private Vector3 InitialPosition;
    
    // Audio
    AudioFBController audioFBController;

    public void Init(DataController trialData, DataController frameData, AudioFBController audioFBController)
    {
        trialData.AddDatum("SliderBarValue", () => Slider?.value); //OLD (didnt have the question mark)
        //trialData.AddDatum("SliderBarValue", () => Slider != null ? Slider.value : -1); //NEW
        trialData.AddDatum("SliderBarFilled", ()=> sliderBarFull);
        frameData.AddDatum("SliderAnimationPhase", () => animationPhase.ToString());
        frameData.AddDatum("SliderVisibility", ()=> Slider != null? Slider.enabled : false);
        this.audioFBController = audioFBController;
    }
    public void InitializeSlider()
    {
        Transform sliderCanvas = GameObject.Find("SliderCanvas").transform;
        SliderGO = Instantiate(SliderPrefab, sliderCanvas);
        SliderGO.name = "Slider";
        InitialPosition = SliderGO.transform.localPosition;
        SliderHaloGO = Instantiate(SliderHaloPrefab, sliderCanvas);
        SliderHaloGO.name = "SliderHalo";
        SliderGO.SetActive(false);
        SliderHaloGO.SetActive(false);
        numSliderBarFull = 0; // Initialize at the Add_Control_Level_Initialization so this can be reset every block/ new task
    }

    public void ConfigureSlider(float sliderSize, float sliderInitialValue = 0, Vector3? posAdj = null)
    {
        SliderHaloImage = SliderHaloGO.GetComponent<Image>();
        Slider = SliderGO.GetComponent<Slider>();
        Slider.value = 0;
        Slider.value += sliderInitialValue;
        Slider.transform.localScale = new Vector3(sliderSize / 10f, sliderSize / 10f, 1f);
        SliderHaloGO.transform.localScale = new Vector3(sliderSize/ 10f, sliderSize / 10f, 1f);

        if(posAdj != null)
        {
            Vector3 newPos = InitialPosition + posAdj.Value;
            SliderGO.transform.localPosition = newPos;
        }
    }

    public void SetSliderRectSize(Vector2 size)
    {
        SliderGO.GetComponent<RectTransform>().sizeDelta = size;
    }

    public void SetUpdateDuration(float sliderUpdateDuration)
    {
        updateTime = sliderUpdateDuration;
    }
    public void SetFlashingDuration(float flashingDuration)
    {
        flashingTime = flashingDuration;
    }

    public bool isSliderBarFull()
    {
        return sliderBarFull;
    }

    public int GetNumSliderBarFull()
    {
        return numSliderBarFull;
    }
    public void ResetSliderBarFull()
    {
        sliderBarFull = false;
    }
    public void Update()
    {
        if (animationPhase == AnimationPhase.None)
        {
            if (SliderHaloGO != null)
                SliderHaloGO.SetActive(false);
           // sliderBarFull = false;
            return;
        }
        // Switch to next animation phase if the current one ended
        if (Time.unscaledTime >= animationEndTime)
        {
            animationStartTime = Time.unscaledTime;
            animationEndTime = animationStartTime;
            switch (animationPhase)
            {
                case AnimationPhase.Update:
                    if (Slider.value < 0)
                        Slider.value = 0; //set number to 0 if you lose more than you have, avoids neg slider
                    
                    animationPhase = AnimationPhase.None;
                    if (Slider.value >= 0.95f) // completes the slider within a 0.05 threshold
                    {
                        Slider.value = 1;
                        animationPhase = AnimationPhase.Flashing;
                        sliderBarFull = true;
                        numSliderBarFull++;
                        audioFBController.Play("TripleCollected");
                        Session.EventCodeManager.AddToFrameEventCodeBuffer(Session.EventCodeManager.SessionEventCodes["SliderFbController_SliderCompleteFbOn"]);
                        animationEndTime += updateTime + flashingTime;
                    }
                    break;
                case AnimationPhase.Flashing:
                    animationPhase = AnimationPhase.None;
                    Session.EventCodeManager.AddToFrameEventCodeBuffer(Session.EventCodeManager.SessionEventCodes["SliderFbController_SliderCompleteFbOn"]);
                    Session.EventCodeManager.AddToFrameEventCodeBuffer(Session.EventCodeManager.SessionEventCodes["SliderFbController_SliderReset"]);
                    break;
            }
        }
        // Set up the GUI state based on the animation phase
        //float dt = Time.unscaledTime - animationStartTime;
        switch (animationPhase)
        {
            case AnimationPhase.Update:
                float progress = (Time.unscaledTime - animationStartTime) / updateTime;
                Slider.value = Mathf.Lerp(Slider.value, targetValue, progress);
                break;
            case AnimationPhase.Flashing:
                int flashingInterval = (int)(flashingTime * 10000 / 4);
                int elapsed = (int)((Time.unscaledTime - animationStartTime) * 10000 % (flashingTime * 10000));
                int colorIndex = elapsed / flashingInterval;
                if (colorIndex % 2 == 0)
                    SliderHaloImage.color = PositiveHaloColor;
                else
                    SliderHaloImage.color = NegativeHaloColor;
                break;
        }
    }
    
    private void AnimateSlider(float sliderValueChange)
    {
        // Start the animation phase state machine with the first state
        SliderHaloGO.SetActive(true);
        if (sliderValueChange > 0)
        {
            audioFBController.Play("Positive");
            SliderHaloImage.color = PositiveHaloColor;
        }
        else 
        {
            audioFBController.Play("Negative");
            SliderHaloImage.color = NegativeHaloColor;
        }
        animationPhase = AnimationPhase.Update;
        animationStartTime = Time.unscaledTime;
        animationEndTime = animationStartTime + updateTime;
    }

    public void UpdateSliderValue(float sliderChange)
    {
        sliderValueChange = sliderChange;
        targetValue = Slider.value + sliderValueChange;
        
        if (targetValue < 0)
            targetValue = 0;
        
        AnimateSlider(sliderValueChange);
    }

}
