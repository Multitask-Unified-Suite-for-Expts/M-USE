using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BounceStim : MonoBehaviour
{
    public float bounceHeight = .2f;
    public float bounceSpeed = 5f;
    public float bounceOffset = 0f;

    public Vector3 initialPosition;
    private float startTime;


    void Start()
    {
        initialPosition = transform.position;
        bounceOffset = Random.Range(0f, 2f);
        startTime = Time.time;
    }

    void Update()
    {
        Bounce();        
    }

    void Bounce()
    {
        float bounceDirection = Mathf.Sin((Time.time - startTime + bounceOffset) * bounceSpeed);
        float newY = initialPosition.y + bounceDirection * bounceHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }


}
