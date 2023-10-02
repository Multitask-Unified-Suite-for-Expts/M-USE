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



using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;


public class KeyboardController : MonoBehaviour
{
    public GameObject Keyboard_GO;
    public GameObject GridItemPrefab;
    public GameObject GridParent_GO;

    private List<string> CharacterList;
    private List<GameObject> gridItems;
    private bool Caps;
    private EventSystem eventSystem;
    private TMP_InputField CurrentInputField;

    [HideInInspector] public bool UsingKeyboard;

    InitScreen_Level InitScreen_Level;


    private void Start()
    {
        InitScreen_Level = GameObject.Find("ControlLevels").GetComponent<InitScreen_Level>();
        eventSystem = EventSystem.current;
        if (Keyboard_GO == null)
        {
            Debug.LogError("KEYBOARD GAMEOBJECT IS NULL!");
            return;
        }
        SetCharacterList();
        GenerateGridItems();
    }

    private void Update()
    {
        if(UsingKeyboard)
        {
            GameObject selectedGO = eventSystem.currentSelectedGameObject;
            if(selectedGO != null)
            {
                TMP_InputField selectedInputField = selectedGO.GetComponent<TMP_InputField>();
                if (selectedInputField != null)
                {
                    if (!Keyboard_GO.activeInHierarchy)
                        Keyboard_GO.SetActive(true);

                    if(CurrentInputField == null || selectedInputField != CurrentInputField)
                    {
                        CurrentInputField = selectedInputField;
                        if(selectedInputField.gameObject.name == "SubjectID_InputField" || selectedInputField.gameObject.name == "SubjectAge_InputField")
                            Keyboard_GO.transform.localPosition = new Vector3(0, -300f, 0);
                        else
                            Keyboard_GO.transform.localPosition = new Vector3(0, 300f, 0);
                    }
                }
            }
        }
    }

    public void OnKeyboardGridButtonPressed()
    {
        if (CurrentInputField != null)
        {
            InitScreen_Level.PlayAudio(InitScreen_Level.ToggleChange_AudioClip);
            string selected = eventSystem.currentSelectedGameObject.GetComponentInChildren<TextMeshProUGUI>().text;
            if(selected != null)
                CurrentInputField.text += selected;
        }
    }

    public void OnNonGridButtonPress()
    {
        if(CurrentInputField == null)
        {
            Debug.LogError("CURRENT INPUT FIELD IS NULL!");
            return;
        }

        InitScreen_Level.PlayAudio(InitScreen_Level.ToggleChange_AudioClip);

        GameObject clickedGO = eventSystem.currentSelectedGameObject;

        if (clickedGO.name == "HideButton")
        {
            Keyboard_GO.SetActive(false);
        }
        else if (clickedGO.name == "BackButton")
        {
            InitScreen_Level.PlayAudio(InitScreen_Level.ToggleChange_AudioClip);
            string currentText = CurrentInputField.text;
            CurrentInputField.text = currentText.Substring(0, currentText.Length - 1);
        }
        else if (clickedGO.name == "ClearButton")
        {
            CurrentInputField.text = "";
        }
        else if (clickedGO.name == "CapsButton")
        {
            Caps = !Caps;
            clickedGO.GetComponentInChildren<TextMeshProUGUI>().text = Caps ? "CAPS" : "Caps";
            foreach (GameObject go in gridItems)
            {
                TextMeshProUGUI textComponent = go.transform.GetComponentInChildren<TextMeshProUGUI>();
                textComponent.text = Caps ? textComponent.text.ToUpper() : textComponent.text.ToLower();
            }
        }
        else if(clickedGO.name == "SpaceButton")
        {
            CurrentInputField.text += " ";
        }
    }

    private void SetCharacterList()
    {
        CharacterList = new List<string>()
        {
            "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "*", "=", "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", ":", ";", "A", "S", "D", "F", "G", "H", "J", "K", "L", "-", "_", "+", "Z", "X", "C", "V", "B", "N", "M", ",", ".", "|", "/", "!"
        };
    }

    public void GenerateGridItems()
    {
        gridItems = new List<GameObject>();

        for (int i = 0; i < CharacterList.Count; i++)
        {
            string current = CharacterList[i].ToString().ToLower();
            GameObject gridItem = Instantiate(GridItemPrefab);
            gridItem.name = "Item_" + current;
            gridItem.transform.SetParent(GridParent_GO.transform);
            gridItem.transform.localPosition = Vector3.zero;
            gridItem.transform.localScale = Vector3.one;
            gridItem.transform.GetComponentInChildren<TextMeshProUGUI>().text = current;
            gridItem.AddComponent<Button>().onClick.AddListener(OnKeyboardGridButtonPressed);
            gridItems.Add(gridItem);
        }
    }


}
