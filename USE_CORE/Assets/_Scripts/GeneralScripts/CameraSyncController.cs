using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSyncController : MonoBehaviour
{
    private float Camera_PulseSentTime = 0f;
    private readonly float PulseInterval = 1f / 30f;


    void Update()
    {
        if (Session.SessionDef != null && Session.SessionDef.SendCameraPulses && Session.SyncBoxController != null && Session.SessionDef.SyncBoxActive)
        {
            if (Time.time - Camera_PulseSentTime >= PulseInterval)
            {
                Session.SyncBoxController.SendCameraSyncPulses(Session.SessionDef.Camera_NumPulses, Session.SessionDef.Camera_PulseSize_Ticks);
                Camera_PulseSentTime = Time.time;
            }
        }
    }
}
