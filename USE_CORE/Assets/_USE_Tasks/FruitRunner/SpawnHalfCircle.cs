using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnHalfCircle : MonoBehaviour
{
    private List<GameObject> prefabsToSpawn;
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

            float y = Session.UsingDefaultConfigs ? -2.25f : -1.75f;

            Vector3 spawnPosition = new Vector3(x, y, z + zAdj) + transform.position;

            GameObject instantiated = Instantiate(prefabsToSpawn[i], spawnPosition, Quaternion.identity);
            instantiated.transform.parent = transform;
            float scale = Session.UsingDefaultConfigs ? 3f : 2f;
            instantiated.transform.localScale = new Vector3(scale, scale, scale);
            instantiated.transform.LookAt(GameObject.Find("Player").transform);
            instantiated.SetActive(true);
        }
    }

    public void DestroySpawnedObjects()
    {
        for(int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }
}


