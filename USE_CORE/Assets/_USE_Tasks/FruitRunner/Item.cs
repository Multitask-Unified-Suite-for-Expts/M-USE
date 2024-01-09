using System;
using System.Collections;
using UnityEngine;

public class Item : MonoBehaviour
{
    protected AudioManager audioManager;
    protected FloorManager floorManager;
    protected PlayerMovement playerMovement;

    private bool isHandlingCollision = false;


    private void Start()
    {
        audioManager = GameObject.Find("AudioManager").GetComponent<AudioManager>();
        floorManager = GameObject.Find("FloorManager").GetComponent<FloorManager>();
        playerMovement = GameObject.Find("Player").GetComponent<PlayerMovement>();

    }
}

public class Item_Quaddle : Item
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            //audioManager.PlayNegativeItemClip();
            audioManager.PlayPositiveItemClip();
            Destroy(gameObject);
        }
    }
}

public class Item_Door : Item
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            audioManager.PlayNegativeItemClip();
            playerMovement.StartAnimation("injured");
            floorManager.DeactivateMovement();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerMovement.StartAnimation("run");
            floorManager.ActivateMovement();
        }
    }

}