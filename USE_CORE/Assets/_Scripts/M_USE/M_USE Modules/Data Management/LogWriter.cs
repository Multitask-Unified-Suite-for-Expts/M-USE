using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


public class LogWriter : MonoBehaviour //Class is a component attached to "MiscScripts" GameObject. 
{
    private List<string> LogMessages = new List<string>();

    public bool LogFolderCreated;
    public bool LogFileCreated;
    private readonly int Capacity = 100;

    private string ServerLogFolderPath
    {
        get
        {
            if (ServerManager.SessionDataFolderPath == null)
            {
                Debug.Log("Trying to Get ServerLogFolderPath but ServerManager.SessionDataFolderPath hasnt been set yet!");
                return null;
            }
            else
                return $"{ServerManager.SessionDataFolderPath}/LogFile";
        }
    }

    private string LocalLogFolderPath
    {
        get
        {
            if (SessionValues.SessionDataPath == null)
            {
                Debug.Log("Trying to Get LocalLogFolderPath but SessionValues.SessionDataPath hasnt been set yet!");
                return null;
            }
            else
                return SessionValues.SessionDataPath + Path.DirectorySeparatorChar + "LogFile";
        }
    }

    private string ServerLogFilePath
    {
        get
        {
            if (ServerManager.SessionDataFolderPath == null)
            {
                Debug.Log("Trying to Get ServerLogFilePath but ServerManager.SessionDataFolderPath hasnt been set yet!");
                return null;
            }
            else
                return $"{ServerManager.SessionDataFolderPath}/LogFile/Player.log";
        }
    }

    private string LocalLogFilePath
    {
        get
        {
            if (SessionValues.SessionDataPath == null)
            {
                Debug.Log("Trying to Get LocalLogFilePath but SessionValues.SessionDataPath hasnt been set yet!");
                return null;
            }
            else
                return SessionValues.SessionDataPath + Path.DirectorySeparatorChar + "LogFile" + Path.DirectorySeparatorChar + "Player.log";
        }
    }

    [HideInInspector] public bool StoreData; //Set by SessionLevel


    private void Start()
    {
        Application.logMessageReceived += HandleLogMessage;
        Application.quitting += OnApplicationQuit;
    }

    private void HandleLogMessage(string logMessage, string stackTrace, LogType type)
    {
        if (!StoreData)
            return;

        if(!LogFolderCreated)
            StartCoroutine(CreateLogFolder());

        LogMessages.Add(logMessage);

        if (LogMessages.Count >= Capacity)
        {
            if(!LogFileCreated)
                StartCoroutine(CreateLogFile());
            else
                StartCoroutine(AppendDataToLogFile());
        }
    }


    private IEnumerator CreateLogFolder()
    {
        if (SessionValues.WebBuild)
        {
            if (ServerManager.SessionDataFolderCreated)
                yield return ServerManager.CreateFolder(ServerLogFolderPath);
        }
        else
        {
            Directory.CreateDirectory(LocalLogFolderPath);
        }
        LogFolderCreated = true;
    }

    private void WriteLogMessages(StreamWriter writer)
    {
        if (LogMessages.Count > 0)
        {
            foreach (string message in LogMessages)
                writer.WriteLine(message);
            LogMessages.Clear();
        }
    }


    private IEnumerator CreateLogFile()
    {
        if (SessionValues.WebBuild)
        {
            string content = string.Join("\n", LogMessages.ToArray());
            LogMessages.Clear();
            yield return ServerManager.CreateFileAsync(ServerLogFilePath, "Player.log", content);
        }
        else
        {
            using StreamWriter createFileWriter = File.CreateText(LocalLogFilePath);
            WriteLogMessages(createFileWriter);
        }
        LogFileCreated = true;
    }

    private IEnumerator AppendDataToLogFile()
    {
        if (SessionValues.WebBuild)
        {
            string content = string.Join("\n", LogMessages.ToArray());
            LogMessages.Clear();
            yield return ServerManager.AppendToFileAsync(ServerLogFilePath, "Player.log", content);
        }
        else
        {
            using StreamWriter appendFileWriter = File.AppendText(LocalLogFilePath);
            WriteLogMessages(appendFileWriter);
        }
    }

    private void OnApplicationQuit()
    {
        if (!StoreData)
            return;

        if (LogFileCreated)
            StartCoroutine(AppendDataToLogFile());
        else
            StartCoroutine(CreateLogFile());
    }


    private void OnDestroy()
    {
        Application.logMessageReceived -= HandleLogMessage;
        Application.quitting -= OnApplicationQuit;
    }

}
