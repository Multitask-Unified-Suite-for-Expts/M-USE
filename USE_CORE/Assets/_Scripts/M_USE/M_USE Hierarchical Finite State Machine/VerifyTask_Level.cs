using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Reflection;
using ConfigDynamicUI;
using Tobii.Research.Unity.CodeExamples;
using UnityEngine;
using USE_Def_Namespace;
using USE_ExperimentTemplate_Classes;
using USE_ExperimentTemplate_Task;
using USE_States;
using USE_StimulusManagement;

public class VerifyTask_Level : ControlLevel
{
    public ImportSettings_Level importSettings_Level;
    public bool fileParsed;
    public string currentFileName;
    public object parsedResult = null;
    public ControlLevel_Task_Template TaskLevel;


    public override void DefineControlLevel()
    {
        State ImportSettings = new State("ImportSettings");
        State HandleTrialAndBlockDefs = new State("HandleTrialAndBlockDefs");
        State FindStims = new State("FindStims");

        AddActiveStates(new List<State> {ImportSettings, HandleTrialAndBlockDefs, FindStims});

        importSettings_Level = GameObject.Find("ControlLevels").GetComponent<ImportSettings_Level>();
        ImportSettings.AddChildLevel(importSettings_Level);
        ImportSettings.AddInitializationMethod(() =>
        {
            //GOTTA FIND WHERE TO PUT THIS!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!:
            if (SessionValues.UsingDefaultConfigs)
                WriteTaskConfigsToPersistantDataPath(TaskLevel);
            TaskLevel.SpecifyTypes();
            importSettings_Level.SettingsDetails = new List<SettingsDetails>()
            {
                new SettingsDetails("TaskDef", TaskLevel.TaskDefType),
                new SettingsDetails("BlockDef", TaskLevel.BlockDefType),
                new SettingsDetails("TrialDef", TaskLevel.TrialDefType),
                new SettingsDetails("StimDef", TaskLevel.StimDefType),
                new SettingsDetails("EventCode", typeof(Dictionary<string, EventCode>)),
                new SettingsDetails("ConfigUi", typeof(ConfigVarStore)),
            };
            // SetValuesForLoading("TaskDef");

            importSettings_Level.SettingsDetails[0].FilePath = GetFilePath("TaskDef");
        });

        ImportSettings.AddUpdateMethod(() =>
        {
            if (importSettings_Level.fileLoadingFinished)
            {
                importSettings_Level.importPaused = false;
            }

            if (importSettings_Level.fileParsed)
            {
                currentFileName = importSettings_Level.currentSettingsDetails.FilePath;
                parsedResult = importSettings_Level.parsedResult;
                Type currentType = importSettings_Level.currentSettingsDetails.SettingType;

                if (parsedResult != null)
                {
                    if (currentType.Equals(TaskLevel.TaskDefType))
                    {
                        MethodInfo SettingsConverter_methodTask = GetType()
                            .GetMethod(nameof(this.SettingsConverterTask)).MakeGenericMethod(new Type[] {currentType});
                        SettingsConverter_methodTask.Invoke(this, new object[] {parsedResult});
                        Debug.Log(TaskLevel.TaskName + " TaskDef imported.");
                    }
                    else if (currentType.Equals(TaskLevel.BlockDefType))
                    {
                        MethodInfo SettingsConverter_methodTask = GetType()
                            .GetMethod(nameof(this.SettingsConverterBlock)).MakeGenericMethod(new Type[] {currentType});
                        SettingsConverter_methodTask.Invoke(this, new object[] {parsedResult});
                        Debug.Log(TaskLevel.TaskName + " " + TaskLevel.BlockDefs.Length + " BlockDefs imported.");
                    }
                    else if (currentType.Equals(TaskLevel.TrialDefType))
                    {
                        MethodInfo SettingsConverter_methodTask = GetType()
                            .GetMethod(nameof(this.SettingsConverterTrial)).MakeGenericMethod(new Type[] {currentType});
                        SettingsConverter_methodTask.Invoke(this, new object[] {parsedResult});
                        Debug.Log(TaskLevel.TaskName + " " + TaskLevel.AllTrialDefs.Length + " TrialDefs imported.");
                    }
                    else if (currentType.Equals(TaskLevel.StimDefType))
                    {
                        MethodInfo SettingsConverter_methodTask = GetType()
                            .GetMethod(nameof(this.SettingsConverterStim)).MakeGenericMethod(new Type[] {currentType});
                        SettingsConverter_methodTask.Invoke(this, new object[] {parsedResult});
                        if(SessionValues.UsingLocalConfigs)
                            Debug.Log(TaskLevel.TaskName + " " + TaskLevel.ExternalStims.stimDefs.Count +
                                  " External StimDefs imported.");
                        else if (SessionValues.UsingDefaultConfigs)
                            Debug.Log(TaskLevel.TaskName + " " + TaskLevel.PrefabStims.stimDefs.Count +
                                      " Prefab StimDefs imported.");
                    }
                    else if (currentType.Equals(typeof(Dictionary<string, EventCode>)))
                    {
                        TaskLevel.CustomTaskEventCodes = (Dictionary<string, EventCode>) parsedResult;
                        Debug.Log(TaskLevel.TaskName + " " + TaskLevel.CustomTaskEventCodes.Count +
                                  " Event Codes imported.");
                    }
                    else if (currentType.Equals(typeof(ConfigVarStore)))
                    {
                        TaskLevel.ConfigUiVariables = (ConfigVarStore) parsedResult;
                        Debug.Log(TaskLevel.TaskName + " " + TaskLevel.ConfigUiVariables.getAllVariables().Count +
                                  " Config UI Variables imported.");
                    }
                }



                fileParsed = true;

                if (importSettings_Level.SettingsDetails.Count > 1)
                    importSettings_Level.SettingsDetails[1].FilePath =
                        GetFilePath(importSettings_Level.SettingsDetails[1].SearchString);


                importSettings_Level.importPaused = false;
            }
        });
        ImportSettings.SpecifyTermination(() => ImportSettings.ChildLevel.Terminated, HandleTrialAndBlockDefs,
            () => Debug.Log("ImportSettings state terminated."));

        HandleTrialAndBlockDefs.AddInitializationMethod(() => { TaskLevel.HandleTrialAndBlockDefs(true); });
        HandleTrialAndBlockDefs.SpecifyTermination(() => TaskLevel.TrialAndBlockDefsHandled, FindStims);

        FindStims.AddInitializationMethod(() =>
        {
            Debug.Log("External stims: " + TaskLevel.ExternalStims);

            TaskLevel.TaskStims = new TaskStims();
            if (TaskLevel.PrefabStims == null)
                TaskLevel.PrefabStims = new StimGroup("PrefabStims");
            if (TaskLevel.PreloadedStims == null)
                TaskLevel.PreloadedStims = new StimGroup("PreloadedStims");
            if (TaskLevel.ExternalStims == null)
                TaskLevel.ExternalStims = new StimGroup("ExternalStims");
            TaskLevel.FindStims();
        });
        FindStims.SpecifyTermination(() => TaskLevel.StimsHandled, () => null);
    }

    public void ContinueToNextSetting()
    {
        importSettings_Level.importPaused = false;
    }


    //WHERE SHOULD WE CALL THIS METHOD?!?!?! 
    private void WriteTaskConfigsToPersistantDataPath(ControlLevel_Task_Template tl)
    {
        if (!SessionValues.UsingDefaultConfigs)
            return;

        Debug.Log("ABOUT TO WRITE TASK CONFIGS FOR: " + tl.TaskName);

        tl.TaskConfigPath = $"{SessionValues.ConfigFolderPath}/{tl.TaskName}_DefaultConfigs";
        Debug.Log("TASK CONFIG PATH: " + tl.TaskConfigPath);

        if (Directory.Exists(tl.TaskConfigPath))
            Directory.Delete(tl.TaskConfigPath, true);

        Debug.Log("CREATING A DIRECTORY AT: " + tl.TaskConfigPath);
        Directory.CreateDirectory(tl.TaskConfigPath);

        Dictionary<string, string> configDict = new Dictionary<string, string>
        {
            {"_TaskDef_singleType", "_TaskDef_singleType.txt"},
            {"_BlockDef_array", "_BlockDef_array.txt"},
            {"_TrialDef_array", "_TrialDef_array.txt"},
            {"_StimDef_array", "_StimDef_array.txt"},
            {"_ConfigUiDetails_json", "_ConfigUiDetails_json.json"},
            {"_EventCodeConfig_json", "_EventCodeConfig_json.json"},
            {"MazeDef_array", "MazeDef_array.txt"}
        };
        TextAsset configTextAsset;
        foreach (var entry in configDict)
        {
            configTextAsset = Resources.Load<TextAsset>("DefaultSessionConfigs/" + tl.TaskName + "_DefaultConfigs/" +
                                                        tl.TaskName + entry.Key);
            if (configTextAsset ==
                null) //try it without task name (cuz MazeDef.txt doesnt have MazeGame in front of it)
                configTextAsset =
                    Resources.Load<TextAsset>("DefaultSessionConfigs/" + tl.TaskName + "_DefaultConfigs/" + entry.Key);
            if (configTextAsset != null)
                File.WriteAllBytes(tl.TaskConfigPath + Path.DirectorySeparatorChar + tl.TaskName + entry.Value,
                    configTextAsset.bytes);
        }
    }



    private string GetFilePath(string searchString)
    {
        Debug.Log("Looking for file with search string " + searchString);

        string pathToFile = "";
        string pathToFolder = "";

        if (SessionValues.UsingDefaultConfigs)
            pathToFolder = $"{SessionValues.ConfigFolderPath}/{TaskLevel.TaskName}_DefaultConfigs";
        else if (SessionValues.UsingLocalConfigs)
            pathToFolder = $"{SessionValues.ConfigFolderPath}{TaskLevel.TaskName}";
        else if (SessionValues.UsingServerConfigs)
            pathToFolder = $"{SessionValues.ConfigFolderPath}/{TaskLevel.TaskName}";


        if (SessionValues.UsingServerConfigs)
        {
            StartCoroutine(ServerManager.GetFilePath(pathToFolder, searchString, result =>
            {
                if (!string.IsNullOrEmpty(result))
                    pathToFile = result;
                else
                    Debug.Log("Server GetFilePath() Result is null for: " + searchString);
            }));
        }
        else
            pathToFile = SessionValues.LocateFile.FindFilePathInExternalFolder(pathToFolder, $"*{searchString}*");

        return pathToFile;
    }

    private void SetValuesForLoading(string searchString)
    {
        importSettings_Level.SettingsDetails[0].SearchString = searchString;
        GetFilePath(importSettings_Level.SettingsDetails[0].SearchString);

        switch (searchString.ToLower())
        {
            case "taskdef":
                importSettings_Level.SettingsDetails[0].SettingType = TaskLevel.TaskDefType;
                break;
            case "blockdef":
                importSettings_Level.SettingsDetails[0].SettingType = TaskLevel.BlockDefType;
                break;
            case "trialdef":
                importSettings_Level.SettingsDetails[0].SettingType = TaskLevel.TrialDefType;
                break;
            case "stimdef":
                importSettings_Level.SettingsDetails[0].SettingType = TaskLevel.StimDefType;
                break;
            case "eventcode":
                importSettings_Level.SettingsDetails[0].SettingType =
                    typeof(Dictionary<string, EventCode>); //this correct for event code?
                break;
            case "configui":
                importSettings_Level.SettingsDetails[0].SettingType =
                    typeof(ConfigVarStore); //this correct for ConfigUI?
                break;
            default:
                Debug.LogError("SET VALUES FOR LOADING DEFAULT SWITCH STATEMENT!");
                break;
        }

    }

    public void SettingsConverterTask<T>(object parsedSettings) where T : TaskDef
    {
        Debug.Log(parsedSettings);
        TaskLevel.TaskDef = (T) parsedSettings;
        Debug.Log(TaskLevel.TaskDef.FeedbackControllers[0]);
    }

    public void SettingsConverterBlock<T>(object parsedSettings) where T : BlockDef
    {
        TaskLevel.BlockDefs = (T[]) parsedSettings;
    }

    public void SettingsConverterTrial<T>(object parsedSettings) where T : TrialDef
    {
        TaskLevel.AllTrialDefs = (T[]) parsedSettings;
    }

    public void SettingsConverterStim<T>(object parsedSettings) where T : StimDef
    {
        if (SessionValues.UsingDefaultConfigs)
            TaskLevel.PrefabStims = new StimGroup("PrefabStims", (T[]) parsedSettings);
        else if (SessionValues.UsingLocalConfigs)
            TaskLevel.ExternalStims = new StimGroup("ExternalStims", (T[]) parsedSettings);
    }
}
