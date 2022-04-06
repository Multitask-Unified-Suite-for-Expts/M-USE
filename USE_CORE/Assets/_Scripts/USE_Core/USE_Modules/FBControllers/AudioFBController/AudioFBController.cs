using System;
using System.Collections.Generic;
using UnityEngine;
using USE_Data;

public class AudioFBController : MonoBehaviour
{
    [Serializable]
    public struct AudioFB {
        public string name;
        public AudioClip clip;
    }
    public AudioFB[] DefaultAudioFeedbacks;

    public AudioSource audioSource;
    private Dictionary<string, AudioClip> clips;

    private string playingClipName = null;

    public void Init(DataController frameData) {
        frameData.AddDatum("PlayingAudioClipName", () => playingClipName);

        UpdateAudioSource();
        clips = new Dictionary<string, AudioClip>();
        
        foreach (AudioFB audioFB in DefaultAudioFeedbacks) {
            clips.Add(audioFB.name, audioFB.clip);
        }
    }

    // Every time a new task is started, the old audio source is deactivated,
    // so we need to make sure to find the new one
    public void UpdateAudioSource() {
        foreach (GameObject camera in GameObject.FindGameObjectsWithTag("MainCamera")) {
            if (camera.activeInHierarchy) {
                audioSource = camera.AddComponent<AudioSource>();
                break;
            }
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
        playingClipName = clipName;
        if (clips.TryGetValue(clipName, out AudioClip clip)) {
            audioSource.PlayOneShot(clip);
        } else {
            Debug.LogWarning("Trying to play clip " + clipName + " but it has not been added");
        }
    }

    public bool IsPlaying() {
        return audioSource.isPlaying;
    }

    private void Update() {
        if (audioSource != null && !audioSource.isPlaying) {
            playingClipName = null;
        }
    }
}