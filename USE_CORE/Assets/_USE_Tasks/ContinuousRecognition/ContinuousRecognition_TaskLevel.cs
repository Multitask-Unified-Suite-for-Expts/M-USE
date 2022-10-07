using System;
using System.Collections.Generic;
using USE_ExperimentTemplate;
using ContinuousRecognition_Namespace;
using UnityEngine;
using UnityEngine.UI;
using USE_Settings;
using USE_StimulusManagement;
using static ContinuousRecognition_TrialLevel;

public class ContinuousRecognition_TaskLevel : ControlLevel_Task_Template
{   
    public override void SpecifyTypes()
    {
        TaskLevelType = typeof(ContinuousRecognition_TaskLevel);
        TrialLevelType = typeof(ContinuousRecognition_TrialLevel);
        TaskDefType = typeof(ContinuousRecognition_TaskDef);
        BlockDefType = typeof(ContinuousRecognition_BlockDef);
        TrialDefType = typeof(ContinuousRecognition_TrialDef);
        StimDefType = typeof(ContinuousRecognition_StimDef);
    } 
    public override void DefineControlLevel() //RUNS WHEN THE TASK IS DEFINED!
    {        
        ContinuousRecognition_TrialLevel trialLevel = (ContinuousRecognition_TrialLevel)TrialLevel;
        string TaskName = "ContinuousRecognition";
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ContextExternalFilePath"))
            trialLevel.MaterialFilePath = (String)SessionSettings.Get(TaskName + "_TaskSettings", "ContextExternalFilePath");

        StimGroup wrongGroup = new StimGroup("wrong");
        StimGroup rightGroup = new StimGroup("right");


        BlockFeedback.AddInitializationMethod(() =>
        {
            //Note: Unless they get so far they run out of trials, block feedback should only be displayed when they lose. 
            Debug.Log("BLOCK FEEDBACKKKKKKKKKKKKKKKKKKK INITIALIZING");

            //Get the current trial stim that were chosen. 
            //List<int> chosenStimIndices = TrialLevel.GetCurrentTrialDef<ContinuousRecognition_TrialDef>().PC_Stim;
            ////calculate total num to display (for the grid);
            //int totalNumToDisplay = chosenStimIndices.Count;
            ////Get the Index of the Stim they got wrong. 
            //int wrongStim_Index = TrialLevel.GetCurrentTrialDef<ContinuousRecognition_TrialDef>().WrongStimIndex;
            ////remove wrong stim from chosenStim so we can display it separately.
            //if (chosenStimIndices.Contains(wrongStim_Index)) chosenStimIndices.Remove(wrongStim_Index);
            ////NOW WE HAVE A LIST OF CHOSEN AND A LIST OF THE ONE THEY GOT WRONG. 

            //Text rightText = null;
            //Text wrongText = null;

            //put the one they got wrong at the top


            //Show the player BlockFB, where we display the PC Stim and the stim they chose incorrectly.
        });
        BlockFeedback.AddUpdateMethod(() =>
        {
            if (BlockFbFinished)
            {
                //displayGroup.DestroyStimGroup();
                //wrongGroup.DestroyStimGroup();
            }
        });
        BlockFeedback.AddTimer(() => 5f, () => null);

    }


    public T GetCurrentBlockDef<T>() where T : BlockDef
    {
        return (T)CurrentBlockDef;
    }
}
