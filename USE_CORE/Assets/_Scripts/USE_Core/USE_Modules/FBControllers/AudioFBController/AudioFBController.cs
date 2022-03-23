using System;
using System.Collections.Generic;
using UnityEngine;

public class AudioFBController : MonoBehaviour
{
    [Serializable]
    public struct AudioFB {
        public string name;
        public AudioClip clip;
    }
    public AudioFB[] DefaultAudioFeedbacks;

    private AudioSource audioSource;
    private Dictionary<string, AudioClip> clips;

    public void Init() {
        audioSource = GameObject.FindWithTag("MainCamera").AddComponent<AudioSource>();
        clips = new Dictionary<string, AudioClip>();
        
        foreach (AudioFB audioFB in DefaultAudioFeedbacks) {
            clips.Add(audioFB.name, audioFB.clip);
        }
    }

    public AudioClip Get(string clipName) {
        return clips[clipName];
    }

    public AudioFBController Set(string clipName, AudioClip clip) {
        clips[clipName] = clip;
        return this;
    }

    public void Play(string clipName) {
        if (clips.TryGetValue(clipName, out AudioClip clip)) {
            audioSource.PlayOneShot(clip);
        } else {
            Debug.LogWarning("Trying to play clip " + clipName + " but it has not been added");
        }
    }

    public bool IsPlaying() {
        return audioSource.isPlaying;
    }
}