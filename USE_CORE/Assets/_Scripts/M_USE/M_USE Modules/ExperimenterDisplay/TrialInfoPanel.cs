/*
MIT License

Copyright (c) 2023 Multitask - Unified - Suite -for-Expts

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files(the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/




using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
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
        
    }
    public override void CustomPanelUpdate()
    {
        if (Session.TrialLevel != null)
        {
            trialInfoText.GetComponent<Text>().supportRichText = true;
            trialInfoText.GetComponent<Text>().text = "\n<size=23><color=#d9d9d9>" + Session.TrialLevel.TrialSummaryString + "</color></size>";
        }

    }


    public class TrialInfo
    {
        public string dataDescription;
        public string dataValue;

        public string GenerateTextDescription()
        {
            return dataDescription + dataValue;
        }
        
    }
    public class TrialInfoList
    {
        List<TrialInfo> TrialInfos = new List<TrialInfo>();
        public string GenerateTrialInfo()
        {
            string completeString = "";
            foreach (TrialInfo ti in TrialInfos)
                completeString = completeString + ti.GenerateTextDescription() + "\n";
            
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
