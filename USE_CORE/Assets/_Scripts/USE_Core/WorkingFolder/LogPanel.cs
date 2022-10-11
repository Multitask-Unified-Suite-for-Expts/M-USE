using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using USE_ExperimenterDisplay;

public class LogPanel: ExperimenterDisplayPanel
{
    private Text logPanelText;
    private string logText = "";

    public override void CustomPanelInitialization()
    {
        Application.logMessageReceived += HandleLog;

        logPanelText = GameObject.Find("LogPanelText").GetComponent<Text>();
        logPanelText.supportRichText = true;
    }

    public override void CustomPanelUpdate()
    {
        logPanelText.text = "<size=25><color=#2d3436ff>" + logText + "</color></size>";
    }

    private void HandleLog(string condition, string stackTrace, LogType type) {
        if (type == LogType.Exception) {
            logText += condition + "\n";
        }
    }
}
