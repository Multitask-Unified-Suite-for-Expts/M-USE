using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraIntroMovement : MonoBehaviour
{
    public Transform player; // Reference to the player's transform
    public float movementDuration = 5f; // Duration of the intro movement
    public Vector3 startingPosition; // Explicitly set the starting position
    public Vector3 endingPosition; // Explicitly set the ending position

    private float elapsedTime = 0f;

    public bool Move = false;

    public void StartMovement(Vector3 startPos, Vector3 endPos)
    {
        startingPosition = startPos;
        endingPosition = endPos;

        transform.position = startingPosition;

        Move = true;
    }

    private void Update()
    {
        if(Move)
        {
            if (elapsedTime < movementDuration)
            {
                // Calculate the new position
                Vector3 newPosition = Vector3.Lerp(startingPosition, endingPosition, elapsedTime / movementDuration);

                // Set the new position and look at the player
                transform.position = newPosition;
                transform.LookAt(player.position);

                elapsedTime += Time.deltaTime;
            }
            else
            {
                Move = false;
                Debug.LogWarning("DONE WITH CAMERA MOVEMENT");
                Destroy(this);
            }

        }
    }
}
