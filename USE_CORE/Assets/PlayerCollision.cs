using UnityEngine;

public class PlayerCollision : MonoBehaviour
{
    public GameObject selectedStimulus;

    void OnCollisionEnter(Collision collision)
    {
        GameObject collidedObject = collision.gameObject;

        // Check if the collided object is Stimulus
        if (collidedObject.CompareTag("Stimulus"))
        {
            selectedStimulus = collidedObject;
            collidedObject.SetActive(false);
        }
    }
}