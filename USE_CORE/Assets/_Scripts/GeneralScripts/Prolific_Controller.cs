using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class Prolific_Controller : MonoBehaviour
{


    private void Start()
    {
        SetProlificVariables();
    }

    private void SetProlificVariables()
    {
        string configName = GetQueryParam("config");
        //Session.Prolific_ConfigFolderName = configName;  //UNCOMMENT LATER
        Session.Prolific_ConfigFolderName = "CONFIGS_2-24-25";  //DELETE LATER
        Debug.LogWarning("PROLIFIC CONFIG NAME = " + Session.Prolific_ConfigFolderName);

        string prolificID = GetQueryParam("pid");
        //Session.Prolific_PlayerID = prolificID; //UN COMMENT LATER
        Session.Prolific_PlayerID = "21"; //TEMPORARY - DELETE LATER!!!!
        Debug.LogWarning("PROLIFIC PLAYER ID = " + Session.Prolific_PlayerID);
    }

    private string GetQueryParam(string key)
    {
        string url = Application.absoluteURL;

        if (string.IsNullOrEmpty(url))
        {
            Debug.LogWarning("URL IS NULL OR EMPTY");
            return null;
        }

        Debug.LogWarning("URL = " + url);

        int q = url.IndexOf('?');

        if (q < 0)
        {
            Debug.LogWarning("INDEX OF '?' LESS THAN 0");
            return null;
        }

        string query = url.Substring(q + 1); // everything after ?
        var parts = query.Split('&');
        foreach (var p in parts)
        {
            var kv = p.Split('=');
            if (kv.Length >= 2)
            {
                string k = UnityWebRequest.UnEscapeURL(kv[0]);
                string v = UnityWebRequest.UnEscapeURL(kv[1]);
                if (k == key)
                    return v;
            }
        }
        return null;
    }

}
