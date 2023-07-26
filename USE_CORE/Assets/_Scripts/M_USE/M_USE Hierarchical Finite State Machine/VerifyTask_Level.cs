using System;
using System.Collections.Generic;
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
    public ControlLevel_Task_Template CurrentTask;


    public override void DefineControlLevel()
    {
        State ImportSettings = new State("ImportSettings");
        State HandleTrialAndBlockDefs = new State("HandleTrialAndBlockDefs");
        State FindStims = new State("FindStims");

        AddActiveStates(new List<State> { ImportSettings, HandleTrialAndBlockDefs, FindStims });

        importSettings_Level = GameObject.Find("ControlLevels").GetComponent<ImportSettings_Level>();
        ImportSettings.AddChildLevel(importSettings_Level);
        ImportSettings.AddInitializationMethod(() =>
        {
            CurrentTask.SpecifyTypes();
            importSettings_Level.SettingsDetails = new List<SettingsDetails>()
            {
                new SettingsDetails("TaskDef", CurrentTask.TaskDefType),
                new SettingsDetails("BlockDef", CurrentTask.BlockDefType),
                new SettingsDetails("TrialDef", CurrentTask.TrialDefType),
                new SettingsDetails("StimDef", CurrentTask.StimDefType),
                new SettingsDetails("EventCode", typeof(Dictionary<string, EventCode>)),
                new SettingsDetails("ConfigUI", typeof(ConfigVarStore)),
            };
            // SetValuesForLoading("TaskDef");
            
            importSettings_Level.SettingsDetails[0].FilePath = SetFilePath("TaskDef");
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
                // CurrentTask.
                parsedResult = importSettings_Level.parsedResult;
                Type currentType = importSettings_Level.currentSettingsDetails.SettingType;

                if (parsedResult != null)
                {
                    if (currentType.Equals(CurrentTask.TaskDefType))
                    {
                        MethodInfo SettingsConverter_methodTask = GetType()
                            .GetMethod(nameof(this.SettingsConverterTask)).MakeGenericMethod(new Type[] {currentType});
                        SettingsConverter_methodTask.Invoke(this, new object[] {parsedResult});
                        Debug.Log(CurrentTask.TaskName + " TaskDef imported.");
                    }
                    else if (currentType.Equals(CurrentTask.BlockDefType))
                    {
                        MethodInfo SettingsConverter_methodTask = GetType()
                            .GetMethod(nameof(this.SettingsConverterBlock)).MakeGenericMethod(new Type[] {currentType});
                        SettingsConverter_methodTask.Invoke(this, new object[] {parsedResult});
                        Debug.Log(CurrentTask.TaskName + " " + CurrentTask.BlockDefs.Length + " BlockDefs imported.");
                    }
                    else if (currentType.Equals(CurrentTask.TrialDefType))
                    {
                        MethodInfo SettingsConverter_methodTask = GetType()
                            .GetMethod(nameof(this.SettingsConverterTrial)).MakeGenericMethod(new Type[] {currentType});
                        SettingsConverter_methodTask.Invoke(this, new object[] {parsedResult});
                        Debug.Log(CurrentTask.TaskName + " " + CurrentTask.AllTrialDefs.Length + " TrialDefs imported.");
                    }
                    else if (currentType.Equals(CurrentTask.StimDefType))
                    {
                        MethodInfo SettingsConverter_methodTask = GetType()
                            .GetMethod(nameof(this.SettingsConverterStim)).MakeGenericMethod(new Type[] {currentType});
                        SettingsConverter_methodTask.Invoke(this, new object[] {parsedResult});
                        Debug.Log(CurrentTask.TaskName + " " + CurrentTask.ExternalStims.stimDefs.Count +
                                  " External StimDefs imported.");
                    }
                    else if (currentType.Equals(typeof(Dictionary<string, EventCode>)))
                    {
                        CurrentTask.CustomTaskEventCodes = (Dictionary<string, EventCode>) parsedResult;
                        Debug.Log(CurrentTask.TaskName + " " + CurrentTask.CustomTaskEventCodes.Count +
                                  " Event Codes imported.");
                    }
                    else if (currentType.Equals(typeof(ConfigVarStore)))
                    {
                        CurrentTask.ConfigUiVariables = (ConfigVarStore) parsedResult;
                        Debug.Log(CurrentTask.TaskName + " " + CurrentTask.ConfigUiVariables.getAllVariables().Count +
                                  " Config UI Variables imported.");
                    }
                }



                fileParsed = true;

                if (importSettings_Level.SettingsDetails.Count > 1)
                    importSettings_Level.SettingsDetails[1].FilePath = SetFilePath(importSettings_Level.SettingsDetails[1].SearchString);
                    
                // MethodInfo readBlockDefs = GetType().GetMethod(nameof(this.SettingsConverter))
                //     .MakeGenericMethod(new Type[] { BlockDefType });
                //
                // var methodInfo = GetType().GetMethod(nameof(nameof(SettingsConverter)));
                // if (currentFileName.ToLower().Contains("taskdef"))
                //     SetValuesForLoading("BlockDef");
                // else if (currentFileName.ToLower().Contains("blockdef"))
                //     SetValuesForLoading("TrialDef");
                // else if (currentFileName.ToLower().Contains("trialdef"))
                //     SetValuesForLoading("StimDef");
                // else if (currentFileName.ToLower().Contains("stimdef"))
                //     SetValuesForLoading("EventCode");
                // else if (currentFileName.ToLower().Contains("eventcode"))
                //     SetValuesForLoading("ConfigUi");
                // else if (currentFileName.ToLower().Contains("configui"))
                //     Debug.Log("Parsed ConfigUI and no more to parse");
                // else if (string.IsNullOrEmpty(currentFileName))
                //     Debug.Log("Attempted to find settings file using search string " + importSettings_Level.currentSettingsDetails.SearchString + "but no corresponding file was found (this may not be a problem if it is not required)");
                // else
                //     Debug.LogError($"{currentFileName} has been parsed, but is not a TaskDef, BlockDef, TrialDef, StimDef, EventCode, or ConfigUI.");

                importSettings_Level.importPaused = false;
            }
        });
        ImportSettings.SpecifyTermination(() => ImportSettings.ChildLevel.Terminated, HandleTrialAndBlockDefs, () => Debug.Log("DONE WITH IMPORT SETTINGS STATE!"));

        HandleTrialAndBlockDefs.AddInitializationMethod(() =>
        {
            CurrentTask.HandleTrialAndBlockDefs(true);
        });
        HandleTrialAndBlockDefs.SpecifyTermination(() => CurrentTask.TrialAndBlockDefsHandled, FindStims);

        FindStims.AddInitializationMethod(() =>
        {
            CurrentTask.TaskStims = new TaskStims();
            if (CurrentTask.PrefabStims == null)
                CurrentTask.PrefabStims = new StimGroup("PrefabStims");
            if (CurrentTask.PreloadedStims == null)
                CurrentTask.PreloadedStims = new StimGroup("PreloadedStims");
            if (CurrentTask.ExternalStims == null)
                CurrentTask.ExternalStims = new StimGroup("ExternalStims");
            CurrentTask.FindStims();
        });
        FindStims.SpecifyTermination(() => CurrentTask.StimsHandled, () => null);
    }

    public void ContinueToNextSetting()
    {
        importSettings_Level.importPaused = false;
    }

    private string SetFilePath(string searchString)
    {
        Debug.Log("Looking for file with search string " + searchString);
        
        string pathToFile;

        if (SessionValues.WebBuild && SessionValues.UsingDefaultConfigs)
            pathToFile = $"{SessionValues.ConfigFolderPath}/{CurrentTask.TaskName}_DefaultConfigs";
        else
            pathToFile = $"{SessionValues.ConfigFolderPath}/{CurrentTask.TaskName}"; //test for windows!

        if (SessionValues.ConfigAccessType == "Default" || SessionValues.ConfigAccessType == "Local")
            pathToFile = SessionValues.LocateFile.FindFilePathInExternalFolder(pathToFile, $"*{searchString}*");

        Debug.Log("Found file " + pathToFile);
        
        return pathToFile;
    }

    private void SetValuesForLoading(string searchString)
    {
        importSettings_Level.SettingsDetails[0].SearchString = searchString;
        SetFilePath(importSettings_Level.SettingsDetails[0].SearchString);

        switch (searchString.ToLower())
        {
            case "taskdef":
                importSettings_Level.SettingsDetails[0].SettingType = CurrentTask.TaskDefType;
                break;
            case "blockdef":
                importSettings_Level.SettingsDetails[0].SettingType = CurrentTask.BlockDefType;
                break;
            case "trialdef":
                importSettings_Level.SettingsDetails[0].SettingType = CurrentTask.TrialDefType;
                break;
            case "stimdef":
                importSettings_Level.SettingsDetails[0].SettingType = CurrentTask.StimDefType;
                break;
            case "eventcode":
                importSettings_Level.SettingsDetails[0].SettingType = typeof(Dictionary<string, EventCode>); //this correct for event code?
                break;
            case "configui":
                importSettings_Level.SettingsDetails[0].SettingType = typeof(ConfigVarStore); //this correct for ConfigUI?
                break;
            default:
                Debug.LogError("SET VALUES FOR LOADING DEFAULT SWITCH STATEMENT!");
                break;
        }
        
    }

    public void SettingsConverterTask<T>(object parsedSettings) where T: TaskDef
    {
        CurrentTask.TaskDef = (T) parsedSettings;
    }
    
    public void SettingsConverterBlock<T>(object parsedSettings) where T: BlockDef
    {
        CurrentTask.BlockDefs = (T[]) parsedSettings;
    }
    public void SettingsConverterTrial<T>(object parsedSettings) where T: TrialDef
    {
        CurrentTask.AllTrialDefs = (T[]) parsedSettings;
    }
    public void SettingsConverterStim<T>(object parsedSettings) where T: StimDef
    {
        CurrentTask.ExternalStims = new StimGroup("ExternalStims", (T[]) parsedSettings);
    }
}
