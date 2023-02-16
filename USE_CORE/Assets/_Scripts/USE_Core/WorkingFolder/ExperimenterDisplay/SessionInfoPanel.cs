using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Serialization;
using UnityEngine.UI;
using USE_ExperimenterDisplay;

public class SessionInfoPanel : ExperimenterDisplayPanel
{
    public GameObject sessionInfoPanel;
    public GameObject sessionInfoPanelText;

    // Start is called before the first frame update
    public override void CustomPanelInitialization()
    {
        sessionInfoPanel = GameObject.Find("SessionInfoPanel");
        sessionInfoPanelText = GameObject.Find("SessionInfoPanelText");
    }

    public override void CustomPanelUpdate()
    {
        if (TaskLevel != null)
        {
            Debug.Log("IN THE SESSION INFO PANEL IF1");
            if(SessionLevel.SessionSummaryString.Length > 0)
            {
                sessionInfoPanelText.GetComponent<Text>().supportRichText = true;
                Debug.Log("IN THE SESSION INFO PANEL IF2");
                sessionInfoPanelText.GetComponent<Text>().text = "\n<size=23><color=#2d3436ff>" + SessionLevel.SessionSummaryString + "</color></size>";
            }
        }
            
    }
}

