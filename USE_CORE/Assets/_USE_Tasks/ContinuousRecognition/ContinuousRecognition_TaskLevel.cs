using System.Collections.Generic;
using USE_ExperimentTemplate;
using ContinuousRecognition_Namespace;
using UnityEngine;
using UnityEngine.UI;
using USE_StimulusManagement;

public class ContinuousRecognition_TaskLevel : ControlLevel_Task_Template
{

    public override void SpecifyTypes()
    {
        //note that since EffortControl_TaskDef and EffortControl_BlockDef do not add any fields or methods to their parent types, 
        //they do not actually need to be specified here, but they are included to make this script more useful for later copying.
        TaskLevelType = typeof(ContinuousRecognition_TaskLevel);
        TrialLevelType = typeof(ContinuousRecognition_TrialLevel);
        TaskDefType = typeof(ContinuousRecognition_TaskDef);
        BlockDefType = typeof(ContinuousRecognition_BlockDef);
        TrialDefType = typeof(ContinuousRecognition_TrialDef);
        StimDefType = typeof(ContinuousRecognition_StimDef);
    }
    public override void DefineControlLevel()
    {
        StimGroup display;
        BlockFeedback.AddInitializationMethod(() =>
        {
            // THE NUMBER THAT MEASURE PERFORMANCE
            
            
            
            Camera.main.backgroundColor = Color.yellow;
            BlockFbSimpleDuration = 5f;
            List<int> chosen = TrialLevel.GetCurrentTrialDef<ContinuousRecognition_TrialDef>().PreviouslyChosenStimuli;
            bool n = TrialLevel.GetCurrentTrialDef<ContinuousRecognition_TrialDef>().isNewStim;
            Text chosenText = null;
            Text wrongText = null;
            
            int len = chosen.Count;
            int row = len / 6 + 1;
            int col = 0;
            if (len > 6)
            {
                col = 6;
            }
            else
            {
                col = len;
            }

            Vector3[] loc_arr = new Vector3[row*col];
            
            // calculate horizontal and vertical offset
            float horizontal = 12f/6;
            float vertical = 7.7f/6;
            int gridIndex = 0;
            // edges
            float x = -5;
            float y = 4;
            float z = 0;
            
            // create grid by filling in location array
            for (int i = 0; i < row; i++)
            {
                x = -5;
                for (int j = 0; j < col; j++)
                {
                    loc_arr[gridIndex] = new Vector3(x, y, z);
                    x += horizontal;
                    gridIndex++;
                }
                y -= vertical;
            }

            StimGroup d, wrong_group;
            Vector3[] loc;
            //Debug.Log("nnnnnnnnnnnnnnn isNew is: " + n);
            if (!n)
            {
                List<int> sublist = chosen.GetRange(0, len - 1);
                d = new StimGroup("display", ExternalStims, sublist);
                loc = new Vector3[len - 1];
                for (int i = 0; i < len-1; i++)
                {
                    loc[i] = loc_arr[i];
                }
                Vector3 wrong_loc = new Vector3(-5, y - vertical, 0);
                Vector3[] wrong_arr = new Vector3[1];
                wrong_arr[0] = wrong_loc;
                wrong_group = new StimGroup("wrong", ExternalStims, chosen.GetRange(len-1, 1));
                wrong_group.SetLocations(wrong_arr);
                wrong_group.LoadStims();
                wrong_group.ToggleVisibility(true);
                wrongText = GameObject.Find("WrongText").GetComponent<Text>();
                wrongText.text = "Wrong Stim:";
            }
            else
            {
                d = new StimGroup("display", ExternalStims, chosen);
                loc = new Vector3[len];
                for (int i = 0; i < len; i++)
                {
                    loc[i] = loc_arr[i];
                }
            }
            chosenText = GameObject.Find("ChosenText").GetComponent<Text>();
            chosenText.text = "Chosen:";
            d.SetLocations(loc);
            d.LoadStims();
            d.ToggleVisibility(true);
        });
        //BlockFeedback.AddTimer(()=> 5f, () => null);
    }
}