using System.Collections.Generic;
using UnityEngine;
using USE_Data;
using USE_ExperimentTemplate;

namespace USE_ExperimentTemplate_Data
{
    public class SessionData : DataController
    {
        public ControlLevel_Session_Template sessionLevel;

        public override void DefineDataController()
        {
            AddDatum("SubjectID", () => sessionLevel.SubjectID);
            AddDatum("SessionID", () => sessionLevel.SessionID);
            AddStateTimingData(sessionLevel);
        }
    }

    public class BlockData : DataController
    {
        public ControlLevel_Task_Template taskLevel;

        public override void DefineDataController()
        {
            AddDatum("SubjectID", () => taskLevel.SubjectID);
            AddDatum("SessionID", () => taskLevel.SessionID);
            AddDatum("TaskName", () => taskLevel.TaskName);
            AddDatum("BlockCount", () => taskLevel.BlockCount + 1);
        }
    }

    public class TrialData : DataController
    {
        public ControlLevel_Task_Template taskLevel;
        public ControlLevel_Trial_Template trialLevel;

        public override void DefineDataController()
        {
            AddDatum("SubjectID", () => taskLevel.SubjectID);
            AddDatum("SessionID", () => taskLevel.SessionID);
            AddDatum("TaskName", () => taskLevel.TaskName);
            AddDatum("BlockCount", () => taskLevel.BlockCount + 1);
            AddDatum("TrialCount_InTask", () => trialLevel.TrialCount_InTask + 1);
            AddDatum("TrialCount_InBlock", () => trialLevel.TrialCount_InBlock + 1);
            AddDatum("AbortCode", () => trialLevel.AbortCode);
        }
    }

    public class FrameData : DataController
    {
        public ControlLevel_Task_Template taskLevel;
        public ControlLevel_Trial_Template trialLevel;

        public override void DefineDataController()
        {
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

        public DataController InstantiateDataController(string str, bool storeData, string path)
        {
            DataController dc = AddContainer(str).AddComponent<DataController>();
            SpecifyParameters(dc, storeData, path);
            return dc;
        }

        public SessionData InstantiateSessionData(bool storeData, string path)
        {
            SessionData dc = AddContainer("SessionData").AddComponent<SessionData>();
            SpecifyParameters(dc, storeData, path);
            return dc;
        }

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