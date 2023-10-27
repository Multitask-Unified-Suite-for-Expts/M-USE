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
        AddActiveStates(new List<State> {VerifyTask, OtherSetup });
        
        verifyTask_Level = GameObject.Find("ControlLevels").GetComponent<VerifyTask_Level>();
        VerifyTask.AddChildLevel(verifyTask_Level);
        VerifyTask.AddSpecificInitializationMethod(() =>
        {
            verifyTask_Level.TaskLevel = TaskLevel;
        });

        
        VerifyTask.SpecifyTermination(() => VerifyTask.ChildLevel.Terminated, OtherSetup, () =>
        {
            TrialLevel = TaskLevel.TrialLevel;
            TrialLevel.LoadSharedTrialTextures();
        });

        OtherSetup.AddSpecificInitializationMethod(() =>
        {  
            //Setup data management
            TaskDataPath = Session.SessionDataPath + Path.DirectorySeparatorChar + "Task" +Session.GetNiceIntegers(Session.SessionLevel.taskCount + 1) + "_" + TaskLevel.ConfigFolderName;

            if (Session.StoringDataOnServer)
            {
                StartCoroutine(HandleCreateExternalFolder(TaskDataPath)); //Create Task Data folder on External Server
            }

            if (TaskName == "GazeCalibration")
            {
                //Setup data management
                if (Session.SessionLevel.CurrentState.StateName == "SetupSession")
                    // Store Data in the Session Level / Gaze Calibration folder if running at the session level
                    TaskDataPath = Session.TaskSelectionDataPath + Path.DirectorySeparatorChar +
                                   "PreTask_GazeCalibration";

                else
                    // Store Data in the Task / Gaze Calibration folder if not running at the session level
                    TaskDataPath = Session.SessionDataPath + Path.DirectorySeparatorChar + ConfigFolderName +
                                   Path.DirectorySeparatorChar + "InTask_GazeCalibration";

                ConfigFolderName = "GazeCalibration";

            }


            string filePrefix = $"{Session.FilePrefix}_{ConfigFolderName}";

            string subFolderPath = TaskDataPath + Path.DirectorySeparatorChar + "BlockData";
            BlockData = (BlockData) Session.SessionDataControllers.InstantiateDataController<BlockData>(
                "BlockData", ConfigFolderName, subFolderPath);
            BlockData.fileName = filePrefix + "__BlockData.txt";

            subFolderPath = TaskDataPath + Path.DirectorySeparatorChar + "TrialData";
            TrialData = (TrialData) Session.SessionDataControllers.InstantiateDataController<TrialData>(
                "TrialData", ConfigFolderName,
                TaskDataPath + Path.DirectorySeparatorChar + "TrialData");

            //TrialLevel = TaskLevel.TrialLevel; //Moved up to verifyTask term method so that it exists before loading shared textures

            TrialLevel.TrialData = TrialData;
            TrialData.fileName = filePrefix + "__TrialData.txt";

            subFolderPath = TaskDataPath + Path.DirectorySeparatorChar + "FrameData";
            FrameData = (FrameData) Session.SessionDataControllers.InstantiateDataController<FrameData>(
                "FrameData", ConfigFolderName,
                TaskDataPath + Path.DirectorySeparatorChar + "FrameData");

           // TrialLevel.FrameData = FrameData;
            FrameData.fileName = filePrefix + "__FrameData_PreTrial.txt";

            if (Session.SessionDef.EyeTrackerActive)
            {
                Session.GazeData.fileName = filePrefix + "__GazeData_PreTrial.txt";
                Session.GazeData.folderPath = TaskLevel.TaskDataPath + Path.DirectorySeparatorChar + "GazeData";
            }

            FrameData.fileName = filePrefix + "__FrameData_PreTrial.txt";

            BlockData.InitDataController();
            TrialData.InitDataController();
            FrameData.InitDataController();

            BlockData.ManuallyDefine();
            TrialData.ManuallyDefine();
            FrameData.ManuallyDefine();
            if (Session.SessionDef.EyeTrackerActive)
                Session.GazeData.ManuallyDefine();

            if (Session.SessionDef.EventCodesActive)
                FrameData.AddEventCodeColumns();
            if (Session.SessionDef.FlashPanelsActive)
                FrameData.AddFlashPanelColumns();

            
            TaskLevel.BlockData = BlockData;
            TaskLevel.FrameData = FrameData;
            TaskLevel.TrialData = TrialData;
            TaskName = TaskLevel.TaskName;
            TaskLevel.TrialLevel = TrialLevel;

            if (TaskLevel.CustomTaskEventCodes != null)
                TrialLevel.TaskEventCodes = TaskLevel.CustomTaskEventCodes;

            if (Session.SessionDef.EyeTrackerActive)
                Session.GazeTracker.Init(FrameData, 0);
            Session.MouseTracker.Init(FrameData, 0);

            GameObject fbControllers = Instantiate(Resources.Load<GameObject>("FeedbackControllers"), Session.InputManager.transform);

            if (TaskLevel.TaskDef != null)
            {
                List<string> fbControllersList = TaskLevel.TaskDef?.FeedbackControllers;
                fbControllers.GetComponent<TokenFBController>().SetTotalTokensNum(TaskLevel.TaskDef.TotalTokensNum);

                TrialLevel.AudioFBController = fbControllers.GetComponent<AudioFBController>();
                TrialLevel.HaloFBController = fbControllers.GetComponent<HaloFBController>();
                TrialLevel.TokenFBController = fbControllers.GetComponent<TokenFBController>();
                TrialLevel.SliderFBController = fbControllers.GetComponent<SliderFBController>();
                TrialLevel.TouchFBController = fbControllers.GetComponent<TouchFBController>();
                TrialLevel.TouchFBController.audioFBController = TrialLevel.AudioFBController;

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

            
            }

            Session.InputManager.SetActive(false);
            TaskLevel.DefineControlLevel();

            TrialLevel.TaskLevel = TaskLevel;
            TrialLevel.FrameData = FrameData;
            TrialLevel.TrialData = TrialData;
            TrialLevel.DefineTrialLevel();
            
            BlockData.AddStateTimingData(TaskLevel);
            StartCoroutine(BlockData.CreateFile());
            StartCoroutine(FrameData.CreateFile());
            if (Session.SessionDef.EyeTrackerActive)
                StartCoroutine(Session.GazeData.CreateFile());

        });
        OtherSetup.SpecifyTermination(() => true, () => null);

    }

    public static IEnumerator HandleCreateExternalFolder(string configName)
    {
        yield return ServerManager.CreateFolder(configName);
    }
}
