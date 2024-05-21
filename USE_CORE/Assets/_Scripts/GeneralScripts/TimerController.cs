using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;


//Script is attached to the Scripts/MiscScripts GameObject.
//Trial level's can use this class by calling CreateTimer() and StartTimer();

public class TimerController : MonoBehaviour
{
    private AudioSource ClockAudioSource;
    private AudioClip ClockAudioClip;

    [HideInInspector] public GameObject TimerGO;
    [HideInInspector] public Timer Timer;


    private void Start()
    {
        LoadAudioClip();
    }

    private void LoadAudioClip()
    {
        try
        {
            ClockAudioClip = Resources.Load<AudioClip>("ClockTicking");
            if (ClockAudioClip == null)
            {
                Debug.LogError("CLOCK AUDIO CLIP NOT FOUND IN RESOURCES!");
            }
            else
            {
                ClockAudioSource = gameObject.AddComponent<AudioSource>();
                ClockAudioSource.volume = 1f;
                ClockAudioSource.loop = true;
                ClockAudioSource.clip = ClockAudioClip;
            }
        }
        catch(Exception e)
        {
            Debug.LogError("FAILED LOADING AUDIO CLIP! ERROR: " + e.Message);
        }
    }

    public void CreateTimer(Transform parent)
    {
        if(TimerGO != null)
        {
            Destroy(TimerGO);
        }

        try
        {
            TimerGO = Instantiate(Resources.Load<GameObject>("Timer"));

            TimerGO.SetActive(false);
            TimerGO.name = "Timer";

            Timer = TimerGO.GetComponent<Timer>();

            TimerGO.transform.SetParent(parent.transform);
            TimerGO.transform.localScale = Vector3.one;

            RectTransform rect = TimerGO.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0);
                rect.anchorMax = new Vector2(0.5f, 0);
                rect.pivot = new Vector2(0.5f, 0);
                rect.anchoredPosition = new Vector2(0, 25);
                rect.localPosition = new Vector3(rect.localPosition.x, rect.localPosition.y, 0);
            }
            else
                Debug.LogWarning("NO RECT TRANSFORM ON THE TIMER GAMEOBJECT!");
        }
        catch(Exception e)
        {
            Debug.LogError("FAILED TO CREATE TIMER GAMEOBJECT! ERROR: " + e.Message);
        }

    }

    public void StartTimer(float duration)
    {
        StopAllCoroutines();
        StartCoroutine(TimerRoutine(duration));
    }

    public void StopTimer()
    {
        StopAllCoroutines();
        ClockAudioSource.Stop();
        if(Timer.TimerFillImage != null)
            Timer.TimerFillImage.fillAmount = 0f;
        if(Timer.TimerText != null)
            Timer.TimerText.text = "0";
        if(TimerGO != null)
            TimerGO.SetActive(false);
    }

    private IEnumerator TimerRoutine(float duration)
    {
        float timeRemaining = duration;
        Timer.TimerFillImage.fillAmount = 1f;
        ClockAudioSource.Play();

        TimerGO.SetActive(true);

        while (timeRemaining > 0 && Timer.TimerText != null && Timer.TimerFillImage != null)
        {
            timeRemaining -= Time.deltaTime;
            Timer.TimerText.text = timeRemaining.ToString("0");
            Timer.TimerFillImage.fillAmount = timeRemaining / duration;
            yield return null;
        }

        StopTimer();
    }


    public void ActivateTimer()
    {
        if (TimerGO != null)
            TimerGO.SetActive(true);
    }

    public void DeactivateTimer()
    {
        if (TimerGO != null)
            TimerGO.SetActive(false);
    }

}
