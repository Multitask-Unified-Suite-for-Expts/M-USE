/*
This software is part of the Unified Suite for Experiments (USE).
Information on USE is available at
http://accl.psy.vanderbilt.edu/resources/analysis-tools/unifiedsuiteforexperiments/

Copyright (c) <2018> <Marcus Watson>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

1) The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
2) If this software is used as a component of a project that leads to publication
(e.g. a paper in a scientific journal or a student thesis), the published work
will give appropriate attribution (e.g. citation) to the following paper:
Watson, M.R., Voloh, B., Thomas, C., Hasan, A., Womelsdorf, T. (2018). USE: An
integrative suite for temporally-precise psychophysical experiments in virtual
environments for human, nonhuman, and artificially intelligent agents. BioRxiv:
http://dx.doi.org/10.1101/434944

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using System.Collections;
using UnityEngine;
using TMPro;


public class InitScreen : MonoBehaviour
{
    [HideInInspector] public bool Confirmed;

    public event System.Action OnConfirm, OnLoadSettings;

    //Set In Inspector
    public M_USE_ControlLevel_Session session;
    public GameObject initScreenCanvasGO;
    public GameObject confirmButtonGO;
    public GameObject locateFileGO;
    public GameObject buttonsParentGO;
    public GameObject webBuildChildrenGO;
    public GameObject dropdownGO;

    private FolderDropdown folderDropdown;
    private TMP_Dropdown dropdown;


    void Start()
    {
        folderDropdown = dropdownGO.GetComponent<FolderDropdown>();
        dropdown = dropdownGO.GetComponent<TMP_Dropdown>();

        if (SessionValues.WebBuild)
        {
            StartCoroutine(ServerManager.GetSessionConfigFolders(folders => folderDropdown.SetFolders(folders)));
            SetConfirmButtonPosition();
            confirmButtonGO.SetActive(true);
            webBuildChildrenGO.SetActive(true);
            buttonsParentGO.SetActive(false);
            initScreenCanvasGO.GetComponent<Canvas>().targetDisplay = 0; //Move initscreen to main display.
        }
        else
        {
            webBuildChildrenGO.SetActive(false);
            locateFileGO.SetActive(true); //comment out if want them to pick between default and local configs
            //confirmButtonGO.SetActive(false); //uncomment if want them to pick between default and local configs
            //buttonsParentGO.SetActive(true); //uncomment if want them to pick between default and local configs
        }
    }

    private void Update()
    {
        SetConfirmButtonPosition();
    }

    public void Confirm()
    {
        StartCoroutine(HandleConfirm());
    }

    IEnumerator HandleConfirm()
    {
        SessionValues.LocateFile = locateFileGO.GetComponent<LocateFile>();
            
        if(SessionValues.WebBuild)
        {
            string sessionConfigFolder = dropdown.options[dropdown.value].text;
            ServerManager.SetSessionConfigFolderName(sessionConfigFolder);
            if (sessionConfigFolder.ToLower().Contains("default"))
                SessionValues.UsingDefaultConfigs = true;
        }

        if (OnLoadSettings != null)
            OnLoadSettings();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        Confirmed = true;
        if (OnConfirm != null)
            OnConfirm();
        gameObject.SetActive(false);
        yield return 0;
    }

    public void OnConfigButtonPress() //Used by Normal Build for user to select Default or Local configs. (not using yet)
    {
        buttonsParentGO.SetActive(false);
       
        if(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.name.ToLower().Contains("default"))
        {
            SessionValues.UsingDefaultConfigs = true;
            Confirm();
        }
        else
        {
            locateFileGO.SetActive(true);
            confirmButtonGO.SetActive(true);
        }
    }

    private void SetConfirmButtonPosition()
    {
        if (!Application.isEditor)
        {
            if (Screen.fullScreen)
                confirmButtonGO.transform.localPosition = new Vector3(0, -315, 0);
            else
                confirmButtonGO.transform.localPosition = new Vector3(0, -305, 0);
        }
    }





}


