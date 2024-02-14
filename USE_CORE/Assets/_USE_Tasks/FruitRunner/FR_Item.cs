using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


public class FR_Item : MonoBehaviour
{
    protected FR_FloorManager floorManager;
    protected FR_PlayerManager playerManager;
    protected GameObject PlayerGO;

    private Collider Collider;

    [HideInInspector] public bool CodeSent;

    [HideInInspector] public GameObject ParticlesGO;

    public string GeneralPosition;



    private void Awake()
    {
        try
        {
            floorManager = GameObject.Find("FloorManager").GetComponent<FR_FloorManager>();
            playerManager = GameObject.Find("Player").GetComponent<FR_PlayerManager>();
            Collider = GetComponent<Collider>();
            PlayerGO = playerManager.gameObject;
        }
        catch(Exception e)
        {
            Debug.LogError("ITEM START METHOD FAILED! Message: " + e.Message);
        }
    }

    private void Update()
    {
        if(PlayerGO != null && !CodeSent)
        {
            if (Collider.bounds.max.z <= PlayerGO.transform.position.z)
            {
                ItemMissed();
                CodeSent = true;
            }
        }
    }

    public virtual void ItemMissed()
    {
    }

    public virtual void SetItemPosition(Transform parentTransform)
    {
    }

}

public class FR_Item_Quaddle : FR_Item
{
    public string QuaddleType;
    public int QuaddleTokenRewardMag;


    public override void ItemMissed()
    {
        if (QuaddleType == "Positive")
            FR_EventManager.TriggerTargetMissed(GeneralPosition);
        else
            FR_EventManager.TriggerDistractorAvoided(GeneralPosition);

    }

    public override void SetItemPosition(Transform parentTransform)
    {
        List<Transform> spawnPoints = parentTransform.gameObject.GetComponent<FR_Item_Floor>().spawnPoints;
        Transform spawnPoint;

        switch (GeneralPosition.ToLower().Trim())
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
        ParticlesGO = Instantiate(Resources.Load<GameObject>(QuaddleType == "Positive" ? "Prefabs/ParticleHaloPositive" : "Prefabs/ParticleHaloNegative"));
        ParticlesGO.transform.position = spawnPos;
        ParticlesGO.transform.localScale = new Vector3(.5f, .5f, .5f);
        Destroy(ParticlesGO, 2f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            FR_EventManager.TriggerScoreChanged(QuaddleTokenRewardMag * 1000);

            if (QuaddleType == "Positive")
            {
                playerManager.StartAnimation("Happy");
                playerManager.TokenFbController.AddTokens(gameObject, QuaddleTokenRewardMag, -.3f);
                FR_EventManager.TriggerTargetHit(GeneralPosition);
            }
            else if (QuaddleType == "Negative" || QuaddleType == "Neutral")
            {
                playerManager.StartAnimation("Sad");
                playerManager.TokenFbController.RemoveTokens(gameObject, Mathf.Abs(QuaddleTokenRewardMag), -.3f); //abs value since its negative
                FR_EventManager.TriggerDistractorHit(GeneralPosition);
            }

            CreateParticlesOnObject(transform.position);
            gameObject.SetActive(false);
        }
    }
}


//Currently still random positions though
public class FR_Item_Banana : FR_Item
{
    public int TokenGain;

    public Vector3 rotationAxis = Vector3.up;
    public float rotationSpeed = 100f;

    public float bobbingAmount = .1f;
    public float bobbingSpeed = .5f;

    private Vector3 originalPos;
    private float randomBobbingOffset;


    private void Start()
    {
        transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        originalPos = transform.position;
        randomBobbingOffset = Random.Range(0f, 2 * Mathf.PI);
    }

    private void Update()
    {
        float rotationAmount = rotationSpeed * Time.deltaTime;
        transform.Rotate(rotationAxis, rotationAmount);

        //float bobbingOffset = Mathf.Sin((Time.time + randomBobbingOffset) * 2 * Mathf.PI * bobbingSpeed) * bobbingAmount;
        //transform.position = new Vector3(originalPos.x, originalPos.y, transform.position.z) + Vector3.up * bobbingOffset;
    }

    public override void ItemMissed()
    {
        FR_EventManager.TriggerTargetMissed(GeneralPosition);
    }

    public override void SetItemPosition(Transform parentTransform)
    {
        List<Transform> spawnPoints = parentTransform.gameObject.GetComponent<FR_Item_Floor>().spawnPoints;
        Transform randomSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];

        if (randomSpawnPoint.position.x == 0)
            GeneralPosition = "Middle";
        else if (randomSpawnPoint.position.x < 0)
            GeneralPosition = "Left";
        else
            GeneralPosition = "Right";

        transform.position = new Vector3(randomSpawnPoint.position.x, .75f, randomSpawnPoint.position.z);
        transform.parent = parentTransform;
    }

    public void CreateParticlesOnObject(Vector3 spawnPos)
    {
        ParticlesGO = Instantiate(Resources.Load<GameObject>("Prefabs/ParticleHaloPositive"));
        ParticlesGO.transform.position = spawnPos;
        ParticlesGO.transform.localScale = new Vector3(.5f, .5f, .5f);
        Destroy(ParticlesGO, 2f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            FR_EventManager.TriggerTargetHit(GeneralPosition);
            FR_EventManager.TriggerScoreChanged(1000);
            playerManager.StartAnimation("Happy");
            playerManager.TokenFbController.AddTokens(gameObject, TokenGain, -.3f);
            CreateParticlesOnObject(transform.position);
            gameObject.SetActive(false);
        }
    }


}

public class FR_Item_Blockade : FR_Item
{
    public bool HitByPlayer;
    public int TokenLoss;


    public override void SetItemPosition(Transform parentTransform)
    {
        List<Transform> spawnPoints = parentTransform.gameObject.GetComponent<FR_Item_Floor>().spawnPoints;
        Transform randomSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];
        transform.position = new Vector3(transform.position.x, .75f, randomSpawnPoint.position.z);
        transform.parent = parentTransform;
    }

    public override void ItemMissed()
    {
        if(!HitByPlayer)
            FR_EventManager.TriggerBlockadeAvoided(GeneralPosition);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            HitByPlayer = true;
            FR_EventManager.TriggerScoreChanged(-500);
            FR_EventManager.TriggerBlockadeHit(GeneralPosition);
            playerManager.TokenFbController.RemoveTokens(other.gameObject, TokenLoss, .4f);
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
