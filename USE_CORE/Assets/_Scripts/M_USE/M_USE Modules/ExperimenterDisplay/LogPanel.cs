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

    private void HandleLog(string condition, string stackTrace, LogType type)
    {
        if (Array.IndexOf(LOGGED_LOG_TYPES, type) > -1)
        {
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
