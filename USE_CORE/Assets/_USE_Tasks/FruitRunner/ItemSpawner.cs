using System.Collections.Generic;
using FruitRunner_Namespace;
using UnityEngine;
using USE_StimulusManagement;


public class ItemSpawner : MonoBehaviour
{
    public List<GameObject> BlockadePrefabs;
    public List<FruitRunner_StimDef> TrialQuaddles;
    private int Count_ItemsBetweenDoors = 0;


    private void Start()
    {
        BlockadePrefabs = new List<GameObject>
        {
            Resources.Load<GameObject>("Prefabs/Blockade_Left"),
            Resources.Load<GameObject>("Prefabs/Blockade_Right")
        };
    }

    public void AddToQuaddleList(List<StimDef> stims)
    {
        TrialQuaddles = new List<FruitRunner_StimDef>();
        foreach (FruitRunner_StimDef stim in stims)
            TrialQuaddles.Add(stim);
    }



    public void SpawnItem(Transform parentTransform)
    {        
        GameObject stim;

        if (Count_ItemsBetweenDoors < TrialQuaddles.Count)
        {
            FruitRunner_StimDef quaddle = TrialQuaddles[Count_ItemsBetweenDoors];

            stim = Instantiate(quaddle.StimGameObject);
            stim.tag = "Quaddle";
            stim.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);

            Item_Quaddle quaddleComponent = stim.AddComponent<Item_Quaddle>();
            quaddleComponent.QuaddleType = quaddle.QuaddleType;
            quaddleComponent.QuaddleGeneralPosition = quaddle.QuaddleGeneralPosition;

            stim.AddComponent<CapsuleCollider>().isTrigger = true;
            SetQuaddlePosition(stim, parentTransform);
            Count_ItemsBetweenDoors++;
        }
        else
        {
            Count_ItemsBetweenDoors = 0;
            stim = Instantiate(BlockadePrefabs[Random.Range(0, BlockadePrefabs.Count)]);
            stim.name = "Blockade";
            stim.tag = "Blockade";
            stim.AddComponent<Item_Blockade>();
            SetBlockadePosition(stim, parentTransform);
        }

        //SetItemPosition(stim, parentTransform);
        stim.SetActive(true);
    }

    void SetQuaddlePosition(GameObject item, Transform parentTransform)
    {
        string generalPosition = item.GetComponent<Item_Quaddle>().QuaddleGeneralPosition;

        List<Transform> spawnPoints = parentTransform.gameObject.GetComponent<Item_Floor>().spawnPoints;
        Transform spawnPoint;

        switch(generalPosition.ToLower().Trim())
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

        item.transform.position = new Vector3(spawnPoint.position.x, .7f, spawnPoint.position.z);
        item.transform.parent = parentTransform;
    }

    void SetBlockadePosition(GameObject item, Transform parentTransform)
    {
        List<Transform> spawnPoints = parentTransform.gameObject.GetComponent<Item_Floor>().spawnPoints;
        Transform randomSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];
        item.transform.position = new Vector3(item.transform.position.x, .75f, randomSpawnPoint.position.z);
        item.transform.parent = parentTransform;
    }



    //void SetItemPosition(GameObject item, Transform parentTransform)
    //{
    //    bool isBlockade = item.name == "Blockade";

    //    List<Transform> spawnPoints = parentTransform.gameObject.GetComponent<Item_Floor>().spawnPoints;
    //    Transform randomSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];

    //    item.transform.position = new Vector3(isBlockade ? item.transform.position.x : randomSpawnPoint.position.x, isBlockade ? .75f : .7f, randomSpawnPoint.position.z); 
    //    item.transform.parent = parentTransform;
    //}




}

