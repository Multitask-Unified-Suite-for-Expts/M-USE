using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using System.IO;
using System.Linq;
using USE_ExperimentTemplate;
using Newtonsoft.Json;

public class GenerateConfigTemplates
{
    static public string TaskFolderPath;
    static public string TaskConfigFolder;
    static public string TaskName;
    static public Type[] ConfigTypes;
    
    [MenuItem("USE/Create Config File Templates")]
    static void OpenTaskSelectionDialog()
    {
        TaskSelectionDialog.GetWindow<TaskSelectionDialog>();
        // TaskFolder = EditorUtility.OpenFolderPanel("Select Task Folder", "Assets/_USE_Tasks", "");
        
    }

    static public void OpenConfigSelectionDialog()
    {
        ConfigSelectionDialog.GetWindow<ConfigSelectionDialog>();
    }

    static void CreateFiles(Type[] configTypes, string taskConfigFolder = "")
    {
        if (!string.IsNullOrEmpty(taskConfigFolder))
            TaskConfigFolder = taskConfigFolder;
        foreach (Type cType in configTypes)
        {
            string templateFolder = TaskConfigFolder + Path.DirectorySeparatorChar + "ConfigFileTemplates" +
                                    Path.DirectorySeparatorChar + cType.Name;
            Directory.CreateDirectory(templateFolder);

            string fileName = "templateFolder + Path.DirectorySeparatorChar + TaskName";
            
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
        T dummyInstance = new T();
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
            arrayTemplate += "\t" + listValues[iName];
        }
        File.WriteAllText(filePath, arrayTemplate);
    }
    public static void CreateSingleTypeJsonTemplate<T>(string filePath = "") where T:new()
    {
        T dummyInstance = new T();
        string arrayTemplate = JsonConvert.SerializeObject(dummyInstance);
        File.WriteAllText(filePath, arrayTemplate);
    }
    public static void CreateMultipleTypeTdfTemplate<T>(string filePath = "") where T:new()
    {
        // adapted from https://robscode.onl/c-get-member-variables-of-class/ 
        var bindingFlags = System.Reflection.BindingFlags.Instance |
                           System.Reflection.BindingFlags.NonPublic |
                           System.Reflection.BindingFlags.Public;
        T dummyInstance = new T();
        List<object> listValues = typeof(T).GetFields(bindingFlags).Select(field => field.GetValue(dummyInstance)).ToList();
        List<string> listNames = typeof(T).GetFields(bindingFlags).Select(field => field.Name).ToList();
        string arrayTemplate = "";
        for (int iName = 1; iName < listValues.Count; iName++)
        {
            arrayTemplate += listValues[iName].GetType().Name + "\t" + listNames[iName] + "\t" + listValues[iName];
            if (iName < listValues.Count - 1)
                arrayTemplate += "\n";
        }
        File.WriteAllText(filePath, arrayTemplate);
    }
}

public class TaskSelectionDialog : EditorWindow
{
    private string taskFolderPath;

    private void OnGUI()
    {
        taskFolderPath = EditorUtility.OpenFolderPanel("Select Task Folder", "Assets/_USE_Tasks", "");
    }

    void OnDestroy()
    {
        GenerateConfigTemplates.TaskFolderPath = taskFolderPath;
        GenerateConfigTemplates.OpenConfigSelectionDialog();
    }
}
public class ConfigSelectionDialog : EditorWindow
{
    private void OnGUI()
    {
        // bool[] 
    }
}