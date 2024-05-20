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

    private bool Caps;
    private EventSystem eventSystem;
    private TMP_InputField CurrentInputField;

    [HideInInspector] public bool UsingKeyboard;

    InitScreen_Level InitScreen_Level;

    private KeyboardButtons keyboardButtons;
   

    private void Start()
    {
        InitScreen_Level = GameObject.Find("ControlLevels").GetComponent<InitScreen_Level>();
        eventSystem = EventSystem.current;
        if (Keyboard_GO == null)
            Debug.LogError("KEYBOARD GAMEOBJECT IS NULL!");
        keyboardButtons = new KeyboardButtons(this);
    }

    private void Update()
    {
        if(UsingKeyboard)
        {
            GameObject selectedGO = eventSystem.currentSelectedGameObject;
            if (selectedGO != null)
            {
                if (selectedGO.TryGetComponent<TMP_InputField>(out var selectedInputField))
                {
                    if (!Keyboard_GO.activeInHierarchy)
                        Keyboard_GO.SetActive(true);

                    if (CurrentInputField == null || selectedInputField != CurrentInputField)
                    {
                        CurrentInputField = selectedInputField;
                        if (selectedInputField.gameObject.name == "SubjectID_InputField" || selectedInputField.gameObject.name == "SubjectAge_InputField")
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
            Session.SessionAudioController.PlayAudioClip("ClickedButton");
            keyboardButtons.HandleGridButtonPress(eventSystem.currentSelectedGameObject);
        }
    }

    public void OnNonGridButtonPress()
    {
        if(CurrentInputField != null)
        {
            Session.SessionAudioController.PlayAudioClip("ClickedButton");
            keyboardButtons.HandleNonGridButtonPress(eventSystem.currentSelectedGameObject);
        }
    }

    public class KeyboardButtons
    {
        public List<NonGridKeyboardButton> nonGridButtonList;
        public List<GridKeyboardButton> gridButtonList;
        public KeyboardController keyboardController;
        public delegate void ActionDelegate();


        public KeyboardButtons(KeyboardController kbController)
        {
            keyboardController = kbController;
            nonGridButtonList = new List<NonGridKeyboardButton>();
            gridButtonList = new List<GridKeyboardButton>();
            CreateNonGridButtons();
            CreateGridButtons();
        }


        public void CreateNonGridButtons()
        {
            NonGridKeyboardButton hideButton = new NonGridKeyboardButton
            {
                buttonName = "HideButton",
                buttonAction = () => keyboardController.Keyboard_GO.SetActive(false)
            };
            nonGridButtonList.Add(hideButton);

            NonGridKeyboardButton backButton = new NonGridKeyboardButton
            {
                buttonName = "BackButton",
                buttonAction = () =>
                {
                    Session.SessionAudioController.PlayAudioClip("ClickedButton");
                    string currentText = keyboardController.CurrentInputField.text;
                    keyboardController.CurrentInputField.text = currentText.Substring(0, currentText.Length - 1);
                }
            };
            nonGridButtonList.Add(backButton);

            NonGridKeyboardButton clearButton = new NonGridKeyboardButton
            {
                buttonName = "ClearButton",
                buttonAction = () => keyboardController.CurrentInputField.text = ""
            };
            nonGridButtonList.Add(clearButton);

            NonGridKeyboardButton capsButton = new NonGridKeyboardButton
            {
                buttonName = "CapsButton",
                buttonAction = () =>
                {
                    GameObject clickedGO = keyboardController.eventSystem.currentSelectedGameObject;
                    keyboardController.Caps = !keyboardController.Caps;
                    clickedGO.GetComponentInChildren<TextMeshProUGUI>().text = keyboardController.Caps ? "CAPS" : "Caps";
                    foreach (GridKeyboardButton button in gridButtonList)
                    {
                        TextMeshProUGUI textComponent = button.buttonGO.transform.GetComponentInChildren<TextMeshProUGUI>();
                        textComponent.text = keyboardController.Caps ? textComponent.text.ToUpper() : textComponent.text.ToLower();
                    }
                }
            };
            nonGridButtonList.Add(capsButton);

            NonGridKeyboardButton spaceButton = new NonGridKeyboardButton
            {
                buttonName = "SpaceButton",
                buttonAction = () => keyboardController.CurrentInputField.text += " "
            };
            nonGridButtonList.Add(spaceButton);

        }

        public void CreateGridButtons()
        {
            List<string> characterList = new List<string>()
            {
                "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "*", "=", "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", ":", ";", "A", "S", "D", "F", "G", "H", "J", "K", "L", "-", "_", "+", "Z", "X", "C", "V", "B", "N", "M", ",", ".", "|", "/", "!"
            };

            for (int i = 0; i < characterList.Count; i++)
            {
                string currentCharacter = characterList[i].ToLower();

                GameObject gridItem = Instantiate(keyboardController.GridItemPrefab);
                gridItem.name = "Item_" + currentCharacter;
                gridItem.transform.SetParent(keyboardController.GridParent_GO.transform);
                gridItem.transform.localPosition = Vector3.zero;
                gridItem.transform.localScale = Vector3.one;
                gridItem.transform.GetComponentInChildren<TextMeshProUGUI>().text = currentCharacter;
                gridItem.AddComponent<Button>().onClick.AddListener(keyboardController.OnKeyboardGridButtonPressed);

                GridKeyboardButton button = new GridKeyboardButton
                {
                    buttonCharacter = currentCharacter,
                    buttonAction = () =>
                    {
                        string selected = keyboardController.eventSystem.currentSelectedGameObject.GetComponentInChildren<TextMeshProUGUI>().text;
                        if (selected != null)
                            keyboardController.CurrentInputField.text += selected;
                    },
                    buttonGO = gridItem
                };
                gridButtonList.Add(button);
            }
        }

        public void HandleGridButtonPress(GameObject buttonPressed)
        {
            foreach(GridKeyboardButton button in gridButtonList)
            {
                if (button.buttonGO == buttonPressed)
                    button.buttonAction();
            }
        }

        public void HandleNonGridButtonPress(GameObject buttonPressed)
        {
            foreach (NonGridKeyboardButton button in nonGridButtonList)
            {
                if (button.buttonName == buttonPressed.name)
                {
                    button.buttonAction();
                    break;
                }
            }
        }

        public class NonGridKeyboardButton
        {
            public string buttonName;
            public ActionDelegate buttonAction;
        }

        public class GridKeyboardButton
        {
            public string buttonCharacter;
            public ActionDelegate buttonAction;
            public GameObject buttonGO;
        }


    }



}
