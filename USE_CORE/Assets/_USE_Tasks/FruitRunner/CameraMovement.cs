using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public Transform Player;
    private Vector3 offset = new Vector3(0, 2f, -2.6f);

    //private void LateUpdate()
    //{
    //    if (Player != null)
    //    {
    //        transform.position = Player.position + offset;
    //    }
    //}
}
