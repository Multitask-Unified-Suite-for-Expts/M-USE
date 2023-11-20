using UnityEngine;

public class CameraSyncController : MonoBehaviour
{
    private float Camera_PulseSentTime = 0f;
    private readonly float PulseInterval = 0.030f; // Set the interval to 30 milliseconds

    void FixedUpdate()
    {
        float timeSinceLastFrame = Time.deltaTime;
        Debug.Log("TIME DIFF: " + timeSinceLastFrame);

        if (Session.SessionDef != null && Session.SessionDef.SendCameraPulses && Session.SyncBoxController != null && Session.SessionDef.SyncBoxActive)
        {
            if (Time.time - Camera_PulseSentTime >= PulseInterval)
            {
                Debug.Log("this is the supposed interval: " + (Time.time - Camera_PulseSentTime));
              //  Session.SyncBoxController.SendCameraSyncPulses(Session.SessionDef.Camera_NumPulses, Session.SessionDef.Camera_PulseSize_Ticks);
                Camera_PulseSentTime = Time.time;
            }
        }
    }

}
