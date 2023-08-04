using UnityEngine;
using TMPro;
using UnityEngine.UI;


public class KeyboardManager : MonoBehaviour
{
    public TMP_InputField inputField;
    
    public void OpenKeyboard()
    {
        Debug.Log("OPENING KEYBOARD!");

        TouchScreenKeyboard keyboard = TouchScreenKeyboard.Open(inputField.text, TouchScreenKeyboardType.Default, false, false, false, false);
        keyboard.text = inputField.text;

        if (TouchScreenKeyboard.isSupported)
            Debug.Log("IS SUPPORTED!");
        else
            Debug.Log("NOT SUPPORTED!!!!!!!!");
    }
}