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
using System.Collections.Specialized;
using System.Collections;
using System.IO;
using UnityEngine;
using USE_Data;
using USE_States;
using USE_ExperimentTemplate_Session;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Trial;
using System.Linq;
using System.Text.RegularExpressions;


namespace USE_ExperimentTemplate_Data
{
    public class SummaryData
    {
        private static string FolderPath;
        private static char Separator;

        public static void Init()
        {
            if (!SessionValues.StoreData)
                return;

            Separator = SessionValues.WebBuild ? '/' : Path.DirectorySeparatorChar;
            FolderPath = SessionValues.SessionDataPath + Separator + "SummaryData";

            if(SessionValues.StoringDataOnServer)
                CoroutineHelper.StartCoroutine(ServerManager.CreateFolder(FolderPath));
            else
                Directory.CreateDirectory(FolderPath);
        }

        public static IEnumerator AddTaskRunData(string ConfigName, ControlLevel state, OrderedDictionary data)
        {
            if (!SessionValues.StoreData)
                yield break;
            
            data["Start Time"] = state.StartTimeAbsolute;
            data["Duration"] = state.Duration;

            string filePath = FolderPath + Separator + "Task" + SessionValues.GetNiceIntegers(4,SessionValues.SessionLevel.taskCount +1) + "_SummaryData_" +ConfigName + ".txt";

            if(SessionValues.StoringDataOnServer)
            {
                string content = "";
                foreach (DictionaryEntry entry in data)
                    content += $"{entry.Key}:\t{entry.Value}\n";
                yield return CoroutineHelper.StartCoroutine(ServerManager.CreateFileAsync(filePath, content));
            }
            else
            {
                using StreamWriter dataStream = File.AppendText(filePath);
                foreach (DictionaryEntry entry in data)
                    dataStream.Write($"{entry.Key}:\t{entry.Value}\n");
            }
        }
    }

    public abstract class USE_Template_DataController : DataController
    {
        public string DataControllerName;
        public ControlLevel_Session_Template sessionLevel;
        public ControlLevel_Task_Template taskLevel;
        public ControlLevel_Trial_Template trialLevel;


        public override void DefineDataController()
        {
            DefineUSETemplateDataController();
        }

        public abstract void DefineUSETemplateDataController();

        public void CreateNewTrialIndexedFile(int trialCount, string filePrefix)
        {
            fileCreated = false;
            fileName = filePrefix + "__" + DataControllerName + "_Trial_" + SessionValues.GetNiceIntegers(4, trialCount) + ".txt";
            StartCoroutine(CreateFile());
        }

        public void CreateNewTaskIndexedFolder(int taskCount, string sessionDataPath, string parentFolder, string suffix)
        {
            folderPath = sessionDataPath + Path.DirectorySeparatorChar + parentFolder + Path.DirectorySeparatorChar + suffix  + SessionValues.GetNiceIntegers(4, taskCount);
                         
        }
        
    }

    public class SessionData : USE_Template_DataController
    {
        public override void DefineUSETemplateDataController()
        {
            DataControllerName = "SessionData";
            AddDatum("SubjectID", () => SessionValues.SubjectID);
            AddDatum("SubjectAge", () => SessionValues.SubjectAge);
            AddDatum("SessionTime", () => SessionValues.FilePrefix);
            AddStateTimingData(sessionLevel);
           // DataControllerHoldsFrames = true;
        }
    }

    public class SerialSentData : USE_Template_DataController
    {
        public SerialPortThreaded sc;

        public override void DefineUSETemplateDataController()
        {
            DataControllerName = "SerialSentData";
            AddDatum("FrameSent\tFrameStart\tSystemTimestamp\tMessage",
                () => sc.BufferToString("sent"));
        }
    }
    public class SerialRecvData : USE_Template_DataController
    {
        public SerialPortThreaded sc;

        public override void DefineUSETemplateDataController()
        {
            DataControllerName = "SerialRecvData";
            AddDatum("FrameRecv\tFrameStart\tSystemTimestamp\tMessage", 
                () => sc.BufferToString("received"));
        }
    }
    // INSERT GAZE DATA CONTROLLER WITH GAZE DATA FIELDS, SESSION FIELDS FROM THE SESSION LEVEL
    // TIME.FRAME 
    // 
    public class BlockData : USE_Template_DataController
    {
        public override void DefineUSETemplateDataController()
        {
            DataControllerName = "BlockData";
            AddDatum("SubjectID", () => SessionValues.SubjectID);
            AddDatum("SubjectAge", () => SessionValues.SubjectAge);
            AddDatum("SessionTime", () => SessionValues.FilePrefix);
            AddDatum("TaskName", () => taskLevel != null ? taskLevel.TaskName : "NoTaskActive");
            AddDatum("BlockCount", () => taskLevel != null ? (taskLevel.BlockCount + 1).ToString() : "NoTaskActive");
            AddDatum("NumRewardPulses_InBlock", () => taskLevel != null ? (taskLevel.NumRewardPulses_InBlock).ToString() : "NoTaskActive");
            AddDatum("NumAbortedTrials_InBlock", () => taskLevel != null ? (taskLevel.NumAbortedTrials_InBlock).ToString() : "NoTaskActive");
        //    DataControllerHoldsFrames = true;
        }
    }

    public class TrialData : USE_Template_DataController
    {
        public override void DefineUSETemplateDataController()
        {
            DataControllerName = "TrialData";
            AddDatum("SubjectID", () => SessionValues.SubjectID); //session level instead of task level
            AddDatum("SubjectAge", () => SessionValues.SubjectAge);
            AddDatum("SessionTime", () => SessionValues.FilePrefix);
            AddDatum("TaskName", () => taskLevel != null? taskLevel.TaskName:"NoTaskActive");
            AddDatum("BlockCount", () => taskLevel != null ? (taskLevel.BlockCount + 1).ToString():"NoTaskActive");
            AddDatum("TrialCount_InTask", () => trialLevel != null ? (trialLevel.TrialCount_InTask + 1).ToString() : "NoTaskActive");
            AddDatum("TrialCount_InBlock", () => trialLevel != null ? (trialLevel.TrialCount_InBlock + 1).ToString() : "NoTaskActive");
            AddDatum("AbortCode", () => trialLevel != null ? (trialLevel.AbortCode).ToString() : "NoTaskActive");
           // DataControllerHoldsFrames = true;
        }
    }

    public class FrameData : USE_Template_DataController
    {
        public override void DefineUSETemplateDataController()
        {
            DataControllerName = "FrameData";
            AddDatum("SubjectID", () => SessionValues.SubjectID);
            AddDatum("SubjectAge", () => SessionValues.SubjectAge);
            AddDatum("SessionTime", () => SessionValues.FilePrefix);
            AddDatum("TaskName", () => taskLevel != null ? taskLevel.TaskName : "NoTaskActive");
            AddDatum("BlockCount", () => taskLevel != null ? (taskLevel.BlockCount + 1).ToString() : "NoTaskActive");
            AddDatum("TrialCount_InTask", () => trialLevel != null ? (trialLevel.TrialCount_InTask + 1).ToString() : "NoTaskActive");
            AddDatum("TrialCount_InBlock", () => trialLevel != null ? (trialLevel.TrialCount_InBlock + 1).ToString() : "NoTaskActive");
            AddDatum("CurrentTrialState", () => trialLevel != null ? trialLevel.CurrentState.StateName : "NoTaskActive");
            AddDatum("Frame", () => Time.frameCount);
            AddDatum("FrameStartUnity", () => Time.time);
        }

        public void AddFlashPanelColumns()
        {
            AddDatum("FlashPanelLStatus", ()=> SessionValues.FlashPanelController.leftLuminanceFactor);
            AddDatum("FlashPanelLStatus", ()=> SessionValues.FlashPanelController.rightLuminanceFactor);
        }
        public void AddEventCodeColumns()
        {
            AddDatum("EventCodes", () => string.Join(",", SessionValues.EventCodeManager.GetBuffer("sent")));
            AddDatum("SplitEventCodes", () => string.Join(",", SessionValues.EventCodeManager.GetBuffer("split")));
            AddDatum("PreSplitEventCodes", () => string.Join(",", SessionValues.EventCodeManager.GetBuffer("presplit")));
        }
    }

    public class GazeData : USE_Template_DataController
    {
        public override void DefineUSETemplateDataController()
        {
            DataControllerName = "GazeData";

            AddDatum("SubjectID", () => SessionValues.SubjectID);
            AddDatum("SubjectAge", () => SessionValues.SubjectAge);
            AddDatum("SessionTime", () => SessionValues.FilePrefix);
            AddDatum("TaskName", () => taskLevel != null ? taskLevel.TaskName : "NoTaskActive");
            AddDatum("BlockCount", () => taskLevel != null ? (taskLevel.BlockCount + 1).ToString() : "NoTaskActive");
            AddDatum("TrialCount_InTask", () => trialLevel != null ? (trialLevel.TrialCount_InTask + 1).ToString() : "NoTaskActive");
            AddDatum("TrialCount_InBlock", () => trialLevel != null ? (trialLevel.TrialCount_InBlock + 1).ToString() : "NoTaskActive");
            AddDatum("CurrentTrialState", () => trialLevel != null ? trialLevel.CurrentState.StateName : "NoTaskActive");
            AddDatum("Frame", () => Time.frameCount);
            AddDatum("FrameStartUnity", () => Time.time);

            AddDatum("LeftPupilValidity", () => SessionValues.TobiiEyeTrackerController.mostRecentGazeSample.leftPupilValidity);
            AddDatum("LeftGazeOriginValidity", () => SessionValues.TobiiEyeTrackerController.mostRecentGazeSample.leftGazeOriginValidity);
            AddDatum("LeftGazePointValidity", () => SessionValues.TobiiEyeTrackerController.mostRecentGazeSample.leftGazePointValidity);
            AddDatum("LeftGazePointOnDisplayArea", () => string.Format("({0:F7}, {1:F7})", SessionValues.TobiiEyeTrackerController.mostRecentGazeSample.leftGazePointOnDisplayArea.x, SessionValues.TobiiEyeTrackerController.mostRecentGazeSample.leftGazePointOnDisplayArea.y)); 
            AddDatum("LeftGazeOriginInUserCoordinateSystem", () => string.Format("({0:F7}, {1:F7}, {2:F7})", SessionValues.TobiiEyeTrackerController.mostRecentGazeSample.leftGazeOriginInUserCoordinateSystem.x, SessionValues.TobiiEyeTrackerController.mostRecentGazeSample.leftGazeOriginInUserCoordinateSystem.y, SessionValues.TobiiEyeTrackerController.mostRecentGazeSample.leftGazeOriginInUserCoordinateSystem.z));
            AddDatum("LeftGazePointInUserCoordinateSystem", () => string.Format("({0:F7}, {1:F7}, {2:F7})", SessionValues.TobiiEyeTrackerController.mostRecentGazeSample.leftGazePointInUserCoordinateSystem.x, SessionValues.TobiiEyeTrackerController.mostRecentGazeSample.leftGazePointInUserCoordinateSystem.y, SessionValues.TobiiEyeTrackerController.mostRecentGazeSample.leftGazePointInUserCoordinateSystem.z));
            AddDatum("LeftGazeOriginInTrackboxCoordinateSystem", () => string.Format("({0:F7}, {1:F7}, {2:F7})", SessionValues.TobiiEyeTrackerController.mostRecentGazeSample.leftGazeOriginInTrackboxCoordinateSystem.x, SessionValues.TobiiEyeTrackerController.mostRecentGazeSample.leftGazeOriginInTrackboxCoordinateSystem.y, SessionValues.TobiiEyeTrackerController.mostRecentGazeSample.leftGazeOriginInTrackboxCoordinateSystem.z));
            AddDatum("LeftPupilDiameter", () => SessionValues.TobiiEyeTrackerController.mostRecentGazeSample.leftPupilDiameter);
            
            AddDatum("RightPupilValidity", () => SessionValues.TobiiEyeTrackerController.mostRecentGazeSample.rightPupilValidity);
            AddDatum("RightGazeOriginValidity", () => SessionValues.TobiiEyeTrackerController.mostRecentGazeSample.rightGazeOriginValidity);
            AddDatum("RightGazePointValidity", () => SessionValues.TobiiEyeTrackerController.mostRecentGazeSample.rightGazePointValidity);
            AddDatum("RightGazePointOnDisplayArea", () => string.Format("({0:F7}, {1:F7})", SessionValues.TobiiEyeTrackerController.mostRecentGazeSample.rightGazePointOnDisplayArea.x, SessionValues.TobiiEyeTrackerController.mostRecentGazeSample.rightGazePointOnDisplayArea.y));
            AddDatum("RightGazeOriginInUserCoordinateSystem", () => string.Format("({0:F7}, {1:F7}, {2:F7})", SessionValues.TobiiEyeTrackerController.mostRecentGazeSample.rightGazeOriginInUserCoordinateSystem.x, SessionValues.TobiiEyeTrackerController.mostRecentGazeSample.rightGazeOriginInUserCoordinateSystem.y, SessionValues.TobiiEyeTrackerController.mostRecentGazeSample.rightGazeOriginInUserCoordinateSystem.z));
            AddDatum("RightGazePointInUserCoordinateSystem", () => string.Format("({0:F7}, {1:F7}, {2:F7})", SessionValues.TobiiEyeTrackerController.mostRecentGazeSample.rightGazePointInUserCoordinateSystem.x, SessionValues.TobiiEyeTrackerController.mostRecentGazeSample.rightGazePointInUserCoordinateSystem.y, SessionValues.TobiiEyeTrackerController.mostRecentGazeSample.rightGazePointInUserCoordinateSystem.z));
            AddDatum("RightGazeOriginInTrackboxCoordinateSystem", () => string.Format("({0:F7}, {1:F7}, {2:F7})", SessionValues.TobiiEyeTrackerController.mostRecentGazeSample.rightGazeOriginInTrackboxCoordinateSystem.x, SessionValues.TobiiEyeTrackerController.mostRecentGazeSample.rightGazeOriginInTrackboxCoordinateSystem.y, SessionValues.TobiiEyeTrackerController.mostRecentGazeSample.rightGazeOriginInTrackboxCoordinateSystem.z));
            AddDatum("RightPupilDiameter", () => SessionValues.TobiiEyeTrackerController.mostRecentGazeSample.rightPupilDiameter);

            AddDatum("TobiiSystemTimeStamp", () => SessionValues.TobiiEyeTrackerController.mostRecentGazeSample.systemTimeStamp);
        }
    }

    public class SessionDataControllers//:MonoBehaviour
    {
        private Dictionary<string, GameObject> DataControllerContainers;
        private GameObject DataContainer;

        public SessionDataControllers(GameObject cont)
        {
            DataControllerContainers = new Dictionary<string, GameObject>();
            DataContainer = cont;
        }

        public DataController InstantiateDataController<T>(string dataControllerName, string path) where T: DataController
        {
            T dc = AddContainer(dataControllerName).AddComponent<T>();
            SpecifyParameters(dc, path);
            return dc;
        }
        public DataController InstantiateDataController<T>(string dataControllerName, string taskName, string path) where T: DataController
        {
            T dc = AddContainer(dataControllerName + "_" + taskName).AddComponent<T>();
            SpecifyParameters(dc, path);
            return dc;
        }

        public SessionData InstantiateSessionData(string path)
        {
            SessionData dc = AddContainer("SessionData").AddComponent<SessionData>();
            SpecifyParameters(dc, path);
            return dc;
        }

        // public SerialSentData InstantiateSerialSentData(string path)
        // {
        // }

        public BlockData InstantiateBlockData(string taskName, string path)
        {
            BlockData dc = AddContainer("BlockData_" + taskName).AddComponent<BlockData>();
            SpecifyParameters(dc, path);
            return dc;
        }

        public TrialData InstantiateTrialData(string taskName, string path)
        {
            TrialData dc = AddContainer("TrialData_" + taskName).AddComponent<TrialData>();
            SpecifyParameters(dc, path);
            return dc;
        }

        public FrameData InstantiateFrameData(string taskName, string path)
        {
            FrameData dc = AddContainer("FrameData_" + taskName).AddComponent<FrameData>();
            SpecifyParameters(dc, path);
            return dc;
        }

        private GameObject AddContainer(string st)
        {
            if (DataContainer.transform.Find(st) == null)
            {
                GameObject go = new GameObject(st);
                go.transform.SetParent(DataContainer.transform);
                DataControllerContainers.Add(st, go);
                return go;
            }
            else
            {
                Debug.Log("Attempted to add data controller container named " + st +
                " to DataControllers but a container with the same name has already been created.");
                return DataContainer.transform.Find(st).gameObject;
                //return null;
            }
        }

        private void SpecifyParameters(DataController dc, string path, bool sm = true)
        {
            dc.folderPath = path;
            dc.DefineManually = sm;
        }

        public void RemoveDataController(string name)
        {
            if (DataControllerContainers.ContainsKey(name))
            {
                GameObject.Destroy(DataControllerContainers[name]);
                DataControllerContainers.Remove(name);
            }
            else
                Debug.LogWarning("Attempted to destroy data controller " + name + ", but this does not exist.");
        }

    }
}