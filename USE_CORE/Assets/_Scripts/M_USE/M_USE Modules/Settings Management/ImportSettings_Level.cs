using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;
using USE_DisplayManagement;
using USE_ExperimentTemplate_Classes;
using USE_ExperimentTemplate_Session;
using USE_States;

public class ImportSettings_Level : ControlLevel
{
    public List<object> outputSettings = new List<object>();
    public List<string> fileNames = new List<string>();
    public List<string> settingParsingStyles = new List<string>();
    public List<Type> settingTypes = new List<Type>(); 
    public List<string> filePathStrings;

    public List<SettingsDetails> SettingsDetails;
    public SettingsDetails currentSettingsDetails;

    private int iSettings;

    public object parsedResult = null;

    public bool fileParsed;
    public bool fileLoadingFinished;
    public bool importPaused;


    public override void DefineControlLevel()
    {
        State loadFile = new State("LoadFile");
        State parseFile = new State("ParseFile");
        AddActiveStates(new List<State> { loadFile, parseFile });

        SettingsDetails = new List<SettingsDetails>();
        
        Add_ControlLevel_InitializationMethod(() =>
        {
            outputSettings = new List<object>();
        });

        loadFile.AddDefaultInitializationMethod(() =>
        {
	        fileLoadingFinished = false;
            currentSettingsDetails = SettingsDetails[0];
            Debug.Log("Attempting to load settings file  " + currentSettingsDetails.FilePath);
            if (string.IsNullOrEmpty(currentSettingsDetails.FilePath))
            {
	            Debug.Log("No settings file found with search string " + currentSettingsDetails.SearchString);
	            fileLoadingFinished = true;
            }
            else
            {
	            StartCoroutine(GetFileContentString(currentSettingsDetails.FilePath,
		            currentSettingsDetails.SearchString, (contentString) =>
		            {
			            if (!string.IsNullOrEmpty(contentString))
			            {
				            currentSettingsDetails.FileContentString = contentString;
				            Debug.Log("File " + currentSettingsDetails.FilePath + " successfully loaded.");
				            fileLoadingFinished = true;
			            }
			            else
			            {
				            Debug.Log($"Loaded file {currentSettingsDetails.FileName} but no data was found in it.");
				            fileLoadingFinished = true;
			            }
		            }));
            }
        });
        loadFile.SpecifyTermination(() => fileLoadingFinished, parseFile, () => fileLoadingFinished = false);
        // loadFile.SpecifyTermination(() => !importPaused && SettingsDetails.Count == 0, parseFile);
        //     parseFile, () => { fileLoaded = false; continueToLoadFile = false; }) ;
        loadFile.AddTimer(() => 10f, parseFile ); //adjust timer value for a sensible amount of time for the server to retrieve file content 

        parseFile.AddDefaultInitializationMethod(() =>
        {
	        if (!string.IsNullOrEmpty(currentSettingsDetails.FileContentString))
	        {
		        Debug.Log("Attempting to parse " + currentSettingsDetails.FilePath);
		        // currentSettingsDetails.SettingType = currentSettingsDetails.SettingType;
		        if (string.IsNullOrEmpty(currentSettingsDetails.SettingParsingStyle))
			        currentSettingsDetails.SettingParsingStyle = DetermineParsingStyle(currentSettingsDetails.FilePath);
		        ConvertStringToSettings();
		        fileParsed = true;
		        importPaused = true;
	        }
	        else
	        {
		        parsedResult = null;
		        fileParsed = true;
		        importPaused = true;
	        }
        });
        parseFile.SpecifyTermination(()=> !importPaused && SettingsDetails.Count > 1, loadFile, ()=>
        {
            importPaused = false;
            fileParsed = false;
            iSettings++;
            SettingsDetails.RemoveAt(0);
        });
        parseFile.SpecifyTermination(() => !importPaused && SettingsDetails.Count == 1, ()=> null,() =>
        {
            SettingsDetails.RemoveAt(0);
            fileParsed = false;
            importPaused = false;
            ResetVariables();
        });
    }

    private void ConvertStringToSettings()
    {
        if (currentSettingsDetails.SettingParsingStyle == "Array")
        {
            char delimiter = '\t';
            MethodInfo methodInfo = GetType().GetMethod(nameof(ConvertTextToSettings_Array));
            MethodInfo ConvertTextToSettings_SingleTypeArray_meth = methodInfo.MakeGenericMethod(new Type[] { currentSettingsDetails.SettingType });
            object result = ConvertTextToSettings_SingleTypeArray_meth.Invoke(this, new object[] { currentSettingsDetails.FileContentString, delimiter });
            parsedResult = result;
        }
        else if (currentSettingsDetails.SettingParsingStyle == "JSON")
        {
            MethodInfo methodInfo = GetType().GetMethod(nameof(ConvertTextToSettings_JSON));
            MethodInfo ConvertTextToSettings_SingleTypeJSON_meth = methodInfo.MakeGenericMethod(new Type[] { currentSettingsDetails.SettingType });
            object result = ConvertTextToSettings_SingleTypeJSON_meth.Invoke(this, new object[] { currentSettingsDetails.FileContentString});
            parsedResult = result;
        }
        else if (currentSettingsDetails.SettingParsingStyle == "SingleType")
        {
            MethodInfo methodInfo = GetType().GetMethod(nameof(ConvertTextToSettings_SingleType));
            MethodInfo ConvertTextToSettings_SingleTypeDelimited_meth = methodInfo.MakeGenericMethod(new Type[] { currentSettingsDetails.SettingType });
            object result = ConvertTextToSettings_SingleTypeDelimited_meth.Invoke(this, new object[] { currentSettingsDetails.FileContentString, '\t' });
            parsedResult = result;
        }
        else
            Debug.LogError("Settings parsing style is " + settingParsingStyles[iSettings] + ", but this is not handled by script.");
    }
    private IEnumerator GetFileContentString(string filePath, string searchString, Action<string> callback)
    {
        string fileContent;

        if (SessionValues.ConfigAccessType == "Local" || SessionValues.ConfigAccessType == "Default")
        {
            fileContent = File.ReadAllText(filePath); //Will need to check that this works during Web Build
            callback(fileContent);
        }
        else if (SessionValues.ConfigAccessType == "Server")
        {
            yield return CoroutineHelper.StartCoroutine(ServerManager.GetFileStringAsync(filePath, searchString, result =>
            {
                currentSettingsDetails.FileName = result[0];
                if(result != null)
                    callback(result[1]);
                else
                {
                    Debug.Log("GET FILE STRING ASYNC RESULT IS NULL!");
                    callback(null);
                }
            }));
        }
        else
            callback(null);

    }
    public T[] ConvertTextToSettings_Array<T>(string fileContentString, char delimiter = '\t')
    {
        string[] lines;
        // if (SessionValues.ConfigAccessType == "Server")
        // {
            string[] splitLines = fileContentString.Split('\n');
            List<string> stringList = new List<string>();
            foreach (var line in splitLines)
            {
                if (string.IsNullOrEmpty(line.Trim()) || line.Trim().StartsWith("//", StringComparison.Ordinal)) // Skip empty and/or commented out lines
                    continue;
                stringList.Add(line);
            }
            lines = stringList.ToArray();
        // }
        // else
        // {
        //     lines = new[] { "idk" };
        // }

        T[] settingsArray = new T[lines.Length - 1];

        string[] fieldNames = lines[0].Split(delimiter);

        foreach (string fieldName in fieldNames)
        {
            string tempFieldName = fieldName.Trim();
            if (typeof(T).GetProperty(tempFieldName) == null && typeof(T).GetField(tempFieldName) == null)
            {
                throw new Exception("Settings file contains the header \""
                                    + tempFieldName + "\" but this is not a public property or field of the provided type "
                                    + typeof(T) + ".");
            }
        }

        FieldInfo[] fieldInfos = typeof(T).GetFields();

        for (int iLine = 1; iLine < lines.Length; iLine++)
        {
            // Creates an instance for the entire line (ie. BlockDef)
            settingsArray[iLine - 1] = Activator.CreateInstance<T>();
        
            // Splits the separate fields for the single instance (ie. fields of BlockDef)
            string[] values = lines[iLine].Split(delimiter);
            for (int iVal = 0; iVal < fieldNames.Length; iVal++)
            {
                string fieldName = fieldNames[iVal].Trim();
                try
                {
                    PropertyInfo propertyInfo = typeof(T).GetProperty(fieldName);
                    FieldInfo fieldInfo = typeof(T).GetField(fieldName);
                
                    // Checks if the value is a Field or Property of the type T, and sets the value 
                    if (propertyInfo != null)
                    {
                        Type propertyType = propertyInfo.PropertyType;
                        propertyInfo.SetValue(settingsArray[iLine - 1], Convert.ChangeType(values[iVal], propertyType));
                    }
                    else if (fieldInfo != null)
                    {
	                    Type ft = fieldInfo.FieldType;
	                    if (ft == typeof(string))
		                    fieldInfo.SetValue(settingsArray[iLine - 1], values[iVal]);
	                    else if (ft == typeof(bool))
		                    fieldInfo.SetValue(settingsArray[iLine - 1],
			                    (bool) ConvertStringToType<bool>(values[iVal]));
	                    else if (ft == typeof(int))
		                    fieldInfo.SetValue(settingsArray[iLine - 1], (int) ConvertStringToType<int>(values[iVal]));
	                    else if (ft == typeof(float))
		                    fieldInfo.SetValue(settingsArray[iLine - 1],
			                    (float) ConvertStringToType<float>(values[iVal]));
	                    else if (ft == typeof(bool?))
		                    fieldInfo.SetValue(settingsArray[iLine - 1],
			                    (bool?) ConvertStringToType<bool>(values[iVal]));
	                    else if (ft == typeof(int?))
		                    fieldInfo.SetValue(settingsArray[iLine - 1], (int?) ConvertStringToType<int>(values[iVal]));
	                    else if (ft == typeof(float?))
		                    fieldInfo.SetValue(settingsArray[iLine - 1],
			                    (float?) ConvertStringToType<float>(values[iVal]));
	                    else if (ft == typeof(Vector2))
		                    fieldInfo.SetValue(settingsArray[iLine - 1],
			                    (Vector2) ConvertStringToType<Vector2>(values[iVal]));
	                    else if (ft == typeof(Vector3))
		                    fieldInfo.SetValue(settingsArray[iLine - 1],
			                    (Vector3) ConvertStringToType<Vector3>(values[iVal]));
	                    else if (ft == typeof(string[]))
		                    fieldInfo.SetValue(settingsArray[iLine - 1],
			                    (string[]) ConvertStringToType<string[]>(values[iVal]));
	                    else if (ft == typeof(bool[]))
		                    fieldInfo.SetValue(settingsArray[iLine - 1],
			                    (bool[]) ConvertStringToType<bool[]>(values[iVal]));
	                    else if (ft == typeof(int[]))
		                    fieldInfo.SetValue(settingsArray[iLine - 1],
			                    (int[]) ConvertStringToType<int[]>(values[iVal]));
	                    else if (ft == typeof(float[]))
		                    fieldInfo.SetValue(settingsArray[iLine - 1],
			                    (float[]) ConvertStringToType<float[]>(values[iVal]));
	                    else if (ft == typeof(bool?[]))
		                    fieldInfo.SetValue(settingsArray[iLine - 1],
			                    (bool?[]) ConvertStringToType<bool[]>(values[iVal]));
	                    else if (ft == typeof(int?[]))
		                    fieldInfo.SetValue(settingsArray[iLine - 1],
			                    (int?[]) ConvertStringToType<int[]>(values[iVal]));
	                    else if (ft == typeof(float?[]))
		                    fieldInfo.SetValue(settingsArray[iLine - 1],
			                    (float?[]) ConvertStringToType<float[]>(values[iVal]));
	                    else if (ft == typeof(Vector2[]))
		                    fieldInfo.SetValue(settingsArray[iLine - 1],
			                    (Vector2[]) ConvertStringToType<Vector2[]>(values[iVal]));
	                    else if (ft == typeof(Vector3[]))
		                    fieldInfo.SetValue(settingsArray[iLine - 1],
			                    (Vector3[]) ConvertStringToType<Vector3[]>(values[iVal]));
	                    else if (ft == typeof(Reward[]))
		                    fieldInfo.SetValue(settingsArray[iLine - 1],
			                    (Reward[]) ConvertStringToType<Reward[]>(values[iVal]));
	                    else if (ft == typeof(string[][]))
		                    fieldInfo.SetValue(settingsArray[iLine - 1],
			                    (string[][]) ConvertStringToType<string[][]>(values[iVal]));
	                    else if (ft == typeof(bool[][]))
		                    fieldInfo.SetValue(settingsArray[iLine - 1],
			                    (bool[][]) ConvertStringToType<bool[][]>(values[iVal]));
	                    else if (ft == typeof(int[][]))
		                    fieldInfo.SetValue(settingsArray[iLine - 1],
			                    (int[][]) ConvertStringToType<int[][]>(values[iVal]));
	                    else if (ft == typeof(float[][]))
		                    fieldInfo.SetValue(settingsArray[iLine - 1],
			                    (float[][]) ConvertStringToType<float[][]>(values[iVal]));
	                    else if (ft == typeof(bool?[][]))
		                    fieldInfo.SetValue(settingsArray[iLine - 1],
			                    (bool?[][]) ConvertStringToType<bool[][]>(values[iVal]));
	                    else if (ft == typeof(int?[][]))
		                    fieldInfo.SetValue(settingsArray[iLine - 1],
			                    (int?[][]) ConvertStringToType<int[][]>(values[iVal]));
	                    else if (ft == typeof(float?[][]))
		                    fieldInfo.SetValue(settingsArray[iLine - 1],
			                    (float?[][]) ConvertStringToType<float[][]>(values[iVal]));
	                    else if (ft == typeof(Reward[][]))
		                    fieldInfo.SetValue(settingsArray[iLine - 1],
			                    (Reward[][]) ConvertStringToType<Reward[][]>(values[iVal]));
	                    else if (ft == typeof(Color))
		                    fieldInfo.SetValue(settingsArray[iLine - 1],
			                    (Color) ConvertStringToType<Color>(values[iVal]));
	                    else if (ft == typeof(MazeGame_Namespace.MazeDef[]))
		                    fieldInfo.SetValue(settingsArray[iLine - 1],
			                    (MazeGame_Namespace.MazeDef[]) ConvertStringToType<MazeGame_Namespace.MazeDef[]>(
				                    values[iVal]));
	                    else
		                    Debug.LogError("Attempted to convert value " + values[iVal] + " with header " + fieldName +
		                                   " to type " + ft + " but there is no conversion specified for this type.");
                    }
                }
                    catch (Exception e)
                    {
                        Debug.Log(fieldNames[iVal] + ": " + values[iVal]);
                        // Debug.Log("Error adding TDF file \"" + settingsCategory + "\" to Settings \"" + settingsCategory + "\".");
                        throw new Exception(e.Message + "\t" + e.StackTrace);
                    }
                }
            }

        return settingsArray;

    }
    
    
		public static object ConvertStringToType<T>(string s)
		{
			if (typeof(T) == typeof(string))
				return s;
			else if (typeof(T) == typeof(Vector2))
			{
				try
				{
					return (Vector2)ConvertStringArray<Vector2>(s);
				}
				catch (Exception e)
				{
					Debug.LogError("Tried to convert string \"" + s + "\" to type \""
						+ typeof(T).Name + " but the conversion failed.");

					throw new ArgumentException(e.Message + "\t" + e.StackTrace);
				}
			}
			else if (typeof(T) == typeof(Vector3))
			{
				try
				{
					return (Vector3)ConvertStringArray<Vector3>(s);
				}
				catch (Exception e)
				{
					Debug.LogError("Tried to convert string \"" + s + "\" to type \""
						+ typeof(T).Name + " but the conversion failed.");

					throw new ArgumentException(e.Message + "\t" + e.StackTrace);
				}
			}
			else if (typeof(T) == typeof(Vector2[]))
			{
				try
				{
					return (Vector2[])ConvertStringArray<Vector2[]>(s);
				}
				catch (Exception e)
				{
					Debug.LogError("Tried to convert string \"" + s + "\" to type \""
						+ typeof(T).Name + " but the conversion failed.");

					throw new ArgumentException(e.Message + "\t" + e.StackTrace);
				}
			}
			else if (typeof(T) == typeof(Vector3[]))
			{
				try
				{
					return (Vector3[])ConvertStringArray<Vector3[]>(s);
				}
				catch (Exception e)
				{
					Debug.LogError("Tried to convert string \"" + s + "\" to type \""
						+ typeof(T).Name + " but the conversion failed.");

					throw new ArgumentException(e.Message + "\t" + e.StackTrace);
				}
			}
			else if (typeof(T) == typeof(Reward))
			{
				try
				{
					return (Reward)JsonConvert.DeserializeObject(s, typeof(Reward));
				}
				catch (Exception e)
				{
					Debug.LogError("Tried to convert string \"" + s + "\" to type \""
						+ typeof(T).Name + " but the conversion failed.");

					throw new ArgumentException(e.Message + "\t" + e.StackTrace);
				}
			}
			else if (typeof(T) == typeof(Reward[]))
			{
				try
				{
					return (Reward[])JsonConvert.DeserializeObject(s, typeof(Reward[]));
				}
				catch (Exception e)
				{
					Debug.LogError("Tried to convert string \"" + s + "\" to type \""
						+ typeof(T).Name + " but the conversion failed.");

					throw new ArgumentException(e.Message + "\t" + e.StackTrace);
				}
			}
			else if (typeof(T) == typeof(Reward[][]))
			{
				try
				{
					return (Reward[][])JsonConvert.DeserializeObject(s, typeof(Reward[][]));
				}
				catch (Exception e)
				{
					Debug.LogError("Tried to convert string \"" + s + "\" to type \""
						+ typeof(T).Name + " but the conversion failed.");

					throw new ArgumentException(e.Message + "\t" + e.StackTrace);
				}
			}
            else if (typeof(T) == typeof(float[]))
			{
				try
				{
					return (float[])ConvertStringArray<float>(s);
				}
				catch (Exception e)
				{
					Debug.LogError("Tried to convert string \"" + s + "\" to type \""
						+ typeof(T).Name + " but the conversion failed.");

					throw new ArgumentException(e.Message + "\t" + e.StackTrace);
				}
			}
			else if (typeof(T) == typeof(int[]))
			{
				try
				{
					return (int[])ConvertStringArray<int>(s);
				}
				catch (Exception e)
				{
					Debug.LogError("Tried to convert string \"" + s + "\" to type \""
						+ typeof(T).Name + " but the conversion failed.");

					throw new ArgumentException(e.Message + "\t" + e.StackTrace);
				}
			}
			else if (typeof(T) == typeof(int[][]))
			{

				try
				{// Remove the parentheses

					return (int[][])ConvertStringJaggedArray<int>(s);
				}
				catch (Exception e)
				{
					Debug.LogError("Tried to convert string \"" + s + "\" to type \""
						+ typeof(T).Name + " but the conversion failed.");

					throw new ArgumentException(e.Message + "\t" + e.StackTrace);
				}
			}
			else if (typeof(T) == typeof(float[][]))
			{

				try
				{// Remove the parentheses

					return (float[][])ConvertStringJaggedArray<float>(s);
				}
				catch (Exception e)
				{
					Debug.LogError("Tried to convert string \"" + s + "\" to type \""
						+ typeof(T).Name + " but the conversion failed.");

					throw new ArgumentException(e.Message + "\t" + e.StackTrace);
				}
			}
			else if (typeof(T) == typeof(bool[][]))
			{

				try
				{// Remove the parentheses

					return (bool[][])ConvertStringJaggedArray<bool>(s);
				}
				catch (Exception e)
				{
					Debug.LogError("Tried to convert string \"" + s + "\" to type \""
						+ typeof(T).Name + " but the conversion failed.");

					throw new ArgumentException(e.Message + "\t" + e.StackTrace);
				}
			}
			else if (typeof(T) == typeof(Color))
			{
				try
				{
					return (Color)ConvertStringJaggedArray<Color>(s);
				}
				catch (Exception e)
				{
					Debug.LogError("Tried to convert string \"" + s + "\" to type \""
						+ typeof(T).Name + " but the conversion failed.");

					throw new ArgumentException(e.Message + "\t" + e.StackTrace);
				}
			}

			else if (typeof(T) != null)
			{
				try
				{
					//can add custom conversion instructions for particular typeStrings if needed
					return Convert.ChangeType(s, typeof(T));
				}
				catch (Exception e)
				{
					Debug.LogError("Tried to convert string \"" + s + "\" to type \""
						+ typeof(T).Name + " but the conversion failed.");

					throw new ArgumentException(e.Message + "\t" + e.StackTrace);
				}
			}
			else
			{
				throw new ArgumentException("Tried to convert string \"" + s + "\" to type \""
						+ typeof(T).Name + " but the type was not recognized.");
			}
		}
		
		
		public static object ConvertStringArray<T>(string s)
		{
			if (typeof(T) == typeof(int))
			{
				string[] sArray = GetStringArray(s);
				int[] finalArray = new int[sArray.Length];
				for (int iVal = 0; iVal < sArray.Length; iVal++)
					finalArray[iVal] = int.Parse(sArray[iVal]);
				return finalArray;
			}
			else if (typeof(T) == typeof(float))
			{
				string[] sArray = GetStringArray(s);
				float[] finalArray = new float[sArray.Length];
				for (int iVal = 0; iVal < sArray.Length; iVal++)
					finalArray[iVal] = float.Parse(sArray[iVal]);
				return finalArray;
			}
			else if (typeof(T) == typeof(string))
			{
				return GetStringArray(s);
			}
			else if (typeof(T) == typeof(Vector2))
			{
				float[] floatArray = (float[])ConvertStringArray<float>(s);
				return new Vector2(floatArray[0], floatArray[1]);
			}
			else if (typeof(T) == typeof(Vector3))
			{
				float[] floatArray = (float[])ConvertStringArray<float>(s);
				return new Vector3(floatArray[0], floatArray[1], floatArray[2]);
			}
			else if (typeof(T) == typeof(Vector2[]))
			{
				string[][] sArray = GetStringArrayofArrays(s);
				Vector2[] finalArray = new Vector2[sArray.Length];
				for (int iVal = 0; iVal < sArray.Length; iVal++)
				{
					finalArray[iVal] = new Vector2(float.Parse(sArray[iVal][0]), float.Parse(sArray[iVal][1]));
				}
				return finalArray;
			}
			else if (typeof(T) == typeof(Vector3[]))
			{
				string[][] sArray = GetStringArrayofArrays(s);
				Vector3[] finalArray = new Vector3[sArray.Length];
				for (int iVal = 0; iVal < sArray.Length; iVal++)
				{
					finalArray[iVal] = new Vector3(float.Parse(sArray[iVal][0]), float.Parse(sArray[iVal][1]), float.Parse(sArray[iVal][2]));
				}
				return finalArray;
			}
			else
			{
				return GetStringArray(s);
			}
		}

		public static object ConvertStringJaggedArray<T>(string s)
		{
			if (typeof(T) == typeof(int))
			{
				string[][] outerArray = GetStringArrayofArrays(s);
				int[][] finalArray = new int[outerArray.Length][];
				for (int iOuter = 0; iOuter < outerArray.Length; iOuter++)
				{
					finalArray[iOuter] = Array.ConvertAll(outerArray[iOuter], str => int.Parse(str));
					//string[] innerArray = GetStringArray(outerArray[iOuter]);
					//finalArray[iOuter] = new int[innerArray.Length];
					//for (int iInner = 0; iInner < innerArray.Length; iInner++)
						//finalArray[iOuter][iInner] = int.Parse(innerArray[iInner]);
				}
				return finalArray;
			}
			else if (typeof(T) == typeof(float))
			{
				string[][] outerArray = GetStringArrayofArrays(s);
				float[][] finalArray = new float[outerArray.Length][];
				for (int iOuter = 0; iOuter < outerArray.Length; iOuter++)
				{
					finalArray[iOuter] = Array.ConvertAll(outerArray[iOuter], str => float.Parse(str));
					//string[] innerArray = GetStringArray(outerArray[iOuter]);
					//finalArray[iOuter] = new float[innerArray.Length];
					//for (int iInner = 0; iInner < innerArray.Length; iInner++)
					//finalArray[iOuter][iInner] = float.Parse(innerArray[iInner]);
				}
				return finalArray;
			}
			else if (typeof(T) == typeof(string))
			{
				return GetStringArrayofArrays(s);
			}
			else if (typeof(T) == typeof(bool))
			{
				string[][] outerArray = GetStringArrayofArrays(s);
				bool[][] finalArray = new bool[outerArray.Length][];
				for (int iOuter = 0; iOuter < outerArray.Length; iOuter++)
				{
					finalArray[iOuter] = Array.ConvertAll(outerArray[iOuter], str => bool.Parse(str));
				}
				return finalArray;
			}
			else
				return new object[0][];
		}
		public static string[] GetStringArray(string s)
		{
			return (string[])JsonConvert.DeserializeObject(s, typeof(string[]));
		}
		public static string[][] GetStringArrayofArrays(string s)
		{
			return (string[][])JsonConvert.DeserializeObject(s, typeof(string[][]));
		}
    
   // public static T[] ImportSettings_SingleTypeJSON<T>(string settingsCategory, string settingsPath, string serverFileString = null, string dictName = "")
    public T ConvertTextToSettings_JSON<T>(string fileContentString)
    {
        
        T settingsInstance = Activator.CreateInstance<T>();
        try
        {
            settingsInstance = JsonConvert.DeserializeObject<T>(fileContentString);
        }
        catch (Exception e)
        {
            // Debug.Log("Error adding JSON file \"" + settingsCategory + "\" to Settings \"" + settingsCategory + "\".");
            throw new Exception(e.Message + "\t" + e.StackTrace);
         }

        // T[] settingsArray = new T[] { settingsInstance };
        return settingsInstance;
    }
  //  public static T ImportSettings_SingleTypeDelimited<T>(string settingsCategory, string settingsPath, string serverFileString = null, char delimiter = '\t')
    public T ConvertTextToSettings_SingleType<T>(string fileContentString, char delimiter = '\t')
    {
        string[] lines;
        lines = fileContentString.Split('\n');
        
        T settingsInstance = Activator.CreateInstance<T>();

        foreach (string line in lines)
        {
            if (string.IsNullOrEmpty(line.Trim()) || line.Trim().StartsWith("//", StringComparison.Ordinal)) //Skip empty and/or commented out lines
                continue;
            string[] splitString = line.Split(delimiter);
            try
            {
                string fieldName = "";
                string fieldValue = "";
                if (splitString.Length == 3)
                {
                    fieldName = splitString[1].Trim();
                    fieldValue = splitString[2].Trim();
                }
                else if (splitString.Length == 2)
                {
                    fieldName = splitString[0].Trim();
                    fieldValue = splitString[1].Trim();
                }

                if(SurroundedByQuotes(fieldValue))
                    fieldValue = fieldValue = fieldValue.Substring(1, fieldValue.Length - 2);

                AssignFieldValue<T>(fieldName, fieldValue, settingsInstance);
            }
            
            catch (Exception e)
            {
                Debug.Log("Attempted to import Settings file but line \"" + line + "\" has " + line.Length + " entries, 3 expected.");
                throw new Exception(e.Message + "\t" + e.StackTrace);
            }
        }

        return settingsInstance;
    }
    private void AssignFieldValue<T>(string fieldName, string fieldValue, T settingsInstance)
    {
        FieldInfo fieldInfo = typeof(T).GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        PropertyInfo propertyInfo = typeof(T).GetProperty(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (propertyInfo != null)
        {
            Type propertyType = propertyInfo.PropertyType;
            propertyInfo.SetValue(settingsInstance, Convert.ChangeType(fieldValue, propertyType));
        }
        else if (fieldInfo != null)
        {
            Type fieldType = fieldInfo.FieldType;

            if (fieldType.Equals(typeof(OrderedDictionary)))
            {
                if (StartsOrEndsWithBrackets(fieldValue.Trim()))
                {
                    fieldValue = fieldValue.Substring(1, fieldValue.Length - 2);
                }
                string[] sArray = fieldValue.Split(',');
                OrderedDictionary pairs = new OrderedDictionary();
                for (int sCount = 0; sCount < sArray.Length; sCount++)
                {
                    sArray[sCount] = sArray[sCount].Replace("\"", "");
                    sArray[sCount] = sArray[sCount].Trim();
                    string[] sArray2 = sArray[sCount].Split(':');
                    pairs.Add(sArray2[0].Trim(), sArray2[1].Trim());
                }
                fieldInfo.SetValue(settingsInstance, pairs);
            }
            else if (fieldType.Equals(typeof(Dictionary<string, string>)))
            {
                if (StartsOrEndsWithBrackets(fieldValue))
                    fieldValue = fieldValue.Substring(1, fieldValue.Length - 2);
                string[] sArray = fieldValue.Split(',');
                Dictionary<string, string> pairs = new Dictionary<string, string>();
                for (int sCount = 0; sCount < sArray.Length; sCount++)
                {
                    sArray[sCount] = sArray[sCount].Replace("\"", "");
                    sArray[sCount] = sArray[sCount].Trim();
                    string[] sArray2 = sArray[sCount].Split(':');
                    pairs.Add(sArray2[0].Trim(), sArray2[1].Trim());
                }
                fieldInfo.SetValue(settingsInstance, pairs);
            }
            else if (fieldType.Equals(typeof(Vector3[])))
            {
                string[][] sArray = (string[][])JsonConvert.DeserializeObject(fieldValue, typeof(string[][]));
                Vector3[] finalArray = new Vector3[sArray.Length];
                for (int iVal = 0; iVal < sArray.Length; iVal++)
                {
                    finalArray[iVal] = new Vector3(float.Parse(sArray[iVal][0]), float.Parse(sArray[iVal][1]), float.Parse(sArray[iVal][2]));
                }
                fieldInfo.SetValue(settingsInstance, finalArray);
            }
            else if (fieldType.Equals(typeof(List<string>)))
            {
                if (StartsOrEndsWithBrackets(fieldValue))
                    fieldValue = fieldValue.Substring(1, fieldValue.Length - 2);
                string[] sArray = fieldValue.Split(',');
                List<string> valuesList = new List<string>();
                for (int sCount = 0; sCount < sArray.Length; sCount++)
                {
                    sArray[sCount] = sArray[sCount].Replace("\"", "");
                    sArray[sCount] = sArray[sCount].Trim();
                    valuesList.Add(sArray[sCount]);
                }
                fieldInfo.SetValue(settingsInstance, valuesList);
            }
            else if (fieldType.Equals(typeof(MonitorDetails)))
            {
                var deserializedValue = JsonConvert.DeserializeObject<MonitorDetails>(fieldValue);
                fieldInfo.SetValue(settingsInstance, deserializedValue);
            }
            else if (fieldType.Equals(typeof(ScreenDetails)))
            {
                var deserializedValue = JsonConvert.DeserializeObject<ScreenDetails>(fieldValue);
                fieldInfo.SetValue(settingsInstance, deserializedValue);
            }
            else if (fieldType.Equals(typeof(int?)))
            {
                fieldInfo.SetValue(settingsInstance, GetValueOrNull<float>(fieldValue));
            }
            else if (fieldType.Equals(typeof(float?)))
            {
                fieldInfo.SetValue(settingsInstance, GetValueOrNull<float>(fieldValue));
            }
            else if (fieldType.Equals((typeof(string))))
                fieldInfo.SetValue(settingsInstance, fieldValue);
            else
            {
                Debug.Log("UNSPECIFIED SETTINGS FIELD TYPE: " + fieldType);
                fieldInfo.SetValue(settingsInstance, Convert.ChangeType(fieldValue, fieldType));
            }
        }
    }
    
    public  T? GetValueOrNull<T>(string valueAsString)
        where T : struct 
    {
        if (string.IsNullOrEmpty(valueAsString))
            return null;
        return (T) Convert.ChangeType(valueAsString, typeof(T));
    }

    private bool StartsOrEndsWithBrackets(string s)
    {
        if (s.StartsWith("(", StringComparison.Ordinal) &&
            s.EndsWith(")", StringComparison.Ordinal) ||
            s.StartsWith("{", StringComparison.Ordinal) &&
            s.EndsWith("}", StringComparison.Ordinal) ||
            s.StartsWith("[", StringComparison.Ordinal) &&
            s.EndsWith("]", StringComparison.Ordinal))
        {
            return true;
        }
        else
            return false;
    }

    private bool SurroundedByQuotes(string s)
    {
        if(s.StartsWith("\"", StringComparison.Ordinal) &&
            s.EndsWith("\"", StringComparison.Ordinal))
            return true;
        else return false;
    }

    private void ResetVariables()
    {
        fileNames.Clear();
        settingParsingStyles.Clear();
        settingTypes.Clear();
        filePathStrings.Clear();

        currentSettingsDetails = null;
    }

    public string DetermineParsingStyle(string filename)
    {
        string assumedParsingStyle = "";


        if (filename.ToLower().Contains("json"))
            assumedParsingStyle = "JSON";
        else if (filename.ToLower().Contains("array"))
            assumedParsingStyle = "Array";
        else if (filename.ToLower().Contains("singletype"))
            assumedParsingStyle = "SingleType";
        else
            Debug.LogError("Attempting to parse FileName: " + filename + " , but this name does not contain a substring indicated settings parsing type.");


        return assumedParsingStyle;

        //string verifiedParsingStyle = "";
        //string[] lines = currentFileContentString.Split('\n');
        //int tabCount = 0;
        //for (int i = 0; i < lines.Length; i++)
        //{
        //    string line = lines[i].Trim();

        //    if (line.Contains('\t'))
        //    {
        //        tabCount = line.Split('\t').Length; //check if all lines have same number of tabs 
        //        for (int j = i + 1; j < lines.Length; j++)
        //        {
        //            string nextLine = lines[j].Trim();
        //            if (!string.IsNullOrEmpty(nextLine) && nextLine.Split('\t').Length != tabCount)
        //                verifiedParsingStyle = "SingleTypeJSON"; //Inconsistent number of tab-separated values
        //        }
        //    }
        //    else
        //        verifiedParsingStyle = "SingleTypeJSON";
        //}

        ////check for 2 tabs
        //if (tabCount != 2 && verifiedParsingStyle != "SingleTypeJSON")
        //    verifiedParsingStyle = "SingleTypeArray";

        //if (verifiedParsingStyle != assumedParsingStyle)
        //{
        //    if (tabCount == 2)
        //    {
        //        verifiedParsingStyle = assumedParsingStyle;
        //    }
        //    else
        //        verifiedParsingStyle = "SingleTypeJSON";
        //}
        //if (verifiedParsingStyle == "")
        //    Debug.LogError("Attempting to verify parsing of " + SettingsDetails.FileName + " but the file is not parseable as one of the three settings types.");

        //Debug.Log("VERIFIED PARSING STYLE!");

        //return verifiedParsingStyle;
    }

}

public class SettingsDetails
{
    public string SettingParsingStyle;
    public string FilePath;
    public string FileName;
    public string SearchString;
    public string FileContentString;
    public Type SettingType;

    public SettingsDetails(string settingParsingStyle, string searchString, Type settingType)
    {
        SettingParsingStyle = settingParsingStyle;
        SearchString = searchString;
        SettingType = settingType;
    }

    public SettingsDetails(string searchString, Type settingType)
    {
        SearchString = searchString;
        SettingType = settingType;
    }
}
