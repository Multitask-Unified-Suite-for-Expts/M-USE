using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraCircle : MonoBehaviour
{
    public float radius = 25f;
    public float speed = .03f;
    [HideInInspector] public float targetHeight = 5f;

    private float angle = 180f;
    private Vector3 originalCamPos;
    private LayerMask groundLayer;

    void Start()
    {
        originalCamPos = transform.position;
        groundLayer = LayerMask.GetMask("Terrain");
    }

    void Update()
    {
        float x = Mathf.Sin(angle) * radius + originalCamPos.x;
        float z = Mathf.Cos(angle) * radius + originalCamPos.z;
        Vector3 newPosition = new Vector3(x, 0f, z); // Temporarily set y to 0

        RaycastHit hit;
        if (Physics.Raycast(newPosition + Vector3.up * 100f, Vector3.down, out hit, Mathf.Infinity, groundLayer))
        {
            float groundHeight = hit.point.y;
            newPosition.y = groundHeight + targetHeight; // Set the y-position to maintain target height above ground
        }

        transform.position = newPosition;

        angle -= speed * Time.deltaTime;

        if (angle >= 360f)
            angle -= 360f;
    }

}
