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
            return "http://serverName:port";
        else if (inputField_GO.name.ToLower().Contains("serverdata"))
            return "DATA";
        else
            return "DefaultValue";
    }

}
