
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.SceneManagement;
using USE_ExperimentTemplate;

static class TaskCreation
{
    static private string TaskName,
        ScriptTemplateFolder,
        TaskLevelTemplateFile,
        TrialLevelTemplateFile,
        NameSpaceTemplateFile,
        TaskTypeTemplateFile,
        TaskTypeDefinitionFile,
        USE_TaskFolder,
        USE_TaskFolderPath,
        TaskFolder,
        UsingDirectivesEndComment,
        CustomTypesEndComment;
    static private Dictionary<string, string> TemplateStringConstants;
    static private ControlLevel_Task_Template TaskLevel;
    static private ControlLevel_Trial_Template TrialLevel;

    [MenuItem("USE/Create Task Scripts")]
    static void OpenTaskNameWindow()
    {
        TaskNameSelectionWindow.GetWindow<TaskNameSelectionWindow>();
    }

    [MenuItem("USE/Create Task Scene")]
    static void UpdateTypeReferences()
    {
        GetTaskNameForUpdating.GetWindow<GetTaskNameForUpdating>();
    }

    private static void UpdateFields(string taskName)
    {
        TaskName = taskName;
        USE_TaskFolder = "_USE_Tasks";
        USE_TaskFolderPath = "Assets/" + USE_TaskFolder;
        TaskFolder = USE_TaskFolderPath + "/" + TaskName;
        ScriptTemplateFolder = "Assets/ScriptTemplates";
        TaskLevelTemplateFile = "82-USE Scripts__Task Level-NewTaskLevel.cs.txt";
        TrialLevelTemplateFile = "82-USE Scripts__Trial Level-NewTrialLevel.cs.txt";
        NameSpaceTemplateFile = "82-USE Scripts__Task Namespace-NewTaskNamespace.cs.txt";
        TaskTypeTemplateFile = "TaskTypesDictionaryTemplate.txt";
        TaskTypeDefinitionFile = "USE_Tasks_CustomTypes.cs";
        UsingDirectivesEndComment = "//#END OF USING DIRECTIVES - DO NOT DELETE THIS LINE#";
        CustomTypesEndComment = "//#END OF CUSTOMTASKTYPES DICTIONARY - DO NOT DELETE THIS LINE#";
        TemplateStringConstants = new Dictionary<string, string>() {{"#TASKNAME#",TaskName}, {"#NOTRIM#", "\n"}};
    }
    
    public static void CreateTaskScripts(string taskName)
    {
        UpdateFields(taskName);

        if (string.IsNullOrEmpty(taskName))
        {
            Debug.LogWarning("Attempted to create task with empty name, no further actions taken.");
            return;
        }
        else if (AssetDatabase.IsValidFolder(TaskFolder))
        {
            Debug.LogWarning("Attempted to create task with name " + TaskName + ", but a task with this name already exists, no further action taken.");
            return;
        }

        if (!AssetDatabase.IsValidFolder(USE_TaskFolderPath))
            AssetDatabase.CreateFolder("Assets", USE_TaskFolder);

        AssetDatabase.CreateFolder(USE_TaskFolderPath, TaskName);

        CreateScriptFromTemplate(ScriptTemplateFolder + "/" + NameSpaceTemplateFile,
            TaskName + "_Namespace", TaskFolder);
        CreateScriptFromTemplate(ScriptTemplateFolder + "/" + TaskLevelTemplateFile,
            TaskName + "_TaskLevel", TaskFolder);
        CreateScriptFromTemplate(ScriptTemplateFolder + "/" + TrialLevelTemplateFile,
            TaskName + "_TrialLevel", TaskFolder);

        AddToScriptFromString("using " + TaskName + "_Namespace;", TaskTypeDefinitionFile,
            USE_TaskFolderPath, UsingDirectivesEndComment);
        AddToScriptFromTemplate(ScriptTemplateFolder + "/" + TaskTypeTemplateFile, TaskTypeDefinitionFile,
            USE_TaskFolderPath, CustomTypesEndComment);
        
        AssetDatabase.Refresh();
    }

    static void CreateScriptFromTemplate(string templatePath, string scriptName, string destinationFolder)
    {
        string newScript = ReplaceConstants(File.ReadAllText(templatePath));
        string scriptPath = destinationFolder + "/" + scriptName + ".cs";
        File.WriteAllText(scriptPath, newScript);
    }

    static void AddToScriptFromTemplate(string templatePath, string scriptName, string scriptFolder,
        string editPointMarker)
    {
        string textToAdd = ReplaceConstants(File.ReadAllText(templatePath));
        AddToScriptFromString(textToAdd, scriptName, scriptFolder, editPointMarker);
    }

    static void AddToScriptFromString(string textToAdd, string scriptName, string scriptFolder,
        string editPointMarker)
    {
        string originalScript = File.ReadAllText(scriptFolder + Path.DirectorySeparatorChar + scriptName);
        string editedScript = originalScript.Replace(editPointMarker, textToAdd + "\n" + editPointMarker);
        File.WriteAllText(scriptFolder + Path.DirectorySeparatorChar+scriptName, editedScript);
    }

    static private string ReplaceConstants(string text)
    {
        foreach (string key in TemplateStringConstants.Keys)
            text = text.Replace(key, TemplateStringConstants[key]);

        return text;
    }
    
    public static void CreateTaskScene(string taskName)
    {
        UpdateFields(taskName);
        
        if (!AssetDatabase.IsValidFolder(USE_TaskFolderPath + "/" + TaskName))
        {
            Debug.LogError("A folder with the name " + TaskName + " has not yet been created. Run the USE/Create Task Scripts menu command first.");
            return;
        }
        
        var taskScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        taskScene.name = TaskName;
        GameObject.Find("Main Camera").name = TaskName + "_Camera";
        GameObject scriptObject = new GameObject(TaskName + "_Scripts");
        Type taskType = USE_Tasks_CustomTypes.CustomTaskDictionary[TaskName].TaskLevelType;
        Type trialType = USE_Tasks_CustomTypes.CustomTaskDictionary[TaskName].TrialLevelType;

        var methodInfo = typeof(ReflectionTypes).GetMethod(nameof(ReflectionTypes.UpdateTaskLevel));
        MethodInfo updateTaskLevel = methodInfo.MakeGenericMethod(new Type[] {taskType});
        TaskLevel = (ControlLevel_Task_Template)updateTaskLevel.Invoke(new ReflectionTypes(), new object[] {scriptObject});
        
        methodInfo = typeof(ReflectionTypes).GetMethod(nameof(ReflectionTypes.UpdateTrialLevel));
        MethodInfo updateTrialLevel = methodInfo.MakeGenericMethod(new Type[] {trialType});
        TrialLevel = (ControlLevel_Trial_Template)updateTrialLevel.Invoke(new ReflectionTypes(), new object[] {scriptObject});
        

        TaskLevel.ControlLevelName = taskName + "_TaskLevel";
        TaskLevel.isMainLevel = false;
        TaskLevel.CallDefineLevelAutomatically = false;
        TaskLevel.quitApplicationAtEnd = false;
        TaskLevel.DebugActive = true;
        TaskLevel.TaskName = TaskName;
        TaskLevel.TrialLevel = TrialLevel;
        
        TrialLevel.ControlLevelName = taskName + "_TrialLevel";
        TrialLevel.isMainLevel = false;
        TrialLevel.CallDefineLevelAutomatically = false;
        TrialLevel.quitApplicationAtEnd = false;
        TrialLevel.DebugActive = true;

        EditorSceneManager.SaveScene(taskScene, TaskFolder + "/" + TaskName + ".unity");
    }


}

public class ReflectionTypes
{
    public ControlLevel_Trial_Template UpdateTrialLevel<T>(GameObject scriptObject) where T : ControlLevel_Trial_Template
    {
        return scriptObject.AddComponent(typeof(T)) as T;
    }
        
    public ControlLevel_Task_Template UpdateTaskLevel<T>(GameObject scriptObject) where T : ControlLevel_Task_Template
    {
        return scriptObject.AddComponent(typeof(T)) as T;
    }
}

public class TaskNameSelectionWindow : EditorWindow
{
    string textPrompt = "Input the desired task name: ";
    public string taskName = "";

    public void OnGUI()
    {
        taskName = EditorGUILayout.TextField(textPrompt, taskName);

        if (GUILayout.Button("Create task scripts"))
        {
            TaskCreation.CreateTaskScripts(taskName);
            Close();
        }

        if (GUILayout.Button("Abort"))
        {
            Close();
        }
    }

}

public class GetTaskNameForUpdating : EditorWindow
{
    string textPrompt = "Input the desired task name: ";
    public string taskName = "";

    public void OnGUI()
    {
        taskName = EditorGUILayout.TextField(textPrompt, taskName);

        if (GUILayout.Button("Create Task Scene"))
        {
            TaskCreation.CreateTaskScene(taskName);
            Close();
        }

        if (GUILayout.Button("Abort"))
        {
            Close();
        }
    }

}