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
using SelectionTracking;
using UnityEngine;
using USE_Def_Namespace;
using USE_DisplayManagement;
using USE_ExperimenterDisplay;
using USE_ExperimentTemplate_Data;
using USE_ExperimentTemplate_Session;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Trial;
using USE_UI;


public static class Session
{
    public static bool WebBuild;
    public static bool Using2DStim;

    public static readonly string DefaultStimFolderPath = "DefaultResources/Stimuli";
    public static readonly string DefaultContextFolderPath = "DefaultResources/Contexts";

    //Info Collected from Init Screen Panels:
    public static string SubjectID;
    public static string SubjectAge;
    public static bool UsingDefaultConfigs;
    public static bool UsingLocalConfigs;
    public static bool UsingServerConfigs;
    public static bool StoringDataLocally;
    public static bool StoringDataOnServer;
    public static bool StoreData
    {
        get
        {
            return StoringDataLocally || StoringDataOnServer;
        }
    }

    public static CameraSyncController CameraSyncController;
    public static FullScreenController FullScreenController;
    public static SessionAudioController SessionAudioController;
    public static TimerController TimerController;

    public static SessionInfoPanel SessionInfoPanel;
    public static USE_StartButton USE_StartButton;
    public static GameObject TaskSelectionCanvasGO;
    public static GameObject InitCamGO;


    //New main canvas:
    public static GameObject MainExperimenterCanvas_GO;
    public static GameObject MainExperimenterCanvas_LoadingText_GO;

    public static GameObject ParticipantCanvas_GO;
    public static GameObject ParticipantCanvas_LoadingText_GO;

    public static HumanStartPanel HumanStartPanel;
    public static ExperimenterDisplayController ExperimenterDisplayController;
    public static SessionDataControllers SessionDataControllers;
    public static LocateFile LocateFile;
    public static string SessionDataPath;
    public static string TaskSelectionDataPath;
    public static string FilePrefix;

    public static SerialRecvData SerialRecvData;
    public static SerialSentData SerialSentData;
    public static GazeData GazeData;
    public static GameObject InputTrackers;
    public static MouseTracker MouseTracker;
    public static GazeTracker GazeTracker;
    public static GazeCalibrationController GazeCalibrationController;
    public static TobiiEyeTrackerController TobiiEyeTrackerController;
    public static GameObject InputManager;
    public static FlashPanelController FlashPanelController;

    public static EventCodeManager EventCodeManager;

    public static LogWriter LogWriter;
    
    public static string ConfigFolderPath;

    public static SyncBoxController SyncBoxController;
    public static SerialPortThreaded SerialPortController;

    public static SelectionTracker SelectionTracker;
    public static SelectionTracker.SelectionHandler SelectionHandler;

    public static ControlLevel_Session_Template SessionLevel;
    public static ControlLevel_Task_Template TaskLevel;
    public static ControlLevel_Trial_Template TrialLevel;
    public static SessionDef SessionDef;

    //FOR EVENT CODES:
    public static List<GameObject> TargetObjects, DistractorObjects, IrrelevantObjects;



    static Session()
    {
        TargetObjects = new List<GameObject>();
        DistractorObjects = new List<GameObject>();
        IrrelevantObjects = new List<GameObject>();
    }



    public static void ClearStimLists()
    {
        TargetObjects.Clear();
        DistractorObjects.Clear();
        IrrelevantObjects.Clear();
    }

    public static List<GameObject> GetStartButtonChildren()
    {
        if(SessionDef == null)
            Debug.LogWarning("TRIED TO GET START BUTTON CHILDREN BUT SESSION DEF IS NULL!!!!!");
        else
        {
            if (SessionDef.IsHuman && HumanStartPanel.StartButtonChildren != null)
                return HumanStartPanel.StartButtonChildren;
            else if (!SessionDef.IsHuman && USE_StartButton.StartButtonChildren != null)
                return USE_StartButton.StartButtonChildren;
        }
        Debug.LogWarning("TRIED TO GET START BUTTON CHILDREN BUT THEY ARE NULL!!!!");
        return null;
    }

    public static string GetNiceIntegers(int desiredNum)
    {
        if (desiredNum >= 999)
            return desiredNum.ToString();
        else if (desiredNum >= 99)
            return "0" + desiredNum;
        else if (desiredNum >= 9)
            return "00" + desiredNum;
        else
            return "000" + desiredNum;
    }



}
