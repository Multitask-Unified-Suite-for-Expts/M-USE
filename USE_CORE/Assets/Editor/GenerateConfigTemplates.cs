using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public class GenerateConfigTemplates
{
    static public string TaskFolderPath;
    static public string ConfigFolderPath;
    static public string TaskConfigFolder;
    static public string TaskName;
    static public List<Type> ConfigTypes;
    static public Dictionary<string, bool> ConfigDict = new Dictionary<string, bool>()
        {{"TaskDef", true}, {"BlockDef", true}, {"TrialDef", true}, {"StimDef", true}};

    private static ConfigSelectionDialog cd;

    static void PopulateConfigTypes()
    {
        ConfigTypes = new List<Type>();
        foreach (string key in ConfigDict.Keys)
        {
            if (ConfigDict[key])
            {
                switch (key)
                {
                    case "TaskDef":
                        ConfigTypes.Add(USE_Tasks_CustomTypes.CustomTaskDictionary[TaskName].TaskDefType);
                        break;
                    case "BlockDef":
                        ConfigTypes.Add(USE_Tasks_CustomTypes.CustomTaskDictionary[TaskName].BlockDefType);
                        break;
                    case "TrialDef":
                        ConfigTypes.Add(USE_Tasks_CustomTypes.CustomTaskDictionary[TaskName].TrialDefType);
                        break;
                    case "StimDef":
                        ConfigTypes.Add(USE_Tasks_CustomTypes.CustomTaskDictionary[TaskName].StimDefType);
                        break;
                }
            }
        }
    }

    //[MenuItem("USE/Create Config File Templates")]
    static void OpenTaskSelectionDialog()
    {
        // TaskSelectionDialog.GetWindow<TaskSelectionDialog>();
        TaskFolderPath = EditorUtility.OpenFolderPanel("Select Task Folder", "Assets/_USE_Tasks", "");
        cd = ConfigSelectionDialog.GetWindow<ConfigSelectionDialog>();
    }

    static public void OpenConfigSelectionDialog()
    {
        ConfigSelectionDialog.GetWindow<ConfigSelectionDialog>();
    }

    static public void OpenDestinationSelectionDialog()
    {    
        cd.Close();
        ConfigFolderPath = EditorUtility.OpenFolderPanel("Select Config Folder", "Assets/_USE_Tasks", "");
        CreateFiles();
    }

    public static void CreateFiles()
    {
        string[] splitPath = TaskFolderPath.Split('/');
        TaskName = splitPath[splitPath.Length - 1];
        TaskConfigFolder = ConfigFolderPath + Path.DirectorySeparatorChar + TaskName;
        PopulateConfigTypes();
        foreach (Type cType in ConfigTypes)
        {
            string templateFolder = TaskConfigFolder + Path.DirectorySeparatorChar + TaskName + "_ConfigFileTemplates" +
                                    Path.DirectorySeparatorChar + cType.Name + "_Templates";
            Directory.CreateDirectory(templateFolder);

            string fileName = templateFolder + Path.DirectorySeparatorChar + cType.Name;
            
            var methodInfo = typeof(ReflectionMethods).GetMethod(nameof(ReflectionMethods.CreateSingleTypeArrayTemplate));
            MethodInfo createSingleTypeArrayTemplate = methodInfo.MakeGenericMethod(new Type[] {cType});
            createSingleTypeArrayTemplate.Invoke(new ReflectionTypes(), new object[] {fileName + "_TDF_SingleTypeTemplate.txt"});
            
            methodInfo = typeof(ReflectionMethods).GetMethod(nameof(ReflectionMethods.CreateSingleTypeJsonTemplate));
            MethodInfo createSingleTypeJsonTemplate = methodInfo.MakeGenericMethod(new Type[] {cType});
            createSingleTypeJsonTemplate.Invoke(new ReflectionTypes(), new object[] {fileName + "_JSON_SingleTypeTemplate.txt"});
            
            methodInfo = typeof(ReflectionMethods).GetMethod(nameof(ReflectionMethods.CreateMultipleTypeTdfTemplate));
            MethodInfo createMultipleTypeTdfTemplate = methodInfo.MakeGenericMethod(new Type[] {cType});
            createMultipleTypeTdfTemplate.Invoke(new ReflectionTypes(), new object[] {fileName + "_TDF_MultipleTypeTemplate.txt"});
        }
    }
}

public class ReflectionMethods
{
    public static void CreateSingleTypeArrayTemplate<T>(string filePath = "") where T:new()
    {
        // adapted from https://robscode.onl/c-get-member-variables-of-class/
        var bindingFlags = System.Reflection.BindingFlags.Instance |
                           System.Reflection.BindingFlags.NonPublic |
                           System.Reflection.BindingFlags.Public;
        T dummyInstance = CreateDummyInstance<T>();
        List<object> listValues =
            typeof(T).GetFields(bindingFlags).Select(field => field.GetValue(dummyInstance)).ToList();
        List<string> listNames = typeof(T).GetFields(bindingFlags).Select(field => field.Name).ToList();

        string arrayTemplate = listNames[0];
        for (int iName = 1; iName < listNames.Count; iName++)
        {
            arrayTemplate += "\t" + listNames[iName];
        }
        arrayTemplate += "\n" + listValues[0];
        for (int iName = 1; iName < listValues.Count; iName++)
        {
            if (listValues[iName] != null)
                arrayTemplate += "\t" + listValues[iName];
            else
            {
                arrayTemplate += "\tnull";
            }
        }
        File.WriteAllText(filePath, arrayTemplate);
    }
    public static void CreateSingleTypeJsonTemplate<T>(string filePath = "") where T:new()
    {
        T dummyInstance = CreateDummyInstance<T>();
        string arrayTemplate = JsonConvert.SerializeObject(dummyInstance);
        using (var stringReader = new StringReader(arrayTemplate))
        using (var stringWriter = new StringWriter())
        {
            var jsonReader = new JsonTextReader(stringReader);
            var jsonWriter = new JsonTextWriter(stringWriter) { Formatting = Formatting.Indented };
            jsonWriter.WriteToken(jsonReader);
            File.WriteAllText(filePath, stringWriter.ToString());
        }
    }
    public static void CreateMultipleTypeTdfTemplate<T>(string filePath = "") where T:new()
    {
        // adapted from https://robscode.onl/c-get-member-variables-of-class/
        var bindingFlags = System.Reflection.BindingFlags.Instance |
                           System.Reflection.BindingFlags.NonPublic |
                           System.Reflection.BindingFlags.Public;
        T dummyInstance = CreateDummyInstance<T>();
        List<object> listValues =
            typeof(T).GetFields(bindingFlags).Select(field => field.GetValue(dummyInstance)).ToList();
        List<string> listNames = typeof(T).GetFields(bindingFlags).Select(field => field.Name).ToList();
        string arrayTemplate = ""; 
        for (int iVal = 0; iVal < listNames.Count; iVal++)
        {
            //needs type name
            arrayTemplate += listNames[iVal] + "\t" + listValues[iVal];
            if (iVal < listNames.Count - 1)
                arrayTemplate += "\n";
        }
        File.WriteAllText(filePath, arrayTemplate);
    }

    public static T CreateDummyInstance<T>() where T : new()
    {
        var bindingFlags = System.Reflection.BindingFlags.Instance |
                           System.Reflection.BindingFlags.NonPublic |
                           System.Reflection.BindingFlags.Public;
        T dummyInstance = new T();
        List<FieldInfo> fieldInfos = typeof(T).GetFields(bindingFlags).Select(field => field).ToList();
        // List<string> listNames = typeof(T).GetFields(bindingFlags).Select(field => field.Name).ToList();
        // List<Type> fieldTypes = typeof(T).GetFields(bindingFlags).Select(field => field.FieldType).ToList();
        FieldInfo fi;
        string test = "";
        Debug.Log(fieldInfos.Count);
        for (int iT = 0; iT < fieldInfos.Count; iT++)
        {
            // test += GetCSharpRepresentation(fieldInfos[iT].FieldType, true) + "\n" + fieldInfos[iT].FieldType.FullName +
            //         "\n\n";
            // fi = typeof(T).GetField(fieldInfos[iT].Name);
            // fi.SetValue(dummyInstance, typeof(T).GetField(fieldInfos[iT].Name));
            //fieldInfos[iT].FieldType.FullName + "\n";
            if (fieldInfos[iT].FieldType == typeof(string))
            {
                fieldInfos[iT].SetValue(dummyInstance, "SomeString");
            }
            else if (fieldInfos[iT].FieldType == typeof(int))
            {
                fieldInfos[iT].SetValue(dummyInstance, 1);
            }
            else if (fieldInfos[iT].FieldType == typeof(float))
            {
                fieldInfos[iT].SetValue(dummyInstance, 1.1f);
            }
            else if (fieldInfos[iT].FieldType == typeof(bool))
            {
                fieldInfos[iT].SetValue(dummyInstance, false);
            }
            else if (fieldInfos[iT].FieldType == typeof(Vector2))
            {
                fieldInfos[iT].SetValue(dummyInstance, new Vector2(1.1f, 1.2f));
            }
            else if (fieldInfos[iT].FieldType == typeof(Vector3))
            {
                fieldInfos[iT].SetValue(dummyInstance, new Vector3(1.1f, 1.2f, 1.3f));
            }
            else if (fieldInfos[iT].FieldType == typeof(Vector4))
            {
                fieldInfos[iT].SetValue(dummyInstance, new Vector4(1.1f, 1.2f, 1.3f, 1.4f));
            }
            else if (fieldInfos[iT].FieldType == typeof(int?))
            {
                fieldInfos[iT].SetValue(dummyInstance, 1);
            }
            else if (fieldInfos[iT].FieldType == typeof(float?))
            {
                fieldInfos[iT].SetValue(dummyInstance, 1.1f);
            }
            else if (fieldInfos[iT].FieldType == typeof(bool?))
            {
                fieldInfos[iT].SetValue(dummyInstance, false);
            }
            else if (fieldInfos[iT].FieldType == typeof(Vector2?))
            {
                fieldInfos[iT].SetValue(dummyInstance, new Vector2(1.1f, 1.2f));
            }
            else if (fieldInfos[iT].FieldType == typeof(Vector3?))
            {
                fieldInfos[iT].SetValue(dummyInstance, new Vector3(1.1f, 1.2f, 1.3f));
            }
            else if (fieldInfos[iT].FieldType == typeof(Vector4?))
            {
                fieldInfos[iT].SetValue(dummyInstance, new Vector4(1.1f, 1.2f, 1.3f, 1.4f));
            }
            else if (fieldInfos[iT].FieldType == typeof(string[]))
            {
                Debug.Log("hallllllloooooo: " + fieldInfos[iT].GetValue(dummyInstance));
                fieldInfos[iT].SetValue(dummyInstance, new[]{"SomeString1", "SomeString2", "SomeString3"});
                string[] tester = (string[]) fieldInfos[iT].GetValue(dummyInstance);
                Debug.Log("goooodbyyyyeeee: " + tester[0]);
            }
            else if (fieldInfos[iT].FieldType == typeof(int[]))
            {
                fieldInfos[iT].SetValue(dummyInstance, new int[]{1, 2, 3});
            }
            else if (fieldInfos[iT].FieldType == typeof(float[]))
            {
                fieldInfos[iT].SetValue(dummyInstance, new float[]{1.1f, 1.2f, 1.3f});
            }
            else if (fieldInfos[iT].FieldType == typeof(bool[]))
            {
                fieldInfos[iT].SetValue(dummyInstance, new bool[]{true, false, true});
            }
            else if (fieldInfos[iT].FieldType == typeof(Vector2[]))
            {
                fieldInfos[iT].SetValue(dummyInstance, new Vector2[]{new Vector2(1.1f, 1.2f), new Vector2(2.1f, 2.2f)});
            }
            else if (fieldInfos[iT].FieldType == typeof(Vector3[]))
            {
                fieldInfos[iT].SetValue(dummyInstance, new Vector3[]{new Vector3(1.1f, 1.2f, 1.3f), new Vector3(2.1f, 2.2f, 2.3f)});
            }
            else if (fieldInfos[iT].FieldType == typeof(Vector4[]))
            {
                fieldInfos[iT].SetValue(dummyInstance, new Vector4[]{new Vector4(1.1f, 1.2f, 1.3f, 1.4f), new Vector4(2.1f, 2.2f, 2.3f, 1.4f)});
            }
            else if (fieldInfos[iT].FieldType == typeof(string[]))
            {
                fieldInfos[iT].SetValue(dummyInstance, new string[]{"SomeString1", "SomeString2", "SomeString3"});
            }
            else if (fieldInfos[iT].FieldType == typeof(int[]))
            {
                fieldInfos[iT].SetValue(dummyInstance, new int[]{1, 2, 3});
            }
            else if (fieldInfos[iT].FieldType == typeof(float[]))
            {
                fieldInfos[iT].SetValue(dummyInstance, new float[]{1.1f, 1.2f, 1.3f});
            }
            else if (fieldInfos[iT].FieldType == typeof(bool[]))
            {
                fieldInfos[iT].SetValue(dummyInstance, new bool[]{true, false, true});
            }
            else if (fieldInfos[iT].FieldType == typeof(Vector2[]))
            {
                fieldInfos[iT].SetValue(dummyInstance, new Vector2[]{new Vector2(1.1f, 1.2f), new Vector2(2.1f, 2.2f)});
            }
            else if (fieldInfos[iT].FieldType == typeof(Vector3[]))
            {
                fieldInfos[iT].SetValue(dummyInstance, new Vector3[]{new Vector3(1.1f, 1.2f, 1.3f), new Vector3(2.1f, 2.2f, 2.3f)});
            }
            else if (fieldInfos[iT].FieldType == typeof(Vector4[]))
            {
                fieldInfos[iT].SetValue(dummyInstance, new Vector4[]{new Vector4(1.1f, 1.2f, 1.3f, 1.4f), new Vector4(2.1f, 2.2f, 2.3f, 1.4f)});
            }
            else if (fieldInfos[iT].FieldType == typeof(int?[]))
            {
                fieldInfos[iT].SetValue(dummyInstance, new int?[]{1, 2, null});
            }
            else if (fieldInfos[iT].FieldType == typeof(float?[]))
            {
                fieldInfos[iT].SetValue(dummyInstance, new float?[]{1.1f, 1.2f, null});
            }
            else if (fieldInfos[iT].FieldType == typeof(bool?[]))
            {
                fieldInfos[iT].SetValue(dummyInstance, new bool?[]{true, false, null});
            }
            else if (fieldInfos[iT].FieldType == typeof(Vector2?[]))
            {
                fieldInfos[iT].SetValue(dummyInstance, new Vector2?[]{new Vector2(1.1f, 1.2f), null});
            }
            else if (fieldInfos[iT].FieldType == typeof(Vector3?[]))
            {
                fieldInfos[iT].SetValue(dummyInstance, new Vector3?[]{new Vector3(1.1f, 1.2f, 1.3f), null});
            }
            else if (fieldInfos[iT].FieldType == typeof(Vector4?[]))
            {
                fieldInfos[iT].SetValue(dummyInstance, new Vector4?[]{new Vector4(1.1f, 1.2f, 1.3f, 1.4f), null});
            }
            else if (fieldInfos[iT].FieldType == typeof(List<string>))
            {
                fieldInfos[iT].SetValue(dummyInstance, new List<string>(){"SomeString1", "SomeString2", "SomeString3"});
            }
            else if (fieldInfos[iT].FieldType == typeof(List<int>))
            {
                fieldInfos[iT].SetValue(dummyInstance, new List<int>(){1, 2, 3});
            }
            else if (fieldInfos[iT].FieldType == typeof(List<float>))
            {
                fieldInfos[iT].SetValue(dummyInstance, new List<float>(){1.1f, 1.2f, 1.3f});
            }
            else if (fieldInfos[iT].FieldType == typeof(List<bool>))
            {
                fieldInfos[iT].SetValue(dummyInstance, new List<bool>(){true, false, true});
            }
            else if (fieldInfos[iT].FieldType == typeof(List<Vector2>))
            {
                fieldInfos[iT].SetValue(dummyInstance, new List<Vector2>(){new Vector2(1.1f, 1.2f), new Vector2(2.1f, 2.2f)});
            }
            else if (fieldInfos[iT].FieldType == typeof(List<Vector3>))
            {
                fieldInfos[iT].SetValue(dummyInstance, new List<Vector3>(){new Vector3(1.1f, 1.2f, 1.3f), new Vector3(2.1f, 2.2f, 2.3f)});
            }
            else if (fieldInfos[iT].FieldType == typeof(List<Vector4>))
            {
                fieldInfos[iT].SetValue(dummyInstance, new List<Vector4>(){new Vector4(1.1f, 1.2f, 1.3f, 1.4f), new Vector4(2.1f, 2.2f, 2.3f, 1.4f)});
            }
            else if (fieldInfos[iT].FieldType == typeof(List<int?>))
            {
                fieldInfos[iT].SetValue(dummyInstance, new List<int?>(){1, 2, null});
            }
            else if (fieldInfos[iT].FieldType == typeof(List<float?>))
            {
                fieldInfos[iT].SetValue(dummyInstance, new List<float?>(){1.1f, 1.2f, null});
            }
            else if (fieldInfos[iT].FieldType == typeof(List<bool?>))
            {
                fieldInfos[iT].SetValue(dummyInstance, new List<bool?>(){true, false, null});
            }
            else if (fieldInfos[iT].FieldType == typeof(List<Vector2?>))
            {
                fieldInfos[iT].SetValue(dummyInstance, new List<Vector2?>(){new Vector2(1.1f, 1.2f), null});
            }
            else if (fieldInfos[iT].FieldType == typeof(List<Vector3?>))
            {
                fieldInfos[iT].SetValue(dummyInstance, new List<Vector3?>(){new Vector3(1.1f, 1.2f, 1.3f), null});
            }
            else if (fieldInfos[iT].FieldType == typeof(List<Vector4?>))
            {
                fieldInfos[iT].SetValue(dummyInstance, new List<Vector4?>(){new Vector4(1.1f, 1.2f, 1.3f, 1.4f), null});
            }
            else if (fieldInfos[iT].FieldType == typeof(List<string[]>))
            {
                fieldInfos[iT].SetValue(dummyInstance,
                    new List<string[]>()
                    {
                        new string[] {"SomeString1", "SomeString2", "SomeString3"},
                        new string[] {"SomeString4", "SomeString5", "SomeString6"}
                    });
            }
            else if (fieldInfos[iT].FieldType == typeof(List<int[]>))
            {
                fieldInfos[iT].SetValue(dummyInstance, new List<int[]>() {new int[] {1, 2, 3}, new int[] {4, 5, 6}});
            }
            else if (fieldInfos[iT].FieldType == typeof(List<float[]>))
            {
                fieldInfos[iT].SetValue(dummyInstance,
                    new List<float[]>() {new float[] {1.1f, 1.2f, 1.3f}, new float[] {2.1f, 2.2f, 3.3f}});
            }
            else if (fieldInfos[iT].FieldType == typeof(List<bool[]>))
            {
                fieldInfos[iT].SetValue(dummyInstance, new List<bool[]>(){new bool[]{true, false, true}, new bool[]{false, true, false}});
            }
            else if (fieldInfos[iT].FieldType == typeof(List<Vector2[]>))
            {
                fieldInfos[iT].SetValue(dummyInstance,
                    new List<Vector2[]>()
                    {
                        new Vector2[] {new Vector2(1.1f, 1.2f), new Vector2(2.1f, 2.2f)},
                        new Vector2[] {new Vector2(3.1f, 3.2f), new Vector2(4.1f, 4.2f)}
                    });
            }
            else if (fieldInfos[iT].FieldType == typeof(List<Vector3[]>))
            {
                fieldInfos[iT].SetValue(dummyInstance, new List<Vector3[]>()
                {
                    new Vector3[] {new Vector3(1.1f, 1.2f, 1.3f), new Vector3(2.1f, 2.2f, 2.3f)},
                    new Vector3[] {new Vector3(3.1f, 3.2f, 3.3f), new Vector3(4.1f, 4.2f, 4.3f)}
                });
            }
            else if (fieldInfos[iT].FieldType == typeof(List<Vector4[]>))
            {
                fieldInfos[iT].SetValue(dummyInstance, new List<Vector4[]>()
                {
                    new Vector4[] {new Vector4(1.1f, 1.2f, 1.3f, 1.4f), new Vector4(2.1f, 2.2f, 2.3f, 2.4f)},
                    new Vector4[] {new Vector4(3.1f, 3.2f, 3.3f, 3.4f), new Vector4(4.1f, 4.2f, 4.3f, 4.4f)}
                });
            }
            else if (fieldInfos[iT].FieldType == typeof(List<int?[]>))
            {
                fieldInfos[iT].SetValue(dummyInstance, new List<int?[]>() {new int?[] {1, 2, null}, new int?[] {4, 5, null}});
            }
            else if (fieldInfos[iT].FieldType == typeof(List<float?[]>))
            {
                fieldInfos[iT].SetValue(dummyInstance,
                    new List<float?[]>() {new float?[] {1.1f, 1.2f, null}, new float?[] {2.1f, 2.2f, null}});
            }
            else if (fieldInfos[iT].FieldType == typeof(List<bool?[]>))
            {
                fieldInfos[iT].SetValue(dummyInstance, new List<bool?[]>(){new bool?[]{true, false, null}, new bool?[]{false, true, null}});
            }
            else if (fieldInfos[iT].FieldType == typeof(List<Vector2?[]>))
            {
                fieldInfos[iT].SetValue(dummyInstance,
                    new List<Vector2?[]>()
                    {
                        new Vector2?[] {new Vector2(1.1f, 1.2f), null},
                        new Vector2?[] {new Vector2(3.1f, 3.2f), null}
                    });
            }
            else if (fieldInfos[iT].FieldType == typeof(List<Vector3?[]>))
            {
                fieldInfos[iT].SetValue(dummyInstance, new List<Vector3?[]>()
                {
                    new Vector3?[] {new Vector3(1.1f, 1.2f, 1.3f), null},
                    new Vector3?[] {new Vector3(3.1f, 3.2f, 3.3f), null}
                });
            }
            else if (fieldInfos[iT].FieldType == typeof(List<Vector4?[]>))
            {
                fieldInfos[iT].SetValue(dummyInstance, new List<Vector4?[]>()
                {
                    new Vector4?[] {new Vector4(1.1f, 1.2f, 1.3f, 1.4f), null},
                    new Vector4?[] {new Vector4(3.1f, 3.2f, 3.3f, 3.4f), null}
                });
            }
            else if (fieldInfos[iT].FieldType == typeof(List<string>[]))
            {
                fieldInfos[iT].SetValue(dummyInstance,
                    new List<string>[]
                    {
                        new List<string>() {"SomeString1", "SomeString2", "SomeString3"},
                        new List<string>() {"SomeString4", "SomeString5", "SomeString6"}
                    });
            }
            else if (fieldInfos[iT].FieldType == typeof(List<int>[]))
            {
                fieldInfos[iT].SetValue(dummyInstance,
                    new[] {new List<int>() {1, 2, 3}, new List<int>() {4, 5, 6}});
            }
            else if (fieldInfos[iT].FieldType == typeof(List<float>[]))
            {
                fieldInfos[iT].SetValue(dummyInstance,
                    new List<float>[] {new List<float>() {1.1f, 1.2f, 1.3f}, new List<float>() {2.1f, 2.2f, 3.3f}});
            }
            else if (fieldInfos[iT].FieldType == typeof(List<bool>[]))
            {
                fieldInfos[iT].SetValue(dummyInstance,
                    new List<bool>[] {new List<bool>() {true, false, true}, new List<bool>() {false, true, false}});
            }
            else if (fieldInfos[iT].FieldType == typeof(List<Vector2>[]))
            {
                fieldInfos[iT].SetValue(dummyInstance,
                    new List<Vector2>[]
                    {
                        new List<Vector2> {new Vector2(1.1f, 1.2f), new Vector2(2.1f, 2.2f)},
                        new List<Vector2> {new Vector2(3.1f, 3.2f), new Vector2(4.1f, 4.2f)}
                    });
            }
            else if (fieldInfos[iT].FieldType == typeof(List<Vector3>[]))
            {
                fieldInfos[iT].SetValue(dummyInstance, new List<Vector3>[]
                {
                    new List<Vector3> {new Vector3(1.1f, 1.2f, 1.3f), new Vector3(2.1f, 2.2f, 2.3f)},
                    new List<Vector3> {new Vector3(3.1f, 3.2f, 3.3f), new Vector3(4.1f, 4.2f, 4.3f)}
                });
            }
            else if (fieldInfos[iT].FieldType == typeof(List<Vector4>[]))
            {
                fieldInfos[iT].SetValue(dummyInstance, new List<Vector4>[]
                {
                    new List<Vector4> {new Vector4(1.1f, 1.2f, 1.3f, 1.4f), new Vector4(2.1f, 2.2f, 2.3f, 2.4f)},
                    new List<Vector4> {new Vector4(3.1f, 3.2f, 3.3f, 3.4f), new Vector4(4.1f, 4.2f, 4.3f, 4.4f)}
                });
            }
            else if (fieldInfos[iT].FieldType == typeof(List<int?>[]))
            {
                fieldInfos[iT].SetValue(dummyInstance, new List<int?>[] {new List<int?> {1, 2, null}, new List<int?> {4, 5, null}});
            }
            else if (fieldInfos[iT].FieldType == typeof(List<float?>[]))
            {
                fieldInfos[iT].SetValue(dummyInstance,
                    new List<float?>[] {new List<float?> {1.1f, 1.2f, null}, new List<float?> {2.1f, 2.2f, null}});
            }
            else if (fieldInfos[iT].FieldType == typeof(List<bool?>[]))
            {
                fieldInfos[iT].SetValue(dummyInstance, new List<bool?>[]{new List<bool?>{true, false, null}, new List<bool?>{false, true, null}});
            }
            else if (fieldInfos[iT].FieldType == typeof(List<Vector2?>[]))
            {
                fieldInfos[iT].SetValue(dummyInstance,
                    new List<Vector2?>[]
                    {
                        new List<Vector2?> {new Vector2(1.1f, 1.2f), null},
                        new List<Vector2?> {new Vector2(3.1f, 3.2f), null}
                    });
            }
            else if (fieldInfos[iT].FieldType == typeof(List<Vector3?>[]))
            {
                fieldInfos[iT].SetValue(dummyInstance, new List<Vector3?>[]
                {
                    new List<Vector3?> {new Vector3(1.1f, 1.2f, 1.3f), null},
                    new List<Vector3?> {new Vector3(3.1f, 3.2f, 3.3f), null}
                });
            }
            else if (fieldInfos[iT].FieldType == typeof(List<Vector4?>[]))
            {
                fieldInfos[iT].SetValue(dummyInstance, new List<Vector4?>[]
                {
                    new List<Vector4?> {new Vector4(1.1f, 1.2f, 1.3f, 1.4f), null},
                    new List<Vector4?> {new Vector4(3.1f, 3.2f, 3.3f, 3.4f), null}
                });
            }
            else
            {
                test += GetCSharpRepresentation(fieldInfos[iT].FieldType, true) + "\n" +
                        fieldInfos[iT].FieldType.FullName +
                        "\n\n";
                Debug.Log("salkghfjlskfhglkasdjfhglksahdfg");
            }

            switch (fieldInfos[iT].FieldType.Name.ToLower())
            {
                // case "int":
                //     fi = typeof(T).GetField(fieldInfos[iT].Name);
                //     fi.SetValue(dummyInstance, 1);
                //     break;
                // case "float":
                //     fi = typeof(T).GetField(fieldInfos[iT].Name);
                //     fi.SetValue(dummyInstance, 1.5f);
                //     break;
                // case "string":
                //     fi = typeof(T).GetField(fieldInfos[iT].Name);
                //     fi.SetValue(dummyInstance, "SomeString");
                //     break;
                default:
                    break;
            }
        }

        File.WriteAllText("C:\\Users\\Owner\\Desktop\\test\\test.txt", test);
        PropertyInfo propertyInfo;
        return dummyInstance;
    }
    
    
    static string GetCSharpRepresentation( Type t, bool trimArgCount ) {
        if( t.IsGenericType ) {
            var genericArgs = t.GetGenericArguments().ToList();

            return GetCSharpRepresentation( t, trimArgCount, genericArgs );
        }

        return t.Name;
    }

    static string GetCSharpRepresentation( Type t, bool trimArgCount, List<Type> availableArguments ) {
        if( t.IsGenericType ) {
            string value = t.Name;
            if( trimArgCount && value.IndexOf("`") > -1 ) {
                value = value.Substring( 0, value.IndexOf( "`" ) );
            }

            if( t.DeclaringType != null ) {
                // This is a nested type, build the nesting type first
                value = GetCSharpRepresentation( t.DeclaringType, trimArgCount, availableArguments ) + "+" + value;
            }

            // Build the type arguments (if any)
            string argString = "";
            var thisTypeArgs = t.GetGenericArguments();
            for( int i = 0; i < thisTypeArgs.Length && availableArguments.Count > 0; i++ ) {
                if( i != 0 ) argString += ", ";

                argString += GetCSharpRepresentation( availableArguments[0], trimArgCount );
                availableArguments.RemoveAt( 0 );
            }

            // If there are type arguments, add them with < >
            if( argString.Length > 0 ) {
                value += "<" + argString + ">";
            }

            return value;
        }

        return t.Name;
    }
}

public class ConfigSelectionDialog : EditorWindow
{
    private void OnGUI()
    {
        EditorGUILayout.BeginVertical();
        List<string> configKeys = new List<string>(GenerateConfigTemplates.ConfigDict.Keys);
        foreach(string key in configKeys)
        {
            GenerateConfigTemplates.ConfigDict[key] = EditorGUILayout.Toggle(key, true);
        }

        EditorGUILayout.EndVertical();
        if (GUILayout.Button("Select destination folder"))
        {
            GenerateConfigTemplates.OpenDestinationSelectionDialog();
        }
    }
}


