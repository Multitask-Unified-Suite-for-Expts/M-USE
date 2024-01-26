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
        try
        {
            audioManager = GameObject.Find("AudioManager").GetComponent<AudioManager>();
            floorManager = GameObject.Find("FloorManager").GetComponent<FloorManager>();
            playerMovement = GameObject.Find("Player").GetComponent<PlayerMovement>();
        }
        catch(Exception e)
        {
            Debug.LogError("ITEM START METHOD FAILED! Message: " + e.Message);
        }
    }

    public virtual void SetItemPosition(Transform parentTransform)
    {
    }
}

public class Item_Quaddle : Item
{
    public string QuaddleType;
    public string QuaddleGeneralPosition;
    public int QuaddleTokenRewardMag;


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
        
        transform.position = new Vector3(spawnPoint.position.x, .7f, spawnPoint.position.z);
        transform.parent = parentTransform;
    }

    private void CreateParticlesOnObject(Vector3 spawnPos)
    {
        Vector3 offset = new Vector3(0f, 0f, 0f);
        GameObject particleGO = Instantiate(Resources.Load<GameObject>(QuaddleType == "Positive" ? "Prefabs/ParticleHaloPositive" : "Prefabs/ParticleHaloNegative"));
        particleGO.transform.position = spawnPos + offset;
        particleGO.transform.localScale = new Vector3(.5f, .5f, .5f);
        Destroy(particleGO, 2f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            //playerMovement.StartAnimation("slash");

            if(QuaddleType == "Positive")
            {
                playerMovement.StartAnimation("Happy");
                playerMovement.TokenFbController.AddTokens(gameObject, QuaddleTokenRewardMag, -.3f);
                //playerMovement.TokenFbController.AddTokens(other.gameObject, QuaddleTokenRewardMag, .6f);
            }
            else if(QuaddleType == "Negative")
            {
                playerMovement.StartAnimation("Sad");
                playerMovement.TokenFbController.RemoveTokens(gameObject, Mathf.Abs(QuaddleTokenRewardMag), -.3f); //abs value since its negative
                //playerMovement.TokenFbController.RemoveTokens(other.gameObject, Mathf.Abs(QuaddleTokenRewardMag), .6f); //abs value since its negative
            }
            else if(QuaddleType == "Neutral")
            {
                playerMovement.StartAnimation("Sad");
                playerMovement.TokenFbController.RemoveTokens(gameObject, Mathf.Abs(QuaddleTokenRewardMag), -.3f); //abs value since its negative
                //playerMovement.TokenFbController.RemoveTokens(other.gameObject, Mathf.Abs(QuaddleTokenRewardMag), .6f); //abs value since its negative

            }
            CreateParticlesOnObject(transform.position);
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
