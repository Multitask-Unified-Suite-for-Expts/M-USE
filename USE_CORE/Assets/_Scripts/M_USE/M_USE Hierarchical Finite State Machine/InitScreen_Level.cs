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
using USE_States;
using TMPro;
using System.IO;
using UnityEngine.UI;
using System.Collections;
using System.Threading.Tasks;
using GLTFast;
using USE_StimulusManagement;
using USE_UI;

public class InitScreen_Level : ControlLevel
{
    private GameObject InitScreen_GO;
    private GameObject InitScreenCanvas_GO;
    private GameObject StartPanel_GO;
    private GameObject MainPanel_GO;

    private TextMeshProUGUI SubjectID_Text;
    private TextMeshProUGUI SubjectAge_Text;
    private TextMeshProUGUI ServerData_Text;
    private TextMeshProUGUI LocalData_Text;

    private GameObject LocalData_GO;
    private GameObject ServerData_GO;
    private GameObject LocalConfig_GO;
    private GameObject ServerConfig_GO;

    private GameObject Settings_GO;
    private GameObject SettingsButton_GO;
    private GameObject SettingsPanel_GO;

    private Toggle LocalConfig_Toggle;
    private Toggle ServerConfig_Toggle;
    private Toggle DefaultConfig_Toggle;
    private Toggle LocalData_Toggle;
    private Toggle ServerData_Toggle;
    private Toggle NoData_Toggle;

    private GameObject ConnectToServerButton_GO;

    private GameObject RedX_GO;
    private GameObject GreenCheckMark_GO;

    private GameObject ErrorHandling_GO;
    private GameObject[] GreyOutPanels_Array;

    private AudioSource AudioSource;
    [HideInInspector] public AudioClip ToggleChange_AudioClip;
    [HideInInspector] public AudioClip Error_AudioClip;
    [HideInInspector] public AudioClip Connected_AudioClip;

    private State StartScreen;
    private State CollectInfoScreen;

    private FolderDropdown FolderDropdown;

    private bool ValuesLoaded;
    private bool ConfirmButtonPressed;
    private bool ConnectedToServer;
    private bool FoldersSet;

    private string ErrorType;

    private KeyboardController KeyboardController;
    private Toggle KeyboardToggle;


    public override void DefineControlLevel()
    {
        StartScreen = new State("StartScreen");
        CollectInfoScreen = new State("CollectInfoScreen");
        AddActiveStates(new List<State> { StartScreen, CollectInfoScreen });

        SetGameObjects();

        //StartScreen State-----------------------------------------------------------------------------------------------------------------------------------
        StartScreen.AddSpecificInitializationMethod(() =>
        {
            if (SessionValues.WebBuild)
                GameObject.Find("InitScreenCanvas").GetComponent<Canvas>().targetDisplay = 0; //Move initscreen to main display.

            StartPanel_GO.SetActive(true);
        });
        StartScreen.AddUpdateMethod(() =>
        {
            if (StartPanel_GO.transform.localPosition != Vector3.zero)
                StartPanel_GO.transform.localPosition = Vector3.MoveTowards(StartPanel_GO.transform.localPosition, Vector3.zero, 900 * Time.deltaTime);
        });
        StartScreen.SpecifyTermination(() => ConfirmButtonPressed, CollectInfoScreen, () =>
        {
            ConfirmButtonPressed = false;
            StartPanel_GO.SetActive(false);
        });

        //CollectInfo State-----------------------------------------------------------------------------------------------------------------------------------
        CollectInfoScreen.AddSpecificInitializationMethod(() =>
        {
            StartCoroutine(ActivateObjectsAfterPlayerPrefsLoaded());
            MainPanel_GO.SetActive(true);
            Settings_GO.SetActive(true);
        });
        CollectInfoScreen.AddUpdateMethod(() =>
        {
            if (MainPanel_GO.transform.localPosition != Vector3.zero)
                MainPanel_GO.transform.localPosition = Vector3.MoveTowards(MainPanel_GO.transform.localPosition, Vector3.zero, 1000 * Time.deltaTime);

            if (ErrorHandling_GO.activeInHierarchy)
            {
                if (ErrorHandled())
                    ErrorHandling_GO.SetActive(false);
            }
        });
        CollectInfoScreen.SpecifyTermination(() => ConfirmButtonPressed, () => null, () =>
        {
            ConfirmButtonPressed = false;

            SessionValues.SubjectID = GetSubjectID();
            SessionValues.SubjectAge = GetSubjectAge();

            SetConfigInfo();
            SetDataInfo();

            MainPanel_GO.SetActive(false);
            InitScreenCanvas_GO.SetActive(false); //turn off init canvas since last state.

            SessionValues.LoadingController.ActivateLoadingCanvas(); //turn on loading canvas/circle so that it immedietely shows its loading!
        });

    }

    public void OnKeyboardTogglePressed()
    {
        PlayAudio(ToggleChange_AudioClip);
        KeyboardController.UsingKeyboard = KeyboardToggle.isOn;
    }

    private IEnumerator ActivateObjectsAfterPlayerPrefsLoaded()
    {
        yield return new WaitForEndOfFrame(); //Have to wait a frame so that the toggle's can load their IsOn value from PlayerPrefs during their Start() method of ToggleManager.cs

        if(SessionValues.WebBuild)
        {
            LocalConfig_Toggle.isOn = false;
            LocalData_Toggle.isOn = false;
        }

        if(!LocalConfig_Toggle.isOn && !ServerConfig_Toggle.isOn && !DefaultConfig_Toggle.isOn) //if none are on to start, turn on grey out panels
        {
            foreach (var panel in GreyOutPanels_Array)
                panel.SetActive(true);
        }
        
        //Config Toggles:
        if(LocalConfig_Toggle.isOn)
        {
            LocalConfig_GO.SetActive(true);
            ServerConfig_GO.SetActive(false);
        }
        else if(ServerConfig_Toggle.isOn)
        {
            ServerConfig_GO.SetActive(true);
            LocalConfig_GO.SetActive(false);
            if(!ConnectedToServer)
                GreyOutPanels_Array[2].SetActive(true); //set config folder grey out panel active since they havent connected to server yet
        }

        //Data Toggles:
        if (LocalData_Toggle.isOn)
        {
            LocalData_GO.SetActive(true);
            ServerData_GO.SetActive(false);
        }
        else if(ServerData_Toggle.isOn)
        {
            ServerData_GO.SetActive(true);
            LocalData_GO.SetActive(false);
        }

        if (!ServerConfig_Toggle.isOn && !ServerData_Toggle.isOn)
            GreyOutPanels_Array[0].SetActive(true);

        if(NoData_Toggle.isOn || (ServerData_Toggle.isOn && !ConnectedToServer)) //set server data grey out panel on since they havent connected to server
            GreyOutPanels_Array[1].SetActive(true);

        if(DefaultConfig_Toggle.isOn)
            GreyOutPanels_Array[2].SetActive(true);

        ValuesLoaded = true;
    }

    private bool ErrorHandled()
    {
        switch(ErrorType)
        {
            case "EmptyID":
                if (GetSubjectID().Length > 0 && GetSubjectAge().Length > 0)
                    return true;
                break;
            case "EmptyConfigToggle":
                if (DefaultConfig_Toggle.isOn || LocalConfig_Toggle.isOn || ServerConfig_Toggle.isOn)
                    return true;
                break;
            case "EmptyDataToggle":
                if (LocalData_Toggle.isOn || ServerData_Toggle.isOn || NoData_Toggle.isOn)
                    return true;
                break;
            case "NotConnectedToServer":
                if((ServerConfig_Toggle.isOn || ServerData_Toggle.isOn) && ConnectedToServer)
                    return true;
                if (!ServerConfig_Toggle.isOn && !ServerData_Toggle.isOn) //if they changed options from server, then can remove the "connect to server!" error message. 
                    return true;
                break;
            case "EmptyDataFolder":
                if (!NoData_Toggle.isOn && GetDataValue().Length > 0)
                    return true;
                break;
            default:
                Debug.LogWarning("DEFAULT ERROR HANDLED SWITCH STATEMENT!");
                break;
        }
        return false;
    }

    private void SetDataInfo()
    {
        if (LocalData_Toggle.isOn)
        {
            SessionValues.StoringDataLocally = true;
            SessionValues.SessionDataPath = SessionValues.LocateFile.GetPath("Data Folder");
        }
        else if (ServerData_Toggle.isOn)
        {
            SessionValues.StoringDataOnServer = true;
            ServerManager.RootDataFolder = GetDataValue();
        }
    }

    private void SetConfigInfo()
    {
        if (LocalConfig_Toggle.isOn)
        {
            SessionValues.UsingLocalConfigs = true;
            SessionValues.ConfigFolderPath = SessionValues.LocateFile.GetPath("Config Folder");
        }
        else if (ServerConfig_Toggle.isOn)
        {
            SessionValues.UsingServerConfigs = true;
            ServerManager.SetSessionConfigFolderName(FolderDropdown.dropdown.options[FolderDropdown.dropdown.value].text);
            SessionValues.ConfigFolderPath = ServerManager.SessionConfigFolderPath;
        }
        else if (DefaultConfig_Toggle.isOn)
        {
            SessionValues.UsingDefaultConfigs = true;
            SessionValues.ConfigFolderPath = Application.persistentDataPath + Path.DirectorySeparatorChar + "M_USE_DefaultConfigs";
        }
        else
            Debug.LogWarning("TRYING TO SET CONFIG INFO BUT NO CONFIG TOGGLE IS SELECTED!");
    }

    private void HandleConfigToggle(GameObject selectedGO)
    {
        if (selectedGO == LocalConfig_Toggle.gameObject && LocalConfig_Toggle.isOn)
        {
            TurnTogglesOff(new List<Toggle>() { DefaultConfig_Toggle, ServerConfig_Toggle });
            LocalConfig_GO.SetActive(true);
            GreyOutPanels_Array[0].SetActive(!ServerData_Toggle.isOn);
            DeactivateObjects(new List<GameObject>() { ServerConfig_GO, GreyOutPanels_Array[2] });  
        }
        else if (selectedGO == ServerConfig_Toggle.gameObject && ServerConfig_Toggle.isOn)
        {
            TurnTogglesOff(new List<Toggle>() { DefaultConfig_Toggle, LocalConfig_Toggle });
            ServerConfig_GO.SetActive(true);
            List<GameObject> deactivateList = new List<GameObject>() { LocalConfig_GO, GreyOutPanels_Array[0] };
            if (ConnectedToServer)
                deactivateList.Add(GreyOutPanels_Array[2]);
            else
                GreyOutPanels_Array[2].SetActive(true);
            DeactivateObjects(deactivateList);
            if (ConnectedToServer && !FoldersSet)
                PopulateServerDropdown();
        }
        else if (selectedGO == DefaultConfig_Toggle.gameObject && DefaultConfig_Toggle.isOn)
        {
            TurnTogglesOff(new List<Toggle>() { LocalConfig_Toggle, ServerConfig_Toggle });
            GreyOutPanels_Array[2].SetActive(true);   
            GreyOutPanels_Array[0].SetActive(!ServerData_Toggle.isOn);
        }
    }

    private void HandleDataToggle(GameObject selectedGO)
    {
        if (selectedGO == LocalData_Toggle.gameObject && LocalData_Toggle.isOn)
        {
            TurnTogglesOff(new List<Toggle>() { ServerData_Toggle, NoData_Toggle });
            LocalData_GO.SetActive(true);
            GreyOutPanels_Array[0].SetActive(!ServerConfig_Toggle.isOn);
            DeactivateObjects(new List<GameObject>() { ServerData_GO, GreyOutPanels_Array[1] });  
        }
        else if (selectedGO == ServerData_Toggle.gameObject && ServerData_Toggle.isOn)
        {
            TurnTogglesOff(new List<Toggle>() { LocalData_Toggle, NoData_Toggle });
            ServerData_GO.SetActive(true);
            List<GameObject> toDeactivate = new List<GameObject>() { LocalData_GO, GreyOutPanels_Array[0] };
            if (ConnectedToServer)
                toDeactivate.Add(GreyOutPanels_Array[1]);
            DeactivateObjects(toDeactivate);
        }
        else if (selectedGO == NoData_Toggle.gameObject && NoData_Toggle.isOn)
        {
            TurnTogglesOff(new List<Toggle>() { ServerData_Toggle, LocalData_Toggle });
            GreyOutPanels_Array[1].SetActive(true);   
            GreyOutPanels_Array[0].SetActive(!ServerConfig_Toggle.isOn);
        }
    }

    private void TurnTogglesOff(List<Toggle> toggles)
    {
        foreach(Toggle toggle in toggles)
        {
            toggle.isOn = false;
        }
    }

    private void DeactivateObjects(List<GameObject> objects)
    {
        foreach (GameObject go in objects)
            go.SetActive(false);

        objects.Clear();
    }


    public void OnToggleChange()
    {
        if (ValuesLoaded)
            PlayAudio(ToggleChange_AudioClip);

        GameObject selectedGO = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;

        if (selectedGO.name.ToLower().Contains("config"))
            HandleConfigToggle(selectedGO);

        else if (selectedGO.name.ToLower().Contains("data"))
            HandleDataToggle(selectedGO);
    }

    private void SetGameObjects()
    {
        InitScreen_GO = GameObject.Find("InitScreen_GO");
        InitScreenCanvas_GO = GameObject.Find("InitScreenCanvas");

        KeyboardController = InitScreenCanvas_GO.GetComponent<KeyboardController>();
        KeyboardToggle = GameObject.Find("Keyboard_Toggle").GetComponent<Toggle>();

        StartPanel_GO = InitScreen_GO.transform.Find("StartPanel").gameObject;
        StartPanel_GO.transform.localPosition = new Vector3(0, -800, 0); //start it off the screen

        MainPanel_GO = InitScreen_GO.transform.Find("MainPanel").gameObject;
        MainPanel_GO.transform.localPosition = new Vector3(0, -800, 0); //start it off the screen  

        LocalConfig_Toggle = GameObject.Find("LocalConfigs_Toggle").GetComponent<Toggle>();
        ServerConfig_Toggle = GameObject.Find("ServerConfigs_Toggle").GetComponent<Toggle>();
        DefaultConfig_Toggle = GameObject.Find("DefaultConfigs_Toggle").GetComponent<Toggle>();
        LocalData_Toggle = GameObject.Find("LocalData_Toggle").GetComponent<Toggle>();
        ServerData_Toggle = GameObject.Find("ServerData_Toggle").GetComponent<Toggle>();
        NoData_Toggle = GameObject.Find("NoData_Toggle").GetComponent<Toggle>();

        ErrorHandling_GO = GameObject.Find("ErrorHandling_Panel");
        ErrorHandling_GO.SetActive(false);

        SubjectID_Text = GameObject.Find("SubjectID_Text").GetComponent<TextMeshProUGUI>();
        SubjectAge_Text = GameObject.Find("SubjectAge_Text").GetComponent<TextMeshProUGUI>();
        ServerData_Text = GameObject.Find("ServerData_Text").GetComponent<TextMeshProUGUI>();
        LocalData_Text = GameObject.Find("LocalData_Text").GetComponent<TextMeshProUGUI>();

        GreyOutPanels_Array = new GameObject[3]
        {
            GameObject.Find("GreyOutPanel_ServerURL"), GameObject.Find("GreyOutPanel_Data"), GameObject.Find("GreyOutPanel_Config")
        };
        foreach (GameObject go in GreyOutPanels_Array)
            go.SetActive(false);

        ConnectToServerButton_GO = GameObject.Find("ConnectButton");

        RedX_GO = GameObject.Find("RedX");
        RedX_GO.SetActive(false);
        GreenCheckMark_GO = GameObject.Find("GreenCheckMark");
        GreenCheckMark_GO.SetActive(false);

        LocalData_GO = GameObject.Find("LocalData_GO");
        ServerData_GO = GameObject.Find("ServerData_GO");

        FolderDropdown = GameObject.Find("Dropdown").GetComponent<FolderDropdown>();

        LocalConfig_GO = GameObject.Find("LocalConfig_GO");
        ServerConfig_GO = GameObject.Find("ServerConfig_GO");

        Settings_GO = GameObject.Find("InitScreen_Settings");
        SettingsPanel_GO = GameObject.Find("SettingsPanel");
        SettingsButton_GO = GameObject.Find("SettingsButton");
        SettingsButton_GO.GetComponent<Button>().onClick.AddListener(HandleSettingButtonClicked);
        SettingsPanel_GO.SetActive(false);
        Settings_GO.SetActive(false);


        //SETUP FILE ITEMS FOR BOTH ConfigFolder & DataFolder:
        FileSpec configFileSpec = new FileSpec
        {
            name = "Config Folder",
            isFolder = true
        };
        SessionValues.LocateFile.AddToFilesDict(configFileSpec); //add to locatefile files dict
        TMP_InputField configInputField = LocalConfig_GO.GetComponentInChildren<TMP_InputField>();
        FileItem_TMP configFileItem = LocalConfig_GO.AddComponent<FileItem_TMP>();
        TextMeshProUGUI configText = GameObject.Find("LocalConfig_Text").GetComponent<TextMeshProUGUI>();
        configFileItem.ManualStart(configFileSpec, configInputField, configText);
        LocalConfig_GO.GetComponentInChildren<Button>().onClick.AddListener(configFileItem.Locate);

        FileSpec dataFileSpec = new FileSpec
        {
            name = "Data Folder",
            isFolder = true
        };
        SessionValues.LocateFile.AddToFilesDict(dataFileSpec); //add to locatefile files dict
        TMP_InputField dataInputField = LocalData_GO.GetComponentInChildren<TMP_InputField>();
        FileItem_TMP dataFileItem = LocalData_GO.AddComponent<FileItem_TMP>();
        TextMeshProUGUI dataText = GameObject.Find("LocalData_Text").GetComponent<TextMeshProUGUI>();
        dataFileItem.ManualStart(dataFileSpec, dataInputField, dataText);
        LocalData_GO.GetComponentInChildren<Button>().onClick.AddListener(dataFileItem.Locate);


        if (SessionValues.WebBuild)
        {
            LocalData_GO.SetActive(false);
            LocalConfig_GO.SetActive(false);
        }
        else
        {
            ServerData_GO.SetActive(false);
            ServerConfig_GO.SetActive(false);

            //Un-Block out local toggle options if not web build:
            GameObject.Find("LocalConfigsToggle_GREYPANEL").SetActive(false);
            GameObject.Find("LocalDataToggle_GREYPANEL").SetActive(false);
        }

        MainPanel_GO.SetActive(false);

        AudioSource = gameObject.AddComponent<AudioSource>();
        ToggleChange_AudioClip = Resources.Load<AudioClip>("GridItemAudio");
        Error_AudioClip = Resources.Load<AudioClip>("Error");
        Connected_AudioClip = Resources.Load<AudioClip>("DoubleBeep");
    }

    public void HandleSettingButtonClicked()
    {
        SettingsPanel_GO.SetActive(!SettingsPanel_GO.activeInHierarchy);
    }

    private void DisplayErrorMessage(string message, string errorType)
    {
        PlayAudio(Error_AudioClip);
        ErrorType = errorType;
        ErrorHandling_GO.SetActive(true);
        ErrorHandling_GO.transform.Find("ErrorHandling_Text").GetComponent<TextMeshProUGUI>().text = message;
    }

    public void HandleConfirmButtonPress() //For the AllInfo Panel's Confirm Button
    {
        if (GetSubjectID().Length < 1 || GetSubjectAge().Length < 1)
            DisplayErrorMessage("Input a SubjectID and SubjectAge!", "EmptyID");
        else if (!LocalConfig_Toggle.isOn && !ServerConfig_Toggle.isOn && !DefaultConfig_Toggle.isOn) //make sure 1 of the config types is selected
            DisplayErrorMessage("Select a Config Type!", "EmptyConfigToggle");
        else if (!LocalData_Toggle.isOn && !ServerData_Toggle.isOn && !NoData_Toggle.isOn) //make sure 1 of the data options is selected
            DisplayErrorMessage("Select a Data Option!", "EmptyDataToggle");
        else if ((ServerConfig_Toggle.isOn || ServerData_Toggle.isOn) && !ConnectedToServer) //if one of the 2 server toggle's is on, make sure already connected to server and they've selected a dropdown
            DisplayErrorMessage("Connect to your server!", "NotConnectedToServer");
        else if(!NoData_Toggle.isOn && GetDataValue().Length < 1)
            DisplayErrorMessage("Input a Data Folder Path!", "EmptyDataFolder");
        else
        {
            PlayAudio(ToggleChange_AudioClip);
            ConfirmButtonPressed = true;
        }
    }

    public void HandleStartSessionButtonPress()
    {
        PlayAudio(ToggleChange_AudioClip);
        ConfirmButtonPressed = true;
    }

    public void HandleConnectToServerButtonPressed()
    {
        if (ConnectedToServer)
            return;

        string url = GameObject.Find("ServerURL_Text").GetComponent<TextMeshProUGUI>().text;
        ServerManager.ServerURL = url.Remove(url.Length - 1, 1);
        StartCoroutine(TestServerConnection());
    }

    private IEnumerator TestServerConnection()
    {
        yield return ServerManager.TestServerConnection((isConnected) =>
        {
            if (isConnected)
            {
                PlayAudio(Connected_AudioClip);
                ConnectedToServer = true;
                if(ServerConfig_Toggle.isOn)
                    GreyOutPanels_Array[2].SetActive(false);
                if(ServerData_Toggle.isOn)
                    GreyOutPanels_Array[1].SetActive(false);
                ConnectToServerButton_GO.GetComponent<Image>().color = Color.green;
                ConnectToServerButton_GO.GetComponentInChildren<Text>().text = "Connected";
                GreenCheckMark_GO.SetActive(true);
                RedX_GO.SetActive(false);

                if(ServerConfig_Toggle.isOn && !FoldersSet)
                    PopulateServerDropdown();
            }
            else
            {
                Debug.LogWarning("UNABLE TO CONNECT TO SERVER!");
                PlayAudio(Error_AudioClip);
                ConnectToServerButton_GO.GetComponentInChildren<Image>().color = Color.red;
                RedX_GO.SetActive(true);
            }
        });
    }

    public void PopulateServerDropdown()
    {
        StartCoroutine(ServerManager.GetSessionConfigFolders(folders =>
        {
            if (folders != null)
            {
                FolderDropdown.SetFolders(folders);
                FoldersSet = true;
            }
            else
                Debug.Log("TRIED TO GET FOLDERS FROM SERVER BUT THE RESULT IS NULL!");
        }));
    }

    private string GetSubjectID()
    {
        string subjectID = SubjectID_Text.GetComponent<TextMeshProUGUI>().text;
        return subjectID.Remove(subjectID.Length - 1, 1);
    }
    private string GetSubjectAge()
    {
        string SubjectAge = SubjectAge_Text.GetComponent<TextMeshProUGUI>().text;
        return SubjectAge.Remove(SubjectAge.Length - 1, 1);
    }
    private string GetDataValue()
    {
        string datavalue = ServerData_GO.activeInHierarchy ? ServerData_Text.GetComponent<TextMeshProUGUI>().text : LocalData_Text.GetComponent<TextMeshProUGUI>().text;
        return datavalue.Remove(datavalue.Length - 1, 1);
    }

    public void PlayAudio(AudioClip clip)
    {
        if (clip != null)
        {
            AudioSource.clip = clip;
            AudioSource.Play();
        }
        else
            Debug.Log("CANT PLAY AUDIO CLIP BECAUSE IT IS NULL!");
    }


}
