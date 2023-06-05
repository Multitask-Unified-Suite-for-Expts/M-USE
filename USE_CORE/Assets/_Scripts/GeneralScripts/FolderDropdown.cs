using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEditor;

public class FolderDropdown : MonoBehaviour
{
    public Dropdown dropdown;
    [HideInInspector] public string folderPath = "DefaultSessionConfigs";
    private int defaultIndex = 0;

    private void Start()
    {
        LoadFolders();
    }

    private void LoadFolders()
    {
        HashSet<string> uniqueFolders = new HashSet<string>();
        Object[] folderObjects = Resources.LoadAll(folderPath, typeof(Object));
        List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();

        for(int i = 0; i < folderObjects.Length; i++)
        {
            string assetPath = AssetDatabase.GetAssetPath(folderObjects[i]);
            string folderName = assetPath.Split('/')[4];

            if (folderName.ToLower().Contains("default"))
                defaultIndex = i;

            if (!uniqueFolders.Contains(folderName) && !string.IsNullOrEmpty(folderName))
            {
                uniqueFolders.Add(folderName);
                Dropdown.OptionData option = new Dropdown.OptionData(folderName);
                options.Add(option);
            }
        }
        dropdown.AddOptions(options);
        dropdown.value = defaultIndex;
        dropdown.RefreshShownValue();
    }
}