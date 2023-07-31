using UnityEngine;
using UnityEngine.UI;


public class ToggleManager : MonoBehaviour
{
    [HideInInspector] public Toggle toggle;
    public GameObject toggle_GO;
    public string playerPrefKey;

    private void Start() //Load saved values
    {
        toggle = toggle_GO.GetComponent<Toggle>();
        toggle.isOn = PlayerPrefs.GetInt(playerPrefKey, 0) == 1;
    }

    public void SaveInputValue() //Save current value when user changes it
    {
        int toggleValue = toggle.isOn ? 1 : 0;
        PlayerPrefs.SetInt(playerPrefKey, toggleValue);
    }
}
