using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraIntroMovement : MonoBehaviour
{
    public Transform Player;
    public float MovementDuration = 1f;
    public Vector3 StartingPos; 
    public Vector3 EndingPos;

    private float elapsedTime = 0f;

    public bool Move = false;

    public void StartMovement(Transform player, Vector3 startPos, Vector3 endPos)
    {
        Player = player;
        StartingPos = startPos;
        EndingPos = endPos;

        transform.position = StartingPos;

        Move = true;
    }

    private void Update()
    {
        if(Move)
        {
            if (elapsedTime < MovementDuration)
            {
                Vector3 newPosition = Vector3.Lerp(StartingPos, EndingPos, elapsedTime / MovementDuration);
                transform.position = newPosition;
                elapsedTime += Time.deltaTime;
            }
            else
            {
                //transform.SetPositionAndRotation(EndingPos, Quaternion.Euler(15f, 0f, 0f));
                Move = false;
                Destroy(this);
            }

        }
    }
}
