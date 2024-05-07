using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImageRotator : MonoBehaviour
{
    public float rotationSpeed = 60f;


    void Update()
    {
        transform.Rotate(0f, 0f, -(rotationSpeed * Time.deltaTime));
    }
}
