using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;


public class KeyboardController : MonoBehaviour
{
    public GameObject Keyboard_GO;
    public GameObject KeyboardCanvas_GO;
    public GameObject GridItemPrefab;
    public GameObject GridParent_GO;
    private List<string> CharacterList;
    private List<GameObject> gridItems;
    public bool UsingKeyboard;
    private bool Caps;
    private EventSystem eventSystem;
    private TMP_InputField CurrentInputField;



    private void Start()
    {
        eventSystem = EventSystem.current;

        if (Keyboard_GO != null)
        {
            SetCharacterList();
            GenerateGridItems();
        }
        else
            Debug.LogError("KEYBOARD IS NULL!");
    }

    private void Update()
    {
        if (UsingKeyboard)
        {
            GameObject selectedGO = eventSystem.currentSelectedGameObject;
            if(selectedGO != null)
            {
                TMP_InputField selectedInputField = selectedGO.GetComponent<TMP_InputField>();
                if (selectedInputField != null)
                {
                    if (CurrentInputField == null || CurrentInputField != selectedInputField)
                    {
                        CurrentInputField = selectedInputField;
                        Keyboard_GO.SetActive(true);
                        SetKeyboardPosition(selectedInputField.transform.position);
                    }
                }
                else
                {
                    CurrentInputField = null;
                    Keyboard_GO.SetActive(false);
                }
            }
        }
    }

    private void SetKeyboardPosition(Vector3 inputFieldPos)
    {
        if(inputFieldPos.y >= 600)
            Keyboard_GO.transform.localPosition = new Vector3(0, -275f, 0);
        else
            Keyboard_GO.transform.localPosition = new Vector3(0, 275f, 0);
    }

    public void OnKeyboardButtonPressed()
    {
        if (CurrentInputField == null)
        {
            Debug.LogError("NO CURRENT INPUT FIELD!");
            return;
        }

        string currentText = CurrentInputField.text;

        string selected = eventSystem.currentSelectedGameObject.GetComponentInChildren<TextMeshProUGUI>().text;
        if(selected != null)
        {
            if (selected.ToLower() == "back")
                CurrentInputField.text = currentText.Substring(0, currentText.Length - 1);
            else
                CurrentInputField.text += selected;
        }
        else
            Debug.Log("SELECTED GO DOES NOT HAVE A TEXT COMPONENT");
    }

    public void OnHideButtonPressed()
    {
        Keyboard_GO.SetActive(false);
    }

    private void SetCharacterList()
    {
        CharacterList = new List<string>()
        {
            "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "Back", "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", ":", "A", "S", "D", "F", "G", "H", "J", "K", "L", "-", "_", "Z", "X", "C", "V", "B", "N", "M", ".", "/"
        };
    }

    public void OnCapsButtonPressed()
    {
        Caps = !Caps;
        foreach(GameObject go in gridItems)
        {
            TextMeshProUGUI textComponent = go.transform.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent.text.ToLower() == "back")
                continue;
            textComponent.text = Caps ? textComponent.text.ToUpper() : textComponent.text.ToLower();
        }
    }

    public void GenerateGridItems()
    {
        gridItems = new List<GameObject>();

        for (int i = 0; i < CharacterList.Count; i++)
        {
            string current = CharacterList[i].ToString();
            if (current != "Back")
                current = current.ToLower();
            GameObject gridItem = Instantiate(GridItemPrefab);
            gridItem.name = "Item_" + current;
            gridItem.transform.SetParent(GridParent_GO.transform);
            gridItem.transform.GetComponentInChildren<TextMeshProUGUI>().text = current;
            if (CharacterList[i].ToLower() == "back")
                gridItem.GetComponent<Image>().color = new Color32(255, 136, 0, 183);

            gridItem.AddComponent<Button>().onClick.AddListener(OnKeyboardButtonPressed);

            gridItems.Add(gridItem);
            
        }
    }


}
