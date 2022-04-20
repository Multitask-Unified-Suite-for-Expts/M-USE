using UnityEngine;

public class HaloFBController : MonoBehaviour
{
    public GameObject PositiveHaloPrefab;
    public GameObject NegativeHaloPrefab;

    private GameObject instantiated;

    public void Init() {
        if (instantiated != null) {
            Debug.LogWarning("Initializing HaloFB Controller with an already visible halo");
            Destroy(instantiated);
        }
        instantiated = null;
    }

    public void ShowPositive(GameObject gameObj) {
        Show(PositiveHaloPrefab, gameObj);
    }
    
    public void ShowNegative(GameObject gameObj) {
        Show(NegativeHaloPrefab, gameObj);
    }

    private void Show(GameObject haloPrefab, GameObject gameObj) {
        if (instantiated != null) {
            Debug.LogWarning("Trying to show HaloFB but one is already being shown");
            Destroy(instantiated);
        }
        instantiated = Instantiate(haloPrefab, gameObj.transform);
    }

    public void Destroy() {
        Destroy(instantiated);
    }
}