using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coin : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        // spin the coin
        transform.Rotate(new Vector3(0f, 100f, 0f) * Time.deltaTime);
    }
}
