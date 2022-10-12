using System;
using System.Collections.Generic;
using USE_ExperimentTemplate;
using ConReg_Namespace;
using UnityEngine;
using UnityEngine.UI;
using USE_Settings;
using USE_StimulusManagement;

public class ConReg_TaskLevel : ControlLevel_Task_Template
{
    public override void SpecifyTypes() //Specifies the types of any custom classes. 
    {
        TaskLevelType = typeof(ConReg_TaskLevel);
        TrialLevelType = typeof(ConReg_TrialLevel);
        TaskDefType = typeof(ConReg_TaskDef);
        BlockDefType = typeof(ConReg_BlockDef);
        TrialDefType = typeof(ConReg_TrialDef);
        StimDefType = typeof(ConReg_StimDef);
    }
    public override void DefineControlLevel() //Runs when the task is defined!
    {
        // StimGroup display;
        StimGroup wrongGroup = new StimGroup("wrong");
        StimGroup displayGroup = new StimGroup("display");
        ConReg_TrialLevel trialLevel = (ConReg_TrialLevel)TrialLevel;
        string TaskName = "ContinuousRecognition";
        if (SessionSettings.SettingExists(TaskName + "_TaskSettings", "ContextExternalFilePath"))
            trialLevel.MaterialFilePath = (String) SessionSettings.Get(TaskName + "_TaskSettings", "ContextExternalFilePath");

        
        BlockFeedback.AddInitializationMethod(() =>
        {
            // THE NUMBER THAT MEASURE PERFORMANCE
            
            BlockFbSimpleDuration = 5f;
            List<int> chosenStim = TrialLevel.GetCurrentTrialDef<ConReg_TrialDef>().PreviouslyChosenStim;
            bool isStimNew = TrialLevel.GetCurrentTrialDef<ConReg_TrialDef>().isNewStim;
            Text chosenText = null;
            Text wrongText = null;
            
            int len = chosenStim.Count;
            int row = len / 6 + 1;
            int col = 0;
            if (len > 6) col = 6;
            else col = len;
          
            Vector3[] loc_arr = new Vector3[row*col];
            
            // calculate horizontal and vertical offset
            //maybe make them available to user. 
            float horizontal = 12f/(row*2);
            float vertical = 7.7f/(col*2);
            int gridIndex = 0;
            // edges
            float x = -3;
            float y = 2;
            float z = 0;
            
            // create grid by filling in location array
            for (int i = 0; i < row; i++)
            {
                x = -3;
                for (int j = 0; j < col; j++)
                {
                    loc_arr[gridIndex] = new Vector3(x, y, z);
                    x += horizontal;
                    gridIndex++;
                }
                y -= vertical;
            }

            Vector3[] loc;
            if (!isStimNew)
            {
                List<int> sublist = chosenStim.GetRange(0, len - 1);
                displayGroup = new StimGroup("display", ExternalStims, sublist);
                loc = new Vector3[len - 1];
                for (int i = 0; i < len-1; i++)
                {
                    loc[i] = loc_arr[i];
                }
                Vector3 wrong_loc = new Vector3(-5, y - vertical, 0);
                Vector3[] wrong_arr = new Vector3[1];
                wrong_arr[0] = wrong_loc;
                wrongGroup = new StimGroup("wrong", ExternalStims, chosenStim.GetRange(len - 1, 1));
                wrongGroup.AddStims(ExternalStims, chosenStim.GetRange(len-1, 1));
                wrongGroup.SetLocations(wrong_arr);
                wrongGroup.LoadStims();
                wrongGroup.ToggleVisibility(true);
                wrongText = GameObject.Find("WrongText").GetComponent<Text>();
                wrongText.text = "Wrong Stim:";
            }
            else
            {
                displayGroup.AddStims(ExternalStims, chosenStim);
                loc = new Vector3[len];
                for (int i = 0; i < len; i++)
                {
                    loc[i] = loc_arr[i];
                }
            }
            chosenText = GameObject.Find("ChosenText").GetComponent<Text>();
            chosenText.text = "Chosen:";
            displayGroup.SetLocations(loc);
            displayGroup.LoadStims();
            displayGroup.ToggleVisibility(true);
        });
        
        BlockFeedback.AddUpdateMethod(() =>
        {
            if (BlockFbFinished)
            {
                displayGroup.DestroyStimGroup();
                wrongGroup.DestroyStimGroup();
            }
        });
        BlockFeedback.AddTimer(() => 5f, () => null);
    }
}