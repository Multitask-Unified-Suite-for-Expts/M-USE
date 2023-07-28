using UnityEngine;
using TMPro;
using UnityEngine.UI;


public class InputFieldManager : MonoBehaviour
{
    [HideInInspector] public TMP_InputField inputField;
    public GameObject inputField_GO;
    public string playerPrefKey;

    private void Start() //Load saved values
    {
        inputField = inputField_GO.GetComponent<TMP_InputField>();
        inputField.text = PlayerPrefs.GetString(playerPrefKey, "");        
    }

    public void SaveInputValue() //Save current value when user changes it
    {
        PlayerPrefs.SetString(playerPrefKey, inputField.text);
    }
}
