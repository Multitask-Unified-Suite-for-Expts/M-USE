using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class FR_AudioManager : MonoBehaviour
{
    private AudioSource Player_AudioSource;
    private AudioClip Slide_AudioClip;
    private AudioClip Cheering_AudioClip;


    private void Start()
    {
        try
        {
            Player_AudioSource = gameObject.AddComponent<AudioSource>();
            Slide_AudioClip = Resources.Load<AudioClip>("AudioClips/Slide");
            Cheering_AudioClip = Resources.Load<AudioClip>("AudioClips/Cheer");
        }
        catch(Exception e)
        {
            Debug.LogError("FR_AudioManager Start() method failed! | Error: " + e.Message);
        }

    }

    public void StopAllAudio()
    {
        Player_AudioSource.Stop();
    }

    public void PlayCrowdCheering()
    {
        Player_AudioSource.clip = Cheering_AudioClip;
        Player_AudioSource.volume = 1f;
        Player_AudioSource.Play();
    }

    public void PlaySlideClip()
    {
        Player_AudioSource.clip = Slide_AudioClip;
        Player_AudioSource.volume = .2f;
        Player_AudioSource.Play();
    }




}
