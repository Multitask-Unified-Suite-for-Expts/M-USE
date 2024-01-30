using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnHalfCircle : MonoBehaviour
{
    private GameObject prefabToSpawn;
    private int numberOfObjects = 6;
    private float archRadius = 9;
    private float angleRange = 180;

    private float zAdj = 10f;

    void Start()
    {
        prefabToSpawn = Resources.Load<GameObject>("Prefabs/stimExample");
        SpawnObjectsInArch();
    }

    void SpawnObjectsInArch()
    {
        float angleStep = angleRange / (numberOfObjects - 1);

        for (int i = 0; i < numberOfObjects; i++)
        {
            float angle = i * angleStep; //36
            float radians = Mathf.Deg2Rad * angle;

            float x = archRadius * Mathf.Cos(radians);
            float z = archRadius * Mathf.Sin(radians);

            Vector3 spawnPosition = new Vector3(x, -1f, z + zAdj) + transform.position;

            GameObject instantiated = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
            instantiated.AddComponent<BounceStim>();
            instantiated.AddComponent<AnimateArms>();
            instantiated.transform.localScale *= 3f;
            instantiated.transform.LookAt(GameObject.Find("Player").transform);
        }
    }
}



public class AnimateArms : MonoBehaviour
{
    public Transform leftArm;
    public Transform rightArm;
    public float waveFrequency = 5f;
    public float waveAmplitude = 60f;

    private float timeOffset;

    void Start()
    {
        leftArm = transform.Find("Arm");
        rightArm = transform.Find("Arm.001");

        timeOffset = Random.Range(0f, 1f); // Add a random offset to avoid synchronized waving
    }

    void Update()
    {
        // Calculate the waving motion based on time
        float waveOffset = Mathf.Sin((Time.time + timeOffset) * waveFrequency) * waveAmplitude;

        // Apply the waving motion to the arms
        if (leftArm != null)
        {
            leftArm.localRotation = Quaternion.Euler(waveOffset, 0f, 0f);
        }

        if (rightArm != null)
        {
            rightArm.localRotation = Quaternion.Euler(-waveOffset, 0f, 0f);
        }
    }
}
