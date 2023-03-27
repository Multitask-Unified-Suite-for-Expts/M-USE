using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using USE_Data;
using USE_ExperimentTemplate_Classes;

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
    private bool sliderBarFull = false;
    
    private float animationStartTime = 0f;
    private float animationEndTime = 0f;
    private float flashingTime = 0.5f;
    private float updateTime = 0.5f;
    private int sliderInitialValue = 0;
    private int numSliderBarFull = 0;
    private int numFlashes = 4;

    private float targetValue = 0;
    private float sliderValueChange = 0;
    private double frameRate = 0.0167;
    
    // Audio
    AudioFBController audioFBController;

    //Event Codes:
    public EventCodeManager EventCodeManager;
    public Dictionary<string, EventCode> SessionEventCodes;

    public void Init(DataController trialData, DataController frameData, AudioFBController audioFBController)
    {
        trialData.AddDatum("SliderBarValue", () => Slider.value);
        trialData.AddDatum("SliderBarFilled", ()=> sliderBarFull);
        frameData.AddDatum("SliderAnimationPhase", () => animationPhase.ToString());
        frameData.AddDatum("SliderVisibility", ()=> Slider != null? Slider.enabled : false);
        this.audioFBController = audioFBController;
    }
    public void InitializeSlider()
    {
        Transform sliderCanvas = GameObject.Find("SliderCanvas").transform;
        SliderGO = Instantiate(SliderPrefab, sliderCanvas);
        SliderHaloGO = Instantiate(SliderHaloPrefab, sliderCanvas);
        SliderGO.SetActive(false);
        SliderHaloGO.SetActive(false);
        numSliderBarFull = 0; // Initialize at the Add_Control_Level_Initialization so this can be reset every block/ new task

        EventCodeManager = new EventCodeManager();
    }

    public void ConfigureSlider(Vector3 sliderPosition, float sliderSize, float sliderInitialValue = 0)
    {
        SliderHaloImage = SliderHaloGO.GetComponent<Image>();
        Slider = SliderGO.GetComponent<Slider>();
        Slider.value = 0;
        Slider.value += sliderInitialValue;
        SliderGO.transform.localPosition = sliderPosition;
        SliderHaloGO.transform.localPosition = sliderPosition;
        Slider.transform.localScale = new Vector3(sliderSize / 10f, sliderSize / 10f, 1f);
        SliderHaloGO.transform.localScale = new Vector3(sliderSize/ 10f, sliderSize / 10f, 1f);

    }

    public void SetUpdateDuration(float sliderUpdateDuration)
    {
        updateTime = sliderUpdateDuration;
    }
    public void SetFlashingDuration(float flashingDuration)
    {
        flashingTime = flashingDuration;
    }

    public void SetNumFlashes(int num)
    {
        numFlashes = num;
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
            EndHaloAnimation();
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
                    if (Slider.value >= 1)
                    {
                        animationPhase = AnimationPhase.Flashing;
                        sliderBarFull = true;
                        numSliderBarFull++;
                        audioFBController.Play("TripleCollected");
                        EventCodeManager.SendCodeImmediate(SessionEventCodes["SliderFbController_SliderCompleteFbOn"]);
                       // animationEndTime += updateTime + flashingTime;
                        animationEndTime += updateTime + flashingTime;

                    }
                    break;
                case AnimationPhase.Flashing:
                    animationPhase = AnimationPhase.None;
                    EventCodeManager.SendCodeImmediate(SessionEventCodes["SliderFbController_SliderCompleteFbOn"]);
                    EventCodeManager.SendCodeImmediate(SessionEventCodes["SliderFbController_SliderReset"]);
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
                int flashingInterval = (int)(flashingTime * 10000 / numFlashes);
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
        AnimateSlider(sliderValueChange);
    }

    private void EndHaloAnimation()
    {
        if (Time.unscaledTime >= animationEndTime)
        {
            // when the slider halo is done flashing
            if(SliderHaloGO != null)
                SliderHaloGO.SetActive(false);
        }
    }

}
