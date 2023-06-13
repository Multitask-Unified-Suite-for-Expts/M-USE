using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEditor;


public class FolderDropdown : MonoBehaviour
{
    public Dropdown dropdown;


    public void SetFolders(List<string> sessionConfigFolders)
    {
        List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();

        foreach(string folder in sessionConfigFolders)
        {
            Dropdown.OptionData option = new Dropdown.OptionData(folder);
            options.Add(option);
        }
        dropdown.AddOptions(options);
    }

}