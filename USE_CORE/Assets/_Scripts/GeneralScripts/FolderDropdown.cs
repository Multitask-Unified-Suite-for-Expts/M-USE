using UnityEngine;
using System.Collections.Generic;
using TMPro;


public class FolderDropdown : MonoBehaviour
{
    public TMP_Dropdown dropdown;

    private void Start()
    {
        dropdown = GetComponent<TMP_Dropdown>();
        if (dropdown == null)
            Debug.LogError("The FolderDropdown's dropdown component is null!");
    }

    public void SetFolders(List<string> sessionConfigFolders)
    {
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();

        foreach(string folder in sessionConfigFolders)
        {
            TMP_Dropdown.OptionData option = new TMP_Dropdown.OptionData(folder);
            options.Add(option);
        }
        dropdown.ClearOptions();
        dropdown.AddOptions(options);
    }

}