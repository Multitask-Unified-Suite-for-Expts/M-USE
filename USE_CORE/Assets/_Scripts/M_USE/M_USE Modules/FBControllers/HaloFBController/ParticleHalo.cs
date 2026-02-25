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

        PositiveParticleHaloPrefab.gameObject.AddComponent<FaceCamera>();
        NegativeParticleHaloPrefab.gameObject.AddComponent<FaceCamera>();

        ParticleEffectDuration = PositiveParticleHaloPrefab.GetComponent<ParticleSystem>().main.duration;
    }

    public void ShowParticleHalo(string feedbackType, GameObject go, float? destroyDuration = null)
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

        //Make it face camera so the rotation is correct
        InstantiatedParticleHaloGO.AddComponent<FaceCamera>();

        if (Session.SessionDef.EventCodesActive)
            Session.EventCodeManager.SendCodeThisFrame(Session.EventCodeManager.SessionEventCodes["HaloFbController_SelectionVisualFbOn"]);
        
        Destroy(InstantiatedParticleHaloGO, destroyDuration == null ? ParticleEffectDuration : destroyDuration.Value);
    }

    public void ShowParticleHalo2D(string feedbackType, GameObject go, float depth = 10, float? destroyDuration = null)
    {
        GameObject particlePrefab = (feedbackType.ToLower() == "positive") ? PositiveParticleHaloPrefab : NegativeParticleHaloPrefab;

        InstantiatedParticleHaloGO = Instantiate(particlePrefab, null);
        Vector2 pos2d = Camera.main.WorldToScreenPoint(go.transform.position);
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(pos2d.x, pos2d.y, depth));
        InstantiatedParticleHaloGO.transform.position = worldPos;
        InstantiatedParticleHaloGO.transform.localScale = InstantiatedParticleHaloGO.transform.localScale = go.transform.localScale * .5f;


        if (Session.SessionDef.EventCodesActive)
            Session.EventCodeManager.SendCodeThisFrame(Session.EventCodeManager.SessionEventCodes["HaloFbController_SelectionVisualFbOn"]);
       
        Destroy(InstantiatedParticleHaloGO, destroyDuration == null ? ParticleEffectDuration : destroyDuration.Value);

    }
    public float GetParticleEffectDuration() { return ParticleEffectDuration; }
}
