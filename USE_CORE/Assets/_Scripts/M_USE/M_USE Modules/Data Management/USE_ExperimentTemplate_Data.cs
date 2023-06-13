using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections;
using System;
using System.IO;
using UnityEngine;
using USE_Data;
using USE_States;
using USE_ExperimentTemplate_Session;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Trial;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Data.SqlClient;
//using MySql.Data.MySqlClient;

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
                return;
            
            data["Start Time"] = state.StartTimeAbsolute;
            data["Duration"] = state.Duration;

            string filePath = Path.Combine(folderPath, ConfigName + ".txt");
            using (StreamWriter dataStream = File.AppendText(filePath))
            {
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


        private readonly string ConnectionString = "server=localhost;port=3306;database=USE_Test;uid=MUSE_User;password=Dziadziu21!;";

        public SqlConnection Connection
        {
            get
            {
                return new SqlConnection(ConnectionString);
            }
        }



        //Dict to hold all the sql data types, so they will be correct for sql command
        Dictionary<string, string> TypeDict_SQL = new Dictionary<string, string>()
        {
            { "Boolean", "BIT" },
            { "Byte", "TINYINT" },
            { "Short", "SMALLINT" },
            { "Int32", "INT" },
            { "Int", "INT" },
            { "Long", "BIGINT" },
            { "Float", "REAL" },
            { "Double", "FLOAT" },
            { "Decimal", "DECIMAL" },
            { "Single", "REAL" },
            { "DateTime", "DATETIME" },
            { "String", "NVARCHAR(MAX)" }
        };

        public string GetSQLType(IDatum datum)
        {
            var full = datum.GetType().ToString();
            string[] split = Regex.Split(full, @"\b" + "System." + @"\b");
            string dataType = new string(split[split.Length - 1].Where(c => c != ']').ToArray());
            return TypeDict_SQL[dataType];
        }
        public string GetSQLType(string typeName) //Overload for if already have the 1 word sys type string:
        {
            return TypeDict_SQL[typeName];
        }

        public void LogDataController()
        {
            Debug.Log("DATA CONTROLLER NAME: " + name);
            Debug.Log("---------------------------");
            foreach (var datum in data)
            {
                Debug.Log("Name: " + datum.Name + " | Type: " + GetSQLType(datum));
                Debug.Log("---------------------------");
            }
        }

        public void TestConnectionToDB()
        {
            using (var conn = Connection)
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT * FROM Task;";

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Debug.Log((int)reader["Id"]);
                            Debug.Log((reader.GetString(reader.GetOrdinal("Name"))));
                        }
                    }
                }
            }
        }

        //public bool DoesSQLTableExist()
        //{
        //    bool tableExists = new bool();
        //    using (SqlConnection conn = Connection)
        //    {
        //        conn.Open();
        //        using (SqlCommand cmd = conn.CreateCommand())
        //        {
        //            cmd.CommandText = $@"SELECT * FROM {name};";
        //            SqlDataReader reader = cmd.ExecuteReader();
        //            tableExists = reader.Read();
        //            reader.Close();
        //        }
        //        conn.Close();
        //        return tableExists;
        //    }
        //}

        public void CreateTable_SQL() //currently using the datacontrollers name as the TableName
        {
            //if (!DoesSQLTableExist())
            //{
            //    using (SqlConnection conn = Connection)
            //    {
            //        conn.Open();
            //        using (SqlCommand cmd = conn.CreateCommand())
            //        {
            //            string sqlString = $"CREATE TABLE {name} (Id INT PRIMARY KEY";
            //            foreach (IDatum datum in data)
            //            {
            //                var sqlType = GetSQLType(datum);
            //                if (sqlType == null)
            //                    Debug.Log(datum.Name + " Does not have a matching SQL Type in the dictionary");
            //                else
            //                    sqlString += $", {datum.Name} {sqlType}";
            //            }
            //            sqlString += ")";
            //            cmd.CommandText = sqlString;
            //            cmd.ExecuteNonQuery();
            //        }
            //        conn.Close();
            //    }
            //}
        }

        public void AddData_SQL()
        {
            ////First check if table exists in DB:
            //if (DoesSQLTableExist())
            //{
            //    using (SqlConnection conn = Connection)
            //    {
            //        conn.Open();
            //        DataTable dataTable = new DataTable();

            //        //Add all columns first:
            //        foreach (var datum in data)
            //            dataTable.Columns.Add(datum.Name);

            //        //Then add all rows:
            //        foreach (var datum in data)
            //            dataTable.Rows.Add(datum.ValueAsString);

            //        using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn))
            //        {
            //            bulkCopy.DestinationTableName = name;
            //            bulkCopy.WriteToServer(dataTable);
            //        }
            //    }
            //}
            //else
            //    Debug.Log($"There is no SQL table in the database with the name {name}");
        }



        public override void DefineDataController()
        {
            DefineUSETemplateDataController();
        }

        public abstract void DefineUSETemplateDataController();

        public void CreateNewTrialIndexedFile(int trialCount, string filePrefix)
        {
            fileName = filePrefix + "__" + DataControllerName + "_Trial_" + GetNiceIntegers(4, trialCount) + ".txt";
            CreateFile();
        }

        public void CreateNewTaskIndexedFolder(int taskCount, string sessionDataPath, string parentFolder, string suffix)
        {
            folderPath = sessionDataPath + Path.DirectorySeparatorChar + parentFolder + Path.DirectorySeparatorChar + GetNiceIntegers(4, taskCount) + "_" +
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
            AddDatum("SubjectID", () => sessionLevel.SubjectID);
            AddDatum("SessionID", () => sessionLevel.SessionID);
            AddDatum("TaskName", () => taskLevel != null ? taskLevel.TaskName : "NoTaskActive");
            AddDatum("BlockCount", () => taskLevel != null ? (taskLevel.BlockCount + 1).ToString() : "NoTaskActive");
        }
    }

    public class TrialData : USE_Template_DataController
    {
        public override void DefineUSETemplateDataController()
        {
            DataControllerName = "TrialData";
            AddDatum("SubjectID", () => sessionLevel.SubjectID); //session level instead of task level
            AddDatum("SessionID", () => sessionLevel.SessionID);
            AddDatum("TaskName", () => taskLevel != null? taskLevel.TaskName:"NoTaskActive");
            AddDatum("BlockCount", () => taskLevel != null ? (taskLevel.BlockCount + 1).ToString():"NoTaskActive");
            AddDatum("TrialCount_InTask", () => trialLevel != null ? (trialLevel.TrialCount_InTask + 1).ToString() : "NoTaskActive");
            AddDatum("TrialCount_InBlock", () => trialLevel != null ? (trialLevel.TrialCount_InBlock + 1).ToString() : "NoTaskActive");
            AddDatum("AbortCode", () => trialLevel != null ? (trialLevel.AbortCode).ToString() : "NoTaskActive");
        }
    }

    public class FrameData : USE_Template_DataController
    {
        public override void DefineUSETemplateDataController()
        {
            DataControllerName = "FrameData";
            AddDatum("SubjectID", () => sessionLevel.SubjectID);
            AddDatum("SessionID", () => sessionLevel.SessionID);
            AddDatum("TaskName", () => taskLevel != null ? taskLevel.TaskName : "NoTaskActive");
            AddDatum("BlockCount", () => taskLevel != null ? (taskLevel.BlockCount + 1).ToString() : "NoTaskActive");
            AddDatum("TrialCount_InTask", () => trialLevel != null ? (trialLevel.TrialCount_InTask + 1).ToString() : "NoTaskActive");
            AddDatum("TrialCount_InBlock", () => trialLevel != null ? (trialLevel.TrialCount_InBlock + 1).ToString() : "NoTaskActive");
            AddDatum("CurrentTrialState", () => trialLevel != null ? trialLevel.CurrentState.StateName : "NoTaskActive");
            AddDatum("Frame", () => Time.frameCount);
            AddDatum("FrameStartUnity", () => Time.time);
        }

        public void AddEventCodeColumns()
        {
            AddDatum("EventCodes", () => taskLevel != null ? string.Join(",", taskLevel.EventCodeManager.GetBuffer("sent")) : "NoTaskActive");
            AddDatum("SplitEventCodes", () => taskLevel != null ? string.Join(",", taskLevel.EventCodeManager.GetBuffer("split")) : "NoTaskActive");
            AddDatum("PreSplitEventCodes", () => taskLevel != null ? string.Join(",", taskLevel.EventCodeManager.GetBuffer("presplit")) : "NoTaskActive");
        }
    }

    public class GazeData : USE_Template_DataController
    {
        public override void DefineUSETemplateDataController()
        {
            DataControllerName = "GazeData";
            AddDatum("SubjectID", () => sessionLevel.SubjectID);
            AddDatum("SessionID", () => sessionLevel.SessionID);
            AddDatum("TaskName", () => taskLevel != null ? taskLevel.TaskName : "NoTaskActive");
            AddDatum("BlockCount", () => taskLevel != null ? (taskLevel.BlockCount + 1).ToString() : "NoTaskActive");
            AddDatum("TrialCount_InTask", () => trialLevel != null ? (trialLevel.TrialCount_InTask + 1).ToString() : "NoTaskActive");
            AddDatum("TrialCount_InBlock", () => trialLevel != null ? (trialLevel.TrialCount_InBlock + 1).ToString() : "NoTaskActive");
            AddDatum("CurrentTrialState", () => trialLevel != null ? trialLevel.CurrentState.StateName : "NoTaskActive");
            AddDatum("Frame", () => Time.frameCount);
            AddDatum("FrameStartUnity", () => Time.time);

            AddDatum("LeftPupilValidity", () => trialLevel.TobiiGazeSample.leftPupilValidity);
            AddDatum("LeftGazeOriginValidity", () => trialLevel.TobiiGazeSample.leftGazeOriginValidity);
            AddDatum("LeftGazePointValidity", () => trialLevel.TobiiGazeSample.leftGazePointValidity);
            AddDatum("LeftGazePointOnDisplayArea", () => trialLevel.TobiiGazeSample.leftGazePointOnDisplayArea);
            AddDatum("LeftGazeOriginInUserCoordinateSystem", () => trialLevel.TobiiGazeSample.leftGazeOriginInUserCoordinateSystem);
            AddDatum("LeftGazePointInUserCoordinateSystem", () => trialLevel.TobiiGazeSample.leftGazePointInUserCoordinateSystem);
            AddDatum("LeftGazeOriginInTrackboxCoordinateSystem", () => trialLevel.TobiiGazeSample.leftGazeOriginInTrackboxCoordinateSystem);
            AddDatum("LeftPupilDiameter", () => trialLevel.TobiiGazeSample.leftPupilDiameter);
            
            AddDatum("RightPupilValidity", () => trialLevel.TobiiGazeSample.rightPupilValidity);
            AddDatum("RightGazeOriginValidity", () => trialLevel.TobiiGazeSample.rightGazeOriginValidity);
            AddDatum("RightGazePointValidity", () => trialLevel.TobiiGazeSample.rightGazePointValidity);
            AddDatum("RightGazePointOnDisplayArea", () => trialLevel.TobiiGazeSample.rightGazePointOnDisplayArea);
            AddDatum("RightGazeOriginInUserCoordinateSystem", () => trialLevel.TobiiGazeSample.rightGazeOriginInUserCoordinateSystem);
            AddDatum("RightGazePointInUserCoordinateSystem", () => trialLevel.TobiiGazeSample.rightGazePointInUserCoordinateSystem);
            AddDatum("RightGazeOriginInTrackboxCoordinateSystem", () => trialLevel.TobiiGazeSample.rightGazeOriginInTrackboxCoordinateSystem);
            AddDatum("RightPupilDiameter", () => trialLevel.TobiiGazeSample.rightPupilDiameter);

            AddDatum("TobiiSystemTimeStamp", () => trialLevel.TobiiGazeSample.systemTimeStamp);
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