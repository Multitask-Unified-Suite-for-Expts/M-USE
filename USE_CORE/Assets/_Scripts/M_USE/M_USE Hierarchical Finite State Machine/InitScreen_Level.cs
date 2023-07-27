using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using USE_States;
using TMPro;
using System.IO;
using UnityEngine.UI;

public class InitScreen_Level : ControlLevel
{
    private State SetupInitScreen;
    private State Get_SubjectId_SessionId;
    private State Get_AllInfo;

    private GameObject InitScreenGO;
    private GameObject InitScreenCanvasGO;

    public GameObject SubjectIDSessionID_GO;
    public GameObject AllInfo_GO;

    public GameObject ConfirmButtonGO;

    public GameObject LocalSectionGO;
    public GameObject ServerSectionGO;

    public GameObject ErrorHandlingGO;

    public GameObject SubjectIDTextGO;
    public GameObject SessionIDTextGO;

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



    public override void DefineControlLevel()
    {
        SetupInitScreen = new State("SetupInitScreen");
        Get_SubjectId_SessionId = new State("Get_SubjectId_SessionId");
        Get_AllInfo = new State("Get_AllInfo");
        AddActiveStates(new List<State> { SetupInitScreen, Get_SubjectId_SessionId, Get_AllInfo});

        SessionValues.LocateFile = GameObject.Find("LocateFile").GetComponent<LocateFile>();

        SetGameObjects();

        //SetupInitScreen State-----------------------------------------------------------------------------------------------------------------------------------
        SetupInitScreen.AddInitializationMethod(() =>
        {
            SetupAudio();
            if (SessionValues.WebBuild)
                GameObject.Find("InitScreenCanvas").GetComponent<Canvas>().targetDisplay = 0; //Move initscreen to main display.
        });
        SetupInitScreen.SpecifyTermination(() => true, Get_SubjectId_SessionId);

        //GetSubjectIdSessionID State-----------------------------------------------------------------------------------------------------------------------------------
        Get_SubjectId_SessionId.AddInitializationMethod(() => SubjectIDSessionID_GO.SetActive(true));
        Get_SubjectId_SessionId.AddUpdateMethod(() =>
        {
            if(ErrorHandlingGO.activeInHierarchy)
            {
                string subjectValue = SubjectIDTextGO.GetComponent<TextMeshProUGUI>().text.Trim();
                string sessionValue = SessionIDTextGO.GetComponent<TextMeshProUGUI>().text.Trim();
                if (subjectValue.Length > 1 && sessionValue.Length > 1)
                    ErrorHandlingGO.SetActive(false);
            }
        });
        Get_SubjectId_SessionId.SpecifyTermination(() => ConfirmButtonPressed, Get_AllInfo, () =>
        {
            ConfirmButtonPressed = false;
            SessionValues.SubjectID = GameObject.Find("SubjectID_Text").GetComponent<TextMeshProUGUI>().text;
            SessionValues.SessionID = GameObject.Find("SessionID_Text").GetComponent<TextMeshProUGUI>().text;
            SubjectIDSessionID_GO.SetActive(false);
        });

        //Get_ALLINFO State-----------------------------------------------------------------------------------------------------------------------------------
        Get_AllInfo.AddInitializationMethod(() => AllInfo_GO.SetActive(true));
        Get_AllInfo.AddUpdateMethod(() =>
        {
            if (ErrorHandlingGO.activeInHierarchy)
            {
                if((DefaultConfigToggle.isOn || LocalConfigToggle.isOn || ServerConfigToggle.isOn) && (LocalDataToggle.isOn || ServerDataToggle.isOn || NoDataToggle.isOn))
                    ErrorHandlingGO.SetActive(false);
            }
        });
        Get_AllInfo.SpecifyTermination(() => ConfirmButtonPressed, () => null, () =>
        {
            ConfirmButtonPressed = false;

            SetConfigInfo();

            SetDataInfo();

            SetServerInfo(); //actually needs to be set after the (not created yet)'s ServerButton is pressed, so that it can load the session config files. 

            AllInfo_GO.SetActive(false);
            InitScreenCanvasGO.SetActive(false); //turn off init canvas since last state.
        });

    }


    private void SetServerInfo()
    {
        if(SessionValues.StoringDataOnServer || SessionValues.UsingServerConfigs)
            ServerManager.ServerURL = ServerSetup_GO.transform.Find("Placeholder").GetComponent<TextMeshProUGUI>().text; //update later
        if(SessionValues.UsingServerConfigs)
            ServerManager.RootConfigFolder = ServerSetup_GO.transform.Find("NewText").GetComponent<TextMeshProUGUI>().text; //update later
        if(SessionValues.StoringDataOnServer)
            ServerManager.RootDataFolder = ServerSetup_GO.transform.Find("texty").GetComponent<TextMeshProUGUI>().text; //update later
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

                    //if neither server option is clicked (and local section isn't active for some reason), make sure local folder locations section is active!
                    if (!ServerConfigToggle.isOn && !ServerDataToggle.isOn && !LocalSectionGO.activeInHierarchy)
                    {
                        ServerSectionGO.SetActive(false);
                        LocalSectionGO.SetActive(true);
                        ConfirmButtonGO.SetActive(true);
                    }
                }
            }
            else if (selectedGO == ServerConfigToggle.gameObject)
            {
                if(ServerConfigToggle.isOn)
                {
                    DefaultConfigToggle.isOn = false;
                    LocalConfigToggle.isOn = false;

                    //make sure server info section is active!
                    LocalSectionGO.SetActive(false);
                    ConfirmButtonGO.SetActive(false);
                    ServerSectionGO.SetActive(true);
                }
            }
            else if (selectedGO == DefaultConfigToggle.gameObject)
            {
                if(DefaultConfigToggle.isOn)
                {
                    LocalConfigToggle.isOn = false;
                    ServerConfigToggle.isOn = false;
                }
            }
            else
                Debug.LogError("THIS SHOULD NEVER HIT!");
        }

        if(selectedGO.name.ToLower().Contains("data"))
        {
            if (selectedGO == LocalDataToggle.gameObject)
            {
                if(LocalDataToggle.isOn)
                {
                    ServerDataToggle.isOn = false;
                    NoDataToggle.isOn = false;

                    //if neither server option is clicked (and local section isn't active for some reason), make sure local folder locations section is active!
                    if (!ServerConfigToggle.isOn && !ServerDataToggle.isOn && !LocalSectionGO.activeInHierarchy)
                    {
                        ServerSectionGO.SetActive(false);
                        LocalSectionGO.SetActive(true);
                        ConfirmButtonGO.SetActive(true);
                    }
                }
            }
            else if (selectedGO == ServerDataToggle.gameObject)
            {
                if(ServerDataToggle.isOn)
                {
                    LocalDataToggle.isOn = false;
                    NoDataToggle.isOn = false;

                    //make sure server info section is active!
                    LocalSectionGO.SetActive(false);
                    ConfirmButtonGO.SetActive(false);
                    ServerSectionGO.SetActive(true);
                }
            }
            else if (selectedGO == NoDataToggle.gameObject)
            {
                if(NoDataToggle.isOn)
                {
                    ServerDataToggle.isOn = false;
                    LocalDataToggle.isOn = false;
                }
            }
            else
                Debug.LogError("THIS SHOULD NEVER HIT!");
        }

    }

    private void SetGameObjects()
    {
        InitScreenGO = GameObject.Find("InitializationScreen_TWO_PANELS");
        InitScreenCanvasGO = GameObject.Find("InitScreenCanvas");

        SubjectIDSessionID_GO = InitScreenGO.transform.Find("Panel_1_SubjectId_SessionId").gameObject;

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

        AllInfo_GO = InitScreenGO.transform.Find("ALL_INFO_PANEL").gameObject;
        ConfirmButtonGO = AllInfo_GO.transform.Find("ButtonConfirm").transform.gameObject;
        LocalSectionGO = AllInfo_GO.transform.Find("LocalFolders_GO").transform.gameObject;
        ServerSectionGO = AllInfo_GO.transform.Find("ServerInfo_GO").transform.gameObject;
        ServerSectionGO.SetActive(false);

        AllInfo_GO.SetActive(false);
    }

    public void HandleConfirmButtonPress() //For the AllInfo Panel's Confirm Button
    {
        if(!LocalConfigToggle.isOn && !ServerConfigToggle.isOn && !DefaultConfigToggle.isOn) //make sure 1 of the config types is selected
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
            Debug.Log("Properly selected both a Config option and a Data option!");
            ConfirmButtonPressed = true;
            Continue_AudioSource.Play();
        }
    }

    public void HandleStartButtonPressed() //For the subjectID/SessionID Panel's Start Button
    {
        string subjectValue = SubjectIDTextGO.GetComponent<TextMeshProUGUI>().text.Trim();
        string sessionValue = SessionIDTextGO.GetComponent<TextMeshProUGUI>().text.Trim();

        if (subjectValue.Length > 1 && sessionValue.Length > 1) //Short-term solution cuz for some reason when its empty its saying length is 1, even when trimmed!!!
        {
            Debug.Log($"Both SubjectID and SessionID have been filled out properly | SubjectID = {subjectValue} | SessionID = {sessionValue}");
            Continue_AudioSource.Play();
            ConfirmButtonPressed = true;

            if (ErrorHandlingGO.activeInHierarchy)
            {
                ErrorHandlingGO.transform.Find("ErrorHandling_Text").GetComponent<TextMeshProUGUI>().text = "Input both a SubjectID and SessionID!";
                ErrorHandlingGO.SetActive(false);
            }
        }
        else
            ErrorHandlingGO.SetActive(true);
    }

    public void HandleServerInfoButtonPressed()
    {
        //need to make a button for the server info, that will then use the info they input to load the session configs from the server and populate the dropdown. 
    }

    private void SetupAudio()
    {
        Continue_AudioSource = gameObject.AddComponent<AudioSource>();
        Continue_AudioClip = Resources.Load<AudioClip>("GridItemAudio");
        Continue_AudioSource.clip = Continue_AudioClip;
    }


}
