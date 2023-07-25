using System;
using System.Collections.Generic;
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
            Debug.Log("STARTING IMPORT SETTINGS STATE!");
            Debug.Log("CURRENT TASK: " + CurrentTask);
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
            SetValuesForLoading("TaskDef");
        });

        ImportSettings.AddUpdateMethod(() =>
        {
            if (importSettings_Level.fileLoaded)
            {
                importSettings_Level.importPaused = false;
            }
            if (importSettings_Level.fileParsed)
            {
                currentFileName = importSettings_Level.SettingsDetails[0].FileName;
                parsedResult = importSettings_Level.parsedResult;
                fileParsed = true;

                if (currentFileName.ToLower().Contains("taskdef"))
                    SetValuesForLoading("BlockDef");
                else if (currentFileName.ToLower().Contains("blockdef"))
                    SetValuesForLoading("TrialDef");
                else if (currentFileName.ToLower().Contains("trialdef"))
                    SetValuesForLoading("StimDef");
                else if (currentFileName.ToLower().Contains("stimdef"))
                    SetValuesForLoading("EventCode");
                else if (currentFileName.ToLower().Contains("eventcode"))
                    SetValuesForLoading("ConfigUi");
                else if (currentFileName.ToLower().Contains("configui"))
                    Debug.Log("Parsed ConfigUI and no more to parse");

                else
                    Debug.LogError($"{currentFileName} has been parsed, but is not a TaskDef, BlockDef, TrialDef, StimDef, EventCode, or ConfigUI.");
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
        string pathToFolder;

        if (SessionValues.WebBuild && SessionValues.UsingDefaultConfigs)
            pathToFolder = $"{SessionValues.ConfigFolderPath}/{CurrentTask.TaskName}_DefaultConfigs";
        else
            pathToFolder = $"{SessionValues.ConfigFolderPath}/{CurrentTask.TaskName}"; //test for windows!

        if (SessionValues.ConfigAccessType == "Default" || SessionValues.ConfigAccessType == "Local")
            return importSettings_Level.SettingsDetails[0].FilePath = SessionValues.LocateFile.FindFilePathInExternalFolder(pathToFolder, $"*{searchString}*");
        else //Server
            return importSettings_Level.SettingsDetails[0].FilePath = pathToFolder;
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

}
