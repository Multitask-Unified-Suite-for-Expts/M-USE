using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace USE_Data
{
    public interface IDatum
    {
        string Name { get; }
        string ValueAsString { get; }
    }


    public class Datum<T> : IDatum
    {
        readonly string name;
        public string Name { get { return name; } }

        delegate string stringFunction();
        stringFunction stringFunc;

        public string ValueAsString
        {
            get
            {
                return stringFunc();
            }
        }

        public Datum(string name, Func<T> variable)
        {
            this.name = name;
            //most variables can use standard tostring method - if not will have to add if statements to build custom string methods
            {
                stringFunc = () => variable().ToString();//toString(()=>variable());
            }
        }
    }



    public class DataController
    {

        private bool storeData;

        private string folderPath;
        public string fileName { get; set; }
        private int capacity;
        private List<IDatum> data;
        private List<string> dataBuffer;
        private int frameChecker = 0;


        public DataController(bool store, int cap, string path)
        {
            capacity = cap;
            dataBuffer = new List<string>();
            dataBuffer.Capacity = cap;
            folderPath = path;
            data = new List<IDatum>();
            storeData = store;
        }
        //more overloads may need to be added here in order to define new data types
        public void AddDatum(string name, Func<int> variable)
        {
            IDatum datum = new Datum<int>(name, ()=>variable());
            data.Add(datum);
        }
        public void AddDatum(string name, Func<int?> variable)
        {
            IDatum datum = new Datum<int?>(name, () => variable());
            data.Add(datum);
        }
        public void AddDatum(string name, Func<float> variable)
        {
            IDatum datum = new Datum<float>(name, () => variable());
            data.Add(datum);
        }
        public void AddDatum(string name, Func<float?> variable)
        {
            IDatum datum = new Datum<float?>(name, () => variable());
            data.Add(datum);
        }
        public void AddDatum(string name, Func<string> variable)
        {
            IDatum datum = new Datum<string>(name, () => variable());
            data.Add(datum);
        }
        public void AddDatum(string name, Func<bool> variable)
        {
            IDatum datum = new Datum<bool>(name, () => variable());
            data.Add(datum);
        }
        public void AddDatum(string name, Func<bool?> variable)
        {
            IDatum datum = new Datum<bool?>(name, () => variable());
            data.Add(datum);
        }
        public void AddDatum(string name, Func<Int64> variable)
        {
            IDatum datum = new Datum<Int64>(name, () => variable());
            data.Add(datum);
        }
        public void AddDatum(string name, Func<Int64?> variable)
        {
            IDatum datum = new Datum<Int64?>(name, () => variable());
            data.Add(datum);
        }
        public void AddDatum(string name, Func<Vector2> variable)
        {
            IDatum datum = new Datum<Vector2>(name, () => variable());
            data.Add(datum);
        }
        public void AddDatum(string name, Func<Vector2?> variable)
        {
            IDatum datum = new Datum<Vector2?>(name, () => variable());
            data.Add(datum);
        }
        public void AddDatum(string name, Func<Vector3> variable)
        {
            IDatum datum = new Datum<Vector3>(name, () => variable());
            data.Add(datum);
        }
        public void AddDatum(string name, Func<Vector3?> variable)
        {
            IDatum datum = new Datum<Vector3?>(name, () => variable());
            data.Add(datum);
        }
        public void AddDatum(string name, Func<Vector4> variable)
        {
            IDatum datum = new Datum<Vector4>(name, () => variable());
            data.Add(datum);
        }
        public void AddDatum(string name, Func<Vector4?> variable)
        {
            IDatum datum = new Datum<Vector4?>(name, () => variable());
            data.Add(datum);
        }

        public void AppendData()
        {
            if (storeData && Time.frameCount > frameChecker)
            {
                string[] currentVals = new string[data.Count];
                for (int i = 0; i < data.Count; i++)
                {
                    currentVals[i] = data[i].ValueAsString;
                }
                dataBuffer.Add(String.Join("\t", currentVals));
                if (dataBuffer.Count == capacity)
                {
                    WriteData();
                }
                frameChecker = Time.frameCount;
            }
        }

        public void CreateFile()
        {
            if (storeData)
            {
                Directory.CreateDirectory(folderPath);
                string titleString = "";
                for (int i = 0; i < data.Count; i++)
                {
                    if (i > 0)
                    {
                        titleString = titleString + "\t";
                    }
                    titleString = titleString + data[i].Name;
                }
                using (StreamWriter dataStream = File.CreateText(folderPath + Path.DirectorySeparatorChar + fileName))
                {
                    dataStream.Write(titleString);
                }
            }
        }

        public void WriteData()
        {
            if (storeData && dataBuffer.Count > 0)
            {
                using (StreamWriter dataStream = File.AppendText(folderPath + Path.DirectorySeparatorChar + fileName))
                {
                    dataStream.Write("\n" + String.Join("\n", dataBuffer.ToArray()));
                }
                dataBuffer.Clear();
            }
        }

        void OnApplicationQuit()
        {
            WriteData();
        }

    }
}
