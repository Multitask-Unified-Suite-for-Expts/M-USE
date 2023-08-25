using USE_Settings;
using USE_StimulusManagement;
using USE_ExperimentTemplate_Task;
using USE_ExperimentTemplate_Block;
using Tetris_Namespace;
using UnityEngine;

public class Tetris_TaskLevel : ControlLevel_Task_Template
{
    Tetris_TrialLevel trialLevel;


    public override void SpecifyTypes()
    {
        TaskLevelType = typeof(Tetris_TaskLevel);
        TrialLevelType = typeof(Tetris_TrialLevel);
        TaskDefType = typeof(Tetris_TaskDef);
        BlockDefType = typeof(Tetris_BlockDef);
        TrialDefType = typeof(Tetris_TrialDef);
    }

    public override void DefineControlLevel()
    {
        trialLevel = (Tetris_TrialLevel)TrialLevel;
        SetSettings();
    }

    public void SetSettings()
    {
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonPosition"))
            trialLevel.ButtonPosition = (Vector3)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonPosition");
        else
            trialLevel.ButtonPosition = new Vector3(0, 0, 0);

        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ButtonScale"))
            trialLevel.ButtonScale = (float)SessionSettings.Get(TaskName + "_TaskSettings", "ButtonScale");
        else
            trialLevel.ButtonScale = 120f;

        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "TouchFeedbackDuration"))
            trialLevel.TouchFeedbackDuration = (float)SessionSettings.Get(TaskName + "_TaskSettings", "TouchFeedbackDuration");
        else
            trialLevel.TouchFeedbackDuration = .3f;
    }


}