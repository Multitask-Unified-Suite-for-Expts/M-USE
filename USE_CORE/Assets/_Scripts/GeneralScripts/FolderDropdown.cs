using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEditor;


public class FolderDropdown : MonoBehaviour
{
    public Dropdown dropdown;
    [HideInInspector] public string folderPath = "DefaultSessionConfigs/";
    private int defaultIndex = 0;


    private void Start()
    {
        #if(UNITY_WEBGL)
            //SetFolders();
        #endif
        //LoadFolders();
    }

    private void SetFolders()
    {
        List<string> folders = new List<string>() //Can't use AssetDatabase so gotta do it manually for now
        {
            "SessionConfig_021_VS_FL_VS_v01_Set01",
            "SessionConfigs_Default"
        };

        List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
        foreach (string folderName in folders)
        {
            Dropdown.OptionData option = new Dropdown.OptionData(folderName);
            options.Add(option);
        }
        dropdown.AddOptions(options);
    }

    //private void LoadFolders() //Wont work for WebGL build cuz AssetDatabase.GetAssetPath
    //{
    //    HashSet<string> uniqueFolders = new HashSet<string>();
    //    Object[] folderObjects = Resources.LoadAll(folderPath, typeof(Object));
    //    List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();

    //    for (int i = 0; i < folderObjects.Length; i++)
    //    {
    //        string assetPath = AssetDatabase.GetAssetPath(folderObjects[i]); //ASSETDATABASE! PATH DOESNT WORK FOR WEB BUILDS!!
    //        string folderName = assetPath.Split('/')[4];

    //        if (folderName.ToLower().Contains("default"))
    //            defaultIndex = i;

    //        if (!uniqueFolders.Contains(folderName) && !string.IsNullOrEmpty(folderName))
    //        {
    //            uniqueFolders.Add(folderName);
    //            Dropdown.OptionData option = new Dropdown.OptionData(folderName);
    //            options.Add(option);
    //        }
    //    }
    //    dropdown.AddOptions(options);
    //    dropdown.value = defaultIndex;
    //    dropdown.RefreshShownValue();
    //}


}