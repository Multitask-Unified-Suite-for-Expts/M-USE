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

    public void WriteDataFileThenDeactivateDataController(ControlLevel_Trial_Template trialLevel, ControlLevel_Task_Template taskLevel)
    {
        Debug.Log("**Trial Data Path: " + trialLevel.TrialData.folderPath);
        StartCoroutine(WriteDataFilesAndDeactivate(trialLevel, taskLevel));
    }

    private IEnumerator WriteSerialAndGazeDataAndReassignPath(string path)
    {
        // Write the Serial Sent, Serial Recv, and Gaze Data before changing path
        if (Session.SessionDef.SerialPortActive)
        {
            yield return StartCoroutine(Session.SerialRecvData.AppendDataToFile());
            yield return StartCoroutine(Session.SerialSentData.AppendDataToFile());

        }
        yield return StartCoroutine(Session.GazeData.AppendDataToFile());

        if (Session.SessionDef.SerialPortActive)
        {
            Session.SerialRecvData.folderPath =  path + Path.DirectorySeparatorChar + "SerialRecvData";
            Session.SerialSentData.folderPath = path + Path.DirectorySeparatorChar + "SerialSentData";
        }

        Session.GazeData.folderPath = path + Path.DirectorySeparatorChar + "GazeData";
        Session.GazeCalibrationController.ReassignGazeCalibrationDataFolderPath(path);

    }
    public void WriteSerialAndGazeDataThenReassignDataPath(string transition)
    {
        // I want to use a
        switch (transition)
        {
            case "TaskToGazeCalibration":
                string taskGazeCalibrationFolderPath = Session.SessionDataPath + Path.DirectorySeparatorChar + "GazeCalibration" + Path.DirectorySeparatorChar + "TaskData";
                StartCoroutine(WriteSerialAndGazeDataAndReassignPath(taskGazeCalibrationFolderPath));
                Debug.Log("**WRITING TASK LEVEL SERIAL AND GAZE DATA TO : " + Session.SerialRecvData.folderPath + " AND CHANGING PATH TO " + taskGazeCalibrationFolderPath);
                break;
            case "GazeCalibrationToTask":
                string taskFolderPath = OriginalTaskLevel.TaskDataPath;
                StartCoroutine(WriteSerialAndGazeDataAndReassignPath(taskFolderPath));
                Debug.Log("**WRITING GAZE CALIBRATION SERIAL AND GAZE DATA TO : " + Session.SerialRecvData.folderPath + " AND CHANGING PATH TO " + taskFolderPath);
                break;
            case "SessionToGazeCalibration":
                string sessionGazeCalibrationFolderPath = Session.SessionDataPath + Path.DirectorySeparatorChar + "GazeCalibration" + Path.DirectorySeparatorChar + "TaskSelectionData";
                StartCoroutine(WriteSerialAndGazeDataAndReassignPath(sessionGazeCalibrationFolderPath));
                Debug.Log("**WRITING SESSION SERIAL AND GAZE DATA TO : " + Session.SerialRecvData.folderPath + " AND CHANGING PATH TO " + sessionGazeCalibrationFolderPath);
                break;
            case "GazeCalibrationToSession":
                string sessionFolderPath = Session.TaskSelectionDataPath;
                StartCoroutine(WriteSerialAndGazeDataAndReassignPath(sessionFolderPath));
                Debug.Log("**WRITING GAZE CALIBRATION SERIAL AND GAZE DATA TO : " + Session.SerialRecvData.folderPath + " AND CHANGING PATH TO " + sessionFolderPath);
                break;
            default:
                Debug.LogWarning("INVALID TRANSITION TYPE BETWEEN DATA PATHS.");
                break;
        }
    }
}
