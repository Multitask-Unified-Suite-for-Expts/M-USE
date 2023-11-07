using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Trial;

public class GazeCalibrationController : MonoBehaviour
{
    public GameObject GazeCalibration_CanvasGO;
    public GameObject GazeCalibration_CameraGO;

    public ControlLevel_Task_Template GazeCalibrationTaskLevel;
    public ControlLevel_Trial_Template GazeCalibrationTrialLevel;

    public bool RunCalibration;
    public string TaskLevelGazeDataFileName;
    public string GazeCalibrationDataFolderPath;

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
}
