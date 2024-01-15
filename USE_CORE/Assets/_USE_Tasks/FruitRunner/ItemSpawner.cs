using System.Collections.Generic;
using UnityEngine;
using USE_StimulusManagement;


public class ItemSpawner : MonoBehaviour
{
    public List<GameObject> BlockadePrefabs;
    public List<GameObject> TrialQuaddles;
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
        TrialQuaddles = new List<GameObject>();
        foreach (StimDef stim in stims)
        {
            TrialQuaddles.Add(stim.StimGameObject);
        }
    }



    public void SpawnItem(Transform parentTransform)
    {
        GameObject stim;

        if (Count_ItemsBetweenDoors < TrialQuaddles.Count)
        {
            stim = Instantiate(TrialQuaddles[Random.Range(0, TrialQuaddles.Count)]);
            stim.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
            stim.AddComponent<Item_Quaddle>();
            stim.AddComponent<CapsuleCollider>().isTrigger = true;
            stim.tag = "Quaddle";

            Count_ItemsBetweenDoors++;
        }
        else
        {
            Count_ItemsBetweenDoors = 0;
            stim = Instantiate(BlockadePrefabs[Random.Range(0, BlockadePrefabs.Count)]);
            stim.name = "Blockade";
            stim.AddComponent<Item_Blockade>();
            stim.tag = "Blockade";

        }

        SetItemPosition(stim, parentTransform);
        stim.SetActive(true);
    }


    void SetItemPosition(GameObject item, Transform parentTransform)
    {
        bool isBlockade = item.name == "Blockade";

        List<Transform> spawnPoints = parentTransform.gameObject.GetComponent<Item_Floor>().spawnPoints;
        Transform randomSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];

        item.transform.position = new Vector3(isBlockade ? item.transform.position.x : randomSpawnPoint.position.x, isBlockade ? .75f : .7f, randomSpawnPoint.position.z); 
        item.transform.parent = parentTransform;
    }




}

