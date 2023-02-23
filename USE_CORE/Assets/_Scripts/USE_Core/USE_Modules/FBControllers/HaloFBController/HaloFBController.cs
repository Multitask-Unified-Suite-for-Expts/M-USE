using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using USE_Data;

public class HaloFBController : MonoBehaviour
{
    public GameObject PositiveHaloPrefab;
    public GameObject NegativeHaloPrefab;

    private GameObject instantiated;
    private bool LeaveFBOn = false;

    
    // Logging
    private enum State { None, Positive, Negative };
    private State state;

    public void Init(DataController frameData) {
        frameData.AddDatum("HaloType", () => state.ToString());
        if (instantiated != null) {
            Debug.LogWarning("Initializing HaloFB Controller with an already visible halo");
            Destroy(instantiated);
        }
        instantiated = null;
    }

    public void SetLeaveFeedbackOn()
    {
        LeaveFBOn = true;
    }
    public void ShowPositive(GameObject gameObj) {
        state = State.Positive;
        Show(PositiveHaloPrefab, gameObj);
    }
    
    public void ShowNegative(GameObject gameObj) {
        state = State.Negative;
        Show(NegativeHaloPrefab, gameObj);
    }

    private void Show(GameObject haloPrefab, GameObject gameObj) {
        if (instantiated != null) {
            if (!LeaveFBOn) {
                Debug.LogWarning("Trying to show HaloFB but one is already being shown");
                Destroy(instantiated);
            }

            return;
        }
        GameObject rootObj = gameObj.transform.root.gameObject;
        instantiated = Instantiate(haloPrefab, rootObj.transform);
        instantiated.transform.SetParent(rootObj.transform);

        // Position the haloPrefab behind the game object
        float distanceBehind = 0.3f; // Set the distance behind the gameObj
        Vector3 behindPos = rootObj.transform.position - rootObj.transform.forward * distanceBehind;
        instantiated.transform.position = behindPos;


    }
    

    public void Destroy() {
        Destroy(instantiated);
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

}