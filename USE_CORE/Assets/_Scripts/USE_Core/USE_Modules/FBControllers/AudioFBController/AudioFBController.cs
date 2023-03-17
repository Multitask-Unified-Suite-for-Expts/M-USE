using System;
using System.Collections.Generic;
using UnityEngine;
using USE_Data;
using USE_ExperimentTemplate_Classes;

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

    public EventCodeManager EventCodeManager;
    public Dictionary<string, EventCode> SessionEventCodes;


    public void Init(DataController frameData)
    {
        frameData.AddDatum("PlayingAudioClipName", () => playingClipName);

        UpdateAudioSource();
        clips = new Dictionary<string, AudioClip>();
        
        foreach (AudioFB audioFB in DefaultAudioFeedbacks)
            clips.Add(audioFB.name, audioFB.clip);

        EventCodeManager = new EventCodeManager();
    }

    public void UpdateAudioSource() //When new task starts, old audio is deactivated. need to find the new one
    {
        foreach (GameObject camera in GameObject.FindGameObjectsWithTag("MainCamera"))
        {
            if (camera.activeInHierarchy)
            {
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
            EventCodeManager.SendCodeImmediate(SessionEventCodes["AudioFbController_SelectionAuditoryFbOn"]);
        }
        else
            Debug.LogWarning("Trying to play clip " + clipName + " but it has not been added");
        
    }

    public bool IsPlaying()
    {
        return audioSource.isPlaying;
    }

    private void Update()
    {
        if (audioSource != null && !audioSource.isPlaying)
            playingClipName = null;
    }
}