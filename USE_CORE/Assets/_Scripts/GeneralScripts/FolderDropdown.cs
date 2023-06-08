using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEditor;


public class FolderDropdown : MonoBehaviour
{
    public Dropdown dropdown;
    //[HideInInspector] public string folderPath = "DefaultSessionConfigs/";
    private int defaultIndex = 0;


    public void SetFolders(List<string> sessionConfigFolders)
    {
        if(sessionConfigFolders == null || sessionConfigFolders.Count < 1)
        {
            Debug.Log("TRYING TO SET FOLDERS BUT THE SESSION CONFIG LIST IS EMPTY!");
            return;
        }

        List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();

        for(int i = 0; i < sessionConfigFolders.Count; i++)
        {
            string folder = sessionConfigFolders[i];

            if (folder.ToLower().Contains("default"))
                defaultIndex = i;

            if (folder.Length > 3)
            {
                Dropdown.OptionData option = new Dropdown.OptionData(folder);
                options.Add(option);
            }
        }
        dropdown.AddOptions(options);
        dropdown.value = defaultIndex;
        dropdown.RefreshShownValue();
    }

}