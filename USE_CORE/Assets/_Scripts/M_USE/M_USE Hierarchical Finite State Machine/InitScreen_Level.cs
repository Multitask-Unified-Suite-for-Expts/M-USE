using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using USE_States;
using TMPro;
using System.IO;
using UnityEngine.UI;
using static Dropbox.Api.Common.PathRoot;
using static Dropbox.Api.TeamLog.ExternalDriveBackupStatus;
using static UnityEditor.PlayerSettings;
//using UnityEngine.UIElements;

public class InitScreen_Level : ControlLevel
{
    private State SetupInitScreen;
    private State InitialScreen;
    private State Get_AllInfo;

    private GameObject InitScreenGO;
    private GameObject InitScreenCanvasGO;

    public GameObject InitialPanel_GO;
    public GameObject AllInfoPanel_GO;

    public GameObject ConfirmButtonGO;

    public GameObject ErrorHandlingGO;

    public GameObject SubjectIDTextGO;
    public GameObject SessionIDTextGO;

    public GameObject ServerURL_GO;

    public GameObject LocalDataGO;
    public GameObject ServerDataGO;

    public GameObject LocalConfigGO;
    public GameObject ServerConfigGO;

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

    private string CurrentError = "";


    public override void DefineControlLevel()
    {
        SetupInitScreen = new State("SetupInitScreen");
        InitialScreen = new State("InitialScreen");
        Get_AllInfo = new State("Get_AllInfo");
        AddActiveStates(new List<State> { SetupInitScreen, InitialScreen, Get_AllInfo});

        //not sure what to do with this right now:
        //SessionValues.LocateFile = GameObject.Find("LocateFile").GetComponent<LocateFile>();

        SetGameObjects();

        //SetupInitScreen State-----------------------------------------------------------------------------------------------------------------------------------
        SetupInitScreen.AddInitializationMethod(() =>
        {
            SetupAudio();
            if (SessionValues.WebBuild)
                GameObject.Find("InitScreenCanvas").GetComponent<Canvas>().targetDisplay = 0; //Move initscreen to main display.
        });
        SetupInitScreen.SpecifyTermination(() => true, InitialScreen);

        //InitialScreen State-----------------------------------------------------------------------------------------------------------------------------------
        InitialScreen.AddInitializationMethod(() => InitialPanel_GO.SetActive(true));
        InitialScreen.SpecifyTermination(() => ConfirmButtonPressed, Get_AllInfo, () =>
        {
            ConfirmButtonPressed = false;
            InitialPanel_GO.SetActive(false);
        });

        //Get_ALLINFO State-----------------------------------------------------------------------------------------------------------------------------------
        Get_AllInfo.AddInitializationMethod(() => AllInfoPanel_GO.SetActive(true));
        Get_AllInfo.AddUpdateMethod(() =>
        {
            if (ErrorHandlingGO.activeInHierarchy)
            {
                if(AllFieldsCompleted())
                    ErrorHandlingGO.SetActive(false);
            }
        });
        Get_AllInfo.SpecifyTermination(() => ConfirmButtonPressed, () => null, () =>
        {
            ConfirmButtonPressed = false;

            SessionValues.SubjectID = GameObject.Find("SubjectID_Text").GetComponent<TextMeshProUGUI>().text;
            SessionValues.SessionID = GameObject.Find("SessionID_Text").GetComponent<TextMeshProUGUI>().text;

            SetConfigInfo();

            SetDataInfo();

            AllInfoPanel_GO.SetActive(false);
            InitScreenCanvasGO.SetActive(false); //turn off init canvas since last state.
        });

    }

    private bool AllFieldsCompleted()
    {
        string subjectValue = SubjectIDTextGO.GetComponent<TextMeshProUGUI>().text.Trim();
        string sessionValue = SessionIDTextGO.GetComponent<TextMeshProUGUI>().text.Trim();

        if ((DefaultConfigToggle.isOn || LocalConfigToggle.isOn || ServerConfigToggle.isOn)
             && (LocalDataToggle.isOn || ServerDataToggle.isOn || NoDataToggle.isOn)
             && subjectValue.Length > 1 && sessionValue.Length > 1)
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

        InitialPanel_GO = InitScreenGO.transform.Find("Initial_InitScreen_Panel").gameObject;

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
        //GreyOutPanels_Array[1].SetActive(false);
        GreyOutPanels_Array[2] = GameObject.Find("GreyOutPanel_Config");
        //GreyOutPanels_Array[2].SetActive(false);


        AllInfoPanel_GO = InitScreenGO.transform.Find("ALL_INFO_PANEL").gameObject;
        ConfirmButtonGO = AllInfoPanel_GO.transform.Find("ButtonConfirm").transform.gameObject;

        ServerURL_GO = GameObject.Find("ServerURL_GO").transform.gameObject;

        LocalDataGO = GameObject.Find("LocalDataFolderPath_GO").transform.gameObject;
        ServerDataGO = GameObject.Find("ServerDataFolderPath_GO").transform.gameObject;
        ServerDataGO.SetActive(false);

        LocalConfigGO = GameObject.Find("LocalConfig_GO").transform.gameObject;
        ServerConfigGO = GameObject.Find("ServerConfig_GO").transform.gameObject;
        ServerConfigGO.SetActive(false);

        AllInfoPanel_GO.SetActive(false);
    }

    public void HandleConfirmButtonPress() //For the AllInfo Panel's Confirm Button
    {
        string subjectValue = SubjectIDTextGO.GetComponent<TextMeshProUGUI>().text.Trim();
        string sessionValue = SessionIDTextGO.GetComponent<TextMeshProUGUI>().text.Trim();

        if(subjectValue.Length < 2 || sessionValue.Length < 2) // Short - term solution cuz for some reason when its empty its saying length is 1, even when trimmed!!!
        {
            ErrorHandlingGO.SetActive(true);
            ErrorHandlingGO.transform.Find("ErrorHandling_Text").GetComponent<TextMeshProUGUI>().text = "Input both a SubjectID and SessionID!";
            CurrentError = "id";
        }
        else if (!LocalConfigToggle.isOn && !ServerConfigToggle.isOn && !DefaultConfigToggle.isOn) //make sure 1 of the config types is selected
        {
            ErrorHandlingGO.SetActive(true);
            TextMeshProUGUI errorText = ErrorHandlingGO.transform.Find("ErrorHandling_Text").GetComponent<TextMeshProUGUI>();
            errorText.text = "Select a Config Type!";
            CurrentError = "config";
        }
        else if(!LocalDataToggle.isOn && !ServerDataToggle.isOn && !NoDataToggle.isOn) //make sure 1 of the data options is selected
        {
            ErrorHandlingGO.SetActive(true);
            ErrorHandlingGO.transform.Find("ErrorHandling_Text").GetComponent<TextMeshProUGUI>().text = "Select a Data Option!";
            CurrentError = "data";
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

    //NEED TO MOVE THE SUBJECT ID AND SESSION ID ERROR HANDLING SOMEWHERE ELSE!!!!
    public void HandleStartButtonPressed() 
    {
        string subjectValue = SubjectIDTextGO.GetComponent<TextMeshProUGUI>().text.Trim();
        string sessionValue = SessionIDTextGO.GetComponent<TextMeshProUGUI>().text.Trim();

        if (subjectValue.Length > 1 && sessionValue.Length > 1) //Short-term solution cuz for some reason when its empty its saying length is 1, even when trimmed!!!
        {
            Debug.Log($"Both SubjectID and SessionID have been filled out properly | SubjectID = {subjectValue} | SessionID = {sessionValue}");
            Continue_AudioSource.Play();
            ConfirmButtonPressed = true;

            if (ErrorHandlingGO.activeInHierarchy)
                ErrorHandlingGO.SetActive(false);
        }
        else
        {
            ErrorHandlingGO.SetActive(true);
            ErrorHandlingGO.transform.Find("ErrorHandling_Text").GetComponent<TextMeshProUGUI>().text = "Input a SubjectID and SessionID!";
        }
    }

    public void HandleConnectToServerButtonPressed()
    {
        //Set Server Info:
        if (SessionValues.StoringDataOnServer || SessionValues.UsingServerConfigs)
            ServerManager.ServerURL = ServerSetup_GO.transform.Find("Placeholder").GetComponent<TextMeshProUGUI>().text; //update later
        if (SessionValues.UsingServerConfigs)
            ServerManager.RootConfigFolder = ServerSetup_GO.transform.Find("NewText").GetComponent<TextMeshProUGUI>().text; //update later
        if (SessionValues.StoringDataOnServer)
            ServerManager.RootDataFolder = ServerSetup_GO.transform.Find("texty").GetComponent<TextMeshProUGUI>().text; //update later

        //Load Server session config folders and populate dropdown
        //XXXXXXXXXX


    }

    private void SetupAudio()
    {
        Continue_AudioSource = gameObject.AddComponent<AudioSource>();
        Continue_AudioClip = Resources.Load<AudioClip>("GridItemAudio");
        Continue_AudioSource.clip = Continue_AudioClip;
    }


}
