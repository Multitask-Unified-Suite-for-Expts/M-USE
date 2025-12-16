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

    public void ShowParticleHalo(string feedbackType, GameObject go)
    {
        GameObject particlePrefab = (feedbackType.ToLower() == "positive") ? PositiveParticleHaloPrefab : NegativeParticleHaloPrefab;

        GameObject rootObj = go.transform.root.gameObject;
        InstantiatedParticleHaloGO = Instantiate(particlePrefab, rootObj.transform);

        if(Session.UsingDefaultConfigs)
        {
            InstantiatedParticleHaloGO.transform.localScale = new Vector3(.65f, .65f, .65f);
        }
        else
        {
            InstantiatedParticleHaloGO.transform.localScale = rootObj.transform.localScale;
        }

        float distanceBehind = -0.25f; ; // Set the distance behind the gameObj
        Vector3 behindPos = rootObj.transform.position - rootObj.transform.forward * distanceBehind;
        InstantiatedParticleHaloGO.transform.position = behindPos;

        if (Session.SessionDef.EventCodesActive)
            Session.EventCodeManager.SendCodeThisFrame(Session.EventCodeManager.SessionEventCodes["HaloFbController_SelectionVisualFbOn"]);
        
        Destroy(InstantiatedParticleHaloGO, ParticleEffectDuration);
    }

    public void ShowParticleHalo2D(string feedbackType, GameObject go, float depth = 10)
    {
        GameObject particlePrefab = (feedbackType.ToLower() == "positive") ? PositiveParticleHaloPrefab : NegativeParticleHaloPrefab;

        InstantiatedParticleHaloGO = Instantiate(particlePrefab, null);
        Vector2 pos2d = Camera.main.WorldToScreenPoint(go.transform.position);
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(pos2d.x, pos2d.y, depth));
        InstantiatedParticleHaloGO.transform.position = worldPos;
        InstantiatedParticleHaloGO.transform.localScale = InstantiatedParticleHaloGO.transform.localScale = go.transform.localScale * .5f;


        if (Session.SessionDef.EventCodesActive)
            Session.EventCodeManager.SendCodeThisFrame(Session.EventCodeManager.SessionEventCodes["HaloFbController_SelectionVisualFbOn"]);
       
        Destroy(InstantiatedParticleHaloGO, ParticleEffectDuration * 2);

    }
    public float GetParticleEffectDuration() { return ParticleEffectDuration; }
}
