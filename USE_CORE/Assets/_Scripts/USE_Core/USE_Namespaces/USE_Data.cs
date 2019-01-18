/*
This software is part of the Unified Suite for Experiments (USE).
Information on USE is available at
http://accl.psy.vanderbilt.edu/resources/analysis-tools/unifiedsuiteforexperiments/

Copyright (c) <2018> <Marcus Watson>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

1) The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
2) If this software is used as a component of a project that leads to publication
(e.g. a paper in a scientific journal or a student thesis), the published work
will give appropriate attribution (e.g. citation) to the following paper:
Watson, M.R., Voloh, B., Thomas, C., Hasan, A., Womelsdorf, T. (2018). USE: An
integrative suite for temporally-precise psychophysical experiments in virtual
environments for human, nonhuman, and artificially intelligent agents. BioRxiv:
http://dx.doi.org/10.1101/434944

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using USE_States;

namespace USE_Data
{
    public interface IDatum
    {
        string Name { get; }
        string ValueAsString { get; }
    }


    public class Datum<T> : IDatum
    {
        public string name;
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

        public Datum(string nm, Func<T> variable)
        {
            name = nm;
            //most variables can use standard tostring method - if not will have to add if statements to build custom string methods
            {
                stringFunc = () => variable().ToString();//toString(()=>variable());
            }
        }
    }

    //public interface IHeldDatum
    //{
    //    int Pos { get; }
    //    string ValueAsString { get; }
    //}

    //public class HeldDatum<T> : IHeldDatum
    //{
    //    public int Pos { get; }
    //    delegate string stringFunction();
    //    stringFunction stringFunc;

    //    public HeldDatum(Func<T> variable, int pos)
    //    {
    //        Pos = pos;
    //        stringFunc = () => variable().ToString();
    //    }

    //    public string ValueAsString
    //    {
    //        get
    //        {
    //            return stringFunc();
    //        }
    //    }
    //}

    public abstract class DataController: MonoBehaviour
    {
        //basic settings
        public bool storeData { get; set; }
        public string folderPath { get; set; }
        public string fileName { get; set; }
        public int capacity { get; set; }

        //list of data to store
        private List<IDatum> data;
        //string to write to file
        private List<string> dataBuffer;
        //records frame when data is appended (to prevent double-writing)
        private int frameChecker = 0;

        //handles case where data needs to be updated next frame (e.g. State duration)
        private bool updateDataNextFrame;
        private bool writeDataNextFrame;
        //private List<IHeldDatum> dataToUpdateNextFrame;
        private List<int> dataToUpdateNextFrame;
        private List<string> heldDataLine;

        private bool Defined = false;

        public DataController(int cap = 100)
        {
            capacity = cap;
            data = new List<IDatum>();
            dataBuffer = new List<string>();
            //dataToUpdateNextFrame = new List<IHeldDatum>();
            dataToUpdateNextFrame = new List<int>();
            heldDataLine = new List<string>();
        }

        //public virtual void Update()
        void Start()
        {
            if (!Defined)
            {
                Defined = true;
                DefineDataController();
                if(storeData)
                {
                    CreateFile();
                }
            }
        }

        private void LateUpdate()
        {
            if (updateDataNextFrame)
            {
                for (int i = 0; i < dataToUpdateNextFrame.Count; i++)
                {
                    //int pos = dataToUpdateNextFrame[i].Pos;
                    int pos = dataToUpdateNextFrame[i];
                    //Debug.Log("1: " + heldDataLine[pos]);
                    heldDataLine[pos] = data[pos].ValueAsString;//dataToUpdateNextFrame[i].ValueAsString;
                    //Debug.Log("2: " + heldDataLine[pos]);
                }
                dataBuffer.Add(String.Join("\t", heldDataLine.ToArray()));
                updateDataNextFrame = false;
                if (dataBuffer.Count == capacity | writeDataNextFrame)
                {
                    WriteData();
                }
                writeDataNextFrame = false;
            }
            if (writeDataNextFrame)
            {
                WriteData();
                writeDataNextFrame = false;
            }
        }


        public abstract void DefineDataController();

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
                if (dataToUpdateNextFrame.Count == 0)
                {
                    dataBuffer.Add(String.Join("\t", currentVals));
                    if (dataBuffer.Count == capacity)
                    {
                        WriteData();
                    }
                }
                else
                {
                    heldDataLine = currentVals.ToList();
                    updateDataNextFrame = true;
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
            if (storeData)
            {
                if (!updateDataNextFrame)
                {
                    if (dataBuffer.Count > 0)
                    {
                        using (StreamWriter dataStream = File.AppendText(folderPath + Path.DirectorySeparatorChar + fileName))
                        {
                            dataStream.Write("\n" + String.Join("\n", dataBuffer.ToArray()));
                        }
                        dataBuffer.Clear();
                    }
                }
                else
                {
                    writeDataNextFrame = true;
                }
            }
        }

        public void AddStateTimingData(ControlLevel level, IEnumerable<string> timingTypes = null)
        {
            foreach (State s in level.ActiveStates)
            {
                if (timingTypes == null)//add all state timing information to data
                {
                    AddDatum(s.StateName + "_StartFrame", () => s.TimingInfo.StartFrame);
                    AddDatum(s.StateName + "_EndFrame", () => s.TimingInfo.EndFrame);
                    AddDatum(s.StateName + "_StartTimeAbsolute", () => s.TimingInfo.StartTimeAbsolute);
                    AddDatum(s.StateName + "_StartTimeRelative", () => s.TimingInfo.StartTimeRelative);
                    AddDatum(s.StateName + "_EndTimeAbsolute", () => s.TimingInfo.EndTimeAbsolute);
                    //dataToUpdateNextFrame.Add(new HeldDatum<float>(() => s.TimingInfo.EndTimeAbsolute, data.Count - 1));
                    dataToUpdateNextFrame.Add(data.Count - 1);
                    AddDatum(s.StateName + "_EndTimeRelative", () => s.TimingInfo.EndTimeRelative);
                    //dataToUpdateNextFrame.Add(new HeldDatum<float>(() => s.TimingInfo.EndTimeRelative, data.Count - 1));
                    dataToUpdateNextFrame.Add(data.Count - 1);
                    AddDatum(s.StateName + "_Duration", () => s.TimingInfo.Duration);
                    //dataToUpdateNextFrame.Add(new HeldDatum<float>(() => s.TimingInfo.Duration, data.Count - 1));
                    dataToUpdateNextFrame.Add(data.Count - 1);
                }
                else //specify which timing information to add
                {
                    foreach(string t in timingTypes)
                    {
                        switch (t)
                        {
                            case "StartFrame":
                                AddDatum(s.StateName + "_StartFrame", () => s.TimingInfo.StartFrame);
                                break;
                            case "EndFrame":
                                AddDatum(s.StateName + "_EndFrame", () => s.TimingInfo.EndFrame);
                                break;
                            case "StartTimeAbsolute":
                                AddDatum(s.StateName + "_StartTimeAbsolute", () => s.TimingInfo.StartTimeAbsolute);
                                break;
                            case "StartTimeRelative":
                                AddDatum(s.StateName + "_StartTimeRelative", () => s.TimingInfo.StartTimeRelative);
                                break;
                            case "EndTimeAbsolute":
                                AddDatum(s.StateName + "_EndTimeAbsolute", () => s.TimingInfo.EndTimeAbsolute);
                                //dataToUpdateNextFrame.Add(new HeldDatum<float>(() => s.TimingInfo.EndTimeAbsolute, data.Count - 1));
                                dataToUpdateNextFrame.Add(data.Count - 1);
                                break;
                            case "EndTimeRelative":
                                AddDatum(s.StateName + "_EndTimeRelative", () => s.TimingInfo.EndTimeRelative);
                                //dataToUpdateNextFrame.Add(new HeldDatum<float>(() => s.TimingInfo.EndTimeRelative, data.Count - 1));
                                dataToUpdateNextFrame.Add(data.Count - 1);
                                break;
                            case "Duration":
                                AddDatum(s.StateName + "_Duration", () => s.TimingInfo.Duration);
                                //dataToUpdateNextFrame.Add(new HeldDatum<float>(() => s.TimingInfo.Duration, data.Count - 1));
                                dataToUpdateNextFrame.Add(data.Count - 1);
                                break;
                            default:
                                Debug.Log("Attempted to add state timing information called \"" + t + "\", but this is not a known timing information type.");
                                break;
                        }
                    }
                }
            }
        }

        void OnApplicationQuit()
        {
            WriteData();
        }

    }
}
