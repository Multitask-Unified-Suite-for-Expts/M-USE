using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleHalo : MonoBehaviour
{
    public GameObject PositiveParticleHaloPrefab; 
    public GameObject NegativeParticleHaloPrefab; 

    private GameObject InstantiatedParticleHaloGO;

    private float ParticleEffectDuration;

    public void Initialize(GameObject posParticleHaloPrefab, GameObject negParticleHaloPrefab)
    {
        PositiveParticleHaloPrefab = posParticleHaloPrefab;
        NegativeParticleHaloPrefab = negParticleHaloPrefab;
        ParticleEffectDuration = PositiveParticleHaloPrefab.GetComponent<ParticleSystem>().main.duration;
    }

    public void ShowParticleHalo(string feedbackType, GameObject gameObj)
    {
        GameObject particlePrefab = (feedbackType.ToLower() == "positive") ? PositiveParticleHaloPrefab : NegativeParticleHaloPrefab;

        GameObject rootObj = gameObj.transform.root.gameObject;
        InstantiatedParticleHaloGO = Instantiate(particlePrefab, rootObj.transform);

        // Position the haloPrefab behind the game object
        float distanceBehind = 1.5f; // Set the distance behind the gameObj
        Vector3 behindPos = rootObj.transform.position - rootObj.transform.forward * distanceBehind;
        InstantiatedParticleHaloGO.transform.position = behindPos;

        if (Session.SessionDef.EventCodesActive)
            Session.EventCodeManager.AddToFrameEventCodeBuffer(Session.EventCodeManager.SessionEventCodes["HaloFbController_SelectionVisualFbOn"]);
        
        Destroy(InstantiatedParticleHaloGO, ParticleEffectDuration);
    }

    public void ShowParticleHalo2D(string feedbackType, GameObject gameObj, float depth = 10)
    {
        GameObject particlePrefab = (feedbackType.ToLower() == "positive") ? PositiveParticleHaloPrefab : NegativeParticleHaloPrefab;
        GameObject rootObj = gameObj.transform.root.gameObject;

        InstantiatedParticleHaloGO = Instantiate(particlePrefab, null);
        Vector3 pos3d = rootObj.transform.position;
        Vector2 pos2d = Camera.main.WorldToScreenPoint(pos3d);
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(pos2d.x, pos2d.y, depth));
        InstantiatedParticleHaloGO.transform.position = worldPos;

        if (Session.SessionDef.EventCodesActive)
            Session.EventCodeManager.AddToFrameEventCodeBuffer(Session.EventCodeManager.SessionEventCodes["HaloFbController_SelectionVisualFbOn"]);
       
        Destroy(InstantiatedParticleHaloGO, ParticleEffectDuration * 2);

    }
    public float GetParticleEffectDuration() { return ParticleEffectDuration; }
}
