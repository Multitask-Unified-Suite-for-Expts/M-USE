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

public class InitScreen_Level : ControlLevel
{
    public GameObject InitScreen_GO;
    public GameObject StartPanel_GO;
    public GameObject MainPanel_GO;

    public GameObject InitScreenCanvas_GO;

    public TextMeshProUGUI SubjectID_Text;
    public TextMeshProUGUI SubjectAge_Text;
    public TextMeshProUGUI ServerData_Text;
    public TextMeshProUGUI LocalData_Text;

    public TextMeshProUGUI LocalConfigText;
    public TextMeshProUGUI LocalDataText;

    public GameObject LocalData_GO;
    public GameObject ServerData_GO;
    public GameObject LocalConfig_GO;
    public GameObject ServerConfig_GO;

    public GameObject Settings_GO;
    public GameObject SettingsButton_GO;
    public GameObject SettingsPanel_GO;

    public Toggle LocalConfig_Toggle;
    public Toggle ServerConfig_Toggle;
    public Toggle DefaultConfig_Toggle;
    public Toggle LocalData_Toggle;
    public Toggle ServerData_Toggle;
    public Toggle NoData_Toggle;

    public GameObject ConnectToServerButton_GO;

    public GameObject RedX_GO;
    public GameObject GreenCheckMark_GO;

    public GameObject ErrorHandling_GO;
    public GameObject[] GreyOutPanels_Array;

    public GameObject LocalConfigsToggle_GreyPanel;
    public GameObject LocalDataToggle_GreyPanel;

    private AudioSource ButtonAudioSource;
    [HideInInspector] public AudioClip ToggleChange_AudioClip;
    [HideInInspector] public AudioClip Error_AudioClip;
    [HideInInspector] public AudioClip Connected_AudioClip;

    public FolderDropdown FolderDropdown;

    private bool ValuesLoaded;
    private bool ConfirmButtonPressed;
    private bool ConnectedToServer;
    private bool FoldersSet;

    private string ErrorType;

    private KeyboardController KeyboardController;
    public Toggle KeyboardToggle;

    private GameObject Parent_MuseTextGO;
    private GameObject MuseTextGO;
    private GameObject PlayBackgroundImageGO;
    private GameObject PlayTextGO;


    public override void DefineControlLevel()
    {
        State Setup = new State("Setup");
        State StartScreen = new State("StartScreen");
        State CollectInfoScreen = new State("CollectInfoScreen");
        AddActiveStates(new List<State> { Setup, StartScreen, CollectInfoScreen });


        //Setup State-----------------------------------------------------------------------------------------------------------------------------------
        Setup.AddSpecificInitializationMethod(() =>
        {
            SetupInitScreen();
        });
        Setup.AddTimer(() => 1f, StartScreen); //using small timer to while things load

        //StartScreen State-----------------------------------------------------------------------------------------------------------------------------------
        StartScreen.AddSpecificInitializationMethod(() =>
        {
            Parent_MuseTextGO = Instantiate(Resources.Load<GameObject>("NewTitleText"), InitScreenCanvas_GO.transform);
            Parent_MuseTextGO.name = "TitleTextParent";
            Parent_MuseTextGO.SetActive(true);

            PlayBackgroundImageGO = Parent_MuseTextGO.transform.Find("BackgroundImage").gameObject;

            PlayBackgroundImageGO.gameObject.AddComponent<Button>().onClick.AddListener(HandleStartSessionButtonPress);
            PlayTextGO = PlayBackgroundImageGO.transform.Find("PlayText").gameObject;
            PlayBackgroundImageGO.SetActive(false);

            Session.BackgroundMusicController.PlayMusic();

            Session.InitCamGO.AddComponent<CameraCircle>();

            StartCoroutine(PlayButtonDelay(3f));

        });
        StartScreen.AddUpdateMethod(() =>
        {
            if (StartPanel_GO.transform.localPosition != Vector3.zero)
                StartPanel_GO.transform.localPosition = Vector3.MoveTowards(StartPanel_GO.transform.localPosition, Vector3.zero, 900 * Time.deltaTime);
        });
        StartScreen.SpecifyTermination(() => ConfirmButtonPressed, CollectInfoScreen);
        StartScreen.AddUniversalTerminationMethod(() =>
        {
            ConfirmButtonPressed = false;
            StartPanel_GO.SetActive(false);
            Parent_MuseTextGO.SetActive(false);
            //Session.InitCamGO.GetComponent<Skybox>().material = Resources.Load<Material>("MUSE_MainBackground");
        });

        //CollectInfo State-----------------------------------------------------------------------------------------------------------------------------------
        CollectInfoScreen.AddSpecificInitializationMethod(() =>
        {
            StartCoroutine(ActivateObjectsAfterPlayerPrefsLoaded());
            MainPanel_GO.transform.localPosition = new Vector3(0, -800, 0); //start it off the screen  
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
            Session.SubjectID = GetSubjectID();
            Session.SubjectAge = GetSubjectAge();
            SetConfigInfo();
            SetDataInfo();
            InitScreenCanvas_GO.SetActive(false);
            Session.LoadingController.ActivateLoadingCanvas(); //turn on loading canvas/circle so that it immedietely shows its loading!

            Destroy(Session.InitCamGO.GetComponent<CameraCircle>());

            //Set Y of the camera so its far above the terrain for task selection
            Camera.main.transform.position = new Vector3(Camera.main.transform.position.x, 60f, Camera.main.transform.position.z);
            Camera.main.gameObject.AddComponent<CameraCircle>().targetHeight = 55f;
        });

    }

    private IEnumerator PlayButtonDelay(float duration)
    {
        yield return new WaitForSeconds(duration);

        PlayBackgroundImageGO.SetActive(true);
    }

    public void OnKeyboardTogglePressed()
    {
        PlayAudio(ToggleChange_AudioClip);
        KeyboardController.UsingKeyboard = KeyboardToggle.isOn;
    }

    private IEnumerator ActivateObjectsAfterPlayerPrefsLoaded()
    {
        yield return new WaitForEndOfFrame(); //Have to wait a frame so that the toggle's can load their IsOn value from PlayerPrefs during their Start() method of ToggleManager.cs

        if(Session.WebBuild)
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
            Session.StoringDataLocally = true;
            Session.SessionDataPath = Session.LocateFile.GetPath("Data Folder");
        }
        else if (ServerData_Toggle.isOn)
        {
            Session.StoringDataOnServer = true;
            ServerManager.RootDataFolder = GetDataValue();
        }
    }

    private void SetConfigInfo()
    {
        if (LocalConfig_Toggle.isOn)
        {
            Session.UsingLocalConfigs = true;
            Session.ConfigFolderPath = Session.LocateFile.GetPath("Config Folder");
        }
        else if (ServerConfig_Toggle.isOn)
        {
            Session.UsingServerConfigs = true;
            ServerManager.SetSessionConfigFolderName(FolderDropdown.dropdown.options[FolderDropdown.dropdown.value].text);
            Session.ConfigFolderPath = ServerManager.SessionConfigFolderPath;
        }
        else if (DefaultConfig_Toggle.isOn)
        {
            Session.UsingDefaultConfigs = true;
            Session.ConfigFolderPath = Application.persistentDataPath + Path.DirectorySeparatorChar + "M_USE_DefaultConfigs";
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

        if(selectedGO != null)
        {
            if (selectedGO.name.ToLower().Contains("config"))
                HandleConfigToggle(selectedGO);
            else if (selectedGO.name.ToLower().Contains("data"))
                HandleDataToggle(selectedGO);
        }
    }

    private void SetupInitScreen()
    {
        KeyboardController = InitScreenCanvas_GO.GetComponent<KeyboardController>();

        SettingsButton_GO.GetComponent<Button>().onClick.AddListener(HandleSettingButtonClicked);

        //SETUP FILE ITEMS FOR BOTH ConfigFolder & DataFolder:
        FileSpec configFileSpec = new FileSpec
        {
            name = "Config Folder",
            isFolder = true
        };
        Session.LocateFile.AddToFilesDict(configFileSpec); //add to locatefile files dict
        TMP_InputField configInputField = LocalConfig_GO.GetComponentInChildren<TMP_InputField>();
        FileItem_TMP configFileItem = LocalConfig_GO.AddComponent<FileItem_TMP>();
        configFileItem.ManualStart(configFileSpec, configInputField, LocalConfigText);
        LocalConfig_GO.GetComponentInChildren<Button>().onClick.AddListener(configFileItem.Locate);

        FileSpec dataFileSpec = new FileSpec
        {
            name = "Data Folder",
            isFolder = true
        };
        Session.LocateFile.AddToFilesDict(dataFileSpec); //add to locatefile files dict
        TMP_InputField dataInputField = LocalData_GO.GetComponentInChildren<TMP_InputField>();
        FileItem_TMP dataFileItem = LocalData_GO.AddComponent<FileItem_TMP>();
        dataFileItem.ManualStart(dataFileSpec, dataInputField, LocalDataText);
        LocalData_GO.GetComponentInChildren<Button>().onClick.AddListener(dataFileItem.Locate);

        if (Session.WebBuild)
        {
            LocalData_GO.SetActive(false);
            LocalConfig_GO.SetActive(false);
        }
        else
        {
            ServerData_GO.SetActive(false);
            ServerConfig_GO.SetActive(false);
            //Un-Block out local toggle options if not web build:
            LocalConfigsToggle_GreyPanel.SetActive(false);
            LocalDataToggle_GreyPanel.SetActive(false);
        }

        ButtonAudioSource = gameObject.AddComponent<AudioSource>();
        ToggleChange_AudioClip = Resources.Load<AudioClip>("GridItemAudio");
        Error_AudioClip = Resources.Load<AudioClip>("Error");
        Connected_AudioClip = Resources.Load<AudioClip>("DoubleBeep");

        if (Session.WebBuild)
            Session.InitCamGO.GetComponent<Camera>().targetDisplay = 0;
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
            ButtonAudioSource.clip = clip;
            ButtonAudioSource.Play();
        }
        else
            Debug.Log("CANT PLAY AUDIO CLIP BECAUSE IT IS NULL!");
    }


}
