using System;
using UnityEngine;

public class BackgroundMusicController : MonoBehaviour
{
    private AudioClip BackgroundMusic_AudioClip;
    private float AudioPlaybackSpot;
    [HideInInspector] public AudioSource BackgroundMusic_AudioSource;


    void Start()
    {
        AudioPlaybackSpot = 0f;
        SetupMusic();
    }

    private void SetupMusic()
    {
        BackgroundMusic_AudioClip = Resources.Load<AudioClip>("BackgroundMusic");
        BackgroundMusic_AudioSource = gameObject.AddComponent<AudioSource>();
        BackgroundMusic_AudioSource.clip = BackgroundMusic_AudioClip;
        BackgroundMusic_AudioSource.loop = true;
        BackgroundMusic_AudioSource.volume = .55f;
    }

    public void PlayMusic()
    {
        if (BackgroundMusic_AudioSource.isPlaying)
            return;
        BackgroundMusic_AudioSource.time = AudioPlaybackSpot;
        BackgroundMusic_AudioSource.Play();
    }

    public void StopBackgroundMusic()
    {
        AudioPlaybackSpot = BackgroundMusic_AudioSource.time;
        BackgroundMusic_AudioSource.Stop();
    }

    

}
