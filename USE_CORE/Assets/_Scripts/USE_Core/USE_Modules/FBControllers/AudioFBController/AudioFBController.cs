using System.Collections.Generic;
using UnityEngine;

public class AudioFBController : MonoBehaviour
{
    public Dictionary<string, AudioClip> clips;

    public AudioClip PositiveClip;
    public AudioClip NegativeClip;

    private AudioSource audioSource;

    public void Init() {
        audioSource = GameObject.FindWithTag("MainCamera").AddComponent<AudioSource>();
        clips = new Dictionary<string, AudioClip>();
        
        if (PositiveClip == null) {
            Debug.LogWarning("No positive clip specified");
        } else {
            Set("Positive", PositiveClip);
        }

        if (NegativeClip == null) {
            Debug.LogWarning("No negative clip specified");
        } else {
            Set("Negative", NegativeClip);
        }
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