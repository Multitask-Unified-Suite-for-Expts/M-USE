using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    public GameObject cam;

    void Start()
    {
        cam = Camera.main.gameObject;
    }

    void Update()
    {
        transform.LookAt(cam.transform);
    }
}
