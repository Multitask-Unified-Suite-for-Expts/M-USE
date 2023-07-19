using System;
using System.Collections.Generic;
using UnityEngine;
using USE_Def_Namespace;
using USE_States;
using USE_StimulusManagement;

public class VerifyTask_Level : ControlLevel
{
    public ImportSettings_Level importSettings_Level;
    public bool fileParsed;
    public string currentFileName;
    public object parsedResult = null;


    public override void DefineControlLevel()
    {
        State ImportSettings = new State("ImportSettings");
        State HandleTrialAndBlockDefs = new State("HandleTrialAndBlockDefs");
        State FindStims = new State("FindStims");

        AddActiveStates(new List<State> { ImportSettings, HandleTrialAndBlockDefs, FindStims });

        ImportSettings.AddChildLevel(importSettings_Level);
        ImportSettings.AddInitializationMethod(() => SetValuesForLoading("TaskDef"));
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

                if (currentFileName == "TaskDef")
                    SetValuesForLoading("BlockDef");
                else if (currentFileName == "BlockDef")
                    SetValuesForLoading("TrialDef");
                else if (currentFileName == "TrialDef")
                    SetValuesForLoading("StimDef");
                else if (currentFileName == "StimDef")
                    Debug.Log("Parsed the final file (stim def), so not setting anymore values for loading");
                else
                    Debug.LogError($"{currentFileName} has been parsed, but is not a TaskDef, BlockDef, TrialDef, or StimDef.");
            }
        });
        ImportSettings.SpecifyTermination(() => ImportSettings.ChildLevel.Terminated, HandleTrialAndBlockDefs);

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

    private void SetFilePath(string fileName)
    {
        if (importSettings_Level.SettingsDetails == null)
            Debug.Log("SETTINGS DETAILS IS NULL!");

        if (SessionValues.ConfigAccessType == "Default" || SessionValues.ConfigAccessType == "Local")
            importSettings_Level.SettingsDetails.FilePath = SessionValues.LocateFile.FindFilePathInExternalFolder(SessionValues.ConfigFolderPath, $"*{fileName}*");
        else //Server
            importSettings_Level.SettingsDetails.FilePath = SessionValues.ConfigFolderPath;
    }

    private void SetValuesForLoading(string fileName)
    {
        importSettings_Level.SettingsDetails.FileName = fileName;
        SetFilePath(importSettings_Level.SettingsDetails.FileName);

        switch (fileName.ToLower())
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
            default:
                Debug.LogError("SET VALUES FOR LOADING DEFAULT SWITCH STATEMENT!");
                break;
        }

    }

}
