using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class ShakeStim : MonoBehaviour
{
    public float speed = 5f;
    public float radius = .025f;

    private Vector3 originalPosition;
    private float currentAngle;

    private void Start()
    {
        originalPosition = transform.position;
        currentAngle = Random.Range(0, 360);
    }

    private void Update()
    {
        if(transform.gameObject.activeInHierarchy)
        {
            currentAngle += speed * Time.deltaTime;
            float x = originalPosition.x + Mathf.Cos(currentAngle) * radius;
            float y = originalPosition.y + Mathf.Sin(currentAngle) * radius;
            transform.position = new Vector3(x, y, originalPosition.z);
        }

    }
}
