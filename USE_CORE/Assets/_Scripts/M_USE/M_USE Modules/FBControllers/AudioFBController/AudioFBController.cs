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
using System.Collections.Generic;
using UnityEngine;
using USE_Data;

public class AudioFBController : MonoBehaviour
{
    [Serializable]
    public struct AudioFB
    {
        public string name;
        public AudioClip clip;
    }
    public AudioFB[] DefaultAudioFeedbacks;

    public AudioSource audioSource;
    private Dictionary<string, AudioClip> clips;

    private string playingClipName = null;


    public void Init(DataController frameData)
    {
        frameData.AddDatum("PlayingAudioClipName", () => playingClipName);

        UpdateAudioSource();
        clips = new Dictionary<string, AudioClip>();
        
        foreach (AudioFB audioFB in DefaultAudioFeedbacks)
            clips.Add(audioFB.name, audioFB.clip);

    }

    public void UpdateAudioSource() //When new task starts, old audio is deactivated. need to find the new one
    {
        foreach (GameObject camera in GameObject.FindGameObjectsWithTag("MainCamera"))
        {
            if (camera.activeInHierarchy)
            {
                audioSource = camera.GetComponent<AudioSource>();
                if(audioSource == null)
                    audioSource = camera.AddComponent<AudioSource>();
                break;
            }
        }
    }

    public AudioClip GetClip(string clipName)
    {
        return clips[clipName];
    }

    public AudioFBController AddClip(string clipName, AudioClip clip)
    {
        clips[clipName] = clip;
        return this;
    }

    public void Play(string clipName)
    {
        playingClipName = clipName;
        if (clips.TryGetValue(clipName, out AudioClip clip))
        {
            if (IsPlaying())
                audioSource.Stop();
            audioSource.PlayOneShot(clip);
            SessionValues.EventCodeManager.SendCodeImmediate(SessionValues.EventCodeManager.SessionEventCodes["AudioFbController_SelectionAuditoryFbOn"]);
        }
        else
            Debug.LogWarning("Trying to play clip " + clipName + " but it has not been added");
        
    }

    public bool IsPlaying()
    {
        if(audioSource != null)
            return audioSource.isPlaying;
        return false;
    }

    private void Update()
    {
        if (audioSource != null && !audioSource.isPlaying)
            playingClipName = null;
    }
}