using System.Collections.Generic;
using UnityEngine;
using USE_States;
using TMPro;
using System.IO;
using UnityEngine.UI;
using System.Collections;
using System.Security.Policy;

public class InitScreen_Level : ControlLevel
{
    private GameObject InitScreenGO;
    private GameObject InitScreenCanvasGO;

    public GameObject StartPanel_GO;
    public GameObject MainPanel_GO;

    public GameObject ConfirmButtonGO;

    public GameObject ErrorHandlingGO;

    public GameObject SubjectIDTextGO;
    public GameObject SessionIDTextGO;

    public GameObject ServerURL_GO;

    public GameObject LocalDataGO;
    public GameObject ServerDataGO;

    public GameObject LocalConfigGO;
    public GameObject ServerConfigGO;

    public GameObject ConnectButton;

    private GameObject[] GreyOutPanels_Array;

    public GameObject ConfigType_GO;
    public GameObject StoreData_GO;
    public GameObject StoreDataLocations_GO;
    public GameObject LocalFolderLocations_GO;
    public GameObject ServerSetup_GO;
    public GameObject ServerDropdown_GO;

    private AudioSource Continue_AudioSource;
    private AudioClip Continue_AudioClip;

    private bool ConfirmButtonPressed;

    private Toggle LocalConfigToggle;
    private Toggle ServerConfigToggle;
    private Toggle DefaultConfigToggle;

    private Toggle LocalDataToggle;
    private Toggle ServerDataToggle;
    private Toggle NoDataToggle;

    private State SetupInitScreen;
    private State StartScreen;
    private State CollectInfo;

    private FolderDropdown FolderDropdown;


    public override void DefineControlLevel()
    {
        SetupInitScreen = new State("SetupInitScreen");
        StartScreen = new State("StartScreen");
        CollectInfo = new State("CollectInfo");
        AddActiveStates(new List<State> { SetupInitScreen, StartScreen, CollectInfo});

        SetGameObjects();

        //SetupInitScreen State-----------------------------------------------------------------------------------------------------------------------------------
        SetupInitScreen.AddInitializationMethod(() =>
        {
            SetupAudio();
            if (SessionValues.WebBuild)
                GameObject.Find("InitScreenCanvas").GetComponent<Canvas>().targetDisplay = 0; //Move initscreen to main display.
        });
        SetupInitScreen.SpecifyTermination(() => true, StartScreen);

        //InitialScreen State-----------------------------------------------------------------------------------------------------------------------------------
        StartScreen.AddInitializationMethod(() => StartPanel_GO.SetActive(true));
        StartScreen.SpecifyTermination(() => ConfirmButtonPressed, CollectInfo, () =>
        {
            ConfirmButtonPressed = false;
            StartPanel_GO.SetActive(false);
        });

        //Get_ALLINFO State-----------------------------------------------------------------------------------------------------------------------------------
        CollectInfo.AddInitializationMethod(() =>
        {
            StartCoroutine(UsePlayerPrefsToActivateObjects());
            MainPanel_GO.SetActive(true);
        });
        CollectInfo.AddUpdateMethod(() =>
        {
            if (ErrorHandlingGO.activeInHierarchy)
            {
                if(AllFieldsCompleted())
                    ErrorHandlingGO.SetActive(false);
            }
        });
        CollectInfo.SpecifyTermination(() => ConfirmButtonPressed, () => null, () =>
        {
            Debug.Log("DONE COLLECTING INFO NOW ABOUT TO SET EVERYTHING!");
            ConfirmButtonPressed = false;

            SetSubjectIDAndSessionID();

            //set the server config folder with what they selected in the server dropdown:
            if (ServerConfigToggle.isOn)
            {
                string sessionConfigFolder = FolderDropdown.dropdown.options[FolderDropdown.dropdown.value].text;
                ServerManager.SetSessionConfigFolderName(sessionConfigFolder);
            }

            if (ServerDataToggle.isOn)
                ServerManager.RootDataFolder = GameObject.Find("ServerData_Text").GetComponent<TextMeshProUGUI>().text;
            

            SetConfigInfo();

            SetDataInfo();

            MainPanel_GO.SetActive(false);
            InitScreenCanvasGO.SetActive(false); //turn off init canvas since last state.
        });

    }

    private void SetSubjectIDAndSessionID()
    {
        string subjectID = GameObject.Find("SubjectID_Text").GetComponent<TextMeshProUGUI>().text;
        subjectID = subjectID.Remove(subjectID.Length - 1, 1);
        SessionValues.SubjectID = subjectID;

        string sessionID = GameObject.Find("SessionID_Text").GetComponent<TextMeshProUGUI>().text;
        sessionID = sessionID.Remove(sessionID.Length - 1, 1);
        SessionValues.SessionID = sessionID;
    }

    private IEnumerator UsePlayerPrefsToActivateObjects()
    {
        yield return new WaitForEndOfFrame(); //Have to wait a frame so that the toggle's can load their IsOn value from PlayerPrefs during their Start() method of ToggleManager.cs

        if(LocalConfigToggle.isOn)
        {
            LocalConfigGO.SetActive(true);
            ServerConfigGO.SetActive(false);
        }
        else if(ServerConfigToggle.isOn)
        {
            ServerConfigGO.SetActive(true);
            LocalConfigGO.SetActive(false);
        }

        if (LocalDataToggle.isOn)
        {
            LocalDataGO.SetActive(true);
            ServerDataGO.SetActive(false);
        }
        else if(ServerDataToggle.isOn)
        {
            ServerDataGO.SetActive(true);
            LocalDataGO.SetActive(false);
        }


        if (!ServerConfigToggle.isOn && !ServerDataToggle.isOn)
            GreyOutPanels_Array[0].SetActive(true);

        if(NoDataToggle.isOn)
            GreyOutPanels_Array[1].SetActive(true);

        if(DefaultConfigToggle.isOn)
            GreyOutPanels_Array[2].SetActive(true);
    }

    //will eventually need to add more for the fields in the main section!
    private bool AllFieldsCompleted()
    {
        string subjectValue = SubjectIDTextGO.GetComponent<TextMeshProUGUI>().text;
        subjectValue = subjectValue.Remove(subjectValue.Length - 1, 1);
        string sessionValue = SessionIDTextGO.GetComponent<TextMeshProUGUI>().text.Trim();
        sessionValue = sessionValue.Remove(sessionValue.Length - 1, 1);

        if ((DefaultConfigToggle.isOn || LocalConfigToggle.isOn || ServerConfigToggle.isOn)
             && (LocalDataToggle.isOn || ServerDataToggle.isOn || NoDataToggle.isOn)
             && subjectValue.Length > 0 && sessionValue.Length > 0)
            return true;
        
        return false;
    }

    private void SetDataInfo()
    {
        if (LocalDataToggle.isOn)
        {
            SessionValues.StoringDataLocally = true;
            SessionValues.SessionDataPath = SessionValues.LocateFile.GetPath("Data Folder");
        }
        else if (ServerDataToggle.isOn)
            SessionValues.StoringDataOnServer = true;
        //the else is Not Storing Data, so dont need to set anything
    }

    private void SetConfigInfo()
    {
        if (LocalConfigToggle.isOn)
        {
            SessionValues.UsingLocalConfigs = true;
            SessionValues.ConfigAccessType = "Local";
            SessionValues.ConfigFolderPath = SessionValues.LocateFile.GetPath("Config Folder");
        }
        else if (ServerConfigToggle.isOn)
        {
            SessionValues.UsingServerConfigs = true;
            SessionValues.ConfigAccessType = "Server";
        }
        else //default config toggle is on
        {
            SessionValues.UsingDefaultConfigs = true;
            SessionValues.ConfigAccessType = "Default";
            SessionValues.ConfigFolderPath = Application.persistentDataPath + Path.DirectorySeparatorChar + "M_USE_DefaultConfigs";
        }
    }

    public void HandleToggleChange()
    {
        Continue_AudioSource.Play();
        GameObject selectedGO = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;

        if(selectedGO.name.ToLower().Contains("config"))
        {
            if(selectedGO == LocalConfigToggle.gameObject)
            {
                if (LocalConfigToggle.isOn)
                {
                    DefaultConfigToggle.isOn = false;
                    ServerConfigToggle.isOn = false;
                    GreyOutPanels_Array[0].SetActive(!ServerDataToggle.isOn);
                    LocalConfigGO.SetActive(true);
                    ServerConfigGO.SetActive(false);
                    GreyOutPanels_Array[2].SetActive(false);
                }
            }
            else if (selectedGO == ServerConfigToggle.gameObject)
            {
                if(ServerConfigToggle.isOn)
                {
                    DefaultConfigToggle.isOn = false;
                    LocalConfigToggle.isOn = false;
                    GreyOutPanels_Array[0].SetActive(false);
                    ServerConfigGO.SetActive(true);
                    LocalConfigGO.SetActive(false);
                    GreyOutPanels_Array[2].SetActive(false);
                }
            }
            else if (selectedGO == DefaultConfigToggle.gameObject)
            {
                if(DefaultConfigToggle.isOn)
                {
                    LocalConfigToggle.isOn = false;
                    ServerConfigToggle.isOn = false;
                    GreyOutPanels_Array[0].SetActive(!ServerDataToggle.isOn);
                    GreyOutPanels_Array[2].SetActive(true);                  
                }
            }
        }

        if(selectedGO.name.ToLower().Contains("data"))
        {
            if (selectedGO == LocalDataToggle.gameObject)
            {
                if(LocalDataToggle.isOn)
                {
                    ServerDataToggle.isOn = false;
                    NoDataToggle.isOn = false;
                    GreyOutPanels_Array[0].SetActive(!ServerConfigToggle.isOn);
                    LocalDataGO.SetActive(true);
                    ServerDataGO.SetActive(false);
                    GreyOutPanels_Array[1].SetActive(false);
                }
            }
            else if (selectedGO == ServerDataToggle.gameObject)
            {
                if(ServerDataToggle.isOn)
                {
                    LocalDataToggle.isOn = false;
                    NoDataToggle.isOn = false;
                    GreyOutPanels_Array[0].SetActive(false);
                    ServerDataGO.SetActive(true);
                    LocalDataGO.SetActive(false);
                    GreyOutPanels_Array[1].SetActive(false);
                }
            }
            else if (selectedGO == NoDataToggle.gameObject)
            {
                if(NoDataToggle.isOn)
                {
                    ServerDataToggle.isOn = false;
                    LocalDataToggle.isOn = false;
                    GreyOutPanels_Array[0].SetActive(!ServerConfigToggle.isOn);
                    GreyOutPanels_Array[1].SetActive(true);
                }
            }
        }

    }

    private void SetGameObjects()
    {
        InitScreenGO = GameObject.Find("InitScreen_GO");
        InitScreenCanvasGO = GameObject.Find("InitScreenCanvas");

        StartPanel_GO = InitScreenGO.transform.Find("StartPanel").gameObject;

        LocalConfigToggle = GameObject.Find("LocalConfigs_Toggle").GetComponent<Toggle>();
        ServerConfigToggle = GameObject.Find("ServerConfigs_Toggle").GetComponent<Toggle>();
        DefaultConfigToggle = GameObject.Find("DefaultConfigs_Toggle").GetComponent<Toggle>();

        LocalDataToggle = GameObject.Find("LocalData_Toggle").GetComponent<Toggle>();
        ServerDataToggle = GameObject.Find("ServerData_Toggle").GetComponent<Toggle>();
        NoDataToggle = GameObject.Find("NoData_Toggle").GetComponent<Toggle>();

        ErrorHandlingGO = GameObject.Find("ErrorHandling_Panel");
        ErrorHandlingGO.SetActive(false);

        SubjectIDTextGO = GameObject.Find("SubjectID_Text");
        SessionIDTextGO = GameObject.Find("SessionID_Text");

        GreyOutPanels_Array = new GameObject[3];
        GreyOutPanels_Array[0] = GameObject.Find("GreyOutPanel_ServerURL");
        GreyOutPanels_Array[1] = GameObject.Find("GreyOutPanel_Data");
        GreyOutPanels_Array[2] = GameObject.Find("GreyOutPanel_Config");
        foreach (GameObject go in GreyOutPanels_Array)
            go.SetActive(false);

        MainPanel_GO = InitScreenGO.transform.Find("MainPanel").gameObject;
        ConfirmButtonGO = MainPanel_GO.transform.Find("ButtonConfirm").transform.gameObject;

        ServerURL_GO = GameObject.Find("ServerURL_GO");

        ConnectButton = GameObject.Find("ConnectButton");

        LocalDataGO = GameObject.Find("LocalData_GO");
        ServerDataGO = GameObject.Find("ServerData_GO");
        ServerDataGO.SetActive(false);

        FolderDropdown = GameObject.Find("Dropdown").GetComponent<FolderDropdown>();


        LocalConfigGO = GameObject.Find("LocalConfig_GO");
        ServerConfigGO = GameObject.Find("ServerConfig_GO");
        ServerConfigGO.SetActive(false);


        //SETUP FILE ITEMS FOR BOTH ConfigFolder & DataFolder:
        FileSpec configFileSpec = new FileSpec();
        configFileSpec.name = "Config Folder";
        configFileSpec.isFolder = true;
        TMP_InputField configInputField = LocalConfigGO.GetComponentInChildren<TMP_InputField>();
        FileItem_TMP configFileItem = LocalConfigGO.AddComponent<FileItem_TMP>();
        configFileItem.ManualStart(configFileSpec, configInputField);
        LocalConfigGO.GetComponentInChildren<Button>().onClick.AddListener(configFileItem.Locate);

        FileSpec dataFileSpec = new FileSpec();
        dataFileSpec.name = "Data Folder";
        dataFileSpec.isFolder = true;
        TMP_InputField dataInputField = LocalDataGO.GetComponentInChildren<TMP_InputField>();
        FileItem_TMP dataFileItem = LocalDataGO.AddComponent<FileItem_TMP>();
        dataFileItem.ManualStart(dataFileSpec, dataInputField);
        LocalDataGO.GetComponentInChildren<Button>().onClick.AddListener(dataFileItem.Locate);


        MainPanel_GO.SetActive(false);
    }

    public void HandleConfirmButtonPress() //For the AllInfo Panel's Confirm Button
    {
        //need to also add that if one of the 2 server toggle's is on, make sure already connected to server and they've selected a dropdown

        string subjectValue = SubjectIDTextGO.GetComponent<TextMeshProUGUI>().text.Trim();
        string sessionValue = SessionIDTextGO.GetComponent<TextMeshProUGUI>().text.Trim();

        if(subjectValue.Length < 1 || sessionValue.Length < 1) // Short - term solution cuz for some reason when its empty its saying length is 1, even when trimmed!!!
        {
            ErrorHandlingGO.SetActive(true);
            ErrorHandlingGO.transform.Find("ErrorHandling_Text").GetComponent<TextMeshProUGUI>().text = "Input both a SubjectID and SessionID!";
        }
        else if (!LocalConfigToggle.isOn && !ServerConfigToggle.isOn && !DefaultConfigToggle.isOn) //make sure 1 of the config types is selected
        {
            ErrorHandlingGO.SetActive(true);
            TextMeshProUGUI errorText = ErrorHandlingGO.transform.Find("ErrorHandling_Text").GetComponent<TextMeshProUGUI>();
            errorText.text = "Select a Config Type!";
        }
        else if(!LocalDataToggle.isOn && !ServerDataToggle.isOn && !NoDataToggle.isOn) //make sure 1 of the data options is selected
        {
            ErrorHandlingGO.SetActive(true);
            ErrorHandlingGO.transform.Find("ErrorHandling_Text").GetComponent<TextMeshProUGUI>().text = "Select a Data Option!";
        }
        else
        {
            Debug.Log("Properly Filled out all neccessary information!");
            Continue_AudioSource.Play();
            ConfirmButtonPressed = true;
        }
    }

    public void HandleStartSessionButtonPress()
    {
        Continue_AudioSource.Play();
        ConfirmButtonPressed = true;
    }

    public void HandleConnectToServerButtonPressed()
    {
        if (ConnectButton.GetComponentInChildren<Text>().text == "Connected")
            return;

        Continue_AudioSource.Play();

        //Set Server Info: //CURRENTLY NOT WORKING GETTING THE TEXT FROM INPUT FIELD!!! UN COMMENT BELOW WHEN FIXED!!
        string url = GameObject.Find("ServerURL_Text").GetComponent<TextMeshProUGUI>().text;
        ServerManager.ServerURL = url.Remove(url.Length - 1, 1);

        //If using server configs, go fetch the config folders and populate dropdown
        if (ServerConfigToggle.isOn)
            StartCoroutine(ServerManager.GetSessionConfigFolders(folders =>
            {
                FolderDropdown.SetFolders(folders);
                ConnectButton.GetComponent<Image>().color = Color.green;
                ConnectButton.GetComponentInChildren<Text>().text = "Connected";
            }));

    }

    private void SetupAudio()
    {
        Continue_AudioSource = gameObject.AddComponent<AudioSource>();
        Continue_AudioClip = Resources.Load<AudioClip>("GridItemAudio");
        Continue_AudioSource.clip = Continue_AudioClip;
    }


}
