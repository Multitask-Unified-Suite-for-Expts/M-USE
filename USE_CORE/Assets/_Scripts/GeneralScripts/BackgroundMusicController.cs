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

using System;
using UnityEngine;

public class BackgroundMusicController : MonoBehaviour
{
    [HideInInspector] public AudioSource BackgroundMusic_AudioSource;
    private AudioClip BackgroundMusic_AudioClip;
    private float AudioPlaybackSpot;


    void Start()
    {
        AudioPlaybackSpot = 0f;
        SetupMusic();
    }

    private void SetupMusic()
    {
        BackgroundMusic_AudioClip = Resources.Load<AudioClip>("PerfectBeauty");
        //BackgroundMusic_AudioClip = Resources.Load<AudioClip>("BackgroundMusic");
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

    public void StopMusic()
    {
        AudioPlaybackSpot = BackgroundMusic_AudioSource.time;
        BackgroundMusic_AudioSource.Stop();
    }


    public void RestartMusic()
    {
        StopMusic();
        AudioPlaybackSpot = 0f;
        PlayMusic();
    }

    public void ChangeVolume(float newVolume)
    {
        newVolume = Mathf.Clamp01(newVolume);
        BackgroundMusic_AudioSource.volume = newVolume;
    }

    

}
