using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using USE_Utilities;

public class TrialInfoPanel:MonoBehaviour
{
    public TrialInfoList tiList;
    public GameObject trialInfoText;

    // Start is called before the first frame update
    void Start()
    {
        tiList = new TrialInfoList();
        tiList.Initialize();
        trialInfoText = transform.Find("TrialInfoPanelText").gameObject;
        trialInfoText.GetComponent<Text>().supportRichText = true;
        trialInfoText.GetComponent<Text>().text = "<size=35><b><color=#2962486>Trial Info</color></b></size>" + "\n<size=20>" + tiList.GenerateTrialInfo() + "</size>";
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public class TrialInfo
    {
        public string dataDescription;
        //public string dataValue;
        public string GenerateTextDescription()
        {
            return dataDescription; // add "+ dataValue" eventually
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
