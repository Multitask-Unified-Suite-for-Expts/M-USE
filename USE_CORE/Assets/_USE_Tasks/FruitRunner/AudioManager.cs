using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class AudioManager : MonoBehaviour
{
    private AudioSource ItemAudioSource;
    private AudioSource PlayerAudioSource;

    private AudioClip PositiveCollected_AudioClip;
    private AudioClip NegativeCollected_AudioClip;
    private AudioClip Slide_AudioClip;
    private AudioClip Cheering_AudioClip;

    private void Start()
    {
        ItemAudioSource = gameObject.AddComponent<AudioSource>();
        PlayerAudioSource = gameObject.AddComponent<AudioSource>();
        PositiveCollected_AudioClip = Resources.Load<AudioClip>("AudioClips/CorrectItemPickup");
        NegativeCollected_AudioClip = Resources.Load<AudioClip>("AudioClips/WrongItemPickup");
        Slide_AudioClip = Resources.Load<AudioClip>("AudioClips/Slide");
        Cheering_AudioClip = Resources.Load<AudioClip>("AudioClips/Cheer");
    }

    public void StopAllAudio()
    {
        ItemAudioSource.Stop();
        PlayerAudioSource.Stop();
    }

    public void PlayCrowdCheering()
    {
        PlayerAudioSource.clip = Cheering_AudioClip;
        PlayerAudioSource.volume = 1f;
        PlayerAudioSource.Play();
    }

    public void PlaySlideClip()
    {
        PlayerAudioSource.clip = Slide_AudioClip;
        PlayerAudioSource.volume = .2f;
        PlayerAudioSource.Play();
    }

    public void PlayPositiveItemClip()
    {
        ItemAudioSource.clip = PositiveCollected_AudioClip;
        ItemAudioSource.volume = 1f;
        ItemAudioSource.Play();
    }

    public void PlayNegativeItemClip()
    {
        ItemAudioSource.clip = NegativeCollected_AudioClip;
        ItemAudioSource.volume = .2f;
        ItemAudioSource.Play();
    }



}
