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

    public AudioClip GetClip(string clipName) {
        return clips[clipName];
    }

    public AudioFBController AddClip(string clipName, AudioClip clip) {
        clips[clipName] = clip;
        return this;
    }

    public AudioFBController AddTone(string clipName, float freq, float duration) {
        AudioClip clip = AudioClip.Create(clipName, (int)(duration * 44100), 1, 44100, false);
        float[] samples = new float[clip.samples];
        for (int i = 0; i < samples.Length; i++) {
            samples[i] = Mathf.Sin(2 * Mathf.PI * freq * i / clip.frequency);
        }
        clip.SetData(samples, 0);

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