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
    public GameObject PositiveCircleHaloPrefab; // Set in Inspector
    public GameObject NegativeCircleHaloPrefab; // Set in Inspector

    public GameObject PositiveParticleHaloPrefab; // Set in Inspector
    public GameObject NegativeParticleHaloPrefab; // Set in Inspector

    private List<CircleHalo> PositiveCircleHalos = new List<CircleHalo>();
    private List<CircleHalo> NegativeCircleHalos = new List<CircleHalo>();

    private bool LeaveFBOn;
    private bool IsFlashing;

    private enum State { None, Positive, Negative };
    private State state;

    public void Init(DataController frameData)
    {
        frameData.AddDatum("HaloType", () => state.ToString());
    }

    public void ShowPositive(GameObject gameObj, bool particleHaloActive = true, bool circleHaloActive = false, float? destroyTime = null, float? depth = null)
    {
        state = State.Positive;

        ParticleHalo particleHalo = null;
        CircleHalo circleHalo = null;

        if (particleHaloActive)
        {
            particleHalo = GetOrCreateParticleHalo(gameObj);
            if (depth == null)
                particleHalo.ShowParticleHalo("positive", gameObj);
            else
                particleHalo.ShowParticleHalo2D("positive", gameObj, depth.Value);
        }


        if (circleHaloActive) // If the feedback is being left on after a selection, create the halo light effect
        {
            // See if the selected game object has a CircleHalo Component, add one if not
            circleHalo = GetOrCreateCircleHalo(gameObj);

            PositiveCircleHalos.Add(circleHalo);
            if (depth == null)
                StartCoroutine(circleHalo.CreateCircleHalo("positive", gameObj, false, particleHalo?.GetParticleEffectDuration(), destroyTime, null));
            else
                StartCoroutine(circleHalo.CreateCircleHalo("positive", gameObj, true, particleHalo?.GetParticleEffectDuration(), destroyTime, depth.Value));

        }
        if (Session.SessionDef.EventCodesActive)
            Session.EventCodeManager.AddToFrameEventCodeBuffer(Session.EventCodeManager.SessionEventCodes["HaloFbController_SelectionVisualFbOn"]);

    }
    public void ShowNegative(GameObject gameObj,  bool particleHaloActive = false, bool circleHaloActive = true, float? destroyTime = null, float? depth = null)
    {
        state = State.Negative;

        ParticleHalo particleHalo = null;
        CircleHalo circleHalo = null;

        if (particleHaloActive)
        {
            particleHalo = GetOrCreateParticleHalo(gameObj);

            if (depth == null)
                particleHalo.ShowParticleHalo("negative", gameObj);
            else
                particleHalo.ShowParticleHalo2D("negative", gameObj, depth.Value);

        }

        if (circleHaloActive) // If the feedback is being left on after a selection, create the halo light effect
        {
            // See if the selected game object has a CircleHalo Component, add one if not
            circleHalo = GetOrCreateCircleHalo(gameObj);

            NegativeCircleHalos.Add(circleHalo);

            if (depth == null)
                StartCoroutine(circleHalo.CreateCircleHalo("negative", gameObj, false, particleHalo?.GetParticleEffectDuration(), destroyTime, null));
            else
                StartCoroutine(circleHalo.CreateCircleHalo("negative", gameObj, true, particleHalo?.GetParticleEffectDuration(), destroyTime, depth.Value));

        }

        if (Session.SessionDef.EventCodesActive)
            Session.EventCodeManager.AddToFrameEventCodeBuffer(Session.EventCodeManager.SessionEventCodes["HaloFbController_SelectionVisualFbOn"]);
    }
    private ParticleHalo GetOrCreateParticleHalo(GameObject gameObj)
    {
        ParticleHalo particleHalo = gameObj.GetComponent<ParticleHalo>();
        if (particleHalo == null)
        {
            particleHalo = gameObj.AddComponent<ParticleHalo>();
            particleHalo.Initialize(PositiveParticleHaloPrefab, NegativeParticleHaloPrefab);
        }
        return particleHalo;
    }

    private CircleHalo GetOrCreateCircleHalo(GameObject gameObj)
    {
        // Check if the current game object has the CircleHalo component
        CircleHalo circleHalo = gameObj.GetComponent<CircleHalo>();

        // If not found, check the children recursively
        if (circleHalo == null)
        {
            circleHalo = SearchInChildrenForCircleHalo(gameObj.transform);
        }
        
        // If CircleHalo still not found, create and initialize a new one
        if (circleHalo == null)
        {
            circleHalo = gameObj.AddComponent<CircleHalo>();
            circleHalo.Initialize(PositiveCircleHaloPrefab, NegativeCircleHaloPrefab);
        }

        return circleHalo;
    }

    // Recursive method to search for CircleHalo in children
    private CircleHalo SearchInChildrenForCircleHalo(Transform parent)
    {
        // Iterate through each child of the parent transform
        foreach (Transform child in parent)
        {
            // Check if the child has the CircleHalo component
            CircleHalo circleHalo = child.GetComponent<CircleHalo>();

            // If found, return it
            if (circleHalo != null)
            {
                return circleHalo;
            }

            // If not found, recursively search in the child's children
            CircleHalo foundInChild = SearchInChildrenForCircleHalo(child);

            // If CircleHalo found in any child, return it
            if (foundInChild != null)
            {
                return foundInChild;
            }
        }

        return null; // Returns null if CircleHalo not found in any child
    }


    public void SetCircleHaloSize(float size)
    {
        PositiveCircleHaloPrefab.transform.localScale = new Vector3(size, size, size);
        NegativeCircleHaloPrefab.transform.localScale = new Vector3(size, size, size);
    }

    public void SetCircleHaloIntensity(float newIntensity)
    {
        PositiveCircleHaloPrefab.GetComponent<Light>().intensity = newIntensity;
        NegativeCircleHaloPrefab.GetComponent<Light>().intensity = newIntensity;
    }    
    public void SetCircleHaloRange(float newRange)
    {
        PositiveCircleHaloPrefab.GetComponent<Light>().range = newRange;
        NegativeCircleHaloPrefab.GetComponent<Light>().range = newRange;
    }

    public void SetPositiveCircleHaloColor(Color color)
    {
        PositiveCircleHaloPrefab.GetComponent<Light>().color = color;
    }

    public void SetNegativeCircleHaloColor(Color color)
    {
        NegativeCircleHaloPrefab.GetComponent<Light>().color = color;
    }
    public void SetParticleHaloSize(float size)
    {
        PositiveParticleHaloPrefab.transform.localScale = new Vector3(size, size, size);
        NegativeParticleHaloPrefab.transform.localScale = new Vector3(size, size, size);
    }
    public void SetPositiveParticleHaloColor(Color color)
    {
        var mainModule = PositiveParticleHaloPrefab.GetComponent<ParticleSystem>().main;
        mainModule.startColor = color;
    }

    public void SetNegativeParticleHaloColor(Color color)
    {
        var mainModule = NegativeParticleHaloPrefab.GetComponent<ParticleSystem>().main;
        mainModule.startColor = color;
    }

    public void SetPositiveHaloColor(Color color)
    {
        SetPositiveParticleHaloColor(color);
        SetPositiveCircleHaloColor(color);
    }
    public void SetNegativeHaloColor(Color color)
    {
        SetNegativeParticleHaloColor(color);
        SetNegativeCircleHaloColor(color);
    }

    // Call this method to start flashing the halo
    public void StartFlashingHalo(float flashingDuration, int numFlashes, GameObject go)
    {
        CircleHalo circleHalo = GetOrCreateCircleHalo(go);
        StartCoroutine(circleHalo.FlashHalo(this, flashingDuration, numFlashes, go));
    }

    public void SetLeaveFeedbackOn(bool leaveFbOn)
    {
        LeaveFBOn = leaveFbOn;
    }
    public bool IsHaloFlashing()
    {
        return IsFlashing;
    }
    // Method to destroy all CircleHalos in the lists
    private void DestroyAllCircleHalos()
    {
        foreach (var circleHalo in PositiveCircleHalos)
        {
            Destroy(circleHalo);
        }
        PositiveCircleHalos.Clear();

        foreach (var circleHalo in NegativeCircleHalos)
        {
            Destroy(circleHalo);
        }
        NegativeCircleHalos.Clear();
    }
    public void DestroyNegativeCircleHalos()
    {
        foreach (var circleHalo in NegativeCircleHalos)
        {
            circleHalo.DestroyInstantiatedCircleHalo();
            Destroy(circleHalo);
        }
        NegativeCircleHalos.Clear();
    } 
    public void DestroySpecificHalo(GameObject go)
    {
        // Check if the current game object has the CircleHalo component
        CircleHalo circleHalo = go.GetComponent<CircleHalo>();

        // If not found, check the children recursively
        if (circleHalo == null)
        {
            circleHalo = SearchInChildrenForCircleHalo(go.transform);
        }

        circleHalo.DestroyInstantiatedCircleHalo();
        Destroy(circleHalo);
    }
    // Method to destroy all halos
    public void DestroyAllHalos()
    {
       // DestroyAllParticleHalos();
        DestroyAllCircleHalos();
        state = State.None;
        if (Session.SessionDef.EventCodesActive)
            Session.EventCodeManager.AddToFrameEventCodeBuffer(Session.EventCodeManager.SessionEventCodes["HaloFbController_SelectionVisualFbOff"]);

    }
    public List<CircleHalo> GetNegativeCircleHalos() { return NegativeCircleHalos;   }

    /*private void Show(GameObject particlePrefab, GameObject haloPrefab, GameObject gameObj)
    {
        if (InstantiatedParticleHalo != null && !LeaveFBOn)
        {
            Debug.LogWarning("Trying to show HaloFB but one is already being shown");
            Destroy(InstantiatedParticleHalo);
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


        if (Session.SessionDef.EventCodesActive)
            Session.EventCodeManager.AddToFrameEventCodeBuffer(Session.EventCodeManager.SessionEventCodes["HaloFbController_SelectionVisualFbOn"]);


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

    
*/
    /*

    public void DestroyHaloFeedback()
    {
        Destroy(InstantiatedCircleHalo);
        InstantiatedCircleHalo = null;
        Destroy(InstantiatedParticleHalo);
        InstantiatedParticleHalo = null;
        state = State.None;

        if (Session.SessionDef.EventCodesActive)
            Session.EventCodeManager.AddToFrameEventCodeBuffer(Session.EventCodeManager.SessionEventCodes["HaloFbController_SelectionVisualFbOff"]);
    }
*/
    public bool IsHaloGameObjectNull()
    {
        Debug.LogWarning("THIS IS NOT IMPLEMENTED");
        return false;
    }

    public void SetIsFlashing(bool flashingStatus)
    {
        IsFlashing = flashingStatus;
    }
    public bool GetIsFlashing()
    {
        return IsFlashing;
    }
}
