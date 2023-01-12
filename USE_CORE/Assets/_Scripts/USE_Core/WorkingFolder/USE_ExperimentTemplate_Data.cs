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

namespace USE_ExperimentTemplate_Data
{
    public class SummaryData
    {
        private static string folderPath;
        private static bool storeData;

        public static void Init(bool storeData, string folderPath)
        {
            SummaryData.storeData = storeData;
            SummaryData.folderPath = Path.Combine(folderPath, "SummaryData");
            if (storeData)
            {
                System.IO.Directory.CreateDirectory(SummaryData.folderPath);
            }
        }

        public static void AddTaskRunData(string ConfigName, ControlLevel state, OrderedDictionary data)
        {
            if (!storeData)
            {
                return;
            }

            data["Start Time"] = state.StartTimeAbsolute;
            data["Duration"] = state.Duration;

            string filePath = Path.Combine(folderPath, ConfigName + ".txt");
            using (StreamWriter dataStream = File.AppendText(filePath))
            {
                foreach (DictionaryEntry entry in data)
                {
                    dataStream.Write($"{entry.Key}:\t{entry.Value}\n");
                }
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
            fileName = filePrefix + "__" + DataControllerName + "_Trial_" + GetNiceIntegers(4, trialCount);
            CreateFile();
        }

        public void CreateNewTaskIndexedFolder(int taskCount, string sessionDataPath, string suffix)
        {
            folderPath = sessionDataPath + Path.DirectorySeparatorChar + GetNiceIntegers(4, taskCount) +
                         suffix;
        }
        public string GetNiceIntegers(int numDigits, int desiredNum)
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

    public class SessionData : USE_Template_DataController
    {
        public override void DefineUSETemplateDataController()
        {
            DataControllerName = "SessionData";
            AddDatum("SubjectID", () => sessionLevel.SubjectID);
            AddDatum("SessionID", () => sessionLevel.SessionID);
            AddStateTimingData(sessionLevel);
        }
    }

    public class SerialSentData : USE_Template_DataController
    {
        public SerialPortThreaded sc;

        public override void DefineUSETemplateDataController()
        {
            DataControllerName = "SerialSentData";
            AddDatum("FrameSent\tFrameStart\tSystemTimestamp\tMsWait\tMessage",
                () => sc.BufferToString("sent"));
        }
    }
    public class SerialRecvData : USE_Template_DataController
    {
        public SerialPortThreaded sc;

        public override void DefineUSETemplateDataController()
        {
            DataControllerName = "SerialRecvData";
            AddDatum("FrameRecv\tFrameStart\tSystemTimestamp\tMsWait\tMessage", 
                () => sc.BufferToString("received"));
        }
    }

    public class BlockData : USE_Template_DataController
    {
        public override void DefineUSETemplateDataController()
        {
            DataControllerName = "BlockData";
            AddDatum("SubjectID", () => taskLevel.SubjectID);
            AddDatum("SessionID", () => taskLevel.SessionID);
            AddDatum("TaskName", () => taskLevel.TaskName);
            AddDatum("BlockCount", () => taskLevel.BlockCount + 1);
        }
    }

    public class TrialData : USE_Template_DataController
    {
        public override void DefineUSETemplateDataController()
        {
            DataControllerName = "TrialData";
            AddDatum("SubjectID", () => taskLevel.SubjectID);
            AddDatum("SessionID", () => taskLevel.SessionID);
            AddDatum("TaskName", () => taskLevel.TaskName);
            AddDatum("BlockCount", () => taskLevel.BlockCount + 1);
            AddDatum("TrialCount_InTask", () => trialLevel.TrialCount_InTask + 1);
            AddDatum("TrialCount_InBlock", () => trialLevel.TrialCount_InBlock + 1);
            AddDatum("AbortCode", () => trialLevel.AbortCode);
        }
    }

    public class FrameData : USE_Template_DataController
    {
        public override void DefineUSETemplateDataController()
        {
            DataControllerName = "FrameData";
            AddDatum("SubjectID", () => taskLevel.SubjectID);
            AddDatum("SessionID", () => taskLevel.SessionID);
            AddDatum("TaskName", () => taskLevel.TaskName);
            AddDatum("BlockCount", () => taskLevel.BlockCount + 1);
            AddDatum("TrialCount_InTask", () => trialLevel.TrialCount_InTask + 1);
            AddDatum("TrialCount_InBlock", () => trialLevel.TrialCount_InBlock + 1);
            AddDatum("Frame", () => Time.frameCount);
            AddDatum("FrameStartUnity", () => Time.time);
        }

        public void AddEventCodeColumns()
        {
            AddDatum("EventCodes", () => string.Join(",", taskLevel.EventCodeManager.GetBuffer("sent")));
            AddDatum("SplitEventCodes", () => string.Join(",", taskLevel.EventCodeManager.GetBuffer("split")));
            AddDatum("PreSplitEventCodes", () => string.Join(",", taskLevel.EventCodeManager.GetBuffer("presplit")));
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

        public DataController InstantiateDataController<T>(string dataControllerName, bool storeData, string path) where T: DataController
        {
            T dc = AddContainer(dataControllerName).AddComponent<T>();
            SpecifyParameters(dc, storeData, path);
            return dc;
        }
        public DataController InstantiateDataController<T>(string dataControllerName, string taskName, bool storeData, string path) where T: DataController
        {
            T dc = AddContainer(dataControllerName + "_" + taskName).AddComponent<T>();
            SpecifyParameters(dc, storeData, path);
            return dc;
        }

        public SessionData InstantiateSessionData(bool storeData, string path)
        {
            SessionData dc = AddContainer("SessionData").AddComponent<SessionData>();
            SpecifyParameters(dc, storeData, path);
            return dc;
        }

        // public SerialSentData InstantiateSerialSentData(bool storeData, string path)
        // {
        // }

        public BlockData InstantiateBlockData(bool storeData, string taskName, string path)
        {
            BlockData dc = AddContainer("BlockData_" + taskName).AddComponent<BlockData>();
            SpecifyParameters(dc, storeData, path);
            return dc;
        }

        public TrialData InstantiateTrialData(bool storeData, string taskName, string path)
        {
            TrialData dc = AddContainer("TrialData_" + taskName).AddComponent<TrialData>();
            SpecifyParameters(dc, storeData, path);
            return dc;
        }

        public FrameData InstantiateFrameData(bool storeData, string taskName, string path)
        {
            FrameData dc = AddContainer("FrameData_" + taskName).AddComponent<FrameData>();
            SpecifyParameters(dc, storeData, path);
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
                Debug.LogError("Attempted to add data controller container named " + st +
                " to DataControllers but a container with the same name has already been created.");
                return null;
            }
        }

        private void SpecifyParameters(DataController dc, bool storeData, string path, bool sm = true)
        {
            dc.storeData = storeData;
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