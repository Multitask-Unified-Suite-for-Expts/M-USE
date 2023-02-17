using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
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
        SetSessionSummaryString();
        if(SessionSummaryString.Length > 0)
        {
            sessionInfoPanelText.GetComponent<Text>().supportRichText = true;
            sessionInfoPanelText.GetComponent<Text>().text = "\n<size=23><color=#2d3436ff>" + SessionSummaryString + "</color></size>";
        }
    }

    private void SetSessionSummaryString()
    {
        SessionSummaryString.Clear();
        SessionSummaryString.Append(
            "\nTotal Trials: " + totalTrials +
            "\nTotal Reward Pulses: " + totalRewardPulses +
            "\nSession Duration: " + sessionDuration +
            "\nTime From Last Trial Completion: " + timeFromLastTrialCompletion);
        if (TaskLevel != null)
            SessionSummaryString.AppendLine("Selected Configs: " + TaskSummaryString);
    }
}