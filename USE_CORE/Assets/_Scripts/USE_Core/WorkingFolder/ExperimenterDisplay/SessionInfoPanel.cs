using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Serialization;
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

    // Start is called before the first frame update
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
            "\nSession Duration: " + sessionDuration +
            "\nTime From Last Trial Completion: " + timeFromLastTrialCompletion);
        
        TaskSummaryString.Clear();
        if (TaskLevel != null)
            TaskSummaryString.Append("<b>\n\nSelected Configs: </b>" + TaskLevel.ConfigName);
    }

    public void UpdateSessionSummaryValues(params (string, object)[] valuesToUpdate)
    {
        foreach ((string variableName, object changeValue) in valuesToUpdate)
        {
            switch (variableName)
            {
                case nameof(totalTrials):
                    if (changeValue is int trialsIncrement && trialsIncrement > 0)
                    {
                        totalTrials += trialsIncrement;
                    }
                    break;
                case nameof(totalRewardPulses):
                    if (changeValue is int rewardPulsesIncrement && rewardPulsesIncrement > 0)
                    {
                        totalRewardPulses += rewardPulsesIncrement;
                    }
                    break;
                case nameof(sessionDuration):
                    if (changeValue is float currentSessionDuration && currentSessionDuration > 0)
                    {
                        sessionDuration = currentSessionDuration;
                    }
                    break;
                case nameof(timeFromLastTrialCompletion):
                    if (changeValue is float timeOfLastTrialCompletion && timeOfLastTrialCompletion > 0)
                    {
                        timeFromLastTrialCompletion = timeOfLastTrialCompletion;
                    }
                    break;
            }
        }
    }

}