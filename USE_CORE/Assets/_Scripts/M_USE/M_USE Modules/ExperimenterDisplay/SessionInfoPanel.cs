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



using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using UnityEngine.UI;
using USE_ExperimenterDisplay;

public class SessionInfoPanel : ExperimenterDisplayPanel
{
    public GameObject sessionInfoPanel;
    public GameObject sessionInfoPanelText;
    public int totalTrials;
    public int totalRewardPulses;
    public float sessionDuration;
    public float timeFromLastTrialCompletion;
    public StringBuilder TaskSummaryString;
    public StringBuilder SessionSummaryString;

    public override void CustomPanelInitialization()
    {
        sessionInfoPanel = GameObject.Find("SessionInfoPanel");
        sessionInfoPanelText = GameObject.Find("SessionInfoPanelText");
        SessionSummaryString = new StringBuilder();
        TaskSummaryString = new StringBuilder();
    }

    public override void CustomPanelUpdate()
    {
        float currentDuration = Time.time - SessionLevel.StartTimeAbsolute;
        UpdateSessionSummaryValues(("sessionDuration", currentDuration));
        if (TrialLevel != null && totalTrials > 1)
        {
            float timeOfLastTrialCompletion = Time.time - TrialLevel.TrialCompleteTime;
            UpdateSessionSummaryValues(("timeFromLastTrialCompletion",timeOfLastTrialCompletion));
        }
        else if (totalTrials > 1)
        {
            // Makes sure that the player has completed atleast one trial in the session
            float timeOfLastTrialCompletion = Time.time - SessionLevel.GetStateFromName("SelectTask").TimingInfo.StartTimeRelative;
            UpdateSessionSummaryValues(("timeFromLastTrialCompletion", timeOfLastTrialCompletion));  
        }
                  
        SetSessionSummaryString();
        if(TaskSummaryString.Length > 0)
            SessionSummaryString.AppendLine(TaskSummaryString.ToString());
        if(SessionSummaryString.Length > 0)
        {
            sessionInfoPanelText.GetComponent<Text>().supportRichText = true;
            sessionInfoPanelText.GetComponent<Text>().text = "<size=23><color=#2d3436ff>" + SessionSummaryString + "</color></size>";
        }
    }

    private void SetSessionSummaryString()
    {
        SessionSummaryString.Clear();
        SessionSummaryString.Append(
            "Total Trials: " + totalTrials +
            "\nTotal Reward Pulses: " + totalRewardPulses +
            "\nSession Duration: " + String.Format("{0:0.0}", sessionDuration) + " s" +
            "\nTime From Last Trial Completion: " + String.Format("{0:0.0}", timeFromLastTrialCompletion) + " s");
        
        TaskSummaryString.Clear();
        if (TaskLevel != null)
        {
            TaskSummaryString.Append("<b>\n\nSelected Configs: </b>" + TaskLevel.CurrentTaskSummaryString);
        }
        if (SessionLevel.PreviousTaskSummaryString.Length > 0)
            TaskSummaryString.AppendLine(SessionLevel.PreviousTaskSummaryString.ToString());

    }

    public void UpdateSessionSummaryValues(params (string, object)[] valuesToUpdate)
    {
        foreach ((string variableName, object changeValue) in valuesToUpdate)
        {
            switch (variableName)
            {
                case nameof(totalTrials):
                    if (changeValue is int trialsIncrement && trialsIncrement > 0)
                        totalTrials += trialsIncrement;
                    break;
                case nameof(totalRewardPulses):
                    if (changeValue is int rewardPulsesIncrement && rewardPulsesIncrement > 0)
                        totalRewardPulses += rewardPulsesIncrement;
                    break;
                case nameof(sessionDuration):
                    if (changeValue is float currentSessionDuration && currentSessionDuration > 0)
                        sessionDuration = currentSessionDuration;
                    break;
                case nameof(timeFromLastTrialCompletion):
                    if (changeValue is float timeOfLastTrialCompletion && timeOfLastTrialCompletion > 0)
                        timeFromLastTrialCompletion = timeOfLastTrialCompletion;
                    break;
            }
        }
    }

}