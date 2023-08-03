using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ConfigDynamicUI;
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
        //importSettings_Level.TaskLevel = TaskLevel;
        ImportSettings.AddChildLevel(importSettings_Level);
        ImportSettings.AddInitializationMethod(() =>
        {
            if (SessionValues.UsingDefaultConfigs)
                TaskLevel.TaskConfigPath += "_DefaultConfigs";

            if (SessionValues.UsingDefaultConfigs)
                WriteTaskConfigsToPersistantDataPath();

            TaskLevel.SpecifyTypes();

            importSettings_Level.SettingsDetails = new List<SettingsDetails>()
            {
                new SettingsDetails(TaskLevel.TaskConfigPath, "TaskDef", TaskLevel.TaskDefType),
                new SettingsDetails(TaskLevel.TaskConfigPath, "BlockDef", TaskLevel.BlockDefType),
                new SettingsDetails(TaskLevel.TaskConfigPath, "TrialDef", TaskLevel.TrialDefType),
                new SettingsDetails(TaskLevel.TaskConfigPath, "StimDef", TaskLevel.StimDefType),
                new SettingsDetails(TaskLevel.TaskConfigPath, "EventCode", typeof(Dictionary<string, EventCode>)),
                new SettingsDetails(TaskLevel.TaskConfigPath, "ConfigUi", typeof(ConfigVarStore)),
            };

            TaskLevel.customSettings = new List<CustomSettings>();
            TaskLevel.DefineCustomSettings();
            
            foreach (CustomSettings customSettingsType in TaskLevel.customSettings)
            {
                importSettings_Level.SettingsDetails.Add(new SettingsDetails(TaskLevel.TaskConfigPath,
                    customSettingsType.SearchString, customSettingsType.SettingsType));
            }

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
                    else 
                    {
                        foreach (CustomSettings customSetting in TaskLevel.customSettings)
                        {
                            object[] parameters = new object[] {parsedResult};
                            MethodInfo SettingsConverter_methodTask = GetType()
                                .GetMethod(nameof(this.SettingsConverterCustom))
                                .MakeGenericMethod(new Type[] { customSetting.SettingsType });
                            customSetting.ParsedResult =  SettingsConverter_methodTask.Invoke(this, parameters);
                            
                            Debug.Log(TaskLevel.TaskName + " " + customSetting.SearchString + " file imported.");

                        }
                        
                    }
                    
                }

                fileParsed = true;
                importSettings_Level.importPaused = false;
            }
        });
        ImportSettings.SpecifyTermination(() => ImportSettings.ChildLevel.Terminated, HandleTrialAndBlockDefs,
            () => Debug.Log("ImportSettings state terminated."));

        HandleTrialAndBlockDefs.AddInitializationMethod(() => { TaskLevel.HandleTrialAndBlockDefs(true); });
        HandleTrialAndBlockDefs.SpecifyTermination(() => TaskLevel.TrialAndBlockDefsHandled, FindStims);

        FindStims.AddInitializationMethod(() =>
        {
            TaskLevel.TaskStims = new TaskStims();
            if (TaskLevel.PrefabStims == null)
                TaskLevel.PrefabStims = new StimGroup("PrefabStims");
            if (TaskLevel.PreloadedStims == null)
                TaskLevel.PreloadedStims = new StimGroup("PreloadedStims");
            if (TaskLevel.ExternalStims == null)
                TaskLevel.ExternalStims = new StimGroup("ExternalStims");
            if (TaskLevel.RuntimeStims == null)
                TaskLevel.RuntimeStims = new StimGroup("RuntimeStims");

            TaskLevel.FindStims();
        });
        FindStims.SpecifyTermination(() => TaskLevel.StimsHandled, () => null);
    }

    public void ContinueToNextSetting()
    {
        importSettings_Level.importPaused = false;
    }


    private void WriteTaskConfigsToPersistantDataPath()
    {
        if (!SessionValues.UsingDefaultConfigs)
            return;

        if (Directory.Exists(TaskLevel.TaskConfigPath))
            Directory.Delete(TaskLevel.TaskConfigPath, true);

        Directory.CreateDirectory(TaskLevel.TaskConfigPath);

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
            configTextAsset = Resources.Load<TextAsset>("DefaultSessionConfigs/" + TaskLevel.TaskName + "_DefaultConfigs/" + TaskLevel.TaskName + entry.Key);
            if (configTextAsset == null) //try it without task name (cuz MazeDef.txt doesnt have MazeGame in front of it)
                configTextAsset = Resources.Load<TextAsset>("DefaultSessionConfigs/" + TaskLevel.TaskName + "_DefaultConfigs/" + entry.Key);
            if (configTextAsset != null)
                File.WriteAllBytes(TaskLevel.TaskConfigPath + Path.DirectorySeparatorChar + TaskLevel.TaskName + entry.Value,
                    configTextAsset.bytes);
        }
    }


    private void SetValuesForLoading(string searchString)
    {
        importSettings_Level.SettingsDetails[0].SearchString = searchString;

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
                    typeof(Dictionary<string, EventCode>);
                break;
            case "configui":
                importSettings_Level.SettingsDetails[0].SettingType =
                    typeof(ConfigVarStore);
                break;
            default:
                Debug.LogError("SET VALUES FOR LOADING DEFAULT SWITCH STATEMENT!");
                break;
        }

    }

    public void SettingsConverterTask<T>(object parsedSettings) where T : TaskDef
    {
        TaskLevel.TaskDef = (T) parsedSettings;
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
        else if (SessionValues.UsingLocalConfigs || SessionValues.UsingServerConfigs)
            TaskLevel.ExternalStims = new StimGroup("ExternalStims", (T[]) parsedSettings);
    }

    public T SettingsConverterCustom<T>(object parsedSettings)
    {
        return (T)parsedSettings;
    }

}
