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
            if (!Session.StoreData)
                return;

            Separator = Session.WebBuild ? '/' : Path.DirectorySeparatorChar;
            FolderPath = Session.SessionDataPath + Separator + "SummaryData";

            if(Session.StoringDataOnServer)
                CoroutineHelper.StartCoroutine(ServerManager.CreateFolder(FolderPath));
            else
                Directory.CreateDirectory(FolderPath);
        }

        public static IEnumerator AddTaskRunData(string ConfigName, ControlLevel state, OrderedDictionary data)
        {
            if (!Session.StoreData)
                yield break;
            
            data["Start Time"] = state.StartTimeAbsolute;
            data["Duration"] = state.Duration;

            string filePath = FolderPath + Separator + "Task" + Session.GetNiceIntegers(Session.SessionLevel.taskCount +1) + "_SummaryData_" +ConfigName + ".txt";

            if(Session.StoringDataOnServer)
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

        public override void DefineDataController()
        {
            DefineUSETemplateDataController();
        }

        public abstract void DefineUSETemplateDataController();

        public void CreateNewTrialIndexedFile(int trialCount, string filePrefix)
        {
            fileCreated = false;
            fileName = filePrefix + "__" + DataControllerName + "_Trial_" + Session.GetNiceIntegers(trialCount) + ".txt";
            StartCoroutine(CreateFile());
        }

        public void CreateNewTaskIndexedFolder(int taskCount, string sessionDataPath, string parentFolder, string suffix)
        {
            folderPath = sessionDataPath + Path.DirectorySeparatorChar + parentFolder + Path.DirectorySeparatorChar + suffix  + Session.GetNiceIntegers(taskCount);
            StartCoroutine(CreateFile());

        }

    }

    public class SessionData : USE_Template_DataController
    {
        public override void DefineUSETemplateDataController()
        {
            DataControllerName = "SessionData";
            AddDatum("SubjectID", () => Session.SubjectID);
            AddDatum("SubjectAge", () => Session.SubjectAge);
            AddDatum("SessionTime", () => Session.FilePrefix);
            AddStateTimingData(Session.SessionLevel);
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
            AddDatum("SubjectID", () => Session.SubjectID);
            AddDatum("SubjectAge", () => Session.SubjectAge);
            AddDatum("SessionTime", () => Session.FilePrefix);
            AddDatum("TaskName", () => Session.TaskLevel != null ? Session.TaskLevel.TaskName : "NoTaskActive");
            AddDatum("BlockCount", () => Session.TaskLevel != null ? (Session.TaskLevel.BlockCount + 1).ToString() : "NoTaskActive");
            AddDatum("NumRewardPulses_InBlock", () => Session.TaskLevel != null ? (Session.TaskLevel.NumRewardPulses_InBlock).ToString() : "NoTaskActive");
            AddDatum("NumAbortedTrials_InBlock", () => Session.TaskLevel != null ? (Session.TaskLevel.NumAbortedTrials_InBlock).ToString() : "NoTaskActive");
        //    DataControllerHoldsFrames = true;
        }
    }

    public class TrialData : USE_Template_DataController
    {
        public override void DefineUSETemplateDataController()
        {
            DataControllerName = "TrialData";
            AddDatum("SubjectID", () => Session.SubjectID); //session level instead of task level
            AddDatum("SubjectAge", () => Session.SubjectAge);
            AddDatum("SessionTime", () => Session.FilePrefix);
            AddDatum("TaskName", () => Session.TaskLevel != null? Session.TaskLevel.TaskName:"NoTaskActive");
            AddDatum("BlockCount", () => Session.TaskLevel != null ? (Session.TaskLevel.BlockCount + 1).ToString():"NoTaskActive");
            AddDatum("TrialCount_InTask", () => Session.TrialLevel != null ? (Session.TrialLevel.TrialCount_InTask + 1).ToString() : "NoTaskActive");
            AddDatum("TrialCount_InBlock", () => Session.TrialLevel != null ? (Session.TrialLevel.TrialCount_InBlock + 1).ToString() : "NoTaskActive");
            AddDatum("AbortCode", () => Session.TrialLevel != null ? (Session.TrialLevel.AbortCode).ToString() : "NoTaskActive");
           // DataControllerHoldsFrames = true;
        }
    }

    public class FrameData : USE_Template_DataController
    {
        public override void DefineUSETemplateDataController()
        {
            DataControllerName = "FrameData";
            AddDatum("SubjectID", () => Session.SubjectID);
            AddDatum("SubjectAge", () => Session.SubjectAge);
            AddDatum("SessionTime", () => Session.FilePrefix);
            AddDatum("TaskName", () => Session.TaskLevel != null ? Session.TaskLevel.TaskName : "NoTaskActive");
            AddDatum("BlockCount", () => Session.TaskLevel != null ? (Session.TaskLevel.BlockCount + 1).ToString() : "NoTaskActive");
            AddDatum("TrialCount_InTask", () => Session.TrialLevel != null ? (Session.TrialLevel.TrialCount_InTask + 1).ToString() : "NoTaskActive");
            AddDatum("TrialCount_InBlock", () => Session.TrialLevel != null ? (Session.TrialLevel.TrialCount_InBlock + 1).ToString() : "NoTaskActive");
            AddDatum("CurrentTrialState", () => Session.TrialLevel != null ? Session.TrialLevel.CurrentState?.StateName : "NoTaskActive");
            AddDatum("Frame", () => Time.frameCount);
            AddDatum("FrameStartUnity", () => Time.time);
        }

        public void AddFlashPanelColumns()
        {
            AddDatum("FlashPanelLStatus", ()=> Session.FlashPanelController.leftLuminanceFactor);
            AddDatum("FlashPanelRStatus", ()=> Session.FlashPanelController.rightLuminanceFactor);
        }

        public void AddEventCodeColumns()
        {
            //AddDatum("ReferenceEventCodeSent", () =>
            //{
            //    string dataString = string.Join(",", Session.EventCodeManager.GetBuffer("sent"));
            //    Session.EventCodeManager.sentBuffer.Clear();
            //    return dataString; // Return the data string
            //});

            AddDatum("FrameEventCode", () =>
            {
                string dataString = "";
                if(Session.EventCodeManager.frameEventCodeBuffer.Count > 0)
                    dataString = Session.EventCodeManager.frameEventCodeBuffer[0].ToString();
                //string dataString = string.Join(",", Session.EventCodeManager.frameEventCodeBufferToStore);
                //Session.EventCodeManager.frameEventCodeBufferToStore.Clear();
                return dataString; // Return the data string
            });

            // AddDatum("SplitEventCodes", () => string.Join(",", SessionValues.EventCodeManager.GetBuffer("split")));
            //AddDatum("PreSplitEventCodes", () => string.Join(",", SessionValues.EventCodeManager.GetBuffer("presplit")));
        }
    }

    public class GazeData : USE_Template_DataController
    {
        public override void DefineUSETemplateDataController()
        {
            DataControllerName = "GazeData";

            AddDatum("SubjectID", () => Session.SubjectID);
            AddDatum("SubjectAge", () => Session.SubjectAge);
            AddDatum("SessionTime", () => Session.FilePrefix);
            AddDatum("ParentLevelName", () => Session.SessionLevel?.GetStateFromName("RunTask").ChildLevel != null ? Session.SessionLevel.GetStateFromName("RunTask").ChildLevel.name : "SessionLevel");
            AddDatum("TaskName", () => Session.TaskLevel != null ? Session.TaskLevel.TaskName : "NoTaskActive");
            AddDatum("BlockCount", () => Session.TaskLevel != null ? (Session.TaskLevel.BlockCount + 1).ToString() : "NoTaskActive");
            AddDatum("TrialCount_InTask", () => Session.TrialLevel != null ? (Session.TrialLevel.TrialCount_InTask + 1).ToString() : "NoTaskActive");
            AddDatum("TrialCount_InBlock", () => Session.TrialLevel != null ? (Session.TrialLevel.TrialCount_InBlock + 1).ToString() : "NoTaskActive");
            AddDatum("CurrentTrialState", () => Session.TrialLevel != null ? Session.TrialLevel.CurrentState?.StateName : "NoTaskActive");
            AddDatum("Frame", () => Time.frameCount);
            AddDatum("FrameStartUnity", () => Time.time);

            AddDatum("LeftPupilValidity", () => Session.TobiiEyeTrackerController.mostRecentGazeSample.leftPupilValidity);
            AddDatum("LeftGazeOriginValidity", () => Session.TobiiEyeTrackerController.mostRecentGazeSample.leftGazeOriginValidity);
            AddDatum("LeftGazePointValidity", () => Session.TobiiEyeTrackerController.mostRecentGazeSample.leftGazePointValidity);
            AddDatum("LeftGazePointOnDisplayArea", () => string.Format("({0:F7}, {1:F7})", Session.TobiiEyeTrackerController.mostRecentGazeSample.leftGazePointOnDisplayArea.x, Session.TobiiEyeTrackerController.mostRecentGazeSample.leftGazePointOnDisplayArea.y)); 
            AddDatum("LeftGazeOriginInUserCoordinateSystem", () => string.Format("({0:F7}, {1:F7}, {2:F7})", Session.TobiiEyeTrackerController.mostRecentGazeSample.leftGazeOriginInUserCoordinateSystem.x, Session.TobiiEyeTrackerController.mostRecentGazeSample.leftGazeOriginInUserCoordinateSystem.y, Session.TobiiEyeTrackerController.mostRecentGazeSample.leftGazeOriginInUserCoordinateSystem.z));
            AddDatum("LeftGazePointInUserCoordinateSystem", () => string.Format("({0:F7}, {1:F7}, {2:F7})", Session.TobiiEyeTrackerController.mostRecentGazeSample.leftGazePointInUserCoordinateSystem.x, Session.TobiiEyeTrackerController.mostRecentGazeSample.leftGazePointInUserCoordinateSystem.y, Session.TobiiEyeTrackerController.mostRecentGazeSample.leftGazePointInUserCoordinateSystem.z));
            AddDatum("LeftGazeOriginInTrackboxCoordinateSystem", () => string.Format("({0:F7}, {1:F7}, {2:F7})", Session.TobiiEyeTrackerController.mostRecentGazeSample.leftGazeOriginInTrackboxCoordinateSystem.x, Session.TobiiEyeTrackerController.mostRecentGazeSample.leftGazeOriginInTrackboxCoordinateSystem.y, Session.TobiiEyeTrackerController.mostRecentGazeSample.leftGazeOriginInTrackboxCoordinateSystem.z));
            AddDatum("LeftPupilDiameter", () => Session.TobiiEyeTrackerController.mostRecentGazeSample.leftPupilDiameter);
            
            AddDatum("RightPupilValidity", () => Session.TobiiEyeTrackerController.mostRecentGazeSample.rightPupilValidity);
            AddDatum("RightGazeOriginValidity", () => Session.TobiiEyeTrackerController.mostRecentGazeSample.rightGazeOriginValidity);
            AddDatum("RightGazePointValidity", () => Session.TobiiEyeTrackerController.mostRecentGazeSample.rightGazePointValidity);
            AddDatum("RightGazePointOnDisplayArea", () => string.Format("({0:F7}, {1:F7})", Session.TobiiEyeTrackerController.mostRecentGazeSample.rightGazePointOnDisplayArea.x, Session.TobiiEyeTrackerController.mostRecentGazeSample.rightGazePointOnDisplayArea.y));
            AddDatum("RightGazeOriginInUserCoordinateSystem", () => string.Format("({0:F7}, {1:F7}, {2:F7})", Session.TobiiEyeTrackerController.mostRecentGazeSample.rightGazeOriginInUserCoordinateSystem.x, Session.TobiiEyeTrackerController.mostRecentGazeSample.rightGazeOriginInUserCoordinateSystem.y, Session.TobiiEyeTrackerController.mostRecentGazeSample.rightGazeOriginInUserCoordinateSystem.z));
            AddDatum("RightGazePointInUserCoordinateSystem", () => string.Format("({0:F7}, {1:F7}, {2:F7})", Session.TobiiEyeTrackerController.mostRecentGazeSample.rightGazePointInUserCoordinateSystem.x, Session.TobiiEyeTrackerController.mostRecentGazeSample.rightGazePointInUserCoordinateSystem.y, Session.TobiiEyeTrackerController.mostRecentGazeSample.rightGazePointInUserCoordinateSystem.z));
            AddDatum("RightGazeOriginInTrackboxCoordinateSystem", () => string.Format("({0:F7}, {1:F7}, {2:F7})", Session.TobiiEyeTrackerController.mostRecentGazeSample.rightGazeOriginInTrackboxCoordinateSystem.x, Session.TobiiEyeTrackerController.mostRecentGazeSample.rightGazeOriginInTrackboxCoordinateSystem.y, Session.TobiiEyeTrackerController.mostRecentGazeSample.rightGazeOriginInTrackboxCoordinateSystem.z));
            AddDatum("RightPupilDiameter", () => Session.TobiiEyeTrackerController.mostRecentGazeSample.rightPupilDiameter);

            AddDatum("TobiiSystemTimeStamp", () => Session.TobiiEyeTrackerController.mostRecentGazeSample.systemTimeStamp);
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
                Debug.Log("Attempted to destroy data controller " + name + ", but this does not exist.");
        }

    }
}