
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.Playables;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

static class CreateTaskScene
{
    static public string TaskName, ScriptTemplateFolder, TaskLevelTemplateFile, TrialLevelTemplateFile, NameSpaceTemplateFile, USE_TaskFolder, USE_TaskFolderPath;
    
    [MenuItem("USE/Create Task")]
    static void CreateTask()
    {
        TaskNameSelectionWindow.GetWindow<TaskNameSelectionWindow>();
    }

    public static void Create(string taskName)
    {
        USE_TaskFolder = "USE_Tasks";
        USE_TaskFolderPath = "Assets/" + USE_TaskFolder;
        ScriptTemplateFolder = "Assets/ScriptTemplates";
        TaskLevelTemplateFile = "82-USE Scripts__Task Level-NewTaskLevel.cs.txt";
        TrialLevelTemplateFile = "82-USE Scripts__Trial Level-NewTrialLevel.cs.txt";
        NameSpaceTemplateFile = "82-USE Scripts__Task Namespace-NewTaskNamespace.cs.txt";
        TaskName = taskName;
        
        if (string.IsNullOrEmpty(taskName))
        {
            Debug.LogWarning("Attempted to create task with empty name, no further actions taken.");
            return;
        }
        else if (AssetDatabase.IsValidFolder(USE_TaskFolderPath + TaskName))
        {
            Debug.LogWarning("Attempted to create task with name " + TaskName + ", but a task with this name already exists, no further action taken.");
            return;
        }

        if (!AssetDatabase.IsValidFolder(USE_TaskFolder))
            AssetDatabase.CreateFolder("Assets", USE_TaskFolder);

        AssetDatabase.CreateFolder(USE_TaskFolderPath, TaskName);
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        GameObject ScriptObject = new GameObject(taskName + "Scripts");

        string namespacePath =
            CreateScriptFromTemplate(ScriptTemplateFolder + "/" + NameSpaceTemplateFile,
                TaskName + "_Namespace");
        string taskLevelPath =
            CreateScriptFromTemplate(ScriptTemplateFolder + "/" + TaskLevelTemplateFile,
                TaskName + "_TaskLevel");
        string trialLevelPath =
            CreateScriptFromTemplate(ScriptTemplateFolder + "/" + TrialLevelTemplateFile,
                TaskName + "_TrialLevel");

        Type taskType = Type.GetType(taskName + "_TaskLevel");
        Type trialType = Type.GetType(taskName + "_TrialLevel");
        
        Component taskLevel = ScriptObject.AddComponent(taskType);
        Component trialLevel = ScriptObject.AddComponent(trialType);
    }

    static string CreateScriptFromTemplate(string templatePath, string scriptName)
    {
        string template = File.ReadAllText(templatePath);
        string result = template.Replace("#TASKNAME#", TaskName);
        result = result.Replace("#NOTRIM#", "\n");
        string scriptPath = USE_TaskFolderPath + "/" + TaskName + "/" + scriptName + ".cs";
        File.WriteAllText(scriptPath, result);
        return scriptPath;
    }
}

public class TaskNameSelectionWindow : EditorWindow
{
    string textPrompt = "Input the desired task name: ";
    public string taskName = "";

    public void OnGUI()
    {
        taskName = EditorGUILayout.TextField(textPrompt, taskName);

        if (GUILayout.Button("Create new task"))
        {
            CreateTaskScene.Create(taskName);
            Close();
        }

        if (GUILayout.Button("Abort"))
        {
            Close();
        }
    }

}
