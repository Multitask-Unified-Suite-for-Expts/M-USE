using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleHalo : MonoBehaviour
{
    // Each stimulus with a circle halo will contain its own instance 

    public GameObject PositiveCircleHaloPrefab; // Set in the Inspector
    public GameObject NegativeCircleHaloPrefab; // Set in Inspector

    private GameObject InstantiatedCircleHaloGO;

    public void Initialize(GameObject posParticleHaloPrefab, GameObject negParticleHaloPrefab)
    {
        PositiveCircleHaloPrefab = posParticleHaloPrefab;
        NegativeCircleHaloPrefab = negParticleHaloPrefab;
    }

    public IEnumerator<WaitForSeconds> FlashHalo(float flashingDuration, int numFlashes, GameObject go)
    {
        // Calculate the time to stay on and off for each flash
        float onDuration = flashingDuration / (2 * numFlashes);
        // IsFlashing = true;
        // Flash the halo for the specified number of times
        for (int i = 0; i < numFlashes; i++)
        {
            InstantiatedCircleHaloGO.SetActive(true);
            yield return new WaitForSeconds(onDuration);

            InstantiatedCircleHaloGO.SetActive(false);
            yield return new WaitForSeconds(onDuration);
        }

        // IsFlashing = false;
    }

    // Call this method to start flashing the halo
    public void StartFlashingHalo(float flashingDuration, int numFlashes, GameObject go)
    {
        StartCoroutine(FlashHalo(flashingDuration, numFlashes, go));
    }

    public IEnumerator CreateCircleHalo(string feedbackType, GameObject gameObj, bool use2D, float particleEffectDuration, float? depth = null)
    {
        yield return new WaitForSeconds(particleEffectDuration * .75f);

        GameObject circleHaloPrefab = (feedbackType.ToLower() == "positive") ? PositiveCircleHaloPrefab : NegativeCircleHaloPrefab;

        InstantiatedCircleHaloGO = Instantiate(circleHaloPrefab, gameObj.transform.root.transform);

        if (use2D)
        {
            Vector3 pos3d = gameObj.transform.root.transform.position;
            Vector2 pos2d = Camera.main.WorldToScreenPoint(pos3d);
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(pos2d.x, pos2d.y, depth.Value));
            InstantiatedCircleHaloGO.transform.position = worldPos;
        }
        else
        {
            InstantiatedCircleHaloGO.transform.SetParent(gameObj.transform.root.transform);
        }
    }
    /*
            public void SetCircleHaloSize(float size)
            {
                Light light = PositiveHaloPrefab.GetComponent<Light>();
                light.range = size;
                light = NegativeHaloPrefab.GetComponent<Light>();
                light.range = size;
                return this;
            }*/
}
