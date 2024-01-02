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

    private GameObject instantiated;

    private bool LeaveFBOn = false;
    public bool IsFlashing;

    private enum State { None, Positive, Negative };
    private State state;



    public void Init(DataController frameData)
    {
        frameData.AddDatum("HaloType", () => state.ToString());
        if (instantiated != null) {
            Debug.LogWarning("Initializing HaloFB Controller with an already visible halo");
            Destroy(instantiated);
        }
        instantiated = null;
    }

    public bool IsHaloGameObjectNull()
    {
        return instantiated == null;
    }

    public void SetLeaveFeedbackOn()
    {
        LeaveFBOn = true;
    }
    public void ShowPositive(GameObject gameObj, float? depth = null, float? destroyTime = null)
    {
        state = State.Positive;
        if (depth == null)
            Show(PositiveHaloPrefab, gameObj);
        else
            Show2D(PositiveHaloPrefab, gameObj, depth.Value);

        if (destroyTime != null)
            StartCoroutine(DestroyAfterTime(destroyTime.Value));
    }
    
    public void ShowNegative(GameObject gameObj, float? depth = null, float? destroyTime = null)
    {
        state = State.Negative;
        if(depth == null)
            Show(NegativeHaloPrefab, gameObj);
        else
            Show2D(NegativeHaloPrefab, gameObj, depth.Value);

        if(destroyTime != null)
            StartCoroutine(DestroyAfterTime(destroyTime.Value));
    }
    private void Show(GameObject haloPrefab, GameObject gameObj)
    {
        if (instantiated != null && !LeaveFBOn)
        {
            Debug.LogWarning("Trying to show HaloFB but one is already being shown");
            Destroy(instantiated);   
        }

        GameObject rootObj = gameObj.transform.root.gameObject;
        instantiated = Instantiate(haloPrefab, rootObj.transform);
        instantiated.transform.SetParent(rootObj.transform);

        // Position the haloPrefab behind the game object
        float distanceBehind = 1.5f; // Set the distance behind the gameObj
        Vector3 behindPos = rootObj.transform.position - rootObj.transform.forward * distanceBehind;
        instantiated.transform.position = behindPos;

        if(Session.SessionDef.EventCodesActive)
            Session.EventCodeManager.AddToFrameEventCodeBuffer(Session.EventCodeManager.SessionEventCodes["HaloFbController_SelectionVisualFbOn"]);
    }

    public void Show2D(GameObject haloPrefab, GameObject gameObj, float depth = 10)
    {
        if (instantiated != null)
        {
            if (!LeaveFBOn)
            {
                Destroy(instantiated);
            }
        }
        GameObject rootObj = gameObj.transform.root.gameObject;
        instantiated = Instantiate(haloPrefab, null);
        if (Session.SessionDef.EventCodesActive)
            Session.EventCodeManager.AddToFrameEventCodeBuffer(Session.EventCodeManager.SessionEventCodes["HaloFbController_SelectionVisualFbOn"]);
        Vector3 pos3d = gameObj.transform.position;
        Vector2 pos2d = Camera.main.WorldToScreenPoint(pos3d);
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(pos2d.x, pos2d.y, depth));
        instantiated.transform.position = worldPos;
    }

    public IEnumerator<WaitForSeconds> FlashHalo(float flashingDuration, int numFlashes, GameObject go)
    {
        if (go.GetComponentInChildren<Light>() != null)
        {
            instantiated = GetRootObject(go.transform).GetComponentInChildren<Light>().gameObject;
            instantiated.SetActive(false);
        }
        else
            ShowPositive(go);

        
        // Calculate the time to stay on and off for each flash
        float onDuration = flashingDuration / (2 * numFlashes);
        IsFlashing = true;
        // Flash the halo for the specified number of times
        for (int i = 0; i < numFlashes; i++)
        {
            instantiated.SetActive(true);
            yield return new WaitForSeconds(onDuration);
            
            instantiated.SetActive(false);
            yield return new WaitForSeconds(onDuration);
        }

        IsFlashing = false;
        Destroy();
    }
    
    // Call this method to start flashing the halo
    public void StartFlashingHalo(float flashingDuration, int numFlashes, GameObject go)
    {
        StartCoroutine(FlashHalo(flashingDuration, numFlashes, go));
    }

    public IEnumerator DestroyAfterTime(float time)
    {
        yield return new WaitForSeconds(time);
        Destroy();
    }

    public void Destroy()
    {
        Destroy(instantiated);
        if (Session.SessionDef.EventCodesActive)
            Session.EventCodeManager.AddToFrameEventCodeBuffer(Session.EventCodeManager.SessionEventCodes["HaloFbController_SelectionVisualFbOff"]);
        instantiated = null;
        state = State.None;
    }

    public HaloFBController SetHaloSize(float size)
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
        return this;
    }

    public HaloFBController SetNegativeHaloColor(Color color)
    {
        NegativeHaloPrefab.GetComponent<Light>().color = color;
        return this;
    }

    public HaloFBController SetHaloIntensity(float intensity)
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
