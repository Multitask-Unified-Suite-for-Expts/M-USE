using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using USE_ExperimenterDisplay;

public class LogPanel: ExperimenterDisplayPanel
{
    private Text logPanelText;
    private string logText = "";
    private static LogPanel singleton;

    private readonly LogType[] LOGGED_LOG_TYPES = {LogType.Error, LogType.Exception};

    public override void CustomPanelInitialization()
    {
        Application.logMessageReceived += HandleLog;

        logPanelText = GameObject.Find("LogPanelText").GetComponent<Text>();
        logPanelText.supportRichText = true;

        singleton = this;
    }

    public override void CustomPanelUpdate()
    {
        logPanelText.text = "<size=25><color=#2d3436ff>" + logText + "</color></size>";
    }

    private void HandleLog(string condition, string stackTrace, LogType type) {
        if (Array.IndexOf(LOGGED_LOG_TYPES, type) > -1) {
            logText += condition + "\n";
        }
    }

    public static bool HasError()
    {
        if (singleton == null)
            return false;
        else
            return singleton.logText.Length > 0;
    }
}
