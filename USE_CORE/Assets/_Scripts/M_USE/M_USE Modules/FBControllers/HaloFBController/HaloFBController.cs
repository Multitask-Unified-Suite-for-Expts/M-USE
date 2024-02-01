/*
MIT License

Copyright (c) 2023 Multitask - Unified - Suite -for-Expts

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files(the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/



using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using USE_Data;

public class HaloFBController : MonoBehaviour
{
    public GameObject PositiveHaloPrefab;
    public GameObject NegativeHaloPrefab;

    public GameObject PositiveParticlePrefab;
    public GameObject NegativeParticlePrefab;

    private GameObject InstantiatedParticleHalo;
    public GameObject InstantiatedCircleHalo;

    private bool LeaveFBOn = false;
    public bool IsFlashing;

    private enum State { None, Positive, Negative };
    private State state;

    public float ParticleEffectDuration;


    public void Init(DataController frameData)
    {
        frameData.AddDatum("HaloType", () => state.ToString());
        if (InstantiatedParticleHalo != null) {
            Debug.LogWarning("Initializing HaloFB Controller with an already visible halo");
            Destroy(InstantiatedParticleHalo);
        }
        InstantiatedParticleHalo = null;

        PositiveParticlePrefab = Resources.Load<GameObject>("Prefabs/ParticleHaloPositive");
        NegativeParticlePrefab = Resources.Load<GameObject>("Prefabs/ParticleHaloNegative");

        ParticleEffectDuration = PositiveParticlePrefab.GetComponent<ParticleSystem>().main.duration;

        PositiveHaloPrefab.name = "PositiveHalo";
        NegativeHaloPrefab.name = "NegativeHalo";
        PositiveParticlePrefab.name = "PositiveParticleHalo";
        NegativeParticlePrefab.name = "NegativeParticleHalo";


    }

    public bool IsHaloGameObjectNull()
    {
        return InstantiatedParticleHalo == null;
    }

    public void SetLeaveFeedbackOn()
    {
        LeaveFBOn = true;
    }
    public void ShowPositive(GameObject gameObj, float? depth = null)
    {
        state = State.Positive;
        if (depth == null)
            Show(PositiveParticlePrefab, PositiveHaloPrefab, gameObj);
        else
            Show2D(PositiveParticlePrefab, PositiveHaloPrefab, gameObj, depth.Value);

    }
    
    public void ShowNegative(GameObject gameObj, float? depth = null)
    {
        state = State.Negative;
        if(depth == null)
            Show(NegativeParticlePrefab, NegativeHaloPrefab, gameObj);
        else
            Show2D(NegativeParticlePrefab, NegativeHaloPrefab, gameObj, depth.Value);

    }
    private void Show(GameObject particlePrefab, GameObject haloPrefab, GameObject gameObj)
    {
        if (InstantiatedParticleHalo != null)
        {
            Debug.LogWarning("Trying to show PARTICLE HaloFB but one is already being shown");
            Destroy(InstantiatedParticleHalo);   
        }

        if (InstantiatedCircleHalo != null && !LeaveFBOn)
        {
            Debug.LogWarning("Trying to show CIRCLE HaloFB but one is already being shown");
            Destroy(InstantiatedCircleHalo);
        }

        GameObject rootObj = gameObj.transform.root.gameObject;
        InstantiatedParticleHalo = Instantiate(particlePrefab, rootObj.transform);
        InstantiatedParticleHalo.transform.SetParent(rootObj.transform);

        // Position the haloPrefab behind the game object
        float distanceBehind = 1.5f; // Set the distance behind the gameObj
        Vector3 behindPos = rootObj.transform.position - rootObj.transform.forward * distanceBehind;
        InstantiatedParticleHalo.transform.position = behindPos;

        //Create circle halo for when LeaveFeedbackOn is true:
        if (LeaveFBOn)
            StartCoroutine(CreateFollowUpHalo(haloPrefab, rootObj.transform, false));

        Destroy(InstantiatedParticleHalo, ParticleEffectDuration * 2); //Destroy the particle effect gameobject after its done doing its effect (did x2 for some wiggle room)


        if(Session.SessionDef.EventCodesActive)
            Session.EventCodeManager.AddToFrameEventCodeBuffer(Session.EventCodeManager.SessionEventCodes["HaloFbController_SelectionVisualFbOn"]);
    }

    private IEnumerator CreateFollowUpHalo(GameObject haloPrefab, Transform parent, bool use2D, float? depth = null)
    {
        yield return new WaitForSeconds(ParticleEffectDuration * .7f);

        InstantiatedCircleHalo = Instantiate(haloPrefab, parent);

        if(use2D)
        {
            Vector3 pos3d = parent.transform.position;
            Vector2 pos2d = Camera.main.WorldToScreenPoint(pos3d);
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(pos2d.x, pos2d.y, depth.Value));
            InstantiatedCircleHalo.transform.position = worldPos;
        }
        else
        {
            InstantiatedCircleHalo.transform.SetParent(parent);
        }
    }

    public void Show2D(GameObject particlePrefab, GameObject haloPrefab, GameObject gameObj, float depth = 10)
    {
        if (InstantiatedParticleHalo != null && !LeaveFBOn)
                Destroy(InstantiatedParticleHalo);

        GameObject rootObj = gameObj.transform.root.gameObject;

        InstantiatedParticleHalo = Instantiate(particlePrefab, null);
        Vector3 pos3d = gameObj.transform.position;
        Vector2 pos2d = Camera.main.WorldToScreenPoint(pos3d);
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(pos2d.x, pos2d.y, depth));
        InstantiatedParticleHalo.transform.position = worldPos;


        //CREATE SECOND HALO FOR WHEN LEAVE FEEDBACK ON
        if (LeaveFBOn)
            StartCoroutine(CreateFollowUpHalo(haloPrefab, rootObj.transform, false));


        if (Session.SessionDef.EventCodesActive)
            Session.EventCodeManager.AddToFrameEventCodeBuffer(Session.EventCodeManager.SessionEventCodes["HaloFbController_SelectionVisualFbOn"]);
    }

    public IEnumerator<WaitForSeconds> FlashHalo(float flashingDuration, int numFlashes, GameObject go)
    {
        if (go.GetComponentInChildren<Light>() != null)
        {
            InstantiatedCircleHalo = GetRootObject(go.transform).GetComponentInChildren<Light>().gameObject;
            InstantiatedCircleHalo.SetActive(false);
        }
        else
        {
            Debug.LogWarning("ELSE");
            //StartCoroutine(CreateFollowUpHalo(PositiveHaloPrefab, go.transform, false));
            //ShowPositive(go);
        }

        
        // Calculate the time to stay on and off for each flash
        float onDuration = flashingDuration / (2 * numFlashes);
        IsFlashing = true;
        // Flash the halo for the specified number of times
        for (int i = 0; i < numFlashes; i++)
        {
            InstantiatedCircleHalo.SetActive(true);
            yield return new WaitForSeconds(onDuration);

            InstantiatedCircleHalo.SetActive(false);
            yield return new WaitForSeconds(onDuration);
        }

        IsFlashing = false;
        DestroyHalos();
    }
    
    // Call this method to start flashing the halo
    public void StartFlashingHalo(float flashingDuration, int numFlashes, GameObject go)
    {
        StartCoroutine(FlashHalo(flashingDuration, numFlashes, go));
    }

    public IEnumerator DestroyCircleHaloCoroutine(float time)
    {
        yield return new WaitForSeconds(time);
        DestroyHalos();
    }

    public void DestroyHalos()
    {
        Destroy(InstantiatedCircleHalo);
        InstantiatedCircleHalo = null;
        Destroy(InstantiatedParticleHalo);
        InstantiatedParticleHalo = null;
        state = State.None;

        if (Session.SessionDef.EventCodesActive)
            Session.EventCodeManager.AddToFrameEventCodeBuffer(Session.EventCodeManager.SessionEventCodes["HaloFbController_SelectionVisualFbOff"]);
    }

    public void SetParticleHaloSize(float size)
    {
        PositiveParticlePrefab.transform.localScale = new Vector3(size, size, size);
        NegativeParticlePrefab.transform.localScale = new Vector3(size, size, size);
    }

    public HaloFBController SetCircleHaloSize(float size)
    {
        Light light = PositiveHaloPrefab.GetComponent<Light>();
        light.range = size;
        light = NegativeHaloPrefab.GetComponent<Light>();
        light.range = size;
        return this;
    }

    public HaloFBController SetPositiveHaloColor(Color color)
    {
        PositiveHaloPrefab.GetComponent<Light>().color = color;
        PositiveParticlePrefab.GetComponent<ParticleSystem>().startColor = color;
        return this;
    }

    public HaloFBController SetNegativeHaloColor(Color color)
    {
        NegativeHaloPrefab.GetComponent<Light>().color = color;
        NegativeParticlePrefab.GetComponent<ParticleSystem>().startColor = color;
        return this;
    }

    public HaloFBController SetCircleHaloIntensity(float intensity)
    {
        Light light = PositiveHaloPrefab.GetComponent<Light>();
        light.intensity = intensity;
        light = NegativeHaloPrefab.GetComponent<Light>();
        light.intensity = intensity;
        return this;
    }
    
    private GameObject GetRootObject(Transform childTransform)
    {
        Transform currentTransform = childTransform;

        // Traverse up the hierarchy until we find the root object.
        while (currentTransform.parent != null)
        {
            currentTransform = currentTransform.parent;
        }

        // The currentTransform now points to the root object's transform.
        return currentTransform.gameObject;
    }

}
