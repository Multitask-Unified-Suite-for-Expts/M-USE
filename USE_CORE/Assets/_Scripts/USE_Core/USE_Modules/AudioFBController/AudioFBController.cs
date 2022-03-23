using UnityEngine;

public class AudioFBController : MonoBehaviour
{
    public AudioClip PositiveSound;
    public AudioClip NegativeSound;

    private AudioSource audioSource;

    public void Init() {
        audioSource = GameObject.FindWithTag("MainCamera").AddComponent<AudioSource>();
    }

    public void PlayPositive() {
        audioSource.PlayOneShot(PositiveSound);
    }
    
    public void PlayNegative() {
        audioSource.PlayOneShot(NegativeSound);
    }

    public bool IsPlaying() {
        return audioSource.isPlaying;
    }
}