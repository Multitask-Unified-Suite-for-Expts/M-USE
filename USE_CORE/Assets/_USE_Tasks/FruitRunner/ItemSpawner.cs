using System.Collections.Generic;
using UnityEngine;
using USE_StimulusManagement;


public class ItemSpawner : MonoBehaviour
{
    public List<GameObject> DoorPrefabs;
    public List<GameObject> TrialQuaddles;
    private int ItemsBetweenDoorCount = 0;


    private void Start()
    {
        DoorPrefabs = new List<GameObject>
        {
            Resources.Load<GameObject>("Prefabs/LeftDoor"),
            Resources.Load<GameObject>("Prefabs/RightDoor")
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

        //stim = Instantiate(TrialQuaddles[Random.Range(0, TrialQuaddles.Count)]);
        //stim.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
        //stim.AddComponent<Item>();
        //stim.AddComponent<CapsuleCollider>().isTrigger = true;

        if (ItemsBetweenDoorCount < TrialQuaddles.Count)
        {
            stim = Instantiate(TrialQuaddles[Random.Range(0, TrialQuaddles.Count)]);
            stim.transform.localScale = new Vector3(1.25f, 1.25f, 1.25f);
            stim.AddComponent<Item>();
            stim.AddComponent<CapsuleCollider>().isTrigger = true;
            ItemsBetweenDoorCount++;
        }
        else
        {
            ItemsBetweenDoorCount = 0;
            stim = Instantiate(DoorPrefabs[Random.Range(0, DoorPrefabs.Count)]);
            stim.name = "Door";
        }

        SetItemPosition(stim, parentTransform);
        stim.SetActive(true);
    }


    void SetItemPosition(GameObject item, Transform parentTransform)
    {
        bool isDoor = item.name == "Door";

        List<Transform> spawnPoints = new List<Transform>();
        foreach (Transform child in parentTransform)
            spawnPoints.Add(child);

        Transform randomSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];
        item.transform.position = new Vector3(isDoor ? item.transform.position.x : randomSpawnPoint.position.x, isDoor ? .75f : .8f, randomSpawnPoint.position.z); //.5 for items. .4 for quaddles
        item.transform.parent = parentTransform;
    }



}

