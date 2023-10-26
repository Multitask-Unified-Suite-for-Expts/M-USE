using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestButton : MonoBehaviour
{

    void Update()
    {
        if(InputBroker.GetMouseButtonDown(0))
        {
            GameObject hit = InputBroker.RaycastBoth(InputBroker.mousePosition);
            if (hit == gameObject)
                Destroy(gameObject);
        }
    }
}
