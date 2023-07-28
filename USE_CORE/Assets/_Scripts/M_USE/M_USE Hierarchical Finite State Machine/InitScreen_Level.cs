using System.Collections.Generic;
using UnityEngine;
using USE_States;
using TMPro;
using System.IO;
using UnityEngine.UI;
using System.Collections;


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
    public GameObject ServerDataTextGO;
    public GameObject LocalDataTextGO;
    public GameObject LocalConfigTextGO;

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

    private bool ConnectedToServer;

    private string ErrorType;


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
                if(ErrorHandled())
                    ErrorHandlingGO.SetActive(false);
            }
        });
        CollectInfo.SpecifyTermination(() => ConfirmButtonPressed, () => null, () =>
        {
            Debug.Log("DONE COLLECTING INFO NOW ABOUT TO SET EVERYTHING!");
            ConfirmButtonPressed = false;

            //Set SubjectID and SessionID:
            SessionValues.SubjectID = GetSubjectID();
            SessionValues.SessionID = GetSessionID();


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

    private bool ErrorHandled()
    {
        switch(ErrorType)
        {
            case "EmptyID":
                if (GetSubjectID().Length > 0 && GetSessionID().Length > 0)
                    return true;
                break;
            case "EmptyConfigToggle":
                if (DefaultConfigToggle.isOn || LocalConfigToggle.isOn || ServerConfigToggle.isOn)
                    return true;
                break;
            case "EmptyDataToggle":
                if (LocalDataToggle.isOn || ServerDataToggle.isOn || NoDataToggle.isOn)
                    return true;
                break;
            case "NotConnectedToServer":
                if((ServerConfigToggle.isOn || ServerDataToggle.isOn) && ConnectedToServer)
                    return true;
                break;
            case "EmptyDataFolder":
                if (!NoDataToggle.isOn && GetDataValue().Length > 0)
                    return true;
                break;
            default:
                Debug.LogError("DEFAULT ERROR HANDLED SWITCH STATEMENT!");
                break;
        }
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

                    //if already connected to server, go load the session configs and populate dropdown:
                    if (ConnectedToServer)
                        PopulateServerDropdown();
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
        //StartPanel_GO.transform.localPosition = new Vector3(0, 750f, 0);

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
        ServerDataTextGO = GameObject.Find("ServerData_Text");
        LocalDataTextGO = GameObject.Find("LocalData_Text");
        LocalConfigTextGO = GameObject.Find("LocalConfig_Text");


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
        if (GetSubjectID().Length < 1 || GetSessionID().Length < 1)
            DisplayErrorMessage("Input a SubjectID and SessionID!", "EmptyID");
        else if (!LocalConfigToggle.isOn && !ServerConfigToggle.isOn && !DefaultConfigToggle.isOn) //make sure 1 of the config types is selected
            DisplayErrorMessage("Select a Config Type!", "EmptyConfigToggle");
        else if (!LocalDataToggle.isOn && !ServerDataToggle.isOn && !NoDataToggle.isOn) //make sure 1 of the data options is selected
            DisplayErrorMessage("Select a Data Option!", "EmptyDataToggle");
        else if ((ServerConfigToggle.isOn || ServerDataToggle.isOn) && !ConnectedToServer) //if one of the 2 server toggle's is on, make sure already connected to server and they've selected a dropdown
            DisplayErrorMessage("Connect to your server!", "NotConnectedToServer");
        else if(!NoDataToggle.isOn && GetDataValue().Length < 1)
            DisplayErrorMessage("Input a Data Folder Path!", "EmptyDataFolder");
        else
        {
            Debug.Log("Properly Filled out all neccessary information!");
            Continue_AudioSource.Play();
            ConfirmButtonPressed = true;
        }
    }

    private void DisplayErrorMessage(string message, string errorType)
    {
        ErrorType = errorType;
        ErrorHandlingGO.SetActive(true);
        ErrorHandlingGO.transform.Find("ErrorHandling_Text").GetComponent<TextMeshProUGUI>().text = message;
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
        string url = GameObject.Find("ServerURL_Text").GetComponent<TextMeshProUGUI>().text;
        ServerManager.ServerURL = url.Remove(url.Length - 1, 1);
        StartCoroutine(TestServerConnection());
    }

    private IEnumerator TestServerConnection()
    {
        yield return ServerManager.TestServerConnection((isConnected) =>
        {
            if(isConnected)
            {
                ConnectedToServer = true;
                ConnectButton.GetComponent<Image>().color = Color.green;
                ConnectButton.GetComponentInChildren<Text>().text = "Connected";
            }
            else
                Debug.LogError("UNABLE TO CONNECT TO SERVER!");
        });

        if (ConnectedToServer && ServerConfigToggle.isOn)
            PopulateServerDropdown();
    }

    public void PopulateServerDropdown()
    {
        StartCoroutine(ServerManager.GetSessionConfigFolders(folders => FolderDropdown.SetFolders(folders)));
    }

    private string GetSubjectID()
    {
        string subjectID = SubjectIDTextGO.GetComponent<TextMeshProUGUI>().text;
        return subjectID.Remove(subjectID.Length - 1, 1);
    }
    private string GetSessionID()
    {
        string sessionID = SessionIDTextGO.GetComponent<TextMeshProUGUI>().text;
        return sessionID.Remove(sessionID.Length - 1, 1);
    }
    private string GetDataValue()
    {
        string datavalue = ServerDataGO.activeInHierarchy ? ServerDataTextGO.GetComponent<TextMeshProUGUI>().text : LocalDataTextGO.GetComponent<TextMeshProUGUI>().text;
        return datavalue.Remove(datavalue.Length - 1, 1);
    }

    private void SetupAudio()
    {
        Continue_AudioSource = gameObject.AddComponent<AudioSource>();
        Continue_AudioClip = Resources.Load<AudioClip>("GridItemAudio");
        Continue_AudioSource.clip = Continue_AudioClip;
    }


}
