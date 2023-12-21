using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ItemSpawner : MonoBehaviour
{
    List<GameObject> items = new List<GameObject>();

    public List<GameObject> ItemPrefabs;
    public List<GameObject> DoorPrefabs;

    public List<GameObject> Quaddles;

    private int NumItemsBetweenDoor = 10;
    private int ItemsBetweenDoorCount = 0;


    public void SpawnItem(Transform parentTransform)
    {
        GameObject item;

        if(ItemsBetweenDoorCount < NumItemsBetweenDoor)
        {
            item = Instantiate(Quaddles[Random.Range(0, Quaddles.Count)]);
            //item = Instantiate(ItemPrefabs[Random.Range(0, ItemPrefabs.Count)]);
            item.name = "Item";
            item.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            //item.transform.localScale = new Vector3(.3f, .3f, .3f);
            //SetItemColor(item);
            ItemsBetweenDoorCount++;
        }
        else
        {
            ItemsBetweenDoorCount = 0;
            item = Instantiate(DoorPrefabs[Random.Range(0, DoorPrefabs.Count)]);
            item.name = "Door";
        }

        if (item.GetComponent<Item>() == null)
            item.AddComponent<Item>();

        SetItemPosition(item, parentTransform);
        items.Add(item);
    }


    void SetItemPosition(GameObject item, Transform parentTransform)
    {
        bool isDoor = item.name == "Door";

        List<Transform> spawnPoints = new List<Transform>();
        foreach (Transform child in parentTransform)
            spawnPoints.Add(child);

        Transform randomSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];
        item.transform.position = new Vector3(isDoor ? item.transform.position.x : randomSpawnPoint.position.x, isDoor ? .75f : .4f, randomSpawnPoint.position.z); //.5 for items. .4 for quaddles
        item.transform.parent = parentTransform;
    }

    public void SetItemColor(GameObject item)
    {
        Material material = item.GetComponent<MeshRenderer>().material;
        material.color = item.GetComponent<Item>().NegativeItem ? Color.red : new Color32((byte)Random.Range(0, 256), (byte)Random.Range(0, 256), (byte)Random.Range(0, 256), 255);
    }

}

