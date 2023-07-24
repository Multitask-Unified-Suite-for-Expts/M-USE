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

        ImportSettings.AddChildLevel(importSettings_Level);
        ImportSettings.AddInitializationMethod(() =>
        {
            Debug.Log("STARTING IMPORT SETTINGS STATE!");
            SetValuesForLoading("TaskDef");
        });

        ImportSettings.AddUpdateMethod(() =>
        {
            if (importSettings_Level.fileLoaded)
            {
                importSettings_Level.SettingsDetails.SettingParsingStyle = importSettings_Level.DetermineParsingStyle();
                importSettings_Level.continueToLoadFile = true;
            }
            if (importSettings_Level.fileParsed)
            {
                currentFileName = importSettings_Level.SettingsDetails.FileName;
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

        });
        HandleTrialAndBlockDefs.SpecifyTermination(() => true, FindStims);

        FindStims.AddInitializationMethod(() =>
        {

        });
        FindStims.SpecifyTermination(() => true, () => null);
    }

    public void ContinueToNextSetting()
    {
        importSettings_Level.continueToNextSetting = true;
    }

    private void SetFilePath(string searchString)
    {
        string pathToFolder;

        if (SessionValues.WebBuild && SessionValues.UsingDefaultConfigs)
            pathToFolder = $"{SessionValues.ConfigFolderPath}/{CurrentTask.TaskName}_DefaultConfigs";
        else
            pathToFolder = $"{SessionValues.ConfigFolderPath}/{CurrentTask.TaskName}"; //test for windows!

        if (SessionValues.ConfigAccessType == "Default" || SessionValues.ConfigAccessType == "Local")
            importSettings_Level.SettingsDetails.FilePath = SessionValues.LocateFile.FindFilePathInExternalFolder(pathToFolder, $"*{searchString}*");
        else //Server
            importSettings_Level.SettingsDetails.FilePath = pathToFolder;
    }

    private void SetValuesForLoading(string searchString)
    {
        importSettings_Level.SettingsDetails.SearchString = searchString;
        SetFilePath(importSettings_Level.SettingsDetails.SearchString);

        switch (searchString.ToLower())
        {
            case "taskdef":
                importSettings_Level.SettingsDetails.SettingType = typeof(TaskDef);
                break;
            case "blockdef":
                importSettings_Level.SettingsDetails.SettingType = typeof(BlockDef[]);
                break;
            case "trialdef":
                importSettings_Level.SettingsDetails.SettingType = typeof(TrialDef[]);
                break;
            case "stimdef":
                importSettings_Level.SettingsDetails.SettingType = typeof(StimDef[]);
                break;
            case "eventcode":
                importSettings_Level.SettingsDetails.SettingType = typeof(Dictionary<string, EventCode>); //this correct for event code?
                break;
            case "configui":
                importSettings_Level.SettingsDetails.SettingType = typeof(ConfigVarStore); //this correct for ConfigUI?
                break;
            default:
                Debug.LogError("SET VALUES FOR LOADING DEFAULT SWITCH STATEMENT!");
                break;
        }
    }

}
