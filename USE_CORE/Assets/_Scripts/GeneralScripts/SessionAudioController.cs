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
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class SessionAudioController : MonoBehaviour
{
    [HideInInspector] public AudioSource GeneralSounds_AudioSource;

    [HideInInspector] public AudioSource BackgroundMusic_AudioSource;
    private AudioClip BackgroundMusic_AudioClip;
    private float BackgroundMusic_AudioPlaybackSpot;

    public Dictionary<string, AudioClip> AudioClips;


    void Start()
    {
        GeneralSounds_AudioSource = gameObject.AddComponent<AudioSource>();

        BackgroundMusic_AudioPlaybackSpot = 0f;
        BackgroundMusic_AudioClip = Resources.Load<AudioClip>("BackgroundMusic");
        BackgroundMusic_AudioSource = gameObject.AddComponent<AudioSource>();
        BackgroundMusic_AudioSource.clip = BackgroundMusic_AudioClip;
        BackgroundMusic_AudioSource.loop = true;
        BackgroundMusic_AudioSource.volume = .55f;


        //Load Clips:
        AudioClips = new Dictionary<string, AudioClip>();

        AudioClip clip = Resources.Load<AudioClip>("ClickedButton");
        AudioClips.Add("ClickedButton", clip);

        clip = Resources.Load<AudioClip>("Error");
        AudioClips.Add("Error", clip);

        clip = Resources.Load<AudioClip>("Connected");
        AudioClips.Add("Connected", clip);


    }


    public void PlayAudioClip(string clipName)
    {
        if (AudioClips.TryGetValue(clipName, out AudioClip clip))
        {
            if (GeneralSounds_AudioSource.isPlaying)
                GeneralSounds_AudioSource.Stop();

            GeneralSounds_AudioSource.PlayOneShot(clip);
        }
        else
            Debug.LogWarning("TRIED TO PLAY CLIP " + clipName + " BUT IT HASNT BEEN ADDED");
    }

    public void PlayBackgroundMusic()
    {
        if (BackgroundMusic_AudioSource.isPlaying)
            return;
        BackgroundMusic_AudioSource.time = BackgroundMusic_AudioPlaybackSpot;
        BackgroundMusic_AudioSource.Play();
    }

    public void StopBackgroundMusic()
    {
        BackgroundMusic_AudioPlaybackSpot = BackgroundMusic_AudioSource.time;
        BackgroundMusic_AudioSource.Stop();
    }


    public void RestartBackgroundMusic()
    {
        StopBackgroundMusic();
        BackgroundMusic_AudioPlaybackSpot = 0f;
        PlayBackgroundMusic();
    }



    public AudioClip LoadExternalWAV(string filePath)
    {
        AudioClip audioClip = null;
        byte[] wavData;

        if (File.Exists(filePath))
        {
            wavData = File.ReadAllBytes(filePath);

            float[] floatData = new float[wavData.Length / 2]; //each sample in a 16 bit PCM WAV file is 2 bytes, so divide by 2. 
            for (int i = 0; i < wavData.Length / 2; i++) //convert each 2 byte sample into a float value. 
            {
                short sample = (short)((wavData[i * 2 + 1] << 8) | wavData[i * 2]); //combines 2 bytes to form a 16 bit short sample.
                floatData[i] = sample / 32768f; // Normalize FROM original range (-32768 to 32767) TO (-1, 1).
            }

            audioClip = AudioClip.Create(name, floatData.Length, 1, 44100, false);
            audioClip.SetData(floatData, 0);
        }
        else
            Debug.LogError("WAV FILE DOESNT EXIST AT FILEPATH: " + filePath);

        return audioClip;
    }





}
