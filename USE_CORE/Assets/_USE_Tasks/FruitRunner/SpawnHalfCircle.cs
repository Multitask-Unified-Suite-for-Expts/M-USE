using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnHalfCircle : MonoBehaviour
{
    private List<GameObject> prefabsToSpawn;
    public List<GameObject> spawnedGameObjects;
    private float archRadius = 7;
    private float angleRange = 180;
    private float zAdj = 9f;


    public void SetPrefabs(List<GameObject> prefabs)
    {
        prefabsToSpawn = prefabs;
    }

    public void SpawnObjectsInArch()
    {
        float angleStep = angleRange / (prefabsToSpawn.Count - 1);

        for (int i = 0; i < prefabsToSpawn.Count; i++)
        {
            float angle = i * angleStep;
            float radians = Mathf.Deg2Rad * angle;

            float x = archRadius * Mathf.Cos(radians);
            float z = archRadius * Mathf.Sin(radians);

            Vector3 spawnPosition = new Vector3(x, -1.75f, z + zAdj) + transform.position;

            spawnedGameObjects = new List<GameObject>();

            GameObject instantiated = Instantiate(prefabsToSpawn[i], spawnPosition, Quaternion.identity);
            instantiated.transform.parent = transform;
            //instantiated.AddComponent<BounceStim>();
            instantiated.transform.localScale = new Vector3(2f, 2f, 2f);
            instantiated.transform.LookAt(GameObject.Find("Player").transform);
            instantiated.SetActive(true);
            spawnedGameObjects.Add(instantiated);
        }
    }

    public void DestroySpawnedObjects()
    {
        if(spawnedGameObjects != null && spawnedGameObjects.Count > 0)
        foreach (GameObject go in spawnedGameObjects)
            Destroy(go);
    }
}


