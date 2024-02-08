using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


public class FR_Item : MonoBehaviour
{
    protected FR_FloorManager floorManager;
    protected FR_PlayerManager playerManager;


    private void Start()
    {
        try
        {
            floorManager = GameObject.Find("FloorManager").GetComponent<FR_FloorManager>();
            playerManager = GameObject.Find("Player").GetComponent<FR_PlayerManager>();
        }
        catch(Exception e)
        {
            Debug.LogError("ITEM START METHOD FAILED! Message: " + e.Message);
        }

        if (playerManager == null)
            Debug.LogError("PLAYER MAN IS NULL!!!!!!!!!!!!");
        else
            Debug.LogWarning("NOT NULL....");
    }

    public virtual void SetItemPosition(Transform parentTransform)
    {
    }
}

public class FR_Item_Quaddle : FR_Item
{
    public string QuaddleType;
    public string QuaddleGeneralPosition;
    public int QuaddleTokenRewardMag;


    public override void SetItemPosition(Transform parentTransform)
    {
        List<Transform> spawnPoints = parentTransform.gameObject.GetComponent<FR_Item_Floor>().spawnPoints;
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
            FR_EventManager.TriggerScoreChanged(QuaddleTokenRewardMag * 1000);

            if(QuaddleType == "Positive")
            {
                playerManager.StartAnimation("Happy");
                playerManager.TokenFbController.AddTokens(gameObject, QuaddleTokenRewardMag, -.3f);
            }
            else if(QuaddleType == "Negative")
            {
                playerManager.StartAnimation("Sad");
                playerManager.TokenFbController.RemoveTokens(gameObject, Mathf.Abs(QuaddleTokenRewardMag), -.3f); //abs value since its negative
            }
            else if(QuaddleType == "Neutral")
            {
                playerManager.StartAnimation("Sad");
                playerManager.TokenFbController.RemoveTokens(gameObject, Mathf.Abs(QuaddleTokenRewardMag), -.3f); //abs value since its negative

            }
            CreateParticlesOnObject(transform.position);
            Destroy(gameObject);
        }
    }
}


//BUT HOW DO I CONTROL HOW MUCH TOKEN REWARD??
public class FR_Item_Banana : FR_Item
{
    public Vector3 rotationAxis = Vector3.up;
    public float rotationSpeed = 75f;

    private void Start()
    {
        transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
    }

    private void Update()
    {
        float rotationAmount = rotationSpeed * Time.deltaTime;
        transform.Rotate(rotationAxis, rotationAmount);
    }

    public override void SetItemPosition(Transform parentTransform)
    {
        List<Transform> spawnPoints = parentTransform.gameObject.GetComponent<FR_Item_Floor>().spawnPoints;
        Transform randomSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];
        transform.position = new Vector3(randomSpawnPoint.position.x, .75f, randomSpawnPoint.position.z);
        transform.parent = parentTransform;
    }

    public void CreateParticlesOnObject(Vector3 spawnPos)
    {
        Vector3 offset = new Vector3(0f, 0f, 0f);
        GameObject particleGO = Instantiate(Resources.Load<GameObject>("Prefabs/ParticleHaloPositive"));
        particleGO.transform.position = spawnPos + offset;
        particleGO.transform.localScale = new Vector3(.5f, .5f, .5f);
        Destroy(particleGO, 2f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            FR_EventManager.TriggerScoreChanged(1000);
            //if (playerManager == null)
            //    Debug.LogWarning("PLAYER IS NULL");
            //playerManager.StartAnimation("Happy");
            //playerManager.TokenFbController.AddTokens(gameObject, 1, -.3f);

            //Session.TrialLevel.HaloFBController.SetParticleHaloSize(.5f);
            //Session.TrialLevel.HaloFBController.ShowPositive(gameObject);

            CreateParticlesOnObject(transform.position);

            Destroy(gameObject);
        }
    }


}

public class FR_Item_Blockade : FR_Item
{

    public override void SetItemPosition(Transform parentTransform)
    {
        List<Transform> spawnPoints = parentTransform.gameObject.GetComponent<FR_Item_Floor>().spawnPoints;
        Transform randomSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];
        transform.position = new Vector3(transform.position.x, .75f, randomSpawnPoint.position.z);
        transform.parent = parentTransform;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            FR_EventManager.TriggerScoreChanged(-500);

            playerManager.TokenFbController.RemoveTokens(other.gameObject, 1, .4f);
            playerManager.StartAnimation("injured");
            floorManager.DeactivateMovement();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerManager.StartAnimation("run");
            floorManager.ActivateMovement();
        }
    }

}

public class FR_Item_Floor : FR_Item
{
    public List<Transform> spawnPoints;


    private void Awake()
    {
        spawnPoints = new List<Transform>();

        try
        {
            Transform child = transform.Find("Row4");
       
            foreach(Transform grandChild in child)
            {
                if (grandChild.name.ToLower().Contains("spawnpoint"))
                    spawnPoints.Add(grandChild);
            }
        }
        catch(Exception e)
        {
            Debug.LogError("FAILED DURING FR_Item_Floor's Awake Method! | Error: " + e.Message);
        }

    }
}
