using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class WebBuildLogWriter : MonoBehaviour
{
    private bool StoreLog;

    private List<string> logMessages = new List<string>();

    public bool logFolderCreated;
    public bool logFileCreated;
    private int capacity = 100;

    private void Start()
    {
        Application.logMessageReceived += HandleLogMessage;
        Application.quitting += OnApplicationQuit;

        #if (UNITY_WEBGL)
            StoreLog = true;
        #endif
    }

    private void HandleLogMessage(string logMessage, string stackTrace, LogType type)
    {
        if (!StoreLog)
            return;

        if (ServerManager.SessionDataFolderCreated && !logFolderCreated) //Create log folder once session data folder created.
            StartCoroutine(CreateLogFolder());

        logMessages.Add(logMessage);

        if (logMessages.Count >= capacity)
        {
            if (!logFileCreated)
                StartCoroutine(CreateLogFile());
            else
                StartCoroutine(AppendDataToLogFile());
        }
    }

    private void OnApplicationQuit()
    {
        if (!StoreLog)
            return;

        if (logFileCreated)
            StartCoroutine(AppendDataToLogFile());
        else
            StartCoroutine(CreateLogFile());
    }

    private IEnumerator CreateLogFolder()
    {
        logFolderCreated = true;
        yield return ServerManager.CreateFolder($"{ServerManager.SessionDataFolderPath}/LogFile");
    }

    private IEnumerator CreateLogFile()
    {
        string content = string.Join("\n", logMessages.ToArray());
        logMessages.Clear();
        logFileCreated = true;
        yield return ServerManager.CreateFileAsync($"{ServerManager.SessionDataFolderPath}/LogFile/Player.log", "Player.log", content);
    }

    private IEnumerator AppendDataToLogFile()
    {
        string content = string.Join("\n", logMessages.ToArray());
        logMessages.Clear();
        yield return ServerManager.AppendToFileAsync($"{ServerManager.SessionDataFolderPath}/LogFile", "Player.log", content);
    }


    private void OnDestroy()
    {
        Application.logMessageReceived -= HandleLogMessage;
        Application.quitting -= OnApplicationQuit;
    }

}
