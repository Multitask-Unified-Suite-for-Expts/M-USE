using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class ShakeStim : MonoBehaviour
{
    public float Speed = 5f;
    public float Radius = .025f;

    private Vector3 OriginalPosition;
    private float CurrentAngle;

    private void Start()
    {
        OriginalPosition = transform.position;
        CurrentAngle = Random.Range(0, 360);
    }

    private void Update()
    {
        if(transform.gameObject.activeInHierarchy)
        {
            CurrentAngle += Speed * Time.deltaTime;
            float x = OriginalPosition.x + Mathf.Cos(CurrentAngle) * Radius;
            float y = OriginalPosition.y + Mathf.Sin(CurrentAngle) * Radius;
            transform.position = new Vector3(x, y, OriginalPosition.z);
        }

    }
}
