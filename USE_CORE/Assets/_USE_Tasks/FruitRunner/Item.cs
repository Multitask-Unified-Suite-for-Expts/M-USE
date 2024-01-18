using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


public class Item : MonoBehaviour
{
    protected AudioManager audioManager;
    protected FloorManager floorManager;
    protected PlayerMovement playerMovement;


    private void Start()
    {
        audioManager = GameObject.Find("AudioManager").GetComponent<AudioManager>();
        floorManager = GameObject.Find("FloorManager").GetComponent<FloorManager>();
        playerMovement = GameObject.Find("Player").GetComponent<PlayerMovement>();
    }

    public virtual void SetItemPosition(Transform parentTransform)
    {
        Debug.LogWarning("PARENT GETTING CALLED");
    }
}

public class Item_Quaddle : Item
{
    public string QuaddleType;
    public string QuaddleGeneralPosition;


    public override void SetItemPosition(Transform parentTransform)
    {
        List<Transform> spawnPoints = parentTransform.gameObject.GetComponent<Item_Floor>().spawnPoints;
        Transform spawnPoint;

        switch (QuaddleGeneralPosition.ToLower().Trim())
        {
            case "left":
                spawnPoint = spawnPoints[0];
                break;
            case "middle":
                spawnPoint = spawnPoints[1];
                break;
            case "right":
                spawnPoint = spawnPoints[2];
                break;
            default:
                Debug.LogWarning("DEFAULT SWITCH CASE FOR STIM GENERAL POSITION. SETTING TO MIDDLE!");
                spawnPoint = spawnPoints[1];
                break;
        }

        transform.position = new Vector3(spawnPoint.position.x, .65f, spawnPoint.position.z);
        transform.parent = parentTransform;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if(QuaddleType == "Positive")
            {
                playerMovement.StartAnimation("Happy");
                playerMovement.TokenFbController.AddTokens(other.gameObject, 1, .4f);
            }
            else if(QuaddleType == "Negative")
            {
                playerMovement.StartAnimation("Sad");
                playerMovement.TokenFbController.RemoveTokens(other.gameObject, 1, .4f);
            }
            else if(QuaddleType == "Neutral")
            {
                //what to do with neutral stim?
                audioManager.PlayPositiveItemClip();
            }
            Destroy(gameObject);
        }
    }
}

public class Item_Blockade : Item
{

    public override void SetItemPosition(Transform parentTransform)
    {
        List<Transform> spawnPoints = parentTransform.gameObject.GetComponent<Item_Floor>().spawnPoints;
        Transform randomSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];
        transform.position = new Vector3(transform.position.x, .75f, randomSpawnPoint.position.z);
        transform.parent = parentTransform;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerMovement.TokenFbController.RemoveTokens(other.gameObject, 1, .4f);
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

public class Item_Floor : Item
{
    public List<Transform> spawnPoints;


    private void Awake()
    {
        spawnPoints = new List<Transform>();

        Transform child = transform.Find("Row4");

        foreach(Transform grandChild in child)
        {
            if (grandChild.name.ToLower().Contains("spawnpoint"))
                spawnPoints.Add(grandChild);
        }
    }
}
