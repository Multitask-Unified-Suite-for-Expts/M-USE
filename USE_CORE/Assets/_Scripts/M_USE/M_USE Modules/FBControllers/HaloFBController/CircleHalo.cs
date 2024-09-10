using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleHalo : MonoBehaviour
{
    // Each stimulus with a circle halo will contain its own instance 

    public GameObject PositiveCircleHaloPrefab; // Passed in from HaloFBController during Intialize
    public GameObject NegativeCircleHaloPrefab; // Passed in from HaloFBController during Intialize

    public GameObject InstantiatedCircleHaloGO;

    public void Initialize(GameObject posParticleHaloPrefab, GameObject negParticleHaloPrefab)
    {
        PositiveCircleHaloPrefab = posParticleHaloPrefab;
        NegativeCircleHaloPrefab = negParticleHaloPrefab;
    }

    public IEnumerator<WaitForSeconds> FlashHalo(HaloFBController haloFBController, float flashingDuration, int numFlashes, GameObject go)
    {
        // Calculate the time to stay on and off for each flash
        float onDuration = flashingDuration / (2 * numFlashes);
        haloFBController.SetIsFlashing(true);
        // Flash the halo for the specified number of times

        for (int i = 0; i < numFlashes; i++)
        {
            if(InstantiatedCircleHaloGO == null)
            {
                InstantiatedCircleHaloGO = Instantiate(PositiveCircleHaloPrefab, go.transform.root.transform);
                InstantiatedCircleHaloGO.transform.localScale = go.transform.localScale * .5f;
            }
            else    
                InstantiatedCircleHaloGO.SetActive(true);
            
            yield return new WaitForSeconds(onDuration);

            InstantiatedCircleHaloGO.SetActive(false);
            yield return new WaitForSeconds(onDuration);
        }
        haloFBController.SetIsFlashing(false);

    }

    public IEnumerator CreateCircleHalo(string feedbackType, GameObject go, bool use2D, float? particleEffectDuration, float? circleEffectDuration = null, float? depth = null)
    {
        if(particleEffectDuration != null)
            yield return new WaitForSeconds((float)particleEffectDuration * .5f);

        GameObject circleHaloPrefab = (feedbackType.ToLower() == "positive") ? PositiveCircleHaloPrefab : NegativeCircleHaloPrefab;

        if (InstantiatedCircleHaloGO == null)
        {
            InstantiatedCircleHaloGO = Instantiate(circleHaloPrefab, go.transform.root.transform);
            InstantiatedCircleHaloGO.transform.localScale = go.transform.localScale;

        }

        InstantiatedCircleHaloGO.SetActive(true);

        if (use2D)
        {
            Vector3 pos3d = go.transform.position;
            Vector2 pos2d = Camera.main.WorldToScreenPoint(pos3d);
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(pos2d.x, pos2d.y, depth.Value));
            InstantiatedCircleHaloGO.transform.position = worldPos;
        }
        else
        {
            InstantiatedCircleHaloGO.transform.SetParent(go.transform.root.transform);
        }

        if (circleEffectDuration != null)
            Destroy(InstantiatedCircleHaloGO, (float)circleEffectDuration);
    }
    public void DestroyInstantiatedCircleHalo()
    {
        Destroy(InstantiatedCircleHaloGO);
    }
    public void DeactivateInstantiatedCircleHalo()
    {
        InstantiatedCircleHaloGO.SetActive(false);
    }

    public GameObject? GetInstantiatedCircleHaloGO() { return  InstantiatedCircleHaloGO; }

}
