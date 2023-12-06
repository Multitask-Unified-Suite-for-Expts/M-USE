using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    List<GameObject> items = new List<GameObject>();
    List<float> xPositions = new List<float>() {-1.25f, 0f, 1.25f};

    float SpawnStartTime;
    float SpawnGap = .65f;


    void Start()
    {
        SpawnItem();
    }

    private void Update()
    {
        if (Time.time - SpawnStartTime >= SpawnGap)
            SpawnItem();
    }

    void SpawnItem()
    {
        SpawnStartTime = Time.time;

        GameObject item = Instantiate(Resources.Load<GameObject>("Prefabs/Item"));
        int randomNum = Mathf.FloorToInt(Random.Range(0, 3));
        bool positiveItem = randomNum != 0; //two thirds chance for positive item, one third chance for neg item
        item.name = (positiveItem ? "Positive" : "Negative") + "_Item_" + items.Count;
        item.GetComponent<Item>().PositiveItem = positiveItem;
        SetItemColor(item, positiveItem);
        SetItemPosition(item);
        items.Add(item);
    }

    void SetItemPosition(GameObject item)
    {
        int randomX = Mathf.RoundToInt(Random.Range(0, 2.01f));
        float newXPos = xPositions[randomX];
        Vector3 spawnPos = new Vector3(newXPos, 1f, 60);
        item.transform.position = spawnPos;
        item.transform.parent = GameObject.Find("Floor").transform;
    }

    public void SetItemColor(GameObject item, bool isPositive)
    {
        Material material = item.GetComponent<MeshRenderer>().material;
        material.color = isPositive? Color.green : Color.red;
        //material.color = new Color32((byte)Random.Range(0, 256), (byte)Random.Range(0, 256), (byte)Random.Range(0, 256), 255);
    }

}

