using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using USE_ExperimentTemplate_Data;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Trial;

public class GazeCalibrationController : MonoBehaviour
{
    public GameObject GazeCalibration_CanvasGO;
    public GameObject GazeCalibration_CameraGO;

    public ControlLevel_Task_Template GazeCalibrationTaskLevel;
    public ControlLevel_Trial_Template GazeCalibrationTrialLevel;

    public ControlLevel_Task_Template OriginalTaskLevel;
    public ControlLevel_Trial_Template OriginalTrialLevel;


    public bool RunCalibration;
    public string TaskLevelGazeDataFileName;
    public string GazeCalibrationDataFolderPath;

    public string serialRecvDataFileName = "", serialSentDataFileName = "", gazeDataFileName = "";

    private bool CreatedSessionSerialAndGazeDataFiles;
    private bool CreatedTaskSerialAndGazeDataFiles;

    public string taskGazeCalibrationFolderPath, taskFolderPath, sessionGazeCalibrationFolderPath, sessionFolderPath;
    public void ActivateGazeCalibrationComponents()
    {
        GazeCalibration_CanvasGO.SetActive(true);
        GazeCalibration_CameraGO.SetActive(true);

    }    
    public void DectivateGazeCalibrationComponents()
    {
        GazeCalibration_CanvasGO.SetActive(false);
        GazeCalibration_CameraGO.SetActive(false);

    }
    public void ReassignGazeCalibrationDataFolderPath(string newFolderPath)
    {
        GazeCalibrationTaskLevel.BlockData.folderPath = newFolderPath + Path.DirectorySeparatorChar + "BlockData";
        GazeCalibrationTaskLevel.TrialData.folderPath = newFolderPath + Path.DirectorySeparatorChar + "TrialData";
        GazeCalibrationTaskLevel.FrameData.folderPath = newFolderPath + Path.DirectorySeparatorChar + "FrameData";
    }

    private IEnumerator WriteDataFilesAndDeactivate(ControlLevel_Trial_Template trialLevel, ControlLevel_Task_Template taskLevel)
    {
        // Start TrialData coroutines and wait for them to complete
        yield return StartCoroutine(trialLevel.TrialData.AppendDataToBuffer());
        yield return StartCoroutine(trialLevel.TrialData.AppendDataToFile());

        // Start TaskLevel.FrameData coroutines and wait for them to complete
        yield return StartCoroutine(taskLevel.FrameData.AppendDataToBuffer());
        yield return StartCoroutine(taskLevel.FrameData.AppendDataToFile());

        

        taskLevel.DeactivateTaskDataControllers();


    }

    public void WriteDataFileThenDeactivateDataController(ControlLevel_Trial_Template trialLevel, ControlLevel_Task_Template taskLevel, string transition)
    {
        Debug.Log("**Trial Data Path: " + trialLevel.TrialData.folderPath);
        StartCoroutine(WriteDataFilesAndDeactivate(trialLevel, taskLevel));

        switch (transition)
        {
            case ("TaskToGazeCalibration"):
                Session.TrialLevel = GazeCalibrationTrialLevel;
                Session.TaskLevel = GazeCalibrationTaskLevel;
                break;
            case ("GazeCalibrationToTask"):
                Session.TrialLevel = OriginalTrialLevel;
                Session.TaskLevel = OriginalTaskLevel;
                break;
            default:
                Debug.Log("INVALID TRANSITION PASSED THROUGH FUNCTION");
                break;
        }
    }

    private IEnumerator WriteSerialAndGazeDataAndReassignPath(string path, string transition)
    {
        if (transition.Equals("GazeCalibrationToSession"))
        {
            if (Session.SessionDef.SerialPortActive)
            {
                Session.SerialRecvData.fileName = serialRecvDataFileName;
                Session.SerialSentData.fileName = serialSentDataFileName;
                Debug.Log("**Returning to the main level and the serial file name is: " + Session.SerialSentData.fileName);
            }
            Session.GazeData.fileName = gazeDataFileName;
        }
        // Write the Serial Sent, Serial Recv, and Gaze Data before changing path
        if (Session.SessionDef.SerialPortActive)
        {
            yield return StartCoroutine(Session.SerialRecvData.AppendDataToFile());
            yield return StartCoroutine(Session.SerialSentData.AppendDataToFile());

        }
        yield return StartCoroutine(Session.GazeData.AppendDataToFile());

        if (Session.SessionDef.SerialPortActive)
        {
            if (transition.Equals("GazeCalibrationToSession"))
            {
                Session.SerialRecvData.folderPath = path;
                Session.SerialSentData.folderPath = path;
            }
            else
            {
                Session.SerialRecvData.folderPath = path + Path.DirectorySeparatorChar + "SerialRecvData";
                Session.SerialSentData.folderPath = path + Path.DirectorySeparatorChar + "SerialSentData";
            }
            
        }

        if (transition.Equals("GazeCalibrationToSession"))
            Session.GazeData.folderPath = path;
        else
            Session.GazeData.folderPath = path + Path.DirectorySeparatorChar + "GazeData";

       // Session.GazeCalibrationController.ReassignGazeCalibrationDataFolderPath(path);

        if (!CreatedSessionSerialAndGazeDataFiles && transition.Equals("SessionToGazeCalibration"))
        {
            if (Session.SessionDef.SerialPortActive)
            {
                StartCoroutine(Session.SerialSentData.CreateFile());
                StartCoroutine(Session.SerialRecvData.CreateFile());
            }
            StartCoroutine(Session.GazeData.CreateFile());
            CreatedSessionSerialAndGazeDataFiles = true;
        }
        if (!CreatedTaskSerialAndGazeDataFiles && transition.Equals("TaskToGazeCalibration"))
        {
            if (Session.SessionDef.SerialPortActive)
            {
                Session.SerialSentData.CreateNewTrialIndexedFile(GazeCalibrationTrialLevel.TrialCount_InTask + 1, Session.FilePrefix);
                Session.SerialRecvData.CreateNewTrialIndexedFile(GazeCalibrationTrialLevel.TrialCount_InTask + 1, Session.FilePrefix);
            }
            Session.GazeData.CreateNewTrialIndexedFile(GazeCalibrationTrialLevel.TrialCount_InTask + 1, Session.FilePrefix);
            CreatedTaskSerialAndGazeDataFiles = true;
        }


    }
    public void WriteSerialAndGazeDataThenReassignDataPath(string transition)
    {
        switch (transition)
        {
            case "TaskToGazeCalibration":
                taskGazeCalibrationFolderPath = Session.SessionDataPath + Path.DirectorySeparatorChar + "GazeCalibration" + Path.DirectorySeparatorChar + "TaskData";
                StartCoroutine(WriteSerialAndGazeDataAndReassignPath(taskGazeCalibrationFolderPath, transition));
                Debug.Log("**WRITING TASK LEVEL SERIAL AND GAZE DATA TO : " + Session.SerialRecvData.folderPath + " AND CHANGING PATH TO " + taskGazeCalibrationFolderPath);
                break;
            case "GazeCalibrationToTask":
                taskFolderPath = OriginalTaskLevel.TaskDataPath;
                StartCoroutine(WriteSerialAndGazeDataAndReassignPath(taskFolderPath, transition));
                Debug.Log("**WRITING GAZE CALIBRATION SERIAL AND GAZE DATA TO : " + Session.SerialRecvData.folderPath + " AND CHANGING PATH TO " + taskFolderPath);
                break;
            case "SessionToGazeCalibration":
                sessionGazeCalibrationFolderPath = Session.SessionDataPath + Path.DirectorySeparatorChar + "GazeCalibration" + Path.DirectorySeparatorChar + "TaskSelectionData";
                serialRecvDataFileName = Session.SerialRecvData.fileName;
                serialSentDataFileName = Session.SerialSentData.fileName;
                gazeDataFileName = Session.GazeData.fileName;
                StartCoroutine(WriteSerialAndGazeDataAndReassignPath(sessionGazeCalibrationFolderPath, transition));
                Debug.Log("**WRITING SESSION SERIAL AND GAZE DATA TO : " + Session.SerialRecvData.folderPath + " AND CHANGING PATH TO " + sessionGazeCalibrationFolderPath);
                break;
            case "GazeCalibrationToSession":
                sessionFolderPath = Session.TaskSelectionDataPath;
                StartCoroutine(WriteSerialAndGazeDataAndReassignPath(sessionFolderPath, transition));
                Debug.Log("**WRITING GAZE CALIBRATION SERIAL AND GAZE DATA TO : " + Session.SerialRecvData.folderPath + " AND CHANGING PATH TO " + sessionFolderPath);
                break;
            default:
                Debug.LogWarning("INVALID TRANSITION TYPE BETWEEN DATA PATHS.");
                break;
        }
    }
}
