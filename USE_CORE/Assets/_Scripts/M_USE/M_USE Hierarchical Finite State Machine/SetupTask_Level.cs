using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using USE_ExperimentTemplate_Task;
using USE_States;
using USE_ExperimentTemplate_Data;
using USE_ExperimentTemplate_Trial;


public class SetupTask_Level : ControlLevel
{
    public ImportSettings_Level importSettings_Level;
    public VerifyTask_Level verifyTask_Level;

    public ControlLevel_Task_Template TaskLevel;
    public ControlLevel_Trial_Template TrialLevel;
    private BlockData BlockData;
    private FrameData FrameData;
    private TrialData TrialData;
    private string TaskDataPath, ConfigFolderName, TaskName;

    public override void DefineControlLevel()
    {
        State VerifyTask = new State("VerifyTask");
        State OtherSetup = new State("OtherSetup");
        AddActiveStates(new List<State> {VerifyTask, OtherSetup});
        
        verifyTask_Level = GameObject.Find("ControlLevels").GetComponent<VerifyTask_Level>();
        VerifyTask.AddChildLevel(verifyTask_Level);
        VerifyTask.AddSpecificInitializationMethod(() =>
        {
            verifyTask_Level.TaskLevel = TaskLevel;
        });
            
        VerifyTask.SpecifyTermination(() => VerifyTask.ChildLevel.Terminated, OtherSetup, () =>
        {
        });

        OtherSetup.AddSpecificInitializationMethod(() =>
        {  
            //Setup data management
            TaskDataPath = SessionValues.SessionDataPath + Path.DirectorySeparatorChar + SessionValues.SerialRecvData.GetNiceIntegers(4,SessionValues.SessionLevel.taskCount + 1) + "_" + TaskLevel.ConfigFolderName;

            if (SessionValues.StoringDataOnServer)
            {
                StartCoroutine(HandleCreateExternalFolder(TaskDataPath)); //Create Task Data folder on External Server
            }

            if (TaskName == "GazeCalibration")
            {
                //Setup data management
                if (SessionValues.SessionLevel.CurrentState.StateName == "SetupSession")
                    // Store Data in the Session Level / Gaze Calibration folder if running at the session level
                    TaskDataPath = SessionValues.TaskSelectionDataPath + Path.DirectorySeparatorChar +
                                   "PreTask_GazeCalibration";

                else
                    // Store Data in the Task / Gaze Calibration folder if not running at the session level
                    TaskDataPath = SessionValues.SessionDataPath + Path.DirectorySeparatorChar + ConfigFolderName +
                                   Path.DirectorySeparatorChar + "InTask_GazeCalibration";

                ConfigFolderName = "GazeCalibration";

            }


            string filePrefix = $"{SessionValues.FilePrefix}_{ConfigFolderName}";

            string subFolderPath = TaskDataPath + Path.DirectorySeparatorChar + "BlockData";
            BlockData = (BlockData) SessionValues.SessionDataControllers.InstantiateDataController<BlockData>(
                "BlockData", ConfigFolderName, SessionValues.StoreData, subFolderPath);
            BlockData.taskLevel = TaskLevel;
            BlockData.sessionLevel = SessionValues.SessionLevel;
            BlockData.fileName = filePrefix + "__BlockData.txt";

            subFolderPath = TaskDataPath + Path.DirectorySeparatorChar + "TrialData";
            TrialData = (TrialData) SessionValues.SessionDataControllers.InstantiateDataController<TrialData>(
                "TrialData", ConfigFolderName, SessionValues.StoreData,
                TaskDataPath + Path.DirectorySeparatorChar + "TrialData");
            
            
            TrialLevel = TaskLevel.TrialLevel;
            TrialData.taskLevel = TaskLevel;
            TrialData.trialLevel = TrialLevel;
            TrialData.sessionLevel = SessionValues.SessionLevel;

            TrialLevel.TrialData = TrialData;
            TrialData.fileName = filePrefix + "__TrialData.txt";

            subFolderPath = TaskDataPath + Path.DirectorySeparatorChar + "FrameData";
            FrameData = (FrameData) SessionValues.SessionDataControllers.InstantiateDataController<FrameData>(
                "FrameData", ConfigFolderName, SessionValues.StoreData,
                TaskDataPath + Path.DirectorySeparatorChar + "FrameData");
            FrameData.taskLevel = TaskLevel;
            FrameData.trialLevel = TrialLevel;
            FrameData.sessionLevel = SessionValues.SessionLevel;

           // TrialLevel.FrameData = FrameData;
            FrameData.fileName = filePrefix + "__FrameData_PreTrial.txt";

            if (SessionValues.SessionDef.EyeTrackerActive)
            {
                SessionValues.GazeData.taskLevel = TaskLevel;
                SessionValues.GazeData.trialLevel = TrialLevel;
                SessionValues.GazeData.sessionLevel = SessionValues.SessionLevel;
                SessionValues.GazeData.fileName = filePrefix + "__GazeData_PreTrial.txt";
                SessionValues.GazeData.folderPath = TaskLevel.TaskDataPath + Path.DirectorySeparatorChar + "GazeData";
            }

            FrameData.taskLevel = TaskLevel;
            FrameData.trialLevel = TrialLevel;
            FrameData.fileName = filePrefix + "__FrameData_PreTrial.txt";

            BlockData.InitDataController();
            TrialData.InitDataController();
            FrameData.InitDataController();

            BlockData.ManuallyDefine();
            TrialData.ManuallyDefine();
            FrameData.ManuallyDefine();
            if (SessionValues.SessionDef.EyeTrackerActive)
                SessionValues.GazeData.ManuallyDefine();

            if (SessionValues.SessionDef.EventCodesActive)
                FrameData.AddEventCodeColumns();
            if (SessionValues.SessionDef.FlashPanelsActive)
                FrameData.AddFlashPanelColumns();

            
            TaskLevel.BlockData = BlockData;
            TaskLevel.FrameData = FrameData;
            TaskLevel.TrialData = TrialData;
            TaskName = TaskLevel.TaskName;
            TaskLevel.TrialLevel = TrialLevel;
            //user-defined task control level 



            GameObject fbControllers = Instantiate(Resources.Load<GameObject>("FeedbackControllers"),
                SessionValues.InputManager.transform);

            
            List<string> fbControllersList = TaskLevel.TaskDef.FeedbackControllers;
            int totalTokensNum = TaskLevel.TaskDef.TotalTokensNum;


            //GOTTA BE A BETTER WAY TO DO THIS:
            /*fbControllers.GetComponent<AudioFBController>().SessionEventCodes =
                SessionValues.EventCodeManager.SessionEventCodes;
            fbControllers.GetComponent<HaloFBController>().SessionEventCodes =
                SessionValues.EventCodeManager.SessionEventCodes;
            fbControllers.GetComponent<TokenFBController>().SessionEventCodes =
                SessionValues.EventCodeManager.SessionEventCodes;
            fbControllers.GetComponent<SliderFBController>().SessionEventCodes =
                SessionValues.EventCodeManager.SessionEventCodes;
            fbControllers.GetComponent<TouchFBController>().SessionEventCodes =
                SessionValues.EventCodeManager.SessionEventCodes;
                */

            fbControllers.GetComponent<TokenFBController>().SetTotalTokensNum(totalTokensNum);


            // TrialLevel.SelectionTracker = SelectionTracker;

            TrialLevel.AudioFBController = fbControllers.GetComponent<AudioFBController>();
            TrialLevel.HaloFBController = fbControllers.GetComponent<HaloFBController>();
            TrialLevel.TokenFBController = fbControllers.GetComponent<TokenFBController>();
            TrialLevel.SliderFBController = fbControllers.GetComponent<SliderFBController>();
            TrialLevel.TouchFBController = fbControllers.GetComponent<TouchFBController>();
            TrialLevel.TouchFBController.audioFBController = TrialLevel.AudioFBController;

          //  TrialLevel.TouchFBController.EventCodeManager = SessionValues.EventCodeManager;

            if (TaskLevel.CustomTaskEventCodes != null)
                TrialLevel.TaskEventCodes = TaskLevel.CustomTaskEventCodes;

            if (SessionValues.SessionDef.EyeTrackerActive)
                SessionValues.GazeTracker.Init(FrameData, 0);
            SessionValues.MouseTracker.Init(FrameData, 0);


            TrialLevel.LoadTexturesFromResources();
            //CURRENTLY STILL LOADING THE SHARED TEXTURES FROM RESOURCES:
            //if (SessionValues.UsingDefaultConfigs)
            //    TrialLevel.LoadTexturesFromResources();
            //else if (SessionValues.UsingServerConfigs)
            //    TrialLevel.LoadTexturesFromServer();
            //else if (SessionValues.UsingLocalConfigs)
            //    TrialLevel.LoadTextures(SessionValues.SessionDef.ContextExternalFilePath);


            //Automatically giving TouchFbController;
            TrialLevel.TouchFBController.Init(TrialData, FrameData);


            bool audioInited = false;
            foreach (string fbController in fbControllersList)
            {
                switch (fbController)
                {
                    case "Audio":
                        if (!audioInited)
                        {
                            TrialLevel.AudioFBController.Init(FrameData);
                            audioInited = true;
                        }

                        break;

                    case "Halo":
                        TrialLevel.HaloFBController.Init(FrameData);
                        break;

                    case "Token":
                        if (!audioInited)
                        {
                            TrialLevel.AudioFBController.Init(FrameData);
                            audioInited = true;
                        }

                        TrialLevel.TokenFBController.Init(TrialData, FrameData, TrialLevel.AudioFBController);
                        break;

                    case "Slider":
                        if (!audioInited)
                        {
                            TrialLevel.AudioFBController.Init(FrameData);
                            audioInited = true;
                        }

                        TrialLevel.SliderFBController.Init(TrialData, FrameData, TrialLevel.AudioFBController);
                        break;

                    default:
                        Debug.LogWarning(fbController + " is not a valid feedback controller.");
                        break;
                }
            }

            SessionValues.InputManager.SetActive(false);

            TaskLevel.DefineControlLevel();
            TrialLevel.TaskLevel = TaskLevel;
            TrialLevel.FrameData = FrameData;
            TrialLevel.TrialData = TrialData;
            TrialLevel.DefineTrialLevel();
            
            BlockData.AddStateTimingData(TaskLevel);
            StartCoroutine(BlockData.CreateFile());
            StartCoroutine(FrameData.CreateFile());
            if (SessionValues.SessionDef.EyeTrackerActive)
                StartCoroutine(SessionValues.GazeData.CreateFile());

        });
        
        OtherSetup.SpecifyTermination(() => true, () => null, () => { });
    }

    public static IEnumerator HandleCreateExternalFolder(string configName)
    {
        yield return ServerManager.CreateFolder(configName);
    }
}
