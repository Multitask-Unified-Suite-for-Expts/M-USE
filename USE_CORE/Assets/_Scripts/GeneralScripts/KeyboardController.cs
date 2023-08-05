using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class KeyboardController : MonoBehaviour
{
    public GameObject Keyboard_GO;
    //public GameObject HideButton_GO;
    public GameObject KeyboardCanvas_GO;
    public GameObject GridItemPrefab;
    public GameObject GridParent_GO;


    private List<string> CharacterList;

    private bool UsingKeyboard;



    private void Start()
    {
        if (Keyboard_GO != null)
        {
            SetCharacterList();
            GenerateGridItems();
        }
        else
            Debug.LogError("KEYBOARD IS NULL!");

    }

    private void SetCharacterList()
    {
        CharacterList = new List<string>()
        {
            "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "Back", "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", ":", "A", "S", "D", "F", "G", "H", "J", "K", "L", "-", "_", "Z", "X", "C", "V", "B", "N", "M", ".", "/"
        };
    }

    public void GenerateGridItems()
    {
        for (int i = 0; i < CharacterList.Count; i++)
        {
            string current = CharacterList[i].ToString();
            GameObject gridItem = Instantiate(GridItemPrefab);
            gridItem.name = "Item_" + current;
            gridItem.transform.SetParent(GridParent_GO.transform);
            gridItem.transform.GetComponentInChildren<TextMeshProUGUI>().text = current;
            if (CharacterList[i].ToLower() == "back")
                gridItem.GetComponent<Image>().color = new Color32(255, 136, 0, 183);
            
        }
    }

    public void OnCharacterSelected()
    {

    }


    public void TurnKeyboardOn()
    {
        if (UsingKeyboard)
            Keyboard_GO.SetActive(true);
    }


}
