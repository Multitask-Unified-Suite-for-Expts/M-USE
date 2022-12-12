using UnityEditor;
using UnityEngine;
using USE_Data;

public class HaloFBController : MonoBehaviour
{
    public GameObject PositiveHaloPrefab;
    public GameObject NegativeHaloPrefab;

    private GameObject instantiated;
    
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
            Debug.LogWarning("Trying to show HaloFB but one is already being shown");
            Destroy(instantiated);
        }
        instantiated = Instantiate(haloPrefab, gameObj.transform);
    }

    public void SetHaloParameter(string paramName, float floatArg){
        switch(paramName.ToLower()) 
        {
            case "size":
               // this.Find("YellowHalo").GetComponent<Halo>()
                break;
            default:
                Debug.LogError("No appropriate parameter name given");
                break;
        }
    }
    public void SetHaloParameter(string paramName, Color colorArg){
        switch(paramName.ToLower()) 
        {
            case "color":
                //SerializedObject haloComponent = new SerializedObject(this.gameObject.GetComponent("Halo"));
                //haloComponent.FindProperty("m_Color").colorValue = colorArg;
                break;
            default:
                Debug.LogError("No appropriate parameter name given");
                break;
        }
    }
    /*
    public void SetHaloSize(float size)
    {
        SerializedObject haloComponent = new SerializedObject(this.gameObject.GetComponent("Halo"));
        haloComponent.FindProperty("m_Size").floatValue = size;
    }
    
    public void SetHaloColor(Color color)
    {
        SerializedObject haloComponent = new SerializedObject(this.gameObject.GetComponent("Halo"));
        haloComponent.FindProperty("m_Color").colorValue = color;
    }
    */
    public void Destroy() {
        Destroy(instantiated);
        instantiated = null;
        state = State.None;
    }
}