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
