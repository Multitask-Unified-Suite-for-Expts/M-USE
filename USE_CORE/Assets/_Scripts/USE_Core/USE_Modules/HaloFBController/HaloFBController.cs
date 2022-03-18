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

    public void ShowPositive(Transform pos) {
        Show(PositiveHaloPrefab, pos);
    }
    
    public void ShowNegative(Transform pos) {
        Show(NegativeHaloPrefab, pos);
    }

    private void Show(GameObject haloPrefab, Transform pos) {
        if (instantiated != null) {
            Debug.LogWarning("Trying to show HaloFB but one is already being shown");
            Destroy(instantiated);
        }
        instantiated = Instantiate(haloPrefab, pos);
    }

    public void Destroy() {
        Destroy(instantiated);
    }
}