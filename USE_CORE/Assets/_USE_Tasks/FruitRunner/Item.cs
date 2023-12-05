using UnityEngine;

public class Item : MonoBehaviour
{
    float ItemSpawnTime;
    float ItemLifeLength = 12f;
    public bool PositiveItem;

    private AudioManager audioManager;

    private void Start()
    {
        audioManager = GameObject.Find("AudioManager").GetComponent<AudioManager>();
        ItemSpawnTime = Time.time;
    }

    private void Update()
    {
        if (Time.time - ItemSpawnTime >= ItemLifeLength)
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (PositiveItem)
                audioManager.PlayPositiveItemCollected();
            else
                audioManager.PlayNegativeItemCollected();

            Destroy(gameObject);
        }
    }

}