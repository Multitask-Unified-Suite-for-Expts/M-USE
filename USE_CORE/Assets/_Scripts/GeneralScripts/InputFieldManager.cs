using UnityEngine;
using TMPro;


public class InputFieldManager : MonoBehaviour
{
    [HideInInspector] public TMP_InputField inputField;
    public GameObject inputField_GO;
    public string playerPrefKey;

    private void Start() //Load saved values
    {
        inputField = inputField_GO.GetComponent<TMP_InputField>();
        string storedValue = PlayerPrefs.GetString(playerPrefKey, "");

        //this is just to get ipad off and running cuz got stuck on "defaultValue" and couldnt change it. Can delete this later
        if (inputField_GO.name.ToLower().Contains("serverurl") && storedValue.ToLower().Contains("default"))
            storedValue = "http://m-use.psy.vanderbilt.edu:8080";

        if (string.IsNullOrEmpty(storedValue))
            storedValue = GetDefaultValue();

        inputField.text = storedValue;      
    }

    public void SaveInputValue() //Save current value when user changes it
    {
        PlayerPrefs.SetString(playerPrefKey, inputField.text);
    }

    private string GetDefaultValue()
    {
        if (inputField_GO.name.ToLower().Contains("subjectid"))
            return "Player";
        else if (inputField_GO.name.ToLower().Contains("subjectage"))
            return "50";
        else if (inputField_GO.name.ToLower().Contains("serverurl"))
            return "http://m-use.psy.vanderbilt.edu:8080";
        else if (inputField_GO.name.ToLower().Contains("serverdata"))
            return "DATA";
        else
            return "DefaultValue";
        
    }

}
