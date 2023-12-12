using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    List<GameObject> items = new List<GameObject>();

    public List<GameObject> ItemPrefabs;


    public void SpawnItem(Transform parentTransform)
    {
        GameObject item = Instantiate(ItemPrefabs[Random.Range(0, ItemPrefabs.Count)]);
        item.transform.localScale = new Vector3(.3f, .3f, .3f);
        SetItemColor(item);
        SetItemPosition(item, parentTransform);
        items.Add(item);
    }

    void SetItemPosition(GameObject item, Transform parentTransform)
    {
        List<Transform> spawnPoints = new List<Transform>();
        foreach (Transform child in parentTransform)
            spawnPoints.Add(child);

        Transform randomSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];

        item.transform.position = new Vector3(randomSpawnPoint.position.x, 1f, randomSpawnPoint.position.z);
        item.transform.parent = parentTransform;
    }

    public void SetItemColor(GameObject item)
    {
        Material material = item.GetComponent<MeshRenderer>().material;
        material.color = item.GetComponent<Item>().NegativeItem ? Color.red : new Color32((byte)Random.Range(0, 256), (byte)Random.Range(0, 256), (byte)Random.Range(0, 256), 255);
    }

}

