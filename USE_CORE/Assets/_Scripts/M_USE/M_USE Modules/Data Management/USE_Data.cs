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
DOI https://doi.org/10.1101/434944

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
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
		readonly string name;
		public string Name { get { return name; } }

		delegate string stringFunction();
		stringFunction stringFunc;

		public string ValueAsString
		{
			get
			{
				string v = null;
				try
				{
					v = stringFunc();
				}
				catch (Exception e)
				{
                    //Debug.Log("Null error, name: " + name + " variable: " + v); //may want to uncomment
                    //Debug.LogError("Null error, name: " + this.name + " variable: " + v);
					//throw e;
				}
				return v;

				// return stringFunc();

			}
		}

		public Datum(string name, Func<T> variable)
		{
			this.name = name;
			//most variables can use standard tostring method - if not will have to add if statements to build custom string methods
			{
				stringFunc = () => {
					var v = variable();
					string returnString = "";
					if (v == null)
					{
						// Debug.LogWarning("Null value returned for Datum, name: " + this.name);
						returnString = "null";
					}
					else if (typeof(T) == typeof(List<int>))
					{
						returnString = "(";
						List<int> varList = variable() as List<int>;
						for(int index = 0; index < varList.Count; index++)
						{
							returnString += varList[index].ToString();
							if (index < varList.Count - 1)
								returnString += ", ";
							else
								returnString += ")";
						}
					}
					else
						returnString = v.ToString();//toString(()=>variable());

					return returnString;
				};

				//stringFunc = () => variable().ToString();//toString(()=>variable());
			}
		}
	}

	public interface IHeldDatum
	{
		string Name { get; }
		int Pos { get; }
		string ValueAsString { get; }
	}

	public class HeldDatum<T> : IHeldDatum
	{
		public string Name{ get; }
		public int Pos { get; }
		delegate string stringFunction();
		stringFunction stringFunc;

		public HeldDatum(string name, Func<T> variable, int pos)
		{
			Name = name;
			Pos = pos;
			stringFunc = () => variable().ToString();
		}

		public string ValueAsString
		{
			get
			{
				return stringFunc();
			}
		}
	}

	/// <summary>
	/// The base DataController class.
	/// </summary>
	public abstract class DataController : MonoBehaviour
	{
		//basic settings
		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="T:USE_Data.DataController"/> will write files to the hard drive.
		/// </summary>
		/// <value><c>true</c> if store data; otherwise, <c>false</c>.</value>
		public bool storeData { get; set; }
		/// <summary>
		/// Gets or sets the folder path where data files will be written.
		/// </summary>
		/// <value>The folder path.</value>
		public string folderPath { get; set; }
		/// <summary>
		/// Gets or sets the current filename.
		/// </summary>
		/// <value>The name of the file.</value>
		public string fileName { get; set; }
		/// <summary>
		/// The maximum number of lines of data kept in the active buffer (if exceeded, file writing is automatically triggered).
		/// </summary>
		/// <value>The capacity.</value>
		public int capacity { get; set; }

		//list of data to store
		public List<IDatum> data;
		//string to write to file
		public List<string> dataBuffer;
		//records frame when data is appended (to prevent double-writing)
		private int frameChecker = 0;

		//handles case where data needs to be updated next frame (e.g. State duration)
		private bool appendDataToBufferNextFrame;
		private bool appendDataToFileNextFrame;
		private List<IHeldDatum> dataToUpdateNextFrame;
		private List<string> PreviousFrameHeldDataValues;
		public bool DataControllerHoldsFrames;

		private bool Defined = false;
		public bool DefineManually;

		public string fileHeaders;
		public bool fileCreated;



		public void InitDataController(int cap = 10000)
		{
			capacity = cap;
			data = new List<IDatum>();
			dataBuffer = new List<string>();
			dataToUpdateNextFrame = new List<IHeldDatum>();
			PreviousFrameHeldDataValues = new List<string>();
		}

		void Start()
		{
			if (!DefineManually)
				OnStart();
		}

		public void ManuallyDefine(int cap = 100)
		{
		//everything in Start() should be triggered by init screen Confirm button press
			if (!Defined)
			{
				Defined = true;
				DefineDataController();
			}
		}

		void OnStart()
		{
			//everything in Start() should be triggered by init screen Confirm button press
			if (!Defined)
			{
				Defined = true;
				InitDataController();
				DefineDataController();
				if (storeData)
				{
					StartCoroutine(CreateFile());
				}
			}
			if (appendDataToBufferNextFrame)
			{
				for (int i = 0; i < dataToUpdateNextFrame.Count; i++)
				{
					PreviousFrameHeldDataValues[i] = dataToUpdateNextFrame[i].ValueAsString;
				}
				dataBuffer.Add(string.Join("\t", PreviousFrameHeldDataValues.ToArray()));
                appendDataToBufferNextFrame = false;
				if (dataBuffer.Count == capacity || appendDataToFileNextFrame)
				{
					StartCoroutine(AppendDataToFile());
				}
				appendDataToFileNextFrame = false;
			}
		}

		public void UpdateData()
		{
			if (appendDataToBufferNextFrame)
            {
                for (int i = 0; i < dataToUpdateNextFrame.Count; i++)
				{
					Debug.LogWarning(Time.frameCount + " Appended data = " + dataToUpdateNextFrame[i].Name + dataToUpdateNextFrame[i].ValueAsString);
					PreviousFrameHeldDataValues[dataToUpdateNextFrame[i].Pos] = dataToUpdateNextFrame[i].ValueAsString;
				}
				dataBuffer.Add(string.Join("\t", PreviousFrameHeldDataValues.ToArray()));
				if(name.ToLower().Contains("trial"))
					Debug.LogWarning("DATA ADDED TO BUFFER! SETTING AppendDataToBufferNextFrame to FALSE! " + Time.frameCount);
                appendDataToBufferNextFrame = false;
			}
			if (dataBuffer.Count == capacity || appendDataToFileNextFrame)
			{
				appendDataToFileNextFrame = false;
				StartCoroutine(AppendDataToFile());
			}
		}

		/// <summary>
		/// Defines the data controller.
		/// </summary>
		public abstract void DefineDataController();

		/// <summary>
		/// Adds the datum.
		/// </summary>
		/// <param name="name">Name of column in output file.</param>
		/// <param name="variable">Variable to be tracked.</param>
		/// <overloads>There are many overloads to this method, one for each possible data type. Add more if needed.</overloads>
		public void AddDatum(string name, Func<int> variable)
		{
			IDatum datum = new Datum<int>(name, () => variable());
			data.Add(datum);
		}
		/// <summary>
		/// Adds the datum.
		/// </summary>
		/// <param name="name">Name of column in output file.</param>
		/// <param name="variable">Variable to be tracked.</param>
		/// <overloads>There are many overloads to this method, one for each possible data type. Add more if needed.</overloads>
		public void AddDatum(string name, Func<int?> variable)
		{
			IDatum datum = new Datum<int?>(name, () => variable());
			data.Add(datum);
		}
		/// <summary>
		/// Adds the datum.
		/// </summary>
		/// <param name="name">Name of column in output file.</param>
		/// <param name="variable">Variable to be tracked.</param>
		/// <overloads>There are many overloads to this method, one for each possible data type. Add more if needed.</overloads>
		public void AddDatum(string name, Func<float> variable)
		{
			IDatum datum = new Datum<float>(name, () => variable());
			data.Add(datum);
		}
		/// <summary>
		/// Adds the datum.
		/// </summary>
		/// <param name="name">Name of column in output file.</param>
		/// <param name="variable">Variable to be tracked.</param>
		/// <overloads>There are many overloads to this method, one for each possible data type. Add more if needed.</overloads>
		public void AddDatum(string name, Func<float?> variable)
		{
			IDatum datum = new Datum<float?>(name, () => variable());
			data.Add(datum);
		}
		/// <summary>
		/// Adds the datum.
		/// </summary>
		/// <param name="name">Name of column in output file.</param>
		/// <param name="variable">Variable to be tracked.</param>
		/// <overloads>There are many overloads to this method, one for each possible data type. Add more if needed.</overloads>
		public void AddDatum(string name, Func<string> variable)
		{
			IDatum datum = new Datum<string>(name, () => variable());
			data.Add(datum);
		}
		/// <summary>
		/// Adds the datum.
		/// </summary>
		/// <param name="name">Name of column in output file.</param>
		/// <param name="variable">Variable to be tracked.</param>
		/// <overloads>There are many overloads to this method, one for each possible data type. Add more if needed.</overloads>
		public void AddDatum(string name, Func<bool> variable)
		{
			IDatum datum = new Datum<bool>(name, () => variable());
			data.Add(datum);
		}
		/// <summary>
		/// Adds the datum.
		/// </summary>
		/// <param name="name">Name of column in output file.</param>
		/// <param name="variable">Variable to be tracked.</param>
		/// <overloads>There are many overloads to this method, one for each possible data type. Add more if needed.</overloads>
		public void AddDatum(string name, Func<bool?> variable)
		{
			IDatum datum = new Datum<bool?>(name, () => variable());
			data.Add(datum);
		}
		/// <summary>
		/// Adds the datum.
		/// </summary>
		/// <param name="name">Name of column in output file.</param>
		/// <param name="variable">Variable to be tracked.</param>
		/// <overloads>There are many overloads to this method, one for each possible data type. Add more if needed.</overloads>
		public void AddDatum(string name, Func<Int64> variable)
		{
			IDatum datum = new Datum<Int64>(name, () => variable());
			data.Add(datum);
		}
		/// <summary>
		/// Adds the datum.
		/// </summary>
		/// <param name="name">Name of column in output file.</param>
		/// <param name="variable">Variable to be tracked.</param>
		/// <overloads>There are many overloads to this method, one for each possible data type. Add more if needed.</overloads>
		public void AddDatum(string name, Func<Int64?> variable)
		{
			IDatum datum = new Datum<Int64?>(name, () => variable());
			data.Add(datum);
		}
		/// <summary>
		/// Adds the datum.
		/// </summary>
		/// <param name="name">Name of column in output file.</param>
		/// <param name="variable">Variable to be tracked.</param>
		/// <overloads>There are many overloads to this method, one for each possible data type. Add more if needed.</overloads>
		public void AddDatum(string name, Func<Vector2> variable)
		{
			IDatum datum = new Datum<Vector2>(name, () => variable());
			data.Add(datum);
		}
		/// <summary>
		/// Adds the datum.
		/// </summary>
		/// <param name="name">Name of column in output file.</param>
		/// <param name="variable">Variable to be tracked.</param>
		/// <overloads>There are many overloads to this method, one for each possible data type. Add more if needed.</overloads>
		public void AddDatum(string name, Func<Vector2?> variable)
		{
			IDatum datum = new Datum<Vector2?>(name, () => variable());
			data.Add(datum);
		}
		/// <summary>
		/// Adds the datum.
		/// </summary>
		/// <param name="name">Name of column in output file.</param>
		/// <param name="variable">Variable to be tracked.</param>
		/// <overloads>There are many overloads to this method, one for each possible data type. Add more if needed.</overloads>
		public void AddDatum(string name, Func<Vector3> variable)
		{
			IDatum datum = new Datum<Vector3>(name, () => variable());
			data.Add(datum);
		}
		/// <summary>
		/// Adds the datum.
		/// </summary>
		/// <param name="name">Name of column in output file.</param>
		/// <param name="variable">Variable to be tracked.</param>
		/// <overloads>There are many overloads to this method, one for each possible data type. Add more if needed.</overloads>
		public void AddDatum(string name, Func<Vector3?> variable)
		{
			IDatum datum = new Datum<Vector3?>(name, () => variable());
			data.Add(datum);
		}
		/// <summary>
		/// Adds the datum.
		/// </summary>
		/// <param name="name">Name of column in output file.</param>
		/// <param name="variable">Variable to be tracked.</param>
		/// <overloads>There are many overloads to this method, one for each possible data type. Add more if needed.</overloads>
		public void AddDatum(string name, Func<Vector4> variable)
		{
			IDatum datum = new Datum<Vector4>(name, () => variable());
			data.Add(datum);
		}
		/// <summary>
		/// Adds the datum.
		/// </summary>
		/// <param name="name">Name of column in output file.</param>
		/// <param name="variable">Variable to be tracked.</param>
		/// <overloads>There are many overloads to this method, one for each possible data type. Add more if needed.</overloads>
		public void AddDatum(string name, Func<Vector4?> variable)
		{
			IDatum datum = new Datum<Vector4?>(name, () => variable());
			data.Add(datum);
		}

		public void AddDatum(string name, Func<List<int>> variable)
		{
			IDatum datum = new Datum<List<int>>(name, () => variable());
			data.Add(datum);
		}
		public void AddDatum(string name, Func<List<int?>> variable)
		{
			IDatum datum = new Datum<List<int?>>(name, () => variable());
			data.Add(datum);
		}
		public void AddDatum(string name, Func<List<Int64>> variable)
		{
			IDatum datum = new Datum<List<Int64>>(name, () => variable());
			data.Add(datum);
		}
		public void AddDatum(string name, Func<List<Int64?>> variable)
		{
			IDatum datum = new Datum<List<Int64?>>(name, () => variable());
			data.Add(datum);
		}
		public void AddDatum(string name, Func<List<float>> variable)
		{
			IDatum datum = new Datum<List<float>>(name, () => variable());
			data.Add(datum);
		}
		public void AddDatum(string name, Func<List<float?>> variable)
		{
			IDatum datum = new Datum<List<float?>>(name, () => variable());
			data.Add(datum);
		}
		public void AddDatum(string name, Func<List<bool>> variable)
		{
			IDatum datum = new Datum<List<bool>>(name, () => variable());
			data.Add(datum);
		}
		public void AddDatum(string name, Func<List<bool?>> variable)
		{
			IDatum datum = new Datum<List<bool?>>(name, () => variable());
			data.Add(datum);
		}
		public void AddDatum(string name, Func<List<string>> variable)
		{
			IDatum datum = new Datum<List<string>>(name, () => variable());
			data.Add(datum);
		}
		public void AddDatum(string name, Func<List<Vector2>> variable)
		{
			IDatum datum = new Datum<List<Vector2>>(name, () => variable());
			data.Add(datum);
		}
		public void AddDatum(string name, Func<List<Vector2?>> variable)
		{
			IDatum datum = new Datum<List<Vector2?>>(name, () => variable());
			data.Add(datum);
		}
		public void AddDatum(string name, Func<List<Vector3>> variable)
		{
			IDatum datum = new Datum<List<Vector3>>(name, () => variable());
			data.Add(datum);
		}
		public void AddDatum(string name, Func<List<Vector3?>> variable)
		{
			IDatum datum = new Datum<List<Vector3?>>(name, () => variable());
			data.Add(datum);
		}
		public void AddDatum(string name, Func<List<Vector4>> variable)
		{
			IDatum datum = new Datum<List<Vector4>>(name, () => variable());
			data.Add(datum);
		}
		public void AddDatum(string name, Func<List<Vector4?>> variable)
		{
			IDatum datum = new Datum<List<Vector4?>>(name, () => variable());
			data.Add(datum);
		}
		public void AddDatum(string name, Func<int[]> variable)
		{
			IDatum datum = new Datum<int[]>(name, () => variable());
			data.Add(datum);
		}
		public void AddDatum(string name, Func<int?[]> variable)
		{
			IDatum datum = new Datum<int?[]>(name, () => variable());
			data.Add(datum);
		}
		public void AddDatum(string name, Func<Int64[]> variable)
		{
			IDatum datum = new Datum<Int64[]>(name, () => variable());
			data.Add(datum);
		}
		public void AddDatum(string name, Func<Int64?[]> variable)
		{
			IDatum datum = new Datum<Int64?[]>(name, () => variable());
			data.Add(datum);
		}
		public void AddDatum(string name, Func<float[]> variable)
		{
			IDatum datum = new Datum<float[]>(name, () => variable());
			data.Add(datum);
		}
		public void AddDatum(string name, Func<float?[]> variable)
		{
			IDatum datum = new Datum<float?[]>(name, () => variable());
			data.Add(datum);
		}
		public void AddDatum(string name, Func<bool[]> variable)
		{
			IDatum datum = new Datum<bool[]>(name, () => variable());
			data.Add(datum);
		}
		public void AddDatum(string name, Func<bool?[]> variable)
		{
			IDatum datum = new Datum<bool?[]>(name, () => variable());
			data.Add(datum);
		}
		public void AddDatum(string name, Func<string[]> variable)
		{
			IDatum datum = new Datum<string[]>(name, () => variable());
			data.Add(datum);
		}
		public void AddDatum(string name, Func<Vector2[]> variable)
		{
			IDatum datum = new Datum<Vector2[]>(name, () => variable());
			data.Add(datum);
		}
		public void AddDatum(string name, Func<Vector2?[]> variable)
		{
			IDatum datum = new Datum<Vector2?[]>(name, () => variable());
			data.Add(datum);
		}
		public void AddDatum(string name, Func<Vector3[]> variable)
		{
			IDatum datum = new Datum<Vector3[]>(name, () => variable());
			data.Add(datum);
		}
		public void AddDatum(string name, Func<Vector3?[]> variable)
		{
			IDatum datum = new Datum<Vector3?[]>(name, () => variable());
			data.Add(datum);
		}
		public void AddDatum(string name, Func<Vector4[]> variable)
		{
			IDatum datum = new Datum<Vector4[]>(name, () => variable());
			data.Add(datum);
		}
		public void AddDatum(string name, Func<Vector4?[]> variable)
		{
			IDatum datum = new Datum<Vector4?[]>(name, () => variable());
			data.Add(datum);
		}

		public event Action OnLogChanged;


        public void CreateSQLTable(string databaseAddress)
        {
            //connect to database
            //create empty table with DataController name
            //loop through DataController.Data, add column to table for each datum
        }

        public void CreateSQLTableIfNecessary(string databaseAddess)
        {
            //check if table exists in database
            //if not, CreateSQLTable()
        }




		/// <summary>
		/// Appends current values of all Datums to data buffer.
		/// </summary>
		public IEnumerator AppendDataToBuffer()
		{
			if (storeData) //&& Time.frameCount > frameChecker)
			{
				string[] currentVals = new string[data.Count];
				
				for (int i = 0; i < data.Count; i++) // get current value of each variable
					currentVals[i] = data[i].ValueAsString;

				if(DataControllerHoldsFrames)
				{
					if(dataToUpdateNextFrame.Count > 0)
					{
                        if (name.ToLower().Contains("trial"))
                            Debug.LogWarning("SETTING TO TRUE! (" + name + ") " + Time.frameCount);
                        PreviousFrameHeldDataValues = currentVals.ToList();
                        appendDataToBufferNextFrame = true;
                    }
				}
                else // if we don't need to hold data for a frame to get accurate times, append all data to string buffer
                {
                    dataBuffer.Add(string.Join("\t", currentVals));
                    if (dataBuffer.Count == capacity)
                        yield return StartCoroutine(AppendDataToFile());
                }
				
				frameChecker = Time.frameCount;
			}
            OnLogChanged?.Invoke();
        }

        /// <summary>
        /// Creates a new data file.
        /// </summary>
        public IEnumerator CreateFile()
		{
			if (storeData && fileName != null)
			{
				fileHeaders = "";
				for (int i = 0; i < data.Count; i++)
				{
					if (i > 0)
						fileHeaders += "\t";
					fileHeaders += data[i].Name;
				}

				if (SessionValues.StoringDataOnServer) //Create File With Headers
				{
					if (!ServerManager.FolderCreated(folderPath))
						yield return StartCoroutine(CreateServerFolder(folderPath));

					if (!fileCreated)
						yield return StartCoroutine(CreateServerFileWithHeaders());
				}
				else
				{
					Directory.CreateDirectory(folderPath);
					using StreamWriter dataStream = File.CreateText(folderPath + Path.DirectorySeparatorChar + fileName);
					dataStream.Write(fileHeaders);

					fileCreated = true;
				}
			}
		}

		/// <summary>
		/// Writes the data buffer to file.
		/// </summary>
		public IEnumerator AppendDataToFile()
		{
			if (storeData && fileName != null && dataBuffer.Count > 0)
			{
				string content = string.Join("\n", dataBuffer.ToArray());

				if (!appendDataToBufferNextFrame) //If NOT waiting...
				{
                    if (name.ToLower().Contains("trial"))
                        Debug.LogWarning("APPENDING DATA TO FILE! (" + name + ") " + Time.frameCount);
					if (SessionValues.StoringDataOnServer)
						yield return StartCoroutine(AppendDataToServerFile(content));
					else
					{
						using StreamWriter dataStream = File.AppendText(folderPath + Path.DirectorySeparatorChar + fileName);
						dataStream.Write("\n" + content);
					}
                }
                else
				{
                    if (name.ToLower().Contains("trial"))
                        Debug.LogWarning("---------- Waiting to write data to file ( " + name + ") ----------");
					appendDataToFileNextFrame = true;
				}

				dataBuffer.Clear();
			}
		}

		private IEnumerator CreateServerFileWithHeaders()
		{
            yield return ServerManager.CreateFileAsync(folderPath + "/" + fileName, fileHeaders);
            fileCreated = true;   
        }

        private IEnumerator AppendDataToServerFile(string fileContent)
		{
            yield return ServerManager.AppendToFileAsync(folderPath + "/" + fileName, fileContent);	
		}


        private IEnumerator CreateServerFolder(string folderName)
        {
            yield return ServerManager.CreateFolder(folderName);
        }





        /// <summary>
        /// Adds standardized timing data for the current Control Level's states to be tracked.
        /// </summary>
        /// <param name="level">The Control Level whose active states should be tracked.</param>
        /// <param name="timingTypes">(Optional) List of strings specifying which state timing data to track. Possible values: "StartFrame", "EndFrame", "StartTimeAbsolute", "StartTimeRelative", "EndTimeAbsolute", "EndTimeRelative", "Duration".</param>
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
					dataToUpdateNextFrame.Add(new HeldDatum<float>("EndTimeAbsolute", () => s.TimingInfo.EndTimeAbsolute, data.Count - 1));
					AddDatum(s.StateName + "_EndTimeRelative", () => s.TimingInfo.EndTimeRelative);
					dataToUpdateNextFrame.Add(new HeldDatum<float>("EndTimeRelative",() => s.TimingInfo.EndTimeRelative, data.Count - 1));
					AddDatum(s.StateName + "_Duration", () => s.TimingInfo.Duration);
					dataToUpdateNextFrame.Add(new HeldDatum<float>("Duration",() => s.TimingInfo.Duration, data.Count - 1));
				}
				else //specify which timing information to add
				{
					foreach (string t in timingTypes)
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
								dataToUpdateNextFrame.Add(new HeldDatum<float>("EndTimeAbsolute",() => s.TimingInfo.EndTimeAbsolute, data.Count - 1));
								break;
							case "EndTimeRelative":
								AddDatum(s.StateName + "_EndTimeRelative", () => s.TimingInfo.EndTimeRelative);
								dataToUpdateNextFrame.Add(new HeldDatum<float>("EndTimeRelative",() => s.TimingInfo.EndTimeRelative, data.Count - 1));
								break;
							case "Duration":
								AddDatum(s.StateName + "_Duration", () => s.TimingInfo.Duration);
								dataToUpdateNextFrame.Add(new HeldDatum<float>("Duration",() => s.TimingInfo.Duration, data.Count - 1));
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
            StartCoroutine(AppendDataToFile());
		}

	}
}
