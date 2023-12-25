using UnityEngine;

public class Item : MonoBehaviour
{
    public bool NegativeItem;
    private AudioManager audioManager;


    private void Start()
    {
        audioManager = GameObject.Find("AudioManager").GetComponent<AudioManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (NegativeItem)
                audioManager.PlayNegativeItemClip();
            else
                audioManager.PlayPositiveItemClip();

            Destroy(gameObject);
        }
    }

}