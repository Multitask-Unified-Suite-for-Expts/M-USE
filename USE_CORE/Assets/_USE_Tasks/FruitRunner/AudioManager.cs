using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class AudioManager : MonoBehaviour
{
    private AudioSource AudioSource;
    private AudioClip PositiveCollected_AudioClip;
    private AudioClip NegativeCollected_AudioClip;


    private void Start()
    {
        AudioSource = gameObject.GetComponent<AudioSource>();
        PositiveCollected_AudioClip = Resources.Load<AudioClip>("AudioClips/CorrectItemPickup");
        NegativeCollected_AudioClip = Resources.Load<AudioClip>("AudioClips/WrongItemPickup");
    }

    public void PlayPositiveItemCollected()
    {
        AudioSource.clip = PositiveCollected_AudioClip;
        AudioSource.volume = 1f;
        AudioSource.Play();
    }

    public void PlayNegativeItemCollected()
    {
        AudioSource.clip = NegativeCollected_AudioClip;
        AudioSource.volume = .2f;
        AudioSource.Play();
    }

}
