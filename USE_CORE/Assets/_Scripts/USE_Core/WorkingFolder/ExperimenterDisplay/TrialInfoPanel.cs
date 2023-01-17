using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using USE_Utilities;
using USE_ExperimenterDisplay;

public class TrialInfoPanel: ExperimenterDisplayPanel
{
    public TrialInfoList tiList;
    public GameObject trialInfoText;
    public GameObject trialInfoPanel;
    public Transform parentTransform;
    public string trialLevelNum;


    public override void CustomPanelInitialization()
    {
        tiList = new TrialInfoList();
        tiList.Initialize();
        
        trialInfoPanel = GameObject.Find("TrialInfoPanel");
        trialInfoText = GameObject.Find("TrialInfoPanelText");
        trialInfoText.transform.SetParent(trialInfoPanel.GetComponent<Transform>());
        
    }
    public override void CustomPanelUpdate()
    {
        if (TrialLevel != null)
        {
            trialInfoText.GetComponent<Text>().supportRichText = true;
            trialInfoText.GetComponent<Text>().text = "<size=24><b><color=#2d3436ff>Trial Info: </color></b></size>" + "\n<size=23><color=#2d3436ff>" + TrialLevel.TrialSummaryString + "</color></size>";
        }

    }


    public class TrialInfo
    {
        public string dataDescription;
        public string dataValue;

        public string GenerateTextDescription()
        {
            return (dataDescription + dataValue); // add "+ dataValue" eventually
        }
        
    }
    public class TrialInfoList
    {
       
        List<TrialInfo> TrialInfos = new List<TrialInfo>();
        public string GenerateTrialInfo()
        {
            string completeString = "";
            foreach (TrialInfo ti in TrialInfos)
            {
                completeString = completeString + ti.GenerateTextDescription() + "\n";
            }

            Debug.Log("TrialInfo: " + completeString);

            return completeString;
        }
        public void Initialize(Func<List<TrialInfo>> CustomTrialInfoList = null)
        {
            if (CustomTrialInfoList == null)
                TrialInfos = DefaultTrialInfoList(); //this is your default function
            else
                TrialInfos = CustomTrialInfoList(); //allows users to specify task-specific lists - this will end up looking something like the various task-specific classes like WWW_TaskDef or whatever

            //GenerateTextForPanel(); //method that loops through each hotkey and creates the string to show the hotkey options, using the GenerateTextDescription function of each on
        }
        public List<TrialInfo> DefaultTrialInfoList()
        {
            
            List<TrialInfo> TrialInfoList = new List<TrialInfo>();
            TrialInfo trialNumber = new TrialInfo
            {
                dataDescription = "Trial: "
            };
            TrialInfoList.Add(trialNumber);

            TrialInfo trialPerformance = new TrialInfo
            {
                dataDescription = "Trial Performance: "
            };
            TrialInfoList.Add(trialPerformance);

            return TrialInfoList;
        }
    }

    }
